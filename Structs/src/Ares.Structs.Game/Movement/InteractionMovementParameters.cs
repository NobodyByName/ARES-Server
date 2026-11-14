using Ares.Structs.Game.Loot;

namespace Ares.Structs.Game.Movement;

/// <summary>
/// What an interaction does to the character's movement while it runs. The interactor layer
/// (<c>UPendulumInteractorNodeData</c>) steps the interaction through its events; while one runs, the ground node lines the
/// character up at <see cref="DefaultDesiredCharacterDistance"/> from it (replacing the player's movement input), and an
/// interaction that immobilizes makes the persistent action layer hold <see cref="Tags"/> and <see cref="UpperTags"/>,
/// which zero the player's movement input. Interactions whose own animation container aligns the character play as
/// root motion in the interact node instead (<see cref="RootMotionNodeTags"/>).
/// </summary>
public static class InteractionMovementParameters
{
    /// <summary>Seconds the character lines up before the interaction (or its hold) starts, unless the component sets its own.</summary>
    public const double DefaultAlignmentSeconds = 0.5;

    /// <summary>Distance from the interaction, horizontally from the feet, the ground node lines the character up at.</summary>
    public const double DefaultDesiredCharacterDistance = 75;

    /// <summary>How far the character may be from <see cref="DefaultDesiredCharacterDistance"/> before it is moved.</summary>
    public const double DefaultDesiredCharacterDistanceTolerance = 25;

    /// <summary>Distance added to the prompt distance within which an interaction starts and keeps running.</summary>
    public const double DistanceLeeway = LootInteractions.ServerPromptLeeway;

    /// <summary>Seconds a requested interaction waits for access before it fails.</summary>
    public const double RequestTimeoutSeconds = 3;

    /// <summary>Movement input strength from which a change of input direction cancels an immobilizing interaction.</summary>
    public const double CancelInputStrength = 0.75;

    /// <summary>Height of the interaction above the feet from which the crouch button no longer crouches the character.</summary>
    public const double NoCrouchHeight = 150;

    /// <summary>Horizontal distance from the interaction within which an aligned interaction enters the root-motion node.</summary>
    public const double MarkerFarDistance = 375;

    /// <summary>Prompt distance of an interaction that does not immobilize (<c>Standard</c> preset).</summary>
    public const double StandardPromptDistance = 135;

    /// <summary>Prompt distance of an interaction that immobilizes (<c>StandardStationary</c> preset).</summary>
    public const double StationaryPromptDistance = 195;

    /// <summary>Alignment of the doors' open and close buttons (<c>ASimpleDoorActor</c>).</summary>
    public const double DoorButtonAlignmentSeconds = 0.2;

    /// <summary>Whether a loot container's search immobilizes the character even without a hold.</summary>
    public const bool LootSearchImmobilizes = true;

    /// <summary>Semantic tag of a loot container's first search, which picks its fallback animation container.</summary>
    public const string LootSearchSemanticTag = "Animation.Interaction.Container";

    /// <summary>Tags 0–63 the persistent action layer holds while an immobilizing interaction runs.</summary>
    public const ulong Tags = GraphStateTags.AnimationPendulumStateStationaryInteractionNonRootMotion;

    /// <summary>Tags 64–111 the persistent action layer holds while an immobilizing interaction runs.</summary>
    public const ulong UpperTags = GraphStateUpperTags.AbilityBlockEquipItemInteract | GraphStateUpperTags.AbilityBlockFirePrimaryInteract
        | GraphStateUpperTags.AbilityBlockFireSecondaryInteract | GraphStateUpperTags.AbilityBlockJumpInteract
        | GraphStateUpperTags.AbilityBlockReloadInteract | GraphStateUpperTags.StatusImmobilizedInteract;

    /// <summary>Tags the root-motion interact node holds on every tick it runs.</summary>
    public const ulong RootMotionNodeTags = GraphStateTags.AbilityBlockFirePrimaryPendulumMove | GraphStateTags.AbilityBlockFireSecondaryPendulumMove
        | GraphStateTags.AbilityBlockReloadPendulumMove | GraphStateTags.AbilityBlockEquipItemPendulumMove
        | GraphStateTags.AnimationPendulumStateStationaryInteractionRootMotion;

    /// <summary>Allowance for rounding in summed step times when comparing interaction times.</summary>
    public const double ElapsedTolerance = 1e-6;
}
