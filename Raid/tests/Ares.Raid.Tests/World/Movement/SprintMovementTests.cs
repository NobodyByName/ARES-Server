using Ares.Raid.World.Movement;
using Ares.Structs.Game.Movement;

namespace Ares.Raid.Tests.World.Movement;

public sealed class SprintMovementTests
{
    [Fact]
    public void Sprint_starts_at_running_speed_reaches_the_burst_and_returns_to_sustained_speed()
    {
        Assert.Equal(300, SprintMovement.MaximumSpeed(300, SprintMovementParameters.Armed, 0));
        double peak = SprintMovementParameters.BurstCurve[1].Time;
        Assert.Equal(660, SprintMovement.MaximumSpeed(300, SprintMovementParameters.Armed, peak), 4);
        Assert.Equal(780, SprintMovement.MaximumSpeed(375, SprintMovementParameters.Unarmed, peak), 4);
        Assert.Equal(550, SprintMovement.MaximumSpeed(300, SprintMovementParameters.Armed, 4), 4);
    }

    [Fact]
    public void A_duration_bonus_holds_the_peak_before_resuming_the_curve()
    {
        double peak = SprintMovementParameters.BurstCurve[1].Time;
        SprintMovementParameters extended = SprintMovementParameters.Armed with { DurationBonus = 0.5 };
        Assert.Equal(SprintMovement.MaximumSpeed(300, SprintMovementParameters.Armed, peak),
            SprintMovement.MaximumSpeed(300, extended, peak + 0.5), 8);
        Assert.True(SprintMovement.MaximumSpeed(300, extended, peak + 0.5)
            > SprintMovement.MaximumSpeed(300, SprintMovementParameters.Armed, peak + 0.5));
    }

    [Fact]
    public void Blocking_the_burst_retains_the_sustained_sprint_speed()
    {
        SprintMovementParameters blocked = SprintMovementParameters.Armed with { BurstAllowed = false };
        Assert.Equal(550, SprintMovement.MaximumSpeed(300, blocked, 0));
        Assert.Equal(550, SprintMovement.MaximumSpeed(300, blocked, 1));
    }

    [Fact]
    public void A_sprint_hold_keeps_the_original_start_time()
    {
        CharacterSimulation simulation = new(default);
        MovementInput movement = new(1, 126, 0, MovementButtons.Sprint);
        CharacterState first = Step(1);
        Assert.Equal(LocoMode.Sprinting, first.Locomotion);
        CharacterState later = default;
        for (uint frame = 2; frame <= 35; frame++)
        {
            later = Step(frame);
        }

        Assert.Equal(first.SprintStartedAt, later.SprintStartedAt);
        Assert.True(later.Velocity.X > GroundMovementParameters.Armed.MaximumSpeed);

        return;

        CharacterState Step(uint frame) => simulation.StepOnGround(new CharacterInput(frame, 0), movement with { Frame = frame },
            0, 1.0 / 30, GroundMovementParameters.Armed, steerAboveMaximum: false, fixBrakingOvershoot: false, sprint: SprintMovementParameters.Armed);
    }
}
