using System.Security.Cryptography;
using System.Text;
using Ares.Raid.Replication;
using Ares.Raid.Transport;
using Ares.Raid.World;
using Ares.Structs.Backend.Progression;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Core.Serialization;
using Ares.Structs.Game.Characters;
using Ares.Structs.Game.Movement;
using Ares.Structs.Game.Replication;
using Ares.Structs.Game.World;
using Ares.Structs.Net.Replication;
using Ares.Structs.Net.Session;

namespace Ares.Raid.Tests.World;

public sealed class RaidSessionTests
{
    // SHA-256 of the expected bunch trace, including inventory and shield-health initialization.
    private const string ReferenceTrace = "a8fc95dbf5cd525e2df2deff9884ff87edba632cce896d48b0b2c84aac6a8efa";

    [Fact]
    public void A_scripted_session_sends_the_reference_bunches()
    {
        ScriptedPlayer session = new();
        session.Possess();
        foreach (double at in (double[])[100.0, 103.0, 106.0, 110.0, 121.0, 121.5, 122.0])
        {
            session.Service(at);
        }

        session.Input(30, 33, session.Door.Buttons[0]);
        foreach (double at in (double[])[122.5, 123.0, 125.0, 125.5])
        {
            session.Service(at);
        }

        Assert.Equal(DeliveryPhase.Possessed, session.Channels.Phase);
        Assert.True(session.Player.PossessionAcknowledged);
        Assert.Equal((1, 0, 0), (session.Channels.DoorPresses, session.Channels.IgnoredDoorRequests, session.Channels.InputDecodeFailures));
        Assert.Equal(DoorOpenState.Open, session.Raid.DoorStates.Get(session.Door.Index).State.Open);
        Assert.Equal(ReferenceTrace, Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join('\n', session.Trace) + "\n"))));
    }

    [Fact]
    public void Calls_on_components_the_server_does_not_simulate_are_counted_not_rejected()
    {
        ScriptedPlayer session = new();
        session.Possess();
        session.ComponentCall(2, session.Player.Controller, "PingManager");
        session.ComponentCall(6, session.Player.Pawn, "InventoryComponent");
        Assert.Equal((1, 1), (session.Channels.IgnoredCalls["path:PingManager"], session.Channels.IgnoredCalls["path:InventoryComponent"]));
        Assert.Throws<InvalidPacketException>(() => session.ComponentCall(2, session.Player.Pawn, "PingManager"));
    }

    [Fact]
    public void Surrender_sends_the_menu_reply_and_ends_the_world_only_when_the_connection_closes()
    {
        ScriptedPlayer session = new(loadout: InventoryTests.StandardLoadout());
        session.Possess();
        RaidWorld raid = session.Raid;
        session.Sent.Clear();

        session.Call(FunctionNames.ServerSurrender, []);
        Assert.False(raid.Ended);
        Assert.False(session.Worlds.HasLeft(raid.MatchId, 42));
        Assert.True(session.Worlds.AcceptingPlayers(raid.MatchId));
        session.Flush("surrender");
        ChannelBunch reply = Assert.Single(session.Sent, bunch => bunch is { Channel: 2, PackageExports: false });
        RpcCodec calls = raid.State.Replication.Calls(raid.State.Objects([session.Player.Controller]));
        (NetField field, ActorFieldPayload payload) = Assert.Single(calls.ReadFields(PlayerClasses.PlayerController.Class,
            ActorContentBlock.Read(new BitReader(reply.Payload, reply.PayloadBits)), RpcDirection.Client));
        Assert.Equal(FunctionNames.ClientReturnToMainMenuWithTextReason, field.Name);
        Assert.Equal(1, payload.PayloadBits);
        Assert.False(new BitReader(payload.Payload, payload.PayloadBits).ReadBit()); // Empty return-reason text.

        session.Channels.Closed();

        Assert.True(raid.Ended);
        Assert.True(session.Worlds.HasLeft(raid.MatchId, 42));
        Assert.False(session.Worlds.HasLeft("another-match", 42));
        Assert.False(session.Worlds.AcceptingPlayers(raid.MatchId));
        Assert.Null(session.Worlds.WelcomeFor(new SessionPrincipal(42, raid.MatchId)));
        session.Channels.Closed();
        Assert.Equal(4, Assert.Single(raid.Players).Loadout.Items.Single(item => item.GameAssetId == StorageItems.RubberDuckAssetId).Amount);
    }

    [Fact]
    public void A_disconnect_without_surrender_does_not_finish_the_reserved_raid()
    {
        ScriptedPlayer session = new();
        session.Possess();
        session.Channels.Closed();
        Assert.False(session.Raid.Ended);
        Assert.True(session.Worlds.AcceptingPlayers(session.Raid.MatchId));
    }

    [Fact]
    public void A_discarded_surrender_packet_cannot_end_the_world()
    {
        ScriptedPlayer session = new();
        session.Possess();
        IPlayerChannels copy = session.Channels.Copy();
        ChannelBunch surrender = session.ControllerCall(FunctionNames.ServerSurrender, []);
        copy.Receive(new ChannelIo(session.Io.Sender.Copy(), session.Io.Receiver.Copy()), surrender);
        copy.Closed();
        session.Channels.Closed();
        Assert.False(session.Raid.Ended);

        session.Channels.Receive(session.Io, surrender);
        session.Channels.Commit();
        session.Channels.Closed();
        Assert.True(session.Raid.Ended);
    }

    [Fact]
    public void Surrender_before_possession_is_rejected_and_does_not_end_the_raid()
    {
        ScriptedPlayer session = new();
        session.Flush("start");
        Assert.Throws<InvalidPacketException>(() => session.Call(FunctionNames.ServerSurrender, []));
        session.Channels.Commit();
        session.Channels.Closed();
        Assert.False(session.Raid.Ended);
    }

    [Fact]
    public void Doors_close_when_their_cell_is_hidden_and_failed_doors_are_not_offered_again()
    {
        ScriptedPlayer session = new();
        session.Possess();
        foreach (double at in (double[])[100.0, 106.0, 121.0, 121.5, 122.0])
        {
            session.Service(at);
        }

        int ladders = 64 + session.Raid.Doors.Doors.Count;
        int[] open = [.. session.Sent.Where(bunch => bunch is { Channel: >= 64, Opened: true } && bunch.Channel < ladders).Select(bunch => bunch.Channel)];
        Assert.NotEmpty(open);
        session.Channels.ActorChannelFailed(open[0]);
        session.ReportVisible((session.Door.Cell, false, false));
        session.Flush("hidden");
        session.Service(123.0);
        Assert.Equal(open.Order(), session.Sent.Where(bunch => bunch.CloseReason == Bunches.LevelUnloaded).Select(bunch => bunch.Channel).Order());

        session.Sent.Clear();
        session.ReportVisible((session.Door.Cell, true, false));
        session.Service(124.0);
        session.Service(124.5);
        Assert.DoesNotContain(session.Sent, bunch => bunch.Channel == open[0]);
        Assert.Contains(session.Sent, bunch => bunch.PackageExports);
    }

    [Fact]
    public void A_barricaded_door_keeps_its_buttons_disabled_and_ignores_presses()
    {
        ScriptedPlayer session = new();
        Door barricaded = session.Raid.Doors.Doors.First(door => DamDoors.All[door.Index].StartAsSealed);
        session.Possess();
        session.ReportVisible((barricaded.Cell, true, false));
        foreach (double at in (double[])[100.0, 106.0, 121.0, 121.5, 122.0, 122.5, 123.0])
        {
            session.Service(at);
        }

        session.Input(30, 33, barricaded.Buttons[0]);
        session.Service(123.5);

        (DoorState state, int revision, _) = session.Raid.DoorStates.Get(barricaded.Index);
        Assert.Contains(session.Sent, bunch => bunch is { Opened: true } && bunch.Channel == 64 + barricaded.Index);
        Assert.Equal((0, 1), (session.Channels.DoorPresses, session.Channels.IgnoredDoorRequests));
        Assert.Equal((DoorOpenState.Closed, DoorSealState.Sealed, 0), (state.Open, state.Seal, revision));
        Assert.Equal(new DoorButtons(CurrentInteractionState.Disabled, CurrentInteractionState.Idle), session.Raid.DoorStates.Buttons(barricaded.Index));
    }

    [Fact]
    public void Holding_the_breach_button_removes_the_barricade_after_the_hold_time()
    {
        ScriptedPlayer session = new();
        Door barricaded = session.Raid.Doors.Doors.First(door => DamDoors.All[door.Index].StartAsSealed);
        session.Possess();
        session.ReportVisible((barricaded.Cell, true, false));
        foreach (double at in (double[])[100.0, 106.0, 121.0, 121.5, 122.0, 122.5, 123.0])
        {
            session.Service(at);
        }

        session.Input(30, 33, barricaded.Breach);
        session.Service(123.5);
        Assert.Equal(1, session.Channels.DoorBreaches);
        Assert.Equal(new DoorButtons(CurrentInteractionState.Disabled, CurrentInteractionState.Ongoing), session.Raid.DoorStates.Buttons(barricaded.Index));
        Assert.Equal(DoorSealState.Sealed, session.Raid.DoorStates.Get(barricaded.Index).State.Seal);

        int sent = session.Sent.Count;
        session.Service(123.5 + DamDoors.BreachAlignmentSeconds + DamDoors.BreachHoldSeconds + 0.1);

        DoorState state = session.Raid.DoorStates.Get(barricaded.Index).State;
        Assert.Equal((DoorOpenState.Closed, DoorSealState.Unsealed), (state.Open, state.Seal));
        Assert.Equal(new DoorButtons(CurrentInteractionState.Idle, CurrentInteractionState.SuccessDisabled), session.Raid.DoorStates.Buttons(barricaded.Index));
        Assert.Contains(session.Sent.Skip(sent), bunch => bunch.Channel == 64 + barricaded.Index);
        Assert.Equal(1, session.Channels.BreachRequests[PendulumInput.PressRequest]);
    }

    [Fact]
    public void Moving_during_a_breach_cancels_it_and_the_door_can_be_breached_again()
    {
        ScriptedPlayer session = new();
        Door barricaded = session.Raid.Doors.Doors.First(door => DamDoors.All[door.Index].StartAsSealed);
        session.Possess();
        session.ReportVisible((barricaded.Cell, true, false));
        foreach (double at in (double[])[100.0, 106.0, 121.0, 121.5, 122.0, 122.5, 123.0])
        {
            session.Service(at);
        }

        session.Input(30, 33, barricaded.Breach);
        session.Service(123.5);
        session.Input(33, 36);
        Assert.True(session.Raid.DoorStates.Breaching(barricaded.Index));

        session.Input(36, 39, forward: 100);
        Assert.Equal(1, session.Channels.CancelledBreaches);
        Assert.False(session.Raid.DoorStates.Breaching(barricaded.Index));
        Assert.Equal(new DoorButtons(CurrentInteractionState.Disabled, CurrentInteractionState.Idle), session.Raid.DoorStates.Buttons(barricaded.Index));

        session.Service(123.5 + DamDoors.BreachAlignmentSeconds + DamDoors.BreachHoldSeconds + 0.1);
        Assert.Equal(DoorSealState.Sealed, session.Raid.DoorStates.Get(barricaded.Index).State.Seal);

        session.Input(39, 42, barricaded.Breach);
        Assert.Equal(2, session.Channels.DoorBreaches);
    }

    [Fact]
    public void A_breach_pressed_with_the_raider_tool_holstered_starts_on_that_press()
    {
        ScriptedPlayer session = new();
        Door barricaded = session.Raid.Doors.Doors.First(door => DamDoors.All[door.Index].StartAsSealed);
        session.Possess();
        session.ReportVisible((barricaded.Cell, true, false));
        foreach (double at in (double[])[100.0, 106.0, 121.0, 121.5, 122.0, 122.5, 123.0])
        {
            session.Service(at);
        }

        session.Input(30, 33, barricaded.Breach, request: PendulumInput.PressRequest | PendulumInput.EquipToolRequestFlag);
        Assert.Equal((1, 0), (session.Channels.DoorBreaches, session.Channels.IgnoredDoorRequests));
        Assert.True(session.Raid.DoorStates.Breaching(barricaded.Index));
    }

    [Fact]
    public void No_pawn_frame_resets_the_clients_breach_until_its_ending_played()
    {
        ScriptedPlayer session = new();
        Door barricaded = session.Raid.Doors.Doors.First(door => DamDoors.All[door.Index].StartAsSealed);
        session.Possess();
        session.ReportVisible((barricaded.Cell, true, false));
        foreach (double at in (double[])[100.0, 106.0, 121.0, 121.5, 122.0, 122.5, 123.0])
        {
            session.Service(at);
        }

        Vector3D reported = new(1.0, 2.0, 3.0);
        session.Input(30, 33, focus: barricaded.Breach, position: reported);
        session.Service(123.5);
        int frames = session.Channels.BlackboardFrames;
        session.Input(33, 36, barricaded.Breach, focus: barricaded.Breach, position: reported);
        session.Service(124.0);

        // The barricade comes off, so the focused breach button is no longer usable: that frame waits.
        const double End = 124.0 + DamDoors.BreachAlignmentSeconds + DamDoors.BreachHoldSeconds;
        session.Service(End + 0.1);
        session.Input(36, 39, focus: barricaded.Breach, position: reported);
        session.Service(End + 1);
        Assert.Equal(frames, session.Channels.BlackboardFrames);

        session.Input(39, 42, focus: barricaded.Breach, position: reported);
        session.Service(End + 3);
        Assert.Equal(frames + 1, session.Channels.BlackboardFrames);
    }

    [Fact]
    public void A_breach_is_started_once_and_a_door_breaches_open_when_its_map_flag_says_so()
    {
        DoorStates states = new([new DoorPlacement("cell", "door") { StartAsSealed = true, BreachOpensDoor = true }]);
        Assert.True(states.StartBreach(0, 10.0));
        Assert.False(states.StartBreach(0, 10.5));
        Assert.False(states.Press(0, 10.5));

        states.Advance(10.0 + DamDoors.BreachAlignmentSeconds + DamDoors.BreachHoldSeconds);
        Assert.Equal((DoorOpenState.Opening, DoorSealState.Unsealed), (states.Get(0).State.Open, states.Get(0).State.Seal));
        Assert.False(states.StartBreach(0, 30.0));
    }

    [Fact]
    public void A_press_on_a_door_this_player_has_not_opened_is_ignored()
    {
        ScriptedPlayer session = new();
        session.Possess();
        session.Input(30, 33, session.HiddenDoor.Buttons[0]);
        session.Input(0, 40, 0x12345);
        Assert.Equal((0, 2), (session.Channels.DoorPresses, session.Channels.IgnoredDoorRequests));
    }

    [Fact]
    public void Frames_return_the_reported_position_without_drift()
    {
        ScriptedPlayer session = new();
        session.Possess();
        session.Sent.Clear();
        foreach (double at in (double[])[100.0, 106.0, 121.0, 121.5, 122.0])
        {
            session.Service(at);
        }

        ulong button = session.Door.Buttons[0];
        Vector3D there = new(516538.10336022545, 527468.742117282, -38998.81315174722);
        session.Input(30, 33, focus: button);
        session.Service(122.5);
        Dictionary<int, PendulumSlot> first = session.PawnFrame(session.SpawnState);

        session.Input(33, 36, focus: button, position: there);
        session.Sent.Clear();
        session.Service(123.0);
        session.Input(36, 39, focus: button, position: there);
        session.Service(123.5);
        Dictionary<int, PendulumSlot> second = session.PawnFrame(first);

        PendulumSlot movement = second[PawnBlackboard.MovementStateSlot];
        IReadOnlyList<PendulumField> fields = PendulumRegistry.Descriptors[Assert.NotNull(movement.DescriptorId)].Fields;
        Assert.Equal((Expected(there.X), Expected(there.Y)), (Position(PawnBlackboard.PositionXField), Position(PawnBlackboard.PositionYField)));
        Assert.Equal(Expected(there.Z), Position(PawnBlackboard.PositionZField));
        return;

        ulong Position(int field) => PendulumCodec.Integer((PendulumScalarField)fields[field], movement.Value);
        ulong Expected(double world) => (ulong)Math.Round((world + PawnBlackboard.PositionRange) * ((1 << 24) - 1) / (2 * PawnBlackboard.PositionRange));
    }

    [Fact]
    public void Focusing_a_usable_button_sends_a_frame_at_the_reported_position()
    {
        ScriptedPlayer session = new();
        session.Possess();
        foreach (double at in (double[])[100.0, 106.0, 121.0, 121.5, 122.0])
        {
            session.Service(at);
        }

        ulong button = session.Door.Buttons[0];
        Vector3D there = new(450000.0, 600000.0, -38000.0);
        (uint start, double startAt) = session.Settle(30, 122.5);
        session.Input(start, start + 3, focus: button);
        session.Service(startAt);
        Assert.Equal((button, ScriptedPlayer.SettledFrames), (session.Channels.InputFocus, session.Channels.BlackboardFrames));

        session.Input(start + 3, start + 6, focus: button, position: there);
        session.Service(startAt + 0.1);
        session.Input(start + 6, start + 9, focus: button, position: there);
        session.Service(startAt + 0.2);
        Assert.Equal(ScriptedPlayer.SettledFrames + 1, session.Channels.BlackboardFrames);

        session.Input(start + 9, start + 12, position: there);
        session.Service(startAt + 0.3);
        Assert.Equal((0UL, ScriptedPlayer.SettledFrames + 2), (session.Channels.InputFocus, session.Channels.BlackboardFrames));
    }

    [Fact]
    public void Focusing_a_button_whose_door_this_player_has_not_opened_sends_nothing()
    {
        ScriptedPlayer session = new();
        session.Possess();
        foreach (double at in (double[])[100.0, 106.0, 121.0, 121.5, 122.0])
        {
            session.Service(at);
        }

        (uint start, double startAt) = session.Settle(30, 122.5);
        session.Input(start, start + 3, focus: session.HiddenDoor.Buttons[0], position: new Vector3D(1.0, 2.0, 3.0));
        session.Service(startAt);
        Assert.Equal(ScriptedPlayer.SettledFrames, session.Channels.BlackboardFrames);
    }

    [Fact]
    public void Possession_of_another_pawn_or_a_foreign_level_is_rejected()
    {
        ScriptedPlayer session = new();
        session.Flush("start");
        PlayerChannels before = (PlayerChannels)session.Channels.Copy();
        Assert.Throws<InvalidPacketException>(() => session.Call(FunctionNames.ServerAcknowledgePossession, new Dictionary<string, object?> { [PropertyNames.P] = session.Player.Controller }));
        Assert.Throws<InvalidPacketException>(() => session.ReportVisible(("/Game/Elsewhere/Level", true, true)));
        Assert.Equal(before.Phase, session.Channels.Phase);
        Assert.False(session.Player.PossessionAcknowledged);
    }

    [Fact]
    public void Undecodable_input_is_counted_without_rejecting_the_packet()
    {
        ScriptedPlayer session = new();
        session.Possess();
        Assert.True(session.Channels.ObserveDirect(1, [0x81, 0xFF, 0xFF]));
        Assert.False(session.Channels.ObserveDirect(2, [0x01]));
        Assert.Equal((1, 30U), (session.Channels.InputDecodeFailures, session.Channels.InputFrame));
    }

    [Fact]
    public void Actor_traffic_before_the_player_actors_open_is_rejected()
    {
        ScriptedPlayer session = new();
        ChannelBunch bunch = new(2, [0], 1) { Name = ChannelName.Actor, Sequence = 1 };
        Assert.Throws<InvalidPacketException>(() => session.Channels.Receive(session.Io, bunch));
        Assert.Throws<InvalidPacketException>(() => session.Channels.Receive(session.Io, bunch with { Channel = 40 }));
    }

    [Fact]
    public void One_reservation_owns_the_world_until_it_is_released()
    {
        const double Now = 0;
        DamWorlds worlds = new(() => Now, null, () => 7);
        Assert.Null(worlds.WelcomeFor(new SessionPrincipal(1, "a")));
        worlds.Prepare("a", [1]);
        worlds.Prepare("a", [1]);
        Assert.Throws<WorldPreparationException>(() => worlds.Prepare("b", [2]));
        Assert.Throws<WorldPreparationException>(() => new DamWorlds(() => Now).Prepare("c", [1, 2]));
        Assert.Equal(RaidWorld.Welcome, worlds.WelcomeFor(new SessionPrincipal(1, "a")));
        Assert.Throws<InvalidPacketException>(() => worlds.WelcomeFor(new SessionPrincipal(2, "a")));
        Assert.Contains(worlds.Current!.Lighting, DamWorlds.Lightings);
        Assert.True(worlds.AcceptingPlayers("a"));
        worlds.Release("b");
        Assert.True(worlds.AcceptingPlayers("a"));
        worlds.Release("a");
        Assert.False(worlds.AcceptingPlayers("a"));
        Assert.Throws<ArgumentException>(() => new DamWorlds(() => Now, "Lighting.Unknown"));
    }

    [Fact]
    public void The_round_turns_on_its_map_layers()
    {
        DamWorlds worlds = new(() => 0, null, () => 7);
        worlds.Prepare("a", [1]);
        RaidWorld world = worlds.Current!;

        // Keycard doors live in the locked-doors layer; without it their doorways are empty.
        string[] packages = [.. DamDataLayers.All.Where(layer => world.ActiveDataLayers.Contains(layer.Name)).Select(layer => layer.Asset.Split('.')[0])];
        Assert.Contains("/Game/Pioneer/Core/DataLayers/RoundConfiguration_LockedDoors_Standard", packages);
        Assert.Contains("/Game/Pioneer/Core/DataLayers/RoundConfiguration_ToxicSwamp_Standard", packages);
        Assert.DoesNotContain("/Game/Pioneer/Core/DataLayers/RoundConfiguration_LockedDoors_Open", packages);
    }

    [Fact]
    public void Spawn_points_are_lifted_to_the_capsule_center()
    {
        RoundConfiguration round = DamRounds.All[0];
        IReadOnlyList<PlayerStart> starts = Spawns.Eligible(round);
        Assert.NotEmpty(starts);
        Assert.All(starts, start => Assert.Contains(DamMap.GameModeTag, start.Tags));
        PlayerStart first = starts[0];
        SpawnPlacement placement = Spawns.Place(first, 0);
        Assert.Equal(first.RootLocation.Z + first.SpawnPoints[0].Translation.Z + DamPlayerStarts.Capsule.HalfHeight, placement.Location.Z, 6);
        Assert.Throws<ArgumentOutOfRangeException>(() => Spawns.Place(first, first.SpawnPoints.Count));
    }
}
