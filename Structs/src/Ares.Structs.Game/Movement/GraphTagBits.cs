namespace Ares.Structs.Game.Movement;

/// <summary>A graph state tag's bit: bit <c>Index % 64</c> of tag word <c>Index / 64</c>.</summary>
/// <param name="Tag">The tag name, for example <c>Ability.Crouch</c>.</param>
/// <param name="Index">The tag's position in the character graph's tag list.</param>
public readonly record struct GraphTagBit(string Tag, int Index)
{
    /// <summary>
    /// The tag word holding the bit: 0 for <see cref="PawnBlackboard.GraphTagsField"/>, 1 for
    /// <see cref="PawnBlackboard.GraphUpperTagsField"/>.
    /// </summary>
    public int Word => Index / MovementAssets.GraphTagWordBits;

    /// <summary>The bit within its word.</summary>
    public ulong Mask => 1UL << (Index % MovementAssets.GraphTagWordBits);
}

/// <summary>
/// The bits of the graph state tags a server reads or sets (<see cref="GameNames.GraphTags"/>). The bits follow the
/// character graph's tag list, which the game data holds: build this once from it at startup.
/// </summary>
public sealed class GraphTagBits
{
    /// <summary>Looks up the bit of every tag of <see cref="GameNames.GraphTags"/>.</summary>
    /// <param name="bitOf">The bit of a tag name in the character graph's tag list.</param>
    public GraphTagBits(Func<string, GraphTagBit> bitOf)
    {
        ArgumentNullException.ThrowIfNull(bitOf);
        AbilityBlockContextInteractPendulumMove = bitOf(GameNames.GraphTags.AbilityBlockContextInteractPendulumMove);
        AbilityBlockCrouchPendulumMove = bitOf(GameNames.GraphTags.AbilityBlockCrouchPendulumMove);
        AbilityBlockEquipItemInteract = bitOf(GameNames.GraphTags.AbilityBlockEquipItemInteract);
        AbilityBlockEquipItemPendulumMove = bitOf(GameNames.GraphTags.AbilityBlockEquipItemPendulumMove);
        AbilityBlockFirePrimaryInteract = bitOf(GameNames.GraphTags.AbilityBlockFirePrimaryInteract);
        AbilityBlockFirePrimaryPendulumMove = bitOf(GameNames.GraphTags.AbilityBlockFirePrimaryPendulumMove);
        AbilityBlockFireSecondaryInteract = bitOf(GameNames.GraphTags.AbilityBlockFireSecondaryInteract);
        AbilityBlockFireSecondaryPendulumMove = bitOf(GameNames.GraphTags.AbilityBlockFireSecondaryPendulumMove);
        AbilityBlockInputCrouchPendulumMove = bitOf(GameNames.GraphTags.AbilityBlockInputCrouchPendulumMove);
        AbilityBlockInputSprintPendulumMove = bitOf(GameNames.GraphTags.AbilityBlockInputSprintPendulumMove);
        AbilityBlockJumpInteract = bitOf(GameNames.GraphTags.AbilityBlockJumpInteract);
        AbilityBlockKnockbackPendulumMove = bitOf(GameNames.GraphTags.AbilityBlockKnockbackPendulumMove);
        AbilityBlockReloadInteract = bitOf(GameNames.GraphTags.AbilityBlockReloadInteract);
        AbilityBlockReloadPendulumMove = bitOf(GameNames.GraphTags.AbilityBlockReloadPendulumMove);
        AbilityCrouch = bitOf(GameNames.GraphTags.AbilityCrouch);
        AbilityDodgeRollActive = bitOf(GameNames.GraphTags.AbilityDodgeRollActive);
        AbilityFirePrimaryPendulumWep = bitOf(GameNames.GraphTags.AbilityFirePrimaryPendulumWep);
        AbilityFlashlightActive = bitOf(GameNames.GraphTags.AbilityFlashlightActive);
        AbilityJump = bitOf(GameNames.GraphTags.AbilityJump);
        AbilityJumpPending = bitOf(GameNames.GraphTags.AbilityJumpPending);
        AbilityLandingHeavy = bitOf(GameNames.GraphTags.AbilityLandingHeavy);
        AbilityLandingRecovery = bitOf(GameNames.GraphTags.AbilityLandingRecovery);
        AbilitySprintActive = bitOf(GameNames.GraphTags.AbilitySprintActive);
        AbilityVault = bitOf(GameNames.GraphTags.AbilityVault);
        AbilityZooming = bitOf(GameNames.GraphTags.AbilityZooming);
        AnimationAbilityJumpStarted = bitOf(GameNames.GraphTags.AnimationAbilityJumpStarted);
        AnimationPendulumDisplaySyncedPose = bitOf(GameNames.GraphTags.AnimationPendulumDisplaySyncedPose);
        AnimationPendulumStateDbno = bitOf(GameNames.GraphTags.AnimationPendulumStateDbno);
        AnimationPendulumStateDbnoToBaseMove = bitOf(GameNames.GraphTags.AnimationPendulumStateDbnoToBaseMove);
        AnimationPendulumStateDeathToDbno = bitOf(GameNames.GraphTags.AnimationPendulumStateDeathToDbno);
        AnimationPendulumStateSnapHookActive = bitOf(GameNames.GraphTags.AnimationPendulumStateSnapHookActive);
        AnimationPendulumStateStationaryInteractionNonRootMotion = bitOf(GameNames.GraphTags.AnimationPendulumStateStationaryInteractionNonRootMotion);
        AnimationPendulumStateStationaryInteractionRootMotion = bitOf(GameNames.GraphTags.AnimationPendulumStateStationaryInteractionRootMotion);
        AttributeMovementHeavy = bitOf(GameNames.GraphTags.AttributeMovementHeavy);
        AttributeMovementSuperHeavy = bitOf(GameNames.GraphTags.AttributeMovementSuperHeavy);
        AttributeStaminaBlockRechargePendulumMove = bitOf(GameNames.GraphTags.AttributeStaminaBlockRechargePendulumMove);
        GameplayCueAbilityLadderSlide = bitOf(GameNames.GraphTags.GameplayCueAbilityLadderSlide);
        GameplayEffectAttachedTraversalLadder = bitOf(GameNames.GraphTags.GameplayEffectAttachedTraversalLadder);
        GameplayEffectAttachedTraversalZipline = bitOf(GameNames.GraphTags.GameplayEffectAttachedTraversalZipline);
        StatusExhausted = bitOf(GameNames.GraphTags.StatusExhausted);
        StatusImmobilizedInteract = bitOf(GameNames.GraphTags.StatusImmobilizedInteract);
        UiSkillStateActiveSprintBoost = bitOf(GameNames.GraphTags.UiSkillStateActiveSprintBoost);
    }

    /// <summary>Tag <c>Ability.Block.ContextInteract.PendulumMove</c>.</summary>
    public GraphTagBit AbilityBlockContextInteractPendulumMove { get; }

    /// <summary>Tag <c>Ability.Block.Crouch.PendulumMove</c>.</summary>
    public GraphTagBit AbilityBlockCrouchPendulumMove { get; }

    /// <summary>Tag <c>Ability.Block.EquipItem.Interact</c>.</summary>
    public GraphTagBit AbilityBlockEquipItemInteract { get; }

    /// <summary>Tag <c>Ability.Block.EquipItem.PendulumMove</c>.</summary>
    public GraphTagBit AbilityBlockEquipItemPendulumMove { get; }

    /// <summary>Tag <c>Ability.Block.FirePrimary.Interact</c>.</summary>
    public GraphTagBit AbilityBlockFirePrimaryInteract { get; }

    /// <summary>Tag <c>Ability.Block.FirePrimary.PendulumMove</c>.</summary>
    public GraphTagBit AbilityBlockFirePrimaryPendulumMove { get; }

    /// <summary>Tag <c>Ability.Block.FireSecondary.Interact</c>.</summary>
    public GraphTagBit AbilityBlockFireSecondaryInteract { get; }

    /// <summary>Tag <c>Ability.Block.FireSecondary.PendulumMove</c>.</summary>
    public GraphTagBit AbilityBlockFireSecondaryPendulumMove { get; }

    /// <summary>Tag <c>Ability.BlockInput.Crouch.PendulumMove</c>.</summary>
    public GraphTagBit AbilityBlockInputCrouchPendulumMove { get; }

    /// <summary>Tag <c>Ability.BlockInput.Sprint.PendulumMove</c>.</summary>
    public GraphTagBit AbilityBlockInputSprintPendulumMove { get; }

    /// <summary>Tag <c>Ability.Block.Jump.Interact</c>.</summary>
    public GraphTagBit AbilityBlockJumpInteract { get; }

    /// <summary>Tag <c>Ability.Block.Knockback.PendulumMove</c>.</summary>
    public GraphTagBit AbilityBlockKnockbackPendulumMove { get; }

    /// <summary>Tag <c>Ability.Block.Reload.Interact</c>.</summary>
    public GraphTagBit AbilityBlockReloadInteract { get; }

    /// <summary>Tag <c>Ability.Block.Reload.PendulumMove</c>.</summary>
    public GraphTagBit AbilityBlockReloadPendulumMove { get; }

    /// <summary>Tag <c>Ability.Crouch</c>.</summary>
    public GraphTagBit AbilityCrouch { get; }

    /// <summary>Tag <c>Ability.DodgeRoll.Active</c>.</summary>
    public GraphTagBit AbilityDodgeRollActive { get; }

    /// <summary>Tag <c>Ability.FirePrimary.PendulumWep</c>.</summary>
    public GraphTagBit AbilityFirePrimaryPendulumWep { get; }

    /// <summary>Tag <c>Ability.Flashlight.Active</c>.</summary>
    public GraphTagBit AbilityFlashlightActive { get; }

    /// <summary>Tag <c>Ability.Jump</c>.</summary>
    public GraphTagBit AbilityJump { get; }

    /// <summary>Tag <c>Ability.Jump.Pending</c>.</summary>
    public GraphTagBit AbilityJumpPending { get; }

    /// <summary>Tag <c>Ability.Landing.Heavy</c>.</summary>
    public GraphTagBit AbilityLandingHeavy { get; }

    /// <summary>Tag <c>Ability.Landing.Recovery</c>.</summary>
    public GraphTagBit AbilityLandingRecovery { get; }

    /// <summary>Tag <c>Ability.Sprint.Active</c>.</summary>
    public GraphTagBit AbilitySprintActive { get; }

    /// <summary>Tag <c>Ability.Vault</c>.</summary>
    public GraphTagBit AbilityVault { get; }

    /// <summary>Tag <c>Ability.Zooming</c>.</summary>
    public GraphTagBit AbilityZooming { get; }

    /// <summary>Tag <c>Animation.Ability.Jump.Started</c>.</summary>
    public GraphTagBit AnimationAbilityJumpStarted { get; }

    /// <summary>Tag <c>Animation.Pendulum.DisplaySyncedPose</c>.</summary>
    public GraphTagBit AnimationPendulumDisplaySyncedPose { get; }

    /// <summary>Tag <c>Animation.PendulumState.DBNO</c>.</summary>
    public GraphTagBit AnimationPendulumStateDbno { get; }

    /// <summary>Tag <c>Animation.PendulumState.DBNOToBaseMove</c>.</summary>
    public GraphTagBit AnimationPendulumStateDbnoToBaseMove { get; }

    /// <summary>Tag <c>Animation.PendulumState.DeathToDBNO</c>.</summary>
    public GraphTagBit AnimationPendulumStateDeathToDbno { get; }

    /// <summary>Tag <c>Animation.PendulumState.SnapHookActive</c>.</summary>
    public GraphTagBit AnimationPendulumStateSnapHookActive { get; }

    /// <summary>Tag <c>Animation.PendulumState.StationaryInteraction.NonRootMotion</c>.</summary>
    public GraphTagBit AnimationPendulumStateStationaryInteractionNonRootMotion { get; }

    /// <summary>Tag <c>Animation.PendulumState.StationaryInteraction.RootMotion</c>.</summary>
    public GraphTagBit AnimationPendulumStateStationaryInteractionRootMotion { get; }

    /// <summary>Tag <c>Attribute.Movement.Heavy</c>.</summary>
    public GraphTagBit AttributeMovementHeavy { get; }

    /// <summary>Tag <c>Attribute.Movement.SuperHeavy</c>.</summary>
    public GraphTagBit AttributeMovementSuperHeavy { get; }

    /// <summary>Tag <c>Attribute.Stamina.BlockRecharge.PendulumMove</c>.</summary>
    public GraphTagBit AttributeStaminaBlockRechargePendulumMove { get; }

    /// <summary>Tag <c>GameplayCue.Ability.Ladder.Slide</c>.</summary>
    public GraphTagBit GameplayCueAbilityLadderSlide { get; }

    /// <summary>Tag <c>GameplayEffect.AttachedTraversal.Ladder</c>.</summary>
    public GraphTagBit GameplayEffectAttachedTraversalLadder { get; }

    /// <summary>Tag <c>GameplayEffect.AttachedTraversal.Zipline</c>.</summary>
    public GraphTagBit GameplayEffectAttachedTraversalZipline { get; }

    /// <summary>Tag <c>Status.Exhausted</c>.</summary>
    public GraphTagBit StatusExhausted { get; }

    /// <summary>Tag <c>Status.Immobilized.Interact</c>.</summary>
    public GraphTagBit StatusImmobilizedInteract { get; }

    /// <summary>Tag <c>UI.SkillState.Active.SprintBoost</c>.</summary>
    public GraphTagBit UiSkillStateActiveSprintBoost { get; }
}
