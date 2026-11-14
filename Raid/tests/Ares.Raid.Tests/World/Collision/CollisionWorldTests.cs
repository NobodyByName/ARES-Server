using System.Numerics;
using Ares.Common.Maps;
using Ares.Raid.World;
using Ares.Raid.World.Collision;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Game.Collision;
using static Ares.Raid.Tests.World.Collision.SyntheticMaps;

namespace Ares.Raid.Tests.World.Collision;

public sealed class CollisionWorldTests
{
    private const PlacementFlags Pawn = PlacementFlags.BlocksPawn;
    private const PlacementFlags Climbable = PlacementFlags.BlocksClimbable;
    private const double Precision = 1e-4;

    [Fact]
    public void A_rotated_box_with_non_uniform_scale_is_hit_on_each_face()
    {
        // Body X (half 10 · 3) turns to world Y, body Y (half 20) to world −X, body Z (half 30 · 0.5) stays Z.
        CollisionWorld world = Single(Body("box", boxes: [Box(new Vector3(10, 20, 30))]), At(1000, 2000, 300, Rotation(90, 0, 0, 1), new Vector3D(3, 1, 0.5)));

        AssertHit(world.Raycast(new Vector3D(1010, 2025, 1000), -Vector3.UnitZ, 2000, Pawn), 685, new Vector3D(1010, 2025, 315), Vector3.UnitZ);
        AssertHit(world.Raycast(new Vector3D(2000, 2000, 300), -Vector3.UnitX, 2000, Pawn), 980, new Vector3D(1020, 2000, 300), Vector3.UnitX);
        AssertHit(world.Raycast(new Vector3D(1000, 3000, 300), -Vector3.UnitY, 2000, Pawn), 970, new Vector3D(1000, 2030, 300), Vector3.UnitY);
        Assert.Null(world.Raycast(new Vector3D(1000, 2035, 1000), -Vector3.UnitZ, 2000, Pawn));
        Assert.Null(world.Raycast(new Vector3D(1010, 2025, 1000), -Vector3.UnitZ, 600, Pawn));
    }

    [Fact]
    public void A_ray_starting_inside_a_solid_hits_at_distance_zero()
    {
        CollisionWorld world = Single(Body("box", boxes: [Box(new Vector3(50, 50, 50))]), At(0, 0, 0));

        CollisionHit hit = Assert.NotNull(world.Raycast(new Vector3D(10, 0, 0), Vector3.UnitX, 100, Pawn));
        Assert.Equal(0, hit.Distance);
        Assert.Equal(-Vector3.UnitX, hit.Normal);
    }

    [Fact]
    public void A_sphere_under_non_uniform_scale_keeps_its_smallest_scaled_radius()
    {
        // Radius 10 · 1.5; the centre scales fully, to (0, 0, 7.5).
        CollisionWorld world = Single(Body("sphere", spheres: [new CollisionSphere(new Vector3(0, 0, 5), 10)]), At(0, 0, 0, scale: new Vector3D(2, 3, 1.5)));

        AssertHit(world.Raycast(new Vector3D(100, 0, 7.5), -Vector3.UnitX, 200, Pawn), 85, new Vector3D(15, 0, 7.5), Vector3.UnitX);
        AssertHit(world.Raycast(new Vector3D(0, 0, 100), -Vector3.UnitZ, 200, Pawn), 77.5, new Vector3D(0, 0, 22.5), Vector3.UnitZ);
        AssertHit(world.Raycast(new Vector3D(9, 0, 100), -Vector3.UnitZ, 200, Pawn), 80.5, new Vector3D(9, 0, 19.5), new Vector3(0.6f, 0, 0.8f));
    }

    [Fact]
    public void A_capsule_under_non_uniform_scale_scales_its_radius_by_x_or_y_and_its_height_by_z()
    {
        // Radius 10 · 1.5 = 15; full half height (20 + 10) · 2 = 60, so the segment reaches ±45.
        CollisionCapsule capsule = new(Vector3.Zero, System.Numerics.Quaternion.Identity, 10, 20);
        CollisionWorld world = Single(Body("capsule", capsules: [capsule]), At(0, 0, 0, scale: new Vector3D(1.5, -1, 2)));

        AssertHit(world.Raycast(new Vector3D(0, 0, 200), -Vector3.UnitZ, 500, Pawn), 140, new Vector3D(0, 0, 60), Vector3.UnitZ);
        AssertHit(world.Raycast(new Vector3D(0, 100, 30), -Vector3.UnitY, 500, Pawn), 85, new Vector3D(0, 15, 30), Vector3.UnitY);
    }

    [Fact]
    public void A_turned_box_under_non_uniform_scale_scales_its_extents_along_the_body_axes()
    {
        // The box's own X turns onto body Y; its half extents (10, 20, 30) scale by (3, 1, 1) before turning.
        CollisionBox box = new(Vector3.Zero, ToNumerics(Rotation(90, 0, 0, 1)), new Vector3(10, 20, 30));
        CollisionWorld world = Single(Body("box", boxes: [box]), At(0, 0, 0, scale: new Vector3D(3, 1, 1)));

        AssertHit(world.Raycast(new Vector3D(100, 0, 0), -Vector3.UnitX, 500, Pawn), 80, new Vector3D(20, 0, 0), Vector3.UnitX);
        AssertHit(world.Raycast(new Vector3D(0, 100, 0), -Vector3.UnitY, 500, Pawn), 70, new Vector3D(0, 30, 0), Vector3.UnitY);
    }

    [Fact]
    public void A_capsule_turned_onto_the_x_axis_is_hit_on_its_side_and_caps()
    {
        CollisionCapsule capsule = new(Vector3.Zero, System.Numerics.Quaternion.Identity, 40, 52);
        CollisionWorld world = Single(Body("capsule", capsules: [capsule]), At(500, 0, 0, Rotation(90, 0, 1, 0)));

        AssertHit(world.Raycast(new Vector3D(530, 0, 500), -Vector3.UnitZ, 1000, Pawn), 460, new Vector3D(530, 0, 40), Vector3.UnitZ);
        AssertHit(world.Raycast(new Vector3D(1000, 0, 0), -Vector3.UnitX, 1000, Pawn), 408, new Vector3D(592, 0, 0), Vector3.UnitX);
        CollisionHit cap = Assert.NotNull(world.Raycast(new Vector3D(572, 0, 500), -Vector3.UnitZ, 1000, Pawn));
        Assert.Equal(Math.Sqrt((40 * 40) - (20 * 20)), cap.Point.Z, 5);
        Assert.True(cap.Normal is { X: > 0.4f, Z: > 0.8f });
    }

    [Fact]
    public void A_mirrored_convex_hull_keeps_its_faces_pointing_outwards()
    {
        MapCollisionBuilder builder = Builder();
        int cell = builder.AddCell("/Game/Cell", []);
        int body = builder.AddBody(Body("hull", hulls: [BoxHull(new Vector3(0, -5, -5), new Vector3(10, 5, 5))]));
        builder.AddPlacement(body, cell, At(0, 0, 0, scale: new Vector3D(-1, 1, 1)), Pawn);
        builder.AddPlacement(body, cell, At(0, 100, 0, Rotation(90, 0, 0, 1), new Vector3D(2, 1, 1)), Pawn);
        CollisionWorld world = new(builder.Build());

        AssertHit(world.Raycast(new Vector3D(-100, 0, 0), Vector3.UnitX, 200, Pawn), 90, new Vector3D(-10, 0, 0), -Vector3.UnitX);
        AssertHit(world.Raycast(new Vector3D(100, 0, 0), -Vector3.UnitX, 200, Pawn), 100, new Vector3D(0, 0, 0), Vector3.UnitX);

        // Turned onto +Y and stretched: it spans y 100 to 120 at x −5 to 5.
        AssertHit(world.Raycast(new Vector3D(0, 300, 0), -Vector3.UnitY, 500, Pawn), 180, new Vector3D(0, 120, 0), Vector3.UnitY);
        AssertHit(world.Raycast(new Vector3D(-50, 110, 0), Vector3.UnitX, 500, Pawn), 45, new Vector3D(-5, 110, 0), -Vector3.UnitX);
    }

    [Fact]
    public void A_triangle_mesh_collides_from_both_sides_with_the_normal_facing_the_ray()
    {
        // The square turns from the XY plane into the XZ plane and stretches along X.
        CollisionWorld world = Single(Body("mesh", meshes: [Square(50)]), At(0, 200, 0, Rotation(90, 1, 0, 0), new Vector3D(2, 1, 1)));

        AssertHit(world.Raycast(new Vector3D(80, 0, 10), Vector3.UnitY, 500, Pawn), 200, new Vector3D(80, 200, 10), -Vector3.UnitY);
        AssertHit(world.Raycast(new Vector3D(80, 400, 10), -Vector3.UnitY, 500, Pawn), 200, new Vector3D(80, 200, 10), Vector3.UnitY);
        Assert.Null(world.Raycast(new Vector3D(120, 0, 10), Vector3.UnitY, 500, Pawn));
    }

    [Fact]
    public void Large_triangle_meshes_find_the_nearest_triangle()
    {
        // 20 stacked squares (40 triangles): square k lies at z = 10k with half size 100 − 4k.
        List<Vector3> vertices = [];
        List<int> indices = [];
        for (int k = 0; k < 20; k++)
        {
            float half = 100 - (4 * k);
            int first = vertices.Count;
            vertices.AddRange([new Vector3(-half, -half, 10 * k), new Vector3(half, -half, 10 * k), new Vector3(half, half, 10 * k), new Vector3(-half, half, 10 * k)]);
            indices.AddRange([first, first + 1, first + 2, first, first + 2, first + 3]);
        }

        TriangleMesh mesh = new([.. vertices], [.. indices], [], new BoundingBox(new Vector3(-100, -100, 0), new Vector3(100, 100, 190)));
        CollisionWorld world = Single(Body("stack", meshes: [mesh]), At(0, 0, 0));
        for (double x = 1.5; x < 100; x += 7)
        {
            Assert.Equal(10 * Math.Min(19, Math.Floor((100 - x) / 4)), world.GroundHeight(x, 0.5, 500, 1000, Pawn));
            Assert.Equal(0, world.Raycast(new Vector3D(x, 0.5, -100), Vector3.UnitZ, 1000, Pawn)!.Value.Point.Z, 9);
        }
    }

    [Fact]
    public void Placements_far_from_the_origin_keep_centimetre_precision()
    {
        CollisionWorld world = Single(Body("box", boxes: [Box(new Vector3(25, 25, 25))]), At(750000.25, -750000.5, 20000.125));

        double? ground = world.GroundHeight(750010, -750010, 25000, 10000, Pawn);
        Assert.NotNull(ground);
        Assert.Equal(20025.125, ground.Value, 6);
    }

    [Fact]
    public void Placements_collide_only_with_queries_for_what_they_block()
    {
        MapCollisionBuilder builder = Builder();
        int cell = builder.AddCell("/Game/Cell", []);
        int body = builder.AddBody(Body("box", boxes: [Box(new Vector3(10, 10, 10))]));
        builder.AddPlacement(body, cell, At(0, 0, 0), Climbable);
        builder.AddPlacement(body, cell, At(0, 0, -100), PlacementFlags.None);
        CollisionWorld world = new(builder.Build());

        Assert.Equal(1, world.PlacementCount);
        Assert.Null(world.Raycast(new Vector3D(0, 0, 100), -Vector3.UnitZ, 500, Pawn));
        AssertHit(world.Raycast(new Vector3D(0, 0, 100), -Vector3.UnitZ, 500, Climbable), 90, new Vector3D(0, 0, 10), Vector3.UnitZ);
        Assert.NotNull(world.Raycast(new Vector3D(0, 0, 100), -Vector3.UnitZ, 500, Pawn | Climbable));
        Assert.Throws<ArgumentException>(() => world.Raycast(new Vector3D(0, 0, 100), -Vector3.UnitZ, 500, PlacementFlags.None));
    }

    [Fact]
    public void Layered_cells_collide_only_while_one_of_their_layers_is_active()
    {
        MapCollisionBuilder builder = Builder();
        int baseCell = builder.AddCell("/Game/Base", []);
        int layered = builder.AddCell("/Game/Layered", ["Layer_A", "Layer_B"]);
        int body = builder.AddBody(Body("box", boxes: [Box(new Vector3(10, 10, 10))]));
        builder.AddPlacement(body, baseCell, At(0, 0, 0), Pawn);
        builder.AddPlacement(body, layered, At(0, 0, 50), Pawn);
        builder.AddHeightField(FlatTile(layered, new Vector3D(-100, -100, 100), 100, 1, 3, 0));
        MapCollision map = builder.Build();

        CollisionWorld baseOnly = new(map);
        Assert.Equal((1, 0), (baseOnly.PlacementCount, baseOnly.HeightFieldCount));
        Assert.Equal(10, baseOnly.GroundHeight(0, 0, 500, 1000, Pawn));
        Assert.Equal(10, new CollisionWorld(map, ["Layer_C"]).GroundHeight(0, 0, 500, 1000, Pawn));

        CollisionWorld layeredWorld = new(map, ["Layer_B"]);
        Assert.Equal((2, 1), (layeredWorld.PlacementCount, layeredWorld.HeightFieldCount));
        CollisionHit hit = Assert.NotNull(layeredWorld.Raycast(new Vector3D(0, 0, 500), -Vector3.UnitZ, 1000, Pawn));
        Assert.Equal((CollisionHitKind.HeightField, 0, 100.0), (hit.Kind, hit.Index, hit.Point.Z));
        Assert.Equal(60, layeredWorld.GroundHeight(0, 0, 90, 1000, Pawn));
    }

    [Fact]
    public void Each_raid_indexes_the_map_with_its_own_active_data_layers()
    {
        DamWorlds withoutCollision = new(() => 0, null, () => 7);
        withoutCollision.Prepare("a", [1]);
        Assert.Null(withoutCollision.Current!.Collision);
        string activeLayer = withoutCollision.Current.ActiveDataLayers[0];

        MapCollisionBuilder builder = Builder();
        int body = builder.AddBody(Body("box", boxes: [Box(new Vector3(10, 10, 10))]));
        builder.AddPlacement(body, builder.AddCell("/Game/Base", []), At(0, 0, 0), Pawn);
        builder.AddPlacement(body, builder.AddCell("/Game/Active", [activeLayer]), At(100, 0, 0), Pawn);
        builder.AddPlacement(body, builder.AddCell("/Game/Inactive", ["Layer_Inactive"]), At(200, 0, 0), Pawn);
        DamWorlds worlds = new(() => 0, null, () => 7, collision: builder.Build());
        worlds.Prepare("a", [1]);

        Assert.Equal(2, worlds.Current!.Collision!.PlacementCount);
    }

    [Theory]
    [InlineData(HeightFieldCellFlags.None, 30.0, 30.0, 0.0)]
    [InlineData(HeightFieldCellFlags.None, 60.0, 60.0, 20.0)]
    [InlineData(HeightFieldCellFlags.None, 60.0, 30.0, 0.0)]
    [InlineData(HeightFieldCellFlags.DiagonalFromLowCorner, 60.0, 30.0, 30.0)]
    [InlineData(HeightFieldCellFlags.DiagonalFromLowCorner, 30.0, 60.0, 30.0)]
    [InlineData(HeightFieldCellFlags.DiagonalFromLowCorner, 20.0, 70.0, 20.0)]
    [InlineData(HeightFieldCellFlags.LowYTriangleHole, 30.0, 30.0, null)]
    [InlineData(HeightFieldCellFlags.LowYTriangleHole, 60.0, 60.0, 20.0)]
    [InlineData(HeightFieldCellFlags.HighYTriangleHole, 60.0, 60.0, null)]
    [InlineData(HeightFieldCellFlags.HighYTriangleHole, 30.0, 30.0, 0.0)]
    public void Height_fields_follow_the_cell_diagonal_and_holes(HeightFieldCellFlags flags, double x, double y, double? expected)
    {
        // One cell, raised only at sample (1, 1).
        MapCollisionBuilder builder = Builder();
        int cell = builder.AddCell("/Game/Cell", []);
        builder.AddHeightField(new HeightFieldTile(cell, new Vector3D(0, 0, 0), 100, 100, 1, 2, 2, [0, 0, 0, 100], [flags]));
        CollisionWorld world = new(builder.Build());

        double? height = world.GroundHeight(x, y, 500, 1000, Pawn);
        Assert.Equal(expected is null, height is null);
        if (expected is not null)
        {
            Assert.Equal(expected.Value, height!.Value, 9);
        }
    }

    [Fact]
    public void A_slanted_ray_walks_the_height_field_cells_and_falls_through_holes()
    {
        // 5 × 5 samples, 100 apart, all at height 20 × 0.5 above the origin: the surface is z = 60.
        HeightFieldCellFlags[] holes = new HeightFieldCellFlags[16];
        holes[(0 * 4) + 3] = HeightFieldCellFlags.LowYTriangleHole | HeightFieldCellFlags.HighYTriangleHole;
        Vector3D origin = new(1000, 2000, 50);
        Vector3 direction = Vector3.Normalize(new Vector3(4, 0, -1));

        MapCollisionBuilder solid = Builder();
        solid.AddHeightField(FlatTile(solid.AddCell("/Game/Cell", []), origin, 100, 0.5f, 5, 20));
        CollisionHit hit = Assert.NotNull(new CollisionWorld(solid.Build()).Raycast(new Vector3D(950, 2050, 160), direction, 1000, Pawn));
        Assert.Equal((CollisionHitKind.HeightField, 1350.0, 2050.0, 60.0), (hit.Kind, Math.Round(hit.Point.X, 6), Math.Round(hit.Point.Y, 6), Math.Round(hit.Point.Z, 6)));
        Assert.Equal(Vector3.UnitZ, hit.Normal);

        MapCollisionBuilder holed = Builder();
        holed.AddHeightField(FlatTile(holed.AddCell("/Game/Cell", []), origin, 100, 0.5f, 5, 20, holes));
        Assert.Null(new CollisionWorld(holed.Build()).Raycast(new Vector3D(950, 2050, 160), direction, 1000, Pawn));
    }

    [Fact]
    public void The_nearest_of_many_placements_is_found()
    {
        // A 40 × 40 grid of boxes, 200 apart, 100 wide, with tops at different heights.
        MapCollisionBuilder builder = Builder();
        int cell = builder.AddCell("/Game/Cell", []);
        int body = builder.AddBody(Body("box", boxes: [Box(new Vector3(50, 50, 50))]));
        for (int i = 0; i < 40; i++)
        {
            for (int j = 0; j < 40; j++)
            {
                builder.AddPlacement(body, cell, At(i * 200, j * 200, Top(i, j) - 50), Pawn);
            }
        }

        CollisionWorld world = new(builder.Build());
        Random random = new(7);
        for (int ray = 0; ray < 2000; ray++)
        {
            double x = random.NextDouble() * 8000;
            double y = random.NextDouble() * 8000;
            int i = (int)Math.Round(x / 200);
            int j = (int)Math.Round(y / 200);
            bool over = Math.Abs(x - (i * 200)) < 49.9 && Math.Abs(y - (j * 200)) < 49.9 && i < 40 && j < 40;
            bool between = Math.Abs(x - (i * 200)) > 50.1 || Math.Abs(y - (j * 200)) > 50.1 || i >= 40 || j >= 40;
            double? height = world.GroundHeight(x, y, 5000, 10000, Pawn);
            if (over)
            {
                Assert.Equal(Top(i, j), height!.Value, 9);
            }
            else if (between)
            {
                Assert.Null(height);
            }
        }

        // Along a row the first box blocks the ray.
        CollisionHit hit = Assert.NotNull(world.Raycast(new Vector3D(-1000, 600, Top(0, 3) - 10), Vector3.UnitX, 20000, Pawn));
        Assert.Equal(-50, hit.Point.X, 9);
        return;

        static double Top(int i, int j) => 50 + (((i * 3) + j) % 7 * 30);
    }

    [Fact]
    public void A_deep_placement_hierarchy_is_searched_down_to_its_last_leaf()
    {
        // Boxes at doubling distances split off one at a time, so the hierarchy is as deep as the boxes are many.
        MapCollisionBuilder builder = Builder();
        int cell = builder.AddCell("/Game/Cell", []);
        int body = builder.AddBody(Body("box", boxes: [Box(new Vector3(50, 50, 50))]));
        const int Count = 48;
        for (int k = 0; k < Count; k++)
        {
            builder.AddPlacement(body, cell, At(200 * Math.Pow(2, k), 0, k), Pawn);
        }

        CollisionWorld world = new(builder.Build());
        for (int k = 0; k < Count; k++)
        {
            Assert.Equal(50 + k, world.GroundHeight(200 * Math.Pow(2, k), 0, 5000, 10000, Pawn)!.Value, 9);
            Assert.True(world.OverlapCapsule(new Vector3D(200 * Math.Pow(2, k), 0, k), System.Numerics.Quaternion.Identity, 10, 20, Pawn));
        }
    }

    private static System.Numerics.Quaternion ToNumerics(Structs.Core.Geometry.Quaternion rotation) =>
        new((float)rotation.X, (float)rotation.Y, (float)rotation.Z, (float)rotation.W);

    [Fact]
    public void Hits_report_the_surface_type_of_the_cell_placement_or_triangle_they_reach()
    {
        TriangleMesh ramp = Square(100) with { MaterialIndices = [1, 0] };
        MapCollisionBuilder builder = Builder();
        int cell = builder.AddCell("/Game/Cell", []);
        builder.AddPlacement(builder.AddBody(Body("crate", boxes: [Box(new Vector3(50, 50, 50))])), cell, At(0, 0, 0), Pawn, PhysicalSurfaceType.Wood);
        builder.AddPlacement(builder.AddBody(Body("ramp", meshes: [ramp])), cell, At(1000, 0, 0), Pawn, PhysicalSurfaceType.Concrete,
            [PhysicalSurfaceType.Metal, PhysicalSurfaceType.Tile]);
        builder.AddPlacement(builder.AddBody(Body("plain", meshes: [Square(100)])), cell, At(2000, 0, 0), Pawn, PhysicalSurfaceType.Concrete);
        HeightFieldTile tile = FlatTile(cell, new Vector3D(-5000, -5000, -100), 100, 1, 3, 0) with
        {
            CellSurfaces = [PhysicalSurfaceType.Sand, PhysicalSurfaceType.Grass, PhysicalSurfaceType.Snow, PhysicalSurfaceType.Mud],
        };
        builder.AddHeightField(tile);
        CollisionWorld world = new(builder.Build());

        Assert.Equal(PhysicalSurfaceType.Wood, Ground(world, 0, 0));
        // Triangle 0 (below the diagonal, y < x) names slot 1, triangle 1 slot 0.
        Assert.Equal(PhysicalSurfaceType.Tile, Ground(world, 1050, -50));
        Assert.Equal(PhysicalSurfaceType.Metal, Ground(world, 950, 50));
        Assert.Equal(PhysicalSurfaceType.Concrete, Ground(world, 2000, 0));
        Assert.Equal(PhysicalSurfaceType.Grass, Ground(world, -4850, -4950));
        Assert.Equal(PhysicalSurfaceType.Snow, Ground(world, -4950, -4850));
        Assert.Equal(PhysicalSurfaceType.Wood, world.SweepSphere(new Vector3D(0, 0, 200), new Vector3D(0, 0, 0), 10, Pawn)?.Surface);
        Assert.Equal(PhysicalSurfaceType.Mud, world.SweepSphere(new Vector3D(-4850, -4850, 200), new Vector3D(-4850, -4850, -200), 10, Pawn)?.Surface);
    }

    private static PhysicalSurfaceType? Ground(CollisionWorld world, double x, double y) =>
        world.Raycast(new Vector3D(x, y, 500), -Vector3.UnitZ, 1000, Pawn)?.Surface;

    private static CollisionWorld Single(CollisionBody body, Transform transform)
    {
        MapCollisionBuilder builder = Builder();
        int cell = builder.AddCell("/Game/Cell", []);
        builder.AddPlacement(builder.AddBody(body), cell, transform, Pawn | Climbable);
        return new CollisionWorld(builder.Build());
    }

    private static void AssertHit(CollisionHit? result, double distance, Vector3D point, Vector3 normal)
    {
        CollisionHit hit = Assert.NotNull(result);
        Assert.Equal(distance, hit.Distance, Precision);
        Assert.Equal(point.X, hit.Point.X, Precision);
        Assert.Equal(point.Y, hit.Point.Y, Precision);
        Assert.Equal(point.Z, hit.Point.Z, Precision);
        Assert.Equal(normal.X, hit.Normal.X, 5);
        Assert.Equal(normal.Y, hit.Normal.Y, 5);
        Assert.Equal(normal.Z, hit.Normal.Z, 5);
    }
}
