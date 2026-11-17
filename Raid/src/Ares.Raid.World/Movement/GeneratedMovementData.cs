using Ares.Structs.Game.Movement;

namespace Ares.Raid.World.Movement;

/// <summary>
/// Transitional: the groups of <see cref="GraphStateFlags"/> as constants of the generated Structs tag bits, for the raid
/// code that does not receive the loaded game data yet. Each member is named like its <see cref="GraphStateFlags"/>
/// property; the raid switches to the instance of its game data (<c>GameDataSet.Names.GraphFlags</c>), and this file goes
/// with the generated classes.
/// </summary>
public static class GeneratedGraphStateFlags
{
    /// <summary>See <see cref="GraphStateFlags.BlockedActions"/>.</summary>
    public const ulong BlockedActions = GraphStateTags.AbilityBlockFirePrimaryPendulumMove | GraphStateTags.AbilityBlockFireSecondaryPendulumMove
        | GraphStateTags.AbilityBlockEquipItemPendulumMove | GraphStateTags.AbilityBlockReloadPendulumMove;

    /// <summary>See <see cref="GraphStateFlags.HeavyLandingRecovery"/>.</summary>
    public const ulong HeavyLandingRecovery = BlockedActions | GraphStateTags.AbilityLandingHeavy | GraphStateTags.AnimationPendulumDisplaySyncedPose;

    /// <summary>See <see cref="GraphStateFlags.LadderClimbing"/>.</summary>
    public const ulong LadderClimbing = BlockedActions | GraphStateTags.AbilityBlockContextInteractPendulumMove
        | GraphStateTags.AbilityBlockInputCrouchPendulumMove | GraphStateTags.GameplayEffectAttachedTraversalLadder;

    /// <summary>See <see cref="GraphStateFlags.DodgeRolling"/>.</summary>
    public const ulong DodgeRolling = BlockedActions | GraphStateTags.AbilityBlockContextInteractPendulumMove
        | GraphStateTags.AnimationPendulumDisplaySyncedPose | GraphStateTags.AbilityDodgeRollActive;

    /// <summary>See <see cref="GraphStateFlags.Vaulting"/>.</summary>
    public const ulong Vaulting = BlockedActions | GraphStateTags.AbilityBlockContextInteractPendulumMove
        | GraphStateTags.AbilityBlockCrouchPendulumMove | GraphStateTags.AnimationPendulumDisplaySyncedPose | GraphStateTags.AbilityVault;

    /// <summary>See <see cref="GraphStateFlags.LadderState"/>.</summary>
    public const ulong LadderState = BlockedActions | GraphStateTags.AbilityBlockContextInteractPendulumMove
        | GraphStateTags.GameplayEffectAttachedTraversalLadder;

    /// <summary>See <see cref="GraphStateFlags.LadderCrouchInputBlocked"/>.</summary>
    public const ulong LadderCrouchInputBlocked = GraphStateTags.AbilityBlockInputCrouchPendulumMove;

    /// <summary>See <see cref="GraphStateFlags.LadderSliding"/>.</summary>
    public const ulong LadderSliding = GraphStateTags.GameplayCueAbilityLadderSlide;

    /// <summary>See <see cref="GraphStateFlags.LadderSprintActive"/>.</summary>
    public const ulong LadderSprintActive = GraphStateTags.AbilitySprintActive;

    /// <summary>See <see cref="GraphStateFlags.Simulated"/>.</summary>
    public const ulong Simulated = GraphStateTags.AbilitySprintActive | GraphStateTags.AbilityCrouch
        | GraphStateTags.AttributeStaminaBlockRechargePendulumMove | GraphStateTags.AbilityBlockInputSprintPendulumMove | HeavyLandingRecovery
        | LadderClimbing | DodgeRolling | Vaulting | GraphStateTags.AnimationAbilityJumpStarted
        | GraphStateTags.AbilityJumpPending | GraphStateTags.AbilityFlashlightActive | GraphStateTags.AbilityJump
        | GraphStateTags.AbilityLandingRecovery | LadderSliding;

    /// <summary>See <see cref="GraphStateFlags.HeldItem"/>.</summary>
    public const ulong HeldItem = GraphStateTags.AbilityZooming | GraphStateTags.AbilityFirePrimaryPendulumWep;

    /// <summary>See <see cref="GraphStateFlags.SimulatedUpper"/>.</summary>
    public const ulong SimulatedUpper = GraphStateUpperTags.UiSkillStateActiveSprintBoost | GraphStateUpperTags.StatusExhausted;

    /// <summary>See <see cref="GraphStateFlags.Swinging"/>.</summary>
    public const ulong Swinging = GraphStateTags.AbilityBlockEquipItemPendulumMove;

    /// <summary>See <see cref="GraphStateFlags.SwingStaminaBlock"/>.</summary>
    public const ulong SwingStaminaBlock = GraphStateTags.AttributeStaminaBlockRechargePendulumMove;

    /// <summary>See <see cref="GraphStateFlags.SwingJumpStarted"/>.</summary>
    public const ulong SwingJumpStarted = GraphStateTags.AnimationAbilityJumpStarted;

    /// <summary>See <see cref="GraphStateFlags.InteractionTags"/>.</summary>
    public const ulong InteractionTags = GraphStateTags.AnimationPendulumStateStationaryInteractionNonRootMotion;

    /// <summary>See <see cref="GraphStateFlags.InteractionUpperTags"/>.</summary>
    public const ulong InteractionUpperTags = GraphStateUpperTags.AbilityBlockEquipItemInteract | GraphStateUpperTags.AbilityBlockFirePrimaryInteract
        | GraphStateUpperTags.AbilityBlockFireSecondaryInteract | GraphStateUpperTags.AbilityBlockJumpInteract
        | GraphStateUpperTags.AbilityBlockReloadInteract | GraphStateUpperTags.StatusImmobilizedInteract;

    /// <summary>See <see cref="GraphStateFlags.InteractionRootMotionNodeTags"/>.</summary>
    public const ulong InteractionRootMotionNodeTags = GraphStateTags.AbilityBlockFirePrimaryPendulumMove | GraphStateTags.AbilityBlockFireSecondaryPendulumMove
        | GraphStateTags.AbilityBlockReloadPendulumMove | GraphStateTags.AbilityBlockEquipItemPendulumMove
        | GraphStateTags.AnimationPendulumStateStationaryInteractionRootMotion;

    /// <summary>See <see cref="GraphStateFlags.SnapHookPulling"/>.</summary>
    public const ulong SnapHookPulling = GraphStateTags.AnimationPendulumStateSnapHookActive | GraphStateTags.AbilityBlockEquipItemPendulumMove
        | GraphStateTags.AbilityBlockContextInteractPendulumMove;

    /// <summary>See <see cref="GraphStateFlags.SnapHookCancelling"/>.</summary>
    public const ulong SnapHookCancelling = GraphStateTags.AbilityLandingHeavy;

    /// <summary>See <see cref="GraphStateFlags.ZiplineRiding"/>.</summary>
    public const ulong ZiplineRiding = GraphStateTags.AbilityBlockReloadPendulumMove | GraphStateTags.AbilityBlockContextInteractPendulumMove
        | GraphStateTags.GameplayEffectAttachedTraversalZipline;

    /// <summary>See <see cref="GraphStateFlags.DownedNodeTags"/>.</summary>
    public const ulong DownedNodeTags = GraphStateTags.AnimationPendulumStateDbno | GraphStateTags.AbilityBlockKnockbackPendulumMove
        | GraphStateTags.AbilityBlockFirePrimaryPendulumMove | GraphStateTags.AbilityBlockFireSecondaryPendulumMove
        | GraphStateTags.AbilityBlockReloadPendulumMove;
}

/// <summary>
/// Transitional: the dodge roll and vault selections of the generated Structs movement tables, for the raid code that
/// does not receive the loaded game data yet. The raid switches to <c>GameDataSet.Movement.DodgeRoll</c> and
/// <c>GameDataSet.Movement.Vaults</c>; this file goes with the generated classes.
/// </summary>
public static class GeneratedMovementData
{
    /// <summary>The authored metrics of a roll variant.</summary>
    /// <param name="kind">Roll variant.</param>
    /// <returns>Its sequence.</returns>
    public static DodgeRollSequence DodgeRoll(DodgeRollKind kind) => kind switch
    {
        DodgeRollKind.Standard => DodgeRollSequences.Standard,
        DodgeRollKind.Sprint => DodgeRollSequences.Sprint,
        DodgeRollKind.Exhausted => DodgeRollSequences.Exhausted,
        DodgeRollKind.Ninja => DodgeRollSequences.Ninja,
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    /// <summary>The vault definition set for the character's equipment and condition.</summary>
    /// <param name="armed">Whether an item other than empty hands is held.</param>
    /// <param name="exhausted">Whether the character is exhausted (and exhausted vaults are not disabled) or carries a heavy load.</param>
    /// <returns>The set.</returns>
    public static VaultDefinitionSet Vaults(bool armed, bool exhausted) => (armed, exhausted) switch
    {
        (true, false) => VaultDefinitions.Armed,
        (false, false) => VaultDefinitions.Unarmed,
        (true, true) => VaultDefinitions.ArmedExhausted,
        (false, true) => VaultDefinitions.UnarmedExhausted,
    };
}
