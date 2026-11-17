using Ares.GameDataExtract.Packages;
using Ares.GameDataExtract.Properties;
using Ares.GameDataExtract.Registry;
using Ares.GameDataExtract.Scripts;
using Ares.Structs.Backend.Progression;

namespace Ares.GameDataExtract.Items;

/// <summary>An inventory structure data asset as read: registry entry, package, properties and asset id.</summary>
internal sealed record InventoryStructure(RegistryAsset Asset, CookedPackage Package, PropertyBag Values, long AssetId)
{
    /// <summary>The asset's script class name without package, for example <c>ModSlotAsset</c>.</summary>
    public string Kind => Asset.ClassPath[ScriptSchemas.ScriptPackage.Length..];
}

/// <summary>
/// The inventory structure data assets (loadout frames, container slots, containers, content slots and mod slots) and
/// the child slots each gives its menu definition: a frame its container slots (structural), a container slot its
/// default container (required single slot), a container its content slots (array), a content or mod slot the items
/// its query allows (optional single slot: menu asset ids, ordered by the first query tag they hold, then by usable
/// index). Assets are read once, on first use.
/// </summary>
internal sealed class InventoryStructures
{
    public const string LoadoutFrameClass = ScriptSchemas.ScriptPackage + "LoadoutFrameItemDataAsset";
    public const string ContainerSlotClass = ScriptSchemas.ScriptPackage + "InventoryContainerSlotDataAsset";
    public const string ContainerClass = ScriptSchemas.ScriptPackage + "InventoryContainerItemDataAsset";
    public const string ContentSlotClass = ScriptSchemas.ScriptPackage + "InventoryContentSlotDataAsset";
    public const string ModSlotClass = ScriptSchemas.ScriptPackage + "ModSlotAsset";

    private static readonly string[] s_classes = [LoadoutFrameClass, ContainerSlotClass, ContainerClass, ContentSlotClass, ModSlotClass];

    private readonly ItemSources _sources;
    private readonly IReadOnlyList<ItemAsset> _items;
    private readonly Dictionary<string, RegistryAsset> _assets;
    private readonly Dictionary<string, InventoryStructure> _read = new(StringComparer.Ordinal);

    /// <summary>The structures of <paramref name="sources"/>; content and mod slots allow items of <paramref name="items"/> (by usable index).</summary>
    public InventoryStructures(ItemSources sources, IReadOnlyList<ItemAsset> items)
    {
        _sources = sources;
        _items = items;
        _assets = sources.Registry.Assets.Where(asset => s_classes.Contains(asset.ClassPath)).ToDictionary(asset => asset.PackageName, StringComparer.Ordinal);
    }

    /// <summary>Packages of every structure asset of class <paramref name="classPath"/>, in registry order.</summary>
    public IEnumerable<string> PackagesOf(string classPath) => _assets.Values.Where(asset => asset.ClassPath == classPath).Select(asset => asset.PackageName);

    /// <summary>The structure asset named <paramref name="technicalName"/>.</summary>
    /// <exception cref="InvalidDataException">The registry lists no structure asset of that name, or several.</exception>
    public InventoryStructure Named(string technicalName)
    {
        RegistryAsset[] found = [.. _assets.Values.Where(asset => asset.AssetName == technicalName).Take(2)];
        return found.Length == 1 ? Get(found[0].PackageName) : throw new InvalidDataException($"Expected one inventory structure asset {technicalName}, found {found.Length}");
    }

    /// <summary>The structure asset in package <paramref name="packageName"/>.</summary>
    /// <exception cref="InvalidDataException">The registry lists no structure asset there, or it does not decode.</exception>
    public InventoryStructure Get(string packageName)
    {
        if (_read.TryGetValue(packageName, out InventoryStructure? known))
        {
            return known;
        }

        RegistryAsset asset = _assets.TryGetValue(packageName, out RegistryAsset? found) ? found : throw new InvalidDataException($"{packageName} is not an inventory structure asset");
        CookedPackage package = _sources.Package(asset);
        PropertyBag values = _sources.ReadAsset(asset);
        InventoryStructure structure = new(asset, package, values, _sources.PersistenceId(package, values));
        _read[packageName] = structure;
        return structure;
    }

    /// <summary>The structure a member of <paramref name="owner"/>'s block (read from <paramref name="package"/>) refers to.</summary>
    public InventoryStructure Referenced(CookedPackage package, PropertyBag owner, string member) =>
        Get(ItemSources.ImportedPackage(package, owner.Get<ObjectReference>(member)));

    /// <summary>The structures <paramref name="structure"/>'s child slots name, in slot order (none for content and mod slots).</summary>
    public IReadOnlyList<InventoryStructure> Children(InventoryStructure structure) => structure.Asset.ClassPath switch
    {
        LoadoutFrameClass => [.. Containers(structure).Select(container => Referenced(structure.Package, container, "ContainerSlotDataAsset"))],
        ContainerSlotClass => [Referenced(structure.Package, structure.Values, "DefaultContainer")],
        ContainerClass => [Referenced(structure.Package, structure.Values, "ContentSlotAsset")],
        _ => [],
    };

    /// <summary>The <c>FInventoryContainerSlotAndTypePair</c> entries of a loadout frame.</summary>
    public static IEnumerable<PropertyBag> Containers(InventoryStructure frame) => frame.Values.Get<object[]>("Containers", []).Cast<PropertyBag>();

    /// <summary>The child-slot layout of <paramref name="structure"/>'s menu definition.</summary>
    public ItemSlotConfiguration SlotsOf(InventoryStructure structure)
    {
        switch (structure.Asset.ClassPath)
        {
            case LoadoutFrameClass:
                return new StructuralItemSlots([.. Children(structure).Select(child => child.AssetId)]);
            case ContainerSlotClass:
                long container = Children(structure)[0].AssetId;
                return new SingleItemSlot([container], true, container);
            case ContainerClass:
                return new ItemSlotArray(Children(structure)[0].AssetId, _sources.Integer(structure.Values, "U" + structure.Kind, "NumSlots"));
            case ContentSlotClass or ModSlotClass:
                GameplayTagQuery query = GameplayTagQuery.Of(structure.Values.Get<PropertyBag>(structure.Asset.ClassPath == ModSlotClass ? "AllowedModsQuery" : "AllowedItemsQuery"));
                return new SingleItemSlot(
                [
                    .. _items.Where(item => query.Matches(item.Row.ItemTypeTags))
                        .OrderBy(item => query.FirstTagHeld(item.Row.ItemTypeTags))
                        .ThenBy(item => item.Row.UsableIndex)
                        .Select(item => item.Row.AssetId),
                ], false, 0);
            default:
                throw new InvalidDataException($"{structure.Asset.PackageName}: {structure.Asset.ClassPath} is not an inventory structure");
        }
    }
}
