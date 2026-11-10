using System.Security.Cryptography;
using Ares.Structs.Core.Serialization;
using Ares.Structs.Net.Session;

namespace Ares.Raid.Transport;

/// <summary>A reliable channel buffer is full; the connection cannot continue.</summary>
/// <param name="message">What overflowed.</param>
public sealed class ReliableOverflowException(string message) : InvalidPacketException(message);

/// <summary>A reliable bunch waiting for its acknowledgement.</summary>
/// <param name="Key">Channel and absolute reliable sequence.</param>
/// <param name="Bunch">The bunch, with its wire sequence.</param>
/// <param name="WireBits">Encoded size.</param>
public sealed record OutgoingBunch(ReliableKey Key, ChannelBunch Bunch, long WireBits)
{
    /// <summary>Whether the peer acknowledged a packet that carried it.</summary>
    public bool Acknowledged { get; set; }

    /// <summary>When it was last sent.</summary>
    public double? LastSentAt { get; set; }

    /// <summary>Packet that last carried it.</summary>
    public int? LastPacket { get; set; }

    /// <summary>Whether it must be resent without waiting for the retry interval.</summary>
    public bool RetryNow { get; set; }

    /// <summary>Number of sends.</summary>
    public int Attempts { get; set; }

    /// <summary>When it was first sent.</summary>
    public double? FirstSentAt { get; set; }

    /// <summary>Packets carrying it that the peer reported as not received.</summary>
    public int Naks { get; set; }

    /// <summary>Packets carrying it that left the packet history without an acknowledgement.</summary>
    public int Evictions { get; set; }

    /// <summary>A short description for diagnostics.</summary>
    /// <param name="now">Current time in seconds.</param>
    /// <returns>The description.</returns>
    public string Describe(double now) =>
        $"channel {Key.Channel} sequence {Key.Sequence} ({WireBits} bits, open {Bunch.Opened}, exports {Bunch.PackageExports}, "
        + $"must-be-mapped {Bunch.MustBeMapped}, close {Bunch.CloseReason?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "none"}): "
        + $"{Attempts} sends over {now - (FirstSentAt ?? now):0.0} s, {Naks} reported lost, {Evictions} unanswered";
}

/// <summary>Identifies a queued bunch.</summary>
/// <param name="Channel">Channel index.</param>
/// <param name="Sequence">Absolute reliable sequence on that channel, or a negative number for an unreliable bunch.</param>
public readonly record struct ReliableKey(int Channel, int Sequence);

/// <summary>
/// Outgoing reliable delivery: per-channel sequences seeded from the handshake cookie, retries with the
/// same bunch in new packets, and in-order completion per channel when packets are acknowledged.
/// Unreliable bunches are sent once; the sender keeps whether their packet arrived.
/// Bunches that would need fragmentation are rejected.
/// </summary>
public sealed class ReliableSender
{
    private const int MaxQueuePerChannel = 255;
    private const int MaxRecords = 1024;
    private const int MaxAttempts = 64;
    private const int MaxPacketHistory = 256;
    private const int FragmentReserveBits = 512;
    private const int MaxOutcomes = 256;

    private readonly int _seed;
    private readonly double _retryInterval;
    private readonly int _maxPacketBytes;
    private readonly Dictionary<int, int> _next = [];
    private readonly Dictionary<int, Queue<ReliableKey>> _queues = [];
    private readonly Dictionary<ReliableKey, OutgoingBunch> _records = [];
    private readonly List<ReliableKey> _order = [];
    private readonly Dictionary<int, ReliableKey[]> _packets = [];
    private readonly List<int> _packetOrder = [];
    private readonly HashSet<int> _active = [0];
    private readonly HashSet<int> _closing = [];
    private readonly HashSet<int> _openAcknowledged = [];
    private readonly Dictionary<ReliableKey, bool> _outcomes = [];
    private readonly Queue<ReliableKey> _outcomeOrder = new();
    private int _nextUnreliable;

    /// <summary>Creates a sender.</summary>
    /// <param name="outgoingSeed">First outgoing packet sequence (from the handshake cookie).</param>
    /// <param name="retryInterval">Seconds before an unacknowledged bunch is resent.</param>
    /// <param name="maxPacketBytes">Largest packet (128 to 1024).</param>
    public ReliableSender(int outgoingSeed, double retryInterval = 1.0, int maxPacketBytes = 1024)
    {
        PacketSequence.Check(outgoingSeed);
        if (!double.IsFinite(retryInterval) || retryInterval <= 0 || maxPacketBytes is < 128 or > 1024)
        {
            throw new InvalidPacketException("Invalid reliable delivery limits");
        }

        _seed = outgoingSeed & (ChannelBunch.SequenceCount - 1);
        _retryInterval = retryInterval;
        _maxPacketBytes = maxPacketBytes;
        LastPacket = (outgoingSeed - 1) & PacketSequence.Mask;
        LastAck = LastPacket;
    }

    private ReliableSender(ReliableSender other)
    {
        _seed = other._seed;
        _retryInterval = other._retryInterval;
        _maxPacketBytes = other._maxPacketBytes;
        LastPacket = other.LastPacket;
        LastAck = other.LastAck;
        _next = new Dictionary<int, int>(other._next);
        _queues = other._queues.ToDictionary(pair => pair.Key, pair => new Queue<ReliableKey>(pair.Value));
        _records = other._records.ToDictionary(pair => pair.Key, pair => pair.Value with { });
        _order = [.. other._order];
        _packets = other._packets.ToDictionary(pair => pair.Key, pair => pair.Value);
        _packetOrder = [.. other._packetOrder];
        _active = [.. other._active];
        _closing = [.. other._closing];
        _openAcknowledged = [.. other._openAcknowledged];
        _outcomes = new Dictionary<ReliableKey, bool>(other._outcomes);
        _outcomeOrder = new Queue<ReliableKey>(other._outcomeOrder);
        _nextUnreliable = other._nextUnreliable;
    }

    /// <summary>Last packet sent.</summary>
    private int LastPacket { get; set; }

    /// <summary>Last packet the peer acknowledged.</summary>
    private int LastAck { get; set; }

    /// <summary>Channels whose opening bunch was acknowledged.</summary>
    public IReadOnlySet<int> OpenAcknowledged => _openAcknowledged;

    /// <summary>A deep copy for speculative packet processing.</summary>
    /// <returns>The copy.</returns>
    public ReliableSender Copy() => new(this);

    /// <summary>Queues a bunch; its reliable sequence is assigned here.</summary>
    /// <param name="bunch">The bunch (its sequence, if set, must be the next one).</param>
    /// <returns>Its key.</returns>
    public ReliableKey Queue(ChannelBunch bunch)
    {
        ArgumentNullException.ThrowIfNull(bunch);
        int channel = bunch.Channel;
        if (channel is < 0 or >= ChannelBunch.ChannelCount)
        {
            throw new InvalidPacketException("Invalid outgoing channel index");
        }

        if (_closing.Contains(channel) || (channel == 0 && !_active.Contains(channel)))
        {
            throw new InvalidPacketException("Cannot queue data after a channel close");
        }

        if (channel == 0 && bunch.Opened)
        {
            throw new InvalidPacketException("The server cannot reopen the control channel");
        }

        int sequence = _next.GetValueOrDefault(channel, _seed) + 1;
        if (bunch.Sequence is { } requested && requested != (sequence & (ChannelBunch.SequenceCount - 1)))
        {
            throw new InvalidPacketException("Reliable sequence does not match the channel sender");
        }

        bunch = bunch with { Sequence = sequence & (ChannelBunch.SequenceCount - 1) };
        BitWriter writer = new();
        bunch.Write(writer, _maxPacketBytes);
        if (writer.Length > (_maxPacketBytes * 8L) - FragmentReserveBits)
        {
            throw new InvalidPacketException("Reliable bunch needs fragmentation, which is not supported");
        }

        Queue<ReliableKey> queue = _queues.TryGetValue(channel, out Queue<ReliableKey>? existing) ? existing : new Queue<ReliableKey>();
        if (queue.Count >= MaxQueuePerChannel || _records.Count >= MaxRecords)
        {
            throw new ReliableOverflowException("Reliable channel buffer exhausted");
        }

        if (channel != 0 && bunch.Opened == _active.Contains(channel))
        {
            throw new InvalidPacketException(bunch.Opened ? "Actor channel is already open" : "Actor channel must be opened before sending data");
        }

        ReliableKey key = new(channel, sequence);
        _next[channel] = sequence;
        queue.Enqueue(key);
        _queues[channel] = queue;
        _records[key] = new OutgoingBunch(key, bunch, writer.Length);
        _order.Add(key);
        if (bunch.Opened)
        {
            _active.Add(channel);
            _openAcknowledged.Remove(channel);
        }

        if (bunch.CloseReason is null)
        {
            return key;
        }

        _closing.Add(channel);
        foreach (ReliableKey unsent in _order.Where(item => item.Channel == channel && item.Sequence < 0 && _records[item].LastSentAt is null).ToArray())
        {
            Settle(unsent, false);
        }

        return key;
    }

    /// <summary>Queues an unreliable bunch on an open actor channel; it is sent once and never resent.</summary>
    /// <param name="bunch">The bunch (no sequence, name, open or close).</param>
    /// <returns>Its key; <see cref="Arrived"/> later tells whether it arrived.</returns>
    public ReliableKey QueueUnreliable(ChannelBunch bunch)
    {
        ArgumentNullException.ThrowIfNull(bunch);
        bunch = bunch with { Name = null };
        if (bunch.Channel is <= 0 or >= ChannelBunch.ChannelCount || bunch.Sequence is not null || bunch.Opened || bunch.CloseReason is not null
            || !_active.Contains(bunch.Channel) || _closing.Contains(bunch.Channel))
        {
            throw new InvalidPacketException("Unreliable data needs an open actor channel");
        }

        BitWriter writer = new();
        bunch.Write(writer, _maxPacketBytes);
        if (writer.Length > (_maxPacketBytes * 8L) - FragmentReserveBits || _records.Count >= MaxRecords)
        {
            throw new InvalidPacketException("Unreliable bunch does not fit");
        }

        ReliableKey key = new(bunch.Channel, --_nextUnreliable);
        _records[key] = new OutgoingBunch(key, bunch, writer.Length);
        _order.Add(key);
        return key;
    }

    /// <summary>Whether an unreliable bunch arrived: true or false once its packet was answered, else null (also once forgotten).</summary>
    /// <param name="key">Its key.</param>
    /// <returns>The outcome.</returns>
    public bool? Arrived(ReliableKey key) => _outcomes.TryGetValue(key, out bool arrived) ? arrived : null;

    /// <summary>Whether a queued bunch is still waiting for completion.</summary>
    /// <param name="key">Its key.</param>
    /// <returns>Whether it is pending.</returns>
    public bool Pending(ReliableKey key) => _records.ContainsKey(key);

    /// <summary>The wire sequence the next bunch on <paramref name="channel"/> will get.</summary>
    /// <param name="channel">Channel index.</param>
    /// <returns>The sequence.</returns>
    public int NextSequence(int channel) =>
        channel is >= 0 and < ChannelBunch.ChannelCount
            ? (_next.GetValueOrDefault(channel, _seed) + 1) & (ChannelBunch.SequenceCount - 1)
            : throw new InvalidPacketException("Invalid outgoing channel index");

    /// <summary>Bunches to send now: unsent, due for retry, or to resend at once; within the bit budget.</summary>
    /// <param name="now">Current time in seconds.</param>
    /// <param name="budgetBits">Bits available in the packet.</param>
    /// <param name="limit">Largest number of bunches (1 to 64).</param>
    /// <returns>The selected keys.</returns>
    public IReadOnlyList<ReliableKey> Select(double now, long budgetBits, int limit = 64)
    {
        if (!double.IsFinite(now) || budgetBits < 0 || limit is < 1 or > 64)
        {
            throw new InvalidPacketException("Invalid reliable selection bounds");
        }

        List<ReliableKey> selected = [];
        foreach (ReliableKey key in _order)
        {
            OutgoingBunch record = _records[key];
            if (record.Acknowledged || (record is { LastSentAt: { } sentAt, RetryNow: false } && now - sentAt < _retryInterval)
                || (record.Key.Sequence < 0 && record.LastSentAt is not null))
            {
                continue;
            }

            if (record.Attempts >= MaxAttempts)
            {
                throw new ReliableOverflowException($"Reliable message retry limit exhausted: {record.Describe(now)}");
            }

            if (record.WireBits > budgetBits)
            {
                continue;
            }

            selected.Add(key);
            budgetBits -= record.WireBits;
            if (selected.Count >= limit)
            {
                break;
            }
        }

        return selected;
    }

    /// <summary>The bunches of selected keys.</summary>
    /// <param name="selected">Keys.</param>
    /// <returns>The bunches.</returns>
    public IReadOnlyList<ChannelBunch> Bunches(IEnumerable<ReliableKey> selected) => [.. selected.Select(key => _records[key].Bunch)];

    /// <summary>Records that a packet carrying <paramref name="selected"/> was sent.</summary>
    /// <param name="packetSequence">The packet's sequence (after the last one sent).</param>
    /// <param name="selected">Keys it carried.</param>
    /// <param name="now">Current time in seconds.</param>
    public void Sent(int packetSequence, IReadOnlyList<ReliableKey> selected, double now)
    {
        ArgumentNullException.ThrowIfNull(selected);
        if (PacketSequence.Delta(packetSequence, LastPacket) <= 0 || !double.IsFinite(now))
        {
            throw new InvalidPacketException("Outgoing reliable packet did not advance");
        }

        if (selected.Distinct().Count() != selected.Count)
        {
            throw new InvalidPacketException("Duplicate reliable record in a packet");
        }

        OutgoingBunch[] records = [.. selected.Select(key => _records.GetValueOrDefault(key) ?? throw new InvalidPacketException("Unknown outgoing reliable record"))];
        if (records.Any(record => record.Acknowledged || record.Attempts >= MaxAttempts))
        {
            throw new InvalidPacketException("Completed outgoing reliable record");
        }

        LastPacket = packetSequence;
        foreach (OutgoingBunch record in records)
        {
            record.FirstSentAt ??= now;
            record.LastSentAt = now;
            record.LastPacket = packetSequence;
            record.RetryNow = false;
            record.Attempts++;
        }

        if (selected.Count != 0)
        {
            _packets[packetSequence] = [.. selected];
            _packetOrder.Add(packetSequence);
        }

        // Only packet references are evicted; their bunches stay queued and become retryable.
        while (_packets.Count > MaxPacketHistory)
        {
            int oldest = _packetOrder[0];
            _packetOrder.RemoveAt(0);
            foreach (ReliableKey key in _packets[oldest])
            {
                if (_records.GetValueOrDefault(key) is not { Acknowledged: false } record || record.LastPacket != oldest)
                {
                    continue;
                }

                if (key.Sequence < 0)
                {
                    Settle(key, false);
                    continue;
                }

                record.RetryNow = true;
                record.Evictions++;
            }

            _packets.Remove(oldest);
        }
    }

    /// <summary>Applies the peer's acknowledgements; returns bunches completed in channel order.</summary>
    /// <param name="header">The peer's notification header.</param>
    /// <returns>Completed bunches.</returns>
    public IReadOnlyList<OutgoingBunch> Acknowledge(NotificationHeader header)
    {
        ArgumentNullException.ThrowIfNull(header);
        if (header.History.Count is < 1 or > NotificationHeader.MaxHistoryWords
            || PacketSequence.Delta(header.Acknowledged, LastPacket) > 0
            || PacketSequence.Delta(header.Acknowledged, LastAck) < 0)
        {
            throw new InvalidPacketException("Invalid reliable packet acknowledgement");
        }

        LastAck = header.Acknowledged;
        foreach (int packet in _packetOrder.ToArray())
        {
            int age = PacketSequence.Delta(header.Acknowledged, packet);
            if (age < 0)
            {
                continue;
            }

            bool received = age < header.History.Count * 32 && ((header.History[age / 32] >> (age % 32)) & 1) != 0;
            foreach (ReliableKey key in _packets[packet])
            {
                if (!_records.TryGetValue(key, out OutgoingBunch? record))
                {
                    continue;
                }

                if (key.Sequence < 0)
                {
                    Settle(key, received);
                }
                else if (received)
                {
                    record.Acknowledged = true;
                }
                else if (!record.Acknowledged && record.LastPacket == packet)
                {
                    record.RetryNow = true;
                    record.Naks++;
                }
            }

            _packets.Remove(packet);
            _packetOrder.Remove(packet);
        }

        List<OutgoingBunch> delivered = [];
        foreach ((int channel, Queue<ReliableKey> queue) in _queues)
        {
            while (queue.Count != 0 && _records[queue.Peek()].Acknowledged)
            {
                ReliableKey key = queue.Dequeue();
                OutgoingBunch record = _records[key];
                _records.Remove(key);
                delivered.Add(record);
                if (record.Bunch.Opened)
                {
                    _openAcknowledged.Add(channel);
                }

                if (record.Bunch.CloseReason is null)
                {
                    continue;
                }

                _active.Remove(channel);
                _closing.Remove(channel);
                _openAcknowledged.Remove(channel);
            }
        }

        _order.RemoveAll(key => !_records.ContainsKey(key));
        foreach (int packet in _packetOrder.ToArray())
        {
            ReliableKey[] keys = [.. _packets[packet].Where(key => _records.TryGetValue(key, out OutgoingBunch? record) && !record.Acknowledged)];
            if (keys.Length == 0)
            {
                _packets.Remove(packet);
                _packetOrder.Remove(packet);
            }
            else
            {
                _packets[packet] = keys;
            }
        }

        return delivered;
    }

    // An unreliable bunch's packet was answered (or forgotten): it leaves the queue and its outcome is kept a while.
    private void Settle(ReliableKey key, bool arrived)
    {
        _records.Remove(key);
        _order.Remove(key);
        _outcomes[key] = arrived;
        _outcomeOrder.Enqueue(key);
        while (_outcomeOrder.Count > MaxOutcomes)
        {
            _outcomes.Remove(_outcomeOrder.Dequeue());
        }
    }
}

/// <summary>
/// Incoming reliable delivery: per-channel ordering with a bounded buffer for early bunches, duplicate
/// and conflict detection for retransmissions, and channel ownership (only the server registers actor
/// channels).
/// </summary>
public sealed class ReliableReceiver
{
    private const int Window = 256;
    private const int MaxQueued = 255;
    private const int MaxSeen = 255;
    private const int Half = ChannelBunch.SequenceCount / 2;

    private readonly int _seed;
    private readonly Dictionary<int, IncomingChannel> _channels = [];

    /// <summary>Creates a receiver.</summary>
    /// <param name="incomingSeed">First incoming packet sequence (from the handshake cookie).</param>
    public ReliableReceiver(int incomingSeed)
    {
        PacketSequence.Check(incomingSeed);
        _seed = incomingSeed & (ChannelBunch.SequenceCount - 1);
        _channels[0] = new IncomingChannel(ChannelName.Control, _seed, false);
    }

    private ReliableReceiver(ReliableReceiver other)
    {
        _seed = other._seed;
        _channels = other._channels.ToDictionary(pair => pair.Key, pair => pair.Value.Copy());
    }

    /// <summary>A deep copy for speculative packet processing.</summary>
    /// <returns>The copy.</returns>
    public ReliableReceiver Copy() => new(this);

    /// <summary>Number of reliable bunches delivered on a channel so far.</summary>
    /// <param name="channel">Channel index.</param>
    /// <returns>The count (0 for an unknown channel).</returns>
    public int Delivered(int channel) => _channels.TryGetValue(channel, out IncomingChannel? current) ? current.Sequence - _seed : 0;

    /// <summary>Whether bunches on an actor channel are accepted (or were, before it closed).</summary>
    /// <param name="channel">Channel index.</param>
    /// <returns>Whether the channel was registered.</returns>
    public bool IsRegistered(int channel) => _channels.ContainsKey(channel);

    /// <summary>Accepts bunches on an actor channel the server opened.</summary>
    /// <param name="channel">Channel index.</param>
    public void RegisterActor(int channel)
    {
        if (channel is < 1 or >= ChannelBunch.ChannelCount)
        {
            throw new InvalidPacketException("Invalid actor channel index");
        }

        IncomingChannel? current = _channels.GetValueOrDefault(channel);
        if (current is { Closed: false })
        {
            throw new InvalidPacketException("Actor channel is already bound");
        }

        _channels[channel] = new IncomingChannel(ChannelName.Actor, current?.Sequence ?? _seed, true, current?.Seen);
    }

    /// <summary>Stops accepting new bunches on an actor channel.</summary>
    /// <param name="channel">Channel index.</param>
    public void CloseActor(int channel)
    {
        if (channel is < 1 or >= ChannelBunch.ChannelCount || _channels.GetValueOrDefault(channel) is not { Closed: false } current)
        {
            throw new InvalidPacketException("No live actor channel to close");
        }

        current.Closed = true;
        current.Queued.Clear();
    }

    /// <summary>Receives a bunch; returns the bunches now deliverable in order (none for duplicates or early ones).</summary>
    /// <param name="bunch">The bunch.</param>
    /// <returns>Deliverable bunches.</returns>
    public IReadOnlyList<ChannelBunch> Receive(ChannelBunch bunch)
    {
        ArgumentNullException.ThrowIfNull(bunch);
        bunch.Validate();
        IncomingChannel current = _channels.GetValueOrDefault(bunch.Channel) ?? throw new InvalidPacketException("Bunch for a channel that is not open");
        if (bunch.Name is { } name && name != current.Name)
        {
            throw new InvalidPacketException("Incoming channel type changed");
        }

        if (bunch.Channel != 0 && bunch.MustBeMapped)
        {
            throw new InvalidPacketException("Client actor bunches cannot carry must-map dependencies");
        }

        if (bunch.Sequence is not { } wire)
        {
            return current is { Closed: false, Opened: true } && bunch is { Opened: false, CloseReason: null }
                ? [bunch]
                : throw new InvalidPacketException("Unreliable data cannot open or close a channel");
        }

        int delta = (wire - (current.Sequence & (ChannelBunch.SequenceCount - 1))) & (ChannelBunch.SequenceCount - 1);
        if (delta == Half)
        {
            throw new InvalidPacketException("Ambiguous reliable sequence");
        }

        delta = delta < Half ? delta : delta - ChannelBunch.SequenceCount;
        int absolute = current.Sequence + delta;
        byte[] fingerprint = Fingerprint(bunch);
        if (delta <= 0)
        {
            return current.Seen.TryGetValue(absolute, out byte[]? seen) && !seen.AsSpan().SequenceEqual(fingerprint)
                ? throw new InvalidPacketException("Conflicting reliable retransmission")
                : [];
        }

        if (current.Closed)
        {
            throw new InvalidPacketException("New data after a channel close");
        }

        if (delta > Window)
        {
            throw new ReliableOverflowException("Incoming reliable window exhausted");
        }

        if (current.Queued.TryGetValue(absolute, out (ChannelBunch Bunch, byte[] Fingerprint) queued))
        {
            return queued.Fingerprint.AsSpan().SequenceEqual(fingerprint) ? [] : throw new InvalidPacketException("Conflicting queued retransmission");
        }

        if (delta > 1 && current.Queued.Count >= MaxQueued)
        {
            throw new ReliableOverflowException("Incoming reliable buffer exhausted");
        }

        if (bunch.Opened && (current.Opened || delta != 1))
        {
            throw new InvalidPacketException("Peer reopened a channel or opened one out of order");
        }

        if (!current.Opened && delta == 1 && !bunch.Opened)
        {
            throw new InvalidPacketException("The control channel must open before its first message");
        }

        current.Queued[absolute] = (bunch, fingerprint);
        List<ChannelBunch> result = [];
        while (current.Queued.Remove(current.Sequence + 1, out (ChannelBunch Bunch, byte[] Fingerprint) next))
        {
            current.Sequence++;
            current.Opened |= next.Bunch.Opened;
            current.Seen[current.Sequence] = next.Fingerprint;
            current.SeenOrder.Enqueue(current.Sequence);
            while (current.Seen.Count > MaxSeen)
            {
                current.Seen.Remove(current.SeenOrder.Dequeue());
            }

            result.Add(next.Bunch);
            if (next.Bunch.CloseReason is null)
            {
                continue;
            }

            current.Closed = true;
            current.Queued.Clear();
            break;
        }

        return result;
    }

    private static byte[] Fingerprint(ChannelBunch bunch)
    {
        BitWriter writer = new();
        bunch.Write(writer, 1024);
        writer.WriteBits((ulong)writer.Length & 0xFFFF, 16);
        return SHA256.HashData(writer.ToArray());
    }

    private sealed class IncomingChannel(ChannelName name, int sequence, bool opened, Dictionary<int, byte[]>? seen = null)
    {
        public ChannelName Name { get; } = name;

        public int Sequence { get; set; } = sequence;

        public bool Opened { get; set; } = opened;

        public bool Closed { get; set; }

        public Dictionary<int, (ChannelBunch Bunch, byte[] Fingerprint)> Queued { get; } = [];

        public Dictionary<int, byte[]> Seen { get; } = seen is null ? [] : new Dictionary<int, byte[]>(seen);

        public Queue<int> SeenOrder { get; private init; } = new(seen is null ? [] : seen.Keys.Order());

        public IncomingChannel Copy()
        {
            IncomingChannel copy = new(Name, Sequence, Opened, Seen) { Closed = Closed, SeenOrder = new Queue<int>(SeenOrder) };
            foreach ((int sequence, (ChannelBunch, byte[]) item) in Queued)
            {
                copy.Queued[sequence] = item;
            }

            return copy;
        }
    }
}
