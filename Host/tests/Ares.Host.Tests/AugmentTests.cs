using Ares.Common.Game.Inventory;
using Ares.Host.Services;
using Ares.Host.Services.Inventory;
using Ares.Structs.Backend.Progression;

namespace Ares.Host.Tests;

public sealed class AugmentTests : IDisposable
{
    private readonly TestDatabase _database = new();

    [Theory]
    [InlineData(ItemTechnicalNames.CombatMk1)]
    [InlineData(ItemTechnicalNames.LootingMk1)]
    [InlineData(ItemTechnicalNames.TacticalMk1)]
    public void The_initial_menu_contains_the_next_frame_and_accepts_the_clients_local_item_migration(string type)
    {
        InventoryStore store = Create(type);
        OwnedItem duck = store.Inventory.Stash.Items.Single(item => item.Type == ItemTechnicalNames.RubberDuck);
        OwnedItem augment = store.Inventory.Stash.Items.Single(item => item.Type == type);
        Assert.Equal(200, store.Commit(Move(duck.InstanceId, BackpackSlots(store.Snapshot)[0], "carry"), DateTimeOffset.UnixEpoch).StatusCode);
        InventorySnapshot before = store.Snapshot;
        long frame = AugmentItems.FrameAssetIds[ItemTechnicalNames.AssetIds[type]];
        string destination = BackpackSlots(before, frame)[0];
        Assert.NotEqual(BackpackSlots(before)[0], destination);
        string saved = _database.InventoryJson;
        Assert.Equal(400, store.Commit(Move(duck.InstanceId, destination, "inactive"), DateTimeOffset.UnixEpoch).StatusCode);
        Assert.Equal(saved, _database.InventoryJson);

        InventoryOperationsRequest migration = new(
            [new SetItemParents(duck.InstanceId, [destination]), new SetItemParents(augment.InstanceId, [AugmentSlot(store)])], "equip-and-migrate");
        ServiceReply reply = store.Commit(migration, DateTimeOffset.UnixEpoch);
        Assert.Equal(200, reply.StatusCode);
        AssertChanges(before, reply, store.Snapshot);
        ItemChanges changes = Assert.IsType<InventoryOperationsResponse>(reply.Body).Changeset.Items;
        Assert.Empty(changes.Created);
        Assert.Empty(changes.Deleted);
        Assert.Equal(destination, store.Snapshot.Items.Single(item => item.Slots.Contains(duck.InstanceId)).InstanceId);
        Assert.Empty(store.Snapshot.Items.Single(item => item.InstanceId == BackpackSlots(before)[0]).Slots);
        foreach (long availableFrame in AugmentItems.FrameAssetIds.Values.Prepend(LoadoutItems.StandardFrameAssetId))
        {
            Assert.Equal(BackpackSlots(before, availableFrame), BackpackSlots(store.Snapshot, availableFrame));
        }

        PlayerLoadout handoff = InventorySnapshots.Loadout(store.Snapshot);
        Assert.Contains(handoff.Items, item => item.GameAssetId == frame);
        Assert.DoesNotContain(handoff.Items, item => item.GameAssetId == LoadoutItems.StandardFrameAssetId);
        Assert.DoesNotContain(handoff.Items, item => AugmentItems.FrameAssetIds.Values.Contains(item.GameAssetId) && item.GameAssetId != frame);
        Assert.Single(handoff.Items, item => item.InstanceId == duck.InstanceId);
        Assert.Equal(destination, new InventoryStore(_database.Store, TestDatabase.UserId).Snapshot.Items.Single(item => item.Slots.Contains(duck.InstanceId)).InstanceId);
    }

    [Fact]
    public void Operator_additions_preserve_existing_items_and_reject_unknown_full_or_reserved_inventory()
    {
        InventoryStore store = Create();
        OwnedItem duck = Assert.Single(store.Inventory.Stash.Items);
        store.AddItem(ItemTechnicalNames.CombatMk1, DateTimeOffset.UnixEpoch);
        Assert.Contains(duck, store.Inventory.Stash.Items);
        Assert.Contains(store.Inventory.Stash.Items, item => item is { Type: ItemTechnicalNames.CombatMk1, Quantity: 1 });
        string saved = _database.InventoryJson;
        Assert.Throws<InvalidDataException>(() => store.AddItem("unknown", DateTimeOffset.UnixEpoch));
        Assert.Equal(saved, _database.InventoryJson);
        store.Import(store.Inventory with { Stash = store.Inventory.Stash with { SlotCount = 2 } }, store.Inventory.Revision);
        saved = _database.InventoryJson;
        Assert.Throws<InvalidDataException>(() => store.AddItem(ItemTechnicalNames.TacticalMk1, DateTimeOffset.UnixEpoch));
        Assert.Equal(saved, _database.InventoryJson);
        store.Reserve(Guid.NewGuid().ToString("D"));
        saved = _database.InventoryJson;
        Assert.Throws<InvalidOperationException>(() => store.AddItem(ItemTechnicalNames.TacticalMk1, DateTimeOffset.UnixEpoch));
        Assert.Equal(saved, _database.InventoryJson);
    }

    [Theory]
    [InlineData(ItemTechnicalNames.CombatMk1, 16, 4)]
    [InlineData(ItemTechnicalNames.LootingMk1, 18, 4)]
    [InlineData(ItemTechnicalNames.TacticalMk1, 15, 5)]
    public void Equipping_an_augment_changes_the_frame_and_capacities_and_survives_a_raid_reservation(string type, int backpack, int quickUse)
    {
        InventoryStore store = Create(type);
        InventorySnapshot before = store.Snapshot;
        InventoryItem empty = Assert.Single(before.Items, item => item.GameAssetId == AugmentItems.AugmentContentSlotAssetId);
        Assert.Empty(empty.Slots);
        string[] slots = BackpackSlots(before, AugmentItems.FrameAssetIds[ItemTechnicalNames.AssetIds[type]]);
        OwnedItem augment = store.Inventory.Stash.Items.Single(item => item.Type == type);
        ServiceReply reply = store.Commit(Move(augment.InstanceId, empty.InstanceId, "equip"), DateTimeOffset.UnixEpoch.AddMinutes(1));
        Assert.Equal(200, reply.StatusCode);
        Assert.Equal(backpack, store.Inventory.Loadout.BackpackCapacity);
        Assert.Equal(quickUse, store.Inventory.Loadout.QuickUseCapacity);
        Assert.Equal(1, store.Inventory.Loadout.SafePocketCapacity);
        Assert.Equal(slots, BackpackSlots(store.Snapshot));
        Assert.Equal(augment with { ETag = "ares-host-inventory-3", UpdatedAt = 60000 }, store.Inventory.Loadout.Augment);
        AssertChanges(before, reply, store.Snapshot);

        InventoryStore restarted = new(_database.Store, TestDatabase.UserId);
        Assert.Equal(augment.InstanceId, restarted.Inventory.Loadout.Augment!.InstanceId);
        Assert.Equal(backpack, restarted.Inventory.Loadout.BackpackCapacity);
        string ticket = Guid.NewGuid().ToString("D");
        PlayerLoadout loadout = restarted.Reserve(ticket);
        Assert.Contains(loadout.Items, item => item.InstanceId == augment.InstanceId);
        long frame = AugmentItems.FrameAssetIds[ItemTechnicalNames.AssetIds[type]];
        Assert.Contains(loadout.Items, item => item.GameAssetId == frame);
        Assert.DoesNotContain(loadout.Items, item => item.GameAssetId == LoadoutItems.StandardFrameAssetId);
        Assert.Equal(409, restarted.Commit(Move(augment.InstanceId, StashSlot(restarted), "reserved"), DateTimeOffset.UnixEpoch).StatusCode);
        restarted.Release(ticket);
        InventorySnapshot equipped = restarted.Snapshot;
        reply = restarted.Commit(Move(augment.InstanceId, StashSlot(restarted), "remove"), DateTimeOffset.UnixEpoch.AddMinutes(2));
        Assert.Equal(200, reply.StatusCode);
        Assert.Null(restarted.Inventory.Loadout.Augment);
        Assert.Equal(10, restarted.Inventory.Loadout.BackpackCapacity);
        AssertChanges(equipped, reply, restarted.Snapshot);
    }

    [Fact]
    public void A_two_item_swap_and_its_retry_preserve_both_augments()
    {
        InventoryStore store = Create(ItemTechnicalNames.CombatMk1, ItemTechnicalNames.TacticalMk1);
        OwnedItem combat = store.Inventory.Stash.Items.Single(item => item.Type == ItemTechnicalNames.CombatMk1);
        OwnedItem tactical = store.Inventory.Stash.Items.Single(item => item.Type == ItemTechnicalNames.TacticalMk1);
        string slot = AugmentSlot(store);
        Assert.Equal(200, store.Commit(Move(combat.InstanceId, slot, "equip"), DateTimeOffset.UnixEpoch).StatusCode);
        InventorySnapshot before = store.Snapshot;
        InventoryOperationsRequest swap = new([new SetItemParents(tactical.InstanceId, [slot]), new SetItemParents(combat.InstanceId, [StashSlot(store)])], "swap");
        ServiceReply reply = store.Commit(swap, DateTimeOffset.UnixEpoch);
        Assert.Equal(200, reply.StatusCode);
        AssertChanges(before, reply, store.Snapshot);
        Assert.Equal(tactical.InstanceId, store.Inventory.Loadout.Augment!.InstanceId);
        Assert.Contains(store.Inventory.Stash.Items, item => item.InstanceId == combat.InstanceId);
        Assert.Equal(3, store.Inventory.ItemCount);
        long revision = store.Inventory.Revision;
        Assert.Equal(200, new InventoryStore(_database.Store, TestDatabase.UserId).Commit(swap, DateTimeOffset.UnixEpoch).StatusCode);
        Assert.Equal(revision, store.Inventory.Revision);
    }

    [Fact]
    public void Capacity_reduction_rejects_overflow_and_accepts_a_batch_that_moves_the_excess_items()
    {
        InventoryStore store = Create(ItemTechnicalNames.CombatMk1);
        OwnedItem augment = store.Inventory.Stash.Items.Single(item => item.Type == ItemTechnicalNames.CombatMk1);
        Assert.Equal(200, store.Commit(Move(augment.InstanceId, AugmentSlot(store), "equip"), DateTimeOffset.UnixEpoch).StatusCode);
        OwnedItem duck = store.Inventory.Stash.Items.Single(item => item.Type == ItemTechnicalNames.RubberDuck);
        Assert.Equal(200, store.Commit(Move(duck.InstanceId, BackpackSlots(store.Snapshot)[15], "last-slot"), DateTimeOffset.UnixEpoch).StatusCode);
        string saved = _database.InventoryJson;
        Assert.Equal(400, store.Commit(Move(augment.InstanceId, StashSlot(store), "overflow"), DateTimeOffset.UnixEpoch).StatusCode);
        Assert.Equal(saved, _database.InventoryJson);
        InventorySnapshot before = store.Snapshot;
        InventoryOperationsRequest remove = new([new SetItemParents(augment.InstanceId, [StashSlot(store)]), new SetItemParents(duck.InstanceId, [StashSlot(store)])], "remove-both");
        ServiceReply reply = store.Commit(remove, DateTimeOffset.UnixEpoch);
        Assert.Equal(200, reply.StatusCode);
        AssertChanges(before, reply, store.Snapshot);
        Assert.Null(store.Inventory.Loadout.Augment);
        Assert.Equal(2, store.Inventory.Stash.Items.Count);
        Assert.Contains(store.Inventory.Stash.Items, item => item.InstanceId == duck.InstanceId && item.Slot is null);
        ItemChanges original = Assert.IsType<InventoryOperationsResponse>(reply.Body).Changeset.Items;
        ServiceReply retried = new InventoryStore(_database.Store, TestDatabase.UserId).Commit(remove, DateTimeOffset.UnixEpoch);
        Assert.Equal(original.Deleted, Assert.IsType<InventoryOperationsResponse>(retried.Body).Changeset.Items.Deleted);
        Assert.Equal(200, store.Commit(Move(augment.InstanceId, AugmentSlot(store), "reequip"), DateTimeOffset.UnixEpoch).StatusCode);
        long revision = store.Inventory.Revision;
        retried = store.Commit(remove, DateTimeOffset.UnixEpoch);
        Assert.Equal(revision, store.Inventory.Revision);
        Assert.NotNull(store.Inventory.Loadout.Augment);
        Assert.Empty(Assert.IsType<InventoryOperationsResponse>(retried.Body).Changeset.Items.Deleted);
    }

    [Fact]
    public void A_batch_can_use_the_new_capacity_before_its_equip_operation()
    {
        InventoryStore store = Create(ItemTechnicalNames.CombatMk1);
        OwnedItem augment = store.Inventory.Stash.Items.Single(item => item.Type == ItemTechnicalNames.CombatMk1);
        OwnedItem duck = store.Inventory.Stash.Items.Single(item => item.Type == ItemTechnicalNames.RubberDuck);
        PlayerInventory equipped = store.Inventory with
        {
            Stash = store.Inventory.Stash with { Items = [duck] },
            Loadout = store.Inventory.Loadout with { Augment = augment },
        };
        string destination = BackpackSlots(InventoryProjection.Create(TestDatabase.UserId, equipped).Snapshot)[15];
        InventoryOperationsRequest batch = new([new SetItemParents(duck.InstanceId, [destination]), new SetItemParents(augment.InstanceId, [AugmentSlot(store)])], "grow");
        Assert.Equal(200, store.Commit(batch, DateTimeOffset.UnixEpoch).StatusCode);
        Assert.Equal(15, Assert.Single(store.Inventory.Loadout.Backpack.Items).Slot);
        Assert.Equal(augment.InstanceId, store.Inventory.Loadout.Augment!.InstanceId);
    }

    [Fact]
    public void Invalid_items_slots_and_unsupported_tiers_leave_inventory_unchanged()
    {
        InventoryStore store = Create(ItemTechnicalNames.CombatMk1);
        OwnedItem duck = store.Inventory.Stash.Items.Single(item => item.Type == ItemTechnicalNames.RubberDuck);
        string saved = _database.InventoryJson;
        Assert.Equal(400, store.Commit(Move(duck.InstanceId, AugmentSlot(store), "duck"), DateTimeOffset.UnixEpoch).StatusCode);
        Assert.Equal(saved, _database.InventoryJson);
        OwnedItem unsupported = duck with { Type = "DA_Item_Augment_Combat_T2" };
        Assert.Throws<InvalidDataException>(() => store.Import(store.Inventory with { Loadout = store.Inventory.Loadout with { Augment = unsupported } }, store.Inventory.Revision));
        Assert.Equal(saved, _database.InventoryJson);
    }

    private InventoryStore Create(params string[] types)
    {
        InventoryStore store = new(_database.Store, TestDatabase.UserId);
        store.InitializeLayout(DateTimeOffset.UnixEpoch);
        OwnedItem[] augments = [.. types.Select(type => new OwnedItem(Guid.NewGuid().ToString("D"), type, 1, 0.5, 1, "original", 0))];
        store.Import(store.Inventory with { Stash = store.Inventory.Stash with { Items = [.. store.Inventory.Stash.Items, .. augments] } }, store.Inventory.Revision);
        return store;
    }

    private static string AugmentSlot(InventoryStore store) => store.Snapshot.Items.Single(item => item.GameAssetId == AugmentItems.AugmentContentSlotAssetId).InstanceId;
    private static string StashSlot(InventoryStore store) => store.Snapshot.Items.Single(item => item.GameAssetId == StorageItems.StashContainerAssetId).Slots[0];
    private static InventoryOperationsRequest Move(string item, string target, string id) => new([new SetItemParents(item, [target])], id);

    private static string[] BackpackSlots(InventorySnapshot snapshot, long? frameAssetId = null)
    {
        InventoryItem root = snapshot.Items.Single(item => item.GameAssetId == LoadoutItems.FrameSlotAssetId);
        InventoryItem frame = frameAssetId is { } asset ? snapshot.Items.Single(item => item.GameAssetId == asset)
            : snapshot.Items.Single(item => item.InstanceId == root.Slots[0]);
        InventoryItem branch = snapshot.Items.Single(item => frame.Slots.Contains(item.InstanceId)
            && LoadoutDefinitions.StandardBranch(item.GameAssetId) == LoadoutItems.BackpackSlotAssetId);
        return [.. snapshot.Items.Single(item => item.InstanceId == branch.Slots[0]).Slots];
    }

    private static void AssertChanges(InventorySnapshot before, ServiceReply reply, InventorySnapshot after)
    {
        ItemChanges changes = Assert.IsType<InventoryOperationsResponse>(reply.Body).Changeset.Items;
        Dictionary<string, InventoryItem> cache = before.Items.ToDictionary(item => item.InstanceId, StringComparer.Ordinal);
        foreach (string id in changes.Deleted)
        {
            Assert.True(cache.Remove(id));
        }

        foreach (InventoryItem item in changes.Created)
        {
            Assert.True(cache.TryAdd(item.InstanceId, item));
        }

        foreach (InventoryItem item in changes.Updated)
        {
            Assert.True(cache.ContainsKey(item.InstanceId));
            cache[item.InstanceId] = item;
        }

        Assert.True(InventorySnapshots.Same(new PlayerLoadout(after.UserId, after.Revision, [.. cache.Values]), new PlayerLoadout(after.UserId, after.Revision, after.Items)));
    }

    public void Dispose() => _database.Dispose();
}
