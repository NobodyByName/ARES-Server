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
/// <param name="Limiters">Whether the map's placement limiters cap the sockets they cover.</param>
/// <param name="SpawnerClasses">Container classes (class paths) dynamic loot spawners may place.</param>
public sealed record LootSpawnSettings(
    IReadOnlyDictionary<string, double> Chances,
    IReadOnlyDictionary<PointOfInterestLootValue, double> AreaMultipliers,
    bool GroupCaps,
    bool Limiters,
    IReadOnlySet<string> SpawnerClasses)
{
    /// <summary>The chance of a container tag: its own entry, else its nearest parent's, else 0.</summary>
    /// <param name="tag">The container tag.</param>
    /// <returns>The chance from 0 to 1.</returns>
    public double Chance(string tag)
    {
        ArgumentNullException.ThrowIfNull(tag);
        for (string current = tag; current.Length > 0; current = current.LastIndexOf('.') is var dot and > 0 ? current[..dot] : string.Empty)
        {
            if (Chances.TryGetValue(current, out double chance))
            {
                return chance;
            }
        }

        return 0;
    }

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
/// <param name="Stacks">Number of stacks, drawn uniformly; null for the game's weighted container targets.</param>
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
/// <param name="TablePools">Table pools by container category.</param>
/// <param name="ExtraStack">The extra stack.</param>
/// <param name="TypePools">Type pools.</param>
public sealed record LootContentSettings(
    IReadOnlyList<string> ExcludedDropTags,
    IReadOnlyList<LootTier> Tiers,
    string? FirstStackTier,
    IReadOnlyList<LootTablePool> TablePools,
    LootExtraStack ExtraStack,
    IReadOnlyList<LootTypePool> TypePools);
