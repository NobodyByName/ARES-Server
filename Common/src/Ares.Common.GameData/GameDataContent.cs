using System.Collections.Immutable;
using Ares.Structs.Backend.Progression;
using Ares.Structs.Game.Inventory;
using Ares.Structs.Game.Loot;
using Ares.Structs.Game.Movement;

namespace Ares.Common.GameData;

/// <summary>
/// The rows of every global table (<see cref="GameTables"/>) and of each map's tables, as read from a game data
/// directory or written by hand for a test. <see cref="GameDataSet.Create"/> checks and indexes them.
/// </summary>
/// <remarks>Each member is named after its table and holds its rows in the table's order.</remarks>
public sealed record GameDataContent
{
    /// <summary><see cref="GameTables.UsableItems"/>.</summary>
    public required ImmutableArray<ItemDefinition> UsableItems { get; init; }

    /// <summary><see cref="GameTables.ItemValues"/>.</summary>
    public ImmutableArray<ItemValue> ItemValues { get; init; } = [];

    /// <summary><see cref="GameTables.WorldItemList"/>.</summary>
    public ImmutableArray<string> WorldItemList { get; init; } = [];

    /// <summary><see cref="GameTables.Augments"/>.</summary>
    public ImmutableArray<AugmentDefinition> Augments { get; init; } = [];

    /// <summary><see cref="GameTables.LoadoutFrames"/>.</summary>
    public ImmutableArray<LoadoutFrameDefinition> LoadoutFrames { get; init; } = [];

    /// <summary><see cref="GameTables.Firearms"/>.</summary>
    public ImmutableArray<FirearmDefinition> Firearms { get; init; } = [];

    /// <summary><see cref="GameTables.Shields"/>.</summary>
    public ImmutableArray<ShieldDefinition> Shields { get; init; } = [];

    /// <summary><see cref="GameTables.MenuAugments"/>.</summary>
    public ImmutableArray<MenuItem> MenuAugments { get; init; } = [];

    /// <summary><see cref="GameTables.MenuCurrency"/>.</summary>
    public ImmutableArray<MenuItem> MenuCurrency { get; init; } = [];

    /// <summary><see cref="GameTables.MenuGadgets"/>.</summary>
    public ImmutableArray<MenuItem> MenuGadgets { get; init; } = [];

    /// <summary><see cref="GameTables.MenuSalvage"/>.</summary>
    public ImmutableArray<MenuItem> MenuSalvage { get; init; } = [];

    /// <summary><see cref="GameTables.MenuShields"/>.</summary>
    public ImmutableArray<MenuItem> MenuShields { get; init; } = [];

    /// <summary><see cref="GameTables.MenuWeapons"/>.</summary>
    public ImmutableArray<MenuItem> MenuWeapons { get; init; } = [];

    /// <summary><see cref="GameTables.DodgeRolls"/>, one row per <see cref="DodgeRollKind"/>.</summary>
    public required ImmutableArray<DodgeRollSequence> DodgeRolls { get; init; }

    /// <summary><see cref="GameTables.VaultDefinitions"/>, the four sets of <see cref="MovementAssets.VaultPackages"/>.</summary>
    public required ImmutableArray<VaultDefinitionSet> VaultDefinitions { get; init; }

    /// <summary><see cref="GameTables.HeavyLandingLevels"/>.</summary>
    public ImmutableArray<HeavyLandingLevel> HeavyLandingLevels { get; init; } = [];

    /// <summary><see cref="GameTables.RecoveryLandingLevels"/>.</summary>
    public ImmutableArray<HeavyLandingLevel> RecoveryLandingLevels { get; init; } = [];

    /// <summary><see cref="GameTables.GraphStateTags"/>.</summary>
    public ImmutableArray<string> GraphStateTags { get; init; } = [];

    /// <summary>The one row of <see cref="GameTables.SnapHookItem"/>.</summary>
    public required SnapHookItemDefaults SnapHookItem { get; init; }

    /// <summary><see cref="GameTables.SprintBurstCurve"/>.</summary>
    public ImmutableArray<WeightedCurveKey> SprintBurstCurve { get; init; } = [];

    /// <summary><see cref="GameTables.LootContainerTypes"/>.</summary>
    public ImmutableArray<LootContainerType> LootContainerTypes { get; init; } = [];

    /// <summary><see cref="GameTables.LootContainerClasses"/>.</summary>
    public ImmutableArray<LootContainerClass> LootContainerClasses { get; init; } = [];

    /// <summary><see cref="GameTables.StaticValueLootTables"/>.</summary>
    public ImmutableArray<StaticValueLootTable> StaticValueLootTables { get; init; } = [];

    /// <summary><see cref="GameTables.SkillLootTables"/>.</summary>
    public ImmutableArray<SkillLootTable> SkillLootTables { get; init; } = [];

    /// <summary><see cref="GameTables.InteractionAnimationContainers"/>.</summary>
    public ImmutableArray<InteractionAnimationContainer> InteractionAnimationContainers { get; init; } = [];

    /// <summary><see cref="GameTables.TaggedInteractionAnimationContainers"/>.</summary>
    public ImmutableArray<TaggedInteractionAnimationContainer> TaggedInteractionAnimationContainers { get; init; } = [];

    /// <summary>The tables of each map.</summary>
    public ImmutableArray<MapContent> Maps { get; init; } = [];
}
