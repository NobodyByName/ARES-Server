using Ares.Common.Game.Inventory;

namespace Ares.Host.Services.Inventory;

/// <summary>What <see cref="InventoryStore.Settle"/> did with a raid result.</summary>
public enum RaidSettlement
{
    /// <summary>The carried items replaced the reserved loadout and the reservation was released.</summary>
    Saved,

    /// <summary>The inventory is not reserved for the result's ticket, because it was settled or released before. Nothing changed.</summary>
    NotReserved,
}

public sealed partial class InventoryStore
{
    /// <summary>
    /// Replaces the reserved loadout with what the player carried home, keeps the stash and releases the
    /// reservation in one save. Brought items that are unchanged and in the same place keep their version;
    /// changed, moved and new items are stamped with the new revision. Releasing the reservation in the same
    /// save makes a repeated result a no-op.
    /// </summary>
    /// <exception cref="InvalidDataException">
    /// The result is malformed, stale, belongs to another player or does not fit the inventory. Nothing is
    /// saved and the reservation is kept.
    /// </exception>
    public RaidSettlement Settle(RaidResult result, DateTimeOffset now)
    {
        RaidResults.Validate(result);
        lock (_lock)
        {
            Refresh();
            if (_state.RaidTicketId != result.TicketId)
            {
                return RaidSettlement.NotReserved;
            }

            if (result.UserId != _userId)
            {
                throw new InvalidDataException("The raid result belongs to another player");
            }

            if (result.Revision != _database.ReadReservation(_userId, result.TicketId)!.Loadout.Revision)
            {
                throw new InvalidDataException("The raid result does not match the reserved loadout revision");
            }

            HashSet<string> stashIds = _state.Stash.Items.Select(item => item.InstanceId).ToHashSet(StringComparer.Ordinal);
            Dictionary<string, (InventoryArea Area, OwnedItem Item)> brought = Enum.GetValues<InventoryArea>()
                .Where(area => area != InventoryArea.Stash)
                .SelectMany(area => InventoryCatalog.Items(_state, area).Select(item => (Area: area, Item: item)))
                .ToDictionary(entry => entry.Item.InstanceId, StringComparer.Ordinal);
            long revision = checked(_state.Revision + 1);
            string etag = ItemETag(revision);
            long updatedAt = now.ToUnixTimeMilliseconds();
            Dictionary<InventoryArea, List<OwnedItem>> placed = [];
            foreach (CarriedItem carried in result.Items)
            {
                if (stashIds.Contains(carried.Item.InstanceId))
                {
                    throw new InvalidDataException("A carried item has the id of a stash item");
                }

                InventoryArea area = CarriedArea(carried.Place);
                OwnedItem item = new(carried.Item.InstanceId, InventoryCatalog.TechnicalName(carried.Item.GameAssetId), carried.Item.Amount,
                    carried.Item.Durability, carried.Item.MaxDurability, etag, updatedAt)
                {
                    Slot = InventoryCatalog.UsesSlots(area) ? carried.Slot : null,
                };
                if (brought.TryGetValue(item.InstanceId, out (InventoryArea Area, OwnedItem Item) previous) && previous.Area == area
                    && previous.Item == item with { ETag = previous.Item.ETag, UpdatedAt = previous.Item.UpdatedAt })
                {
                    item = previous.Item;
                }

                if (!placed.TryGetValue(area, out List<OwnedItem>? items))
                {
                    placed[area] = items = [];
                }

                items.Add(item);
            }

            PlayerInventory candidate = _state with { Revision = revision, Loadout = PlayerInventory.Empty.Loadout, RaidTicketId = null };
            foreach ((InventoryArea area, List<OwnedItem> items) in placed)
            {
                candidate = InventoryCatalog.WithItems(candidate, area, items);
            }

            Save(candidate);
            return RaidSettlement.Saved;
        }
    }

    private static InventoryArea CarriedArea(string place) => place switch
    {
        CarriedPlaces.Augment => InventoryArea.Augment,
        CarriedPlaces.Shield => InventoryArea.Shield,
        CarriedPlaces.MainWeapon => InventoryArea.MainWeapon,
        CarriedPlaces.SecondaryWeapon => InventoryArea.SecondaryWeapon,
        CarriedPlaces.Backpack => InventoryArea.Backpack,
        CarriedPlaces.QuickUse => InventoryArea.QuickUse,
        CarriedPlaces.SafePockets => InventoryArea.SafePockets,
        _ => throw new InvalidDataException("Invalid carried item place"),
    };
}
