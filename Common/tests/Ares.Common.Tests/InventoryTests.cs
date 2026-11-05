using Ares.Common.Game.Inventory;
using Ares.Structs.Backend.Progression;

namespace Ares.Common.Tests;

public sealed class InventoryTests
{
    private static InventoryItem Item(string id, params string[] slots) => new(id, 42, 1, slots, 0, 0, "", "test", 0);

    [Fact]
    public void Menu_frames_can_exceed_the_raid_item_bound_without_expanding_the_handoff_limit()
    {
        InventoryItem[] items = [.. Enumerable.Range(0, InventorySnapshots.MaximumItems + 1).Select(index => Item(index.ToString(System.Globalization.CultureInfo.InvariantCulture)))];
        InventorySnapshot snapshot = InventorySnapshots.Copy(new InventorySnapshot(1, 0, items, ["0"]));
        Assert.Single(InventorySnapshots.Loadout(snapshot).Items);
        Assert.Throws<InvalidDataException>(() => InventorySnapshots.Copy(new PlayerLoadout(1, 0, items)));
        InventoryItem[] largeTree = [items[0] with { Slots = [.. items.Skip(1).Select(item => item.InstanceId)] }, .. items.Skip(1)];
        Assert.Throws<InvalidDataException>(() => InventorySnapshots.Loadout(snapshot with { Items = largeTree }));
        InventoryItem[] oversized = [.. Enumerable.Range(0, InventorySnapshots.MaximumSnapshotItems + 1).Select(index => Item(index.ToString(System.Globalization.CultureInfo.InvariantCulture)))];
        Assert.Throws<InvalidDataException>(() => InventorySnapshots.Copy(new InventorySnapshot(1, 0, oversized, [])));
    }

    [Fact]
    public void Loadout_selects_complete_trees_and_does_not_share_mutable_lists()
    {
        string[] slots = ["carried"];
        InventoryItem[] items = [Item("loadout", slots), Item("carried"), Item("stash")];
        InventorySnapshot snapshot = InventorySnapshots.Copy(new InventorySnapshot(1, 7, items, ["loadout"]));
        PlayerLoadout loadout = InventorySnapshots.Loadout(snapshot);
        slots[0] = "stash";
        items[1] = Item("replacement");

        Assert.Equal(["loadout", "carried"], loadout.Items.Select(item => item.InstanceId));
        Assert.Equal(["carried"], loadout.Items[0].Slots);
        Assert.Equal(7, loadout.Revision);
        Assert.Equal("carried", snapshot.Items[1].InstanceId);
        Assert.Throws<NotSupportedException>(() => ((IList<InventoryItem>)snapshot.Items)[0] = Item("changed"));
    }

    [Fact]
    public void Invalid_item_graphs_are_rejected()
    {
        InventoryItem[][] graphs =
        [
            [Item("duplicate"), Item("duplicate")],
            [Item("root", "missing")],
            [Item("a", "child"), Item("b", "child"), Item("child")],
            [Item("a", "b"), Item("b", "a")],
            [Item("self", "self")],
            [Item("bad") with { Amount = 0 }],
            [Item("bad") with { Durability = double.NaN }],
            [Item("bad") with { Durability = 2, MaxDurability = 1 }],
            [Item("bad") with { Slots = [null!] }],
            [null!],
        ];
        Assert.All(graphs, items => Assert.Throws<InvalidDataException>(() =>
            InventorySnapshots.Copy(new InventorySnapshot(1, 0, items, []))));
        Assert.Throws<InvalidDataException>(() => InventorySnapshots.Copy(new InventorySnapshot(1, 0, [Item("a", "b"), Item("b")], ["b"])));
        Assert.Throws<InvalidDataException>(() => InventorySnapshots.Copy(new InventorySnapshot(1, 0, [Item("a")], ["a", "a"])));
    }

    [Fact]
    public void Loadout_retries_compare_contents_and_player_identity()
    {
        PlayerLoadout original = new(42, 3, [Item("a", "b"), Item("b")]);
        Assert.True(InventorySnapshots.Same(original, InventorySnapshots.Copy(original)));
        Assert.True(InventorySnapshots.Same(original, original with { Items = [.. original.Items.Reverse()] }));
        Assert.Equal(InventorySnapshots.Receipt(original), InventorySnapshots.Receipt(original with { Items = [.. original.Items.Reverse()] }));
        Assert.False(InventorySnapshots.Same(original, original with { Revision = 4 }));
        Assert.False(InventorySnapshots.Same(original, original with { Items = [original.Items[0], Item("b") with { Amount = 2 }] }));
        Assert.NotEqual(InventorySnapshots.Receipt(original), InventorySnapshots.Receipt(original with { Items = [original.Items[0], Item("b") with { Amount = 2 }] }));
        Assert.Throws<InvalidDataException>(() => InventorySnapshots.ForPlayers([42], [original with { UserId = 43 }]));
        Assert.Throws<InvalidDataException>(() => InventorySnapshots.ForPlayers([42], []));
        Assert.Empty(Assert.Single(InventorySnapshots.ForPlayers([42], null)).Items);
    }
}
