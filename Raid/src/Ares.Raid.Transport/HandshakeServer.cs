using System.Buffers.Binary;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using Ares.Structs.Core.Serialization;
using Ares.Structs.Net.Session;

namespace Ares.Raid.Transport;

/// <summary>Outcome of a handshake datagram.</summary>
public enum HandshakeEvent
{
    /// <summary>A challenge with a fresh cookie was sent.</summary>
    ChallengeSent,

    /// <summary>The response's cookie was valid; the connection exists now.</summary>
    CookieVerified,

    /// <summary>A repeated response for an existing connection was acknowledged again.</summary>
    AckRetried,

    /// <summary>Not a valid handshake packet, or from an unusable address.</summary>
    Invalid,

    /// <summary>Another network version or feature set.</summary>
    Incompatible,

    /// <summary>An initial packet with non-empty cookie fields.</summary>
    InvalidInitial,

    /// <summary>A response for another session.</summary>
    WrongSession,

    /// <summary>A cookie that is too old, from the future, or from a retired key.</summary>
    Expired,

    /// <summary>A cookie that does not match this server, peer and timestamp.</summary>
    InvalidCookie,

    /// <summary>A different handshake for an address that already has a connection.</summary>
    ConnectionExists,

    /// <summary>No room for another connection.</summary>
    CapacityReached,

    /// <summary>A handshake packet type the server does not accept.</summary>
    UnexpectedType,
}

/// <summary>A connection whose handshake completed.</summary>
/// <param name="ClientId">Client id from the handshake.</param>
/// <param name="Cookie">The verified cookie.</param>
/// <param name="ExpiresAt">When the entry expires without traffic, in clock seconds.</param>
public sealed record TransportConnection(int ClientId, byte[] Cookie, double ExpiresAt)
{
    /// <summary>First packet sequences of both sides.</summary>
    public SequenceSeeds Seeds => SequenceSeeds.FromCookie(Cookie);
}

/// <summary>
/// The stateless handshake: a challenge carrying a cookie that only this server can produce, then a
/// response echoing it. Nothing is stored before the response proves the peer received the challenge.
/// </summary>
/// <remarks>
/// Cookie design (the client treats the cookie as opaque bytes): two key slots, the active one replaced
/// every <see cref="KeyLifetime"/>; each slot remembers when it issued cookies. A cookie is the first 20
/// bytes of HMAC-SHA256 over the timestamp, peer address and port, and is accepted for
/// <see cref="CookieLifetime"/> if its timestamp falls in its key's issuing window.
/// </remarks>
public sealed class HandshakeServer
{
    /// <summary>How long a key issues cookies.</summary>
    private const double KeyLifetime = 30;

    /// <summary>How long a cookie is accepted.</summary>
    private const double CookieLifetime = 60;

    /// <summary>How long a connection entry lives without traffic.</summary>
    private const double ConnectionLifetime = 60;

    private const int KeyBytes = 32;
    private static readonly byte[] s_label = [.. "ARES handshake cookie"u8];

    private readonly Func<double> _clock;
    private readonly Action<Span<byte>> _random;
    private readonly int _maxConnections;
    private readonly KeySlot[] _slots = new KeySlot[2];
    private readonly Dictionary<IPEndPoint, TransportConnection> _connections = [];
    private int _active;

    /// <summary>Creates the server.</summary>
    /// <param name="networkVersion">Network version checksum clients must present.</param>
    /// <param name="networkFeatures">Network features clients must present.</param>
    /// <param name="sessionId">Session id (0 to 3).</param>
    /// <param name="clock">Monotonic clock in seconds, positive; defaults to seconds since creation plus one.</param>
    /// <param name="random">Random byte source; defaults to the system's cryptographic generator.</param>
    /// <param name="maxConnections">Largest number of connections.</param>
    public HandshakeServer(
        uint networkVersion,
        ushort networkFeatures,
        int sessionId = 0,
        Func<double>? clock = null,
        Action<Span<byte>>? random = null,
        int maxConnections = 256)
    {
        if (sessionId is < 0 or > 3 || maxConnections < 1)
        {
            throw new ArgumentException("Invalid handshake configuration");
        }

        NetworkVersion = networkVersion;
        NetworkFeatures = networkFeatures;
        SessionId = sessionId;
        long started = Stopwatch.GetTimestamp();
        _clock = clock ?? (() => 1 + Stopwatch.GetElapsedTime(started).TotalSeconds);
        _random = random ?? RandomNumberGenerator.Fill;
        _maxConnections = maxConnections;
        double now = _clock();
        _slots[0] = new KeySlot(NewKey(), now, double.PositiveInfinity);
        _slots[1] = new KeySlot(NewKey(), double.NegativeInfinity, double.NegativeInfinity);
    }

    /// <summary>Network version checksum.</summary>
    public uint NetworkVersion { get; }

    /// <summary>Network features.</summary>
    public ushort NetworkFeatures { get; }

    /// <summary>Session id.</summary>
    public int SessionId { get; }

    /// <summary>Connections whose handshake completed, by peer.</summary>
    public IReadOnlyDictionary<IPEndPoint, TransportConnection> Connections => _connections;

    /// <summary>Handles a handshake datagram.</summary>
    /// <param name="datagram">The datagram.</param>
    /// <param name="peer">Its sender (IPv4).</param>
    /// <returns>The reply to send, if any, and what happened. Request contents are never part of the event.</returns>
    public (byte[]? Reply, HandshakeEvent Event) Handle(byte[] datagram, IPEndPoint peer)
    {
        ArgumentNullException.ThrowIfNull(peer);
        double now = _clock();
        Maintain(now);
        HandshakePacket packet;
        try
        {
            packet = HandshakePacket.Decode(datagram);
        }
        catch (InvalidPacketException)
        {
            return (null, HandshakeEvent.Invalid);
        }

        if (peer.AddressFamily != AddressFamily.InterNetwork || peer.Port == 0)
        {
            return (null, HandshakeEvent.Invalid);
        }

        StatelessPrefix prefix = packet.Prefix;
        if (prefix.NetworkVersion != NetworkVersion || packet.NetworkFeatures != NetworkFeatures)
        {
            return (null, HandshakeEvent.Incompatible);
        }

        switch ((HandshakeType)prefix.PacketType)
        {
            case HandshakeType.Initial:
                if (packet.Timestamp != 0 || packet.SecretId != 0 || packet.Cookie.Any(b => b != 0))
                {
                    return (null, HandshakeEvent.InvalidInitial);
                }

                return (Reply(packet, HandshakeType.Challenge, _active, now, Cookie(_slots[_active].Key, peer, now)), HandshakeEvent.ChallengeSent);
            case HandshakeType.Response:
                return Verify(packet, peer, now);
            default:
                return (null, HandshakeEvent.UnexpectedType);
        }
    }

    private (byte[]? Reply, HandshakeEvent Event) Verify(HandshakePacket packet, IPEndPoint peer, double now)
    {
        if (packet.Prefix.SessionId != SessionId)
        {
            return (null, HandshakeEvent.WrongSession);
        }

        KeySlot slot = _slots[packet.SecretId];
        double age = now - packet.Timestamp;
        if (packet.Timestamp <= 0 || age is < 0 or >= CookieLifetime || packet.Timestamp < slot.IssuedFrom || packet.Timestamp >= slot.IssuedUntil)
        {
            return (null, HandshakeEvent.Expired);
        }

        if (!CryptographicOperations.FixedTimeEquals(packet.Cookie, Cookie(slot.Key, peer, packet.Timestamp)))
        {
            return (null, HandshakeEvent.InvalidCookie);
        }

        TransportConnection? old = _connections.GetValueOrDefault(peer);
        if (old is not null && (old.ClientId != packet.Prefix.ClientId || !old.Cookie.AsSpan().SequenceEqual(packet.Cookie)))
        {
            return (null, HandshakeEvent.ConnectionExists);
        }

        if (old is null && _connections.Count >= _maxConnections)
        {
            return (null, HandshakeEvent.CapacityReached);
        }

        _connections[peer] = new TransportConnection(packet.Prefix.ClientId, packet.Cookie, now + ConnectionLifetime);
        return (Reply(packet, HandshakeType.Ack, 1, -1.0, packet.Cookie), old is null ? HandshakeEvent.CookieVerified : HandshakeEvent.AckRetried);
    }

    /// <summary>Removes a connection (closed or timed out by the connection layer).</summary>
    /// <param name="peer">The peer.</param>
    public void Forget(IPEndPoint peer) => _connections.Remove(peer);

    /// <summary>Extends a connection entry after traffic from the peer.</summary>
    /// <param name="peer">The peer.</param>
    public void Touch(IPEndPoint peer)
    {
        if (_connections.TryGetValue(peer, out TransportConnection? connection))
        {
            _connections[peer] = connection with { ExpiresAt = _clock() + ConnectionLifetime };
        }
    }

    private byte[] Reply(HandshakePacket request, HandshakeType type, int secret, double timestamp, byte[] cookie)
    {
        byte[] padding = new byte[request.Padding.Length];
        _random(padding);
        StatelessPrefix prefix = new(SessionId, request.Prefix.ClientId, false, 4, 4, (byte)type, request.Prefix.SentCount, NetworkVersion);
        return new HandshakePacket(prefix, NetworkFeatures, secret, timestamp, cookie, padding).Encode();
    }

    /// <summary>Rotates keys and expires idle connections.</summary>
    public void Maintain() => Maintain(_clock());

    private void Maintain(double now)
    {
        if (now - _slots[_active].IssuedFrom >= KeyLifetime)
        {
            _slots[_active] = _slots[_active] with { IssuedUntil = now };
            _active ^= 1;
            _slots[_active] = new KeySlot(NewKey(), now, double.PositiveInfinity);
        }

        foreach (IPEndPoint peer in _connections.Where(pair => pair.Value.ExpiresAt <= now).Select(pair => pair.Key).ToArray())
        {
            _connections.Remove(peer);
        }
    }

    private byte[] NewKey()
    {
        byte[] key = new byte[KeyBytes];
        _random(key);
        return key;
    }

    private static byte[] Cookie(byte[] key, IPEndPoint peer, double timestamp)
    {
        Span<byte> input = stackalloc byte[s_label.Length + 8 + 4 + 2];
        s_label.CopyTo(input);
        BinaryPrimitives.WriteDoubleLittleEndian(input[s_label.Length..], timestamp);
        peer.Address.TryWriteBytes(input.Slice(s_label.Length + 8, 4), out _);
        BinaryPrimitives.WriteUInt16BigEndian(input[(s_label.Length + 12)..], (ushort)peer.Port);
        return HMACSHA256.HashData(key, input)[..HandshakePacket.CookieLength];
    }

    private sealed record KeySlot(byte[] Key, double IssuedFrom, double IssuedUntil);
}
