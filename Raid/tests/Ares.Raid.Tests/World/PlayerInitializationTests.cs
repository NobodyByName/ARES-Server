using Ares.Raid.World;
using Ares.Structs.Core.Serialization;
using Ares.Structs.Game.Characters;
using Ares.Structs.Game.Replication;
using Ares.Structs.Game.World;
using Ares.Structs.Net.Replication;
using Ares.Structs.Net.Session;

namespace Ares.Raid.Tests.World;

public sealed class PlayerInitializationTests
{
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
        ActorContentBlock restart = Assert.Single(controller.Select(Block), block => !block.HasRepLayout);
        (string function, Dictionary<string, object?> parameters) = calls.ReadCall(PlayerClasses.PlayerController.Class, restart, RpcDirection.Client);
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

    private static bool IsActorProperties(ChannelBunch bunch)
    {
        BitReader reader = new(bunch.Payload, bunch.PayloadBits);
        return reader.ReadBit() && reader.ReadBit();
    }

    private static ActorContentBlock Block(ChannelBunch bunch) => ActorContentBlock.Read(new BitReader(bunch.Payload, bunch.PayloadBits));

    private static Dictionary<string, object?> Properties(ScriptedPlayer session, string classPath, ActorContentBlock block)
    {
        Dictionary<ulong, string> objects = session.Raid.State.Objects([session.Player.Controller, session.Player.PlayerState, session.Player.Pawn, session.Player.Squad]);
        PropertyCodec codec = new(new ValueCodec(GameReplication.Catalog, [KnownEnums.NetRole], objects));
        return codec.Decode(new PropertyLayout(GameReplication.Catalog, classPath), block);
    }
}
