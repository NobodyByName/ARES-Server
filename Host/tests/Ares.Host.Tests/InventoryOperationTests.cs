using Ares.Common.Game.Inventory;
using Ares.Host.Services;
using Ares.Host.Services.Inventory;
using Ares.Structs.Backend.Progression;

namespace Ares.Host.Tests;

public sealed class InventoryOperationTests : IDisposable
{
    private readonly TestDatabase _database = new();

    [Fact]
    public void Unequipping_into_a_noncanonical_stash_slot_clears_the_predicted_augment_copy()
    {
        InventoryStore store = Create();
        store.AddItem(ItemTechnicalNames.CombatMk1, DateTimeOffset.UnixEpoch);
        string augment = store.Inventory.Stash.Items.Single(item => item.Type == ItemTechnicalNames.CombatMk1).InstanceId;
        string equipment = store.Snapshot.Items.Single(item => item.GameAssetId == AugmentItems.AugmentContentSlotAssetId).InstanceId;
        Assert.Equal(200, store.Commit(Move(augment, equipment, "equip"), DateTimeOffset.UnixEpoch).StatusCode);
        InventorySnapshot before = store.Snapshot;
        string target = before.Items.Single(item => item.GameAssetId == StorageItems.StashContainerAssetId).Slots[3];
        InventoryOperationsRequest request = Move(augment, target, "");
        Dictionary<string, InventoryItem> predicted = Predict(before, request);
        ServiceReply response = store.Commit(request, DateTimeOffset.UnixEpoch);
        Assert.Equal(200, response.StatusCode);
        Assert.NotEqual(target, Parent(store, augment));
        AssertReconcilesPrediction(predicted, response, store.Snapshot);
        Assert.Single(store.Inventory.Stash.Items, item => item.InstanceId == augment);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Canonical_stash_placement_reconciles_predictions_including_repeated_empty_id_requests(bool carried)
    {
        InventoryStore store = Create();
        string duck = Duck(store).InstanceId;
        if (carried)
        {
            Assert.Equal(200, store.Commit(Move(duck, BackpackSlot(store), "carry"), DateTimeOffset.UnixEpoch).StatusCode);
        }

        string target = store.Snapshot.Items.Single(item => item.GameAssetId == StorageItems.StashContainerAssetId).Slots[20];
        InventoryOperationsRequest request = Move(duck, target, "");
        for (int attempt = 0; attempt < 2; attempt++)
        {
            InventorySnapshot before = store.Snapshot;
            Dictionary<string, InventoryItem> predicted = Predict(before, request);
            ServiceReply response = store.Commit(request, DateTimeOffset.UnixEpoch);
            Assert.Equal(200, response.StatusCode);
            AssertReconcilesPrediction(predicted, response, store.Snapshot);
            Assert.Equal(1, store.Inventory.ItemCount);
            Assert.Equal(before.Revision + (carried && attempt == 0 ? 1 : 0), store.Snapshot.Revision);
        }
    }

    private static Dictionary<string, InventoryItem> Predict(InventorySnapshot snapshot, InventoryOperationsRequest request)
    {
        Dictionary<string, InventoryItem> cache = snapshot.Items.ToDictionary(item => item.InstanceId, StringComparer.Ordinal);
        foreach (SetItemParents operation in request.Operations!.Cast<SetItemParents>())
        {
            foreach (InventoryItem parent in cache.Values.Where(item => item.Slots.Contains(operation.ItemInstanceId)).ToArray())
            {
                cache[parent.InstanceId] = parent with { Slots = [.. parent.Slots.Where(id => id != operation.ItemInstanceId)] };
            }

            foreach (string id in operation.ParentInstanceIds)
            {
                cache[id] = cache[id] with { Slots = [.. cache[id].Slots, operation.ItemInstanceId] };
            }
        }

        return cache;
    }

    private static void AssertReconcilesPrediction(Dictionary<string, InventoryItem> cache, ServiceReply response, InventorySnapshot authoritative)
    {
        ItemChanges changes = Assert.IsType<InventoryOperationsResponse>(response.Body).Changeset.Items;
        foreach (string id in changes.Deleted)
        {
            cache.Remove(id);
        }

        foreach (InventoryItem item in changes.Created.Concat(changes.Updated))
        {
            cache[item.InstanceId] = item;
        }

        InventorySnapshot reconciled = InventorySnapshots.Copy(authoritative with { Items = [.. cache.Values] });
        Assert.True(InventorySnapshots.Same(new PlayerLoadout(reconciled.UserId, reconciled.Revision, reconciled.Items),
            new PlayerLoadout(authoritative.UserId, authoritative.Revision, authoritative.Items)));
    }

    [Fact]
    public void The_same_duck_moves_both_ways_and_its_location_survives_restarts()
    {
        InventoryStore store = Create();
        InventoryItem duck = Duck(store);
        string stash = Parent(store, duck.InstanceId);
        string backpack = BackpackSlot(store);
        int count = store.Snapshot.Items.Count;
        long revision = store.Snapshot.Revision;

        ServiceReply moved = store.Commit(Move(duck.InstanceId, backpack, "into-backpack"), DateTimeOffset.UnixEpoch.AddMinutes(1));
        Assert.Equal(200, moved.StatusCode);
        ItemChanges changes = Assert.IsType<InventoryOperationsResponse>(moved.Body).Changeset.Items;
        Assert.Empty(changes.Created);
        Assert.Empty(changes.Deleted);
        Assert.Equal(3, changes.Updated.Count);
        Assert.Equal(backpack, Parent(store, duck.InstanceId));
        Assert.Equal(count, store.Snapshot.Items.Count);
        Assert.Equal(revision + 1, store.Snapshot.Revision);
        Assert.Equal(duck.Amount, Duck(store).Amount);
        Assert.Contains(InventorySnapshots.Loadout(store.Snapshot).Items, item => item.InstanceId == duck.InstanceId);

        InventoryStore restarted = new(_database.Store, TestDatabase.UserId);
        Assert.Equal(backpack, Parent(restarted, duck.InstanceId));
        Assert.Equal(200, restarted.Commit(Move(duck.InstanceId, stash, "back-to-stash"), DateTimeOffset.UnixEpoch.AddMinutes(2)).StatusCode);
        InventoryStore again = new(_database.Store, TestDatabase.UserId);
        Assert.Equal(stash, Parent(again, duck.InstanceId));
        Assert.Equal(count, again.Snapshot.Items.Count);
        Assert.DoesNotContain(InventorySnapshots.Loadout(again.Snapshot).Items, item => item.InstanceId == duck.InstanceId);
        Assert.Equal(duck.InstanceId, Duck(again).InstanceId);
    }

    [Fact]
    public void Retries_are_persisted_and_cannot_reapply_a_move_after_a_later_move()
    {
        InventoryStore store = Create();
        string duck = Duck(store).InstanceId;
        string stash = Parent(store, duck);
        InventoryOperationsRequest first = Move(duck, BackpackSlot(store), "first");
        Assert.Equal(200, store.Commit(first, DateTimeOffset.UnixEpoch).StatusCode);
        long revision = store.Snapshot.Revision;
        Assert.Equal(200, store.Commit(first, DateTimeOffset.UnixEpoch.AddMinutes(1)).StatusCode);
        Assert.Equal(revision, store.Snapshot.Revision);
        Assert.Equal(409, store.Commit(Move(duck, stash, "first"), DateTimeOffset.UnixEpoch).StatusCode);
        Assert.Equal(200, store.Commit(Move(duck, stash, "second"), DateTimeOffset.UnixEpoch).StatusCode);
        InventoryStore restarted = new(_database.Store, TestDatabase.UserId);
        revision = restarted.Snapshot.Revision;
        ServiceReply replay = restarted.Commit(first, DateTimeOffset.UnixEpoch);
        Assert.Equal(200, replay.StatusCode);
        Assert.Equal(stash, Parent(restarted, duck));
        Assert.Equal(revision, restarted.Snapshot.Revision);
        Assert.Contains(Assert.IsType<InventoryOperationsResponse>(replay.Body).Changeset.Items.Updated,
            item => item.InstanceId == stash && item.Slots.Contains(duck));
    }

    [Fact]
    public void Invalid_batches_and_reserved_loadouts_leave_the_save_unchanged()
    {
        InventoryStore store = Create();
        string duck = Duck(store).InstanceId;
        string backpack = BackpackSlot(store);
        string before = _database.InventoryJson;
        InventoryOperationsRequest batch = new([new SetItemParents(duck, [backpack]), new SetItemParents(duck, ["missing"])], "bad-batch");
        Assert.Equal(400, store.Commit(batch, DateTimeOffset.UnixEpoch).StatusCode);
        string frame = store.Snapshot.Items.Single(item => item.GameAssetId == LoadoutItems.StandardFrameAssetId).InstanceId;
        Assert.Equal(400, store.Commit(Move(duck, frame, "bad-target"), DateTimeOffset.UnixEpoch).StatusCode);
        Assert.Equal(400, store.Commit(Move(frame, backpack, "move-structure"), DateTimeOffset.UnixEpoch).StatusCode);
        Assert.Equal(400, store.Commit(new InventoryOperationsRequest(null, "missing-operations"), DateTimeOffset.UnixEpoch).StatusCode);
        Assert.Equal(400, store.Commit(Move(duck, backpack, "invalid id"), DateTimeOffset.UnixEpoch).StatusCode);
        Assert.Equal(before, _database.InventoryJson);

        store.Reserve(Guid.NewGuid().ToString("D"));
        before = _database.InventoryJson;
        Assert.Equal(409, store.Commit(Move(duck, backpack, "reserved"), DateTimeOffset.UnixEpoch).StatusCode);
        Assert.Equal(before, _database.InventoryJson);
    }

    [Fact]
    public void An_occupied_destination_rejects_the_entire_batch_without_losing_either_item()
    {
        InventoryStore store = Create();
        InventoryItem duck = Duck(store);
        string backpack = BackpackSlot(store);
        OwnedItem second = Assert.Single(store.Inventory.Stash.Items) with { InstanceId = Guid.NewGuid().ToString("D"), Slot = 0 };
        store.Import(store.Inventory with { Loadout = store.Inventory.Loadout with { Backpack = new PlayerItemContainer([second]) } }, store.Inventory.Revision);
        string before = _database.InventoryJson;
        Assert.Equal(400, store.Commit(Move(duck.InstanceId, backpack, "occupied"), DateTimeOffset.UnixEpoch).StatusCode);
        Assert.Equal(before, _database.InventoryJson);
        Assert.Equal(backpack, Parent(store, second.InstanceId));
        Assert.Equal(2, store.Snapshot.Items.Count(item => item.GameAssetId == StorageItems.RubberDuckAssetId));
    }

    [Fact]
    public void Stash_order_is_derived_and_moves_return_all_slots_affected_by_compaction()
    {
        InventoryStore store = Create();
        OwnedItem original = Assert.Single(store.Inventory.Stash.Items);
        OwnedItem first = original with { InstanceId = "a-duck" };
        OwnedItem second = original with { InstanceId = "b-duck" };
        store.Import(store.Inventory with { Stash = store.Inventory.Stash with { Items = [second, first] } }, store.Inventory.Revision);
        InventoryItem stash = store.Snapshot.Items.Single(item => item.GameAssetId == StorageItems.StashContainerAssetId);
        string firstSlot = stash.Slots[0];
        string secondSlot = stash.Slots[1];
        Assert.Contains(first.InstanceId, store.Snapshot.Items.Single(item => item.InstanceId == firstSlot).Slots);
        ServiceReply moved = store.Commit(Move(first.InstanceId, BackpackSlot(store), "compact-stash"), DateTimeOffset.UnixEpoch);
        Assert.Equal(200, moved.StatusCode);
        IReadOnlyList<InventoryItem> changed = Assert.IsType<InventoryOperationsResponse>(moved.Body).Changeset.Items.Updated;
        Assert.Contains(changed, item => item.InstanceId == firstSlot && item.Slots.Contains(second.InstanceId));
        Assert.Contains(changed, item => item.InstanceId == secondSlot && item.Slots.Count == 0);
        Assert.Null(Assert.Single(store.Inventory.Stash.Items).Slot);
        Assert.Equal(0, Assert.Single(store.Inventory.Loadout.Backpack.Items).Slot);
        Assert.Equal(2, store.Inventory.ItemCount);
    }

    private InventoryStore Create()
    {
        InventoryStore store = new(_database.Store, TestDatabase.UserId);
        store.InitializeLayout(DateTimeOffset.UnixEpoch);
        return store;
    }

    private static InventoryItem Duck(InventoryStore store) => store.Snapshot.Items.Single(item => item.GameAssetId == StorageItems.RubberDuckAssetId);

    private static string Parent(InventoryStore store, string itemId) => store.Snapshot.Items.Single(item => item.Slots.Contains(itemId)).InstanceId;

    private static string BackpackSlot(InventoryStore store) => store.Snapshot.Items.Single(item => item.GameAssetId == LoadoutItems.BackpackAssetId).Slots[0];

    private static InventoryOperationsRequest Move(string item, string target, string requestId) => new([new SetItemParents(item, [target])], requestId);

    public void Dispose() => _database.Dispose();
}
