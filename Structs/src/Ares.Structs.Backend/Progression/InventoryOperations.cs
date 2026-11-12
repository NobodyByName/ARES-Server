using System.Text.Json.Serialization;

namespace Ares.Structs.Backend.Progression;

/// <summary>A batch of menu inventory operations with an idempotency key.</summary>
public sealed record InventoryOperationsRequest(
    [property: JsonPropertyName("operations")] IReadOnlyList<InventoryOperation>? Operations,
    [property: JsonPropertyName("requestId")] string? RequestId);

/// <summary>A menu inventory operation. Other operation types are not decoded here.</summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "discriminator")]
[JsonDerivedType(typeof(SetItemParents), "setParents")]
[JsonDerivedType(typeof(MoveItemAmount), "moveAmount")]
public abstract record InventoryOperation;

/// <summary>Changes the slots containing an existing item.</summary>
public sealed record SetItemParents(
    [property: JsonPropertyName("itemInstanceId")] string ItemInstanceId,
    [property: JsonPropertyName("parentInstanceIds")] IReadOnlyList<string> ParentInstanceIds) : InventoryOperation;

/// <summary>
/// Moves part of a stack into another stack of the same item. A target id that does not exist yet names a
/// new stack (a split), which a later <see cref="SetItemParents"/> in the same batch places.
/// </summary>
public sealed record MoveItemAmount(
    [property: JsonPropertyName("fromInstanceId")] string FromInstanceId,
    [property: JsonPropertyName("toInstanceId")] string ToInstanceId,
    [property: JsonPropertyName("amount")] long Amount) : InventoryOperation;

/// <summary>The server-authoritative changes produced by a batch of operations.</summary>
public sealed record InventoryOperationsResponse([property: JsonPropertyName("changeset")] Changeset Changeset);
