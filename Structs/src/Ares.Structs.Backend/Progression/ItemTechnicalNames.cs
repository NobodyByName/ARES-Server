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

    /// <summary>Protocol asset IDs indexed by technical item name.</summary>
    public static IReadOnlyDictionary<string, long> AssetIds { get; } = new Dictionary<string, long>(StringComparer.Ordinal)
    {
        [RubberDuck] = StorageItems.RubberDuckAssetId,
        [LightShield] = ShieldItems.LightShieldAssetId,
        [FerroI] = WeaponItems.FerroIAssetId,
        [HeavyAmmo] = WeaponItems.HeavyAmmoAssetId,
    }.Concat(AugmentItems.ItemAssetIds).ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);

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
    };
}
