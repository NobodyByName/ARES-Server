using Ares.Structs.Net.GameplayTags;

namespace Ares.Structs.Game.Loot;

/// <summary>The game's value loot tables (<c>UStaticValueLootTableDataAsset</c>), and loot container defaults.</summary>
public static class StaticValueLootTables
{
    /// <summary>Every value loot table, ordered by asset path.</summary>
    public static IReadOnlyList<StaticValueLootTable> All { get; } =
    [
        .. StaticValueLootTablesPart0.Tables,
    ];

    /// <summary>
    /// Weighted target numbers of item stacks for a loot container (<c>StaticLootSpawnTargetStacks</c>); every
    /// container class uses these defaults.
    /// </summary>
    public static IReadOnlyList<LootTargetStacks> ContainerTargetStacks { get; } =
    [
        new(2, 2.0),
        new(3, 3.0),
    ];

    private static readonly Dictionary<string, StaticValueLootTable> s_byName =
        All.GroupBy(table => table.Name, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() == 1)
            .ToDictionary(group => group.Key, group => group.Single(), StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// The table with an asset name, ignoring case; null when no table or more than one table (in different
    /// folders) has it. <see cref="StaticValueLootTable.Path"/> tells same-named tables apart.
    /// </summary>
    /// <param name="name">Asset name, such as <c>StaticValueLootTable_ElectricalAll</c>.</param>
    /// <returns>The table, or null.</returns>
    public static StaticValueLootTable? Find(string name) => s_byName.GetValueOrDefault(name);
}

/// <summary>Tables 0 to 70 of <see cref="StaticValueLootTables.All"/>.</summary>
internal static class StaticValueLootTablesPart0
{
    public static IReadOnlyList<StaticValueLootTable> Tables { get; } =
    [
        new(
            "StaticValueLootTable_ElectricalTechMixedMaterialsAll_FrozenTrail",
            "/Game/Pioneer/LootTables/FrozenTrail/StaticValueLootTable_ElectricalTechMixedMaterialsAll_FrozenTrail",
            [
                new StaticValueLootTableEntry(LootItemQueryType.Query, new GameplayTagQuery(["Item.Drop.Category.Area.Electrical", "Item.Drop.Type.Salvage.Material.Uncommon", "Item.Drop.Category.Area.Technological"], [0, 1, 5, 1, 4, 2, 2, 2, 0, 1, 2, 2, 1, 2]), null, -1, new LootRange(2, 5), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.RoundRobin),
                new StaticValueLootTableEntry(LootItemQueryType.Query, new GameplayTagQuery(["Item.Drop.Category.Area.Electrical", "Item.Drop.Type.Salvage.Material.Rare", "Item.Drop.Category.Area.Technological"], [0, 1, 5, 1, 4, 2, 2, 2, 0, 1, 2, 2, 2, 1]), null, -1, new LootRange(1, 1), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.RoundRobin),
                new StaticValueLootTableEntry(LootItemQueryType.Query, new GameplayTagQuery(["Item.Drop.Category.Area.Electrical", "Item.Drop.Type.Salvage.Trinket", "Item.Drop.Category.Area.Technological"], [0, 1, 5, 1, 4, 2, 2, 2, 0, 1, 2, 2, 2, 1]), null, -1, new LootRange(1, 1), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.RoundRobin),
                new StaticValueLootTableEntry(LootItemQueryType.Query, new GameplayTagQuery(["Item.Drop.Category.Area.Electrical", "Item.Drop.Type.Salvage.Recyclables", "Item.Drop.Category.Area.Technological"], [0, 1, 5, 1, 4, 2, 2, 2, 0, 1, 2, 2, 2, 1]), null, -1, new LootRange(1, 1), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.RoundRobin),
            ]),
        new(
            "StaticValueLootTable_ExodusMixed_FrozenTrail",
            "/Game/Pioneer/LootTables/FrozenTrail/StaticValueLootTable_ExodusMixed_FrozenTrail",
            [
                new StaticValueLootTableEntry(LootItemQueryType.Query, new GameplayTagQuery(["Item.Drop.Category.Area.Exodus", "Item.Drop.Type.Salvage.Material.Uncommon"], [0, 1, 5, 1, 2, 2, 0, 1]), null, -1, new LootRange(1, 3), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.MixAllPlacements),
                new StaticValueLootTableEntry(LootItemQueryType.Query, new GameplayTagQuery(["Item.Drop.Category.Area.Exodus", "Item.Drop.Type.Salvage.Material.Rare"], [0, 1, 5, 1, 2, 2, 0, 1]), null, -1, new LootRange(1, 1), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.MixAllPlacements),
                new StaticValueLootTableEntry(LootItemQueryType.Query, new GameplayTagQuery(["Item.Drop.Category.Area.Exodus", "Item.Drop.Type.Salvage.Material.Epic"], [0, 1, 5, 1, 2, 2, 0, 1]), null, -1, new LootRange(1, 1), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.MixAllPlacements),
                new StaticValueLootTableEntry(LootItemQueryType.Query, new GameplayTagQuery(["Item.Drop.Category.Area.Exodus", "Item.Drop.Type.Salvage.Recyclables", "Item.Drop.Tier.High"], [0, 1, 5, 2, 2, 2, 0, 1, 3, 1, 2]), null, -1, new LootRange(1, 1), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.MixAllPlacements),
            ]),
        new(
            "StaticValueLootTable_FrozenTrailSpecificItems",
            "/Game/Pioneer/LootTables/FrozenTrail/StaticValueLootTable_FrozenTrailSpecificItems",
            [
                new StaticValueLootTableEntry(LootItemQueryType.Query, new GameplayTagQuery(["Item.Drop.Category.Map.FrozenTrail"], [0, 1, 1, 1, 0]), null, -1, new LootRange(1, 1), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.RoundRobin),
            ]),
        new(
            "StaticValueLootTable_GlobalCommonMaterials_FrozenTrail",
            "/Game/Pioneer/LootTables/FrozenTrail/StaticValueLootTable_GlobalCommonMaterials_FrozenTrail",
            [
                new StaticValueLootTableEntry(LootItemQueryType.Query, new GameplayTagQuery(["Item.Drop.Type.Salvage.Material.Common", "Item.Drop.Type.Salvage.Ascension"], [0, 1, 5, 2, 1, 1, 0, 3, 1, 1]), null, -1, new LootRange(5, 10), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.MixAllPlacements),
            ]),
        new(
            "StaticValueLootTable_IndustrialAll_FrozenTrail",
            "/Game/Pioneer/LootTables/FrozenTrail/StaticValueLootTable_IndustrialAll_FrozenTrail",
            [
                new StaticValueLootTableEntry(LootItemQueryType.Query, new GameplayTagQuery(["Item.Drop.Category.Area.Industrial", "Item.Drop.Type.Salvage.Material.Uncommon"], [0, 1, 5, 1, 2, 2, 0, 1]), null, -1, new LootRange(1, 3), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.MixAllPlacements),
                new StaticValueLootTableEntry(LootItemQueryType.Query, new GameplayTagQuery(["Item.Drop.Category.Area.Industrial", "Item.Drop.Type.Salvage.Material.Rare"], [0, 1, 5, 1, 2, 2, 0, 1]), null, -1, new LootRange(1, 1), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.MixAllPlacements),
                new StaticValueLootTableEntry(LootItemQueryType.Query, new GameplayTagQuery(["Item.Drop.Category.Area.Industrial", "Item.Drop.Type.Salvage.Recyclables"], [0, 1, 5, 1, 2, 2, 0, 1]), null, -1, new LootRange(1, 1), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.MixAllPlacements),
            ]),
        new(
            "StaticValueLootTable_MechanicalIndustrialMixedAll_FrozenTrail",
            "/Game/Pioneer/LootTables/FrozenTrail/StaticValueLootTable_MechanicalIndustrialMixedAll_FrozenTrail",
            [
                new StaticValueLootTableEntry(LootItemQueryType.Query, new GameplayTagQuery(["Item.Drop.Category.Area.Industrial", "Item.Drop.Type.Salvage.Material.Uncommon", "Item.Drop.Category.Area.Mechanical"], [0, 1, 5, 1, 4, 2, 2, 2, 0, 1, 2, 2, 1, 2]), null, -1, new LootRange(1, 3), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.RoundRobin),
                new StaticValueLootTableEntry(LootItemQueryType.Query, new GameplayTagQuery(["Item.Drop.Category.Area.Industrial", "Item.Drop.Type.Salvage.Material.Rare", "Item.Drop.Category.Area.Mechanical"], [0, 1, 5, 1, 4, 2, 2, 2, 0, 1, 2, 2, 2, 1]), null, -1, new LootRange(1, 1), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.RoundRobin),
                new StaticValueLootTableEntry(LootItemQueryType.Query, new GameplayTagQuery(["Item.Drop.Type.Salvage.Recyclables", "Item.Drop.Category.Area.Mechanical"], [0, 1, 5, 1, 2, 2, 0, 1]), null, -1, new LootRange(1, 1), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.RoundRobin),
                new StaticValueLootTableEntry(LootItemQueryType.Query, new GameplayTagQuery(["Item.Drop.Type.Salvage.Recyclables", "Item.Drop.Category.Area.Industrial"], [0, 1, 5, 1, 2, 2, 0, 1]), null, -1, new LootRange(1, 1), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.RoundRobin),
            ]),
        new(
            "StaticValueLootTable_Nature_FrozenTrail",
            "/Game/Pioneer/LootTables/FrozenTrail/StaticValueLootTable_Nature_FrozenTrail",
            [
                new StaticValueLootTableEntry(LootItemQueryType.Exact, null, "DA_Item_Salvage_Event_Item_Candleberries", -1, new LootRange(1, 3), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.RoundRobin),
                new StaticValueLootTableEntry(LootItemQueryType.Exact, null, "DA_Item_Consumable_Mushroom", -1, new LootRange(1, 2), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.RoundRobin),
                new StaticValueLootTableEntry(LootItemQueryType.Exact, null, "DA_Item_Consumable_Moss", -1, new LootRange(1, 2), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.RoundRobin),
            ]),
        new(
            "StaticValueLootTable_GlobalRecyclables",
            "/Game/Pioneer/LootTables/StaticLootTables/StaticValueLootTable_GlobalRecyclables",
            [
                new StaticValueLootTableEntry(LootItemQueryType.Query, new GameplayTagQuery(["Item.Drop.Type.Salvage.Recyclables", "Item.Drop.Category.Map.FrozenTrail", "Item.Drop.Category.Area.Exodus", "Item.Drop.Tier.High"], [0, 1, 5, 3, 1, 1, 0, 3, 1, 1, 6, 1, 2, 2, 2, 3]), null, -1, new LootRange(1, 1), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.MixAllPlacements),
            ]),
        new(
            "StaticValueLootTable_CommercialAll",
            "/Game/Pioneer/LootTables/ValueLootTables/Commercial/StaticValueLootTable_CommercialAll",
            [
                new StaticValueLootTableEntry(LootItemQueryType.Query, new GameplayTagQuery(["Item.Drop.Category.Area.Commercial", "Item.Drop.Type.Salvage.Material.Uncommon", "Item.Drop.Category.Map.FrozenTrail"], [0, 1, 5, 2, 2, 2, 0, 1, 3, 1, 2]), null, -1, new LootRange(1, 3), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.RoundRobin),
                new StaticValueLootTableEntry(LootItemQueryType.Query, new GameplayTagQuery(["Item.Drop.Category.Area.Commercial", "Item.Drop.Type.Salvage.Material.Rare", "Item.Drop.Category.Map.FrozenTrail"], [0, 1, 5, 2, 2, 2, 0, 1, 3, 1, 2]), null, -1, new LootRange(1, 1), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.RoundRobin),
                new StaticValueLootTableEntry(LootItemQueryType.Query, new GameplayTagQuery(["Item.Drop.Type.Salvage.Trinket", "Item.Drop.Category.Area.Commercial", "Item.Drop.Category.Map.FrozenTrail"], [0, 1, 5, 2, 2, 2, 0, 1, 3, 1, 2]), null, -1, new LootRange(1, 1), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.RoundRobin),
                new StaticValueLootTableEntry(LootItemQueryType.Query, new GameplayTagQuery(["Item.Drop.Category.Area.Commercial", "Item.Drop.Type.Salvage.Recyclables", "Item.Drop.Category.Map.FrozenTrail"], [0, 1, 5, 2, 2, 2, 0, 1, 3, 1, 2]), null, -1, new LootRange(1, 1), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.RoundRobin),
            ]),
        new(
            "StaticValueLootTable_OldWorldAll",
            "/Game/Pioneer/LootTables/ValueLootTables/Commercial/StaticValueLootTable_OldWorldAll",
            [
                new StaticValueLootTableEntry(LootItemQueryType.Query, new GameplayTagQuery(["Item.Drop.Category.Area.OldWorld"], [0, 1, 1, 1, 0]), null, -1, new LootRange(1, 1), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.RoundRobin),
            ]),
        new(
            "StaticValueLootTable_RareMaterialsCommercial",
            "/Game/Pioneer/LootTables/ValueLootTables/Commercial/StaticValueLootTable_RareMaterialsCommercial",
            [
                new StaticValueLootTableEntry(LootItemQueryType.Query, new GameplayTagQuery(["Item.Drop.Category.Area.Commercial", "Item.Drop.Type.Salvage.Material.Rare", "Item.Drop.Category.Map.FrozenTrail"], [0, 1, 5, 2, 2, 2, 0, 1, 3, 1, 2]), null, -1, new LootRange(1, 1), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.MixAllPlacements),
            ]),
        new(
            "StaticValueLootTable_UncommonMaterialsCommercial",
            "/Game/Pioneer/LootTables/ValueLootTables/Commercial/StaticValueLootTable_UncommonMaterialsCommercial",
            [
                new StaticValueLootTableEntry(LootItemQueryType.Query, new GameplayTagQuery(["Item.Drop.Category.Area.Commercial", "Item.Drop.Type.Salvage.Material.Uncommon", "Item.Drop.Category.Map.FrozenTrail"], [0, 1, 5, 2, 2, 2, 0, 1, 3, 1, 2]), null, -1, new LootRange(1, 3), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.MixAllPlacements),
            ]),
        new(
            "StaticValueLootTable_BasicMaterialsElectrical",
            "/Game/Pioneer/LootTables/ValueLootTables/Electrical/StaticValueLootTable_BasicMaterialsElectrical",
            [
                new StaticValueLootTableEntry(LootItemQueryType.Query, new GameplayTagQuery(["Item.Drop.Category.Area.Electrical", "Item.Drop.Type.Salvage.Material.Common", "Item.Drop.Category.Area.Technological", "Item.Drop.Category.Map.FrozenTrail"], [0, 1, 5, 2, 4, 2, 2, 2, 0, 1, 2, 2, 1, 2, 3, 1, 3]), null, -1, new LootRange(5, 10), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.RoundRobin),
            ]),
        new(
            "StaticValueLootTable_ElectricalAll",
            "/Game/Pioneer/LootTables/ValueLootTables/Electrical/StaticValueLootTable_ElectricalAll",
            [
                new StaticValueLootTableEntry(LootItemQueryType.Query, new GameplayTagQuery(["Item.Drop.Category.Area.Electrical", "Item.Drop.Type.Salvage.Material.Uncommon", "Item.Drop.Category.Map.FrozenTrail"], [0, 1, 5, 2, 2, 2, 0, 1, 3, 1, 2]), null, -1, new LootRange(2, 5), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.MixAllPlacements),
                new StaticValueLootTableEntry(LootItemQueryType.Query, new GameplayTagQuery(["Item.Drop.Category.Area.Electrical", "Item.Drop.Type.Salvage.Material.Rare", "Item.Drop.Category.Map.FrozenTrail"], [0, 1, 5, 2, 2, 2, 0, 1, 3, 1, 2]), null, -1, new LootRange(1, 1), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.RoundRobin),
                new StaticValueLootTableEntry(LootItemQueryType.Query, new GameplayTagQuery(["Item.Drop.Category.Area.Electrical", "Item.Drop.Type.Salvage.Trinket", "Item.Drop.Category.Map.FrozenTrail"], [0, 1, 5, 2, 2, 2, 0, 1, 3, 1, 2]), null, -1, new LootRange(1, 1), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.MixAllPlacements),
                new StaticValueLootTableEntry(LootItemQueryType.Query, new GameplayTagQuery(["Item.Drop.Category.Area.Electrical", "Item.Drop.Type.Salvage.Recyclables", "Item.Drop.Category.Map.FrozenTrail"], [0, 1, 5, 2, 2, 2, 0, 1, 3, 1, 2]), null, -1, new LootRange(1, 1), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.MixAllPlacements),
            ]),
        new(
            "StaticValueLootTable_RareMaterialsElectrical",
            "/Game/Pioneer/LootTables/ValueLootTables/Electrical/StaticValueLootTable_RareMaterialsElectrical",
            [
                new StaticValueLootTableEntry(LootItemQueryType.Query, new GameplayTagQuery(["Item.Drop.Category.Area.Electrical", "Item.Drop.Type.Salvage.Material.Rare", "Item.Drop.Category.Map.FrozenTrail"], [0, 1, 5, 2, 2, 2, 0, 1, 3, 1, 2]), null, -1, new LootRange(1, 1), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.RoundRobin),
            ]),
        new(
            "StaticValueLootTable_UncommonMaterialsElectrical",
            "/Game/Pioneer/LootTables/ValueLootTables/Electrical/StaticValueLootTable_UncommonMaterialsElectrical",
            [
                new StaticValueLootTableEntry(LootItemQueryType.Query, new GameplayTagQuery(["Item.Drop.Category.Area.Electrical", "Item.Drop.Type.Salvage.Material.Uncommon", "Item.Drop.Category.Map.FrozenTrail"], [0, 1, 5, 2, 2, 2, 0, 1, 3, 1, 2]), null, -1, new LootRange(2, 5), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.RoundRobin),
            ]),
        new(
            "StaticValueLootTable_EpicMaterialsExodus",
            "/Game/Pioneer/LootTables/ValueLootTables/Exodus/StaticValueLootTable_EpicMaterialsExodus",
            [
                new StaticValueLootTableEntry(LootItemQueryType.Query, new GameplayTagQuery(["Item.Drop.Category.Area.Exodus", "Item.Drop.Type.Salvage.Material.Epic", "Item.Drop.Category.Map.FrozenTrail"], [0, 1, 5, 2, 2, 2, 0, 1, 3, 1, 2]), null, -1, new LootRange(1, 1), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.RoundRobin),
            ]),
        new(
            "StaticValueLootTable_ExodusMixed",
            "/Game/Pioneer/LootTables/ValueLootTables/Exodus/StaticValueLootTable_ExodusMixed",
            [
                new StaticValueLootTableEntry(LootItemQueryType.Query, new GameplayTagQuery(["Item.Drop.Category.Area.Exodus", "Item.Drop.Type.Salvage.Material.Uncommon", "Item.Drop.Category.Map.FrozenTrail"], [0, 1, 5, 2, 2, 2, 0, 1, 3, 1, 2]), null, -1, new LootRange(1, 3), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.MixAllPlacements),
                new StaticValueLootTableEntry(LootItemQueryType.Query, new GameplayTagQuery(["Item.Drop.Category.Area.Exodus", "Item.Drop.Type.Salvage.Material.Rare", "Item.Drop.Category.Map.FrozenTrail"], [0, 1, 5, 2, 2, 2, 0, 1, 3, 1, 2]), null, -1, new LootRange(1, 1), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.MixAllPlacements),
                new StaticValueLootTableEntry(LootItemQueryType.Query, new GameplayTagQuery(["Item.Drop.Category.Area.Exodus", "Item.Drop.Type.Salvage.Material.Epic", "Item.Drop.Category.Map.FrozenTrail"], [0, 1, 5, 2, 2, 2, 0, 1, 3, 1, 2]), null, -1, new LootRange(1, 1), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.MixAllPlacements),
                new StaticValueLootTableEntry(LootItemQueryType.Query, new GameplayTagQuery(["Item.Drop.Category.Area.Exodus", "Item.Drop.Type.Salvage.Recyclables", "Item.Drop.Tier.High", "Item.Drop.Category.Map.FrozenTrail"], [0, 1, 5, 2, 2, 2, 0, 1, 3, 2, 2, 3]), null, -1, new LootRange(1, 1), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.MixAllPlacements),
            ]),
        new(
            "StaticValueLootTable_UncommonMaterialsExodus",
            "/Game/Pioneer/LootTables/ValueLootTables/Exodus/StaticValueLootTable_UncommonMaterialsExodus",
            [
                new StaticValueLootTableEntry(LootItemQueryType.Query, new GameplayTagQuery(["Item.Drop.Category.Area.Exodus", "Item.Drop.Type.Salvage.Material.Uncommon", "Item.Drop.Category.Map.FrozenTrail"], [0, 1, 5, 2, 2, 2, 0, 1, 3, 1, 2]), null, -1, new LootRange(1, 1), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.RoundRobin),
            ]),
        new(
            "StaticValueLootTable_IndustrialAll",
            "/Game/Pioneer/LootTables/ValueLootTables/Industrial/StaticValueLootTable_IndustrialAll",
            [
                new StaticValueLootTableEntry(LootItemQueryType.Query, new GameplayTagQuery(["Item.Drop.Category.Area.Industrial", "Item.Drop.Type.Salvage.Material.Uncommon", "Item.Drop.Category.Map.FrozenTrail"], [0, 1, 5, 2, 2, 2, 0, 1, 3, 1, 2]), null, -1, new LootRange(1, 3), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.MixAllPlacements),
                new StaticValueLootTableEntry(LootItemQueryType.Query, new GameplayTagQuery(["Item.Drop.Category.Area.Industrial", "Item.Drop.Type.Salvage.Material.Rare", "Item.Drop.Category.Map.FrozenTrail"], [0, 1, 5, 2, 2, 2, 0, 1, 3, 1, 2]), null, -1, new LootRange(1, 1), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.MixAllPlacements),
                new StaticValueLootTableEntry(LootItemQueryType.Query, new GameplayTagQuery(["Item.Drop.Category.Area.Industrial", "Item.Drop.Type.Salvage.Recyclables", "Item.Drop.Category.Map.FrozenTrail"], [0, 1, 5, 2, 2, 2, 0, 1, 3, 1, 2]), null, -1, new LootRange(1, 1), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.MixAllPlacements),
            ]),
        new(
            "StaticValueLootTable_RareMaterialsIndustrial",
            "/Game/Pioneer/LootTables/ValueLootTables/Industrial/StaticValueLootTable_RareMaterialsIndustrial",
            [
                new StaticValueLootTableEntry(LootItemQueryType.Query, new GameplayTagQuery(["Item.Drop.Category.Area.Industrial", "Item.Drop.Type.Salvage.Material.Rare", "Item.Drop.Category.Map.FrozenTrail"], [0, 1, 5, 2, 2, 2, 0, 1, 3, 1, 2]), null, -1, new LootRange(1, 1), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.RoundRobin),
            ]),
        new(
            "StaticValueLootTable_UncommonMaterialsIndustrial",
            "/Game/Pioneer/LootTables/ValueLootTables/Industrial/StaticValueLootTable_UncommonMaterialsIndustrial",
            [
                new StaticValueLootTableEntry(LootItemQueryType.Query, new GameplayTagQuery(["Item.Drop.Category.Area.Industrial", "Item.Drop.Type.Salvage.Material.Uncommon", "Item.Drop.Category.Map.FrozenTrail"], [0, 1, 5, 2, 2, 2, 0, 1, 3, 1, 2]), null, -1, new LootRange(1, 3), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.RoundRobin),
            ]),
        new(
            "StaticValueLootTable_BasicMaterialsMechanical",
            "/Game/Pioneer/LootTables/ValueLootTables/Mechanical/StaticValueLootTable_BasicMaterialsMechanical",
            [
                new StaticValueLootTableEntry(LootItemQueryType.Query, new GameplayTagQuery(["Item.Drop.Category.Area.Mechanical", "Item.Drop.Type.Salvage.Material.Common", "Item.Drop.Category.Area.Industrial"], [0, 1, 4, 2, 2, 2, 0, 1, 2, 2, 2, 1]), null, -1, new LootRange(5, 10), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.RoundRobin),
            ]),
        new(
            "StaticValueLootTable_MechanicalAll",
            "/Game/Pioneer/LootTables/ValueLootTables/Mechanical/StaticValueLootTable_MechanicalAll",
            [
                new StaticValueLootTableEntry(LootItemQueryType.Query, new GameplayTagQuery(["Item.Drop.Category.Area.Mechanical", "Item.Drop.Type.Salvage.Material.Uncommon", "Item.Drop.Category.Map.FrozenTrail"], [0, 1, 5, 2, 2, 2, 0, 1, 3, 1, 2]), null, -1, new LootRange(1, 3), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.MixAllPlacements),
                new StaticValueLootTableEntry(LootItemQueryType.Query, new GameplayTagQuery(["Item.Drop.Category.Area.Mechanical", "Item.Drop.Type.Salvage.Material.Rare", "Item.Drop.Category.Map.FrozenTrail"], [0, 1, 5, 2, 2, 2, 0, 1, 3, 1, 2]), null, -1, new LootRange(1, 1), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.RoundRobin),
                new StaticValueLootTableEntry(LootItemQueryType.Query, new GameplayTagQuery(["Item.Drop.Category.Area.Mechanical", "Item.Drop.Type.Salvage.Trinket", "Item.Drop.Category.Map.FrozenTrail"], [0, 1, 5, 2, 2, 2, 0, 1, 3, 1, 2]), null, -1, new LootRange(1, 1), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.MixAllPlacements),
                new StaticValueLootTableEntry(LootItemQueryType.Query, new GameplayTagQuery(["Item.Drop.Category.Area.Mechanical", "Item.Drop.Type.Salvage.Recyclables", "Item.Drop.Category.Map.FrozenTrail"], [0, 1, 5, 2, 2, 2, 0, 1, 3, 1, 2]), null, -1, new LootRange(1, 1), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.MixAllPlacements),
            ]),
        new(
            "StaticValueLootTable_RareMaterialsMechanical",
            "/Game/Pioneer/LootTables/ValueLootTables/Mechanical/StaticValueLootTable_RareMaterialsMechanical",
            [
                new StaticValueLootTableEntry(LootItemQueryType.Query, new GameplayTagQuery(["Item.Drop.Category.Area.Mechanical", "Item.Drop.Type.Salvage.Material.Rare", "Item.Drop.Category.Map.FrozenTrail"], [0, 1, 5, 2, 2, 2, 0, 1, 3, 1, 2]), null, -1, new LootRange(1, 1), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.RoundRobin),
            ]),
        new(
            "StaticValueLootTable_UncommonMaterialsMechanical",
            "/Game/Pioneer/LootTables/ValueLootTables/Mechanical/StaticValueLootTable_UncommonMaterialsMechanical",
            [
                new StaticValueLootTableEntry(LootItemQueryType.Query, new GameplayTagQuery(["Item.Drop.Category.Area.Mechanical", "Item.Drop.Type.Salvage.Material.Uncommon", "Item.Drop.Category.Map.FrozenTrail"], [0, 1, 5, 2, 2, 2, 0, 1, 3, 1, 2]), null, -1, new LootRange(1, 3), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.RoundRobin),
            ]),
        new(
            "StaticValueLootTable_MedicalMixed",
            "/Game/Pioneer/LootTables/ValueLootTables/Medical/StaticValueLootTable_MedicalMixed",
            [
                new StaticValueLootTableEntry(LootItemQueryType.Query, new GameplayTagQuery(["Item.Drop.Type.Consumable.Healing", "Item.Drop.Tier.Low"], [0, 1, 2, 2, 0, 1]), null, -1, new LootRange(1, 2), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.RoundRobin),
                new StaticValueLootTableEntry(LootItemQueryType.Query, new GameplayTagQuery(["Item.Drop.Category.Area.Medical", "Item.Drop.Type.Salvage.Material.Epic", "Item.Drop.Type.Salvage.Material.Rare", "Item.Drop.Type.Salvage.Material.Uncommon"], [0, 1, 4, 3, 2, 2, 0, 1, 2, 2, 0, 2, 2, 2, 0, 3]), null, -1, new LootRange(1, 2), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.RoundRobin),
            ]),
        new(
            "StaticValueLootTable_CivilianMixedAll",
            "/Game/Pioneer/LootTables/ValueLootTables/MixedMaterials/StaticValueLootTable_CivilianMixedAll",
            [
                new StaticValueLootTableEntry(LootItemQueryType.Query, new GameplayTagQuery(["Item.Drop.Category.Area.Residential", "Item.Drop.Type.Salvage.Material.Uncommon", "Item.Drop.Category.Area.Commercial"], [0, 1, 4, 2, 2, 2, 0, 1, 2, 2, 2, 1]), null, -1, new LootRange(1, 3), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.RoundRobin),
                new StaticValueLootTableEntry(LootItemQueryType.Query, new GameplayTagQuery(["Item.Drop.Category.Area.Residential", "Item.Drop.Type.Salvage.Material.Rare", "Item.Drop.Category.Area.Commercial"], [0, 1, 4, 2, 2, 2, 0, 1, 2, 2, 2, 1]), null, -1, new LootRange(1, 1), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.RoundRobin),
                new StaticValueLootTableEntry(LootItemQueryType.Query, new GameplayTagQuery(["Item.Drop.Category.Area.Residential", "Item.Drop.Type.Salvage.Trinket", "Item.Drop.Category.Area.Commercial"], [0, 1, 4, 2, 2, 2, 0, 1, 2, 2, 2, 1]), null, -1, new LootRange(1, 1), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.MixAllPlacements),
                new StaticValueLootTableEntry(LootItemQueryType.Query, new GameplayTagQuery(["Item.Drop.Category.Area.Residential", "Item.Drop.Type.Salvage.Recyclables", "Item.Drop.Category.Area.Commercial"], [0, 1, 4, 2, 2, 2, 0, 1, 2, 2, 2, 1]), null, -1, new LootRange(1, 1), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.RoundRobin),
            ]),
        new(
            "StaticValueLootTable_CivilianMixedMaterials",
            "/Game/Pioneer/LootTables/ValueLootTables/MixedMaterials/StaticValueLootTable_CivilianMixedMaterials",
            [
                new StaticValueLootTableEntry(LootItemQueryType.Query, new GameplayTagQuery(["Item.Drop.Category.Area.Residential", "Item.Drop.Type.Salvage.Material.Common", "Item.Drop.Category.Area.Commercial"], [0, 1, 4, 2, 2, 2, 0, 1, 2, 2, 2, 1]), null, -1, new LootRange(5, 8), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.MixAllPlacements),
                new StaticValueLootTableEntry(LootItemQueryType.Query, new GameplayTagQuery(["Item.Drop.Category.Area.Residential", "Item.Drop.Type.Salvage.Material.Uncommon", "Item.Drop.Category.Area.Commercial"], [0, 1, 4, 2, 2, 2, 0, 1, 2, 2, 2, 1]), null, -1, new LootRange(3, 5), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.MixAllPlacements),
                new StaticValueLootTableEntry(LootItemQueryType.Query, new GameplayTagQuery(["Item.Drop.Category.Area.Residential", "Item.Drop.Type.Salvage.Material.Rare", "Item.Drop.Category.Area.Commercial"], [0, 1, 4, 2, 2, 2, 0, 1, 2, 2, 2, 1]), null, -1, new LootRange(1, 1), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.MixAllPlacements),
            ]),
        new(
            "StaticValueLootTable_ElectricalTechMixedMaterials",
            "/Game/Pioneer/LootTables/ValueLootTables/MixedMaterials/StaticValueLootTable_ElectricalTechMixedMaterials",
            [
                new StaticValueLootTableEntry(LootItemQueryType.Query, new GameplayTagQuery(["Item.Drop.Category.Area.Electrical", "Item.Drop.Type.Salvage.Material.Uncommon", "Item.Drop.Category.Area.Technological", "Item.Drop.Category.Map.FrozenTrail"], [0, 1, 5, 2, 4, 2, 2, 2, 0, 1, 2, 2, 1, 2, 3, 1, 3]), null, -1, new LootRange(2, 5), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.RoundRobin),
                new StaticValueLootTableEntry(LootItemQueryType.Query, new GameplayTagQuery(["Item.Drop.Category.Area.Electrical", "Item.Drop.Type.Salvage.Material.Rare", "Item.Drop.Category.Area.Technological", "Item.Drop.Category.Map.FrozenTrail"], [0, 1, 5, 2, 4, 2, 2, 2, 0, 1, 2, 2, 2, 1, 3, 1, 3]), null, -1, new LootRange(1, 1), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.RoundRobin),
            ]),
        new(
            "StaticValueLootTable_ElectricalTechMixedMaterialsAll",
            "/Game/Pioneer/LootTables/ValueLootTables/MixedMaterials/StaticValueLootTable_ElectricalTechMixedMaterialsAll",
            [
                new StaticValueLootTableEntry(LootItemQueryType.Query, new GameplayTagQuery(["Item.Drop.Category.Area.Electrical", "Item.Drop.Type.Salvage.Material.Uncommon", "Item.Drop.Category.Area.Technological", "Item.Drop.Category.Map.FrozenTrail"], [0, 1, 5, 2, 4, 2, 2, 2, 0, 1, 2, 2, 1, 2, 3, 1, 3]), null, -1, new LootRange(2, 5), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.RoundRobin),
                new StaticValueLootTableEntry(LootItemQueryType.Query, new GameplayTagQuery(["Item.Drop.Category.Area.Electrical", "Item.Drop.Type.Salvage.Material.Rare", "Item.Drop.Category.Area.Technological", "Item.Drop.Category.Map.FrozenTrail"], [0, 1, 5, 2, 4, 2, 2, 2, 0, 1, 2, 2, 2, 1, 3, 1, 3]), null, -1, new LootRange(1, 1), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.RoundRobin),
                new StaticValueLootTableEntry(LootItemQueryType.Query, new GameplayTagQuery(["Item.Drop.Category.Area.Electrical", "Item.Drop.Type.Salvage.Trinket", "Item.Drop.Category.Area.Technological", "Item.Drop.Category.Map.FrozenTrail"], [0, 1, 5, 2, 4, 2, 2, 2, 0, 1, 2, 2, 2, 1, 3, 1, 3]), null, -1, new LootRange(1, 1), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.RoundRobin),
                new StaticValueLootTableEntry(LootItemQueryType.Query, new GameplayTagQuery(["Item.Drop.Category.Area.Electrical", "Item.Drop.Type.Salvage.Recyclables", "Item.Drop.Category.Area.Technological", "Item.Drop.Category.Map.FrozenTrail"], [0, 1, 5, 2, 4, 2, 2, 2, 0, 1, 2, 2, 2, 1, 3, 1, 3]), null, -1, new LootRange(1, 1), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.RoundRobin),
            ]),
        new(
            "StaticValueLootTable_MechanicalIndustrialMixed",
            "/Game/Pioneer/LootTables/ValueLootTables/MixedMaterials/StaticValueLootTable_MechanicalIndustrialMixed",
            [
                new StaticValueLootTableEntry(LootItemQueryType.Query, new GameplayTagQuery(["Item.Drop.Category.Area.Industrial", "Item.Drop.Type.Salvage.Material.Uncommon", "Item.Drop.Category.Area.Mechanical", "Item.Drop.Category.Map.FrozenTrail"], [0, 1, 5, 2, 4, 2, 2, 2, 0, 1, 2, 2, 1, 2, 3, 1, 3]), null, -1, new LootRange(1, 3), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.RoundRobin),
                new StaticValueLootTableEntry(LootItemQueryType.Query, new GameplayTagQuery(["Item.Drop.Category.Area.Industrial", "Item.Drop.Type.Salvage.Material.Rare", "Item.Drop.Category.Area.Mechanical", "Item.Drop.Category.Map.FrozenTrail"], [0, 1, 5, 2, 4, 2, 2, 2, 0, 1, 2, 2, 2, 1, 3, 1, 3]), null, -1, new LootRange(1, 1), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.RoundRobin),
            ]),
        new(
            "StaticValueLootTable_MechanicalIndustrialMixedAll",
            "/Game/Pioneer/LootTables/ValueLootTables/MixedMaterials/StaticValueLootTable_MechanicalIndustrialMixedAll",
            [
                new StaticValueLootTableEntry(LootItemQueryType.Query, new GameplayTagQuery(["Item.Drop.Category.Area.Industrial", "Item.Drop.Type.Salvage.Material.Uncommon", "Item.Drop.Category.Area.Mechanical", "Item.Drop.Category.Map.FrozenTrail"], [0, 1, 5, 2, 4, 2, 2, 2, 0, 1, 2, 2, 1, 2, 3, 1, 3]), null, -1, new LootRange(1, 3), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.RoundRobin),
                new StaticValueLootTableEntry(LootItemQueryType.Query, new GameplayTagQuery(["Item.Drop.Category.Area.Industrial", "Item.Drop.Type.Salvage.Material.Rare", "Item.Drop.Category.Area.Mechanical", "Item.Drop.Category.Map.FrozenTrail"], [0, 1, 5, 2, 4, 2, 2, 2, 0, 1, 2, 2, 2, 1, 3, 1, 3]), null, -1, new LootRange(1, 1), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.RoundRobin),
                new StaticValueLootTableEntry(LootItemQueryType.Query, new GameplayTagQuery(["Item.Drop.Type.Salvage.Recyclables", "Item.Drop.Category.Area.Mechanical", "Item.Drop.Category.Map.FrozenTrail"], [0, 1, 5, 2, 2, 2, 0, 1, 3, 1, 2]), null, -1, new LootRange(1, 1), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.RoundRobin),
                new StaticValueLootTableEntry(LootItemQueryType.Query, new GameplayTagQuery(["Item.Drop.Type.Salvage.Recyclables", "Item.Drop.Category.Area.Industrial", "Item.Drop.Category.Map.FrozenTrail"], [0, 1, 5, 2, 2, 2, 0, 1, 3, 1, 2]), null, -1, new LootRange(1, 1), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.RoundRobin),
            ]),
        new(
            "StaticValueLootTable_Nature",
            "/Game/Pioneer/LootTables/ValueLootTables/Nature/StaticValueLootTable_Nature",
            [
                new StaticValueLootTableEntry(LootItemQueryType.Query, new GameplayTagQuery(["Item.Drop.Category.Area.Nature"], [0, 1, 1, 1, 0]), null, -1, new LootRange(1, 1), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.RoundRobin),
            ]),
        new(
            "StaticValueLootTable_Nature_BlueGate",
            "/Game/Pioneer/LootTables/ValueLootTables/Nature/StaticValueLootTable_Nature_BlueGate",
            [
                new StaticValueLootTableEntry(LootItemQueryType.Exact, null, "DA_Item_Consumable_Lemon", -1, new LootRange(1, 1), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.RoundRobin),
                new StaticValueLootTableEntry(LootItemQueryType.Exact, null, "DA_Item_Consumable_Moss", -1, new LootRange(1, 1), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.RoundRobin),
                new StaticValueLootTableEntry(LootItemQueryType.Exact, null, "DA_Item_Consumable_Apricot", -1, new LootRange(1, 1), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.RoundRobin),
                new StaticValueLootTableEntry(LootItemQueryType.Exact, null, "DA_Item_Salvage_TorchGinger", -1, new LootRange(1, 1), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.RoundRobin),
                new StaticValueLootTableEntry(LootItemQueryType.Exact, null, "DA_Item_Throwable_VolcanicRock", -1, new LootRange(1, 1), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.RoundRobin),
                new StaticValueLootTableEntry(LootItemQueryType.Exact, null, "DA_Item_Salvage_Roots", -1, new LootRange(1, 1), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.RoundRobin),
                new StaticValueLootTableEntry(LootItemQueryType.Exact, null, "DA_Item_Salvage_Fertilizer", -1, new LootRange(1, 1), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.RoundRobin),
                new StaticValueLootTableEntry(LootItemQueryType.Exact, null, "DA_Item_Consumable_Olives", -1, new LootRange(1, 1), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.RoundRobin),
            ]),
        new(
            "StaticValueLootTable_Nature_BuriedCity",
            "/Game/Pioneer/LootTables/ValueLootTables/Nature/StaticValueLootTable_Nature_BuriedCity",
            [
                new StaticValueLootTableEntry(LootItemQueryType.Exact, null, "DA_Item_Consumable_PricklyPear", -1, new LootRange(1, 1), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.RoundRobin),
                new StaticValueLootTableEntry(LootItemQueryType.Exact, null, "DA_Item_Consumable_Agave", -1, new LootRange(1, 1), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.RoundRobin),
                new StaticValueLootTableEntry(LootItemQueryType.Exact, null, "DA_Item_Salvage_TorchGinger", -1, new LootRange(1, 1), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.RoundRobin),
                new StaticValueLootTableEntry(LootItemQueryType.Exact, null, "DA_Item_Consumable_Lemon", -1, new LootRange(1, 1), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.RoundRobin),
                new StaticValueLootTableEntry(LootItemQueryType.Exact, null, "DA_Item_Consumable_Apricot", -1, new LootRange(1, 1), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.RoundRobin),
            ]),
        new(
            "StaticValueLootTable_Nature_ColdSnap",
            "/Game/Pioneer/LootTables/ValueLootTables/Nature/StaticValueLootTable_Nature_ColdSnap",
            [
                new StaticValueLootTableEntry(LootItemQueryType.Exact, null, "DA_Item_Salvage_Event_Item_Candleberries", -1, new LootRange(1, 3), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.RoundRobin),
                new StaticValueLootTableEntry(LootItemQueryType.Exact, null, "DA_Item_Consumable_Mushroom", -1, new LootRange(1, 2), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.RoundRobin),
            ]),
        new(
            "StaticValueLootTable_Nature_RivenTides",
            "/Game/Pioneer/LootTables/ValueLootTables/Nature/StaticValueLootTable_Nature_RivenTides",
            [
                new StaticValueLootTableEntry(LootItemQueryType.Exact, new GameplayTagQuery([], [0, 1, 5, 1, 1, 0]), "DA_Item_Consumable_Mushroom", -1, new LootRange(1, 1), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.RoundRobin),
                new StaticValueLootTableEntry(LootItemQueryType.Exact, null, "DA_Item_Consumable_Moss", -1, new LootRange(1, 1), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.RoundRobin),
                new StaticValueLootTableEntry(LootItemQueryType.Exact, null, "DA_Item_Consumable_Resin", -1, new LootRange(1, 1), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.RoundRobin),
                new StaticValueLootTableEntry(LootItemQueryType.Exact, null, "DA_Item_Salvage_TorchGinger", -1, new LootRange(1, 1), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.RoundRobin),
                new StaticValueLootTableEntry(LootItemQueryType.Exact, null, "DA_Item_Consumable_Agave", -1, new LootRange(1, 1), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.RoundRobin),
                new StaticValueLootTableEntry(LootItemQueryType.Exact, null, "DA_Item_Consumable_Apricot", -1, new LootRange(1, 1), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.RoundRobin),
                new StaticValueLootTableEntry(LootItemQueryType.Exact, null, "DA_Item_Consumable_PricklyPear", -1, new LootRange(1, 1), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.RoundRobin),
                new StaticValueLootTableEntry(LootItemQueryType.Exact, null, "DA_Item_Salvage_Fertilizer", -1, new LootRange(1, 1), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.RoundRobin),
                new StaticValueLootTableEntry(LootItemQueryType.Exact, null, "DA_Item_Salvage_Roots", -1, new LootRange(1, 1), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.RoundRobin),
            ]),
        new(
            "StaticValueLootTable_Nature_StellaMontis",
            "/Game/Pioneer/LootTables/ValueLootTables/Nature/StaticValueLootTable_Nature_StellaMontis",
            [
                new StaticValueLootTableEntry(LootItemQueryType.Exact, null, "DA_Item_Salvage_TorchGinger", -1, new LootRange(1, 3), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.RoundRobin),
                new StaticValueLootTableEntry(LootItemQueryType.Exact, null, "DA_Item_Salvage_Seeds", -1, new LootRange(3, 5), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.RoundRobin),
            ]),
        new(
            "StaticValueLootTable_GlobalRecyclables",
            "/Game/Pioneer/LootTables/ValueLootTables/REcyclables/StaticValueLootTable_GlobalRecyclables",
            [
                new StaticValueLootTableEntry(LootItemQueryType.Query, new GameplayTagQuery(["Item.Drop.Type.Salvage.Recyclables", "Item.Drop.Category.Map.FrozenTrail", "Item.Drop.Category.Area.Exodus", "Item.Drop.Tier.High"], [0, 1, 5, 3, 1, 1, 0, 3, 1, 1, 6, 1, 2, 2, 2, 3]), null, -1, new LootRange(1, 1), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.MixAllPlacements),
            ]),
        new(
            "StaticValueLootTable_GlobalRecyclablesLow",
            "/Game/Pioneer/LootTables/ValueLootTables/REcyclables/StaticValueLootTable_GlobalRecyclablesLow",
            [
                new StaticValueLootTableEntry(LootItemQueryType.Query, new GameplayTagQuery(["Item.Drop.Type.Salvage.Recyclables", "Item.Drop.Tier.Low", "Item.Drop.Category.Map.FrozenTrail"], [0, 1, 5, 2, 2, 2, 0, 1, 3, 1, 2]), null, -1, new LootRange(1, 1), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.RoundRobin),
            ]),
        new(
            "StaticValueLootTable_GlobalRecyclablesMid",
            "/Game/Pioneer/LootTables/ValueLootTables/REcyclables/StaticValueLootTable_GlobalRecyclablesMid",
            [
                new StaticValueLootTableEntry(LootItemQueryType.Query, new GameplayTagQuery(["Item.Drop.Type.Salvage.Recyclables", "Item.Drop.Tier.Mid", "Item.Drop.Category.Map.FrozenTrail"], [0, 1, 5, 2, 2, 2, 0, 1, 3, 1, 2]), null, -1, new LootRange(1, 1), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.RoundRobin),
            ]),
        new(
            "StaticValueLootTable_RecyclablesCommercial",
            "/Game/Pioneer/LootTables/ValueLootTables/REcyclables/StaticValueLootTable_RecyclablesCommercial",
            [
                new StaticValueLootTableEntry(LootItemQueryType.Query, new GameplayTagQuery(["Item.Drop.Type.Salvage.Recyclables", "Item.Drop.Category.Area.Commercial", "Item.Drop.Category.Map.FrozenTrail"], [0, 1, 5, 2, 2, 2, 0, 1, 3, 1, 2]), null, -1, new LootRange(1, 1), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.MixAllPlacements),
            ]),
        new(
            "StaticValueLootTable_RecyclablesExodus_HighTier",
            "/Game/Pioneer/LootTables/ValueLootTables/REcyclables/StaticValueLootTable_RecyclablesExodus_HighTier",
            [
                new StaticValueLootTableEntry(LootItemQueryType.Query, new GameplayTagQuery(["Item.Drop.Type.Salvage.Recyclables", "Item.Drop.Category.Area.Exodus", "Item.Drop.Tier.High", "Item.Drop.Category.Map.FrozenTrail"], [0, 1, 5, 2, 2, 3, 0, 1, 2, 3, 1, 3]), null, -1, new LootRange(1, 1), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.MixAllPlacements),
            ]),
        new(
            "StaticValueLootTable_RecyclablesFabricBased",
            "/Game/Pioneer/LootTables/ValueLootTables/REcyclables/StaticValueLootTable_RecyclablesFabricBased",
            [
                new StaticValueLootTableEntry(LootItemQueryType.Exact, null, "DA_Item_Salvage_TatteredClothes", -1, new LootRange(1, 1), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.RoundRobin),
                new StaticValueLootTableEntry(LootItemQueryType.Exact, null, "DA_Item_Salvage_TornBlanket", -1, new LootRange(1, 1), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.RoundRobin),
                new StaticValueLootTableEntry(LootItemQueryType.Exact, null, "DA_Item_Salvage_SpringCushion", -1, new LootRange(1, 1), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.RoundRobin),
            ]),
        new(
            "StaticValueLootTable_RecyclablesMedical",
            "/Game/Pioneer/LootTables/ValueLootTables/REcyclables/StaticValueLootTable_RecyclablesMedical",
            [
                new StaticValueLootTableEntry(LootItemQueryType.Query, new GameplayTagQuery(["Item.Drop.Type.Salvage.Recyclables", "Item.Drop.Category.Area.Medical", "Item.Drop.Category.Map.FrozenTrail"], [0, 1, 5, 2, 2, 2, 0, 1, 3, 1, 2]), null, -1, new LootRange(1, 1), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.MixAllPlacements),
            ]),
        new(
            "StaticValueLootTable_RecyclablesResidential",
            "/Game/Pioneer/LootTables/ValueLootTables/REcyclables/StaticValueLootTable_RecyclablesResidential",
            [
                new StaticValueLootTableEntry(LootItemQueryType.Query, new GameplayTagQuery(["Item.Drop.Type.Salvage.Recyclables", "Item.Drop.Category.Area.Residential", "Item.Drop.Category.Map.FrozenTrail"], [0, 1, 5, 2, 2, 2, 0, 1, 3, 1, 2]), null, -1, new LootRange(1, 1), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.MixAllPlacements),
            ]),
        new(
            "StaticValueLootTable_RecyclablesSecurity",
            "/Game/Pioneer/LootTables/ValueLootTables/REcyclables/StaticValueLootTable_RecyclablesSecurity",
            [
                new StaticValueLootTableEntry(LootItemQueryType.Query, new GameplayTagQuery(["Item.Drop.Type.Salvage.Recyclables", "Item.Drop.Category.Area.Security", "Item.Drop.Category.Map.FrozenTrail"], [0, 1, 5, 2, 2, 2, 0, 1, 3, 1, 2]), null, -1, new LootRange(1, 1), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.MixAllPlacements),
            ]),
        new(
            "StaticValueLootTable_RecyclablesTechnological",
            "/Game/Pioneer/LootTables/ValueLootTables/REcyclables/StaticValueLootTable_RecyclablesTechnological",
            [
                new StaticValueLootTableEntry(LootItemQueryType.Query, new GameplayTagQuery(["Item.Drop.Type.Salvage.Recyclables", "Item.Drop.Category.Area.Technological", "Item.Drop.Category.Map.FrozenTrail"], [0, 1, 5, 2, 2, 2, 0, 1, 3, 1, 2]), null, -1, new LootRange(1, 1), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.RoundRobin),
            ]),
        new(
            "StaticValueLootTable_RareMaterialsResidental",
            "/Game/Pioneer/LootTables/ValueLootTables/Residential/StaticValueLootTable_RareMaterialsResidental",
            [
                new StaticValueLootTableEntry(LootItemQueryType.Query, new GameplayTagQuery(["Item.Drop.Category.Area.Residential", "Item.Drop.Type.Salvage.Material.Rare"], [0, 1, 2, 2, 0, 1]), null, -1, new LootRange(1, 1), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.MixAllPlacements),
            ]),
        new(
            "StaticValueLootTable_ResidentialAll",
            "/Game/Pioneer/LootTables/ValueLootTables/Residential/StaticValueLootTable_ResidentialAll",
            [
                new StaticValueLootTableEntry(LootItemQueryType.Query, new GameplayTagQuery(["Item.Drop.Category.Area.Residential", "Item.Drop.Type.Salvage.Material.Uncommon"], [0, 1, 4, 1, 2, 2, 0, 1]), null, -1, new LootRange(1, 3), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.RoundRobin),
                new StaticValueLootTableEntry(LootItemQueryType.Query, new GameplayTagQuery(["Item.Drop.Category.Area.Residential", "Item.Drop.Type.Salvage.Material.Rare"], [0, 1, 4, 1, 2, 2, 0, 1]), null, -1, new LootRange(1, 1), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.RoundRobin),
                new StaticValueLootTableEntry(LootItemQueryType.Query, new GameplayTagQuery(["Item.Drop.Category.Area.Residential", "Item.Drop.Type.Salvage.Trinket"], [0, 1, 2, 2, 0, 1]), null, -1, new LootRange(1, 1), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.RoundRobin),
                new StaticValueLootTableEntry(LootItemQueryType.Query, new GameplayTagQuery(["Item.Drop.Category.Area.Residential", "Item.Drop.Type.Salvage.Recyclables"], [0, 1, 2, 2, 0, 1]), null, -1, new LootRange(1, 1), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.RoundRobin),
            ]),
        new(
            "StaticValueLootTable_UncommonMaterialsResidental",
            "/Game/Pioneer/LootTables/ValueLootTables/Residential/StaticValueLootTable_UncommonMaterialsResidental",
            [
                new StaticValueLootTableEntry(LootItemQueryType.Query, new GameplayTagQuery(["Item.Drop.Category.Area.Residential", "Item.Drop.Type.Salvage.Material.Uncommon"], [0, 1, 2, 2, 0, 1]), null, -1, new LootRange(1, 3), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.MixAllPlacements),
            ]),
        new(
            "StaticValueLootTable_SecurityAll",
            "/Game/Pioneer/LootTables/ValueLootTables/Security/StaticValueLootTable_SecurityAll",
            [
                new StaticValueLootTableEntry(LootItemQueryType.Query, new GameplayTagQuery(["Item.Drop.Category.Area.Security", "Item.Drop.Type.Salvage.Material.Uncommon"], [0, 1, 4, 1, 2, 2, 0, 1]), null, -1, new LootRange(1, 3), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.RoundRobin),
                new StaticValueLootTableEntry(LootItemQueryType.Query, new GameplayTagQuery(["Item.Drop.Category.Area.Security", "Item.Drop.Type.Salvage.Material.Rare"], [0, 1, 4, 1, 2, 2, 0, 1]), null, -1, new LootRange(1, 1), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.RoundRobin),
                new StaticValueLootTableEntry(LootItemQueryType.Query, new GameplayTagQuery(["Item.Drop.Category.Area.Security", "Item.Drop.Type.Salvage.Recyclables"], [0, 1, 2, 2, 0, 1]), null, -1, new LootRange(1, 1), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.RoundRobin),
            ]),
        new(
            "StaticValueLootTable_SecurityMaterials",
            "/Game/Pioneer/LootTables/ValueLootTables/Security/StaticValueLootTable_SecurityMaterials",
            [
                new StaticValueLootTableEntry(LootItemQueryType.Query, new GameplayTagQuery(["Item.Drop.Category.Area.Security", "Item.Drop.Type.Salvage.Material.Uncommon"], [0, 1, 4, 1, 2, 2, 0, 1]), null, -1, new LootRange(1, 3), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.MixAllPlacements),
                new StaticValueLootTableEntry(LootItemQueryType.Query, new GameplayTagQuery(["Item.Drop.Category.Area.Security", "Item.Drop.Type.Salvage.Material.Rare"], [0, 1, 4, 1, 2, 2, 0, 1]), null, -1, new LootRange(1, 1), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.MixAllPlacements),
            ]),
        new(
            "StaticValueLootTable_ARC",
            "/Game/Pioneer/LootTables/ValueLootTables/StaticValueLootTable_ARC",
            [
                new StaticValueLootTableEntry(LootItemQueryType.Query, new GameplayTagQuery(["Item.Drop.Category.Area.ARC", "Item.Drop.Enemy", "Item.Drop.Type.Recipe", "Item.Drop.Category.Area.Frigate", "Item.Drop.Category.Area.Collector"], [0, 1, 5, 2, 1, 1, 0, 3, 4, 1, 2, 3, 4]), null, -1, new LootRange(1, 1), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.RoundRobin),
            ]),
        new(
            "StaticValueLootTable_BuriedTresureSingleItems",
            "/Game/Pioneer/LootTables/ValueLootTables/StaticValueLootTable_BuriedTresureSingleItems",
            [
                new StaticValueLootTableEntry(LootItemQueryType.Query, new GameplayTagQuery(["Item.Drop.Type.Salvage.Trinket"], [0, 1, 1, 1, 0]), null, -1, new LootRange(1, 1), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.RoundRobin),
                new StaticValueLootTableEntry(LootItemQueryType.Query, new GameplayTagQuery(["Item.Drop.Rarity.01Common", "Item.Drop.Type.Equipment.Weapon.Firearm"], [0, 1, 2, 2, 0, 1]), null, -1, new LootRange(1, 1), new LootRange(20, 60), new LootRange(60, 65), LootDistributionMethod.RoundRobin),
                new StaticValueLootTableEntry(LootItemQueryType.Query, new GameplayTagQuery(["Item.Drop.Type.Salvage.Material", "Item.Drop.Type.Salvage.Key", "Item.Drop.Category.Area.Nature", "Item.Drop.Category.Map.FrozenTrail"], [0, 1, 5, 2, 1, 1, 0, 3, 3, 1, 2, 3]), null, -1, new LootRange(1, 1), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.RoundRobin),
                new StaticValueLootTableEntry(LootItemQueryType.Exact, null, "DA_Item_Salvage_RedCoralJewelry_01", -1, new LootRange(1, 1), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.RoundRobin),
            ]),
        new(
            "StaticValueLootTable_GlobalCommonMaterials",
            "/Game/Pioneer/LootTables/ValueLootTables/StaticValueLootTable_GlobalCommonMaterials",
            [
                new StaticValueLootTableEntry(LootItemQueryType.Query, new GameplayTagQuery(["Item.Drop.Type.Salvage.Material.Common", "Item.Drop.Category.Map.FrozenTrail", "Item.Drop.Type.Salvage.Ascension"], [0, 1, 5, 2, 1, 1, 0, 3, 2, 1, 2]), null, -1, new LootRange(5, 10), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.MixAllPlacements),
            ]),
        new(
            "StaticValueLootTable_GlobalEpicMaterials",
            "/Game/Pioneer/LootTables/ValueLootTables/StaticValueLootTable_GlobalEpicMaterials",
            [
                new StaticValueLootTableEntry(LootItemQueryType.Query, new GameplayTagQuery(["Item.Drop.Type.Salvage.Material.Epic", "Item.Drop.Category.Map.FrozenTrail", "Item.Drop.Category.Area.ARC", "Item.Drop.Type.Salvage.Ascension"], [0, 1, 5, 2, 1, 1, 0, 3, 3, 1, 2, 3]), null, -1, new LootRange(1, 1), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.RoundRobin),
            ]),
        new(
            "StaticValueLootTable_GlobalOutpostMaterials",
            "/Game/Pioneer/LootTables/ValueLootTables/StaticValueLootTable_GlobalOutpostMaterials",
            [
                new StaticValueLootTableEntry(LootItemQueryType.Exact, new GameplayTagQuery(["Item.Drop.Type.Salvage.Material.OutpostMaterial"], [0, 1, 1, 1, 0]), "DA_Item_Salvage_Paint", -1, new LootRange(1, 1), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.RoundRobin),
                new StaticValueLootTableEntry(LootItemQueryType.Exact, null, "DA_Item_Salvage_SheetMetal", -1, new LootRange(2, 5), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.RoundRobin),
                new StaticValueLootTableEntry(LootItemQueryType.Exact, null, "DA_Item_Salvage_Planks", -1, new LootRange(2, 5), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.RoundRobin),
            ]),
        new(
            "StaticValueLootTable_GlobalRareMaterials",
            "/Game/Pioneer/LootTables/ValueLootTables/StaticValueLootTable_GlobalRareMaterials",
            [
                new StaticValueLootTableEntry(LootItemQueryType.Query, new GameplayTagQuery(["Item.Drop.Type.Salvage.Material.Rare", "Item.Drop.Category.Map.FrozenTrail", "Item.Drop.Type.Salvage.Ascension"], [0, 1, 5, 2, 1, 1, 0, 3, 2, 1, 2]), null, -1, new LootRange(1, 1), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.RoundRobin),
            ]),
        new(
            "StaticValueLootTable_GlobalUncommonMaterials",
            "/Game/Pioneer/LootTables/ValueLootTables/StaticValueLootTable_GlobalUncommonMaterials",
            [
                new StaticValueLootTableEntry(LootItemQueryType.Query, new GameplayTagQuery(["Item.Drop.Type.Salvage.Material.Uncommon", "Item.Drop.Category.Map.FrozenTrail", "Item.Drop.Type.Salvage.Ascension"], [0, 1, 5, 2, 1, 1, 0, 3, 2, 1, 2]), null, -1, new LootRange(1, 3), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.RoundRobin),
            ]),
        new(
            "StaticValueLootTable_WinterEvent_ColdSnap",
            "/Game/Pioneer/LootTables/ValueLootTables/StaticValueLootTable_WinterEvent_ColdSnap",
            [
                new StaticValueLootTableEntry(LootItemQueryType.Exact, null, "DA_Item_Salvage_CandleHolder", -1, new LootRange(1, 1), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.MixAllPlacements),
                new StaticValueLootTableEntry(LootItemQueryType.Exact, null, "DA_Item_Salvage_Trinket_BurntoutCandles", -1, new LootRange(1, 1), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.MixAllPlacements),
                new StaticValueLootTableEntry(LootItemQueryType.Exact, null, "DA_Item_Salvage_Trinket_LightBulb", -1, new LootRange(1, 1), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.MixAllPlacements),
            ]),
        new(
            "StaticValueLootTable_RareMaterialsTechnological",
            "/Game/Pioneer/LootTables/ValueLootTables/Technological/StaticValueLootTable_RareMaterialsTechnological",
            [
                new StaticValueLootTableEntry(LootItemQueryType.Query, new GameplayTagQuery(["Item.Drop.Category.Area.Technological", "Item.Drop.Type.Salvage.Material.Rare"], [0, 1, 2, 2, 0, 1]), null, -1, new LootRange(1, 1), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.RoundRobin),
            ]),
        new(
            "StaticValueLootTable_UncommonMaterialsTechnological",
            "/Game/Pioneer/LootTables/ValueLootTables/Technological/StaticValueLootTable_UncommonMaterialsTechnological",
            [
                new StaticValueLootTableEntry(LootItemQueryType.Query, new GameplayTagQuery(["Item.Drop.Category.Area.Technological", "Item.Drop.Type.Salvage.Material.Uncommon"], [0, 1, 2, 2, 0, 1]), null, -1, new LootRange(1, 3), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.RoundRobin),
            ]),
        new(
            "StaticValueLootTable_GlobalTrinkets",
            "/Game/Pioneer/LootTables/ValueLootTables/Trinkets/StaticValueLootTable_GlobalTrinkets",
            [
                new StaticValueLootTableEntry(LootItemQueryType.Query, new GameplayTagQuery(["Item.Drop.Type.Salvage.Trinket", "Item.Drop.Category.Area.Nature"], [0, 1, 5, 2, 1, 1, 0, 3, 1, 1]), null, -1, new LootRange(1, 1), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.MixAllPlacements),
            ]),
        new(
            "StaticValueLootTable_GlobalTrinketsHigh",
            "/Game/Pioneer/LootTables/ValueLootTables/Trinkets/StaticValueLootTable_GlobalTrinketsHigh",
            [
                new StaticValueLootTableEntry(LootItemQueryType.Query, new GameplayTagQuery(["Item.Drop.Type.Salvage.Trinket", "Item.Drop.Tier.High", "Item.Drop.Category.Area.Nature"], [0, 1, 5, 2, 2, 2, 0, 1, 3, 1, 2]), null, -1, new LootRange(1, 1), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.MixAllPlacements),
            ]),
        new(
            "StaticValueLootTable_GlobalTrinketsLow",
            "/Game/Pioneer/LootTables/ValueLootTables/Trinkets/StaticValueLootTable_GlobalTrinketsLow",
            [
                new StaticValueLootTableEntry(LootItemQueryType.Query, new GameplayTagQuery(["Item.Drop.Type.Salvage.Trinket", "Item.Drop.Tier.Low", "Item.Drop.Category.Area.Nature"], [0, 1, 5, 2, 2, 2, 0, 1, 3, 1, 2]), null, -1, new LootRange(1, 1), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.MixAllPlacements),
            ]),
        new(
            "StaticValueLootTable_GlobalTrinketsMid",
            "/Game/Pioneer/LootTables/ValueLootTables/Trinkets/StaticValueLootTable_GlobalTrinketsMid",
            [
                new StaticValueLootTableEntry(LootItemQueryType.Query, new GameplayTagQuery(["Item.Drop.Type.Salvage.Trinket", "Item.Drop.Tier.Mid", "Item.Drop.Category.Area.Nature"], [0, 1, 5, 2, 2, 2, 0, 1, 3, 1, 2]), null, -1, new LootRange(1, 1), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.MixAllPlacements),
            ]),
        new(
            "StaticValueLootTable_TrinketsCommercial",
            "/Game/Pioneer/LootTables/ValueLootTables/Trinkets/StaticValueLootTable_TrinketsCommercial",
            [
                new StaticValueLootTableEntry(LootItemQueryType.Query, new GameplayTagQuery(["Item.Drop.Type.Salvage.Trinket", "Item.Drop.Category.Area.Commercial"], [0, 1, 4, 1, 2, 2, 0, 1]), null, -1, new LootRange(1, 1), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.MixAllPlacements),
            ]),
        new(
            "StaticValueLootTable_TrinketsOldWorld",
            "/Game/Pioneer/LootTables/ValueLootTables/Trinkets/StaticValueLootTable_TrinketsOldWorld",
            [
                new StaticValueLootTableEntry(LootItemQueryType.Query, new GameplayTagQuery(["Item.Drop.Type.Salvage.Trinket", "Item.Drop.Category.Area.OldWorld", "Item.Drop.Tier.High"], [0, 1, 5, 2, 2, 2, 0, 1, 3, 1, 2]), null, -1, new LootRange(1, 1), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.RoundRobin),
            ]),
        new(
            "StaticValueLootTable_TrinketsResidential",
            "/Game/Pioneer/LootTables/ValueLootTables/Trinkets/StaticValueLootTable_TrinketsResidential",
            [
                new StaticValueLootTableEntry(LootItemQueryType.Query, new GameplayTagQuery(["Item.Drop.Type.Salvage.Trinket", "Item.Drop.Category.Area.Residential"], [0, 1, 2, 2, 0, 1]), null, -1, new LootRange(1, 1), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.MixAllPlacements),
            ]),
    ];
}
