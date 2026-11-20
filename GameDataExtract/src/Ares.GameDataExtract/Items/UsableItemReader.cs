using System.Collections.Immutable;
using Ares.GameDataExtract.Packages;
using Ares.GameDataExtract.Properties;
using Ares.GameDataExtract.Registry;
using Ares.Structs.Game.Inventory;

namespace Ares.GameDataExtract.Items;

/// <summary>An item data asset as read: its registry entry, package, properties, item class and catalogue row.</summary>
internal sealed record ItemAsset(RegistryAsset Asset, CookedPackage Package, PropertyBag Values, ItemClass Class, ItemDefinition Row);

/// <summary>
/// Reads the usable-item catalogue (<see cref="ItemDefinition"/>). The catalogue is every <c>ItemDataAsset</c> and
/// <c>AugmentItemDataAsset</c> name of the asset registry, ordered by its lower-case form (ordinal); the position is the
/// usable index. Each item's id is its persistence asset's; items with quality redirectors use their quality-0
/// redirector's id in menus. Rarity is the <c>QualityLevel</c> of the item's UI metadata item. Weight, durability,
/// stack size, uniqueness and equip times follow the rules and class defaults of <see cref="ItemDataRules"/>.
/// </summary>
internal static class UsableItemReader
{
    private const string UiMetaDataAssetClass = "/Script/EmbarkUI.UIMetaDataAsset";
    private const string UiMetaDataItemPrefix = "UIGameplayItemMetaDataItem";
    private const string ItemsFolder = "/Game/Pioneer/Items/";
    private const string SocketTagPrefix = "Socket.";
    private const string NoTag = "None";

    private static readonly string[] s_itemClasses = [ItemAssetSchemas.ItemDataAssetClass, ItemAssetSchemas.AugmentItemDataAssetClass];
    private static readonly string[] s_redirectorClasses = [ItemAssetSchemas.ItemWithQualityRedirectorAssetClass, ItemAssetSchemas.WeaponRedirectorAssetClass];

    /// <summary>Reads every item of the catalogue, by usable index.</summary>
    /// <exception cref="InvalidDataException">An item, redirector, UI metadata item or item class does not decode or breaks a catalogue rule.</exception>
    public static ImmutableArray<ItemAsset> Read(ItemSources sources, ItemClasses classes)
    {
        RegistryAsset[] assets = [.. sources.Registry.Assets.Where(asset => s_itemClasses.Contains(asset.ClassPath))
            .OrderBy(asset => asset.AssetName.ToLowerInvariant(), StringComparer.Ordinal)];
        if (assets.Select(asset => asset.AssetName).Distinct(StringComparer.Ordinal).Count() != assets.Length)
        {
            throw new InvalidDataException("The asset registry lists two item data assets of the same name");
        }

        Dictionary<string, SortedDictionary<int, long>> qualities = ReadRedirectors(sources);
        Dictionary<long, int> rarities = ReadRarities(sources);
        ImmutableArray<ItemAsset>.Builder items = ImmutableArray.CreateBuilder<ItemAsset>(assets.Length);
        for (int index = 0; index < assets.Length; index++)
        {
            RegistryAsset asset = assets[index];
            CookedPackage package = sources.Package(asset);
            PropertyBag values = sources.ReadAsset(asset);
            qualities.Remove(asset.PackageName, out SortedDictionary<int, long>? levels);
            ItemClass type = classes.Get(values.Get<SoftObjectPath>("ItemClass").Path);
            ItemDefinition row = Row(sources, index, asset, package, values, levels, type, rarities);
            items.Add(new ItemAsset(asset, package, values, type, row));
        }

        return qualities.Count == 0
            ? items.MoveToImmutable()
            : throw new InvalidDataException($"Quality redirectors for unknown items: {string.Join(", ", qualities.Keys.Order(StringComparer.Ordinal).Take(3))}");
    }

    private static ItemDefinition Row(ItemSources sources, int index, RegistryAsset asset, CookedPackage package, PropertyBag values,
        SortedDictionary<int, long>? levels, ItemClass type, Dictionary<long, int> rarities)
    {
        string name = asset.AssetName;
        if (values.Get<byte>("DevelopmentAvailability", 0) != ItemDataRules.UserFacingAvailability || values.Get("bOverrideItemAssetId", false))
        {
            throw new InvalidDataException($"{name}: unexpected availability or asset id override");
        }

        byte category = values.Get<byte>("ItemCategory");
        if (!Enum.IsDefined((ItemCategory)category))
        {
            throw new InvalidDataException($"{name}: category {category}");
        }

        string[] dropTags = values.Get<string[]>("DropTags", []);
        if (dropTags.Any(tag => !tag.StartsWith(ItemDataRules.DropTagPrefix, StringComparison.Ordinal)))
        {
            throw new InvalidDataException($"{name}: drop tag outside {ItemDataRules.DropTagPrefix}");
        }

        long[] qualityIds = levels is null ? [] : [.. levels.Values];
        if (levels is not null && !levels.Keys.SequenceEqual(Enumerable.Range(0, levels.Count)))
        {
            throw new InvalidDataException($"{name}: quality levels {string.Join(", ", levels.Keys)}");
        }

        long baseId = sources.PersistenceId(package, values);
        long assetId = qualityIds.Length > 0 ? qualityIds[0] : baseId;
        ItemCategory itemCategory = (ItemCategory)category;
        double weight = values.Get("Weight", 0.0);
        string[] tags = values.Get<string[]>("Tags");
        int rarity = rarities.TryGetValue(assetId, out int own) ? own : rarities.GetValueOrDefault(baseId);
        return new ItemDefinition(
            index, name, asset.ObjectPath, assetId, baseId, qualityIds, itemCategory, (ItemRarity)rarity,
            values.Get("MaxStackCount", ItemDataRules.DefaultMaxStack),
            weight > 0 ? weight : ItemDataRules.DefaultWeight(itemCategory),
            ItemDataRules.DurabilityRates.Any(rate => values.Get(rate, 0.0) > 0) || ItemDataRules.IsDurableCategory(itemCategory) || values.Get("bHasCustomDurability", false),
            values.Get("bMustBeUniqueInInventory", false) || tags.Contains(ItemDataRules.UniqueTag),
            type.RefersToModSlots && sources.Derives(type.ScriptClass, ItemDataRules.ChildSlotItemClass),
            values.Get<string[]>("InventoryItemTypeTags", []),
            dropTags,
            tags,
            [.. values.Get<object[]>("StaticLootPlacement", []).Select(placement => Placement(name, (PropertyBag)placement))],
            values.Get<string[]>("StaticLootAvoidGroups", []),
            type.Path,
            sources.Number(values, ScriptClassOf(asset), "EquipTime", ItemDataRules.DefaultEquipSeconds),
            sources.Number(values, ScriptClassOf(asset), "UnequipTime", ItemDataRules.DefaultUnequipSeconds),
            sources.Number(values, ScriptClassOf(asset), "UnequipTimeToItems", ItemDataRules.DefaultUnequipToItemsSeconds),
            sources.Number(values, ScriptClassOf(asset), "ReEquipTimeFromItem", ItemDataRules.DefaultReEquipFromItemSeconds));
    }

    /// <summary>The script class of an item data asset, for example <c>UItemDataAsset</c>.</summary>
    private static string ScriptClassOf(RegistryAsset asset) => "U" + asset.ClassPath[(asset.ClassPath.LastIndexOf('.') + 1)..];

    /// <summary><c>FStaticLootPlacementSpecification</c>: a socket tag (an unset one matches no socket) and the tags under it to skip.</summary>
    private static LootPlacement Placement(string item, PropertyBag placement)
    {
        string tag = placement.Get<PropertyBag>("SocketTag").Get("TagName", NoTag);
        string[] excluded = placement.Get<string[]>("ExcludeTags", []);
        return tag.StartsWith(SocketTagPrefix, StringComparison.Ordinal) || (tag == NoTag && excluded.Length == 0)
            ? new LootPlacement(tag == NoTag ? "" : tag, excluded)
            : throw new InvalidDataException($"{item}: unexpected loot placement {tag}");
    }

    /// <summary>{item package: {quality level: redirector asset id}} of every quality redirector.</summary>
    private static Dictionary<string, SortedDictionary<int, long>> ReadRedirectors(ItemSources sources)
    {
        Dictionary<string, SortedDictionary<int, long>> result = new(StringComparer.Ordinal);
        foreach (RegistryAsset asset in sources.Registry.Assets.Where(asset => s_redirectorClasses.Contains(asset.ClassPath)))
        {
            CookedPackage package = sources.Package(asset);
            PropertyBag values = sources.ReadAsset(asset);
            string target = ItemSources.ImportedPackage(package, values.Get<ObjectReference>("ActualItem"));
            int quality = values.Get("QualityLevel", 0);
            SortedDictionary<int, long> levels = result.TryGetValue(target, out SortedDictionary<int, long>? known) ? known : result[target] = [];
            if (!levels.TryAdd(quality, sources.PersistenceId(package, values)))
            {
                throw new InvalidDataException($"{target}: two quality {quality} redirectors");
            }
        }

        return result;
    }

    /// <summary>{asset id: QualityLevel} of every <c>UIGameplayItemMetaDataItem</c> of the items' UI metadata assets.</summary>
    private static Dictionary<long, int> ReadRarities(ItemSources sources)
    {
        Dictionary<long, int> result = [];
        foreach (RegistryAsset asset in sources.Registry.Assets.Where(asset => asset.ClassPath == UiMetaDataAssetClass && asset.PackageName.StartsWith(ItemsFolder, StringComparison.Ordinal)))
        {
            if (sources.Packages.Load(asset.PackageName) is not { } package)
            {
                continue;
            }

            foreach (ZenExport export in package.Exports.Where(export => export.Name.StartsWith(UiMetaDataItemPrefix, StringComparison.Ordinal)))
            {
                PropertyBag values = PropertyReader.ReadPrefix(package, CookedPackage.StartOf(export), ItemAssetSchemas.UiGameplayItemMetaDataItem, out _);
                long? id = values.Get("bOverrideAssetId", false) ? values.Get<long>("OverrideAssetId")
                    : values.Get("PersistenceDataAsset", new ObjectReference(0)).IsNull ? null
                    : sources.PersistenceIdOf(ItemSources.ImportedPackage(package, values.Get<ObjectReference>("PersistenceDataAsset")));
                if (id is not { } assetId)
                {
                    continue;
                }

                int quality = values.Get<byte>("QualityLevel", 0);
                if (!Enum.IsDefined((ItemRarity)quality) || result.GetValueOrDefault(assetId, quality) != quality)
                {
                    throw new InvalidDataException($"{asset.PackageName}: rarity {quality} of {assetId}");
                }

                result[assetId] = quality;
            }
        }

        return result;
    }
}
