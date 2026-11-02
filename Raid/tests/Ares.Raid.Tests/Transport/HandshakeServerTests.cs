using System.Net;
using Ares.Raid.Transport;
using Ares.Structs.Net.Session;

namespace Ares.Raid.Tests.Transport;

public sealed class HandshakeServerTests
{
    private const uint Version = 0x12345678;
    private const ushort Features = 1;

    private static readonly IPEndPoint s_peer = new(IPAddress.Loopback, 23456);

    private readonly double[] _now = [100];

    private HandshakeServer Server(int sessionId = 2, int maxConnections = 2) =>
        new(Version, Features, sessionId, () => _now[0], maxConnections: maxConnections);

    private static HandshakePacket Initial() =>
        new(new StatelessPrefix(0, 5, false, 3, 4, (byte)HandshakeType.Initial, 7, Version), Features, 0, 0, new byte[20], new byte[8]);

    private static HandshakePacket ResponseTo(byte[] challenge)
    {
        HandshakePacket packet = HandshakePacket.Decode(challenge);
        return packet with { Prefix = packet.Prefix with { PacketType = (byte)HandshakeType.Response, SentCount = 8 } };
    }

    [Fact]
    public void A_valid_exchange_creates_one_connection()
    {
        HandshakeServer server = Server();
        byte[] initial = Initial().Encode();
        (byte[]? reply, HandshakeEvent result) = server.Handle(initial, s_peer);
        Assert.Equal(HandshakeEvent.ChallengeSent, result);
        Assert.Empty(server.Connections);
        Assert.Equal(initial.Length, reply!.Length);
        HandshakePacket challenge = HandshakePacket.Decode(reply);
        Assert.Equal((2, 5, (byte)HandshakeType.Challenge, (byte)4, (byte)7, 100.0), (challenge.Prefix.SessionId, challenge.Prefix.ClientId, challenge.Prefix.PacketType, challenge.Prefix.MinimumVersion, challenge.Prefix.SentCount, challenge.Timestamp));
        Assert.Contains(challenge.Cookie, b => b != 0);

        HandshakePacket response = ResponseTo(reply);
        (byte[]? ack, HandshakeEvent verified) = server.Handle(response.Encode(), s_peer);
        Assert.Equal(HandshakeEvent.CookieVerified, verified);
        HandshakePacket ackPacket = HandshakePacket.Decode(ack!);
        Assert.Equal(((byte)HandshakeType.Ack, -1.0, 1), (ackPacket.Prefix.PacketType, ackPacket.Timestamp, ackPacket.SecretId));
        Assert.Equal(challenge.Cookie, ackPacket.Cookie);
        TransportConnection connection = Assert.Single(server.Connections).Value;
        Assert.Equal(SequenceSeeds.FromCookie(challenge.Cookie), connection.Seeds);
        Assert.Equal(HandshakeEvent.AckRetried, server.Handle(response.Encode(), s_peer).Event);
    }

    [Fact]
    public void Forged_or_misdirected_responses_are_rejected()
    {
        HandshakeServer server = Server();
        HandshakePacket response = ResponseTo(server.Handle(Initial().Encode(), s_peer).Reply!);
        HandshakePacket[] forged =
        [
            response with { Cookie = new byte[20] },
            response with { Timestamp = 100.1 },
            response with { SecretId = 1 },
            response with { Prefix = response.Prefix with { SessionId = 1 } },
            response with { Prefix = response.Prefix with { NetworkVersion = Version + 1 } },
            response with { NetworkFeatures = 0 },
        ];
        Assert.All(forged, packet => Assert.Null(server.Handle(packet.Encode(), s_peer).Reply));
        Assert.Null(server.Handle(response.Encode(), new IPEndPoint(IPAddress.Parse("127.0.0.2"), 23456)).Reply);
        Assert.Null(server.Handle(response.Encode(), new IPEndPoint(IPAddress.Loopback, 23457)).Reply);
        Assert.Null(server.Handle(response.Encode(), new IPEndPoint(IPAddress.IPv6Loopback, 23456)).Reply);
        Assert.Empty(server.Connections);
    }

    [Fact]
    public void Malformed_datagrams_are_ignored()
    {
        HandshakeServer server = Server();
        byte[] raw = Initial().Encode();
        byte[][] corrupt = [[.. raw[..^1], 0], [.. raw[..^1], 0x80], [.. raw, 0], raw[..20]];
        Assert.All(corrupt, datagram => Assert.Equal(HandshakeEvent.Invalid, server.Handle(datagram, s_peer).Event));
        HandshakePacket withCookie = Initial() with { Cookie = [.. Enumerable.Repeat((byte)1, 20)] };
        Assert.Equal(HandshakeEvent.InvalidInitial, server.Handle(withCookie.Encode(), s_peer).Event);
        Assert.Empty(server.Connections);
    }

    [Fact]
    public void Cookies_survive_one_key_rotation_then_expire()
    {
        HandshakeServer server = Server();
        HandshakePacket response = ResponseTo(server.Handle(Initial().Encode(), s_peer).Reply!);
        Assert.Equal(HandshakeEvent.CookieVerified, server.Handle(response.Encode(), s_peer).Event);
        _now[0] = 135;
        Assert.Equal(HandshakeEvent.AckRetried, server.Handle(response.Encode(), s_peer).Event);
        _now[0] = 159;
        Assert.Equal(HandshakeEvent.AckRetried, server.Handle(response.Encode(), s_peer).Event);
        _now[0] = 161;
        Assert.Equal(HandshakeEvent.Expired, server.Handle(response.Encode(), s_peer).Event);
        _now[0] = 300;
        server.Handle([0], s_peer);
        Assert.Empty(server.Connections);
    }

    [Fact]
    public void A_cookie_from_a_replaced_key_is_rejected()
    {
        HandshakeServer server = Server();
        HandshakePacket old = ResponseTo(server.Handle(Initial().Encode(), s_peer).Reply!);
        _now[0] = 131;
        server.Handle([0], s_peer);
        _now[0] = 145;
        HandshakePacket fresh = ResponseTo(server.Handle(Initial().Encode(), s_peer).Reply!);
        Assert.NotEqual(old.SecretId, fresh.SecretId);
        _now[0] = 159;
        Assert.Equal(HandshakeEvent.CookieVerified, server.Handle(old.Encode(), s_peer).Event);
        _now[0] = 161;
        server.Handle([0], s_peer);
        _now[0] = 162;
        Assert.Equal(HandshakeEvent.Expired, server.Handle(old.Encode(), s_peer).Event);
    }

    [Fact]
    public void Connections_are_capped()
    {
        HandshakeServer server = Server(sessionId: 0);
        for (int port = 25000; port < 25003; port++)
        {
            IPEndPoint peer = new(IPAddress.Loopback, port);
            HandshakePacket response = ResponseTo(server.Handle(Initial().Encode(), peer).Reply!);
            (byte[]? reply, HandshakeEvent result) = server.Handle(response.Encode(), peer);
            Assert.Equal(port < 25002 ? HandshakeEvent.CookieVerified : HandshakeEvent.CapacityReached, result);
            Assert.Equal(port < 25002, reply is not null);
        }

        Assert.Equal(2, server.Connections.Count);
    }
}
