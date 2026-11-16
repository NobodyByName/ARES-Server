using Ares.Structs.Game.Loot;
using Ares.Structs.Game.World;

namespace Ares.Raid.World.Loot;

/// <summary>How common a loot item is, read from its drop tags; the roll weights a container's next stack by it.</summary>
public enum LootTier
{
    /// <summary>Common crafting materials.</summary>
    Common,

    /// <summary>Uncommon crafting materials.</summary>
    Uncommon,

    /// <summary>Rare crafting materials.</summary>
    Rare,

    /// <summary>Epic crafting materials.</summary>
    Epic,

    /// <summary>Items players recycle into materials.</summary>
    Recyclables,

    /// <summary>Valuables to sell.</summary>
    Trinkets,

    /// <summary>Every other item: plants, ARC parts, healing items.</summary>
    Other,
}

/// <summary>The weight of a tier when a container draws its next stack.</summary>
/// <param name="Tier">The tier.</param>
/// <param name="DropTag">Drop tag of the tier's items (an item with this tag or one below it); null for <see cref="LootTier.Other"/>.</param>
/// <param name="Weight">Relative weight.</param>
public sealed record LootTierWeight(LootTier Tier, string? DropTag, double Weight);

/// <summary>How much the loot value of the area around a container scales the weight of a tier.</summary>
/// <param name="Value">Loot value of the area.</param>
/// <param name="Tier">The tier.</param>
/// <param name="Multiplier">Factor on the tier's weight.</param>
public sealed record LootTierMultiplier(PointOfInterestLootValue Value, LootTier Tier, double Multiplier);

/// <summary>
/// The value loot tables a container category draws from. An item of these tables may go in a container only when its
/// own placement list also accepts the container's tag.
/// </summary>
/// <param name="Name">Name of the pool, as logs show it.</param>
/// <param name="Tag">
/// Container tag of the category: the tag itself and every tag below it that has no pool of its own. Tag names compare
/// without case.
/// </param>
/// <param name="Tables">Value loot table asset names; an item's stack size and durability come from the first entry that selects it.</param>
/// <param name="Stacks">Number of stacks, drawn uniformly; null for the game's weighted container targets.</param>
/// <param name="ExtraStack">Whether the container may get one extra stack from <see cref="LootContentDefaults.ExtraStackEntries"/>.</param>
public sealed record LootTablePool(string Name, string Tag, IReadOnlyList<string> Tables, LootRange? Stacks = null, bool ExtraStack = true);

/// <summary>Items chosen by their drop tags, and how each stack of them looks.</summary>
/// <param name="DropTag">Drop tag of the items: an item with this tag or one below it.</param>
/// <param name="ExcludedDropTags">Items with one of these drop tags (or one below it) are left out.</param>
/// <param name="StackSize">Items per stack, drawn uniformly and capped at the item's largest stack.</param>
/// <param name="Weight">Relative weight among the entries of its pool.</param>
public sealed record LootTypeEntry(string DropTag, IReadOnlyList<string> ExcludedDropTags, LootRange StackSize, double Weight);

/// <summary>
/// Contents of containers no value loot table fills (the game's tables for them are not part of the client): items
/// chosen by drop tags, with no placement check. Avoid groups do not apply; an item still appears once per container.
/// </summary>
/// <param name="Name">Name of the pool, as logs show it.</param>
/// <param name="Classes">Container class paths that use the pool.</param>
/// <param name="Tags">Container tags that use the pool: the tag itself and every tag below it.</param>
/// <param name="Stacks">Number of stacks, drawn uniformly.</param>
/// <param name="Entries">The entries; each stack draws one by weight, then one of its items evenly.</param>
public sealed record LootTypePool(string Name, IReadOnlyList<string> Classes, IReadOnlyList<string> Tags, LootRange Stacks, IReadOnlyList<LootTypeEntry> Entries);

/// <summary>
/// ARES's default container contents for Dam. A container draws its stacks when the raid creates it: a container
/// class or tag with a type pool uses it, otherwise its tag's table pool. A container with no candidate item holds
/// the placeholder rubber duck. These are ARES numbers, not the official server's (its value budgets per area are not
/// part of the client).
/// </summary>
public static class LootContentDefaults
{
    /// <summary>Tables every salvage category adds to its own: global tiers, old-world and Exodus items.</summary>
    public static IReadOnlyList<string> SharedTables { get; } =
    [
        "StaticValueLootTable_GlobalCommonMaterials",
        "StaticValueLootTable_GlobalUncommonMaterials",
        "StaticValueLootTable_GlobalRareMaterials",
        "StaticValueLootTable_GlobalEpicMaterials",
        "StaticValueLootTable_GlobalOutpostMaterials",
        "StaticValueLootTable_GlobalRecyclables",
        "StaticValueLootTable_GlobalRecyclablesLow",
        "StaticValueLootTable_GlobalRecyclablesMid",
        "StaticValueLootTable_GlobalTrinkets",
        "StaticValueLootTable_GlobalTrinketsLow",
        "StaticValueLootTable_GlobalTrinketsMid",
        "StaticValueLootTable_GlobalTrinketsHigh",
        "StaticValueLootTable_OldWorldAll",
        "StaticValueLootTable_ExodusMixed",
        "StaticValueLootTable_UncommonMaterialsExodus",
        "StaticValueLootTable_EpicMaterialsExodus",
        "StaticValueLootTable_RecyclablesExodus_HighTier",
    ];

    /// <summary>Table pools by container category.</summary>
    public static IReadOnlyList<LootTablePool> TablePools { get; } =
    [
        new("Tech", "Socket.LootContainer.Tech",
        [
            "StaticValueLootTable_ElectricalTechMixedMaterialsAll", "StaticValueLootTable_BasicMaterialsElectrical",
            "StaticValueLootTable_RareMaterialsTechnological", "StaticValueLootTable_RecyclablesTechnological", .. SharedTables,
        ]),
        new("Electrical", "Socket.LootContainer.Electrical",
        [
            "StaticValueLootTable_ElectricalAll", "StaticValueLootTable_BasicMaterialsElectrical", "StaticValueLootTable_ElectricalTechMixedMaterials",
            .. SharedTables,
        ]),
        new("Mechanical", "Socket.LootContainer.Mechanical",
        [
            "StaticValueLootTable_MechanicalAll", "StaticValueLootTable_BasicMaterialsMechanical", "StaticValueLootTable_MechanicalIndustrialMixedAll",
            .. SharedTables,
        ]),
        new("Industrial", "Socket.LootContainer.Industrial",
        [
            "StaticValueLootTable_IndustrialAll", "StaticValueLootTable_BasicMaterialsMechanical", "StaticValueLootTable_MechanicalIndustrialMixedAll",
            "StaticValueLootTable_SecurityAll", .. SharedTables,
        ]),
        new("Commercial", "Socket.LootContainer.Commercial",
        [
            "StaticValueLootTable_CommercialAll", "StaticValueLootTable_CivilianMixedMaterials", "StaticValueLootTable_TrinketsCommercial",
            "StaticValueLootTable_RecyclablesCommercial", .. SharedTables,
        ]),
        new("Residential", "Socket.LootContainer.Residential",
        [
            "StaticValueLootTable_ResidentialAll", "StaticValueLootTable_CivilianMixedMaterials", "StaticValueLootTable_TrinketsResidential",
            "StaticValueLootTable_RecyclablesResidential", "StaticValueLootTable_RecyclablesFabricBased", .. SharedTables,
        ]),
        new("Medical", "Socket.LootContainer.Medical",
        [
            "StaticValueLootTable_MedicalMixed", "StaticValueLootTable_RecyclablesMedical", .. SharedTables,
        ]),
        new("Security", "Socket.LootContainer.Security",
        [
            "StaticValueLootTable_SecurityAll", "StaticValueLootTable_SecurityMaterials", "StaticValueLootTable_RecyclablesSecurity", .. SharedTables,
        ]),
        new("OldWorld", "Socket.LootContainer.OldWorld", ["StaticValueLootTable_TrinketsOldWorld", .. SharedTables]),
        new("Exodus", "Socket.LootContainer.Exodus", [.. SharedTables]),
        new("Generic", "Socket.LootContainer.Generic", ["StaticValueLootTable_CivilianMixedAll", .. SharedTables]),
        new("ARC", "Socket.LootContainer.ARC", ["StaticValueLootTable_ARC"], ExtraStack: false),
        new("Nature", "Socket.LootContainer.Nature", ["StaticValueLootTable_Nature"], new LootRange(1, 1), ExtraStack: false),
    ];

    /// <summary>Weights of the tiers. The first stack of a container is a common material whenever its pool has one.</summary>
    public static IReadOnlyList<LootTierWeight> TierWeights { get; } =
    [
        new(LootTier.Common, "Item.Drop.Type.Salvage.Material.Common", 3),
        new(LootTier.Uncommon, "Item.Drop.Type.Salvage.Material.Uncommon", 3),
        new(LootTier.Rare, "Item.Drop.Type.Salvage.Material.Rare", 2),
        new(LootTier.Epic, "Item.Drop.Type.Salvage.Material.Epic", 0.25),
        new(LootTier.Recyclables, "Item.Drop.Type.Salvage.Recyclables", 2),
        new(LootTier.Trinkets, "Item.Drop.Type.Salvage.Trinket", 1),
        new(LootTier.Other, null, 2),
    ];

    /// <summary>Tier multipliers by the loot value of the area around a container; tiers without an entry keep their weight.</summary>
    public static IReadOnlyList<LootTierMultiplier> TierMultipliers { get; } =
    [
        new(PointOfInterestLootValue.Medium, LootTier.Rare, 1.5),
        new(PointOfInterestLootValue.Medium, LootTier.Epic, 2),
        new(PointOfInterestLootValue.High, LootTier.Rare, 2),
        new(PointOfInterestLootValue.High, LootTier.Epic, 3),
    ];

    /// <summary>Items no container gets: every map tag names a map other than Dam.</summary>
    public static IReadOnlyList<string> ExcludedDropTags { get; } = ["Item.Drop.Category.Map"];

    /// <summary>Chance that a container of a table pool with <see cref="LootTablePool.ExtraStack"/> gets one extra stack.</summary>
    public const double ExtraStackChance = 0.15;

    /// <summary>What the extra stack draws from (an ARES pool: the game's tables for these items are not part of the client).</summary>
    public static IReadOnlyList<LootTypeEntry> ExtraStackEntries { get; } =
    [
        new("Item.Drop.Type.Ammo", [], new LootRange(10, 30), 1),
        new("Item.Drop.Type.Consumable.Healing", [], new LootRange(1, 2), 1),
    ];

    /// <summary>ARES pools of the Raider containers and weapon lockers.</summary>
    public static IReadOnlyList<LootTypePool> TypePools { get; } =
    [
        new("RaiderAmmo", ["/Game/Pioneer/Environment/Props/Raider/AmmoBox_01/BP_SocketContainer_Raider_AmmoBox_01_Lid_A_DynamicHighTier.BP_SocketContainer_Raider_AmmoBox_01_Lid_A_DynamicHighTier_C"],
            [], new LootRange(1, 3), [new LootTypeEntry("Item.Drop.Type.Ammo", [], new LootRange(10, 30), 1)]),
        new("RaiderMedical", ["/Game/Pioneer/Environment/Props/Raider/MedicalBag_01/BP_SocketContainer_Raider_MedicalBag_01_Lid_A__Dynamic.BP_SocketContainer_Raider_MedicalBag_01_Lid_A__Dynamic_C"],
            [], new LootRange(1, 3), [new LootTypeEntry("Item.Drop.Type.Consumable.Healing", [], new LootRange(1, 3), 1)]),
        new("RaiderGrenades", ["/Game/Pioneer/Environment/Props/Raider/GrenadeContainer_01/BP_SocketContainer_Raider_GrenadeContainer_01_Lid_A_Dynamic.BP_SocketContainer_Raider_GrenadeContainer_01_Lid_A_Dynamic_C"],
            [], new LootRange(1, 3), [new LootTypeEntry("Item.Drop.Type.Consumable.Throwable.Grenade", [], new LootRange(1, 3), 1)]),
        new("RaiderWeaponsHigh", ["/Game/Pioneer/Environment/Props/Raider/WeaponCase_01/BP_SocketContainer_Raider_WeaponCase_01_Lid_A_01_DynamicHigh.BP_SocketContainer_Raider_WeaponCase_01_Lid_A_01_DynamicHigh_C"],
            [], new LootRange(1, 3),
            [
                new LootTypeEntry("Item.Drop.Type.Equipment.Modification.Weapon", ["Item.Drop.Tier.Low"], new LootRange(1, 1), 1),
                new LootTypeEntry("Item.Drop.Type.Ammo", [], new LootRange(10, 30), 1),
            ]),
        new("RaiderWeapons",
            [
                "/Game/Pioneer/Environment/Props/Raider/WeaponCase_01/BP_SocketContainer_Raider_WeaponCase_01_Lid_A_01_Dynamic.BP_SocketContainer_Raider_WeaponCase_01_Lid_A_01_Dynamic_C",
                "/Game/Pioneer/Environment/Props/Research/WeaponsRack_01/BP_SocketContainer_Rsr_WeaponsRack_01_Dynamic.BP_SocketContainer_Rsr_WeaponsRack_01_Dynamic_C",
            ],
            ["Socket.LootContainer.Raider.WeaponCase", "Socket.LootContainer.Military.WeaponLocker"], new LootRange(1, 3),
            [
                new LootTypeEntry("Item.Drop.Type.Equipment.Modification.Weapon", ["Item.Drop.Tier.High"], new LootRange(1, 1), 1),
                new LootTypeEntry("Item.Drop.Type.Ammo", [], new LootRange(10, 30), 1),
            ]),
        new("RaiderBackpack", ["/Game/Pioneer/Environment/Props/_Generic/HikingBag/BP_SocketContainer_Raider_Backpack_Dynamic.BP_SocketContainer_Raider_Backpack_Dynamic_C"],
            [], new LootRange(1, 3),
            [
                new LootTypeEntry("Item.Drop.Type.Salvage.Material.Common", [], new LootRange(2, 5), 3),
                new LootTypeEntry("Item.Drop.Type.Salvage.Recyclables", [], new LootRange(1, 1), 2),
                new LootTypeEntry("Item.Drop.Type.Salvage.Trinket", [], new LootRange(1, 1), 1),
                new LootTypeEntry("Item.Drop.Type.Consumable.Healing", [], new LootRange(1, 2), 1),
            ]),
        new("RaiderFruit", ["/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C"],
            ["Socket.LootContainer.Nature.FruitBasket"], new LootRange(1, 3),
            [new LootTypeEntry("Item.Drop.Category.Area.Nature", ["Item.Drop.Type.Salvage.Rock", "Item.Drop.Type.Salvage.Material"], new LootRange(1, 3), 1)]),
    ];
}
