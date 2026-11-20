using System.Globalization;
using System.Text.Json;
using Ares.Common.Game.Content;
using Ares.Common.Game.Inventory;
using Ares.Common.Game.Loot;
using Ares.Common.GameData;
using Ares.Common.Testing;
using Ares.Structs.Game.Inventory;
using Ares.Structs.Game.Loot;
using Ares.Structs.Game.World;

namespace Ares.Common.Tests;

/// <summary>
/// Real-data tier: <c>loot-rules.json</c> in a content folder, resolved against the extracted game data. Skipped without
/// <see cref="RealGameData.DirectoryVariable"/>.
/// </summary>
public sealed class LootRulesContentTests : IDisposable
{
    private static readonly Lazy<LoadoutDefinitions> s_definitions = new(() => ShippedContent.Value.ItemDefinitions(RealGameData.Data));

    private readonly string _directory = Path.Combine(Path.GetTempPath(), "ares-loot-rules-" + Guid.NewGuid().ToString("N"));

    private static GameDataSet Data => RealGameData.Data;

    private static MapData Dam => Data.Names.Dam;

    private static LoadoutDefinitions Definitions => s_definitions.Value;

    // A container item that stacks, one that does not, and catalogue items no container may hold.
    private static ItemDefinition Stackable => Definitions.ContainerItems.First(item => item.MaxStack > 1);

    private static ItemDefinition Single => Definitions.ContainerItems.First(item => item.MaxStack == 1);

    private static ItemDefinition Outside => Data.Items.All.First(item => Definitions.ContainerItems.All(container => container.TechnicalName != item.TechnicalName));

    // The parent of a container type's tag: a tag rule covers it through the tags below it.
    private static string ParentTag
    {
        get
        {
            string tag = Data.Loot.ContainerTypes[0].Tag;
            return tag[..tag.LastIndexOf('.')];
        }
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    [RealDataFact]
    public void The_shipped_content_fills_server_and_locker_racks_by_group()
    {
        Assert.Contains(AresContent.LootRulesFile, AresContent.FileNames);
        LootRules rules = ShippedContent.Value.LootRules(Data, Definitions);

        // Every shipped rule fills the props of container tags by group, and only that.
        Assert.All(rules.Rules, rule =>
        {
            Assert.Equal((0, (double?)null, (LootList?)null), (rule.Containers.Count, rule.SpawnChance, rule.Loot));
            Assert.NotEmpty(rule.Tags);
            Assert.True(Weights(rule).Sum(entry => entry.Weight) > 0, rule.Name);
        });

        // Server racks (5 doors) fill none, one or two of their doors.
        LootRule servers = Assert.Single(rules.Rules, rule => rule.Name == "Server racks");
        Assert.Equal(["Socket.LootContainer.Tech.Server.Door"], servers.Tags);
        Assert.Equal([(0, 6.0), (1, 4.0), (2, 9.0)], Weights(servers));

        // Locker racks (6 doors) can fill any number of their doors, and an empty rack is the exception.
        LootRule lockers = Assert.Single(rules.Rules, rule => rule.Name == "Locker racks");
        Assert.Equal(["Socket.LootContainer.Industrial.Lockers.Door"], lockers.Tags);
        Assert.Equal(Enumerable.Range(0, 7), Weights(lockers).Select(entry => entry.Count));
        Assert.True(Weights(lockers)[0].Weight < Weights(lockers).Skip(1).Sum(entry => entry.Weight));
    }

    [RealDataFact]
    public void A_rule_may_set_only_a_reveal_delay()
    {
        LootRules rules = Load($$"""{ "rules": [{ "name": "slow", "tags": [{{Json(ParentTag)}}], "revealDelaySeconds": 2.5 }] }""").LootRules(Data, Definitions);

        LootRule rule = Assert.Single(rules.Rules);
        Assert.Equal(("slow", (double?)null, (LootList?)null, (LootFill?)null, (double?)2.5), (rule.Name, rule.SpawnChance, rule.Loot, rule.GroupFill, rule.RevealDelaySeconds));
        Assert.Equal(2.5, rules.RevealDelaySeconds(null, [Data.Loot.ContainerTypes[0].Tag]));
        Assert.Null(rules.SpawnChance(null, [Data.Loot.ContainerTypes[0].Tag]));
    }

    [RealDataFact]
    public void A_rule_may_set_only_a_group_fill()
    {
        string socket = LootContainerIds.Of(Dam.LootSockets[0]);
        LootRules rules = Load($$"""{ "rules": [{ "name": "racks", "tags": [{{Json(ParentTag)}}], "groupFill": { "0": 1, "2": 3.5, "7": 0 } }] }""").LootRules(Data, Definitions);

        LootRule rule = Assert.Single(rules.Rules);
        Assert.Equal(("racks", (double?)null, (LootList?)null), (rule.Name, rule.SpawnChance, rule.Loot));
        Assert.Equal([(0, 1.0), (2, 3.5), (7, 0.0)], Weights(rule));
        Assert.Same(rule, rules.SocketSpawn(socket, Data.Loot.ContainerTypes[0].Tag));
        Assert.Null(rules.SpawnChance(socket, [Data.Loot.ContainerTypes[0].Tag]));
        Assert.Null(rules.LootOf(socket, Data.Loot.ContainerTypes[0].Tag));
    }

    [RealDataFact]
    public void An_empty_rules_file_in_the_content_folder_has_no_rules()
    {
        AresContent content = Load("{\"rules\": []}");

        Assert.Same(LootRules.None, content.LootRules(Data, Definitions));
    }

    [RealDataFact]
    public void Every_container_and_loose_item_socket_id_of_the_dam_names_one_of_them()
    {
        string[] ids =
        [
            .. Dam.LootSockets.Select(LootContainerIds.Of),
            .. Dam.PlacedLoot.Select(LootContainerIds.Of),
            .. Dam.DynamicLootSpawners.Select(LootContainerIds.Of),
            .. Dam.LooseItemSockets.Select(LootContainerIds.Of),
        ];

        Assert.Equal(ids.Length, ids.Distinct(StringComparer.Ordinal).Count());
    }

    [RealDataFact]
    public void A_valid_file_resolves_its_rules_in_order_with_the_game_items()
    {
        string socket = LootContainerIds.Of(Dam.LootSockets[0]);
        string placed = LootContainerIds.Of(Dam.PlacedLoot[0]);
        string spawner = LootContainerIds.Of(Dam.DynamicLootSpawners[0]);
        string text = $$"""
            {
              "rules": [
                { "name": "fewer", "tags": [{{Json(ParentTag)}}], "spawnChance": 0.5 },
                {
                  "containers": [{{Json(socket)}}, {{Json(placed)}}, {{Json(spawner)}}],
                  "spawnChance": 1,
                  "loot": {
                    "stacks": { "min": 1, "max": 3 },
                    "items": [
                      { "item": {{Json(Stackable.TechnicalName)}}, "weight": 2.5, "amount": { "min": 1, "max": {{Stackable.MaxStack.ToString(CultureInfo.InvariantCulture)}} } },
                      { "item": {{Json(Single.TechnicalName)}} }
                    ]
                  }
                },
                { "name": "every container", "loot": { "items": [{ "item": {{Json(Single.TechnicalName)}} }] } }
              ]
            }
            """;

        LootRules rules = Load(text).LootRules(Data, Definitions);

        Assert.Equal(3, rules.Rules.Count);
        LootRule fewer = rules.Rules[0];
        Assert.Equal(("fewer", (double?)0.5, (LootList?)null), (fewer.Name, fewer.SpawnChance, fewer.Loot));
        Assert.Equal([ParentTag], fewer.Tags);
        Assert.Empty(fewer.Containers);

        LootRule chosen = rules.Rules[1];
        Assert.Equal(("#2", (double?)1.0), (chosen.Name, chosen.SpawnChance));
        Assert.Empty(chosen.Tags);
        Assert.True(chosen.Containers.SetEquals([socket, placed, spawner]));
        LootList list = Assert.IsType<LootList>(chosen.Loot);
        Assert.Equal(new LootRange(1, 3), list.Stacks);
        Assert.Equal(2, list.Items.Count);
        Assert.Same(Definitions.ContainerItems.First(item => item.TechnicalName == Stackable.TechnicalName), list.Items[0].Item);
        Assert.Equal((2.5, new LootRange(1, Stackable.MaxStack)), (list.Items[0].Weight, list.Items[0].Amount));
        Assert.Equal((Single.TechnicalName, 1.0, new LootRange(1, 1)), (list.Items[1].Item.TechnicalName, list.Items[1].Weight, list.Items[1].Amount));

        LootRule every = rules.Rules[2];
        Assert.Null(every.SpawnChance);
        Assert.Null(Assert.IsType<LootList>(every.Loot).Stacks);

        // The rules resolve per container: last rule setting each field wins.
        Assert.Equal(1, rules.SpawnChance(socket, [Dam.LootSockets[0].ContainerTag]));
        Assert.Equal(0.5, rules.SpawnChance("/NotListed", [Data.Loot.ContainerTypes[0].Tag]));
        Assert.Null(rules.SpawnChance("/NotListed", ["Socket.Unrelated"]));
        Assert.Same(every, rules.LootOf(socket, Dam.LootSockets[0].ContainerTag));
    }

    [RealDataFact]
    public void Loose_item_tags_and_ids_are_accepted_and_resolve_like_container_ones()
    {
        LooseItemSocket socket = Dam.LooseItemSockets[0];
        (string Id, string Tag) loose = (LootContainerIds.Of(socket), socket.Tag);
        string text = $$"""
            {
              "rules": [
                { "name": "loose family", "tags": ["socket.loot"], "spawnChance": 0.25 },
                { "name": "loose tag", "tags": [{{Json(loose.Tag)}}], "loot": { "items": [{ "item": {{Json(Single.TechnicalName)}} }] } },
                { "name": "loose id", "containers": [{{Json(loose.Id)}}], "spawnChance": 1 }
              ]
            }
            """;

        LootRules rules = Load(text).LootRules(Data, Definitions);

        Assert.Equal(1, rules.SpawnChance(loose.Id, [loose.Tag]));
        Assert.Equal(0.25, rules.SpawnChance("/NotListed", [loose.Tag]));
        Assert.Same(rules.Rules[1], rules.LootOf(loose.Id, loose.Tag));
        Assert.Null(rules.SpawnChance(LootContainerIds.Of(Dam.LootSockets[0]), [Dam.LootSockets[0].ContainerTag]));
    }

    [RealDataFact]
    public void Tags_and_ids_are_checked_by_the_game_data_but_tags_ignore_case()
    {
        LootRules rules = Load($$"""{ "rules": [{ "tags": [{{Json(ParentTag.ToLowerInvariant())}}], "spawnChance": 0 }] }""").LootRules(Data, Definitions);

        Assert.Equal(0, rules.SpawnChance(null, [Data.Loot.ContainerTypes[0].Tag]));
    }

    [RealDataTheory]
    [InlineData("""{ "rules": [{ "tags": ["Socket.LootContainer.NoSuchTag"], "spawnChance": 1 }] }""", "rule #1: no container or loose item socket has the tag Socket.LootContainer.NoSuchTag or a tag below it")]
    [InlineData("""{ "rules": [{ "tags": ["Socket.LootCont"], "spawnChance": 1 }] }""", "no container or loose item socket has the tag Socket.LootCont or")]
    [InlineData("""{ "rules": [{ "tags": ["Socket.Loot.NoSuchTag"], "spawnChance": 1 }] }""", "no container or loose item socket has the tag Socket.Loot.NoSuchTag or")]
    [InlineData("""{ "rules": [{ "name": "ids", "containers": ["NoCell/NoActor"], "spawnChance": 1 }] }""", "ids: no map has the container or loose item socket NoCell/NoActor")]
    [InlineData("""{ "rules": [{ "containers": ["{socket}/Extra"], "spawnChance": 1 }] }""", "no map has the container")]
    [InlineData("""{ "rules": [{ "containers": ["{loose}/Extra"], "spawnChance": 1 }] }""", "no map has the container or loose item socket {loose}/Extra")]
    [InlineData("""{ "rules": [{ "spawnChance": 1.5 }] }""", "rule #1: spawnChance must be from 0 to 1")]
    [InlineData("""{ "rules": [{ "spawnChance": -0.1 }] }""", "spawnChance must be from 0 to 1")]
    [InlineData("""{ "rules": [{ "spawnChance": 1 }, { "name": "idle", "tags": ["{tag}"] }] }""", "idle: sets none of spawnChance, groupFill, loot and revealDelaySeconds")]
    [InlineData("""{ "rules": [{ "revealDelaySeconds": -0.5 }] }""", "rule #1: revealDelaySeconds must be 0 or more")]
    [InlineData("""{ "rules": [{ "groupFill": { "one": 1 } }] }""", "rule #1: groupFill key one must be a number of sockets (0 or more)")]
    [InlineData("""{ "rules": [{ "groupFill": { "-1": 1 } }] }""", "groupFill key -1 must be a number of sockets (0 or more)")]
    [InlineData("""{ "rules": [{ "groupFill": { "1.5": 1 } }] }""", "groupFill key 1.5 must be a number of sockets (0 or more)")]
    [InlineData("""{ "rules": [{ "groupFill": { "0": 1, "1": -1 } }] }""", "rule #1: groupFill 1 must appear once with a weight of 0 or more")]
    [InlineData("""{ "rules": [{ "groupFill": { "1": 1, "01": 2 } }] }""", "groupFill 01 must appear once with a weight of 0 or more")]
    [InlineData("""{ "rules": [{ "groupFill": { "0": 0, "1": 0 } }] }""", "rule #1: groupFill needs a weight above 0")]
    [InlineData("""{ "rules": [{ "groupFill": { } }] }""", "groupFill needs a weight above 0")]
    [InlineData("""{ "rules": [{ "loot": { "items": [] } }] }""", "rule #1: loot must list at least one item")]
    [InlineData("""{ "rules": [{ "loot": { "items": [{ "item": "DA_Item_NoSuchItem" }] } }] }""", "the game data has no item DA_Item_NoSuchItem")]
    [InlineData("""{ "rules": [{ "loot": { "items": [{ "item": "{augment}" }] } }] }""", "{augment} cannot be put in a loot container yet")]
    [InlineData("""{ "rules": [{ "loot": { "items": [{ "item": "{outside}" }] } }] }""", "{outside} cannot be put in a loot container yet")]
    [InlineData("""{ "rules": [{ "loot": { "items": [{ "item": "{stackable}", "amount": { "min": 1, "max": {max+1} } }] } }] }""", "the amount of {stackable} must have 1 <= min <= max <= {max}")]
    [InlineData("""{ "rules": [{ "loot": { "items": [{ "item": "{single}", "amount": { "min": 1, "max": 2 } }] } }] }""", "the amount of {single} must have 1 <= min <= max <= 1")]
    [InlineData("""{ "rules": [{ "loot": { "items": [{ "item": "{stackable}", "amount": { "min": 2, "max": 1 } }] } }] }""", "the amount of {stackable} must have 1 <= min <= max")]
    [InlineData("""{ "rules": [{ "loot": { "items": [{ "item": "{stackable}", "amount": { "min": 0, "max": 1 } }] } }] }""", "the amount of {stackable} must have 1 <= min <= max")]
    [InlineData("""{ "rules": [{ "loot": { "stacks": { "min": 3, "max": 2 }, "items": [{ "item": "{single}" }] } }] }""", "rule #1: loot stacks must have 1 <= min <= max")]
    [InlineData("""{ "rules": [{ "loot": { "stacks": { "min": 0, "max": 2 }, "items": [{ "item": "{single}" }] } }] }""", "loot stacks must have 1 <= min <= max")]
    [InlineData("""{ "rules": [{ "loot": { "items": [{ "item": "{single}", "weight": 0 }] } }] }""", "the weight of {single} must be more than 0")]
    [InlineData("""{ "rules": [{ "loot": { "items": [{ "item": "{single}", "weight": -1 }] } }] }""", "the weight of {single} must be more than 0")]
    public void An_invalid_rule_is_refused_naming_the_rule_and_the_problem(string text, string expected)
    {
        AresContent content = Load(Fill(text));

        AresContentException error = Assert.Throws<AresContentException>(() => content.LootRules(Data, Definitions));
        Assert.StartsWith(Path.Combine(_directory, AresContent.LootRulesFile) + ": ", error.Message, StringComparison.Ordinal);
        Assert.Contains(Fill(expected), error.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("""{ "rules": [], "extra": 1 }""")]
    [InlineData("""{ "rules": [{ "spawnChance": 1, "chance": 1 }] }""")]
    [InlineData("""{ "rules": [{ "loot": { "items": [{ "item": "DA_Item_X" }], "count": 1 } }] }""")]
    [InlineData("""{ "rules": [{ "loot": { "items": [{ "item": "DA_Item_X", "count": 1 }] } }] }""")]
    [InlineData("""{ "rules": [{ "loot": { "items": [{ "item": "DA_Item_X", "amount": { "min": 1, "max": 2, "mean": 1 } }] } }] }""")]
    [InlineData("{ }")]
    [InlineData("""{ "rules": [{ "loot": { } }] }""")]
    [InlineData("""{ "rules": [{ "loot": { "items": [{ "weight": 1 }] } }] }""")]
    [InlineData("""{ "rules": [{ "loot": { "items": [{ "item": "DA_Item_X", "amount": { "min": 1 } }] } }] }""")]
    [InlineData("""{ "rules": [{ "spawnChance": "1" }] }""")]
    [InlineData("""{ "rules": [{ "groupFill": { "0": NaN } }] }""")]
    [InlineData("""{ "rules": [{ "groupFill": { "0": "NaN" } }] }""")]
    [InlineData("""{ "rules": [{ "groupFill": [1, 2] }] }""")]
    public void A_file_with_an_unknown_or_missing_member_is_refused_when_loaded(string text)
    {
        Write(text);

        Assert.Contains("loot-rules.json is not valid content", Assert.Throws<AresContentException>(() => AresContent.Load(_directory)).Message, StringComparison.Ordinal);
    }

    [RealDataTheory]
    [InlineData("""{ "rules": [null] }""")]
    [InlineData("""{ "rules": [{ "tags": [null], "spawnChance": 1 }] }""")]
    [InlineData("""{ "rules": [{ "containers": [null], "spawnChance": 1 }] }""")]
    [InlineData("""{ "rules": [{ "loot": { "items": [null] } }] }""")]
    public void A_null_rule_tag_container_or_item_is_refused_as_invalid_content(string text)
    {
        // Null list elements pass the JSON reader (nullable annotations do not reach list elements), so the rules must refuse them.
        AresContent content = Load(text);

        AresContentException error = Assert.IsType<AresContentException>(Record.Exception(() => content.LootRules(Data, Definitions)));
        Assert.Contains(AresContent.LootRulesFile, error.Message, StringComparison.Ordinal);
    }

    private static string Json(string value) => JsonSerializer.Serialize(value);

    private static (int Count, double Weight)[] Weights(LootRule rule) =>
        [.. Assert.IsType<LootFill>(rule.GroupFill).Weights.OrderBy(entry => entry.Key).Select(entry => (entry.Key, entry.Value))];

    private static string Fill(string text) => text
        .Replace("{max+1}", (Stackable.MaxStack + 1).ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal)
        .Replace("{max}", Stackable.MaxStack.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal)
        .Replace("{stackable}", Stackable.TechnicalName, StringComparison.Ordinal)
        .Replace("{single}", Single.TechnicalName, StringComparison.Ordinal)
        .Replace("{augment}", Data.Names.CombatMk1.TechnicalName, StringComparison.Ordinal)
        .Replace("{outside}", Outside.TechnicalName, StringComparison.Ordinal)
        .Replace("{socket}", LootContainerIds.Of(Dam.LootSockets[0]), StringComparison.Ordinal)
        .Replace("{loose}", LootContainerIds.Of(Dam.LooseItemSockets[0]), StringComparison.Ordinal)
        .Replace("{tag}", ParentTag, StringComparison.Ordinal);

    private AresContent Load(string text)
    {
        Write(text);
        return AresContent.Load(_directory);
    }

    // Writes the rules file into a copy of the shipped content.
    private void Write(string text)
    {
        ShippedContent.CopyTo(_directory);
        File.WriteAllText(Path.Combine(_directory, AresContent.LootRulesFile), text);
    }
}
