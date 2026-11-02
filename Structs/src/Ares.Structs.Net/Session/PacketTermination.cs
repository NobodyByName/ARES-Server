using Ares.Structs.Core.Serialization;

namespace Ares.Structs.Net.Session;

/// <summary>
/// Packets end with a single set bit after their last data bit; the bytes after it are zero.
/// </summary>
public static class PacketTermination
{
    /// <summary>Number of data bits before the final set bit.</summary>
    /// <param name="packet">The packet.</param>
    /// <returns>The data bit length.</returns>
    public static long DataBits(ReadOnlySpan<byte> packet)
    {
        if (packet.IsEmpty || packet[^1] == 0)
        {
            throw new InvalidPacketException("Packet has no termination bit");
        }

        int highest = 7 - System.Numerics.BitOperations.LeadingZeroCount(packet[^1]) + 24;
        return ((packet.Length - 1) * 8L) + highest;
    }
}
