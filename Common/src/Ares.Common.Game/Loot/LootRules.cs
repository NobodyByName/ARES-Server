using Ares.Structs.Game.Inventory;
using Ares.Structs.Game.Loot;
using Ares.Structs.Game.World;

namespace Ares.Common.Game.Loot;

/// <summary>
/// The ids that name one loot container of a map in the operator's loot rules, built from the map's game data: a prop
/// socket is <c>cell/actor/component/socket</c>, a placed container or a dynamic loot spawner is <c>cell/actor</c>
/// (the cell is empty for the map's persistent level, so such an id starts with <c>/</c>).
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
/// once) with an amount in its range, at full durability.
/// </summary>
/// <param name="Stacks">How many stacks, or null for the game's usual target (2 or 3).</param>
/// <param name="Items">The items to draw from; never empty.</param>
public sealed record LootList(LootRange? Stacks, IReadOnlyList<LootListItem> Items);

/// <summary>
/// One operator loot rule. It covers the containers listed by id, those whose container tag is one of
/// <see cref="Tags"/> or below it, or every container when it lists neither.
/// </summary>
/// <param name="Name">Name shown in logs.</param>
/// <param name="Tags">Container tags it covers, each with the tags below it.</param>
/// <param name="Containers">Ids (see <see cref="LootContainerIds"/>) of the containers it covers.</param>
/// <param name="SpawnChance">
/// Chance from 0 to 1 that a covered container spawns, replacing the game's decision (chance, caps, groups), or null to
/// keep it.
/// </param>
/// <param name="Loot">Contents of a covered container, replacing the default roll, or null to keep it.</param>
/// <param name="GroupFill">
/// How many container sockets of each group the rule covers get a container, replacing the game's decision for them
/// (chance, caps), or null to keep it. The containers a rule lists form one group; otherwise a group is the game's: the
/// sockets of one placed prop with the same tag. Placed containers and spawners ignore it.
/// </param>
/// <param name="RevealDelaySeconds">
/// Wait in seconds after a first search's hold before a covered container's contents start to reveal, replacing the
/// container class's own delay, or null to keep it.
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
    /// <summary>Whether the rule covers a container.</summary>
    /// <param name="id">The container's id, or null for a container the raid spawned on request.</param>
    /// <param name="tags">The container's tags (several for a spawner that can place several classes).</param>
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
/// setting, the last rule that covers a container and sets it wins; a container no rule covers keeps the defaults.
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

    /// <summary>The spawn chance the rules set for a container.</summary>
    /// <param name="id">The container's id.</param>
    /// <param name="tags">The container's tags.</param>
    /// <returns>The chance, or null to keep the game's decision.</returns>
    public double? SpawnChance(string? id, IReadOnlyCollection<string> tags) => Rules.LastOrDefault(rule => rule.SpawnChance is not null && rule.Covers(id, tags))?.SpawnChance;

    /// <summary>The rule that decides whether a container socket spawns: the last covering rule that sets a spawn chance or a group fill.</summary>
    /// <param name="id">The socket's container id.</param>
    /// <param name="tag">The socket's tag.</param>
    /// <returns>The rule, or null to keep the game's decision.</returns>
    public LootRule? SocketSpawn(string id, string tag) =>
        Rules.LastOrDefault(rule => (rule.SpawnChance is not null || rule.GroupFill is not null) && rule.Covers(id, [tag]));

    /// <summary>The rule that sets a container's contents.</summary>
    /// <param name="id">The container's id, or null.</param>
    /// <param name="tag">The container's tag, or null.</param>
    /// <returns>The rule, or null to keep the default roll.</returns>
    public LootRule? LootOf(string? id, string? tag) => Rules.LastOrDefault(rule => rule.Loot is not null && rule.Covers(id, tag is null ? [] : [tag]));

    /// <summary>The reveal delay the rules set for a container.</summary>
    /// <param name="id">The container's id, or null.</param>
    /// <param name="tags">The container's tags.</param>
    /// <returns>The delay in seconds, or null to keep the container class's own.</returns>
    public double? RevealDelaySeconds(string? id, IReadOnlyCollection<string> tags) =>
        Rules.LastOrDefault(rule => rule.RevealDelaySeconds is not null && rule.Covers(id, tags))?.RevealDelaySeconds;
}

/// <summary>How many sockets of a group get a container: a number drawn by weight, at most the group's sockets.</summary>
/// <param name="Weights">Relative weight of each number of filled sockets.</param>
public sealed record LootFill(IReadOnlyDictionary<int, double> Weights)
{
    /// <summary>Draws how many sockets of a group get a container.</summary>
    /// <param name="sockets">Sockets of the group.</param>
    /// <param name="random">The raid's random numbers.</param>
    /// <returns>The number, from 0 to <paramref name="sockets"/>.</returns>
    public int Draw(int sockets, Random random)
    {
        ArgumentNullException.ThrowIfNull(random);
        KeyValuePair<int, double>[] entries = [.. Weights.Where(entry => entry.Value > 0).OrderBy(entry => entry.Key)];
        if (entries.Length == 0)
        {
            return 0;
        }

        double roll = random.NextDouble() * entries.Sum(entry => entry.Value);
        foreach ((int count, double weight) in entries)
        {
            if (roll < weight)
            {
                return Math.Min(count, sockets);
            }

            roll -= weight;
        }

        return Math.Min(entries[^1].Key, sockets);
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
