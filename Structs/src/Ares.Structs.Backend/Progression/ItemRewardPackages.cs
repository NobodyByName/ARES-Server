namespace Ares.Structs.Backend.Progression;

/// <summary>How the game describes recycling and selling an item, and how it prices a sold item.</summary>
public static class ItemRewardPackages
{
    /// <summary>Durability below which an item still sells as if it had this durability.</summary>
    public const float MinimumSaleDurability = 0.1f;

    /// <summary>
    /// The items one recycled item gives, as the item description's recycle package, or null when
    /// <see cref="ItemRecycling.Outputs"/> lists nothing for the item.
    /// </summary>
    /// <param name="assetId">Item asset id.</param>
    /// <returns>The item's recycle package, or null when it cannot be recycled.</returns>
    public static OfferRewardPackage? Recycle(long assetId) => ItemRecycling.Outputs.TryGetValue(assetId, out IReadOnlyList<ItemAssetAmount>? outputs)
        ? Recycle(outputs)
        : null;

    /// <summary>The items one recycled item gives, as the item description's recycle package.</summary>
    /// <param name="outputs">The items one recycled item gives, in package order.</param>
    /// <returns>The recycle package.</returns>
    public static OfferRewardPackage Recycle(IReadOnlyList<ItemAssetAmount> outputs)
    {
        ArgumentNullException.ThrowIfNull(outputs);
        return new OfferRewardPackage([.. outputs.Select(output => new OfferRewardedItem(output.GameAssetId, output.Amount, FullDurability, [], "", 0, HideInUi: false))]);
    }

    /// <summary>The coins an item sells for at full durability, or null when it has no scrap value.</summary>
    /// <param name="scrapValue">The item's scrap value (the game data's item values), 0 when it has none.</param>
    /// <param name="coinsAssetId">Asset id of the coins item.</param>
    /// <returns>The item's scrap description, or null when it cannot be sold.</returns>
    public static ItemScrapDescription? Scrap(int scrapValue, long coinsAssetId) => scrapValue > 0
        ? new ItemScrapDescription(new OfferRewardPackage([new OfferRewardedItem(coinsAssetId, scrapValue, FullDurability, [], "", 0, HideInUi: false)]))
        : null;

    /// <summary>
    /// Coins one item sells for: its full-durability value scaled by its durability (0 to 1), never below
    /// <see cref="MinimumSaleDurability"/> of the value, rounded down. Computed in single precision, as the menu does.
    /// </summary>
    /// <param name="value">Full-durability value of one item.</param>
    /// <param name="durability">Durability of the item, 0 to 1.</param>
    /// <returns>The coins one item sells for.</returns>
    public static long SaleUnitPrice(long value, double durability) =>
        (long)MathF.Floor(MathF.Max(value * (float)durability, value * MinimumSaleDurability));

    /// <summary>Durability of every reward in a recycle or scrap package.</summary>
    private const double FullDurability = 1.0;
}
