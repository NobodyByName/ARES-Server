using System.Security.Cryptography;
using System.Text;
using Ares.Raid.Replication;
using Ares.Raid.Transport;
using Ares.Raid.World;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Core.Serialization;
using Ares.Structs.Game.Replication;
using Ares.Structs.Game.World;
using Ares.Structs.Net.Session;

namespace Ares.Raid.Tests.World;

public sealed class RaidSessionTests
{
    // SHA-256 of the expected bunch trace of the scripted session below.
    private const string ReferenceTrace = "fff15efb738e9ebcbff3bba93a5de9794d92be374393f1b5163ac0b2315ded26";

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
    public void Doors_close_when_their_cell_is_hidden_and_failed_doors_are_not_offered_again()
    {
        ScriptedPlayer session = new();
        session.Possess();
        foreach (double at in (double[])[100.0, 106.0, 121.0, 121.5, 122.0])
        {
            session.Service(at);
        }

        int[] open = [.. session.Sent.Where(bunch => bunch is { Channel: >= 64, Opened: true }).Select(bunch => bunch.Channel)];
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
        Assert.Equal(CurrentInteractionState.Disabled, session.Raid.DoorStates.Buttons(barricaded.Index));
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
        session.Input(30, 33, focus: button);
        session.Service(122.5);
        Assert.Equal((button, 1), (session.Channels.InputFocus, session.Channels.BlackboardFrames));

        session.Input(33, 36, focus: button, position: there);
        session.Service(123.0);
        session.Input(36, 39, focus: button, position: there);
        session.Service(123.5);
        Assert.Equal(2, session.Channels.BlackboardFrames);

        session.Input(39, 42, position: there);
        session.Service(124.0);
        Assert.Equal((0UL, 3), (session.Channels.InputFocus, session.Channels.BlackboardFrames));
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

        session.Input(30, 33, focus: session.HiddenDoor.Buttons[0], position: new Vector3D(1.0, 2.0, 3.0));
        session.Service(122.5);
        Assert.Equal(1, session.Channels.BlackboardFrames);
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
