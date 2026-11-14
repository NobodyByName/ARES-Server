using Ares.Structs.Game.Movement;

namespace Ares.Raid.World.Movement;

/// <summary>Movement node whose stamina drain effect is active.</summary>
public enum StaminaDrainSource : byte
{
    /// <summary>Ground sprint (the ground node's sprint drain).</summary>
    GroundSprint = 0,

    /// <summary>Sprinting up a ladder (the ladder node's sprint drain).</summary>
    LadderSprint = 1,
}

/// <summary>The active stamina drain effect.</summary>
/// <param name="Source">Node that applied it.</param>
/// <param name="Ticks">Ticks since it was applied; it executes on ticks that are a multiple of its period.</param>
public readonly record struct StaminaDrain(StaminaDrainSource Source, int Ticks);

/// <summary>Carried load the recharge effect reads.</summary>
/// <param name="Weight">Carried weight attribute.</param>
/// <param name="MaximumWeight">Maximum carried weight attribute.</param>
/// <param name="Buffer">Recharge weight buffer attribute.</param>
public readonly record struct StaminaLoad(double Weight, double MaximumWeight, double Buffer)
{
    /// <summary>The load of a new character carrying nothing.</summary>
    public static StaminaLoad Empty { get; } = new(
        StaminaParameters.StartingWeight,
        StaminaParameters.StartingMaximumWeight,
        StaminaParameters.StartingRechargeWeightBuffer);
}

/// <summary>Stamina state of one character, owned by the server.</summary>
/// <param name="Stamina">Stamina attribute, within [0, <paramref name="Maximum"/>].</param>
/// <param name="Maximum">Maximum stamina attribute.</param>
/// <param name="RechargeRate">Recharge-rate attribute, stamina per second before the load reduces it.</param>
/// <param name="RechargeTicks">Ticks since the recharge effect was applied (when the character spawned).</param>
/// <param name="Drain">The active drain effect, if any.</param>
public readonly record struct StaminaState(float Stamina, float Maximum, float RechargeRate, int RechargeTicks, StaminaDrain? Drain)
{
    /// <summary>State of a newly spawned character, on the tick its recharge effect is applied.</summary>
    public static StaminaState Initial { get; } = new(
        (float)StaminaParameters.StartingStamina,
        (float)StaminaParameters.MaximumStamina,
        (float)StaminaParameters.StartingRechargeRate,
        0,
        null);
}

/// <summary>What the movement did to stamina during one tick.</summary>
/// <param name="Stance">Character stance the traversal component reports.</param>
/// <param name="Locomotion">Locomotion the traversal component reports.</param>
/// <param name="Drain">Node that wants its drain effect active after this tick, or null for none.</param>
/// <param name="Cost">Sum of the stamina costs started this tick (roll, jump, vault, slide).</param>
/// <param name="AimMoving">Whether the equipped item aims while moving (lower walking recharge).</param>
/// <param name="OnLadder">Whether the character is still on a ladder.</param>
/// <param name="HangingOnRope">Whether the character hangs on a rope.</param>
/// <param name="RechargeBlocked">Whether a tag below <c>Attribute.Stamina.BlockRecharge</c> is present for another reason than the drain (for example the slide's).</param>
/// <param name="DrainBlocked">Whether <c>Attribute.Stamina.BlockDrain</c> is present: costs do not apply and the drain does not execute.</param>
/// <param name="Load">Carried load, or null for <see cref="StaminaLoad.Empty"/>.</param>
public readonly record struct StaminaInput(
    CharacterStance Stance,
    LocoMode Locomotion,
    StaminaDrainSource? Drain = null,
    double Cost = 0,
    bool AimMoving = false,
    bool OnLadder = false,
    bool HangingOnRope = false,
    bool RechargeBlocked = false,
    bool DrainBlocked = false,
    StaminaLoad? Load = null);

/// <summary>Result of one stamina tick.</summary>
/// <param name="State">State after the tick.</param>
/// <param name="SprintStartingStamina">Stamina read when a ground sprint applied its drain this tick (the ground node keeps it), else null.</param>
public readonly record struct StaminaStep(StaminaState State, float? SprintStartingStamina);

/// <summary>
/// Stamina as the server's gameplay effects change it: instant costs, the periodic drain while sprinting and the
/// periodic recharge whose rate follows the stance. Ticks are pawn simulation ticks.
/// </summary>
public static class StaminaSimulation
{
    /// <summary>Ticks between two executions of the drain effect.</summary>
    public static int DrainPeriodTicks { get; } = (int)Math.Round(StaminaParameters.DrainPeriodSeconds / PawnBlackboard.TickSeconds);

    /// <summary>Ticks between two executions of the recharge effect.</summary>
    public static int RechargePeriodTicks { get; } = (int)Math.Round(StaminaParameters.RechargePeriodSeconds / PawnBlackboard.TickSeconds);

    /// <summary>Stamina a dodge roll of this kind costs when it starts.</summary>
    /// <param name="kind">Roll variant.</param>
    /// <returns>The cost.</returns>
    public static double DodgeRollCost(DodgeRollKind kind) => DodgeRollParameters.Sequence(kind).StaminaCost;

    /// <summary>Stamina a vault costs when it starts.</summary>
    /// <param name="verticalDistance">Vertical vault distance of the vault query.</param>
    /// <param name="staminaFree">Whether <c>GameplayEffect.StaminaFreeVault</c> is present.</param>
    /// <returns>The cost.</returns>
    public static double VaultCost(double verticalDistance, bool staminaFree = false) =>
        staminaFree ? 0 : Math.Clamp(verticalDistance / StaminaParameters.VaultFullCostHeight, 0, 1) * StaminaParameters.VaultCost;

    /// <summary>Whether the ground node keeps its sprint drain active this tick.</summary>
    /// <param name="onGround">Whether the ground node moves on the ground (not in the air).</param>
    /// <param name="locomotion">Locomotion the ground node derives from its speed.</param>
    /// <param name="carryingSlowed">Whether <c>GameplayEffect.CarryableSlowedMovement</c> is present.</param>
    /// <param name="sprintHeld">Whether the sprint input is held.</param>
    /// <param name="freeSprint">Whether <c>Ability.StaminaFreeSprintAfterSprintDodgeRoll</c> is present.</param>
    /// <returns>True while sprinting on the ground costs stamina.</returns>
    public static bool GroundSprintDrains(bool onGround, LocoMode locomotion, bool carryingSlowed = false, bool sprintHeld = false, bool freeSprint = false) =>
        onGround && !freeSprint && (locomotion == LocoMode.Sprinting || (carryingSlowed && sprintHeld));

    /// <summary>Recharge-rate attribute the traversal component sets for a stance and locomotion.</summary>
    /// <param name="stance">Character stance.</param>
    /// <param name="locomotion">Locomotion.</param>
    /// <param name="aimMoving">Whether the equipped item aims while moving.</param>
    /// <param name="onLadder">Whether the character is still on a ladder.</param>
    /// <param name="hangingOnRope">Whether the character hangs on a rope.</param>
    /// <returns>Stamina per second.</returns>
    public static double RechargeRate(CharacterStance stance, LocoMode locomotion, bool aimMoving = false, bool onLadder = false, bool hangingOnRope = false) =>
        stance switch
        {
            CharacterStance.Stand => locomotion switch
            {
                LocoMode.Still => StaminaParameters.StillRechargeRate,
                LocoMode.Walking => aimMoving ? StaminaParameters.AimWalkingRechargeRate : StaminaParameters.WalkingRechargeRate,
                LocoMode.Running => StaminaParameters.RunningRechargeRate,
                _ => 0,
            },
            CharacterStance.Crouch => locomotion == LocoMode.Still
                ? StaminaParameters.CrouchedStillRechargeRate
                : StaminaParameters.CrouchedMovingRechargeRate,
            CharacterStance.Dbno => StaminaParameters.StillRechargeRate,
            CharacterStance.Slide => StaminaParameters.SlideRechargeRate,
            CharacterStance.Ladder => onLadder ? StaminaParameters.HangingRechargeRate : 0,
            CharacterStance.Uncontrolled => hangingOnRope ? StaminaParameters.HangingRechargeRate : 0,
            _ => 0,
        };

    /// <summary>Stamina one execution of the recharge effect adds.</summary>
    /// <param name="rate">Recharge-rate attribute.</param>
    /// <param name="load">Carried load.</param>
    /// <returns>The change, before clamping.</returns>
    public static float RechargeMagnitude(float rate, StaminaLoad load)
    {
        float limit = (float)load.MaximumWeight + (float)load.Buffer;
        float over = Math.Clamp((float)load.Weight - (float)load.MaximumWeight, 0f, limit);
        float magnitude = rate * (1f - (over / limit));
        return (float)StaminaParameters.RechargeCoefficient * magnitude;
    }

    /// <summary>Graph upper tags a stamina state sets in the pawn frame.</summary>
    /// <param name="exhausted">Whether the character is exhausted.</param>
    /// <returns><see cref="GraphStateUpperTags.StatusExhausted"/> while exhausted, else zero.</returns>
    public static ulong UpperGraphTags(bool exhausted) => exhausted ? GraphStateUpperTags.StatusExhausted : 0;

    /// <summary>Advances stamina by one tick: costs, then the drain, then the recharge rate and the recharge.</summary>
    /// <param name="state">State before the tick.</param>
    /// <param name="input">What the movement did this tick.</param>
    /// <returns>The state after the tick.</returns>
    public static StaminaStep Step(StaminaState state, StaminaInput input)
    {
        float stamina = state.Stamina;
        if (input.Cost != 0 && !input.DrainBlocked)
        {
            stamina = Clamp(stamina + (float)-input.Cost, state.Maximum);
        }

        float? starting = null;
        StaminaDrain? drain = state.Drain;
        if (input.Drain is not { } source)
        {
            drain = null;
        }
        else if (drain is { } active && active.Source == source)
        {
            drain = active with { Ticks = active.Ticks + 1 };
        }
        else
        {
            if (source == StaminaDrainSource.GroundSprint)
            {
                starting = stamina;
            }

            drain = new StaminaDrain(source, 0);
        }

        if (drain is { } running && !input.DrainBlocked && running.Ticks % DrainPeriodTicks == 0)
        {
            stamina = Clamp(stamina + DrainMagnitude(running.Source), state.Maximum);
        }

        float rate = (float)RechargeRate(input.Stance, input.Locomotion, input.AimMoving, input.OnLadder, input.HangingOnRope);
        int rechargeTicks = state.RechargeTicks + 1;
        if (drain is null && !input.RechargeBlocked && rechargeTicks % RechargePeriodTicks == 0)
        {
            stamina = Clamp(stamina + RechargeMagnitude(rate, input.Load ?? StaminaLoad.Empty), state.Maximum);
        }

        return new StaminaStep(new StaminaState(stamina, state.Maximum, rate, rechargeTicks, drain), starting);
    }

    private static float DrainMagnitude(StaminaDrainSource source) => source switch
    {
        StaminaDrainSource.LadderSprint => (float)LadderParameters.SprintStaminaMagnitude,
        _ => (float)StaminaParameters.SprintDrainMagnitude,
    };

    private static float Clamp(float stamina, float maximum) => Math.Clamp(stamina, 0f, maximum);
}
