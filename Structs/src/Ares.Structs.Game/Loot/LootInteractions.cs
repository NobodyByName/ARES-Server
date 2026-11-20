using Ares.Structs.Game.Movement;
using Ares.Structs.Game.World;

namespace Ares.Structs.Game.Loot;

/// <summary>Rules of loot container searches that hold for every container class.</summary>
public static class LootInteractions
{
    /// <summary>Distance the server adds to a prompt distance before an interaction under way ends.</summary>
    public const double ServerPromptLeeway = 75;

    /// <summary>Hold start time of a search interaction nobody holds.</summary>
    public const double NoHoldStartedTime = StandardInteractionDefaults.NoHoldStartedTime;

    /// <summary>Hold time of a container that has been opened: it opens again as soon as the character is lined up.</summary>
    public const float OpenedHoldSeconds = 0;
}

/// <summary>
/// The class defaults of a loot container's search interaction (<c>ULootInteractionComponent</c> and its super classes),
/// its item container (<c>UItemContainerComponent</c>) and its actor (<c>ALootContainerSingle</c>): what a container class
/// whose templates do not set a member takes. The alignment time and the immobilization are
/// <see cref="InteractionMovementParameters.DefaultAlignmentSeconds"/> and
/// <see cref="InteractionMovementParameters.LootSearchImmobilizes"/>; the prompt distances of the standard preset
/// are <see cref="InteractionMovementParameters.StandardPromptDistance"/> and
/// <see cref="InteractionMovementParameters.StationaryPromptDistance"/>.
/// </summary>
public static class LootInteractionDefaults
{
    /// <summary><c>InteractionType</c>.</summary>
    public const StandardInteractionType InteractionType = StandardInteractionDefaults.InteractionType;

    /// <summary><c>HoldDuration</c> of a first search, in seconds.</summary>
    public const double HoldSeconds = StandardInteractionDefaults.HoldSeconds;

    /// <summary><c>bEquipRaiderToolDuringInitialization</c>.</summary>
    public const bool EquipsRaiderTool = false;

    /// <summary><c>PromptDistancePreset</c>: the <see cref="LootContainerBlueprintMembers.StandardPreset"/> entry.</summary>
    public const string PromptDistancePreset = LootContainerBlueprintMembers.StandardPreset;

    /// <summary><c>WaitDelayOnFirstOpen</c>, in seconds.</summary>
    public const double OpenDelaySeconds = 0;

    /// <summary><c>ItemLimit</c>: the slot count of the item container.</summary>
    public const int SlotCount = 8;

    /// <summary><c>bUseLidAnimDurationForOpenDelay</c>.</summary>
    public const bool UsesLidAnimationForOpenDelay = true;

    /// <summary><c>MinOpenDelay</c>, in seconds.</summary>
    public const double MinOpenDelaySeconds = 0.4;
}
