using Ares.Raid.World;
using Ares.Structs.Core.Serialization;
using Ares.Structs.Game.Characters;
using Ares.Structs.Game.Replication;
using Ares.Structs.Net.Replication;
using Ares.Structs.Net.Session;

namespace Ares.Raid.Tests.World;

public sealed class PlayerInitializationTests
{
    [Fact]
    public void The_controller_opens_at_the_planned_spawn()
    {
        ScriptedPlayer session = new();
        session.Flush("start");
        Assert.Equal(DeliveryPhase.PossessionPending, session.Channels.Phase);
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
    }

    [Fact]
    public void The_pawn_opens_with_the_authored_spawn_facing()
    {
        ScriptedPlayer session = new();
        session.Flush("start");
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

    private static Dictionary<string, object?> Properties(ScriptedPlayer session, string classPath, ActorContentBlock block)
    {
        Dictionary<ulong, string> objects = session.Raid.State.Objects([session.Player.Controller, session.Player.PlayerState, session.Player.Pawn, session.Player.Squad]);
        PropertyCodec codec = new(new ValueCodec(GameReplication.Catalog, [KnownEnums.NetRole], objects));
        return codec.Decode(new PropertyLayout(GameReplication.Catalog, classPath), block);
    }
}
