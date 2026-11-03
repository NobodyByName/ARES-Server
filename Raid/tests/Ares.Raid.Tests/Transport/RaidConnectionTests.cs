using System.Net;
using Ares.Raid.Transport;
using Ares.Structs.Core.Serialization;
using Ares.Structs.Net.Control;
using Ares.Structs.Net.Session;

namespace Ares.Raid.Tests.Transport;

public sealed class RaidConnectionTests
{
    private const uint Version = 0x12345678;
    private static readonly byte[] s_token = [.. Enumerable.Range(1, 16).Select(i => (byte)i)];
    private static readonly SessionPrincipal s_player = new(42, "match");
    private static readonly IPEndPoint s_peer = new(IPAddress.Loopback, 40000);
    private static readonly ControlWelcome s_welcome = new("/Game/Map", "/Game/Mode.Mode_C", "");

    private readonly double[] _now = [10];
    private readonly FakeWorld _world = new();

    private (RaidConnections Connections, Client Client) Start()
    {
        HandshakeServer handshake = new(Version, 0, clock: () => _now[0]);
        RaidConnections connections = new(handshake, new RoutingAuthenticator(new Routes()), _world, packet => packet, () => _now[0]);
        Client client = new(connections);
        client.Handshake();
        return (connections, client);
    }

    [Fact]
    public void A_client_logs_in_joins_and_gets_its_player_channels()
    {
        (RaidConnections connections, Client client) = Start();
        IReadOnlyList<ControlMessage> challenge = client.SendControl(new ControlHello(Version, "42", 0));
        ControlChallenge sent = Assert.IsType<ControlChallenge>(Assert.Single(challenge));
        Assert.Equal(8, sent.Challenge.Length);

        _world.Ready = true;
        IReadOnlyList<ControlMessage> welcome = client.SendControl(new ControlLogin("0", "/Game/Map?EncryptionToken=42?HandshakeId=7", new UniqueNetId(5, "42", "", false, false), "WIN"));
        Assert.Equal(s_welcome, Assert.Single(welcome));

        client.SendControl(new ControlNetspeed(50000), new ControlJoin());
        FakePlayer player = Assert.Single(_world.Players);
        Assert.Equal((s_player, 7U, "42"), (player.Principal, player.Login.HandshakeId, player.Login.UniqueId.Contents));
        Assert.Equal(1, connections.Count);

        client.SendControl(new ControlActorChannelFailure(9));
        Assert.Equal([9], _world.Players[0].Failures);
    }

    [Fact]
    public void Rejected_packets_change_nothing()
    {
        (RaidConnections connections, Client client) = Start();
        client.SendControl(new ControlHello(Version, "42", 0));

        // A Login for another identity is rejected without a trace: the next attempt reuses its reliable sequence.
        ConnectionResult rejected = client.Send(Client.Control(new ControlLogin("0", "?EncryptionToken=43", new UniqueNetId(5, "43", "", false, false), "WIN")));
        Assert.Equal(ConnectionEvent.AuthenticationRejected, rejected.Event);
        _world.Ready = true;
        Assert.IsType<ControlWelcome>(Assert.Single(client.SendControl(new ControlLogin("0", "?EncryptionToken=42", new UniqueNetId(5, "42", "", false, false), "WIN"))));

        Assert.Equal(ConnectionEvent.ReplayedPacket, client.Replay().Event);
        Assert.Equal(1, connections.Count);
    }

    [Fact]
    public void A_conflicting_hello_retransmission_is_detected()
    {
        (_, Client client) = Start();
        client.SendControl(new ControlHello(Version, "42", 0));
        ConnectionResult result = client.Send(Client.Control(new ControlHello(Version + 1, "42", 0)) with { Sequence = client.FirstControlSequence, Opened = true });
        Assert.Equal((ConnectionEvent.ConflictingRetransmission, "Hello"), (result.Event, result.Detail));
    }

    [Fact]
    public void Wrong_credentials_and_early_or_foreign_packets_are_rejected()
    {
        (RaidConnections connections, Client client) = Start();
        Assert.Equal(ConnectionEvent.RoutingRejected, connections.Handle([1, 2, 3, .. new byte[16], 0], s_peer).Event);
        Assert.Equal(ConnectionEvent.IncompatibleHello, client.Send(Client.Control(new ControlHello(Version + 1, "42", 0))).Event);
        Assert.Equal(ConnectionEvent.AuthenticationRejected, client.Send(Client.Control(new ControlHello(Version, "43", 0))).Event);
        Assert.Equal(ConnectionEvent.PacketBeforeHandshake, connections.Handle(Client.Wrap(client.Envelope([])), new IPEndPoint(IPAddress.Loopback, 40001)).Event);
        byte[] sideband = new ConnectionEnvelope(0, Client.ClientId, [], 0) { DirectKind = 1, DirectPayload = [1] }.Encode(fromServer: false);
        Assert.Equal(ConnectionEvent.InvalidPacket, connections.Handle(Client.Wrap(sideband), s_peer).Event);
    }

    [Fact]
    public void A_client_close_ends_the_connection()
    {
        (RaidConnections connections, Client client) = Start();
        client.SendControl(new ControlHello(Version, "42", 0));
        ConnectionResult result = client.Send(Client.Control(new ControlCloseReason("Quit")));
        Assert.Equal((ConnectionEvent.ClientClosed, "Quit"), (result.Event, result.Detail));
        Assert.NotNull(result.Reply);
        Assert.Equal(0, connections.Count);
    }

    [Fact]
    public void Polling_sends_keepalives_and_retries()
    {
        (RaidConnections connections, Client client) = Start();
        client.SendControl(new ControlHello(Version, "42", 0));
        Assert.Empty(connections.Poll());
        _now[0] += 1.5;
        (IPEndPoint peer, ConnectionResult result) = Assert.Single(connections.Poll());
        Assert.Equal((s_peer, ConnectionEvent.KeepaliveSent), (peer, result.Event));
        Assert.IsType<ControlChallenge>(Assert.Single(client.Read(result.Reply!)));
    }

    private sealed class Routes : IRouteResolver
    {
        public SessionPrincipal? Route(ReadOnlySpan<byte> token, bool renew) => token.SequenceEqual(s_token) ? s_player : null;
    }

    private sealed class FakeWorld : IRaidWorld
    {
        public bool Ready { get; set; }

        public List<FakePlayer> Players { get; } = [];

        public ControlWelcome? WelcomeFor(SessionPrincipal principal) => Ready ? s_welcome : null;

        public IPlayerChannels StartPlayer(SessionPrincipal principal, PlayerLogin login, ChannelIo io)
        {
            FakePlayer player = new(principal, login, this);
            return player;
        }
    }

    private sealed class FakePlayer(SessionPrincipal principal, PlayerLogin login, FakeWorld world) : IPlayerChannels
    {
        public SessionPrincipal Principal { get; } = principal;

        public PlayerLogin Login { get; } = login;

        public List<int> Failures { get; private init; } = [];

        public IPlayerChannels Copy() => new FakePlayer(Principal, Login, world) { Failures = [.. Failures] };

        public void Acknowledge(ChannelIo io, NotificationHeader header) => io.Sender.Acknowledge(header);

        public void Receive(ChannelIo io, ChannelBunch bunch) => throw new InvalidPacketException("No actor channels in this test");

        public bool ObserveDirect(int kind, byte[] payload) => false;

        public void Service(ChannelIo io, double now)
        {
        }

        public void ActorChannelFailed(int channel) => Failures.Add(channel);

        public void Commit()
        {
            world.Players.RemoveAll(player => player.Principal == Principal);
            world.Players.Add(this);
        }
    }

    // Plays the client side: routing wrapper, handshake, packet sequence and reliable control bunches.
    // Like a retransmitting client, its reliable sequence only advances when the server accepts a bunch.
    private sealed class Client(RaidConnections connections)
    {
        private int _sequence;
        private int _lastServer;
        private int _controlSequence;
        private bool _opened;
        private ConnectionPacket? _lastPacket;

        public const int ClientId = 3;

        public int FirstControlSequence => (_seed + 1) & (ChannelBunch.SequenceCount - 1);

        private int _seed;

        public void Handshake()
        {
            HandshakePacket initial = new(new StatelessPrefix(0, ClientId, false, 4, 4, (byte)HandshakeType.Initial, 0, Version), 0, 0, 0, new byte[20], new byte[8]);
            HandshakePacket challenge = HandshakePacket.Decode(connections.Handle(Wrap(initial.Encode()), s_peer).Reply!);
            HandshakePacket response = challenge with { Prefix = challenge.Prefix with { PacketType = (byte)HandshakeType.Response } };
            Assert.NotNull(connections.Handle(Wrap(response.Encode()), s_peer).Reply);
            SequenceSeeds seeds = SequenceSeeds.FromCookie(challenge.Cookie);
            _sequence = seeds.ClientPacket;
            _lastServer = (seeds.ServerPacket - 1) & PacketSequence.Mask;
            _seed = seeds.ClientPacket & (ChannelBunch.SequenceCount - 1);
            _controlSequence = _seed;
        }

        public static ChannelBunch Control(params ControlMessage[] messages)
        {
            BitWriter writer = new();
            foreach (ControlMessage message in messages)
            {
                message.Write(writer);
            }

            return new ChannelBunch(0, writer.ToArray(), writer.Length) { Name = ChannelName.Control };
        }

        public List<ControlMessage> SendControl(params ControlMessage[] messages)
        {
            ConnectionResult result = Send(Control(messages));
            Assert.True(result.Event is ConnectionEvent.ReplySent or ConnectionEvent.Acknowledged, $"{result.Event}: {result.Detail}");
            return result.Reply is null ? [] : Read(result.Reply);
        }

        public ConnectionResult Send(ChannelBunch bunch)
        {
            bool numbered = bunch.Sequence is null;
            ChannelBunch sent = numbered ? bunch with { Sequence = (_controlSequence + 1) & (ChannelBunch.SequenceCount - 1), Opened = !_opened } : bunch;
            ConnectionResult result = connections.Handle(Wrap(Envelope([sent])), s_peer);
            if (!numbered || result.Event is not (ConnectionEvent.ReplySent or ConnectionEvent.Acknowledged))
            {
                return result;
            }

            _controlSequence++;
            _opened = true;
            return result;
        }

        public ConnectionResult Replay() => connections.Handle(Wrap(Envelope(_lastPacket!)), s_peer);

        public List<ControlMessage> Read(byte[] reply)
        {
            ConnectionEnvelope envelope = ConnectionEnvelope.Decode(reply, fromServer: true);
            ConnectionPacket packet = ConnectionPacket.Read(new BitReader(envelope.Payload, envelope.PayloadBits), fromServer: true, RaidConnections.MaxPacketBytes);
            _lastServer = packet.Notification.Sequence;
            List<ControlMessage> messages = [];
            foreach (ChannelBunch bunch in packet.Bunches)
            {
                BitReader reader = new(bunch.Payload, bunch.PayloadBits);
                while (reader.Remaining > 0)
                {
                    messages.Add(ControlMessage.Read(reader));
                }
            }

            return messages;
        }

        public byte[] Envelope(IReadOnlyList<ChannelBunch> bunches)
        {
            _lastPacket = new ConnectionPacket(new NotificationHeader(_sequence, _lastServer, [uint.MaxValue]), bunches);
            _sequence = (_sequence + 1) & PacketSequence.Mask;
            return Envelope(_lastPacket);
        }

        public static byte[] Wrap(byte[] packet) => [.. packet, .. s_token, 0];

        private static byte[] Envelope(ConnectionPacket packet)
        {
            BitWriter writer = new();
            packet.Write(writer, fromServer: false, RaidConnections.MaxPacketBytes);
            return new ConnectionEnvelope(0, ClientId, writer.ToArray(), writer.Length).Encode(fromServer: false);
        }
    }
}
