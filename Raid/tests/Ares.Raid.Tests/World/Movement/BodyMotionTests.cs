using System.Numerics;
using Ares.Common.Maps;
using Ares.Raid.Tests.World.Collision;
using Ares.Raid.World.Collision;
using Ares.Raid.World.Movement;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Game.Collision;
using Ares.Structs.Game.Movement;

namespace Ares.Raid.Tests.World.Movement;

public sealed class BodyMotionTests
{
    private const double Tick = PawnBlackboard.TickSeconds;

    [Fact]
    public void A_walk_on_level_ground_keeps_its_height_and_covers_its_speed()
    {
        CollisionWorld world = World();
        BodyState body = BodyMotion.Start(world, new Vector3D(-1000, 0, 0));
        Assert.False(body.Airborne);

        for (uint frame = 1; frame <= 30; frame++)
        {
            body = BodyMotion.Move(world, BodyMotion.Begin(body, frame, false), new Vector3D(300, 0, 0), Tick);
        }

        Assert.False(body.Airborne);
        Assert.Equal(-700, body.Position.X, 6);
        Assert.Equal(0, body.Position.Z, 6);
        Assert.Equal((300, 0), (body.Velocity.X, body.Velocity.Y));
        Assert.Equal(0, body.Velocity.Z, 9);
        Assert.True(body.Ground.Carries);
    }

    [Fact]
    public void A_jump_takes_off_after_its_delay_rises_with_the_jump_speed_and_lands()
    {
        CollisionWorld world = World();
        BodyState body = BodyMotion.Start(world, new Vector3D(-1000, 0, 0));
        uint frame = 10;
        body = BodyMotion.Move(world, BodyMotion.Begin(body, frame, true), default, Tick);
        Assert.Equal(frame, body.JumpRequestedAt);

        while (!body.Airborne)
        {
            frame++;
            body = BodyMotion.Move(world, BodyMotion.Begin(body, frame, false), default, Tick);
        }

        Assert.Equal(10u + (uint)AirMovementParameters.JumpDelayTicks, frame);
        Assert.True(body.Jumping);
        Assert.Null(body.JumpRequestedAt);
        const double First = AirMovementParameters.JumpSpeed - (AirMovementParameters.FallAcceleration * Tick);
        Assert.Equal(First, body.Velocity.Z, 6);
        Assert.Equal(First * Tick, body.Position.Z, 6);

        double highest = 0;
        int airTicks = 1;
        while (body.Airborne)
        {
            frame++;
            airTicks++;
            body = BodyMotion.Move(world, BodyMotion.Begin(body, frame, false), default, Tick);
            highest = Math.Max(highest, body.Position.Z);
        }

        // The landing tick is on the ground: the feet passed under it and the landing spring holds them.
        // Semi-implicit steps (speed first) stay below the continuous apex by at most half a tick of the jump speed.
        const double Apex = AirMovementParameters.JumpSpeed * AirMovementParameters.JumpSpeed / (2 * AirMovementParameters.FallAcceleration);
        Assert.InRange(highest, Apex - (AirMovementParameters.JumpSpeed * Tick / 2), Apex);
        Assert.InRange(airTicks, 19, 22);
        Assert.True(body.Landing);
        Assert.False(body.Jumping);
        Assert.InRange(body.Position.Z, -20, 0);

        for (int settle = 0; settle < 30; settle++)
        {
            frame++;
            body = BodyMotion.Move(world, BodyMotion.Begin(body, frame, false), default, Tick);
        }

        // A character that stands still after its landing keeps the height the landing left it at.
        Assert.False(body.Landing);
        Assert.InRange(body.Position.Z, -GroundContactParameters.SettledDistance, GroundContactParameters.SettledDistance);
        Assert.Equal(0, body.Velocity.Z);
    }

    [Fact]
    public void Moving_feet_settle_on_the_ground_and_standing_feet_keep_their_height()
    {
        CollisionWorld world = World();
        BodyState body = BodyMotion.Start(world, new Vector3D(-1000, 0, 1));

        // Standing a little above the ground changes nothing.
        body = BodyMotion.Move(world, BodyMotion.Begin(body, 1, false), default, Tick);
        Assert.Equal(1, body.Position.Z);
        Assert.Equal(0, body.Velocity.Z);

        // One walking tick takes the share of the height that the smoothest path to rest within the settle time covers.
        const double Share = Tick / GroundContactParameters.SettleSeconds;
        const double Pull = 30 * Share * Share * (1 - Share) * (1 - Share) / GroundContactParameters.SettleSeconds;
        body = BodyMotion.Move(world, BodyMotion.Begin(body, 2, false), new Vector3D(30, 0, 0), Tick);
        Assert.Equal(-Pull, body.Velocity.Z, 9);
        Assert.Equal(1 - (Pull * Tick), body.Position.Z, 9);

        for (uint frame = 3; frame < 60; frame++)
        {
            body = BodyMotion.Move(world, BodyMotion.Begin(body, frame, false), new Vector3D(30, 0, 0), Tick);
        }

        Assert.Equal(0, body.Position.Z, 4);
        Assert.InRange(body.Velocity.Z, -0.001, 0);
    }

    [Fact]
    public void A_jump_press_in_the_air_does_not_jump()
    {
        CollisionWorld world = World();
        BodyState body = BodyMotion.Start(world, new Vector3D(-1000, 0, 500));
        Assert.True(body.Airborne);

        body = BodyMotion.Begin(body, 5, true);

        Assert.Null(body.JumpRequestedAt);
        Assert.False(body.Jumping);
    }

    [Fact]
    public void Walking_off_a_ledge_falls_once_the_pill_has_left_it_and_lands_below()
    {
        CollisionWorld world = World();
        BodyState body = BodyMotion.Start(world, new Vector3D(-100, 0, 0));
        uint frame = 0;
        double left = double.NaN;
        while (body.Position.X < 400 && frame < 200)
        {
            frame++;
            body = BodyMotion.Move(world, BodyMotion.Begin(body, frame, false), new Vector3D(300, 0, 0), Tick);
            if (body.Airborne && double.IsNaN(left))
            {
                left = body.Position.X;
            }
        }

        // The upper floor ends at x = 0; the lower one is 300 below.
        Assert.InRange(left, GroundContactParameters.PillRadius, GroundContactParameters.PillRadius + 25);
        Assert.False(body.Airborne);
        Assert.Equal(-300, body.Position.Z, 0);
    }

    [Fact]
    public void A_wall_stops_the_walk_at_the_pill_radius_and_turns_it_along_the_wall()
    {
        CollisionWorld world = World();
        BodyState body = BodyMotion.Start(world, new Vector3D(-1000, 0, 0));
        for (uint frame = 1; frame <= 60; frame++)
        {
            body = BodyMotion.Move(world, BodyMotion.Begin(body, frame, false), new Vector3D(0, 300, 0), Tick);
        }

        // The wall's face is at y = 200.
        Assert.InRange(body.Position.Y, 200 - GroundContactParameters.PillRadius - 0.5, 200 - GroundContactParameters.PillRadius + 0.01);
        Assert.Equal(0, body.Velocity.Y, 3);

        double along = body.Position.X;
        for (uint frame = 61; frame <= 90; frame++)
        {
            body = BodyMotion.Move(world, BodyMotion.Begin(body, frame, false), new Vector3D(200, 200, 0), Tick);
        }

        Assert.Equal(along + 200, body.Position.X, 0);
        Assert.InRange(body.Position.Y, 200 - GroundContactParameters.PillRadius - 0.5, 200 - GroundContactParameters.PillRadius + 0.01);
        Assert.Equal(0, body.Position.Z, 6);
    }

    [Fact]
    public void A_body_pushed_out_of_a_wall_moves_no_faster_than_it_was_asked_to()
    {
        CollisionWorld world = World();
        // Placed with its pill a few units into the wall, whose face is at y = 200.
        BodyState body = BodyMotion.Start(world, new Vector3D(-1000, 200 - GroundContactParameters.PillRadius + 5, 0));
        BodyState moved = BodyMotion.Move(world, BodyMotion.Begin(body, 1, false), new Vector3D(2, 0, 0), Tick);

        Assert.True(moved.Position.Y < body.Position.Y);
        Assert.InRange(Math.Sqrt((moved.Velocity.X * moved.Velocity.X) + (moved.Velocity.Y * moved.Velocity.Y)), 0, 2 + 1e-9);
    }

    [Fact]
    public void Ground_lower_than_the_knees_is_stepped_onto_and_does_not_block()
    {
        CollisionWorld world = World();
        BodyState body = BodyMotion.Start(world, new Vector3D(-1000, -400, 0));
        for (uint frame = 1; frame <= 40; frame++)
        {
            body = BodyMotion.Move(world, BodyMotion.Begin(body, frame, false), new Vector3D(0, -300, 0), Tick);
        }

        // A platform 20 high starts at y = -500.
        Assert.Equal(-800, body.Position.Y, 6);
        Assert.False(body.Airborne);
        Assert.Equal(20, body.Position.Z, 1);
    }

    [Theory]
    [InlineData(20, true)]
    [InlineData(60, false)]
    public void A_slope_carries_the_character_up_to_the_walkable_angle(double degrees, bool walkable)
    {
        MapCollisionBuilder builder = SyntheticMaps.Builder();
        int cell = builder.AddCell("/Game/Cell", []);
        builder.AddPlacement(builder.AddBody(SyntheticMaps.Body("slope", boxes: [SyntheticMaps.Box(new Vector3(2000, 2000, 10))])), cell,
            SyntheticMaps.At(0, 0, 0, SyntheticMaps.Rotation(degrees, 0, 1, 0)), PlacementFlags.BlocksPawn);
        CollisionWorld world = new(builder.Build());
        double height = world.GroundHeight(0, 0, 500, 1000, PlacementFlags.BlocksPawn)!.Value;

        GroundContact ground = BodyMotion.Probe(world, new Vector3D(0, 0, height));

        Assert.True(ground.Supported);
        Assert.Equal(walkable, ground.Walkable);
        Assert.Equal(height, ground.Height, 0.01);
        // On steep ground some probes end outside the reach, so the fitted plane is only roughly the slope.
        Assert.Equal(Math.Cos(degrees * Math.PI / 180), ground.Normal.Z, walkable ? 0.001 : 0.05);
        Assert.Equal(!walkable, BodyMotion.Start(world, new Vector3D(0, 0, height)).Airborne);
    }

    [Fact]
    public void The_ground_takes_the_surface_type_under_the_axis_else_under_the_first_probe_that_reaches_ground()
    {
        MapCollisionBuilder builder = SyntheticMaps.Builder();
        int cell = builder.AddCell("/Game/Cell", []);
        builder.AddPlacement(builder.AddBody(SyntheticMaps.Body("floor", boxes: [SyntheticMaps.Box(new Vector3(500, 500, 10))])), cell,
            SyntheticMaps.At(0, 0, -10), PlacementFlags.BlocksPawn, PhysicalSurfaceType.Concrete);
        builder.AddPlacement(builder.AddBody(SyntheticMaps.Body("mat", boxes: [SyntheticMaps.Box(new Vector3(10, 10, 1))])), cell,
            SyntheticMaps.At(0, 0, -0.5), PlacementFlags.BlocksPawn, PhysicalSurfaceType.Fabric);
        CollisionWorld world = new(builder.Build());

        Assert.Equal(PhysicalSurfaceType.Fabric, BodyMotion.Probe(world, new Vector3D(0, 0, 0)).Surface);
        Assert.Equal(PhysicalSurfaceType.Concrete, BodyMotion.Probe(world, new Vector3D(200, 0, 0)).Surface);
        Assert.Equal(PhysicalSurfaceType.Concrete, BodyMotion.Probe(world, new Vector3D(510, 0, 0)).Surface);
        Assert.Equal(PhysicalSurfaceType.Default, BodyMotion.Probe(world, new Vector3D(2000, 0, 0)).Surface);
    }

    [Fact]
    public void Under_a_low_ceiling_a_crouched_character_neither_stands_up_nor_jumps()
    {
        // A floor at height 0 and, over x below 0, a ceiling 130 above it: room to crouch, not to stand.
        MapCollisionBuilder builder = SyntheticMaps.Builder();
        int cell = builder.AddCell("/Game/Cell", []);
        builder.AddPlacement(builder.AddBody(SyntheticMaps.Body("floor", boxes: [SyntheticMaps.Box(new Vector3(4000, 4000, 10))])), cell,
            SyntheticMaps.At(0, 0, -10), PlacementFlags.BlocksPawn);
        builder.AddPlacement(builder.AddBody(SyntheticMaps.Body("ceiling", boxes: [SyntheticMaps.Box(new Vector3(1000, 1000, 10))])), cell,
            SyntheticMaps.At(-1000, 0, 140), PlacementFlags.BlocksPawn);
        CollisionWorld world = new(builder.Build());

        Assert.False(BodyMotion.CanStand(world, new Vector3D(-500, 0, 0)));
        Assert.True(BodyMotion.CanStand(world, new Vector3D(500, 0, 0)));

        CharacterSimulation simulation = new(new Vector3D(-500, 0, 0), world: world);
        for (uint frame = 1; frame <= 5; frame++)
        {
            Step(simulation, frame, MovementButtons.Crouch | (frame > 1 ? MovementButtons.PreviousCrouch : MovementButtons.None));
        }

        CharacterState released = Step(simulation, 6, MovementButtons.PreviousCrouch);
        for (uint frame = 7; frame <= 20; frame++)
        {
            Step(simulation, frame, frame == 7 ? MovementButtons.Jump : MovementButtons.PreviousJump);
        }

        Assert.Equal(CharacterStance.Crouch, released.MovementStance);
        Assert.Equal(CharacterStance.Crouch, simulation.State.MovementStance);
        Assert.False(simulation.State.Body!.Value.Airborne);
        Assert.Null(simulation.State.Body.Value.JumpRequestedAt);
    }

    [Fact]
    public void A_requested_vault_over_a_low_wall_runs_its_path_and_hands_back_to_movement_in_the_air()
    {
        // A floor at height 0 and a wall 115 high and 20 thick across the way, 90 ahead of the character.
        MapCollisionBuilder builder = SyntheticMaps.Builder();
        int cell = builder.AddCell("/Game/Cell", []);
        builder.AddPlacement(builder.AddBody(SyntheticMaps.Body("floor", boxes: [SyntheticMaps.Box(new Vector3(4000, 4000, 10))])), cell,
            SyntheticMaps.At(0, 0, -10), PlacementFlags.BlocksPawn | PlacementFlags.BlocksClimbable);
        builder.AddPlacement(builder.AddBody(SyntheticMaps.Body("wall", boxes: [SyntheticMaps.Box(new Vector3(10, 500, 57.5f))])), cell,
            SyntheticMaps.At(100, 0, 57.5), PlacementFlags.BlocksPawn | PlacementFlags.BlocksClimbable);
        CollisionWorld world = new(builder.Build());
        CharacterSimulation simulation = new(new Vector3D(-100, 0, 0), world: world);
        uint frame = 1;
        for (; frame <= 10; frame++)
        {
            Walk(simulation, frame, 0);
        }

        CharacterState found = Walk(simulation, frame++, 1UL << VaultParameters.RequestBit);
        Assert.NotNull(found.Vault);
        Assert.False(found.VaultMissed);
        Assert.Equal(VaultKind.Over, found.Vault!.Value.Kind);
        Assert.Equal(VaultParameters.ActionStance, found.Stance);
        Assert.Equal(VaultParameters.VaultingFlags, found.GraphFlags & VaultParameters.VaultingFlags);
        Assert.True(found.Body!.Value.Airborne);

        int duration = VaultMovement.Duration(found.Vault.Value);
        for (int tick = 1; tick < duration; tick++)
        {
            Assert.True(Walk(simulation, frame++, 0).Vaulting);
        }

        Assert.True(simulation.State.VaultFinished);
        CharacterState after = Walk(simulation, frame, 0);
        Assert.False(after.Vaulting);
        Assert.Equal(0UL, after.GraphFlags & VaultParameters.VaultingFlags);
        Assert.True(after.Body!.Value.Position.X > 110);
    }

    [Fact]
    public void A_vault_request_with_nothing_to_vault_is_reported_as_missed()
    {
        CharacterSimulation simulation = new(new Vector3D(-1000, 0, 0), world: World());
        CharacterState state = Walk(simulation, 1, 1UL << VaultParameters.RequestBit);

        Assert.True(state.VaultMissed);
        Assert.False(state.Vaulting);
    }

    [Fact]
    public void A_probe_finds_no_ground_beyond_its_reach()
    {
        CollisionWorld world = World();

        Assert.True(BodyMotion.Probe(world, new Vector3D(-1000, 0, GroundContactParameters.GroundReach - 1)).Supported);
        Assert.False(BodyMotion.Probe(world, new Vector3D(-1000, 0, GroundContactParameters.GroundReach + 1)).Supported);
    }

    // An upper floor at height 0 for x below 0, a lower floor 300 below it everywhere, a tall wall whose face is
    // at y = 200 and a platform 20 high beyond y = -500, both over the upper floor.
    private static CollisionWorld World()
    {
        MapCollisionBuilder builder = SyntheticMaps.Builder();
        int cell = builder.AddCell("/Game/Cell", []);
        builder.AddPlacement(builder.AddBody(SyntheticMaps.Body("upper", boxes: [SyntheticMaps.Box(new Vector3(2000, 2000, 10))])), cell,
            SyntheticMaps.At(-2000, 0, -10), PlacementFlags.BlocksPawn);
        builder.AddPlacement(builder.AddBody(SyntheticMaps.Body("lower", boxes: [SyntheticMaps.Box(new Vector3(4000, 4000, 10))])), cell,
            SyntheticMaps.At(0, 0, -310), PlacementFlags.BlocksPawn);
        builder.AddPlacement(builder.AddBody(SyntheticMaps.Body("wall", boxes: [SyntheticMaps.Box(new Vector3(1500, 50, 200))])), cell,
            SyntheticMaps.At(-2000, 250, 200), PlacementFlags.BlocksPawn);
        builder.AddPlacement(builder.AddBody(SyntheticMaps.Body("platform", boxes: [SyntheticMaps.Box(new Vector3(1500, 500, 10))])), cell,
            SyntheticMaps.At(-2000, -1000, 10), PlacementFlags.BlocksPawn);
        return new CollisionWorld(builder.Build());
    }

    private static CharacterState Walk(CharacterSimulation simulation, uint frame, ulong claims) =>
        simulation.StepOnGround(new CharacterInput(frame, claims), new MovementInput(frame, 126, 0, MovementButtons.None), 0, PawnBlackboard.TickSeconds,
            GroundMovementParameters.Unarmed, steerAboveMaximum: true, fixBrakingOvershoot: true, vaults: VaultParameters.Definitions(armed: false, exhausted: false));

    private static CharacterState Step(CharacterSimulation simulation, uint frame, MovementButtons buttons) =>
        simulation.StepOnGround(new CharacterInput(frame, 0), new MovementInput(frame, 0, 0, buttons), 0, PawnBlackboard.TickSeconds,
            GroundMovementParameters.Unarmed, steerAboveMaximum: true, fixBrakingOvershoot: true);
}
