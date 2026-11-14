using System.Numerics;
using Ares.Common.Maps;
using Ares.Raid.Tests.World.Collision;
using Ares.Raid.World.Collision;
using Ares.Raid.World.Movement;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Game.Movement;
using Ares.Structs.Game.World;

namespace Ares.Raid.Tests.World.Movement;

public sealed class LadderMovementTests
{
    private const double Tick = 1.0 / 30;
    private const int Climbing = 15;
    private static readonly double s_halfHeight = DamPlayerStarts.Capsule.HalfHeight;

    // A ladder facing +Y (yaw 90) at the origin, 925 cm of spline: climb height 824, line 55 cm behind it (-Y).
    private static readonly LadderPlacement s_ladder = new("/Game/Cell", "BP_Ladder", 925, new Vector3D(0, 0, 0), 90);
    private static readonly LadderLine s_line = LadderLine.From(s_ladder);

    [Fact]
    public void The_climbing_line_lies_behind_the_ladder_and_starts_above_its_origin()
    {
        Assert.Equal(0, s_line.Origin.X, 9);
        Assert.Equal(-55, s_line.Origin.Y, 9);
        Assert.Equal(824, s_line.Height);
        Assert.Equal(Math.PI / 2, s_line.Yaw, 12);
        Vector3D local = s_line.Local(new Vector3D(10, -55, 33 + 100));
        Assert.Equal(0, local.X, 9);
        Assert.Equal(-10, local.Y, 9);
        Assert.Equal(100, local.Z, 9);
        Assert.Equal(725, LadderParameters.TopExitHeight(s_line.Height));
    }

    [Fact]
    public void Every_Dam_ladder_has_a_line_long_enough_to_leave_over_the_top()
    {
        Assert.All(DamLadders.All, ladder => Assert.True(LadderParameters.TopExitHeight(LadderLine.From(ladder).Height) > 0));
    }

    [Fact]
    public void Climbing_up_reaches_the_climb_speed_at_a_constant_acceleration()
    {
        LadderStep first = Climb(Input(InputDirection.Forward), Feet(100), default);
        Assert.Equal(500 * Tick, first.Velocity.Z, 9);
        Assert.Equal(Feet(100).Z + (first.Velocity.Z * Tick), first.Position.Z, 9);
        Assert.Equal(LadderExit.None, first.Exit);
        Assert.Equal(LocoMode.Still, first.ActionLocomotion);
        Assert.Equal(140, Climb(Input(InputDirection.ForwardLeft), Feet(100), new Vector3D(0, 0, 135)).Velocity.Z, 9);
        LadderStep sprint = Climb(Input(InputDirection.Forward) with { Sprint = true }, Feet(100), new Vector3D(0, 0, 170));
        Assert.Equal(170, sprint.Velocity.Z, 9);
        Assert.Equal(LocoMode.Sprinting, sprint.ActionLocomotion);
        Assert.True(sprint.StaminaDrain);
        Assert.Equal(-1.2, LadderParameters.SprintStaminaMagnitude, 12);
        Assert.Equal(LadderParameters.SprintActiveFlag, sprint.Flags & LadderParameters.SprintActiveFlag);
        LadderStep exhausted = Climb(Input(InputDirection.Forward) with { Sprint = true, Exhausted = true }, Feet(100), new Vector3D(0, 0, 140));
        Assert.Equal(140, exhausted.Velocity.Z, 9);
        Assert.False(exhausted.StaminaDrain);
    }

    [Fact]
    public void Releasing_the_input_slows_the_climb_down_and_holds_still()
    {
        Assert.Equal(140 - (500 * Tick), Climb(Input(InputDirection.None), Feet(100), new Vector3D(0, 0, 140)).Velocity.Z, 9);
        Assert.Equal(0, Climb(Input(InputDirection.Left), Feet(100), default).Velocity.Z);
    }

    [Fact]
    public void Climbing_down_and_sliding_with_sprint()
    {
        Assert.Equal(-155, Climb(Input(InputDirection.Backward), Feet(300), new Vector3D(0, 0, -150)).Velocity.Z, 9);
        LadderStep slide = Climb(Input(InputDirection.BackwardRight) with { Sprint = true }, Feet(300), new Vector3D(0, 0, -480));
        Assert.Equal(-480 - (500 * Tick), slide.Velocity.Z, 9);
        Assert.Equal(LadderParameters.SlidingFlag, slide.Flags & LadderParameters.SlidingFlag);
        Assert.Equal(-700 + (5000 * Tick), Climb(Input(InputDirection.None), Feet(300), new Vector3D(0, 0, -700)).Velocity.Z, 9);
    }

    [Fact]
    public void Off_the_line_the_character_moves_back_at_the_distance_per_second_and_faces_the_ladder()
    {
        Vector3D feet = Feet(100) with { X = 10 };
        LadderStep step = Climb(Input(InputDirection.None), feet, default);
        Assert.Equal(-10, step.Velocity.X, 9);
        Assert.Equal(0, step.Velocity.Y, 9);
        Assert.Equal(Math.PI / 2, step.Yaw, 12);
        Assert.Equal(LadderParameters.ClimbingFlags | LadderParameters.CrouchInputBlockedFlag, step.Flags);
        Assert.Equal(LadderExit.FallOff, Climb(Input(InputDirection.None), Feet(100) with { X = 60 }, default).Exit);
        Assert.Equal(LadderExit.None, Climb(Input(InputDirection.None), Feet(100) with { X = 59 }, default).Exit);
    }

    [Fact]
    public void Getting_on_pulls_a_share_of_the_way_onto_the_line_each_tick()
    {
        LadderState state = LadderMovement.Begin(s_line, topEntry: false);
        // Feet on the floor 20 cm below the line start: pulled towards 10 cm above it.
        LadderStep step = LadderMovement.Step(state, Input(InputDirection.Forward) with { JumpPressed = true }, Feet(-20) with { X = 5 }, default,
            s_halfHeight, Tick, null);
        Assert.Equal(0.3 * 30 / Tick, step.Velocity.Z, 9);
        Assert.Equal(-0.3 * 5 / Tick, step.Velocity.X, 9);
        Assert.Equal(LadderExit.None, step.Exit);
        Assert.Equal(1, step.State.Ticks);
        Assert.Equal(LadderParameters.ClimbingFlags, step.Flags);
        Assert.True(state with { Ticks = 14 } is { Aligning: true });
        Assert.False(state with { Ticks = 15 } is { Aligning: true });
    }

    [Fact]
    public void A_falling_catch_slides_and_slows_down_over_time()
    {
        LadderState state = LadderMovement.Begin(s_line, topEntry: false);
        LadderStep caught = LadderMovement.Step(state, Input(InputDirection.None), Feet(300), new Vector3D(0, 0, -300), s_halfHeight, Tick, null);
        Assert.Equal(-300, caught.Velocity.Z, 9);
        Assert.Equal(LadderParameters.SlidingFlag, caught.Flags & LadderParameters.SlidingFlag);
        LadderStep later = LadderMovement.Step(state with { Ticks = 6 }, Input(InputDirection.None), Feet(300), new Vector3D(0, 0, -300), s_halfHeight, Tick, null);
        // 0.2 s of the 4 s fade.
        Assert.Equal(-285, later.Velocity.Z, 4);
        // At the bottom a fast fall goes on falling and leaves the ladder.
        LadderStep fast = LadderMovement.Step(state, Input(InputDirection.None), Feet(-5), new Vector3D(0, 0, -600), s_halfHeight, Tick, null);
        Assert.Equal(LadderExit.StepOff, fast.Exit);
        Assert.Equal(-600 - (1964 * Tick), fast.Velocity.Z, 9);
    }

    [Fact]
    public void Reaching_the_bottom_steps_off_onto_a_floor()
    {
        LadderStep bottom = Climb(Input(InputDirection.Backward), Feet(3), new Vector3D(0, 0, -155));
        Assert.Equal(LadderExit.StepOff, bottom.Exit);
        Assert.Equal(default, bottom.Velocity);
        Assert.Equal(LadderExit.None, Climb(Input(InputDirection.None), Feet(10), default).Exit);
        // Without a floor within reach below the capsule centre the character stays at the bottom.
        LadderStep hanging = Climb(Input(InputDirection.Backward), Feet(3), new Vector3D(0, 0, -155), Floor(-600));
        Assert.Equal(LadderExit.None, hanging.Exit);
        Assert.Equal(default, hanging.Velocity);
        Assert.Equal(LadderExit.StepOff, Climb(Input(InputDirection.Backward), Feet(3), new Vector3D(0, 0, -155), Floor(-100)).Exit);
        Assert.Equal(LadderExit.StepOff, Climb(Input(InputDirection.None) with { Sprint = true }, Feet(-1), default, Floor(-600)).Exit);
    }

    [Fact]
    public void Crouch_drops_off_the_ladder()
    {
        LadderStep step = Climb(Input(InputDirection.Forward) with { CrouchHeld = true }, Feet(300), new Vector3D(0, 0, 140));
        Assert.Equal(LadderExit.StepOff, step.Exit);
        Assert.Equal(default, step.Velocity);
        Assert.Equal(6, LadderMovement.CrouchBlockTicks);
        Assert.True(LadderInput.From(new MovementInput(1, 0, 0, MovementButtons.None), new CharacterInput(1, 1UL << 5), 0, false).CrouchHeld);
    }

    [Fact]
    public void A_jump_leaves_from_a_point_in_front_and_above_the_feet()
    {
        LadderStep jump = Climb(Input(InputDirection.Backward) with { JumpPressed = true, ControlYaw = 0 }, Feet(300), new Vector3D(0, 0, -155));
        Assert.Equal(LadderExit.JumpOff, jump.Exit);
        Assert.Equal(default, jump.Velocity);
        Vector3D start = jump.ExitStart!.Value;
        Assert.Equal(0, start.X, 9);
        Assert.Equal(-55 + 70, start.Y, 9);
        Assert.Equal(Feet(300).Z + s_halfHeight - 92 + 150, start.Z, 9);
        Assert.Equal(-1, jump.JumpDirection!.Value.X, 9);
    }

    [Fact]
    public void Climbing_past_the_top_exit_height_leaves_over_the_top()
    {
        LadderStep top = Climb(Input(InputDirection.Forward), Feet(724), new Vector3D(0, 0, 140));
        Assert.Equal(LadderExit.ClimbOffTop, top.Exit);
        Vector3D target = top.ExitTarget!.Value;
        Assert.Equal(-55 + 99, target.Y, 9);
        Assert.Equal(Feet(724).Z + s_halfHeight, target.Z, 9);
        Assert.Equal(LadderExit.None, Climb(Input(InputDirection.Forward), Feet(700), new Vector3D(0, 0, 140)).Exit);
        Assert.Equal(target with { Z = 800 }, LadderMovement.ClimbOffTopFloor(Floor(800), target));
        Assert.Equal(target, LadderMovement.ClimbOffTopFloor(Floor(-100), target));
        // A ladder that does not climb off stops at the top exit height.
        LadderState stopping = LadderMovement.Begin(s_line with { AutoClimbOffTop = false }, false) with { Ticks = Climbing };
        LadderStep stopped = LadderMovement.Step(stopping, Input(InputDirection.Forward), Feet(724), new Vector3D(0, 0, 30), s_halfHeight, Tick, null);
        Assert.Equal(LadderExit.None, stopped.Exit);
        Assert.Equal(30, stopped.Velocity.Z, 9);
    }

    [Fact]
    public void A_top_entry_plays_its_animation_for_a_second()
    {
        LadderState state = LadderMovement.Begin(s_line, topEntry: true);
        LadderStep entry = LadderMovement.Step(state with { Ticks = 29 }, Input(InputDirection.Forward), Feet(700), default, s_halfHeight, Tick, null);
        Assert.Equal(default, entry.Velocity);
        Assert.Equal(LadderExit.None, entry.Exit);
        Assert.NotEqual(default, LadderMovement.Step(state with { Ticks = 30 }, Input(InputDirection.Forward), Feet(700), default, s_halfHeight, Tick, null).Velocity);
        Vector3D anchor = LadderMovement.TopEntryAnchor(s_ladder, new Vector3D(1, 2, 900));
        Assert.Equal(new Vector3D(0, 0, 900 - 187 - 33), anchor);
    }

    private static LadderInput Input(InputDirection direction) => new(direction, Sprint: false, JumpPressed: false, CrouchHeld: false, ControlYaw: Math.PI / 2);

    // Feet on the line at a height above its start.
    private static Vector3D Feet(double height) => new(0, -55, LadderParameters.LineStartHeight + height);

    private static LadderStep Climb(LadderInput input, Vector3D feet, Vector3D velocity, CollisionWorld? world = null) =>
        LadderMovement.Step(LadderMovement.Begin(s_line, topEntry: false) with { Ticks = Climbing }, input, feet, velocity, s_halfHeight, Tick, world);

    private static CollisionWorld Floor(double height)
    {
        MapCollisionBuilder builder = SyntheticMaps.Builder();
        int cell = builder.AddCell("/Game/Cell", []);
        builder.AddPlacement(builder.AddBody(SyntheticMaps.Body("floor", boxes: [SyntheticMaps.Box(new Vector3(3000, 3000, 10))])), cell,
            SyntheticMaps.At(0, 0, height - 10), PlacementFlags.BlocksPawn | PlacementFlags.BlocksClimbable);
        return new CollisionWorld(builder.Build());
    }
}
