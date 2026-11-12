using Ares.Host.Services;
using Ares.Host.Services.Inventory;
using Ares.Structs.Backend.Progression;

namespace Ares.Host.Tests;

public sealed class InventoryAmountTests : IDisposable
{
    private const string SplitId = "0f6c2a1e-7b3d-4c5a-9e8f-1a2b3c4d5e6f";
    private readonly TestDatabase _database = new();

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Splitting_creates_a_placed_stack_and_reduces_the_source(bool intoBackpack)
    {
        InventoryStore store = Create();
        string source = AddAmmo(store, 30);
        string target = intoBackpack ? BackpackSlot(store) : StashSlot(store, 5);
        ServiceReply reply = store.Commit(Request(new MoveItemAmount(source, SplitId, 15), new SetItemParents(SplitId, [target])), DateTimeOffset.UnixEpoch);

        Assert.Equal(200, reply.StatusCode);
        ItemChanges changes = Changes(reply);
        Assert.Equal(15, Assert.Single(changes.Created, item => item.InstanceId == SplitId).Amount);
        Assert.Contains(changes.Updated, item => item.InstanceId == source && item.Amount == 15);
        Assert.Empty(changes.Deleted);
        Assert.Equal(15, Amount(store, source));
        Assert.Equal(15, Amount(store, SplitId));
        Assert.Equal(intoBackpack, store.Inventory.Loadout.Backpack.Items.Any(item => item.InstanceId == SplitId));
    }

    [Fact]
    public void Merging_a_whole_stack_deletes_the_source()
    {
        InventoryStore store = Create();
        string target = AddAmmo(store, 30);
        string source = AddAmmo(store, 20);
        ServiceReply reply = store.Commit(Request(new MoveItemAmount(source, target, 20)), DateTimeOffset.UnixEpoch);

        Assert.Equal(200, reply.StatusCode);
        ItemChanges changes = Changes(reply);
        Assert.Equal([source], changes.Deleted);
        Assert.Contains(changes.Updated, item => item.InstanceId == target && item.Amount == 50);
        Assert.Equal(50, Amount(store, target));
        Assert.DoesNotContain(store.Inventory.Stash.Items, item => item.InstanceId == source);
    }

    [Fact]
    public void A_parent_move_of_a_merged_away_stack_is_ignored()
    {
        InventoryStore store = Create();
        string target = AddAmmo(store, 30);
        string source = AddAmmo(store, 20);
        ServiceReply reply = store.Commit(Request(new SetItemParents(source, [BackpackSlot(store)]), new MoveItemAmount(source, target, 20)),
            DateTimeOffset.UnixEpoch);

        Assert.Equal(200, reply.StatusCode);
        Assert.Equal(50, Amount(store, target));
        Assert.Empty(store.Inventory.Loadout.Backpack.Items);
    }

    [Fact]
    public void Invalid_amount_moves_change_nothing()
    {
        InventoryStore store = Create();
        long max = MaxAmmo(store);
        string full = AddAmmo(store, max);
        string partial = AddAmmo(store, 1);
        string duck = store.Inventory.Stash.Items.Single(item => item.Type == ItemTechnicalNames.RubberDuck).InstanceId;
        store.AddItem(ItemTechnicalNames.FerroI, DateTimeOffset.UnixEpoch);
        string ferro = store.Inventory.Stash.Items.Single(item => item.Type == ItemTechnicalNames.FerroI).InstanceId;
        PlayerInventory before = store.Inventory;

        InventoryOperationsRequest[] rejected =
        [
            Request(new MoveItemAmount(partial, full, 1)),
            Request(new MoveItemAmount(partial, SplitId, 2), new SetItemParents(SplitId, [StashSlot(store, 5)])),
            Request(new MoveItemAmount(full, SplitId, 1)),
            Request(new MoveItemAmount(full, duck, 1)),
            Request(new MoveItemAmount(full, partial, 0)),
            Request(new MoveItemAmount(full, StashSlot(store, 5), 1), new SetItemParents(StashSlot(store, 5), [StashSlot(store, 6)])),
            Request(new MoveItemAmount(ferro, SplitId, 1), new SetItemParents(SplitId, [StashSlot(store, 5)])),
        ];
        Assert.All(rejected.Select(request => store.Commit(request, DateTimeOffset.UnixEpoch).StatusCode), status => Assert.Equal(400, status));

        Assert.Equal(before.Revision, store.Inventory.Revision);
        Assert.Equal(before.Stash.Items, store.Inventory.Stash.Items);
    }

    [Fact]
    public void A_repeated_split_request_is_applied_once()
    {
        InventoryStore store = Create();
        string source = AddAmmo(store, 30);
        InventoryOperationsRequest request = Request(new MoveItemAmount(source, SplitId, 10), new SetItemParents(SplitId, [StashSlot(store, 5)])) with
        {
            RequestId = "split",
        };

        Assert.Equal(200, store.Commit(request, DateTimeOffset.UnixEpoch).StatusCode);
        Assert.Equal(200, store.Commit(request, DateTimeOffset.UnixEpoch).StatusCode);
        Assert.Equal(20, Amount(store, source));
        Assert.Equal(10, Amount(store, SplitId));
    }

    private static InventoryOperationsRequest Request(params InventoryOperation[] operations) => new(operations, "");

    private static ItemChanges Changes(ServiceReply reply) => Assert.IsType<InventoryOperationsResponse>(reply.Body).Changeset.Items;

    private static long Amount(InventoryStore store, string itemId) => store.Snapshot.Items.Single(item => item.InstanceId == itemId).Amount;

    private static long MaxAmmo(InventoryStore store) =>
        store.GameData().ItemDescriptions.ItemDescriptions.Single(item => item.GameAssetId == WeaponItems.HeavyAmmoAssetId).MaxAmount;

    private static string AddAmmo(InventoryStore store, long quantity)
    {
        HashSet<string> existing = [.. store.Inventory.Stash.Items.Select(item => item.InstanceId)];
        store.AddItem(ItemTechnicalNames.HeavyAmmo, DateTimeOffset.UnixEpoch, quantity);
        return store.Inventory.Stash.Items.Single(item => !existing.Contains(item.InstanceId)).InstanceId;
    }

    private static string StashSlot(InventoryStore store, int index) =>
        store.Snapshot.Items.Single(item => item.GameAssetId == StorageItems.StashContainerAssetId).Slots[index];

    private static string BackpackSlot(InventoryStore store) => store.Snapshot.Items.Single(item => item.GameAssetId == LoadoutItems.BackpackAssetId).Slots[0];

    private InventoryStore Create()
    {
        InventoryStore store = new(_database.Store, TestDatabase.UserId);
        store.InitializeLayout(DateTimeOffset.UnixEpoch);
        return store;
    }

    public void Dispose() => _database.Dispose();
}
