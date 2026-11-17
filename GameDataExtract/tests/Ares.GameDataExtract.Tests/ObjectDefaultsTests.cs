using Ares.GameDataExtract.Packages;
using Ares.GameDataExtract.Properties;
using Ares.Structs.Core.Assets;
using Ares.Structs.Core.Geometry;

namespace Ares.GameDataExtract.Tests;

public sealed class ObjectDefaultsTests
{
    private const string PackagePath = "/Game/Things/BP_Thing";
    private const string ParentClass = "/Script/Test.Parent";
    private const string ParentDefaults = "/Script/Test.Default__Parent";
    private const string VectorStruct = "/Script/CoreUObject.Vector";
    private const string GeneratedClass = "/Script/Engine.BlueprintGeneratedClass";

    private static readonly string[] s_exports = ["BP_Thing_C", "Default__BP_Thing_C", "Placed"];
    private static readonly string[] s_names = ["IntProperty", "Speed", "StructProperty", "Offset", "None"];
    private static readonly StructSchema s_parent = new("Parent", [new PropertyField("Health", PropertyType.Int32)]);

    private readonly CookedPackage _package;
    private readonly PackageCache _packages;

    public ObjectDefaultsTests()
    {
        byte[] classExport =
        [
            .. PropertyBlocks.Header(), 0, 0, 0, 0, .. BitConverter.GetBytes(-1), .. BitConverter.GetBytes(0), .. BitConverter.GetBytes(2),
            .. Member("IntProperty", "Speed", sizeof(int)),
            .. Member("StructProperty", "Offset", 24), .. BitConverter.GetBytes(-2),
            .. new byte[8],
        ];
        byte[] defaults =
        [
            .. PropertyBlocks.Header(0, 1, 2), .. BitConverter.GetBytes(3), .. PropertyBlocks.Vector(1, 2, 3), .. BitConverter.GetBytes(50), 0, 0, 0, 0,
        ];
        byte[] placed = [.. PropertyBlocks.Header(0), .. BitConverter.GetBytes(9), 0, 0, 0, 0];
        PackageObjectIndex blueprintClass = PackageObjectIndex.FromExport(0);
        _package = CookedPackage.Parse(SyntheticArchives.Package(PackagePath,
            [
                (s_exports[0], PackageObjectIndex.Null, PackageObjectIndex.FromScriptPath(GeneratedClass), 0, classExport),
                (s_exports[1], PackageObjectIndex.Null, blueprintClass, 0, defaults),
                (s_exports[2], PackageObjectIndex.Null, blueprintClass, 0, placed),
            ],
            imports: [PackageObjectIndex.FromScriptPath(ParentClass), PackageObjectIndex.FromScriptPath(VectorStruct)],
            extraNames: s_names,
            templates: new Dictionary<int, PackageObjectIndex> { [1] = PackageObjectIndex.FromScriptPath(ParentDefaults), [2] = PackageObjectIndex.FromExport(1) },
            supers: new Dictionary<int, PackageObjectIndex> { [0] = PackageObjectIndex.FromScriptPath(ParentClass) }));
        Dictionary<PackageObjectIndex, string> scripts = new[] { ParentClass, ParentDefaults, VectorStruct, GeneratedClass }
            .ToDictionary(PackageObjectIndex.FromScriptPath);
        _packages = new PackageCache(path => path == PackagePath ? _package : null, scripts.GetValueOrDefault);
    }

    [Fact]
    public void A_blueprint_class_declares_its_members_over_its_super_chain()
    {
        ClassSchemas schemas = new(_packages, path => path == ParentClass ? s_parent : null);

        StructSchema schema = schemas.ForObject(Export(1));

        Assert.Equal(3, schema.ChainLength);
        Assert.Equal(["Speed", "Offset"], schema.Members.Select(member => member!.Name));
        Assert.Equal(PropertyKind.Int32, schema.Members[0]!.Type.Kind);
        Assert.Same(EngineStructs.Vector, schema.Members[1]!.Type.Struct);
        Assert.Same(s_parent, schema.Super);
    }

    [Fact]
    public void An_object_resolves_over_its_archetype_chain()
    {
        ClassSchemas schemas = new(_packages, path => path == ParentClass ? s_parent : null);
        ObjectDefaults defaults = new(_packages, schemas.ForObject);

        IReadOnlyList<ExportRef> chain = defaults.Archetypes(Export(2));
        PropertyBag placed = defaults.Resolve(Export(2));

        Assert.Equal([s_exports[2], s_exports[1]], chain.Select(entry => entry.Export.Name));
        Assert.Equal(9, placed.Get<int>("Speed"));
        Assert.Equal(new Vector3D(1, 2, 3), placed.Get<Vector3D>("Offset"));
        Assert.Equal(50, placed.Get<int>("Health"));
        Assert.Equal(3, defaults.Resolve(Export(1)).Get<int>("Speed"));
    }

    [Fact]
    public void A_class_without_a_known_chain_or_member_list_fails()
    {
        ClassSchemas unknownParent = new(_packages, _ => null);
        Assert.Throws<InvalidDataException>(() => unknownParent.ForObject(Export(1)));

        ZenExport broken = _package.Exports[2];
        Assert.Throws<InvalidDataException>(() => BlueprintClassReader.DeclaredMembers(_package, broken, _ => null));
    }

    private ExportRef Export(int index) => new(_package, _package.Exports[index]);

    /// <summary>A stored member: type name, name, flags, array dimension, element size, property flags, replication index, notify function and condition.</summary>
    private static byte[] Member(string type, string name, int size)
    {
        return
        [
            .. PropertyBlocks.Name(NameIndex(type)), .. PropertyBlocks.Name(NameIndex(name)), .. BitConverter.GetBytes(0), .. BitConverter.GetBytes(1),
            .. BitConverter.GetBytes(size), .. BitConverter.GetBytes(0UL), 0, 0, .. PropertyBlocks.Name(NameIndex("None")), 0,
        ];
    }

    private static int NameIndex(string name) => 1 + s_exports.Length + Array.IndexOf(s_names, name);
}
