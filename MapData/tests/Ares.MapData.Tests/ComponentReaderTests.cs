using Ares.MapData.Components;
using Ares.MapData.Packages;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Game.Collision;
using static Ares.MapData.Tests.PropertyBlocks;

namespace Ares.MapData.Tests;

public sealed class ComponentReaderTests
{
    private const string CellPath = "/Game/Maps/Cell";
    private const int MeshScene = StaticMeshComponentMembers.ScenePart;
    private const int HismChain = HierarchicalInstancedStaticMeshComponentMembers.StaticMeshComponentChainStart;
    private const int MaxExports = 5;
    private const byte LodGlobalFlags = StaticMeshLodInfoNativeData.EditorDataStripped | 4;
    private const int EngineName = 1 + MaxExports;
    private const int BlockAllName = EngineName + 1;

    private static readonly PackageObjectIndex s_actor = PackageObjectIndex.FromScriptPath("/Script/Engine.Actor");
    private static readonly PackageObjectIndex s_sceneComponent = PackageObjectIndex.FromScriptPath(SceneComponentMembers.ClassPath);
    private static readonly PackageObjectIndex s_staticMeshComponent = PackageObjectIndex.FromScriptPath(StaticMeshComponentMembers.ClassPath);
    private static readonly PackageObjectIndex s_hism = PackageObjectIndex.FromScriptPath(HierarchicalInstancedStaticMeshComponentMembers.ClassPath);
    private static readonly PackageObjectIndex s_firstImport = new((ulong)PackageObjectKind.PackageImport << 62);

    [Fact]
    public void One_byte_scene_members_after_the_scale_are_stepped_over()
    {
        byte[] component =
        [
            .. Header(MeshScene + SceneComponentMembers.RelativeLocation, MeshScene + SceneComponentMembers.Unknown38, MeshScene + SceneComponentMembers.Unknown39),
            .. Vector(1, 2, 3),
            1,
            1,
            .. EmptyStaticMeshNativeData(),
        ];
        (ComponentReader reader, CookedPackage cell) = Read([("Actor", PackageObjectIndex.Null, s_actor, 0, []), ("Mesh", PackageObjectIndex.FromExport(0), s_staticMeshComponent, 0, component)]);

        Assert.Equal(new Vector3D(1, 2, 3), reader.WorldTransform(cell, cell.Exports[1]).Translation);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Static_mesh_native_data_with_lod_infos_is_stepped_over(bool overrideColors)
    {
        byte[] stripped = [LodGlobalFlags, StaticMeshLodInfoNativeData.OverrideColorsStripped, .. new byte[StaticMeshLodInfoNativeData.FixedSize]];
        byte[] colored =
        [
            LodGlobalFlags, 0, .. new byte[StaticMeshLodInfoNativeData.FixedSize],
            1,
            5, 0, .. BitConverter.GetBytes(StaticMeshLodInfoNativeData.ColorSize), .. BitConverter.GetBytes(2),
            .. BitConverter.GetBytes(StaticMeshLodInfoNativeData.ColorSize), .. BitConverter.GetBytes(2),
            0, 0, 0, 0xFF, 0x10, 0x10, 0x10, 0xFF,
        ];
        byte[] component =
        [
            .. Header(MeshScene + SceneComponentMembers.RelativeLocation),
            .. Vector(4, 5, 6),
            .. new byte[ComponentNativeData.HeadSize],
            .. BitConverter.GetBytes(2),
            .. stripped,
            .. overrideColors ? colored : stripped,
            .. ComponentNativeData.StaticMeshTrailer,
        ];
        (ComponentReader reader, CookedPackage cell) = Read([("Actor", PackageObjectIndex.Null, s_actor, 0, []), ("Mesh", PackageObjectIndex.FromExport(0), s_staticMeshComponent, 0, component)]);

        Assert.Equal(new Vector3D(4, 5, 6), reader.WorldTransform(cell, cell.Exports[1]).Translation);
    }

    [Fact]
    public void A_socket_the_parent_mesh_lacks_attaches_to_the_parent_transform()
    {
        byte[] parent =
        [
            .. Header(StaticMeshComponentMembers.StaticMesh, MeshScene + SceneComponentMembers.RelativeLocation),
            .. BitConverter.GetBytes(-1),
            .. Vector(100, 0, 0),
            .. EmptyStaticMeshNativeData(),
        ];
        byte[] child =
        [
            .. Header(MeshScene + SceneComponentMembers.AttachParent, MeshScene + SceneComponentMembers.AttachSocketName, MeshScene + SceneComponentMembers.RelativeLocation),
            .. BitConverter.GetBytes(2),
            .. Name(EngineName),
            .. Vector(0, 0, 50),
            .. EmptyStaticMeshNativeData(),
        ];
        (ComponentReader reader, CookedPackage cell) = Read(
        [
            ("Actor", PackageObjectIndex.Null, s_actor, 0, []),
            ("Base", PackageObjectIndex.FromExport(0), s_staticMeshComponent, 0, parent),
            ("Engine", PackageObjectIndex.FromExport(0), s_staticMeshComponent, 0, child),
        ]);

        Assert.Equal(new Vector3D(100, 0, 50), reader.WorldTransform(cell, cell.Exports[2]).Translation);
    }

    [Fact]
    public void A_body_instance_reads_through_its_other_fixed_size_members()
    {
        byte[] body =
        [
            .. Header(BodyInstanceMembers.CollisionEnabled, BodyInstanceMembers.Unknown20, BodyInstanceMembers.CollisionProfileName, BodyInstanceMembers.Unknown33,
                BodyInstanceMembers.Unknown48, BodyInstanceMembers.Unknown49),
            (byte)CollisionEnabled.QueryOnly,
            1,
            .. Name(2),
            .. BitConverter.GetBytes(40f),
            1,
            1,
        ];
        CookedPackage package = CookedPackage.Parse(SyntheticArchives.Package(CellPath, [("Body", PackageObjectIndex.Null, s_actor, 0, body)], extraNames: ["BlockAll"]));
        int start = CookedPackage.StartOf(package.Exports[0]);

        BodyInstanceValues? values = BodyInstanceReader.TryRead(package, start, out int end);

        Assert.Equal("BlockAll", values?.ProfileName);
        Assert.Equal(CollisionEnabled.QueryOnly, values?.CollisionEnabled);
        Assert.Equal(CookedPackage.EndOf(package.Exports[0]), end);
    }

    [Fact]
    public void A_template_instanced_component_with_its_own_body_instance_attaches_where_that_body_ends()
    {
        byte[] root = [.. Header(SceneComponentMembers.RelativeLocation), .. Vector(10, 20, 30), .. new byte[ComponentNativeData.HeadSize]];
        byte[] other = [.. Header(SceneComponentMembers.RelativeLocation), .. Vector(-5, 0, 0), .. new byte[ComponentNativeData.HeadSize]];
        byte[] component =
        [
            .. Header(HismChain + StaticMeshComponentMembers.BodyInstance, HismChain + MeshScene + SceneComponentMembers.AttachParent),
            .. Header(BodyInstanceMembers.CollisionProfileName, BodyInstanceMembers.Unknown42),
            .. Name(BlockAllName),
            .. BitConverter.GetBytes(3),
            .. BitConverter.GetBytes(2),
        ];
        byte[] template =
        [
            .. Header(HismChain + StaticMeshComponentMembers.StaticMesh, HismChain + StaticMeshComponentMembers.BodyInstance),
            .. BitConverter.GetBytes(-1),
            .. Header(BodyInstanceMembers.CollisionEnabled),
            (byte)CollisionEnabled.QueryOnly,
            .. BitConverter.GetBytes(InstancedStaticMeshComponentMembers.InstanceDataElementSize),
            .. BitConverter.GetBytes(1),
            .. Vector(1, 0, 0), .. BitConverter.GetBytes(0.0),
            .. Vector(0, 1, 0), .. BitConverter.GetBytes(0.0),
            .. Vector(0, 0, 1), .. BitConverter.GetBytes(0.0),
            .. Vector(1, 2, 3), .. BitConverter.GetBytes(1.0),
        ];
        (ComponentReader reader, CookedPackage cell) = Read(
        [
            ("Actor", PackageObjectIndex.Null, s_actor, 0, []),
            ("Root", PackageObjectIndex.FromExport(0), s_sceneComponent, 0, root),
            ("Other", PackageObjectIndex.FromExport(0), s_sceneComponent, 0, other),
            ("Instances", PackageObjectIndex.FromExport(0), s_hism, 0, component),
            ("Template", PackageObjectIndex.Null, s_hism, 0, template),
        ],
        new Dictionary<int, PackageObjectIndex> { [3] = PackageObjectIndex.FromExport(4) });

        InstancedComponent instanced = reader.ReadInstanced(cell, cell.Exports[3], InstancedComponentKind.HierarchicalInstanced);

        Assert.Equal(new Vector3D(11, 22, 33), Assert.Single(instanced.Instances).Origin);
        Assert.Equal("BlockAll", instanced.Body?.ProfileName);
        Assert.Equal(CollisionEnabled.QueryOnly, instanced.Body?.CollisionEnabled);
    }

    [Fact]
    public void Instances_follow_the_component_relative_transform_over_its_template()
    {
        byte[] root = [.. Header(SceneComponentMembers.RelativeLocation), .. Vector(10, 20, 30), .. new byte[ComponentNativeData.HeadSize]];
        byte[] other = [.. Header(SceneComponentMembers.RelativeLocation), .. Vector(-5, 0, 0), .. new byte[ComponentNativeData.HeadSize]];
        byte[] component =
        [
            .. Header(HismChain + MeshScene + SceneComponentMembers.AttachParent, HismChain + MeshScene + SceneComponentMembers.RelativeLocation,
                HismChain + MeshScene + SceneComponentMembers.RelativeRotation),
            .. BitConverter.GetBytes(2),
            .. Vector(100, 0, 0),
            .. Vector(0, 90, 0),
        ];
        byte[] template =
        [
            .. Header(HismChain + StaticMeshComponentMembers.StaticMesh, HismChain + StaticMeshComponentMembers.BodyInstance,
                HismChain + MeshScene + SceneComponentMembers.RelativeScale3D),
            .. BitConverter.GetBytes(-1),
            .. Header(BodyInstanceMembers.CollisionEnabled),
            (byte)CollisionEnabled.QueryOnly,
            .. Vector(2, 2, 2),
            .. BitConverter.GetBytes(InstancedStaticMeshComponentMembers.InstanceDataElementSize),
            .. BitConverter.GetBytes(1),
            .. Vector(1, 0, 0), .. BitConverter.GetBytes(0.0),
            .. Vector(0, 1, 0), .. BitConverter.GetBytes(0.0),
            .. Vector(0, 0, 1), .. BitConverter.GetBytes(0.0),
            .. Vector(1, 2, 3), .. BitConverter.GetBytes(1.0),
        ];
        (ComponentReader reader, CookedPackage cell) = Read(
        [
            ("Actor", PackageObjectIndex.Null, s_actor, 0, []),
            ("Root", PackageObjectIndex.FromExport(0), s_sceneComponent, 0, root),
            ("Other", PackageObjectIndex.FromExport(0), s_sceneComponent, 0, other),
            ("Instances", PackageObjectIndex.FromExport(0), s_hism, 0, component),
            ("Template", PackageObjectIndex.Null, s_hism, 0, template),
        ],
        new Dictionary<int, PackageObjectIndex> { [3] = PackageObjectIndex.FromExport(4) });

        TransformMatrix instance = Assert.Single(reader.ReadInstanced(cell, cell.Exports[3], InstancedComponentKind.HierarchicalInstanced).Instances);

        // (1, 2, 3) scaled by the template's 2, turned 90° about Z, moved by the component's (100, 0, 0) and the root's (10, 20, 30).
        Assert.Equal(106, instance.Origin.X, 9);
        Assert.Equal(22, instance.Origin.Y, 9);
        Assert.Equal(36, instance.Origin.Z, 9);
        Assert.Equal(0, instance.AxisX.X, 9);
        Assert.Equal(2, instance.AxisX.Y, 9);
    }

    /// <summary>The native data of a static mesh component without entries or LOD infos.</summary>
    private static byte[] EmptyStaticMeshNativeData() =>
        [.. new byte[ComponentNativeData.HeadSize + ComponentNativeData.LodCountSize], .. ComponentNativeData.StaticMeshTrailer];

    /// <summary>
    /// A reader over a cell package with <paramref name="exports"/> (at most <see cref="MaxExports"/>) that imports the test
    /// mesh (import reference −1); "Engine" and "BlockAll" are names <see cref="EngineName"/> and <see cref="BlockAllName"/>.
    /// </summary>
    private static (ComponentReader Reader, CookedPackage Cell) Read(
        IReadOnlyList<(string Name, PackageObjectIndex Outer, PackageObjectIndex Class, ulong PublicHash, byte[] Payload)> exports,
        IReadOnlyDictionary<int, PackageObjectIndex>? templates = null)
    {
        List<string> extraNames = [.. Enumerable.Repeat("Unused", MaxExports - exports.Count), "Engine", "BlockAll"];
        CookedPackage cell = CookedPackage.Parse(SyntheticArchives.Package(CellPath, exports,
            importedPackages: [BodyLibraryTests.MeshPackage],
            importedHashes: [BodyLibraryTests.MeshHash],
            imports: [s_firstImport],
            extraNames: extraNames,
            templates: templates));
        CookedPackage mesh = CookedPackage.Parse(BodyLibraryTests.MeshPackageBytes());
        PackageCache cache = new(path => path switch
        {
            CellPath => cell,
            BodyLibraryTests.MeshPackage => mesh,
            _ => null,
        }, _ => null);
        return (new ComponentReader(cache), cell);
    }
}
