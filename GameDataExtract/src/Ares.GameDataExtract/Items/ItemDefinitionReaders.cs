using System.Collections.Immutable;
using Ares.GameDataExtract.Packages;
using Ares.GameDataExtract.Properties;
using Ares.Structs.Game.Inventory;

namespace Ares.GameDataExtract.Items;

/// <summary>
/// Reads the per-class item tables: firearms (item classes deriving from <c>UFirearmItem</c>) and shields (the classes
/// of armor items) from their class default objects, augments and loadout frames from their data assets.
/// </summary>
internal static class ItemDefinitionReaders
{
    private const string FirearmScriptClass = "UFirearmItem";
    private const string ModSlotsMember = "ModSlots";
    private const string QualitySetMember = "FixedWeaponQuailitySet";
    private const string ReloadModifiersMember = "Reload Time Modifier Set";
    private const string AugmentScriptClass = "UAugmentItemDataAsset";

    /// <summary>Every firearm class of the catalogue's items, ordered by class path (ordinal).</summary>
    public static ImmutableArray<FirearmDefinition> Firearms(ItemSources sources, ItemClasses classes, InventoryStructures structures, IEnumerable<ItemAsset> items)
    {
        return [.. DistinctClasses(items.Where(item => sources.Derives(item.Class.ScriptClass, FirearmScriptClass)))
            .Select(type => Firearm(sources, classes, structures, type))];
    }

    /// <summary>Every shield class of the catalogue's armor items, ordered by class path (ordinal).</summary>
    public static ImmutableArray<ShieldDefinition> Shields(ItemSources sources, ItemClasses classes, IEnumerable<ItemAsset> items)
    {
        return [.. DistinctClasses(items.Where(item => item.Row.Category == ItemCategory.Armor)).Select(type =>
        {
            PropertyBag values = classes.Defaults(type);
            return new ShieldDefinition(type.Path,
                sources.Number(values, type.ScriptClass, "MaxArmorHealth"),
                sources.Number(values, type.ScriptClass, "DamageMitigationPerPlate"));
        })];
    }

    /// <summary>Every augment of the catalogue, by usable index.</summary>
    public static ImmutableArray<AugmentDefinition> Augments(ItemSources sources, InventoryStructures structures, IEnumerable<ItemAsset> items)
    {
        return [.. items.Where(item => item.Asset.ClassPath == ItemAssetSchemas.AugmentItemDataAssetClass).Select(item => new AugmentDefinition(
            item.Row.TechnicalName,
            item.Row.AssetId,
            structures.Referenced(item.Package, item.Values, "LoadoutFrameDataAsset").AssetId,
            sources.Number(item.Values, AugmentScriptClass, "MaxWeight", ItemDataRules.DefaultAugmentMaxWeight)))];
    }

    /// <summary>Every loadout frame of the registry, ordered by technical name (ordinal).</summary>
    public static ImmutableArray<LoadoutFrameDefinition> LoadoutFrames(InventoryStructures structures)
    {
        return [.. structures.PackagesOf(InventoryStructures.LoadoutFrameClass)
            .Select(structures.Get)
            .OrderBy(frame => frame.Asset.AssetName, StringComparer.Ordinal)
            .Select(frame => new LoadoutFrameDefinition(frame.Asset.AssetName, frame.AssetId,
            [
                .. InventoryStructures.Containers(frame).Select(container => new LoadoutFrameContainer(
                    container.Get<byte>("Type", 0),
                    structures.Referenced(frame.Package, container, "ContainerSlotDataAsset").AssetId)),
            ]))];
    }

    private static IEnumerable<ItemClass> DistinctClasses(IEnumerable<ItemAsset> items) =>
        items.Select(item => item.Class).DistinctBy(type => type.Path).OrderBy(type => type.Path, StringComparer.Ordinal);

    private static FirearmDefinition Firearm(ItemSources sources, ItemClasses classes, InventoryStructures structures, ItemClass type)
    {
        PropertyBag values = classes.Defaults(type);
        string script = type.ScriptClass;
        return new FirearmDefinition(type.Path,
            sources.Number(values, script, "BaseClipSize", WeaponItemDefaults.ClipSize),
            sources.Number(values, script, "BaseFireRate", WeaponItemDefaults.FireRate),
            sources.Number(values, script, "BaseReloadTime", WeaponItemDefaults.BaseReloadSeconds),
            ReloadModifiers(sources, type),
            sources.Number(values, script, "BaseCameraZoomTime", WeaponItemDefaults.ZoomSeconds),
            sources.Number(values, script, "PartialReloadDuration", WeaponItemDefaults.PartialReloadSeconds),
            ModSlots(sources, structures, type));
    }

    /// <summary>Asset ids of the mod slot assets a firearm's <c>ModSlots</c> name, in slot order.</summary>
    private static long[] ModSlots(ItemSources sources, InventoryStructures structures, ItemClass type)
    {
        return sources.Holder(type.DefaultObject, ModSlotsMember) is ({ } own, { } package)
            ? [.. own.Get<object[]>(ModSlotsMember).Cast<PropertyBag>().Select(slot => structures.Referenced(package, slot, "ModSlotAsset").AssetId)]
            : [];
    }

    /// <summary>The <c>Reload Time Modifier Set</c> of a firearm's fixed quality set (a Blueprint class's default object), or none.</summary>
    private static double[] ReloadModifiers(ItemSources sources, ItemClass type)
    {
        if (sources.Holder(type.DefaultObject, QualitySetMember) is not ({ } own, { } package)
            || sources.Packages.Reference(package, own.Get<ObjectReference>(QualitySetMember).Value) is not { } set)
        {
            return [];
        }

        // A class reference names the Blueprint class; its default object holds the values.
        ExportRef defaults = set.Export.Name.StartsWith("Default__", StringComparison.Ordinal) ? set
            : new ExportRef(set.Package, set.Package.Exports.Single(export => export.Name == "Default__" + set.Export.Name));
        return [.. sources.Defaults.Resolve(defaults).Get<object[]>(ReloadModifiersMember, []).Select(value => value switch
        {
            double number => number,
            float number => number,
            _ => throw new InvalidDataException($"{set.Path}: {ReloadModifiersMember} holds a {value.GetType().Name}"),
        })];
    }
}
