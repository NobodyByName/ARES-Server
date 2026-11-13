using System.Collections.Immutable;
using Ares.Common.Maps;
using Ares.MapData.Cells;
using Ares.MapData.Packages;
using Ares.Structs.Game.World;

namespace Ares.MapData.Tests;

public sealed class MapCellReaderTests
{
    private const string LevelPath = "/Game/Maps/Test";
    private const string LayerA = "DataLayer_A";
    private const string LayerB = "DataLayer_B";

    private static readonly PackageObjectIndex s_runtimeCell = PackageObjectIndex.FromScriptPath(RuntimeCellMembers.ClassPath);
    private static readonly PackageObjectIndex s_layer = PackageObjectIndex.FromScriptPath("/Script/Engine.DataLayerInstance");

    private static readonly IReadOnlyList<DataLayer> s_layers =
    [
        new(LayerA, "/Game/Layers/A.A", DataLayerRuntimeState.Unloaded),
        new(LayerB, "/Game/Layers/B.B", DataLayerRuntimeState.Unloaded),
    ];

    [Fact]
    public void Runtime_cells_give_their_data_layer_names()
    {
        IReadOnlyDictionary<string, ImmutableArray<string>> cells = MapCellReader.ReadRuntimeCells(Level());

        Assert.Equal(3, cells.Count);
        Assert.Equal([LayerA, LayerB], cells["LAYERED"]);
        Assert.Empty(cells["BASE"]);
        Assert.Empty(cells["OTHER"]);
    }

    [Fact]
    public void Cells_follow_the_package_list_and_the_level_is_base_map()
    {
        IReadOnlyList<MapCell> cells = MapCellReader.ReadCells(Level(), [LevelPath, Cell("LAYERED"), Cell("BASE")], s_layers);

        Assert.Equal([LevelPath, Cell("LAYERED"), Cell("BASE")], cells.Select(cell => cell.PackageName));
        Assert.True(cells[0].IsBaseMap);
        Assert.Equal([LayerA, LayerB], cells[1].DataLayers);
        Assert.True(cells[2].IsBaseMap);
    }

    [Fact]
    public void Packages_without_a_runtime_cell_and_unknown_layers_are_rejected()
    {
        Assert.Throws<InvalidDataException>(() => MapCellReader.ReadCells(Level(), [Cell("MISSING")], s_layers));
        Assert.Throws<InvalidDataException>(() => MapCellReader.ReadCells(Level(), ["/Game/Elsewhere/LAYERED"], s_layers));
        Assert.Throws<InvalidDataException>(() => MapCellReader.ReadCells(Level(), [Cell("LAYERED")], [.. s_layers.Take(1)]));
    }

    private static string Cell(string name) => $"{LevelPath}/{RuntimeCellMembers.GeneratedFolder}/{name}";

    private static CookedPackage Level()
    {
        // Name map: the package path, then the export names, so export i's name has index i + 1.
        byte[] layered =
        [
            .. PropertyBlocks.Header(RuntimeCellMembers.Unknown0, RuntimeCellMembers.Unknown2, RuntimeCellMembers.DataLayers, 4),
            .. BitConverter.GetBytes(7),
            1,
            .. PropertyBlocks.Header(DataLayerNamesMembers.Unknown0, DataLayerNamesMembers.Names),
            1,
            .. BitConverter.GetBytes(2),
            .. PropertyBlocks.Name(1),
            .. PropertyBlocks.Name(2),
            .. BitConverter.GetBytes(99),
        ];
        byte[] other = [.. PropertyBlocks.Header(1), .. BitConverter.GetBytes(0L)];
        return CookedPackage.Parse(SyntheticArchives.Package(LevelPath,
        [
            (LayerA, PackageObjectIndex.Null, s_layer, 0, []),
            (LayerB, PackageObjectIndex.Null, s_layer, 0, []),
            ("LAYERED", PackageObjectIndex.Null, s_runtimeCell, 0, layered),
            ("BASE", PackageObjectIndex.Null, s_runtimeCell, 0, PropertyBlocks.Header()),
            ("OTHER", PackageObjectIndex.Null, s_runtimeCell, 0, other),
        ]));
    }
}
