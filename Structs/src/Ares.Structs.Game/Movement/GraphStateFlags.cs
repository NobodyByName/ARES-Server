namespace Ares.Structs.Game.Movement;

/// <summary>
/// Groups of <see cref="GraphStateTags"/> the character holds together, and the slide ticks that carry its slide
/// tags. The roll and vault groups are <see cref="DodgeRollParameters.RollingFlags"/> and
/// <see cref="VaultParameters.VaultingFlags"/>.
/// </summary>
public static class GraphStateFlags
{
    /// <summary>
    /// Slide ticks, counted from the first, that carry <see cref="GraphStateTags.AttributeStaminaBlockRechargePendulumMove"/>.
    /// </summary>
    public const int SlideStaminaRechargeBlockedTicks = 14;

    /// <summary>
    /// First slide tick, counted from one, that carries <see cref="GraphStateTags.AbilityBlockInputSprintPendulumMove"/>;
    /// it stays until the end of <see cref="SlideStaminaRechargeBlockedTicks"/>.
    /// </summary>
    public const int SlideSprintInputBlockedFirstTick = 9;

    /// <summary>Actions a locked movement (heavy-landing recovery, roll, vault, ladder) blocks.</summary>
    public const ulong BlockedActions = GraphStateTags.AbilityBlockFirePrimaryPendulumMove | GraphStateTags.AbilityBlockFireSecondaryPendulumMove
        | GraphStateTags.AbilityBlockEquipItemPendulumMove | GraphStateTags.AbilityBlockReloadPendulumMove;

    /// <summary>Tags held while the character recovers from a heavy landing.</summary>
    public const ulong HeavyLandingRecovery = BlockedActions | GraphStateTags.AbilityLandingHeavy | GraphStateTags.AnimationPendulumDisplaySyncedPose;

    /// <summary>Tags held while the character climbs a ladder.</summary>
    public const ulong LadderClimbing = BlockedActions | GraphStateTags.AbilityBlockContextInteractPendulumMove
        | GraphStateTags.AbilityBlockInputCrouchPendulumMove | GraphStateTags.GameplayEffectAttachedTraversalLadder;

    /// <summary>Every tag the character simulation decides; the others keep their initial values.</summary>
    public const ulong Simulated = GraphStateTags.AbilitySprintActive | GraphStateTags.AbilityCrouch
        | GraphStateTags.AttributeStaminaBlockRechargePendulumMove | GraphStateTags.AbilityBlockInputSprintPendulumMove | HeavyLandingRecovery
        | LadderClimbing | DodgeRollParameters.RollingFlags | VaultParameters.VaultingFlags | GraphStateTags.AnimationAbilityJumpStarted
        | GraphStateTags.AbilityJumpPending | GraphStateTags.AbilityFlashlightActive | GraphStateTags.AbilityJump
        | GraphStateTags.AbilityLandingRecovery | LadderParameters.SlidingFlag;

    /// <summary>Tags the held item decides (aiming and its primary use), when the character simulation runs the held item.</summary>
    public const ulong HeldItem = GraphStateTags.AbilityZooming | GraphStateTags.AbilityFirePrimaryPendulumWep;

    /// <summary>Every tag from 64 the character simulation decides (<see cref="GraphStateUpperTags"/>); the others keep their initial values.</summary>
    public const ulong SimulatedUpper = GraphStateUpperTags.UiSkillStateActiveSprintBoost | GraphStateUpperTags.StatusExhausted;
}
