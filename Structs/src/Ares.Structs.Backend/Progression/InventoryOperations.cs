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
[JsonDerivedType(typeof(AcceptOffer), "acceptOffer")]
[JsonDerivedType(typeof(ScrapItem), "scrap")]
[JsonDerivedType(typeof(RecycleItem), "recycle")]
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

/// <summary>
/// Buys an offer <paramref name="Times"/> times. <paramref name="Payment"/> names the stacks that pay for it;
/// when empty, the server chooses them.
/// </summary>
public sealed record AcceptOffer(
    [property: JsonPropertyName("offerId")] long OfferId,
    [property: JsonPropertyName("times")] long Times,
    [property: JsonPropertyName("payment")] IReadOnlyList<ItemAmountToConsume>? Payment,
    [property: JsonPropertyName("paymentByAmounts")] IReadOnlyList<UnknownOfferPaymentByAmount>? PaymentByAmounts) : InventoryOperation;

/// <summary>
/// Sells an item for the coins of its <see cref="ItemDescription.Scrap"/> package: the whole item, with whatever it
/// holds, or <paramref name="Amount"/> of a stack. Members other than these are rejected, so an operation of another
/// shape is not mistaken for a sale.
/// </summary>
/// <param name="InstanceId">The item sold.</param>
/// <param name="Amount">How many of the stack are sold, or null for the whole item.</param>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record ScrapItem(
    [property: JsonPropertyName("instanceId"), JsonRequired] string InstanceId,
    [property: JsonPropertyName("amount"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] long? Amount) : InventoryOperation;

/// <summary>
/// Recycles an item into the items of its <see cref="ItemDescription.Recycle"/> package, each times the recycled amount:
/// the whole item, or <paramref name="Amount"/> of a stack. Members other than these are rejected, so an operation of
/// another shape is not mistaken for recycling.
/// </summary>
/// <param name="InstanceId">The item recycled.</param>
/// <param name="Amount">How many of the stack are recycled, or null for the whole item.</param>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record RecycleItem(
    [property: JsonPropertyName("instanceId"), JsonRequired] string InstanceId,
    [property: JsonPropertyName("amount"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] long? Amount) : InventoryOperation;

/// <summary>An entry of <see cref="AcceptOffer.PaymentByAmounts"/>; the trade screen sends this list empty.</summary>
public sealed record UnknownOfferPaymentByAmount;

/// <summary>An amount taken from one owned stack.</summary>
public sealed record ItemAmountToConsume(
    [property: JsonPropertyName("itemInstanceId")] string ItemInstanceId,
    [property: JsonPropertyName("amount")] long Amount);

/// <summary>The server-authoritative changes produced by a batch of operations.</summary>
public sealed record InventoryOperationsResponse([property: JsonPropertyName("changeset")] Changeset Changeset);
