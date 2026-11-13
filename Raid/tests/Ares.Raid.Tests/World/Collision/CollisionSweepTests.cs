using System.Numerics;
using Ares.Common.Maps;
using Ares.Raid.World.Collision;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Game.Collision;
using static Ares.Raid.Tests.World.Collision.SyntheticMaps;
using NumericsQuaternion = System.Numerics.Quaternion;

namespace Ares.Raid.Tests.World.Collision;

public sealed class CollisionSweepTests
{
    private const PlacementFlags Pawn = PlacementFlags.BlocksPawn;
    private const PlacementFlags Climbable = PlacementFlags.BlocksClimbable;
    private const double Precision = 2e-3;

    [Fact]
    public void A_sphere_sweep_meets_each_face_of_a_turned_and_scaled_box()
    {
        // World half extents: 20 along X, 30 along Y, 15 along Z, centred on (1000, 2000, 300).
        CollisionWorld world = Single(Body("box", boxes: [Box(new Vector3(10, 20, 30))]), At(1000, 2000, 300, Rotation(90, 0, 0, 1), new Vector3D(3, 1, 0.5)));

        AssertHit(world.SweepSphere(new Vector3D(1010, 2025, 1000), new Vector3D(1010, 2025, 0), 5, Pawn), 680, new Vector3D(1010, 2025, 315), Vector3.UnitZ);
        AssertHit(world.SweepSphere(new Vector3D(2000, 2000, 300), new Vector3D(0, 2000, 300), 5, Pawn), 975, new Vector3D(1020, 2000, 300), Vector3.UnitX);
        AssertHit(world.SweepSphere(new Vector3D(1000, 3000, 300), new Vector3D(1000, 1000, 300), 5, Pawn), 965, new Vector3D(1000, 2030, 300), Vector3.UnitY);
        Assert.Null(world.SweepSphere(new Vector3D(1000, 2040, 1000), new Vector3D(1000, 2040, 0), 5, Pawn));
        Assert.Null(world.SweepSphere(new Vector3D(1010, 2025, 1000), new Vector3D(1010, 2025, 330), 5, Pawn));
    }

    [Fact]
    public void A_capsule_meets_a_scaled_sphere_on_its_side_and_on_its_rounded_end()
    {
        CollisionWorld world = Single(Body("sphere", spheres: [new CollisionSphere(Vector3.Zero, 10)]), At(0, 0, 0, scale: new Vector3D(2, 2, 2)));

        AssertHit(world.SweepCapsule(new Vector3D(-100, 0, 0), new Vector3D(100, 0, 0), 5, 15, Pawn), 75, new Vector3D(-20, 0, 0), -Vector3.UnitX);

        // Raised by 18, the lowest point of the capsule's segment (z = 8) is the one that touches.
        CollisionHit hit = Assert.NotNull(world.SweepCapsule(new Vector3D(-100, 0, 18), new Vector3D(100, 0, 18), 5, 15, Pawn));
        double x = -Math.Sqrt((25 * 25) - (8 * 8));
        Assert.Equal(100 + x, hit.Distance, Precision);
        AssertVector(new Vector3D(x * 20 / 25, 0, 8.0 * 20 / 25), hit.Point);
        AssertNormal(Vector3.Normalize(new Vector3((float)x, 0, 8)), hit.Normal);
    }

    [Fact]
    public void A_capsule_lying_along_x_meets_a_turned_capsule()
    {
        // The placed capsule lies along X from 448 to 552 with radius 40.
        CollisionCapsule capsule = new(Vector3.Zero, NumericsQuaternion.Identity, 40, 52);
        CollisionWorld world = Single(Body("capsule", capsules: [capsule]), At(500, 0, 0, Rotation(90, 0, 1, 0)));
        NumericsQuaternion alongX = NumericsQuaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI / 2);

        AssertHit(world.SweepSphere(new Vector3D(530, 0, 500), new Vector3D(530, 0, 0), 10, Pawn), 450, new Vector3D(530, 0, 40), Vector3.UnitZ);
        // Parallel segments touch along a line; the point is somewhere on it.
        CollisionHit parallel = Assert.NotNull(world.SweepCapsule(new Vector3D(500, 0, 500), new Vector3D(500, 0, 0), alongX, 10, 100, Pawn));
        Assert.Equal(450, parallel.Distance, Precision);
        Assert.Equal(40, parallel.Point.Z, Precision);
        Assert.InRange(parallel.Point.X, 447.99, 552.01);
        AssertNormal(Vector3.UnitZ, parallel.Normal);

        // End to end along X: the caps meet at x = 552 + 40.
        AssertHit(world.SweepCapsule(new Vector3D(1000, 0, 0), new Vector3D(0, 0, 0), alongX, 10, 100, Pawn), 308, new Vector3D(592, 0, 0), Vector3.UnitX);
    }

    [Fact]
    public void A_mirrored_and_scaled_hull_is_swept_against_its_outer_faces()
    {
        MapCollisionBuilder builder = Builder();
        int cell = builder.AddCell("/Game/Cell", []);
        int body = builder.AddBody(Body("hull", hulls: [BoxHull(new Vector3(0, -5, -5), new Vector3(10, 5, 5))]));
        builder.AddPlacement(body, cell, At(0, 0, 0, scale: new Vector3D(-1, 1, 1)), Pawn);
        builder.AddPlacement(body, cell, At(0, 100, 0, Rotation(90, 0, 0, 1), new Vector3D(2, 1, 1)), Pawn);
        CollisionWorld world = new(builder.Build());

        AssertHit(world.SweepSphere(new Vector3D(-100, 0, 0), new Vector3D(100, 0, 0), 2, Pawn), 88, new Vector3D(-10, 0, 0), -Vector3.UnitX);
        AssertHit(world.SweepSphere(new Vector3D(100, 0, 0), new Vector3D(-100, 0, 0), 2, Pawn), 98, new Vector3D(0, 0, 0), Vector3.UnitX);
        AssertHit(world.SweepSphere(new Vector3D(0, 300, 0), new Vector3D(0, 0, 0), 3, Pawn), 177, new Vector3D(0, 120, 0), Vector3.UnitY);

        // A hull corner rounds the normal: (5, 120, 5) seen from up and to the side.
        CollisionHit corner = Assert.NotNull(world.SweepSphere(new Vector3D(8, 300, 8), new Vector3D(8, 0, 8), 10, Pawn));
        double y = 120 + Math.Sqrt(100 - 18);
        Assert.Equal(300 - y, corner.Distance, Precision);
        AssertVector(new Vector3D(5, 120, 5), corner.Point);
        AssertNormal(Vector3.Normalize(new Vector3(3, (float)(y - 120), 3)), corner.Normal);
    }

    [Fact]
    public void Triangle_meshes_collide_from_both_sides_under_scale_and_through_their_hierarchy()
    {
        // The square turns into the XZ plane at y = 200 and stretches to x ±100.
        CollisionWorld wall = Single(Body("mesh", meshes: [Square(50)]), At(0, 200, 0, Rotation(90, 1, 0, 0), new Vector3D(2, 1, 1)));
        AssertHit(wall.SweepSphere(new Vector3D(80, 0, 10), new Vector3D(80, 400, 10), 10, Pawn), 190, new Vector3D(80, 200, 10), -Vector3.UnitY);
        AssertHit(wall.SweepSphere(new Vector3D(80, 400, 10), new Vector3D(80, 0, 10), 10, Pawn), 190, new Vector3D(80, 200, 10), Vector3.UnitY);
        Assert.Null(wall.SweepSphere(new Vector3D(115, 0, 10), new Vector3D(115, 400, 10), 10, Pawn));

        // 20 stacked squares (40 triangles, indexed by a hierarchy): square k lies at z = 10k with half size 100 − 4k.
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
        CollisionWorld stack = Single(Body("stack", meshes: [mesh]), At(0, 0, 0));
        for (double x = 1.5; x < 80; x += 7)
        {
            // A sphere of radius 2 rests on the highest square reaching x + 2 (or touches the edge of one just inside).
            CollisionHit hit = Assert.NotNull(stack.SweepSphere(new Vector3D(x, 0.5, 500), new Vector3D(x, 0.5, -100), 2, Pawn));
            double top = 10 * Math.Min(19, Math.Floor((100 - x) / 4));
            Assert.InRange(hit.Point.Z, top - Precision, top + 10 + Precision);
        }
    }

    [Theory]
    [InlineData(HeightFieldCellFlags.None, 30.0, 30.0, 0.0)]
    [InlineData(HeightFieldCellFlags.None, 60.0, 30.0, 0.0)]
    [InlineData(HeightFieldCellFlags.DiagonalFromLowCorner, 60.0, 30.0, 30.0)]
    [InlineData(HeightFieldCellFlags.LowYTriangleHole, 30.0, 25.0, null)]
    [InlineData(HeightFieldCellFlags.HighYTriangleHole, 40.0, 70.0, null)]
    [InlineData(HeightFieldCellFlags.HighYTriangleHole, 30.0, 25.0, 0.0)]
    public void A_small_sphere_lands_on_height_field_triangles_and_falls_through_holes(HeightFieldCellFlags flags, double x, double y, double? surface)
    {
        // One 100 × 100 cell raised only at sample (1, 1). The sphere is small enough to stay inside one triangle.
        MapCollisionBuilder builder = Builder();
        int cell = builder.AddCell("/Game/Cell", []);
        builder.AddHeightField(new HeightFieldTile(cell, new Vector3D(0, 0, 0), 100, 100, 1, 2, 2, [0, 0, 0, 100], [flags]));
        CollisionWorld world = new(builder.Build());

        CollisionHit? hit = world.SweepSphere(new Vector3D(x, y, 500), new Vector3D(x, y, -500), 0.5, Pawn);
        Assert.Equal(surface is null, hit is null);
        if (surface is null)
        {
            return;
        }

        Assert.Equal(CollisionHitKind.HeightField, hit!.Value.Kind);
        Assert.Equal(surface.Value, hit.Value.Point.Z, 0.5);
    }

    [Fact]
    public void A_capsule_rests_on_a_height_field_slope_and_is_pushed_out_along_its_normal()
    {
        // 6 × 6 samples, 100 apart, on the plane z = 0.5 · x.
        short[] heights = new short[36];
        for (int y = 0; y < 6; y++)
        {
            for (int x = 0; x < 6; x++)
            {
                heights[(y * 6) + x] = (short)(50 * x);
            }
        }

        MapCollisionBuilder builder = Builder();
        int cell = builder.AddCell("/Game/Cell", []);
        builder.AddHeightField(new HeightFieldTile(cell, new Vector3D(0, 0, 0), 100, 100, 1, 6, 6, [.. heights], [.. new HeightFieldCellFlags[25]]));
        CollisionWorld world = new(builder.Build());

        // The lower cap's centre rests Radius / cos(slope) above the plane; the capsule's centre 60 above that.
        const double Radius = 30;
        const double HalfHeight = 90;
        Vector3 normal = Vector3.Normalize(new Vector3(-0.5f, 0, 1));
        double rest = (0.5 * 250) + (Radius * Math.Sqrt(1.25)) + (HalfHeight - Radius);
        CollisionHit hit = Assert.NotNull(world.SweepCapsule(new Vector3D(250, 250, 1000), new Vector3D(250, 250, 0), Radius, HalfHeight, Pawn));
        Assert.Equal(1000 - rest, hit.Distance, Precision);
        AssertNormal(normal, hit.Normal);
        Assert.Equal(0.5 * hit.Point.X, hit.Point.Z, Precision);

        Assert.False(world.OverlapCapsule(new Vector3D(250, 250, rest), NumericsQuaternion.Identity, Radius, HalfHeight, Pawn));
        Assert.Null(world.ComputePenetration(new Vector3D(250, 250, rest), NumericsQuaternion.Identity, Radius, HalfHeight, Pawn));
        Assert.True(world.OverlapCapsule(new Vector3D(250, 250, rest - 2), NumericsQuaternion.Identity, Radius, HalfHeight, Pawn));
        Vector3D push = Assert.NotNull(world.ComputePenetration(new Vector3D(250, 250, rest - 2), NumericsQuaternion.Identity, Radius, HalfHeight, Pawn));
        double depth = 2 / Math.Sqrt(1.25);
        AssertVector(new Vector3D(normal.X * depth, 0, normal.Z * depth), push);

        // Sliding down the slope while resting on it touches nothing new.
        Assert.Null(world.SweepCapsule(new Vector3D(250, 250, rest + 0.0005), new Vector3D(150, 250, rest + 0.0005 - 50), Radius, HalfHeight, Pawn));
    }

    [Fact]
    public void A_sweep_that_starts_inside_reports_the_depth_and_the_way_out()
    {
        CollisionWorld world = Single(Body("box", boxes: [Box(new Vector3(50, 50, 50))]), At(0, 0, 0));

        // The sphere's centre is outside: the way out is along the closest points.
        CollisionHit shallow = Assert.NotNull(world.SweepSphere(new Vector3D(55, 0, 0), new Vector3D(155, 0, 0), 10, Pawn));
        Assert.True(shallow.StartPenetrating);
        Assert.Equal(0, shallow.Distance);
        Assert.Equal(5, shallow.PenetrationDepth, 6);
        AssertNormal(Vector3.UnitX, shallow.Normal);
        AssertVector(new Vector3D(45, 0, 0), shallow.Point);

        // The capsule's segment is inside: the shortest way out is through the nearest face, plus the radius.
        CollisionHit deep = Assert.NotNull(world.SweepCapsule(new Vector3D(40, 0, -5), new Vector3D(40, 0, -105), 10, 30, Pawn));
        Assert.True(deep.StartPenetrating);
        Assert.Equal(20, deep.PenetrationDepth, 6);
        AssertNormal(Vector3.UnitX, deep.Normal);

        // Starting exactly in contact is not penetrating: no hit moving away, a hit at 0 moving in.
        Assert.Null(world.SweepSphere(new Vector3D(60, 0, 0), new Vector3D(160, 0, 0), 10, Pawn));
        CollisionHit touch = Assert.NotNull(world.SweepSphere(new Vector3D(60, 0, 0), new Vector3D(0, 0, 0), 10, Pawn));
        Assert.False(touch.StartPenetrating);
        Assert.Equal(0, touch.Distance, Precision);

        Vector3D push = Assert.NotNull(world.ComputePenetration(new Vector3D(40, 0, -5), NumericsQuaternion.Identity, 10, 30, Pawn));
        AssertVector(new Vector3D(20, 0, 0), push);
    }

    [Fact]
    public void A_tilted_capsule_piercing_a_floor_mesh_is_pushed_out_through_the_face()
    {
        CollisionWorld world = Single(Body("floor", meshes: [Square(100)]), At(0, 0, 0));
        NumericsQuaternion tilted = NumericsQuaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI / 4);

        // The segment reaches 40 · cos 45° below its centre at z = 5, then the radius 10.
        double depth = (40 * Math.Sqrt(0.5)) + 10 - 5;
        Vector3D push = Assert.NotNull(world.ComputePenetration(new Vector3D(0, 0, 5), tilted, 10, 50, Pawn));
        AssertVector(new Vector3D(0, 0, depth), push);
        Assert.False(world.OverlapCapsule(new Vector3D(0, 0, 5 + depth), tilted, 10, 50, Pawn));

        CollisionHit hit = Assert.NotNull(world.SweepCapsule(new Vector3D(0, 0, 5), new Vector3D(100, 0, 5), tilted, 10, 50, Pawn));
        Assert.True(hit.StartPenetrating);
        Assert.Equal(depth, hit.PenetrationDepth, Precision);
        AssertNormal(Vector3.UnitZ, hit.Normal);
    }

    [Fact]
    public void Sweeps_parallel_to_a_surface_or_grazing_past_it_do_not_hit()
    {
        CollisionWorld box = Single(Body("box", boxes: [Box(new Vector3(50, 50, 50))]), At(0, 0, 0));
        // Touching a face (within the contact tolerance) while moving along it; passing 0.01 clear of it.
        Assert.Null(box.SweepSphere(new Vector3D(-40, 60, 0), new Vector3D(40, 60, 0), 10, Pawn));
        Assert.Null(box.SweepSphere(new Vector3D(-40, 59.9995, 0), new Vector3D(40, 59.9995, 0), 10, Pawn));
        Assert.Null(box.SweepSphere(new Vector3D(-200, 60.01, 0), new Vector3D(200, 60.01, 0), 10, Pawn));

        // 1 deep, the sphere meets the box's edge.
        CollisionHit edge = Assert.NotNull(box.SweepSphere(new Vector3D(-200, 59, 0), new Vector3D(200, 59, 0), 10, Pawn));
        Assert.Equal(150 - Math.Sqrt(19), edge.Distance, Precision);

        // A sphere of radius 20 passes 25.01 or 24.99 from the centre of a sweep of radius 5.
        CollisionWorld ball = Single(Body("sphere", spheres: [new CollisionSphere(Vector3.Zero, 20)]), At(0, 0, 0));
        Assert.Null(ball.SweepSphere(new Vector3D(-200, 25.01, 0), new Vector3D(200, 25.01, 0), 5, Pawn));
        CollisionHit graze = Assert.NotNull(ball.SweepSphere(new Vector3D(-200, 24.99, 0), new Vector3D(200, 24.99, 0), 5, Pawn));
        Assert.Equal(200 - Math.Sqrt((25 * 25) - (24.99 * 24.99)), graze.Distance, Precision);
    }

    [Fact]
    public void A_capsule_slides_along_a_wall_of_many_triangles_and_stops_when_turning_into_it()
    {
        // A wall in the XZ plane at y = 0 made of a 10 × 10 grid of squares (200 triangles, indexed by a hierarchy).
        List<Vector3> vertices = [];
        List<int> indices = [];
        for (int i = 0; i <= 10; i++)
        {
            for (int k = 0; k <= 10; k++)
            {
                vertices.Add(new Vector3(i * 100, 0, k * 100));
            }
        }

        for (int i = 0; i < 10; i++)
        {
            for (int k = 0; k < 10; k++)
            {
                int corner = (i * 11) + k;
                indices.AddRange([corner, corner + 11, corner + 12, corner, corner + 12, corner + 1]);
            }
        }

        TriangleMesh mesh = new([.. vertices], [.. indices], [], new BoundingBox(Vector3.Zero, new Vector3(1000, 0, 1000)));
        CollisionWorld world = Single(Body("wall", meshes: [mesh]), At(0, 0, 0));

        // Touching the wall, moving along it across many triangle edges.
        Assert.Null(world.SweepCapsule(new Vector3D(50, 34, 300), new Vector3D(950, 34, 750), 34, 88, Pawn));
        Assert.False(world.OverlapCapsule(new Vector3D(500, 34, 500), NumericsQuaternion.Identity, 34, 88, Pawn));

        // Turning into the wall stops at once; turning away is free.
        CollisionHit stop = Assert.NotNull(world.SweepCapsule(new Vector3D(500, 34, 500), new Vector3D(600, 24, 500), 34, 88, Pawn));
        Assert.False(stop.StartPenetrating);
        Assert.Equal(0, stop.Distance, Precision);
        AssertNormal(Vector3.UnitY, stop.Normal);
        Assert.Null(world.SweepCapsule(new Vector3D(500, 34, 500), new Vector3D(600, 44, 500), 34, 88, Pawn));

        // Approaching at an angle meets the wall where the capsule's side reaches it, along the side's whole length.
        CollisionHit side = Assert.NotNull(world.SweepCapsule(new Vector3D(300, 134, 500), new Vector3D(450, -16, 500), 34, 88, Pawn));
        Assert.Equal(Math.Sqrt(2) * 100, side.Distance, Precision);
        Assert.Equal(400, side.Point.X, Precision);
        Assert.Equal(0, side.Point.Y, Precision);
        Assert.InRange(side.Point.Z, 446, 554);
        AssertNormal(Vector3.UnitY, side.Normal);
    }

    [Fact]
    public void A_capsule_meets_a_step_edge_with_a_rounded_normal()
    {
        // A floor whose top is z = 0 and a step from x = 100 whose top is z = 30.
        MapCollisionBuilder builder = Builder();
        int cell = builder.AddCell("/Game/Cell", []);
        builder.AddPlacement(builder.AddBody(Body("floor", boxes: [Box(new Vector3(1000, 1000, 50))])), cell, At(0, 0, -50), Pawn);
        builder.AddPlacement(builder.AddBody(Body("step", boxes: [Box(new Vector3(100, 1000, 15))])), cell, At(200, 0, 15), Pawn);
        CollisionWorld world = new(builder.Build());

        // Standing on the floor, the lower cap's centre is 34 high, 4 above the step's top edge.
        CollisionHit hit = Assert.NotNull(world.SweepCapsule(new Vector3D(0, 0, 88), new Vector3D(100, 0, 88), 34, 88, Pawn));
        double reach = Math.Sqrt((34 * 34) - (4 * 4));
        Assert.Equal(100 - reach, hit.Distance, Precision);
        AssertVector(new Vector3D(100, 0, 30), hit.Point);
        AssertNormal(Vector3.Normalize(new Vector3((float)-reach, 0, 4)), hit.Normal);
        Assert.False(world.OverlapCapsule(new Vector3D(0, 0, 88), NumericsQuaternion.Identity, 34, 88, Pawn));

        // Lifted by the step height, the capsule clears the edge and lands on the step.
        Assert.Null(world.SweepCapsule(new Vector3D(0, 0, 118.01), new Vector3D(150, 0, 118.01), 34, 88, Pawn));
        AssertHit(world.SweepCapsule(new Vector3D(150, 0, 118.01), new Vector3D(150, 0, 0), 34, 88, Pawn), 0.01, new Vector3D(150, 0, 30), Vector3.UnitZ);
    }

    [Fact]
    public void Penetration_resolves_a_floor_and_a_wall_together()
    {
        MapCollisionBuilder builder = Builder();
        int cell = builder.AddCell("/Game/Cell", []);
        builder.AddPlacement(builder.AddBody(Body("floor", boxes: [Box(new Vector3(1000, 1000, 50))])), cell, At(0, 0, -50), Pawn);
        builder.AddPlacement(builder.AddBody(Body("wall", boxes: [Box(new Vector3(50, 1000, 500))])), cell, At(150, 0, 500), Climbable);
        CollisionWorld world = new(builder.Build());
        Vector3D sunk = new(70, 0, 85);

        Vector3D push = Assert.NotNull(world.ComputePenetration(sunk, NumericsQuaternion.Identity, 34, 88, Pawn | Climbable));
        AssertVector(new Vector3D(-4, 0, 3), push);
        Vector3D clear = new(sunk.X + push.X, sunk.Y + push.Y, sunk.Z + push.Z);
        Assert.False(world.OverlapCapsule(clear, NumericsQuaternion.Identity, 34, 88, Pawn | Climbable));

        // The wall blocks only climbing traces.
        AssertVector(new Vector3D(0, 0, 3), Assert.NotNull(world.ComputePenetration(sunk, NumericsQuaternion.Identity, 34, 88, Pawn)));
        Assert.True(world.OverlapCapsule(new Vector3D(70, 0, 200), NumericsQuaternion.Identity, 34, 88, Climbable));
        Assert.False(world.OverlapCapsule(new Vector3D(70, 0, 200), NumericsQuaternion.Identity, 34, 88, Pawn));
        Assert.Null(world.SweepCapsule(new Vector3D(0, 0, 200), new Vector3D(500, 0, 200), 34, 88, Pawn));
    }

    [Fact]
    public void A_vanishing_radius_sweep_matches_the_raycast()
    {
        // Every shape kind under turns and non-uniform or mirrored scale, above a height field.
        MapCollisionBuilder builder = Builder();
        int cell = builder.AddCell("/Game/Cell", []);
        CollisionBody mixed = Body(
            "mixed",
            boxes: [new CollisionBox(new Vector3(0, 0, 50), ToNumerics(Rotation(30, 0, 0, 1)), new Vector3(40, 20, 50))],
            spheres: [new CollisionSphere(new Vector3(120, 0, 40), 40)],
            capsules: [new CollisionCapsule(new Vector3(-120, 0, 60), ToNumerics(Rotation(60, 1, 0, 0)), 25, 40)],
            hulls: [BoxHull(new Vector3(0, 80, 0), new Vector3(60, 140, 90))],
            meshes: [Square(60)]);
        int body = builder.AddBody(mixed);
        Random layout = new(3);
        for (int index = 0; index < 12; index++)
        {
            Vector3D scale = new(0.5 + layout.NextDouble(), (layout.Next(2) == 0 ? -1 : 1) * (0.5 + layout.NextDouble()), 0.5 + layout.NextDouble());
            int row = index / 4;
            builder.AddPlacement(body, cell, At(index % 4 * 400, row * 400, 100 + (layout.NextDouble() * 50), Rotation(layout.NextDouble() * 360, 0, 0, 1), scale), Pawn);
        }

        short[] heights = new short[100];
        for (int index = 0; index < heights.Length; index++)
        {
            heights[index] = (short)layout.Next(0, 80);
        }

        HeightFieldCellFlags[] flags = new HeightFieldCellFlags[81];
        for (int index = 0; index < flags.Length; index++)
        {
            flags[index] = (HeightFieldCellFlags)layout.Next(0, 8);
        }

        builder.AddHeightField(new HeightFieldTile(cell, new Vector3D(-300, -300, 0), 200, 200, 1, 10, 10, [.. heights], [.. flags]));
        CollisionWorld world = new(builder.Build());

        Random rays = new(11);
        int hits = 0;
        for (int ray = 0; ray < 3000; ray++)
        {
            Vector3D origin = new((rays.NextDouble() * 1800) - 300, (rays.NextDouble() * 1500) - 300, 600);
            Vector3 direction = Vector3.Normalize(new Vector3((float)(rays.NextDouble() - 0.5), (float)(rays.NextDouble() - 0.5), -1));
            CollisionHit? expected = world.Raycast(origin, direction, 1000, Pawn);
            Vector3D end = new(origin.X + (direction.X * 1000.0), origin.Y + (direction.Y * 1000.0), origin.Z + (direction.Z * 1000.0));
            CollisionHit? actual = world.SweepCapsule(origin, end, 0, 0, Pawn);
            Assert.Equal(expected is null, actual is null);
            if (expected is null)
            {
                continue;
            }

            hits++;
            Assert.Equal((expected.Value.Kind, expected.Value.Index), (actual!.Value.Kind, actual.Value.Index));
            Assert.Equal(expected.Value.Distance, actual.Value.Distance, Precision);
            Assert.True(Vector3.Dot(expected.Value.Normal, actual.Value.Normal) > 0.99f, $"{expected} {actual}");
        }

        Assert.True(hits > 1000);
    }

    private static NumericsQuaternion ToNumerics(Structs.Core.Geometry.Quaternion rotation) =>
        new((float)rotation.X, (float)rotation.Y, (float)rotation.Z, (float)rotation.W);

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
        Assert.False(hit.StartPenetrating);
        Assert.Equal(distance, hit.Distance, Precision);
        AssertVector(point, hit.Point);
        AssertNormal(normal, hit.Normal);
    }

    private static void AssertVector(Vector3D expected, Vector3D actual)
    {
        Assert.Equal(expected.X, actual.X, Precision);
        Assert.Equal(expected.Y, actual.Y, Precision);
        Assert.Equal(expected.Z, actual.Z, Precision);
    }

    private static void AssertNormal(Vector3 expected, Vector3 actual)
    {
        Assert.Equal(expected.X, actual.X, 1e-3);
        Assert.Equal(expected.Y, actual.Y, 1e-3);
        Assert.Equal(expected.Z, actual.Z, 1e-3);
    }
}
