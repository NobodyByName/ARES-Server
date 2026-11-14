using System.Numerics;
using Ares.Common.Maps;
using Ares.Raid.Tests.World.Collision;
using Ares.Raid.World.Collision;
using Ares.Raid.World.Movement;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Game.Movement;
using Ares.Structs.Game.World;

namespace Ares.Raid.Tests.World.Movement;

public sealed class VaultMovementTests
{
    private const double Tick = 1.0 / 30;
    private const PlacementFlags Solid = PlacementFlags.BlocksPawn | PlacementFlags.BlocksClimbable;
    private static readonly MovementInput s_press = new(10, 126, 0, MovementButtons.Jump);
    private static readonly VaultCapsule s_capsule = new(DamPlayerStarts.Capsule.Radius, DamPlayerStarts.Capsule.HalfHeight,
        DamPlayerStarts.Capsule.HalfHeight, 40);

    [Fact]
    public void A_vault_is_looked_for_on_a_jump_press_with_input_or_on_request()
    {
        CharacterInput none = new(10, 0);
        Assert.True(VaultDetection.ShouldAttempt(s_press, none, 10, null, null, 0));
        Assert.False(VaultDetection.ShouldAttempt(s_press with { Forward = 0 }, none, 10, null, null, 0));
        Assert.False(VaultDetection.ShouldAttempt(s_press, new CharacterInput(10, 1UL << VaultParameters.ImmobilizedBit), 10, null, null, 0));
        Assert.False(VaultDetection.ShouldAttempt(s_press with { Buttons = MovementButtons.Jump | MovementButtons.PreviousJump }, none, 10, 5, null, 0));
        Assert.True(VaultDetection.Requested(new CharacterInput(10, 1UL << VaultParameters.RequestBit)));
        Assert.False(VaultDetection.Requested(none));
    }

    [Fact]
    public void High_above_the_floor_a_held_or_recent_jump_keeps_looking_unless_it_just_took_off()
    {
        MovementInput held = s_press with { Buttons = MovementButtons.Jump | MovementButtons.PreviousJump };
        MovementInput released = s_press with { Buttons = MovementButtons.None };
        CharacterInput none = new(40, 0);
        Assert.True(VaultDetection.ShouldAttempt(held, none, 40, null, null, 60));
        Assert.False(VaultDetection.ShouldAttempt(held, none, 40, null, null, 50));
        Assert.False(VaultDetection.ShouldAttempt(held, none, 40, null, 30, 60));
        Assert.True(VaultDetection.ShouldAttempt(held, none, 40, null, 20, 60));
        Assert.True(VaultDetection.ShouldAttempt(released, none, 40, 25, null, 60));
        Assert.False(VaultDetection.ShouldAttempt(released, none, 40, 24, null, 60));
    }

    [Fact]
    public void The_obstacle_height_limits_follow_speed_and_falling()
    {
        Assert.Equal(30, VaultDetection.MinimumHeight(0));
        Assert.Equal(40, VaultDetection.MinimumHeight(150));
        Assert.Equal(50, VaultDetection.MinimumHeight(900));
        Assert.Equal(350, VaultDetection.MaximumHeight(falling: false, 0));
        Assert.Equal(250, VaultDetection.MaximumHeight(falling: true, -100));
        Assert.Equal(150, VaultDetection.MaximumHeight(falling: true, -101));
    }

    [Theory]
    [InlineData(VaultKind.Over, 115, 160, "HM_Vault_A_UA_Over100cmFast_00")]
    [InlineData(VaultKind.Over, 115, 140, "HM_Vault_A_UA_Over100cmSlow_00")]
    [InlineData(VaultKind.Onto, 115, 0, "HM_Vault_A_UA_Onto100cmSlow_00")]
    [InlineData(VaultKind.Onto, 260, 400, "HM_Vault_A_UA_Onto200cm_00")]
    [InlineData(VaultKind.Over, 60, 0, "HM_Vault_A_UA_AutoStepWalkFwd_00")]
    public void The_clip_is_the_highest_row_not_above_the_obstacle_at_the_nearest_speed(VaultKind kind, double height, double speed, string expected)
    {
        VaultClip? clip = VaultDetection.Select(VaultDefinitions.Unarmed, Trace(kind, height), falling: false, new Vector3D(speed, 0, 0));

        Assert.Equal(expected, clip?.Sequence.Name);
    }

    [Fact]
    public void Obstacles_below_every_row_have_no_clip()
    {
        Assert.Null(VaultDetection.Select(VaultDefinitions.Unarmed, Trace(VaultKind.Over, 40), falling: false, default));
    }

    [Fact]
    public void A_low_wall_ahead_is_vaulted_over_and_a_deep_block_is_climbed_onto()
    {
        VaultTrace over = Assert.IsType<VaultTrace>(VaultDetection.Trace(World(depth: 20, Solid), Standing(), out VaultMiss overMiss));
        VaultTrace onto = Assert.IsType<VaultTrace>(VaultDetection.Trace(World(depth: 400, Solid), Standing(), out VaultMiss ontoMiss));

        Assert.Equal((VaultMiss.None, VaultMiss.None), (overMiss, ontoMiss));
        Assert.Equal(VaultKind.Over, over.Kind);
        Assert.Equal(VaultKind.Onto, onto.Kind);
        Assert.Equal(115, over.Height, 1);
        Assert.Equal(100, over.Edge.X, 1);
        Assert.Equal(-1, over.WallNormal.X, 3);
        Assert.Equal(115, onto.Landing.Z, 1);
        Assert.True(onto.CanStand);
    }

    [Fact]
    public void Collision_that_only_blocks_movement_is_not_vaulted()
    {
        Assert.Null(VaultDetection.Trace(World(depth: 20, PlacementFlags.BlocksPawn), Standing(), out VaultMiss miss));
        Assert.Equal(VaultMiss.NoWall, miss);
    }

    [Fact]
    public void A_vault_plays_its_root_path_from_the_edge_and_hands_over_before_the_animation_ends()
    {
        VaultClip clip = VaultDefinitions.Unarmed.Ground[1].Speeds[1].Over;
        VaultTrace trace = Trace(VaultKind.Over, 115);
        VaultState state = VaultMovement.Begin(clip, VaultDefinitions.Unarmed, trace, new Vector3D(-50, 0, 0), default, onGround: true);
        VaultPathPoint start = clip.Sequence.Path[0];

        Assert.Equal(clip.MaxEntrySecondsFromGround, state.StartTime, 6);
        Assert.Equal(new Vector3D(100.0 + start.X, start.Y, 115.0 + start.Z), VaultMovement.Position(state, 0));
        int duration = VaultMovement.Duration(state);
        Assert.Equal((int)Math.Ceiling((clip.Sequence.Length - state.StartTime - VaultParameters.ExitBlendSeconds) / Tick - 1e-9), duration);

        Vector3D feet = new(-50, 0, 0);
        VaultStep step = default;
        for (int tick = 1; tick <= duration; tick++)
        {
            step = VaultMovement.Step(state, feet, Tick);
            Assert.Equal(RoughMovementMode.Air, step.Mode);
            Assert.Equal(CharacterStance.Uncontrolled, step.ActionStance);
            Assert.Equal(tick == duration, step.Finished);
            (state, feet) = (step.State, step.Position);
        }

        Assert.Equal(VaultMovement.Position(state, state.StartTime + (duration * Tick)), step.Position);
    }

    // A character standing at the origin facing +X with a wall face at X = 100, 115 high, on a floor at height 0.
    private static VaultQuery Standing() =>
        new(new Vector3D(0, 0, s_capsule.HalfHeight), 0, 0, 0, InputDirection.Forward, default, Vector3.UnitZ, OnGround: true, Falling: false, s_capsule);

    private static CollisionWorld World(double depth, PlacementFlags blocks)
    {
        MapCollisionBuilder builder = SyntheticMaps.Builder();
        int cell = builder.AddCell("/Game/Cell", []);
        builder.AddPlacement(builder.AddBody(SyntheticMaps.Body("floor", boxes: [SyntheticMaps.Box(new Vector3(3000, 3000, 10))])), cell,
            SyntheticMaps.At(0, 0, -10), Solid);
        builder.AddPlacement(builder.AddBody(SyntheticMaps.Body("block", boxes: [SyntheticMaps.Box(new Vector3((float)(depth / 2), 300, 57.5f))])), cell,
            SyntheticMaps.At(100 + (depth / 2), 0, 57.5), blocks);
        return new CollisionWorld(builder.Build());
    }

    private static VaultTrace Trace(VaultKind kind, double height) =>
        new(kind, true, new Vector3D(100, 0, height), -Vector3.UnitX, new Vector3D(180, 0, 0), height, 100, 100, height, default, default);
}
