using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using Ares.Host.Services.Content;
using Ares.Structs.Backend;
using Ares.Structs.Backend.Progression;

namespace Ares.Host.Services.Inventory;

public sealed partial class InventoryStore
{
    private const int MaximumReceipts = 32;
    private const int MaximumOperations = 64;

    private static readonly Dictionary<long, OfferDescription> s_offers =
        BackendContent.GameData.OfferDescriptions.OfferDescriptions.ToDictionary(offer => offer.OfferId);

    public ServiceReply Commit(InventoryOperationsRequest request, DateTimeOffset now)
    {
        if ((!string.IsNullOrEmpty(request.RequestId) && !ValidRequestId(request.RequestId))
            || request.Operations is not { Count: <= MaximumOperations } operations
            || operations.Any(operation => operation is not (SetItemParents { ItemInstanceId.Length: > 0, ParentInstanceIds: [{ Length: > 0 }] }
                or MoveItemAmount { FromInstanceId.Length: > 0, ToInstanceId.Length: > 0, Amount: > 0 }
                or AcceptOffer { Times: > 0 }
                or ScrapItem { InstanceId.Length: > 0, Amount: null or > 0 }
                or RecycleItem { InstanceId.Length: > 0, Amount: null or > 0 }
                or UpgradeItem { ItemInstanceId.Length: > 0 }))
            || operations.OfType<AcceptOffer>().Any(purchase => purchase.Payment?.Any(item => item is not { ItemInstanceId.Length: > 0, Amount: > 0 }) == true)
            || operations.OfType<UpgradeItem>().Any(upgrade => upgrade.Payment?.Items?.Any(item => item is not { ItemInstanceId.Length: > 0, Amount: > 0 }) == true))
        {
            return ServiceReply.Error(400, "invalid_inventory_operations");
        }

        if (operations.OfType<AcceptOffer>().Any(purchase => !s_offers.ContainsKey(purchase.OfferId)))
        {
            return ServiceReply.Error(400, "unknown_offer");
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
            SetItemParents[] parentMoves = [.. operations.OfType<SetItemParents>()];
            HashSet<string> movedIds = [.. parentMoves.Select(operation => operation.ItemInstanceId)];
            HashSet<string> predictedParents = [.. parentMoves.SelectMany(operation => operation.ParentInstanceIds)];
            try
            {
                // Amount moves and purchases only change quantities, create split stacks and add bought
                // stacks to the stash; the parent moves then decide the final layout, which also places
                // every newly split stack.
                HashSet<string> created = new(StringComparer.Ordinal);
                HashSet<string> consumed = new(StringComparer.Ordinal);
                string etag = ItemETag(revision);
                foreach (InventoryOperation operation in operations)
                {
                    switch (operation)
                    {
                        case MoveItemAmount transfer:
                            candidate = MoveAmount(candidate, transfer, before, created, consumed, etag, now.ToUnixTimeMilliseconds());
                            changed = true;
                            break;
                        case AcceptOffer purchase:
                            if (Accept(candidate, purchase, consumed, etag, now.ToUnixTimeMilliseconds(), out string rejection) is not { } bought)
                            {
                                return ServiceReply.Error(400, rejection);
                            }

                            candidate = bought;
                            changed = true;
                            break;
                        case ScrapItem sale:
                            if (Sell(candidate, sale, created, consumed, etag, now.ToUnixTimeMilliseconds(), out string refusal) is not { } sold)
                            {
                                return ServiceReply.Error(400, refusal);
                            }

                            candidate = sold;
                            changed = true;
                            break;
                        case RecycleItem recycling:
                            if (Recycle(candidate, recycling, created, consumed, etag, now.ToUnixTimeMilliseconds(), out string reason) is not { } recycled)
                            {
                                return ServiceReply.Error(400, reason);
                            }

                            candidate = recycled;
                            changed = true;
                            break;
                        case UpgradeItem upgrade:
                            if (Upgrade(candidate, upgrade, before, consumed, etag, now.ToUnixTimeMilliseconds(), out string denial) is not { } upgraded)
                            {
                                return ServiceReply.Error(400, denial);
                            }

                            candidate = upgraded;
                            changed = true;
                            break;
                    }
                }

                Dictionary<string, (InventoryArea Area, OwnedItem Item)> owned = Enum.GetValues<InventoryArea>()
                    .SelectMany(area => InventoryCatalog.Items(candidate, area).Select(item => (Area: area, Item: item)))
                    .ToDictionary(entry => entry.Item.InstanceId, StringComparer.Ordinal);
                Dictionary<string, SetItemParents> moves = new(StringComparer.Ordinal);
                foreach (SetItemParents operation in parentMoves.Where(operation => !consumed.Contains(operation.ItemInstanceId)))
                {
                    if (!owned.ContainsKey(operation.ItemInstanceId))
                    {
                        return ServiceReply.Error(400, "invalid_inventory_move");
                    }

                    moves[operation.ItemInstanceId] = operation;
                }

                if (!created.All(moves.ContainsKey))
                {
                    return ServiceReply.Error(400, "invalid_inventory_move");
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
                        ETag = etag,
                        UpdatedAt = now.ToUnixTimeMilliseconds(),
                    } : item;
                    candidate = InventoryCatalog.WithItems(candidate, target.Area, [.. destination, placed]);
                    changed |= moved;
                }
            }
            catch (Exception error) when (error is InvalidDataException or InvalidOperationException or OverflowException)
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

    /// <summary>
    /// Moves part of a stackable stack into an existing stack of the same item or into a new stack named
    /// by the target id. A source moved in full is removed. New stacks wait in the source's area for
    /// their parent move.
    /// </summary>
    private static PlayerInventory MoveAmount(PlayerInventory inventory, MoveItemAmount transfer, Dictionary<string, InventoryItem> before,
        HashSet<string> created, HashSet<string> consumed, string etag, long updatedAt)
    {
        (InventoryArea area, OwnedItem source) = Find(inventory, transfer.FromInstanceId) ?? throw new InvalidDataException("Unknown amount source");
        ItemDescription definition = InventoryCatalog.ByAsset[InventoryCatalog.AssetId(source.Type)];
        if (!definition.Stackable || transfer.Amount > source.Quantity || transfer.FromInstanceId == transfer.ToInstanceId)
        {
            throw new InvalidDataException("Invalid amount move");
        }

        bool emptied = transfer.Amount == source.Quantity;
        PlayerInventory result = Replace(inventory, area, source.InstanceId,
            emptied ? null : source with { Quantity = source.Quantity - transfer.Amount, ETag = etag, UpdatedAt = updatedAt });
        if (Find(result, transfer.ToInstanceId) is var (targetArea, target))
        {
            if (target.Type != source.Type || target.Quantity + transfer.Amount > definition.MaxAmount)
            {
                throw new InvalidDataException("Invalid amount move");
            }

            result = Replace(result, targetArea, target.InstanceId, target with { Quantity = target.Quantity + transfer.Amount, ETag = etag, UpdatedAt = updatedAt });
        }
        else
        {
            if (!ValidRequestId(transfer.ToInstanceId) || before.ContainsKey(transfer.ToInstanceId) || consumed.Contains(transfer.ToInstanceId))
            {
                throw new InvalidDataException("Invalid split stack id");
            }

            OwnedItem split = source with { InstanceId = transfer.ToInstanceId, Quantity = transfer.Amount, Slot = null, ETag = etag, UpdatedAt = updatedAt };
            result = InventoryCatalog.WithItems(result, area, [.. InventoryCatalog.Items(result, area), split]);
            created.Add(split.InstanceId);
        }

        if (!emptied)
        {
            return result;
        }

        created.Remove(source.InstanceId);
        consumed.Add(source.InstanceId);
        return result;
    }

    /// <summary>
    /// Buys an offer <see cref="AcceptOffer.Times"/> times. The cost comes from the named stacks, or else from
    /// the player's stacks of each cost item, stash first and smaller stacks first. The rewards go to the stash,
    /// filling stacks of the same item before starting new ones. Returns null with the reason when the player
    /// cannot pay or the stash has no room.
    /// </summary>
    private static PlayerInventory? Accept(PlayerInventory inventory, AcceptOffer purchase, HashSet<string> consumed, string etag, long updatedAt,
        out string rejection)
    {
        OfferDescription offer = s_offers[purchase.OfferId];
        rejection = "offer_cost_not_met";
        if (offer.Cost is not ItemAmountsOfferCost cost
            || Pay(inventory, cost.ItemAmounts, purchase.Times, purchase.Payment, consumed, etag, updatedAt) is not { } paid)
        {
            return null;
        }

        rejection = "stash_full";
        return Give(paid, offer.RewardPackage, purchase.Times, etag, updatedAt);
    }

    /// <summary>
    /// Upgrades the stash container into the next tier of its chain at this Host's stash upgrade price, paid like an
    /// offer. The container keeps its identity and contents. Returns null with the reason when the item cannot be
    /// upgraded or the player cannot pay.
    /// </summary>
    private PlayerInventory? Upgrade(PlayerInventory inventory, UpgradeItem upgrade, Dictionary<string, InventoryItem> before,
        HashSet<string> consumed, string etag, long updatedAt, out string rejection)
    {
        rejection = "item_not_upgradable";
        if (!before.TryGetValue(upgrade.ItemInstanceId, out InventoryItem? item) || item.GameAssetId != inventory.Stash.ContainerAssetId
            || _stashUpgrades.Upgrade(item.GameAssetId) is not { NextItemGameAssetId: not 0 } description
            || upgrade.TargetItemGameAssetId != 0 && upgrade.TargetItemGameAssetId != description.NextItemGameAssetId)
        {
            return null;
        }

        rejection = "upgrade_cost_not_met";
        return description.Cost is ItemAmountsOfferCost cost
            && Pay(inventory, cost.ItemAmounts, 1, upgrade.Payment?.Items, consumed, etag, updatedAt) is { } paid
            ? paid with { Stash = paid.Stash with { ContainerAssetId = description.NextItemGameAssetId } }
            : null;
    }

    /// <summary>
    /// Takes <paramref name="price"/> <paramref name="times"/> over from the stacks named by <paramref name="chosen"/>, or,
    /// when none are named, from the player's stacks of each cost item, stash first and smaller stacks first. Returns
    /// null when the named stacks do not match the price or the player cannot pay.
    /// </summary>
    private static PlayerInventory? Pay(PlayerInventory inventory, IReadOnlyList<ItemAssetAmount> price, long times,
        IReadOnlyList<ItemAmountToConsume>? chosen, HashSet<string> consumed, string etag, long updatedAt)
    {
        Dictionary<long, long> due = price.GroupBy(cost => cost.GameAssetId)
            .ToDictionary(group => group.Key, group => checked(group.Sum(cost => cost.Amount) * times));
        Dictionary<string, (InventoryArea Area, OwnedItem Item)> owned = Enum.GetValues<InventoryArea>()
            .SelectMany(area => InventoryCatalog.Items(inventory, area).Select(item => (Area: area, Item: item)))
            .ToDictionary(entry => entry.Item.InstanceId, StringComparer.Ordinal);
        Dictionary<string, long> taken = new(StringComparer.Ordinal);
        if (chosen is [_, ..])
        {
            if (chosen.Any(item => !taken.TryAdd(item.ItemInstanceId, item.Amount)))
            {
                return null;
            }
        }
        else
        {
            foreach ((long asset, long amount) in due)
            {
                long remaining = amount;
                foreach ((_, OwnedItem stack) in owned.Values.Where(entry => InventoryCatalog.AssetId(entry.Item.Type) == asset)
                             .OrderBy(entry => entry.Area).ThenBy(entry => entry.Item.Quantity).TakeWhile(_ => remaining > 0))
                {
                    long take = Math.Min(remaining, stack.Quantity);
                    taken[stack.InstanceId] = take;
                    remaining -= take;
                }
            }
        }

        if (taken.Any(pair => !owned.TryGetValue(pair.Key, out (InventoryArea Area, OwnedItem Item) entry) || pair.Value > entry.Item.Quantity
                || !due.ContainsKey(InventoryCatalog.AssetId(entry.Item.Type)))
            || due.Any(pair => taken.Where(entry => InventoryCatalog.AssetId(owned[entry.Key].Item.Type) == pair.Key).Sum(entry => entry.Value) != pair.Value))
        {
            return null;
        }

        foreach ((string instanceId, long amount) in taken)
        {
            (InventoryArea area, OwnedItem stack) = owned[instanceId];
            bool emptied = amount == stack.Quantity;
            inventory = Replace(inventory, area, instanceId, emptied ? null : stack with { Quantity = stack.Quantity - amount, ETag = etag, UpdatedAt = updatedAt });
            if (emptied)
            {
                consumed.Add(instanceId);
            }
        }

        return inventory;
    }

    /// <summary>
    /// Adds a reward package <paramref name="times"/> over to the stash, filling stacks of the same item before
    /// starting new ones. Returns null when the stash has no room.
    /// </summary>
    private static PlayerInventory? Give(PlayerInventory inventory, OfferRewardPackage package, long times, string etag, long updatedAt)
    {
        foreach (IGrouping<long, OfferRewardedItem> reward in package.RewardedItems.GroupBy(item => item.GameAssetId))
        {
            string type = InventoryCatalog.TechnicalName(reward.Key);
            ItemDescription definition = InventoryCatalog.ByAsset[reward.Key];
            long stackLimit = definition.Stackable ? definition.MaxAmount : 1;
            long remaining = checked(reward.Sum(item => item.Amount) * times);
            List<OwnedItem> stash = [.. inventory.Stash.Items];
            for (int index = 0; index < stash.Count && remaining > 0; index++)
            {
                OwnedItem stack = stash[index];
                if (stack.Type != type || stack.Quantity >= stackLimit)
                {
                    continue;
                }

                long added = Math.Min(remaining, stackLimit - stack.Quantity);
                stash[index] = stack with { Quantity = stack.Quantity + added, ETag = etag, UpdatedAt = updatedAt };
                remaining -= added;
            }

            while (remaining > 0)
            {
                if (stash.Count >= inventory.Stash.SlotCount)
                {
                    return null;
                }

                long added = Math.Min(remaining, stackLimit);
                stash.Add(new OwnedItem(Guid.NewGuid().ToString("D"), type, added,
                    StorageItems.InitialDurability, StorageItems.InitialMaxDurability, etag, updatedAt));
                remaining -= added;
            }

            inventory = inventory with { Stash = inventory.Stash with { Items = stash } };
        }

        return inventory;
    }

    /// <summary>
    /// Sells the whole item, or <see cref="ScrapItem.Amount"/> of its stack, for its scrap package scaled by the item's
    /// durability (<see cref="ItemRewardPackages.SaleUnitPrice"/>). Whatever the item holds goes with it. The coins join the wallet's
    /// stack of each currency, which a sale may not fill beyond its maximum. A stack split off earlier in the batch and sold
    /// whole needs no parent move. Returns null with the reason when the item cannot be sold.
    /// </summary>
    private static PlayerInventory? Sell(PlayerInventory inventory, ScrapItem sale, HashSet<string> created, HashSet<string> consumed, string etag,
        long updatedAt, out string rejection)
    {
        rejection = "item_not_sellable";
        if (Find(inventory, sale.InstanceId) is not var (area, item) || area == InventoryArea.Wallet
            || InventoryCatalog.ByAsset[InventoryCatalog.AssetId(item.Type)].Scrap is not { } scrap)
        {
            return null;
        }

        long amount = sale.Amount ?? item.Quantity;
        if (amount > item.Quantity)
        {
            rejection = "invalid_scrap_amount";
            return null;
        }

        inventory = Take(inventory, area, item, amount, created, consumed, etag, updatedAt);
        foreach (IGrouping<long, OfferRewardedItem> reward in scrap.RewardPackage.RewardedItems.GroupBy(rewarded => rewarded.GameAssetId))
        {
            string currency = InventoryCatalog.TechnicalName(reward.Key);
            long coins = checked(reward.Sum(rewarded => ItemRewardPackages.SaleUnitPrice(rewarded.Amount, item.Durability)) * amount);
            if (!InventoryCatalog.IsCurrency(currency))
            {
                throw new InvalidDataException("Unsupported scrap reward");
            }

            if (coins == 0)
            {
                continue;
            }

            OwnedItem? stack = inventory.Wallet.SingleOrDefault(owned => owned.Type == currency);
            if (coins > InventoryCatalog.ByAsset[reward.Key].MaxAmount - (stack?.Quantity ?? 0))
            {
                rejection = "currency_full";
                return null;
            }

            OwnedItem paid = stack is null
                ? new OwnedItem(Guid.NewGuid().ToString("D"), currency, coins, StorageItems.InitialDurability, StorageItems.InitialMaxDurability, etag, updatedAt)
                : stack with { Quantity = stack.Quantity + coins, ETag = etag, UpdatedAt = updatedAt };
            inventory = inventory with { Wallet = [.. inventory.Wallet.Where(owned => owned.Type != currency), paid] };
        }

        return inventory;
    }

    /// <summary>
    /// Recycles the whole item, or <see cref="RecycleItem.Amount"/> of its stack, into its recycle package times the
    /// amount. Whatever the item holds goes with it. The recycled items go to the stash like offer rewards
    /// (<see cref="Give"/>). A stack split off earlier in the batch and recycled whole needs no parent move. Returns null
    /// with the reason when the item cannot be recycled or the stash has no room.
    /// </summary>
    private static PlayerInventory? Recycle(PlayerInventory inventory, RecycleItem recycling, HashSet<string> created, HashSet<string> consumed,
        string etag, long updatedAt, out string rejection)
    {
        rejection = "item_not_recyclable";
        if (Find(inventory, recycling.InstanceId) is not var (area, item) || area == InventoryArea.Wallet
            || InventoryCatalog.ByAsset[InventoryCatalog.AssetId(item.Type)].Recycle is not { } package)
        {
            return null;
        }

        long amount = recycling.Amount ?? item.Quantity;
        if (amount > item.Quantity)
        {
            rejection = "invalid_recycle_amount";
            return null;
        }

        rejection = "stash_full";
        return Give(Take(inventory, area, item, amount, created, consumed, etag, updatedAt), package, amount, etag, updatedAt);
    }

    /// <summary>
    /// Removes <paramref name="amount"/> of an owned stack, or the whole item when that is all of it; a removed item no
    /// longer needs the parent move a split would.
    /// </summary>
    private static PlayerInventory Take(PlayerInventory inventory, InventoryArea area, OwnedItem item, long amount, HashSet<string> created,
        HashSet<string> consumed, string etag, long updatedAt)
    {
        bool emptied = amount == item.Quantity;
        inventory = Replace(inventory, area, item.InstanceId, emptied ? null : item with { Quantity = item.Quantity - amount, ETag = etag, UpdatedAt = updatedAt });
        if (!emptied)
        {
            return inventory;
        }

        created.Remove(item.InstanceId);
        consumed.Add(item.InstanceId);
        return inventory;
    }

    private static (InventoryArea Area, OwnedItem Item)? Find(PlayerInventory inventory, string instanceId)
    {
        foreach (InventoryArea area in Enum.GetValues<InventoryArea>())
        {
            if (InventoryCatalog.Items(inventory, area).FirstOrDefault(item => item.InstanceId == instanceId) is { } found)
            {
                return (area, found);
            }
        }

        return null;
    }

    private static PlayerInventory Replace(PlayerInventory inventory, InventoryArea area, string instanceId, OwnedItem? replacement) =>
        InventoryCatalog.WithItems(inventory, area,
            [.. InventoryCatalog.Items(inventory, area).Select(item => item.InstanceId == instanceId ? replacement : item).OfType<OwnedItem>()]);

    /// <summary>Version stamped on items an inventory change creates or modifies.</summary>
    private static string ItemETag(long revision) => "ares-host-inventory-" + revision.ToString(CultureInfo.InvariantCulture);

    private static bool ValidRequestId(string? id) => id is { Length: > 0 and <= 128 }
        && !id.Any(character => char.IsControl(character) || char.IsWhiteSpace(character));

    private static ServiceReply Reply(IReadOnlyList<InventoryItem> updated, IReadOnlyList<string> deleted) =>
        ServiceReply.Ok(new InventoryOperationsResponse(Changeset.Empty with { Items = new ItemChanges([], updated, deleted) }));
}
