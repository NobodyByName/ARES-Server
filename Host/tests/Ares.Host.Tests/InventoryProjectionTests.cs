using Ares.Common.Game.Inventory;
using Ares.Host.Services.Inventory;
using Ares.Structs.Backend.Progression;

namespace Ares.Host.Tests;

public sealed class InventoryProjectionTests
{
    [Fact]
    public void Client_projection_preserves_owned_item_state_and_selects_carried_items_for_raid()
    {
        OwnedItem stash = new("stash-duck", ItemTechnicalNames.RubberDuck, 3, 0.5, 1, "stash-version", 100);
        OwnedItem carried = new("carried-duck", ItemTechnicalNames.RubberDuck, 2, 0.25, 1, "carried-version", 200) { Slot = 0 };
        PlayerInventory inventory = PlayerInventory.Empty with
        {
            Stash = new PlayerStash(StorageItems.StashSlotCount, [stash]),
            Loadout = PlayerInventory.Empty.Loadout with { Backpack = new PlayerItemContainer([carried]) },
        };
        InventorySnapshot snapshot = InventoryProjection.Create(TestDatabase.UserId, inventory).Snapshot;
        InventoryItem projected = Assert.Single(snapshot.Items, item => item.InstanceId == carried.InstanceId);
        Assert.Equal(StorageItems.RubberDuckAssetId, projected.GameAssetId);
        Assert.Equal(carried.Quantity, projected.Amount);
        Assert.Equal(carried.Durability, projected.Durability);
        Assert.Equal(carried.MaxDurability, projected.MaxDurability);
        Assert.Equal(carried.ETag, projected.ETag);
        Assert.Equal(carried.UpdatedAt, projected.UpdatedAt);
        PlayerLoadout loadout = InventorySnapshots.Loadout(snapshot);
        Assert.Contains(loadout.Items, item => item.InstanceId == carried.InstanceId);
        Assert.DoesNotContain(loadout.Items, item => item.InstanceId == stash.InstanceId);
    }

    [Fact]
    public void Increasing_stash_capacity_preserves_existing_slot_identities()
    {
        PlayerInventory inventory = PlayerInventory.Empty with { Stash = new PlayerStash(StorageItems.StashSlotCount, []) };
        InventorySnapshot before = InventoryProjection.Create(TestDatabase.UserId, inventory).Snapshot;
        InventorySnapshot expanded = InventoryProjection.Create(TestDatabase.UserId, inventory with { Stash = inventory.Stash with { SlotCount = 128 } }).Snapshot;
        InventoryItem originalStash = Assert.Single(before.Items, item => item.GameAssetId == StorageItems.StashContainerAssetId);
        InventoryItem expandedStash = Assert.Single(expanded.Items, item => item.GameAssetId == StorageItems.StashContainerAssetId);
        Assert.Equal(128, expandedStash.Slots.Count);
        Assert.Equal(originalStash.Slots, expandedStash.Slots.Take(originalStash.Slots.Count));
        Assert.True(InventorySnapshots.Same(InventorySnapshots.Loadout(before), InventorySnapshots.Loadout(expanded)));
    }
}
