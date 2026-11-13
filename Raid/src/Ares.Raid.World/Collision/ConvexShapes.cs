using System.Numerics;
using Ares.Common.Maps;
using Ares.Structs.Game.Collision;
using Quaternion = System.Numerics.Quaternion;

namespace Ares.Raid.World.Collision;

/// <summary>
/// A convex point set described by its support mapping: the core of a collision shape, which a radius may round
/// (a sphere is a rounded point, a capsule a rounded segment).
/// </summary>
internal interface IConvexShape
{
    /// <summary>Upper bound on the number of axes <see cref="PenetrationAxes"/> writes.</summary>
    int PenetrationAxisCount { get; }

    /// <summary>A point of the set farthest along <paramref name="direction"/>, which need not be a unit vector.</summary>
    Double3 Support(Double3 direction);

    /// <summary>
    /// Writes candidate push-out axes against a segment along <paramref name="segment"/> (zero for a point): normally
    /// the set's face normals and its edges crossed with the segment, which include every facet normal of the two
    /// sets' Minkowski difference. Axes need not be normalized and may be zero. Returns the number written.
    /// </summary>
    int PenetrationAxes(Double3 segment, Span<Double3> axes);
}

/// <summary>A segment from <c>Center − HalfAxis</c> to <c>Center + HalfAxis</c>; a point when the half axis is zero.</summary>
internal readonly record struct SegmentShape(Double3 Center, Double3 HalfAxis) : IConvexShape
{
    public int PenetrationAxisCount => 3;

    public Double3 Support(Double3 direction) => Double3.Dot(HalfAxis, direction) >= 0 ? Center + HalfAxis : Center - HalfAxis;

    public int PenetrationAxes(Double3 segment, Span<Double3> axes)
    {
        // Two crossing segments have a flat difference; parallel segments and points leave every perpendicular direction.
        Double3 along = HalfAxis.LengthSquared() > 0 ? HalfAxis : segment;
        Double3 perpendicular = ConvexShapes.Perpendicular(along);
        axes[0] = Double3.Cross(HalfAxis, segment);
        axes[1] = perpendicular;
        axes[2] = Double3.Cross(along, perpendicular);
        return 3;
    }
}

/// <summary>An oriented box: its centre and the three half-size vectors along its own axes.</summary>
internal readonly record struct OrientedBoxShape(Double3 Center, Quaternion Rotation, Double3 HalfExtents, Double3 AxisX, Double3 AxisY, Double3 AxisZ)
    : IConvexShape
{
    public int PenetrationAxisCount => 6;

    public Double3 Support(Double3 direction) =>
        Center
        + (Double3.Dot(AxisX, direction) >= 0 ? AxisX : -AxisX)
        + (Double3.Dot(AxisY, direction) >= 0 ? AxisY : -AxisY)
        + (Double3.Dot(AxisZ, direction) >= 0 ? AxisZ : -AxisZ);

    public int PenetrationAxes(Double3 segment, Span<Double3> axes)
    {
        axes[0] = AxisX;
        axes[1] = AxisY;
        axes[2] = AxisZ;
        axes[3] = Double3.Cross(AxisX, segment);
        axes[4] = Double3.Cross(AxisY, segment);
        axes[5] = Double3.Cross(AxisZ, segment);
        return 6;
    }

    /// <summary>Smallest corner of the box's axis-aligned bounds.</summary>
    public Double3 Minimum => Center - Reach;

    /// <summary>Largest corner of the box's axis-aligned bounds.</summary>
    public Double3 Maximum => Center + Reach;

    private Double3 Reach => AxisX.Abs() + AxisY.Abs() + AxisZ.Abs();
}

/// <summary>A convex hull scaled per axis (negative components mirror it).</summary>
internal readonly record struct HullShape(ConvexHull Hull, Vector3 Scale) : IConvexShape
{
    public int PenetrationAxisCount => Hull.Polygons.Length + Hull.PolygonVertexIndices.Length;

    public Double3 Support(Double3 direction)
    {
        // The vertex farthest along d after scaling is the one farthest along scale ∘ d before it.
        Double3 scaled = direction.Scale(Scale);
        ReadOnlySpan<Vector3> vertices = Hull.Vertices.AsSpan();
        int best = 0;
        double bestDot = double.NegativeInfinity;
        for (int index = 0; index < vertices.Length; index++)
        {
            Vector3 vertex = vertices[index];
            double dot = (vertex.X * scaled.X) + (vertex.Y * scaled.Y) + (vertex.Z * scaled.Z);
            if (!(dot > bestDot))
            {
                continue;
            }

            bestDot = dot;
            best = index;
        }

        return Double3.From(vertices[best]).Scale(Scale);
    }

    public int PenetrationAxes(Double3 segment, Span<Double3> axes)
    {
        ReadOnlySpan<Vector3> vertices = Hull.Vertices.AsSpan();
        int count = 0;
        foreach (HullPolygon polygon in Hull.Polygons)
        {
            // Plane normals transform with the inverse scale.
            axes[count++] = Double3.From(polygon.Plane.Normal).DivideBy(Scale);
            ReadOnlySpan<byte> loop = Hull.PolygonVertices(polygon);
            for (int index = 0; index < loop.Length; index++)
            {
                Double3 edge = Double3.From(vertices[loop[(index + 1) % loop.Length]] - vertices[loop[index]]).Scale(Scale);
                axes[count++] = Double3.Cross(edge, segment);
            }
        }

        return count;
    }
}

/// <summary>
/// A triangle; both sides collide. A segment through it is pushed out along the face normal only: triangles are
/// pieces of larger surfaces, whose inner edges must not push sideways.
/// </summary>
internal readonly record struct TriangleShape(Double3 A, Double3 B, Double3 C) : IConvexShape
{
    public int PenetrationAxisCount => 1;

    public Double3 Support(Double3 direction)
    {
        double a = Double3.Dot(A, direction);
        double b = Double3.Dot(B, direction);
        double c = Double3.Dot(C, direction);
        return a >= b ? (a >= c ? A : C) : (b >= c ? B : C);
    }

    public int PenetrationAxes(Double3 segment, Span<Double3> axes)
    {
        axes[0] = Double3.Cross(B - A, C - A);
        return 1;
    }

    public Double3 Minimum => Double3.Min(Double3.Min(A, B), C);

    public Double3 Maximum => Double3.Max(Double3.Max(A, B), C);
}

/// <summary>
/// Boxes, spheres and capsules of a placed body in the placement's frame: the placement's translation and rotation
/// removed, its scale applied. They stay rigid under any scale. Centres scale with the full scale. A box keeps its
/// orientation and scales each half extent by the absolute scale of the matching body axis. A sphere's radius scales
/// by the smallest absolute scale component. A capsule keeps its orientation; its radius scales by the larger of the
/// absolute X and Y scales, its total half height (half length plus radius) by the absolute Z scale, and the radius
/// is capped at that half height.
/// </summary>
internal static class ConvexShapes
{
    public static OrientedBoxShape Box(in CollisionBox box, Vector3 scale)
    {
        Double3 half = Double3.From(box.HalfExtents).Scale(Vector3.Abs(scale));
        return new OrientedBoxShape(
            Double3.From(box.Center).Scale(scale),
            box.Rotation,
            half,
            new Double3(half.X, 0, 0).Rotate(box.Rotation),
            new Double3(0, half.Y, 0).Rotate(box.Rotation),
            new Double3(0, 0, half.Z).Rotate(box.Rotation));
    }

    public static SegmentShape Sphere(in CollisionSphere sphere, Vector3 scale, out double radius)
    {
        radius = sphere.Radius * (double)Math.Min(Math.Abs(scale.X), Math.Min(Math.Abs(scale.Y), Math.Abs(scale.Z)));
        return new SegmentShape(Double3.From(sphere.Center).Scale(scale), default);
    }

    public static SegmentShape Capsule(in CollisionCapsule capsule, Vector3 scale, out double radius, out double halfLength)
    {
        double halfHeight = (capsule.HalfLength + (double)capsule.Radius) * Math.Abs(scale.Z);
        radius = Math.Min(capsule.Radius * (double)Math.Max(Math.Abs(scale.X), Math.Abs(scale.Y)), halfHeight);
        halfLength = halfHeight - radius;
        return new SegmentShape(Double3.From(capsule.Center).Scale(scale), new Double3(0, 0, halfLength).Rotate(capsule.Rotation));
    }

    /// <summary>A unit vector perpendicular to <paramref name="value"/> (any unit vector when it is zero).</summary>
    public static Double3 Perpendicular(Double3 value)
    {
        Double3 absolute = value.Abs();
        Double3 other = absolute.X <= absolute.Y && absolute.X <= absolute.Z ? new Double3(1, 0, 0)
            : absolute.Y <= absolute.Z ? new Double3(0, 1, 0)
            : new Double3(0, 0, 1);
        Double3 perpendicular = Double3.Cross(value, other);
        return perpendicular.LengthSquared() > 0 ? perpendicular.Normalized() : new Double3(0, 0, 1);
    }
}
