using System.Globalization;
using System.Text.Json;
using Ares.Common.Game.Content;
using Ares.Common.Game.Loot;
using Ares.Common.Testing;
using Ares.Raid.World;
using Ares.Raid.World.Loot;
using Ares.Raid.World.Replication;
using Ares.Structs.Game.Inventory;
using Ares.Structs.Game.Loot;
using Ares.Structs.Game.World;

namespace Ares.Raid.Tests.World;

/// <summary>The operator's loot rules (<c>loot-rules.json</c>) in seed-fixed Dam raids.</summary>
public sealed class LootRuleTests : IDisposable
{
    private const int Seed = 7;

    private static readonly Lazy<string> s_round = new(() => TestGameData.Dam.Rounds[0].Name);

    private static readonly Lazy<ReplicationContext> s_replication = new(() => new ScriptedPlayer().Worlds.Current!.State.Replication);

    // The shipped content with an empty rules file: every container is left to the game.
    private static readonly Lazy<RaidGameData> s_noRules = new(() =>
    {
        string directory = Path.Combine(Path.GetTempPath(), "ares-loot-rules-" + Guid.NewGuid().ToString("N"));
        try
        {
            ShippedContent.CopyTo(directory);
            File.WriteAllText(Path.Combine(directory, AresContent.LootRulesFile), """{ "rules": [] }""");
            return new RaidGameData(TestGameData.Data, AresContent.Load(directory));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    });

    private readonly List<string> _directories = [];

    public void Dispose()
    {
        foreach (string directory in _directories.Where(Directory.Exists))
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void Without_rules_a_raid_spawns_the_sockets_the_game_selects_and_fills_every_container_by_default()
    {
        RaidGameData empty = s_noRules.Value;
        Assert.Same(LootRules.None, empty.LootRules);
        LootSocket[] sockets = [.. ServableSockets()];

        for (int seed = 1; seed <= 3; seed++)
        {
            LootCatalog actual = Catalog(empty, seed);
            LootSpawnSelection game = LootSpawnSelection.Select(TestGameData.Loot, sockets, LootSpawnRules.Of(TestGameData.Dam, empty.LootSpawn), [], new Random(seed));

            Assert.Equal(sockets.Where((_, index) => game.Spawns[index]), actual.Containers.Where(container => container.Source == LootContainerSource.Socket).Select(container => container.Socket!));
            Assert.Equal((game.SkippedByRoll, game.SkippedByGroupCap, game.SkippedByLimiter), (actual.Counts.SkippedByRoll, actual.Counts.SkippedByGroupCap, actual.Counts.SkippedByLimiter));
            Assert.Equal(Signature(actual), Signature(Catalog(empty, seed)));
            Assert.All(actual.Containers, container => Assert.False(container.Contents.Pool?.StartsWith("rule ", StringComparison.Ordinal) ?? false));
            Assert.All(actual.Containers.Where(container => container.Source == LootContainerSource.Socket), container => Assert.Equal(LootContainerIds.Of(container.Socket!), container.Id));
            Assert.All(actual.Containers, container => Assert.NotNull(container.Id));
        }
    }

    [Fact]
    public void A_tag_rule_with_spawn_chance_0_leaves_no_container_of_that_tag()
    {
        LootCatalog before = Catalog(TestGameData.Raid);
        string tag = CommonParentTag(before);
        Assert.Contains(before.Containers, container => IsBelow(container, tag));

        LootCatalog after = Catalog(Raid($$"""{ "rules": [{ "name": "none", "tags": [{{Json(tag)}}], "spawnChance": 0 }] }"""));

        Assert.DoesNotContain(after.Containers, container => IsBelow(container, tag));
        Assert.Contains(after.Containers, container => container.Source == LootContainerSource.Socket);
        Assert.True(after.Counts.SkippedByRoll > before.Counts.SkippedByRoll);
    }

    [Fact]
    public void A_socket_rule_with_spawn_chance_1_spawns_it_even_past_group_caps_and_limiters()
    {
        // Every roll succeeds, so the sockets left empty are those the group caps and limiters blocked.
        LootSpawnRules always = LootSpawnRules.Of(TestGameData.Dam, TestGameData.Raid.LootSpawn) with { Chance = (_, _) => 1 };
        LootCatalog before = Catalog(s_noRules.Value, Seed, always);
        Assert.Equal(0, before.Counts.SkippedByRoll);
        Assert.True(before.Counts.SkippedByGroupCap + before.Counts.SkippedByLimiter > 0);
        HashSet<string> spawned = [.. before.Containers.Select(container => container.Id!)];
        LootSocket[] blocked = [.. ServableSockets().Where(socket => !spawned.Contains(LootContainerIds.Of(socket)))];
        Assert.NotEmpty(blocked);
        LootSocket chosen = blocked[blocked.Length / 2];
        string id = LootContainerIds.Of(chosen);

        LootCatalog after = Catalog(Raid($$"""{ "rules": [{ "containers": [{{Json(id)}}], "spawnChance": 1 }] }"""), Seed, always);

        LootContainer container = Assert.Single(after.Containers, container => container.Id == id);
        Assert.Equal((LootContainerSource.Socket, chosen), (container.Source, container.Socket));
        Assert.Equal(before.Counts.Spawned + 1, after.Counts.Spawned);

        // With the game's own chances, the socket spawns too.
        LootCatalog rolled = Catalog(Raid($$"""{ "rules": [{ "containers": [{{Json(id)}}], "spawnChance": 1 }] }"""));
        Assert.Single(rolled.Containers, entry => entry.Id == id);
    }

    [Fact]
    public void A_loot_rule_fills_the_covered_containers_from_its_list_only()
    {
        LootCatalog before = Catalog(s_noRules.Value);
        string tag = CommonParentTag(before);
        ItemDefinition stackable = TestGameData.Items.Definitions.ContainerItems.First(item => item.MaxStack >= 5);
        ItemDefinition single = TestGameData.Items.Definitions.ContainerItems.First(item => item.MaxStack == 1);
        string text = $$"""
            {
              "rules": [{
                "name": "listed",
                "tags": [{{Json(tag)}}],
                "loot": {
                  "stacks": { "min": 1, "max": 3 },
                  "items": [
                    { "item": {{Json(stackable.TechnicalName)}}, "amount": { "min": 2, "max": 5 } },
                    { "item": {{Json(single.TechnicalName)}}, "weight": 3 }
                  ]
                }
              }]
            }
            """;

        LootCatalog after = Catalog(Raid(text));

        // Contents draw apart from the spawn rolls, so the same containers spawn.
        Assert.Equal(before.Containers.Select(container => (container.Source, container.Location, container.Class.Container.Class)), after.Containers.Select(container => (container.Source, container.Location, container.Class.Container.Class)));
        LootContainer[] covered = [.. after.Containers.Where(container => IsBelow(container, tag))];
        Assert.NotEmpty(covered);
        foreach (LootContainer container in covered)
        {
            Assert.Equal("rule listed", container.Contents.Pool);
            Assert.InRange(container.Contents.Stacks.Count, Math.Min(1, container.Class.SlotCount), Math.Min(3, container.Class.SlotCount));
            foreach (LootStack stack in container.Contents.Stacks)
            {
                Assert.Equal(1.0, stack.Durability);
                if (stack.Item.TechnicalName == stackable.TechnicalName)
                {
                    Assert.InRange(stack.Amount, 2, 5);
                }
                else
                {
                    Assert.Equal((single.TechnicalName, 1), (stack.Item.TechnicalName, stack.Amount));
                }
            }
        }

        LootStack[] drawn = [.. covered.SelectMany(container => container.Contents.Stacks)];
        Assert.Contains(drawn, stack => stack.Item.TechnicalName == stackable.TechnicalName);
        Assert.Contains(drawn, stack => stack.Item.TechnicalName == single.TechnicalName);
        Assert.Contains(covered, container => container.Contents.Stacks.Count > 1);
        Assert.All(after.Containers.Where(container => !IsBelow(container, tag)), container => Assert.NotEqual("rule listed", container.Contents.Pool));
    }

    [Fact]
    public void A_container_rule_sets_the_contents_of_that_container_over_an_earlier_tag_rule()
    {
        LootCatalog before = Catalog(s_noRules.Value);
        LootContainer target = before.Containers.First(container => container is { Source: LootContainerSource.Socket, Class.SlotCount: >= 2 });
        ItemDefinition single = TestGameData.Items.Definitions.ContainerItems.First(item => item.MaxStack == 1);
        string text = $$"""
            {
              "rules": [
                { "name": "all", "loot": { "items": [{ "item": {{Json(single.TechnicalName)}} }] } },
                { "name": "one", "containers": [{{Json(target.Id!)}}], "loot": { "stacks": { "min": 2, "max": 2 }, "items": [{ "item": {{Json(single.TechnicalName)}} }] } }
              ]
            }
            """;

        LootCatalog after = Catalog(Raid(text));

        LootContainer ruled = Assert.Single(after.Containers, container => container.Id == target.Id);
        Assert.Equal(("rule one", 2), (ruled.Contents.Pool, ruled.Contents.Stacks.Count));
        Assert.All(after.Containers.Where(container => container.Id != target.Id), container => Assert.Equal("rule all", container.Contents.Pool));
    }

    [Fact]
    public void A_rule_stack_count_is_capped_by_the_container_slots()
    {
        ItemDefinition single = TestGameData.Items.Definitions.ContainerItems.First(item => item.MaxStack == 1);
        LootList list = new(new LootRange(50, 50), [new LootListItem(single, 1, new LootRange(1, 1))]);

        LootContents contents = TestGameData.Raid.LootRolls.Roll("many", list, 4, new Random(1));

        Assert.Equal(("rule many", 4), (contents.Pool, contents.Stacks.Count));
    }

    [Fact]
    public void A_rule_stack_range_up_to_the_largest_count_the_file_accepts_still_rolls()
    {
        // The content check accepts any stacks max (int.MaxValue included); the roll must not overflow on it.
        ItemDefinition single = TestGameData.Items.Definitions.ContainerItems.First(item => item.MaxStack == 1);
        LootList list = new(new LootRange(1, int.MaxValue), [new LootListItem(single, 1, new LootRange(1, 1))]);

        LootContents contents = TestGameData.Raid.LootRolls.Roll("all", list, 6, new Random(1));

        Assert.InRange(contents.Stacks.Count, 1, 6);
    }

    [Fact]
    public void Every_spawner_a_rule_can_name_belongs_to_a_loot_group()
    {
        HashSet<string> grouped = [.. FreeStandingLoot.Of(TestGameData.Dam, TestGameData.Raid.LootSpawn).Spawners.Select(LootContainerIds.Of)];

        Assert.All(TestGameData.Dam.DynamicLootSpawners, spawner => Assert.Contains(LootContainerIds.Of(spawner), grouped));
    }

    [Fact]
    public void A_spawner_rule_with_spawn_chance_1_runs_the_spawner_and_0_removes_its_containers()
    {
        LootCatalog before = Catalog(TestGameData.Raid);
        HashSet<string> ran = [.. before.Containers.Where(container => container.Source == LootContainerSource.Spawner).Select(container => container.Id!)];
        Assert.NotEmpty(ran);
        // Only the round's groups can run a spawner, a set chance included.
        HashSet<DynamicLootSpawner> inRound = [.. TestGameData.Dam.DynamicLootGroups.Where(group => group.Rounds.Contains(s_round.Value, StringComparer.Ordinal)).SelectMany(group => group.Spawners)];
        DynamicLootSpawner idle = inRound.First(spawner => spawner.DataLayer is null && spawner.Count > 0
            && !ran.Contains(LootContainerIds.Of(spawner)) && spawner.Classes.All(entry => LootSocketClasses.Served(TestGameData.Loot, entry.Class) is not null));
        string idleId = LootContainerIds.Of(idle);
        string otherRoundId = LootContainerIds.Of(FreeStandingLoot.Of(TestGameData.Dam, TestGameData.Raid.LootSpawn).Spawners.First(spawner => !inRound.Contains(spawner)));
        string ranId = ran.Order(StringComparer.Ordinal).First();

        LootCatalog after = Catalog(Raid($$"""
            { "rules": [{ "containers": [{{Json(idleId)}}], "spawnChance": 1 }, { "containers": [{{Json(ranId)}}], "spawnChance": 0 }, { "containers": [{{Json(otherRoundId)}}], "spawnChance": 1 }] }
            """));

        LootContainer[] placed = [.. after.Containers.Where(container => container.Id == idleId)];
        Assert.NotEmpty(placed);
        Assert.All(placed, container => Assert.Equal((LootContainerSource.Spawner, (LootSocket?)null), (container.Source, container.Socket)));
        Assert.All(placed, container => Assert.Contains(idle.Points, point => point.Location == container.Location));
        Assert.DoesNotContain(after.Containers, container => container.Id == ranId || container.Id == otherRoundId);
    }

    [Fact]
    public void A_placed_container_rule_replaces_the_placed_container_roll()
    {
        PlacedLootContainer chosen = TestGameData.Dam.PlacedLoot.First(container => container.DataLayer is null && LootSocketClasses.Served(TestGameData.Loot, container.Class) is not null);
        string id = LootContainerIds.Of(chosen);
        string rule = $$"""{ "rules": [{ "containers": [{{Json(id)}}], "spawnChance": {0} }] }""";
        int placeable = TestGameData.Dam.PlacedLoot.Count(container => container.DataLayer is null);

        LootCatalog never = Catalog(Raid(rule.Replace("{0}", "1", StringComparison.Ordinal)), Seed, LootSpawnRules.Of(TestGameData.Dam, TestGameData.Raid.LootSpawn) with { Chance = (_, _) => 0 });
        LootContainer only = Assert.Single(never.Containers, container => container.Source == LootContainerSource.Placed);
        Assert.Equal((id, chosen.Location), (only.Id, only.Location));
        Assert.Equal((1, placeable - 1), (never.Counts.Placed.Spawned, never.Counts.Placed.SkippedByRoll));

        LootCatalog always = Catalog(Raid(rule.Replace("{0}", "0", StringComparison.Ordinal)), Seed, LootSpawnRules.Of(TestGameData.Dam, TestGameData.Raid.LootSpawn) with { Chance = (_, _) => 1 });
        Assert.DoesNotContain(always.Containers, container => container.Id == id);
        Assert.Equal((placeable - 1, 1), (always.Counts.Placed.Spawned, always.Counts.Placed.SkippedByRoll));
    }

    [Theory]
    [InlineData("Socket.LootContainer.Tech.Server.Door", new[] { 6.0, 4, 9 })]
    [InlineData("Socket.LootContainer.Industrial.Lockers.Door", new[] { 32.0, 5, 3 })]
    public void Shipped_racks_get_0_1_or_2_containers_by_the_shipped_weights(string tag, double[] weights)
    {
        const int Raids = 200;
        IGrouping<(string, string, string), LootSocket>[] racks = [.. ServableSockets()
            .Where(socket => string.Equals(socket.ContainerTag, tag, StringComparison.OrdinalIgnoreCase))
            .GroupBy(socket => (socket.Cell, socket.Actor, socket.Component))];
        Assert.NotEmpty(racks);
        Assert.All(racks, rack => Assert.True(rack.Count() >= 2, $"{rack.Key} has {rack.Count()} sockets"));
        int[] histogram = new int[3];
        for (int seed = 1; seed <= Raids; seed++)
        {
            HashSet<LootSocket> spawned = [.. Catalog(TestGameData.Raid, seed).Containers.Select(container => container.Socket).OfType<LootSocket>()];
            foreach (IGrouping<(string, string, string), LootSocket> rack in racks)
            {
                int filled = rack.Count(spawned.Contains);
                Assert.InRange(filled, 0, 2);
                histogram[filled]++;
            }
        }

        int draws = racks.Length * Raids;
        for (int count = 0; count < 3; count++)
        {
            double expected = weights[count] / weights.Sum();
            double margin = 4 * Math.Sqrt(expected * (1 - expected) / draws);
            Assert.InRange(histogram[count] / (double)draws, expected - margin, expected + margin);
        }
    }

    [Fact]
    public void Containers_a_group_fill_rule_lists_form_one_group_across_props_and_tags()
    {
        LootSocket[] listed = [.. ServableSockets()
            .GroupBy(socket => socket.ContainerTag, StringComparer.OrdinalIgnoreCase)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .Take(4)
            .Select(group => group.First())];
        Assert.Equal(4, listed.DistinctBy(socket => (socket.Cell, socket.Actor, socket.Component)).Count());
        string[] ids = [.. listed.Select(LootContainerIds.Of)];
        RaidGameData data = Raid($$"""{ "rules": [{ "containers": [{{string.Join(", ", ids.Select(Json))}}], "groupFill": { "1": 1 } }] }""");

        HashSet<string> chosen = [];
        for (int seed = 1; seed <= 20; seed++)
        {
            LootContainer only = Assert.Single(Catalog(data, seed).Containers, container => container.Id is { } id && ids.Contains(id));
            _ = chosen.Add(only.Id!);
        }

        Assert.True(chosen.Count > 1);
    }

    [Fact]
    public void A_tag_rule_with_a_group_fill_of_0_leaves_no_container_of_that_tag()
    {
        LootCatalog before = Catalog(s_noRules.Value);
        string tag = CommonParentTag(before);
        Assert.Contains(before.Containers, container => IsBelow(container, tag));
        RaidGameData none = Raid($$"""{ "rules": [{ "tags": [{{Json(tag)}}], "groupFill": { "0": 1 } }] }""");

        for (int seed = 1; seed <= 3; seed++)
        {
            LootCatalog after = Catalog(none, seed);
            Assert.DoesNotContain(after.Containers, container => IsBelow(container, tag));
            Assert.Contains(after.Containers, container => container.Source == LootContainerSource.Socket);
        }
    }

    [Fact]
    public void Group_fill_sockets_spawn_past_group_caps_and_limiters()
    {
        // Every roll succeeds, so the sockets left empty are those the group caps and limiters blocked.
        LootSpawnRules always = LootSpawnRules.Of(TestGameData.Dam, TestGameData.Raid.LootSpawn) with { Chance = (_, _) => 1 };
        LootCatalog before = Catalog(s_noRules.Value, Seed, always);
        HashSet<string> spawned = [.. before.Containers.Select(container => container.Id!)];
        LootSocket[] limited = [.. ServableSockets().Where(socket => !spawned.Contains(LootContainerIds.Of(socket)) && InLimiter(socket))];
        Assert.NotEmpty(limited);
        string limitedId = LootContainerIds.Of(limited[limited.Length / 2]);

        // A tag whose props hold more sockets than its group cap allows.
        const string Tag = "Socket.LootContainer.Tech.Server.Door";
        LootSocket[] racks = [.. ServableSockets().Where(socket => string.Equals(socket.ContainerTag, Tag, StringComparison.OrdinalIgnoreCase))];
        Assert.Contains(racks.GroupBy(socket => (socket.Cell, socket.Actor, socket.Component)), rack => rack.Count() > TestGameData.Loot.FindContainerType(Tag)!.MaxPerGroup);

        LootCatalog after = Catalog(Raid($$"""
            { "rules": [{ "tags": [{{Json(Tag)}}], "groupFill": { "99": 1 } }, { "containers": [{{Json(limitedId)}}], "groupFill": { "1": 1 } }] }
            """), Seed, always);

        HashSet<string> filled = [.. after.Containers.Select(container => container.Id!)];
        Assert.All(racks, socket => Assert.Contains(LootContainerIds.Of(socket), filled));
        Assert.Contains(limitedId, filled);
    }

    private static bool InLimiter(LootSocket socket) => TestGameData.Dam.LootLimiters.Any(limiter => limiter.DataLayer is null
        && limiter.Limits.Any(limit => !limit.Unlimited && LootLimiterRules.Matches(limit.Tag, socket.ContainerTag)) && LootLimiterRules.IsPointInside(limiter, socket.Location));

    private static LootCatalog Catalog(RaidGameData data, int seed = Seed, LootSpawnRules? rules = null) =>
        new(data, s_replication.Value, s_round.Value, [], seed, rules ?? LootSpawnRules.Of(TestGameData.Dam, TestGameData.Raid.LootSpawn));

    private static IEnumerable<string> Signature(LootCatalog catalog) => catalog.Containers.Select(container => string.Create(CultureInfo.InvariantCulture,
        $"{container.Source} {container.Id} {container.Location} {container.Class.Container.Class} {container.Contents.Pool}: {string.Join(", ", container.Contents.Stacks.Select(stack => $"{stack.Item.TechnicalName} x{stack.Amount} {stack.Durability}"))}"));

    // The servable sockets of a raid without data layers.
    private static IEnumerable<LootSocket> ServableSockets() =>
        TestGameData.Dam.LootSockets.Where(socket => socket.DataLayer is null && LootSocketClasses.Supported(TestGameData.Data, socket).Count > 0);

    // The three-segment tag (Socket.LootContainer.X) most of a raid's containers fall under.
    private static string CommonParentTag(LootCatalog catalog) => catalog.Containers
        .Select(container => container.Tag)
        .OfType<string>()
        .Select(tag => string.Join('.', tag.Split('.').Take(3)))
        .GroupBy(tag => tag, StringComparer.OrdinalIgnoreCase)
        .OrderByDescending(group => group.Count())
        .ThenBy(group => group.Key, StringComparer.Ordinal)
        .First().Key;

    private static bool IsBelow(LootContainer container, string tag) => container.Tag is { } own && LootTags.IsTagOrBelow(own, tag);

    private static string Json(string value) => JsonSerializer.Serialize(value);

    private RaidGameData Raid(string rules)
    {
        string directory = Path.Combine(Path.GetTempPath(), "ares-loot-rules-" + Guid.NewGuid().ToString("N"));
        ShippedContent.CopyTo(directory);
        _directories.Add(directory);
        File.WriteAllText(Path.Combine(directory, AresContent.LootRulesFile), rules);
        return new RaidGameData(TestGameData.Data, AresContent.Load(directory));
    }
}
