using Ares.Raid.Transport;
using Ares.Raid.World;
using Ares.Raid.World.Movement;
using Ares.Structs.Core.Serialization;
using Ares.Structs.Game.Characters;
using Ares.Structs.Game.Movement;
using Ares.Structs.Game.Replication;
using Ares.Structs.Game.World;
using Ares.Structs.Net.Replication;
using Ares.Structs.Net.Session;

namespace Ares.Raid.Tests.World;

public sealed class PlayerInitializationTests
{
    [Fact]
    public void The_hud_is_assigned_once_before_the_game_state_starts_play()
    {
        ScriptedPlayer session = new();
        session.Flush("start");
        ChannelBunch hud = Assert.Single(session.Sent, bunch => IsHudAssignment(session, bunch));
        ChannelBunch gameState = Assert.Single(session.Sent, bunch => bunch is { Opened: true, Channel: RaidWorld.GameStateChannel });
        ChannelBunch playerState = Assert.Single(session.Sent, bunch => bunch is { Opened: true, Channel: 3 });
        Assert.True(session.Sent.IndexOf(hud) < session.Sent.IndexOf(gameState));
        Assert.True(session.Sent.IndexOf(playerState) < session.Sent.IndexOf(gameState));

        session.Call(FunctionNames.ServerNotifyHasLoadedInitialLevels, []);
        session.Flush("loaded");
        Assert.Single(session.Sent, bunch => IsHudAssignment(session, bunch));
    }

    [Fact]
    public void World_opening_waits_for_hud_assignment_acknowledgement_including_retries()
    {
        ScriptedPlayer session = new();
        for (int packet = 300; packet < 500; packet++)
        {
            IReadOnlyList<ReliableKey> keys = session.Io.Sender.Select(100, 8000);
            Assert.NotEmpty(keys);
            IReadOnlyList<ChannelBunch> bunches = session.Io.Sender.Bunches(keys);
            Assert.DoesNotContain(bunches, bunch => bunch is { Opened: true, Channel: RaidWorld.GameStateChannel });
            session.Io.Sender.Sent(packet, keys, 100);
            ChannelBunch? hud = bunches.SingleOrDefault(bunch => IsHudAssignment(session, bunch));
            if (hud is null)
            {
                session.Channels.Acknowledge(session.Io, new NotificationHeader(1, packet, [uint.MaxValue]));
                continue;
            }

            session.Channels.Service(session.Io, 101);
            IReadOnlyList<ReliableKey> retry = session.Io.Sender.Select(101, 8000);
            ChannelBunch repeated = Assert.Single(session.Io.Sender.Bunches(retry));
            Assert.Equal(hud, repeated);
            session.Io.Sender.Sent(packet + 1, retry, 101);
            session.Channels.Service(session.Io, 102);
            Assert.All(session.Io.Sender.Bunches(session.Io.Sender.Select(102, 8000)), bunch => Assert.True(IsHudAssignment(session, bunch)));

            session.Channels.Acknowledge(session.Io, new NotificationHeader(1, packet + 1, [uint.MaxValue]));
            Assert.Contains(session.Io.Sender.Bunches(session.Io.Sender.Select(102, 8000)), bunch => bunch.Opened);
            return;
        }

        Assert.Fail("HUD assignment was not sent");
    }

    [Fact]
    public void The_controller_opens_at_the_planned_spawn_without_linking_the_pawn_in_either_direction()
    {
        ScriptedPlayer session = new();
        session.Flush("start");
        Assert.Equal(DeliveryPhase.InitialLevelsPending, session.Channels.Phase);
        ChannelBunch open = Assert.Single(session.Sent, bunch => bunch is { Channel: 2, Opened: true });
        BitReader reader = new(open.Payload, open.PayloadBits);
        MustMapGuids.Read(reader);
        Assert.Equal(session.Player.Controller, NetGuid.Read(reader));
        Assert.Equal(session.Raid.Imports.Archetypes[PlayerClasses.PlayerController.Class], NetGuid.Read(reader));
        Assert.Equal(0UL, NetGuid.Read(reader));
        Assert.Equal(0UL, reader.ReadBits(4));
        Assert.Equal(new PlayerControllerOpen(0, 7, 0), PlayerControllerOpen.Read(reader));
        Dictionary<string, object?> opening = Properties(session, PlayerClasses.PlayerController.Class, ActorContentBlock.Read(reader));
        Assert.Equal(0, reader.Remaining);
        Assert.Equal(session.Raid.Spawn.Location, opening[PropertyNames.SpawnLocation]);
        Assert.Equal(3, opening.Count);

        foreach (ChannelBunch bunch in session.Sent.Where(bunch => bunch is { Channel: 2, Opened: false }))
        {
            ActorContentBlock block = Block(bunch);
            if (!block.HasRepLayout)
            {
                continue;
            }

            Dictionary<string, object?> values = Properties(session, PlayerClasses.PlayerController.Class, block);
            Assert.DoesNotContain(PropertyNames.Pawn, values.Keys);
            Assert.DoesNotContain(PropertyNames.IsServerReady, values.Keys);
        }

        ChannelBunch state = Assert.Single(session.Sent, bunch => bunch is { Channel: 3, Opened: false });
        Dictionary<string, object?> stateValues = Properties(session, PlayerClasses.PlayerState.Class, Block(state));
        Assert.DoesNotContain(PropertyNames.PioneerCharacter, stateValues.Keys);
        Assert.DoesNotContain(PropertyNames.CurrentPawn, stateValues.Keys);
        Assert.DoesNotContain(session.Sent, bunch => bunch.Channel == 6);
    }

    [Fact]
    public void The_pawn_opens_with_the_authored_spawn_facing()
    {
        ScriptedPlayer session = new();
        session.Flush("start");
        session.Call(FunctionNames.ServerNotifyHasLoadedInitialLevels, []);
        session.Flush("loaded");
        ChannelBunch open = Assert.Single(session.Sent, bunch => bunch is { Channel: 6, Opened: true });
        BitReader reader = new(open.Payload, open.PayloadBits);
        MustMapGuids.Read(reader);
        ActorSpawn spawn = ActorSpawn.Read(reader);
        Assert.Equal(session.Raid.Spawn.Location, spawn.Location);
        Assert.NotEqual(0, session.Raid.Spawn.Yaw);
        double expected = (session.Raid.Spawn.Yaw + 360) % 360;
        Assert.InRange(Math.Abs(expected - spawn.Rotation.Yaw), 0, 360.0 / 65536);
        Assert.Equal(0, spawn.Rotation.Pitch);
        Assert.Equal(0, spawn.Rotation.Roll);
    }

    [Fact]
    public void Loading_confirmation_releases_the_pawn_and_ready_flag_together_once()
    {
        ScriptedPlayer session = new();
        session.Flush("start");
        session.ReportVisible((DamMap.MapPackage, true, false));
        session.Service(200);
        Assert.Equal(DeliveryPhase.InitialLevelsPending, session.Channels.Phase);
        Assert.False(session.Player.PossessionAcknowledged);
        Assert.DoesNotContain(session.Sent, bunch => bunch.Channel == 6);

        session.Sent.Clear();
        session.Call(FunctionNames.ServerNotifyHasLoadedInitialLevels, []);
        session.Flush("loaded");
        Assert.Equal(DeliveryPhase.PossessionPending, session.Channels.Phase);
        ChannelBunch[] controller = [.. session.Sent.Where(bunch => bunch is { Channel: 2, Opened: false })];
        ActorContentBlock ready = Assert.Single(controller.Select(Block), block => block.HasRepLayout);
        Dictionary<string, object?> values = Properties(session, PlayerClasses.PlayerController.Class, ready);
        Assert.Equal(2, values.Count);
        Assert.Equal(session.Player.Pawn, values[PropertyNames.Pawn]);
        Assert.Equal(true, values[PropertyNames.IsServerReady]);

        RpcCodec calls = session.Raid.State.Replication.Calls(session.Raid.State.Objects([session.Player.Pawn]));
        ActorContentBlock[] controllerCalls = [.. controller.Select(Block).Where(block => !block.HasRepLayout)];
        Assert.Equal(2, controllerCalls.Length);
        (string function, Dictionary<string, object?> parameters) = calls.ReadCall(PlayerClasses.PlayerController.Class, controllerCalls[0], RpcDirection.Client);
        Assert.Equal(FunctionNames.ClientSetViewTarget, function);
        Assert.Equal(session.Player.Pawn, parameters[PropertyNames.A]);
        (function, parameters) = calls.ReadCall(PlayerClasses.PlayerController.Class, controllerCalls[1], RpcDirection.Client);
        Assert.Equal(FunctionNames.ClientRetryClientRestart, function);
        Assert.Equal(session.Player.Pawn, parameters[PropertyNames.NewPawn]);
        ChannelBunch state = Assert.Single(session.Sent, bunch => bunch is { Channel: 3, Opened: false });
        Dictionary<string, object?> stateValues = Properties(session, PlayerClasses.PlayerState.Class, Block(state));
        Assert.Equal(session.Player.Pawn, stateValues[PropertyNames.PioneerCharacter]);
        Assert.Equal(session.Player.Pawn, stateValues[PropertyNames.CurrentPawn]);
        ChannelBunch[] pawnProperties = [.. session.Sent.Where(bunch => bunch is { Channel: 6, Opened: false })
            .Where(IsActorProperties)];
        Dictionary<string, object?> initial = Properties(session, PlayerClasses.Pawn.Class, Block(pawnProperties[0]));
        Assert.DoesNotContain(PropertyNames.Controller, initial.Keys);
        Dictionary<string, object?> pawnValues = Properties(session, PlayerClasses.Pawn.Class, Block(pawnProperties[1]));
        Assert.Equal(session.Player.Controller, pawnValues[PropertyNames.Controller]);

        session.Call(FunctionNames.ServerAcknowledgePossession, new Dictionary<string, object?> { [PropertyNames.P] = session.Player.Pawn });
        Assert.Equal(DeliveryPhase.Possessed, session.Channels.Phase);
        session.Sent.Clear();
        session.Call(FunctionNames.ServerNotifyHasLoadedInitialLevels, []);
        session.Flush("loaded again");
        Assert.Empty(session.Sent);
    }

    [Fact]
    public void A_loaded_possessed_player_receives_its_first_simulation_frame_without_a_settling_delay()
    {
        ScriptedPlayer session = new();
        session.Possess();
        Assert.Equal(0, session.Channels.BlackboardFrames);
        session.Sent.Clear();
        session.Service(100);
        Assert.Equal(1, session.Channels.BlackboardFrames);
        uint frame = session.Channels.InputFrame!.Value;
        ActorContentBlock expected = session.Raid.State.BlackboardFrame(0, 1,
            PawnBlackboardFrames.State(frame, new PawnMotion(session.Raid.Spawn.Location, Yaw: session.Raid.Spawn.Yaw * Math.PI / 180), 0,
                session.Raid.Inventory.InitialBlackboard, session.Raid.Weapons.Variables(frame)),
            new Dictionary<int, PendulumSlot>());
        ChannelBunch first = Assert.Single(session.Sent, bunch => bunch.Channel == 6);
        ActorContentBlock actual = Block(first);
        Assert.Equal(expected.PayloadBits, actual.PayloadBits);
        Assert.Equal(expected.Payload, actual.Payload);
    }

    [Fact]
    public void Snapshot_delivery_numbers_are_consecutive_while_simulation_ticks_follow_the_inputs()
    {
        ScriptedPlayer session = new();
        session.Possess();
        session.Sent.Clear();
        session.Service(100);
        session.Input(30, 1000, position: session.Raid.Spawn.Location);
        session.Service(100.1);
        session.Service(100.2);
        session.Service(100.3);
        session.Input(1000, 1003, position: session.Raid.Spawn.Location);
        session.Service(100.4);

        ChannelBunch[] frames = [.. session.Sent.Where(bunch => bunch.Channel == 6 && (bunch.Payload[0] & 3) == 2)];
        Assert.Equal(3, frames.Length);
        uint[] ticks = [30, 999, 1002];
        NetFieldTable fields = session.Raid.State.Replication.Calls().Fields(PlayerClasses.Pawn.Class);
        Dictionary<int, PendulumSlot> previous = [];
        for (int index = 0; index < frames.Length; index++)
        {
            ActorContentBlock block = Block(frames[index]);
            ActorFieldPayload payload = ActorFieldPayload.Read(new BitReader(block.Payload, block.PayloadBits), fields.Count);
            Assert.Equal(PropertyNames.BlackBoardReplicationPtr, fields.FieldAt(payload.Index).Name);
            BitReader reader = new(payload.Payload, payload.PayloadBits);
            Assert.True(reader.ReadBit());
            Assert.True(reader.ReadBit());
            uint baseline = reader.ReadBounded(PendulumFrames.BaselineRange);
            Assert.Equal((uint)index, baseline);
            Assert.Equal(index == 0, reader.ReadBit());
            Assert.Equal((uint)index + 1, PendulumFrames.ReadRelative(reader, baseline));
            Assert.False(reader.ReadBit());
            Assert.True(reader.ReadBit());
            previous = PendulumCodec.Default.ReadFrame(reader, previous);
            Assert.Equal(0, reader.Remaining);
            PendulumSlot graph = previous[PawnBlackboard.GraphStateSlot];
            Assert.Equal(ticks[index], PendulumCodec.Integer(
                PendulumRegistry.Descriptors[Assert.NotNull(graph.DescriptorId)].Fields[PawnBlackboard.TickField], graph.Value));
        }
    }

    private static bool IsActorProperties(ChannelBunch bunch)
    {
        BitReader reader = new(bunch.Payload, bunch.PayloadBits);
        return reader.ReadBit() && reader.ReadBit();
    }

    private static bool IsHudAssignment(ScriptedPlayer session, ChannelBunch bunch)
    {
        if (bunch is not { Channel: 2, Opened: false, PackageExports: false })
        {
            return false;
        }

        ActorContentBlock block = Block(bunch);
        if (block.HasRepLayout)
        {
            return false;
        }

        RpcCodec calls = session.Raid.State.Replication.Calls(session.Raid.State.Objects([]));
        return calls.ReadFields(PlayerClasses.PlayerController.Class, block, RpcDirection.Client).Any(call => call.Field.Name == FunctionNames.ClientSetHud);
    }

    private static ActorContentBlock Block(ChannelBunch bunch) => ActorContentBlock.Read(new BitReader(bunch.Payload, bunch.PayloadBits));

    private static Dictionary<string, object?> Properties(ScriptedPlayer session, string classPath, ActorContentBlock block)
    {
        Dictionary<ulong, string> objects = session.Raid.State.Objects([session.Player.Controller, session.Player.PlayerState, session.Player.Pawn, session.Player.Squad]);
        PropertyCodec codec = new(new ValueCodec(GameReplication.Catalog, [KnownEnums.NetRole], objects));
        return codec.Decode(new PropertyLayout(GameReplication.Catalog, classPath), block);
    }
}
