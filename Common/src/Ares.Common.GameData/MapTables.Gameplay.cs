using Ares.Structs.Game.World;

namespace Ares.Common.GameData;

/// <summary>The per-map gameplay tables: game mode, doors, rounds, lighting, extraction and shared defaults.</summary>
public static partial class MapTables
{
    /// <summary>The map's game mode, game state and location tag (<see cref="MapGameMode"/>), one row.</summary>
    /// <param name="mapName">The map's world name.</param>
    public static string GameMode(string mapName) => GameDataFiles.MapFile(mapName, "game-mode.json");

    /// <summary>The regular doors the map's cells place (<see cref="DoorPlacement"/>), ordered by cell and actor name (ordinal).</summary>
    /// <param name="mapName">The map's world name.</param>
    public static string Doors(string mapName) => GameDataFiles.MapFile(mapName, "doors.json");

    /// <summary>The map's round configurations (<see cref="RoundConfiguration"/>), in <see cref="RoundConfigurationAssets"/> order.</summary>
    /// <param name="mapName">The map's world name.</param>
    public static string Rounds(string mapName) => GameDataFiles.MapFile(mapName, "rounds.json");

    /// <summary>The scenarios of the map's lighting lookup (<see cref="LightingScenario"/>), in asset order.</summary>
    /// <param name="mapName">The map's world name.</param>
    public static string LightingScenarios(string mapName) => GameDataFiles.MapFile(mapName, "lighting-scenarios.json");

    /// <summary>The round lighting groups of the map's level script (<see cref="RoundLightingGroup"/>), in asset order.</summary>
    /// <param name="mapName">The map's world name.</param>
    public static string RoundLightingGroups(string mapName) => GameDataFiles.MapFile(mapName, "round-lighting-groups.json");

    /// <summary>What the map's elevator extraction points share (<see cref="ExtractionElevator"/>), one row.</summary>
    /// <param name="mapName">The map's world name.</param>
    public static string ExtractionElevator(string mapName) => GameDataFiles.MapFile(mapName, "extraction-elevator.json");

    /// <summary>The buttons of each elevator (<see cref="ExtractionButton"/>), in <see cref="ExtractionClasses.Buttons"/> order.</summary>
    /// <param name="mapName">The map's world name.</param>
    public static string ExtractionButtons(string mapName) => GameDataFiles.MapFile(mapName, "extraction-buttons.json");

    /// <summary>The elevators the map's persistent level places (<see cref="ExtractionPointPlacement"/>), ordered by actor name (ordinal).</summary>
    /// <param name="mapName">The map's world name.</param>
    public static string ExtractionPoints(string mapName) => GameDataFiles.MapFile(mapName, "extraction-points.json");

    /// <summary>What every rideable zipline shares (<see cref="ZiplineLineDefaults"/>), one row.</summary>
    /// <param name="mapName">The map's world name.</param>
    public static string ZiplineDefaults(string mapName) => GameDataFiles.MapFile(mapName, "zipline-defaults.json");

    /// <summary>The capsule of generated group spawn points (<see cref="PlayerStartCapsule"/>), one row.</summary>
    /// <param name="mapName">The map's world name.</param>
    public static string PlayerStartCapsule(string mapName) => GameDataFiles.MapFile(mapName, "player-start-capsule.json");
}
