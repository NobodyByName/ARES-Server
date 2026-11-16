namespace Ares.Structs.Backend.Progression;

/// <summary>How the game prices a sold item.</summary>
public static class ItemSale
{
    /// <summary>Durability below which an item still sells as if it had this durability.</summary>
    public const float MinimumDurability = 0.1f;

    /// <summary>
    /// Coins one item sells for: its full-durability value scaled by its durability (0 to 1), never below
    /// <see cref="MinimumDurability"/> of the value, rounded down. Computed in single precision, as the menu does.
    /// </summary>
    /// <param name="value">Full-durability value of one item.</param>
    /// <param name="durability">Durability of the item, 0 to 1.</param>
    /// <returns>The coins one item sells for.</returns>
    public static long UnitPrice(long value, double durability) =>
        (long)MathF.Floor(MathF.Max(value * (float)durability, value * MinimumDurability));

    /// <summary>The coins an item sells for at full durability, or null when <see cref="ItemValues"/> gives it no value.</summary>
    /// <param name="assetId">Item asset id.</param>
    /// <returns>The item's scrap description, or null when it cannot be sold.</returns>
    public static ItemScrapDescription? Scrap(long assetId) => ItemValues.ScrapValues.TryGetValue(assetId, out int value) && value > 0
        ? new ItemScrapDescription(new OfferRewardPackage([new OfferRewardedItem(CurrencyItems.CoinsAssetId, value, FullDurability, [], "", 0, HideInUi: false)]))
        : null;

    /// <summary>Durability the scrap reward package is priced at.</summary>
    private const double FullDurability = 1.0;
}
