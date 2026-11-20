using System.Globalization;
using Ares.GameDataExtract.Packages;
using Ares.GameDataExtract.Properties;
using Ares.GameDataExtract.Registry;
using Ares.GameDataExtract.Scripts;
using Ares.Structs.Core.Assets;
using Ares.Structs.Game.Inventory;
using Ares.Structs.Game.Scripts;

namespace Ares.GameDataExtract.Items;

/// <summary>
/// The install readers the item tables share: the packages, the asset registry, the script layouts, the property chains
/// of item data assets (the script layouts' chains over the native item data asset bases,
/// <see cref="ItemAssetSchemas.Natives"/>) and object defaults along archetype chains.
/// </summary>
internal sealed class ItemSources
{
    /// <summary>Bytes after the properties of a data asset export.</summary>
    public const int ExportTrailer = 4;

    /// <summary>Registry class of persistence assets, whose <see cref="AssetIdTag"/> tag is an item's asset id.</summary>
    private const string PersistenceDataAssetClass = ScriptLayouts.ScriptPackage + "PersistenceDataAsset";

    /// <summary>Registry tag of a persistence asset holding its asset id.</summary>
    private const string AssetIdTag = "AssetId";

    private readonly Dictionary<string, long> _persistenceIds;

    public ItemSources(PackageCache packages, AssetRegistry registry, ScriptLayouts layouts)
    {
        Packages = packages;
        Registry = registry;
        Layouts = layouts;
        Schemas = layouts.Schemas(ItemAssetSchemas.Natives);
        Classes = new ClassSchemas(packages, Schemas.ForPath);
        Defaults = new ObjectDefaults(packages, Classes.ForObject);
        _persistenceIds = registry.Assets
            .Where(asset => asset.ClassPath == PersistenceDataAssetClass)
            .Select(asset => (asset.PackageName, Id: registry.Tags(asset).GetValueOrDefault(AssetIdTag)))
            .Where(entry => entry.Id is not null)
            .ToDictionary(entry => entry.PackageName, entry => long.Parse(entry.Id!, CultureInfo.InvariantCulture), StringComparer.Ordinal);
    }

    public PackageCache Packages { get; }

    public AssetRegistry Registry { get; }

    /// <summary>The script classes, structs and enums cooked blocks are laid out by.</summary>
    public ScriptLayouts Layouts { get; }

    private ScriptTypeSchemas Schemas { get; }

    private ClassSchemas Classes { get; }

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
        return package.Exports.Count == 1
            ? PropertyReader.ReadExport(package, package.Exports[0], SchemaOf(asset.ClassPath), ExportTrailer)
            : throw new InvalidDataException($"{asset.PackageName}: expected one export, found {package.Exports.Count}");
    }

    /// <summary>The property chain of script class <paramref name="classPath"/>.</summary>
    /// <exception cref="InvalidDataException">The class has no known chain.</exception>
    private StructSchema SchemaOf(string classPath) =>
        Schemas.ForPath(classPath) ?? throw new InvalidDataException($"No property chain is known for {classPath}");

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
    /// A floating-point member's value from <paramref name="values"/>, or <paramref name="fallback"/> (the class default)
    /// when the block does not hold it.
    /// </summary>
    /// <exception cref="InvalidDataException">The member holds no number, or script class <paramref name="scriptClass"/>'s chain has no such member.</exception>
    public double Number(PropertyBag values, string scriptClass, string member, double fallback)
    {
        ExpectMember(scriptClass, member);
        return values.Contains(member) ? NumberOf(values, scriptClass, member) : fallback;
    }

    /// <summary>A floating-point member's value from <paramref name="values"/>, which must hold it.</summary>
    /// <exception cref="InvalidDataException">The block does not hold a number for it, or script class <paramref name="scriptClass"/>'s chain has no such member.</exception>
    public double Number(PropertyBag values, string scriptClass, string member)
    {
        ExpectMember(scriptClass, member);
        return values.Contains(member) ? NumberOf(values, scriptClass, member) : throw NotSet(scriptClass, member);
    }

    /// <summary>An <see cref="int"/> member's value from <paramref name="values"/>, which must hold it.</summary>
    /// <exception cref="InvalidDataException">The block does not hold the member, or script class <paramref name="scriptClass"/>'s chain has no such member.</exception>
    public int Integer(PropertyBag values, string scriptClass, string member)
    {
        ExpectMember(scriptClass, member);
        return values.Contains(member) ? values.Get<int>(member) : throw NotSet(scriptClass, member);
    }

    private static double NumberOf(PropertyBag values, string scriptClass, string member) => values.Get<object>(member) switch
    {
        double value => value,
        float value => value,
        { } other => throw new InvalidDataException($"{scriptClass}.{member} holds a {other.GetType().Name}, not a number"),
    };

    private static InvalidDataException NotSet(string scriptClass, string member) => new($"{scriptClass}.{member} is not set, and no class default is known for it");

    /// <summary>Checks that script class <paramref name="scriptClass"/>'s reflected chain declares <paramref name="member"/>.</summary>
    /// <exception cref="InvalidDataException">No class of the chain declares it.</exception>
    private void ExpectMember(string scriptClass, string member)
    {
        if (Layouts.ReflectedChain(scriptClass, out _).All(declared => declared.Name != member))
        {
            throw new InvalidDataException($"The script class chain of {scriptClass} has no member {member}");
        }
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

    /// <summary>Whether script class <paramref name="name"/> is <paramref name="ancestor"/> or derives from it.</summary>
    public bool Derives(string name, string ancestor) => Layouts.Derives(name, ancestor);
}
