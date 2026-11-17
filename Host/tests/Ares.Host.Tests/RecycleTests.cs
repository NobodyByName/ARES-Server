using Ares.Common.Game.Inventory;
using Ares.Host.Services;
using Ares.Host.Services.Inventory;
using Ares.Structs.Backend;
using Ares.Structs.Backend.Progression;

namespace Ares.Host.Tests;

public sealed class RecycleTests : IDisposable
{
    private readonly TestDatabase _database = new();

    [Fact]
    public void Game_data_describes_recycling_for_items_whose_outputs_are_all_supported()
    {
        IReadOnlyList<ItemDescription> descriptions = Create().GameData().ItemDescriptions.ItemDescriptions;
        HashSet<long> supported = [.. descriptions.Select(item => item.GameAssetId)];

        Assert.All(descriptions, item =>
        {
            bool recyclable = ItemRecycling.Outputs.TryGetValue(item.GameAssetId, out IReadOnlyList<ItemAssetAmount>? outputs)
                && outputs.All(output => supported.Contains(output.GameAssetId));
            Assert.Equal(recyclable, item.Recycle is not null);
            if (!recyclable)
            {
                return;
            }

            Assert.Equal(outputs, item.Recycle!.RewardedItems.Select(reward => new ItemAssetAmount(reward.GameAssetId, reward.Amount)));
            Assert.All(item.Recycle.RewardedItems, reward => Assert.Equal(1.0, reward.Durability));
        });
        Assert.Equal([new ItemAssetAmount(Asset(ItemTechnicalNames.RubberParts), 2)], Outputs(descriptions, ItemTechnicalNames.Wires));
        Assert.NotNull(Description(descriptions, ItemTechnicalNames.FerroI).Recycle);
        Assert.Null(Description(descriptions, ItemTechnicalNames.MetalParts).Recycle);
        Assert.Null(Description(descriptions, ItemTechnicalNames.RubberDuck).Recycle);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(3L)]
    public void Recycling_a_whole_stack_deletes_it_and_adds_its_outputs_to_the_stash(long? amount)
    {
        InventoryStore store = Create();
        string wires = Add(store, ItemTechnicalNames.Wires, 3);

        ServiceReply reply = store.Commit(Recycle(wires, amount), DateTimeOffset.UnixEpoch);

        Assert.Equal(200, reply.StatusCode);
        ItemChanges changes = Changes(reply);
        Assert.Equal([wires], changes.Deleted);
        InventoryItem rubber = Assert.Single(changes.Created);
        Assert.Equal((Asset(ItemTechnicalNames.RubberParts), 3 * 2L), (rubber.GameAssetId, rubber.Amount));
        Assert.Equal(rubber, store.Snapshot.Items.Single(item => item.InstanceId == rubber.InstanceId));
        Assert.DoesNotContain(store.Inventory.Stash.Items, item => item.InstanceId == wires);
    }

    [Fact]
    public void Recycling_part_of_a_stack_keeps_the_rest_and_fills_existing_stacks_first()
    {
        InventoryStore store = Create();
        long stackLimit = InventoryCatalog.ByAsset[Asset(ItemTechnicalNames.RubberParts)].MaxAmount;
        string rubber = Add(store, ItemTechnicalNames.RubberParts, stackLimit - 1);
        string wires = Add(store, ItemTechnicalNames.Wires, 5);

        ServiceReply reply = store.Commit(Recycle(wires, 2), DateTimeOffset.UnixEpoch);

        Assert.Equal(200, reply.StatusCode);
        ItemChanges changes = Changes(reply);
        Assert.Empty(changes.Deleted);
        Assert.Equal(3, Assert.Single(changes.Updated, item => item.InstanceId == wires).Amount);
        Assert.Equal(stackLimit, Assert.Single(changes.Updated, item => item.InstanceId == rubber).Amount);
        Assert.Equal(3, Assert.Single(changes.Created).Amount);
    }

    [Fact]
    public void Recycling_a_weapon_removes_what_it_holds()
    {
        InventoryStore store = Create();
        string ferro = Add(store, ItemTechnicalNames.FerroI, 1);
        string[] held = [.. store.Snapshot.Items.Single(item => item.InstanceId == ferro).Slots];
        Assert.NotEmpty(held);
        IReadOnlyList<ItemAssetAmount> outputs = ItemRecycling.Outputs[Asset(ItemTechnicalNames.FerroI)];

        ServiceReply reply = store.Commit(Recycle(ferro), DateTimeOffset.UnixEpoch);

        Assert.Equal(200, reply.StatusCode);
        ItemChanges changes = Changes(reply);
        Assert.Equal([.. held.Append(ferro).Order(StringComparer.Ordinal)], changes.Deleted.Order(StringComparer.Ordinal));
        Assert.Equal(outputs.OrderBy(output => output.GameAssetId),
            changes.Created.Select(item => new ItemAssetAmount(item.GameAssetId, item.Amount)).OrderBy(output => output.GameAssetId));
    }

    [Fact]
    public void Recycling_that_cannot_complete_changes_nothing()
    {
        InventoryStore store = Create();
        store.AddItem(ItemTechnicalNames.Coins, DateTimeOffset.UnixEpoch, 5);
        string coins = Assert.Single(store.Inventory.Wallet).InstanceId;
        string metal = Add(store, ItemTechnicalNames.MetalParts, 2);
        string wires = Add(store, ItemTechnicalNames.Wires, 5);
        PlayerInventory state = store.Inventory;
        store.Import(TestStash.Full(state), state.Revision);
        string saved = _database.InventoryJson;

        (InventoryOperationsRequest Request, string Error)[] rejected =
        [
            (Recycle(metal), "item_not_recyclable"),
            (Recycle(coins), "item_not_recyclable"),
            (Recycle("missing"), "item_not_recyclable"),
            (Recycle(wires, 6), "invalid_recycle_amount"),
            (Recycle(wires, 0), "invalid_inventory_operations"),
            (Recycle(wires, 1), "stash_full"),
        ];

        Assert.All(rejected, entry =>
        {
            ServiceReply reply = store.Commit(entry.Request, DateTimeOffset.UnixEpoch);
            Assert.Equal((400, entry.Error), (reply.StatusCode, Assert.IsType<ErrorResponse>(reply.Body).Error));
        });
        Assert.Equal(saved, _database.InventoryJson);
        Assert.Equal(200, store.Commit(Recycle(wires), DateTimeOffset.UnixEpoch).StatusCode);
    }

    private static long Asset(string type) => LoadoutDefinitions.Generated.AssetIds[type];

    private static ItemDescription Description(IReadOnlyList<ItemDescription> descriptions, string type) =>
        descriptions.Single(item => item.GameAssetId == Asset(type));

    private static IEnumerable<ItemAssetAmount> Outputs(IReadOnlyList<ItemDescription> descriptions, string type) =>
        Description(descriptions, type).Recycle!.RewardedItems.Select(reward => new ItemAssetAmount(reward.GameAssetId, reward.Amount));

    private static InventoryOperationsRequest Recycle(string itemId, long? amount = null) => new([new RecycleItem(itemId, amount)], "");

    private static ItemChanges Changes(ServiceReply reply) => Assert.IsType<InventoryOperationsResponse>(reply.Body).Changeset.Items;

    private static string Add(InventoryStore store, string type, long quantity)
    {
        HashSet<string> existing = [.. store.Inventory.Stash.Items.Select(item => item.InstanceId)];
        store.AddItem(type, DateTimeOffset.UnixEpoch, quantity);
        return store.Inventory.Stash.Items.Single(item => !existing.Contains(item.InstanceId)).InstanceId;
    }

    private InventoryStore Create()
    {
        InventoryStore store = new(_database.Store, TestDatabase.UserId);
        store.InitializeLayout(DateTimeOffset.UnixEpoch);
        return store;
    }

    public void Dispose() => _database.Dispose();
}
