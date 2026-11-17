using Ares.Common.Game.Inventory;
using Ares.Host.Services.Inventory;
using Ares.Structs.Backend.Progression;

namespace Ares.Host.Tests;

public sealed class ParentMoveLayoutTests
{
    private static readonly OwnedItem s_duck = Item("duck", ItemTechnicalNames.RubberDuck);
    private static readonly OwnedItem s_equipped = Item("equipped", ItemTechnicalNames.CombatMk1);
    private static readonly OwnedItem s_spare = Item("spare", ItemTechnicalNames.CombatMk1);

    private static readonly PlayerInventory s_inventory = PlayerInventory.Empty with
    {
        Stash = new PlayerStash(TestCatalog.StashContainer, [s_duck, s_spare]),
        Loadout = PlayerInventory.Empty.Loadout with { Augment = s_equipped },
    };

    [Fact]
    public void A_move_of_an_item_the_player_does_not_own_is_refused()
    {
        Assert.Null(Place(Applied(), new SetItemParents("missing", [StashSlot(0)])));
    }

    [Fact]
    public void A_split_stack_left_without_a_parent_move_is_refused()
    {
        Assert.Null(Place(Applied() with { Created = new HashSet<string>([s_duck.InstanceId], StringComparer.Ordinal) }));
    }

    [Fact]
    public void A_move_of_an_item_the_batch_consumed_is_skipped()
    {
        AppliedOperations applied = Applied() with { Consumed = new HashSet<string>(["missing"], StringComparer.Ordinal) };
        PlayerInventory? placed = ParentMoveLayout.Apply(TestCatalog.Real, TestDatabase.UserId, applied, [new SetItemParents("missing", [StashSlot(0)])], AugmentSlot(), "moved", 1,
            out bool moved);
        Assert.NotNull(placed);
        Assert.False(moved);
        Assert.Equal(s_inventory.Stash.Items, placed.Stash.Items);
    }

    [Fact]
    public void A_second_augment_cannot_join_an_occupied_augment_slot()
    {
        Assert.Null(Place(Applied(), new SetItemParents(s_spare.InstanceId, [AugmentSlot()])));
    }

    [Fact]
    public void Augments_swap_in_one_batch()
    {
        PlayerInventory? placed = Place(Applied(),
            new SetItemParents(s_equipped.InstanceId, [StashSlot(5)]),
            new SetItemParents(s_spare.InstanceId, [AugmentSlot()]));
        Assert.NotNull(placed);
        Assert.Equal(s_spare.InstanceId, placed.Loadout.Augment?.InstanceId);
        Assert.Equal("moved", placed.Loadout.Augment?.ETag);
        Assert.Contains(placed.Stash.Items, item => item.InstanceId == s_equipped.InstanceId && item.ETag == "moved");
    }

    private static PlayerInventory? Place(AppliedOperations applied, params SetItemParents[] moves) =>
        ParentMoveLayout.Apply(TestCatalog.Real, TestDatabase.UserId, applied, moves, AugmentSlot(), "moved", 1, out _);

    private static AppliedOperations Applied() =>
        new(s_inventory, false, new HashSet<string>(StringComparer.Ordinal), new HashSet<string>(StringComparer.Ordinal));

    private static string AugmentSlot() => Snapshot().Items.Single(item => item.GameAssetId == TestCatalog.AugmentContentSlot).InstanceId;

    private static string StashSlot(int index) => Snapshot().Items.Single(item => item.GameAssetId == TestCatalog.StashContainer).Slots[index];

    private static InventorySnapshot Snapshot() => InventoryProjection.Create(TestCatalog.Real, TestDatabase.UserId, s_inventory).Snapshot;

    private static OwnedItem Item(string instanceId, string type) =>
        new(instanceId, type, 1, OwnedItem.FullDurability, OwnedItem.FullDurability, "initial", 0);
}
