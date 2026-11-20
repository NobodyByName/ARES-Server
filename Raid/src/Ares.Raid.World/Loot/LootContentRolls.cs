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
/// gets 2 or 3 stacks (the content's weighted targets, or the pool's own range), at most one per slot: one of the first
/// stack's tier first when there is one, then by tier weight (scaled by the loot value of the area), each item at most
/// once and never next to an item of a group either one avoids. Some containers then get the extra stack. A type pool
/// draws its stacks from its entries. Only items any slot holds as plain stacks qualify (see <see cref="LoadoutDefinitions.PlainItems"/>).
/// The container's contents profile (<see cref="MergedLootProfile"/>) shapes both: it filters and weights the items, adds
/// items whatever their placement lists, and may set the stacks, the first stack's tier, the tier weights and the extra
/// stack. Built once from the game data and shared by the raids.
/// </summary>
public sealed class LootContentRolls
{
    // Durability, in percent, of an added item's stack.
    private static readonly LootRange s_fullDurability = new(100, 100);

    private readonly LootData _loot;
    private readonly LootContentSettings _settings;
    private readonly ItemDefinition[] _items;

    // Every given item, root excluded drop tags or not: the items a profile may add by name.
    private readonly ItemDefinition[] _plainItems;

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
        : this((definitions ?? throw new ArgumentNullException(nameof(definitions))).PlainItems, loot, settings)
    {
    }

    /// <summary>Prepares the pools from the items a container may hold and the loot settings.</summary>
    /// <param name="items">The items a container may hold: plain items.</param>
    /// <param name="loot">The loot definitions: container types, value loot tables and the container defaults.</param>
    /// <param name="settings">The loot content settings.</param>
    public LootContentRolls(IEnumerable<ItemDefinition> items, LootData loot, LootContentSettings settings)
    {
        ArgumentNullException.ThrowIfNull(items);
        _loot = loot ?? throw new ArgumentNullException(nameof(loot));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _firstTier = TierNamed(settings.FirstStackTier);
        _otherTier = settings.Tiers.ToList().FindIndex(tier => tier.DropTag is null);
        _plainItems = [.. items];
        _items = [.. _plainItems.Where(item => !item.DropTags.Any(tag => settings.ExcludedDropTags.Any(excluded => IsTagOrBelow(tag, excluded))))];
        _dropTagGroups = _plainItems.ToDictionary<ItemDefinition, ItemDefinition, HashSet<string>>(item => item,
            item => new HashSet<string>(item.DropTags.SelectMany(TagAndParents), StringComparer.OrdinalIgnoreCase), ReferenceEqualityComparer.Instance);
        _tableSelections = settings.TablePools
            .SelectMany(pool => pool.Tables).Distinct(StringComparer.OrdinalIgnoreCase)
            .ToDictionary(name => name, Selections, StringComparer.OrdinalIgnoreCase);
        _tablePools = settings.TablePools.ToDictionary(pool => pool.Tag, Prepare, StringComparer.OrdinalIgnoreCase);
        _typeCandidates = loot.ContainerTypes.ToDictionary(type => type.Tag, type => FilterCandidates(type.Tag), StringComparer.OrdinalIgnoreCase);
        _typeItems = settings.TypePools
            .SelectMany(pool => pool.Entries).Concat(settings.ExtraStack.Entries).Concat(settings.Profiles.SelectMany(profile => profile.ExtraStack?.Entries ?? [])).Distinct()
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

    /// <summary>The contents profile of a container: the profiles of its tag's chain, then of its class, merged.</summary>
    /// <param name="containerTag">The container's tag, or null when it has none.</param>
    /// <param name="containerClass">The container's class path.</param>
    /// <returns>The merged profile.</returns>
    public MergedLootProfile Profile(string? containerTag, string containerClass)
    {
        ArgumentNullException.ThrowIfNull(containerClass);
        return MergedLootProfile.Resolve(_settings.Profiles, containerTag, containerClass);
    }

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
        MergedLootProfile profile = Profile(containerTag, containerClass);
        if (TypePool(containerTag, containerClass) is { } type)
        {
            return RollType(type, profile, slotCount, random);
        }

        if (containerTag is null || FindTablePool(containerTag) is not { } pool)
        {
            return new LootContents(null, []);
        }

        List<PoolItem> available = ProfileCandidates(containerTag, profile);
        if (available.Count == 0)
        {
            return new LootContents(pool.Pool.Name, []);
        }

        List<LootStack> drawn = [];
        LootRange? stacks = profile.Stacks ?? pool.Pool.Stacks;
        int stackTarget = Math.Min(stacks is { } range ? Uniform(range, random) : Target(random), slotCount);
        int firstTier = profile.FirstStackTier is { } first ? TierNamed(first.Tier) : _firstTier;
        while (drawn.Count < stackTarget && available.Count > 0)
        {
            int tier = drawn.Count == 0 && firstTier >= 0 && available.Exists(entry => entry.Tier == firstTier)
                ? firstTier
                : PickTier([.. available.Select(entry => entry.Tier).Distinct().Order()], value, profile, random);
            if (tier < 0)
            {
                break;
            }

            PoolItem[] inTier = [.. available.Where(entry => entry.Tier == tier)];
            PoolItem picked = MergedLootProfile.Pick(inTier, entry => entry.Multiplier, random);
            drawn.Add(new LootStack(picked.Item, Amount(picked.Item, picked.StackSize, random), Uniform(picked.Durability, random) / 100.0));
            available.RemoveAll(entry => !Fits(entry.Item, picked.Item));
        }

        if ((pool.Pool.ExtraStack || profile.SetsExtraStack) && ExtraStack(drawn, slotCount, profile, random) is { } extra)
        {
            drawn.Add(extra);
        }

        // Containers shuffle their contents on spawn.
        LootStack[] shuffled = [.. drawn];
        random.Shuffle(shuffled);
        return new LootContents(pool.Pool.Name, shuffled);
    }

    // A type pool's stacks: its entries' items and the profile's added items (an entry of weight 1 each), filtered and
    // weighted by the profile; then the extra stack when a profile sets it.
    private LootContents RollType(LootTypePool type, MergedLootProfile profile, int slotCount, Random random)
    {
        DrawEntry[] entries =
        [
            .. type.Entries.Select(entry => Entry(entry, profile)),
            .. profile.Add.Select(add => new DrawEntry(1, [.. AddedItems(add).Where(profile.Keeps)], add.Amount)),
        ];
        List<LootStack> stacks = [];
        int target = Math.Min(Uniform(profile.Stacks ?? type.Stacks, random), slotCount);
        while (stacks.Count < target && Draw(entries, stacks, false, profile, random) is { } stack)
        {
            stacks.Add(stack);
        }

        if (profile.SetsExtraStack && ExtraStack(stacks, slotCount, profile, random) is { } extra)
        {
            stacks.Add(extra);
        }

        return new LootContents(type.Name, stacks);
    }

    // The extra stack, when the container has a free slot and the chance roll succeeds.
    private LootStack? ExtraStack(List<LootStack> drawn, int slotCount, MergedLootProfile profile, Random random) =>
        drawn.Count < slotCount && random.NextDouble() < (profile.ExtraStackChance ?? _settings.ExtraStack.Chance)
            ? Draw([.. (profile.ExtraStackEntries ?? _settings.ExtraStack.Entries).Select(entry => Entry(entry, profile))], drawn, true, profile, random)
            : null;

    private DrawEntry Entry(LootTypeEntry entry, MergedLootProfile profile) => new(entry.Weight, [.. _typeItems[entry].Where(profile.Keeps)], entry.StackSize);

    // One stack from weighted entries: an entry that still has an item not in the container, then one of its items by
    // the profile's multiplier. Null when no entry has one left.
    private LootStack? Draw(IReadOnlyList<DrawEntry> entries, List<LootStack> present, bool avoidGroups, MergedLootProfile profile, Random random)
    {
        DrawEntry[] open =
        [
            .. entries.Select(entry => entry with { Items = [.. entry.Items.Where(item => avoidGroups ? Fits(item, present) : present.TrueForAll(stack => !ReferenceEquals(stack.Item, item)))] })
                .Where(entry => entry.Items.Count > 0 && entry.Weight > 0),
        ];
        if (open.Length == 0)
        {
            return null;
        }

        double roll = random.NextDouble() * open.Sum(entry => entry.Weight);
        DrawEntry chosen = open[^1];
        foreach (DrawEntry entry in open)
        {
            if (roll < entry.Weight)
            {
                chosen = entry;
                break;
            }

            roll -= entry.Weight;
        }

        ItemDefinition item = MergedLootProfile.Pick(chosen.Items, profile.Multiplier, random);
        return new LootStack(item, Amount(item, chosen.AmountRange, random), 1);
    }

    // A tier (by position) by weight among those present; the area's loot value and the profile scale the weights.
    // Tiers weighing 0 are never picked; -1 when no tier present weighs more than 0.
    private int PickTier(IReadOnlyList<int> tiers, PointOfInterestLootValue value, MergedLootProfile profile, Random random)
    {
        double[] weights = [.. tiers.Select(tier => Weight(tier, value) * profile.TierMultiplier(_settings.Tiers[tier].Name))];
        double total = weights.Sum();
        if (total <= 0)
        {
            return -1;
        }

        double roll = random.NextDouble() * total;
        for (int index = 0; index < tiers.Count; index++)
        {
            if (roll < weights[index])
            {
                return tiers[index];
            }

            roll -= weights[index];
        }

        return tiers[Array.FindLastIndex(weights, weight => weight > 0)];
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

    // The weighted target number of stacks of the content.
    private int Target(Random random)
    {
        IReadOnlyList<LootTargetStacks> targets = _settings.TargetStacks;
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

    // Position of a tier by name, or -1 for none.
    private int TierNamed(string? name) => name is null ? -1 : _settings.Tiers.ToList().FindIndex(tier => tier.Name == name);

    // The items an add brings: by name any given item, by drop tag those without a root excluded drop tag.
    private IEnumerable<ItemDefinition> AddedItems(LootProfileAdd add) => (add.Match.DropTag is null ? _plainItems : _items).Where(add.Match.Matches);

    // The pool's candidates with the profile's added items (the first add bringing an item sets its amount and tier, and
    // replaces the item's pool entry), then only those the profile keeps, each with its multiplier.
    private List<PoolItem> ProfileCandidates(string containerTag, MergedLootProfile profile)
    {
        List<PoolItem> candidates = [.. PoolCandidates(containerTag)];
        HashSet<ItemDefinition> added = new(ReferenceEqualityComparer.Instance);
        foreach (LootProfileAdd add in profile.Add)
        {
            int tier = TierNamed(add.Tier);
            foreach (ItemDefinition item in AddedItems(add).Where(added.Add))
            {
                PoolItem entry = new(item, tier >= 0 ? tier : TierIndex(item), add.Amount, s_fullDurability);
                int existing = candidates.FindIndex(candidate => ReferenceEquals(candidate.Item, item));
                if (existing >= 0)
                {
                    candidates[existing] = entry;
                }
                else
                {
                    candidates.Add(entry);
                }
            }
        }

        return [.. candidates.Where(entry => profile.Keeps(entry.Item)).Select(entry => entry with { Multiplier = profile.Multiplier(entry.Item) })];
    }

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

    private sealed record PoolItem(ItemDefinition Item, int Tier, LootRange StackSize, LootRange Durability, double Multiplier = 1);

    // Weighted items a stack may come from, with their amount range.
    private sealed record DrawEntry(double Weight, IReadOnlyList<ItemDefinition> Items, LootRange AmountRange);

    private sealed record PreparedPool(LootTablePool Pool, IReadOnlyList<PoolItem> Items);
}
