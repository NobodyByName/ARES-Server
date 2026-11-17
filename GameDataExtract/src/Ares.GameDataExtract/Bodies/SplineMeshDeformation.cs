using System.Numerics;
using System.Runtime.InteropServices;
using Ares.GameDataExtract.Components;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Game.Collision;

namespace Ares.GameDataExtract.Bodies;

/// <summary>
/// Bends a mesh along a spline mesh component's segment, as the engine bends a spline mesh's collision: the mesh's X
/// axis runs along the segment, its X coordinate mapped from the mesh bounds onto the segment's parameter. At that
/// parameter the segment's cubic Hermite position and direction, the frame built from the direction and the class
/// default's up direction, and the linearly interpolated offset, roll and scale place the vertex's Y and Z.
/// </summary>
internal static class SplineMeshDeformation
{
    /// <summary>The triangle mesh <paramref name="mesh"/> bent along <paramref name="spline"/>; triangles and material slots are kept.</summary>
    /// <param name="mesh">The source mesh, in the static mesh's space.</param>
    /// <param name="spline">The segment, in component space.</param>
    /// <param name="meshMinimumX">The smallest X of the static mesh's bounds.</param>
    /// <param name="meshLengthX">The X size of the static mesh's bounds.</param>
    /// <exception cref="InvalidDataException">The bounds have no length or a bent vertex is not finite.</exception>
    public static TriangleMesh Bend(TriangleMesh mesh, SplineMeshParams spline, double meshMinimumX, double meshLengthX)
    {
        if (!(meshLengthX > 0) || !double.IsFinite(meshMinimumX))
        {
            throw new InvalidDataException("The spline mesh's static mesh bounds have no length");
        }

        Vector3[] vertices = new Vector3[mesh.Vertices.Length];
        Vector3 minimum = new(float.MaxValue);
        Vector3 maximum = new(float.MinValue);
        for (int index = 0; index < vertices.Length; index++)
        {
            Vector3 source = mesh.Vertices[index];
            Vector3D bent = Place(spline, (source.X - meshMinimumX) / meshLengthX, source.Y, source.Z);
            Vector3 vertex = new((float)bent.X, (float)bent.Y, (float)bent.Z);
            if (!float.IsFinite(vertex.X) || !float.IsFinite(vertex.Y) || !float.IsFinite(vertex.Z))
            {
                throw new InvalidDataException("A bent spline mesh vertex is not finite");
            }

            vertices[index] = vertex;
            minimum = Vector3.Min(minimum, vertex);
            maximum = Vector3.Max(maximum, vertex);
        }

        return mesh with
        {
            Vertices = ImmutableCollectionsMarshal.AsImmutableArray(vertices),
            Bounds = vertices.Length == 0 ? default : new BoundingBox(minimum, maximum),
        };
    }

    /// <summary>Where a mesh point with cross coordinates (<paramref name="y"/>, <paramref name="z"/>) lands at segment parameter <paramref name="alpha"/>.</summary>
    private static Vector3D Place(SplineMeshParams spline, double alpha, double y, double z)
    {
        Vector3D position = Position(spline, alpha);
        Vector3D direction = Normalized(Tangent(spline, alpha));
        Vector3D baseX = Normalized(Cross(SplineMeshComponentMembers.DefaultSplineUpDirection, direction));
        Vector3D baseY = Normalized(Cross(direction, baseX));
        double offsetX = Lerp(spline.StartOffset.X, spline.EndOffset.X, alpha);
        double offsetY = Lerp(spline.StartOffset.Y, spline.EndOffset.Y, alpha);
        double roll = Lerp(spline.StartRoll, spline.EndRoll, alpha);
        double cos = Math.Cos(roll);
        double sin = Math.Sin(roll);
        Vector3D axisX = Add(Scale(baseX, cos), Scale(baseY, -sin));
        Vector3D axisY = Add(Scale(baseY, cos), Scale(baseX, sin));
        double scaleX = Lerp(spline.StartScale.X, spline.EndScale.X, alpha);
        double scaleY = Lerp(spline.StartScale.Y, spline.EndScale.Y, alpha);
        position = Add(Add(position, Scale(baseX, offsetX)), Scale(baseY, offsetY));
        return Add(Add(position, Scale(axisX, y * scaleX)), Scale(axisY, z * scaleY));
    }

    private static Vector3D Position(SplineMeshParams spline, double a)
    {
        double a2 = a * a;
        double a3 = a2 * a;
        return Add(
            Add(Scale(spline.StartPosition, (2 * a3) - (3 * a2) + 1), Scale(spline.StartTangent, a3 - (2 * a2) + a)),
            Add(Scale(spline.EndTangent, a3 - a2), Scale(spline.EndPosition, (-2 * a3) + (3 * a2))));
    }

    private static Vector3D Tangent(SplineMeshParams spline, double a)
    {
        Vector3D c = Add(Add(Scale(spline.StartPosition, 6), Scale(spline.StartTangent, 3)), Add(Scale(spline.EndTangent, 3), Scale(spline.EndPosition, -6)));
        Vector3D d = Add(Add(Scale(spline.StartPosition, -6), Scale(spline.StartTangent, -4)), Add(Scale(spline.EndTangent, -2), Scale(spline.EndPosition, 6)));
        return Add(Add(Scale(c, a * a), Scale(d, a)), spline.StartTangent);
    }

    private static double Lerp(double from, double to, double alpha) => from + ((to - from) * alpha);

    private static Vector3D Add(Vector3D left, Vector3D right) => new(left.X + right.X, left.Y + right.Y, left.Z + right.Z);

    private static Vector3D Scale(Vector3D value, double factor) => new(value.X * factor, value.Y * factor, value.Z * factor);

    private static Vector3D Cross(Vector3D left, Vector3D right) =>
        new((left.Y * right.Z) - (left.Z * right.Y), (left.Z * right.X) - (left.X * right.Z), (left.X * right.Y) - (left.Y * right.X));

    /// <summary>The unit vector along <paramref name="value"/>, or zero for a (nearly) zero vector.</summary>
    private static Vector3D Normalized(Vector3D value)
    {
        double squared = (value.X * value.X) + (value.Y * value.Y) + (value.Z * value.Z);
        return squared > 1e-8 ? Scale(value, 1 / Math.Sqrt(squared)) : default;
    }
}
