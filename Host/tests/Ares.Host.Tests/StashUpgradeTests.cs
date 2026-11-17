using System.Text.Json;
using Ares.Common.Game.Inventory;
using Ares.Host.Services;
using Ares.Host.Services.Inventory;
using Ares.Structs.Backend;
using Ares.Structs.Backend.Progression;

namespace Ares.Host.Tests;

public sealed class StashUpgradeTests : IDisposable
{
    private readonly TestDatabase _database = new();

    [Fact]
    public void Upgrading_the_stash_pays_coins_and_keeps_the_container_and_its_contents()
    {
        InventoryStore store = Create();
        store.AddItem(ItemTechnicalNames.Coins, DateTimeOffset.UnixEpoch, 7000);
        OwnedItem duck = Assert.Single(store.Inventory.Stash.Items);
        InventoryItem container = Container(store);
        long next = TestStash.Next(TestCatalog.StashContainer);

        ServiceReply reply = store.Commit(Upgrade(container.InstanceId), DateTimeOffset.UnixEpoch);

        Assert.Equal(200, reply.StatusCode);
        ItemChanges changes = Assert.IsType<InventoryOperationsResponse>(reply.Body).Changeset.Items;
        InventoryItem upgraded = Assert.Single(changes.Updated, item => item.InstanceId == container.InstanceId);
        Assert.Equal(next, upgraded.GameAssetId);
        Assert.Equal(88, upgraded.Slots.Count);
        Assert.Equal(container.Slots, upgraded.Slots.Take(container.Slots.Count));
        Assert.Equal(24, changes.Created.Count);
        Assert.Contains(changes.Updated, item => item.GameAssetId == TestCatalog.Asset(ItemTechnicalNames.Coins) && item.Amount == 2000);
        InventoryStore restarted = new(_database.Store, TestDatabase.UserId, TestCatalog.Real);
        Assert.Equal(next, restarted.Inventory.Stash.ContainerAssetId);
        Assert.Equal(88, TestCatalog.Real.StashCapacity(restarted.Inventory.Stash.ContainerAssetId));
        Assert.Equal([duck], restarted.Inventory.Stash.Items);
    }

    [Fact]
    public void An_upgrade_the_player_cannot_make_changes_nothing()
    {
        InventoryStore store = Create();
        store.AddItem(ItemTechnicalNames.Coins, DateTimeOffset.UnixEpoch, 4999);
        string container = Container(store).InstanceId;
        string duck = Assert.Single(store.Inventory.Stash.Items).InstanceId;
        string saved = _database.InventoryJson;

        (InventoryOperationsRequest Request, string Error)[] rejected =
        [
            (Upgrade(container), "upgrade_cost_not_met"),
            (Upgrade(duck), "item_not_upgradable"),
            (Upgrade("missing"), "item_not_upgradable"),
            (new InventoryOperationsRequest([new UpgradeItem(container, null, TestCatalog.StashContainer)], ""), "item_not_upgradable"),
        ];

        Assert.All(rejected, entry =>
        {
            ServiceReply reply = store.Commit(entry.Request, DateTimeOffset.UnixEpoch);
            Assert.Equal((400, entry.Error), (reply.StatusCode, Assert.IsType<ErrorResponse>(reply.Body).Error));
            Assert.Equal(saved, _database.InventoryJson);
        });
    }

    [Fact]
    public void Every_tier_up_to_the_last_holds_a_full_stash_and_the_last_cannot_be_upgraded()
    {
        InventoryStore store = Create();
        long tier = TestCatalog.StashContainer;
        List<int> sizes = [];
        while (true)
        {
            store.Import(store.Inventory with { Stash = store.Inventory.Stash with { ContainerAssetId = tier } }, store.Inventory.Revision);
            sizes.Add(TestCatalog.Real.StashCapacity(store.Inventory.Stash.ContainerAssetId));
            if (TestCatalog.Content.Stash.Definitions.Single(definition => definition.GameAssetId == tier).Upgrade is not { } upgrade)
            {
                break;
            }

            tier = upgrade.NextItemGameAssetId;
        }

        Assert.Equal([.. Enumerable.Range(0, 12).Select(index => 64 + (24 * index))], sizes);
        store.Import(TestStash.Full(store.Inventory), store.Inventory.Revision);
        Assert.InRange(store.Snapshot.Items.Count, 1, InventorySnapshots.MaximumSnapshotItems);
        store.AddItem(ItemTechnicalNames.Coins, DateTimeOffset.UnixEpoch, 1000);
        ServiceReply reply = store.Commit(Upgrade(Container(store).InstanceId), DateTimeOffset.UnixEpoch);
        Assert.Equal((400, "item_not_upgradable"), (reply.StatusCode, Assert.IsType<ErrorResponse>(reply.Body).Error));
    }

    [Fact]
    public void The_direct_upgrade_call_replies_with_the_upgraded_container()
    {
        InventoryStore store = Create();
        store.AddItem(ItemTechnicalNames.Coins, DateTimeOffset.UnixEpoch, 5000);
        string container = Container(store).InstanceId;
        string coins = Assert.Single(store.Inventory.Wallet).InstanceId;
        UpgradeInventoryItemRequest request = JsonSerializer.Deserialize<UpgradeInventoryItemRequest>(
            $$"""{"instanceId":"{{container}}","etag":"","itemsToConsume":[],"requestId":"upgrade-1","targetItemGameAssetId":0}""", BackendJson.Options)!;

        ServiceReply reply = InventoryStore.UpgradeReply(store.Commit(InventoryStore.UpgradeOperations(request), DateTimeOffset.UnixEpoch), container);

        UpgradeInventoryItemResponse upgraded = Assert.IsType<UpgradeInventoryItemResponse>(reply.Body);
        Assert.Equal((container, TestStash.Next(TestCatalog.StashContainer)), (upgraded.UpgradedItem.InstanceId, upgraded.UpgradedItem.GameAssetId));
        Assert.Equal([coins], upgraded.Changeset.Items.Deleted);
    }

    [Fact]
    public void Operator_prices_are_announced_and_charged()
    {
        StashUpgradePrices prices = new(TestCatalog.Content.Stash, [1234, .. StashUpgradePrices.Default.Coins.Skip(1)]);
        InventoryStore store = new(_database.Store, TestDatabase.UserId, new InventoryCatalog(TestCatalog.Data, TestCatalog.Content, prices));
        store.InitializeLayout(DateTimeOffset.UnixEpoch);
        store.AddItem(ItemTechnicalNames.Coins, DateTimeOffset.UnixEpoch, 2000);
        ItemDescription first = store.GameData().ItemDescriptions.ItemDescriptions.Single(item => item.GameAssetId == TestCatalog.StashContainer);
        Assert.Equal(1234, Assert.Single(first.Upgrade!.CostPackage.CostItems).Amount);
        Assert.Equal(5000, Assert.Single(Assert.IsType<ItemAmountsOfferCost>(TestCatalog.Real.GameData.ItemDescriptions.ItemDescriptions
            .Single(item => item.GameAssetId == TestCatalog.StashContainer).Upgrade!.Cost).ItemAmounts).Amount);

        Assert.Equal(200, store.Commit(Upgrade(Container(store).InstanceId), DateTimeOffset.UnixEpoch).StatusCode);

        Assert.Equal(766, Assert.Single(store.Inventory.Wallet).Quantity);
        Assert.Throws<ArgumentException>(() => new StashUpgradePrices(TestCatalog.Content.Stash, [1, 2]));
    }

    [Fact]
    public void The_batch_upgrade_operation_reads_its_wire_form()
    {
        InventoryOperation operation = JsonSerializer.Deserialize<InventoryOperation>(
            """{"discriminator":"upgrade","itemInstanceId":"stash","payment":{"items":[{"itemInstanceId":"coins","amount":5000}]},"targetItemGameAssetId":0}""",
            BackendJson.Options)!;

        UpgradeItem upgrade = Assert.IsType<UpgradeItem>(operation);
        Assert.Equal(("stash", 0L), (upgrade.ItemInstanceId, upgrade.TargetItemGameAssetId));
        Assert.Equal([new ItemAmountToConsume("coins", 5000)], upgrade.Payment!.Items);
    }

    private static InventoryOperationsRequest Upgrade(string instanceId) => new([new UpgradeItem(instanceId, null, 0)], "");

    private static InventoryItem Container(InventoryStore store) =>
        Assert.Single(store.Snapshot.Items, item => item.GameAssetId == store.Inventory.Stash.ContainerAssetId);

    private InventoryStore Create()
    {
        InventoryStore store = new(_database.Store, TestDatabase.UserId, TestCatalog.Real);
        store.InitializeLayout(DateTimeOffset.UnixEpoch);
        return store;
    }

    public void Dispose() => _database.Dispose();
}
