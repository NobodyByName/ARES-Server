namespace Ares.Structs.Game.Loot;

/// <summary>
/// Loot container data assets (<c>ULootContainerDataAsset</c>, assets <c>DA_LootContainer_*</c>): the socket tag they
/// serve, the container classes to spawn and the members that tune them. The container repository registers an asset
/// under its own tag, and only when its availability is <see cref="RegisteredAvailability"/>.
/// </summary>
public static class LootContainerDataAssetMembers
{
    /// <summary>Script path of the data asset class.</summary>
    public const string ClassPath = "/Script/Angelscript.LootContainerDataAsset";

    /// <summary>Script class name of the data asset class.</summary>
    public const string ScriptClass = "ULootContainerDataAsset";

    /// <summary>Name prefix of every loot container data asset.</summary>
    public const string AssetPrefix = "DA_LootContainer_";

    /// <summary>The socket tag the asset serves (<c>FGameplayTag</c>).</summary>
    public const string LootContainerTag = "LootContainerTag";

    /// <summary>The container classes to pick from (<c>TArray&lt;TSubclassOf&gt;</c>).</summary>
    public const string ContainerClasses = "ContainerClasses";

    /// <summary>Whether the asset ignores <see cref="MaterialOverrideContainerClasses"/>.</summary>
    public const string IgnoreMaterialOverrides = "bIgnoreMaterialOverrides";

    /// <summary>Container classes by material instance of the socketed prop (<c>TMap&lt;TSoftObjectPtr, FLootSocketContainers&gt;</c>).</summary>
    public const string MaterialOverrideContainerClasses = "MaterialOverrideContainerClasses";

    /// <summary>Member of <c>FLootSocketContainers</c> holding the classes of one material.</summary>
    public const string VariantContainerClasses = "ContainerClasses";

    /// <summary>Limit on the containers of the asset per group.</summary>
    public const string MaxContainersPerGroup = "MaxContainersPerGroup";

    /// <summary>The asset's availability (<see cref="AvailabilityEnum"/>).</summary>
    public const string DevelopmentAvailability = "DevelopmentAvailability";

    /// <summary>Enum of <see cref="DevelopmentAvailability"/>.</summary>
    public const string AvailabilityEnum = "EDevelopmentAvailability";

    /// <summary>The availability the repository registers outside development mode.</summary>
    public const string RegisteredAvailability = "UserFacing";

    /// <summary>
    /// <see cref="DevelopmentAvailability"/> of an asset that sets none: entry 1 of <see cref="AvailabilityEnum"/>
    /// (<c>DeveloperOnly</c>), so such an asset is not registered.
    /// </summary>
    public const byte DefaultAvailability = 1;

    /// <summary><see cref="IgnoreMaterialOverrides"/> of an asset that sets none.</summary>
    public const bool DefaultIgnoreMaterialOverrides = false;
}

/// <summary>
/// Value loot tables (<c>UStaticValueLootTableDataAsset</c>, assets <c>StaticValueLootTable_*</c>) and the structs
/// their entries are made of.
/// </summary>
public static class StaticValueLootTableMembers
{
    /// <summary>Script path of the table class.</summary>
    public const string ClassPath = "/Script/Angelscript.StaticValueLootTableDataAsset";

    /// <summary>Name prefix of every value loot table.</summary>
    public const string AssetPrefix = "StaticValueLootTable_";

    /// <summary>The table's entries (<c>TArray&lt;FStaticValueLootTableEntry&gt;</c>).</summary>
    public const string Entries = "Entries";

    /// <summary><c>FStaticValueLootTableEntry.ItemQuery</c> (<c>FStaticLooTableItemQuery</c>).</summary>
    public const string ItemQuery = "ItemQuery";

    /// <summary><c>FStaticValueLootTableEntry.QualityLevel</c>.</summary>
    public const string QualityLevel = "QualityLevel";

    /// <summary><c>FStaticValueLootTableEntry.StackSize</c> (<c>FInt32Interval</c>).</summary>
    public const string StackSize = "StackSize";

    /// <summary><c>FStaticValueLootTableEntry.DurabilityConfig</c> (<c>FLootTableDurabilityConfig</c>).</summary>
    public const string DurabilityConfig = "DurabilityConfig";

    /// <summary><c>FStaticValueLootTableEntry.DistributionMethod</c> (<see cref="DistributionEnum"/>).</summary>
    public const string DistributionMethod = "DistributionMethod";

    /// <summary><c>FStaticLooTableItemQuery.ItemQueryType</c> (<see cref="QueryTypeEnum"/>).</summary>
    public const string ItemQueryType = "ItemQueryType";

    /// <summary><c>FStaticLooTableItemQuery.ItemQuery</c> (<c>FGameplayTagQuery</c> over item drop tags).</summary>
    public const string TagQuery = "ItemQuery";

    /// <summary><c>FStaticLooTableItemQuery.ExactItem</c> (an item data asset).</summary>
    public const string ExactItem = "ExactItem";

    /// <summary><c>FLootTableDurabilityConfig.DurabilityRange</c>.</summary>
    public const string DurabilityRange = "DurabilityRange";

    /// <summary><c>FLootTableDurabilityConfig.DurabilityCapRange</c>.</summary>
    public const string DurabilityCapRange = "DurabilityCapRange";

    /// <summary>Enum of <see cref="ItemQueryType"/>; its names are those of <see cref="LootItemQueryType"/>.</summary>
    public const string QueryTypeEnum = "EStaticLootTableItemQueryType";

    /// <summary>Enum of <see cref="DistributionMethod"/>; its names are those of <see cref="LootDistributionMethod"/>.</summary>
    public const string DistributionEnum = "ELootDistributionMethod";
}

/// <summary>Skill loot tables (<c>UPlayerSkillLootTableDataAsset</c>) and the structs they are made of.</summary>
public static class SkillLootTableMembers
{
    /// <summary>Script path of the table class.</summary>
    public const string ClassPath = "/Script/Angelscript.PlayerSkillLootTableDataAsset";

    /// <summary>
    /// The table's groups by skill tag (<c>TMap&lt;FGameplayTag, FPlayerSkillLootTableContainerGroups&gt;</c>), and the
    /// groups of a skill (<c>FPlayerSkillLootTableContainerGroups.Entries</c>).
    /// </summary>
    public const string Entries = "Entries";

    /// <summary><c>FPlayerSkillLootTableContainerGroupEntry.ContainerQuery</c> (<c>FGameplayTagQuery</c>).</summary>
    public const string ContainerQuery = "ContainerQuery";

    /// <summary><c>FPlayerSkillLootTableContainerGroupEntry.Items</c>.</summary>
    public const string Items = "Items";

    /// <summary><c>FPlayerSkillLootTableItemEntry.Weight</c>.</summary>
    public const string Weight = "Weight";

    /// <summary><c>FPlayerSkillLootTableItemEntry.EntryType</c> (<see cref="EntryTypeEnum"/>).</summary>
    public const string EntryType = "EntryType";

    /// <summary><c>FPlayerSkillLootTableItemEntry.ItemDataAsset</c>.</summary>
    public const string ItemDataAsset = "ItemDataAsset";

    /// <summary><c>FPlayerSkillLootTableItemEntry.StackSize</c> (<c>FInt32Interval</c>).</summary>
    public const string StackSize = "StackSize";

    /// <summary><c>FPlayerSkillLootTableItemEntry.QualityLevel</c>.</summary>
    public const string QualityLevel = "QualityLevel";

    /// <summary>Enum of <see cref="EntryType"/>.</summary>
    public const string EntryTypeEnum = "EPlayerSkillLootTableItemEntryType";

    /// <summary>The <see cref="EntryTypeEnum"/> entry that names an item; the others draw nothing.</summary>
    public const string ItemEntry = "Item";
}

/// <summary>Item classes whose assets loot tables name.</summary>
public static class LootItemAssetClasses
{
    /// <summary>Script paths of the item data asset classes.</summary>
    public static IReadOnlyList<string> ClassPaths { get; } =
        ["/Script/Angelscript.ItemDataAsset", "/Script/Angelscript.AugmentItemDataAsset"];
}

/// <summary>The loot container's item container component (<c>UItemContainerComponent</c>): its slot count.</summary>
public static class ItemContainerComponentMembers
{
    /// <summary>Script class name.</summary>
    public const string ScriptClass = "UItemContainerComponent";

    /// <summary>Slot count of the container.</summary>
    public const string ItemLimit = "ItemLimit";
}

/// <summary>Layout facts shared by the loot data assets.</summary>
public static class LootDataAssets
{
    /// <summary>Bytes closing a loot data asset's export after its properties.</summary>
    public const int ExportTrailer = 4;
}
