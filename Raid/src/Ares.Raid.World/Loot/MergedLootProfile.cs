using Ares.Common.Game.Loot;
using Ares.Structs.Game.Inventory;
using Ares.Structs.Game.Loot;

namespace Ares.Raid.World.Loot;

/// <summary>
/// The contents profile a container or loose item socket rolls with: the profiles of its tag's chain, from the top tag
/// down to its own (tags compare without case), then the profile of its class, merged in that order so that a nearer
/// profile overrides. <see cref="Only"/>, <see cref="Stacks"/>, <see cref="FirstStackTier"/> and each part of the extra
/// stack come from the nearest profile that sets them; <see cref="Tiers"/> per tier name likewise; <see cref="Weights"/>
/// and <see cref="Add"/> are keyed by their match, the nearest profile's entry of a key winning and other keys
/// accumulating, ordered nearest profile first, then as each profile lists them.
/// </summary>
/// <param name="Chain">The profiles merged, in order.</param>
/// <param name="Only">Matches the source items must have one of, or null for every item.</param>
/// <param name="Weights">The merged weights.</param>
/// <param name="Add">The merged added items; when several bring the same item, the first sets its amount and tier.</param>
/// <param name="Tiers">Factor on a tier's weight by tier name; a tier without an entry keeps its weight.</param>
/// <param name="FirstStackTier">The first stack's tier, or null for the settings' one.</param>
/// <param name="Stacks">The number of stacks, or null for the pool's.</param>
/// <param name="SetsExtraStack">Whether a profile of the chain sets the extra stack, which then applies whatever the pool allows.</param>
/// <param name="ExtraStackChance">The extra stack's chance, or null for the settings' one.</param>
/// <param name="ExtraStackEntries">The extra stack's entries, or null for the settings' ones.</param>
public sealed record MergedLootProfile(
    IReadOnlyList<LootProfile> Chain,
    IReadOnlyList<LootMatch>? Only,
    IReadOnlyList<LootProfileWeight> Weights,
    IReadOnlyList<LootProfileAdd> Add,
    IReadOnlyDictionary<string, double> Tiers,
    LootFirstStackTier? FirstStackTier,
    LootRange? Stacks,
    bool SetsExtraStack,
    double? ExtraStackChance,
    IReadOnlyList<LootTypeEntry>? ExtraStackEntries)
{
    /// <summary>Merges the profiles that apply to a container or loose item socket.</summary>
    /// <param name="profiles">Every profile of the settings.</param>
    /// <param name="tag">The container's or socket's tag, or null when it has none.</param>
    /// <param name="containerClass">The container's class path, or null for a loose item socket.</param>
    /// <returns>The merged profile; it changes nothing when no profile applies.</returns>
    public static MergedLootProfile Resolve(IReadOnlyList<LootProfile> profiles, string? tag, string? containerClass)
    {
        ArgumentNullException.ThrowIfNull(profiles);
        List<LootProfile> chain = [];
        if (tag is not null)
        {
            foreach (string level in TopDown(tag))
            {
                if (profiles.FirstOrDefault(profile => string.Equals(profile.Tag, level, StringComparison.OrdinalIgnoreCase)) is { } found)
                {
                    chain.Add(found);
                }
            }
        }

        if (containerClass is not null && profiles.FirstOrDefault(profile => string.Equals(profile.Class, containerClass, StringComparison.Ordinal)) is { } byClass)
        {
            chain.Add(byClass);
        }

        IReadOnlyList<LootMatch>? only = null;
        List<LootProfileWeight> weights = [];
        List<LootProfileAdd> adds = [];
        Dictionary<string, double> tiers = new(StringComparer.Ordinal);
        LootFirstStackTier? first = null;
        LootRange? stacks = null;
        bool setsExtra = false;
        double? chance = null;
        IReadOnlyList<LootTypeEntry>? entries = null;
        foreach (LootProfile profile in chain)
        {
            only = profile.Only ?? only;
            foreach ((string name, double factor) in profile.Tiers)
            {
                tiers[name] = factor;
            }

            first = profile.FirstStackTier ?? first;
            stacks = profile.Stacks ?? stacks;
            if (profile.ExtraStack is not { } extra)
            {
                continue;
            }

            setsExtra = true;
            chance = extra.Chance ?? chance;
            entries = extra.Entries ?? entries;
        }

        for (int index = chain.Count - 1; index >= 0; index--)
        {
            Merge(weights, chain[index].Weights, weight => weight.Match);
            Merge(adds, chain[index].Add, add => add.Match);
        }

        return new MergedLootProfile(chain, only, weights, adds, tiers, first, stacks, setsExtra, chance, entries);
    }

    /// <summary>An item's weight factor: the product of every merged weight whose match covers it (1 without one).</summary>
    /// <param name="item">The item.</param>
    /// <returns>The factor.</returns>
    public double Multiplier(ItemDefinition item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return Weights.Where(weight => weight.Match.Matches(item)).Aggregate(1.0, (factor, weight) => factor * weight.Multiplier);
    }

    /// <summary>Whether an item stays a candidate: <see cref="Only"/> covers it (when set) and its multiplier is not 0.</summary>
    /// <param name="item">The item.</param>
    /// <returns>True when the item may be drawn.</returns>
    public bool Keeps(ItemDefinition item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return (Only is null || Only.Any(match => match.Matches(item))) && Multiplier(item) > 0;
    }

    /// <summary>The factor on a tier's weight.</summary>
    /// <param name="tier">The tier's name.</param>
    /// <returns>The factor; 1 when no profile sets one.</returns>
    public double TierMultiplier(string tier) => Tiers.GetValueOrDefault(tier, 1);

    /// <summary>
    /// One of some items, drawn by weight; evenly (one integer draw) when every weight is the same, so rolls without
    /// differing weights draw the same random numbers as an even pick.
    /// </summary>
    /// <param name="items">The items; at least one.</param>
    /// <param name="weight">The weight of an item.</param>
    /// <param name="random">The random numbers.</param>
    /// <returns>The drawn item.</returns>
    internal static T Pick<T>(IReadOnlyList<T> items, Func<T, double> weight, Random random)
    {
        double[] weights = [.. items.Select(weight)];
        if (Array.TrueForAll(weights, value => value.Equals(weights[0])))
        {
            return items[random.Next(items.Count)];
        }

        double roll = random.NextDouble() * weights.Sum();
        for (int index = 0; index < items.Count; index++)
        {
            if (roll < weights[index])
            {
                return items[index];
            }

            roll -= weights[index];
        }

        return items[^1];
    }

    // A tag and its parents, from the top one down to the tag itself.
    private static List<string> TopDown(string tag)
    {
        List<string> levels = [];
        for (string current = tag; current.Length > 0; current = current.LastIndexOf('.') is var dot and > 0 ? current[..dot] : string.Empty)
        {
            levels.Add(current);
        }

        levels.Reverse();
        return levels;
    }

    // Merging from the nearest profile up: an entry joins unless a nearer one has its key.
    private static void Merge<T>(List<T> merged, IReadOnlyList<T> farther, Func<T, LootMatch> key)
    {
        foreach (T entry in farther)
        {
            if (!merged.Exists(existing => key(existing).SameKey(key(entry))))
            {
                merged.Add(entry);
            }
        }
    }
}
