using Ares.Raid.Replication;
using Ares.Raid.World;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Core.Serialization;
using Ares.Structs.Game.Characters;
using Ares.Structs.Game.Replication;
using Ares.Structs.Game.World;
using Ares.Structs.Net.GameplayTags;
using Ares.Structs.Net.Replication;
using Ares.Structs.Net.Session;

namespace Ares.Raid.Tests.World;

public sealed class DepartureTests
{
    private const int ControllerChannel = 2;

    // The server's wait for the client's readiness before it sends the client back to the main menu.
    private const double FallbackSeconds = 20.0;

    private const string EndOfRoundPrefix = "EOR.";

    [Fact]
    public void A_player_inside_a_leaving_elevator_gets_the_end_of_round_in_order()
    {
        (ScriptedPlayer session, double leaves) = Departing();
        session.Sent.Clear();
        session.Service(leaves + 0.05);

        // The extraction and the export naming the controller's end-of-round component come first.
        (string name, Dictionary<string, object?> parameters) = Assert.Single(Calls(session));
        Assert.Equal(FunctionNames.ClientNotifyExtracted, name);
        Assert.Equal(new GameplayTag(DamExtraction.ExtractionType), parameters[PropertyNames.ExtractionType]);
        ObjectExports named = Assert.Single(session.Sent.Where(bunch => bunch.PackageExports)
            .Select(bunch => ObjectExports.Read(new BitReader(bunch.Payload, bunch.PayloadBits)).Exports), exports => exports.Records.ContainsKey(session.Player.EndOfRound));
        StableSubobjectExport component = Assert.IsType<StableSubobjectExport>(named.Records[session.Player.EndOfRound]);
        Assert.Equal((session.Player.Controller, PlayerClasses.EndOfRoundComponent, GameReplication.Catalog.Class(ClassPaths.EndOfRoundComponent).Checksum),
            (component.OuterGuid, component.Path, component.Checksum));
        Assert.True(session.Member.Departed);
        Assert.Matches(@"^0@\d+\.\d{3}$", session.Channels.Departure);

        // Once the client has it: quest progress, no experience data, no reconnect, the transition, an empty journey.
        session.Sent.Clear();
        session.Service(leaves + 0.1);
        List<(string Name, Dictionary<string, object?> Parameters)> calls = Calls(session);
        Assert.Equal(
        [
            EndOfRoundPrefix + FunctionNames.ClientFinalizeQuestProgress,
            FunctionNames.ReceiveXpData,
            FunctionNames.ClientClearReconnectData,
            FunctionNames.ClientStartEndOfRoundTransition,
            FunctionNames.ReceiveEndOfRoundJourneySummary,
            FunctionNames.EndOfRoundJourneyDataTransferComplete,
        ], calls.Select(call => call.Name));
        Assert.Equal(true, calls[0].Parameters[PropertyNames.PlayerWon]);
        Assert.Empty(Assert.IsType<IReadOnlyList<object>>(Members(calls[1].Parameters, PropertyNames.XpEventLog)[PropertyNames.Entries], exactMatch: false));
        Assert.Equal(true, calls[3].Parameters[PropertyNames.PlayerWon]);
        Assert.All(Members(calls[4].Parameters, PropertyNames.Summary).Values, value => Assert.True(value is false or 0));
        Assert.Matches(@"^0@\d+\.\d{3} transition@\d+\.\d{3}$", session.Channels.Departure);
        Assert.False(session.Member.Left);
        Assert.False(session.Raid.Ended);

        // Nothing more until the client is ready; its input no longer moves the character.
        PlayerMotion motion = session.Member.Motion!;
        session.Input(60, 63, position: session.Member.Spawn.Location);
        session.Sent.Clear();
        session.Service(leaves + 5);
        Assert.Empty(Calls(session));
        Assert.Same(motion, session.Member.Motion);
    }

    [Fact]
    public void The_clients_readiness_brings_the_end_of_round_and_its_disconnect_leaves_the_raid()
    {
        (ScriptedPlayer session, double leaves) = Departing();
        session.Service(leaves + 0.05);
        session.Service(leaves + 0.1);

        session.ComponentCall(ControllerChannel, session.Player.Controller, PlayerClasses.SpectatorManagerComponent, ReadyToSpectate(session));
        session.Sent.Clear();
        session.Service(leaves + 9);
        (string name, Dictionary<string, object?> parameters) = Assert.Single(Calls(session));
        Assert.Equal(FunctionNames.ClientStartEndOfRound, name);
        Assert.Equal(true, parameters[PropertyNames.PlayerWon]);
        Assert.Matches(@"^0@\d+\.\d{3} transition@\d+\.\d{3} ready@\d+\.\d{3} end@\d+\.\d{3}$", session.Channels.Departure);
        Assert.DoesNotContain(session.Channels.IgnoredCalls.Keys, key => key.Contains("Spectat", StringComparison.Ordinal));

        // No fallback after the end of round; the client's own disconnect takes the player out of the raid.
        session.Sent.Clear();
        session.Service(leaves + 0.1 + FallbackSeconds + 1);
        Assert.Empty(Calls(session));
        session.Channels.Closed();
        Assert.True(session.Member.Left);
        Assert.True(session.Raid.Ended);
    }

    [Fact]
    public void A_client_that_never_says_it_is_ready_is_sent_back_to_the_main_menu()
    {
        (ScriptedPlayer session, double leaves) = Departing();
        session.Service(leaves + 0.05);
        double transition = leaves + 0.1;
        session.Service(transition);

        session.Sent.Clear();
        session.Service(transition + FallbackSeconds - 0.5);
        Assert.Empty(Calls(session));

        session.Service(transition + FallbackSeconds + 0.05);
        (string name, _) = Assert.Single(Calls(session));
        Assert.Equal(FunctionNames.ClientReturnToMainMenuWithTextReason, name);
        Assert.Matches(@"^0@\d+\.\d{3} transition@\d+\.\d{3} fallback@\d+\.\d{3}$", session.Channels.Departure);

        // A late readiness changes nothing.
        session.ComponentCall(ControllerChannel, session.Player.Controller, PlayerClasses.SpectatorManagerComponent, ReadyToSpectate(session));
        session.Sent.Clear();
        session.Service(transition + FallbackSeconds + 1);
        Assert.Empty(Calls(session));
        session.Channels.Closed();
        Assert.True(session.Raid.Ended);
    }

    [Fact]
    public void A_player_outside_the_zone_stays_and_no_longer_sees_the_departed_character()
    {
        ScriptedPlayer first = new();
        ScriptedPlayer second = new(first, 43);
        first.Possess();
        second.Possess();
        (_, double at) = first.Settle(30, 101, Inside(first.Raid));
        (_, at) = second.Settle(30, at);

        // The second player sees the first one once the map actors may be sent.
        int stateChannel = 64 + first.Raid.Doors.Doors.Count + first.Raid.Ladders.Ladders.Count + first.Raid.Loot.Containers.Count
            + first.Raid.Extraction.Points.Count;
        at += 20;
        for (int step = 0; step < 5; step++)
        {
            second.Service(at += 0.1);
        }

        Assert.Contains(second.Sent, bunch => bunch is { Opened: true } && bunch.Channel == stateChannel);
        Assert.Contains(second.Sent, bunch => bunch is { Opened: true } && bunch.Channel == stateChannel + 1);

        // A readiness report outside a departure is only counted.
        second.ComponentCall(ControllerChannel, second.Player.Controller, PlayerClasses.SpectatorManagerComponent, ReadyToSpectate(second));
        Assert.Equal(1, second.Channels.IgnoredCalls[$"spectator:{FunctionNames.ServerReadyToSpectate}"]);

        double leaves = LeavesAt(first, at);
        first.Service(leaves + 0.05);
        Assert.Equal([first.Principal.UserId], first.Raid.ExtractionStates.Departure(0)!.Players);
        second.Sent.Clear();
        second.Service(leaves + 0.1);
        Assert.Equal([(stateChannel + 1, Bunches.Destroyed), (stateChannel, Bunches.Destroyed)],
            second.Sent.Where(bunch => bunch.CloseReason is not null).Select(bunch => (bunch.Channel, bunch.CloseReason!.Value)));
        Assert.DoesNotContain(Calls(second), call => call.Name == FunctionNames.ClientStartEndOfRoundTransition);
        Assert.Equal(string.Empty, second.Channels.Departure);
        Assert.False(second.Member.Departed);

        // The raid goes on with the player who stayed.
        first.Service(leaves + 0.1);
        first.Channels.Closed();
        Assert.True(first.Member.Left);
        Assert.False(first.Raid.Ended);
    }

    // A settled player standing inside elevator 0's zone, and when that elevator leaves on its own after a call
    // (its extraction time over, then its doors closed); serviced up to just before.
    private static (ScriptedPlayer Session, double Leaves) Departing()
    {
        ScriptedPlayer session = new();
        session.Possess();
        (_, double at) = session.Settle(30, 101, Inside(session.Raid));
        Assert.Equal(ExtractionState.Closed, session.Raid.ExtractionStates.Get(0).State);
        double leaves = LeavesAt(session, at);
        session.Service(leaves - 0.1);
        Assert.Equal(ExtractionState.Closing, session.Raid.ExtractionStates.Get(0).State);
        Assert.Equal(string.Empty, session.Channels.Departure);
        Assert.False(session.Member.Departed);
        return (session, leaves);
    }

    private static double LeavesAt(ScriptedPlayer session, double at)
    {
        Assert.True(session.Raid.ExtractionStates.Call(0, session.Principal.UserId, session.Raid.WorldSeconds(at)));
        return at + ExtractionStates.CallSeconds + DamExtraction.Settings.TimeToExtractReadySeconds + DamExtraction.Settings.DefaultExtractionSeconds
            + DamExtraction.DoorsSeconds;
    }

    private static Vector3D Inside(RaidWorld raid)
    {
        ExtractionPointPlacement placement = DamExtraction.All[raid.Extraction.Points[0].Index];
        Vector3D offset = placement.Rotation.Rotate(DamExtraction.ZoneLocation);
        return new Vector3D(placement.Location.X + offset.X, placement.Location.Y + offset.Y, placement.Location.Z + offset.Z);
    }

    private static ActorContentBlock ReadyToSpectate(ScriptedPlayer session) =>
        session.Raid.State.Replication.Calls().Call(ClassPaths.PioneerSpectatorManagerComponent, FunctionNames.ServerReadyToSpectate, new Dictionary<string, object?>(), RpcDirection.Server);

    private static Dictionary<string, object?> Members(Dictionary<string, object?> parameters, string name) =>
        Assert.IsType<Dictionary<string, object?>>(parameters[name]);

    // The calls sent on the controller channel, in order: the controller's by name, its end-of-round component's with
    // a prefix; other content of the channel is skipped.
    private static List<(string Name, Dictionary<string, object?> Parameters)> Calls(ScriptedPlayer session)
    {
        RpcCodec codec = session.Raid.State.Replication.Calls(session.Raid.State.Objects([session.Player.Controller]));
        List<(string, Dictionary<string, object?>)> result = [];
        foreach (ChannelBunch bunch in session.Sent.Where(bunch => bunch is { Channel: ControllerChannel, PackageExports: false }))
        {
            BitReader peek = new(bunch.Payload, bunch.PayloadBits);
            peek.ReadBit();
            BitReader reader = new(bunch.Payload, bunch.PayloadBits);
            string classPath = PlayerClasses.PlayerController.Class;
            string prefix = string.Empty;
            ActorContentBlock block;
            if (peek.ReadBit())
            {
                block = ActorContentBlock.Read(reader);
            }
            else
            {
                StableSubobjectContent content = StableSubobjectContent.Read(reader);
                if (content.Guid != session.Player.EndOfRound)
                {
                    continue;
                }

                (classPath, prefix, block) = (ClassPaths.EndOfRoundComponent, EndOfRoundPrefix, content.Content);
            }

            foreach ((NetField field, ActorFieldPayload payload) in codec.ReadFields(classPath, block, RpcDirection.Client))
            {
                // Text parameters are only sent empty; they are not decoded.
                bool text = field.Function!.Parameters.Any(path => GameReplication.Catalog.Property(path).Kind == PropertyKind.TextProperty);
                result.Add((prefix + field.Name, text ? [] : codec.ReadParameters(field, payload)));
            }
        }

        return result;
    }
}
