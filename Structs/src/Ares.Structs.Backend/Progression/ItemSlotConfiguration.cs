using System.Text.Json.Serialization;

namespace Ares.Structs.Backend.Progression;

/// <summary>The child-slot structure of an inventory item definition.</summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "discriminator")]
[JsonDerivedType(typeof(NoItemSlots), "noSlot")]
[JsonDerivedType(typeof(SingleItemSlot), "singleSlot")]
[JsonDerivedType(typeof(ItemSlotArray), "arraySlot")]
[JsonDerivedType(typeof(StructuralItemSlots), "structuralSlot")]
public abstract record ItemSlotConfiguration;

/// <summary>An item without child slots.</summary>
public sealed record NoItemSlots : ItemSlotConfiguration;

/// <summary>A slot accepting one of the permitted asset types, optionally requiring a default item.</summary>
public sealed record SingleItemSlot(
    [property: JsonPropertyName("allowedSlotAssetIds")] IReadOnlyList<long> AllowedSlotAssetIds,
    [property: JsonPropertyName("isRequired")] bool IsRequired,
    [property: JsonPropertyName("defaultSlotAssetId")] long DefaultSlotAssetId) : ItemSlotConfiguration;

/// <summary>A container with a fixed number of slots of one asset type.</summary>
public sealed record ItemSlotArray(
    [property: JsonPropertyName("slotAssetId")] long SlotAssetId,
    [property: JsonPropertyName("numberOfSlots")] long NumberOfSlots) : ItemSlotConfiguration;

/// <summary>An ordered collection of child slots with distinct asset types.</summary>
public sealed record StructuralItemSlots(
    [property: JsonPropertyName("slotAssetIDs")] IReadOnlyList<long> SlotAssetIds) : ItemSlotConfiguration;
