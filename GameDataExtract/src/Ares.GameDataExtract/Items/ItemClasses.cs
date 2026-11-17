using Ares.GameDataExtract.Packages;
using Ares.GameDataExtract.Properties;
using Ares.GameDataExtract.Scripts;

namespace Ares.GameDataExtract.Items;

/// <summary>An item Blueprint class: its root script class and whether a Blueprint of its chain refers to a mod slot asset.</summary>
/// <param name="Path">The class path, <c>/Game/…/BP_X.BP_X_C</c>.</param>
/// <param name="ScriptClass">The script class the Blueprint chain derives from, with its prefix (for example <c>UFirearmItem</c>).</param>
/// <param name="RefersToModSlots">Whether a Blueprint of the chain imports a mod slot asset package.</param>
/// <param name="DefaultObject">The class default object.</param>
internal sealed record ItemClass(string Path, string ScriptClass, bool RefersToModSlots, ExportRef DefaultObject);

/// <summary>
/// Reads item Blueprint classes: each class export's super chain up to its script class, and the class default object
/// with its values over its archetypes'. Results are cached per class.
/// </summary>
internal sealed class ItemClasses
{
    private const int MaxDepth = 32;
    private const string DefaultPrefix = "Default__";

    private readonly ItemSources _sources;
    private readonly HashSet<string> _modSlotPackages;
    private readonly Dictionary<string, ItemClass> _classes = new(StringComparer.Ordinal);

    /// <summary>Item classes over <paramref name="sources"/>; <paramref name="modSlotPackages"/> are the packages of the mod slot assets.</summary>
    public ItemClasses(ItemSources sources, IEnumerable<string> modSlotPackages)
    {
        _sources = sources;
        _modSlotPackages = new HashSet<string>(modSlotPackages, StringComparer.Ordinal);
    }

    /// <summary>The item class at <paramref name="classPath"/>.</summary>
    /// <exception cref="InvalidDataException">The class or one of its supers is missing or not a Blueprint class over a script class.</exception>
    public ItemClass Get(string classPath) => Get(classPath, 0);

    /// <summary>The resolved properties of the class default object of <paramref name="type"/>.</summary>
    public PropertyBag Defaults(ItemClass type) => _sources.Defaults.Resolve(type.DefaultObject);

    private ItemClass Get(string classPath, int depth)
    {
        if (_classes.TryGetValue(classPath, out ItemClass? known))
        {
            return known;
        }

        if (depth > MaxDepth)
        {
            throw new InvalidDataException($"{classPath}: the class chain is too long");
        }

        int dot = classPath.LastIndexOf('.');
        string packageName = classPath[..dot];
        string className = classPath[(dot + 1)..];
        CookedPackage package = _sources.Packages.Load(packageName) ?? throw new InvalidDataException($"{packageName} is not in the game archives");
        ZenExport classExport = Single(package, className);
        ZenExport defaultObject = Single(package, DefaultPrefix + className);
        bool modSlots = package.Header.ImportedPackageNames.Any(_modSlotPackages.Contains);
        ItemClass result;
        switch (classExport.Super.Kind)
        {
            case PackageObjectKind.ScriptImport:
                string path = _sources.Packages.ScriptPath(classExport.Super) ?? throw new InvalidDataException($"{classPath}: unknown script super class");
                string name = ScriptClassName(path) ?? throw new InvalidDataException($"{classPath}: super class {path} is not a script class");
                result = new ItemClass(classPath, name, modSlots, new ExportRef(package, defaultObject));
                break;
            case PackageObjectKind.PackageImport:
                ExportRef super = _sources.Packages.Resolve(package, classExport.Super) ?? throw new InvalidDataException($"{classPath}: the super class cannot be resolved");
                ItemClass parent = Get(super.Path, depth + 1);
                result = new ItemClass(classPath, parent.ScriptClass, modSlots || parent.RefersToModSlots, new ExportRef(package, defaultObject));
                break;
            default:
                throw new InvalidDataException($"{classPath}: the super class is not an import");
        }

        _classes[classPath] = result;
        return result;
    }

    private string? ScriptClassName(string path)
    {
        if (!path.StartsWith(ScriptSchemas.ScriptPackage, StringComparison.Ordinal))
        {
            return null;
        }

        string name = path[ScriptSchemas.ScriptPackage.Length..];
        return _sources.Scripts.TryGetClass("U" + name, out _) ? "U" + name : _sources.Scripts.TryGetClass("A" + name, out _) ? "A" + name : null;
    }

    private static ZenExport Single(CookedPackage package, string name)
    {
        ZenExport[] found = [.. package.Exports.Where(export => export.Name == name).Take(2)];
        return found.Length == 1 ? found[0] : throw new InvalidDataException($"{package.Name}: expected one export {name}, found {found.Length}");
    }
}
