using Ares.Structs.Core.Serialization;
using Ares.Structs.Net.Replication;
using Ares.Structs.Net.Session;

namespace Ares.Raid.Tests.World;

public sealed class PlayerInitializationTests
{
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
}
