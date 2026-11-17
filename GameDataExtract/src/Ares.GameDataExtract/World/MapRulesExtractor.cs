using System.Collections.Immutable;
using Ares.GameDataExtract.Components;
using Ares.GameDataExtract.Packages;
using Ares.GameDataExtract.Scripts;
using Ares.Structs.Game.World;

namespace Ares.GameDataExtract.World;

/// <summary>The gameplay tables of one map (see <see cref="MapRulesExtractor"/>).</summary>
/// <param name="GameMode">The map's game mode, game state and location tag.</param>
/// <param name="Doors">The regular doors placed in the cells.</param>
/// <param name="Rounds">The map's round configurations.</param>
/// <param name="LightingScenarios">The scenarios of the map's lighting lookup.</param>
/// <param name="RoundLightingGroups">The round lighting groups.</param>
/// <param name="Extraction">The elevator extraction points.</param>
/// <param name="ZiplineDefaults">What every rideable zipline shares.</param>
/// <param name="PlayerStartCapsule">Capsule of generated group spawn points.</param>
internal sealed record MapRulesData(
    MapGameMode GameMode,
    ImmutableArray<DoorPlacement> Doors,
    ImmutableArray<RoundConfiguration> Rounds,
    ImmutableArray<LightingScenario> LightingScenarios,
    ImmutableArray<RoundLightingGroup> RoundLightingGroups,
    ExtractionData Extraction,
    ZiplineLineDefaults ZiplineDefaults,
    PlayerStartCapsule PlayerStartCapsule);

/// <summary>
/// Extracts a map's gameplay tables, which also need the compiled scripts: the game mode, the regular doors, the round
/// configurations, the lighting, the extraction points, the zipline defaults and the group spawn point capsule.
/// </summary>
internal static class MapRulesExtractor
{
    /// <summary>The gameplay tables of map package <paramref name="mapPackage"/>.</summary>
    /// <param name="packages">The game's packages.</param>
    /// <param name="scripts">The game's compiled scripts.</param>
    /// <param name="mapPackage">The map package.</param>
    /// <param name="world">The map's persistent world objects.</param>
    /// <param name="cellPackages">The map's generated cell packages.</param>
    /// <param name="rounds">The map's round configuration packages, in table order.</param>
    /// <exception cref="InvalidDataException">An object or a script default the tables need does not decode.</exception>
    public static MapRulesData Extract(
        PackageCache packages,
        ScriptCache scripts,
        string mapPackage,
        MapWorldObjects world,
        IReadOnlyList<string> cellPackages,
        IReadOnlyList<string> rounds)
    {
        MapRuleSources sources = new(packages, scripts);
        SceneResolver scenes = new ComponentReader(packages).Scenes;
        (MapGameMode gameMode, ExportRef lighting, ExportRef roundLighting) = MapGameModeReader.Read(sources, mapPackage, world);
        DoorReader doors = new(sources);
        foreach (string cell in cellPackages)
        {
            doors.ReadCell(cell, sources.Load(cell));
        }

        return new MapRulesData(
            gameMode,
            doors.Build(),
            [.. rounds.Select(round => RoundReader.Read(sources, round))],
            LightingReader.Scenarios(sources, lighting),
            LightingReader.Groups(sources, roundLighting),
            ExtractionReader.Read(sources, scenes, mapPackage, world),
            ZiplineDefaultsReader.Read(sources),
            ZiplineDefaultsReader.Capsule(sources));
    }
}
