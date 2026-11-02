using Ares.Structs.Core.Serialization;
using Ares.Structs.Net.Replication;

namespace Ares.Structs.Net.Session;

/// <summary>Channel types that can be opened, by their built-in name index.</summary>
public enum ChannelName
{
    /// <summary>Actor channel.</summary>
    Actor = 102,

    /// <summary>Control channel (always channel 0).</summary>
    Control = 255,
}

/// <summary>
/// One unfragmented bunch of a channel: control flags, reliability, channel index, export flags,
/// the channel type when opening or reliable, and the payload bits.
/// </summary>
/// <param name="Channel">Channel index (0 to 32767; 0 is the control channel).</param>
/// <param name="Payload">Payload bits.</param>
/// <param name="PayloadBits">Number of payload bits.</param>
public sealed record ChannelBunch(int Channel, byte[] Payload, long PayloadBits)
{
    /// <summary>Number of channel indexes.</summary>
    public const int ChannelCount = 32768;

    /// <summary>Range of reliable sequence numbers.</summary>
    public const int SequenceCount = 1024;

    /// <summary>Range of close reasons.</summary>
    public const int CloseReasonCount = 15;

    /// <summary>Channel type; required when the bunch is reliable or opens the channel, absent otherwise.</summary>
    public ChannelName? Name { get; init; }

    /// <summary>Reliable sequence number; null for an unreliable bunch.</summary>
    public int? Sequence { get; init; }

    /// <summary>Whether the bunch opens the channel.</summary>
    public bool Opened { get; init; }

    /// <summary>Close reason when the bunch closes the channel.</summary>
    public int? CloseReason { get; init; }

    /// <summary>Whether package exports precede the payload.</summary>
    public bool PackageExports { get; init; }

    /// <summary>Whether the payload references objects that must be mapped first.</summary>
    public bool MustBeMapped { get; init; }

    /// <summary>Replication-paused flag.</summary>
    public bool ReplicationPaused { get; init; }

    /// <summary>A reliable control channel bunch.</summary>
    /// <param name="sequence">Reliable sequence number.</param>
    /// <param name="payload">Payload bits.</param>
    /// <param name="payloadBits">Number of payload bits.</param>
    /// <returns>The bunch.</returns>
    public static ChannelBunch Control(int sequence, byte[] payload, long payloadBits) =>
        new(0, payload, payloadBits) { Name = ChannelName.Control, Sequence = sequence };

    /// <summary>Throws unless the bunch is consistent.</summary>
    public void Validate()
    {
        if (Channel is < 0 or >= ChannelCount)
        {
            throw new InvalidPacketException("Channel index out of range");
        }

        if (Sequence is < 0 or >= SequenceCount || CloseReason is < 0 or >= CloseReasonCount)
        {
            throw new InvalidPacketException("Invalid sequence or close reason");
        }

        bool needsName = Sequence is not null || Opened;
        if (needsName != Name is not null || (Name is { } name && !Enum.IsDefined(name)))
        {
            throw new InvalidPacketException("Missing, misplaced or unsupported channel name");
        }

        if (Channel == 0 ? Sequence is null || Name != ChannelName.Control : Name == ChannelName.Control)
        {
            throw new InvalidPacketException("The control channel is channel 0, reliable and named Control");
        }

        if (PayloadBits < 0 || PayloadBits > Payload.Length * 8L)
        {
            throw new InvalidPacketException("Invalid payload length");
        }
    }

    /// <summary>Writes the bunch.</summary>
    /// <param name="writer">Destination.</param>
    /// <param name="maxPacketBytes">Largest packet; bounds the payload length field.</param>
    public void Write(BitWriter writer, int maxPacketBytes)
    {
        ArgumentNullException.ThrowIfNull(writer);
        Validate();
        bool control = Opened || CloseReason is not null;
        writer.WriteBit(control);
        if (control)
        {
            writer.WriteBit(Opened);
            writer.WriteBit(CloseReason is not null);
            if (CloseReason is { } reason)
            {
                writer.WriteBounded((uint)reason, CloseReasonCount);
            }
        }

        writer.WriteBit(ReplicationPaused);
        writer.WriteBit(Sequence is not null);
        writer.WritePacked((uint)Channel);
        writer.WriteBit(PackageExports);
        writer.WriteBit(MustBeMapped);
        writer.WriteBit(false);
        if (Sequence is { } sequence)
        {
            writer.WriteBounded((uint)sequence, SequenceCount);
        }

        if (Name is { } name)
        {
            new NetworkName(name.ToString(), 0).Write(writer);
        }

        writer.WriteBounded((uint)PayloadBits, MaxPayloadBits(maxPacketBytes));
        writer.WriteBits(Payload, PayloadBits);
    }

    /// <summary>Reads a bunch. Fragmented bunches are not supported.</summary>
    /// <param name="reader">Source.</param>
    /// <param name="maxPacketBytes">Largest packet; bounds the payload length field.</param>
    /// <returns>The bunch.</returns>
    public static ChannelBunch Read(BitReader reader, int maxPacketBytes)
    {
        ArgumentNullException.ThrowIfNull(reader);
        bool control = reader.ReadBit();
        bool opened = control && reader.ReadBit();
        bool closed = control && reader.ReadBit();
        int? closeReason = closed ? (int)reader.ReadBounded(CloseReasonCount) : null;
        bool paused = reader.ReadBit();
        bool reliable = reader.ReadBit();
        uint channel = reader.ReadPacked();
        bool exports = reader.ReadBit();
        bool mapped = reader.ReadBit();
        if (reader.ReadBit())
        {
            throw new InvalidPacketException("Fragmented bunches are not supported");
        }

        int? sequence = reliable ? (int)reader.ReadBounded(SequenceCount) : null;
        ChannelName? name = reliable || opened ? ReadName(reader) : null;
        uint size = reader.ReadBounded(MaxPayloadBits(maxPacketBytes));
        if (size > reader.Remaining)
        {
            throw new InvalidPacketException("Truncated bunch payload");
        }

        ChannelBunch bunch = new(channel < ChannelCount ? (int)channel : -1, reader.ReadBitArray(size), size)
        {
            Name = name,
            Sequence = sequence,
            Opened = opened,
            CloseReason = closeReason,
            PackageExports = exports,
            MustBeMapped = mapped,
            ReplicationPaused = paused,
        };
        bunch.Validate();
        return bunch;
    }

    private static ChannelName ReadName(BitReader reader)
    {
        NetworkName name = NetworkName.Read(reader, 64);
        return name switch
        {
            { Text: nameof(ChannelName.Actor), Number: 0 } => ChannelName.Actor,
            { Text: nameof(ChannelName.Control), Number: 0 } => ChannelName.Control,
            _ => throw new InvalidPacketException("Unsupported channel type"),
        };
    }

    private static ulong MaxPayloadBits(int maxPacketBytes) => (ulong)ConnectionPacket.CheckMaxPacketBytes(maxPacketBytes) * 8;
}
