using Ares.Structs.Core.Serialization;

namespace Ares.Structs.Game.Movement;

/// <summary>Frame numbers of the Pendulum streams (client input frames).</summary>
public static class PendulumFrames
{
    /// <summary>Exclusive upper bound of a frame number.</summary>
    public const uint Range = 0x7FFF_FFFF;

    /// <summary>Exclusive upper bound of a short frame-number delta.</summary>
    public const uint DeltaLimit = 30;

    /// <summary>Exclusive upper bound of a baseline frame number.</summary>
    public const uint BaselineRange = 0xFFFF_FFFF;

    /// <summary>Reads a frame number relative to <paramref name="baseline"/>: equal, a short delta, or absolute.</summary>
    /// <param name="reader">Source.</param>
    /// <param name="baseline">Baseline frame.</param>
    /// <returns>The frame number.</returns>
    public static uint ReadRelative(BitReader reader, uint baseline)
    {
        ArgumentNullException.ThrowIfNull(reader);
        if (reader.ReadBit())
        {
            return baseline;
        }

        if (!reader.ReadBit())
        {
            return reader.ReadBounded(Range);
        }

        bool positive = reader.ReadBit();
        uint delta = reader.ReadBounded(DeltaLimit);
        return positive ? baseline + delta : baseline - delta;
    }

    /// <summary>Writes a frame number relative to <paramref name="baseline"/>.</summary>
    /// <param name="writer">Destination.</param>
    /// <param name="latest">Frame number.</param>
    /// <param name="baseline">Baseline frame.</param>
    public static void WriteRelative(BitWriter writer, uint latest, uint baseline)
    {
        ArgumentNullException.ThrowIfNull(writer);
        long delta = (long)latest - baseline;
        writer.WriteBit(delta == 0);
        if (delta == 0)
        {
            return;
        }

        bool small = Math.Abs(delta) < DeltaLimit;
        writer.WriteBit(small);
        if (small)
        {
            writer.WriteBit(delta > 0);
            writer.WriteBounded((uint)Math.Abs(delta), DeltaLimit);
        }
        else
        {
            writer.WriteBounded(latest, Range);
        }
    }
}

/// <summary>
/// Header of a client input-history payload (direct-replication stream 1): the acknowledged baseline frame
/// and the latest frame; the frames between them follow, coded against the baseline and then each other.
/// </summary>
/// <param name="Baseline">Last frame the server acknowledged (0 when none).</param>
/// <param name="Latest">Latest client frame.</param>
public sealed record PendulumInputHeader(uint Baseline, uint Latest)
{
    /// <summary>Largest payload in bytes.</summary>
    public const int MaxPayloadBytes = 768;

    /// <summary>Reads the header; null when the payload carries no frames (everything acknowledged).</summary>
    /// <param name="reader">Source, at the start of the payload.</param>
    /// <returns>The header, or null.</returns>
    public static PendulumInputHeader? Read(BitReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);
        if (reader.Length is < 8 or > MaxPayloadBytes * 8)
        {
            throw new InvalidPacketException("Invalid input payload size");
        }

        if (!reader.ReadBit())
        {
            return null;
        }

        uint baseline = reader.ReadBounded(PendulumFrames.BaselineRange);
        uint latest = PendulumFrames.ReadRelative(reader, baseline);
        return baseline < PendulumFrames.Range && latest < PendulumFrames.Range
            ? new PendulumInputHeader(baseline, latest)
            : throw new InvalidPacketException("Input frame outside its range");
    }
}

/// <summary>
/// The server pawn's blackboard update (a delta-serialized property): baseline, reset flag, frame number,
/// two flag bits, then the frame body coded against the baseline (or the defaults on reset).
/// </summary>
public static class BlackboardUpdate
{
    /// <summary>Bits of a frame body with no changes from its baseline.</summary>
    public const int UnchangedFrameBits = 74;

    /// <summary>Flag bits the game's frames carry.</summary>
    public const int DefaultFlags = 1;

    /// <summary>Writes an update.</summary>
    /// <param name="writer">Destination.</param>
    /// <param name="baseline">Baseline frame (0 on reset).</param>
    /// <param name="latest">Frame number, above the baseline.</param>
    /// <param name="reset">Whether the client resets its blackboard frames first.</param>
    /// <param name="flags">Two flag bits (received frames carry 1).</param>
    /// <param name="body">Frame body bits, or null for an unchanged frame.</param>
    /// <param name="bodyBits">Number of body bits.</param>
    public static void Write(BitWriter writer, uint baseline, uint latest, bool reset, int flags, byte[]? body, long bodyBits)
    {
        ArgumentNullException.ThrowIfNull(writer);
        if (flags is < 0 or > 3 || baseline >= latest || latest >= PendulumFrames.Range)
        {
            throw new InvalidPacketException("Invalid blackboard update header");
        }

        writer.WriteBit(true);
        writer.WriteBounded(baseline, PendulumFrames.BaselineRange);
        writer.WriteBit(reset);
        PendulumFrames.WriteRelative(writer, latest, baseline);
        writer.WriteBit((flags & 2) != 0);
        writer.WriteBit((flags & 1) != 0);
        if (body is null)
        {
            writer.WriteBits(new byte[(UnchangedFrameBits + 7) / 8], UnchangedFrameBits);
        }
        else
        {
            writer.WriteBits(body, bodyBits);
        }
    }
}
