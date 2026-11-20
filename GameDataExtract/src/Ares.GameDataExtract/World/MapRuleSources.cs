using Ares.GameDataExtract.Packages;
using Ares.GameDataExtract.Properties;
using Ares.GameDataExtract.Scripts;
using Ares.Structs.Core.Assets;
using Ares.Structs.Game.Scripts;
using Ares.Structs.Game.World;
using Ares.Structs.Net.Replication;
using PropertyKind = Ares.Structs.Core.Assets.PropertyKind;

namespace Ares.GameDataExtract.World;

/// <summary>
/// What the map rule readers share: the packages, the script layouts, the property chains of script and Blueprint
/// classes (with the native chains the tables know) and the archetype chains of cooked objects. Members are read up to
/// the last one a reader asks for, so members after it (often of native or unknown types) need not be understood.
/// </summary>
internal sealed class MapRuleSources
{
    private const int MaxClassDepth = 32;

    private readonly ClassSchemas _classes;
    private readonly ObjectDefaults _defaults;

    /// <summary>Sources over <paramref name="packages"/> and <paramref name="layouts"/>.</summary>
    public MapRuleSources(PackageCache packages, ScriptLayouts layouts)
    {
        Packages = packages;
        Layouts = layouts;
        ScriptTypeSchemas schemas = layouts.Schemas(new Dictionary<string, StructSchema>(StringComparer.Ordinal)
        {
            [RoundConfigurationMembers.NativeBase] = RoundConfigurationMembers.PersistenceDataAsset,
            [WorldSettingsMembers.NativeBase] = WorldSettingsMembers.Schema,
        });
        _classes = new ClassSchemas(packages, path => path == LevelScriptMembers.TimerHandlePath ? LevelScriptMembers.TimerHandle : schemas.ForPath(path));
        _defaults = new ObjectDefaults(packages, _classes.ForObject);
    }

    /// <summary>The game's packages.</summary>
    public PackageCache Packages { get; }

    /// <summary>The script classes, structs and enums cooked blocks are laid out by.</summary>
    public ScriptLayouts Layouts { get; }

    /// <summary>The package at <paramref name="path"/>.</summary>
    /// <exception cref="InvalidDataException">No container has it.</exception>
    public CookedPackage Load(string path) => Packages.Load(path) ?? throw new InvalidDataException($"{path} is not in the game archives");

    /// <summary>The only export of <paramref name="path"/> whose path ends with <paramref name="suffix"/>.</summary>
    /// <exception cref="InvalidDataException">The package is missing, or not exactly one export matches.</exception>
    public ExportRef Export(string path, string suffix)
    {
        CookedPackage package = Load(path);
        ZenExport[] found = [.. package.Exports.Where(export => package.Header.ExportPath(export.Index).EndsWith(suffix, StringComparison.Ordinal)).Take(2)];
        return found.Length == 1
            ? new ExportRef(package, found[0])
            : throw new InvalidDataException($"{path} has {(found.Length == 0 ? "no" : "more than one")} export {suffix}");
    }

    /// <summary>
    /// The members <paramref name="names"/> of <paramref name="target"/> over its archetypes' (see
    /// <see cref="ObjectDefaults"/>): each object of the archetype chain is read up to the last of them in its own chain.
    /// Members no object stores are missing from the bag.
    /// </summary>
    /// <exception cref="InvalidDataException">A member is not in an object's chain, or an object stores an unknown member before one of them.</exception>
    public PropertyBag Members(ExportRef target, params IReadOnlyList<string> names)
    {
        IReadOnlyList<ExportRef> chain = _defaults.Archetypes(target);
        PropertyBag? merged = null;
        for (int i = chain.Count - 1; i >= 0; i--)
        {
            PropertyBag own = Own(chain[i], names);
            merged = merged is null ? own : own.Over(merged);
        }

        return merged ?? throw new InvalidOperationException("An archetype chain holds at least the object itself");
    }

    /// <summary>
    /// The bool members <paramref name="names"/> that <paramref name="target"/> or one of its archetypes lists, the
    /// object nearest to <paramref name="target"/> first. A listed bool is true unless the header's zero mask marks it,
    /// so only the headers are read and the values before them need not be understood.
    /// </summary>
    /// <exception cref="InvalidDataException">A member is not a bool of an object's chain.</exception>
    public IReadOnlyDictionary<string, bool> Flags(ExportRef target, IReadOnlyList<string> names)
    {
        Dictionary<string, bool> flags = new(StringComparer.Ordinal);
        foreach (ExportRef current in _defaults.Archetypes(target))
        {
            StructSchema schema = _classes.ForObject(current);
            UnversionedHeader header = UnversionedHeader.Read(current.Package.Data, CookedPackage.StartOf(current.Export));
            foreach (string name in names.Where(name => !flags.ContainsKey(name)))
            {
                int index = SchemaPrefix.IndexOf(schema, name);
                if (schema.At(index).Field?.Type.Kind != PropertyKind.Bool)
                {
                    throw new InvalidDataException($"{schema.Name}.{name} is not a bool");
                }

                if (header.Contains(index))
                {
                    flags[name] = header.IsNonZero(index);
                }
            }
        }

        return flags;
    }

    /// <summary>The members <paramref name="names"/> that <paramref name="target"/> itself stores (no archetype).</summary>
    /// <exception cref="InvalidDataException">A member is not in the chain, or the object stores an unknown member before one of them.</exception>
    public PropertyBag Own(ExportRef target, IReadOnlyList<string> names) => SchemaPrefix.Read(target.Package, target.Export, _classes.ForObject(target), names);

    /// <summary>Checks that the script enum <paramref name="expected"/> describes has its names, in order, with values 0, 1, 2, ….</summary>
    /// <exception cref="InvalidDataException">The enum differs.</exception>
    public void CheckEnum(EnumDefinition expected)
    {
        ScriptEnumDefinition found = Layouts.Enum(expected.Path[(expected.Path.LastIndexOf('.') + 1)..]);
        if (!ScriptLayouts.IsSequential(found) || found.Names.Count != expected.MaxValue || !found.Names.SequenceEqual(expected.Names, StringComparer.Ordinal))
        {
            throw new InvalidDataException($"The script enum {found.Name} is [{string.Join(", ", found.Names)}], not [{string.Join(", ", expected.Names)}]");
        }
    }

    /// <summary>
    /// The first script class of the class chain from <paramref name="index"/> (the class itself, or a Blueprint's first
    /// script super class): its script path and its script type name (with its prefix).
    /// </summary>
    /// <param name="package">The package <paramref name="index"/> belongs to.</param>
    /// <param name="index">The class.</param>
    /// <param name="what">What the class belongs to, for errors.</param>
    /// <exception cref="InvalidDataException">The chain reaches no script class.</exception>
    public (string Path, string Name) ScriptClassOf(CookedPackage package, PackageObjectIndex index, string what)
    {
        for (int depth = 0; depth < MaxClassDepth; depth++)
        {
            if (index.Kind == PackageObjectKind.ScriptImport)
            {
                string path = Packages.ScriptPath(index) ?? throw new InvalidDataException($"{what}: script class {index} is unknown");
                return (path, ScriptName(path));
            }

            ExportRef type = Packages.Resolve(package, index) ?? throw new InvalidDataException($"{what}: a class reference is null");
            (package, index) = (type.Package, type.Export.Super);
        }

        throw new InvalidDataException($"{what}: the class chain is too long");
    }

    /// <summary>The script type name (with its prefix) of the script class at <paramref name="path"/>.</summary>
    /// <exception cref="InvalidDataException">No script class is at that path.</exception>
    private string ScriptName(string path) => Layouts.NameOf(path, "A", "U", "F") ?? throw new InvalidDataException($"{path} is not a script class");

    /// <summary>The package and public export hash of the asset a stored reference names.</summary>
    /// <exception cref="InvalidDataException">The reference names no public export of another package.</exception>
    public (string Package, ulong ExportHash) PublicExport(CookedPackage package, ObjectReference reference)
    {
        ExportRef? found = Packages.Reference(package, reference.Value);
        return found is { } export && reference.Value < 0 && export.Export.PublicExportHash != 0
            ? (export.Package.Name, export.Export.PublicExportHash)
            : throw new InvalidDataException($"{package.Name}: reference {reference.Value} names no public export of another package");
    }

    /// <summary>The name of a stored gameplay tag (a struct holding its name).</summary>
    public static string Tag(PropertyBag tag) => tag.Get<string>(RoundConfigurationMembers.TagName);
}
