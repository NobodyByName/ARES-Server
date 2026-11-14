using Ares.Structs.Game.Characters;
using Ares.Structs.Game.Replication;

namespace Ares.Structs.Game.Movement;

/// <summary>
/// Stamina rules of a Raider: the attributes of <c>UPioneerCharacterAttributeSet</c>, the stamina gameplay effects
/// (<c>UStaminaCostGameplayEffect</c>, <c>UStaminaDrainGameplayEffect</c>, <c>UStaminaRechargeGameplayEffect</c>) and
/// the costs and drains the movement nodes apply through them. Stamina is a float attribute clamped to
/// [0, <see cref="MaximumStamina"/>]. Values are for a character without skills, consumables or flow state; those
/// scale costs and recharge by multipliers of one by default.
/// </summary>
public static class StaminaParameters
{
    /// <summary>
    /// Seconds between two executions of the drain effect. The effect lasts until removed, executes once when
    /// applied and then once per period; while active it grants <c>Attribute.Stamina.BlockRecharge</c> and it is
    /// inhibited while the character has <c>Attribute.Stamina.BlockDrain</c>.
    /// </summary>
    public const double DrainPeriodSeconds = 0.2;

    /// <summary>
    /// Seconds between two executions of the recharge effect. The effect is one of the character's starting effects,
    /// lasts for its whole life and is inhibited while the character has <c>Attribute.Stamina.BlockRecharge</c> or a
    /// tag below it.
    /// </summary>
    public const double RechargePeriodSeconds = 0.2;

    /// <summary>Coefficient of the recharge effect's modifier: stamina per execution = rate × coefficient.</summary>
    public const double RechargeCoefficient = 0.2;

    /// <summary>Stamina per second a ground sprint drains.</summary>
    public const double SprintCostPerSecond = 6;

    /// <summary>Share of <see cref="SprintCostPerSecond"/> one execution of the ground sprint's drain removes.</summary>
    public const double SprintCostShare = 0.2;

    /// <summary>Stamina a jump costs (<c>HandleJump</c>).</summary>
    public const double JumpCost = 5;

    /// <summary>Stamina a slide costs when it starts (<c>UPendulumSlideNodeData.StaminaCost</c>).</summary>
    public const double SlideCost = 5;

    /// <summary>Stamina a vault of <see cref="VaultFullCostHeight"/> or more costs; lower vaults cost proportionally less.</summary>
    public const double VaultCost = 15;

    /// <summary>Vertical vault distance at and above which a vault costs <see cref="VaultCost"/>.</summary>
    public const double VaultFullCostHeight = 350;

    /// <summary>Stamina at which the exhausted state ends (skill value <c>ExhaustedStateExitThreshold</c>).</summary>
    public const double ExhaustedStateExitThreshold = 20;

    /// <summary>Recharge rate standing still or downed, in stamina per second.</summary>
    public const double StillRechargeRate = 9;

    /// <summary>Recharge rate walking upright.</summary>
    public const double WalkingRechargeRate = 7;

    /// <summary>Recharge rate walking upright while aiming.</summary>
    public const double AimWalkingRechargeRate = 4;

    /// <summary>Recharge rate running upright.</summary>
    public const double RunningRechargeRate = 5.5;

    /// <summary>Recharge rate crouching still.</summary>
    public const double CrouchedStillRechargeRate = 7;

    /// <summary>Recharge rate crouching and moving.</summary>
    public const double CrouchedMovingRechargeRate = 2;

    /// <summary>Recharge rate while sliding.</summary>
    public const double SlideRechargeRate = 4;

    /// <summary>Recharge rate on a ladder, and in the uncontrolled stance while hanging on a rope.</summary>
    public const double HangingRechargeRate = 5.5;

    /// <summary>Stamina a new character starts with.</summary>
    public static double StartingStamina => Attribute(PropertyNames.Stamina);

    /// <summary>Maximum stamina of a new character.</summary>
    public static double MaximumStamina => Attribute(PropertyNames.MaxStamina);

    /// <summary>Recharge-rate attribute of a new character, before the movement sets it from the stance.</summary>
    public static double StartingRechargeRate => Attribute(PropertyNames.StaminaRechargeRate);

    /// <summary>
    /// Recharge weight buffer of a new character. Above the maximum carried weight the recharge is scaled by
    /// 1 − excess / (maximum weight + buffer), the excess clamped to [0, maximum weight + buffer].
    /// </summary>
    public static double StartingRechargeWeightBuffer => Attribute(PropertyNames.StaminaRechargeWeightBuffer);

    /// <summary>Carried weight of a new character.</summary>
    public static double StartingWeight => Attribute(PropertyNames.Weight);

    /// <summary>Maximum carried weight of a new character.</summary>
    public static double StartingMaximumWeight => Attribute(PropertyNames.MaxWeight);

    /// <summary>Stamina change of one execution of a ground sprint's drain effect.</summary>
    public static double SprintDrainMagnitude => -SprintCostPerSecond * SprintCostShare;

    private static double Attribute(string name) =>
        CharacterDefaults.AttributeSets.SelectMany(set => set.Defaults).First(attribute => attribute.Name == name).Value;
}
