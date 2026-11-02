using System.Net;
using Ares.Common.Control;
using Ares.Raid.Transport;
using Ares.Raid.World;
using Ares.Structs.Core;
using Ares.Structs.Core.Serialization;
using Ares.Structs.Game.Characters;
using Ares.Structs.Game.Network;
using Ares.Structs.Game.Replication;
using Ares.Structs.Net.Control;
using Ares.Structs.Net.Replication;
using Ares.Structs.Net.Session;
using ScenarioNames = Ares.Structs.Backend.Matchmaking.ScenarioNames;

namespace Ares.Raid.Tests;

public sealed class RaidIntegrationTests
{
    private const string Ticket = "0b7f6c3e-9d0a-4c1b-8e2f-3a4b5c6d7e8f";
    private static readonly IPEndPoint s_peer = new(IPAddress.Loopback, 40000);

    private readonly double[] _now = [10];

    [Fact]
    public void A_reserved_player_logs_in_joins_the_dam_and_possesses_its_character()
    {
        DamWorlds worlds = new(() => _now[0], ScriptedLighting, () => 7);
        RaidAllocations allocations = new(new GameServerEndpoint("ares-raid", "127.0.0.1", 7777, "", "", false, "LOCAL"), worlds, () => _now[0]);
        Allocation allocation = allocations.Reserve(new AllocationRequest(Ticket, ScenarioNames.TheDam, [42])).Allocation!;
        HandshakeServer handshake = new(GameBuild.NetworkChecksum, 0, clock: () => _now[0]);
        RaidConnections connections = new(handshake, new RoutingAuthenticator(allocations), worlds, packet => UdpPadding.Remove(packet), () => _now[0]);
        Client client = new(connections, Convert.FromBase64String(allocation.Gameserver.RoutingToken));

        client.Handshake();
        Assert.IsType<ControlChallenge>(Assert.Single(client.SendControl(new ControlHello(GameBuild.NetworkChecksum, "42", 0))));
        ControlMessage welcome = Assert.Single(client.SendControl(new ControlLogin("0", "/Game/Map?EncryptionToken=42?HandshakeId=7", new UniqueNetId(5, "42", "", false, false), "WIN")));
        Assert.Equal(RaidWorld.Welcome, welcome);
        client.SendControl(new ControlNetspeed(50000), new ControlJoin());

        PlayerChannels player = Assert.IsType<PlayerChannels>(Assert.Single(connections.Players).Player);
        for (int round = 0; round < 64 && Current(connections).Phase != DeliveryPhase.PossessionPending; round++)
        {
            client.Acknowledge();
        }

        Assert.Equal(DeliveryPhase.PossessionPending, Current(connections).Phase);
        Assert.Equal([2, 3, 6], client.Received.Where(bunch => bunch is { Opened: true, Channel: < 8 and not 4 and not 5 and not 7 }).Select(bunch => bunch.Channel).Order());

        RaidWorld raid = worlds.Current!;
        WorldPlayer actors = raid.State.Player(new SessionPrincipal(42, allocation.MatchId));
        RpcCodec calls = raid.State.Replication.Calls(raid.State.Objects([actors.Controller, actors.PlayerState, actors.Pawn, raid.State.GameState.Guid]));
        ConnectionResult result = client.SendActor(2, calls.Call(PlayerClasses.PlayerController.Class, FunctionNames.ServerAcknowledgePossession, new Dictionary<string, object?> { [PropertyNames.P] = actors.Pawn }, RpcDirection.Server));
        Assert.Contains(result.Event, (ConnectionEvent[])[ConnectionEvent.ReplySent, ConnectionEvent.Acknowledged]);
        Assert.Equal(DeliveryPhase.Possessed, Current(connections).Phase);
        Assert.True(raid.State.Player(new SessionPrincipal(42, allocation.MatchId)).PossessionAcknowledged);
        Assert.NotSame(player, Current(connections));
    }

    [Fact]
    public void Datagrams_without_the_reservation_credential_are_rejected()
    {
        DamWorlds worlds = new(() => _now[0]);
        RaidAllocations allocations = new(new GameServerEndpoint("ares-raid", "127.0.0.1", 7777, "", "", false, "LOCAL"), worlds, () => _now[0]);
        allocations.Reserve(new AllocationRequest(Ticket, ScenarioNames.TheDam, [42]));
        HandshakeServer handshake = new(GameBuild.NetworkChecksum, 0, clock: () => _now[0]);
        RaidConnections connections = new(handshake, new RoutingAuthenticator(allocations), worlds, packet => UdpPadding.Remove(packet), () => _now[0]);
        Client client = new(connections, new byte[RoutingAuthenticator.TokenBytes]);
        Assert.Equal(ConnectionEvent.RoutingRejected, client.SendRaw(Client.Initial()).Event);
    }

    private const string ScriptedLighting = "Lighting.GreyMorning";

    private static PlayerChannels Current(RaidConnections connections) => Assert.IsType<PlayerChannels>(Assert.Single(connections.Players).Player);

    // A client sending padded datagrams with its routing credential; it acknowledges every server packet and
    // only advances a reliable sequence when the server accepted the bunch.
    private sealed class Client(RaidConnections connections, byte[] token)
    {
        private const int ClientId = 3;
        private const int Mtu = RaidConnections.MaxPacketBytes;

        private readonly Dictionary<int, int> _reliable = [];
        private readonly HashSet<int> _opened = [];
        private int _sequence;
        private int _lastServer;
        private int _seed;

        public List<ChannelBunch> Received { get; } = [];

        public static byte[] Initial() =>
            new HandshakePacket(new StatelessPrefix(0, ClientId, false, 4, 4, (byte)HandshakeType.Initial, 0, GameBuild.NetworkChecksum), 0, 0, 0, new byte[20], new byte[8]).Encode();

        public ConnectionResult SendRaw(byte[] packet) => connections.Handle([.. UdpPadding.Add(packet), .. token, 0], s_peer);

        public void Handshake()
        {
            HandshakePacket challenge = HandshakePacket.Decode(SendRaw(Initial()).Reply!);
            Assert.NotNull(SendRaw((challenge with { Prefix = challenge.Prefix with { PacketType = (byte)HandshakeType.Response } }).Encode()).Reply);
            SequenceSeeds seeds = SequenceSeeds.FromCookie(challenge.Cookie);
            _sequence = seeds.ClientPacket;
            _lastServer = (seeds.ServerPacket - 1) & PacketSequence.Mask;
            _seed = seeds.ClientPacket & (ChannelBunch.SequenceCount - 1);
        }

        public List<ControlMessage> SendControl(params ControlMessage[] messages)
        {
            BitWriter writer = new();
            foreach (ControlMessage message in messages)
            {
                message.Write(writer);
            }

            ConnectionResult result = SendReliable(new ChannelBunch(0, writer.ToArray(), writer.Length) { Name = ChannelName.Control });
            Assert.True(result.Event is ConnectionEvent.ReplySent or ConnectionEvent.Acknowledged, $"{result.Event}: {result.Detail}");
            return Read(result.Reply);
        }

        public ConnectionResult SendActor(int channel, ActorContentBlock block)
        {
            BitWriter writer = new();
            block.Write(writer);
            return SendReliable(new ChannelBunch(channel, writer.ToArray(), writer.Length) { Name = ChannelName.Actor });
        }

        public void Acknowledge() => Read(Send([]).Reply);

        private ConnectionResult SendReliable(ChannelBunch bunch)
        {
            int sequence = (_reliable.GetValueOrDefault(bunch.Channel, _seed) + 1) & (ChannelBunch.SequenceCount - 1);
            ChannelBunch numbered = bunch with { Sequence = sequence, Opened = bunch.Channel == 0 && _opened.Add(0) };
            ConnectionResult result = Send([numbered]);
            if (result.Event is ConnectionEvent.ReplySent or ConnectionEvent.Acknowledged)
            {
                _reliable[bunch.Channel] = sequence;
            }
            else
            {
                _opened.Remove(0);
            }

            return result;
        }

        private ConnectionResult Send(IReadOnlyList<ChannelBunch> bunches)
        {
            BitWriter writer = new();
            new ConnectionPacket(new NotificationHeader(_sequence, _lastServer, [uint.MaxValue]), bunches).Write(writer, fromServer: false, Mtu);
            _sequence = (_sequence + 1) & PacketSequence.Mask;
            return SendRaw(new ConnectionEnvelope(0, ClientId, writer.ToArray(), writer.Length).Encode(fromServer: false));
        }

        private List<ControlMessage> Read(byte[]? reply)
        {
            if (reply is null)
            {
                return [];
            }

            ConnectionEnvelope envelope = ConnectionEnvelope.Decode(reply, fromServer: true);
            ConnectionPacket packet = ConnectionPacket.Read(new BitReader(envelope.Payload, envelope.PayloadBits), fromServer: true, Mtu);
            _lastServer = packet.Notification.Sequence;
            Received.AddRange(packet.Bunches);
            List<ControlMessage> messages = [];
            foreach (ChannelBunch bunch in packet.Bunches.Where(bunch => bunch is { Channel: 0, PackageExports: false }))
            {
                BitReader reader = new(bunch.Payload, bunch.PayloadBits);
                while (reader.Remaining > 0)
                {
                    messages.Add(ControlMessage.Read(reader));
                }
            }

            return messages;
        }
    }
}
