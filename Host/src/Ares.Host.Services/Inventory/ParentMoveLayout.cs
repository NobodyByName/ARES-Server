using Ares.Structs.Backend.Progression;

namespace Ares.Host.Services.Inventory;

/// <summary>
/// Places a batch's parent moves on the inventory its quantity operations left. Every moving item is lifted out
/// first, so swaps and capacity reductions are checked against the final layout rather than an intermediate one.
/// </summary>
internal static class ParentMoveLayout
{
    /// <summary>
    /// Moves each item named by <paramref name="parentMoves"/> to its first requested parent, skipping items the batch
    /// consumed; the last move of an item wins. <paramref name="augmentSlotId"/> is the augment slot's structural id, whose
    /// augment decides the carried containers the other moves see. Items whose position changes are stamped with
    /// <paramref name="etag"/> and <paramref name="updatedAt"/>, and <paramref name="moved"/> says whether any did. Returns
    /// null when a move names an item the player does not own, leaves a split stack unplaced, or targets a slot that is
    /// occupied, full, inactive or does not accept the item; throws <see cref="InvalidDataException"/> for an unknown
    /// destination.
    /// </summary>
    internal static PlayerInventory? Apply(long userId, AppliedOperations applied, IEnumerable<SetItemParents> parentMoves, string augmentSlotId,
        string etag, long updatedAt, out bool moved)
    {
        moved = false;
        PlayerInventory candidate = applied.Inventory;
        Dictionary<string, (InventoryArea Area, OwnedItem Item)> owned = Enum.GetValues<InventoryArea>()
            .SelectMany(area => InventoryCatalog.Items(candidate, area).Select(item => (Area: area, Item: item)))
            .ToDictionary(entry => entry.Item.InstanceId, StringComparer.Ordinal);
        Dictionary<string, SetItemParents> moves = new(StringComparer.Ordinal);
        foreach (SetItemParents operation in parentMoves.Where(operation => !applied.Consumed.Contains(operation.ItemInstanceId)))
        {
            if (!owned.ContainsKey(operation.ItemInstanceId))
            {
                return null;
            }

            moves[operation.ItemInstanceId] = operation;
        }

        if (!applied.Created.All(moves.ContainsKey))
        {
            return null;
        }

        candidate = Enum.GetValues<InventoryArea>().Aggregate(candidate, (state, area) => InventoryCatalog.WithItems(state, area,
            [.. InventoryCatalog.Items(state, area).Where(item => !moves.ContainsKey(item.InstanceId))]));

        OwnedItem? finalAugment = candidate.Loadout.Augment;
        foreach (SetItemParents move in moves.Values.Where(move => move.ParentInstanceIds[0] == augmentSlotId))
        {
            if (finalAugment is not null)
            {
                return null;
            }

            finalAugment = owned[move.ItemInstanceId].Item with { Slot = null };
        }

        InventoryProjection projection = InventoryProjection.ForOperations(userId, candidate with { Loadout = candidate.Loadout with { Augment = finalAugment } });
        foreach (SetItemParents move in moves.Values)
        {
            InventoryPosition target = projection.Position(move.ParentInstanceIds[0]);
            (InventoryArea source, OwnedItem item) = owned[move.ItemInstanceId];
            if (target.FrameAssetId is { } targetFrame && targetFrame != InventoryCatalog.FrameAssetId(projection.Inventory.Loadout)
                || !projection.Allows(move.ParentInstanceIds[0], item.Type))
            {
                return null;
            }

            IReadOnlyList<OwnedItem> destination = InventoryCatalog.Items(candidate, target.Area);
            if (target.Area == InventoryArea.Stash ? destination.Count >= candidate.Stash.SlotCount
                : InventoryCatalog.UsesSlots(target.Area) ? destination.Any(occupied => occupied.Slot == target.Index) : destination.Count > 0)
            {
                return null;
            }

            bool changed = source != target.Area || InventoryCatalog.UsesSlots(source) && item.Slot != target.Index;
            OwnedItem placed = changed ? item with
            {
                Slot = InventoryCatalog.UsesSlots(target.Area) ? target.Index : null,
                ETag = etag,
                UpdatedAt = updatedAt,
            } : item;
            candidate = InventoryCatalog.WithItems(candidate, target.Area, [.. destination, placed]);
            moved |= changed;
        }

        return candidate;
    }
}
