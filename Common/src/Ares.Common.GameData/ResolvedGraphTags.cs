using System.Runtime.CompilerServices;
using Ares.Structs.Game;

namespace Ares.Common.GameData;

/// <summary>
/// The bits of the graph state tags a server reads or sets (<see cref="GameNames.GraphTags"/>). Generated:
/// <c>GraphStateTags.X</c> was the mask of a word-0 tag and <c>GraphStateUpperTags.X</c> of a word-1 tag; a resolved
/// <see cref="GraphTagBit"/> carries its word.
/// </summary>
public sealed class ResolvedGraphTags
{
    internal ResolvedGraphTags(ResolvedGameNames.Resolver resolver, MovementTuning movement)
    {
        AbilityBlockContextInteractPendulumMove = Tag(resolver, movement, GameNames.GraphTags.AbilityBlockContextInteractPendulumMove);
        AbilityBlockCrouchPendulumMove = Tag(resolver, movement, GameNames.GraphTags.AbilityBlockCrouchPendulumMove);
        AbilityBlockEquipItemInteract = Tag(resolver, movement, GameNames.GraphTags.AbilityBlockEquipItemInteract);
        AbilityBlockEquipItemPendulumMove = Tag(resolver, movement, GameNames.GraphTags.AbilityBlockEquipItemPendulumMove);
        AbilityBlockFirePrimaryInteract = Tag(resolver, movement, GameNames.GraphTags.AbilityBlockFirePrimaryInteract);
        AbilityBlockFirePrimaryPendulumMove = Tag(resolver, movement, GameNames.GraphTags.AbilityBlockFirePrimaryPendulumMove);
        AbilityBlockFireSecondaryInteract = Tag(resolver, movement, GameNames.GraphTags.AbilityBlockFireSecondaryInteract);
        AbilityBlockFireSecondaryPendulumMove = Tag(resolver, movement, GameNames.GraphTags.AbilityBlockFireSecondaryPendulumMove);
        AbilityBlockInputCrouchPendulumMove = Tag(resolver, movement, GameNames.GraphTags.AbilityBlockInputCrouchPendulumMove);
        AbilityBlockInputSprintPendulumMove = Tag(resolver, movement, GameNames.GraphTags.AbilityBlockInputSprintPendulumMove);
        AbilityBlockJumpInteract = Tag(resolver, movement, GameNames.GraphTags.AbilityBlockJumpInteract);
        AbilityBlockKnockbackPendulumMove = Tag(resolver, movement, GameNames.GraphTags.AbilityBlockKnockbackPendulumMove);
        AbilityBlockReloadInteract = Tag(resolver, movement, GameNames.GraphTags.AbilityBlockReloadInteract);
        AbilityBlockReloadPendulumMove = Tag(resolver, movement, GameNames.GraphTags.AbilityBlockReloadPendulumMove);
        AbilityCrouch = Tag(resolver, movement, GameNames.GraphTags.AbilityCrouch);
        AbilityDodgeRollActive = Tag(resolver, movement, GameNames.GraphTags.AbilityDodgeRollActive);
        AbilityFirePrimaryPendulumWep = Tag(resolver, movement, GameNames.GraphTags.AbilityFirePrimaryPendulumWep);
        AbilityFlashlightActive = Tag(resolver, movement, GameNames.GraphTags.AbilityFlashlightActive);
        AbilityJump = Tag(resolver, movement, GameNames.GraphTags.AbilityJump);
        AbilityJumpPending = Tag(resolver, movement, GameNames.GraphTags.AbilityJumpPending);
        AbilityLandingHeavy = Tag(resolver, movement, GameNames.GraphTags.AbilityLandingHeavy);
        AbilityLandingRecovery = Tag(resolver, movement, GameNames.GraphTags.AbilityLandingRecovery);
        AbilitySprintActive = Tag(resolver, movement, GameNames.GraphTags.AbilitySprintActive);
        AbilityVault = Tag(resolver, movement, GameNames.GraphTags.AbilityVault);
        AbilityZooming = Tag(resolver, movement, GameNames.GraphTags.AbilityZooming);
        AnimationAbilityJumpStarted = Tag(resolver, movement, GameNames.GraphTags.AnimationAbilityJumpStarted);
        AnimationPendulumDisplaySyncedPose = Tag(resolver, movement, GameNames.GraphTags.AnimationPendulumDisplaySyncedPose);
        AnimationPendulumStateDbno = Tag(resolver, movement, GameNames.GraphTags.AnimationPendulumStateDbno);
        AnimationPendulumStateDbnoToBaseMove = Tag(resolver, movement, GameNames.GraphTags.AnimationPendulumStateDbnoToBaseMove);
        AnimationPendulumStateDeathToDbno = Tag(resolver, movement, GameNames.GraphTags.AnimationPendulumStateDeathToDbno);
        AnimationPendulumStateSnapHookActive = Tag(resolver, movement, GameNames.GraphTags.AnimationPendulumStateSnapHookActive);
        AnimationPendulumStateStationaryInteractionNonRootMotion = Tag(resolver, movement, GameNames.GraphTags.AnimationPendulumStateStationaryInteractionNonRootMotion);
        AnimationPendulumStateStationaryInteractionRootMotion = Tag(resolver, movement, GameNames.GraphTags.AnimationPendulumStateStationaryInteractionRootMotion);
        AttributeMovementHeavy = Tag(resolver, movement, GameNames.GraphTags.AttributeMovementHeavy);
        AttributeMovementSuperHeavy = Tag(resolver, movement, GameNames.GraphTags.AttributeMovementSuperHeavy);
        AttributeStaminaBlockRechargePendulumMove = Tag(resolver, movement, GameNames.GraphTags.AttributeStaminaBlockRechargePendulumMove);
        GameplayCueAbilityLadderSlide = Tag(resolver, movement, GameNames.GraphTags.GameplayCueAbilityLadderSlide);
        GameplayEffectAttachedTraversalLadder = Tag(resolver, movement, GameNames.GraphTags.GameplayEffectAttachedTraversalLadder);
        GameplayEffectAttachedTraversalZipline = Tag(resolver, movement, GameNames.GraphTags.GameplayEffectAttachedTraversalZipline);
        StatusExhausted = Tag(resolver, movement, GameNames.GraphTags.StatusExhausted);
        StatusImmobilizedInteract = Tag(resolver, movement, GameNames.GraphTags.StatusImmobilizedInteract);
        UiSkillStateActiveSprintBoost = Tag(resolver, movement, GameNames.GraphTags.UiSkillStateActiveSprintBoost);
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

    private static GraphTagBit Tag(ResolvedGameNames.Resolver resolver, MovementTuning movement, string tag, [CallerArgumentExpression(nameof(tag))] string member = "") =>
        resolver.One("graph state tag", tag, movement.FindGraphTag(tag) is { } bit ? [bit] : Array.Empty<GraphTagBit>(), member);
}
