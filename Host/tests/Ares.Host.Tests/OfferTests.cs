using Ares.Host.Services;
using Ares.Host.Services.Inventory;
using Ares.Structs.Backend;
using Ares.Structs.Backend.Progression;

namespace Ares.Host.Tests;

public sealed class OfferTests : IDisposable
{
    private readonly TestDatabase _database = new();

    [Fact]
    public void Every_permanent_offer_costs_and_gives_supported_items()
    {
        Assert.All(TraderOffers.Celeste.SelectMany(offer => Assert.IsType<ItemAmountsOfferCost>(offer.Cost).ItemAmounts.Select(price => price.GameAssetId)
                .Concat(offer.RewardPackage.RewardedItems.Select(reward => reward.GameAssetId))),
            asset => Assert.Contains(asset, ItemTechnicalNames.AssetIds.Values));
        Assert.Equal(TraderOffers.Celeste, new InventoryStore(_database.Store, TestDatabase.UserId).GameData().OfferDescriptions.OfferDescriptions);
    }

    [Fact]
    public void Buying_takes_the_price_from_smaller_seed_stacks_first_and_adds_the_reward()
    {
        InventoryStore store = Create();
        OfferDescription offer = Offer(ItemTechnicalNames.SheetMetal);
        Assert.Equal(3, Price(offer));
        string small = Add(store, ItemTechnicalNames.AssortedSeeds, 2);
        string large = Add(store, ItemTechnicalNames.AssortedSeeds, 5);

        ServiceReply reply = store.Commit(Buy(offer, 1), DateTimeOffset.UnixEpoch);

        Assert.Equal(200, reply.StatusCode);
        ItemChanges changes = Changes(reply);
        Assert.Equal([small], changes.Deleted);
        Assert.Contains(changes.Updated, item => item.InstanceId == large && item.Amount == 4);
        InventoryItem bought = Assert.Single(changes.Created, item => item.GameAssetId == ItemTechnicalNames.AssetIds[ItemTechnicalNames.SheetMetal]);
        Assert.Equal(1, bought.Amount);
        Assert.Equal(4, Owned(store, ItemTechnicalNames.AssortedSeeds));
        Assert.Equal(1, Owned(store, ItemTechnicalNames.SheetMetal));
    }

    [Fact]
    public void Buying_several_fills_the_owned_stack_then_starts_a_new_one()
    {
        InventoryStore store = Create();
        OfferDescription offer = Offer(ItemTechnicalNames.MetalParts);
        long max = MaxAmount(store, ItemTechnicalNames.MetalParts);
        string existing = Add(store, ItemTechnicalNames.MetalParts, max - 1);
        Add(store, ItemTechnicalNames.AssortedSeeds, 3 * Price(offer));

        ServiceReply reply = store.Commit(Buy(offer, 3), DateTimeOffset.UnixEpoch);

        Assert.Equal(200, reply.StatusCode);
        ItemChanges changes = Changes(reply);
        Assert.Contains(changes.Updated, item => item.InstanceId == existing && item.Amount == max);
        Assert.Equal(2, Assert.Single(changes.Created, item => item.GameAssetId == ItemTechnicalNames.AssetIds[ItemTechnicalNames.MetalParts]).Amount);
        Assert.Equal(0, Owned(store, ItemTechnicalNames.AssortedSeeds));
        Assert.Equal(max + 2, Owned(store, ItemTechnicalNames.MetalParts));
    }

    [Fact]
    public void Buying_takes_the_price_from_the_stacks_the_client_names()
    {
        InventoryStore store = Create();
        OfferDescription offer = Offer(ItemTechnicalNames.Planks);
        string small = Add(store, ItemTechnicalNames.AssortedSeeds, 2);
        string large = Add(store, ItemTechnicalNames.AssortedSeeds, 10);

        Assert.Equal(200, store.Commit(Buy(offer, 2, new ItemAmountToConsume(large, 2 * Price(offer))), DateTimeOffset.UnixEpoch).StatusCode);

        Assert.Equal(2, Amount(store, small));
        Assert.Equal(10 - (2 * Price(offer)), Amount(store, large));
        Assert.Equal(2, Owned(store, ItemTechnicalNames.Planks));
    }

    [Fact]
    public void Rejected_purchases_change_nothing()
    {
        InventoryStore store = Create();
        OfferDescription offer = Offer(ItemTechnicalNames.SheetMetal);
        string seeds = Add(store, ItemTechnicalNames.AssortedSeeds, Price(offer) - 1);
        string duck = store.Inventory.Stash.Items.Single(item => item.Type == ItemTechnicalNames.RubberDuck).InstanceId;
        PlayerInventory before = store.Inventory;

        (InventoryOperationsRequest Request, string Error)[] rejected =
        [
            (Buy(offer, 1), "offer_cost_not_met"),
            (Buy(offer with { OfferId = -1 }, 1), "unknown_offer"),
            (Buy(offer, 0), "invalid_inventory_operations"),
            (Buy(offer, 1, new ItemAmountToConsume(seeds, Price(offer))), "offer_cost_not_met"),
            (Buy(offer, 1, new ItemAmountToConsume(duck, 1)), "offer_cost_not_met"),
            (Buy(offer, long.MaxValue), "invalid_inventory_move"),
        ];

        Assert.All(rejected, entry =>
        {
            ServiceReply reply = store.Commit(entry.Request, DateTimeOffset.UnixEpoch);
            Assert.Equal((400, entry.Error), (reply.StatusCode, Assert.IsType<ErrorResponse>(reply.Body).Error));
        });
        Assert.Equal(before.Revision, store.Inventory.Revision);
        Assert.Equal(before.Stash.Items, store.Inventory.Stash.Items);
    }

    [Fact]
    public void A_reward_needing_a_new_stack_in_a_full_stash_is_rejected()
    {
        InventoryStore store = Create();
        OfferDescription offer = Offer(ItemTechnicalNames.Fabric);
        Add(store, ItemTechnicalNames.AssortedSeeds, Price(offer) + 1);
        store.Import(TestStash.Full(store.Inventory), store.Inventory.Revision);
        long revision = store.Inventory.Revision;

        ServiceReply reply = store.Commit(Buy(offer, 1), DateTimeOffset.UnixEpoch);

        Assert.Equal((400, "stash_full"), (reply.StatusCode, Assert.IsType<ErrorResponse>(reply.Body).Error));
        Assert.Equal(revision, store.Inventory.Revision);
    }

    private static OfferDescription Offer(string reward) =>
        TraderOffers.Celeste.Single(offer => offer.RewardPackage.RewardedItems.Single().GameAssetId == ItemTechnicalNames.AssetIds[reward]);

    private static long Price(OfferDescription offer) => Assert.IsType<ItemAmountsOfferCost>(offer.Cost).ItemAmounts.Single().Amount;

    private static InventoryOperationsRequest Buy(OfferDescription offer, long amount, params ItemAmountToConsume[] stacks) =>
        new([new AcceptOffer(offer.OfferId, amount, stacks, [])], "");

    private static ItemChanges Changes(ServiceReply reply) => Assert.IsType<InventoryOperationsResponse>(reply.Body).Changeset.Items;

    private static long Amount(InventoryStore store, string itemId) => store.Snapshot.Items.Single(item => item.InstanceId == itemId).Amount;

    private static long Owned(InventoryStore store, string type) => store.Inventory.Stash.Items.Where(item => item.Type == type).Sum(item => item.Quantity);

    private static long MaxAmount(InventoryStore store, string type) =>
        store.GameData().ItemDescriptions.ItemDescriptions.Single(item => item.GameAssetId == ItemTechnicalNames.AssetIds[type]).MaxAmount;

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
