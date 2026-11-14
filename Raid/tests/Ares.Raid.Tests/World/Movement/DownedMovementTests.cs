using Ares.Raid.World.Movement;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Game.Movement;

namespace Ares.Raid.Tests.World.Movement;

public sealed class DownedMovementTests
{
    private const double Tick = 1.0 / 30;
    private const sbyte Full = PendulumInput.MovementAxisMaximum;
    private static readonly Vector3D s_up = new(0, 0, 1);
    private static readonly MovementInput s_forward = new(0, Full, 0, MovementButtons.None);
    private static readonly MovementInput s_none = new(0, 0, 0, MovementButtons.None);

    private static DownedInput Downed(MovementInput movement) => new(movement, 0, true, false, true, s_up);

    private static DownedStep Step(DownedState state, DownedInput input, Vector3D previous = default) =>
        DownedMovement.Step(state, input, previous, Tick, steerAboveMaximum: true, fixBrakingOvershoot: true);

    [Fact]
    public void Going_down_holds_the_character_for_one_and_a_half_seconds()
    {
        DownedState state = default;
        Vector3D velocity = default;
        for (int tick = 1; tick <= 44; tick++)
        {
            DownedStep held = Step(state, Downed(s_forward), velocity);
            Assert.Equal(DownedPhase.GoingDown, held.Phase);
            Assert.Equal(default, held.Velocity);
            Assert.NotEqual(0UL, held.GraphTags & GraphStateTags.AnimationPendulumStateDeathToDbno);
            (state, velocity) = (held.State, held.Velocity);
        }

        DownedStep crawling = Step(state, Downed(s_forward), velocity);
        Assert.Equal(DownedPhase.Crawling, crawling.Phase);
        Assert.Equal(0UL, crawling.GraphTags & GraphStateTags.AnimationPendulumStateDeathToDbno);
        Assert.Equal(DownedMovementParameters.NodeTags, crawling.GraphTags);
    }

    [Fact]
    public void A_fatal_fall_hands_over_without_the_hold()
    {
        DownedStep step = Step(default, Downed(s_forward) with { FromFatalFall = true });
        Assert.Equal(DownedPhase.Crawling, step.Phase);
        Assert.True(step.State.SkipGoingDown);
        Assert.Equal(DownedPhase.Crawling, Step(step.State, Downed(s_forward)).Phase);
    }

    [Fact]
    public void The_crawl_accelerates_to_one_hundred_along_the_control_yaw_and_brakes_within_three_ticks()
    {
        DownedState state = new(Ticks: 45);
        Vector3D velocity = default;
        double[] speeds = new double[3];
        for (int tick = 0; tick < 3; tick++)
        {
            DownedStep step = Step(state, Downed(s_forward), velocity);
            (state, velocity, speeds[tick]) = (step.State, step.Velocity, step.Velocity.X);
            Assert.Equal(LocoMode.Walking, step.ActionLocomotion);
            Assert.Equal(DownedMovementParameters.CrawlSpeed, step.MaxMoveSpeed);
            Assert.Equal(InputDirection.Forward, step.ResultingDirection);
        }

        Assert.Equal(1000.0 / 30, speeds[0], 9);
        Assert.Equal(2000.0 / 30, speeds[1], 9);
        Assert.Equal(100, speeds[2], 9);
        DownedStep first = Step(state, Downed(s_none), velocity);
        Assert.Equal(100 - ((270 + 1000) / 30.0), first.Velocity.X, 9);
        Assert.Equal(LocoMode.Still, first.ActionLocomotion);
        DownedStep second = Step(first.State, Downed(s_none), first.Velocity);
        DownedStep third = Step(second.State, Downed(s_none), second.Velocity);
        Assert.True(second.Velocity.X > 0);
        Assert.Equal(default, third.Velocity);
    }

    [Fact]
    public void The_crawl_faces_the_control_yaw_and_scales_with_the_skill()
    {
        DownedInput input = Downed(s_forward) with { Yaw = Math.PI / 2, SpeedMultiplier = 1.5 };
        DownedStep step = Step(new DownedState(Ticks: 45), input, new Vector3D(0, 150, 0));
        Assert.Equal(Math.PI / 2, step.Yaw);
        Assert.Equal(150, step.MaxMoveSpeed);
        Assert.Equal(150, step.Velocity.Y, 9);
        Assert.Equal(CharacterStance.Dbno, DownedStep.ActionStance);
    }

    [Theory]
    [InlineData(0.1, 0.3 + (0.1 / 0.75 * 2.2))]
    [InlineData(0.2, 0.3 + (0.2 / 0.75 * 2.2))]
    [InlineData(0.3, 1)]
    [InlineData(0.8, 1)]
    [InlineData(1.4, 1)]
    public void Weak_stick_input_is_remapped_and_strong_input_is_full(double strength, double remapped)
    {
        Vector3D result = DownedMovement.Remap(new Vector3D(strength, 0, 0));
        Assert.Equal(remapped, result.X, 9);
        Assert.Equal(default, DownedMovement.Remap(default));
    }

    [Fact]
    public void Uphill_input_on_a_steep_slope_is_weaker()
    {
        const double Radians = 40 * Math.PI / 180;
        Vector3D normal = new(-Math.Sin(Radians), 0, Math.Cos(Radians));
        (Vector3D uphill, double scalar) = DownedMovement.Slope(new Vector3D(1, 0, 0), normal);
        Assert.Equal((float)(1 - (0.35 * 25 / 50)), scalar, 6);
        Assert.Equal(scalar, uphill.X, 9);
        Assert.Equal(1, DownedMovement.Slope(new Vector3D(0, 1, 0), normal).Scalar);
        Assert.Equal(1, DownedMovement.Slope(new Vector3D(-1, 0, 0), normal).Scalar);
        Assert.Null(Step(new DownedState(Ticks: 45), Downed(s_forward)).SlopeInputScalar);
        Assert.Equal(scalar, Step(new DownedState(Ticks: 45), Downed(s_forward) with { GroundNormal = normal }).SlopeInputScalar);
    }

    [Fact]
    public void An_immobilized_character_does_not_crawl_but_keeps_its_direction()
    {
        DownedStep step = Step(new DownedState(Ticks: 45), Downed(s_forward) with { Immobilized = true });
        Assert.Equal(default, step.Velocity);
        Assert.Equal(LocoMode.Still, step.ActionLocomotion);
        Assert.Equal(InputDirection.Forward, step.ResultingDirection);
    }

    [Fact]
    public void Standing_up_holds_two_seconds_then_hands_over_to_ground_movement()
    {
        DownedState state = new(Ticks: 49);
        DownedInput revived = Downed(s_forward) with { Downed = false };
        DownedStep step = Step(state, revived);
        Assert.Equal(DownedPhase.StandingUp, step.Phase);
        Assert.Equal(50, step.State.OutOfDownedTick);
        Assert.NotEqual(0UL, step.GraphTags & GraphStateTags.AnimationPendulumStateDbnoToBaseMove);
        for (int tick = 51; tick <= 110; tick++)
        {
            step = Step(step.State, revived);
            Assert.Null(step.Exit);
        }

        step = Step(step.State, revived);
        Assert.Equal(DownedExit.StoodUp, step.Exit);
        Assert.True(step.Raises(DownedTransientState.Finished));
        Assert.Equal(0UL, step.GraphTags & GraphStateTags.AnimationPendulumStateDbnoToBaseMove);
    }

    [Fact]
    public void Going_down_again_while_standing_up_hands_over_to_the_death_node()
    {
        DownedStep standing = Step(new DownedState(Ticks: 60), Downed(s_none) with { Downed = false });
        DownedStep again = Step(standing.State, Downed(s_none));
        Assert.Equal(DownedExit.DownedAgain, again.Exit);
        Assert.False(again.State.StandingUpTag);
    }

    [Fact]
    public void Used_up_downed_health_holds_the_character_on_walkable_floor_only()
    {
        DownedStep depleted = Step(new DownedState(Ticks: 60), Downed(s_forward) with { HealthDepleted = true });
        Assert.Equal(DownedPhase.Depleted, depleted.Phase);
        Assert.True(depleted.Raises(DownedTransientState.UnknownTransient1));
        DownedStep falling = Step(new DownedState(Ticks: 60), Downed(s_forward) with { HealthDepleted = true, WalkableFloor = false });
        Assert.Equal(DownedPhase.Crawling, falling.Phase);
    }

    [Fact]
    public void Off_walkable_floor_the_character_is_in_the_air_on_the_standing_capsule()
    {
        DownedStep step = Step(new DownedState(Ticks: 60), Downed(s_forward) with { WalkableFloor = false });
        Assert.True(step.InAir);
        Assert.False(step.Crouched);
        Assert.True(step.Raises(DownedTransientState.UnknownTransient0));
        Assert.True(Step(new DownedState(Ticks: 60), Downed(s_forward)).Crouched);
    }

    [Fact]
    public void A_steep_ground_normal_raises_its_transient_state_while_crawling()
    {
        Vector3D steep = new(0.7, 0, Math.Sqrt(1 - 0.49));
        Assert.True(Step(new DownedState(Ticks: 60), Downed(s_none) with { GroundNormal = steep }).Raises(DownedTransientState.UnknownTransient9));
        Assert.False(Step(new DownedState(Ticks: 60), Downed(s_none)).Raises(DownedTransientState.UnknownTransient9));
    }
}
