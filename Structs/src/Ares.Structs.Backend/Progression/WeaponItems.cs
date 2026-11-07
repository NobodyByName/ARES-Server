namespace Ares.Structs.Backend.Progression;

/// <summary>Supported firearm and ammunition item definitions.</summary>
public static class WeaponItems
{
    /// <summary>Asset id of FerroI.</summary>
    public const long FerroIAssetId = -1300343709L;

    /// <summary>Asset id of HeavyAmmo.</summary>
    public const long HeavyAmmoAssetId = -1402742438L;

    /// <summary>Asset id of MuzzleModSlot.</summary>
    public const long MuzzleModSlotAssetId = -377848428L;

    /// <summary>Asset id of UnderBarrelModSlot.</summary>
    public const long UnderBarrelModSlotAssetId = -1923186072L;

    /// <summary>Asset id of StockModSlot.</summary>
    public const long StockModSlotAssetId = 1415875854L;

    /// <summary>Item definitions and their allowed child slots.</summary>
    public static IReadOnlyList<ItemDescription> Definitions { get; } =
    [
        new(-1300343709L, "GameItem", 1L, false, false,
            ["Online.Release.Base", "ItemSkin.ItemType.SkinnableItem", "ItemSkin.SkinnableItem.BattleRifleBreachAction01"], [])
        {
            BaseGameAssetId = -1180768360L,
            QualityLevel = 0L,
            SlotConfig = new StructuralItemSlots([-377848428L, -1923186072L, 1415875854L]),
        },
        new(-1402742438L, "GameItem", 60L, true, false,
            ["Online.Release.Base"], [])
        {
            BaseGameAssetId = -1402742438L,
            QualityLevel = 0L,
            SlotConfig = new NoItemSlots(),
        },
        new(-377848428L, "InventoryStructure", 1L, false, false,
            ["Online.Item.Modification", "Item.Flag.MandatoryChild", "Online.Release.Base"], [])
        {
            BaseGameAssetId = 0L,
            QualityLevel = 0L,
            SlotConfig = new SingleItemSlot(
            [
                -450689218L, 1363078343L, -1113070540L, -1888542545L, -1072384644L, -518509289L, -977121851L, -53041115L,
                1407760402L, -409081322L, 1921518029L, -621597590L,
            ], false, 0L),
        },
        new(-1923186072L, "InventoryStructure", 1L, false, false,
            ["Online.Item.Modification", "Item.Flag.MandatoryChild", "Online.Release.Base"], [])
        {
            BaseGameAssetId = 0L,
            QualityLevel = 0L,
            SlotConfig = new SingleItemSlot(
            [
                346923872L, -1681246336L, -1131982378L, 1013182792L, -1404148784L, -295158634L, -1074924798L,
            ], false, 0L),
        },
        new(1415875854L, "InventoryStructure", 1L, false, false,
            ["Online.Item.Modification", "Item.Flag.MandatoryChild", "Online.Release.Base"], [])
        {
            BaseGameAssetId = 0L,
            QualityLevel = 0L,
            SlotConfig = new SingleItemSlot(
            [
                668021707L, -2095596285L, 1682428740L, 775298648L, 1287783108L, -123856886L,
            ], false, 0L),
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
