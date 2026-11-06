using Ares.Raid.World.Movement;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Game.Movement;

namespace Ares.Raid.Tests.World.Movement;

public sealed class FlatGroundMovementTests
{
    [Fact]
    public void Running_accelerates_to_the_armed_speed_and_brakes_to_rest()
    {
        MovementInput forward = new(1, 126, 0, MovementButtons.None);
        Vector3D velocity = Step(default, forward);
        Assert.Equal(new Vector3D(25, 0, 0), velocity);
        for (int tick = 0; tick < 20; tick++)
        {
            velocity = Step(velocity, forward);
        }

        Assert.Equal(new Vector3D(300, 0, 0), velocity);
        velocity = Step(velocity, default);
        Assert.Equal(new Vector3D(238, 0, 0), velocity);
        velocity = Step(velocity, default);
        Assert.Equal(new Vector3D(189, 0, 0), velocity);
        for (int tick = 0; tick < 20; tick++)
        {
            velocity = Step(velocity, default);
        }

        Assert.Equal(default, velocity);
    }

    [Fact]
    public void The_velocity_calculation_preserves_fractions_until_the_walking_step_rounds_them()
    {
        Vector3D before = new(300, 0, 0);
        Vector3D calculated = FlatGroundMovement.CalculateVelocity(before, default, 1.0 / 30,
            GroundMovementParameters.Armed, steerAboveMaximum: false, fixBrakingOvershoot: false);
        Assert.Equal(238 + (1.0 / 3), calculated.X, 10);
        Assert.Equal(new Vector3D(238, 0, 0), Step(before, default));
    }

    [Fact]
    public void Turning_changes_direction_without_discarding_all_momentum()
    {
        Vector3D velocity = Step(new Vector3D(300, 0, 0), new MovementInput(1, 0, 126, MovementButtons.None));
        Assert.Equal(new Vector3D(280, 45, 0), velocity);
    }

    [Fact]
    public void Overspeed_steering_preserves_speed_above_the_new_limit()
    {
        Vector3D velocity = FlatGroundMovement.StepVelocity(new Vector3D(600, 0, 0), new MovementInput(1, 126, 0, MovementButtons.None),
            0, 1.0 / 30, GroundMovementParameters.Armed, steerAboveMaximum: true, fixBrakingOvershoot: false);
        Assert.Equal(new Vector3D(478, 0, 0), velocity);
    }

    [Fact]
    public void The_braking_overshoot_fix_retains_target_speed_when_accelerating_forward()
    {
        MovementInput input = new(1, 126, 0, MovementButtons.None);
        Vector3D before = new(304, 0, 0);
        Vector3D fixedVelocity = FlatGroundMovement.StepVelocity(before, input, 0, 1.0 / 30,
            GroundMovementParameters.Armed, steerAboveMaximum: false, fixBrakingOvershoot: true);
        Assert.Equal(new Vector3D(300, 0, 0), fixedVelocity);
        Assert.Equal(new Vector3D(267, 0, 0), Step(before, input));
    }

    private static Vector3D Step(Vector3D before, MovementInput input) => FlatGroundMovement.StepVelocity(before, input,
        0, 1.0 / 30, GroundMovementParameters.Armed, steerAboveMaximum: false, fixBrakingOvershoot: false);
}
