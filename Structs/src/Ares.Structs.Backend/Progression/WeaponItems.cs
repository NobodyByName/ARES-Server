namespace Ares.Structs.Backend.Progression;

/// <summary>Supported firearm and ammunition item definitions.</summary>
public static class WeaponItems
{
    /// <summary>Asset id of FerroI.</summary>
    public const long FerroIAssetId = -1300343709L;

    /// <summary>Asset id of HeavyAmmo.</summary>
    public const long HeavyAmmoAssetId = -1402742438L;

    /// <summary>Item definitions and their allowed child slots.</summary>
    public static IReadOnlyList<ItemDescription> Definitions { get; } =
    [
        new(-1300343709L, "GameItem", 1L, false, false,
            ["Online.Release.Base", "ItemSkin.ItemType.SkinnableItem", "ItemSkin.SkinnableItem.BattleRifleBreachAction01"], [])
        {
            BaseGameAssetId = -1180768360L,
            QualityLevel = 0L,
            SlotConfig = new NoItemSlots(),
        },
        new(-1402742438L, "GameItem", 60L, true, false,
            ["Online.Release.Base"], [])
        {
            BaseGameAssetId = -1402742438L,
            QualityLevel = 0L,
            SlotConfig = new NoItemSlots(),
        },
    ];

    /// <summary>Unit weight of a Ferro I.</summary>
    public const double FerroIWeight = 8.0;

    /// <summary>Unit weight of one Heavy Ammo round.</summary>
    public const double HeavyAmmoWeight = 0.05;

    /// <summary>Ferro index in the usable-item catalogue.</summary>
    public const int FerroIUsableIndex = 47;

    /// <summary>Heavy Ammo index in the usable-item catalogue.</summary>
    public const int HeavyAmmoUsableIndex = 143;
}
