using System.Collections.Immutable;
using Ares.Common.Maps;
using Ares.GameDataExtract.Cells;
using Ares.GameDataExtract.Components;
using Ares.GameDataExtract.Packages;
using Ares.Structs.Game.World;

namespace Ares.GameDataExtract.World;

/// <summary>The world tables of one map (see <see cref="MapWorldExtractor"/>).</summary>
/// <param name="World">The map's persistent world objects.</param>
/// <param name="StreamingCells">The map's generated cell packages, in ordinal order.</param>
/// <param name="LootSocketMeshes">The meshes that carry loot container sockets, by path.</param>
/// <param name="LootSockets">The loot container sockets.</param>
/// <param name="LooseItemSockets">The loose item sockets.</param>
/// <param name="PlacedLoot">The loot container actors the map places.</param>
/// <param name="PlayerStarts">The authored player starts of the persistent level.</param>
/// <param name="Ladders">The climbable ladders placed in the cells.</param>
/// <param name="LadderLogic">The class the server spawns for each ladder, with its class default; none without ladders.</param>
/// <param name="ZiplineLines">The rideable ziplines placed in the cells.</param>
/// <param name="ZiplineLaunchers">The zipline launchers placed in the cells.</param>
internal sealed record MapWorldData(
    MapWorldObjects World,
    ImmutableArray<string> StreamingCells,
    ImmutableArray<LootSocketMesh> LootSocketMeshes,
    ImmutableArray<LootSocket> LootSockets,
    ImmutableArray<LooseItemSocket> LooseItemSockets,
    ImmutableArray<PlacedLootContainer> PlacedLoot,
    ImmutableArray<PlayerStart> PlayerStarts,
    ImmutableArray<LadderPlacement> Ladders,
    ArchetypeClass? LadderLogic,
    ImmutableArray<ZiplinePlacement> ZiplineLines,
    ImmutableArray<ActivatableZiplinePlacement> ZiplineLaunchers);

/// <summary>
/// Extracts a World Partition map's world tables from its map package and generated cell packages: the persistent world
/// objects, the cell list, the loot container and loose item sockets, the placed loot containers, the player starts, the ladders and the
/// ziplines.
/// </summary>
internal static partial class MapWorldExtractor
{
    /// <summary>Extracts the world tables of map package <paramref name="mapPackage"/>.</summary>
    /// <param name="packages">The game's packages.</param>
    /// <param name="mapPackage">The map package.</param>
    /// <param name="cellPackages">The map's generated cell packages.</param>
    /// <param name="dataLayers">The map's data layers; every layer a cell names must be one of them.</param>
    /// <exception cref="InvalidDataException">The map, a cell or a component the tables need does not decode.</exception>
    public static MapWorldData Extract(PackageCache packages, string mapPackage, IReadOnlyList<string> cellPackages, IReadOnlyList<DataLayer> dataLayers)
    {
        CookedPackage level = packages.Load(mapPackage) ?? throw new InvalidDataException($"The map package {mapPackage} is not in the game archives");
        MapWorldObjects world = MapWorldReader.Read(level);
        IReadOnlyList<MapCell> cells = MapCellReader.ReadCells(level, [mapPackage, .. cellPackages], dataLayers);
        ComponentReader components = new(packages);
        LootSocketReader sockets = new(components, packages, mapPackage);
        PlacedLootReader placed = new(components.Scenes, mapPackage);
        LadderReader ladders = new(components.Scenes, packages, mapPackage);
        ZiplineReader ziplines = new(mapPackage);
        foreach (MapCell cell in cells)
        {
            CookedPackage package = packages.Load(cell.PackageName) ?? throw new InvalidDataException($"{cell.PackageName} is not in the game archives");
            sockets.ReadCell(cell, package);
            placed.ReadCell(cell, package);
            ladders.ReadCell(cell, package);
            ziplines.ReadCell(cell, package);
        }

        (ImmutableArray<LootSocketMesh> meshes, ImmutableArray<LootSocket> socketRows, ImmutableArray<LooseItemSocket> looseRows) = sockets.Build();
        (ImmutableArray<LadderPlacement> ladderRows, ArchetypeClass? ladderLogic) = ladders.Build();
        (ImmutableArray<ZiplinePlacement> lines, ImmutableArray<ActivatableZiplinePlacement> launchers) = ziplines.Build();
        return new MapWorldData(
            world,
            [.. cellPackages.Order(StringComparer.Ordinal)],
            meshes,
            socketRows,
            looseRows,
            placed.Build(),
            PlayerStartReader.Read(level, components.Scenes),
            ladderRows,
            ladderLogic,
            lines,
            launchers);
    }
}
