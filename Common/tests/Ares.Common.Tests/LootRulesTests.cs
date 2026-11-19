using Ares.Common.Game.Loot;

namespace Ares.Common.Tests;

/// <summary>Which containers an operator loot rule covers, and which rule wins for each setting (made-up rules, no game data).</summary>
public sealed class LootRulesTests
{
    private const string Tech = "Socket.LootContainer.Tech";
    private const string ServerDoor = "Socket.LootContainer.Tech.Server.Door";
    private const string Medical = "Socket.LootContainer.Medical";
    private const string SocketId = "Cell/Actor/Component/Socket";
    private const string OtherId = "/Actor_2";

    [Fact]
    public void A_tag_is_below_its_parents_at_segment_boundaries_ignoring_case()
    {
        Assert.True(LootTags.IsTagOrBelow(ServerDoor, Tech));
        Assert.True(LootTags.IsTagOrBelow(Tech, Tech));
        Assert.True(LootTags.IsTagOrBelow(ServerDoor.ToUpperInvariant(), Tech));
        Assert.False(LootTags.IsTagOrBelow(Tech, ServerDoor));
        Assert.False(LootTags.IsTagOrBelow("Socket.LootContainer.Technical", Tech));
        Assert.False(LootTags.IsTagOrBelow(Medical, Tech));
    }

    [Fact]
    public void A_rule_without_tags_or_containers_covers_every_container()
    {
        LootRule rule = Rule(chance: 1);

        Assert.True(rule.Covers(SocketId, [ServerDoor]));
        Assert.True(rule.Covers(null, [Medical]));
        Assert.True(rule.Covers(null, []));
    }

    [Fact]
    public void A_tag_rule_covers_containers_with_its_tag_or_a_tag_below_it()
    {
        LootRule rule = Rule(tags: [Tech], chance: 1);

        Assert.True(rule.Covers(null, [Tech]));
        Assert.True(rule.Covers(null, [ServerDoor]));
        Assert.True(rule.Covers(null, [ServerDoor.ToLowerInvariant()]));
        Assert.True(rule.Covers(OtherId, [Medical, ServerDoor]));
        Assert.False(rule.Covers(SocketId, [Medical]));
        Assert.False(rule.Covers(null, ["Socket.LootContainer.Technical"]));
        Assert.False(rule.Covers(null, ["Socket.LootContainer"]));
        Assert.False(rule.Covers(SocketId, []));
    }

    [Fact]
    public void A_container_rule_covers_only_the_listed_ids_exactly()
    {
        LootRule rule = Rule(containers: [SocketId], chance: 1);

        Assert.True(rule.Covers(SocketId, []));
        Assert.True(rule.Covers(SocketId, [Medical]));
        Assert.False(rule.Covers(SocketId.ToUpperInvariant(), [Medical]));
        Assert.False(rule.Covers("Cell/Actor", [Medical]));
        Assert.False(rule.Covers(null, [Medical]));
    }

    [Fact]
    public void A_rule_with_tags_and_containers_covers_a_container_matching_either()
    {
        LootRule rule = Rule(tags: [Tech], containers: [SocketId], chance: 1);

        Assert.True(rule.Covers(SocketId, [Medical]));
        Assert.True(rule.Covers(OtherId, [ServerDoor]));
        Assert.True(rule.Covers(null, [ServerDoor]));
        Assert.False(rule.Covers(OtherId, [Medical]));
    }

    [Fact]
    public void The_last_rule_that_covers_a_container_and_sets_a_spawn_chance_wins()
    {
        LootRules rules = new([
            Rule(chance: 0.5),
            Rule(tags: [Tech], chance: 0.25),
            Rule(containers: [SocketId], chance: 1),
            Rule(tags: [Medical], loot: List()),
        ]);

        Assert.Equal(1, rules.SpawnChance(SocketId, [Medical]));
        Assert.Equal(1, rules.SpawnChance(SocketId, [ServerDoor]));
        Assert.Equal(0.25, rules.SpawnChance(OtherId, [ServerDoor]));
        Assert.Equal(0.25, rules.SpawnChance(OtherId, [Medical, ServerDoor]));
        Assert.Equal(0.5, rules.SpawnChance(OtherId, [Medical]));
        Assert.Equal(0.5, rules.SpawnChance(null, []));
    }

    [Fact]
    public void A_rule_that_sets_only_loot_keeps_the_spawn_decision_and_one_that_sets_only_a_chance_keeps_the_contents()
    {
        LootRule loot = Rule(tags: [Tech], loot: List());
        LootRule chance = Rule(containers: [SocketId], chance: 0);
        LootRules rules = new([loot, chance]);

        Assert.Null(rules.SpawnChance(OtherId, [ServerDoor]));
        Assert.Equal(0, rules.SpawnChance(SocketId, [ServerDoor]));
        Assert.Same(loot, rules.LootOf(SocketId, ServerDoor));
        Assert.Null(rules.LootOf(SocketId, Medical));
    }

    [Fact]
    public void The_last_rule_that_covers_a_container_and_sets_loot_gives_its_contents()
    {
        LootRule all = Rule(loot: List());
        LootRule tech = Rule(tags: [Tech], loot: List());
        LootRule socket = Rule(containers: [SocketId], loot: List());
        LootRules rules = new([all, tech, socket, Rule(chance: 1)]);

        Assert.Same(socket, rules.LootOf(SocketId, ServerDoor));
        Assert.Same(socket, rules.LootOf(SocketId, null));
        Assert.Same(tech, rules.LootOf(OtherId, ServerDoor));
        Assert.Same(tech, rules.LootOf(null, ServerDoor));
        Assert.Same(all, rules.LootOf(OtherId, Medical));
        Assert.Same(all, rules.LootOf(null, null));
        Assert.Same(all, new LootRules([all, Rule(containers: [OtherId], loot: List()), Rule(tags: [Tech], loot: List())]).LootOf(SocketId, Medical));
    }

    [Fact]
    public void No_rules_keep_every_default()
    {
        Assert.Empty(LootRules.None.Rules);
        Assert.Null(LootRules.None.SpawnChance(SocketId, [ServerDoor]));
        Assert.Null(LootRules.None.LootOf(SocketId, ServerDoor));
        Assert.Null(LootRules.None.LootOf(null, null));
    }

    [Fact]
    public void A_group_fill_draws_each_number_of_sockets_by_its_weight()
    {
        LootFill fill = new(new Dictionary<int, double> { [0] = 6, [1] = 4, [2] = 9 });
        Random random = new(11);
        const int Draws = 19000;
        int[] counts = new int[3];
        for (int draw = 0; draw < Draws; draw++)
        {
            counts[fill.Draw(5, random)]++;
        }

        foreach ((int count, double weight) in fill.Weights)
        {
            double expected = weight / 19;
            Assert.InRange(counts[count] / (double)Draws, expected - (4 * Math.Sqrt(expected * (1 - expected) / Draws)), expected + (4 * Math.Sqrt(expected * (1 - expected) / Draws)));
        }
    }

    [Fact]
    public void A_group_fill_never_fills_more_sockets_than_the_group_has()
    {
        LootFill fill = new(new Dictionary<int, double> { [1] = 1, [4] = 3 });
        Random random = new(3);

        int[] ofTwo = [.. Enumerable.Range(0, 400).Select(_ => fill.Draw(2, random))];
        Assert.Equal([1, 2], ofTwo.Distinct().Order());
        Assert.All(Enumerable.Range(0, 50), _ => Assert.Equal(0, fill.Draw(0, random)));
        Assert.Equal([1, 4], Enumerable.Range(0, 400).Select(_ => fill.Draw(10, random)).Distinct().Order());
    }

    [Fact]
    public void A_group_fill_never_draws_a_number_of_weight_0()
    {
        LootFill fill = new(new Dictionary<int, double> { [0] = 0, [1] = 0, [2] = 1, [3] = 0 });
        Random random = new(5);

        Assert.All(Enumerable.Range(0, 500), _ => Assert.Equal(2, fill.Draw(3, random)));
    }

    [Fact]
    public void The_rule_deciding_a_socket_is_the_last_covering_one_setting_a_chance_or_a_group_fill()
    {
        LootFill fill = new(new Dictionary<int, double> { [1] = 1 });
        LootRule chance = Rule(tags: [Tech], chance: 0.5);
        LootRule grouped = Rule(tags: [ServerDoor], fill: fill);
        LootRules rules = new([chance, grouped, Rule(loot: List())]);

        Assert.Same(grouped, rules.SocketSpawn(SocketId, ServerDoor));
        Assert.Same(chance, rules.SocketSpawn(SocketId, Tech));
        Assert.Null(rules.SocketSpawn(SocketId, Medical));
        Assert.Null(rules.SpawnChance(SocketId, [Medical]));
        Assert.Equal(0.5, rules.SpawnChance(SocketId, [ServerDoor]));
    }

    private static LootList List() => new(null, []);

    private static LootRule Rule(string[]? tags = null, string[]? containers = null, double? chance = null, LootList? loot = null, LootFill? fill = null) =>
        new("test", tags ?? [], new HashSet<string>(containers ?? [], StringComparer.Ordinal), chance, loot, fill);
}
