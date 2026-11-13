using System.Numerics;
using System.Text;
using Ares.Common.Maps;
using Ares.MapData.Archives;
using Ares.MapData.Packages;
using Ares.Structs.Core;
using Ares.Structs.Game.Collision;
using Ares.Structs.Game.World;
using Vector3D = Ares.Structs.Core.Geometry.Vector3D;

namespace Ares.MapData.Tests;

public sealed class ExtractionTests : IDisposable
{
    private const string BaseCell = "BASE";
    private const string LayeredCell = "LAYERED";
    private const string ScriptPackage = "/Script/Engine";
    private const string StaticMeshActorDefault = "/Script/Engine.Default__StaticMeshActor";
    private const string StaticMeshActorComponent = StaticMeshActorDefault + ":StaticMeshComponent0";

    private static readonly PackageObjectIndex s_runtimeCell = PackageObjectIndex.FromScriptPath(RuntimeCellMembers.ClassPath);
    private static readonly PackageObjectIndex s_actor = PackageObjectIndex.FromScriptPath("/Script/Engine.Actor");
    private static readonly PackageObjectIndex s_staticMeshComponent = PackageObjectIndex.FromScriptPath(StaticMeshComponentMembers.ClassPath);
    private static readonly PackageObjectIndex s_boxComponent = PackageObjectIndex.FromScriptPath(BoxComponentMembers.ClassPath);
    private static readonly PackageObjectIndex s_firstImport = new((ulong)PackageObjectKind.PackageImport << 62);

    private readonly string _directory = Directory.CreateTempSubdirectory("ares-mapdata-").FullName;

    public void Dispose()
    {
        Directory.Delete(_directory, recursive: true);
    }

    private string Game => Path.Combine(_directory, "game");

    private string Output => Path.Combine(_directory, "map-data");

    [Fact]
    public void Extraction_writes_a_collision_file_that_reads_back()
    {
        WriteGame();

        (int code, string output, string error) = Run(Output);

        Assert.True(code == ExtractCommand.Success, error);
        string path = Path.Combine(Output, MapDataFiles.CollisionFileName(DamMap.WorldName));
        Assert.Equal([Path.GetFileName(path)], Directory.GetFiles(Output).Select(Path.GetFileName));
        MapCollision map;
        using (FileStream stream = File.OpenRead(path))
        {
            map = MapCollisionFile.Read(stream);
        }

        Assert.Equal(DamMap.WorldName, map.MapName);
        Assert.Equal(GameBuild.SteamBuildId, map.GameBuildId);
        Assert.Equal([DamMap.MapPackage, Cell(BaseCell), Cell(LayeredCell)], map.Cells.Select(cell => cell.PackageName));
        Assert.True(map.Cells[0].IsBaseMap);
        Assert.True(map.Cells[1].IsBaseMap);
        Assert.Equal([DamDataLayers.All[0].Name], map.Cells[2].DataLayers);

        HeightFieldTile tile = Assert.Single(map.HeightFields);
        Assert.Equal(1, tile.CellIndex);
        Assert.Equal(new Vector3D(1200, 2300, 300), tile.Origin);
        Assert.Equal(LandscapeReaderTests.HeightAt(2, 1), tile.Height(2, 1));

        Assert.Equal(2, map.Placements.Length);
        BodyPlacement mesh = Assert.Single(map.Placements, placement => map.Bodies[placement.BodyIndex].Name == BodyLibraryTests.MeshPath);
        Assert.Equal(1, mesh.CellIndex);
        Assert.Equal(new Vector3D(500, 600, 700), mesh.Translation);
        Assert.Equal(PlacementFlags.BlocksPawn | PlacementFlags.BlocksClimbable, mesh.Flags);
        Assert.Single(map.Bodies[mesh.BodyIndex].Spheres);

        BodyPlacement box = Assert.Single(map.Placements, placement => placement.BodyIndex != mesh.BodyIndex);
        Assert.Equal(new Vector3D(-100, 0, 50), box.Translation);
        Assert.Equal(PlacementFlags.BlocksPawn | PlacementFlags.BlocksClimbable, box.Flags);
        Assert.Equal(new Vector3(50, 60, 70), Assert.Single(map.Bodies[box.BodyIndex].Boxes).HalfExtents);

        Assert.Contains("Placements: 2 of 2 bodies", output, StringComparison.Ordinal);
        Assert.Contains("Landscape tiles: 1", output, StringComparison.Ordinal);
    }

    [Fact]
    public void An_output_folder_inside_the_game_folder_is_a_usage_error()
    {
        Directory.CreateDirectory(Game);

        (int code, _, string error) = Run(Path.Combine(Game, "map-data"));

        Assert.Equal(ExtractCommand.UsageError, code);
        Assert.Contains("inside the game folder", error, StringComparison.Ordinal);
        Assert.False(Directory.Exists(Path.Combine(Game, "map-data")));
    }

    [Fact]
    public void A_wrong_toc_key_is_a_usage_error()
    {
        WriteGame();
        TocKey wrong = TocKey.Parse(new string('0', 64));

        (int code, _, string error) = Run(Path.Combine(_directory, "out"), wrong);

        Assert.Equal(ExtractCommand.UsageError, code);
        Assert.Contains("does not decrypt the game's .utoc files", error, StringComparison.Ordinal);
        Assert.False(Directory.Exists(Path.Combine(_directory, "out")));
    }

    [Fact]
    public void Invalid_landscape_data_is_a_read_error()
    {
        WriteGame(landscapeHeight: double.NaN);

        (int code, _, string error) = Run(Output);

        Assert.Equal(ExtractCommand.ReadError, code);
        Assert.Contains("Height-field tile has an invalid origin", error, StringComparison.Ordinal);
        Assert.False(Directory.Exists(Output));
    }

    private (int Code, string Output, string Error) Run(string output, TocKey? tocKey = null)
    {
        ExtractCommand command = ExtractCommand.Parse(
            ["--game", Game, "--keys", Path.Combine(_directory, "keys.txt"), "--toc-key", Path.Combine(_directory, "toc-key.txt"), "--out", output]);
        StringWriter standardOutput = new();
        StringWriter standardError = new();
        int code = command.Run(ArchiveTests.Keys(), tocKey ?? ArchiveTests.TableKey(), standardOutput, standardError);
        return (code, standardOutput.ToString(), standardError.ToString());
    }

    private static string Cell(string name) => $"{DamMap.MapPackage}/{RuntimeCellMembers.GeneratedFolder}/{name}";

    /// <summary>
    /// A game folder with the collision config pak and one container: the map's persistent level listing a base-map cell
    /// and a layered cell, the base cell (a landscape proxy at <paramref name="landscapeHeight"/>, a static mesh component that uses its mesh's default
    /// collision and a blocking box component), the layered cell (no collision) and the mesh.
    /// </summary>
    private void WriteGame(double landscapeHeight = 300)
    {
        string paks = GameArchive.ContainerDirectory(Game);
        Directory.CreateDirectory(paks);
        SyntheticPak.Write(Path.Combine(Game, CollisionProfileConfig.ConfigPak), PakFile.InstallMountPrefix,
        [
            new SyntheticPakEntry(CollisionProfileConfig.ConfigFiles[0], Encoding.UTF8.GetBytes(CollisionProfilesTests.Base)),
            new SyntheticPakEntry(CollisionProfileConfig.ConfigFiles[1], Encoding.UTF8.GetBytes(CollisionProfilesTests.Default)),
        ]);

        byte[] scriptObjects = SyntheticArchives.ScriptObjects(
        [
            (ScriptPackage, ScriptPackage, null),
            ("Default__StaticMeshActor", StaticMeshActorDefault, ScriptPackage),
            ("StaticMeshComponent0", StaticMeshActorComponent, StaticMeshActorDefault),
            ("Default__BoxComponent", BoxComponentMembers.DefaultObject, ScriptPackage),
        ]);
        SyntheticArchives.WriteContainer(paks, "pakchunk1",
        [
            new SyntheticChunk(new IoChunkId(0, 0, IoChunkType.ScriptObjects), scriptObjects),
            PackageChunk(DamMap.MapPackage, Level(), ".umap"),
            PackageChunk(Cell(BaseCell), BaseCellPackage(landscapeHeight), ".umap"),
            PackageChunk(Cell(LayeredCell), SyntheticArchives.Package(Cell(LayeredCell), [("Info", PackageObjectIndex.Null, s_actor, 0, [])]), ".umap"),
            PackageChunk(BodyLibraryTests.MeshPackage, BodyLibraryTests.MeshPackageBytes(), ".uasset"),
        ]);
    }

    private static SyntheticChunk PackageChunk(string packagePath, byte[] data, string extension)
    {
        string file = $"{SyntheticArchives.MountPoint}{GameBuild.ProjectName}/Content/{packagePath["/Game/".Length..]}{extension}";
        return new SyntheticChunk(new IoChunkId(PackageId.FromPath(packagePath), 0, IoChunkType.ExportBundleData), data, file);
    }

    /// <summary>The persistent level: a runtime cell without data layers and one in the first Dam data layer (name index 3).</summary>
    private static byte[] Level()
    {
        byte[] layered =
        [
            .. PropertyBlocks.Header(RuntimeCellMembers.Unknown0, RuntimeCellMembers.Unknown2, RuntimeCellMembers.DataLayers, 4),
            .. BitConverter.GetBytes(7),
            1,
            .. PropertyBlocks.Header(DataLayerNamesMembers.Unknown0, DataLayerNamesMembers.Names),
            1,
            .. BitConverter.GetBytes(1),
            .. PropertyBlocks.Name(3),
            .. BitConverter.GetBytes(99),
        ];
        return SyntheticArchives.Package(DamMap.MapPackage,
        [
            (BaseCell, PackageObjectIndex.Null, s_runtimeCell, 0, PropertyBlocks.Header()),
            (LayeredCell, PackageObjectIndex.Null, s_runtimeCell, 0, layered),
        ],
        extraNames: [DamDataLayers.All[0].Name]);
    }

    /// <summary>
    /// The base cell: a landscape proxy (exports 0–2) at <paramref name="landscapeHeight"/>, a static mesh actor's component at (500, 600, 700) showing the mesh
    /// (export 4) and a box component at (−100, 0, 50) with half extents (50, 60, 70) and profile BlockAll (export 6,
    /// profile name index 8).
    /// </summary>
    private static byte[] BaseCellPackage(double landscapeHeight)
    {
        const int MeshScene = StaticMeshComponentMembers.ScenePart;
        const int BoxScene = BoxComponentMembers.ScenePart;
        byte[] mesh =
        [
            .. PropertyBlocks.Header(StaticMeshComponentMembers.StaticMesh, MeshScene + SceneComponentMembers.RelativeLocation),
            .. BitConverter.GetBytes(-1),
            .. PropertyBlocks.Vector(500, 600, 700),
            .. new byte[ComponentNativeData.HeadSize + ComponentNativeData.LodCountSize],
            .. ComponentNativeData.StaticMeshTrailer,
        ];
        byte[] box =
        [
            .. PropertyBlocks.Header(BoxComponentMembers.BoxExtent, BoxScene - PrimitiveComponentMembers.BodyInstanceBeforeScenePart,
                BoxScene + SceneComponentMembers.RelativeLocation),
            .. PropertyBlocks.Vector(50, 60, 70),
            .. PropertyBlocks.Header(BodyInstanceMembers.CollisionProfileName),
            .. PropertyBlocks.Name(8),
            .. PropertyBlocks.Vector(-100, 0, 50),
            .. new byte[ComponentNativeData.HeadSize],
        ];
        List<(string Name, PackageObjectIndex Outer, PackageObjectIndex Class, ulong PublicHash, byte[] Payload)> exports =
        [
            .. LandscapeReaderTests.ProxyExports(LandscapeReaderTests.Collision(yaw: 0), landscapeHeight),
            ("StaticMeshActor_0", PackageObjectIndex.Null, s_actor, 0, []),
            ("StaticMeshComponent0", PackageObjectIndex.FromExport(3), s_staticMeshComponent, 0, mesh),
            ("Blocker_0", PackageObjectIndex.Null, s_actor, 0, []),
            ("Box", PackageObjectIndex.FromExport(5), s_boxComponent, 0, box),
        ];
        return SyntheticArchives.Package(Cell(BaseCell), exports,
            importedPackages: [BodyLibraryTests.MeshPackage],
            importedHashes: [BodyLibraryTests.MeshHash],
            imports: [s_firstImport],
            extraNames: ["BlockAll"],
            templates: new Dictionary<int, PackageObjectIndex>
            {
                [4] = PackageObjectIndex.FromScriptPath(StaticMeshActorComponent),
                [6] = PackageObjectIndex.FromScriptPath(BoxComponentMembers.DefaultObject),
            });
    }
}
