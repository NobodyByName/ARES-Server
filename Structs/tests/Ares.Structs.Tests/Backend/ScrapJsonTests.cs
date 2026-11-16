using System.Text.Json;
using Ares.Structs.Backend;
using Ares.Structs.Backend.Progression;

namespace Ares.Structs.Tests.Backend;

public sealed class ScrapJsonTests
{
    [Fact]
    public void A_sellable_item_describes_its_coin_value_as_a_scrap_reward_package()
    {
        long metalParts = ItemTechnicalNames.AssetIds[ItemTechnicalNames.MetalParts];
        ItemDescription description = SalvageItems.Definitions.Single(item => item.GameAssetId == metalParts) with { Scrap = ItemRewardPackages.Scrap(metalParts) };
        using JsonDocument document = JsonDocument.Parse(JsonSerializer.Serialize(description, BackendJson.Options));

        JsonElement scrap = document.RootElement.GetProperty("scrap");
        Assert.Equal(["rewardPackage"], scrap.EnumerateObject().Select(property => property.Name));
        JsonElement reward = Assert.Single(scrap.GetProperty("rewardPackage").GetProperty("rewardedItems").EnumerateArray());
        Assert.Equal(["gameAssetId", "amount", "durability", "slots", "aliasInstanceId", "maxAmount", "hideInUi"],
            reward.EnumerateObject().Select(property => property.Name));
        Assert.Equal((CurrencyItems.CoinsAssetId, 75L, 1.0), (reward.GetProperty("gameAssetId").GetInt64(), reward.GetProperty("amount").GetInt64(),
            reward.GetProperty("durability").GetDouble()));
    }

    [Fact]
    public void An_item_without_a_value_has_no_scrap_member()
    {
        Assert.Equal(0, ItemValues.ScrapValues[CurrencyItems.CoinsAssetId]);
        Assert.Null(ItemRewardPackages.Scrap(CurrencyItems.CoinsAssetId));
        Assert.Null(ItemRewardPackages.Scrap(StorageItems.StashSlotAssetId));
        using JsonDocument document = JsonDocument.Parse(JsonSerializer.Serialize(CurrencyItems.Definitions.Single(), BackendJson.Options));
        Assert.False(document.RootElement.TryGetProperty("scrap", out _));
    }

    [Theory]
    [InlineData(75, 1.0, 75)]
    [InlineData(75, 0.5, 37)]
    [InlineData(75, 0.0, 7)]
    [InlineData(12, 0.05, 1)]
    [InlineData(475, 0.333, 158)]
    public void The_sell_price_scales_with_durability_down_to_a_tenth(long value, double durability, long expected)
    {
        Assert.Equal(expected, ItemRewardPackages.SaleUnitPrice(value, durability));
    }

    [Fact]
    public void A_scrap_operation_reads_its_item_and_optional_amount()
    {
        const string Json = """
            {"requestId":"r","operations":[{"discriminator":"scrap","instanceId":"a"},{"discriminator":"scrap","instanceId":"b","amount":3}]}
            """;
        InventoryOperationsRequest request = JsonSerializer.Deserialize<InventoryOperationsRequest>(Json, BackendJson.Options)!;

        Assert.Equal([new ScrapItem("a", null), new ScrapItem("b", 3)], request.Operations!.Cast<ScrapItem>());
        Assert.Equal("""{"discriminator":"scrap","instanceId":"a"}""", JsonSerializer.Serialize<InventoryOperation>(new ScrapItem("a", null), BackendJson.Options));
    }

    [Theory]
    [InlineData("""{"discriminator":"scrap","instanceId":"a","unexpected":1}""")]
    [InlineData("""{"discriminator":"scrap","itemInstanceId":"a"}""")]
    public void A_scrap_operation_of_another_shape_is_not_decoded(string json)
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<InventoryOperation>(json, BackendJson.Options));
    }
}
