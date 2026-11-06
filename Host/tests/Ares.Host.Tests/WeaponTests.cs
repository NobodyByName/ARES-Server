using Ares.Common.Game.Inventory;
using Ares.Host.Services;
using Ares.Host.Services.Inventory;
using Ares.Structs.Backend.Progression;

namespace Ares.Host.Tests;

public sealed class WeaponTests : IDisposable
{
    private readonly TestDatabase _database = new();

    [Theory]
    [InlineData(null)]
    [InlineData(ItemTechnicalNames.CombatMk1)]
    [InlineData(ItemTechnicalNames.LootingMk1)]
    [InlineData(ItemTechnicalNames.TacticalMk1)]
    public void Ferro_and_ammunition_keep_identity_condition_and_amount_across_menu_moves_and_restart(string? augmentType)
    {
        InventoryStore store = Create();
        if (augmentType is not null)
        {
            store.AddItem(augmentType, DateTimeOffset.UnixEpoch);
            OwnedItem augment = store.Inventory.Stash.Items.Single(item => item.Type == augmentType);
            Assert.Equal(200, Move(store, augment, store.Snapshot.Items.Single(item => item.GameAssetId == AugmentItems.AugmentContentSlotAssetId).InstanceId, "augment").StatusCode);
        }

        OwnedItem gun = store.Inventory.Stash.Items.Single(item => item.Type == ItemTechnicalNames.FerroI);
        OwnedItem ammo = store.Inventory.Stash.Items.Single(item => item.Type == ItemTechnicalNames.HeavyAmmo);
        // A used gun keeps its condition while changing equipment slots.
        gun = gun with { Durability = .65 };
        store.Import(store.Inventory with
        {
            Stash = store.Inventory.Stash with
            {
                Items = [.. store.Inventory.Stash.Items.Select(item => item.InstanceId == gun.InstanceId ? gun : item)],
            },
        }, store.Inventory.Revision);
        Assert.Equal(200, Move(store, gun, Slot(store, LoadoutItems.Firearm1SlotAssetId), "primary").StatusCode);
        Assert.Equal(200, Move(store, ammo, Slot(store, LoadoutItems.BackpackSlotAssetId), "ammo").StatusCode);
        Assert.Equal(200, Move(store, gun, Slot(store, LoadoutItems.Firearm2SlotAssetId), "secondary").StatusCode);
        Assert.Equal(200, Move(store, ammo, Slot(store, LoadoutItems.SafePocketSlotAssetId), "safe").StatusCode);
        store = new InventoryStore(_database.Store);
        Assert.Null(store.Inventory.Loadout.MainWeapon);
        Assert.Equal(gun.InstanceId, store.Inventory.Loadout.SecondaryWeapon!.InstanceId);
        Assert.Equal(gun.Durability, store.Inventory.Loadout.SecondaryWeapon.Durability);
        Assert.Equal(ammo.InstanceId, Assert.Single(store.Inventory.Loadout.SafePockets.Items).InstanceId);
        Assert.Equal(60, store.Inventory.Loadout.SafePockets.Items[0].Quantity);
        InventoryItem projected = store.Snapshot.Items.Single(item => item.InstanceId == gun.InstanceId);
        Assert.Equal(WeaponItems.FerroIAssetId, projected.GameAssetId);
        Assert.Equal(gun.MaxDurability, projected.MaxDurability);
        Assert.Equal(60, store.Snapshot.Items.Single(item => item.InstanceId == ammo.InstanceId).Amount);
        Assert.Equal(200, Move(store, gun, Slot(store, LoadoutItems.BackpackSlotAssetId), "pack-gun").StatusCode);
        Assert.Equal(200, Move(store, gun, StashSlot(store), "stash-gun").StatusCode);
        Assert.Equal(200, Move(store, ammo, StashSlot(store), "stash-ammo").StatusCode);
        Assert.Equal(gun.Durability, store.Inventory.Stash.Items.Single(item => item.InstanceId == gun.InstanceId).Durability);
        Assert.Equal(60, store.Inventory.Stash.Items.Single(item => item.InstanceId == ammo.InstanceId).Quantity);
    }

    [Fact]
    public void Weapon_swaps_are_atomic_and_retryable_and_incompatible_moves_do_not_write()
    {
        InventoryStore store = Create();
        OwnedItem first = store.Inventory.Stash.Items.Single(item => item.Type == ItemTechnicalNames.FerroI);
        store.AddItem(ItemTechnicalNames.FerroI, DateTimeOffset.UnixEpoch);
        OwnedItem second = store.Inventory.Stash.Items.Single(item => item.Type == ItemTechnicalNames.FerroI && item.InstanceId != first.InstanceId);
        OwnedItem ammo = store.Inventory.Stash.Items.Single(item => item.Type == ItemTechnicalNames.HeavyAmmo);
        string primary = Slot(store, LoadoutItems.Firearm1SlotAssetId);
        Assert.Equal(200, Move(store, first, primary, "equip").StatusCode);
        string saved = _database.InventoryJson;
        Assert.Equal(400, Move(store, second, primary, "occupied").StatusCode);
        Assert.Equal(400, Move(store, ammo, primary, "ammo-weapon").StatusCode);
        Assert.Equal(400, Move(store, first, Slot(store, LoadoutItems.ArmorSlotAssetId), "gun-armor").StatusCode);
        Assert.Equal(400, Move(store, ammo, Slot(store, LoadoutItems.BeltSlotAssetId), "ammo-belt").StatusCode);
        Assert.Equal(saved, _database.InventoryJson);
        InventorySnapshot before = store.Snapshot;
        InventoryOperationsRequest swap = new([new SetItemParents(second.InstanceId, [primary]), new SetItemParents(first.InstanceId, [StashSlot(store)])], "swap");
        ServiceReply reply = store.Commit(swap, DateTimeOffset.UnixEpoch);
        Assert.Equal(200, reply.StatusCode);
        AssertChanges(before, reply, store.Snapshot);
        Assert.Equal(second.InstanceId, store.Inventory.Loadout.MainWeapon!.InstanceId);
        long revision = store.Inventory.Revision;
        Assert.Equal(200, new InventoryStore(_database.Store).Commit(swap, DateTimeOffset.UnixEpoch).StatusCode);
        Assert.Equal(revision, store.Inventory.Revision);
    }

    [Theory]
    [InlineData(ItemTechnicalNames.HeavyAmmo, 0)]
    [InlineData(ItemTechnicalNames.HeavyAmmo, -1)]
    [InlineData(ItemTechnicalNames.HeavyAmmo, 61)]
    [InlineData(ItemTechnicalNames.FerroI, 2)]
    public void Invalid_grants_and_imports_preserve_the_database(string type, long quantity)
    {
        InventoryStore store = Create();
        string saved = _database.InventoryJson;
        Assert.Throws<InvalidDataException>(() => store.AddItem(type, DateTimeOffset.UnixEpoch, quantity));
        PlayerInventory invalid = store.Inventory with
        {
            Stash = store.Inventory.Stash with
            {
                Items = [.. store.Inventory.Stash.Items.Select(item => item.Type == type ? item with { Quantity = quantity } : item)],
            },
        };
        Assert.Throws<InvalidDataException>(() => store.Import(invalid, store.Inventory.Revision));
        Assert.Equal(saved, _database.InventoryJson);
    }

    [Fact]
    public void Grants_preserve_existing_stacks_and_obey_capacity_and_reservation_locks()
    {
        InventoryStore store = Create();
        store.AddItem(ItemTechnicalNames.HeavyAmmo, DateTimeOffset.UnixEpoch);
        Assert.Equal([1L, 60L], store.Inventory.Stash.Items.Where(item => item.Type == ItemTechnicalNames.HeavyAmmo).Select(item => item.Quantity).Order());
        string ticket = Guid.NewGuid().ToString("D");
        store.Reserve(ticket);
        string saved = _database.InventoryJson;
        Assert.Throws<InvalidOperationException>(() => store.AddItem(ItemTechnicalNames.HeavyAmmo, DateTimeOffset.UnixEpoch, 60));
        Assert.Equal(saved, _database.InventoryJson);
        store.Release(ticket);
        store.Import(store.Inventory with { Stash = store.Inventory.Stash with { SlotCount = store.Inventory.Stash.Items.Count } }, store.Inventory.Revision);
        saved = _database.InventoryJson;
        Assert.Throws<InvalidDataException>(() => store.AddItem(ItemTechnicalNames.HeavyAmmo, DateTimeOffset.UnixEpoch, 60));
        Assert.Equal(saved, _database.InventoryJson);
    }

    private InventoryStore Create()
    {
        InventoryStore store = new(_database.Store);
        store.InitializeLayout(DateTimeOffset.UnixEpoch);
        store.AddItem(ItemTechnicalNames.FerroI, DateTimeOffset.UnixEpoch);
        store.AddItem(ItemTechnicalNames.HeavyAmmo, DateTimeOffset.UnixEpoch, 60);
        return store;
    }

    private static string Slot(InventoryStore store, long branchAsset)
    {
        InventorySnapshot snapshot = store.Snapshot;
        string frameId = snapshot.Items.Single(item => item.GameAssetId == LoadoutItems.FrameSlotAssetId).Slots[0];
        InventoryItem frame = snapshot.Items.Single(item => item.InstanceId == frameId);
        InventoryItem branch = snapshot.Items.Single(item => frame.Slots.Contains(item.InstanceId) && LoadoutDefinitions.StandardBranch(item.GameAssetId) == branchAsset);
        return snapshot.Items.Single(item => item.InstanceId == branch.Slots[0]).Slots[0];
    }

    private static string StashSlot(InventoryStore store) => store.Snapshot.Items.Single(item => item.GameAssetId == StorageItems.StashContainerAssetId).Slots[0];
    private static ServiceReply Move(InventoryStore store, OwnedItem item, string target, string id) => store.Commit(new InventoryOperationsRequest([new SetItemParents(item.InstanceId, [target])], id), DateTimeOffset.UnixEpoch);

    private static void AssertChanges(InventorySnapshot before, ServiceReply reply, InventorySnapshot after)
    {
        ItemChanges changes = Assert.IsType<InventoryOperationsResponse>(reply.Body).Changeset.Items;
        Dictionary<string, InventoryItem> items = before.Items.ToDictionary(item => item.InstanceId, StringComparer.Ordinal);
        foreach (string id in changes.Deleted)
        {
            Assert.True(items.Remove(id));
        }

        foreach (InventoryItem item in changes.Created)
        {
            Assert.True(items.TryAdd(item.InstanceId, item));
        }

        foreach (InventoryItem item in changes.Updated)
        {
            Assert.True(items.ContainsKey(item.InstanceId));
            items[item.InstanceId] = item;
        }

        Assert.True(InventorySnapshots.Same(new PlayerLoadout(after.UserId, after.Revision, [.. items.Values]), new PlayerLoadout(after.UserId, after.Revision, after.Items)));
    }

    public void Dispose() => _database.Dispose();
}
