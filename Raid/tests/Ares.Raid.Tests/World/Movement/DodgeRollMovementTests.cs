using Ares.Raid.World.Movement;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Game.Movement;

namespace Ares.Raid.Tests.World.Movement;

public sealed class DodgeRollMovementTests
{
    private const double Tick = 1.0 / 30;
    private static readonly MovementInput s_press = new(1, 126, 0, MovementButtons.DodgeRoll);

    [Fact]
    public void A_roll_starts_on_a_press_with_movement_input_on_the_ground()
    {
        Assert.True(DodgeRollMovement.CanStart(s_press, inAir: false));
        Assert.False(DodgeRollMovement.CanStart(s_press with { Buttons = MovementButtons.DodgeRoll | MovementButtons.PreviousDodgeRoll }, inAir: false));
        Assert.False(DodgeRollMovement.CanStart(s_press with { Forward = 0 }, inAir: false));
        Assert.False(DodgeRollMovement.CanStart(s_press, inAir: true));
        Assert.False(DodgeRollMovement.CanStart(s_press, inAir: false, blocked: true));
        Assert.False(DodgeRollMovement.CanStart(s_press with { Forward = 6, Right = -6 }, inAir: false));
        Assert.True(DodgeRollMovement.CanStart(s_press with { Forward = 7, Right = -6 }, inAir: false));
    }

    [Fact]
    public void The_variant_follows_sprint_exhaustion_load_and_skill()
    {
        Assert.Equal(DodgeRollKind.Standard, DodgeRollMovement.Select(sprinting: false));
        Assert.Equal(DodgeRollKind.Sprint, DodgeRollMovement.Select(sprinting: true));
        Assert.Equal(DodgeRollKind.Standard, DodgeRollMovement.Select(sprinting: true, heavy: true));
        Assert.Equal(DodgeRollKind.Exhausted, DodgeRollMovement.Select(sprinting: true, exhausted: true));
        Assert.Equal(DodgeRollKind.Exhausted, DodgeRollMovement.Select(sprinting: false, heavy: true));
        Assert.Equal(DodgeRollKind.Ninja, DodgeRollMovement.Select(sprinting: false, skill: true));
        Assert.Equal(DodgeRollKind.Sprint, DodgeRollMovement.Select(sprinting: true, skill: true));
    }

    [Theory]
    [InlineData(126, 0, InputDirection.Forward)]
    [InlineData(48, 0, InputDirection.None)]
    [InlineData(49, 0, InputDirection.Forward)]
    [InlineData(-49, 0, InputDirection.Backward)]
    [InlineData(0, -126, InputDirection.Left)]
    [InlineData(90, 90, InputDirection.ForwardRight)]
    [InlineData(90, -90, InputDirection.ForwardLeft)]
    [InlineData(-90, 90, InputDirection.BackwardRight)]
    [InlineData(-90, -90, InputDirection.BackwardLeft)]
    public void Axes_snap_to_eight_directions(sbyte forward, sbyte right, InputDirection expected)
    {
        Assert.Equal(expected, DodgeRollMovement.Direction(s_press with { Forward = forward, Right = right }));
    }

    [Fact]
    public void The_roll_direction_is_the_snapped_input_turned_by_the_control_heading()
    {
        DodgeRollState forward = DodgeRollMovement.Begin(DodgeRollKind.Standard, s_press, Math.PI / 2, default, 0);
        Assert.Equal(0, forward.Direction.X, 12);
        Assert.Equal(1, forward.Direction.Y, 12);
        DodgeRollState diagonal = DodgeRollMovement.Begin(DodgeRollKind.Standard, s_press with { Forward = 100, Right = 60 }, 0, default, 0);
        Assert.Equal(Math.PI / 4, Math.Atan2(diagonal.Direction.Y, diagonal.Direction.X), 6);
        DodgeRollState weak = DodgeRollMovement.Begin(DodgeRollKind.Standard, s_press with { Forward = 20 }, 0, new Vector3D(0, -150, 0), 0);
        Assert.Equal(new Vector3D(0, -1, 0), weak.Direction);
        DodgeRollState still = DodgeRollMovement.Begin(DodgeRollKind.Standard, s_press with { Forward = 20 }, 0, default, Math.PI);
        Assert.Equal(-1, still.Direction.X, 12);
        Assert.Equal(0, still.Direction.Z);
    }

    [Fact]
    public void Each_step_moves_by_the_root_motion_of_one_animation_frame()
    {
        IReadOnlyList<float> root = DodgeRollSequences.Standard.RootForward;
        DodgeRollState state = DodgeRollMovement.Begin(DodgeRollKind.Standard, s_press, 0, default, 0);
        DodgeRollStep first = DodgeRollMovement.Step(state, default, onGround: true, 0, Tick);
        Assert.Equal(((double)root[1] - root[0]) * 30, first.Velocity.X, 9);
        Assert.Equal(0, first.Velocity.Y);
        Assert.Equal(CharacterStance.Uncontrolled, first.ActionStance);
        Assert.Equal(LocoMode.Running, first.ActionLocomotion);
        Assert.False(first.InAir);
        double travelled = first.Velocity.X * Tick;
        DodgeRollStep step = first;
        while (!step.Finished)
        {
            step = DodgeRollMovement.Step(step.State, step.Velocity, onGround: true, 0, Tick);
            travelled += step.Velocity.X * Tick;
        }

        Assert.Equal(29, step.State.Ticks);
        Assert.Equal((double)root[29] - root[0], travelled, 6);
    }

    [Fact]
    public void A_sprint_roll_scales_its_root_motion_to_its_distance()
    {
        IReadOnlyList<float> root = DodgeRollSequences.Sprint.RootForward;
        DodgeRollStep step = DodgeRollMovement.Step(new DodgeRollState(DodgeRollKind.Sprint, new Vector3D(0, 1, 0), 3), default, onGround: true, 0, Tick);
        Assert.Equal(((double)root[4] - root[3]) * 30 * 545 / 600, step.Velocity.Y, 9);
        Assert.InRange(step.Velocity.Y, 1000, 1030);
    }

    [Fact]
    public void The_variants_hand_over_three_frames_before_their_animation_ends()
    {
        Assert.Equal(29, DodgeRollMovement.Duration(DodgeRollKind.Standard));
        Assert.Equal(29, DodgeRollMovement.Duration(DodgeRollKind.Sprint));
        Assert.Equal(43, DodgeRollMovement.Duration(DodgeRollKind.Exhausted));
        Assert.Equal(23, DodgeRollMovement.Duration(DodgeRollKind.Ninja));
    }

    [Fact]
    public void Off_the_ground_the_roll_keeps_falling()
    {
        DodgeRollState state = new(DodgeRollKind.Standard, new Vector3D(1, 0, 0), 5);
        DodgeRollStep step = DodgeRollMovement.Step(state, new Vector3D(300, 0, -50), onGround: false, -AirMovementParameters.FallAcceleration, Tick);
        Assert.Equal(-50 - (AirMovementParameters.FallAcceleration * Tick), step.Velocity.Z, 9);
        Assert.Equal(CharacterStance.InAir, step.ActionStance);
        Assert.True(step.InAir);
        Assert.Equal(-50, DodgeRollMovement.Step(state, new Vector3D(300, 0, -50), onGround: true, -AirMovementParameters.FallAcceleration, Tick).Velocity.Z);
    }

    [Fact]
    public void A_collision_stops_the_roll_and_ends_it_outside_the_grace_windows()
    {
        DodgeRollState start = new(DodgeRollKind.Standard, new Vector3D(1, 0, 0));
        DodgeRollStep early = DodgeRollMovement.Step(start, default, onGround: true, 0, Tick, collided: true);
        Assert.Equal(default, early.Velocity);
        Assert.False(early.Finished);
        Assert.False(DodgeRollMovement.Step(start with { Ticks = 3 }, default, true, 0, Tick, collided: true).Finished);
        Assert.True(DodgeRollMovement.Step(start with { Ticks = 10 }, default, true, 0, Tick, collided: true).Finished);
        Assert.False(DodgeRollMovement.Step(start with { Ticks = 24 }, default, true, 0, Tick, collided: true).Finished);
    }

    [Fact]
    public void Crouch_ends_the_roll_in_a_crouch_late_in_the_animation()
    {
        DodgeRollState state = new(DodgeRollKind.Standard, new Vector3D(1, 0, 0), 22);
        DodgeRollStep late = DodgeRollMovement.Step(state, default, true, 0, Tick, crouch: true);
        Assert.True(late.Finished);
        Assert.Equal(CharacterStance.Crouch, late.ActionStance);
        DodgeRollStep disabled = DodgeRollMovement.Step(state, default, true, 0, Tick, crouch: true, crouchExitDisabled: true);
        Assert.False(disabled.Finished);
        Assert.Equal(CharacterStance.Crouch, disabled.ActionStance);
        DodgeRollStep early = DodgeRollMovement.Step(state with { Ticks = 10 }, default, true, 0, Tick, crouch: true);
        Assert.False(early.Finished);
        Assert.Equal(CharacterStance.Uncontrolled, early.ActionStance);
    }
}
