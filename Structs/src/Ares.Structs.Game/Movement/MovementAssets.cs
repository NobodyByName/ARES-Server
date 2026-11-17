namespace Ares.Structs.Game.Movement;

/// <summary>
/// The installed assets that define movement tuning (data assets, animations, the sprint curve, the character graph's
/// logic object and the snap hook Blueprint), and the authoring facts that read them.
/// </summary>
public static class MovementAssets
{
    /// <summary>Frames per second of the movement animations: root motion is keyed, and play lengths are whole frames, at this rate.</summary>
    public const int AnimationSampleRate = 30;

    /// <summary>Dodge-roll metrics data asset (<c>UDodgeRollMetrics</c>), one roll per <see cref="DodgeRollKind"/>.</summary>
    public const string DodgeRollMetricsPackage = "/Game/Pioneer/Core/Abilities/HumanAbilities/DodgeRoll/DA_DodgeRollMetrics";

    /// <summary>Heavy-landing animations data asset (<c>UHeavyLandingAnimations</c>).</summary>
    public const string HeavyLandingAnimationsPackage = "/Game/Pioneer/Characters/Avatar/Animations/DataAssets/DA_HeavyLandingAnimations";

    /// <summary>Vault definitions with an item in hand.</summary>
    public const string ArmedVaultPackage = "/Game/Pioneer/Characters/Avatar/DA_VaultDefinition_Weapon";

    /// <summary>Vault definitions with empty hands.</summary>
    public const string UnarmedVaultPackage = "/Game/Pioneer/Characters/Avatar/DA_VaultDefinitions_Unarmed";

    /// <summary>Vault definitions with an item in hand while exhausted or carrying a heavy load.</summary>
    public const string ArmedExhaustedVaultPackage = "/Game/Pioneer/Characters/Avatar/DA_VaultDefinitions_Weapon_Exhausted";

    /// <summary>Vault definitions with empty hands while exhausted or carrying a heavy load.</summary>
    public const string UnarmedExhaustedVaultPackage = "/Game/Pioneer/Characters/Avatar/DA_VaultDefinitions_Unarmed_Exhausted";

    /// <summary>The character's local simulation logic (<c>UPioneerCharacterLocalSimLogic</c>), whose root export holds the graph's tracked tags.</summary>
    public const string SimulationLogicPackage = "/Game/Pioneer/Core/Blueprints/Player/PioneerLocalSimulationLogic";

    /// <summary>The sprint burst alpha curve asset.</summary>
    public const string SprintBurstCurvePackage = "/Game/Pioneer/Core/Blueprints/Player/Curve_PlayerSprintBurstAlpha";

    /// <summary>The snap hook item Blueprint (a Blueprint of the script class <c>USnaphookItem</c> that declares no members).</summary>
    public const string SnapHookBlueprint = "/Game/Pioneer/Items/MobilityGadgets/SnapHook/BP_MobilityGadget_SnapHook";

    /// <summary>The item data asset the snap hook's class default names.</summary>
    public const string SnapHookDataAsset = "/Game/Pioneer/Items/MobilityGadgets/SnapHook/DA_Item_SnapHook";

    /// <summary>Name prefix of the notify-state objects an animation package holds for its branch tags (<c>UPendulumAnimBranchTag</c>).</summary>
    public const string BranchTagExportPrefix = "PendulumAnimBranchTag_";

    /// <summary>Chain index of a branch tag's transient state id, a byte; a tag that stores nothing raises no state.</summary>
    public const int BranchTagStateIndex = 1;

    /// <summary>Animation time between two vault root path points when a definition does not set it, in seconds.</summary>
    public const float DefaultVaultSampleSeconds = 0.05f;

    /// <summary>Latest vault start time, in animation seconds, when a definition does not set it.</summary>
    public const float DefaultVaultEntrySeconds = 999f;

    /// <summary>Tags per graph-state tag word: tag <c>k</c> is bit <c>k % 64</c> of tag word <c>k / 64</c>.</summary>
    public const int GraphTagWordBits = 64;

    /// <summary>Number of graph-state tag words (<see cref="PawnBlackboard.GraphTagsField"/> and <see cref="PawnBlackboard.GraphUpperTagsField"/>).</summary>
    public const int GraphTagWords = 2;

    /// <summary>A tracked tag whose bit is known from the pawn's graph state: the sprint flag.</summary>
    public const string SprintActiveTag = "Ability.Sprint.Active";

    /// <summary>Tag index of <see cref="SprintActiveTag"/>.</summary>
    public const int SprintActiveTagIndex = 29;

    /// <summary>A tracked tag whose bit is known from the pawn's graph state: the flashlight flag.</summary>
    public const string FlashlightActiveTag = "Ability.Flashlight.Active";

    /// <summary>Tag index of <see cref="FlashlightActiveTag"/>.</summary>
    public const int FlashlightActiveTagIndex = 55;

    /// <summary>The vault definition packages in table order: armed, unarmed, armed exhausted, unarmed exhausted.</summary>
    public static IReadOnlyList<string> VaultPackages { get; } =
        [ArmedVaultPackage, UnarmedVaultPackage, ArmedExhaustedVaultPackage, UnarmedExhaustedVaultPackage];
}
