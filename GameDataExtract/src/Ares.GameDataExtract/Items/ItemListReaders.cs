using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Ares.GameDataExtract.Packages;
using Ares.GameDataExtract.Properties;
using Ares.GameDataExtract.Registry;
using Ares.Structs.Backend.Progression;
using Ares.Structs.Game.World;

namespace Ares.GameDataExtract.Items;

/// <summary>
/// Reads the item value cache: <c>DA_LootValueCache</c>, a <c>UItemValueDataAsset</c> whose selected branch's
/// <c>ItemValueMap</c> maps asset ids to <c>FItemValueInfo</c> (name, scrap value, economy value).
/// </summary>
internal static class ItemValueReader
{
    /// <summary>Object path of the cache.</summary>
    public const string CacheAsset = "/Game/Pioneer/Items/DA_LootValueCache.DA_LootValueCache";

    private const string DefaultBranch = "Main";

    /// <summary><c>FItemValueInfo.EconomyValue</c> when the cache leaves it out (its script default).</summary>
    private const int EconomyValueDefault = -1;

    /// <summary>The cache's values, in map order.</summary>
    /// <exception cref="InvalidDataException">The cache is missing, selects another branch, or a key differs from its value's asset id.</exception>
    public static ImmutableArray<ItemValue> Read(ItemSources sources)
    {
        RegistryAsset asset = sources.Registry.Assets.SingleOrDefault(asset => asset.ObjectPath == CacheAsset)
            ?? throw new InvalidDataException($"The asset registry has no {CacheAsset}");
        PropertyBag cache = sources.ReadAsset(asset);
        if (cache.Get("SelectedBranch", DefaultBranch) != DefaultBranch)
        {
            throw new InvalidDataException($"{CacheAsset}: unexpected branch selection");
        }

        ImmutableArray<ItemValue>.Builder values = ImmutableArray.CreateBuilder<ItemValue>();
        foreach (KeyValuePair<object, object> entry in cache.Get<PropertyBag>("SelectedBranchValues").Get<KeyValuePair<object, object>[]>("ItemValueMap"))
        {
            long key = (long)entry.Key;
            PropertyBag info = (PropertyBag)entry.Value;
            ItemValue value = new(info.Get("AssetId", 0L), info.Get("Name", ""), info.Get("ScrapValue", 0), info.Get("EconomyValue", EconomyValueDefault));
            values.Add(value.AssetId == key ? value : throw new InvalidDataException($"{CacheAsset}: map key {key} differs from its asset id {value.AssetId}"));
        }

        return values.Count > 0 && values.Select(value => value.AssetId).Distinct().Count() == values.Count
            ? values.DrainToImmutable()
            : throw new InvalidDataException($"{CacheAsset}: missing or duplicate item values");
    }
}

/// <summary>
/// Reads the world item class list: <c>/CookedOnly/WorldItemList</c>, a <c>UPreloadableEmbarkActorListAsset</c> whose
/// one property is an array of soft class paths. The game state flags a class by its index in it.
/// </summary>
internal static partial class WorldItemListReader
{
    /// <summary>The class paths of the list, in order.</summary>
    /// <exception cref="InvalidDataException">The list is missing, holds something else than Blueprint class paths, or a path twice.</exception>
    public static ImmutableArray<string> Read(PackageCache packages)
    {
        CookedPackage package = packages.Load(WorldItemListAsset.Package) ?? throw new InvalidDataException($"{WorldItemListAsset.Package} is not in the game archives");
        if (package.Exports.Count != 1)
        {
            throw new InvalidDataException($"{WorldItemListAsset.Package}: expected one export, found {package.Exports.Count}");
        }

        PropertyBag list = PropertyReader.ReadExport(package, package.Exports[0], WorldItemListAsset.Schema, ItemSources.ExportTrailer);
        ImmutableArray<string> classes = [.. list.Get<object[]>(WorldItemListAsset.ClassesMember).Cast<SoftObjectPath>().Select(path => path.Path)];
        foreach (string path in classes)
        {
            int dot = path.LastIndexOf('.');
            if (!ClassPath().IsMatch(path) || path[(dot + 1)..] != path[(path.LastIndexOf('/', dot) + 1)..dot] + "_C")
            {
                throw new InvalidDataException($"{WorldItemListAsset.Package}: entry {path} is not a Blueprint class path");
            }
        }

        return classes.Distinct(StringComparer.Ordinal).Count() == classes.Length ? classes : throw new InvalidDataException($"{WorldItemListAsset.Package}: duplicate entries");
    }

    [GeneratedRegex(@"\A(/[A-Za-z0-9_]+)+\.[A-Za-z0-9_]+_C\z")]
    private static partial Regex ClassPath();
}
