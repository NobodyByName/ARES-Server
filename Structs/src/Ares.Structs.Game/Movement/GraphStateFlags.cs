namespace Ares.Structs.Game.Movement;

/// <summary>
/// Groups of graph state tags (<see cref="GraphTagBits"/>) the character holds together, as masks of their tag word,
/// and the slide ticks that carry its slide tags. The bits follow the character graph's tag list: build this once from
/// the resolved bits at startup. Masks named without "Upper" are masks of the graph tag word
/// (<see cref="PawnBlackboard.GraphTagsField"/>), the others of the upper tag word
/// (<see cref="PawnBlackboard.GraphUpperTagsField"/>).
/// </summary>
public sealed class GraphStateFlags
{
    /// <summary>Slide ticks, counted from the first, that carry <see cref="GraphTagBits.AttributeStaminaBlockRechargePendulumMove"/>.</summary>
    public const int SlideStaminaRechargeBlockedTicks = 14;

    /// <summary>
    /// First slide tick, counted from one, that carries <see cref="GraphTagBits.AbilityBlockInputSprintPendulumMove"/>;
    /// it stays until the end of <see cref="SlideStaminaRechargeBlockedTicks"/>.
    /// </summary>
    public const int SlideSprintInputBlockedFirstTick = 9;

    /// <summary>Groups the tags.</summary>
    /// <param name="tags">The resolved bits of the tags.</param>
    /// <exception cref="ArgumentException">A tag is not in the tag word its group belongs to.</exception>
    public GraphStateFlags(GraphTagBits tags)
    {
        ArgumentNullException.ThrowIfNull(tags);
        BlockedActions = Lower(tags.AbilityBlockFirePrimaryPendulumMove, tags.AbilityBlockFireSecondaryPendulumMove, tags.AbilityBlockEquipItemPendulumMove,
            tags.AbilityBlockReloadPendulumMove);
        HeavyLandingRecovery = BlockedActions | Lower(tags.AbilityLandingHeavy, tags.AnimationPendulumDisplaySyncedPose);
        LadderClimbing = BlockedActions | Lower(tags.AbilityBlockContextInteractPendulumMove, tags.AbilityBlockInputCrouchPendulumMove,
            tags.GameplayEffectAttachedTraversalLadder);
        DodgeRolling = BlockedActions | Lower(tags.AbilityBlockContextInteractPendulumMove, tags.AnimationPendulumDisplaySyncedPose, tags.AbilityDodgeRollActive);
        Vaulting = BlockedActions | Lower(tags.AbilityBlockContextInteractPendulumMove, tags.AbilityBlockCrouchPendulumMove,
            tags.AnimationPendulumDisplaySyncedPose, tags.AbilityVault);
        LadderState = BlockedActions | Lower(tags.AbilityBlockContextInteractPendulumMove, tags.GameplayEffectAttachedTraversalLadder);
        LadderCrouchInputBlocked = Lower(tags.AbilityBlockInputCrouchPendulumMove);
        LadderSliding = Lower(tags.GameplayCueAbilityLadderSlide);
        LadderSprintActive = Lower(tags.AbilitySprintActive);
        Simulated = HeavyLandingRecovery | LadderClimbing | DodgeRolling | Vaulting | LadderSliding
            | Lower(tags.AbilitySprintActive, tags.AbilityCrouch, tags.AttributeStaminaBlockRechargePendulumMove, tags.AbilityBlockInputSprintPendulumMove,
                tags.AnimationAbilityJumpStarted, tags.AbilityJumpPending, tags.AbilityFlashlightActive, tags.AbilityJump, tags.AbilityLandingRecovery);
        HeldItem = Lower(tags.AbilityZooming, tags.AbilityFirePrimaryPendulumWep);
        SimulatedUpper = Upper(tags.UiSkillStateActiveSprintBoost, tags.StatusExhausted);
        Swinging = Lower(tags.AbilityBlockEquipItemPendulumMove);
        SwingStaminaBlock = Lower(tags.AttributeStaminaBlockRechargePendulumMove);
        SwingJumpStarted = Lower(tags.AnimationAbilityJumpStarted);
        InteractionTags = Lower(tags.AnimationPendulumStateStationaryInteractionNonRootMotion);
        InteractionUpperTags = Upper(tags.AbilityBlockEquipItemInteract, tags.AbilityBlockFirePrimaryInteract, tags.AbilityBlockFireSecondaryInteract,
            tags.AbilityBlockJumpInteract, tags.AbilityBlockReloadInteract, tags.StatusImmobilizedInteract);
        InteractionRootMotionNodeTags = Lower(tags.AbilityBlockFirePrimaryPendulumMove, tags.AbilityBlockFireSecondaryPendulumMove,
            tags.AbilityBlockReloadPendulumMove, tags.AbilityBlockEquipItemPendulumMove, tags.AnimationPendulumStateStationaryInteractionRootMotion);
        SnapHookPulling = Lower(tags.AnimationPendulumStateSnapHookActive, tags.AbilityBlockEquipItemPendulumMove, tags.AbilityBlockContextInteractPendulumMove);
        SnapHookCancelling = Lower(tags.AbilityLandingHeavy);
        ZiplineRiding = Lower(tags.AbilityBlockReloadPendulumMove, tags.AbilityBlockContextInteractPendulumMove, tags.GameplayEffectAttachedTraversalZipline);
        DownedNodeTags = Lower(tags.AnimationPendulumStateDbno, tags.AbilityBlockKnockbackPendulumMove, tags.AbilityBlockFirePrimaryPendulumMove,
            tags.AbilityBlockFireSecondaryPendulumMove, tags.AbilityBlockReloadPendulumMove);
    }

    /// <summary>Actions a locked movement (heavy-landing recovery, roll, vault, ladder) blocks.</summary>
    public ulong BlockedActions { get; }

    /// <summary>Tags held while the character recovers from a heavy landing.</summary>
    public ulong HeavyLandingRecovery { get; }

    /// <summary>Tags held while the character climbs a ladder.</summary>
    public ulong LadderClimbing { get; }

    /// <summary>Tags held while a roll runs.</summary>
    public ulong DodgeRolling { get; }

    /// <summary>Tags held while a vault runs.</summary>
    public ulong Vaulting { get; }

    /// <summary>
    /// Tags of the ladder state: fire primary, fire secondary, equip, reload and context interaction blocked, and the
    /// attached-to-a-ladder tag.
    /// </summary>
    public ulong LadderState { get; }

    /// <summary>Tag set while crouch input is blocked; each climbing tick renews it for <see cref="LadderParameters.CrouchBlockSeconds"/>.</summary>
    public ulong LadderCrouchInputBlocked { get; }

    /// <summary>Tag of the ladder slide cue: sliding down, or catching the ladder while falling.</summary>
    public ulong LadderSliding { get; }

    /// <summary>Tag of an active sprint; on a ladder it follows the sprint button.</summary>
    public ulong LadderSprintActive { get; }

    /// <summary>Every tag the character simulation decides; the others keep their initial values.</summary>
    public ulong Simulated { get; }

    /// <summary>Tags the held item decides (aiming and its primary use), when the character simulation runs the held item.</summary>
    public ulong HeldItem { get; }

    /// <summary>Every tag of the upper tag word the character simulation decides; the others keep their initial values.</summary>
    public ulong SimulatedUpper { get; }

    /// <summary>Tags while the swing runs: equipping another item is blocked.</summary>
    public ulong Swinging { get; }

    /// <summary>Tag raised while climbing, rappelling or kicking on a rope: stamina does not recharge.</summary>
    public ulong SwingStaminaBlock { get; }

    /// <summary>Tag raised on the tick of a jump off the rope.</summary>
    public ulong SwingJumpStarted { get; }

    /// <summary>Tags of the graph tag word the persistent action layer holds while an immobilizing interaction runs.</summary>
    public ulong InteractionTags { get; }

    /// <summary>Tags of the upper tag word the persistent action layer holds while an immobilizing interaction runs.</summary>
    public ulong InteractionUpperTags { get; }

    /// <summary>Tags the root-motion interact node holds on every tick it runs.</summary>
    public ulong InteractionRootMotionNodeTags { get; }

    /// <summary>Tags while pulled by the snap hook: the snap hook animation state, and equipping and context interactions blocked.</summary>
    public ulong SnapHookPulling { get; }

    /// <summary>Tags of the previous tick that cancel a thrown hook.</summary>
    public ulong SnapHookCancelling { get; }

    /// <summary>Tags of a zipline ride: reload and context interaction blocked, and the attached-to-a-zipline tag.</summary>
    public ulong ZiplineRiding { get; }

    /// <summary>Tags the downed node holds on every tick it runs.</summary>
    public ulong DownedNodeTags { get; }

    private static ulong Lower(params ReadOnlySpan<GraphTagBit> bits) => Mask(0, bits);

    private static ulong Upper(params ReadOnlySpan<GraphTagBit> bits) => Mask(1, bits);

    private static ulong Mask(int word, ReadOnlySpan<GraphTagBit> bits)
    {
        ulong mask = 0;
        foreach (GraphTagBit bit in bits)
        {
            if (bit.Word != word)
            {
                throw new ArgumentException($"Graph state tag {bit.Tag} is in tag word {bit.Word} (bit {bit.Index}), but its group belongs to tag word {word}.",
                    nameof(bits));
            }

            mask |= bit.Mask;
        }

        return mask;
    }
}
