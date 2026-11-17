using Ares.Structs.Game.World;

namespace Ares.Common.GameData;

/// <summary>
/// File names of the per-map JSON tables (in <c>maps/&lt;map&gt;/</c>, see <see cref="GameDataFiles.MapFile"/>) and
/// their row types in <see cref="GameDataJsonContext"/>.
/// </summary>
public static partial class MapTables
{
    /// <summary>The map's persistent world objects (<see cref="MapWorldObjects"/>), one row.</summary>
    /// <param name="mapName">The map's world name, for example <c>TheDam_02_P</c>.</param>
    public static string World(string mapName) => GameDataFiles.MapFile(mapName, "world.json");

    /// <summary>The map's generated World Partition cell packages (strings), in ordinal order.</summary>
    /// <param name="mapName">The map's world name.</param>
    public static string StreamingCells(string mapName) => GameDataFiles.MapFile(mapName, "streaming-cells.json");

    /// <summary>
    /// The static meshes that carry the map's loot container sockets (<see cref="LootSocketMesh"/>), ordered by path (ordinal);
    /// <see cref="LootSocket.Mesh"/> indexes this table.
    /// </summary>
    /// <param name="mapName">The map's world name.</param>
    public static string LootSocketMeshes(string mapName) => GameDataFiles.MapFile(mapName, "loot-socket-meshes.json");

    /// <summary>
    /// The map's loot container sockets (<see cref="LootSocket"/>), ordered by cell, actor, component and socket name (ordinal).
    /// </summary>
    /// <param name="mapName">The map's world name.</param>
    public static string LootSockets(string mapName) => GameDataFiles.MapFile(mapName, "loot-sockets.json");

    /// <summary>
    /// The loot container actors the map places itself (<see cref="PlacedLootContainer"/>), ordered by cell and actor name (ordinal).
    /// </summary>
    /// <param name="mapName">The map's world name.</param>
    public static string PlacedLoot(string mapName) => GameDataFiles.MapFile(mapName, "placed-loot.json");

    /// <summary>The authored player starts of the map's persistent level (<see cref="PlayerStart"/>), in export order.</summary>
    /// <param name="mapName">The map's world name.</param>
    public static string PlayerStarts(string mapName) => GameDataFiles.MapFile(mapName, "player-starts.json");

    /// <summary>The climbable ladders the map's cells place (<see cref="LadderPlacement"/>), ordered by cell and actor name (ordinal).</summary>
    /// <param name="mapName">The map's world name.</param>
    public static string Ladders(string mapName) => GameDataFiles.MapFile(mapName, "ladders.json");

    /// <summary>
    /// The class the server spawns for each ladder to make it climbable, with its class default (<see cref="ArchetypeClass"/>):
    /// one row, none for a map without ladders.
    /// </summary>
    /// <param name="mapName">The map's world name.</param>
    public static string LadderLogic(string mapName) => GameDataFiles.MapFile(mapName, "ladder-logic.json");

    /// <summary>The rideable ziplines the map's cells place (<see cref="ZiplinePlacement"/>), ordered by cell and actor name (ordinal).</summary>
    /// <param name="mapName">The map's world name.</param>
    public static string ZiplineLines(string mapName) => GameDataFiles.MapFile(mapName, "zipline-lines.json");

    /// <summary>
    /// The zipline launchers the map's cells place (<see cref="ActivatableZiplinePlacement"/>), ordered by cell and actor name
    /// (ordinal).
    /// </summary>
    /// <param name="mapName">The map's world name.</param>
    public static string ZiplineLaunchers(string mapName) => GameDataFiles.MapFile(mapName, "zipline-launchers.json");

    /// <summary>
    /// The map's points of interest (<see cref="PointOfInterest"/>): its named areas with the loot value the map shows,
    /// ordered by actor name (ordinal).
    /// </summary>
    /// <param name="mapName">The map's world name.</param>
    public static string PointsOfInterest(string mapName) => GameDataFiles.MapFile(mapName, "points-of-interest.json");

    /// <summary>The map's loot placement limiters (<see cref="LootLimiter"/>), ordered by cell and actor name (ordinal).</summary>
    /// <param name="mapName">The map's world name.</param>
    public static string LootLimiters(string mapName) => GameDataFiles.MapFile(mapName, "loot-limiters.json");

    /// <summary>
    /// The map's spawners of free-standing loot containers (<see cref="DynamicLootSpawner"/>), ordered by cell and actor
    /// name (ordinal); <see cref="DynamicLootGroupEntry.Spawners"/> indexes this table.
    /// </summary>
    /// <param name="mapName">The map's world name.</param>
    public static string DynamicLootSpawners(string mapName) => GameDataFiles.MapFile(mapName, "dynamic-loot-spawners.json");

    /// <summary>
    /// The groups that pick which of the map's dynamic loot spawners run (<see cref="DynamicLootGroupEntry"/>), ordered by
    /// activity container.
    /// </summary>
    /// <param name="mapName">The map's world name.</param>
    public static string DynamicLootGroups(string mapName) => GameDataFiles.MapFile(mapName, "dynamic-loot-groups.json");
}
