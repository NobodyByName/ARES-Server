using System.Collections.Immutable;
using Ares.Common.Maps;
using Ares.GameDataExtract.Components;
using Ares.GameDataExtract.Packages;
using Ares.GameDataExtract.Scripts;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Game.Collision;
using Ares.Structs.Game.World;

namespace Ares.GameDataExtract.World;

/// <summary>
/// Reads a map's spawners of free-standing loot containers (<see cref="DynamicLootSpawner"/>): the
/// <c>AActorSpawner</c> actors of the persistent level and the cells whose weighted classes are all
/// <c>BP_SocketContainer_*</c> Blueprints. A spawner's points are its component's point list (each point composed over
/// the root component's world transform), or the root's own transform without one; <c>RandomActorCount</c> stays unset
/// when a block does not store it. Locations are rounded to 2 decimal places and rotations
/// to 3. Read every cell with <see cref="ReadCell"/>, then <see cref="Build"/>.
/// </summary>
internal sealed class DynamicLootSpawnerReader
{
    /// <summary>Name prefix of the socket container Blueprints a loot spawner places.</summary>
    private const string ContainerBlueprintPrefix = "BP_SocketContainer_";

    private const string SpawnerClass = "AActorSpawner";
    private const string ComponentClass = "UActorSpawnerComponent";
    private const string PointStruct = "FActorSpawnerPoint";
    private const string WeightedStruct = "FActorSpawnerWeightedActorClass";
    private const string ComponentName = "ActorSpawnerComponent";
    private const string PlacementEnum = "EActorSpawnerPlacementMethod";
    private const string RotationEnum = "EActorSpawnerRotationMethod";
    private const string ModeEnum = "EActorSpawnerMode";
    private const int LocationDigits = 2;
    private const int RotationDigits = 3;
    private const int PlacedActorChainLength = 3;
    private const int MaxClasses = 64;
    private const int MaxPoints = 1024;

    private static readonly PackageObjectIndex s_spawner = PackageObjectIndex.FromScriptPath("/Script/Angelscript.ActorSpawner");

    private readonly SceneResolver _scenes;
    private readonly string _mapPackage;
    private readonly ScriptMemberChain _spawner;
    private readonly ScriptMemberChain _component;
    private readonly ScriptMemberChain _point;
    private readonly ScriptMemberChain _weighted;
    private readonly int _pointListPlacement;
    private readonly int _volumesPlacement;
    private readonly int _randomRotation;
    private readonly List<Row> _rows = [];

    /// <summary>
    /// A reader for the cells of map package <paramref name="mapPackage"/>, with the member chains of <paramref name="layouts"/>.
    /// </summary>
    /// <exception cref="InvalidDataException">A class, struct or enum the reader needs changed.</exception>
    public DynamicLootSpawnerReader(SceneResolver scenes, ScriptLayouts layouts, string mapPackage)
    {
        _scenes = scenes;
        _mapPackage = mapPackage;
        _spawner = ScriptMemberChain.OfLeading(layouts, SpawnerClass, "Root", ComponentName, "PlacementMethod", "RotationMethod", "bSpawnOnBeginPlay",
            "bUseRoundConfiguration", "SpawnerMode", "RandomActorCount", "ActorType", "ActorTypes", "WeightedActorTypes");
        _component = ScriptMemberChain.OfLeading(layouts, ComponentClass, "PlacementMethod", "PlacementPointList");
        _point = ScriptMemberChain.OfLeading(layouts, PointStruct, "Transform", "Weight", "bIsLocked");
        _weighted = ScriptMemberChain.OfLeading(layouts, WeightedStruct, "ActorType", "Weight");
        string[] placements = ["SpawnerLocation", "PointList", "Volumes"];
        string[] rotations = ["SpawnerRotation", "Random"];
        ScriptMemberChain.ExpectEnum(layouts, PlacementEnum, placements);
        ScriptMemberChain.ExpectEnum(layouts, RotationEnum, rotations);
        ScriptMemberChain.ExpectEnum(layouts, ModeEnum, "RandomActor", "AllActors", "AllActorsAtOneLocation");
        _pointListPlacement = Array.IndexOf(placements, "PointList");
        _volumesPlacement = Array.IndexOf(placements, "Volumes");
        _randomRotation = Array.IndexOf(rotations, "Random");
    }

    /// <summary>Reads the loot spawners placed in <paramref name="cell"/>.</summary>
    /// <exception cref="InvalidDataException">A spawner or its component does not decode as expected.</exception>
    public void ReadCell(MapCell cell, CookedPackage package)
    {
        string cellName = MapCells.Name(cell, _mapPackage);
        string? dataLayer = MapCells.DataLayer(cell);
        foreach (ZenExport export in package.Exports.Where(export => export.Class == s_spawner))
        {
            if (ExportChains.Chain(package, export).Count != PlacedActorChainLength)
            {
                continue;
            }

            try
            {
                if (ReadSpawner(package, export, cellName, dataLayer) is { } row)
                {
                    _rows.Add(row);
                }
            }
            catch (InvalidDataException error)
            {
                throw new InvalidDataException($"{package.Header.ExportPath(export.Index)}: {error.Message}", error);
            }
        }
    }

    /// <summary>The spawners ordered by cell and actor name (ordinal).</summary>
    /// <exception cref="InvalidDataException">Two spawners share an actor name, or a stored count does not fit the points.</exception>
    public ImmutableArray<DynamicLootSpawner> Build()
    {
        DynamicLootSpawner[] spawners = [.. _rows
            .OrderBy(row => row.Cell, StringComparer.Ordinal)
            .ThenBy(row => row.Actor, StringComparer.Ordinal)
            .Select(row => new DynamicLootSpawner(row.Cell, row.Actor, row.DataLayer, row.Classes, row.Points, row.Count, row.RandomRotation))];
        if (spawners.FirstOrDefault(spawner => spawner.Count is { } count && (count < 1 || count > spawner.Points.Count)) is { } wrong)
        {
            throw new InvalidDataException($"{wrong.Actor}: {wrong.Count} actors from {wrong.Points.Count} points");
        }

        // The activity containers refer to spawners by actor name (soft paths into the persistent level).
        return spawners.Select(spawner => spawner.Actor).Distinct(StringComparer.Ordinal).Count() == spawners.Length
            ? [.. spawners]
            : throw new InvalidDataException("Spawner actor names are not unique across cells");
    }

    /// <summary>The spawner row, or <see langword="null"/> for a spawner of other classes than socket containers.</summary>
    private Row? ReadSpawner(CookedPackage package, ZenExport export, string cell, string? dataLayer)
    {
        (Dictionary<int, int> values, (int Reference, double Weight)[] weighted) = ReadMembers(package, export);
        (string? Class, double Weight)[] classes = [.. weighted.Select(entry => (ClassPath(package, entry.Reference), entry.Weight))];
        bool[] kinds = [.. classes.Select(entry => entry.Class is { } path && path[(path.LastIndexOf('/') + 1)..].StartsWith(ContainerBlueprintPrefix, StringComparison.Ordinal)).Distinct()];
        if (kinds is not [true])
        {
            return kinds.Contains(true) ? throw new InvalidDataException("container and other classes in one spawner") : null;
        }

        if (Int(values, "SpawnerMode") != 0 || Int(values, "bSpawnOnBeginPlay") != 0)
        {
            throw new InvalidDataException("spawner mode or spawn on begin play set");
        }

        int? placement = values.TryGetValue(_spawner.Index("PlacementMethod"), out int stored) ? stored : null;
        if (placement == _volumesPlacement || Int(values, "ActorType") != 0 || Int(values, "ActorTypes") != 0)
        {
            throw new InvalidDataException("volume placement or other class lists");
        }

        int rootValue = values.GetValueOrDefault(_spawner.Index("Root"));
        ZenExport rootExport = rootValue > 0 && rootValue <= package.Exports.Count ? package.Exports[rootValue - 1] : throw new InvalidDataException("no Root export");
        Transform world = _scenes.WorldTransform(package, rootExport);
        (int? method, List<(Vector3D Location, Rotator Rotation, double Weight, bool Locked)> points) = ReadPoints(package, export, world);
        if ((method is not null && method != 0 && method != _pointListPlacement) || (placement is not null && method is not null && method != 0 && method != placement))
        {
            throw new InvalidDataException($"placement methods {placement}/{method}");
        }

        if (placement == 0 || points.Count == 0)
        {
            points = [(world.Translation, TransformMath.ToRotator(world.Rotation), 1.0, false)];
        }

        if (points.Any(point => point.Weight <= 0 || point.Locked))
        {
            throw new InvalidDataException("a point with no weight or locked");
        }

        return new Row(
            cell,
            export.Name,
            dataLayer,
            [.. classes.Select(entry => new DynamicLootClass(entry.Class!, entry.Weight))],
            [.. points.Select(point => new DynamicLootPoint(DecimalRounding.Round(point.Location, LocationDigits), DecimalRounding.Round(point.Rotation, RotationDigits), point.Weight))],
            values.TryGetValue(_spawner.Index("RandomActorCount"), out int count) ? count : null,
            Int(values, "RotationMethod") == _randomRotation);
    }

    /// <summary>
    /// The spawner members before <c>WeightedActorTypes</c> by chain index (zero-masked ones as 0), and the weighted classes
    /// (empty when not stored).
    /// </summary>
    private (Dictionary<int, int> Values, (int Reference, double Weight)[] Weighted) ReadMembers(CookedPackage package, ZenExport export)
    {
        UnversionedHeader header = UnversionedHeader.Read(package.Data, CookedPackage.StartOf(export));
        PlacedActorValues reader = new(package, export, header);
        HashSet<int> references = [_spawner.Index("Root"), _spawner.Index(ComponentName)];
        HashSet<int> bytes = [_spawner.Index("PlacementMethod"), _spawner.Index("RotationMethod"), _spawner.Index("bSpawnOnBeginPlay"),
            _spawner.Index("bUseRoundConfiguration"), _spawner.Index("SpawnerMode")];
        int count = _spawner.Index("RandomActorCount");
        int weighted = _spawner.Index("WeightedActorTypes");
        Dictionary<int, int> values = [];
        (int, double)[] classes = [];
        foreach (UnversionedMember member in header.Members.TakeWhile(member => member.Index <= weighted))
        {
            if (!member.IsNonZero)
            {
                values[member.Index] = 0;
            }
            else if (references.Contains(member.Index) || member.Index == count)
            {
                values[member.Index] = reader.Int32();
            }
            else if (bytes.Contains(member.Index))
            {
                values[member.Index] = reader.Byte();
            }
            else if (member.Index == weighted)
            {
                classes = ReadWeightedClasses(package, reader);
            }
            else
            {
                throw new InvalidDataException($"{export.Name} sets member {member.Index} (not read)");
            }
        }

        return (values, classes);
    }

    private (int Reference, double Weight)[] ReadWeightedClasses(CookedPackage package, PlacedActorValues reader)
    {
        int count = reader.Count(MaxClasses);
        int typeIndex = _weighted.Index("ActorType");
        int weightIndex = _weighted.Index("Weight");
        (int, double)[] entries = new (int, double)[count];
        for (int i = 0; i < count; i++)
        {
            UnversionedHeader entry = reader.StructHeader(_weighted.Count);
            if (entry.Members.Count != 2 || !entry.IsNonZero(typeIndex) || !entry.IsNonZero(weightIndex))
            {
                throw new InvalidDataException("weighted class entry members");
            }

            int reference = reader.Int32();
            entries[i] = (reference, RawDouble(package, reader));
        }

        return entries;
    }

    /// <summary>The component's placement method (<see langword="null"/> when not listed) and its point list in world space.</summary>
    private (int? Method, List<(Vector3D Location, Rotator Rotation, double Weight, bool Locked)> Points) ReadPoints(CookedPackage package, ZenExport export, Transform world)
    {
        ZenExport component = package.Child(export.Index, ComponentName) ?? throw new InvalidDataException($"no {ComponentName}");
        UnversionedHeader header = UnversionedHeader.Read(package.Data, CookedPackage.StartOf(component));
        PlacedActorValues reader = new(package, component, header);
        int methodIndex = _component.Index("PlacementMethod");
        int listIndex = _component.Index("PlacementPointList");
        int? method = null;
        List<(Vector3D, Rotator, double, bool)> points = [];
        foreach (UnversionedMember member in header.Members)
        {
            if (member.Index == methodIndex)
            {
                method = member.IsNonZero ? reader.Byte() : 0;
            }
            else if (member.Index == listIndex)
            {
                if (!member.IsNonZero)
                {
                    continue;
                }

                int count = reader.Count(MaxPoints);
                for (int i = 0; i < count; i++)
                {
                    points.Add(ReadPoint(package, reader, world));
                }
            }
            else
            {
                throw new InvalidDataException($"component member {member.Index}");
            }
        }

        return (method, points);
    }

    private (Vector3D, Rotator, double, bool) ReadPoint(CookedPackage package, PlacedActorValues reader, Transform world)
    {
        int transformIndex = _point.Index("Transform");
        int weightIndex = _point.Index("Weight");
        int lockedIndex = _point.Index("bIsLocked");
        UnversionedHeader point = reader.StructHeader(_point.Count);
        if (point.Members.Count != 3 || !point.Contains(weightIndex) || !point.Contains(lockedIndex) || !point.IsNonZero(transformIndex))
        {
            throw new InvalidDataException("point members");
        }

        UnversionedHeader transform = reader.StructHeader(TransformMembers.MemberCount);
        if (transform.Members.Count != TransformMembers.MemberCount)
        {
            throw new InvalidDataException("point transform members");
        }

        Quaternion rotation = transform.IsNonZero(TransformMembers.Rotation) ? reader.Quaternion() : default;
        Vector3D translation = transform.IsNonZero(TransformMembers.Translation) ? RawVector(package, reader) : default;
        Vector3D scale = transform.IsNonZero(TransformMembers.Scale3D) ? RawVector(package, reader) : default;
        double weight = point.IsNonZero(weightIndex) ? RawDouble(package, reader) : 0.0;
        bool locked = point.IsNonZero(lockedIndex) && reader.Byte() != 0;
        Transform placed = TransformMath.Compose(new Transform(rotation, translation, scale), world);
        return (placed.Translation, TransformMath.ToRotator(placed.Rotation), weight, locked);
    }

    private int Int(Dictionary<int, int> values, string member) => values.GetValueOrDefault(_spawner.Index(member));

    /// <summary>The Blueprint class path (<c>Package.Name_C</c>) of an imported class reference, or <see langword="null"/> when it is not a package import.</summary>
    private static string? ClassPath(CookedPackage package, int reference)
    {
        if (!package.IsImportReference(reference) || package.Header.Imports[-reference - 1] is not { Kind: PackageObjectKind.PackageImport } import)
        {
            return null;
        }

        string path = package.Header.ImportedPackageNames[import.ImportedPackageIndex];
        return $"{path}.{path[(path.LastIndexOf('/') + 1)..]}_C";
    }

    /// <summary>A double as stored (a negative zero stays negative).</summary>
    private static double RawDouble(CookedPackage package, PlacedActorValues reader)
    {
        int at = reader.Offset;
        reader.Skip(sizeof(double));
        return package.ReadDouble(at);
    }

    /// <summary>A vector as stored (negative zeros stay negative).</summary>
    private static Vector3D RawVector(CookedPackage package, PlacedActorValues reader)
    {
        int at = reader.Offset;
        reader.Skip(3 * sizeof(double));
        return package.ReadVector(at);
    }

    private sealed record Row(
        string Cell,
        string Actor,
        string? DataLayer,
        DynamicLootClass[] Classes,
        DynamicLootPoint[] Points,
        int? Count,
        bool RandomRotation);
}
