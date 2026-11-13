using Ares.MapData.Archives;
using Ares.MapData.Packages;

namespace Ares.MapData.Tests;

public sealed class PackageTests : IDisposable
{
    private const string MapPath = "/Game/Maps/Test";
    private const string MeshPath = "/Game/Meshes/Rock";
    private const ulong RockHash = 0xABCDEF;

    private static readonly PackageObjectIndex s_meshComponent = PackageObjectIndex.FromScriptPath("/Script/Engine.StaticMeshComponent");
    private static readonly PackageObjectIndex s_world = PackageObjectIndex.FromScriptPath("/Script/Engine.World");

    private readonly string _game = Directory.CreateTempSubdirectory("ares-mapdata-").FullName;

    public void Dispose()
    {
        Directory.Delete(_game, recursive: true);
    }

    [Fact]
    public void A_minimal_package_parses()
    {
        ZenPackage package = ZenPackage.Parse(MapPackage());

        Assert.Equal(MapPath, package.Name);
        Assert.Equal(["Test", "PersistentLevel", "Rock_1"], package.Exports.Select(export => export.Name));
        Assert.Equal(s_meshComponent, package.Exports[2].Class);
        Assert.Equal("/Game/Maps/Test.Test:PersistentLevel.Rock_1", package.ExportPath(2));
        Assert.Equal("/Game/Maps/Test.Test", package.ExportPath(0));
        Assert.Equal([MeshPath], package.ImportedPackageNames);
        Assert.Equal([RockHash], package.ImportedPublicExportHashes);
        Assert.Equal(3, package.ExportBundle.Count);
    }

    [Fact]
    public void A_game_folder_opens_and_packages_resolve()
    {
        string paks = GameArchive.ContainerDirectory(_game);
        Directory.CreateDirectory(paks);
        SyntheticArchives.WriteContainer(paks, "global",
            [new SyntheticChunk(new IoChunkId(0, 0, IoChunkType.ScriptObjects), SyntheticArchives.ScriptObjects(
            [
                ("/Script/Engine", "/Script/Engine", null),
                ("StaticMeshComponent", "/Script/Engine.StaticMeshComponent", "/Script/Engine"),
                ("World", "/Script/Engine.World", "/Script/Engine"),
            ]))],
            flags: IoContainerFlags.Signed);
        const string Project = Structs.Core.GameBuild.ProjectName;
        byte[] mesh = SyntheticArchives.Package(MeshPath, [("Rock", PackageObjectIndex.Null, s_meshComponent, RockHash, [7, 7])]);
        SyntheticArchives.WriteContainer(paks, "pakchunk1",
        [
            new SyntheticChunk(new IoChunkId(PackageId.FromPath(MapPath), 0, IoChunkType.ExportBundleData), MapPackage(), $"../../../{Project}/Content/Maps/Test.umap"),
            new SyntheticChunk(new IoChunkId(PackageId.FromPath(MapPath + "/Cell"), 0, IoChunkType.ExportBundleData),
                SyntheticArchives.Package(MapPath + "/Cell", []), $"../../../{Project}/Content/Maps/Test/Cell.umap"),
            new SyntheticChunk(new IoChunkId(PackageId.FromPath(MeshPath), 0, IoChunkType.ExportBundleData), mesh, $"../../../{Project}/Content/Meshes/Rock.uasset"),
            new SyntheticChunk(new IoChunkId(PackageId.FromPath(MeshPath), 0, IoChunkType.BulkData), [1, 2, 3], $"../../../{Project}/Content/Meshes/Rock.ubulk"),
        ]);

        using GameArchive archive = GameArchive.Open(_game, ArchiveTests.Keys(), ArchiveTests.TableKey());

        Assert.Equal(2, archive.Containers.Count);
        Assert.Equal(3, archive.PackageCount);
        Assert.Equal(3, archive.ScriptObjects.Count);
        Assert.Equal([MapPath, MapPath + "/Cell", MeshPath], archive.PackagePaths);
        Assert.Equal([MapPath + "/Cell"], archive.PackagePathsUnder(MapPath));
        PackageLocation location = Assert.IsType<PackageLocation>(archive.FindPackage(MapPath));
        ZenPackage map = GameArchive.ReadPackage(location);
        Assert.Equal([5, 6, 7], GameArchive.ReadExport(location, map.Exports[1]));
        Assert.Equal("/Script/Engine.StaticMeshComponent", archive.ResolveObjectPath(map, map.Exports[2].Class));
        Assert.Equal("/Game/Maps/Test.Test:PersistentLevel", archive.ResolveObjectPath(map, map.Exports[2].Outer));
        Assert.Equal("/Game/Meshes/Rock.Rock", archive.ResolveObjectPath(map, map.Imports[0]));
        Assert.Null(archive.ResolveObjectPath(map, PackageObjectIndex.Null));
    }

    [Fact]
    public void Missing_containers_and_keys_are_reported()
    {
        Assert.Throws<DirectoryNotFoundException>(() => GameArchive.Open(_game, ArchiveTests.Keys(), ArchiveTests.TableKey()));

        string paks = GameArchive.ContainerDirectory(_game);
        Directory.CreateDirectory(paks);
        SyntheticArchives.WriteContainer(paks, "global", [new SyntheticChunk(new IoChunkId(0, 0, IoChunkType.ScriptObjects), SyntheticArchives.ScriptObjects([]))]);
        TocKey wrongTableKey = TocKey.Parse(Convert.ToHexString([.. SyntheticArchives.TocKey.Select(b => (byte)(b ^ 1))]));
        Assert.Throws<WrongTocKeyException>(() => GameArchive.Open(_game, ArchiveTests.Keys(), wrongTableKey));
    }

    [Theory]
    [InlineData("../../../Proj/Content/Maps/A.umap", "/Game/Maps/A")]
    [InlineData("../../../Engine/Content/Basic/Cube.uasset", "/Engine/Basic/Cube")]
    [InlineData("../../../Proj/Plugins/Feature/Content/Thing.uasset", "/Feature/Thing")]
    [InlineData("../../../Proj/Content/Maps/A.ubulk", null)]
    [InlineData("../../../Proj/Config/Default.ini", null)]
    public void Container_file_paths_map_to_package_paths(string filePath, string? expected)
    {
        string projectPath = filePath.Replace("/Proj/", "/" + Structs.Core.GameBuild.ProjectName + "/", StringComparison.Ordinal);

        Assert.Equal(expected, GameArchive.PackagePathOf(projectPath));
    }

    private static byte[] MapPackage()
    {
        return SyntheticArchives.Package(MapPath,
            [
                ("Test", PackageObjectIndex.Null, s_world, 1, [1, 2, 3, 4]),
                ("PersistentLevel", PackageObjectIndex.FromExport(0), PackageObjectIndex.Null, 0, [5, 6, 7]),
                ("Rock_1", PackageObjectIndex.FromExport(1), s_meshComponent, 0, []),
            ],
            importedPackages: [MeshPath],
            importedHashes: [RockHash],
            imports: [new PackageObjectIndex((2UL << 62) | 0)]);
    }
}
