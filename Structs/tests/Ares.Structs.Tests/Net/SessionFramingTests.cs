using Ares.Structs.Core.Serialization;
using Ares.Structs.Net.Session;

namespace Ares.Structs.Tests.Net;

public sealed class SessionFramingTests
{
    [Theory]
    [MemberData(nameof(SessionFramingVectors.Handshakes), MemberType = typeof(SessionFramingVectors))]
    public void Handshakes_match_the_reference(
        int session,
        int client,
        byte minimum,
        byte type,
        byte sent,
        uint version,
        ushort features,
        int secret,
        double timestamp,
        string cookie,
        string padding,
        string packet)
    {
        HandshakePacket handshake = new(
            new StatelessPrefix(session, client, false, minimum, 4, type, sent, version),
            features,
            secret,
            timestamp,
            Convert.FromHexString(cookie),
            Convert.FromHexString(padding));
        Assert.Equal(packet, Convert.ToHexStringLower(handshake.Encode()));

        HandshakePacket decoded = HandshakePacket.Decode(Convert.FromHexString(packet));
        Assert.Equal(handshake.Prefix, decoded.Prefix);
        Assert.Equal((features, secret, timestamp), (decoded.NetworkFeatures, decoded.SecretId, decoded.Timestamp));
        Assert.Equal(cookie, Convert.ToHexStringLower(decoded.Cookie));
        Assert.Equal(padding, Convert.ToHexStringLower(decoded.Padding));
    }

    [Theory]
    [MemberData(nameof(SessionFramingVectors.BadHandshakes), MemberType = typeof(SessionFramingVectors))]
    public void Malformed_handshakes_are_rejected(string packet, string reason)
    {
        Assert.Throws<InvalidPacketException>(() => HandshakePacket.Decode(Convert.FromHexString(packet)));
        Assert.False(string.IsNullOrEmpty(reason));
    }

    [Theory]
    [MemberData(nameof(SessionFramingVectors.Notifications), MemberType = typeof(SessionFramingVectors))]
    public void Notification_headers_match_the_reference(int sequence, int acknowledged, uint[] history, string encoded)
    {
        BitWriter writer = new();
        new NotificationHeader(sequence, acknowledged, history).Write(writer);
        Assert.Equal(encoded, Convert.ToHexStringLower(writer.ToArray()));

        NotificationHeader decoded = NotificationHeader.Read(new BitReader(Convert.FromHexString(encoded)));
        Assert.Equal((sequence, acknowledged), (decoded.Sequence, decoded.Acknowledged));
        Assert.Equal(history, decoded.History);
    }

    [Theory]
    [MemberData(nameof(SessionFramingVectors.SequenceDeltas), MemberType = typeof(SessionFramingVectors))]
    public void Sequence_deltas_match_the_reference(int newer, int older, int delta) =>
        Assert.Equal(delta, PacketSequence.Delta(newer, older));

    [Theory]
    [MemberData(nameof(SessionFramingVectors.Seeds), MemberType = typeof(SessionFramingVectors))]
    public void Sequence_seeds_come_from_the_cookie(string cookie, int server, int client) =>
        Assert.Equal(new SequenceSeeds(server, client), SequenceSeeds.FromCookie(Convert.FromHexString(cookie)));

    [Theory]
    [MemberData(nameof(SessionFramingVectors.Envelopes), MemberType = typeof(SessionFramingVectors))]
    public void Envelopes_match_the_reference(
        bool fromServer,
        int session,
        int client,
        string payload,
        int payloadBits,
        uint? serverTime,
        int? directKind,
        string direct,
        string packet)
    {
        ConnectionEnvelope envelope = new(session, client, Convert.FromHexString(payload), payloadBits)
        {
            ServerTimeMs = serverTime,
            DirectKind = directKind,
            DirectPayload = Convert.FromHexString(direct),
        };
        Assert.Equal(packet, Convert.ToHexStringLower(envelope.Encode(fromServer)));

        ConnectionEnvelope decoded = ConnectionEnvelope.Decode(Convert.FromHexString(packet), fromServer);
        Assert.Equal((session, client, payloadBits, serverTime, directKind),
            (decoded.SessionId, decoded.ClientId, (int)decoded.PayloadBits, decoded.ServerTimeMs, decoded.DirectKind));
        Assert.Equal(payload, Convert.ToHexStringLower(decoded.Payload));
        Assert.Equal(direct, Convert.ToHexStringLower(decoded.DirectPayload));
    }

    [Theory]
    [MemberData(nameof(SessionFramingVectors.BadEnvelopes), MemberType = typeof(SessionFramingVectors))]
    public void Malformed_envelopes_are_rejected(string packet, bool fromServer, string reason)
    {
        Assert.Throws<InvalidPacketException>(() => ConnectionEnvelope.Decode(Convert.FromHexString(packet), fromServer));
        Assert.False(string.IsNullOrEmpty(reason));
    }

    [Fact]
    public void Invalid_values_are_rejected_when_encoding()
    {
        StatelessPrefix prefix = new(0, 0, false, 4, 4, 0, 0, 0);
        Assert.Throws<InvalidPacketException>(() => new HandshakePacket(prefix, 0, 0, 0, new byte[19], new byte[8]).Encode());
        Assert.Throws<InvalidPacketException>(() => new HandshakePacket(prefix, 0, 0, 0, new byte[20], new byte[17]).Encode());
        Assert.Throws<InvalidPacketException>(() => new HandshakePacket(prefix, 0, 0, double.NaN, new byte[20], new byte[8]).Encode());
        Assert.Throws<InvalidPacketException>(() => new HandshakePacket(prefix with { Restart = true }, 0, 0, 0, new byte[20], new byte[8]).Encode());
        Assert.Throws<InvalidPacketException>(() => new NotificationHeader(0, 0, []).Write(new BitWriter()));
        Assert.Throws<InvalidPacketException>(() => new NotificationHeader(1 << 14, 0, [0]).Write(new BitWriter()));
        Assert.Throws<InvalidPacketException>(() => new ConnectionEnvelope(0, 0, [], 0) { ServerTimeMs = 1 }.Encode(false));
        Assert.Throws<InvalidPacketException>(() => new ConnectionEnvelope(0, 0, [], 0) { DirectPayload = [1] }.Encode(true));
        Assert.Throws<InvalidPacketException>(() => new ConnectionEnvelope(0, 0, [], 0) { DirectKind = 1, DirectPayload = new byte[769] }.Encode(true));
    }
}
