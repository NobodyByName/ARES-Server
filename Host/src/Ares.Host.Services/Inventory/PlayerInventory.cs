using System.Text.Json.Serialization;

namespace Ares.Host.Services.Inventory;

/// <summary>An owned item stack with identity, condition and an optional carried-container position.</summary>
public sealed record OwnedItem(string InstanceId, string Type, long Quantity, double Durability, double MaxDurability,
    [property: JsonPropertyName("etag")] string ETag, long UpdatedAt)
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Slot { get; init; }
}

/// <summary>Purchased stash capacity and its unordered contents.</summary>
public sealed record PlayerStash(int SlotCount, IReadOnlyList<OwnedItem> Items);

/// <summary>Occupied positions in one carried container. Capacity comes from the loadout definition.</summary>
public sealed record PlayerItemContainer(IReadOnlyList<OwnedItem> Items);

/// <summary>Equipped items and carried containers. Single equipment slots are null when empty.</summary>
public sealed record PlayerLoadoutState(OwnedItem? Augment, OwnedItem? Shield, OwnedItem? MainWeapon, OwnedItem? SecondaryWeapon,
    PlayerItemContainer Backpack, PlayerItemContainer QuickUse, PlayerItemContainer SafePockets)
{
    [JsonIgnore]
    public int BackpackCapacity => InventoryCatalog.LoadoutCapacity(this, InventoryArea.Backpack);

    [JsonIgnore]
    public int QuickUseCapacity => InventoryCatalog.LoadoutCapacity(this, InventoryArea.QuickUse);

    [JsonIgnore]
    public int SafePocketCapacity => InventoryCatalog.LoadoutCapacity(this, InventoryArea.SafePockets);
}

/// <summary>Persistent stash, equipped items and carried contents.</summary>
public sealed record PlayerInventory(long Revision, PlayerStash Stash, PlayerLoadoutState Loadout)
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? RaidTicketId { get; init; }

    public IReadOnlyList<InventoryOperationReceipt> OperationReceipts { get; init; } = [];

    [JsonIgnore]
    public int ItemCount => Enum.GetValues<InventoryArea>().Sum(area => InventoryCatalog.Items(this, area).Count);

    public static PlayerInventory Empty { get; } = new(0, new PlayerStash(0, []),
        new PlayerLoadoutState(null, null, null, null, new PlayerItemContainer([]), new PlayerItemContainer([]), new PlayerItemContainer([])));
}

internal enum InventoryArea
{
    Stash,
    Augment,
    Shield,
    MainWeapon,
    SecondaryWeapon,
    Backpack,
    QuickUse,
    SafePockets,
}

internal sealed record InventoryPosition(InventoryArea Area, int Index);
