using System.Collections.Immutable;
using System.Text;
using System.Text.RegularExpressions;
using Ares.Common.Maps;
using Ares.GameDataExtract.Components;
using Ares.GameDataExtract.Packages;
using Ares.GameDataExtract.Scripts;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Game.Collision;
using Ares.Structs.Game.World;

namespace Ares.GameDataExtract.World;

/// <summary>
/// Reads a map's loot placement limiters (<see cref="LootLimiter"/>): the <c>ALootPlacementLimiter</c> actors of the
/// persistent level and the cells. An actor gives its <c>Limits</c> (gameplay tag to <c>FLootContainerLimitConfig</c>)
/// and its label; its gatherer component (<c>ULootPlacementLimiterComponent</c>, attached to the actor's <c>Root</c>)
/// the gathering method, radius and volumes, and the world transform. A member a block does not store stays unset.
/// The volumes are persistent-level actors read as hulls (see <see cref="LootVolumeReader"/>). Locations are rounded to
/// 2 decimal places and rotations to 3. Read every cell with <see cref="ReadCell"/>, then <see cref="Build"/>.
/// </summary>
internal sealed partial class LootLimiterReader
{
    private const string LimiterClass = "ALootPlacementLimiter";
    private const string ComponentClass = "ULootPlacementLimiterComponent";
    private const string ConfigStruct = "FLootContainerLimitConfig";
    private const string MethodEnum = "EEnvironmentalLootSocketGatheringMethod";
    private const int LocationDigits = 2;
    private const int RotationDigits = 3;
    private const double ScaleTolerance = 1e-9;
    private const int MaxLimits = 64;
    private const int MaxVolumes = 64;
    private const int GameplayTagMembers = 1;

    private static readonly PackageObjectIndex s_limiter = PackageObjectIndex.FromScriptPath("/Script/Angelscript.LootPlacementLimiter");
    private static readonly PackageObjectIndex s_component = PackageObjectIndex.FromScriptPath("/Script/Angelscript.LootPlacementLimiterComponent");
    private static readonly PackageObjectIndex s_sceneComponent = PackageObjectIndex.FromScriptPath(SceneComponentMembers.ClassPath);

    /// <summary>The method names in <c>EEnvironmentalLootSocketGatheringMethod</c> order and the shape each gives.</summary>
    private static readonly (string Method, LootLimiterShape Shape)[] s_methods =
    [
        ("None", LootLimiterShape.None),
        ("WithinSphere", LootLimiterShape.Sphere),
        ("WithinBox", LootLimiterShape.Box),
        ("WithinVolume", LootLimiterShape.Volumes),
    ];

    private readonly SceneResolver _scenes;
    private readonly string _mapPackage;
    private readonly ScriptMemberChain _limiter;
    private readonly ScriptMemberChain _component;
    private readonly ScriptMemberChain _config;
    private readonly List<Row> _rows = [];

    /// <summary>
    /// A reader for the cells of map package <paramref name="mapPackage"/>, with the member chains of <paramref name="layouts"/>.
    /// </summary>
    /// <exception cref="InvalidDataException">A class, struct or enum the reader needs changed.</exception>
    public LootLimiterReader(SceneResolver scenes, ScriptLayouts layouts, string mapPackage)
    {
        _scenes = scenes;
        _mapPackage = mapPackage;
        _limiter = ScriptMemberChain.Of(layouts, LimiterClass, "Root", "LootContainerLimiterSocketGatherer", "Limits");
        _component = ScriptMemberChain.Of(layouts, ComponentClass, "SocketGatheringMethod", "SocketGatheringRadius", "Volumes");
        _config = ScriptMemberChain.Of(layouts, ConfigStruct, "bUnlimited", "Limit", "NumSpawned");
        ScriptMemberChain.ExpectEnum(layouts, MethodEnum, [.. s_methods.Select(method => method.Method)]);
    }

    /// <summary>Reads the limiters placed in <paramref name="cell"/>.</summary>
    /// <exception cref="InvalidDataException">A limiter or its components do not decode as expected.</exception>
    public void ReadCell(MapCell cell, CookedPackage package)
    {
        string cellName = MapCells.Name(cell, _mapPackage);
        string? dataLayer = MapCells.DataLayer(cell);
        foreach (ZenExport export in package.Exports.Where(export => export.Class == s_limiter))
        {
            string actor = ExportChains.PlacedActorName(package, export) ?? throw new InvalidDataException($"{package.Header.ExportPath(export.Index)} is not a placed actor");
            try
            {
                _rows.Add(ReadLimiter(package, export, cellName, actor, dataLayer));
            }
            catch (InvalidDataException error)
            {
                throw new InvalidDataException($"{package.Header.ExportPath(export.Index)}: {error.Message}", error);
            }
        }
    }

    /// <summary>The limiters ordered by cell and actor name (ordinal), with the hulls of their volumes from the persistent level <paramref name="level"/>.</summary>
    /// <exception cref="InvalidDataException">A volume does not decode as expected.</exception>
    public ImmutableArray<LootLimiter> Build(CookedPackage level)
    {
        HashSet<string> names = [.. _rows.SelectMany(row => row.Volumes)];
        IReadOnlyDictionary<string, LootVolumeHulls> volumes = LootVolumeReader.Read(_scenes, level, names, LootVolumeReader.LimiterMaxElements, skipFlat: false);
        return [.. _rows
            .OrderBy(row => row.Cell, StringComparer.Ordinal)
            .ThenBy(row => row.Actor, StringComparer.Ordinal)
            .Select(row => new LootLimiter(
                row.Cell,
                row.Actor,
                row.Label,
                row.DataLayer,
                row.Shape,
                row.Location,
                row.Rotation,
                row.Radius,
                [.. row.Volumes.Select(volume => new LootLimiterVolume(volume, volumes[volume].Hulls))],
                row.Limits))];
    }

    private Row ReadLimiter(CookedPackage package, ZenExport export, string cell, string actor, string? dataLayer)
    {
        UnversionedHeader header = UnversionedHeader.Read(package.Data, CookedPackage.StartOf(export));
        int rootComponentIndex = _limiter.Count + ActorMembers.RootComponent;
        int[] expected = [_limiter.Index("Root"), _limiter.Index("LootContainerLimiterSocketGatherer"), _limiter.Index("Limits"), rootComponentIndex];
        if (!header.Members.Select(member => member.Index).SequenceEqual(expected) || header.Members.Any(member => !member.IsNonZero))
        {
            throw new InvalidDataException($"actor members ({string.Join(", ", header.Members.Select(member => member.Index))})");
        }

        PlacedActorValues values = new(package, export, header);
        int rootValue = values.Reference();
        int componentValue = values.Reference();
        LootLimit[] limits = ReadLimits(values);
        int rootComponent = values.Reference();
        string label = values.ActorTrailer();
        ZenExport root = OwnExport(package, rootValue, "Root");
        ZenExport component = OwnExport(package, componentValue, "gatherer component");
        string[] own = [.. package.ChildrenOf(export.Index).Select(child => child.Name).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)];
        string[] expectedOwn = [.. new[] { root.Name, component.Name }.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)];
        if (rootComponent != rootValue || root.Class != s_sceneComponent || component.Class != s_component || !own.SequenceEqual(expectedOwn, StringComparer.Ordinal)
            || !IsOwnedBy(root, export) || !IsOwnedBy(component, export))
        {
            throw new InvalidDataException($"unexpected components ({string.Join(", ", own)})");
        }

        (byte? method, double? radius, string[] volumes, int? parent) = ReadComponent(package, component);
        if (parent != rootValue)
        {
            throw new InvalidDataException("the gatherer component is not attached to Root");
        }

        LootLimiterShape? shape = method is { } stored ? s_methods[stored].Shape : null;
        SceneMembers scene = _scenes.Scene(package, root);
        if (scene.Parent is { } rootParent && (rootParent != 0 || shape is not (LootLimiterShape.None or LootLimiterShape.Volumes)))
        {
            throw new InvalidDataException(rootParent != 0 ? "Root has an attach parent" : "a sphere or box limiter lost its attach parent in the cook");
        }

        Transform world = TransformMath.Compose(TransformMath.FromParts(default, default, Vector3D.One), _scenes.WorldTransform(package, root));
        if (Math.Abs(world.Scale3D.X - 1) > ScaleTolerance || Math.Abs(world.Scale3D.Y - 1) > ScaleTolerance || Math.Abs(world.Scale3D.Z - 1) > ScaleTolerance)
        {
            throw new InvalidDataException("scaled limiter");
        }

        if ((shape == LootLimiterShape.Volumes) != (volumes.Length > 0))
        {
            throw new InvalidDataException($"method {method} with {volumes.Length} volumes");
        }

        return new Row(cell, actor, label, dataLayer, shape, DecimalRounding.Round(world.Translation, LocationDigits),
            DecimalRounding.Round(TransformMath.ToRotator(world.Rotation), RotationDigits), radius, volumes, limits);
    }

    /// <summary>The <c>Limits</c> map: per gameplay tag, the members the entry stores (the others unset).</summary>
    private LootLimit[] ReadLimits(PlacedActorValues values)
    {
        int removed = values.Int32();
        int count = values.Int32();
        if (removed != 0 || count is <= 0 or > MaxLimits)
        {
            throw new InvalidDataException($"limits map header {removed}, {count}");
        }

        LootLimit[] limits = new LootLimit[count];
        int unlimitedIndex = _config.Index("bUnlimited");
        int limitIndex = _config.Index("Limit");
        for (int i = 0; i < count; i++)
        {
            UnversionedHeader tag = values.StructHeader(GameplayTagMembers);
            if (tag.Members is not [{ Index: 0, IsNonZero: true }])
            {
                throw new InvalidDataException("unexpected gameplay tag members");
            }

            string name = values.Name();
            UnversionedHeader config = values.StructHeader(_config.Count);
            if (config.Members.Any(member => member.Index != unlimitedIndex && member.Index != limitIndex))
            {
                throw new InvalidDataException($"limit config members ({string.Join(", ", config.Members.Select(member => member.Index))})");
            }

            // A member missing from the header stays unset; a zero-masked one is zero (false).
            bool? unlimited = config.Contains(unlimitedIndex) ? config.IsNonZero(unlimitedIndex) && Bool(values.Byte()) : null;
            int? limit = config.Contains(limitIndex) ? config.IsNonZero(limitIndex) ? values.Int32() : 0 : null;
            limits[i] = limit is not < 0 ? new LootLimit(name, unlimited, limit) : throw new InvalidDataException($"limit {limit}");
        }

        return limits;
    }

    /// <summary>Gathering method and radius (null when not stored), volume actor names and attach parent of a gatherer component.</summary>
    private (byte? Method, double? Radius, string[] Volumes, int? Parent) ReadComponent(CookedPackage package, ZenExport export)
    {
        UnversionedHeader header = UnversionedHeader.Read(package.Data, CookedPackage.StartOf(export));
        int methodIndex = _component.Index("SocketGatheringMethod");
        int radiusIndex = _component.Index("SocketGatheringRadius");
        int volumesIndex = _component.Index("Volumes");
        int parentIndex = _component.Count + SceneComponentMembers.AttachParent;
        if (header.Members.Any(member => member.Index != methodIndex && member.Index != radiusIndex && member.Index != volumesIndex && member.Index != parentIndex))
        {
            throw new InvalidDataException($"component members ({string.Join(", ", header.Members.Select(member => member.Index))})");
        }

        PlacedActorValues values = new(package, export, header);
        byte? method = header.Contains(methodIndex) ? header.IsNonZero(methodIndex) ? values.Byte() : (byte)0 : null;
        double? radius = header.Contains(radiusIndex) ? header.IsNonZero(radiusIndex) ? values.Double() : 0.0 : null;
        List<string> volumes = [];
        if (header.IsNonZero(volumesIndex))
        {
            int count = values.Count(MaxVolumes);
            for (int i = 0; i < count; i++)
            {
                string volumePackage = values.Name();
                string asset = values.Name();
                string subPath = SubPath(package, values);
                Match match = VolumePath().Match(subPath);
                if (volumePackage != _mapPackage || asset != _mapPackage[(_mapPackage.LastIndexOf('/') + 1)..] || !match.Success)
                {
                    throw new InvalidDataException($"volume {volumePackage} {asset} {subPath} is not a persistent level actor");
                }

                volumes.Add(match.Groups[1].Value);
            }
        }

        int? parent = header.IsNonZero(parentIndex) ? values.Reference() : null;
        return values.Remaining == ComponentNativeData.HeadSize && (method is null || method < s_methods.Length)
            ? (method, radius, [.. volumes], parent)
            : throw new InvalidDataException("the component block does not end where expected");
    }

    /// <summary>
    /// The sub-path string of a soft object path: an int32 byte length, then that many one-byte characters, trailing zeros
    /// dropped (the terminator may be missing).
    /// </summary>
    /// <exception cref="InvalidDataException">The length is negative or runs past the export.</exception>
    public static string SubPath(CookedPackage package, PlacedActorValues values)
    {
        int length = values.Int32();
        if (length < 0)
        {
            throw new InvalidDataException($"Unsupported sub-path length {length}");
        }

        int at = values.Offset;
        values.Skip(length);
        return Encoding.UTF8.GetString(package.Data.AsSpan(at, length).TrimEnd((byte)0));
    }

    private static bool IsOwnedBy(ZenExport child, ZenExport actor) => child.Outer.Kind == PackageObjectKind.Export && child.Outer.ExportIndex == actor.Index;

    private static bool Bool(byte value) => value switch
    {
        0 => false,
        1 => true,
        _ => throw new InvalidDataException($"bool byte {value}"),
    };

    private static ZenExport OwnExport(CookedPackage package, int value, string member)
    {
        return value > 0 && value <= package.Exports.Count ? package.Exports[value - 1] : throw new InvalidDataException($"{member} is not an export of the package");
    }

    [GeneratedRegex(@"^PersistentLevel\.([A-Za-z0-9_]+)$", RegexOptions.CultureInvariant)]
    private static partial Regex VolumePath();

    private sealed record Row(
        string Cell,
        string Actor,
        string Label,
        string? DataLayer,
        LootLimiterShape? Shape,
        Vector3D Location,
        Rotator Rotation,
        double? Radius,
        string[] Volumes,
        LootLimit[] Limits);
}
