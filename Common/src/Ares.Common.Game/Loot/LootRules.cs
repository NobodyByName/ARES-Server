using Ares.Structs.Game.Inventory;
using Ares.Structs.Game.Loot;
using Ares.Structs.Game.World;

namespace Ares.Common.Game.Loot;

/// <summary>
/// The ids that name one loot container or loose item socket of a map in the operator's loot rules, built from the map's
/// game data: a container socket or a loose item socket is <c>cell/actor/component/socket</c>, a placed container or a
/// dynamic loot spawner is <c>cell/actor</c> (the cell is empty for the map's persistent level, so such an id starts
/// with <c>/</c>).
/// </summary>
public static class LootContainerIds
{
    /// <summary>The id of a container socket.</summary>
    /// <param name="socket">The socket.</param>
    /// <returns>The id.</returns>
    public static string Of(LootSocket socket)
    {
        ArgumentNullException.ThrowIfNull(socket);
        return $"{socket.Cell}/{socket.Actor}/{socket.Component}/{socket.Socket}";
    }

    /// <summary>The id of a loose item socket.</summary>
    /// <param name="socket">The socket.</param>
    /// <returns>The id.</returns>
    public static string Of(LooseItemSocket socket)
    {
        ArgumentNullException.ThrowIfNull(socket);
        return $"{socket.Cell}/{socket.Actor}/{socket.Component}/{socket.Socket}";
    }

    /// <summary>The id of a placed container.</summary>
    /// <param name="container">The container.</param>
    /// <returns>The id.</returns>
    public static string Of(PlacedLootContainer container)
    {
        ArgumentNullException.ThrowIfNull(container);
        return $"{container.Cell}/{container.Actor}";
    }

    /// <summary>The id of a dynamic loot spawner: every container it places shares it.</summary>
    /// <param name="spawner">The spawner.</param>
    /// <returns>The id.</returns>
    public static string Of(DynamicLootSpawner spawner)
    {
        ArgumentNullException.ThrowIfNull(spawner);
        return $"{spawner.Cell}/{spawner.Actor}";
    }
}

/// <summary>An item a loot list draws.</summary>
/// <param name="Item">The item.</param>
/// <param name="Weight">Relative chance among the list's items.</param>
/// <param name="Amount">Stack size range, within the item's max stack.</param>
public sealed record LootListItem(ItemDefinition Item, double Weight, LootRange Amount);

/// <summary>
/// Contents an operator rule gives a container instead of the default roll: a number of stacks in
/// <see cref="Stacks"/> (capped by the container's slots), each an item drawn by weight (an item can come up more than
/// once) with an amount in its range, at full durability. A loose item socket gets one stack, drawn by weight among the
/// items that can lie in the world as a pickup; with none of those it keeps its default roll.
/// </summary>
/// <param name="Stacks">How many stacks, or null for the content's weighted targets (2 or 3 by default); loose item sockets ignore it.</param>
/// <param name="Items">The items to draw from; never empty.</param>
public sealed record LootList(LootRange? Stacks, IReadOnlyList<LootListItem> Items);

/// <summary>
/// One operator loot rule. It covers the containers and loose item sockets listed by id, those whose container tag or
/// loose item tag (<c>Socket.Loot.*</c>) is one of <see cref="Tags"/> or below it, or every container and loose item
/// socket when it lists neither.
/// </summary>
/// <param name="Name">Name shown in logs.</param>
/// <param name="Tags">Container and loose item tags it covers, each with the tags below it.</param>
/// <param name="Containers">Ids (see <see cref="LootContainerIds"/>) of the containers and loose item sockets it covers.</param>
/// <param name="SpawnChance">
/// Chance from 0 to 1 that a covered container spawns or a covered loose item socket gets an item, replacing the game's
/// decision (chance, caps, groups, limiters), or null to keep it.
/// </param>
/// <param name="Loot">
/// Contents of a covered container, or the one stack of a covered loose item socket (see <see cref="LootList"/>),
/// replacing the default roll, or null to keep it.
/// </param>
/// <param name="GroupFill">
/// How many container sockets of each group the rule covers get a container, replacing the game's decision for them
/// (chance, caps), or null to keep it. The containers a rule lists form one group; otherwise a group is the game's: the
/// sockets of one placed prop with the same tag. Placed containers, spawners and loose item sockets ignore it.
/// </param>
/// <param name="RevealDelaySeconds">
/// Wait in seconds after a first search's hold before a covered container's contents start to reveal, replacing the
/// container class's own delay, or null to keep it. Loose item sockets ignore it.
/// </param>
public sealed record LootRule(
    string Name,
    IReadOnlyList<string> Tags,
    IReadOnlySet<string> Containers,
    double? SpawnChance,
    LootList? Loot,
    LootFill? GroupFill = null,
    double? RevealDelaySeconds = null)
{
    /// <summary>Whether the rule covers a container or a loose item socket.</summary>
    /// <param name="id">The container's or loose item socket's id, or null for a container the raid spawned on request.</param>
    /// <param name="tags">Its tags (several for a spawner that can place several classes).</param>
    /// <returns>True when the rule applies.</returns>
    public bool Covers(string? id, IEnumerable<string> tags)
    {
        ArgumentNullException.ThrowIfNull(tags);
        return (Tags.Count == 0 && Containers.Count == 0)
            || (id is not null && Containers.Contains(id))
            || tags.Any(tag => Tags.Any(covered => LootTags.IsTagOrBelow(tag, covered)));
    }
}

/// <summary>
/// The operator's loot rules (<c>loot-rules.json</c>), checked against the game data. Rules apply in file order: for each
/// setting, the last rule that covers a container or loose item socket and sets it wins; one no rule covers keeps the
/// defaults.
/// </summary>
public sealed class LootRules
{
    /// <summary>Creates the rules.</summary>
    /// <param name="rules">The rules, in file order.</param>
    public LootRules(IReadOnlyList<LootRule> rules)
    {
        Rules = rules ?? throw new ArgumentNullException(nameof(rules));
    }

    /// <summary>No rules: every container keeps the defaults.</summary>
    public static LootRules None { get; } = new([]);

    /// <summary>The rules, in file order.</summary>
    public IReadOnlyList<LootRule> Rules { get; }

    /// <summary>The spawn chance the rules set for a container or a loose item socket.</summary>
    /// <param name="id">Its id.</param>
    /// <param name="tags">Its tags.</param>
    /// <returns>The chance, or null to keep the game's decision.</returns>
    public double? SpawnChance(string? id, IReadOnlyCollection<string> tags) => Rules.LastOrDefault(rule => rule.SpawnChance is not null && rule.Covers(id, tags))?.SpawnChance;

    /// <summary>The rule that decides whether a container socket spawns: the last covering rule that sets a spawn chance or a group fill.</summary>
    /// <param name="id">The socket's container id.</param>
    /// <param name="tag">The socket's tag.</param>
    /// <returns>The rule, or null to keep the game's decision.</returns>
    public LootRule? SocketSpawn(string id, string tag) =>
        Rules.LastOrDefault(rule => (rule.SpawnChance is not null || rule.GroupFill is not null) && rule.Covers(id, [tag]));

    /// <summary>The rule that sets a container's contents or a loose item socket's stack.</summary>
    /// <param name="id">Its id, or null.</param>
    /// <param name="tag">Its tag, or null.</param>
    /// <returns>The rule, or null to keep the default roll.</returns>
    public LootRule? LootOf(string? id, string? tag) => Rules.LastOrDefault(rule => rule.Loot is not null && rule.Covers(id, tag is null ? [] : [tag]));

    /// <summary>The reveal delay the rules set for a container.</summary>
    /// <param name="id">The container's id, or null.</param>
    /// <param name="tags">The container's tags.</param>
    /// <returns>The delay in seconds, or null to keep the container class's own.</returns>
    public double? RevealDelaySeconds(string? id, IReadOnlyCollection<string> tags) =>
        Rules.LastOrDefault(rule => rule.RevealDelaySeconds is not null && rule.Covers(id, tags))?.RevealDelaySeconds;
}

/// <summary>
/// How many sockets of a group get a container: a number drawn by weight, at most the group's sockets. In an area with a
/// loot value, the weight of each number is scaled by the area's multiplier to the power of the sockets it fills: every
/// socket's odds of a container are multiplied by the area's factor, as a lone socket's chance is (alike at low
/// chances), and the group keeps its shape.
/// </summary>
/// <param name="Weights">Relative weight of each number of filled sockets outside every area with a loot value (multiplier 1).</param>
public sealed record LootFill(IReadOnlyDictionary<int, double> Weights)
{
    /// <summary>Draws how many sockets of a group get a container.</summary>
    /// <param name="sockets">Sockets of the group.</param>
    /// <param name="random">The raid's random numbers.</param>
    /// <param name="areaMultiplier">Factor of the loot value of the group's area (<see cref="LootSpawnSettings.Multiplier"/>).</param>
    /// <returns>The number, from 0 to <paramref name="sockets"/>.</returns>
    public int Draw(int sockets, Random random, double areaMultiplier = 1)
    {
        ArgumentNullException.ThrowIfNull(random);
        (int Count, double Weight)[] entries = InArea(sockets, areaMultiplier);
        double total = entries.Sum(entry => entry.Weight);
        if (entries.Length == 0 || !(total > 0))
        {
            return 0;
        }

        double roll = random.NextDouble() * total;
        foreach ((int count, double weight) in entries)
        {
            if (roll < weight)
            {
                return count;
            }

            roll -= weight;
        }

        return entries[^1].Count;
    }

    /// <summary>
    /// The weights in an area: each number above 0 (at most <paramref name="sockets"/>) with its weight times
    /// <paramref name="areaMultiplier"/> to the power of that number, in ascending order of the configured numbers.
    /// The weights are scaled together so the largest stays finite; only their shares matter.
    /// </summary>
    /// <param name="sockets">Sockets of the group.</param>
    /// <param name="areaMultiplier">Factor of the loot value of the group's area.</param>
    /// <returns>The numbers, capped at <paramref name="sockets"/>, with their weights.</returns>
    public (int Count, double Weight)[] InArea(int sockets, double areaMultiplier)
    {
        (int Count, double Weight)[] entries = [.. Weights.Where(entry => entry.Value > 0).OrderBy(entry => entry.Key)
            .Select(entry => (Count: Math.Min(entry.Key, Math.Max(sockets, 0)), Weight: entry.Value))];
        if (areaMultiplier is 1d || entries.Length == 0)
        {
            return entries;
        }

        if (!(areaMultiplier > 0))
        {
            return [.. entries.Select(entry => (entry.Count, entry.Count == 0 ? entry.Weight : 0))];
        }

        // In logarithms, less the largest, so a high factor on a large group cannot overflow.
        double factor = Math.Log(areaMultiplier);
        double[] logs = [.. entries.Select(entry => Math.Log(entry.Weight) + (entry.Count * factor))];
        double largest = logs.Max();
        return [.. entries.Select((entry, index) => (entry.Count, Math.Exp(logs[index] - largest)))];
    }
}

/// <summary>Gameplay tag comparisons for loot.</summary>
public static class LootTags
{
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
}
