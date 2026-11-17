using Ares.Structs.Core.Assets;

namespace Ares.Structs.Game.Inventory;

/// <summary>
/// Property chains of the item data asset classes, as their cooked unversioned property blocks index them (each
/// class's own members first, then its super class's), and of the structs they hold.
/// </summary>
public static class ItemAssetSchemas
{
    /// <summary>Script path of <see cref="ItemDataAsset"/>.</summary>
    public const string ItemDataAssetClass = "/Script/Angelscript.ItemDataAsset";

    /// <summary>Script path of <see cref="AugmentItemDataAsset"/>.</summary>
    public const string AugmentItemDataAssetClass = "/Script/Angelscript.AugmentItemDataAsset";

    /// <summary>Script path of <see cref="ItemWithQualityRedirectorAsset"/>.</summary>
    public const string ItemWithQualityRedirectorAssetClass = "/Script/Angelscript.ItemWithQualityRedirectorAsset";

    /// <summary>Script path of <see cref="WeaponRedirectorAsset"/>.</summary>
    public const string WeaponRedirectorAssetClass = "/Script/Angelscript.WeaponRedirectorAsset";

    /// <summary>Script path of <see cref="UiGameplayItemMetaDataItem"/>.</summary>
    public const string UiGameplayItemMetaDataItemClass = "/Script/Angelscript.UIGameplayItemMetaDataItem";

    /// <summary><c>FPickupAmountSettings</c>: how many items a pickup gives.</summary>
    public static StructSchema PickupAmountSettings { get; } = new("PickupAmountSettings",
    [
        new PropertyField("Type", PropertyType.ByteEnum),
        new PropertyField("Amount", PropertyType.Int32),
        new PropertyField("MinAmount", PropertyType.Int32),
        new PropertyField("MaxAmount", PropertyType.Int32),
    ]);

    /// <summary><c>FContextActionDefinition</c>: an action the item offers in menus.</summary>
    public static StructSchema ContextActionDefinition { get; } = new("ContextActionDefinition",
    [
        new PropertyField("ActionName", PropertyType.Name),
        new PropertyField("EnhancedInputAction", PropertyType.Object),
        new PropertyField("Modifier", PropertyType.ByteEnum),
        new PropertyField("ActionLiteral", PropertyType.Text),
        new PropertyField("bToggleable", PropertyType.Bool),
        new PropertyField("PrimaryTargetText", PropertyType.Text),
        new PropertyField("SecondaryTargetText", PropertyType.Text),
        new PropertyField("Availability", PropertyType.ByteEnum),
        new PropertyField("ActionContext", PropertyType.Object),
        new PropertyField("ExtraText", PropertyType.Text),
    ]);

    /// <summary><c>FItemUseEffect</c>: effects played when the item is used.</summary>
    public static StructSchema ItemUseEffect { get; } = new("ItemUseEffect",
    [
        new PropertyField("PositionSource", PropertyType.ByteEnum),
        new PropertyField("EffectDataAsset", PropertyType.StructOf(EngineStructs.SoftObjectPath)),
        new PropertyField("ResolvedEffectDataAsset", PropertyType.Object),
        new PropertyField("UseEndedEffectDataAsset", PropertyType.StructOf(EngineStructs.SoftObjectPath)),
        new PropertyField("ResolvedUseEndedEffectDataAsset", PropertyType.Object),
        new PropertyField("LocalPlayerEffectDataAssetOverride", PropertyType.StructOf(EngineStructs.SoftObjectPath)),
        new PropertyField("ResolvedLocalPlayerEffectDataAssetOverride", PropertyType.Object),
        new PropertyField("ItemEffectSocketName", PropertyType.Name),
    ]);

    /// <summary><c>FStaticLootPlacementSpecification</c>: a loot socket tag the item may spawn at and the tags under it to skip.</summary>
    public static StructSchema StaticLootPlacementSpecification { get; } = new("StaticLootPlacementSpecification",
    [
        new PropertyField("SocketTag", PropertyType.StructOf(EngineStructs.GameplayTag)),
        new PropertyField("ExcludeTags", PropertyType.StructOf(EngineStructs.GameplayTagContainer)),
    ]);

    /// <summary><c>UDataAsset</c>: its native class.</summary>
    public static StructSchema DataAsset { get; } = new("DataAsset",
    [
        new PropertyField("NativeClass", PropertyType.Object),
    ]);

    /// <summary><c>UOptionalPrimaryDataAsset</c>: an optional primary asset type override.</summary>
    public static StructSchema OptionalPrimaryDataAsset { get; } = new("OptionalPrimaryDataAsset",
    [
        new PropertyField("OverridePrimaryAssetType", PropertyType.StructOf(EngineStructs.PrimaryAssetType)),
    ], DataAsset);

    /// <summary><c>UEmbarkStreamingDataAsset</c>: streaming references and filter.</summary>
    public static StructSchema EmbarkStreamingDataAsset { get; } = new("EmbarkStreamingDataAsset",
    [
        new PropertyField("LoadedAssetPtrs", PropertyType.MapOf(PropertyType.StructOf(EngineStructs.SoftObjectPath), PropertyType.Object)),
        new PropertyField("bEnableSoftReferenceScanning", PropertyType.Bool),
        new PropertyField("StreamingFilter", PropertyType.ByteEnum),
    ], OptionalPrimaryDataAsset);

    /// <summary><c>UItemDataAssetBaseNative</c>: stack size, item class and inventory item type tags.</summary>
    public static StructSchema ItemDataAssetBaseNative { get; } = new("ItemDataAssetBaseNative",
    [
        new PropertyField("MaxStackCount", PropertyType.Int32),
        new PropertyField("ItemClass", PropertyType.StructOf(EngineStructs.SoftClassPath)),
        new PropertyField("InventoryItemTypeTags", PropertyType.StructOf(EngineStructs.GameplayTagContainer)),
        new PropertyField("ResolvedItemClass", PropertyType.Object),
    ], EmbarkStreamingDataAsset);

    /// <summary><c>UItemDataAssetBase</c> (script): persistence asset, asset id override, tags and availability.</summary>
    public static StructSchema ItemDataAssetBase { get; } = new("ItemDataAssetBase",
    [
        new PropertyField("PersistenceDataAsset", PropertyType.Object),
        new PropertyField("bOverrideItemAssetId", PropertyType.Bool),
        new PropertyField("OverrideItemAssetId", PropertyType.Int64),
        new PropertyField("Tags", PropertyType.StructOf(EngineStructs.GameplayTagContainer)),
        new PropertyField("QuestTags", PropertyType.StructOf(EngineStructs.GameplayTagContainer)),
        new PropertyField("DevelopmentAvailability", PropertyType.ByteEnum),
    ], ItemDataAssetBaseNative);

    /// <summary><c>UItemDataAsset</c> (script): the item definition.</summary>
    public static StructSchema ItemDataAsset { get; } = new("ItemDataAsset",
    [
        new PropertyField("bIsUpgradedVariation", PropertyType.Bool),
        new PropertyField("BaseQualityLevel", PropertyType.Int32),
        new PropertyField("ItemActorClass", PropertyType.StructOf(EngineStructs.SoftClassPath)),
        new PropertyField("Classifications", PropertyType.StructOf(EngineStructs.GameplayTagContainer)),
        new PropertyField("bEnablesZoom", PropertyType.Bool),
        new PropertyField("InputReleasedEndUseReason", PropertyType.ByteEnum),
        new PropertyField("bEnablesPrediction", PropertyType.Bool),
        new PropertyField("MaterialOverrides", PropertyType.ArrayOf(PropertyType.StructOf(EngineStructs.SoftObjectPath))),
        new PropertyField("DefaultPickupMesh", PropertyType.StructOf(EngineStructs.SoftObjectPath)),
        new PropertyField("AdditionalPickupMeshes", PropertyType.ArrayOf(PropertyType.StructOf(EngineStructs.SoftObjectPath))),
        new PropertyField("PickupMeshScale", PropertyType.StructOf(EngineStructs.Vector)),
        new PropertyField("PickupAmountSettings", PropertyType.StructOf(PickupAmountSettings)),
        new PropertyField("Weight", PropertyType.Double),
        new PropertyField("DialogueObjectTag", PropertyType.StructOf(EngineStructs.GameplayTag)),
        new PropertyField("ItemCategory", PropertyType.ByteEnum),
        new PropertyField("InventoryItemTypeTag", PropertyType.StructOf(EngineStructs.GameplayTag)),
        new PropertyField("Purpose", PropertyType.ByteEnum),
        new PropertyField("DropTags", PropertyType.StructOf(EngineStructs.GameplayTagContainer)),
        new PropertyField("StaticLootAvoidGroups", PropertyType.StructOf(EngineStructs.GameplayTagContainer)),
        new PropertyField("CanDrop", PropertyType.Bool),
        new PropertyField("bConsumedOnUse", PropertyType.Bool),
        new PropertyField("bConsumedOnUseSecondary", PropertyType.Bool),
        new PropertyField("bSecondaryUsableWhileConsumed", PropertyType.Bool),
        new PropertyField("bMustBeUniqueInInventory", PropertyType.Bool),
        new PropertyField("bNonTransferable", PropertyType.Bool),
        new PropertyField("UnequipTime", PropertyType.Double),
        new PropertyField("UnequipTimeToItems", PropertyType.Double),
        new PropertyField("EquipTime", PropertyType.Double),
        new PropertyField("ReEquipTimeFromItem", PropertyType.Double),
        new PropertyField("DurabilityConsumedOnUse", PropertyType.Double),
        new PropertyField("DurabilityConsumedPerSecondWhileUsed", PropertyType.Double),
        new PropertyField("DurabilityConsumedOnSecondaryUse", PropertyType.Double),
        new PropertyField("DurabilityConsumedPerSecondWhileSecondaryUsed", PropertyType.Double),
        new PropertyField("bHasCustomDurability", PropertyType.Bool),
        new PropertyField("ContextActions", PropertyType.ArrayOf(PropertyType.StructOf(ContextActionDefinition))),
        new PropertyField("DisplayTags", PropertyType.StructOf(EngineStructs.GameplayTagContainer)),
        new PropertyField("OnUseEffect", PropertyType.StructOf(ItemUseEffect)),
        new PropertyField("OnUseSecondaryEffect", PropertyType.StructOf(ItemUseEffect)),
        new PropertyField("OnTriggerEffect", PropertyType.StructOf(ItemUseEffect)),
        new PropertyField("PickupEffects", PropertyType.Object),
        new PropertyField("DropEffects", PropertyType.Object),
        new PropertyField("ItemFoleySound", PropertyType.StructOf(EngineStructs.SoftObjectPath)),
        new PropertyField("ItemAnimationContainer", PropertyType.Object),
        new PropertyField("SkinSlot", PropertyType.Object),
        new PropertyField("ProjectileSpeedMultiplier", PropertyType.Double),
        new PropertyField("WeaponVisualsOverride", PropertyType.StructOf(EngineStructs.SoftObjectPath)),
        new PropertyField("WeaponQualityVisual", PropertyType.StructOf(EngineStructs.SoftObjectPath)),
        new PropertyField("StaticLootPlacement", PropertyType.ArrayOf(PropertyType.StructOf(StaticLootPlacementSpecification))),
    ], ItemDataAssetBase);

    /// <summary><c>FAugmentIntegratedItem</c>: an item an augment carries built in.</summary>
    public static StructSchema AugmentIntegratedItem { get; } = new("AugmentIntegratedItem",
    [
        new PropertyField("ItemDataAsset", PropertyType.Object),
        new PropertyField("InitialAmount", PropertyType.Int32),
        new PropertyField("MaxAmount", PropertyType.Int32),
        new PropertyField("RegenerationInterval", PropertyType.Double),
    ]);

    /// <summary><c>UAugmentItemDataAsset</c> (script): an augment's loadout frame, skills and integrated items over <see cref="ItemDataAsset"/>.</summary>
    public static StructSchema AugmentItemDataAsset { get; } = new("AugmentItemDataAsset",
    [
        new PropertyField("LoadoutFrameDataAsset", PropertyType.Object),
        new PropertyField("AttachedSkills", PropertyType.ArrayOf(PropertyType.Object)),
        new PropertyField("FallbackSkillPersistenceDataAsset", PropertyType.Object),
        new PropertyField("IntegratedItems", PropertyType.ArrayOf(PropertyType.StructOf(AugmentIntegratedItem))),
        new PropertyField("MaxWeight", PropertyType.Double),
    ], ItemDataAsset);

    /// <summary>
    /// <c>UItemWithQualityRedirectorAsset</c> (script): one quality level of an item, with its own persistence asset. Only
    /// these members of its chain are known.
    /// </summary>
    public static StructSchema ItemWithQualityRedirectorAsset { get; } = new("ItemWithQualityRedirectorAsset",
    [
        new PropertyField("PersistenceDataAsset", PropertyType.Object),
        new PropertyField("ActualItem", PropertyType.Object),
        new PropertyField("QualityLevel", PropertyType.Int32),
    ]);

    /// <summary><c>UWeaponRedirectorAsset</c> (script): a weapon's quality level; it declares no members of its own.</summary>
    public static StructSchema WeaponRedirectorAsset { get; } = new("WeaponRedirectorAsset", [], ItemWithQualityRedirectorAsset);

    /// <summary>
    /// <c>UUIGameplayItemMetaDataItem</c> (script): an item's menu metadata. Only its first members are known; read it
    /// up to the first unknown one.
    /// </summary>
    public static StructSchema UiGameplayItemMetaDataItem { get; } = new("UIGameplayItemMetaDataItem",
    [
        new PropertyField("PersistenceDataAsset", PropertyType.Object),
        new PropertyField("bOverrideAssetId", PropertyType.Bool),
        new PropertyField("OverrideAssetId", PropertyType.Int64),
        new PropertyField("ItemName", PropertyType.Text),
        new PropertyField("Description", PropertyType.Text),
        new PropertyField("QualityLevel", PropertyType.Byte),
        new PropertyField("UpgradeLevel", PropertyType.Byte),
    ]);

    /// <summary>
    /// <c>FWeaponStatModifier</c> (native): how a weapon quality level changes a stat, as a property block of three
    /// floats (added, added as a multiple, multiplied).
    /// </summary>
    public static StructSchema WeaponStatModifier { get; } = new("WeaponStatModifier",
    [
        new PropertyField("Add", PropertyType.Float),
        new PropertyField("AddMultiply", PropertyType.Float),
        new PropertyField("Multiply", PropertyType.Float),
    ]);

    /// <summary>The item asset chains above by class script path.</summary>
    public static IReadOnlyDictionary<string, StructSchema> ByClassPath { get; } = new Dictionary<string, StructSchema>(StringComparer.Ordinal)
    {
        [ItemDataAssetClass] = ItemDataAsset,
        [AugmentItemDataAssetClass] = AugmentItemDataAsset,
        [ItemWithQualityRedirectorAssetClass] = ItemWithQualityRedirectorAsset,
        [WeaponRedirectorAssetClass] = WeaponRedirectorAsset,
        [UiGameplayItemMetaDataItemClass] = UiGameplayItemMetaDataItem,
    };
}
