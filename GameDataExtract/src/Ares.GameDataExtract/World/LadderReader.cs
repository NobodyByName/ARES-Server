using System.Buffers.Binary;
using System.Collections.Immutable;
using Ares.Common.Maps;
using Ares.GameDataExtract.Components;
using Ares.GameDataExtract.Packages;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Game.Collision;
using Ares.Structs.Game.World;

namespace Ares.GameDataExtract.World;

/// <summary>
/// Reads a map's climbable ladders (<see cref="LadderPlacement"/>): actors placed in the generated cells that have a
/// <see cref="LadderLogic.SpawningComponent"/>, with the top of their root <see cref="LadderLogic.SplineComponent"/> and
/// that spline's relative location and yaw (it has no attach parent, so this is the ladder's world transform, and the
/// transform of the climbable actor the server spawns attached to it). Also reads the class the ladders' logic data
/// names. Read every cell with <see cref="ReadCell"/>, then <see cref="Build"/>.
/// </summary>
internal sealed class LadderReader
{
    /// <summary>Suffix of a Blueprint's component template export.</summary>
    private const string TemplateSuffix = "_GEN_VARIABLE";

    private const string ClassSeparator = "_C_";
    private const double MaxSplineOffset = 10;
    private const double MaxSplineTop = 10000;
    private const double MaxCoordinate = 1e7;
    private const double MaxTilt = 1e-6;
    private const double MaxYaw = 720;
    private const double LengthTolerance = 0.01;
    private const int FloatSize = 4;
    private const int VectorSize = 24;

    private readonly SceneResolver _scenes;
    private readonly PackageCache _packages;
    private readonly string _mapPackage;
    private readonly List<LadderPlacement> _ladders = [];
    private readonly SortedSet<string> _blueprints = new(StringComparer.Ordinal);

    /// <summary>A reader for the cells of map package <paramref name="mapPackage"/>.</summary>
    public LadderReader(SceneResolver scenes, PackageCache packages, string mapPackage)
    {
        _scenes = scenes;
        _packages = packages;
        _mapPackage = mapPackage;
    }

    /// <summary>Reads the ladders placed in <paramref name="cell"/>; the persistent level is not read.</summary>
    /// <exception cref="InvalidDataException">A ladder has no spline, or its spline or transform does not decode.</exception>
    public void ReadCell(MapCell cell, CookedPackage package)
    {
        if (cell.PackageName == _mapPackage)
        {
            return;
        }

        foreach (ZenExport actor in PlacedActors.Actors(package))
        {
            if (package.Child(actor.Index, LadderLogic.SpawningComponent) is null)
            {
                continue;
            }

            string path = package.Header.ExportPath(actor.Index);
            int separator = actor.Name.IndexOf(ClassSeparator, StringComparison.Ordinal);
            if (separator < 0 || package.Child(actor.Index, LadderLogic.SplineComponent) is not { } spline)
            {
                throw new InvalidDataException($"{path}: ladder without a spline");
            }

            string blueprint = BlueprintPackage(package, actor, path);
            if (!blueprint.EndsWith("/" + actor.Name[..separator], StringComparison.Ordinal))
            {
                throw new InvalidDataException($"{path}: the actor is not named after its Blueprint {blueprint}");
            }

            try
            {
                double top = SplineTop(package, spline);
                (Vector3D location, double yaw) = SplineTransform(package, spline);
                if (top < LadderLogic.CycleHeight)
                {
                    throw new InvalidDataException($"the spline top {top} is below one climb cycle");
                }

                _ladders.Add(new LadderPlacement(cell.PackageName, actor.Name, top, location, yaw));
            }
            catch (InvalidDataException error)
            {
                throw new InvalidDataException($"{path}: {error.Message}", error);
            }

            _blueprints.Add(blueprint);
        }
    }

    /// <summary>
    /// The ladders ordered by cell and actor name (ordinal), and the class the ladders' logic data names with its class
    /// default (<see langword="null"/> for a map without ladders).
    /// </summary>
    /// <exception cref="InvalidDataException">
    /// Two ladders share a cell and name, or the ladder Blueprints, their logic data or the logic class are not as expected.
    /// </exception>
    public (ImmutableArray<LadderPlacement> Ladders, ArchetypeClass? Logic) Build()
    {
        if (_ladders.Count == 0)
        {
            return ([], null);
        }

        ImmutableArray<LadderPlacement> ladders = [.. _ladders.OrderBy(row => row.Cell, StringComparer.Ordinal).ThenBy(row => row.Actor, StringComparer.Ordinal)];
        for (int i = 1; i < ladders.Length; i++)
        {
            if (ladders[i - 1].Cell == ladders[i].Cell && ladders[i - 1].Actor == ladders[i].Actor)
            {
                throw new InvalidDataException($"Two ladders are {ladders[i].Cell} {ladders[i].Actor}");
            }
        }

        return (ladders, Logic());
    }

    private static string BlueprintPackage(CookedPackage package, ZenExport actor, string path)
    {
        return actor.Class.Kind == PackageObjectKind.PackageImport
            ? package.Header.ImportedPackageNames[actor.Class.ImportedPackageIndex]
            : throw new InvalidDataException($"{path}: the ladder's class is not a Blueprint of another package");
    }

    /// <summary>
    /// The logic class: each ladder Blueprint's spawning component template sets only its first member (the logic data)
    /// and the Blueprint imports one package, the same logic data asset for all; that asset sets only its first member (the
    /// class) and imports one package, the logic Blueprint, whose class default has the climb interaction components.
    /// </summary>
    private ArchetypeClass Logic()
    {
        HashSet<string> data = new(StringComparer.Ordinal);
        foreach (string name in _blueprints)
        {
            CookedPackage blueprint = Load(name);
            ZenExport[] templates = [.. blueprint.Exports.Where(export => export.Name == LadderLogic.SpawningComponent + TemplateSuffix)];
            if (templates.Length != 1 || blueprint.Header.ImportedPackageNames.Count != 1)
            {
                throw new InvalidDataException($"{name}: expected one spawning component and one imported package");
            }

            OnlyFirstMember(blueprint, templates[0], $"{name} spawning component");
            data.Add(blueprint.Header.ImportedPackageNames[0]);
        }

        if (data.Count != 1)
        {
            throw new InvalidDataException($"The ladders use several logic data assets: {string.Join(", ", data.Order(StringComparer.Ordinal))}");
        }

        string dataName = data.Single();
        CookedPackage logicData = Load(dataName);
        if (logicData.Exports.Count != 1 || logicData.Header.ImportedPackageNames.Count != 1)
        {
            throw new InvalidDataException($"{dataName}: unexpected exports or imports");
        }

        OnlyFirstMember(logicData, logicData.Exports[0], dataName);
        string logicName = logicData.Header.ImportedPackageNames[0];
        string leaf = logicName[(logicName.LastIndexOf('/') + 1)..];
        CookedPackage logic = Load(logicName);
        string defaultName = $"Default__{leaf}_C";
        ZenExport classDefault = logic.Exports.SingleOrDefault(export => export.Outer.IsNull && export.Name == defaultName)
            ?? throw new InvalidDataException($"{logicName} has no single {defaultName}");
        foreach (string component in LadderLogic.InteractionComponents)
        {
            if (logic.Child(classDefault.Index, component) is null)
            {
                throw new InvalidDataException($"{logicName} has no {component}");
            }
        }

        return new ArchetypeClass($"{logicName}.{leaf}_C", $"{logicName}.{defaultName}");
    }

    private CookedPackage Load(string name) => _packages.Load(name) ?? throw new InvalidDataException($"{name} is not in the game archives");

    /// <summary>Checks that an export's unversioned header lists exactly its first member.</summary>
    private static void OnlyFirstMember(CookedPackage package, ZenExport export, string what)
    {
        UnversionedHeader header = UnversionedHeader.Read(package.Data, CookedPackage.StartOf(export));
        if (header.Members.Count != 1 || header.Members[0].Index != 0)
        {
            throw new InvalidDataException($"{what} sets members {string.Join(", ", header.Members.Select(member => member.Index))}, expected only the first one");
        }
    }

    /// <summary>
    /// The relative location and yaw of a ladder's root spline, which must store its location and rotation as its last
    /// members, no attach parent and no scale, and stand upright.
    /// </summary>
    private (Vector3D Location, double Yaw) SplineTransform(CookedPackage package, ZenExport spline)
    {
        SceneMembers scene = _scenes.Scene(package, spline);
        const int Location = SplineComponentMembers.ScenePart + SceneComponentMembers.RelativeLocation;
        const int Rotation = SplineComponentMembers.ScenePart + SceneComponentMembers.RelativeRotation;
        if (!scene.Header.IsNonZero(Location) || !scene.Header.IsNonZero(Rotation)
            || scene.Header.Contains(SplineComponentMembers.ScenePart + SceneComponentMembers.AttachParent)
            || scene.Header.Members.Any(member => member.Index > Rotation)
            || scene.Location is not { } at || scene.Rotation is not { } turn)
        {
            throw new InvalidDataException($"spline transform members {string.Join(", ", scene.Header.Members.Select(member => member.Index))}");
        }

        if (!(Math.Abs(at.X) < MaxCoordinate && Math.Abs(at.Y) < MaxCoordinate && Math.Abs(at.Z) < MaxCoordinate)
            || Math.Abs(turn.Pitch) > MaxTilt || Math.Abs(turn.Roll) > MaxTilt || !(Math.Abs(turn.Yaw) <= MaxYaw))
        {
            throw new InvalidDataException($"implausible spline transform {at} {turn}");
        }

        return (new Vector3D(at.X + 0.0, at.Y + 0.0, at.Z + 0.0), turn.Yaw + 0.0);
    }

    /// <summary>
    /// Local Z of the last point of a cooked spline's position curve, found by pattern: the first array of vector curve
    /// points in the export, then the first array of float points after it (the reparameterization table), whose last
    /// distance must equal the point distances' sum. Every segment must be linear.
    /// </summary>
    private static double SplineTop(CookedPackage package, ZenExport spline)
    {
        ReadOnlySpan<byte> data = package.Data.AsSpan(CookedPackage.StartOf(spline), (int)spline.SerialSize);
        (int end, CurvePoint[]? points) = FirstCurve(data, 0, vector: true);
        if (points is null)
        {
            throw new InvalidDataException("no position curve");
        }

        (_, CurvePoint[]? reparam) = FirstCurve(data, end, vector: false);
        if (reparam is null)
        {
            throw new InvalidDataException("no reparameterization table");
        }

        if (points.Any(point => point.Mode != InterpCurvePointMembers.LinearMode))
        {
            throw new InvalidDataException("non-linear spline segments");
        }

        double length = 0;
        for (int i = 0; i + 1 < points.Length; i++)
        {
            length += Distance(points[i].Value, points[i + 1].Value);
        }

        double stored = reparam[^1].In;
        if (Math.Abs(length - stored) > LengthTolerance)
        {
            throw new InvalidDataException($"point distances {length} differ from the stored spline length {stored}");
        }

        Vector3D top = points[^1].Value;
        return Math.Abs(top.X) > MaxSplineOffset || Math.Abs(top.Y) > MaxSplineOffset || top.Z is not (> 0 and < MaxSplineTop)
            ? throw new InvalidDataException($"last spline point {top} is not above the ladder origin")
            : top.Z;
    }

    private static double Distance(Vector3D a, Vector3D b)
    {
        double x = b.X - a.X;
        double y = b.Y - a.Y;
        double z = b.Z - a.Z;
        return Math.Sqrt((x * x) + (y * y) + (z * z));
    }

    /// <summary>The first point array from <paramref name="start"/>: an int32 count, then that many curve points.</summary>
    private static (int End, CurvePoint[]? Points) FirstCurve(ReadOnlySpan<byte> data, int start, bool vector)
    {
        // Every offset with room for a count and a point header (7 bytes).
        for (int at = start; at < data.Length - 6; at++)
        {
            int count = BinaryPrimitives.ReadInt32LittleEndian(data[at..]);
            if (count is < InterpCurvePointMembers.MinPoints or > InterpCurvePointMembers.MaxPoints)
            {
                continue;
            }

            CurvePoint[] points = new CurvePoint[count];
            int position = at + sizeof(int);
            int read = 0;
            while (read < count && ReadPoint(data, position, vector) is { } point)
            {
                (position, points[read]) = point;
                read++;
            }

            if (read == count)
            {
                return (position, points);
            }
        }

        return (-1, null);
    }

    /// <summary>A curve point at <paramref name="at"/>, or <see langword="null"/> when the bytes there are not one.</summary>
    private static (int End, CurvePoint Point)? ReadPoint(ReadOnlySpan<byte> data, int at, bool vector)
    {
        if (at + 3 > data.Length || BinaryPrimitives.ReadUInt16LittleEndian(data[at..]) != InterpCurvePointMembers.HeaderWord)
        {
            return null;
        }

        byte mask = data[at + 2];
        at += 3;
        double inValue = 0;
        Vector3D value = default;
        byte mode = 0;
        for (int member = 0; member < InterpCurvePointMembers.MemberCount; member++)
        {
            if (((mask >> member) & 1) != 0)
            {
                continue;
            }

            int size = member == InterpCurvePointMembers.InterpMode ? 1 : member == InterpCurvePointMembers.InVal || !vector ? FloatSize : VectorSize;
            if (at + size > data.Length)
            {
                return null;
            }

            switch (member)
            {
                case InterpCurvePointMembers.InVal:
                    inValue = BinaryPrimitives.ReadSingleLittleEndian(data[at..]);
                    break;
                case InterpCurvePointMembers.OutVal when vector:
                    value = new Vector3D(
                        BinaryPrimitives.ReadDoubleLittleEndian(data[at..]),
                        BinaryPrimitives.ReadDoubleLittleEndian(data[(at + 8)..]),
                        BinaryPrimitives.ReadDoubleLittleEndian(data[(at + 16)..]));
                    break;
                case InterpCurvePointMembers.InterpMode:
                    mode = data[at];
                    break;
            }

            at += size;
        }

        return (at, new CurvePoint(inValue, value, mode));
    }

    /// <summary>The parts of a curve point the reader uses: its parameter, its vector value (for a position curve) and its interpolation mode.</summary>
    private readonly record struct CurvePoint(double In, Vector3D Value, byte Mode);
}
