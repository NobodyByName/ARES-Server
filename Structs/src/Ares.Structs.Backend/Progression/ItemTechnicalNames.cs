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
    public const string Wires = "DA_Item_Salvage_Wires";
    public const string Battery = "DA_Item_Salvage_Battery";
    public const string SteelSpring = "DA_Item_Salvage_SteelSpring";
    public const string DuctTape = "DA_Item_Salvage_DuctTape";
    public const string Oil = "DA_Item_Salvage_Oil";
    public const string Canister = "DA_Item_Salvage_Canister";
    public const string Magnet = "DA_Item_Salvage_Magnet";
    public const string DurableCloth = "DA_Item_Salvage_WovenCloth";
    public const string MechanicalComponents = "DA_Item_Salvage_MechanicalComponents";
    public const string ElectricalComponents = "DA_Item_Salvage_ElectricalComponents";
    public const string SimpleGunParts = "DA_Item_Salvage_GunParts";
    public const string CrudeExplosives = "DA_Item_Salvage_ExplosiveMixture";
    public const string ArcAlloy = "DA_Item_Salvage_Mat_ARC_Metal";
    public const string Coins = "DA_Item_GrindCurrency";

    /// <summary>
    /// Every name above, in a fixed order (Rubber Duck, the shield, Ferro, its ammunition, the Snap Hook, the augments, the
    /// salvage materials by technical name, coins): the items supported beyond the plain catalogue items.
    /// </summary>
    public static IReadOnlyList<string> All { get; } =
    [
        RubberDuck, LightShield, FerroI, HeavyAmmo, SnapHook, CombatMk1, LootingMk1, TacticalMk1, Battery, Canister, Chemicals, DuctTape,
        ElectricalComponents, CrudeExplosives, Fabric, SimpleGunParts, Magnet, ArcAlloy, MechanicalComponents, Oil, Planks, PlasticParts,
        RubberParts, MetalParts, AssortedSeeds, SheetMetal, SteelSpring, Wires, DurableCloth, Coins,
    ];

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
        [Wires] = "/Game/Pioneer/Items/Salvage/Wires/DA_Item_Salvage_Wires.DA_Item_Salvage_Wires",
        [Battery] = "/Game/Pioneer/Items/Salvage/Battery/DA_Item_Salvage_Battery.DA_Item_Salvage_Battery",
        [SteelSpring] = "/Game/Pioneer/Items/Salvage/SteelSpring/DA_Item_Salvage_SteelSpring.DA_Item_Salvage_SteelSpring",
        [DuctTape] = "/Game/Pioneer/Items/Salvage/DuctTape/DA_Item_Salvage_DuctTape.DA_Item_Salvage_DuctTape",
        [Oil] = "/Game/Pioneer/Items/Salvage/Oil/DA_Item_Salvage_Oil.DA_Item_Salvage_Oil",
        [Canister] = "/Game/Pioneer/Items/Salvage/Canister/DA_Item_Salvage_Canister.DA_Item_Salvage_Canister",
        [Magnet] = "/Game/Pioneer/Items/Salvage/Magnet/DA_Item_Salvage_Magnet.DA_Item_Salvage_Magnet",
        [DurableCloth] = "/Game/Pioneer/Items/Salvage/WovenCloth/DA_Item_Salvage_WovenCloth.DA_Item_Salvage_WovenCloth",
        [MechanicalComponents] = "/Game/Pioneer/Items/Salvage/MechanicalComponents/DA_Item_Salvage_MechanicalComponents.DA_Item_Salvage_MechanicalComponents",
        [ElectricalComponents] = "/Game/Pioneer/Items/Salvage/ElectricalComponents/DA_Item_Salvage_ElectricalComponents.DA_Item_Salvage_ElectricalComponents",
        [SimpleGunParts] = "/Game/Pioneer/Items/Salvage/GunParts/DA_Item_Salvage_GunParts.DA_Item_Salvage_GunParts",
        [CrudeExplosives] = "/Game/Pioneer/Items/Salvage/ExplosiveMixture/DA_Item_Salvage_ExplosiveMixture.DA_Item_Salvage_ExplosiveMixture",
        [ArcAlloy] = "/Game/Pioneer/Items/Salvage/Mat_ARC_Metal/DA_Item_Salvage_Mat_ARC_Metal.DA_Item_Salvage_Mat_ARC_Metal",
    };
}
