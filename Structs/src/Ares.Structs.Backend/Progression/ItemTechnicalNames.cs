namespace Ares.Structs.Backend.Progression;

/// <summary>Technical asset names of supported inventory items.</summary>
public static class ItemTechnicalNames
{
    public const string RubberDuck = "DA_Item_Salvage_Trinket_RubberDuck_Standard";
    public const string CombatMk1 = "DA_Item_Augment_Combat_T1";
    public const string LootingMk1 = "DA_Item_Augment_Looting_T1";
    public const string TacticalMk1 = "DA_Item_Augment_Utility_T1";
    public const string LightShield = "DA_Item_Armor_LightBody";
    public const string FerroI = "DA_Item_BattleRifle_BreachAction_01";
    public const string HeavyAmmo = "DA_Item_HeavyAmmo";
    public const string SnapHook = "DA_Item_SnapHook";
    public const string AssortedSeeds = "DA_Item_Salvage_Seeds";
    public const string Chemicals = "DA_Item_Salvage_Chemicals";
    public const string Fabric = "DA_Item_Salvage_Fabric";
    public const string Planks = "DA_Item_Salvage_Planks";
    public const string PlasticParts = "DA_Item_Salvage_Plastic";
    public const string RubberParts = "DA_Item_Salvage_Rubber";
    public const string SheetMetal = "DA_Item_Salvage_SheetMetal";
    public const string MetalParts = "DA_Item_Salvage_ScrapMetal";
    public const string Coins = "DA_Item_GrindCurrency";

    /// <summary>Protocol asset IDs indexed by technical item name.</summary>
    public static IReadOnlyDictionary<string, long> AssetIds { get; } = new Dictionary<string, long>(StringComparer.Ordinal)
    {
        [RubberDuck] = StorageItems.RubberDuckAssetId,
        [LightShield] = ShieldItems.LightShieldAssetId,
        [FerroI] = WeaponItems.FerroIAssetId,
        [HeavyAmmo] = WeaponItems.HeavyAmmoAssetId,
        [SnapHook] = GadgetItems.SnapHookAssetId,
    }.Concat(AugmentItems.ItemAssetIds).Concat(SalvageItems.ItemAssetIds).Concat(CurrencyItems.ItemAssetIds).ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);

    /// <summary>Object path of each supported item's data asset (the item in the world, for example on the ground), by technical item name.</summary>
    public static IReadOnlyDictionary<string, string> DataAssets { get; } = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        [RubberDuck] = "/Game/Pioneer/Items/Salvage/Trinket_RubberDuck/RubberDuck_Standard/DA_Item_Salvage_Trinket_RubberDuck_Standard.DA_Item_Salvage_Trinket_RubberDuck_Standard",
        [CombatMk1] = "/Game/Pioneer/Items/Augment/Combat/Combat_T1/DA_Item_Augment_Combat_T1.DA_Item_Augment_Combat_T1",
        [LootingMk1] = "/Game/Pioneer/Items/Augment/Looting/Looting_T1/DA_Item_Augment_Looting_T1.DA_Item_Augment_Looting_T1",
        [TacticalMk1] = "/Game/Pioneer/Items/Augment/Utility/Utility_T1/DA_Item_Augment_Utility_T1.DA_Item_Augment_Utility_T1",
        [LightShield] = "/Game/Pioneer/Items/Armor/LightBody/DA_Item_Armor_LightBody.DA_Item_Armor_LightBody",
        [FerroI] = "/Game/Pioneer/Items/Firearms/BattleRifle_BreachAction_01/DA_Item_BattleRifle_BreachAction_01.DA_Item_BattleRifle_BreachAction_01",
        [HeavyAmmo] = "/Game/Pioneer/Items/Projectiles/HeavyAmmo/DA_Item_HeavyAmmo.DA_Item_HeavyAmmo",
        [SnapHook] = "/Game/Pioneer/Items/MobilityGadgets/SnapHook/DA_Item_SnapHook.DA_Item_SnapHook",
        [AssortedSeeds] = "/Game/Pioneer/Items/Salvage/Seeds/DA_Item_Salvage_Seeds.DA_Item_Salvage_Seeds",
        [Chemicals] = "/Game/Pioneer/Items/Salvage/Chemicals/DA_Item_Salvage_Chemicals.DA_Item_Salvage_Chemicals",
        [Fabric] = "/Game/Pioneer/Items/Salvage/Fabric/DA_Item_Salvage_Fabric.DA_Item_Salvage_Fabric",
        [Planks] = "/Game/Pioneer/Items/Salvage/Planks/DA_Item_Salvage_Planks.DA_Item_Salvage_Planks",
        [PlasticParts] = "/Game/Pioneer/Items/Salvage/Plastic/DA_Item_Salvage_Plastic.DA_Item_Salvage_Plastic",
        [RubberParts] = "/Game/Pioneer/Items/Salvage/Rubber/DA_Item_Salvage_Rubber.DA_Item_Salvage_Rubber",
        [SheetMetal] = "/Game/Pioneer/Items/Salvage/SheetMetal/DA_Item_Salvage_SheetMetal.DA_Item_Salvage_SheetMetal",
        [MetalParts] = "/Game/Pioneer/Items/Salvage/ScrapMetal/DA_Item_Salvage_ScrapMetal.DA_Item_Salvage_ScrapMetal",
    };
}
