using System.Numerics;
using Ares.Common.Maps;
using Ares.Raid.World.Collision;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Game.Collision;
using static Ares.Raid.Tests.World.Collision.SyntheticMaps;
using Quaternion = System.Numerics.Quaternion;

namespace Ares.Raid.Tests.World.Collision;

public sealed class ComplexCollisionTests
{
    private const double Precision = 1e-4;
    private const CollisionChannel Bullets = GameCollisionChannels.PlayerTraceProjectile;

    private static readonly PlacementFlags s_solid = PlacementFlags.BlocksPawn | CollisionChannelMask.Of(Bullets);

    // Simple collision: a box whose top is at z = −90. Complex collision: a square at z = 0 above it.
    private static readonly CollisionBody s_both = Body("both", boxes: [new CollisionBox(new Vector3(0, 0, -100), Quaternion.Identity, new Vector3(10, 10, 10))]) with
    {
        ComplexMeshes = [Square(100) with { MaterialIndices = [1, 1] }],
    };

    [Fact]
    public void Simple_queries_hit_the_simple_shapes_and_complex_queries_the_complex_meshes()
    {
        CollisionWorld world = Single(s_both, s_solid);

        Assert.Equal(-90, Down(world, 0, 0, CollisionComplexity.Simple)!.Value.Point.Z, Precision);
        CollisionHit complex = Assert.NotNull(Down(world, 0, 0, CollisionComplexity.Complex));
        Assert.Equal(0, complex.Point.Z, Precision);
        Assert.Equal(PhysicalSurfaceType.Metal, complex.Surface);
        Assert.Equal(-90, world.Raycast(new Vector3D(0, 0, 500), -Vector3.UnitZ, 1000, PlacementFlags.BlocksPawn)!.Value.Point.Z, Precision);
        Assert.Null(Down(world, 50, 50, CollisionComplexity.Simple));
        Assert.NotNull(Down(world, 50, 50, CollisionComplexity.Complex));
    }

    [Fact]
    public void A_simple_and_complex_body_without_complex_meshes_is_invisible_to_complex_queries()
    {
        CollisionWorld world = Single(s_both with { ComplexMeshes = [] }, s_solid);

        Assert.NotNull(Down(world, 0, 0, CollisionComplexity.Simple));
        Assert.Null(Down(world, 0, 0, CollisionComplexity.Complex));
        Assert.Null(world.SweepSphere(new Vector3D(0, 0, 500), new Vector3D(0, 0, -500), 5, Bullets, CollisionComplexity.Complex));
    }

    [Theory]
    [InlineData(CollisionTraceFlag.SimpleAsComplex)]
    [InlineData(CollisionTraceFlag.ComplexAsSimple)]
    public void Bodies_that_use_one_collision_for_both_answer_complex_queries_with_it(CollisionTraceFlag traceFlag)
    {
        CollisionBody body = traceFlag == CollisionTraceFlag.ComplexAsSimple
            ? Body("mesh", meshes: [Square(100)]) with { TraceFlag = traceFlag }
            : Body("box", boxes: [Box(new Vector3(10, 10, 10))]) with { TraceFlag = traceFlag };
        CollisionWorld world = Single(body, s_solid);

        double expected = traceFlag == CollisionTraceFlag.ComplexAsSimple ? 0 : 10;
        Assert.Equal(expected, Down(world, 0, 0, CollisionComplexity.Simple)!.Value.Point.Z, Precision);
        Assert.Equal(expected, Down(world, 0, 0, CollisionComplexity.Complex)!.Value.Point.Z, Precision);
    }

    [Fact]
    public void Queries_hit_only_placements_that_block_their_channel()
    {
        MapCollisionBuilder builder = Builder();
        int cell = builder.AddCell("/Game/Cell", []);
        int body = builder.AddBody(s_both);
        builder.AddPlacement(body, cell, At(0, 0, 0), CollisionChannelMask.Of(Bullets));
        builder.AddPlacement(body, cell, At(1000, 0, 0), PlacementFlags.BlocksPawn);
        CollisionWorld world = new(builder.Build());

        Assert.Equal(2, world.PlacementCount);
        Assert.NotNull(Down(world, 0, 0, CollisionComplexity.Complex));
        Assert.Null(world.Raycast(new Vector3D(0, 0, 500), -Vector3.UnitZ, 1000, PlacementFlags.BlocksPawn, CollisionComplexity.Complex));
        Assert.Null(world.Raycast(new Vector3D(0, 0, 500), -Vector3.UnitZ, 1000, GameCollisionChannels.AiTraceProjectile, CollisionComplexity.Complex));
        Assert.Null(Down(world, 1000, 0, CollisionComplexity.Complex));
        Assert.NotNull(world.Raycast(new Vector3D(1000, 0, 500), -Vector3.UnitZ, 1000, GameCollisionChannels.PlayerPawn, CollisionComplexity.Complex));
    }

    [Fact]
    public void Height_fields_answer_complex_queries()
    {
        MapCollisionBuilder builder = Builder();
        int cell = builder.AddCell("/Game/Cell", []);
        builder.AddHeightField(FlatTile(cell, new Vector3D(-500, -500, -300), 100, 1, 11, 0));
        CollisionWorld world = new(builder.Build());

        Assert.Equal(-300, Down(world, 0, 0, CollisionComplexity.Complex)!.Value.Point.Z, Precision);
    }

    [Fact]
    public void Sphere_sweeps_take_the_complexity_and_capsule_sweeps_stay_simple()
    {
        CollisionWorld world = Single(s_both, s_solid);
        Vector3D start = new(0, 0, 500);
        Vector3D end = new(0, 0, -500);

        Assert.Equal(-90, world.SweepSphere(start, end, 5, Bullets, CollisionComplexity.Simple)!.Value.Point.Z, Precision);
        Assert.Equal(0, world.SweepSphere(start, end, 5, Bullets, CollisionComplexity.Complex)!.Value.Point.Z, Precision);
        Assert.Equal(-90, world.SweepCapsule(start, end, 5, 20, PlacementFlags.BlocksPawn)!.Value.Point.Z, Precision);
        Assert.False(world.OverlapCapsule(default, Quaternion.Identity, 5, 20, PlacementFlags.BlocksPawn));
    }

    private static CollisionHit? Down(CollisionWorld world, double x, double y, CollisionComplexity complexity) =>
        world.Raycast(new Vector3D(x, y, 500), -Vector3.UnitZ, 1000, Bullets, complexity);

    private static CollisionWorld Single(CollisionBody body, PlacementFlags flags)
    {
        MapCollisionBuilder builder = Builder();
        int cell = builder.AddCell("/Game/Cell", []);
        builder.AddPlacement(builder.AddBody(body), cell, At(0, 0, 0), flags, PhysicalSurfaceType.Concrete,
            [PhysicalSurfaceType.Wood, PhysicalSurfaceType.Metal]);
        return new CollisionWorld(builder.Build());
    }
}
