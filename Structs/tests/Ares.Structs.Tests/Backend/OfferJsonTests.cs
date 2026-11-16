using System.Text.Json;
using Ares.Structs.Backend;
using Ares.Structs.Backend.Progression;

namespace Ares.Structs.Tests.Backend;

public sealed class OfferJsonTests
{
    [Fact]
    public void An_offer_writes_every_member_the_trade_screen_reads()
    {
        OfferDescription offer = TraderOffers.Celeste[0];
        using JsonDocument document = JsonDocument.Parse(JsonSerializer.Serialize(offer, BackendJson.Options));
        JsonElement root = document.RootElement;

        Assert.Equal(
            ["offerId", "offerType", "title", "localizedDescription", "ownerGameAssetId", "durationInSeconds", "isVisible", "requiredItems",
                "costPackage", "cost", "rewardPackage", "orderIndex", "startDate", "endDate", "limited"],
            root.EnumerateObject().Select(property => property.Name));
        Assert.Equal(TraderOffers.CelesteAssetId, root.GetProperty("ownerGameAssetId").GetInt64());
        Assert.Equal(OfferDescription.NoDate, root.GetProperty("endDate").GetInt64());
        JsonElement cost = root.GetProperty("cost");
        Assert.Equal(["discriminant", "itemAmounts"], cost.EnumerateObject().Select(property => property.Name));
        Assert.Equal("itemAmounts", cost.GetProperty("discriminant").GetString());
        Assert.Equal(["gameAssetId", "amount"], cost.GetProperty("itemAmounts")[0].EnumerateObject().Select(property => property.Name));
        Assert.Equal(["gameAssetId", "amount", "durability"],
            root.GetProperty("costPackage").GetProperty("costItems")[0].EnumerateObject().Select(property => property.Name));
        Assert.Equal(["gameAssetId", "amount", "durability", "slots", "aliasInstanceId", "maxAmount", "hideInUi"],
            root.GetProperty("rewardPackage").GetProperty("rewardedItems")[0].EnumerateObject().Select(property => property.Name));
        Assert.Equal(["refreshInterval", "maxAmount"], root.GetProperty("limited").EnumerateObject().Select(property => property.Name));
        Assert.Equal(offer, JsonSerializer.Deserialize<OfferDescription>(root.GetRawText(), BackendJson.Options)! with
        {
            RequiredItems = offer.RequiredItems,
            CostPackage = offer.CostPackage,
            Cost = offer.Cost,
            RewardPackage = offer.RewardPackage,
        });
    }

    [Fact]
    public void Celestes_permanent_offers_cost_seeds_and_never_expire()
    {
        Assert.Equal(7, TraderOffers.Celeste.Count);
        Assert.All(TraderOffers.Celeste, offer =>
        {
            Assert.Equal(TraderOffers.CelesteAssetId, offer.OwnerGameAssetId);
            Assert.Equal((OfferDescription.NoDate, OfferDescription.NoDate, 0L), (offer.StartDate, offer.EndDate, offer.Limited.MaxAmount));
            Assert.Empty(offer.RequiredItems);
            ItemAssetAmount price = Assert.Single(Assert.IsType<ItemAmountsOfferCost>(offer.Cost).ItemAmounts);
            Assert.Equal(SalvageItems.ItemAssetIds[ItemTechnicalNames.AssortedSeeds], price.GameAssetId);
        });
        Assert.Equal(TraderOffers.Celeste.OrderBy(offer => offer.OrderIndex), TraderOffers.Celeste);
    }

    [Fact]
    public void An_accept_offer_operation_reads_its_offer_times_and_payment()
    {
        long offerId = TraderOffers.Celeste[0].OfferId;
        string json = $$"""
            {"requestId":"r","operations":[{"discriminator":"acceptOffer","offerId":{{offerId}},"times":2,"payment":[],"paymentByAmounts":[]}]}
            """;
        InventoryOperationsRequest request = JsonSerializer.Deserialize<InventoryOperationsRequest>(json, BackendJson.Options)!;

        AcceptOffer purchase = Assert.IsType<AcceptOffer>(Assert.Single(request.Operations!));
        Assert.Equal((offerId, 2L), (purchase.OfferId, purchase.Times));
        Assert.Empty(purchase.Payment!);
        Assert.Empty(purchase.PaymentByAmounts!);
        Assert.StartsWith("""{"discriminator":"acceptOffer",""", JsonSerializer.Serialize<InventoryOperation>(purchase, BackendJson.Options), StringComparison.Ordinal);
    }
}
