using Ares.Host.Services;
using Ares.Host.Services.Inventory;
using Ares.Structs.Backend;
using Ares.Structs.Backend.Progression;

namespace Ares.Host.Tests;

public sealed class SaleTests : IDisposable
{
    private readonly TestDatabase _database = new();

    [Fact]
    public void Game_data_gives_sellable_items_their_coin_value_and_describes_coins()
    {
        IReadOnlyList<ItemDescription> descriptions = Create().GameData().ItemDescriptions.ItemDescriptions;

        Assert.All(descriptions.Where(item => item.Scrap is not null), item =>
        {
            OfferRewardedItem reward = Assert.Single(item.Scrap!.RewardPackage.RewardedItems);
            Assert.Equal((CurrencyItems.CoinsAssetId, (long)ItemValues.ScrapValues[item.GameAssetId]), (reward.GameAssetId, reward.Amount));
        });
        Assert.Equal(75, Value(descriptions, ItemTechnicalNames.MetalParts));
        Assert.Equal(475, Value(descriptions, ItemTechnicalNames.FerroI));
        Assert.Equal(1000, Value(descriptions, ItemTechnicalNames.RubberDuck));
        Assert.Null(descriptions.Single(item => item.GameAssetId == StorageItems.StashSlotAssetId).Scrap);
        ItemDescription coins = descriptions.Single(item => item.GameAssetId == CurrencyItems.CoinsAssetId);
        Assert.Null(coins.Scrap);
        Assert.True(coins.Stackable);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(3L)]
    public void Selling_a_whole_stack_deletes_it_and_creates_the_coin_stack(long? amount)
    {
        InventoryStore store = Create();
        string stack = Add(store, ItemTechnicalNames.MetalParts, 3);

        ServiceReply reply = store.Commit(Sell(stack, amount), DateTimeOffset.UnixEpoch);

        Assert.Equal(200, reply.StatusCode);
        ItemChanges changes = Changes(reply);
        Assert.Equal([stack], changes.Deleted);
        InventoryItem coins = Assert.Single(changes.Created);
        Assert.Equal((CurrencyItems.CoinsAssetId, 3 * 75L), (coins.GameAssetId, coins.Amount));
        Assert.Empty(coins.Slots);
        Assert.DoesNotContain(store.Snapshot.Items, item => item.Slots.Contains(coins.InstanceId));
        Assert.Equal(coins, store.Snapshot.Items.Single(item => item.InstanceId == coins.InstanceId));
        Assert.DoesNotContain(store.Inventory.Stash.Items, item => item.InstanceId == stack);
        Assert.Equal(3 * 75L, Coins(store));
    }

    [Fact]
    public void A_stack_split_off_and_sold_in_the_same_batch_needs_no_place()
    {
        InventoryStore store = Create();
        string ducks = Add(store, ItemTechnicalNames.RubberDuck, 3);
        const string SplitId = "0f6c2a1e-7b3d-4c5a-9e8f-1a2b3c4d5e6f";
        string slot = store.Snapshot.Items.Single(item => item.GameAssetId == StorageItems.StashContainerAssetId).Slots[5];

        ServiceReply reply = store.Commit(new InventoryOperationsRequest(
            [new MoveItemAmount(ducks, SplitId, 1), new SetItemParents(SplitId, [slot]), new ScrapItem(SplitId, 1)], ""), DateTimeOffset.UnixEpoch);

        Assert.Equal(200, reply.StatusCode);
        ItemChanges changes = Changes(reply);
        Assert.Empty(changes.Deleted);
        Assert.Equal(2, Assert.Single(changes.Updated, item => item.InstanceId == ducks).Amount);
        Assert.DoesNotContain(store.Snapshot.Items, item => item.InstanceId == SplitId);
        Assert.Equal(1000L, Coins(store));
    }

    [Fact]
    public void Selling_part_of_a_stack_keeps_the_rest()
    {
        InventoryStore store = Create();
        string stack = Add(store, ItemTechnicalNames.MetalParts, 5);

        ServiceReply reply = store.Commit(Sell(stack, 2), DateTimeOffset.UnixEpoch);

        Assert.Equal(200, reply.StatusCode);
        ItemChanges changes = Changes(reply);
        Assert.Empty(changes.Deleted);
        Assert.Equal(3, Assert.Single(changes.Updated, item => item.InstanceId == stack).Amount);
        Assert.Equal(2 * 75L, Assert.Single(changes.Created).Amount);
        Assert.Equal(2 * 75L, Coins(store));
    }

    [Fact]
    public void Selling_a_weapon_removes_what_it_holds_and_prices_its_durability()
    {
        InventoryStore store = Create();
        string ferro = Add(store, ItemTechnicalNames.FerroI, 1);
        PlayerInventory worn = store.Inventory;
        store.Import(worn with
        {
            Stash = worn.Stash with { Items = [.. worn.Stash.Items.Select(item => item.InstanceId == ferro ? item with { Durability = 0.5 } : item)] },
        }, worn.Revision);
        string[] held = [.. store.Snapshot.Items.Single(item => item.InstanceId == ferro).Slots];
        Assert.NotEmpty(held);

        ServiceReply reply = store.Commit(Sell(ferro), DateTimeOffset.UnixEpoch);

        Assert.Equal(200, reply.StatusCode);
        Assert.Equal([.. held.Append(ferro).Order(StringComparer.Ordinal)], Changes(reply).Deleted.Order(StringComparer.Ordinal));
        Assert.Equal(ItemRewardPackages.SaleUnitPrice(475, 0.5), Coins(store));
        Assert.DoesNotContain(store.Snapshot.Items, item => held.Contains(item.InstanceId));
    }

    [Fact]
    public void Coins_join_the_one_coin_stack()
    {
        InventoryStore store = Create();
        store.AddItem(ItemTechnicalNames.Coins, DateTimeOffset.UnixEpoch, 100);
        store.AddItem(ItemTechnicalNames.Coins, DateTimeOffset.UnixEpoch, 20);
        OwnedItem wallet = Assert.Single(store.Inventory.Wallet);
        Assert.Equal(120, wallet.Quantity);
        Assert.DoesNotContain(store.Inventory.Stash.Items, item => item.Type == ItemTechnicalNames.Coins);
        string duck = store.Inventory.Stash.Items.Single(item => item.Type == ItemTechnicalNames.RubberDuck).InstanceId;

        ServiceReply reply = store.Commit(Sell(duck), DateTimeOffset.UnixEpoch);

        Assert.Equal(200, reply.StatusCode);
        ItemChanges changes = Changes(reply);
        Assert.Empty(changes.Created);
        Assert.Equal(1120, Assert.Single(changes.Updated, item => item.InstanceId == wallet.InstanceId).Amount);
        Assert.Equal([duck], changes.Deleted);
        Assert.Equal(1120, Assert.Single(store.Inventory.Wallet).Quantity);
    }

    [Fact]
    public void Sales_that_cannot_complete_change_nothing()
    {
        InventoryStore store = Create();
        long maximum = CurrencyItems.Definitions.Single().MaxAmount;
        store.AddItem(ItemTechnicalNames.Coins, DateTimeOffset.UnixEpoch, maximum - 100);
        string metal = Add(store, ItemTechnicalNames.MetalParts, 2);
        string coins = Assert.Single(store.Inventory.Wallet).InstanceId;
        string seed = Add(store, ItemTechnicalNames.AssortedSeeds, 1);
        string saved = _database.InventoryJson;
        Assert.Throws<InvalidDataException>(() => store.AddItem(ItemTechnicalNames.Coins, DateTimeOffset.UnixEpoch, 101));

        (InventoryOperationsRequest Request, string Error)[] rejected =
        [
            (Sell(metal), "currency_full"),
            (Sell(metal, 3), "invalid_scrap_amount"),
            (Sell(coins), "item_not_sellable"),
            (Sell("missing"), "item_not_sellable"),
            (Sell(seed, 0), "invalid_inventory_operations"),
            (new InventoryOperationsRequest([new ScrapItem(seed, null), new ScrapItem(metal, null)], ""), "currency_full"),
        ];

        Assert.All(rejected, entry =>
        {
            ServiceReply reply = store.Commit(entry.Request, DateTimeOffset.UnixEpoch);
            Assert.Equal((400, entry.Error), (reply.StatusCode, Assert.IsType<ErrorResponse>(reply.Body).Error));
        });
        Assert.Equal(saved, _database.InventoryJson);
        Assert.Equal(200, store.Commit(Sell(seed), DateTimeOffset.UnixEpoch).StatusCode);
        Assert.Equal(maximum, Coins(store));
    }

    [Fact]
    public void Coins_outside_the_wallet_or_in_two_stacks_are_rejected()
    {
        InventoryStore store = Create();
        store.AddItem(ItemTechnicalNames.Coins, DateTimeOffset.UnixEpoch, 5);
        PlayerInventory state = store.Inventory;
        OwnedItem coins = Assert.Single(state.Wallet);
        string saved = _database.InventoryJson;

        Assert.Throws<InvalidDataException>(() => store.Import(state with { Wallet = [coins, coins with { InstanceId = Guid.NewGuid().ToString("D") }] }, state.Revision));
        Assert.Throws<InvalidDataException>(() => store.Import(state with { Wallet = [], Stash = state.Stash with { Items = [.. state.Stash.Items, coins] } }, state.Revision));
        Assert.Equal(saved, _database.InventoryJson);
    }

    private static long Value(IReadOnlyList<ItemDescription> descriptions, string type) =>
        descriptions.Single(item => item.GameAssetId == ItemTechnicalNames.AssetIds[type]).Scrap!.RewardPackage.RewardedItems.Single().Amount;

    private static InventoryOperationsRequest Sell(string itemId, long? amount = null) => new([new ScrapItem(itemId, amount)], "");

    private static ItemChanges Changes(ServiceReply reply) => Assert.IsType<InventoryOperationsResponse>(reply.Body).Changeset.Items;

    private static long Coins(InventoryStore store) => store.Inventory.Wallet.Where(item => item.Type == ItemTechnicalNames.Coins).Sum(item => item.Quantity);

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
