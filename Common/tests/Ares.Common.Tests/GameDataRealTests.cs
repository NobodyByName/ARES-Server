using System.Reflection;
using Ares.Common.GameData;
using Ares.Common.Testing;
using Ares.Structs.Backend.Progression;
using Ares.Structs.Game;
using Ares.Structs.Game.Inventory;
using Ares.Structs.Game.Movement;
using Ares.Structs.Game.World;

namespace Ares.Common.Tests;

/// <summary>
/// Real-data tier: the lookups of the loaded <see cref="GameDataSet"/> over the extracted game data, and the protocol
/// constants of Structs that name game data agree with it. Skipped without <see cref="RealGameData.DirectoryVariable"/>.
/// </summary>
public sealed class GameDataRealTests
{
    private static GameDataSet Data => RealGameData.Data;

    private static ResolvedGameNames Names => Data.Names;

    [RealDataFact]
    public void Item_lookups_cover_every_quality_level_and_the_world_item_list()
    {
        ItemCatalog items = Data.Items;

        Assert.Equal(items.All.Sum(item => Math.Max(item.QualityAssetIds.Count, 1)), items.ByAssetId.Count);
        Assert.All(items.All, item => Assert.Same(item, items.ByTechnicalName[item.TechnicalName]));
        Assert.Equal([.. items.All.Select(item => item.UsableIndex).Order()], items.All.Select(item => item.UsableIndex));
        Assert.Equal(Data.WorldItems.Classes.Length - 1, Data.WorldItems.IndexOf(Data.WorldItems.Classes[^1]));
        Assert.Null(Data.WorldItems.IndexOf("/Game/Not/A.Class_C"));
    }

    [RealDataFact]
    public void Item_names_resolve_to_their_classes()
    {
        Assert.Equal(Data.Items.FirearmsByClass[Names.FerroI.ItemClass], Names.FerroIFirearm);
        Assert.Equal(Data.Items.ShieldsByClass[Names.LightShield.ItemClass], Names.LightShieldClass);
        Assert.Equal(Data.Items.AugmentsByTechnicalName[Names.CombatMk1.TechnicalName].FrameAssetId, Data.Menu.Augments.FrameAssetIds[Names.CombatMk1.AssetId]);
        Assert.NotEmpty(Names.StandardLoadoutFrame.Containers.Select(container => container.ContainerSlotAssetId).Intersect(Data.Menu.Augments.BranchAssetIds.Values));
        Assert.All(ItemTechnicalNames.All, name => Assert.Contains(name, Data.Items.ByTechnicalName.Keys));
        Assert.All(ItemTechnicalNames.DataAssets, pair => Assert.Equal(pair.Value, Data.Items.ByTechnicalName[pair.Key].DataAsset));
        Assert.Equal(InventoryBlackboard.RubberDuckIndex, Data.Items.ByTechnicalName[ItemTechnicalNames.RubberDuck].UsableIndex);
    }

    [RealDataFact]
    public void Every_graph_tag_name_resolves_to_a_distinct_bit()
    {
        GraphTagBit[] bits = [.. typeof(GraphTagBits).GetProperties(BindingFlags.Public | BindingFlags.Instance).Select(property => (GraphTagBit)property.GetValue(Names.GraphTags)!)];

        Assert.NotEmpty(bits);
        Assert.Equal(bits.Length, bits.Select(bit => bit.Index).Distinct().Count());
        Assert.All(bits, bit => Assert.Equal(Data.Movement.GraphStateTags[bit.Index], bit.Tag));
    }

    [RealDataFact]
    public void The_dam_agrees_with_its_protocol_constants()
    {
        MapData dam = Data.Map(GameNames.Maps.Dam);

        Assert.Same(dam, Names.Dam);
        Assert.Same(dam, Data.FindMap(GameNames.Maps.Dam));
        Assert.All(dam.DynamicLootGroups.SelectMany(group => group.Spawners), spawner => Assert.Contains(spawner, dam.DynamicLootSpawners));
        Assert.Equal(dam.Rounds.Select(round => round.Package), RoundConfigurationAssets.Dam);
        Assert.Equal(dam.ExtractionButtons.Select(button => (button.Component, button.Role)), ExtractionClasses.Buttons);
        Assert.All(dam.LightingScenarios, scenario => Assert.Single(DamDataLayers.All, layer => layer.Asset.Split('.')[0] == scenario.DataLayerPackage));
        Assert.All(dam.RoundLightingGroups.Values.SelectMany(group => group), choice => Assert.Contains(dam.LightingScenarios, scenario => scenario.Tag == choice.Tag));
        Assert.All(dam.Doors, door => Assert.Contains(door.Cell, dam.StreamingCells));
        Assert.Contains(dam.PlayerStarts, start => start.Tags.Contains(DamProtocol.GameModeTag));
    }

    [RealDataFact]
    public void The_dam_exploit_prevention_areas_hold_the_locked_rooms_triggers()
    {
        MapData dam = Data.Map(GameNames.Maps.Dam);
        HashSet<(string, string)> areas = [.. dam.ExploitPreventionAreas.Select(area => (area.Cell, area.Actor))];

        Assert.Equal(10, dam.ExploitPreventionAreas.Length);
        Assert.All(dam.ExploitPreventionAreas, area => Assert.NotEmpty(area.Triggers));
        Assert.All(dam.ExploitPreventionAreas, area => Assert.Contains(area.Cell, dam.StreamingCells));
        Assert.All(dam.ExploitPreventionAreas.SelectMany(area => area.Triggers), box => Assert.True(box.HalfExtent is { X: > 0, Y: > 0, Z: > 0 }));
        Assert.All(dam.Doors.Where(door => door.ExploitPreventionArea is not null), door => Assert.Contains((door.Cell, door.ExploitPreventionArea!), areas));
    }

    [RealDataFact]
    public void The_dam_spline_paths_have_unique_names_and_world_points()
    {
        MapData dam = Data.Map(GameNames.Maps.Dam);

        Assert.Equal(157, dam.SplinePaths.Length);
        Assert.Equal(118, dam.SplinePaths.Count(path => path.Closed));
        Assert.Equal(dam.SplinePaths.Length, dam.SplinePaths.Select(path => path.Name).Distinct(StringComparer.Ordinal).Count());
        Assert.All(dam.SplinePaths, path => Assert.True(path.Points.Length >= 2, path.Name));
        Assert.All(dam.SplinePaths.SelectMany(path => path.Points), point => Assert.True(point is { X: > 100_000, Y: > 100_000 }, point.ToString()));
    }

    [RealDataFact]
    public void The_dam_loose_item_sockets_are_apart_from_its_container_sockets()
    {
        MapData dam = Data.Map(GameNames.Maps.Dam);
        HashSet<(string, string, string, string)> containers = [.. dam.LootSockets.Select(socket => (socket.Cell, socket.Actor, socket.Component, socket.Socket))];

        Assert.NotEmpty(dam.LooseItemSockets);
        Assert.All(dam.LooseItemSockets, socket => Assert.True(LooseItemTags.IsLooseItemTag(socket.Tag), socket.Tag));
        Assert.DoesNotContain(dam.LooseItemSockets, socket => containers.Contains((socket.Cell, socket.Actor, socket.Component, socket.Socket)));
        Assert.Equal(dam.LooseItemSockets.Length, dam.LooseItemSockets.Select(socket => (socket.Cell, socket.Actor, socket.Component, socket.Socket)).Distinct().Count());
        Assert.Contains(dam.LooseItemSockets, socket => socket.Options.HasFlag(LooseItemOptions.EnablePhysics));
        Assert.Contains(dam.LooseItemSockets, socket => socket.Options == LooseItemOptions.None);
        Assert.All(dam.LooseItemSockets, socket => Assert.Contains(socket.Cell, dam.StreamingCells.Select(cell => cell[(cell.LastIndexOf('/') + 1)..]).Append(string.Empty)));
    }

    [RealDataFact]
    public void Loot_lookups_ignore_case_and_find_every_table()
    {
        LootData loot = Data.Loot;

        Assert.All(loot.ContainerTypes, type => Assert.Equal(type.Tag, loot.FindContainerType(type.Tag.ToUpperInvariant())?.Tag));
        Assert.All(loot.ContainerClasses, type => Assert.Same(type, loot.FindContainerClass(type.Container.Class)));
        Assert.All(loot.StaticValueTables.GroupBy(table => table.Name, StringComparer.OrdinalIgnoreCase),
            tables => Assert.Equal(tables.Count() == 1 ? tables.Single().Path : null, loot.FindStaticValueTable(tables.Key)?.Path));
    }

    [RealDataFact]
    public void A_lid_shorter_than_twice_the_minimum_opens_after_the_minimum_delay()
    {
        const string ServerRackDoor = "/Game/Pioneer/Environment/Props/Government/Server_01/BP_SocketContainer_Gvt_Server_01_x400z250_Door.BP_SocketContainer_Gvt_Server_01_x400z250_Door_C";

        Assert.Equal(0.4, Data.Loot.FindContainerClass(ServerRackDoor)?.OpenDelaySeconds);
        Assert.All(Data.Loot.ContainerClasses, type => Assert.InRange(type.OpenDelaySeconds, 0, 10));
    }
}
