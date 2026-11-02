using System.Net;
using System.Security.Cryptography;
using Ares.Structs.Core.Serialization;
using Ares.Structs.Net.Control;
using Ares.Structs.Net.Session;

namespace Ares.Raid.Transport;

/// <summary>What a datagram or poll did.</summary>
public enum ConnectionEvent
{
    /// <summary>Handshake handled; see the handshake event.</summary>
    Handshake,

    /// <summary>The routing credential was missing or not valid.</summary>
    RoutingRejected,

    /// <summary>The datagram size is outside the accepted range.</summary>
    InvalidDatagramSize,

    /// <summary>A connection packet from a peer without a completed handshake.</summary>
    PacketBeforeHandshake,

    /// <summary>Session or client ids do not match the connection.</summary>
    WrongConnectionIds,

    /// <summary>A packet that was already received, or that acknowledges unsent packets.</summary>
    ReplayedPacket,

    /// <summary>A direct-replication payload was handled.</summary>
    DirectStreamReceived,

    /// <summary>A direct-replication payload of an unhandled kind was dropped.</summary>
    DirectStreamDropped,

    /// <summary>The packet only acknowledged; nothing to send.</summary>
    Acknowledged,

    /// <summary>A reply was sent.</summary>
    ReplySent,

    /// <summary>A keepalive or retry was sent.</summary>
    KeepaliveSent,

    /// <summary>The client closed the connection, or reported a failure.</summary>
    ClientClosed,

    /// <summary>Hello with another network version or feature set.</summary>
    IncompatibleHello,

    /// <summary>A retransmitted Hello or Login differs from the original.</summary>
    ConflictingRetransmission,

    /// <summary>A control message the connection does not accept at this point.</summary>
    UnsupportedControl,

    /// <summary>The player was not admitted.</summary>
    AuthenticationRejected,

    /// <summary>A reliable buffer overflowed; the connection was dropped.</summary>
    ReliableOverflow,

    /// <summary>The packet was malformed or unsupported; nothing changed.</summary>
    InvalidPacket,

    /// <summary>The connection was dropped after an error while polling.</summary>
    Dropped,
}

/// <summary>Outcome of a datagram or poll.</summary>
/// <param name="Reply">Datagram to send back, if any.</param>
/// <param name="Event">What happened.</param>
/// <param name="Detail">Short reason for rejections and handshake outcomes (never request contents).</param>
public sealed record ConnectionResult(byte[]? Reply, ConnectionEvent Event, string? Detail = null);

/// <summary>
/// Connection packets after the handshake: packet window, reliable control channel, login, Welcome and
/// Join, then the player's world channels. Each packet is processed on a copy of the connection's state,
/// which replaces it only if the whole packet is accepted.
/// </summary>
public sealed class RaidConnections
{
    /// <summary>
    /// Largest packet: the engine's packet size, which bounds the whole datagram (envelope included; the client
    /// drops larger ones) and codes bunch sizes.
    /// </summary>
    public const int MaxPacketBytes = 1024;

    // Envelope bits around the connection payload, rounded up to whole bytes.
    private static readonly long s_envelopeBits = new ConnectionEnvelope(0, 0, [], 0).Encode(fromServer: true).Length * 8L;

    private const int InitialControlMessages = 256;

    private readonly HandshakeServer _handshake;
    private readonly RoutingAuthenticator _authenticator;
    private readonly IRaidWorld _world;
    private readonly Func<byte[], byte[]> _unpad;
    private readonly Func<double> _clock;
    private readonly double _keepaliveInterval;
    private readonly Dictionary<IPEndPoint, ConnectionState> _states = [];

    /// <summary>Creates the connection layer.</summary>
    /// <param name="handshake">Handshake server (owns the connection table).</param>
    /// <param name="authenticator">Admits players.</param>
    /// <param name="world">The raid world.</param>
    /// <param name="unpad">Removes client datagram padding; throws <see cref="InvalidPacketException"/> on bad sizes.</param>
    /// <param name="clock">Monotonic clock in seconds (the handshake's clock).</param>
    /// <param name="keepaliveInterval">Seconds between keepalives and reliable retries.</param>
    public RaidConnections(HandshakeServer handshake, RoutingAuthenticator authenticator, IRaidWorld world, Func<byte[], byte[]> unpad, Func<double> clock, double keepaliveInterval = 1.0)
    {
        _handshake = handshake ?? throw new ArgumentNullException(nameof(handshake));
        _authenticator = authenticator ?? throw new ArgumentNullException(nameof(authenticator));
        _world = world ?? throw new ArgumentNullException(nameof(world));
        _unpad = unpad ?? throw new ArgumentNullException(nameof(unpad));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _keepaliveInterval = double.IsFinite(keepaliveInterval) && keepaliveInterval > 0 ? keepaliveInterval : throw new ArgumentOutOfRangeException(nameof(keepaliveInterval));
    }

    /// <summary>Number of connections with connection state.</summary>
    public int Count => _states.Count;

    /// <summary>Connections with their player channels (null before Join).</summary>
    public IReadOnlyList<(IPEndPoint Peer, IPlayerChannels? Player)> Players => [.. _states.Select(entry => (entry.Key, entry.Value.Player))];

    /// <summary>Handles a datagram from a peer.</summary>
    /// <param name="datagram">The datagram.</param>
    /// <param name="peer">Its sender.</param>
    /// <returns>The reply, if any, and what happened.</returns>
    public ConnectionResult Handle(byte[] datagram, IPEndPoint peer)
    {
        ArgumentNullException.ThrowIfNull(datagram);
        ArgumentNullException.ThrowIfNull(peer);
        byte[] packet;
        byte[] routingToken;
        try
        {
            (packet, routingToken) = _authenticator.Unwrap(datagram);
        }
        catch (AuthenticationRejectedException error)
        {
            Prune();
            return new ConnectionResult(null, ConnectionEvent.RoutingRejected, error.Message);
        }

        try
        {
            packet = _unpad(packet);
        }
        catch (InvalidPacketException)
        {
            Prune();
            return new ConnectionResult(null, ConnectionEvent.InvalidDatagramSize);
        }

        (byte[]? reply, HandshakeEvent handshake) = _handshake.Handle(packet, peer);
        Prune();
        if (reply is not null || handshake != HandshakeEvent.Invalid)
        {
            return new ConnectionResult(reply, ConnectionEvent.Handshake, handshake.ToString());
        }

        if (!_handshake.Connections.TryGetValue(peer, out TransportConnection? connection))
        {
            return new ConnectionResult(null, ConnectionEvent.PacketBeforeHandshake);
        }

        try
        {
            return HandleConnectionPacket(packet, peer, connection, routingToken);
        }
        catch (AuthenticationRejectedException error)
        {
            if (_states.GetValueOrDefault(peer)?.Login is { Authenticated: true })
            {
                Drop(peer);
            }

            return new ConnectionResult(null, ConnectionEvent.AuthenticationRejected, error.Message);
        }
        catch (ReliableOverflowException error)
        {
            Drop(peer);
            return new ConnectionResult(null, ConnectionEvent.ReliableOverflow, error.Message);
        }
        catch (InvalidPacketException error)
        {
            return new ConnectionResult(null, ConnectionEvent.InvalidPacket, error.Message);
        }
    }

    /// <summary>Sends due retries and keepalives without waiting for client traffic.</summary>
    /// <returns>Datagrams to send.</returns>
    public IReadOnlyList<(IPEndPoint Peer, ConnectionResult Result)> Poll()
    {
        double now = _clock();
        _handshake.Maintain();
        Prune();
        List<(IPEndPoint, ConnectionResult)> results = [];
        foreach ((IPEndPoint peer, ConnectionState previous) in _states.ToArray())
        {
            if (previous.Hello is null)
            {
                continue;
            }

            try
            {
                ConnectionState state = previous.Copy();
                if (state.Login is { } login)
                {
                    _authenticator.Validate(login);
                }

                ResolveWorld(state);
                state.Player?.Service(state.Io, now);
                IReadOnlyList<ReliableKey> selected = state.Io.Sender.Select(now, Budget(state));
                if (selected.Count == 0 && state.LastSentAt is { } sentAt && now - sentAt < _keepaliveInterval)
                {
                    continue;
                }

                byte[] reply = Encode(state, _handshake.Connections[peer], selected, now);
                _states[peer] = state;
                results.Add((peer, new ConnectionResult(reply, ConnectionEvent.KeepaliveSent)));
            }
            catch (Exception error) when (error is InvalidPacketException or AuthenticationRejectedException)
            {
                Drop(peer);
                results.Add((peer, new ConnectionResult(null, ConnectionEvent.Dropped, error.Message)));
            }
        }

        return results;
    }

    private ConnectionResult HandleConnectionPacket(byte[] packet, IPEndPoint peer, TransportConnection connection, byte[] routingToken)
    {
        ConnectionEnvelope envelope = ConnectionEnvelope.Decode(packet, fromServer: false);
        if (envelope.SessionId != _handshake.SessionId || envelope.ClientId != connection.ClientId)
        {
            return new ConnectionResult(null, ConnectionEvent.WrongConnectionIds);
        }

        ConnectionState? previous = _states.GetValueOrDefault(peer);
        if (envelope is { DirectKind: { } directKind, PayloadBits: 0 })
        {
            // Sideband-only packets carry no packet sequence; they never change the channel state.
            if (previous?.Login is not { Authenticated: true } login)
            {
                throw new InvalidPacketException("Direct stream before authentication");
            }

            _authenticator.Validate(login);
            if (previous.Player is not { } player || !player.ObserveDirect(directKind, envelope.DirectPayload))
            {
                return new ConnectionResult(null, ConnectionEvent.DirectStreamDropped);
            }

            player.Commit();
            return new ConnectionResult(null, ConnectionEvent.DirectStreamReceived);
        }

        ConnectionPacket incoming = ConnectionPacket.Read(new BitReader(envelope.Payload, envelope.PayloadBits), fromServer: false, MaxPacketBytes);
        ConnectionState state = previous?.Copy() ?? ConnectionState.Create(connection, _keepaliveInterval);
        if (!state.Cookie.AsSpan().SequenceEqual(connection.Cookie))
        {
            return new ConnectionResult(null, ConnectionEvent.InvalidPacket, "Connection cookie changed");
        }

        if (!state.Window.Receive(incoming.Notification))
        {
            return new ConnectionResult(null, ConnectionEvent.ReplayedPacket);
        }

        if (state.Login is { } current)
        {
            _authenticator.Validate(current);
        }

        if (envelope.DirectKind is { } kind)
        {
            if (state.Login is not { Authenticated: true })
            {
                throw new InvalidPacketException("Direct stream before authentication");
            }

            state.Player?.ObserveDirect(kind, envelope.DirectPayload);
        }

        if (state.Player is { } channels)
        {
            channels.Acknowledge(state.Io, incoming.Notification);
        }
        else
        {
            state.Io.Sender.Acknowledge(incoming.Notification);
        }

        foreach (ChannelBunch bunch in incoming.Bunches)
        {
            if (bunch.Channel != 0)
            {
                (state.Player ?? throw new InvalidPacketException("Actor channel before Join")).Receive(state.Io, bunch);
                continue;
            }

            if (bunch.PackageExports || bunch.MustBeMapped || bunch.ReplicationPaused)
            {
                throw new InvalidPacketException("Unsupported control bunch");
            }

            if (bunch.CloseReason is not null)
            {
                byte[] goodbye = Encode(state, connection, [], _clock());
                Drop(peer);
                return new ConnectionResult(goodbye, ConnectionEvent.ClientClosed, "control channel closed");
            }

            if (Retransmission(state, bunch) is { } conflict)
            {
                return conflict;
            }

            foreach (ChannelBunch delivered in state.Io.Receiver.Receive(bunch))
            {
                if (ReceiveControl(state, delivered, routingToken) is not { } outcome)
                {
                    continue;
                }

                if (outcome.Event != ConnectionEvent.ClientClosed)
                {
                    return outcome;
                }

                byte[] goodbye = Encode(state, connection, [], _clock());
                Drop(peer);
                return outcome with { Reply = goodbye };
            }
        }

        if (state.Login is { Authenticated: true } admitted)
        {
            _authenticator.Validate(admitted);
        }

        ResolveWorld(state);
        StartPlayer(state);
        double now = _clock();
        if (state.Login is { } pending)
        {
            if (state.ChallengeKey is { } challenge)
            {
                pending.ChallengeAcknowledged = !state.Io.Sender.Pending(challenge);
            }
            else
            {
                state.ChallengeKey = state.Io.Sender.Queue(ControlBunch(new ControlChallenge(pending.Challenge)));
            }
        }

        state.Player?.Service(state.Io, now);
        IReadOnlyList<ReliableKey> selected = state.Io.Sender.Select(now, Budget(state));
        bool quiet = incoming.Bunches.Count == 0 && selected.Count == 0
            && (state.Hello is null || state.LastSentAt is null || now - state.LastSentAt < _keepaliveInterval);
        byte[]? reply = quiet ? null : Encode(state, connection, selected, now);
        if (state.Login is { } refreshed)
        {
            _authenticator.Refresh(refreshed);
        }

        state.Player?.Commit();
        _handshake.Touch(peer);
        _states[peer] = state;
        return new ConnectionResult(reply, quiet ? ConnectionEvent.Acknowledged : ConnectionEvent.ReplySent);
    }

    // A retransmitted Hello or Login must equal the original (checked before the receiver drops it). Only
    // early in the connection: reliable sequences wrap, and later bunches reuse these numbers.
    private static ConnectionResult? Retransmission(ConnectionState state, ChannelBunch bunch)
    {
        if (state.Hello is null || bunch.Sequence is not { } sequence || state.Io.Receiver.Delivered(0) >= InitialControlMessages)
        {
            return null;
        }

        if (sequence == state.ReliableSeed && ControlMessage.Read(new BitReader(bunch.Payload, bunch.PayloadBits)) != state.Hello)
        {
            return new ConnectionResult(null, ConnectionEvent.ConflictingRetransmission, "Hello");
        }

        return sequence == ((state.ReliableSeed + 1) & (ChannelBunch.SequenceCount - 1)) && state.Login?.LoginFingerprint is { } fingerprint
            && !SHA256.HashData(bunch.Payload).AsSpan().SequenceEqual(fingerprint)
                ? new ConnectionResult(null, ConnectionEvent.ConflictingRetransmission, "Login")
                : null;
    }

    private ConnectionResult? ReceiveControl(ConnectionState state, ChannelBunch bunch, byte[] routingToken)
    {
        BitReader reader = new(bunch.Payload, bunch.PayloadBits);
        if (state is { Hello: not null, Login.Authenticated: true, Travel: { } travel })
        {
            if (bunch.PayloadBits >= 8 && bunch.Payload[0] is (byte)ControlMessageType.Failure or (byte)ControlMessageType.CloseReason)
            {
                return ClientClose(reader);
            }

            foreach (int channel in travel.Receive(bunch.Payload, bunch.PayloadBits).ChannelFailures)
            {
                state.Player?.ActorChannelFailed(channel);
            }

            return null;
        }

        ControlMessage message = ControlMessage.Read(reader);
        if (message is ControlFailure or ControlCloseReason)
        {
            return reader.Remaining == 0 ? ClientClose(new BitReader(bunch.Payload, bunch.PayloadBits)) : throw new InvalidPacketException("Trailing close data");
        }

        if (reader.Remaining != 0)
        {
            return new ConnectionResult(null, ConnectionEvent.UnsupportedControl, "Trailing control data");
        }

        switch (message)
        {
            case ControlHello hello when state.Hello is null:
                if (hello.NetworkVersion != _handshake.NetworkVersion || hello.NetworkFeatures != _handshake.NetworkFeatures)
                {
                    return new ConnectionResult(null, ConnectionEvent.IncompatibleHello);
                }

                state.Hello = hello;
                state.Login = _authenticator.BeginHello(hello.EncryptionToken, routingToken);
                return null;
            case ControlLogin login when state.Login is { Authenticated: false } pending:
                state.Viewport = LoginViewport.FromLogin(login);
                pending.LoginFingerprint = SHA256.HashData(bunch.Payload);
                _authenticator.Complete(pending, login);
                return null;
            default:
                return new ConnectionResult(null, ConnectionEvent.UnsupportedControl, message.Type.ToString());
        }
    }

    private static ConnectionResult ClientClose(BitReader reader)
    {
        string reason = ControlMessage.Read(reader) switch
        {
            ControlFailure failure => failure.Reason,
            ControlCloseReason close => close.Reason,
            _ => "",
        };
        bool safe = reason.Length is > 0 and <= 256 && reason.All(c => char.IsAsciiLetterOrDigit(c) || c is '_' or '.' or ':' or '-' or ',');
        return new ConnectionResult(null, ConnectionEvent.ClientClosed, safe ? reason : $"{reason.Length} characters");
    }

    private void ResolveWorld(ConnectionState state)
    {
        if (state.Travel is not null || state.Login is not { Authenticated: true } login || _world.WelcomeFor(login.Principal) is not { } welcome)
        {
            return;
        }

        state.Travel = new WorldTravel();
        state.WelcomeKey = state.Io.Sender.Queue(ControlBunch(welcome));
    }

    private void StartPlayer(ConnectionState state)
    {
        if (state is { Travel.Joined: true, Player: null, Login: { } login, Viewport: { } viewport })
        {
            state.Player = _world.StartPlayer(login.Principal, viewport, state.Io);
        }
    }

    private static ChannelBunch ControlBunch(ControlMessage message)
    {
        BitWriter writer = new();
        message.Write(writer);
        return new ChannelBunch(0, writer.ToArray(), writer.Length) { Name = ChannelName.Control };
    }

    private static long Budget(ConnectionState state)
    {
        BitWriter writer = new();
        new ConnectionPacket(state.Window.NextHeader(), []).Write(writer, fromServer: true, MaxPacketBytes);
        return (MaxPacketBytes * 8L) - s_envelopeBits - writer.Length;
    }

    private byte[] Encode(ConnectionState state, TransportConnection connection, IReadOnlyList<ReliableKey> selected, double now)
    {
        BitWriter writer = new();
        new ConnectionPacket(state.Window.NextHeader(), state.Io.Sender.Bunches(selected)).Write(writer, fromServer: true, MaxPacketBytes);
        byte[] datagram = new ConnectionEnvelope(_handshake.SessionId, connection.ClientId, writer.ToArray(), writer.Length).Encode(fromServer: true);
        if (datagram.Length > MaxPacketBytes)
        {
            throw new InvalidPacketException("Datagram larger than the client accepts");
        }

        state.Io.Sender.Sent(state.Window.NextOutgoing, selected, now);
        if (state is { Login: { } login, ChallengeKey: { } challenge } && selected.Contains(challenge))
        {
            login.ChallengeSent = true;
        }

        if (state is { Travel: { } travel, WelcomeKey: { } welcome } && selected.Contains(welcome))
        {
            travel.SentWelcome();
        }

        state.Window.CommitSent();
        state.LastSentAt = now;
        return datagram;
    }

    private void Prune()
    {
        foreach (IPEndPoint peer in _states.Keys.Where(peer => !_handshake.Connections.ContainsKey(peer)).ToArray())
        {
            _states.Remove(peer);
        }
    }

    private void Drop(IPEndPoint peer)
    {
        _states.Remove(peer);
        _handshake.Forget(peer);
    }

    private sealed class ConnectionState
    {
        public required byte[] Cookie { get; init; }

        public required PacketWindow Window { get; init; }

        public required int ReliableSeed { get; init; }

        public required ChannelIo Io { get; init; }

        public ControlHello? Hello { get; set; }

        public double? LastSentAt { get; set; }

        public PendingLogin? Login { get; set; }

        public ReliableKey? ChallengeKey { get; set; }

        public LoginViewport? Viewport { get; set; }

        public WorldTravel? Travel { get; set; }

        public ReliableKey? WelcomeKey { get; set; }

        public IPlayerChannels? Player { get; set; }

        public static ConnectionState Create(TransportConnection connection, double retryInterval)
        {
            SequenceSeeds seeds = connection.Seeds;
            return new ConnectionState
            {
                Cookie = connection.Cookie,
                Window = new PacketWindow(seeds.ClientPacket, seeds.ServerPacket),
                ReliableSeed = ((seeds.ClientPacket & (ChannelBunch.SequenceCount - 1)) + 1) & (ChannelBunch.SequenceCount - 1),
                Io = new ChannelIo(new ReliableSender(seeds.ServerPacket, retryInterval), new ReliableReceiver(seeds.ClientPacket)),
            };
        }

        public ConnectionState Copy() => new()
        {
            Cookie = Cookie,
            Window = Window.Copy(),
            ReliableSeed = ReliableSeed,
            Io = new ChannelIo(Io.Sender.Copy(), Io.Receiver.Copy()),
            Hello = Hello,
            LastSentAt = LastSentAt,
            Login = Login?.Copy(),
            ChallengeKey = ChallengeKey,
            Viewport = Viewport,
            Travel = Travel?.Copy(),
            WelcomeKey = WelcomeKey,
            Player = Player?.Copy(),
        };
    }
}
