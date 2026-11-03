using System.Text.Json.Serialization;

namespace Ares.Structs.Backend.Progression;

/// <summary>A batch of menu inventory operations with an idempotency key.</summary>
public sealed record InventoryOperationsRequest(
    [property: JsonPropertyName("operations")] IReadOnlyList<InventoryOperation>? Operations,
    [property: JsonPropertyName("requestId")] string? RequestId);

/// <summary>A menu inventory operation. Other operation types are not decoded here.</summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "discriminator")]
[JsonDerivedType(typeof(SetItemParents), "setParents")]
public abstract record InventoryOperation;

/// <summary>Changes the slots containing an existing item.</summary>
public sealed record SetItemParents(
    [property: JsonPropertyName("itemInstanceId")] string ItemInstanceId,
    [property: JsonPropertyName("parentInstanceIds")] IReadOnlyList<string> ParentInstanceIds) : InventoryOperation;

/// <summary>The server-authoritative changes produced by a batch of operations.</summary>
public sealed record InventoryOperationsResponse([property: JsonPropertyName("changeset")] Changeset Changeset);
