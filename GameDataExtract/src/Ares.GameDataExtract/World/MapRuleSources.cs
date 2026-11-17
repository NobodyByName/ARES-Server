using Ares.GameDataExtract.Packages;
using Ares.GameDataExtract.Properties;
using Ares.GameDataExtract.Scripts;
using Ares.Structs.Core.Assets;
using Ares.Structs.Game.World;
using Ares.Structs.Net.Replication;
using PropertyKind = Ares.Structs.Core.Assets.PropertyKind;

namespace Ares.GameDataExtract.World;

/// <summary>
/// What the map rule readers share: the packages, the compiled scripts, the property chains of script and Blueprint
/// classes (with the native chains the tables know) and the archetype chains of cooked objects. Members are read up to
/// the last one a reader asks for, so members after it (often of native or unknown types) need not be understood.
/// </summary>
internal sealed class MapRuleSources
{
    private const int MaxClassDepth = 32;

    private readonly ScriptSchemas _schemas;
    private readonly ClassSchemas _classes;
    private readonly ObjectDefaults _defaults;

    /// <summary>Sources over <paramref name="packages"/> and <paramref name="scripts"/>.</summary>
    public MapRuleSources(PackageCache packages, ScriptCache scripts)
    {
        Packages = packages;
        Scripts = scripts;
        _schemas = new ScriptSchemas(scripts, new Dictionary<string, StructSchema>(StringComparer.Ordinal)
        {
            [RoundConfigurationMembers.NativeBase] = RoundConfigurationMembers.PersistenceDataAsset,
            [WorldSettingsMembers.NativeBase] = WorldSettingsMembers.Schema,
        });
        _classes = new ClassSchemas(packages, path => path == LevelScriptMembers.TimerHandlePath ? LevelScriptMembers.TimerHandle : _schemas.ForPath(path));
        _defaults = new ObjectDefaults(packages, _classes.ForObject);
    }

    /// <summary>The game's packages.</summary>
    public PackageCache Packages { get; }

    /// <summary>The game's compiled scripts.</summary>
    public ScriptCache Scripts { get; }

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
                int index = IndexOf(schema, name);
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
    public PropertyBag Own(ExportRef target, IReadOnlyList<string> names)
    {
        StructSchema schema = _classes.ForObject(target);
        int last = names.Max(name => IndexOf(schema, name));
        StructSchema truncated = Truncated(schema, last);
        int start = CookedPackage.StartOf(target.Export);
        UnversionedHeader header = UnversionedHeader.Read(target.Package.Data, start);
        if (header.Members.FirstOrDefault(member => member.IsNonZero && member.Index <= last && truncated.At(member.Index).Field is null) is { IsNonZero: true } unknown)
        {
            throw new InvalidDataException($"{target.Path} stores the unknown member {unknown.Index} of {schema.Name} before {string.Join(", ", names)}");
        }

        return PropertyReader.ReadPrefix(target.Package, start, truncated, out _);
    }

    /// <summary>
    /// The literal the script default statements of <paramref name="scriptClass"/>'s chain store last into
    /// <paramref name="member"/>, declared by that class or one of its script super classes.
    /// </summary>
    /// <exception cref="InvalidDataException">The member is unknown, or no literal is stored into it.</exception>
    public ScriptConstant Default(string scriptClass, string member)
    {
        return ScriptClassDefaults.Of(Scripts, scriptClass).Constant(Offset(scriptClass, member))
            ?? throw new InvalidDataException($"The script defaults of {scriptClass} store no literal into {member}");
    }

    /// <summary>Byte offset of <paramref name="member"/>, declared by script type <paramref name="scriptClass"/> or one of its script super classes.</summary>
    /// <exception cref="InvalidDataException">No class of the chain declares the member, or the bytecode does not refer to it.</exception>
    public int Offset(string scriptClass, string member)
    {
        ScriptClass declaring = Scripts.Chain(scriptClass, out _).FirstOrDefault(type => type.Properties.Any(property => property.Name == member))
            ?? throw new InvalidDataException($"No class of the script chain of {scriptClass} declares {member}");
        return Scripts.MemberOffsets(declaring.Name).TryGetValue(member, out int offset)
            ? offset
            : throw new InvalidDataException($"The scripts do not refer to {declaring.Name}.{member}");
    }

    /// <summary>Checks that the script enum <paramref name="expected"/> describes has its names, in order, with values 0, 1, 2, ….</summary>
    /// <exception cref="InvalidDataException">The enum differs.</exception>
    public void CheckEnum(EnumDefinition expected)
    {
        ScriptEnum found = Scripts.Enum(expected.Path[(expected.Path.LastIndexOf('.') + 1)..]);
        if (!found.IsSequential || found.Names.Count != expected.MaxValue || !found.Names.SequenceEqual(expected.Names, StringComparer.Ordinal))
        {
            throw new InvalidDataException($"The script enum {found.Name} is [{string.Join(", ", found.Names)}], not [{string.Join(", ", expected.Names)}]");
        }
    }

    /// <summary>
    /// The first script class of the class chain from <paramref name="index"/> (the class itself, or a Blueprint's first
    /// script super class): its script path and the name (with its prefix) the script cache gives it.
    /// </summary>
    /// <param name="package">The package <paramref name="index"/> belongs to.</param>
    /// <param name="index">The class.</param>
    /// <param name="what">What the class belongs to, for errors.</param>
    /// <exception cref="InvalidDataException">The chain reaches no script class of the script cache.</exception>
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

    /// <summary>The script cache's name (with its prefix) of the script class at <paramref name="path"/>.</summary>
    /// <exception cref="InvalidDataException">The cache has no such class.</exception>
    public string ScriptName(string path)
    {
        string name = path.StartsWith(ScriptSchemas.ScriptPackage, StringComparison.Ordinal)
            ? path[ScriptSchemas.ScriptPackage.Length..]
            : throw new InvalidDataException($"{path} is not a script class");
        foreach (string prefix in (ReadOnlySpan<string>)["A", "U", "F"])
        {
            if (Scripts.TryGetClass(prefix + name, out _))
            {
                return prefix + name;
            }
        }

        throw new InvalidDataException($"No script class {name} in the script cache");
    }

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

    private static int IndexOf(StructSchema schema, string name)
    {
        for (int i = 0; i < schema.ChainLength; i++)
        {
            if (schema.At(i) is { Field.Name: var found, Element: 0 } && found == name)
            {
                return i;
            }
        }

        throw new InvalidDataException($"{schema.Name} has no member {name}");
    }

    /// <summary>The chain of <paramref name="schema"/> up to index <paramref name="last"/>: later members read as unknown.</summary>
    private static StructSchema Truncated(StructSchema schema, int last)
    {
        List<PropertyField?> members = [];
        for (int i = 0; i <= last; i++)
        {
            (PropertyField? field, int element) = schema.At(i);
            if (field is null)
            {
                members.Add(null);
            }
            else if (element == 0)
            {
                members.Add(field);
                i += field.ArrayDim - 1;
            }
        }

        return new StructSchema(schema.Name, members);
    }
}
