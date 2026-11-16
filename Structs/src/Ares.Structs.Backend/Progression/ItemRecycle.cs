namespace Ares.Structs.Backend.Progression;

/// <summary>How the game describes recycling an item.</summary>
public static class ItemRecycle
{
    /// <summary>
    /// The items one recycled item gives, as the item description's recycle package, or null when
    /// <see cref="ItemRecycling.Outputs"/> lists nothing for the item.
    /// </summary>
    /// <param name="assetId">Item asset id.</param>
    /// <returns>The item's recycle package, or null when it cannot be recycled.</returns>
    public static OfferRewardPackage? Package(long assetId) => ItemRecycling.Outputs.TryGetValue(assetId, out IReadOnlyList<ItemAssetAmount>? outputs)
        ? new OfferRewardPackage([.. outputs.Select(output => new OfferRewardedItem(output.GameAssetId, output.Amount, FullDurability, [], "", 0, HideInUi: false))])
        : null;

    /// <summary>Durability of every recycled output.</summary>
    private const double FullDurability = 1.0;
}
