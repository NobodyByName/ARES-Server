using Ares.Common.Game.Loot;
using Ares.Common.GameData;
using Ares.Structs.Game.Inventory;
using Ares.Structs.Game.Loot;
using Ares.Structs.Game.World;

namespace Ares.Raid.World.Loot;

/// <summary>
/// Draws the one stack a loose item socket holds (<see cref="LooseItemSettings"/>): a table item, an item the loose
/// tables select whose own placement list accepts the socket's tag, picked by tier weight (scaled by the area's loot
/// value) then evenly, with an amount in the settings' range; or an item of one of the socket pool's entries, chosen
/// against the table items by weight. Stacks are always at full durability. Only the given items qualify, minus those
/// with an excluded drop tag. The socket's contents profile (<see cref="MergedLootProfile"/>, over its tag's chain)
/// filters and weights the items, adds items whatever their placement lists and scales the tier weights. An operator
/// rule's loot list draws among the given items it lists (<see cref="Roll(LootList, Random)"/>). Built once from the game
/// data and shared by the raids.
/// </summary>
public sealed class LooseItemRolls
{
    private readonly LootContentSettings _content;
    private readonly LooseItemSettings _settings;
    private readonly ItemDefinition[] _given;
    private readonly ItemDefinition[] _allowed;
    private readonly (ItemDefinition Item, int Tier)[] _tableItems;
    private readonly Dictionary<LootTypeEntry, ItemDefinition[]> _entryItems;
    private readonly HashSet<string> _pickups;

    /// <summary>Prepares the table items and the pools' entries.</summary>
    /// <param name="items">The items a loose item socket may hold: plain items players can carry.</param>
    /// <param name="loot">The loot definitions: the value loot tables.</param>
    /// <param name="content">The loot content settings: excluded drop tags, tiers and the loose item settings.</param>
    public LooseItemRolls(IEnumerable<ItemDefinition> items, LootData loot, LootContentSettings content)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(loot);
        _content = content ?? throw new ArgumentNullException(nameof(content));
        _settings = content.LooseItems;
        _given = [.. items];
        _pickups = new HashSet<string>(_given.Select(item => item.TechnicalName), StringComparer.Ordinal);
        _allowed = [.. _given.Where(item => !item.DropTags.Any(tag => content.ExcludedDropTags.Any(excluded => LootTags.IsTagOrBelow(tag, excluded))))];
        StaticValueLootTableEntry[] entries = [.. loot.StaticValueTables
            .Where(table => _settings.Tables.Contains(table.Name, StringComparer.OrdinalIgnoreCase))
            .SelectMany(table => table.Entries)];
        _tableItems = [.. _allowed.Where(item => entries.Any(entry => entry.Selects(item.TechnicalName, item.DropTags))).Select(item => (item, TierIndex(item)))];
        _entryItems = _settings.Pools.SelectMany(pool => pool.Entries).Distinct()
            .ToDictionary(entry => entry, entry => _allowed.Where(item => Matches(item, entry)).ToArray());
    }

    /// <summary>The pool of a socket tag: the pool listing the tag itself, else its nearest parent tag.</summary>
    /// <param name="tag">The loose item socket's tag.</param>
    /// <returns>The pool, or null when none covers the tag.</returns>
    public LooseItemPool? Pool(string tag)
    {
        ArgumentNullException.ThrowIfNull(tag);
        for (string current = tag; current.Length > 0; current = current.LastIndexOf('.') is var dot and > 0 ? current[..dot] : string.Empty)
        {
            if (_settings.Pools.FirstOrDefault(pool => pool.Tags.Contains(current, StringComparer.OrdinalIgnoreCase)) is { } found)
            {
                return found;
            }
        }

        return null;
    }

    /// <summary>The table items a socket of a tag may hold: those whose own placement list accepts the tag.</summary>
    /// <param name="tag">The loose item socket's tag.</param>
    /// <returns>The items.</returns>
    public IReadOnlyList<ItemDefinition> TableItems(string tag) => [.. TableCandidates(tag).Select(entry => entry.Item)];

    /// <summary>Whether a socket of a tag can hold anything: a table item, or an item of its pool's entries.</summary>
    /// <param name="tag">The loose item socket's tag.</param>
    /// <returns>True when a roll can draw a stack.</returns>
    public bool CanHold(string tag)
    {
        MergedLootProfile profile = Profile(tag);
        return Sources(tag, ProfileCandidates(tag, profile), profile).Count > 0;
    }

    // The contents profile of a socket tag: the profiles of its tag's chain, merged.
    private MergedLootProfile Profile(string tag)
    {
        ArgumentNullException.ThrowIfNull(tag);
        return MergedLootProfile.Resolve(_content.Profiles, tag, null);
    }

    /// <summary>Draws the stack of a loose item socket.</summary>
    /// <param name="tag">The loose item socket's tag.</param>
    /// <param name="value">Loot value of the area around the socket.</param>
    /// <param name="random">The raid's random numbers for loose item contents.</param>
    /// <returns>The stack, or null when the socket can hold nothing.</returns>
    public LootStack? Roll(string tag, PointOfInterestLootValue value, Random random)
    {
        ArgumentNullException.ThrowIfNull(random);
        MergedLootProfile profile = Profile(tag);
        LooseCandidate[] candidates = ProfileCandidates(tag, profile);
        List<(LootTypeEntry? Entry, double Weight)> sources = Sources(tag, candidates, profile);
        if (sources.Count == 0)
        {
            return null;
        }

        double roll = random.NextDouble() * sources.Sum(source => source.Weight);
        LootTypeEntry? chosen = sources[^1].Entry;
        foreach ((LootTypeEntry? entry, double weight) in sources)
        {
            if (roll < weight)
            {
                chosen = entry;
                break;
            }

            roll -= weight;
        }

        if (chosen is not null)
        {
            ItemDefinition item = MergedLootProfile.Pick(EntryItems(chosen, profile), profile.Multiplier, random);
            return new LootStack(item, Amount(item, chosen.StackSize, random), 1);
        }

        int tier = PickTier([.. candidates.Select(entry => entry.Tier).Distinct().Order()], value, profile, random);
        if (tier < 0)
        {
            return null;
        }

        LooseCandidate[] inTier = [.. candidates.Where(entry => entry.Tier == tier)];
        LooseCandidate picked = MergedLootProfile.Pick(inTier, entry => entry.Multiplier, random);
        return new LootStack(picked.Item, Amount(picked.Item, picked.AmountRange, random), 1);
    }

    /// <summary>Whether an operator rule's loot list can give a loose item socket a stack: it lists an item that can lie in the world.</summary>
    /// <param name="list">The rule's loot list.</param>
    /// <returns>True when <see cref="Roll(LootList, Random)"/> draws a stack.</returns>
    public bool CanHold(LootList list)
    {
        ArgumentNullException.ThrowIfNull(list);
        return list.Items.Any(entry => _pickups.Contains(entry.Item.TechnicalName));
    }

    /// <summary>
    /// Draws the one stack an operator rule's loot list gives a loose item socket: an item drawn by weight among the
    /// listed items that can lie in the world, with an amount in its range, at full durability. The list's stack count
    /// does not apply.
    /// </summary>
    /// <param name="list">The rule's loot list.</param>
    /// <param name="random">The raid's random numbers for loose item contents.</param>
    /// <returns>The stack, or null when no listed item can lie in the world.</returns>
    public LootStack? Roll(LootList list, Random random)
    {
        ArgumentNullException.ThrowIfNull(list);
        ArgumentNullException.ThrowIfNull(random);
        LootListItem[] items = [.. list.Items.Where(entry => _pickups.Contains(entry.Item.TechnicalName))];
        if (items.Length == 0)
        {
            return null;
        }

        double roll = random.NextDouble() * items.Sum(entry => entry.Weight);
        LootListItem chosen = items[^1];
        foreach (LootListItem entry in items)
        {
            if (roll < entry.Weight)
            {
                chosen = entry;
                break;
            }

            roll -= entry.Weight;
        }

        return new LootStack(chosen.Item, Amount(chosen.Item, chosen.Amount, random), 1);
    }

    // What a socket draws from, with weights: the table items (null entry) when there are any, and each pool entry that
    // has items the profile keeps.
    private List<(LootTypeEntry? Entry, double Weight)> Sources(string tag, LooseCandidate[] candidates, MergedLootProfile profile)
    {
        LooseItemPool? pool = Pool(tag);
        List<(LootTypeEntry? Entry, double Weight)> sources = [];
        double tableWeight = pool?.TableWeight ?? 1;
        if (candidates.Length > 0 && tableWeight > 0)
        {
            sources.Add((null, tableWeight));
        }

        if (pool is not null)
        {
            sources.AddRange(pool.Entries.Where(entry => entry.Weight > 0 && EntryItems(entry, profile).Length > 0).Select(entry => ((LootTypeEntry?)entry, entry.Weight)));
        }

        return sources;
    }

    private (ItemDefinition Item, int Tier)[] TableCandidates(string tag)
    {
        ArgumentNullException.ThrowIfNull(tag);
        return [.. _tableItems.Where(entry => LootContentRolls.MatchesContainerTag(entry.Item, tag))];
    }

    private ItemDefinition[] EntryItems(LootTypeEntry entry, MergedLootProfile profile) => [.. _entryItems[entry].Where(profile.Keeps)];

    // The table items with the profile's added items (the first add bringing an item sets its amount and tier, and replaces
    // the table item), then only those the profile keeps, each with its multiplier. An add by name brings any given item,
    // one by drop tag only those without a root excluded drop tag.
    private LooseCandidate[] ProfileCandidates(string tag, MergedLootProfile profile)
    {
        List<LooseCandidate> candidates = [.. TableCandidates(tag).Select(entry => new LooseCandidate(entry.Item, entry.Tier, _settings.Amount, 1))];
        HashSet<ItemDefinition> added = new(ReferenceEqualityComparer.Instance);
        foreach (LootProfileAdd add in profile.Add)
        {
            int tier = add.Tier is { } name ? _content.Tiers.ToList().FindIndex(known => known.Name == name) : -1;
            foreach (ItemDefinition item in (add.Match.DropTag is null ? _given : _allowed).Where(item => add.Match.Matches(item) && added.Add(item)))
            {
                LooseCandidate entry = new(item, tier >= 0 ? tier : TierIndex(item), add.Amount, 1);
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

    // A tier (by position) by weight among those present; the area's loot value and the profile scale the weights.
    // Tiers weighing 0 are never picked; -1 when no tier present weighs more than 0.
    private int PickTier(IReadOnlyList<int> tiers, PointOfInterestLootValue value, MergedLootProfile profile, Random random)
    {
        double[] weights =
        [
            .. tiers.Select(tier => _content.Tiers[tier].Weight * _content.Tiers[tier].AreaMultipliers.GetValueOrDefault(value, 1) * profile.TierMultiplier(_content.Tiers[tier].Name)),
        ];
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

    // Position of an item's tier: the first tier whose drop tag it carries, else the tier without a drop tag.
    private int TierIndex(ItemDefinition item)
    {
        IReadOnlyList<LootTier> tiers = _content.Tiers;
        for (int index = 0; index < tiers.Count; index++)
        {
            if (tiers[index].DropTag is { } tag && item.DropTags.Any(drop => LootTags.IsTagOrBelow(drop, tag)))
            {
                return index;
            }
        }

        return tiers.ToList().FindIndex(tier => tier.DropTag is null);
    }

    private static bool Matches(ItemDefinition item, LootTypeEntry entry) =>
        item.DropTags.Any(tag => LootTags.IsTagOrBelow(tag, entry.DropTag)) && !item.DropTags.Any(tag => entry.ExcludedDropTags.Any(excluded => LootTags.IsTagOrBelow(tag, excluded)));

    private static int Amount(ItemDefinition item, LootRange range, Random random) =>
        Math.Max(1, Math.Min(range.Max <= range.Min ? range.Min : random.Next(range.Min, range.Max + 1), item.MaxStack));

    // An item a socket may hold by tier, with its amount range and the profile's multiplier.
    private sealed record LooseCandidate(ItemDefinition Item, int Tier, LootRange AmountRange, double Multiplier);
}
