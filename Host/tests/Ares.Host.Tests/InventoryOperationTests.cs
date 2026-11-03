using Ares.Common.Game.Inventory;
using Ares.Host.Services;
using Ares.Host.Services.Inventory;
using Ares.Structs.Backend.Progression;

namespace Ares.Host.Tests;

public sealed class InventoryOperationTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "ares-menu-test-" + Guid.NewGuid().ToString("N"));

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

        InventoryStore restarted = new(_directory);
        Assert.Equal(backpack, Parent(restarted, duck.InstanceId));
        Assert.Equal(200, restarted.Commit(Move(duck.InstanceId, stash, "back-to-stash"), DateTimeOffset.UnixEpoch.AddMinutes(2)).StatusCode);
        InventoryStore again = new(_directory);
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
        InventoryStore restarted = new(_directory);
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
        string path = Path.Combine(_directory, "inventory-state.json");
        byte[] before = File.ReadAllBytes(path);
        InventoryOperationsRequest batch = new([new SetItemParents(duck, [backpack]), new SetItemParents(duck, ["missing"])], "bad-batch");
        Assert.Equal(400, store.Commit(batch, DateTimeOffset.UnixEpoch).StatusCode);
        string frame = store.Snapshot.Items.Single(item => item.GameAssetId == LoadoutItems.StandardFrameAssetId).InstanceId;
        Assert.Equal(400, store.Commit(Move(duck, frame, "bad-target"), DateTimeOffset.UnixEpoch).StatusCode);
        Assert.Equal(400, store.Commit(Move(frame, backpack, "move-structure"), DateTimeOffset.UnixEpoch).StatusCode);
        Assert.Equal(400, store.Commit(new InventoryOperationsRequest(null, "missing-operations"), DateTimeOffset.UnixEpoch).StatusCode);
        Assert.Equal(400, store.Commit(Move(duck, backpack, "invalid id"), DateTimeOffset.UnixEpoch).StatusCode);
        Assert.Equal(before, File.ReadAllBytes(path));

        store.Reserve(Guid.NewGuid().ToString("D"));
        before = File.ReadAllBytes(path);
        Assert.Equal(409, store.Commit(Move(duck, backpack, "reserved"), DateTimeOffset.UnixEpoch).StatusCode);
        Assert.Equal(before, File.ReadAllBytes(path));
    }

    [Fact]
    public void An_occupied_destination_rejects_the_entire_batch_without_losing_either_item()
    {
        InventoryStore store = Create();
        InventoryItem duck = Duck(store);
        string backpack = BackpackSlot(store);
        InventoryItem second = duck with { InstanceId = Guid.NewGuid().ToString("D") };
        InventoryItem[] items = [.. store.Snapshot.Items.Select(item => item.InstanceId == backpack ? item with { Slots = [second.InstanceId] } : item), second];
        store.Import(new InventoryImport(items, [.. StorageItems.Definitions, .. LoadoutItems.Definitions], store.Snapshot.LoadoutRoots),
            store.Snapshot.Revision);
        byte[] before = File.ReadAllBytes(Path.Combine(_directory, "inventory-state.json"));
        Assert.Equal(400, store.Commit(Move(duck.InstanceId, backpack, "occupied"), DateTimeOffset.UnixEpoch).StatusCode);
        Assert.Equal(before, File.ReadAllBytes(Path.Combine(_directory, "inventory-state.json")));
        Assert.Equal(backpack, Parent(store, second.InstanceId));
        Assert.Equal(2, store.Snapshot.Items.Count(item => item.GameAssetId == StorageItems.RubberDuckAssetId));
    }

    private InventoryStore Create()
    {
        InventoryStore store = new(_directory);
        store.EnsureStarterStorage(DateTimeOffset.UnixEpoch);
        store.EnsureStarterLoadout(DateTimeOffset.UnixEpoch);
        return store;
    }

    private static InventoryItem Duck(InventoryStore store) => store.Snapshot.Items.Single(item => item.GameAssetId == StorageItems.RubberDuckAssetId);

    private static string Parent(InventoryStore store, string itemId) => store.Snapshot.Items.Single(item => item.Slots.Contains(itemId)).InstanceId;

    private static string BackpackSlot(InventoryStore store) => store.Snapshot.Items.Single(item => item.GameAssetId == LoadoutItems.BackpackAssetId).Slots[0];

    private static InventoryOperationsRequest Move(string item, string target, string requestId) => new([new SetItemParents(item, [target])], requestId);

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }
}
