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
        Assert.True(Catalog.IsA(MapWorldClasses.WorldDataLayers, ClassPaths.Actor));
        Assert.All(DamProtocol.GlobalActors, actor => Assert.True(Catalog.IsA(actor.Class, ClassPaths.Actor), actor.Name));
    }

    [Fact]
    public void Ladders_climb_whole_cycles_and_end_below_their_top()
    {
        Assert.True(Catalog.IsA(LadderLogic.ActorClass, ClassPaths.Actor));
        Assert.True(Catalog.IsA(LadderLogic.InteractionClass, ClassPaths.SceneComponent));

        // 973.67 holds seven 132 cm cycles (924); the climb ends 100 cm below that.
        Assert.Equal(824.0, LadderLogic.LadderHeight(973.6699018106374, LadderLogic.CycleHeight, LadderLogic.EndInteractionDistance), 9);
        Assert.Equal(956.0, LadderLogic.LadderHeight(1056.0, LadderLogic.CycleHeight, LadderLogic.EndInteractionDistance));
        Assert.Throws<ArgumentOutOfRangeException>(() => LadderLogic.LadderHeight(500.0, 0.0, 100.0));
    }

    [Fact]
    public void Hud_is_a_reference_class_with_its_export_checksum()
    {
        Assert.True(Catalog.IsA(PlayerClasses.Hud, ClassPaths.Hud));
        Assert.Equal(PlayerClasses.HudExportChecksum, Catalog.ReferenceClasses[PlayerClasses.Hud].ExportChecksum);
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
        Assert.True(Catalog.IsA(DoorClasses.ActorClass, ClassPaths.Actor));
        Assert.True(Catalog.Classes.ContainsKey(DoorClasses.InteractionClass));
        Assert.Equal(Enum.GetNames<CurrentInteractionState>(), DoorClasses.InteractionState.Names);
        Assert.Equal(DoorClasses.InteractionState.Names.Count, DoorClasses.InteractionState.MaxValue);
        Assert.Equal(EnumPaths.ECurrentInteractionState, DoorClasses.InteractionState.Path);
    }
}
