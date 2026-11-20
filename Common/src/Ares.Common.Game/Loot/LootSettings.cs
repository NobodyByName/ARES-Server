using Ares.Structs.Game.Inventory;
using Ares.Structs.Game.Loot;
using Ares.Structs.Game.World;

namespace Ares.Common.Game.Loot;

/// <summary>
/// How loot containers spawn (<c>loot-spawn.json</c>). Every socket rolls the chance of its tag times the multiplier of
/// the loot value of the area around it (at most 1); a successful roll still yields no container when the socket's
/// group is full (<see cref="GroupCaps"/>) or a placement limiter around it is (<see cref="Limiters"/>). Placed
/// containers roll the chance of their class's tag. Dynamic loot spawners place only the listed classes.
/// </summary>
/// <param name="Chances">Chance from 0 to 1 by container tag; a tag without an entry uses its nearest parent's, else 0.</param>
/// <param name="AreaMultipliers">
/// Factor on the chance by the loot value of the area around a socket; a value without an entry uses the one of
/// <see cref="PointOfInterestLootValue.Unknown"/> (outside every area with a loot value), else 1.
/// </param>
/// <param name="GroupCaps">Whether the game's most containers per group (<see cref="LootContainerType.MaxPerGroup"/>) apply.</param>
/// <param name="MaxPerGroup">Most containers of a type per group when the type does not set its own <see cref="LootContainerType.MaxPerGroup"/>.</param>
/// <param name="Limiters">Whether the map's placement limiters cap the sockets they cover.</param>
/// <param name="LimiterDefaults">What a placement limiter and its limit entries use for the members the map leaves unset.</param>
/// <param name="SpawnerClasses">Container classes (class paths) dynamic loot spawners may place.</param>
/// <param name="SpawnerCount">Containers a dynamic loot spawner places per run when it does not set its own <see cref="DynamicLootSpawner.Count"/>.</param>
/// <param name="LooseChances">
/// Chance from 0 to 1 that a loose item socket gets an item, by socket tag; a tag without an entry uses its nearest
/// parent's, else 0. The area multipliers and the limiters apply as for container sockets.
/// </param>
public sealed record LootSpawnSettings(
    IReadOnlyDictionary<string, double> Chances,
    IReadOnlyDictionary<PointOfInterestLootValue, double> AreaMultipliers,
    bool GroupCaps,
    int MaxPerGroup,
    bool Limiters,
    LootLimiterDefaults LimiterDefaults,
    IReadOnlySet<string> SpawnerClasses,
    int SpawnerCount,
    IReadOnlyDictionary<string, double> LooseChances)
{
    /// <summary>The chance of a container tag: its own entry, else its nearest parent's, else 0.</summary>
    /// <param name="tag">The container tag.</param>
    /// <returns>The chance from 0 to 1.</returns>
    public double Chance(string tag) => Nearest(Chances, tag);

    /// <summary>The chance of a loose item socket tag: its own entry, else its nearest parent's, else 0.</summary>
    /// <param name="tag">The loose item socket's tag.</param>
    /// <returns>The chance from 0 to 1.</returns>
    public double LooseChance(string tag) => Nearest(LooseChances, tag);

    /// <summary>The chance of a loose item socket tag in an area of a loot value: the tag's chance times the value's factor, at most 1.</summary>
    /// <param name="tag">The loose item socket's tag.</param>
    /// <param name="value">The loot value of the area around the socket.</param>
    /// <returns>The chance from 0 to 1.</returns>
    public double LooseChance(string tag, PointOfInterestLootValue value) => Math.Min(1, LooseChance(tag) * Multiplier(value));

    /// <summary>The factor of a loot value: its own entry, else the one of an area without a loot value, else 1.</summary>
    /// <param name="value">The loot value of the area around a container.</param>
    /// <returns>The factor on a tag's chance.</returns>
    public double Multiplier(PointOfInterestLootValue value) =>
        AreaMultipliers.TryGetValue(value, out double factor) ? factor : AreaMultipliers.GetValueOrDefault(PointOfInterestLootValue.Unknown, 1);

    /// <summary>The chance of a container tag in an area of a loot value: the tag's chance times the value's factor, at most 1.</summary>
    /// <param name="tag">The container tag.</param>
    /// <param name="value">The loot value of the area around the container.</param>
    /// <returns>The chance from 0 to 1.</returns>
    public double Chance(string tag, PointOfInterestLootValue value) => Math.Min(1, Chance(tag) * Multiplier(value));

    // The entry of a tag, else of its nearest parent, else 0.
    private static double Nearest(IReadOnlyDictionary<string, double> chances, string tag)
    {
        ArgumentNullException.ThrowIfNull(tag);
        for (string current = tag; current.Length > 0; current = current.LastIndexOf('.') is var dot and > 0 ? current[..dot] : string.Empty)
        {
            if (chances.TryGetValue(current, out double chance))
            {
                return chance;
            }
        }

        return 0;
    }
}

/// <summary>What a loot placement limiter uses for the members its map leaves unset (<see cref="LootLimiter"/>, <see cref="LootLimit"/>).</summary>
/// <param name="Shape">Which points the limiter covers.</param>
/// <param name="Radius">Sphere radius, or twice the box's half extent, in centimetres.</param>
/// <param name="Unlimited">Whether a limit entry caps nothing.</param>
/// <param name="Limit">Placements a limit entry allows for its tag.</param>
public sealed record LootLimiterDefaults(LootLimiterShape Shape, double Radius, bool Unlimited, int Limit)
{
    /// <summary>The limiter with every member it leaves unset, and every such member of its limit entries, set to these defaults.</summary>
    /// <param name="limiter">A limiter of the map's data.</param>
    /// <returns>The limiter with every member set.</returns>
    public LootLimiter Apply(LootLimiter limiter)
    {
        ArgumentNullException.ThrowIfNull(limiter);
        return limiter with
        {
            Shape = limiter.Shape ?? Shape,
            Radius = limiter.Radius ?? Radius,
            Limits = [.. limiter.Limits.Select(limit => limit with { Unlimited = limit.Unlimited ?? Unlimited, Limit = limit.Limit ?? Limit })],
        };
    }
}

/// <summary>A tier of loot items: how often a container's next stack comes from it.</summary>
/// <param name="Name">Name of the tier.</param>
/// <param name="DropTag">Drop tag of its items (an item with this tag or one below it); null for the tier of every other item.</param>
/// <param name="Weight">Relative weight.</param>
/// <param name="AreaMultipliers">Factor on the weight by the loot value of the area around the container; 1 without an entry.</param>
public sealed record LootTier(string Name, string? DropTag, double Weight, IReadOnlyDictionary<PointOfInterestLootValue, double> AreaMultipliers);

/// <summary>
/// The value loot tables a container category draws from. An item of these tables may go in a container only when its
/// own placement list also accepts the container's tag.
/// </summary>
/// <param name="Name">Name of the pool, as logs show it.</param>
/// <param name="Tag">Container tag of the category: the tag itself and every tag below it that has no pool of its own.</param>
/// <param name="Tables">Value loot table asset names; an item's stack size and durability come from the first entry that selects it.</param>
/// <param name="Stacks">Number of stacks, drawn uniformly; null for the weighted targets (<see cref="LootContentSettings.TargetStacks"/>).</param>
/// <param name="ExtraStack">Whether the container may get the extra stack (<see cref="LootContentSettings.ExtraStack"/>).</param>
public sealed record LootTablePool(string Name, string Tag, IReadOnlyList<string> Tables, LootRange? Stacks, bool ExtraStack);

/// <summary>Items chosen by their drop tags, and how each stack of them looks.</summary>
/// <param name="DropTag">Drop tag of the items: an item with this tag or one below it.</param>
/// <param name="ExcludedDropTags">Items with one of these drop tags (or one below it) are left out.</param>
/// <param name="StackSize">Items per stack, drawn uniformly and capped at the item's largest stack.</param>
/// <param name="Weight">Relative weight among the entries of its pool.</param>
public sealed record LootTypeEntry(string DropTag, IReadOnlyList<string> ExcludedDropTags, LootRange StackSize, double Weight);

/// <summary>
/// Contents of containers no value loot table fills: items chosen by drop tags, with no placement check. Avoid groups do
/// not apply; an item still appears once per container.
/// </summary>
/// <param name="Name">Name of the pool, as logs show it.</param>
/// <param name="Classes">Container class paths that use the pool.</param>
/// <param name="Tags">Container tags that use the pool: the tag itself and every tag below it.</param>
/// <param name="Stacks">Number of stacks, drawn uniformly.</param>
/// <param name="Entries">The entries; each stack draws one by weight, then one of its items evenly.</param>
public sealed record LootTypePool(string Name, IReadOnlyList<string> Classes, IReadOnlyList<string> Tags, LootRange Stacks, IReadOnlyList<LootTypeEntry> Entries);

/// <summary>One extra stack some containers of table pools get.</summary>
/// <param name="Chance">Chance from 0 to 1 that a container of a pool with <see cref="LootTablePool.ExtraStack"/> gets it.</param>
/// <param name="Entries">What it draws from.</param>
public sealed record LootExtraStack(double Chance, IReadOnlyList<LootTypeEntry> Entries);

/// <summary>
/// What loot containers hold (<c>loot-contents.json</c>). A container class or tag with a type pool uses it, otherwise
/// its tag's table pool: a number of stacks, the first from <see cref="FirstStackTier"/> when the pool has such an
/// item, then by tier weight (scaled by the area's loot value), each item at most once and never next to an item of a
/// group either one avoids; then maybe the extra stack.
/// </summary>
/// <param name="ExcludedDropTags">Items with one of these drop tags (or one below it) go in no container.</param>
/// <param name="Tiers">The tiers, in order: an item belongs to the first whose drop tag it carries, else to the one without a drop tag.</param>
/// <param name="FirstStackTier">Name of the tier of a container's first stack, or null for none.</param>
/// <param name="TargetStacks">Weighted numbers of stacks of a table pool's container that sets no number of its own; never empty.</param>
/// <param name="TablePools">Table pools by container category.</param>
/// <param name="ExtraStack">The extra stack.</param>
/// <param name="TypePools">Type pools.</param>
/// <param name="Profiles">Contents profiles by container or loose item socket type; the other members are the defaults they change.</param>
/// <param name="LooseItems">What loose item sockets hold.</param>
public sealed record LootContentSettings(
    IReadOnlyList<string> ExcludedDropTags,
    IReadOnlyList<LootTier> Tiers,
    string? FirstStackTier,
    IReadOnlyList<LootTargetStacks> TargetStacks,
    IReadOnlyList<LootTablePool> TablePools,
    LootExtraStack ExtraStack,
    IReadOnlyList<LootTypePool> TypePools,
    IReadOnlyList<LootProfile> Profiles,
    LooseItemSettings LooseItems);

/// <summary>Items a profile entry concerns: those carrying a drop tag (or one below it), or one item. Exactly one is set.</summary>
/// <param name="DropTag">The drop tag, or null.</param>
/// <param name="Item">The item's technical name, or null.</param>
public sealed record LootMatch(string? DropTag, string? Item)
{
    /// <summary>Whether the match covers an item.</summary>
    /// <param name="item">The item.</param>
    /// <returns>True when the item carries the drop tag (or one below it), or is the named item.</returns>
    public bool Matches(ItemDefinition item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return DropTag is { } tag
            ? item.DropTags.Any(drop => LootTags.IsTagOrBelow(drop, tag))
            : string.Equals(item.TechnicalName, Item, StringComparison.Ordinal);
    }

    /// <summary>Whether two matches are the same key: the same drop tag (ignoring case) or the same item.</summary>
    /// <param name="other">The other match.</param>
    /// <returns>True for the same key.</returns>
    public bool SameKey(LootMatch other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return DropTag is not null
            ? other.DropTag is { } otherTag && string.Equals(DropTag, otherTag, StringComparison.OrdinalIgnoreCase)
            : other.DropTag is null && string.Equals(Item, other.Item, StringComparison.Ordinal);
    }

    /// <summary>The drop tag or item name, as messages show it.</summary>
    /// <returns>The key.</returns>
    public override string ToString() => DropTag ?? Item ?? string.Empty;
}

/// <summary>A factor on the pick weight of the items a match covers; 0 removes them.</summary>
/// <param name="Match">The items.</param>
/// <param name="Multiplier">The factor, 0 or more.</param>
public sealed record LootProfileWeight(LootMatch Match, double Multiplier);

/// <summary>
/// Items a profile adds to what the item source offers, whatever their own placement lists accept. A drop tag adds every
/// item with it except those with a root excluded drop tag; a named item joins even with one.
/// </summary>
/// <param name="Match">The items.</param>
/// <param name="Amount">Items per stack, drawn uniformly and capped at the item's largest stack.</param>
/// <param name="Tier">Name of the tier the items count in; null for each item's own tier.</param>
public sealed record LootProfileAdd(LootMatch Match, LootRange Amount, string? Tier);

/// <summary>
/// The extra stack as a profile sets it; each part left null keeps the inherited one. Setting it, even with no part, lets
/// the container get the extra stack.
/// </summary>
/// <param name="Chance">Chance from 0 to 1 of the extra stack, or null.</param>
/// <param name="Entries">What it draws from, or null.</param>
public sealed record LootProfileExtraStack(double? Chance, IReadOnlyList<LootTypeEntry>? Entries);

/// <summary>The first stack's tier as a profile sets it.</summary>
/// <param name="Tier">Name of the tier, or null to force no tier.</param>
public sealed record LootFirstStackTier(string? Tier);

/// <summary>
/// A contents profile: how the contents of one container or loose item socket type differ from the defaults. A type is
/// a tag (the socket's tag or its nearest parent, ignoring case) or a container class. Every member left null or empty
/// keeps what the profiles above it, or the defaults, set.
/// </summary>
/// <param name="Name">Unique name, as logs show it.</param>
/// <param name="Tag">Container (<c>Socket.LootContainer</c> and below) or loose item socket (<c>Socket.Loot</c> and below) tag, or null.</param>
/// <param name="Class">Container class path, or null; exactly one of <paramref name="Tag"/> and this is set.</param>
/// <param name="Only">The source keeps only items one of these matches cover; null keeps every item.</param>
/// <param name="Weights">Factors on item pick weights.</param>
/// <param name="Add">Items added to the source.</param>
/// <param name="Tiers">Factor on a tier's weight by tier name (not on a forced first stack).</param>
/// <param name="FirstStackTier">The first stack's tier, or null to keep the inherited one (containers).</param>
/// <param name="Stacks">Number of stacks, drawn uniformly and capped by the slots, or null (containers).</param>
/// <param name="ExtraStack">The extra stack, or null (containers); a profile that sets it lets the container get it.</param>
public sealed record LootProfile(
    string Name,
    string? Tag,
    string? Class,
    IReadOnlyList<LootMatch>? Only,
    IReadOnlyList<LootProfileWeight> Weights,
    IReadOnlyList<LootProfileAdd> Add,
    IReadOnlyDictionary<string, double> Tiers,
    LootFirstStackTier? FirstStackTier,
    LootRange? Stacks,
    LootProfileExtraStack? ExtraStack);

/// <summary>
/// Items a loose item socket of some tags may hold besides the table items: the pool's entries, drawn against the
/// table items by weight.
/// </summary>
/// <param name="Name">Name of the pool, as logs show it.</param>
/// <param name="Tags">Loose item socket tags that use the pool: the tag itself and every tag below it that no other pool lists.</param>
/// <param name="TableWeight">Weight of a table item against the entries' weights.</param>
/// <param name="Entries">Items chosen by drop tag, with no placement check.</param>
public sealed record LooseItemPool(string Name, IReadOnlyList<string> Tags, double TableWeight, IReadOnlyList<LootTypeEntry> Entries);

/// <summary>
/// What loose item sockets hold: one stack each. The stack is a table item (an item the tables select whose own
/// placement list accepts the socket's tag, picked by tier weight scaled by the area's loot value, then evenly) or,
/// when the socket's pool has entries, an entry's item, chosen by weight. Items with an excluded drop tag never appear.
/// </summary>
/// <param name="Tables">Value loot table asset names the table items come from.</param>
/// <param name="Amount">Items in a table item's stack, drawn uniformly and capped at the item's largest stack.</param>
/// <param name="Pools">Pools by socket tag; a socket no pool covers holds a table item.</param>
public sealed record LooseItemSettings(IReadOnlyList<string> Tables, LootRange Amount, IReadOnlyList<LooseItemPool> Pools);
