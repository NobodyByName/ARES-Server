using System.Diagnostics.CodeAnalysis;
using Ares.Structs.Backend.Progression;

namespace Ares.Host.Services.Inventory;

/// <summary>
/// The inventory after a batch's quantity operations, before its parent moves place anything. <paramref name="Created"/>
/// holds the split stacks that still need a parent move and <paramref name="Consumed"/> the items the batch removed.
/// </summary>
internal sealed record AppliedOperations(PlayerInventory Inventory, bool Changed, IReadOnlySet<string> Created, IReadOnlySet<string> Consumed);

/// <summary>
/// Validates menu inventory operation batches and applies their quantity operations (amount moves, purchases, sales,
/// recycling and upgrades) to an owned-item state, with the definitions, offers and stash upgrade prices of
/// <paramref name="catalog"/>. Parent moves are left to <see cref="ParentMoveLayout"/>.
/// </summary>
internal sealed class InventoryRules(InventoryCatalog catalog)
{
    private const int MaximumOperations = 64;

    /// <summary>
    /// Checks a batch's shape before it touches any state: the request id, the operation count, every operation's
    /// required fields and payments, and that each bought offer exists. Returns false with the error code otherwise.
    /// </summary>
    internal bool TryValidate(InventoryOperationsRequest request, [NotNullWhen(true)] out IReadOnlyList<InventoryOperation>? operations,
        out string rejection)
    {
        rejection = "invalid_inventory_operations";
        if ((!string.IsNullOrEmpty(request.RequestId) && !ValidRequestId(request.RequestId))
            || request.Operations is not { Count: <= MaximumOperations } requested
            || requested.Any(operation => operation is not (SetItemParents { ItemInstanceId.Length: > 0, ParentInstanceIds: [{ Length: > 0 }] }
                or MoveItemAmount { FromInstanceId.Length: > 0, ToInstanceId.Length: > 0, Amount: > 0 }
                or AcceptOffer { Times: > 0 }
                or ScrapItem { InstanceId.Length: > 0, Amount: null or > 0 }
                or RecycleItem { InstanceId.Length: > 0, Amount: null or > 0 }
                or UpgradeItem { ItemInstanceId.Length: > 0 }))
            || requested.OfType<AcceptOffer>().Any(purchase => purchase.Payment?.Any(item => item is not { ItemInstanceId.Length: > 0, Amount: > 0 }) == true)
            || requested.OfType<UpgradeItem>().Any(upgrade => upgrade.Payment?.Items?.Any(item => item is not { ItemInstanceId.Length: > 0, Amount: > 0 }) == true))
        {
            operations = null;
            return false;
        }

        if (requested.OfType<AcceptOffer>().Any(purchase => catalog.Offer(purchase.OfferId) is null))
        {
            rejection = "unknown_offer";
            operations = null;
            return false;
        }

        rejection = "";
        operations = requested;
        return true;
    }

    /// <summary>
    /// Applies a validated batch's amount moves, purchases, sales, recycling and upgrades in order, stamping changed items
    /// with <paramref name="etag"/> and <paramref name="updatedAt"/>; parent moves are skipped. <paramref name="before"/> is
    /// the client view of the inventory before the batch. Returns null with the error code when an operation is refused,
    /// and throws <see cref="InvalidDataException"/> or <see cref="OverflowException"/> when one is malformed.
    /// </summary>
    internal AppliedOperations? Apply(PlayerInventory inventory, IReadOnlyList<InventoryOperation> operations,
        IReadOnlyDictionary<string, InventoryItem> before, string etag, long updatedAt, out string rejection)
    {
        // Amount moves and purchases only change quantities, create split stacks and add bought
        // stacks to the stash; the parent moves then decide the final layout, which also places
        // every newly split stack.
        HashSet<string> created = new(StringComparer.Ordinal);
        HashSet<string> consumed = new(StringComparer.Ordinal);
        bool changed = false;
        rejection = "";
        foreach (InventoryOperation operation in operations)
        {
            switch (operation)
            {
                case MoveItemAmount transfer:
                    inventory = MoveAmount(inventory, transfer, before, created, consumed, etag, updatedAt);
                    changed = true;
                    break;
                case AcceptOffer purchase:
                    if (Accept(inventory, purchase, consumed, etag, updatedAt, out rejection) is not { } bought)
                    {
                        return null;
                    }

                    inventory = bought;
                    changed = true;
                    break;
                case ScrapItem sale:
                    if (Sell(inventory, sale, created, consumed, etag, updatedAt, out rejection) is not { } sold)
                    {
                        return null;
                    }

                    inventory = sold;
                    changed = true;
                    break;
                case RecycleItem recycling:
                    if (Recycle(inventory, recycling, created, consumed, etag, updatedAt, out rejection) is not { } recycled)
                    {
                        return null;
                    }

                    inventory = recycled;
                    changed = true;
                    break;
                case UpgradeItem upgrade:
                    if (Upgrade(inventory, upgrade, before, consumed, etag, updatedAt, out rejection) is not { } upgraded)
                    {
                        return null;
                    }

                    inventory = upgraded;
                    changed = true;
                    break;
            }
        }

        rejection = "";
        return new AppliedOperations(inventory, changed, created, consumed);
    }

    /// <summary>Whether <paramref name="id"/> is usable as a request id or a new split stack id.</summary>
    private static bool ValidRequestId(string? id) => id is { Length: > 0 and <= 128 }
        && !id.Any(character => char.IsControl(character) || char.IsWhiteSpace(character));

    /// <summary>
    /// Moves part of a stackable stack into an existing stack of the same item or into a new stack named
    /// by the target id. A source moved in full is removed. New stacks wait in the source's area for
    /// their parent move.
    /// </summary>
    private PlayerInventory MoveAmount(PlayerInventory inventory, MoveItemAmount transfer, IReadOnlyDictionary<string, InventoryItem> before,
        HashSet<string> created, HashSet<string> consumed, string etag, long updatedAt)
    {
        (InventoryArea area, OwnedItem source) = Find(inventory, transfer.FromInstanceId) ?? throw new InvalidDataException("Unknown amount source");
        ItemDescription definition = catalog.ByAsset[catalog.AssetId(source.Type)];
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
            result = InventoryAreas.WithItems(result, area, [.. InventoryAreas.Items(result, area), split]);
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
    private PlayerInventory? Accept(PlayerInventory inventory, AcceptOffer purchase, HashSet<string> consumed, string etag, long updatedAt,
        out string rejection)
    {
        OfferDescription offer = catalog.Offer(purchase.OfferId)!;
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
    private PlayerInventory? Upgrade(PlayerInventory inventory, UpgradeItem upgrade, IReadOnlyDictionary<string, InventoryItem> before,
        HashSet<string> consumed, string etag, long updatedAt, out string rejection)
    {
        rejection = "item_not_upgradable";
        if (!before.TryGetValue(upgrade.ItemInstanceId, out InventoryItem? item) || item.GameAssetId != inventory.Stash.ContainerAssetId
            || catalog.Upgrade(item.GameAssetId) is not { NextItemGameAssetId: not 0 } description
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
    private PlayerInventory? Pay(PlayerInventory inventory, IReadOnlyList<ItemAssetAmount> price, long times,
        IReadOnlyList<ItemAmountToConsume>? chosen, HashSet<string> consumed, string etag, long updatedAt)
    {
        Dictionary<long, long> due = price.GroupBy(cost => cost.GameAssetId)
            .ToDictionary(group => group.Key, group => checked(group.Sum(cost => cost.Amount) * times));
        Dictionary<string, (InventoryArea Area, OwnedItem Item)> owned = Enum.GetValues<InventoryArea>()
            .SelectMany(area => InventoryAreas.Items(inventory, area).Select(item => (Area: area, Item: item)))
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
                foreach ((_, OwnedItem stack) in owned.Values.Where(entry => catalog.AssetId(entry.Item.Type) == asset)
                             .OrderBy(entry => entry.Area).ThenBy(entry => entry.Item.Quantity).TakeWhile(_ => remaining > 0))
                {
                    long take = Math.Min(remaining, stack.Quantity);
                    taken[stack.InstanceId] = take;
                    remaining -= take;
                }
            }
        }

        if (taken.Any(pair => !owned.TryGetValue(pair.Key, out (InventoryArea Area, OwnedItem Item) entry) || pair.Value > entry.Item.Quantity
                || !due.ContainsKey(catalog.AssetId(entry.Item.Type)))
            || due.Any(pair => taken.Where(entry => catalog.AssetId(owned[entry.Key].Item.Type) == pair.Key).Sum(entry => entry.Value) != pair.Value))
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
    private PlayerInventory? Give(PlayerInventory inventory, OfferRewardPackage package, long times, string etag, long updatedAt)
    {
        foreach (IGrouping<long, OfferRewardedItem> reward in package.RewardedItems.GroupBy(item => item.GameAssetId))
        {
            string type = catalog.TechnicalName(reward.Key);
            ItemDescription definition = catalog.ByAsset[reward.Key];
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
                if (stash.Count >= catalog.StashCapacity(inventory.Stash.ContainerAssetId))
                {
                    return null;
                }

                long added = Math.Min(remaining, stackLimit);
                stash.Add(new OwnedItem(Guid.NewGuid().ToString("D"), type, added, OwnedItem.FullDurability, OwnedItem.FullDurability, etag, updatedAt));
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
    private PlayerInventory? Sell(PlayerInventory inventory, ScrapItem sale, HashSet<string> created, HashSet<string> consumed, string etag,
        long updatedAt, out string rejection)
    {
        rejection = "item_not_sellable";
        if (Find(inventory, sale.InstanceId) is not var (area, item) || area == InventoryArea.Wallet
            || catalog.ByAsset[catalog.AssetId(item.Type)].Scrap is not { } scrap)
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
            string currency = catalog.TechnicalName(reward.Key);
            long coins = checked(reward.Sum(rewarded => ItemRewardPackages.SaleUnitPrice(rewarded.Amount, item.Durability)) * amount);
            if (!catalog.IsCurrency(currency))
            {
                throw new InvalidDataException("Unsupported scrap reward");
            }

            if (coins == 0)
            {
                continue;
            }

            OwnedItem? stack = inventory.Wallet.SingleOrDefault(owned => owned.Type == currency);
            if (coins > catalog.ByAsset[reward.Key].MaxAmount - (stack?.Quantity ?? 0))
            {
                rejection = "currency_full";
                return null;
            }

            OwnedItem paid = stack is null
                ? new OwnedItem(Guid.NewGuid().ToString("D"), currency, coins, OwnedItem.FullDurability, OwnedItem.FullDurability, etag, updatedAt)
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
    private PlayerInventory? Recycle(PlayerInventory inventory, RecycleItem recycling, HashSet<string> created, HashSet<string> consumed,
        string etag, long updatedAt, out string rejection)
    {
        rejection = "item_not_recyclable";
        if (Find(inventory, recycling.InstanceId) is not var (area, item) || area == InventoryArea.Wallet
            || catalog.ByAsset[catalog.AssetId(item.Type)].Recycle is not { } package)
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
            if (InventoryAreas.Items(inventory, area).FirstOrDefault(item => item.InstanceId == instanceId) is { } found)
            {
                return (area, found);
            }
        }

        return null;
    }

    private static PlayerInventory Replace(PlayerInventory inventory, InventoryArea area, string instanceId, OwnedItem? replacement) =>
        InventoryAreas.WithItems(inventory, area,
            [.. InventoryAreas.Items(inventory, area).Select(item => item.InstanceId == instanceId ? replacement : item).OfType<OwnedItem>()]);
}
