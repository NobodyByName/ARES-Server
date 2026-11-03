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

    /// <summary>Moves existing leaf items between compatible slots and saves the complete batch atomically.</summary>
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
            InventoryOperationReceipt? receipt = _state.OperationReceipts.FirstOrDefault(entry => entry.RequestId == request.RequestId);
            if (receipt is not null)
            {
                // A replay returns current state, so it cannot roll the menu back after a later move.
                return receipt.Sha256 == digest
                    ? Reply(_state.Snapshot.Items)
                    : ServiceReply.Error(409, "inventory_request_id_conflict");
            }

            if (_state.Reservation is not null)
            {
                return ServiceReply.Error(409, "inventory_reserved_for_raid");
            }

            Dictionary<string, InventoryItem> items = _state.Snapshot.Items.ToDictionary(item => item.InstanceId, StringComparer.Ordinal);
            Dictionary<long, ItemDescription> definitions = _state.Definitions.ToDictionary(item => item.GameAssetId);
            HashSet<string> changed = new(StringComparer.Ordinal);
            long revision = checked(_state.Snapshot.Revision + 1);
            foreach (SetItemParents operation in operations.Cast<SetItemParents>())
            {
                if (!items.TryGetValue(operation.ItemInstanceId, out InventoryItem? item)
                    || !items.TryGetValue(operation.ParentInstanceIds[0], out InventoryItem? target)
                    || definitions.GetValueOrDefault(item.GameAssetId)?.SlotConfig is not NoItemSlots
                    || definitions.GetValueOrDefault(target.GameAssetId)?.SlotConfig is not SingleItemSlot { IsRequired: false } slot
                    || !slot.AllowedSlotAssetIds.Contains(item.GameAssetId))
                {
                    return ServiceReply.Error(400, "invalid_inventory_move");
                }

                InventoryItem[] parents = [.. items.Values.Where(parent => parent.Slots.Contains(item.InstanceId))];
                if (parents.Length != 1 || definitions.GetValueOrDefault(parents[0].GameAssetId)?.SlotConfig is not SingleItemSlot { IsRequired: false })
                {
                    return ServiceReply.Error(400, "invalid_inventory_move");
                }

                InventoryItem source = parents[0];
                if (source.InstanceId == target.InstanceId)
                {
                    continue;
                }

                Update(source with { Slots = [.. source.Slots.Where(id => id != item.InstanceId)] });
                Update(target with { Slots = [.. target.Slots, item.InstanceId] });
                Update(item);
            }

            InventoryDocument candidate = _state with
            {
                Snapshot = _state.Snapshot with
                {
                    Revision = changed.Count == 0 ? _state.Snapshot.Revision : revision,
                    Items = [.. _state.Snapshot.Items.Select(item => items[item.InstanceId])],
                },
                OperationReceipts = string.IsNullOrEmpty(request.RequestId)
                    ? _state.OperationReceipts
                    : [.. _state.OperationReceipts.TakeLast(MaximumReceipts - 1), new InventoryOperationReceipt(request.RequestId, digest)],
            };
            try
            {
                candidate = Validate(candidate);
            }
            catch (InvalidDataException)
            {
                return ServiceReply.Error(400, "invalid_inventory_move");
            }

            Save(candidate);
            return Reply([.. _state.Snapshot.Items.Where(item => changed.Contains(item.InstanceId))]);

            void Update(InventoryItem updated)
            {
                changed.Add(updated.InstanceId);
                items[updated.InstanceId] = updated with
                {
                    ETag = "ares-host-inventory-" + revision.ToString(CultureInfo.InvariantCulture),
                    UpdatedAt = now.ToUnixTimeMilliseconds(),
                };
            }
        }
    }

    private static bool ValidRequestId(string? id) => id is { Length: > 0 and <= 128 } && !id.Any(character => char.IsControl(character) || char.IsWhiteSpace(character));

    private static ServiceReply Reply(IReadOnlyList<InventoryItem> updated) => ServiceReply.Ok(
        new InventoryOperationsResponse(Changeset.Empty with { Items = new ItemChanges([], updated, []) }));
}
