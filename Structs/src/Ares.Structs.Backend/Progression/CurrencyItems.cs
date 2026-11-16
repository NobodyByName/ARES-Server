namespace Ares.Structs.Backend.Progression;

/// <summary>Supported currency item definitions.</summary>
public static class CurrencyItems
{
    /// <summary>Asset id of Coins.</summary>
    public const long CoinsAssetId = -898439395L;

    /// <summary>Item definitions and their allowed child slots.</summary>
    public static IReadOnlyList<ItemDescription> Definitions { get; } =
    [
        new(-898439395L, "GameItem", 2000000000L, true, true,
            ["Item.Flag.Persistent", "Item.Flag.Unique", "Online.Release.Base"], [])
        {
            BaseGameAssetId = -898439395L,
            QualityLevel = 0L,
            SlotConfig = new NoItemSlots(),
        },
    ];

    /// <summary>Supported currency technical names.</summary>
    public static IReadOnlyDictionary<string, long> ItemAssetIds { get; } = new Dictionary<string, long>
    {
        ["DA_Item_GrindCurrency"] = -898439395L,
    };
}
