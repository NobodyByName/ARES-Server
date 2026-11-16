using Ares.Structs.Net.GameplayTags;

namespace Ares.Structs.Game.Loot;

/// <summary>How the stacks a loot table entry produces are spread over the placements that accept them.</summary>
public enum LootDistributionMethod
{
    /// <summary>No distribution method set.</summary>
    None,

    /// <summary>One placement after another, so every accepting placement gets a stack before any gets a second.</summary>
    RoundRobin,

    /// <summary>Stacks mixed over all accepting placements.</summary>
    MixAllPlacements,
}

/// <summary>What a value loot table entry selects.</summary>
public enum LootItemQueryType
{
    /// <summary>One item, <see cref="StaticValueLootTableEntry.ExactItem"/>.</summary>
    Exact,

    /// <summary>Every item whose drop tags satisfy <see cref="StaticValueLootTableEntry.ItemQuery"/>.</summary>
    Query,
}

/// <summary>An inclusive integer range.</summary>
/// <param name="Min">Lowest value.</param>
/// <param name="Max">Highest value.</param>
public readonly record struct LootRange(int Min, int Max);

/// <summary>One entry of a value loot table: which items, and how each spawned stack looks.</summary>
/// <param name="QueryType">Whether the entry names one item or queries item drop tags.</param>
/// <param name="ItemQuery">
/// Query over an item's drop tags (<c>Item.Drop.*</c>); the entry's items when <paramref name="QueryType"/> is
/// <see cref="LootItemQueryType.Query"/>. Exact entries may keep an unused query.
/// </param>
/// <param name="ExactItem">Item asset name (<c>DA_Item_*</c>) of an exact entry; null when none is set.</param>
/// <param name="QualityLevel">Quality level of the spawned items; -1 lets the game pick any valid quality.</param>
/// <param name="StackSize">Items per stack.</param>
/// <param name="Durability">Durability of a spawned item, in percent.</param>
/// <param name="DurabilityCap">Durability cap of a spawned item, in percent.</param>
/// <param name="Distribution">How the entry's stacks are spread over the accepting placements.</param>
public sealed record StaticValueLootTableEntry(
    LootItemQueryType QueryType,
    GameplayTagQuery? ItemQuery,
    string? ExactItem,
    int QualityLevel,
    LootRange StackSize,
    LootRange Durability,
    LootRange DurabilityCap,
    LootDistributionMethod Distribution)
{
    /// <summary>Whether the entry selects an item.</summary>
    /// <param name="itemAsset">The item's asset name (<c>DA_Item_*</c>).</param>
    /// <param name="dropTags">The item's drop tags (<c>Item.Drop.*</c>).</param>
    /// <returns>True when the entry selects the item.</returns>
    public bool Selects(string itemAsset, IReadOnlyCollection<string> dropTags)
    {
        ArgumentNullException.ThrowIfNull(itemAsset);
        ArgumentNullException.ThrowIfNull(dropTags);
        return QueryType == LootItemQueryType.Exact
            ? string.Equals(ExactItem, itemAsset, StringComparison.OrdinalIgnoreCase)
            : ItemQuery is not null && ItemQuery.Matches(dropTags);
    }
}

/// <summary>A value loot table (<c>StaticValueLootTable_*</c>).</summary>
/// <param name="Name">Asset name.</param>
/// <param name="Path">Package path of the asset.</param>
/// <param name="Entries">Entries in authored order.</param>
public sealed record StaticValueLootTable(string Name, string Path, IReadOnlyList<StaticValueLootTableEntry> Entries);

/// <summary>A weighted target number of item stacks for a loot container.</summary>
/// <param name="Target">Number of stacks.</param>
/// <param name="Weight">Relative weight of this target.</param>
public readonly record struct LootTargetStacks(int Target, double Weight);

/// <summary>One weighted outcome of a skill loot draw.</summary>
/// <param name="Weight">Relative weight.</param>
/// <param name="Item">Item asset name (<c>DA_Item_*</c>); null for the "nothing" outcome.</param>
/// <param name="StackSize">Items in the stack, drawn uniformly.</param>
/// <param name="QualityLevel">Quality level of the item.</param>
public sealed record SkillLootItem(double Weight, string? Item, LootRange StackSize, int QualityLevel);

/// <summary>Skill loot outcomes for the containers a query selects.</summary>
/// <param name="ContainerQuery">Query over the container's tags; a group without one is skipped.</param>
/// <param name="Items">Outcomes; one is drawn by weight.</param>
public sealed record SkillLootGroup(GameplayTagQuery? ContainerQuery, IReadOnlyList<SkillLootItem> Items);

/// <summary>The bonus loot a skill grants when its owner searches a container.</summary>
/// <param name="Skill">Skill tag.</param>
/// <param name="Groups">Groups in order; the first group whose query matches the container draws one outcome.</param>
public sealed record SkillLoot(string Skill, IReadOnlyList<SkillLootGroup> Groups);

/// <summary>A skill loot table (<c>DA_PlayerSkillLootTable_*</c>).</summary>
/// <param name="Name">Asset name.</param>
/// <param name="Path">Package path of the asset.</param>
/// <param name="Skills">Bonus loot per skill.</param>
public sealed record SkillLootTable(string Name, string Path, IReadOnlyList<SkillLoot> Skills);
