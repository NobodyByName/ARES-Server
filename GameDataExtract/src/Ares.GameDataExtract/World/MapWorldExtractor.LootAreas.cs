using System.Collections.Immutable;
using Ares.Common.Maps;
using Ares.GameDataExtract.Cells;
using Ares.GameDataExtract.Components;
using Ares.GameDataExtract.Packages;
using Ares.GameDataExtract.Scripts;
using Ares.Structs.Game.World;

namespace Ares.GameDataExtract.World;

/// <summary>The loot area tables of one map (see <see cref="MapWorldExtractor.ExtractLootAreas"/>).</summary>
/// <param name="PointsOfInterest">The named areas of the persistent level, with their loot value.</param>
/// <param name="LootLimiters">The loot placement limiters of the persistent level and the cells.</param>
/// <param name="DynamicLootSpawners">The spawners of free-standing loot containers.</param>
/// <param name="DynamicLootGroups">The activity groups that pick which spawners run.</param>
internal sealed record MapLootAreaData(
    ImmutableArray<PointOfInterest> PointsOfInterest,
    ImmutableArray<LootLimiter> LootLimiters,
    ImmutableArray<DynamicLootSpawner> DynamicLootSpawners,
    ImmutableArray<DynamicLootGroupEntry> DynamicLootGroups);

internal static partial class MapWorldExtractor
{
    /// <summary>
    /// Extracts the loot area tables of map package <paramref name="mapPackage"/>: points of interest, loot placement
    /// limiters and dynamic loot spawners with their activity groups. Member chains of the script classes come from
    /// <paramref name="layouts"/>.
    /// </summary>
    /// <param name="packages">The game's packages.</param>
    /// <param name="layouts">The script layouts.</param>
    /// <param name="mapPackage">The map package.</param>
    /// <param name="cellPackages">The map's generated cell packages.</param>
    /// <param name="dataLayers">The map's data layers; every layer a cell names must be one of them.</param>
    /// <exception cref="InvalidDataException">The map, a cell, an actor or a script class the tables need does not decode.</exception>
    public static MapLootAreaData ExtractLootAreas(PackageCache packages, ScriptLayouts layouts, string mapPackage, IReadOnlyList<string> cellPackages,
        IReadOnlyList<DataLayer> dataLayers)
    {
        CookedPackage level = packages.Load(mapPackage) ?? throw new InvalidDataException($"The map package {mapPackage} is not in the game archives");
        IReadOnlyList<MapCell> cells = MapCellReader.ReadCells(level, [mapPackage, .. cellPackages], dataLayers);
        SceneResolver scenes = new ComponentReader(packages).Scenes;
        LootLimiterReader limiters = new(scenes, layouts, mapPackage);
        DynamicLootSpawnerReader spawners = new(scenes, layouts, mapPackage);
        List<CookedPackage> cellPackagesRead = [];
        foreach (MapCell cell in cells)
        {
            CookedPackage package = packages.Load(cell.PackageName) ?? throw new InvalidDataException($"{cell.PackageName} is not in the game archives");
            limiters.ReadCell(cell, package);
            spawners.ReadCell(cell, package);
            if (!ReferenceEquals(package, level))
            {
                cellPackagesRead.Add(package);
            }
        }

        ImmutableArray<DynamicLootSpawner> spawnerRows = spawners.Build();
        return new MapLootAreaData(
            new PointOfInterestReader(scenes, layouts).Read(level),
            limiters.Build(level),
            spawnerRows,
            new ActivityTreeReader(layouts).Read(level, cellPackagesRead, spawnerRows));
    }
}
