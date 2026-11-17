using System.Collections.Immutable;
using Ares.GameDataExtract.Bodies;
using Ares.GameDataExtract.Components;
using Ares.GameDataExtract.Packages;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Game.Collision;
using Ares.Structs.Game.World;

namespace Ares.GameDataExtract.World;

/// <summary>A volume's convex parts as world-space hulls, and how many flat parts (all vertices in one plane) were left out.</summary>
internal sealed record LootVolumeHulls(ImmutableArray<LootVolumeHull> Hulls, int FlatElements);

/// <summary>
/// Reads volumes placed in a map's persistent level as world-space hulls: the volume actor's brush component, the
/// convex elements of its body setup (<c>AggGeom.ConvexElems</c>), each element's vertices moved by the element
/// transform and the brush's world transform, and the bounding planes of their convex hull. Plane normals are rounded to
/// 9 decimal places and distances to 4.
/// </summary>
internal static class LootVolumeReader
{
    /// <summary>Most convex elements a limiter's volume may have.</summary>
    public const int LimiterMaxElements = 16;

    /// <summary>Most convex elements a point-of-interest volume may have.</summary>
    public const int PointOfInterestMaxElements = 64;

    private const int MinVertices = 4;
    private const int MaxVertices = 256;
    private const int NormalDigits = 9;
    private const int DistanceDigits = 4;
    private const double SpanTolerance = 1e-7;
    private const double AreaTolerance = 1e-9;
    private const double ParallelTolerance = 1e-9;
    private const int MinPlanes = 4;

    private static readonly PackageObjectIndex s_brushComponent = PackageObjectIndex.FromScriptPath(BrushComponentMembers.ClassPath);
    private static readonly PackageObjectIndex s_bodySetup = PackageObjectIndex.FromScriptPath(CollisionObjectClasses.BodySetup);
    private static readonly Transform s_identity = new(Quaternion.Identity, default, Vector3D.One);

    /// <summary>The hulls of each named volume actor of <paramref name="level"/>'s persistent level.</summary>
    /// <param name="scenes">Resolves the brush components' world transforms.</param>
    /// <param name="level">The map package.</param>
    /// <param name="names">Names of the volume actors.</param>
    /// <param name="maxElements">Most convex elements a volume may have.</param>
    /// <param name="skipFlat">Leave out elements whose vertices all lie in one plane instead of failing on them.</param>
    /// <exception cref="InvalidDataException">A volume is missing, or its brush or body setup is not as expected.</exception>
    public static IReadOnlyDictionary<string, LootVolumeHulls> Read(SceneResolver scenes, CookedPackage level, IReadOnlySet<string> names, int maxElements, bool skipFlat)
    {
        Dictionary<string, ZenExport> actors = new(StringComparer.Ordinal);
        foreach (ZenExport export in level.Exports)
        {
            if (ExportChains.PlacedActorName(level, export) is { } name && names.Contains(name))
            {
                actors[name] = export;
            }
        }

        if (names.FirstOrDefault(name => !actors.ContainsKey(name)) is { } missing)
        {
            throw new InvalidDataException($"The volume {missing} is not in the persistent level");
        }

        Dictionary<string, LootVolumeHulls> volumes = new(StringComparer.Ordinal);
        foreach (string name in names.Order(StringComparer.Ordinal))
        {
            ZenExport brush = Single(level.ChildrenOf(actors[name].Index).Where(export => export.Class == s_brushComponent), name, "brush components");
            ZenExport body = Single(level.ChildrenOf(brush.Index).Where(export => export.Class == s_bodySetup), name, "body setups");
            Transform world = scenes.WorldTransform(level, brush);
            BodySetupContents contents = BodySetupReader.Read(level, body);
            if (contents.Spheres.Length + contents.Boxes.Length + contents.Capsules.Length != 0
                || contents.ConvexElements.Length is 0 || contents.ConvexElements.Length > maxElements)
            {
                throw new InvalidDataException($"{name}: the body setup has {contents.ConvexElements.Length} convex elements and other shapes");
            }

            ImmutableArray<LootVolumeHull>.Builder hulls = ImmutableArray.CreateBuilder<LootVolumeHull>();
            int flat = 0;
            foreach (ConvexElement element in contents.ConvexElements)
            {
                if (element.Vertices.Length is < MinVertices or > MaxVertices)
                {
                    throw new InvalidDataException($"{name}: a convex element has {element.Vertices.Length} vertices");
                }

                Transform elementWorld = TransformMath.Compose(element.Transform ?? s_identity, world);
                Vector3D[] points = [.. element.Vertices.Select(vertex => TransformMath.Compose(new Transform(Quaternion.Identity, vertex, Vector3D.One), elementWorld).Translation)];
                if (BoundingPlanes(points, name, skipFlat) is { } planes)
                {
                    hulls.Add(new LootVolumeHull(planes));
                }
                else
                {
                    flat++;
                }
            }

            volumes[name] = new LootVolumeHulls(hulls.ToImmutable(), flat);
        }

        return volumes;
    }

    /// <summary>
    /// The outward bounding planes of the convex hull of <paramref name="points"/>: every plane through three of them that
    /// has all points on one side, duplicates merged, in the order found. With <paramref name="allowFlat"/>,
    /// <see langword="null"/> for points that all lie in one plane.
    /// </summary>
    /// <exception cref="InvalidDataException">The points span no volume (and are not flat, or flat ones are not allowed).</exception>
    public static IReadOnlyList<LootVolumePlane>? BoundingPlanes(IReadOnlyList<Vector3D> points, string where, bool allowFlat)
    {
        double span = 0;
        for (int i = 0; i < points.Count; i++)
        {
            for (int j = i + 1; j < points.Count; j++)
            {
                span = Math.Max(span, CompensatedFloat.Distance(points[i], points[j]));
            }
        }

        double tolerance = span * SpanTolerance;
        double minLength = span * span * AreaTolerance;
        List<(Vector3D Normal, double Distance)> planes = [];
        for (int i = 0; i < points.Count; i++)
        {
            for (int j = i + 1; j < points.Count; j++)
            {
                for (int k = j + 1; k < points.Count; k++)
                {
                    if (Plane(points, points[i], points[j], points[k], tolerance, minLength) is not { } plane)
                    {
                        continue;
                    }

                    if (!planes.Any(known => CompensatedFloat.Dot(plane.Normal, known.Normal) > 1 - ParallelTolerance && Math.Abs(plane.Distance - known.Distance) <= tolerance))
                    {
                        planes.Add(plane);
                    }
                }
            }
        }

        Vector3D centre = new(
            CompensatedFloat.Sum(points.Select(point => point.X)) / points.Count,
            CompensatedFloat.Sum(points.Select(point => point.Y)) / points.Count,
            CompensatedFloat.Sum(points.Select(point => point.Z)) / points.Count);
        if (planes.Count < MinPlanes || planes.Any(plane => CompensatedFloat.Dot(plane.Normal, centre) >= plane.Distance))
        {
            if (allowFlat && planes.Count > 0 && points.All(point => Math.Abs(CompensatedFloat.Dot(planes[0].Normal, point) - planes[0].Distance) <= tolerance))
            {
                return null;
            }

            throw new InvalidDataException($"{where}: degenerate convex element");
        }

        return [.. planes.Select(plane => new LootVolumePlane(DecimalRounding.Round(plane.Normal, NormalDigits), DecimalRounding.Round(plane.Distance, DistanceDigits)))];
    }

    /// <summary>The outward plane through <paramref name="a"/>, <paramref name="b"/> and <paramref name="c"/> when every point is on one side of it.</summary>
    private static (Vector3D Normal, double Distance)? Plane(IReadOnlyList<Vector3D> points, Vector3D a, Vector3D b, Vector3D c, double tolerance, double minLength)
    {
        Vector3D u = new(b.X - a.X, b.Y - a.Y, b.Z - a.Z);
        Vector3D v = new(c.X - a.X, c.Y - a.Y, c.Z - a.Z);
        Vector3D normal = new((u.Y * v.Z) - (u.Z * v.Y), (u.Z * v.X) - (u.X * v.Z), (u.X * v.Y) - (u.Y * v.X));
        double length = Math.Sqrt(CompensatedFloat.Dot(normal, normal));
        if (length < minLength)
        {
            return null;
        }

        normal = new Vector3D(normal.X / length, normal.Y / length, normal.Z / length);
        double distance = CompensatedFloat.Dot(normal, a);
        bool inside = true;
        bool outside = true;
        foreach (Vector3D point in points)
        {
            double side = CompensatedFloat.Dot(normal, point) - distance;
            inside &= side <= tolerance;
            outside &= side >= -tolerance;
        }

        return inside ? (normal, distance)
            : outside ? (new Vector3D(-normal.X, -normal.Y, -normal.Z), -distance)
            : null;
    }

    private static ZenExport Single(IEnumerable<ZenExport> candidates, string volume, string what)
    {
        ZenExport[] found = [.. candidates.Take(2)];
        return found.Length == 1 ? found[0] : throw new InvalidDataException($"{volume}: {(found.Length == 0 ? "no" : "several")} {what}");
    }
}
