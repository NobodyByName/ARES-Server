using System.Text.Json;
using Ares.Structs.Backend;
using Ares.Structs.Backend.Progression;

namespace Ares.Structs.Tests.Backend;

public sealed class OfferJsonTests
{
    private const long TraderAssetId = 1_000_001;
    private const long PriceAssetId = 1_000_002;
    private const long RewardAssetId = 1_000_003;

    private static readonly OfferDescription s_offer = new(42, "Permanent", "Offer", "", TraderAssetId, 0, true, [],
        new OfferCostPackage([new OfferCostItem(PriceAssetId, 3, 1.0)]), new ItemAmountsOfferCost([new ItemAssetAmount(PriceAssetId, 3)]),
        new OfferRewardPackage([new OfferRewardedItem(RewardAssetId, 1, 1.0, [], "", 0, HideInUi: false)]), 0, OfferDescription.NoDate,
        OfferDescription.NoDate, new OfferLimit(0, 0));

    [Fact]
    public void An_offer_writes_every_member_the_trade_screen_reads()
    {
        OfferDescription offer = s_offer;
        using JsonDocument document = JsonDocument.Parse(JsonSerializer.Serialize(offer, BackendJson.Options));
        JsonElement root = document.RootElement;

        Assert.Equal(
            ["offerId", "offerType", "title", "localizedDescription", "ownerGameAssetId", "durationInSeconds", "isVisible", "requiredItems",
                "costPackage", "cost", "rewardPackage", "orderIndex", "startDate", "endDate", "limited"],
            root.EnumerateObject().Select(property => property.Name));
        Assert.Equal(TraderAssetId, root.GetProperty("ownerGameAssetId").GetInt64());
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
    public void An_accept_offer_operation_reads_its_offer_times_and_payment()
    {
        long offerId = s_offer.OfferId;
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
