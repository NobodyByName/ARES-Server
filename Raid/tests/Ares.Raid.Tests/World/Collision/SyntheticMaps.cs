using System.Collections.Immutable;
using System.Numerics;
using Ares.Common.Maps;
using Ares.Structs.Core;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Game.Collision;
using Quaternion = Ares.Structs.Core.Geometry.Quaternion;

namespace Ares.Raid.Tests.World.Collision;

/// <summary>Builds small collision maps for tests.</summary>
internal static class SyntheticMaps
{
    public const string MapName = "TestMap";

    public static MapCollisionBuilder Builder(int gameBuildId = GameBuild.SteamBuildId) => new(gameBuildId, MapName);

    public static CollisionBody Body(
        string name,
        ImmutableArray<CollisionBox> boxes = default,
        ImmutableArray<CollisionSphere> spheres = default,
        ImmutableArray<CollisionCapsule> capsules = default,
        ImmutableArray<ConvexHull> hulls = default,
        ImmutableArray<TriangleMesh> meshes = default) =>
        new(
            name,
            boxes.IsDefault ? [] : boxes,
            spheres.IsDefault ? [] : spheres,
            capsules.IsDefault ? [] : capsules,
            hulls.IsDefault ? [] : hulls,
            meshes.IsDefault ? [] : meshes);

    public static CollisionBox Box(Vector3 halfExtents) => new(Vector3.Zero, System.Numerics.Quaternion.Identity, halfExtents);

    public static Transform At(double x, double y, double z, Quaternion? rotation = null, Vector3D? scale = null) =>
        new(rotation ?? Quaternion.Identity, new Vector3D(x, y, z), scale ?? Vector3D.One);

    /// <summary>Rotation by an angle in degrees around a unit axis.</summary>
    public static Quaternion Rotation(double degrees, double axisX, double axisY, double axisZ)
    {
        double half = degrees * Math.PI / 360;
        double sine = Math.Sin(half);
        return new Quaternion(axisX * sine, axisY * sine, axisZ * sine, Math.Cos(half));
    }

    /// <summary>A convex hull of an axis-aligned box, with outward face planes.</summary>
    public static ConvexHull BoxHull(Vector3 minimum, Vector3 maximum)
    {
        Vector3[] vertices = new Vector3[8];
        for (int index = 0; index < 8; index++)
        {
            vertices[index] = new Vector3(
                (index & 1) != 0 ? maximum.X : minimum.X,
                (index & 2) != 0 ? maximum.Y : minimum.Y,
                (index & 4) != 0 ? maximum.Z : minimum.Z);
        }

        (Vector3 Normal, float D, byte[] Indices)[] faces =
        [
            (Vector3.UnitX, -maximum.X, [1, 3, 7, 5]),
            (-Vector3.UnitX, minimum.X, [0, 4, 6, 2]),
            (Vector3.UnitY, -maximum.Y, [2, 6, 7, 3]),
            (-Vector3.UnitY, minimum.Y, [0, 1, 5, 4]),
            (Vector3.UnitZ, -maximum.Z, [4, 5, 7, 6]),
            (-Vector3.UnitZ, minimum.Z, [0, 2, 3, 1]),
        ];
        List<HullPolygon> polygons = [];
        List<byte> indices = [];
        foreach ((Vector3 normal, float d, byte[] faceIndices) in faces)
        {
            polygons.Add(new HullPolygon(new Plane(normal, d), indices.Count, faceIndices.Length));
            indices.AddRange(faceIndices);
        }

        return new ConvexHull([.. vertices], [.. polygons], [.. indices], new BoundingBox(minimum, maximum));
    }

    /// <summary>A square of two triangles in the XY plane, centred on the origin.</summary>
    public static TriangleMesh Square(float half) => new(
        [new Vector3(-half, -half, 0), new Vector3(half, -half, 0), new Vector3(half, half, 0), new Vector3(-half, half, 0)],
        [0, 1, 2, 0, 2, 3],
        [],
        new BoundingBox(new Vector3(-half, -half, 0), new Vector3(half, half, 0)));

    /// <summary>A tile with the same height everywhere and the given cell flags.</summary>
    public static HeightFieldTile FlatTile(int cellIndex, Vector3D origin, float spacing, float heightScale, int count, short height, HeightFieldCellFlags[]? flags = null)
    {
        short[] heights = new short[count * count];
        Array.Fill(heights, height);
        return new HeightFieldTile(
            cellIndex,
            origin,
            spacing,
            spacing,
            heightScale,
            count,
            count,
            [.. heights],
            [.. flags ?? new HeightFieldCellFlags[(count - 1) * (count - 1)]]);
    }
}
