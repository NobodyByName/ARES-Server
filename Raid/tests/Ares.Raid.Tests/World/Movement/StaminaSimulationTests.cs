using Ares.Raid.World.Movement;
using Ares.Structs.Game.Movement;

namespace Ares.Raid.Tests.World.Movement;

public sealed class StaminaSimulationTests
{
    private const float Precision = 1e-4f;
    private static readonly StaminaInput s_still = new(CharacterStance.Stand, LocoMode.Still);
    private static readonly StaminaInput s_sprinting = new(CharacterStance.Stand, LocoMode.Sprinting, StaminaDrainSource.GroundSprint);

    [Fact]
    public void A_new_character_starts_full_and_both_effects_run_every_six_ticks()
    {
        Assert.Equal(100f, StaminaState.Initial.Stamina);
        Assert.Equal(100f, StaminaState.Initial.Maximum);
        Assert.Equal(6, StaminaSimulation.DrainPeriodTicks);
        Assert.Equal(6, StaminaSimulation.RechargePeriodTicks);
    }

    [Fact]
    public void A_ground_sprint_drains_once_when_it_starts_then_every_period_and_stops_the_recharge()
    {
        StaminaState state = StaminaState.Initial;
        StaminaStep step = StaminaSimulation.Step(state, s_sprinting);
        Assert.Equal(100f, step.SprintStartingStamina);
        Assert.Equal(98.8f, step.State.Stamina, Precision);
        state = step.State;
        for (int tick = 1; tick < 30; tick++)
        {
            step = StaminaSimulation.Step(state, s_sprinting);
            Assert.Null(step.SprintStartingStamina);
            state = step.State;
        }

        Assert.Equal(94f, state.Stamina, Precision);
        Assert.Equal(0f, state.RechargeRate);
    }

    [Fact]
    public void Standing_still_recharges_nine_per_second_on_the_recharge_period()
    {
        StaminaState state = StaminaState.Initial with { Stamina = 50 };
        for (int tick = 0; tick < 30; tick++)
        {
            state = StaminaSimulation.Step(state, s_still).State;
        }

        Assert.Equal(59f, state.Stamina, Precision);
        Assert.Equal(30, state.RechargeTicks);
    }

    [Theory]
    [InlineData(CharacterStance.Stand, LocoMode.Still, false, 9)]
    [InlineData(CharacterStance.Stand, LocoMode.Walking, false, 7)]
    [InlineData(CharacterStance.Stand, LocoMode.Walking, true, 4)]
    [InlineData(CharacterStance.Stand, LocoMode.Running, false, 5.5)]
    [InlineData(CharacterStance.Stand, LocoMode.Sprinting, false, 0)]
    [InlineData(CharacterStance.Crouch, LocoMode.Still, false, 7)]
    [InlineData(CharacterStance.Crouch, LocoMode.Walking, false, 2)]
    [InlineData(CharacterStance.Dbno, LocoMode.Walking, false, 9)]
    [InlineData(CharacterStance.Slide, LocoMode.Running, false, 4)]
    [InlineData(CharacterStance.InAir, LocoMode.Running, false, 0)]
    public void The_recharge_rate_follows_the_stance_and_locomotion(CharacterStance stance, LocoMode locomotion, bool aimMoving, double rate) =>
        Assert.Equal(rate, StaminaSimulation.RechargeRate(stance, locomotion, aimMoving));

    [Fact]
    public void Ladders_and_ropes_recharge_only_while_the_character_hangs_on_them()
    {
        Assert.Equal(5.5, StaminaSimulation.RechargeRate(CharacterStance.Ladder, LocoMode.Still, onLadder: true));
        Assert.Equal(0, StaminaSimulation.RechargeRate(CharacterStance.Ladder, LocoMode.Still));
        Assert.Equal(5.5, StaminaSimulation.RechargeRate(CharacterStance.Uncontrolled, LocoMode.Still, hangingOnRope: true));
        Assert.Equal(0, StaminaSimulation.RechargeRate(CharacterStance.Uncontrolled, LocoMode.Still));
    }

    [Fact]
    public void Rolls_jumps_slides_and_vaults_cost_their_amounts()
    {
        Assert.Equal(20, StaminaSimulation.DodgeRollCost(DodgeRollKind.Standard));
        Assert.Equal(40, StaminaSimulation.DodgeRollCost(DodgeRollKind.Sprint));
        Assert.Equal(20, StaminaSimulation.DodgeRollCost(DodgeRollKind.Exhausted));
        Assert.Equal(5, StaminaParameters.JumpCost);
        Assert.Equal(5, StaminaParameters.SlideCost);
        Assert.Equal(7.5, StaminaSimulation.VaultCost(175));
        Assert.Equal(15, StaminaSimulation.VaultCost(400));
        Assert.Equal(0, StaminaSimulation.VaultCost(400, staminaFree: true));
    }

    [Fact]
    public void A_cost_never_takes_stamina_below_zero_and_the_recharge_never_above_the_maximum()
    {
        StaminaState low = StaminaState.Initial with { Stamina = 30 };
        Assert.Equal(0f, StaminaSimulation.Step(low, s_still with { Cost = StaminaSimulation.DodgeRollCost(DodgeRollKind.Sprint) }).State.Stamina);

        StaminaState nearlyFull = StaminaState.Initial with { Stamina = 99.5f, RechargeTicks = 5 };
        Assert.Equal(100f, StaminaSimulation.Step(nearlyFull, s_still).State.Stamina);
    }

    [Fact]
    public void Blocking_drain_cancels_costs_and_drain_executions_but_the_drain_still_blocks_the_recharge()
    {
        StaminaState state = StaminaState.Initial with { Stamina = 50, RechargeTicks = 5 };
        StaminaStep step = StaminaSimulation.Step(state, s_sprinting with { Cost = StaminaParameters.JumpCost, DrainBlocked = true });
        Assert.Equal(50f, step.State.Stamina);
        Assert.NotNull(step.State.Drain);
    }

    [Fact]
    public void The_slides_recharge_block_skips_a_recharge_execution()
    {
        StaminaState state = StaminaState.Initial with { Stamina = 50, RechargeTicks = 5 };
        StaminaInput sliding = new(CharacterStance.Slide, LocoMode.Running, RechargeBlocked: true);
        Assert.Equal(50f, StaminaSimulation.Step(state, sliding).State.Stamina);
        Assert.Equal(50.8f, StaminaSimulation.Step(state, sliding with { RechargeBlocked = false }).State.Stamina, Precision);
    }

    [Fact]
    public void Weight_above_the_maximum_slows_the_recharge()
    {
        StaminaLoad heavy = new(150, 100, 0);
        Assert.Equal(1.8f, StaminaSimulation.RechargeMagnitude(9, StaminaLoad.Empty), Precision);
        Assert.Equal(0.9f, StaminaSimulation.RechargeMagnitude(9, heavy), Precision);
        Assert.Equal(0f, StaminaSimulation.RechargeMagnitude(9, heavy with { Weight = 250 }), Precision);
    }

    [Fact]
    public void Sprinting_drains_only_on_the_ground_and_without_a_free_sprint()
    {
        Assert.True(StaminaSimulation.GroundSprintDrains(onGround: true, LocoMode.Sprinting));
        Assert.False(StaminaSimulation.GroundSprintDrains(onGround: false, LocoMode.Sprinting));
        Assert.False(StaminaSimulation.GroundSprintDrains(onGround: true, LocoMode.Running));
        Assert.False(StaminaSimulation.GroundSprintDrains(onGround: true, LocoMode.Sprinting, freeSprint: true));
        Assert.True(StaminaSimulation.GroundSprintDrains(onGround: true, LocoMode.Running, carryingSlowed: true, sprintHeld: true));
    }

    [Fact]
    public void Moving_from_a_ground_sprint_to_a_ladder_sprint_applies_a_new_drain()
    {
        StaminaState state = StaminaSimulation.Step(StaminaState.Initial, s_sprinting).State;
        StaminaStep step = StaminaSimulation.Step(state, new StaminaInput(CharacterStance.Ladder, LocoMode.Sprinting, StaminaDrainSource.LadderSprint, OnLadder: true));
        Assert.Equal(new StaminaDrain(StaminaDrainSource.LadderSprint, 0), step.State.Drain);
        Assert.Null(step.SprintStartingStamina);
        Assert.Equal(97.6f, step.State.Stamina, Precision);
    }

    [Fact]
    public void Exhaustion_sets_its_upper_graph_tag()
    {
        Assert.Equal(GraphStateUpperTags.StatusExhausted, StaminaSimulation.UpperGraphTags(exhausted: true));
        Assert.Equal(0UL, StaminaSimulation.UpperGraphTags(exhausted: false));
        Assert.Equal(20, StaminaParameters.ExhaustedStateExitThreshold);
    }
}
