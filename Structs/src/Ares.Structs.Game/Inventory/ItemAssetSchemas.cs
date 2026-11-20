using Ares.Structs.Core.Assets;

namespace Ares.Structs.Game.Inventory;

/// <summary>
/// The item data asset classes: script paths of the classes the item tables read, and the property chains of their
/// native bases and native structs, as cooked unversioned property blocks index them (each class's own members first,
/// then its super class's). The script classes' own chains come from <see cref="Scripts.ScriptTypeSchemas"/> over
/// <see cref="Natives"/>.
/// </summary>
public static class ItemAssetSchemas
{
    /// <summary>Script path of <c>UItemDataAsset</c>.</summary>
    public const string ItemDataAssetClass = "/Script/Angelscript.ItemDataAsset";

    /// <summary>Script path of <c>UAugmentItemDataAsset</c>.</summary>
    public const string AugmentItemDataAssetClass = "/Script/Angelscript.AugmentItemDataAsset";

    /// <summary>Script path of <c>UItemWithQualityRedirectorAsset</c>.</summary>
    public const string ItemWithQualityRedirectorAssetClass = "/Script/Angelscript.ItemWithQualityRedirectorAsset";

    /// <summary>Script path of <c>UWeaponRedirectorAsset</c>.</summary>
    public const string WeaponRedirectorAssetClass = "/Script/Angelscript.WeaponRedirectorAsset";

    /// <summary>Script path of <see cref="UiGameplayItemMetaDataItem"/>.</summary>
    public const string UiGameplayItemMetaDataItemClass = "/Script/Angelscript.UIGameplayItemMetaDataItem";

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

    /// <summary>
    /// The native classes and structs the item data asset script classes chain to or hold, by name with prefix (the
    /// natives of <see cref="Scripts.ScriptTypeSchemas"/>).
    /// </summary>
    public static IReadOnlyDictionary<string, StructSchema> Natives { get; } = new Dictionary<string, StructSchema>(StringComparer.Ordinal)
    {
        ["UItemDataAssetBaseNative"] = ItemDataAssetBaseNative,
        ["UOptionalPrimaryDataAsset"] = OptionalPrimaryDataAsset,
        ["UDataAsset"] = DataAsset,
        ["FWeaponStatModifier"] = WeaponStatModifier,
    };
}
