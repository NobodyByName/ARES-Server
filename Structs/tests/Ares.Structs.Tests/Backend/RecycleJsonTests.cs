using System.Text.Json;
using Ares.Structs.Backend;
using Ares.Structs.Backend.Progression;

namespace Ares.Structs.Tests.Backend;

public sealed class RecycleJsonTests
{
    [Fact]
    public void A_recyclable_item_describes_its_outputs_as_a_recycle_reward_package()
    {
        long wires = ItemTechnicalNames.AssetIds[ItemTechnicalNames.Wires];
        ItemDescription description = SalvageItems.Definitions.Single(item => item.GameAssetId == wires) with { Recycle = ItemRewardPackages.Recycle(wires) };
        using JsonDocument document = JsonDocument.Parse(JsonSerializer.Serialize(description, BackendJson.Options));

        JsonElement recycle = document.RootElement.GetProperty("recycle");
        Assert.Equal(["rewardedItems"], recycle.EnumerateObject().Select(property => property.Name));
        JsonElement reward = Assert.Single(recycle.GetProperty("rewardedItems").EnumerateArray());
        Assert.Equal(["gameAssetId", "amount", "durability", "slots", "aliasInstanceId", "maxAmount", "hideInUi"],
            reward.EnumerateObject().Select(property => property.Name));
        Assert.Equal((ItemTechnicalNames.AssetIds[ItemTechnicalNames.RubberParts], 2L, 1.0),
            (reward.GetProperty("gameAssetId").GetInt64(), reward.GetProperty("amount").GetInt64(), reward.GetProperty("durability").GetDouble()));
    }

    [Fact]
    public void An_item_that_cannot_be_recycled_has_no_recycle_member()
    {
        long metalParts = ItemTechnicalNames.AssetIds[ItemTechnicalNames.MetalParts];
        Assert.Null(ItemRewardPackages.Recycle(metalParts));
        using JsonDocument document = JsonDocument.Parse(JsonSerializer.Serialize(SalvageItems.Definitions.Single(item => item.GameAssetId == metalParts),
            BackendJson.Options));
        Assert.False(document.RootElement.TryGetProperty("recycle", out _));
    }

    [Fact]
    public void A_recycle_operation_reads_its_item_and_optional_amount()
    {
        const string Json = """
            {"requestId":"r","operations":[{"discriminator":"recycle","instanceId":"a"},{"discriminator":"recycle","instanceId":"b","amount":3}]}
            """;
        InventoryOperationsRequest request = JsonSerializer.Deserialize<InventoryOperationsRequest>(Json, BackendJson.Options)!;

        Assert.Equal([new RecycleItem("a", null), new RecycleItem("b", 3)], request.Operations!.Cast<RecycleItem>());
        Assert.Equal("""{"discriminator":"recycle","instanceId":"b","amount":3}""",
            JsonSerializer.Serialize<InventoryOperation>(new RecycleItem("b", 3), BackendJson.Options));
    }

    [Theory]
    [InlineData("""{"discriminator":"recycle","instanceId":"a","unexpected":1}""")]
    [InlineData("""{"discriminator":"recycle","itemInstanceId":"a"}""")]
    public void A_recycle_operation_of_another_shape_is_not_decoded(string json)
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<InventoryOperation>(json, BackendJson.Options));
    }
}
