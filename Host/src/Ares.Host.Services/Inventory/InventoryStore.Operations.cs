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
                return receipt.Sha256 == digest ? Reply(_projection.Snapshot.Items) : ServiceReply.Error(409, "inventory_request_id_conflict");
            }

            if (_state.RaidTicketId is not null)
            {
                return ServiceReply.Error(409, "inventory_reserved_for_raid");
            }

            PlayerInventory candidate = _state;
            long revision = checked(_state.Revision + 1);
            bool changed = false;
            Dictionary<string, InventoryItem> before = _projection.Snapshot.Items.ToDictionary(item => item.InstanceId, StringComparer.Ordinal);
            foreach (SetItemParents operation in operations.Cast<SetItemParents>())
            {
                try
                {
                    InventoryProjection projection = InventoryProjection.Create(candidate);
                    InventoryPosition target = projection.Position(operation.ParentInstanceIds[0]);
                    InventoryArea source = Enum.GetValues<InventoryArea>().Single(area => InventoryCatalog.Items(candidate, area)
                        .Any(item => item.InstanceId == operation.ItemInstanceId));
                    OwnedItem item = InventoryCatalog.Items(candidate, source).Single(item => item.InstanceId == operation.ItemInstanceId);
                    if (!projection.Allows(operation.ParentInstanceIds[0], item.Type))
                    {
                        return ServiceReply.Error(400, "invalid_inventory_move");
                    }

                    if (source == target.Area && (!InventoryCatalog.UsesSlots(source) || item.Slot == target.Index))
                    {
                        continue;
                    }

                    IReadOnlyList<OwnedItem> destination = InventoryCatalog.Items(candidate, target.Area);
                    if (target.Area == InventoryArea.Stash ? destination.Count >= candidate.Stash.SlotCount
                        : InventoryCatalog.UsesSlots(target.Area) ? destination.Any(occupied => occupied.Slot == target.Index) : destination.Count > 0)
                    {
                        return ServiceReply.Error(400, "invalid_inventory_move");
                    }

                    candidate = InventoryCatalog.WithItems(candidate, source,
                        [.. InventoryCatalog.Items(candidate, source).Where(owned => owned.InstanceId != item.InstanceId)]);
                    OwnedItem moved = item with
                    {
                        Slot = InventoryCatalog.UsesSlots(target.Area) ? target.Index : null,
                        ETag = "ares-host-inventory-" + revision.ToString(CultureInfo.InvariantCulture),
                        UpdatedAt = now.ToUnixTimeMilliseconds(),
                    };
                    candidate = InventoryCatalog.WithItems(candidate, target.Area, [.. InventoryCatalog.Items(candidate, target.Area), moved]);
                    changed = true;
                }
                catch (Exception error) when (error is InvalidDataException or InvalidOperationException)
                {
                    return ServiceReply.Error(400, "invalid_inventory_move");
                }
            }

            candidate = candidate with
            {
                Revision = changed ? revision : _state.Revision,
                OperationReceipts = string.IsNullOrEmpty(request.RequestId) ? _state.OperationReceipts
                    : [.. _state.OperationReceipts.TakeLast(MaximumReceipts - 1), new InventoryOperationReceipt(request.RequestId, digest)],
            };
            Save(candidate);
            return Reply([.. _projection.Snapshot.Items.Where(item => !before.TryGetValue(item.InstanceId, out InventoryItem? previous)
                || item with { Slots = previous.Slots } != previous || !item.Slots.SequenceEqual(previous.Slots))]);
        }
    }

    private static bool ValidRequestId(string? id) => id is { Length: > 0 and <= 128 }
        && !id.Any(character => char.IsControl(character) || char.IsWhiteSpace(character));

    private static ServiceReply Reply(IReadOnlyList<InventoryItem> updated) => ServiceReply.Ok(new InventoryOperationsResponse(Changeset.Empty with { Items = new ItemChanges([], updated, []) }));
}
