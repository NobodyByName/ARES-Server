using Ares.Common.Game.Inventory;
using Ares.Structs.Game.Inventory;
using Ares.Structs.Game.Loot;
using Ares.Structs.Game.World;

namespace Ares.Raid.World.Loot;

/// <summary>One stack a container holds from its creation.</summary>
/// <param name="Item">The item.</param>
/// <param name="Amount">Items in the stack.</param>
/// <param name="Durability">Durability as a fraction of maximum durability.</param>
public sealed record LootStack(ItemDefinition Item, int Amount, double Durability);

/// <summary>What a container holds from its creation.</summary>
/// <param name="Pool">Name of the pool it drew from; null when no pool covers the container.</param>
/// <param name="Stacks">The stacks in slot order; empty when the pool has no item for the container.</param>
public sealed record LootContents(string? Pool, IReadOnlyList<LootStack> Stacks);

/// <summary>
/// Draws the contents of a loot container from <see cref="LootContentDefaults"/>. A table pool offers the carried items
/// its tables select whose own placement list accepts the container's tag; the container gets 2 or 3 stacks (the
/// game's weighted targets, or the pool's own range), at most one per slot: a common material first when there is
/// one, then by tier weight (scaled by the loot value of the area), each item at most once and never next to an item
/// of a group either one avoids. Some containers then get one extra ammunition or healing stack. A type pool draws its
/// stacks from its entries. Only items any slot holds as plain stacks qualify (see <see cref="LoadoutDefinitions.PlainItems"/>).
/// </summary>
public static class LootContentRolls
{
    private static readonly ItemDefinition[] s_items =
    [
        .. LoadoutDefinitions.Generated.PlainItems.Where(item => !item.DropTags.Any(tag => LootContentDefaults.ExcludedDropTags.Any(excluded => IsTagOrBelow(tag, excluded)))),
    ];

    // Every drop tag of each item with the tags above them, which avoid groups are matched against.
    private static readonly Dictionary<ItemDefinition, HashSet<string>> s_dropTagGroups = s_items.ToDictionary<ItemDefinition, ItemDefinition, HashSet<string>>(item => item,
        item => new HashSet<string>(item.DropTags.SelectMany(TagAndParents), StringComparer.OrdinalIgnoreCase), ReferenceEqualityComparer.Instance);

    // What each table the pools name selects: every item with the entry that selects it, in entry order.
    private static readonly Dictionary<string, (ItemDefinition Item, StaticValueLootTableEntry Entry)[]> s_tableSelections = LootContentDefaults.TablePools
        .SelectMany(pool => pool.Tables).Distinct(StringComparer.OrdinalIgnoreCase)
        .ToDictionary(name => name, Selections, StringComparer.OrdinalIgnoreCase);

    private static readonly Dictionary<string, PreparedPool> s_tablePools = LootContentDefaults.TablePools
        .ToDictionary(pool => pool.Tag, Prepare, StringComparer.OrdinalIgnoreCase);

    // The table pool items each container type's tag accepts.
    private static readonly Dictionary<string, PoolItem[]> s_typeCandidates = LootContainerTypes.All
        .ToDictionary(type => type.Tag, type => FilterCandidates(type.Tag), StringComparer.OrdinalIgnoreCase);

    private static readonly Dictionary<LootTypeEntry, ItemDefinition[]> s_typeItems = LootContentDefaults.TypePools
        .SelectMany(pool => pool.Entries).Concat(LootContentDefaults.ExtraStackEntries).Distinct()
        .ToDictionary(entry => entry, entry => s_items.Where(item => Matches(item, entry)).ToArray());

    /// <summary>Whether a gameplay tag is another tag or below it (<c>A.B.C</c> is below <c>A.B</c>), ignoring case.</summary>
    /// <param name="tag">The tag.</param>
    /// <param name="parent">The other tag.</param>
    /// <returns>True when <paramref name="tag"/> equals <paramref name="parent"/> or lies below it.</returns>
    public static bool IsTagOrBelow(string tag, string parent)
    {
        ArgumentNullException.ThrowIfNull(tag);
        ArgumentNullException.ThrowIfNull(parent);
        return tag.StartsWith(parent, StringComparison.OrdinalIgnoreCase) && (tag.Length == parent.Length || tag[parent.Length] == '.');
    }

    /// <summary>
    /// Whether an item's placement list accepts a container: one placement's tag is the container's tag or above it, and
    /// none of that placement's excluded tags is.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <param name="containerTag">The container's tag.</param>
    /// <returns>True when the item may spawn in the container.</returns>
    public static bool MatchesContainerTag(ItemDefinition item, string containerTag)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(containerTag);
        return item.LootPlacements.Any(placement => placement.SocketTag.Length > 0 && IsTagOrBelow(containerTag, placement.SocketTag)
            && !placement.ExcludeTags.Any(excluded => IsTagOrBelow(containerTag, excluded)));
    }

    /// <summary>The tier of an item: the first tier of <see cref="LootContentDefaults.TierWeights"/> whose drop tag it carries.</summary>
    /// <param name="item">The item.</param>
    /// <returns>The tier, <see cref="LootTier.Other"/> when it carries none.</returns>
    public static LootTier TierOf(ItemDefinition item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return LootContentDefaults.TierWeights.FirstOrDefault(tier => tier.DropTag is { } tag && item.DropTags.Any(drop => IsTagOrBelow(drop, tag)))?.Tier
            ?? LootTier.Other;
    }

    /// <summary>The table pool of a container tag: the pool of the tag itself, else of its nearest parent tag.</summary>
    /// <param name="containerTag">The container's tag.</param>
    /// <returns>The pool, or null.</returns>
    public static LootTablePool? TablePool(string containerTag) => FindTablePool(containerTag)?.Pool;

    /// <summary>The type pool of a container: the one listing its class, else one listing its tag or a parent of it.</summary>
    /// <param name="containerTag">The container's tag, or null when it has none.</param>
    /// <param name="containerClass">The container's class path.</param>
    /// <returns>The pool, or null.</returns>
    public static LootTypePool? TypePool(string? containerTag, string containerClass)
    {
        ArgumentNullException.ThrowIfNull(containerClass);
        return LootContentDefaults.TypePools.FirstOrDefault(pool => pool.Classes.Contains(containerClass, StringComparer.Ordinal))
            ?? (containerTag is null ? null : LootContentDefaults.TypePools.FirstOrDefault(pool => pool.Tags.Any(tag => IsTagOrBelow(containerTag, tag))));
    }

    /// <summary>The items a container of a tag may get from its table pool.</summary>
    /// <param name="containerTag">The container's tag.</param>
    /// <returns>The items, in pool order; none when no table pool covers the tag.</returns>
    public static IReadOnlyList<ItemDefinition> Candidates(string containerTag) => [.. PoolCandidates(containerTag).Select(entry => entry.Item)];

    /// <summary>Draws a container's contents.</summary>
    /// <param name="containerTag">The container's tag, or null when it has none.</param>
    /// <param name="containerClass">The container's class path.</param>
    /// <param name="slotCount">Slots of the container.</param>
    /// <param name="value">Loot value of the area around the container.</param>
    /// <param name="random">The raid's random numbers for contents.</param>
    /// <returns>The contents.</returns>
    public static LootContents Roll(string? containerTag, string containerClass, int slotCount, PointOfInterestLootValue value, Random random)
    {
        ArgumentNullException.ThrowIfNull(containerClass);
        ArgumentNullException.ThrowIfNull(random);
        if (TypePool(containerTag, containerClass) is { } type)
        {
            List<LootStack> stacks = [];
            int target = Math.Min(Uniform(type.Stacks, random), slotCount);
            while (stacks.Count < target && Draw(type.Entries, stacks, false, random) is { } stack)
            {
                stacks.Add(stack);
            }

            return new LootContents(type.Name, stacks);
        }

        if (containerTag is null || FindTablePool(containerTag) is not { } pool)
        {
            return new LootContents(null, []);
        }

        PoolItem[] candidates = PoolCandidates(containerTag);
        if (candidates.Length == 0)
        {
            return new LootContents(pool.Pool.Name, []);
        }

        List<LootStack> drawn = [];
        List<PoolItem> available = [.. candidates];
        int stackTarget = Math.Min(pool.Pool.Stacks is { } range ? Uniform(range, random) : Target(random), slotCount);
        while (drawn.Count < stackTarget && available.Count > 0)
        {
            LootTier tier = drawn.Count == 0 && available.Exists(entry => entry.Tier == LootTier.Common)
                ? LootTier.Common
                : PickTier([.. available.Select(entry => entry.Tier).Distinct().Order()], value, random);
            PoolItem[] inTier = [.. available.Where(entry => entry.Tier == tier)];
            PoolItem picked = inTier[random.Next(inTier.Length)];
            drawn.Add(new LootStack(picked.Item, Amount(picked.Item, picked.StackSize, random), Uniform(picked.Durability, random) / 100.0));
            available.RemoveAll(entry => !Fits(entry.Item, picked.Item));
        }

        if (pool.Pool.ExtraStack && drawn.Count < slotCount && random.NextDouble() < LootContentDefaults.ExtraStackChance
            && Draw(LootContentDefaults.ExtraStackEntries, drawn, true, random) is { } extra)
        {
            drawn.Add(extra);
        }

        // Containers shuffle their contents on spawn.
        LootStack[] shuffled = [.. drawn];
        random.Shuffle(shuffled);
        return new LootContents(pool.Pool.Name, shuffled);
    }

    // One stack from weighted drop-tag entries: an entry that still has an item not in the container, then one of its
    // items evenly. Null when no entry has one left.
    private static LootStack? Draw(IReadOnlyList<LootTypeEntry> entries, List<LootStack> present, bool avoidGroups, Random random)
    {
        (LootTypeEntry Entry, ItemDefinition[] Items)[] open =
        [
            .. entries.Select(entry => (Entry: entry, Items: s_typeItems[entry].Where(item => avoidGroups ? Fits(item, present) : present.TrueForAll(stack => !ReferenceEquals(stack.Item, item))).ToArray()))
                .Where(entry => entry.Items.Length > 0 && entry.Entry.Weight > 0),
        ];
        if (open.Length == 0)
        {
            return null;
        }

        double roll = random.NextDouble() * open.Sum(entry => entry.Entry.Weight);
        (LootTypeEntry chosen, ItemDefinition[] items) = open[^1];
        foreach ((LootTypeEntry Entry, ItemDefinition[] Items) entry in open)
        {
            if (roll < entry.Entry.Weight)
            {
                (chosen, items) = entry;
                break;
            }

            roll -= entry.Entry.Weight;
        }

        ItemDefinition item = items[random.Next(items.Length)];
        return new LootStack(item, Amount(item, chosen.StackSize, random), 1);
    }

    // A tier by weight among those present; the area's loot value scales the weights.
    private static LootTier PickTier(IReadOnlyList<LootTier> tiers, PointOfInterestLootValue value, Random random)
    {
        double[] weights = [.. tiers.Select(tier => Weight(tier, value))];
        double roll = random.NextDouble() * weights.Sum();
        for (int index = 0; index < tiers.Count; index++)
        {
            if (roll < weights[index])
            {
                return tiers[index];
            }

            roll -= weights[index];
        }

        return tiers[^1];
    }

    private static double Weight(LootTier tier, PointOfInterestLootValue value) =>
        LootContentDefaults.TierWeights.Where(entry => entry.Tier == tier).Select(entry => entry.Weight).DefaultIfEmpty(0).First()
        * LootContentDefaults.TierMultipliers.Where(entry => entry.Value == value && entry.Tier == tier).Aggregate(1.0, (product, entry) => product * entry.Multiplier);

    // The game's weighted target number of stacks.
    private static int Target(Random random)
    {
        IReadOnlyList<LootTargetStacks> targets = StaticValueLootTables.ContainerTargetStacks;
        double roll = random.NextDouble() * targets.Sum(target => target.Weight);
        foreach (LootTargetStacks target in targets)
        {
            if (roll < target.Weight)
            {
                return target.Target;
            }

            roll -= target.Weight;
        }

        return targets[^1].Target;
    }

    // Whether an item may join a container's stacks: not there yet, and no avoid group of either item covers another.
    private static bool Fits(ItemDefinition item, List<LootStack> present) => present.TrueForAll(stack => Fits(item, stack.Item));

    private static bool Fits(ItemDefinition item, ItemDefinition other) => !ReferenceEquals(item, other) && !Avoids(item, other) && !Avoids(other, item);

    private static bool Avoids(ItemDefinition item, ItemDefinition other)
    {
        HashSet<string> groups = s_dropTagGroups[other];
        return item.LootAvoidGroups.Any(groups.Contains);
    }

    private static IEnumerable<string> TagAndParents(string tag)
    {
        for (string current = tag; current.Length > 0; current = current.LastIndexOf('.') is var dot and > 0 ? current[..dot] : string.Empty)
        {
            yield return current;
        }
    }

    private static int Amount(ItemDefinition item, LootRange range, Random random) => Math.Max(1, Math.Min(Uniform(range, random), item.MaxStack));

    private static int Uniform(LootRange range, Random random) => range.Max <= range.Min ? range.Min : random.Next(range.Min, range.Max + 1);

    private static bool Matches(ItemDefinition item, LootTypeEntry entry) =>
        item.DropTags.Any(tag => IsTagOrBelow(tag, entry.DropTag)) && !item.DropTags.Any(tag => entry.ExcludedDropTags.Any(excluded => IsTagOrBelow(tag, excluded)));

    private static PoolItem[] PoolCandidates(string containerTag)
    {
        ArgumentNullException.ThrowIfNull(containerTag);
        return s_typeCandidates.TryGetValue(containerTag, out PoolItem[]? candidates) ? candidates : FilterCandidates(containerTag);
    }

    private static PoolItem[] FilterCandidates(string containerTag) =>
        FindTablePool(containerTag) is { } pool ? [.. pool.Items.Where(entry => MatchesContainerTag(entry.Item, containerTag))] : [];

    private static PreparedPool? FindTablePool(string containerTag)
    {
        ArgumentNullException.ThrowIfNull(containerTag);
        string current = containerTag;
        while (current.Length > 0)
        {
            if (s_tablePools.TryGetValue(current, out PreparedPool? pool))
            {
                return pool;
            }

            int dot = current.LastIndexOf('.');
            current = dot < 0 ? string.Empty : current[..dot];
        }

        return null;
    }

    // The items a pool's tables select, each with the stack size and durability of the first entry that selects it.
    private static PreparedPool Prepare(LootTablePool pool)
    {
        List<PoolItem> items = [];
        HashSet<ItemDefinition> seen = new(ReferenceEqualityComparer.Instance);
        foreach ((ItemDefinition item, StaticValueLootTableEntry entry) in pool.Tables.SelectMany(name => s_tableSelections[name]))
        {
            if (seen.Add(item))
            {
                items.Add(new PoolItem(item, TierOf(item), entry.StackSize, entry.Durability));
            }
        }

        return new PreparedPool(pool, items);
    }

    // The items a table (every table of that name) selects, entry by entry.
    private static (ItemDefinition Item, StaticValueLootTableEntry Entry)[] Selections(string name)
    {
        StaticValueLootTable[] tables = [.. StaticValueLootTables.All.Where(table => string.Equals(table.Name, name, StringComparison.OrdinalIgnoreCase))];
        if (tables.Length == 0)
        {
            throw new InvalidDataException($"A loot pool names the unknown table {name}");
        }

        return [.. tables.SelectMany(table => table.Entries)
            .SelectMany(entry => s_items.Where(item => entry.Selects(item.TechnicalName, item.DropTags)).Select(item => (item, entry)))];
    }

    private sealed record PoolItem(ItemDefinition Item, LootTier Tier, LootRange StackSize, LootRange Durability);

    private sealed record PreparedPool(LootTablePool Pool, IReadOnlyList<PoolItem> Items);
}
