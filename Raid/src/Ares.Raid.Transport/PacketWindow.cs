using Ares.Structs.Net.Session;

namespace Ares.Raid.Transport;

/// <summary>
/// Packet sequence state of one connection: the last received packet, the next outgoing packet, the last
/// acknowledged outgoing packet, and the receive history sent back in notification headers.
/// Replays and acknowledgements of unsent packets are rejected.
/// </summary>
public sealed class PacketWindow
{
    private const int HistoryBits = 256;

    private System.Numerics.BigInteger _history;

    /// <summary>Starts a window from the handshake seeds.</summary>
    /// <param name="incomingSeed">First packet sequence the peer sends.</param>
    /// <param name="outgoingSeed">First packet sequence this side sends.</param>
    public PacketWindow(int incomingSeed, int outgoingSeed)
    {
        PacketSequence.Check(incomingSeed);
        PacketSequence.Check(outgoingSeed);
        Incoming = (incomingSeed - 1) & PacketSequence.Mask;
        NextOutgoing = outgoingSeed;
        LastOutgoingAck = (outgoingSeed - 1) & PacketSequence.Mask;
    }

    /// <summary>Last packet received from the peer.</summary>
    private int Incoming { get; set; }

    /// <summary>Sequence of the next packet to send.</summary>
    public int NextOutgoing { get; private set; }

    /// <summary>Last outgoing packet the peer acknowledged.</summary>
    private int LastOutgoingAck { get; set; }

    /// <summary>Records a received packet; false for a replay, a stale packet or an impossible acknowledgement.</summary>
    /// <param name="header">The packet's notification header.</param>
    /// <returns>Whether the packet is new and should be processed.</returns>
    public bool Receive(NotificationHeader header)
    {
        ArgumentNullException.ThrowIfNull(header);
        int delta = PacketSequence.Delta(header.Sequence, Incoming);
        if (delta <= 0
            || PacketSequence.Delta(header.Acknowledged, LastOutgoingAck) < 0
            || PacketSequence.Delta(NextOutgoing, header.Acknowledged) <= 0)
        {
            return false;
        }

        Incoming = header.Sequence;
        LastOutgoingAck = header.Acknowledged;
        _history = ((_history << delta) | 1) & ((System.Numerics.BigInteger.One << HistoryBits) - 1);
        return true;
    }

    /// <summary>The header for the next outgoing packet.</summary>
    /// <returns>The header.</returns>
    public NotificationHeader NextHeader()
    {
        int length = (int)_history.GetBitLength();
        int count = Math.Max(1, (length + 31) / 32);
        uint[] words = new uint[count];
        for (int index = 0; index < count; index++)
        {
            words[index] = (uint)((_history >> (32 * index)) & uint.MaxValue);
        }

        return new NotificationHeader(NextOutgoing, Incoming, words);
    }

    /// <summary>A copy for speculative packet processing.</summary>
    /// <returns>The copy.</returns>
    public PacketWindow Copy() => (PacketWindow)MemberwiseClone();

    /// <summary>Marks the next outgoing packet as sent.</summary>
    /// <returns>Its sequence.</returns>
    public int CommitSent()
    {
        int sent = NextOutgoing;
        NextOutgoing = (sent + 1) & PacketSequence.Mask;
        return sent;
    }
}
