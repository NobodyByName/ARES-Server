using Ares.Structs.Core.Serialization;

namespace Ares.Structs.Net.Session;

/// <summary>Packet sequence numbers: 14 bits, wrapping.</summary>
public static class PacketSequence
{
    /// <summary>Bits per sequence number.</summary>
    public const int Bits = 14;

    /// <summary>Number of distinct sequence numbers.</summary>
    public const int Count = 1 << Bits;

    /// <summary>Mask of a sequence number.</summary>
    public const int Mask = Count - 1;

    /// <summary>Signed distance from <paramref name="older"/> to <paramref name="newer"/>, in [-8192, 8191].</summary>
    /// <param name="newer">Newer sequence number.</param>
    /// <param name="older">Older sequence number.</param>
    /// <returns>The distance.</returns>
    public static int Delta(int newer, int older)
    {
        Check(newer);
        Check(older);
        int delta = (newer - older) & Mask;
        return delta < Count / 2 ? delta : delta - Count;
    }

    /// <summary>Throws unless <paramref name="sequence"/> is a valid sequence number.</summary>
    /// <param name="sequence">The sequence number.</param>
    public static void Check(int sequence)
    {
        if (sequence is < 0 or > Mask)
        {
            throw new InvalidPacketException("Invalid packet sequence");
        }
    }
}

/// <summary>First packet sequence numbers of a connection, taken from the handshake cookie.</summary>
/// <param name="ServerPacket">First server packet sequence.</param>
/// <param name="ClientPacket">First client packet sequence.</param>
public sealed record SequenceSeeds(int ServerPacket, int ClientPacket)
{
    /// <summary>Seeds from the first two little-endian 16-bit words of the cookie.</summary>
    /// <param name="cookie">The 20-byte cookie.</param>
    /// <returns>The seeds.</returns>
    public static SequenceSeeds FromCookie(ReadOnlySpan<byte> cookie)
    {
        return cookie.Length == HandshakePacket.CookieLength
            ? new SequenceSeeds((cookie[0] | (cookie[1] << 8)) & PacketSequence.Mask, (cookie[2] | (cookie[3] << 8)) & PacketSequence.Mask)
            : throw new InvalidPacketException("Cookie must have 20 bytes");
    }
}

/// <summary>
/// Packet notification header: this packet's sequence, the last received sequence and the
/// receive history (1 to 8 words, most recent packet in the lowest bit).
/// </summary>
/// <param name="Sequence">Sequence of this packet.</param>
/// <param name="Acknowledged">Last sequence received from the peer.</param>
/// <param name="History">Receive history words.</param>
public sealed record NotificationHeader(int Sequence, int Acknowledged, IReadOnlyList<uint> History)
{
    /// <summary>Largest number of history words.</summary>
    public const int MaxHistoryWords = 8;

    /// <summary>Writes the header.</summary>
    /// <param name="writer">Destination.</param>
    public void Write(BitWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        PacketSequence.Check(Sequence);
        PacketSequence.Check(Acknowledged);
        if (History.Count is < 1 or > MaxHistoryWords)
        {
            throw new InvalidPacketException("Invalid notification history length");
        }

        writer.WriteBits(((uint)Sequence << 18) | ((uint)Acknowledged << 4) | (uint)(History.Count - 1), 32);
        foreach (uint word in History)
        {
            writer.WriteBits(word, 32);
        }
    }

    /// <summary>Reads a header.</summary>
    /// <param name="reader">Source.</param>
    /// <returns>The header.</returns>
    public static NotificationHeader Read(BitReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);
        uint packed = (uint)reader.ReadBits(32);
        int count = (int)(packed & 15) + 1;
        if (count > MaxHistoryWords)
        {
            throw new InvalidPacketException("Notification history too long");
        }

        uint[] history = new uint[count];
        for (int index = 0; index < count; index++)
        {
            history[index] = (uint)reader.ReadBits(32);
        }

        return new NotificationHeader((int)(packed >> 18) & PacketSequence.Mask, (int)(packed >> 4) & PacketSequence.Mask, history);
    }
}
