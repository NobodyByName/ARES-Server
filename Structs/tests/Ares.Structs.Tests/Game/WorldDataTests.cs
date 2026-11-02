using Ares.Structs.Core.Geometry;
using Ares.Structs.Game.Characters;
using Ares.Structs.Game.Replication;
using Ares.Structs.Game.World;
using Ares.Structs.Net.Replication;

namespace Ares.Structs.Tests.Game;

public sealed class WorldDataTests
{
    private static ReplicationCatalog Catalog => GameReplication.Catalog;

    [Fact]
    public void Map_classes_have_replication_schemas()
    {
        Assert.True(Catalog.IsA(PlayerClasses.Pawn.Class, ClassPaths.Pawn));
        Assert.True(Catalog.IsA(PlayerClasses.PlayerController.Class, ClassPaths.PlayerController));
        Assert.True(Catalog.IsA(PlayerClasses.PlayerState.Class, ClassPaths.PlayerState));
        Assert.True(Catalog.IsA(DamMap.GameState.Class, ClassPaths.GameStateBase));
        Assert.True(Catalog.IsA(DamMap.LevelScriptClass, ClassPaths.Actor));
        Assert.True(Catalog.IsA(DamMap.WorldDataLayersClass, ClassPaths.Actor));
        Assert.True(Catalog.Classes.ContainsKey(DamMap.GameModeClass));
        Assert.All(DamMap.GlobalActors, actor => Assert.True(Catalog.IsA(actor.Class, ClassPaths.Actor), actor.Name));
    }

    [Fact]
    public void Ladders_climb_whole_cycles_and_end_below_their_top()
    {
        Assert.Equal(43, DamLadders.All.Count);
        Assert.Equal(DamLadders.All.Count, DamLadders.All.Select(ladder => (ladder.Cell, ladder.Actor)).Distinct().Count());
        Assert.True(Catalog.IsA(DamLadders.ActorClass, ClassPaths.Actor));
        Assert.True(Catalog.IsA(DamLadders.InteractionClass, ClassPaths.SceneComponent));

        // 973.67 holds seven 132 cm cycles (924); the climb ends 100 cm below that.
        Assert.Equal(824.0, LadderLogic.LadderHeight(973.6699018106374, DamLadders.CycleHeight, DamLadders.EndInteractionDistance), 9);
        Assert.Equal(956.0, LadderLogic.LadderHeight(1056.0, DamLadders.CycleHeight, DamLadders.EndInteractionDistance));
        Assert.All(DamLadders.All, ladder => Assert.InRange(
            LadderLogic.LadderHeight(ladder.SplineTop, DamLadders.CycleHeight, DamLadders.EndInteractionDistance), 0.0, ladder.SplineTop));
        Assert.Throws<ArgumentOutOfRangeException>(() => LadderLogic.LadderHeight(500.0, 0.0, 100.0));
    }

    [Fact]
    public void Hud_is_a_reference_class_with_its_export_checksum()
    {
        Assert.True(Catalog.IsA(PlayerClasses.Hud, ClassPaths.Hud));
        Assert.Equal(PlayerClasses.HudExportChecksum, Catalog.ReferenceClasses[PlayerClasses.Hud].ExportChecksum);
    }

    [Fact]
    public void Player_starts_are_placed_starts_or_groups_of_spawn_points()
    {
        IReadOnlyList<PlayerStart> starts = DamPlayerStarts.All;

        Assert.Equal(46, starts.Count);
        Assert.Equal(starts.Count, starts.Select(start => start.Actor).Distinct().Count());
        PlayerStart[] groups = [.. starts.Where(start => start.ClassName == PlayerStartClassNames.Group)];
        PlayerStart[] placed = [.. starts.Where(start => start.ClassName == PlayerStartClassNames.Placed)];
        Assert.Equal(23, groups.Length);
        Assert.Equal(23, placed.Length);
        Assert.Equal(115, groups.Sum(group => group.SpawnPoints.Count));
        Assert.All(placed, start => Assert.Empty(start.SpawnPoints));
        Assert.All(starts, start => Assert.True(start.RootRotation is { Pitch: 0, Roll: 0 }, start.Actor));
        Assert.All(groups.SelectMany(group => group.SpawnPoints), point => Assert.Equal(Vector3D.One, point.Scale3D));
    }

    [Fact]
    public void Each_lighting_scenario_has_exactly_one_data_layer()
    {
        Assert.Equal(DamDataLayers.All.Count, DamDataLayers.All.Select(layer => layer.Name).Distinct().Count());
        foreach (LightingScenario scenario in DamLighting.Scenarios)
        {
            Assert.Single(DamDataLayers.All, layer => layer.Asset.Split('.')[0] == scenario.DataLayerPackage);
        }

        string[] grouped = [.. DamLighting.RoundLightingGroups.Values.SelectMany(group => group).Select(choice => choice.Tag)];
        Assert.All(grouped, tag => Assert.Contains(DamLighting.Scenarios, scenario => scenario.Tag == tag));
    }

    [Fact]
    public void Doors_are_in_streaming_cells()
    {
        HashSet<string> cells = [.. DamStreamingCells.Packages];

        Assert.Equal(DamStreamingCells.Packages.Count, cells.Count);
        Assert.All(cells, cell => Assert.StartsWith(DamMap.MapPackage + "/_Generated_/", cell, StringComparison.Ordinal));
        Assert.Equal(120, DamDoors.All.Distinct().Count());
        Assert.All(DamDoors.All, door => Assert.Contains(door.Cell, cells));
    }

    [Fact]
    public void Some_doors_start_barricaded_locked_open_or_without_power()
    {
        Assert.Equal(7, DamDoors.All.Count(door => door.StartAsSealed));
        Assert.Equal(8, DamDoors.All.Count(door => door.StartAsLocked));
        Assert.Equal(6, DamDoors.All.Count(door => door.StartAsOpened));
        Assert.Equal(7, DamDoors.All.Count(door => !door.StartAsPowered));
        Assert.Equal(0, DamDoors.All.Count(door => door is { StartAsSealed: true, StartAsLocked: true }));
    }

    [Fact]
    public void Door_state_packs_like_the_door_script()
    {
        DoorState state = new(DoorOpenState.Closing, DoorSealState.Sealed, DoorLockState.Unlocked, DoorPowerState.Powered, DoorSwingDirection.Reverse);

        Assert.Equal(0b01_010_010_001_011, state.Pack());
        Assert.Equal(state, DoorState.Unpack(state.Pack()));
        Assert.Equal(new DoorState(DoorOpenState.Closed, DoorSealState.Uninitialized, DoorLockState.Uninitialized, DoorPowerState.Uninitialized, DoorSwingDirection.Forward), DoorState.Unpack(0));
    }

    [Fact]
    public void Door_classes_and_interaction_state_match_the_schema()
    {
        Assert.True(Catalog.IsA(DamDoors.ActorClass, ClassPaths.Actor));
        Assert.True(Catalog.Classes.ContainsKey(DamDoors.InteractionClass));
        Assert.Equal(Enum.GetNames<CurrentInteractionState>(), DamDoors.InteractionState.Names);
        Assert.Equal(DamDoors.InteractionState.Names.Count, DamDoors.InteractionState.MaxValue);
        Assert.Equal(EnumPaths.ECurrentInteractionState, DamDoors.InteractionState.Path);
    }

    [Fact]
    public void Round_queries_refer_to_their_tag_dictionary()
    {
        RoundConfiguration spearmint = Assert.Single(DamRounds.All, round => round.Name == "Spearmint");

        Assert.True(spearmint.FilterPlayerStarts);
        Assert.Contains(DamPlayerStarts.All, start => start.Tags.Contains("RoundConfiguration.Spearmint"));
        Assert.NotEmpty(spearmint.ContentQuery.TagDictionary);
    }
}
