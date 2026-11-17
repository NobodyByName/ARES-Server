using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using Ares.Structs.Backend;
using Ares.Structs.Backend.Progression;

namespace Ares.Host.Services.Inventory;

public sealed partial class InventoryStore
{
    private const int MaximumReceipts = 32;

    public ServiceReply Commit(InventoryOperationsRequest request, DateTimeOffset now)
    {
        if (!_rules.TryValidate(request, out IReadOnlyList<InventoryOperation>? operations, out string invalid))
        {
            return ServiceReply.Error(400, invalid);
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

            PlayerInventory candidate;
            long revision = checked(_state.Revision + 1);
            bool changed;
            Dictionary<string, InventoryItem> before = _projection.Snapshot.Items.ToDictionary(item => item.InstanceId, StringComparer.Ordinal);
            SetItemParents[] parentMoves = [.. operations.OfType<SetItemParents>()];
            HashSet<string> movedIds = [.. parentMoves.Select(operation => operation.ItemInstanceId)];
            HashSet<string> predictedParents = [.. parentMoves.SelectMany(operation => operation.ParentInstanceIds)];
            try
            {
                string etag = ItemETag(revision);
                long updatedAt = now.ToUnixTimeMilliseconds();
                if (_rules.Apply(_state, operations, before, etag, updatedAt, out string rejection) is not { } applied)
                {
                    return ServiceReply.Error(400, rejection);
                }

                string augmentSlot = _projection.Snapshot.Items.Single(item => item.GameAssetId == Catalog.AugmentContentSlotAssetId).InstanceId;
                if (ParentMoveLayout.Apply(Catalog, _userId, applied, parentMoves, augmentSlot, etag, updatedAt, out bool moved) is not { } placed)
                {
                    return ServiceReply.Error(400, "invalid_inventory_move");
                }

                candidate = placed;
                changed = applied.Changed || moved;
            }
            catch (Exception error) when (error is InvalidDataException or InvalidOperationException or OverflowException)
            {
                return ServiceReply.Error(400, "invalid_inventory_move");
            }

            candidate = candidate with { Revision = changed ? revision : _state.Revision };
            IReadOnlyList<string> deleted;
            try
            {
                HashSet<string> remaining = InventoryProjection.Create(Catalog, _userId, candidate).Snapshot.Items.Select(item => item.InstanceId).ToHashSet(StringComparer.Ordinal);
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

    /// <summary>The operation batch that performs a direct item upgrade call.</summary>
    public static InventoryOperationsRequest UpgradeOperations(UpgradeInventoryItemRequest request) =>
        new([new UpgradeItem(request.InstanceId ?? "",
            new UpgradePayment([.. (request.ItemsToConsume ?? []).Select(item => new ItemAmountToConsume(item.InstanceId, item.Amount))]),
            request.TargetItemGameAssetId)], request.RequestId);

    /// <summary>Turns the reply to <see cref="UpgradeOperations"/> into the direct upgrade call's reply.</summary>
    public static ServiceReply UpgradeReply(ServiceReply reply, string instanceId) =>
        reply.Body is InventoryOperationsResponse { Changeset: var changes }
        && changes.Items.Updated.FirstOrDefault(item => item.InstanceId == instanceId) is { } upgraded
            ? ServiceReply.Ok(new UpgradeInventoryItemResponse(upgraded, changes))
            : reply;

    /// <summary>Version stamped on items an inventory change creates or modifies.</summary>
    private static string ItemETag(long revision) => "ares-host-inventory-" + revision.ToString(CultureInfo.InvariantCulture);

    private static ServiceReply Reply(IReadOnlyList<InventoryItem> updated, IReadOnlyList<string> deleted) =>
        ServiceReply.Ok(new InventoryOperationsResponse(Changeset.Empty with { Items = new ItemChanges([], updated, deleted) }));
}
