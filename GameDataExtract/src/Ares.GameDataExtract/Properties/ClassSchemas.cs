using Ares.GameDataExtract.Packages;
using Ares.Structs.Core.Assets;

namespace Ares.GameDataExtract.Properties;

/// <summary>
/// The property chains of the classes cooked objects belong to: a script class's chain comes from the schema source
/// (by script path, for example <c>/Script/Engine.SceneComponent</c>); a Blueprint class's chain is the members its
/// class export declares (see <see cref="BlueprintClassReader"/>) over its super class's chain. Blueprint members of
/// engine struct types use <see cref="EngineStructs"/>, other script structs the schema source; members of
/// Blueprint-defined struct types cannot be read. Results are cached per class.
/// </summary>
internal sealed class ClassSchemas
{
    private const int MaxSuperDepth = 32;

    private readonly PackageCache _packages;
    private readonly Func<string, StructSchema?> _scriptSchema;
    private readonly Dictionary<(CookedPackage, int), StructSchema> _blueprints = [];

    /// <summary>Chains over <paramref name="packages"/>, with script classes and structs from <paramref name="scriptSchema"/>.</summary>
    public ClassSchemas(PackageCache packages, Func<string, StructSchema?> scriptSchema)
    {
        _packages = packages;
        _scriptSchema = scriptSchema;
    }

    /// <summary>The chain of the class of <paramref name="target"/>.</summary>
    /// <exception cref="InvalidDataException">The class or one of its super classes has no known chain.</exception>
    public StructSchema ForObject(ExportRef target) => ForClass(target.Package, target.Export.Class, 0);

    // The depth of a super class, refused past MaxSuperDepth.
    private static int Deeper(CookedPackage package, int depth) =>
        depth < MaxSuperDepth ? depth + 1 : throw new InvalidDataException($"{package.Name}: the class chain is too long");

    private StructSchema ForClass(CookedPackage package, PackageObjectIndex index, int depth)
    {
        if (index.Kind == PackageObjectKind.ScriptImport)
        {
            string path = _packages.ScriptPath(index) ?? throw new InvalidDataException($"{package.Name}: script class {index} is unknown");
            return _scriptSchema(path) ?? throw new InvalidDataException($"No property chain is known for {path}");
        }

        ExportRef type = _packages.Resolve(package, index) ?? throw new InvalidDataException($"{package.Name}: the class reference is null");
        if (_blueprints.TryGetValue((type.Package, type.Export.Index), out StructSchema? cached))
        {
            return cached;
        }

        StructSchema super = ForClass(type.Package, type.Export.Super, Deeper(type.Package, depth));
        IReadOnlyList<PropertyField> members = BlueprintClassReader.DeclaredMembers(type.Package, type.Export, reference => Struct(type.Package, reference));
        StructSchema schema = new(type.Path, members, super);
        _blueprints[(type.Package, type.Export.Index)] = schema;
        return schema;
    }

    private StructSchema? Struct(CookedPackage package, int reference)
    {
        if (!package.IsImportReference(reference) || package.Header.Imports[-reference - 1] is not { Kind: PackageObjectKind.ScriptImport } index)
        {
            return null;
        }

        string? path = _packages.ScriptPath(index);
        return path is null ? null : EngineStructs.ByPath.GetValueOrDefault(path) ?? _scriptSchema(path);
    }
}
