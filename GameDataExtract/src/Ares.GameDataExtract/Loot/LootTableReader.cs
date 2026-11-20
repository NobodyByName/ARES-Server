using Ares.GameDataExtract.Packages;
using Ares.GameDataExtract.Properties;
using Ares.GameDataExtract.Registry;
using Ares.GameDataExtract.Scripts;
using Ares.Structs.Core.Assets;
using Ares.Structs.Game.Loot;
using Ares.Structs.Game.Scripts;
using Ares.Structs.Net.GameplayTags;
using M = Ares.Structs.Game.Loot.StaticValueLootTableMembers;
using S = Ares.Structs.Game.Loot.SkillLootTableMembers;

namespace Ares.GameDataExtract.Loot;

/// <summary>
/// Reads the value loot tables (<see cref="StaticValueLootTable"/>) and the skill loot tables
/// (<see cref="SkillLootTable"/>) the asset registry lists, ordered by package (ordinal). Every cooked entry stores all
/// its members, so no default is assumed: a missing member fails the read. Item references must name the asset of an
/// item package the registry lists (<see cref="LootItemAssetClasses"/>).
/// </summary>
internal sealed class LootTableReader
{
    private readonly PackageCache _packages;
    private readonly AssetRegistry _registry;
    private readonly ScriptLayouts _layouts;
    private readonly ScriptTypeSchemas _schemas;
    private readonly Dictionary<string, string> _items;

    public LootTableReader(PackageCache packages, AssetRegistry registry, ScriptLayouts layouts, ScriptTypeSchemas schemas)
    {
        _packages = packages;
        _registry = registry;
        _layouts = layouts;
        _schemas = schemas;
        _items = registry.Assets.Where(asset => LootItemAssetClasses.ClassPaths.Contains(asset.ClassPath))
            .ToDictionary(asset => asset.PackageName, asset => asset.AssetName, StringComparer.Ordinal);
    }

    /// <summary>Reads the value loot tables.</summary>
    /// <exception cref="InvalidDataException">A table does not decode.</exception>
    public IReadOnlyList<StaticValueLootTable> ValueTables()
    {
        StructSchema schema = Schema(M.ClassPath);
        ScriptEnumDefinition queryTypes = _layouts.Enum(M.QueryTypeEnum);
        ScriptEnumDefinition distributions = _layouts.Enum(M.DistributionEnum);
        List<StaticValueLootTable> tables = [];
        foreach (RegistryAsset asset in LootAssets.OfClass(_registry, M.ClassPath, M.AssetPrefix))
        {
            (CookedPackage package, PropertyBag bag) = LootAssets.Read(_packages, asset, schema);
            string where = asset.PackageName;
            List<StaticValueLootTableEntry> entries = [];
            int unshippedItems = 0;
            foreach (object element in Elements(bag, M.Entries, where))
            {
                PropertyBag entry = LootAssets.Complete(element, where, M.ItemQuery, M.QualityLevel, M.StackSize, M.DurabilityConfig, M.DistributionMethod);
                PropertyBag query = LootAssets.Complete(entry.Get<PropertyBag>(M.ItemQuery), where, M.ItemQueryType, M.TagQuery, M.ExactItem);
                PropertyBag durability = LootAssets.Complete(entry.Get<PropertyBag>(M.DurabilityConfig), where, M.DurabilityRange, M.DurabilityCapRange);
                LootItemQueryType type = LootAssets.EnumValue<LootItemQueryType>(queryTypes, query.Get<byte>(M.ItemQueryType), where);
                GameplayTagQuery? tags = LootAssets.Query(query.Get<PropertyBag>(M.TagQuery), where);
                object exactItem = query.Get<object>(M.ExactItem);
                bool unshipped = IsUnshipped(package, exactItem);
                if (unshipped && type == LootItemQueryType.Exact)
                {
                    unshippedItems++;
                    continue;
                }

                string? item = unshipped ? null : LootAssets.Item(_packages, package, exactItem, _items);
                if ((type == LootItemQueryType.Exact && item is null) || (type == LootItemQueryType.Query && tags is null))
                {
                    throw new InvalidDataException($"{where}: a {type} entry without its item or query");
                }

                entries.Add(new StaticValueLootTableEntry(
                    type,
                    tags,
                    item,
                    entry.Get<int>(M.QualityLevel),
                    LootAssets.Range(entry.Get<PropertyBag>(M.StackSize), where),
                    LootAssets.Range(durability.Get<PropertyBag>(M.DurabilityRange), where),
                    LootAssets.Range(durability.Get<PropertyBag>(M.DurabilityCapRange), where),
                    LootAssets.EnumValue<LootDistributionMethod>(distributions, entry.Get<byte>(M.DistributionMethod), where)));
            }

            if (entries.Count > 0)
            {
                tables.Add(new StaticValueLootTable(asset.AssetName, asset.PackageName, entries));
            }
            else if (unshippedItems == 0)
            {
                throw new InvalidDataException($"{where}: no entries");
            }
        }

        return tables;
    }

    /// <summary>Reads the skill loot tables.</summary>
    /// <exception cref="InvalidDataException">A table does not decode.</exception>
    public IReadOnlyList<SkillLootTable> SkillTables()
    {
        StructSchema schema = Schema(S.ClassPath);
        ScriptEnumDefinition entryTypes = _layouts.Enum(S.EntryTypeEnum);
        List<SkillLootTable> tables = [];
        foreach (RegistryAsset asset in LootAssets.OfClass(_registry, S.ClassPath, prefix: ""))
        {
            (CookedPackage package, PropertyBag bag) = LootAssets.Read(_packages, asset, schema);
            string where = asset.PackageName;
            List<SkillLoot> skills = [];
            foreach (KeyValuePair<object, object> pair in bag.Get<KeyValuePair<object, object>[]>(S.Entries))
            {
                string skill = (pair.Key is PropertyBag tag ? LootAssets.Tag(tag) : null) ?? throw new InvalidDataException($"{where}: a skill without its tag");
                List<SkillLootGroup> groups = [];
                foreach (object element in Elements(LootAssets.Complete(pair.Value, where, S.Entries), S.Entries, where))
                {
                    PropertyBag group = LootAssets.Complete(element, where, S.ContainerQuery, S.Items);
                    List<SkillLootItem> items = [];
                    foreach (object itemElement in Elements(group, S.Items, where))
                    {
                        PropertyBag entry = LootAssets.Complete(itemElement, where, S.Weight, S.EntryType, S.ItemDataAsset, S.StackSize, S.QualityLevel);
                        object itemAsset = entry.Get<object>(S.ItemDataAsset);
                        if (IsUnshipped(package, itemAsset))
                        {
                            continue;
                        }

                        string? item = LootAssets.Item(_packages, package, itemAsset, _items);
                        byte kind = entry.Get<byte>(S.EntryType);
                        if (kind >= entryTypes.Names.Count || (entryTypes.Names[kind] == S.ItemEntry) != (item is not null))
                        {
                            throw new InvalidDataException($"{where}: skill entry type {kind} with item {item ?? "none"}");
                        }

                        items.Add(new SkillLootItem(Number(entry.Get<object>(S.Weight), where), item, LootAssets.Range(entry.Get<PropertyBag>(S.StackSize), where), entry.Get<int>(S.QualityLevel)));
                    }

                    groups.Add(new SkillLootGroup(LootAssets.Query(group.Get<PropertyBag>(S.ContainerQuery), where), items));
                }

                skills.Add(new SkillLoot(skill, groups));
            }

            tables.Add(new SkillLootTable(asset.AssetName, asset.PackageName, skills));
        }

        return tables;
    }

    /// <summary>Whether an item reference points into a package the install does not ship; such entries are left out.</summary>
    private bool IsUnshipped(CookedPackage package, object reference) =>
        reference is ObjectReference value && _packages.ImportsUnshippedPackage(package, value.Value);

    private StructSchema Schema(string classPath) => _schemas.ForPath(classPath) ?? throw new InvalidDataException($"No script class {classPath}");

    /// <summary>The elements of array member <paramref name="name"/>; none of them may be stored as zero.</summary>
    private static object[] Elements(PropertyBag bag, string name, string where)
    {
        object[] elements = bag.Get<object[]>(name, []);
        return elements.Any(element => element is PropertyBag { IsZero: true })
            ? throw new InvalidDataException($"{where}: an empty element of {name}")
            : elements;
    }

    private static double Number(object value, string where) => value switch
    {
        double number => number,
        float single => LootNumbers.Single(single),
        _ => throw new InvalidDataException($"{where}: a weight is not a number"),
    };
}
