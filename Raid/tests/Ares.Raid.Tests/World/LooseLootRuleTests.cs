using Ares.Common.Game.Loot;
using Ares.Raid.World.Loot;
using Ares.Structs.Game.Inventory;
using Ares.Structs.Game.Loot;
using Ares.Structs.Game.World;

namespace Ares.Raid.Tests.World;

/// <summary>The operator's loot rules on the Dam's loose item sockets, in seed-fixed raids.</summary>
public sealed class LooseLootRuleTests
{
    private const int Seed = 3;

    [Fact]
    public void A_tag_rule_with_spawn_chance_1_fills_every_socket_of_the_tag_and_0_none()
    {
        (string tag, LooseItemSocket[] sockets) = CommonTag();
        LooseLoot before = Roll(LootRules.None);
        Assert.InRange(Count(before, tag), 1, sockets.Length - 1);

        LooseLoot all = Roll(Rules(new LootRule("all", [tag], NoIds, 1, null)));
        LooseLoot none = Roll(Rules(new LootRule("none", [tag], NoIds, 0, null)));

        Assert.Equal(sockets.Length, Count(all, tag));
        Assert.Equal(sockets.Length, all.Counts.DecidedByRule);
        Assert.Equal(0, Count(none, tag));
        Assert.Equal(sockets.Length, none.Counts.DecidedByRule);
        Assert.Equal(before.Placements.Count - Count(before, tag), none.Placements.Count);
    }

    [Fact]
    public void A_rule_listing_a_socket_id_spawns_that_socket_alone()
    {
        LooseLoot before = Roll(LootRules.None);
        HashSet<LooseItemSocket> spawned = [.. before.Placements.Select(placement => placement.Socket)];
        LooseItemSocket[] empty = [.. Servable().Where(socket => !spawned.Contains(socket))];
        LooseItemSocket chosen = empty[empty.Length / 2];
        string id = LootContainerIds.Of(chosen);

        LooseLoot after = Roll(Rules(new LootRule("one", [], new HashSet<string>([id], StringComparer.Ordinal), 1, null)));

        Assert.Single(after.Placements, placement => placement.Socket == chosen);
        Assert.Equal(before.Counts.Spawned + 1, after.Counts.Spawned);
        Assert.Equal(1, after.Counts.DecidedByRule);
    }

    [Fact]
    public void A_loot_rule_gives_each_covered_socket_one_stack_from_its_list()
    {
        (string tag, LooseItemSocket[] sockets) = CommonTag();
        ItemDefinition[] pickups = [.. Pickups().Where(item => item.MaxStack >= 5).Take(2)];
        LootList list = new(new LootRange(5, 5), [new LootListItem(pickups[0], 1, new LootRange(2, 4)), new LootListItem(pickups[1], 3, new LootRange(5, 5))]);

        LooseLoot after = Roll(Rules(new LootRule("listed", [tag], NoIds, 1, list)));

        LooseItemPlacement[] covered = [.. after.Placements.Where(placement => LootTags.IsTagOrBelow(placement.Socket.Tag, tag))];
        Assert.Equal(sockets.Length, covered.Length);
        foreach (LootStack stack in covered.Select(placement => placement.Stack))
        {
            Assert.Contains(stack.Item, pickups);
            Assert.InRange(stack.Amount, stack.Item == pickups[0] ? 2 : 5, stack.Item == pickups[0] ? 4 : 5);
            Assert.Equal(1, stack.Durability);
        }

        Assert.All(pickups, item => Assert.Contains(covered, placement => placement.Stack.Item == item));
        Assert.Equal(covered.Length, covered.Select(placement => placement.Socket).Distinct().Count());
    }

    [Fact]
    public void A_loot_rule_with_no_item_that_can_lie_in_the_world_keeps_the_default_roll()
    {
        HashSet<string> pickups = [.. Pickups().Select(item => item.TechnicalName)];
        ItemDefinition held = TestGameData.Items.Definitions.ContainerItems.First(item => !pickups.Contains(item.TechnicalName));
        LootList list = new(null, [new LootListItem(held, 1, new LootRange(1, 1))]);

        LooseLoot after = Roll(Rules(new LootRule("held", [LooseItemTags.Family], NoIds, null, list)));

        Assert.Equal(Signature(Roll(LootRules.None)), Signature(after));
        Assert.Equal(0, after.Counts.DecidedByRule);
    }

    [Fact]
    public void A_rule_without_tags_or_ids_covers_every_loose_item_socket()
    {
        int servable = Servable().Length;

        LooseLoot all = Roll(Rules(new LootRule("every", [], NoIds, 1, null)));
        LooseLoot none = Roll(Rules(new LootRule("every", [], NoIds, 0, null)));

        Assert.Equal(servable, all.Counts.Spawned);
        Assert.Equal((0, servable), (none.Counts.Spawned, none.Counts.DecidedByRule));
    }

    [Fact]
    public void Group_fills_and_reveal_delays_leave_loose_item_sockets_alone()
    {
        LootRules rules = Rules(new LootRule("fill", [], NoIds, null, null, new LootFill(new Dictionary<int, double> { [0] = 1 }), 2));

        LooseLoot after = Roll(rules);

        Assert.Equal(Signature(Roll(LootRules.None)), Signature(after));
        Assert.Equal(0, after.Counts.DecidedByRule);
    }

    private static IReadOnlySet<string> NoIds { get; } = new HashSet<string>(StringComparer.Ordinal);

    private static LootRules Rules(params LootRule[] rules) => new(rules);

    private static LooseLoot Roll(LootRules rules) => LooseLoot.Roll(TestGameData.Raid.LooseRolls, TestGameData.Dam.LooseItemSockets,
        LootSpawnRules.Of(TestGameData.Dam, TestGameData.Raid.LootSpawn), rules, [], Seed, null);

    // The items a loose item socket may hold: plain items players can carry.
    private static IEnumerable<ItemDefinition> Pickups() =>
        TestGameData.Items.Definitions.PlainItems.Where(item => TestGameData.Items.Carried.ContainsKey(checked((ushort)item.UsableIndex)));

    // The sockets of a raid without data layers that hold an item by default.
    private static LooseItemSocket[] Servable() =>
        [.. TestGameData.Dam.LooseItemSockets.Where(socket => socket.DataLayer is null && TestGameData.Raid.LooseRolls.CanHold(socket.Tag))];

    // The tag with the most servable sockets, with the servable sockets at or below it.
    private static (string Tag, LooseItemSocket[] Sockets) CommonTag()
    {
        LooseItemSocket[] servable = Servable();
        string tag = servable
            .GroupBy(socket => socket.Tag, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(entry => entry.Count())
            .ThenBy(entry => entry.Key, StringComparer.Ordinal)
            .First().Key;
        return (tag, [.. servable.Where(socket => LootTags.IsTagOrBelow(socket.Tag, tag))]);
    }

    private static int Count(LooseLoot loot, string tag) => loot.Placements.Count(placement => LootTags.IsTagOrBelow(placement.Socket.Tag, tag));

    private static IEnumerable<string> Signature(LooseLoot loot) =>
        loot.Placements.Select(placement => $"{LootContainerIds.Of(placement.Socket)} {placement.Stack.Item.TechnicalName} x{placement.Stack.Amount}");
}
