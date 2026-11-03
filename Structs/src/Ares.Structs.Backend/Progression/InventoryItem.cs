using System.Text.Json.Serialization;

namespace Ares.Structs.Backend.Progression;

/// <summary>An inventory item, including its ordered child-slot references.</summary>
public sealed record InventoryItem(
    [property: JsonPropertyName("instanceId")] string InstanceId,
    [property: JsonPropertyName("gameAssetId")] long GameAssetId,
    [property: JsonPropertyName("amount")] long Amount,
    [property: JsonPropertyName("slots")] IReadOnlyList<string> Slots,
    [property: JsonPropertyName("durability")] double Durability,
    [property: JsonPropertyName("maxDurability")] double MaxDurability,
    [property: JsonPropertyName("lockedInRound")] string LockedInRound,
    [property: JsonPropertyName("etag")] string ETag,
    [property: JsonPropertyName("updatedAt")] long UpdatedAt);
