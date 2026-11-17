using Ares.Common.GameData;
using Ares.Structs.Game.World;

namespace Ares.Common.Tests;

/// <summary>
/// Local data checks: the Dam world tables that ARES-GameDataExtract writes from the installed game equal the generated
/// Structs data, row for row and in the same order. They run only when <see cref="DirectoryVariable"/> names an extracted
/// game data directory and are skipped otherwise.
/// </summary>
/// <remarks>
/// One deliberate difference: an override material that is a subobject inside a cell package (a placed light's
/// material instance) is named with the engine's subobject separator, <c>Package.World:PersistentLevel.Actor…</c>, where
/// the generated data joins every segment with <c>.</c>; material paths are compared with <c>:</c> read as <c>.</c>.
/// </remarks>
public sealed class DamWorldDataParityTests
{
    /// <summary>Environment variable naming the extracted game data directory.</summary>
    public const string DirectoryVariable = "ARES_GAMEDATA_DIR";

    [Fact]
    public void World_objects_equal_the_generated_map_identity()
    {
        GameDataDirectory data = OpenOrSkip();

        MapWorldObjects world = Assert.Single(data.ReadTable(MapTables.World(DamMap.WorldName), GameDataJsonContext.Default.MapWorldObjects));

        Assert.Equal(
            new MapWorldObjects(DamMap.WorldName, DamMap.PersistentLevelName, DamMap.LevelScriptActorName, DamMap.LevelScriptClass, DamMap.WorldDataLayersActorName),
            world);
    }

    [Fact]
    public void Streaming_cells_equal_the_generated_cells()
    {
        GameDataDirectory data = OpenOrSkip();

        Assert.Equal(DamStreamingCells.Packages, data.ReadTable(MapTables.StreamingCells(DamMap.WorldName), GameDataJsonContext.Default.String));
    }

    [Fact]
    public void Loot_socket_meshes_equal_the_generated_meshes()
    {
        GameDataDirectory data = OpenOrSkip();

        IReadOnlyList<LootSocketMesh> meshes = data.ReadTable(MapTables.LootSocketMeshes(DamMap.WorldName), GameDataJsonContext.Default.LootSocketMesh);

        Assert.Equal(DamLootSockets.Meshes.Select(mesh => mesh.Path), meshes.Select(mesh => mesh.Path));
        Assert.Equal(DamLootSockets.Meshes.Select(mesh => Materials(mesh.Materials)), meshes.Select(mesh => Materials(mesh.Materials)));
    }

    [Fact]
    public void Loot_sockets_equal_the_generated_sockets()
    {
        GameDataDirectory data = OpenOrSkip();

        IReadOnlyList<LootSocket> sockets = data.ReadTable(MapTables.LootSockets(DamMap.WorldName), GameDataJsonContext.Default.LootSocket);

        Assert.Equal(DamLootSockets.All.Select(WithoutMaterials), sockets.Select(WithoutMaterials));
        Assert.Equal(DamLootSockets.All.Select(socket => Materials(socket.OverrideMaterials)), sockets.Select(socket => Materials(socket.OverrideMaterials)));
    }

    [Fact]
    public void Placed_loot_equals_the_generated_placed_containers()
    {
        GameDataDirectory data = OpenOrSkip();

        Assert.Equal(DamPlacedLoot.Containers, data.ReadTable(MapTables.PlacedLoot(DamMap.WorldName), GameDataJsonContext.Default.PlacedLootContainer));
    }

    private static GameDataDirectory OpenOrSkip()
    {
        string directory = Environment.GetEnvironmentVariable(DirectoryVariable) ?? string.Empty;
        Assert.SkipWhen(directory.Length == 0,
            $"Local data check: set {DirectoryVariable} to a game data directory written by ARES-GameDataExtract extract to compare it with the generated Structs data.");
        return GameDataDirectory.Open(directory);
    }

    /// <summary>The socket with its material list emptied, so that records compare by value.</summary>
    private static LootSocket WithoutMaterials(LootSocket socket) => socket with { OverrideMaterials = [] };

    /// <summary>Material paths with the subobject separator read as <c>.</c> (see the remarks), joined for comparison.</summary>
    private static string Materials(IReadOnlyList<string?> materials) =>
        string.Join('|', materials.Select(material => material?.Replace(':', '.') ?? "(none)"));
}
