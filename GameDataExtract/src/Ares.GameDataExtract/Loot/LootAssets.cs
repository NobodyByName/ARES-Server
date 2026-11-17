using Ares.GameDataExtract.Packages;
using Ares.GameDataExtract.Properties;
using Ares.GameDataExtract.Registry;
using Ares.GameDataExtract.Scripts;
using Ares.Structs.Core.Assets;
using Ares.Structs.Game.Loot;
using Ares.Structs.Game.World;
using Ares.Structs.Net.GameplayTags;

namespace Ares.GameDataExtract.Loot;

/// <summary>Reading helpers shared by the loot data asset readers.</summary>
internal static class LootAssets
{
    private const string TagName = "TagName";
    private const string ClassSuffix = "_C";
    private const string DefaultPrefix = "Default__";

    /// <summary>The registry's assets of class <paramref name="classPath"/>, ordered by package (ordinal); each must be named with <paramref name="prefix"/>.</summary>
    /// <exception cref="InvalidDataException">An asset of the class is named otherwise.</exception>
    public static IReadOnlyList<RegistryAsset> OfClass(AssetRegistry registry, string classPath, string prefix)
    {
        RegistryAsset[] assets = [.. registry.Assets.Where(asset => asset.ClassPath == classPath).OrderBy(asset => asset.PackageName, StringComparer.Ordinal)];
        return assets.FirstOrDefault(asset => !asset.AssetName.StartsWith(prefix, StringComparison.Ordinal)) is { } other
            ? throw new InvalidDataException($"{other.ObjectPath}: a {classPath} not named {prefix}*")
            : assets;
    }

    /// <summary>The properties of a data asset, the only export of its package, read up to the loot data asset trailer.</summary>
    /// <exception cref="InvalidDataException">The package is missing, holds other exports, or the properties do not decode.</exception>
    public static (CookedPackage Package, PropertyBag Properties) Read(PackageCache packages, RegistryAsset asset, StructSchema schema)
    {
        CookedPackage package = packages.Load(asset.PackageName) ?? throw new InvalidDataException($"{asset.PackageName} is not in the game archives");
        if (package.Exports is not [{ } export] || export.Name != asset.AssetName)
        {
            throw new InvalidDataException($"{asset.PackageName}: expected the single export {asset.AssetName}");
        }

        return (package, PropertyReader.ReadExport(package, export, schema, LootDataAssets.ExportTrailer));
    }

    /// <summary>The tag a gameplay tag block names, or <see langword="null"/> when it names none.</summary>
    public static string? Tag(PropertyBag tag) => tag.Get<string>(TagName, CookedPackage.NoneName) is { Length: > 0 } name and not CookedPackage.NoneName ? name : null;

    /// <summary>The Blueprint class an imported class reference names, with its class default object.</summary>
    /// <exception cref="InvalidDataException">The reference is not an imported Blueprint class with a class default object.</exception>
    public static ArchetypeClass BlueprintClass(PackageCache packages, CookedPackage package, object reference)
    {
        ExportRef type = (reference is ObjectReference { IsNull: false } value ? packages.Reference(package, value.Value) : null)
            ?? throw new InvalidDataException($"{package.Name}: a class reference is null or a script class");
        string leaf = type.Package.Name[(type.Package.Name.LastIndexOf('/') + 1)..];
        string name = leaf + ClassSuffix;
        string archetype = DefaultPrefix + name;
        if (type.Package == package || type.Export.Name != name || !type.Export.Outer.IsNull
            || !type.Package.Exports.Any(export => export.Name == archetype && export.Outer.IsNull))
        {
            throw new InvalidDataException($"{package.Name}: the class reference {type.Path} is not an imported Blueprint class with its class default");
        }

        return new ArchetypeClass($"{type.Package.Name}.{name}", $"{type.Package.Name}.{archetype}");
    }

    /// <summary>
    /// The item asset name an item reference names (<see langword="null"/> for none): the asset of an item package of
    /// <paramref name="items"/> (item package to asset name).
    /// </summary>
    /// <exception cref="InvalidDataException">The reference names something else.</exception>
    public static string? Item(PackageCache packages, CookedPackage package, object reference, IReadOnlyDictionary<string, string> items)
    {
        if (reference is not ObjectReference value || value.IsNull)
        {
            return reference is ObjectReference ? null : throw new InvalidDataException($"{package.Name}: an item reference is not an object reference");
        }

        ExportRef item = packages.Reference(package, value.Value) ?? throw new InvalidDataException($"{package.Name}: an item reference is a script object");
        return item.Package != package && items.TryGetValue(item.Package.Name, out string? asset) && item.Export.Name == asset
            ? asset
            : throw new InvalidDataException($"{package.Name}: the item reference {item.Path} is not an item asset");
    }

    /// <summary>An <c>FInt32Interval</c> as a range: both members stored, or the whole struct stored as zero.</summary>
    /// <exception cref="InvalidDataException">One member is missing.</exception>
    public static LootRange Range(PropertyBag interval, string where)
    {
        return interval.IsZero ? new LootRange(0, 0)
            : interval.Contains("Min") && interval.Contains("Max") ? new LootRange(interval.Get<int>("Min"), interval.Get<int>("Max"))
            : throw new InvalidDataException($"{where}: an interval without both bounds");
    }

    /// <summary>The members <paramref name="names"/> of a struct, each of which must be stored.</summary>
    /// <exception cref="InvalidDataException">A member is not stored.</exception>
    public static PropertyBag Complete(object value, string where, params string[] names)
    {
        return value is PropertyBag bag && names.All(bag.Contains)
            ? bag
            : throw new InvalidDataException($"{where}: a struct does not store every member of {string.Join(", ", names)}");
    }

    /// <summary>The name of value <paramref name="value"/> of a script enum, as the C# enum of the same names.</summary>
    /// <exception cref="InvalidDataException">The value is out of range or its name is not one of <typeparamref name="T"/>.</exception>
    public static T EnumValue<T>(ScriptEnum type, byte value, string where)
        where T : struct, Enum
    {
        return value < type.Names.Count && Enum.TryParse(type.Names[value], ignoreCase: false, out T parsed) && Enum.IsDefined(parsed)
            ? parsed
            : throw new InvalidDataException($"{where}: {type.Name} value {value}");
    }

    /// <summary>
    /// A gameplay tag query as stored (<see cref="EngineStructs.GameplayTagQuery"/>), or <see langword="null"/> for an
    /// empty one. The token stream must parse completely and use every dictionary tag.
    /// </summary>
    /// <exception cref="InvalidDataException">The query is malformed.</exception>
    public static GameplayTagQuery? Query(PropertyBag query, string where)
    {
        string[] tags = [.. query.Get<object[]>("TagDictionary", []).Select(tag => tag is PropertyBag bag ? Tag(bag) : null)
            .Select(tag => tag ?? throw new InvalidDataException($"{where}: a query tag is empty"))];
        byte[] tokens = [.. query.Get<object[]>("QueryTokenStream", []).Cast<byte>()];
        if (tokens.Length == 0)
        {
            return tags.Length == 0 ? null : throw new InvalidDataException($"{where}: query tags without a token stream");
        }

        GameplayTagQuery result = new(tags, tokens);
        HashSet<int> used = [];
        int at = 2;
        bool valid = query.Get("TokenStreamVersion", 0) == tokens[0] && tokens[0] == 0 && tokens.Length >= 2 && tokens[1] is 0 or 1
            && (tokens[1] == 0 || Expression(tokens, tags.Length, ref at, used, 0)) && at == tokens.Length && used.Count == tags.Length;
        return valid ? result : throw new InvalidDataException($"{where}: the query token stream does not parse or does not use exactly its {tags.Length} tags");
    }

    private static bool Expression(byte[] tokens, int tagCount, ref int at, HashSet<int> used, int depth)
    {
        const int MaxDepth = 16;
        const int AnyTagsMatch = 1;
        const int NoTagsMatch = 3;
        const int AnyExprMatch = 4;
        const int NoExprMatch = 6;
        if (depth > MaxDepth || at + 2 > tokens.Length)
        {
            return false;
        }

        int kind = tokens[at];
        int count = tokens[at + 1];
        at += 2;
        switch (kind)
        {
            case >= AnyTagsMatch and <= NoTagsMatch:
                if (at + count > tokens.Length)
                {
                    return false;
                }

                for (int i = 0; i < count; i++, at++)
                {
                    if (tokens[at] >= tagCount)
                    {
                        return false;
                    }

                    used.Add(tokens[at]);
                }

                return true;
            case >= AnyExprMatch and <= NoExprMatch:
                for (int i = 0; i < count; i++)
                {
                    if (!Expression(tokens, tagCount, ref at, used, depth + 1))
                    {
                        return false;
                    }
                }

                return true;
            default:
                return false;
        }
    }
}
