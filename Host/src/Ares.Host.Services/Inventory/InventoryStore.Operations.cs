using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using Ares.Structs.Backend;
using Ares.Structs.Backend.Progression;

namespace Ares.Host.Services.Inventory;

public sealed partial class InventoryStore
{
    private const int MaximumReceipts = 32;
    private const int MaximumOperations = 64;

    public ServiceReply Commit(InventoryOperationsRequest request, DateTimeOffset now)
    {
        if ((!string.IsNullOrEmpty(request.RequestId) && !ValidRequestId(request.RequestId))
            || request.Operations is not { Count: <= MaximumOperations } operations
            || operations.Any(operation => operation is not SetItemParents { ItemInstanceId.Length: > 0, ParentInstanceIds: [{ Length: > 0 }] }))
        {
            return ServiceReply.Error(400, "invalid_inventory_operations");
        }

        string digest = Convert.ToHexStringLower(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(operations, BackendJson.Options)));
        lock (_lock)
        {
            Refresh();
            InventoryOperationReceipt? receipt = _state.OperationReceipts.FirstOrDefault(entry => entry.RequestId == request.RequestId);
            if (receipt is not null)
            {
                HashSet<string> currentIds = _projection.Snapshot.Items.Select(item => item.InstanceId).ToHashSet(StringComparer.Ordinal);
                return receipt.Sha256 == digest ? Reply(_projection.Snapshot.Items, [.. receipt.DeletedItemIds.Where(id => !currentIds.Contains(id))])
                    : ServiceReply.Error(409, "inventory_request_id_conflict");
            }

            if (_state.RaidTicketId is not null)
            {
                return ServiceReply.Error(409, "inventory_reserved_for_raid");
            }

            PlayerInventory candidate = _state;
            long revision = checked(_state.Revision + 1);
            bool changed = false;
            Dictionary<string, InventoryItem> before = _projection.Snapshot.Items.ToDictionary(item => item.InstanceId, StringComparer.Ordinal);
            HashSet<string> movedIds = [.. operations.Cast<SetItemParents>().Select(operation => operation.ItemInstanceId)];
            HashSet<string> predictedParents = [.. operations.Cast<SetItemParents>().SelectMany(operation => operation.ParentInstanceIds)];
            try
            {
                Dictionary<string, (InventoryArea Area, OwnedItem Item)> owned = Enum.GetValues<InventoryArea>()
                    .SelectMany(area => InventoryCatalog.Items(_state, area).Select(item => (Area: area, Item: item)))
                    .ToDictionary(entry => entry.Item.InstanceId, StringComparer.Ordinal);
                Dictionary<string, SetItemParents> moves = new(StringComparer.Ordinal);
                foreach (SetItemParents operation in operations.Cast<SetItemParents>())
                {
                    if (!owned.ContainsKey(operation.ItemInstanceId))
                    {
                        return ServiceReply.Error(400, "invalid_inventory_move");
                    }

                    moves[operation.ItemInstanceId] = operation;
                }

                // Remove every moving item first, so a swap or a capacity reduction is validated
                // against the final batch rather than an intermediate occupied/undersized layout.
                candidate = Enum.GetValues<InventoryArea>().Aggregate(candidate, (state, area) => InventoryCatalog.WithItems(state, area,
                    [.. InventoryCatalog.Items(state, area).Where(item => !moves.ContainsKey(item.InstanceId))]));

                string augmentSlot = _projection.Snapshot.Items.Single(item => item.GameAssetId == AugmentItems.AugmentContentSlotAssetId).InstanceId;
                OwnedItem? finalAugment = candidate.Loadout.Augment;
                foreach (SetItemParents move in moves.Values.Where(move => move.ParentInstanceIds[0] == augmentSlot))
                {
                    if (finalAugment is not null)
                    {
                        return ServiceReply.Error(400, "invalid_inventory_move");
                    }

                    finalAugment = owned[move.ItemInstanceId].Item with { Slot = null };
                }

                InventoryProjection projection = InventoryProjection.ForOperations(_userId, candidate with { Loadout = candidate.Loadout with { Augment = finalAugment } });
                foreach (SetItemParents move in moves.Values)
                {
                    InventoryPosition target = projection.Position(move.ParentInstanceIds[0]);
                    (InventoryArea source, OwnedItem item) = owned[move.ItemInstanceId];
                    if (target.FrameAssetId is { } targetFrame && targetFrame != InventoryCatalog.FrameAssetId(projection.Inventory.Loadout)
                        || !projection.Allows(move.ParentInstanceIds[0], item.Type))
                    {
                        return ServiceReply.Error(400, "invalid_inventory_move");
                    }

                    IReadOnlyList<OwnedItem> destination = InventoryCatalog.Items(candidate, target.Area);
                    if (target.Area == InventoryArea.Stash ? destination.Count >= candidate.Stash.SlotCount
                        : InventoryCatalog.UsesSlots(target.Area) ? destination.Any(occupied => occupied.Slot == target.Index) : destination.Count > 0)
                    {
                        return ServiceReply.Error(400, "invalid_inventory_move");
                    }

                    bool moved = source != target.Area || InventoryCatalog.UsesSlots(source) && item.Slot != target.Index;
                    OwnedItem placed = moved ? item with
                    {
                        Slot = InventoryCatalog.UsesSlots(target.Area) ? target.Index : null,
                        ETag = "ares-host-inventory-" + revision.ToString(CultureInfo.InvariantCulture),
                        UpdatedAt = now.ToUnixTimeMilliseconds(),
                    } : item;
                    candidate = InventoryCatalog.WithItems(candidate, target.Area, [.. destination, placed]);
                    changed |= moved;
                }
            }
            catch (Exception error) when (error is InvalidDataException or InvalidOperationException)
            {
                return ServiceReply.Error(400, "invalid_inventory_move");
            }

            candidate = candidate with { Revision = changed ? revision : _state.Revision };
            IReadOnlyList<string> deleted;
            try
            {
                HashSet<string> remaining = InventoryProjection.Create(_userId, candidate).Snapshot.Items.Select(item => item.InstanceId).ToHashSet(StringComparer.Ordinal);
                deleted = [.. before.Keys.Where(id => !remaining.Contains(id))];
                candidate = candidate with
                {
                    OperationReceipts = string.IsNullOrEmpty(request.RequestId) ? _state.OperationReceipts
                        : [.. _state.OperationReceipts.TakeLast(MaximumReceipts - 1), new InventoryOperationReceipt(request.RequestId, digest) { DeletedItemIds = deleted }],
                };
                Save(candidate);
            }
            catch (InvalidDataException)
            {
                return ServiceReply.Error(400, "invalid_inventory_move");
            }

            IReadOnlyList<InventoryItem> after = _projection.Snapshot.Items;
            // The client has already predicted the requested parents. Reassert those slots and
            // the canonical parents even when storage is unchanged, so stash sorting cannot leave
            // a predicted copy behind or hide the authoritative item after a no-op/retry.
            return ServiceReply.Ok(new InventoryOperationsResponse(Changeset.Empty with
            {
                Items = new ItemChanges(
                    [.. after.Where(item => !before.ContainsKey(item.InstanceId))],
                    [.. after.Where(item => before.TryGetValue(item.InstanceId, out InventoryItem? previous)
                        && (predictedParents.Contains(item.InstanceId) || movedIds.Contains(item.InstanceId) || item.Slots.Any(movedIds.Contains)
                            || item with { Slots = previous.Slots } != previous || !item.Slots.SequenceEqual(previous.Slots)))],
                    deleted),
            }));
        }
    }

    private static bool ValidRequestId(string? id) => id is { Length: > 0 and <= 128 }
        && !id.Any(character => char.IsControl(character) || char.IsWhiteSpace(character));

    private static ServiceReply Reply(IReadOnlyList<InventoryItem> updated, IReadOnlyList<string> deleted) =>
        ServiceReply.Ok(new InventoryOperationsResponse(Changeset.Empty with { Items = new ItemChanges([], updated, deleted) }));
}
