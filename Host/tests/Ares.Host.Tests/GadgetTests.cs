using Ares.Common.Game.Inventory;
using Ares.Host.Services.Inventory;
using Ares.Structs.Backend.Progression;

namespace Ares.Host.Tests;

public sealed class GadgetTests : IDisposable
{
    private readonly TestDatabase _database = new();

    [Fact]
    public void The_operator_adds_a_single_snap_hook_at_full_durability_to_the_stash()
    {
        InventoryStore store = Create();
        OwnedItem hook = Assert.Single(store.Inventory.Stash.Items, item => item.Type == ItemTechnicalNames.SnapHook);
        Assert.Equal((1L, StorageItems.InitialDurability, StorageItems.InitialMaxDurability), (hook.Quantity, hook.Durability, hook.MaxDurability));
        Assert.Equal(GadgetItems.SnapHookAssetId, Assert.Single(store.Snapshot.Items, item => item.InstanceId == hook.InstanceId).GameAssetId);
        Assert.Contains(store.GameData().ItemDescriptions.ItemDescriptions, item => item.GameAssetId == GadgetItems.SnapHookAssetId);
        string saved = _database.InventoryJson;
        Assert.Throws<InvalidDataException>(() => store.AddItem(ItemTechnicalNames.SnapHook, DateTimeOffset.UnixEpoch, 2));
        Assert.Equal(saved, _database.InventoryJson);
    }

    [Theory]
    [InlineData(null, 4)]
    [InlineData(ItemTechnicalNames.CombatMk1, 4)]
    [InlineData(ItemTechnicalNames.LootingMk1, 4)]
    [InlineData(ItemTechnicalNames.TacticalMk1, 5)]
    public void A_snap_hook_fits_every_quick_use_slot_of_each_frame_and_is_reserved_with_its_durability(string? augmentType, int slots)
    {
        InventoryStore store = Create();
        if (augmentType is not null)
        {
            store.AddItem(augmentType, DateTimeOffset.UnixEpoch);
            OwnedItem augment = store.Inventory.Stash.Items.Single(item => item.Type == augmentType);
            Assert.Equal(200, store.Commit(Move(augment.InstanceId, store.Snapshot.Items.Single(item => item.GameAssetId == AugmentItems.AugmentContentSlotAssetId).InstanceId, "augment"), DateTimeOffset.UnixEpoch).StatusCode);
        }

        OwnedItem hook = store.Inventory.Stash.Items.Single(item => item.Type == ItemTechnicalNames.SnapHook);
        List<string> quickUse = Slots(store, LoadoutItems.BeltSlotAssetId);
        Assert.Equal(slots, quickUse.Count);
        for (int slot = 0; slot < quickUse.Count; slot++)
        {
            Assert.Equal(200, store.Commit(Move(hook.InstanceId, quickUse[slot], $"quick-use-{slot}"), DateTimeOffset.UnixEpoch).StatusCode);
            OwnedItem placed = Assert.Single(store.Inventory.Loadout.QuickUse.Items);
            Assert.Equal<(string, int?)>((hook.InstanceId, slot), (placed.InstanceId, placed.Slot));
        }

        InventoryStore restarted = new(_database.Store, TestDatabase.UserId);
        string ticket = Guid.NewGuid().ToString("D");
        InventoryItem carried = Assert.Single(restarted.Reserve(ticket).Items, item => item.GameAssetId == GadgetItems.SnapHookAssetId);
        Assert.Equal((hook.InstanceId, hook.Durability, hook.MaxDurability), (carried.InstanceId, carried.Durability, carried.MaxDurability));
        restarted.Release(ticket);
        Assert.Equal(200, restarted.Commit(Move(hook.InstanceId, Slots(restarted, LoadoutItems.BackpackSlotAssetId)[0], "backpack"), DateTimeOffset.UnixEpoch).StatusCode);
        Assert.Empty(restarted.Inventory.Loadout.QuickUse.Items);
        Assert.Equal(hook.InstanceId, Assert.Single(restarted.Inventory.Loadout.Backpack.Items).InstanceId);
    }

    [Fact]
    public void A_snap_hook_cannot_be_equipped_as_a_weapon_or_shield()
    {
        InventoryStore store = Create();
        OwnedItem hook = store.Inventory.Stash.Items.Single(item => item.Type == ItemTechnicalNames.SnapHook);
        string saved = _database.InventoryJson;
        foreach (long branch in (long[])[LoadoutItems.Firearm1SlotAssetId, LoadoutItems.Firearm2SlotAssetId, LoadoutItems.ArmorSlotAssetId])
        {
            Assert.Equal(400, store.Commit(Move(hook.InstanceId, Slots(store, branch)[0], $"equip-{branch}"), DateTimeOffset.UnixEpoch).StatusCode);
        }

        Assert.Equal(saved, _database.InventoryJson);
    }

    [Fact]
    public void An_extracted_snap_hook_settles_into_the_quick_use_slot_it_came_back_in()
    {
        InventoryStore store = Create();
        OwnedItem hook = store.Inventory.Stash.Items.Single(item => item.Type == ItemTechnicalNames.SnapHook);
        Assert.Equal(200, store.Commit(Move(hook.InstanceId, Slots(store, LoadoutItems.BeltSlotAssetId)[0], "quick-use"), DateTimeOffset.UnixEpoch).StatusCode);
        string ticket = Guid.NewGuid().ToString("D");
        PlayerLoadout reserved = store.Reserve(ticket);
        InventoryItem carried = reserved.Items.Single(item => item.GameAssetId == GadgetItems.SnapHookAssetId);
        RaidResult result = new(ticket, TestDatabase.UserId, reserved.Revision, RaidOutcomes.Extracted, [new CarriedItem(CarriedPlaces.QuickUse, 2, carried with { Slots = [] })]);
        Assert.Equal(RaidSettlement.Saved, store.Settle(result, DateTimeOffset.UnixEpoch.AddDays(1)));
        OwnedItem home = Assert.Single(new InventoryStore(_database.Store, TestDatabase.UserId).Inventory.Loadout.QuickUse.Items);
        Assert.Equal<(string, string, int?, double)>((hook.InstanceId, ItemTechnicalNames.SnapHook, 2, hook.Durability), (home.InstanceId, home.Type, home.Slot, home.Durability));
    }

    private InventoryStore Create()
    {
        InventoryStore store = new(_database.Store, TestDatabase.UserId);
        store.InitializeLayout(DateTimeOffset.UnixEpoch);
        store.AddItem(ItemTechnicalNames.SnapHook, DateTimeOffset.UnixEpoch);
        return store;
    }

    private static List<string> Slots(InventoryStore store, long branchAsset)
    {
        InventorySnapshot snapshot = store.Snapshot;
        string frameId = snapshot.Items.Single(item => item.GameAssetId == LoadoutItems.FrameSlotAssetId).Slots[0];
        InventoryItem frame = snapshot.Items.Single(item => item.InstanceId == frameId);
        InventoryItem branch = snapshot.Items.Single(item => frame.Slots.Contains(item.InstanceId) && LoadoutDefinitions.StandardBranch(item.GameAssetId) == branchAsset);
        return [.. snapshot.Items.Single(item => item.InstanceId == branch.Slots[0]).Slots];
    }

    private static InventoryOperationsRequest Move(string item, string target, string id) => new([new SetItemParents(item, [target])], id);

    public void Dispose() => _database.Dispose();
}
