using System.Globalization;
using Ares.GameDataExtract.Packages;
using Ares.GameDataExtract.Properties;
using Ares.GameDataExtract.Registry;
using Ares.GameDataExtract.Scripts;
using Ares.Structs.Core.Assets;
using Ares.Structs.Game.Inventory;

namespace Ares.GameDataExtract.Items;

/// <summary>
/// The install readers the item tables share: the packages, the asset registry, the script cache and the property
/// chains of item data assets (<see cref="ItemAssetSchemas"/> for the item asset classes it lists, the script cache
/// for other script classes, over the native item data asset bases), and object defaults along archetype chains.
/// </summary>
internal sealed class ItemSources
{
    /// <summary>Bytes after the properties of a data asset export.</summary>
    public const int ExportTrailer = 4;

    /// <summary>Registry class of persistence assets, whose <see cref="AssetIdTag"/> tag is an item's asset id.</summary>
    public const string PersistenceDataAssetClass = ScriptSchemas.ScriptPackage + "PersistenceDataAsset";

    /// <summary>Registry tag of a persistence asset holding its asset id.</summary>
    public const string AssetIdTag = "AssetId";

    private readonly Dictionary<string, long> _persistenceIds;
    private readonly Dictionary<string, ScriptClassDefaults> _scriptDefaults = new(StringComparer.Ordinal);

    public ItemSources(PackageCache packages, AssetRegistry registry, ScriptCache scripts)
    {
        Packages = packages;
        Registry = registry;
        Scripts = scripts;
        Schemas = new ScriptSchemas(scripts, new Dictionary<string, StructSchema>(StringComparer.Ordinal)
        {
            ["UItemDataAssetBaseNative"] = ItemAssetSchemas.ItemDataAssetBaseNative,
            ["UOptionalPrimaryDataAsset"] = ItemAssetSchemas.OptionalPrimaryDataAsset,
            ["UDataAsset"] = ItemAssetSchemas.DataAsset,
            ["FWeaponStatModifier"] = ItemAssetSchemas.WeaponStatModifier,
        });
        Classes = new ClassSchemas(packages, Schemas.ForPath);
        Defaults = new ObjectDefaults(packages, Classes.ForObject);
        _persistenceIds = registry.Assets
            .Where(asset => asset.ClassPath == PersistenceDataAssetClass)
            .Select(asset => (asset.PackageName, Id: registry.Tags(asset).GetValueOrDefault(AssetIdTag)))
            .Where(entry => entry.Id is not null)
            .ToDictionary(entry => entry.PackageName, entry => long.Parse(entry.Id!, CultureInfo.InvariantCulture), StringComparer.Ordinal);
        CheckItemAssetSchemas();
    }

    public PackageCache Packages { get; }

    public AssetRegistry Registry { get; }

    public ScriptCache Scripts { get; }

    public ScriptSchemas Schemas { get; }

    public ClassSchemas Classes { get; }

    public ObjectDefaults Defaults { get; }

    /// <summary>The registry's assets of class <paramref name="classPath"/>, in registry order.</summary>
    public IEnumerable<RegistryAsset> AssetsOf(string classPath) => Registry.Assets.Where(asset => asset.ClassPath == classPath);

    /// <summary>The package of <paramref name="asset"/>.</summary>
    /// <exception cref="InvalidDataException">No container has it.</exception>
    public CookedPackage Package(RegistryAsset asset) =>
        Packages.Load(asset.PackageName) ?? throw new InvalidDataException($"{asset.PackageName} is not in the game archives");

    /// <summary>The properties of a single-export data asset, read with its class's chain up to the export trailer.</summary>
    /// <exception cref="InvalidDataException">The asset does not decode.</exception>
    public PropertyBag ReadAsset(RegistryAsset asset)
    {
        CookedPackage package = Package(asset);
        if (package.Exports.Count != 1)
        {
            throw new InvalidDataException($"{asset.PackageName}: expected one export, found {package.Exports.Count}");
        }

        return PropertyReader.ReadExport(package, package.Exports[0], SchemaOf(asset.ClassPath), ExportTrailer);
    }

    /// <summary>The property chain of script class <paramref name="classPath"/>.</summary>
    /// <exception cref="InvalidDataException">The class has no known chain.</exception>
    public StructSchema SchemaOf(string classPath) =>
        ItemAssetSchemas.ByClassPath.GetValueOrDefault(classPath) ?? Schemas.ForPath(classPath) ?? throw new InvalidDataException($"No property chain is known for {classPath}");

    /// <summary>The package an object reference of <paramref name="package"/> imports from.</summary>
    /// <exception cref="InvalidDataException">The reference is not a package import.</exception>
    public static string ImportedPackage(CookedPackage package, ObjectReference reference)
    {
        if (!package.IsImportReference(reference.Value) || package.Header.Imports[-reference.Value - 1] is not { Kind: PackageObjectKind.PackageImport } import)
        {
            throw new InvalidDataException($"{package.Name}: expected a package import, got reference {reference.Value}");
        }

        return package.Header.ImportedPackageNames[import.ImportedPackageIndex];
    }

    /// <summary>The asset id of the persistence asset a member of <paramref name="values"/> (read from <paramref name="package"/>) refers to.</summary>
    /// <exception cref="InvalidDataException">The member is unset or does not refer to a persistence asset with an asset id.</exception>
    public long PersistenceId(CookedPackage package, PropertyBag values, string member = "PersistenceDataAsset")
    {
        string persistence = ImportedPackage(package, values.Get<ObjectReference>(member));
        return _persistenceIds.TryGetValue(persistence, out long id) ? id : throw new InvalidDataException($"{package.Name}: {persistence} has no asset id in the registry");
    }

    /// <summary>The asset id of the persistence asset in package <paramref name="packageName"/>, if the registry lists one.</summary>
    public long? PersistenceIdOf(string packageName) => _persistenceIds.TryGetValue(packageName, out long id) ? id : null;

    /// <summary>
    /// A floating-point member's value from <paramref name="values"/>, or the literal the constructors along script class
    /// <paramref name="scriptClass"/>'s chain store into it when the block does not hold it.
    /// </summary>
    /// <exception cref="InvalidDataException">The block does not hold the member and the script sets no literal default.</exception>
    public double Number(PropertyBag values, string scriptClass, string member)
    {
        if (values.Contains(member))
        {
            return values.Get<object>(member) switch
            {
                double value => value,
                float value => value,
                object other => throw new InvalidDataException($"{scriptClass}.{member} holds a {other.GetType().Name}, not a number"),
            };
        }

        return ScriptDefault(scriptClass, member) switch
        {
            { Size: sizeof(double) } constant => constant.AsDouble,
            { Size: sizeof(float) } constant => constant.AsSingle,
            _ => throw new InvalidDataException($"{scriptClass}.{member} has no floating-point literal default in the script cache"),
        };
    }

    /// <summary>An <see cref="int"/> member's value from <paramref name="values"/>, or its script literal default (see <see cref="Number"/>).</summary>
    /// <exception cref="InvalidDataException">The block does not hold the member and the script sets no literal default.</exception>
    public int Integer(PropertyBag values, string scriptClass, string member)
    {
        return values.Contains(member) ? values.Get<int>(member)
            : ScriptDefault(scriptClass, member) is { Size: sizeof(int) } constant ? constant.AsInt32
            : throw new InvalidDataException($"{scriptClass}.{member} has no integer literal default in the script cache");
    }

    /// <summary>
    /// The block of the most derived object along <paramref name="target"/>'s archetype chain that stores
    /// <paramref name="member"/>, with its package (object references resolve against it), or <see langword="null"/>.
    /// </summary>
    /// <exception cref="InvalidDataException">An object of the chain cannot be read.</exception>
    public (PropertyBag Values, CookedPackage Package)? Holder(ExportRef target, string member)
    {
        foreach (ExportRef archetype in Defaults.Archetypes(target))
        {
            PropertyBag own = PropertyReader.Read(archetype.Package, CookedPackage.StartOf(archetype.Export), Classes.ForObject(archetype), out _);
            if (own.Contains(member))
            {
                return (own, archetype.Package);
            }
        }

        return null;
    }

    /// <summary>The literal the constructors along <paramref name="scriptClass"/>'s chain store last into <paramref name="member"/>.</summary>
    private ScriptConstant? ScriptDefault(string scriptClass, string member)
    {
        ScriptClass? declaring = Scripts.Chain(scriptClass, out _).FirstOrDefault(type => type.ReflectedProperties.Any(property => property.Name == member));
        if (declaring is null || !Scripts.MemberOffsets(declaring.Name).TryGetValue(member, out int offset))
        {
            return null;
        }

        if (!_scriptDefaults.TryGetValue(scriptClass, out ScriptClassDefaults? defaults))
        {
            defaults = ScriptClassDefaults.Of(Scripts, scriptClass);
            _scriptDefaults[scriptClass] = defaults;
        }

        return defaults.Constant(offset);
    }

    /// <summary>Whether script class <paramref name="name"/> is <paramref name="ancestor"/> or derives from it.</summary>
    public bool Derives(string name, string ancestor) => Scripts.Chain(name, out string? native).Any(type => type.Name == ancestor) || native == ancestor;

    /// <summary>
    /// Checks that the script members of the item asset chains in <see cref="ItemAssetSchemas"/> are the script cache's
    /// reflected members, in order (the native bases are not in the cache).
    /// </summary>
    private void CheckItemAssetSchemas()
    {
        foreach ((string path, StructSchema schema) in ItemAssetSchemas.ByClassPath)
        {
            string name = "U" + path[ScriptSchemas.ScriptPackage.Length..];
            string[] expected = [.. Scripts.ReflectedChain(name, out _).Select(property => property.Name)];
            List<string> actual = [];
            for (StructSchema? type = schema; type is not null && actual.Count < expected.Length; type = type.Super)
            {
                actual.AddRange(type.Members.Select(member => member?.Name ?? "?"));
            }

            // The UI metadata item is read only up to its last needed member.
            if (!expected.Take(actual.Count).SequenceEqual(actual, StringComparer.Ordinal) || (actual.Count < expected.Length && path != ItemAssetSchemas.UIGameplayItemMetaDataItemClass))
            {
                throw new InvalidDataException($"The script members of {name} differ from ItemAssetSchemas: [{string.Join(", ", expected)}]");
            }
        }
    }
}
