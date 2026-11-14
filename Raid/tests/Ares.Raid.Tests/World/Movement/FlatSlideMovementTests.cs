using Ares.Raid.World.Movement;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Game.Movement;

namespace Ares.Raid.Tests.World.Movement;

public sealed class FlatSlideMovementTests
{
    private static readonly MovementInput s_forward = new(1, 126, 0, MovementButtons.Crouch | MovementButtons.Sprint);

    [Fact]
    public void Sliding_retains_fractional_velocity_while_braking()
    {
        SlideStep step = FlatSlideMovement.Step(default, new Vector3D(650, 0, 0), s_forward, 0, 0, 1.0f / 30,
            SlideMovementParameters.Default, steerAboveMaximum: true, fixBrakingOvershoot: false, forceCancel: true);
        Assert.InRange(step.Velocity.X, 627.16, 627.17);
        Assert.NotEqual(Math.Round(step.Velocity.X), step.Velocity.X);
    }

    [Fact]
    public void A_forward_slide_keeps_its_initial_heading_and_then_loses_speed()
    {
        Vector3D initial = new(650, 0, 0);
        SlideStep step = Step(default, initial, s_forward, 0);
        Assert.Equal(initial, step.State.InitialVelocity);
        Assert.Equal(initial, step.Velocity);
        Assert.False(step.Finished);
        for (int tick = 1; tick < 180 && !step.Finished; tick++)
        {
            step = Step(step.State, step.Velocity, s_forward, tick / 30.0);
            Assert.Equal(0, step.Velocity.Y);
            Assert.True(step.Velocity.X <= initial.X);
        }

        Assert.True(step.Finished);
        Assert.InRange(step.Velocity.X, 0, 150);
    }

    [Fact]
    public void Releasing_crouch_waits_for_the_minimum_slide_time_then_brakes_before_exit()
    {
        Vector3D velocity = new(650, 0, 0);
        MovementInput released = s_forward with { Buttons = MovementButtons.None };
        SlideStep held = Step(default, velocity, s_forward, 1.19);
        SlideStep before = Step(default, velocity, released, 1.19);
        Assert.Equal(held, before);
        SlideStep cancelling = Step(before.State, velocity, released, 1.2);
        Assert.False(cancelling.Finished);
        Assert.Equal(default, cancelling.Input);
        Assert.InRange(cancelling.Velocity.X, 0, velocity.X - 1);
        SlideStep finished = Step(cancelling.State, new Vector3D(412.5, 0, 0), released, 1.25);
        Assert.True(finished.Finished);
        Assert.Equal(default, finished.Input);
        Assert.Equal(cancelling.State.Slowdown, finished.State.Slowdown);
    }

    [Fact]
    public void A_forced_cancellation_can_finish_before_the_minimum_time_at_high_speed()
    {
        SlideStep step = FlatSlideMovement.Step(default, new Vector3D(650, 0, 0), s_forward, 0, 0, 1.0f / 30,
            SlideMovementParameters.Default, steerAboveMaximum: false, fixBrakingOvershoot: false, forceCancel: true);
        Assert.True(step.Finished);
        Assert.Equal(default, step.Input);
        Assert.InRange(step.Velocity.X, 0, 650);
    }

    [Fact]
    public void Sideways_steering_turns_the_slide_and_limits_input_strength()
    {
        MovementInput sideways = s_forward with { Forward = 0, Right = 126 };
        SlideStep step = Step(default, new Vector3D(1000, 0, 0), sideways, 0);
        Assert.True(step.Input.X > 0);
        Assert.True(step.Input.Y > 0);
        Assert.Equal(0.5, Math.Sqrt((step.Input.X * step.Input.X) + (step.Input.Y * step.Input.Y)), 12);
        Assert.True(step.Velocity.Y > 0);
        Assert.False(step.Finished);
    }

    [Fact]
    public void An_active_slide_retains_its_entry_direction_when_velocity_turns()
    {
        SlideState state = new(new Vector3D(650, 0, 0), 0.1f);
        SlideStep step = Step(state, new Vector3D(0, 650, 0), s_forward with { Forward = 0, Right = 126 }, 0.75);
        Assert.Equal(state.InitialVelocity, step.State.InitialVelocity);
        Assert.True(step.Input.X > 0);
        Assert.True(step.Input.Y > 0);
        Assert.True(step.State.Slowdown > state.Slowdown);
    }

    [Fact]
    public void Steering_uses_world_input_relative_to_velocity_and_normalizes_axis_strength()
    {
        Vector3D velocity = new(0, 650, 0);
        SlideStep forward = Step(default, velocity, s_forward, 0.5);
        SlideStep partial = Step(default, velocity, s_forward with { Forward = 63 }, 0.5);
        Assert.Equal(forward, partial);
        SlideStep rotated = FlatSlideMovement.Step(default, velocity, s_forward with { Forward = 0, Right = -126 }, Math.PI / 2, 0.5,
            1.0f / 30, SlideMovementParameters.Default, steerAboveMaximum: false, fixBrakingOvershoot: false);
        Assert.Equal(forward.Input.X, rotated.Input.X, 12);
        Assert.Equal(forward.Input.Y, rotated.Input.Y, 12);
        Assert.Equal(forward.Velocity, rotated.Velocity);
    }

    [Fact]
    public void A_slide_step_counts_itself_and_keeps_the_slides_start_and_maximum_speed()
    {
        SlideState state = new(new Vector3D(650, 0, 0), 0) { StartFrame = 70 };
        SlideStep second = Step(state with { Ticks = 1 }, new Vector3D(650, 0, 0), s_forward, 1 / 30.0);
        SlideStep third = Step(state with { Ticks = 2 }, new Vector3D(650, 0, 0), s_forward, 1 / 30.0);
        Assert.Equal(2, second.State.Ticks);
        Assert.Equal(third.Velocity, second.Velocity);
        Assert.Equal(70U, second.State.StartFrame);
        Assert.Equal(SlideMovementParameters.Default.MaximumSpeed, second.State.MaximumSpeed);
        Assert.Equal(1, Step(default, new Vector3D(650, 0, 0), s_forward, 0).State.Ticks);
    }

    [Fact]
    public void A_tilt_below_the_direction_tolerance_slides_like_level_ground()
    {
        Vector3D velocity = new(650, 0, 0);
        SlideStep level = SlopeStep(velocity, default, 1.0);
        Assert.Equal(level, SlopeStep(velocity, new Vector3D(0, 0, 1), 1.0));
        Assert.Equal(level.Input, SlopeStep(velocity, new Vector3D(0.00005, 0, Math.Sqrt(1 - 0.0000000025)), 1.0).Input);
    }

    [Fact]
    public void Downhill_on_a_slope_the_slope_input_speeds_the_slide_up()
    {
        // 20 degrees down along the velocity: steepness 0.34341, full alignment, no boost while the slope input exceeds the speed's share.
        Vector3D velocity = new(650, 0, 0);
        SlideStep step = SlopeStep(velocity, Tilt(20), 0.5);
        const double Minimum = 100 / 1300.0 * 1.05;
        const double Steepness = Minimum + ((1 - Minimum) * 10 / 35);
        const float Slowdown = (float)(0.075 * (1 - Steepness) * 2 / 30);
        Assert.Equal(Slowdown, step.State.Slowdown, 6);
        Assert.Equal((Steepness + 1) / 2 * (1 - Slowdown), step.Input.X, 6);
        Assert.Equal(0, step.Input.Y, 12);
        Assert.True(step.Velocity.X > velocity.X + 15);
    }

    [Fact]
    public void Going_up_a_slope_brakes_the_slide()
    {
        Vector3D velocity = new(-650, 0, 0);
        SlideStep step = SlopeStep(velocity, Tilt(20), 0.5);
        const double Minimum = 100 / 1300.0 * 1.05;
        const double Steepness = Minimum + ((1 - Minimum) * 10 / 35);
        const float Slowdown = (float)(0.075 * (1 - Steepness) * 2 / 30);
        Assert.Equal((-(Steepness / 2) + (0.8f * 20 / 45)) * (1 - Slowdown), step.Input.X, 6);
        Assert.True(step.Velocity.X > velocity.X + 15);
    }

    [Fact]
    public void On_a_gentle_slope_following_it_adds_a_small_input_that_keeps_the_speed()
    {
        // At 5 degrees the alignment's smallest scale 0.08077 is eased out to 0.39375; half of it joins boost and momentum.
        Vector3D velocity = new(650, 0, 0);
        SlideStep level = SlopeStep(velocity, default, 1.0);
        (_, Vector3D kept, Vector3D gentleInput, _) = SlopeStep(velocity, Tilt(5), 1.0);
        Assert.Equal(0.5 * ((1 - (1 / 1.5)) + 0.5), level.Input.X, 9);
        Assert.Equal(0.5, gentleInput.X, 9);
        Assert.True(level.Velocity.X < velocity.X - 15);
        // The speed is kept along the ground.
        Assert.Equal(velocity.X, Math.Sqrt((kept.X * kept.X) + (kept.Y * kept.Y) + (kept.Z * kept.Z)), 6);
    }

    [Fact]
    public void Sideways_steering_at_held_speed_turns_with_steering_friction()
    {
        SlideState state = new(new Vector3D(613, 0, 0), 0, 8);
        SlideStep step = Step(state, new Vector3D(613, 0, 0), s_forward with { Forward = 0, Right = 126 }, 0.3);
        double degrees = Math.Atan2(step.Velocity.Y, step.Velocity.X) * 180 / Math.PI;
        Assert.InRange(degrees, 1.4, 1.7);
        Assert.Equal(613, Math.Sqrt((step.Velocity.X * step.Velocity.X) + (step.Velocity.Y * step.Velocity.Y)), 6);
    }

    [Fact]
    public void Slide_braking_friction_is_unchanged_by_steering_friction()
    {
        GroundMovementParameters ground = SlideMovementParameters.Ground;
        GroundMovementParameters cancelling = SlideMovementParameters.CancellingGround;
        Assert.Equal(0.9, ground.Friction * ground.BrakingFrictionFactor, 6);
        Assert.Equal(3, cancelling.Friction * cancelling.BrakingFrictionFactor, 6);
    }

    private static SlideStep Step(SlideState state, Vector3D velocity, MovementInput input, double elapsed)
        => FlatSlideMovement.Step(state, velocity, input, 0, elapsed, 1.0f / 30, SlideMovementParameters.Default,
            steerAboveMaximum: false, fixBrakingOvershoot: false);

    private static SlideStep SlopeStep(Vector3D velocity, Vector3D normal, double elapsed)
        => FlatSlideMovement.Step(new SlideState(velocity, 0, 10), velocity, s_forward, 0, elapsed, 1.0f / 30, SlideMovementParameters.Default,
            steerAboveMaximum: true, fixBrakingOvershoot: false, groundNormal: normal);

    // The normal of ground falling away along +X by an angle in degrees.
    private static Vector3D Tilt(double degrees) => new(Math.Sin(degrees * Math.PI / 180), 0, Math.Cos(degrees * Math.PI / 180));
}
