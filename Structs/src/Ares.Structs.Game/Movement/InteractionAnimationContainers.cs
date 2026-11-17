namespace Ares.Structs.Game.Movement;

/// <summary>
/// The interaction animation containers of the Dam interactions and the character's fallback list: an interaction
/// without a container of its own uses the first entry whose semantic tags it has, else the first entry.
/// </summary>
public static class InteractionAnimationContainers
{
    /// <summary>Container <c>DA_InteractionAnimation_DoorBreach</c>.</summary>
    public static InteractionAnimationContainer DoorBreach { get; } = new(
        Path: "/Game/Pioneer/Characters/Avatar/Animations/DataAssets/DA_InteractionAnimation_DoorBreach.DA_InteractionAnimation_DoorBreach",
        Alignment: InteractionAlignment.LookAtMatrixComponentUp,
        FinishesAlignmentAnimationOnStop: true,
        AllowsFloatingAlignment: false,
        FullBody: false,
        CrouchHeight: 50.0,
        FinishesWithAnimations: false,
        Unending: false);

    /// <summary>Container <c>DA_InteractionAnimation_ExtractionElevatorBox</c>.</summary>
    public static InteractionAnimationContainer ExtractionElevatorBox { get; } = new(
        Path: "/Game/Pioneer/Characters/Avatar/Animations/DataAssets/DA_InteractionAnimation_ExtractionElevatorBox.DA_InteractionAnimation_ExtractionElevatorBox",
        Alignment: InteractionAlignment.ComponentAlignment,
        FinishesAlignmentAnimationOnStop: true,
        AllowsFloatingAlignment: false,
        FullBody: false,
        CrouchHeight: 50.0,
        FinishesWithAnimations: false,
        Unending: false);

    /// <summary>Container <c>DA_InteractionAnimation_KnockOut</c>.</summary>
    public static InteractionAnimationContainer KnockOut { get; } = new(
        Path: "/Game/Pioneer/Characters/Avatar/Animations/DataAssets/DA_InteractionAnimation_KnockOut.DA_InteractionAnimation_KnockOut",
        Alignment: InteractionAlignment.LookAtAlignment,
        FinishesAlignmentAnimationOnStop: false,
        AllowsFloatingAlignment: false,
        FullBody: false,
        CrouchHeight: 50.0,
        FinishesWithAnimations: false,
        Unending: false);

    /// <summary>Container <c>DA_InteractionAnimation_Loot_Electrical</c>.</summary>
    public static InteractionAnimationContainer LootElectrical { get; } = new(
        Path: "/Game/Pioneer/Characters/Avatar/Animations/DataAssets/DA_InteractionAnimation_Loot_Electrical.DA_InteractionAnimation_Loot_Electrical",
        Alignment: InteractionAlignment.None,
        FinishesAlignmentAnimationOnStop: false,
        AllowsFloatingAlignment: false,
        FullBody: false,
        CrouchHeight: 50.0,
        FinishesWithAnimations: true,
        Unending: false);

    /// <summary>Container <c>DA_InteractionAnimation_Loot_Locker</c>.</summary>
    public static InteractionAnimationContainer LootLocker { get; } = new(
        Path: "/Game/Pioneer/Characters/Avatar/Animations/DataAssets/DA_InteractionAnimation_Loot_Locker.DA_InteractionAnimation_Loot_Locker",
        Alignment: InteractionAlignment.None,
        FinishesAlignmentAnimationOnStop: false,
        AllowsFloatingAlignment: false,
        FullBody: false,
        CrouchHeight: 50.0,
        FinishesWithAnimations: true,
        Unending: false);

    /// <summary>Container <c>DA_InteractionAnimation_Loot_OpenCupboard</c>.</summary>
    public static InteractionAnimationContainer LootOpenCupboard { get; } = new(
        Path: "/Game/Pioneer/Characters/Avatar/Animations/DataAssets/DA_InteractionAnimation_Loot_OpenCupboard.DA_InteractionAnimation_Loot_OpenCupboard",
        Alignment: InteractionAlignment.None,
        FinishesAlignmentAnimationOnStop: false,
        AllowsFloatingAlignment: false,
        FullBody: false,
        CrouchHeight: 50.0,
        FinishesWithAnimations: true,
        Unending: false);

    /// <summary>Container <c>DA_InteractionAnimation_Loot_OpenLid</c>.</summary>
    public static InteractionAnimationContainer LootOpenLid { get; } = new(
        Path: "/Game/Pioneer/Characters/Avatar/Animations/DataAssets/DA_InteractionAnimation_Loot_OpenLid.DA_InteractionAnimation_Loot_OpenLid",
        Alignment: InteractionAlignment.None,
        FinishesAlignmentAnimationOnStop: false,
        AllowsFloatingAlignment: false,
        FullBody: false,
        CrouchHeight: 50.0,
        FinishesWithAnimations: true,
        Unending: false);

    /// <summary>Container <c>DA_InteractionAnimation_PryFromAbove</c>.</summary>
    public static InteractionAnimationContainer PryFromAbove { get; } = new(
        Path: "/Game/Pioneer/Characters/Avatar/Animations/DataAssets/DA_InteractionAnimation_PryFromAbove.DA_InteractionAnimation_PryFromAbove",
        Alignment: InteractionAlignment.LookAtAlignment,
        FinishesAlignmentAnimationOnStop: false,
        AllowsFloatingAlignment: false,
        FullBody: false,
        CrouchHeight: 50.0,
        FinishesWithAnimations: false,
        Unending: false);

    /// <summary>Container <c>DA_InteractionAnimation_PryFromBelow</c>.</summary>
    public static InteractionAnimationContainer PryFromBelow { get; } = new(
        Path: "/Game/Pioneer/Characters/Avatar/Animations/DataAssets/DA_InteractionAnimation_PryFromBelow.DA_InteractionAnimation_PryFromBelow",
        Alignment: InteractionAlignment.LookAtAlignment,
        FinishesAlignmentAnimationOnStop: false,
        AllowsFloatingAlignment: false,
        FullBody: false,
        CrouchHeight: 50.0,
        FinishesWithAnimations: false,
        Unending: false);

    /// <summary>Container <c>DA_InteractionAnimation_ScavengeHold</c>.</summary>
    public static InteractionAnimationContainer ScavengeHold { get; } = new(
        Path: "/Game/Pioneer/Interactables/Scavengeables/DA_InteractionAnimation_ScavengeHold.DA_InteractionAnimation_ScavengeHold",
        Alignment: InteractionAlignment.None,
        FinishesAlignmentAnimationOnStop: false,
        AllowsFloatingAlignment: false,
        FullBody: false,
        CrouchHeight: 75.0,
        FinishesWithAnimations: true,
        Unending: false);

    /// <summary>Every container above.</summary>
    public static IReadOnlyList<InteractionAnimationContainer> All { get; } =
    [
        DoorBreach,
        ExtractionElevatorBox,
        KnockOut,
        LootElectrical,
        LootLocker,
        LootOpenCupboard,
        LootOpenLid,
        PryFromAbove,
        PryFromBelow,
        ScavengeHold,
    ];

    /// <summary>The character's fallback containers in order; the first is used when no entry's tags match.</summary>
    public static IReadOnlyList<TaggedInteractionAnimationContainer> TagBased { get; } =
    [
        new([], "/Game/Pioneer/Characters/Avatar/Animations/DataAssets/DA_InteractionAnimation_Loot_Locker.DA_InteractionAnimation_Loot_Locker"),
        new(["Interaction.Semantic.LootContainer.Electrical"], "/Game/Pioneer/Characters/Avatar/Animations/DataAssets/DA_InteractionAnimation_Loot_Electrical.DA_InteractionAnimation_Loot_Electrical"),
        new(["Interaction.Semantic.LootContainer.Cabinet"], "/Game/Pioneer/Characters/Avatar/Animations/DataAssets/DA_InteractionAnimation_Loot_Locker.DA_InteractionAnimation_Loot_Locker"),
        new(["Interaction.Semantic.LootContainer.Locker"], "/Game/Pioneer/Characters/Avatar/Animations/DataAssets/DA_InteractionAnimation_Loot_Locker.DA_InteractionAnimation_Loot_Locker"),
        new(["Interaction.Semantic.LootContainer.Body", "Interaction.Semantic.LootContainer.Arc"], "/Game/Pioneer/Interactables/Scavengeables/DA_InteractionAnimation_ScavengeHold.DA_InteractionAnimation_ScavengeHold"),
        new(["Animation.Interaction.Container.OpenLid"], "/Game/Pioneer/Characters/Avatar/Animations/DataAssets/DA_InteractionAnimation_Loot_OpenLid.DA_InteractionAnimation_Loot_OpenLid"),
        new(["Animation.Interaction.Container"], "/Game/Pioneer/Characters/Avatar/Animations/DataAssets/DA_InteractionAnimation_Loot_OpenCupboard.DA_InteractionAnimation_Loot_OpenCupboard"),
        new(["Interaction.Semantic.TetherRemoval"], "/Game/Pioneer/Interactables/Scavengeables/DA_InteractionAnimation_ScavengeHold.DA_InteractionAnimation_ScavengeHold"),
    ];

    /// <summary>The container with the given object or package path, or null.</summary>
    /// <param name="path">Object path, or package path of a data asset named like its package.</param>
    /// <returns>The container, or null when it is not listed.</returns>
    public static InteractionAnimationContainer? Find(string path) =>
        All.FirstOrDefault(container => container.Path == path || container.Path.StartsWith(path + ".", StringComparison.Ordinal));
}
