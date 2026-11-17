using Ares.Structs.Core.Assets;
using Ares.Structs.Game.Movement;

namespace Ares.Structs.Game.Loot;

/// <summary>
/// Loot container Blueprints: every container class is a Blueprint below <see cref="BaseBlueprint"/>, whose parent is
/// the script class <see cref="ScriptActor"/>. The script class creates the components (<see cref="RootComponent"/>,
/// the search interaction, the item container, the quest component, the socket mesh); each Blueprint of the chain stores
/// its component templates as differences from its parent's, as subobjects of its class default object.
/// </summary>
public static class LootContainerBlueprintMembers
{
    /// <summary>Package of the Blueprint every container class derives from.</summary>
    public const string BaseBlueprint = "/Game/Pioneer/Items/LootSpawners/SalvageContainers/BP_SalvageContainer";

    /// <summary>Script class the base Blueprint derives from.</summary>
    public const string ScriptActor = "ASalvageContainerSingle";

    /// <summary>Name prefixes of container Blueprint packages.</summary>
    public static IReadOnlyList<string> BlueprintPrefixes { get; } = ["BP_SalvageContainer_", "BP_SocketContainer_"];

    /// <summary>Script name of the native component selection struct, <see cref="ComponentSelection"/>.</summary>
    public const string ComponentSelectionStruct = "FEmbarkComponentSelection";

    /// <summary>
    /// <c>FEmbarkComponentSelection</c>, a native struct naming a component of the actor (the interaction's loot and prompt
    /// proxy selectors): a property block of two names.
    /// </summary>
    public static StructSchema ComponentSelection { get; } = new("EmbarkComponentSelection",
    [
        new PropertyField("Unknown0", PropertyType.Name),
        new PropertyField("Unknown1", PropertyType.Name),
    ]);

    /// <summary>Name of the search interaction component.</summary>
    public const string InteractionComponent = "LootInteractionComponent";

    /// <summary>Name of the item container component.</summary>
    public const string ItemContainerComponent = "ItemContainerComponent";

    /// <summary>Name of the quest component of the search.</summary>
    public const string QuestComponent = "SearchInteractQuestComponent";

    /// <summary>Name of the root component, a static mesh component.</summary>
    public const string RootComponent = "Mesh";

    /// <summary>Name of the static mesh component a socketed container shows.</summary>
    public const string SocketMeshComponent = "SocketLootContainerMesh";

    /// <summary>Script class of <see cref="InteractionComponent"/>.</summary>
    public const string InteractionScriptClass = "ULootInteractionComponent";

    /// <summary>Script class of <see cref="QuestComponent"/>.</summary>
    public const string QuestScriptClass = "UInteractQuestComponent";

    /// <summary>Script path of <see cref="InteractionScriptClass"/>.</summary>
    public const string InteractionClassPath = "/Script/Angelscript.LootInteractionComponent";

    /// <summary>Script path of <see cref="ItemContainerComponentMembers.ScriptClass"/>.</summary>
    public const string ItemContainerClassPath = "/Script/Angelscript.ItemContainerComponent";

    /// <summary>Script path of <see cref="QuestScriptClass"/>.</summary>
    public const string QuestClassPath = "/Script/Angelscript.InteractQuestComponent";

    /// <summary>Bytes closing a script component template's block after its properties.</summary>
    public const int ScriptComponentTrailer = 8;

    /// <summary>Interaction member: the interaction type (<see cref="InteractionTypeEnum"/>).</summary>
    public const string InteractionType = "InteractionType";

    /// <summary>Interaction member: hold time of a first search, in seconds (float).</summary>
    public const string HoldDuration = "HoldDuration";

    /// <summary>Interaction member: whether a first search draws the raider tool.</summary>
    public const string EquipRaiderTool = "bEquipRaiderToolDuringInitialization";

    /// <summary>Interaction member: time to line up with the container before the hold, in seconds (float).</summary>
    public const string AlignmentTime = "AlignmentTime";

    /// <summary>Interaction member: whether the character cannot move while interacting.</summary>
    public const string ForceImmobilize = "bForceImmobilizeDuringInteraction";

    /// <summary>Interaction member: the prompt distance preset (<see cref="PromptDistancePresetEnum"/>).</summary>
    public const string PromptDistancePreset = "PromptDistancePreset";

    /// <summary>Interaction member: the animation set of the search.</summary>
    public const string AnimationContainer = "AnimationContainer";

    /// <summary>Enum of <see cref="InteractionType"/>.</summary>
    public const string InteractionTypeEnum = "EStandardInteractionType";

    /// <summary>The <see cref="InteractionTypeEnum"/> entry that keeps the character still like a hold.</summary>
    public const string ContinuousInteraction = "Continuous";

    /// <summary>Enum of <see cref="PromptDistancePreset"/>.</summary>
    public const string PromptDistancePresetEnum = "EInteractionPromptDistancePreset";

    /// <summary>
    /// The preset whose distance config is <see cref="StandardPreset"/>, or <see cref="StationaryPreset"/> while the
    /// character is immobilized (forced, a hold, or a continuous interaction).
    /// </summary>
    public const string StandardPreset = "Standard";

    /// <summary>The distance config of the standard preset while the character is immobilized.</summary>
    public const string StationaryPreset = "StandardStationary";

    /// <summary>Struct of the prompt distance configs, and namespace of the preset globals.</summary>
    public const string PromptDistanceConfig = "FInteractionPromptDistanceConfig";

    /// <summary>Member of <see cref="PromptDistanceConfig"/> holding the prompt distance.</summary>
    public const string PromptDistance = "PromptDistance";

    /// <summary>Engine setter of the actor's squared net cull distance, called by the script default statements.</summary>
    public const string SetNetCullDistanceSquared = "SetNetCullDistanceSquared";

    /// <summary>Engine setter of the actor's net dormancy, called by the script default statements.</summary>
    public const string SetDefaultNetDormancy = "SetDefaultNetDormancy";

    /// <summary>Engine setter of whether the actor is always relevant, called by the script default statements.</summary>
    public const string SetAlwaysRelevant = "SetbAlwaysRelevant";

    /// <summary>Script function squaring the cull distance passed to <see cref="SetNetCullDistanceSquared"/>.</summary>
    public const string Square = "Square";

    /// <summary><c>ENetDormancy::DORM_Initial</c>: the server sends a dormant actor once, then only its changes.</summary>
    public const byte NetDormancyInitial = 4;

    /// <summary>
    /// Step of a search location as the interactor reports it: each coordinate floored to a multiple of the interactor
    /// field step, <see cref="PawnBlackboard.InteractionTimeRange"/> over 2^23.
    /// </summary>
    public const double SearchLocationStep = PawnBlackboard.InteractionTimeRange / (1 << 23);
}
