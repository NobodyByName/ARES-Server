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

    [Fact]
    public void Level_ground_moves_exactly_like_the_horizontal_step()
    {
        MovementInput[] inputs =
        [
            new(1, 126, 0, MovementButtons.None), new(1, 89, 89, MovementButtons.None), new(1, 60, -20, MovementButtons.None),
            new(1, 0, 0, MovementButtons.None), new(1, -126, 0, MovementButtons.None),
        ];
        Vector3D velocity = default;
        for (int tick = 0; tick < 100; tick++)
        {
            MovementInput input = inputs[tick / 20];
            Vector3D level = FlatGroundMovement.StepVelocity(velocity, FlatGroundMovement.Direction(input, 0.3), FlatGroundMovement.Level,
                1.0 / 30, GroundMovementParameters.Armed, steerAboveMaximum: true, fixBrakingOvershoot: true);
            Vector3D horizontal = FlatGroundMovement.StepVelocity(velocity, input, 0.3, 1.0 / 30, GroundMovementParameters.Armed,
                steerAboveMaximum: true, fixBrakingOvershoot: true);
            Assert.Equal(horizontal, level);
            velocity = level;
        }
    }

    [Theory]
    [InlineData(10, 1)]
    [InlineData(10, -1)]
    public void Running_up_or_down_a_walkable_slope_holds_the_whole_speed_along_the_ground(double degrees, int uphill)
    {
        Vector3D velocity = Run(degrees, new Vector3D(uphill, 0, 0), 60);
        double radians = degrees * Math.PI / 180;
        Assert.InRange(Length(velocity), 299, 301);
        Assert.InRange(velocity.X * uphill, (300 * Math.Cos(radians)) - 1, (300 * Math.Cos(radians)) + 1);
        // Each component is rounded to whole units, so the velocity sits up to a unit off the ground plane.
        Assert.InRange(velocity.Z, (uphill * 300 * Math.Sin(radians)) - 1.5, (uphill * 300 * Math.Sin(radians)) + 1.5);
        Assert.Equal(0, velocity.Y);
    }

    [Fact]
    public void Running_across_a_slope_keeps_the_input_direction_and_stays_level()
    {
        Vector3D velocity = Run(25, new Vector3D(0, 1, 0), 60);
        Assert.Equal(new Vector3D(0, 300, 0), velocity);
    }

    [Fact]
    public void Uphill_input_on_steep_ground_is_weaker_and_downhill_input_is_not()
    {
        // 25 degrees is a fifth of the way from 15 to 65 degrees: uphill input keeps 1 - 0.35 / 5 = 0.93 of its strength.
        Vector3D normal = Normal(25);
        (Vector3D uphill, double? scalar) = FlatGroundMovement.SlopeInput(new Vector3D(1, 0, 0), normal);
        Assert.Equal(0.93, uphill.X, 6);
        Assert.Equal(0.93, scalar!.Value, 6);
        Assert.Equal((new Vector3D(-1, 0, 0), (double?)1), FlatGroundMovement.SlopeInput(new Vector3D(-1, 0, 0), normal));
        Assert.Equal((new Vector3D(1, 0, 0), (double?)null), FlatGroundMovement.SlopeInput(new Vector3D(1, 0, 0), Normal(15)));

        Vector3D velocity = Run(25, new Vector3D(1, 0, 0), 60);
        Assert.InRange(Length(velocity), 278, 280);
        Assert.InRange(velocity.X, (279 * Math.Cos(25 * Math.PI / 180)) - 1, (279 * Math.Cos(25 * Math.PI / 180)) + 1);
    }

    [Fact]
    public void The_ground_node_keeps_the_slope_scale_until_steep_ground_sets_it_again()
    {
        MovementInput forward = new(40, 126, 0, MovementButtons.None);
        GroundNodeState node = GroundNodeState.Initial.Next(40, forward, 300, false, false, false, LocoMode.Running, CharacterStance.Stand,
            false, false, slopeScalar: 0.93);
        Assert.Equal(0.93, node.SlopeInputScalar);
        node = node.Next(41, forward with { Frame = 41 }, 300, false, false, false, LocoMode.Running, CharacterStance.Stand, false, false);
        Assert.Equal(0.93, node.SlopeInputScalar);
    }

    private static Vector3D Run(double degrees, Vector3D direction, int ticks)
    {
        Vector3D normal = Normal(degrees);
        Vector3D velocity = default;
        for (int tick = 0; tick < ticks; tick++)
        {
            velocity = FlatGroundMovement.StepVelocity(velocity, FlatGroundMovement.SlopeInput(direction, normal).Input, normal, 1.0 / 30,
                GroundMovementParameters.Armed, steerAboveMaximum: true, fixBrakingOvershoot: true);
        }

        return velocity;
    }

    // Ground rising towards +X.
    private static Vector3D Normal(double degrees) =>
        new(-Math.Sin(degrees * Math.PI / 180), 0, Math.Cos(degrees * Math.PI / 180));

    private static double Length(Vector3D vector) => Math.Sqrt((vector.X * vector.X) + (vector.Y * vector.Y) + (vector.Z * vector.Z));

    private static Vector3D Step(Vector3D before, MovementInput input) => FlatGroundMovement.StepVelocity(before, input,
        0, 1.0 / 30, GroundMovementParameters.Armed, steerAboveMaximum: false, fixBrakingOvershoot: false);
}
