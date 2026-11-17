using System.Collections.Immutable;
using Ares.Common.Maps;
using Ares.GameDataExtract.Packages;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Game.Collision;
using Ares.Structs.Game.World;

namespace Ares.GameDataExtract.World;

/// <summary>
/// Reads the ziplines placed in a map's generated cells: rideable lines (<see cref="ZiplinePlacement"/>, actors with
/// every <see cref="ZiplineClasses.LineComponents"/> component, named <see cref="ZiplineClasses.LineActorPrefix"/>…)
/// and launchers (<see cref="ActivatableZiplinePlacement"/>, <see cref="ZiplineClasses.LauncherComponents"/>,
/// <see cref="ZiplineClasses.LauncherActorPrefix"/>…). Each is placed at the relative transform of its
/// <see cref="ZiplineClasses.DefaultSceneRoot"/> (which has no attach parent), and its own settings are read from the
/// members it stores (members it does not store are zero or empty). Read every cell with <see cref="ReadCell"/>, then
/// <see cref="Build"/>.
/// </summary>
internal sealed class ZiplineReader
{
    private const int MaxReferences = 256;
    private const string LabelPrefix = "BP_";
    private const double MaxCoordinate = 1e7;
    private const double MaxAngle = 720;

    private readonly string _mapPackage;
    private readonly List<ZiplinePlacement> _lines = [];
    private readonly List<ActivatableZiplinePlacement> _launchers = [];

    /// <summary>A reader for the cells of map package <paramref name="mapPackage"/>.</summary>
    public ZiplineReader(string mapPackage)
    {
        _mapPackage = mapPackage;
    }

    /// <summary>Reads the ziplines placed in <paramref name="cell"/>; the persistent level is not read.</summary>
    /// <exception cref="InvalidDataException">
    /// An actor's components and name disagree on whether it is a zipline, or a zipline stores a member this reader does
    /// not know or does not decode.
    /// </exception>
    public void ReadCell(MapCell cell, CookedPackage package)
    {
        if (cell.PackageName == _mapPackage)
        {
            return;
        }

        HashSet<int> levels = PlacedActors.Levels(package);
        Dictionary<int, Dictionary<string, ZenExport>> parts = [];
        foreach (ZenExport export in package.Exports)
        {
            if (PlacedActors.ActorOf(package, export, levels) is not { } actor)
            {
                continue;
            }

            if (!parts.TryGetValue(actor.Index, out Dictionary<string, ZenExport>? components))
            {
                components = new Dictionary<string, ZenExport>(StringComparer.Ordinal);
                parts[actor.Index] = components;
            }

            components[export.Name] = export;
        }

        foreach (ZenExport actor in PlacedActors.Actors(package))
        {
            Dictionary<string, ZenExport> components = parts.GetValueOrDefault(actor.Index) ?? new Dictionary<string, ZenExport>(StringComparer.Ordinal);
            bool line = ZiplineClasses.LineComponents.All(components.ContainsKey);
            bool launcher = ZiplineClasses.LauncherComponents.All(components.ContainsKey);
            string path = package.Header.ExportPath(actor.Index);
            if (line != actor.Name.StartsWith(ZiplineClasses.LineActorPrefix, StringComparison.Ordinal)
                || launcher != actor.Name.StartsWith(ZiplineClasses.LauncherActorPrefix, StringComparison.Ordinal))
            {
                throw new InvalidDataException($"{path}: zipline components and name disagree");
            }

            if (!line && !launcher)
            {
                continue;
            }

            try
            {
                ReadZipline(cell.PackageName, package, actor, components, line);
            }
            catch (InvalidDataException error)
            {
                throw new InvalidDataException($"{path}: {error.Message}", error);
            }
        }
    }

    /// <summary>The lines and the launchers, each ordered by cell and actor name (ordinal).</summary>
    public (ImmutableArray<ZiplinePlacement> Lines, ImmutableArray<ActivatableZiplinePlacement> Launchers) Build()
    {
        return ([.. _lines.OrderBy(row => row.Cell, StringComparer.Ordinal).ThenBy(row => row.Actor, StringComparer.Ordinal)],
            [.. _launchers.OrderBy(row => row.Cell, StringComparer.Ordinal).ThenBy(row => row.Actor, StringComparer.Ordinal)]);
    }

    private void ReadZipline(string cell, CookedPackage package, ZenExport actor, Dictionary<string, ZenExport> components, bool line)
    {
        ZenExport root = components[ZiplineClasses.DefaultSceneRoot];
        (Vector3D location, Rotator rotation) = RootTransform(package, root);
        ZiplineValues values = ZiplineValues.Read(package, actor, line);
        int rootReference = root.Index + 1;
        if (values.Reference(line ? PlaceableZiplineMembers.DefaultSceneRoot : ActivatableZiplineMembers.DefaultSceneRoot) != rootReference
            || values.Reference(line ? PlaceableZiplineMembers.RootComponent : ActivatableZiplineMembers.RootComponent) != rootReference)
        {
            throw new InvalidDataException($"the root component references do not name {ZiplineClasses.DefaultSceneRoot}");
        }

        if (line)
        {
            if (values.Reference(PlaceableZiplineMembers.AttachInteraction) != components[ZiplineClasses.AttachInteraction].Index + 1
                || values.Vector(PlaceableZiplineMembers.EndAttachPosition) is not { } end)
            {
                throw new InvalidDataException("attach interaction or end position missing");
            }

            _lines.Add(new ZiplinePlacement(cell, actor.Name, location, rotation, end, values.ExitPoints));
            return;
        }

        if (values.Reference(ActivatableZiplineMembers.ActivationInteraction) != components[ZiplineClasses.ActivationInteraction].Index + 1
            || values.Vector(ActivatableZiplineMembers.AnchorLocation) is not { } anchor)
        {
            throw new InvalidDataException("activation interaction or anchor location missing");
        }

        string? lineClass = values.Reference(ActivatableZiplineMembers.ZiplineClass) is { } reference ? ImportedPackage(package, reference) : null;
        _launchers.Add(new ActivatableZiplinePlacement(
            cell,
            actor.Name,
            location,
            rotation,
            anchor,
            values.Byte(ActivatableZiplineMembers.StartDeployed) != 0,
            values.Byte(ActivatableZiplineMembers.TwoSideActivation) != 0,
            lineClass));
    }

    /// <summary>The package of the public export that an import reference names.</summary>
    private static string ImportedPackage(CookedPackage package, int reference)
    {
        if (!package.IsImportReference(reference) || package.Header.Imports[-reference - 1] is not { Kind: PackageObjectKind.PackageImport } import
            || import.ImportedPackageIndex >= package.Header.ImportedPackageNames.Count)
        {
            throw new InvalidDataException($"the line class reference {reference} is not an import of another package");
        }

        return package.Header.ImportedPackageNames[import.ImportedPackageIndex];
    }

    /// <summary>
    /// The relative location and rotation of a zipline's root component, which stores only those (or only its location)
    /// and closes with an empty native data block.
    /// </summary>
    private static (Vector3D Location, Rotator Rotation) RootTransform(CookedPackage package, ZenExport root)
    {
        UnversionedHeader header = UnversionedHeader.Read(package.Data, CookedPackage.StartOf(root));
        bool rotated = header.Members is [{ Index: SceneComponentMembers.RelativeLocation, IsNonZero: true }, { Index: SceneComponentMembers.RelativeRotation, IsNonZero: true }];
        if (!rotated && header.Members is not [{ Index: SceneComponentMembers.RelativeLocation, IsNonZero: true }])
        {
            throw new InvalidDataException($"the root component sets {string.Join(", ", header.Members.Select(member => member.Index))}");
        }

        int at = header.ValuesOffset;
        int stored = rotated ? 2 : 1;
        if (at + (stored * SceneComponentMembers.VectorSize) + ComponentNativeData.HeadSize != CookedPackage.EndOf(root)
            || package.ReadUInt32(CookedPackage.EndOf(root) - ComponentNativeData.HeadSize) != 0
            || package.ReadUInt32(CookedPackage.EndOf(root) - sizeof(uint)) != 0)
        {
            throw new InvalidDataException("the root component does not end with an empty native data block");
        }

        Vector3D location = package.ReadVector(at);
        Rotator rotation = rotated ? package.ReadRotator(at + SceneComponentMembers.VectorSize) : default;
        if (!(Math.Abs(location.X) < MaxCoordinate && Math.Abs(location.Y) < MaxCoordinate && Math.Abs(location.Z) < MaxCoordinate)
            || !(Math.Abs(rotation.Pitch) <= MaxAngle && Math.Abs(rotation.Yaw) <= MaxAngle && Math.Abs(rotation.Roll) <= MaxAngle))
        {
            throw new InvalidDataException($"implausible root transform {location} {rotation}");
        }

        return (new Vector3D(location.X + 0.0, location.Y + 0.0, location.Z + 0.0), new Rotator(rotation.Pitch + 0.0, rotation.Yaw + 0.0, rotation.Roll + 0.0));
    }

    /// <summary>The members a placed zipline actor lists: stored values, and zero for members its zero mask marks.</summary>
    private sealed class ZiplineValues
    {
        private readonly Dictionary<int, int> _references = [];
        private readonly Dictionary<int, byte> _bytes = [];
        private readonly Dictionary<int, Vector3D> _vectors = [];

        /// <summary>The line's exit points (empty when it lists none).</summary>
        public ImmutableArray<ZiplineExitPoint> ExitPoints { get; private set; } = [];

        /// <summary>Reads the members of a placed line (<paramref name="line"/>) or launcher, to the actor's native data.</summary>
        public static ZiplineValues Read(CookedPackage package, ZenExport actor, bool line)
        {
            UnversionedHeader header = UnversionedHeader.Read(package.Data, CookedPackage.StartOf(actor));
            PlacedActorValues reader = new(package, actor, header);
            ZiplineValues values = new();
            foreach ((int index, bool stored) in header.Members)
            {
                Kind kind = line ? LineMember(index) : LauncherMember(index);
                switch (kind)
                {
                    case Kind.Reference:
                        values._references[index] = stored ? reader.Reference() : 0;
                        break;
                    case Kind.Byte:
                        values._bytes[index] = stored ? reader.Byte() : (byte)0;
                        break;
                    case Kind.Double when stored:
                        _ = reader.Double();
                        break;
                    case Kind.References when stored:
                        _ = reader.References(MaxReferences);
                        break;
                    case Kind.Double or Kind.References:
                        break;
                    case Kind.Vector:
                        values._vectors[index] = stored ? reader.Vector() : default;
                        break;
                    case Kind.ExitPoints:
                        values.ExitPoints = stored ? ReadExitPoints(reader) : [];
                        break;
                    case Kind.Unknown:
                    default:
                        throw new InvalidDataException($"unexpected member {index}");
                }
            }

            string label = reader.ActorTrailer();
            return label.StartsWith(LabelPrefix, StringComparison.Ordinal) && label.Length > LabelPrefix.Length && label.All(c => char.IsAsciiLetterOrDigit(c) || c == '_')
                ? values
                : throw new InvalidDataException($"unexpected actor label {label}");
        }

        /// <summary>The reference a member lists, or <see langword="null"/> when it is not listed.</summary>
        public int? Reference(int member) => _references.TryGetValue(member, out int value) ? value : null;

        /// <summary>The byte a member lists, or 0 when it is not listed.</summary>
        public byte Byte(int member) => _bytes.GetValueOrDefault(member);

        /// <summary>The vector a member lists, or <see langword="null"/> when it is not listed.</summary>
        public Vector3D? Vector(int member) => _vectors.TryGetValue(member, out Vector3D value) ? value : null;

        private static ImmutableArray<ZiplineExitPoint> ReadExitPoints(PlacedActorValues reader)
        {
            int count = reader.Count(ZiplineExitPointMembers.MaxExitPoints);
            ImmutableArray<ZiplineExitPoint>.Builder points = ImmutableArray.CreateBuilder<ZiplineExitPoint>(count);
            for (int i = 0; i < count; i++)
            {
                points.Add(ReadExitPoint(reader, i));
            }

            return points.MoveToImmutable();
        }

        private static ZiplineExitPoint ReadExitPoint(PlacedActorValues reader, int index)
        {
            UnversionedHeader header = reader.StructHeader(ZiplineExitPointMembers.MemberCount);
            Vector3D target = default;
            Vector3D from = default;
            double speed = 0;
            double fromDistance = 0;
            double fraction = 0;
            double distanceAsFraction = 0;
            bool[] flags = new bool[ZiplineExitPointMembers.MemberCount];
            byte direction = 0;
            int ability = 0;
            foreach (UnversionedMember member in header.Members.Where(member => member.IsNonZero))
            {
                switch (member.Index)
                {
                    case ZiplineExitPointMembers.LaunchTarget:
                        target = reader.Vector();
                        break;
                    case ZiplineExitPointMembers.LaunchFrom:
                        from = reader.Vector();
                        break;
                    case ZiplineExitPointMembers.LaunchSpeed:
                        speed = reader.Double();
                        break;
                    case ZiplineExitPointMembers.LaunchFromDistance:
                        fromDistance = reader.Double();
                        break;
                    case ZiplineExitPointMembers.Fraction:
                        fraction = reader.Double();
                        break;
                    case ZiplineExitPointMembers.DistanceAsFraction:
                        distanceAsFraction = reader.Double();
                        break;
                    case ZiplineExitPointMembers.TravelDirection:
                        direction = reader.Byte();
                        break;
                    case ZiplineExitPointMembers.AbilityTag:
                        ability = reader.Reference();
                        break;
                    default:
                        flags[member.Index] = reader.Byte() != 0;
                        break;
                }
            }

            if (direction is not ((byte)ZiplineTravelDirection.Away or (byte)ZiplineTravelDirection.Home) || ability != 0 || fraction is not (>= 0 and <= 1))
            {
                throw new InvalidDataException($"unsupported exit point {index}");
            }

            return new ZiplineExitPoint(
                target,
                from,
                speed,
                fromDistance,
                flags[ZiplineExitPointMembers.Additive],
                flags[ZiplineExitPointMembers.MoreExact],
                flags[ZiplineExitPointMembers.Advanced],
                (ZiplineTravelDirection)direction,
                fraction,
                distanceAsFraction,
                flags[ZiplineExitPointMembers.ResetAndLaunch]);
        }

        private static Kind LineMember(int index) => index switch
        {
            PlaceableZiplineMembers.RoofAttachement or PlaceableZiplineMembers.BottomCablePile or PlaceableZiplineMembers.CableMesh
                or PlaceableZiplineMembers.PioneerPingInfoComponent or PlaceableZiplineMembers.AttachSound or PlaceableZiplineMembers.DefaultSceneRoot
                or PlaceableZiplineMembers.InteractionProxyObject or PlaceableZiplineMembers.ZiplineEffect or PlaceableZiplineMembers.AttachInteraction
                or PlaceableZiplineMembers.AttachmentInformationContainerComponent or PlaceableZiplineMembers.RootComponent => Kind.Reference,
            PlaceableZiplineMembers.AngledRoofAttachment or PlaceableZiplineMembers.DoExtraBlockBarricadeCheckAroundExits
                or PlaceableZiplineMembers.UseManualInteractionBoxSize => Kind.Byte,
            PlaceableZiplineMembers.ManualInteractionSizeZ or PlaceableZiplineMembers.ForcedTravelDirectionThresholdHome => Kind.Double,
            PlaceableZiplineMembers.LocationOffset or PlaceableZiplineMembers.RotationOffset or PlaceableZiplineMembers.EndAttachPosition => Kind.Vector,
            PlaceableZiplineMembers.ExitPoints => Kind.ExitPoints,
            PlaceableZiplineMembers.BlueprintCreatedComponents => Kind.References,
            _ => Kind.Unknown,
        };

        private static Kind LauncherMember(int index) => index switch
        {
            >= ActivatableZiplineMembers.FirstComponent and <= ActivatableZiplineMembers.LastComponent => Kind.Reference,
            ActivatableZiplineMembers.ZiplineClass or ActivatableZiplineMembers.ScatterBlocking or ActivatableZiplineMembers.ActivationInteraction
                or ActivatableZiplineMembers.DefaultSceneRoot or ActivatableZiplineMembers.Unknown39 or ActivatableZiplineMembers.RootComponent => Kind.Reference,
            ActivatableZiplineMembers.StartDeployed or ActivatableZiplineMembers.TwoSideActivation or ActivatableZiplineMembers.HasBeenDeployed
                or ActivatableZiplineMembers.Unknown95 => Kind.Byte,
            ActivatableZiplineMembers.AnchorLocation => Kind.Vector,
            ActivatableZiplineMembers.BlueprintCreatedComponents => Kind.References,
            _ => Kind.Unknown,
        };

        /// <summary>How a zipline member's value is read.</summary>
        private enum Kind
        {
            Unknown,
            Reference,
            Byte,
            Double,
            Vector,
            References,
            ExitPoints,
        }
    }
}
