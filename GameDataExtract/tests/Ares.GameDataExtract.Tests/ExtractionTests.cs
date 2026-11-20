using System.Numerics;
using System.Text;
using Ares.Common;
using Ares.Common.GameData;
using Ares.Common.Maps;
using Ares.GameDataExtract.Archives;
using Ares.GameDataExtract.Packages;
using Ares.GameDataExtract.Registry;
using Ares.GameDataExtract.Scripts;
using Ares.Structs.Core;
using Ares.Structs.Game;
using Ares.Structs.Game.Collision;
using Ares.Structs.Game.Loot;
using Ares.Structs.Game.Movement;
using Ares.Structs.Game.World;
using Vector3D = Ares.Structs.Core.Geometry.Vector3D;

namespace Ares.GameDataExtract.Tests;

public sealed class ExtractionTests : IDisposable
{
    private const string MapPackage = "/Game/Maps/" + GameNames.Maps.Dam;
    private const string BaseCell = "BASE";
    private const string LayeredCell = "LAYERED";
    private const string ScriptPackage = "/Script/Engine";
    private const string StaticMeshActorDefault = "/Script/Engine.Default__StaticMeshActor";
    private const string StaticMeshActorComponent = StaticMeshActorDefault + ":StaticMeshComponent0";

    private static readonly PackageObjectIndex s_runtimeCell = PackageObjectIndex.FromScriptPath(RuntimeCellMembers.ClassPath);
    private static readonly PackageObjectIndex s_actor = PackageObjectIndex.FromScriptPath("/Script/Engine.Actor");
    private static readonly PackageObjectIndex s_staticMeshComponent = PackageObjectIndex.FromScriptPath(StaticMeshComponentMembers.ClassPath);
    private static readonly PackageObjectIndex s_boxComponent = PackageObjectIndex.FromScriptPath(BoxComponentMembers.ClassPath);
    private static readonly PackageObjectIndex s_world = PackageObjectIndex.FromScriptPath(MapWorldClasses.World);
    private static readonly PackageObjectIndex s_level = PackageObjectIndex.FromScriptPath(MapWorldClasses.Level);
    private static readonly PackageObjectIndex s_worldDataLayers = PackageObjectIndex.FromScriptPath(MapWorldClasses.WorldDataLayers);
    private static readonly PackageObjectIndex s_blueprintClass = PackageObjectIndex.FromScriptPath("/Script/Engine.BlueprintGeneratedClass");
    private static readonly PackageObjectIndex s_firstImport = new((ulong)PackageObjectKind.PackageImport << 62);

    private readonly string _directory = Directory.CreateTempSubdirectory("ares-gamedata-").FullName;

    public void Dispose()
    {
        Directory.Delete(_directory, recursive: true);
    }

    private string Game => Path.Combine(_directory, "game");

    private string Output => Path.Combine(_directory, "gamedata");

    [Fact]
    public void Extraction_writes_a_collision_file_world_movement_loot_and_loot_area_tables_and_a_manifest_that_read_back()
    {
        WriteGame();

        (int code, string output, string error) = Run(Output);

        Assert.True(code == ExtractCommand.Success, error);
        string collision = GameDataFiles.CollisionPath(GameNames.Maps.Dam);
        string[] tables =
        [
            MapTables.World(GameNames.Maps.Dam), MapTables.StreamingCells(GameNames.Maps.Dam), MapTables.LootSocketMeshes(GameNames.Maps.Dam),
            MapTables.LootSockets(GameNames.Maps.Dam), MapTables.LooseItemSockets(GameNames.Maps.Dam), MapTables.PlacedLoot(GameNames.Maps.Dam),
            MapTables.PlayerStarts(GameNames.Maps.Dam),
            MapTables.Ladders(GameNames.Maps.Dam), MapTables.LadderLogic(GameNames.Maps.Dam), MapTables.ZiplineLines(GameNames.Maps.Dam),
            MapTables.ZiplineLaunchers(GameNames.Maps.Dam), MapTables.PointsOfInterest(GameNames.Maps.Dam), MapTables.LootLimiters(GameNames.Maps.Dam),
            MapTables.DynamicLootSpawners(GameNames.Maps.Dam), MapTables.DynamicLootGroups(GameNames.Maps.Dam),
            GameTables.DodgeRolls, GameTables.VaultDefinitions, GameTables.HeavyLandingLevels, GameTables.RecoveryLandingLevels,
            GameTables.GraphStateTags, GameTables.SnapHookItem, GameTables.SprintBurstCurve,
            GameTables.LootContainerTypes, GameTables.LootContainerClasses, GameTables.LootContainerDefaults, GameTables.StaticValueLootTables,
            GameTables.SkillLootTables, GameTables.StaticLootSpawnTweakables,
        ];
        Assert.Equal(
            tables.Append(collision).Append(GameDataFiles.ManifestFileName).Order(StringComparer.Ordinal),
            Directory.GetFiles(Output, "*", SearchOption.AllDirectories).Select(file => Path.GetRelativePath(Output, file).Replace(Path.DirectorySeparatorChar, '/')).Order(StringComparer.Ordinal));
        GameDataDirectory data = GameDataDirectory.Open(Output);
        Assert.Equal(GameBuild.GameDataBuildId, data.Manifest.GameBuildId);
        Assert.Equal(AresVersion.Current, data.Manifest.ExtractorVersion);
        GameDataFileEntry entry = Assert.Single(data.Manifest.Files, file => file.Name == collision);
        Assert.Null(entry.RowCount);
        Assert.Equal(
            new MapWorldObjects(GameNames.Maps.Dam, "PersistentLevel", "LevelScript_1", $"{MapPackage}.{GameNames.Maps.Dam}_C", "WorldDataLayers"),
            Assert.Single(data.ReadTable(MapTables.World(GameNames.Maps.Dam), GameDataJsonContext.Default.MapWorldObjects)));
        Assert.Equal([Cell(BaseCell), Cell(LayeredCell)], data.ReadTable(MapTables.StreamingCells(GameNames.Maps.Dam), GameDataJsonContext.Default.String));
        Assert.Empty(data.ReadTable(MapTables.LootSockets(GameNames.Maps.Dam), GameDataJsonContext.Default.LootSocket));
        Assert.Empty(data.ReadTable(MapTables.LooseItemSockets(GameNames.Maps.Dam), GameDataJsonContext.Default.LooseItemSocket));
        Assert.Empty(data.ReadTable(MapTables.PlacedLoot(GameNames.Maps.Dam), GameDataJsonContext.Default.PlacedLootContainer));
        Assert.Empty(data.ReadTable(MapTables.PlayerStarts(GameNames.Maps.Dam), GameDataJsonContext.Default.PlayerStart));
        Assert.Empty(data.ReadTable(MapTables.Ladders(GameNames.Maps.Dam), GameDataJsonContext.Default.LadderPlacement));
        Assert.Empty(data.ReadTable(MapTables.LadderLogic(GameNames.Maps.Dam), GameDataJsonContext.Default.ArchetypeClass));
        Assert.Empty(data.ReadTable(MapTables.ZiplineLines(GameNames.Maps.Dam), GameDataJsonContext.Default.ZiplinePlacement));
        Assert.Empty(data.ReadTable(MapTables.ZiplineLaunchers(GameNames.Maps.Dam), GameDataJsonContext.Default.ActivatableZiplinePlacement));
        Assert.All(data.ReadTable(GameTables.DodgeRolls, GameDataJsonContext.Default.DodgeRollSequence), roll => Assert.Equal(SyntheticMovementGame.RollDistance, roll.Distance));
        Assert.Equal(new SnapHookItemDefaults(MovementAssets.SnapHookDataAsset, 1800, 60), Assert.Single(data.ReadTable(GameTables.SnapHookItem, GameDataJsonContext.Default.SnapHookItemDefaults)));
        Assert.Equal(SyntheticMovementGame.TagCount, data.ReadTable(GameTables.GraphStateTags, GameDataJsonContext.Default.String).Length);
        Assert.Empty(data.ReadTable(GameTables.LootContainerClasses, GameDataJsonContext.Default.LootContainerClass));
        LootContainerDefaults defaults = Assert.Single(data.ReadTable(GameTables.LootContainerDefaults, GameDataJsonContext.Default.LootContainerDefaults));
        Assert.Equal(SyntheticLootScripts.CullDistance, defaults.NetCullDistance);
        Assert.Equal(SyntheticLootScripts.TargetStacks, defaults.TargetStacks);
        Assert.Equal(
            SyntheticLootScripts.Tweakables.ContainerContentsWeights,
            Assert.Single(data.ReadTable(GameTables.StaticLootSpawnTweakables, GameDataJsonContext.Default.LootSpawnTweakables)).ContainerContentsWeights);
        Assert.Empty(data.ReadTable(MapTables.PointsOfInterest(GameNames.Maps.Dam), GameDataJsonContext.Default.PointOfInterest));
        Assert.Empty(data.ReadTable(MapTables.LootLimiters(GameNames.Maps.Dam), GameDataJsonContext.Default.LootLimiter));
        Assert.Empty(data.ReadTable(MapTables.DynamicLootSpawners(GameNames.Maps.Dam), GameDataJsonContext.Default.DynamicLootSpawner));
        Assert.Empty(data.ReadTable(MapTables.DynamicLootGroups(GameNames.Maps.Dam), GameDataJsonContext.Default.DynamicLootGroupEntry));
        MapCollision map;
        using (FileStream stream = data.OpenFile(collision))
        {
            map = MapCollisionFile.Read(stream);
        }

        Assert.Equal(GameNames.Maps.Dam, map.MapName);
        Assert.Equal(GameBuild.GameDataBuildId, map.GameBuildId);
        Assert.Equal([MapPackage, Cell(BaseCell), Cell(LayeredCell)], map.Cells.Select(cell => cell.PackageName));
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
        Assert.Equal(PlacementFlags.BlocksPawn | PlacementFlags.BlocksClimbable, mesh.Flags & (PlacementFlags.BlocksPawn | PlacementFlags.BlocksClimbable));
        Assert.False(mesh.Flags.Blocks(GameCollisionChannels.HitScan));
        Assert.True(mesh.Flags.Blocks(GameCollisionChannels.PlayerTraceProjectile));
        Assert.Single(map.Bodies[mesh.BodyIndex].Spheres);

        BodyPlacement box = Assert.Single(map.Placements, placement => placement.BodyIndex != mesh.BodyIndex);
        Assert.Equal(new Vector3D(-100, 0, 50), box.Translation);
        Assert.Equal(mesh.Flags, box.Flags);
        Assert.Equal(new Vector3(50, 60, 70), Assert.Single(map.Bodies[box.BodyIndex].Boxes).HalfExtents);
        Assert.Equal(CollisionTraceFlag.SimpleAsComplex, map.Bodies[box.BodyIndex].TraceFlag);

        Assert.Contains("Placements: 2 of 2 bodies", output, StringComparison.Ordinal);
        Assert.Contains("Landscape tiles: 1", output, StringComparison.Ordinal);
        Assert.Contains("Movement tables: ", output, StringComparison.Ordinal);
        Assert.Contains("Loot tables: ", output, StringComparison.Ordinal);
        Assert.Contains("Loot area tables: ", output, StringComparison.Ordinal);
        Assert.DoesNotContain("Item tables: ", output, StringComparison.Ordinal);
        Assert.DoesNotContain("Gameplay tables: ", output, StringComparison.Ordinal);
    }

    [Fact]
    public void An_output_folder_inside_the_game_folder_is_a_usage_error()
    {
        Directory.CreateDirectory(Game);

        (int code, _, string error) = Run(Path.Combine(Game, "gamedata"));

        Assert.Equal(ExtractCommand.UsageError, code);
        Assert.Contains("inside the game folder", error, StringComparison.Ordinal);
        Assert.False(Directory.Exists(Path.Combine(Game, "gamedata")));
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

    /// <summary>
    /// Runs the command without the item and gameplay tables, which need the game's whole item catalogue and its gameplay
    /// assets (see <c>ItemReaderTests</c> and the local data checks); the world, movement, loot and loot area tables are
    /// always extracted, from the synthetic game.
    /// </summary>
    private (int Code, string Output, string Error) Run(string output, TocKey? tocKey = null)
    {
        ExtractCommand command = ExtractCommand.Parse(
            ["--game", Game, "--keys", Path.Combine(_directory, "keys.txt"), "--toc-key", Path.Combine(_directory, "toc-key.txt"), "--out", output]);
        StringWriter standardOutput = new();
        StringWriter standardError = new();
        int code = command.Run(ArchiveTests.Keys(), tocKey ?? ArchiveTests.TableKey(), standardOutput, standardError, gameAssetTables: false);
        return (code, standardOutput.ToString(), standardError.ToString());
    }

    /// <summary>
    /// The script cache of the synthetic game: the loot scripts (see <see cref="SyntheticLootScripts"/>), the movement
    /// classes and structs, and the loot area classes (see <see cref="SyntheticLootAreaScripts"/>).
    /// </summary>
    private static byte[] ScriptCache()
    {
        SyntheticScriptCache cache = SyntheticLootScripts.Cache();
        SyntheticMovementGame.AddScripts(cache);
        SyntheticLootAreaScripts.AddScripts(cache);
        return cache.Write();
    }

    private static string Cell(string name) => $"{MapPackage}/{RuntimeCellMembers.GeneratedFolder}/{name}";

    /// <summary>
    /// A game folder with the collision config pak and one container: the map's persistent level listing a base-map cell
    /// and a layered cell, the base cell (a landscape proxy at <paramref name="landscapeHeight"/>, a static mesh component that uses its mesh's default
    /// collision and a blocking box component), the layered cell (no collision), the mesh, and the movement assets (see
    /// <see cref="SyntheticMovementGame"/>), an empty asset registry and the loot, movement and loot area script cache.
    /// </summary>
    private void WriteGame(double landscapeHeight = 300)
    {
        string paks = GameArchive.ContainerDirectory(Game);
        Directory.CreateDirectory(paks);
        SyntheticPak.Write(Path.Combine(Game, CollisionProfileConfig.ConfigPak), PakFile.InstallMountPrefix,
        [
            new SyntheticPakEntry(CollisionProfileConfig.ConfigFiles[0], Encoding.UTF8.GetBytes(CollisionProfilesTests.Base)),
            new SyntheticPakEntry(CollisionProfileConfig.ConfigFiles[1], Encoding.UTF8.GetBytes(CollisionProfilesTests.Default)),
            new SyntheticPakEntry(AssetRegistry.PakPath, SyntheticLootScripts.EmptyRegistry()),
            new SyntheticPakEntry(ScriptCacheReader.CachePath, ScriptCache()),
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
            PackageChunk(MapPackage, Level(), ".umap"),
            PackageChunk(Cell(BaseCell), BaseCellPackage(landscapeHeight), ".umap"),
            PackageChunk(Cell(LayeredCell), SyntheticArchives.Package(Cell(LayeredCell), [("Info", PackageObjectIndex.Null, s_actor, 0, [])]), ".umap"),
            PackageChunk(BodyLibraryTests.MeshPackage, BodyLibraryTests.MeshPackageBytes(), ".uasset"),
            .. SyntheticMovementGame.Packages().Select(package => PackageChunk(package.Path, package.Data, ".uasset")),
        ]);
    }

    private static SyntheticChunk PackageChunk(string packagePath, byte[] data, string extension)
    {
        string file = $"{SyntheticArchives.MountPoint}{GameBuild.ProjectName}/Content/{packagePath["/Game/".Length..]}{extension}";
        return new SyntheticChunk(new IoChunkId(PackageId.FromPath(packagePath), 0, IoChunkType.ExportBundleData), data, file);
    }

    /// <summary>
    /// The map package: a runtime cell without data layers and one in the first Dam data layer (name index 8), then the
    /// world, its persistent level, the level script class and the level's level script and world data layers actors.
    /// </summary>
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
            .. PropertyBlocks.Name(8),
            .. BitConverter.GetBytes(99),
        ];
        return SyntheticArchives.Package(MapPackage,
        [
            (BaseCell, PackageObjectIndex.Null, s_runtimeCell, 0, PropertyBlocks.Header()),
            (LayeredCell, PackageObjectIndex.Null, s_runtimeCell, 0, layered),
            (GameNames.Maps.Dam, PackageObjectIndex.Null, s_world, 0, []),
            ("PersistentLevel", PackageObjectIndex.FromExport(2), s_level, 0, []),
            (GameNames.Maps.Dam + "_C", PackageObjectIndex.Null, s_blueprintClass, 0, []),
            ("LevelScript_1", PackageObjectIndex.FromExport(3), PackageObjectIndex.FromExport(4), 0, []),
            ("WorldDataLayers", PackageObjectIndex.FromExport(3), s_worldDataLayers, 0, []),
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
