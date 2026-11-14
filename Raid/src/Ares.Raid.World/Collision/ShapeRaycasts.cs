using System.Numerics;
using Ares.Structs.Game.Collision;
using Quaternion = System.Numerics.Quaternion;

namespace Ares.Raid.World.Collision;

/// <summary>
/// Ray tests against single shapes: hulls and meshes in their body's space, boxes, spheres and capsules as placed
/// (see <see cref="ConvexShapes"/>). The ray is <c>origin + t · direction</c>; the direction need
/// not be a unit vector, so the parameter keeps its meaning across scaled spaces. Each test reports the smallest
/// <c>t</c> in [0, <c>maxT</c>) and the surface normal there (not normalized). A ray that starts inside a solid shape
/// hits it at <c>t = 0</c> with the normal opposite to the ray. Triangles collide from both sides; their normal faces
/// the ray.
/// </summary>
internal static class ShapeRaycasts
{
    /// <summary>Barycentric slack that keeps rays from slipping between adjacent triangles.</summary>
    private const double EdgeTolerance = 1e-9;

    public static bool Box(in OrientedBoxShape box, Double3 origin, Double3 direction, double maxT, out double t, out Double3 normal)
    {
        Double3 localOrigin = (origin - box.Center).RotateInverse(box.Rotation);
        Double3 localDirection = direction.RotateInverse(box.Rotation);
        if (!AxisBox(-box.HalfExtents, box.HalfExtents, localOrigin, localDirection, maxT, out t, out Double3 localNormal))
        {
            normal = default;
            return false;
        }

        normal = t == 0 ? -direction : localNormal.Rotate(box.Rotation);
        return true;
    }

    public static bool Sphere(Double3 center, double radius, Double3 origin, Double3 direction, double maxT, out double t, out Double3 normal)
    {
        Double3 offset = origin - center;
        if (!CenteredSphere(radius, offset, direction, maxT, out t))
        {
            normal = default;
            return false;
        }

        normal = t == 0 ? -direction : offset + (direction * t);
        return true;
    }

    /// <summary>Ray test against a capsule whose segment runs along <paramref name="rotation"/>'s Z axis.</summary>
    public static bool Capsule(Double3 center, Quaternion rotation, double radius, double half, Double3 origin, Double3 direction, double maxT, out double t, out Double3 normal)
    {
        Double3 o = (origin - center).RotateInverse(rotation);
        Double3 d = direction.RotateInverse(rotation);
        double closestZ = Math.Clamp(o.Z, -half, half);
        if ((o.X * o.X) + (o.Y * o.Y) + ((o.Z - closestZ) * (o.Z - closestZ)) <= radius * radius)
        {
            t = 0;
            normal = -direction;
            return true;
        }

        t = maxT;
        Double3 localNormal = default;
        bool hit = false;
        double a = (d.X * d.X) + (d.Y * d.Y);
        if (a > 0)
        {
            double b = (o.X * d.X) + (o.Y * d.Y);
            double c = (o.X * o.X) + (o.Y * o.Y) - (radius * radius);
            double discriminant = (b * b) - (a * c);
            if (discriminant >= 0)
            {
                double candidate = (-b - Math.Sqrt(discriminant)) / a;
                double z = o.Z + (candidate * d.Z);
                if (candidate >= 0 && candidate < t && z >= -half && z <= half)
                {
                    t = candidate;
                    localNormal = new Double3(o.X + (candidate * d.X), o.Y + (candidate * d.Y), 0);
                    hit = true;
                }
            }
        }

        foreach (double capZ in (ReadOnlySpan<double>)[-half, half])
        {
            Double3 offset = o with { Z = o.Z - capZ };
            if (!CenteredSphere(radius, offset, d, t, out double candidate) || !(candidate < t))
            {
                continue;
            }

            t = candidate;
            localNormal = offset + (d * candidate);
            hit = true;
        }

        normal = hit ? localNormal.Rotate(rotation) : default;
        return hit;
    }

    public static bool Hull(ConvexHull hull, Double3 origin, Double3 direction, double maxT, out double t, out Double3 normal)
    {
        t = 0;
        normal = default;
        if (!AxisBox(Double3.From(hull.Bounds.Minimum), Double3.From(hull.Bounds.Maximum), origin, direction, maxT, out _, out _))
        {
            return false;
        }

        double enter = 0;
        double exit = maxT;
        bool inside = true;
        Double3 enterNormal = default;
        foreach (HullPolygon polygon in hull.Polygons)
        {
            Double3 planeNormal = Double3.From(polygon.Plane.Normal);
            double distance = Double3.Dot(planeNormal, origin) + polygon.Plane.D;
            double approach = Double3.Dot(planeNormal, direction);
            if (distance > 0)
            {
                inside = false;
            }

            if (approach == 0)
            {
                if (distance > 0)
                {
                    return false;
                }

                continue;
            }

            double crossing = -distance / approach;
            if (approach < 0)
            {
                if (crossing > enter)
                {
                    enter = crossing;
                    enterNormal = planeNormal;
                }
            }
            else if (crossing < exit)
            {
                exit = crossing;
            }

            if (enter > exit)
            {
                return false;
            }
        }

        if (inside)
        {
            normal = -direction;
            return true;
        }

        if (enter >= maxT || enterNormal == default)
        {
            return false;
        }

        t = enter;
        normal = enterNormal;
        return true;
    }

    /// <summary>
    /// Ray test against a triangle mesh: through its triangle hierarchy when it has one (see <see cref="MeshTree"/>),
    /// otherwise triangle by triangle. <paramref name="triangle"/> is the index of the triangle hit.
    /// </summary>
    public static bool Mesh(TriangleMesh mesh, BoundingVolumeHierarchy? tree, Double3 origin, Double3 direction, double maxT, out double t, out Double3 normal,
        out int triangle)
    {
        t = maxT;
        normal = default;
        triangle = -1;
        if (!AxisBox(Double3.From(mesh.Bounds.Minimum), Double3.From(mesh.Bounds.Maximum), origin, direction, maxT, out _, out _))
        {
            return false;
        }

        if (tree is not null)
        {
            MeshTriangles visitor = new(mesh, origin, direction);
            tree.Raycast(origin, direction, maxT, ref visitor);
            t = visitor.Hit ? visitor.Distance : maxT;
            normal = visitor.Normal;
            triangle = visitor.TriangleIndex;
            return visitor.Hit;
        }

        bool hit = false;
        for (int candidateTriangle = 0; candidateTriangle < mesh.TriangleCount; candidateTriangle++)
        {
            if (!MeshTriangle(mesh, candidateTriangle, origin, direction, t, out double candidate, out Double3 faceNormal))
            {
                continue;
            }

            t = candidate;
            normal = faceNormal;
            triangle = candidateTriangle;
            hit = true;
        }

        return hit;
    }

    /// <summary>Builds the hierarchy over a mesh's triangles, in the mesh's space.</summary>
    public static BoundingVolumeHierarchy MeshTree(TriangleMesh mesh)
    {
        ReadOnlySpan<Vector3> vertices = mesh.Vertices.AsSpan();
        ReadOnlySpan<int> indices = mesh.Indices.AsSpan();
        WorldBox[] boxes = new WorldBox[mesh.TriangleCount];
        for (int triangle = 0; triangle < boxes.Length; triangle++)
        {
            Vector3 a = vertices[indices[triangle * 3]];
            Vector3 b = vertices[indices[(triangle * 3) + 1]];
            Vector3 c = vertices[indices[(triangle * 3) + 2]];
            boxes[triangle] = new WorldBox(Double3.From(Vector3.Min(Vector3.Min(a, b), c)), Double3.From(Vector3.Max(Vector3.Max(a, b), c)));
        }

        return BoundingVolumeHierarchy.Build(boxes);
    }

    private static bool MeshTriangle(TriangleMesh mesh, int triangle, Double3 origin, Double3 direction, double maxT, out double t, out Double3 normal)
    {
        ReadOnlySpan<Vector3> vertices = mesh.Vertices.AsSpan();
        ReadOnlySpan<int> indices = mesh.Indices.AsSpan();
        int first = triangle * 3;
        return Triangle(
            Double3.From(vertices[indices[first]]),
            Double3.From(vertices[indices[first + 1]]),
            Double3.From(vertices[indices[first + 2]]),
            origin,
            direction,
            maxT,
            out t,
            out normal);
    }

    /// <summary>Möller–Trumbore ray-triangle test; the normal is the face normal turned against the ray.</summary>
    public static bool Triangle(Double3 a, Double3 b, Double3 c, Double3 origin, Double3 direction, double maxT, out double t, out Double3 normal)
    {
        t = 0;
        normal = default;
        Double3 edge1 = b - a;
        Double3 edge2 = c - a;
        Double3 p = Double3.Cross(direction, edge2);
        double determinant = Double3.Dot(edge1, p);
        if (determinant * determinant <= 1e-24 * edge1.LengthSquared() * edge2.LengthSquared() * direction.LengthSquared())
        {
            return false;
        }

        double inverse = 1 / determinant;
        Double3 s = origin - a;
        double u = Double3.Dot(s, p) * inverse;
        if (u is < -EdgeTolerance or > 1 + EdgeTolerance)
        {
            return false;
        }

        Double3 q = Double3.Cross(s, edge1);
        double v = Double3.Dot(direction, q) * inverse;
        if (v < -EdgeTolerance || u + v > 1 + EdgeTolerance)
        {
            return false;
        }

        double candidate = Double3.Dot(edge2, q) * inverse;
        if (candidate < 0 || candidate >= maxT)
        {
            return false;
        }

        t = candidate;
        normal = Double3.Cross(edge1, edge2);
        if (Double3.Dot(normal, direction) > 0)
        {
            normal = -normal;
        }

        return true;
    }

    /// <summary>Keeps the closest triangle of a mesh a ray hits.</summary>
    private struct MeshTriangles(TriangleMesh mesh, Double3 origin, Double3 direction) : IRayVisitor
    {
        public bool Hit { get; private set; }

        public double Distance { get; private set; }

        public Double3 Normal { get; private set; }

        public int TriangleIndex { get; private set; } = -1;

        public double Visit(int item, double maxDistance)
        {
            if (!MeshTriangle(mesh, item, origin, direction, maxDistance, out double t, out Double3 normal))
            {
                return maxDistance;
            }

            Hit = true;
            Distance = t;
            Normal = normal;
            TriangleIndex = item;
            return t;
        }
    }

    /// <summary>Slab test against an axis-aligned box; reports the entry face normal, or t = 0 when starting inside.</summary>
    private static bool AxisBox(Double3 minimum, Double3 maximum, Double3 origin, Double3 direction, double maxT, out double t, out Double3 normal)
    {
        double near = double.NegativeInfinity;
        double far = double.PositiveInfinity;
        Double3 nearNormal = default;
        for (int axis = 0; axis < 3; axis++)
        {
            double o = Component(origin, axis);
            double d = Component(direction, axis);
            double low = Component(minimum, axis);
            double high = Component(maximum, axis);
            if (d == 0)
            {
                if (o < low || o > high)
                {
                    t = 0;
                    normal = default;
                    return false;
                }

                continue;
            }

            double t0 = (low - o) / d;
            double t1 = (high - o) / d;
            double sign = -1;
            if (t0 > t1)
            {
                (t0, t1) = (t1, t0);
                sign = 1;
            }

            if (t0 > near)
            {
                near = t0;
                nearNormal = Unit(axis, sign);
            }

            far = Math.Min(far, t1);
        }

        if (near > far || far < 0 || near >= maxT)
        {
            t = 0;
            normal = default;
            return false;
        }

        t = Math.Max(near, 0);
        normal = near > 0 ? nearNormal : default;
        return true;
    }

    /// <summary>First crossing of a sphere centred at the origin of <paramref name="offset"/>'s space; 0 when starting inside.</summary>
    private static bool CenteredSphere(double radius, Double3 offset, Double3 direction, double maxT, out double t)
    {
        t = 0;
        double c = offset.LengthSquared() - (radius * radius);
        if (c <= 0)
        {
            return true;
        }

        double a = direction.LengthSquared();
        double b = Double3.Dot(offset, direction);
        if (b >= 0 || a == 0)
        {
            return false;
        }

        double discriminant = (b * b) - (a * c);
        if (discriminant < 0)
        {
            return false;
        }

        double candidate = (-b - Math.Sqrt(discriminant)) / a;
        if (candidate >= maxT)
        {
            return false;
        }

        t = candidate;
        return true;
    }

    private static Double3 Unit(int axis, double sign) => axis switch
    {
        0 => new Double3(sign, 0, 0),
        1 => new Double3(0, sign, 0),
        _ => new Double3(0, 0, sign),
    };

    private static double Component(Double3 value, int axis) => axis switch
    {
        0 => value.X,
        1 => value.Y,
        _ => value.Z,
    };
}
