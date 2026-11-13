using System.Collections.Immutable;
using System.Numerics;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Game.Collision;
using Quaternion = System.Numerics.Quaternion;

namespace Ares.Common.Maps;

/// <summary>Consistency rules shared by the builder and the file reader, and the placement bounds.</summary>
internal static class MapCollisionRules
{
    private const float UnitTolerance = 1e-3f;
    private const int MinimumHullPolygons = 4;
    private const float BoundsTolerance = 1e-3f;
    private const float RelativeBoundsTolerance = 1e-6f;

    /// <summary>Describes what is wrong with a body, or returns <see langword="null"/> when it is valid.</summary>
    public static string? FindBodyError(CollisionBody body)
    {
        if (IsUnset(body.Name) || body.Boxes.IsDefault || body.Spheres.IsDefault || body.Capsules.IsDefault ||
            body.ConvexHulls.IsDefault || body.TriangleMeshes.IsDefault)
        {
            return "has an unset member";
        }

        if (body.Boxes.IsEmpty && body.Spheres.IsEmpty && body.Capsules.IsEmpty && body.ConvexHulls.IsEmpty &&
            body.TriangleMeshes.IsEmpty)
        {
            return "has no shapes";
        }

        if (body.Boxes.Any(box => !IsFinite(box.Center) || !IsUnit(box.Rotation) || !IsFinite(box.HalfExtents) ||
                box.HalfExtents.X < 0 || box.HalfExtents.Y < 0 || box.HalfExtents.Z < 0))
        {
            return "has an invalid box";
        }

        if (body.Spheres.Any(sphere => !IsFinite(sphere.Center) || !float.IsFinite(sphere.Radius) || sphere.Radius < 0))
        {
            return "has an invalid sphere";
        }

        if (body.Capsules.Any(capsule => !IsFinite(capsule.Center) || !IsUnit(capsule.Rotation) || !float.IsFinite(capsule.Radius) ||
                capsule.Radius < 0 || !float.IsFinite(capsule.HalfLength) || capsule.HalfLength < 0))
        {
            return "has an invalid capsule";
        }

        string? hullError = body.ConvexHulls.Select(FindHullError).FirstOrDefault(error => error is not null);
        if (hullError is not null)
        {
            return "has a convex hull that " + hullError;
        }

        string? meshError = body.TriangleMeshes.Select(FindMeshError).FirstOrDefault(error => error is not null);
        return meshError is null ? null : "has a triangle mesh that " + meshError;
    }

    /// <summary>Describes what is wrong with a tile, or returns <see langword="null"/> when it is valid.</summary>
    public static string? FindTileError(HeightFieldTile tile, int cellCount)
    {
        if ((uint)tile.CellIndex >= (uint)cellCount)
        {
            return $"refers to cell {tile.CellIndex} of {cellCount}";
        }

        if (!IsFinite(tile.Origin) || !float.IsFinite(tile.SpacingX) || tile.SpacingX <= 0 ||
            !float.IsFinite(tile.SpacingY) || tile.SpacingY <= 0 || !float.IsFinite(tile.HeightScale))
        {
            return "has an invalid origin, spacing or height scale";
        }

        if (tile.CountX < 2 || tile.CountY < 2 || (long)tile.CountX * tile.CountY > int.MaxValue)
        {
            return $"has an invalid size {tile.CountX} × {tile.CountY}";
        }

        if (tile.Heights.IsDefault || tile.Heights.Length != tile.CountX * tile.CountY ||
            tile.CellFlags.IsDefault || tile.CellFlags.Length != (tile.CountX - 1) * (tile.CountY - 1))
        {
            return "has the wrong number of heights or cell flags";
        }

        const HeightFieldCellFlags All = HeightFieldCellFlags.DiagonalFromLowCorner | HeightFieldCellFlags.LowYTriangleHole |
            HeightFieldCellFlags.HighYTriangleHole;
        HeightFieldCellFlags unknown = tile.CellFlags.FirstOrDefault(flags => (flags & ~All) != 0);
        return unknown == 0 ? null : $"has unknown cell flags {(byte)unknown}";
    }

    /// <summary>Describes what is wrong with a placement, or returns <see langword="null"/> when it is valid.</summary>
    public static string? FindPlacementError(in BodyPlacement placement, int bodyCount, int cellCount)
    {
        if ((uint)placement.BodyIndex >= (uint)bodyCount)
        {
            return $"refers to body {placement.BodyIndex} of {bodyCount}";
        }

        if ((uint)placement.CellIndex >= (uint)cellCount)
        {
            return $"refers to cell {placement.CellIndex} of {cellCount}";
        }

        if (!IsFinite(placement.Translation) || !IsUnit(placement.Rotation) || !IsFinite(placement.Scale))
        {
            return "has an invalid transform";
        }

        if ((placement.Flags & ~(PlacementFlags.BlocksPawn | PlacementFlags.BlocksClimbable)) != 0)
        {
            return $"has unknown flags {(uint)placement.Flags}";
        }

        Vector3 minimum = placement.Bounds.Minimum;
        Vector3 maximum = placement.Bounds.Maximum;
        if (!IsFinite(minimum) || !IsFinite(maximum) || minimum.X > maximum.X || minimum.Y > maximum.Y || minimum.Z > maximum.Z)
        {
            return "has invalid bounds";
        }

        return null;
    }

    /// <summary>
    /// World bounds of a body placed with a rotation and scale, relative to the placement's translation, rounded
    /// outwards to single precision.
    /// </summary>
    public static BoundingBox PlacedBounds(CollisionBody body, Quaternion rotation, Vector3 scale)
    {
        BoundsAccumulator bounds = BoundsAccumulator.Empty;
        double largestScale = Math.Max(Math.Abs(scale.X), Math.Max(Math.Abs(scale.Y), Math.Abs(scale.Z)));
        foreach (CollisionBox box in body.Boxes)
        {
            Vector3D center = Scaled(box.Center, scale);
            Vector3D half = new(box.HalfExtents.X * Math.Abs(scale.X), box.HalfExtents.Y * Math.Abs(scale.Y), box.HalfExtents.Z * Math.Abs(scale.Z));
            for (int corner = 0; corner < 8; corner++)
            {
                Vector3D offset = Rotate(
                    box.Rotation,
                    new Vector3D((corner & 1) == 0 ? -half.X : half.X, (corner & 2) == 0 ? -half.Y : half.Y, (corner & 4) == 0 ? -half.Z : half.Z));
                bounds.Include(Rotate(rotation, Add(center, offset, 1)), 0);
            }
        }

        foreach (CollisionSphere sphere in body.Spheres)
        {
            bounds.Include(Rotate(rotation, Scaled(sphere.Center, scale)), sphere.Radius * largestScale);
        }

        foreach (CollisionCapsule capsule in body.Capsules)
        {
            Vector3D center = Scaled(capsule.Center, scale);
            Vector3D axis = Rotate(capsule.Rotation, new Vector3D(0, 0, capsule.HalfLength * largestScale));
            double radius = capsule.Radius * largestScale;
            bounds.Include(Rotate(rotation, Add(center, axis, 1)), radius);
            bounds.Include(Rotate(rotation, Add(center, axis, -1)), radius);
        }

        foreach (Vector3 vertex in body.ConvexHulls.SelectMany(hull => hull.Vertices))
        {
            bounds.Include(Rotate(rotation, Scaled(vertex, scale)), 0);
        }

        foreach (Vector3 vertex in body.TriangleMeshes.SelectMany(mesh => mesh.Vertices))
        {
            bounds.Include(Rotate(rotation, Scaled(vertex, scale)), 0);
        }

        return bounds.ToBoundingBox();
    }

    public static bool IsFinite(Vector3 value) => float.IsFinite(value.X) && float.IsFinite(value.Y) && float.IsFinite(value.Z);

    public static bool IsFinite(Vector3D value) => double.IsFinite(value.X) && double.IsFinite(value.Y) && double.IsFinite(value.Z);

    private static bool IsUnit(Quaternion value) =>
        float.IsFinite(value.X) && float.IsFinite(value.Y) && float.IsFinite(value.Z) && float.IsFinite(value.W) &&
        MathF.Abs(value.LengthSquared() - 1) <= UnitTolerance;

    /// <summary>Whether a reference that callers must set was left null; records built with <c>null!</c> or <c>default</c> get here.</summary>
    private static bool IsUnset(object? value) => value is null;

    private static string? FindHullError(ConvexHull? hull)
    {
        if (hull is null)
        {
            return "is unset";
        }

        if (hull.Vertices.IsDefault || hull.Polygons.IsDefault || hull.PolygonVertexIndices.IsDefault)
        {
            return "has an unset member";
        }

        int vertexCount = hull.Vertices.Length;
        if (vertexCount is 0 or > byte.MaxValue)
        {
            return $"has {vertexCount} vertices";
        }

        if (!hull.Vertices.All(IsFinite))
        {
            return "has a non-finite vertex";
        }

        if (hull.Polygons.Length < MinimumHullPolygons)
        {
            return $"has {hull.Polygons.Length} polygons";
        }

        bool[] used = new bool[hull.PolygonVertexIndices.Length];
        foreach (HullPolygon polygon in hull.Polygons)
        {
            if (!IsFinite(polygon.Plane.Normal) || !float.IsFinite(polygon.Plane.D) || polygon.IndexCount < 3 ||
                polygon.FirstIndex < 0 || polygon.FirstIndex > hull.PolygonVertexIndices.Length - polygon.IndexCount)
            {
                return "has an invalid polygon";
            }

            for (int index = polygon.FirstIndex; index < polygon.FirstIndex + polygon.IndexCount; index++)
            {
                if (used[index])
                {
                    return "has polygons sharing vertex indices";
                }

                used[index] = true;
            }
        }

        foreach (byte index in hull.PolygonVertexIndices.Where(index => index >= vertexCount))
        {
            return $"refers to vertex {index} of {vertexCount}";
        }

        return FindBoundsError(hull.Bounds, hull.Vertices);
    }

    private static string? FindMeshError(TriangleMesh? mesh)
    {
        if (mesh is null)
        {
            return "is unset";
        }

        if (mesh.Vertices.IsDefault || mesh.Indices.IsDefault || mesh.MaterialIndices.IsDefault)
        {
            return "has an unset member";
        }

        if (mesh.Indices.IsEmpty || mesh.Indices.Length % 3 != 0)
        {
            return $"has {mesh.Indices.Length} indices";
        }

        if (!mesh.Vertices.All(IsFinite))
        {
            return "has a non-finite vertex";
        }

        int vertexCount = mesh.Vertices.Length;
        foreach (int index in mesh.Indices.Where(index => (uint)index >= (uint)vertexCount))
        {
            return $"refers to vertex {index} of {vertexCount}";
        }

        return FindBoundsError(mesh.Bounds, mesh.Vertices);
    }

    /// <summary>Bounds must be finite and contain every vertex, give or take a rounding tolerance.</summary>
    private static string? FindBoundsError(BoundingBox bounds, ImmutableArray<Vector3> vertices)
    {
        Vector3 minimum = bounds.Minimum;
        Vector3 maximum = bounds.Maximum;
        if (!IsFinite(minimum) || !IsFinite(maximum))
        {
            return "has non-finite bounds";
        }

        return vertices.Any(vertex =>
            vertex.X < minimum.X - Slack(minimum.X) || vertex.Y < minimum.Y - Slack(minimum.Y) || vertex.Z < minimum.Z - Slack(minimum.Z) ||
            vertex.X > maximum.X + Slack(maximum.X) || vertex.Y > maximum.Y + Slack(maximum.Y) || vertex.Z > maximum.Z + Slack(maximum.Z))
            ? "has bounds that do not contain its vertices"
            : null;
    }

    private static float Slack(float bound) => BoundsTolerance + (MathF.Abs(bound) * RelativeBoundsTolerance);

    private static Vector3D Scaled(Vector3 value, Vector3 scale) => new(value.X * (double)scale.X, value.Y * (double)scale.Y, value.Z * (double)scale.Z);

    private static Vector3D Add(Vector3D value, Vector3D offset, double factor) =>
        new(value.X + (offset.X * factor), value.Y + (offset.Y * factor), value.Z + (offset.Z * factor));

    private static Vector3D Rotate(Quaternion rotation, Vector3D value)
    {
        double x = rotation.X;
        double y = rotation.Y;
        double z = rotation.Z;
        double w = rotation.W;
        double tx = 2 * ((y * value.Z) - (z * value.Y));
        double ty = 2 * ((z * value.X) - (x * value.Z));
        double tz = 2 * ((x * value.Y) - (y * value.X));
        return new Vector3D(
            value.X + (w * tx) + ((y * tz) - (z * ty)),
            value.Y + (w * ty) + ((z * tx) - (x * tz)),
            value.Z + (w * tz) + ((x * ty) - (y * tx)));
    }

    private struct BoundsAccumulator
    {
        private double _minimumX;
        private double _minimumY;
        private double _minimumZ;
        private double _maximumX;
        private double _maximumY;
        private double _maximumZ;

        public static BoundsAccumulator Empty => new()
        {
            _minimumX = double.PositiveInfinity,
            _minimumY = double.PositiveInfinity,
            _minimumZ = double.PositiveInfinity,
            _maximumX = double.NegativeInfinity,
            _maximumY = double.NegativeInfinity,
            _maximumZ = double.NegativeInfinity,
        };

        public void Include(Vector3D point, double radius)
        {
            _minimumX = Math.Min(_minimumX, point.X - radius);
            _minimumY = Math.Min(_minimumY, point.Y - radius);
            _minimumZ = Math.Min(_minimumZ, point.Z - radius);
            _maximumX = Math.Max(_maximumX, point.X + radius);
            _maximumY = Math.Max(_maximumY, point.Y + radius);
            _maximumZ = Math.Max(_maximumZ, point.Z + radius);
        }

        public readonly BoundingBox ToBoundingBox() =>
            new(
                new Vector3(Down(_minimumX), Down(_minimumY), Down(_minimumZ)),
                new Vector3(Up(_maximumX), Up(_maximumY), Up(_maximumZ)));

        private static float Down(double value)
        {
            float result = (float)value;
            return result > value ? MathF.BitDecrement(result) : result;
        }

        private static float Up(double value)
        {
            float result = (float)value;
            return result < value ? MathF.BitIncrement(result) : result;
        }
    }
}
