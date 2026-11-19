using Ares.Common.Game.Inventory;
using Ares.Common.Game.Loot;
using Ares.Common.GameData;
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
/// Draws the contents of a loot container from the content's loot settings (<see cref="LootContentSettings"/>). A table
/// pool offers the carried items its tables select whose own placement list accepts the container's tag; the container
/// gets 2 or 3 stacks (the game's weighted targets, or the pool's own range), at most one per slot: one of the first
/// stack's tier first when there is one, then by tier weight (scaled by the loot value of the area), each item at most
/// once and never next to an item of a group either one avoids. Some containers then get the extra stack. A type pool
/// draws its stacks from its entries. Only items any slot holds as plain stacks qualify (see <see cref="LoadoutDefinitions.PlainItems"/>).
/// Built once from the game data and shared by the raids.
/// </summary>
public sealed class LootContentRolls
{
    private readonly LootData _loot;
    private readonly LootContentSettings _settings;
    private readonly ItemDefinition[] _items;

    // Positions in the settings' tiers: the first stack's tier (or -1), and the tier of items no other tier takes.
    private readonly int _firstTier;
    private readonly int _otherTier;

    // Every drop tag of each item with the tags above them, which avoid groups are matched against.
    private readonly Dictionary<ItemDefinition, HashSet<string>> _dropTagGroups;

    // What each table the pools name selects: every item with the entry that selects it, in entry order.
    private readonly Dictionary<string, (ItemDefinition Item, StaticValueLootTableEntry Entry)[]> _tableSelections;

    private readonly Dictionary<string, PreparedPool> _tablePools;

    // The table pool items each container type's tag accepts.
    private readonly Dictionary<string, PoolItem[]> _typeCandidates;

    private readonly Dictionary<LootTypeEntry, ItemDefinition[]> _typeItems;

    /// <summary>Prepares the pools from the game data and the loot settings.</summary>
    /// <param name="definitions">The shared item definitions: their plain items are the items a container may hold.</param>
    /// <param name="loot">The loot definitions: container types, value loot tables and the container defaults.</param>
    /// <param name="settings">The loot content settings.</param>
    public LootContentRolls(LoadoutDefinitions definitions, LootData loot, LootContentSettings settings)
    {
        ArgumentNullException.ThrowIfNull(definitions);
        _loot = loot ?? throw new ArgumentNullException(nameof(loot));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _firstTier = settings.FirstStackTier is { } first ? settings.Tiers.ToList().FindIndex(tier => tier.Name == first) : -1;
        _otherTier = settings.Tiers.ToList().FindIndex(tier => tier.DropTag is null);
        _items = [.. definitions.PlainItems.Where(item => !item.DropTags.Any(tag => settings.ExcludedDropTags.Any(excluded => IsTagOrBelow(tag, excluded))))];
        _dropTagGroups = _items.ToDictionary<ItemDefinition, ItemDefinition, HashSet<string>>(item => item,
            item => new HashSet<string>(item.DropTags.SelectMany(TagAndParents), StringComparer.OrdinalIgnoreCase), ReferenceEqualityComparer.Instance);
        _tableSelections = settings.TablePools
            .SelectMany(pool => pool.Tables).Distinct(StringComparer.OrdinalIgnoreCase)
            .ToDictionary(name => name, Selections, StringComparer.OrdinalIgnoreCase);
        _tablePools = settings.TablePools.ToDictionary(pool => pool.Tag, Prepare, StringComparer.OrdinalIgnoreCase);
        _typeCandidates = loot.ContainerTypes.ToDictionary(type => type.Tag, type => FilterCandidates(type.Tag), StringComparer.OrdinalIgnoreCase);
        _typeItems = settings.TypePools
            .SelectMany(pool => pool.Entries).Concat(settings.ExtraStack.Entries).Distinct()
            .ToDictionary(entry => entry, entry => _items.Where(item => Matches(item, entry)).ToArray());
    }

    /// <summary>Whether a gameplay tag is another tag or below it (<c>A.B.C</c> is below <c>A.B</c>), ignoring case.</summary>
    /// <param name="tag">The tag.</param>
    /// <param name="parent">The other tag.</param>
    /// <returns>True when <paramref name="tag"/> equals <paramref name="parent"/> or lies below it.</returns>
    public static bool IsTagOrBelow(string tag, string parent) => LootTags.IsTagOrBelow(tag, parent);

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

    /// <summary>The tier of an item: the first tier whose drop tag it carries, else the tier without a drop tag.</summary>
    /// <param name="item">The item.</param>
    /// <returns>The tier.</returns>
    public LootTier TierOf(ItemDefinition item) => _settings.Tiers[TierIndex(item)];

    /// <summary>The table pool of a container tag: the pool of the tag itself, else of its nearest parent tag.</summary>
    /// <param name="containerTag">The container's tag.</param>
    /// <returns>The pool, or null.</returns>
    public LootTablePool? TablePool(string containerTag) => FindTablePool(containerTag)?.Pool;

    /// <summary>The type pool of a container: the one listing its class, else one listing its tag or a parent of it.</summary>
    /// <param name="containerTag">The container's tag, or null when it has none.</param>
    /// <param name="containerClass">The container's class path.</param>
    /// <returns>The pool, or null.</returns>
    public LootTypePool? TypePool(string? containerTag, string containerClass)
    {
        ArgumentNullException.ThrowIfNull(containerClass);
        return _settings.TypePools.FirstOrDefault(pool => pool.Classes.Contains(containerClass, StringComparer.Ordinal))
            ?? (containerTag is null ? null : _settings.TypePools.FirstOrDefault(pool => pool.Tags.Any(tag => IsTagOrBelow(containerTag, tag))));
    }

    /// <summary>The items a container of a tag may get from its table pool.</summary>
    /// <param name="containerTag">The container's tag.</param>
    /// <returns>The items, in pool order; none when no table pool covers the tag.</returns>
    public IReadOnlyList<ItemDefinition> Candidates(string containerTag) => [.. PoolCandidates(containerTag).Select(entry => entry.Item)];

    /// <summary>
    /// Draws a container's contents from an operator rule's list: the list's number of stacks (else the game's target)
    /// capped by the slots, each an item drawn by weight with an amount in its range, at full durability.
    /// </summary>
    /// <param name="rule">Name of the rule, kept as the contents' pool.</param>
    /// <param name="list">The rule's contents.</param>
    /// <param name="slotCount">Slots of the container.</param>
    /// <param name="random">The raid's random numbers for contents.</param>
    /// <returns>The contents.</returns>
    public LootContents Roll(string rule, LootList list, int slotCount, Random random)
    {
        ArgumentNullException.ThrowIfNull(rule);
        ArgumentNullException.ThrowIfNull(list);
        ArgumentNullException.ThrowIfNull(random);
        int target = list.Stacks is { } stacks ? Uniform(new LootRange(Math.Min(stacks.Min, slotCount), Math.Min(stacks.Max, slotCount)), random)
            : Math.Min(Target(random), slotCount);
        double total = list.Items.Sum(entry => entry.Weight);
        List<LootStack> drawn = [];
        while (drawn.Count < target)
        {
            LootListItem picked = Pick(list.Items, random.NextDouble() * total);
            drawn.Add(new LootStack(picked.Item, Uniform(picked.Amount, random), 1.0));
        }

        return new LootContents($"rule {rule}", drawn);
    }

    // The item a weighted roll in [0, total weight) lands on.
    private static LootListItem Pick(IReadOnlyList<LootListItem> items, double roll)
    {
        foreach (LootListItem item in items)
        {
            if (roll < item.Weight)
            {
                return item;
            }

            roll -= item.Weight;
        }

        return items[^1];
    }

    /// <summary>Draws a container's contents.</summary>
    /// <param name="containerTag">The container's tag, or null when it has none.</param>
    /// <param name="containerClass">The container's class path.</param>
    /// <param name="slotCount">Slots of the container.</param>
    /// <param name="value">Loot value of the area around the container.</param>
    /// <param name="random">The raid's random numbers for contents.</param>
    /// <returns>The contents.</returns>
    public LootContents Roll(string? containerTag, string containerClass, int slotCount, PointOfInterestLootValue value, Random random)
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
            int tier = drawn.Count == 0 && _firstTier >= 0 && available.Exists(entry => entry.Tier == _firstTier)
                ? _firstTier
                : PickTier([.. available.Select(entry => entry.Tier).Distinct().Order()], value, random);
            PoolItem[] inTier = [.. available.Where(entry => entry.Tier == tier)];
            PoolItem picked = inTier[random.Next(inTier.Length)];
            drawn.Add(new LootStack(picked.Item, Amount(picked.Item, picked.StackSize, random), Uniform(picked.Durability, random) / 100.0));
            available.RemoveAll(entry => !Fits(entry.Item, picked.Item));
        }

        if (pool.Pool.ExtraStack && drawn.Count < slotCount && random.NextDouble() < _settings.ExtraStack.Chance
            && Draw(_settings.ExtraStack.Entries, drawn, true, random) is { } extra)
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
    private LootStack? Draw(IReadOnlyList<LootTypeEntry> entries, List<LootStack> present, bool avoidGroups, Random random)
    {
        (LootTypeEntry Entry, ItemDefinition[] Items)[] open =
        [
            .. entries.Select(entry => (Entry: entry, Items: _typeItems[entry].Where(item => avoidGroups ? Fits(item, present) : present.TrueForAll(stack => !ReferenceEquals(stack.Item, item))).ToArray()))
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

    // A tier (by position) by weight among those present; the area's loot value scales the weights.
    private int PickTier(IReadOnlyList<int> tiers, PointOfInterestLootValue value, Random random)
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

    private double Weight(int tier, PointOfInterestLootValue value) =>
        _settings.Tiers[tier].Weight * _settings.Tiers[tier].AreaMultipliers.GetValueOrDefault(value, 1);

    // Position of an item's tier: the first tier whose drop tag it carries, else the tier without a drop tag.
    private int TierIndex(ItemDefinition item)
    {
        ArgumentNullException.ThrowIfNull(item);
        IReadOnlyList<LootTier> tiers = _settings.Tiers;
        for (int index = 0; index < tiers.Count; index++)
        {
            if (tiers[index].DropTag is { } tag && item.DropTags.Any(drop => IsTagOrBelow(drop, tag)))
            {
                return index;
            }
        }

        return _otherTier;
    }

    // The game's weighted target number of stacks.
    private int Target(Random random)
    {
        IReadOnlyList<LootTargetStacks> targets = _loot.ContainerDefaults.TargetStacks;
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
    private bool Fits(ItemDefinition item, List<LootStack> present) => present.TrueForAll(stack => Fits(item, stack.Item));

    private bool Fits(ItemDefinition item, ItemDefinition other) => !ReferenceEquals(item, other) && !Avoids(item, other) && !Avoids(other, item);

    private bool Avoids(ItemDefinition item, ItemDefinition other)
    {
        HashSet<string> groups = _dropTagGroups[other];
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

    private PoolItem[] PoolCandidates(string containerTag)
    {
        ArgumentNullException.ThrowIfNull(containerTag);
        return _typeCandidates.TryGetValue(containerTag, out PoolItem[]? candidates) ? candidates : FilterCandidates(containerTag);
    }

    private PoolItem[] FilterCandidates(string containerTag) =>
        FindTablePool(containerTag) is { } pool ? [.. pool.Items.Where(entry => MatchesContainerTag(entry.Item, containerTag))] : [];

    private PreparedPool? FindTablePool(string containerTag)
    {
        ArgumentNullException.ThrowIfNull(containerTag);
        string current = containerTag;
        while (current.Length > 0)
        {
            if (_tablePools.TryGetValue(current, out PreparedPool? pool))
            {
                return pool;
            }

            int dot = current.LastIndexOf('.');
            current = dot < 0 ? string.Empty : current[..dot];
        }

        return null;
    }

    // The items a pool's tables select, each with the stack size and durability of the first entry that selects it.
    private PreparedPool Prepare(LootTablePool pool)
    {
        List<PoolItem> items = [];
        HashSet<ItemDefinition> seen = new(ReferenceEqualityComparer.Instance);
        foreach ((ItemDefinition item, StaticValueLootTableEntry entry) in pool.Tables.SelectMany(name => _tableSelections[name]))
        {
            if (seen.Add(item))
            {
                items.Add(new PoolItem(item, TierIndex(item), entry.StackSize, entry.Durability));
            }
        }

        return new PreparedPool(pool, items);
    }

    // The items a table (every table of that name) selects, entry by entry.
    private (ItemDefinition Item, StaticValueLootTableEntry Entry)[] Selections(string name)
    {
        StaticValueLootTable[] tables = [.. _loot.StaticValueTables.Where(table => string.Equals(table.Name, name, StringComparison.OrdinalIgnoreCase))];
        if (tables.Length == 0)
        {
            throw new InvalidDataException($"A loot pool names the unknown table {name}");
        }

        return [.. tables.SelectMany(table => table.Entries)
            .SelectMany(entry => _items.Where(item => entry.Selects(item.TechnicalName, item.DropTags)).Select(item => (item, entry)))];
    }

    private sealed record PoolItem(ItemDefinition Item, int Tier, LootRange StackSize, LootRange Durability);

    private sealed record PreparedPool(LootTablePool Pool, IReadOnlyList<PoolItem> Items);
}
