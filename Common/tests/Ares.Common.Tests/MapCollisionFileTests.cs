using System.Buffers.Binary;
using System.Collections.Immutable;
using System.Numerics;
using Ares.Common.Maps;
using Ares.Structs.Core;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Game.Collision;
using Quaternion = System.Numerics.Quaternion;

namespace Ares.Common.Tests;

public sealed class MapCollisionFileTests
{
    private const string MapName = "/Game/Maps/Test/Test_P";

    [Fact]
    public void A_map_round_trips_with_all_its_parts()
    {
        MapCollision map = BuildSampleMap();
        MapCollision read = RoundTrip(map);

        Assert.Equal(GameBuild.SteamBuildId, read.GameBuildId);
        Assert.Equal(MapName, read.MapName);
        Assert.Equal(map.Cells.Length, read.Cells.Length);
        for (int i = 0; i < map.Cells.Length; i++)
        {
            Assert.Equal(map.Cells[i].PackageName, read.Cells[i].PackageName);
            Assert.Equal(map.Cells[i].DataLayers, read.Cells[i].DataLayers);
        }

        Assert.True(read.Cells[0].IsBaseMap);
        Assert.False(read.Cells[1].IsBaseMap);

        Assert.Equal(map.Bodies.Length, read.Bodies.Length);
        for (int i = 0; i < map.Bodies.Length; i++)
        {
            CollisionBody expected = map.Bodies[i];
            CollisionBody actual = read.Bodies[i];
            Assert.Equal(expected.Name, actual.Name);
            Assert.Equal(expected.Boxes, actual.Boxes);
            Assert.Equal(expected.Spheres, actual.Spheres);
            Assert.Equal(expected.Capsules, actual.Capsules);
            Assert.Equal(expected.ConvexHulls.Length, actual.ConvexHulls.Length);
            for (int hull = 0; hull < expected.ConvexHulls.Length; hull++)
            {
                Assert.Equal(expected.ConvexHulls[hull].Vertices, actual.ConvexHulls[hull].Vertices);
                Assert.Equal(expected.ConvexHulls[hull].Polygons, actual.ConvexHulls[hull].Polygons);
                Assert.Equal(expected.ConvexHulls[hull].PolygonVertexIndices, actual.ConvexHulls[hull].PolygonVertexIndices);
                Assert.Equal(expected.ConvexHulls[hull].Bounds, actual.ConvexHulls[hull].Bounds);
            }

            Assert.Equal(expected.TriangleMeshes.Length, actual.TriangleMeshes.Length);
            for (int mesh = 0; mesh < expected.TriangleMeshes.Length; mesh++)
            {
                Assert.Equal(expected.TriangleMeshes[mesh].Vertices, actual.TriangleMeshes[mesh].Vertices);
                Assert.Equal(expected.TriangleMeshes[mesh].Indices, actual.TriangleMeshes[mesh].Indices);
                Assert.Equal(expected.TriangleMeshes[mesh].Bounds, actual.TriangleMeshes[mesh].Bounds);
            }
        }

        Assert.Equal(map.Placements, read.Placements);

        HeightFieldTile tile = Assert.Single(read.HeightFields);
        HeightFieldTile written = map.HeightFields[0];
        Assert.Equal(written.CellIndex, tile.CellIndex);
        Assert.Equal(written.Origin, tile.Origin);
        Assert.Equal((written.SpacingX, written.SpacingY, written.HeightScale), (tile.SpacingX, tile.SpacingY, tile.HeightScale));
        Assert.Equal((written.CountX, written.CountY), (tile.CountX, tile.CountY));
        Assert.Equal(written.Heights, tile.Heights);
        Assert.Equal(written.CellFlags, tile.CellFlags);
    }

    [Fact]
    public void Placements_store_their_transform_but_not_their_bounds()
    {
        MapCollision map = BuildSampleMap();
        byte[] bytes = Serialize(map);
        int tag = bytes.AsSpan().IndexOf("PLAC"u8);

        long length = BinaryPrimitives.ReadInt64LittleEndian(bytes.AsSpan(tag + 4));

        Assert.Equal(sizeof(int) + (64 * map.Placements.Length), length);
    }

    [Fact]
    public void Bodies_are_shared_by_name()
    {
        MapCollisionBuilder builder = new(GameBuild.SteamBuildId, MapName);
        int first = builder.AddBody(BoxBody("/Game/Props/Crate"));
        int again = builder.AddBody(SphereBody("/Game/Props/Crate"));

        Assert.Equal(first, again);
        Assert.Equal(1, builder.BodyCount);
        Assert.True(builder.TryGetBody("/Game/Props/Crate", out int found));
        Assert.Equal(first, found);
        Assert.False(builder.TryGetBody("/Game/Props/Barrel", out _));
    }

    [Fact]
    public void A_negative_non_uniform_scale_is_kept_and_its_bounds_mirror_the_body()
    {
        MapCollisionBuilder builder = new(GameBuild.SteamBuildId, MapName);
        int cell = builder.AddCell("/Game/Maps/Test/Cell_0", []);
        int body = builder.AddBody(HullBody("/Game/Props/Wedge"));
        builder.AddPlacement(body, cell, new Transform(Ares.Structs.Core.Geometry.Quaternion.Identity, new Vector3D(250000.5, -1000, 30), new Vector3D(-2, 1, 0.5)), PlacementFlags.BlocksPawn);

        BodyPlacement placement = Assert.Single(RoundTrip(builder.Build()).Placements);

        Assert.Equal(new Vector3(-2, 1, 0.5f), placement.Scale);
        Assert.Equal(new Vector3D(250000.5, -1000, 30), placement.Translation);
        Assert.Equal(new Vector3(-20, 0, 0), placement.Bounds.Minimum);
        Assert.Equal(new Vector3(0, 10, 5), placement.Bounds.Maximum);
        Assert.Equal(new Vector3D(249980.5, -1000, 30), placement.WorldMinimum);
    }

    [Fact]
    public void Bounds_contain_rotated_shapes_and_scale_spheres_by_the_largest_component()
    {
        MapCollisionBuilder builder = new(GameBuild.SteamBuildId, MapName);
        int cell = builder.AddCell("/Game/Maps/Test/Cell_0", []);
        int body = builder.AddBody(SphereBody("/Game/Props/Ball"));
        Ares.Structs.Core.Geometry.Quaternion quarterTurn = new(0, 0, Math.Sqrt(0.5), Math.Sqrt(0.5));
        builder.AddPlacement(body, cell, new Transform(quarterTurn, default, new Vector3D(1, 3, 1)), PlacementFlags.BlocksClimbable);

        BodyPlacement placement = Assert.Single(builder.Build().Placements);

        // The sphere's centre (10, 0, 0) lands at (0, 10, 0); its radius 5 grows to 15.
        Assert.InRange(placement.Bounds.Minimum.X, -15.001f, -14.999f);
        Assert.InRange(placement.Bounds.Maximum.Y, 24.999f, 25.001f);
        Assert.InRange(placement.Bounds.Maximum.Z, 14.999f, 15.001f);
    }

    [Fact]
    public void A_landscape_field_becomes_a_tile_in_landscape_axes()
    {
        // 3 rows (landscape X reversed) × 3 columns (landscape Y); height = 10·x + y.
        HeightFieldSample[] samples = new HeightFieldSample[9];
        for (int x = 0; x < 3; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                samples[((2 - x) * 3) + y] = new HeightFieldSample((short)((10 * x) + y), 0, 0);
            }
        }

        // Grid cell (0, 0) is the field cell of row 1, column 0: shared first corner, first triangle a hole.
        samples[3] = samples[3] with { MaterialIndex0 = 0x80 | HeightFieldSample.HoleMaterial };
        HeightField field = new(3, 3, 0, 0, 0, default, 0, 22, [.. samples]);

        HeightFieldTile tile = HeightFieldTile.FromLandscape(0, field, new Vector3D(100, 200, 300), 100, 100, 2);

        Assert.Equal(21, tile.Height(2, 1));
        Assert.Equal(new Vector3D(300, 300, 342), tile.SamplePosition(2, 1));
        Assert.Equal(HeightFieldCellFlags.LowYTriangleHole, tile.Cell(0, 0));
        Assert.Equal(HeightFieldCellFlags.DiagonalFromLowCorner, tile.Cell(1, 0));
    }

    [Fact]
    public void Another_format_version_is_refused()
    {
        byte[] bytes = Serialize(BuildSampleMap());
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(8), MapCollisionFile.FormatVersion + 1);

        MapCollisionFormatException exception = Assert.Throws<MapCollisionFormatException>(() => MapCollisionFile.Read(new MemoryStream(bytes)));
        Assert.Equal(MapCollisionFileError.UnsupportedFormatVersion, exception.Error);
    }

    [Fact]
    public void Another_game_build_is_refused()
    {
        byte[] bytes = Serialize(BuildSampleMap());

        MapCollisionFormatException exception = Assert.Throws<MapCollisionFormatException>(() => MapCollisionFile.Read(new MemoryStream(bytes), GameBuild.SteamBuildId + 1));
        Assert.Equal(MapCollisionFileError.GameBuildMismatch, exception.Error);
    }

    [Fact]
    public void Other_data_is_not_a_map_collision_file()
    {
        MapCollisionFormatException exception = Assert.Throws<MapCollisionFormatException>(() => MapCollisionFile.Read(new MemoryStream([.. "not a map file"u8])));
        Assert.Equal(MapCollisionFileError.NotAMapCollisionFile, exception.Error);
    }

    [Fact]
    public void A_truncated_file_is_refused_wherever_it_ends()
    {
        byte[] bytes = Serialize(BuildSampleMap());
        for (int length = 0; length < bytes.Length; length++)
        {
            MemoryStream stream = new(bytes, 0, length);
            MapCollisionFormatException exception = Assert.Throws<MapCollisionFormatException>(() => MapCollisionFile.Read(stream));
            Assert.True(
                exception.Error is MapCollisionFileError.Truncated || (length < 8 && exception.Error is MapCollisionFileError.NotAMapCollisionFile),
                $"Length {length}: {exception.Error}");
        }
    }

    [Fact]
    public void An_index_outside_the_map_is_refused()
    {
        MapCollisionBuilder builder = new(GameBuild.SteamBuildId, MapName);
        int cell = builder.AddCell("/Game/Maps/Test/Cell_0", []);
        Transform identity = new(Ares.Structs.Core.Geometry.Quaternion.Identity, default, Vector3D.One);

        Assert.Throws<ArgumentException>(() => builder.AddPlacement(0, cell, identity, PlacementFlags.BlocksPawn));
        int body = builder.AddBody(BoxBody("/Game/Props/Crate"));
        Assert.Throws<ArgumentException>(() => builder.AddPlacement(body, cell + 1, identity, PlacementFlags.BlocksPawn));
    }

    [Fact]
    public void Hulls_with_shared_or_too_few_polygons_and_shapes_outside_their_bounds_are_refused()
    {
        ConvexHull wedge = HullBody("/Game/Props/Wedge").ConvexHulls[0];
        TriangleMesh ramp = MeshBody("/Game/Props/Ramp").TriangleMeshes[0];
        HullPolygon last = wedge.Polygons[^1];
        ConvexHull[] invalidHulls =
        [
            wedge with { Polygons = wedge.Polygons.SetItem(4, last with { FirstIndex = 10 }) },
            wedge with { Polygons = [.. wedge.Polygons.Add(last)] },
            wedge with { Polygons = wedge.Polygons[..3] },
            wedge with { Bounds = new BoundingBox(Vector3.Zero, new Vector3(5, 10, 10)) },
        ];
        MapCollisionBuilder builder = new(GameBuild.SteamBuildId, MapName);
        foreach (ConvexHull hull in invalidHulls)
        {
            Assert.Throws<ArgumentException>(() => builder.AddBody(new CollisionBody("/Game/Props/Bad", [], [], [], [hull], [])));
        }

        TriangleMesh shrunk = ramp with { Bounds = new BoundingBox(Vector3.Zero, new Vector3(100, 100, 40)) };
        Assert.Throws<ArgumentException>(() => builder.AddBody(new CollisionBody("/Game/Props/Bad", [], [], [], [], [shrunk])));

        // The reader applies the same rules: the last polygon of a stored wedge made to start inside the one before it.
        byte[] data = Serialize(BuildSampleMap());
        byte[] lastRange = [.. BitConverter.GetBytes(last.FirstIndex), .. BitConverter.GetBytes(last.IndexCount)];
        int at = data.AsSpan().IndexOf(lastRange);
        BinaryPrimitives.WriteInt32LittleEndian(data.AsSpan(at), 10);
        MapCollisionFormatException error = Assert.Throws<MapCollisionFormatException>(() => MapCollisionFile.Read(new MemoryStream(data)));
        Assert.Contains("sharing", error.Message, StringComparison.Ordinal);
    }

    private static MapCollision BuildSampleMap()
    {
        MapCollisionBuilder builder = new(GameBuild.SteamBuildId, MapName);
        int baseCell = builder.AddCell("/Game/Maps/Test/Cell_0", []);
        int layerCell = builder.AddCell("/Game/Maps/Test/Cell_1", ["Coldsnap", "Night"]);
        int box = builder.AddBody(BoxBody("/Game/Props/Crate"));
        int hull = builder.AddBody(HullBody("/Game/Props/Wedge"));
        int mesh = builder.AddBody(MeshBody("/Game/Props/Ramp"));
        int sphere = builder.AddBody(SphereBody("/Game/Props/Ball"));
        Ares.Structs.Core.Geometry.Quaternion yaw = new(0, 0, Math.Sin(0.3), Math.Cos(0.3));
        builder.AddPlacement(box, baseCell, new Transform(yaw, new Vector3D(-123456.789, 98765.4321, -50.25), Vector3D.One), PlacementFlags.BlocksPawn | PlacementFlags.BlocksClimbable);
        builder.AddPlacement(hull, baseCell, new Transform(yaw, new Vector3D(10, 20, 30), new Vector3D(-1, 2, 3)), PlacementFlags.BlocksPawn);
        builder.AddPlacement(mesh, layerCell, new Transform(Ares.Structs.Core.Geometry.Quaternion.Identity, default, Vector3D.One), PlacementFlags.BlocksClimbable);
        builder.AddPlacement(sphere, layerCell, new Transform(yaw, new Vector3D(1, 1, 1), new Vector3D(0.5, 0.5, 0.5)), PlacementFlags.None);
        builder.AddHeightField(new HeightFieldTile(
            baseCell,
            new Vector3D(-50400, 25200, -1000),
            100,
            100,
            250f / 128,
            3,
            2,
            [-32768, 0, 32767, 5, 6, 7],
            [HeightFieldCellFlags.DiagonalFromLowCorner, HeightFieldCellFlags.HighYTriangleHole]));
        return builder.Build();
    }

    private static CollisionBody BoxBody(string name) =>
        new(
            name,
            [new CollisionBox(new Vector3(0, 0, 50), Quaternion.CreateFromYawPitchRoll(0.1f, 0.2f, 0.3f), new Vector3(50, 40, 50))],
            [],
            [new CollisionCapsule(new Vector3(0, 0, 120), Quaternion.Identity, 20, 30)],
            [],
            []);

    private static CollisionBody SphereBody(string name) =>
        new(name, [], [new CollisionSphere(new Vector3(10, 0, 0), 5)], [], [], []);

    private static CollisionBody HullBody(string name)
    {
        // A wedge: the triangle (0,0,0), (10,0,0), (0,10,0) extruded to Z = 10.
        ImmutableArray<Vector3> vertices =
        [
            new(0, 0, 0), new(10, 0, 0), new(0, 10, 0),
            new(0, 0, 10), new(10, 0, 10), new(0, 10, 10),
        ];
        ImmutableArray<HullPolygon> polygons =
        [
            new(new Plane(0, 0, -1, 0), 0, 3),
            new(new Plane(0, 0, 1, -10), 3, 3),
            new(new Plane(0, -1, 0, 0), 6, 4),
            new(new Plane(-1, 0, 0, 0), 10, 4),
            new(new Plane(Vector3.Normalize(new Vector3(1, 1, 0)), -10 / MathF.Sqrt(2)), 14, 4),
        ];
        ImmutableArray<byte> indices = [0, 2, 1, 3, 4, 5, 0, 1, 4, 3, 0, 3, 5, 2, 1, 2, 5, 4];
        ConvexHull hull = new(vertices, polygons, indices, new BoundingBox(Vector3.Zero, new Vector3(10, 10, 10)));
        return new CollisionBody(name, [], [], [], [hull], []);
    }

    private static CollisionBody MeshBody(string name)
    {
        TriangleMesh mesh = new(
            [new Vector3(0, 0, 0), new Vector3(100, 0, 0), new Vector3(0, 100, 50), new Vector3(100, 100, 50)],
            [0, 1, 2, 2, 1, 3],
            [],
            new BoundingBox(Vector3.Zero, new Vector3(100, 100, 50)));
        return new CollisionBody(name, [], [], [], [], [mesh]);
    }

    private static byte[] Serialize(MapCollision map)
    {
        using MemoryStream stream = new();
        MapCollisionFile.Write(stream, map);
        return stream.ToArray();
    }

    private static MapCollision RoundTrip(MapCollision map) => MapCollisionFile.Read(new MemoryStream(Serialize(map)));
}
