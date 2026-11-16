using Ares.Structs.Core.Serialization;

namespace Ares.Structs.Game.Pendulum;

/// <summary>Bit masks as Pendulum frames code them: slot masks, bit fields and array element masks.</summary>
internal static class PendulumMasks
{
    public static bool IsSet(ulong mask, int bit) => ((mask >> bit) & 1) != 0;

    public static ulong Mask(int count) => count >= 64 ? ulong.MaxValue : (1UL << count) - 1;

    public static ulong ReadSparseMask(BitReader reader)
    {
        ulong result = 0;
        for (int offset = 0; offset < PendulumCodec.SlotCount; offset += 8)
        {
            if (!reader.ReadBit())
            {
                result |= reader.ReadBits(8) << offset;
            }
        }

        return result;
    }

    public static void WriteSparseMask(BitWriter writer, ulong mask)
    {
        for (int offset = 0; offset < PendulumCodec.SlotCount; offset += 8)
        {
            ulong unit = (mask >> offset) & 0xFF;
            writer.WriteBit(unit == 0);
            if (unit != 0)
            {
                writer.WriteBits(unit, 8);
            }
        }
    }

    // Bit fields: a byte coding (each byte unchanged, or its new bits) or a run coding (the positions of
    // the changed or unchanged bits). The writer uses runs when few or almost all bits change.

    public static ulong ReadBitfield(BitReader reader, ulong previous, int count)
    {
        if (!reader.ReadBit())
        {
            bool inverted = reader.ReadBit();
            return previous ^ ToUlong(ReadRuns(reader, count, inverted));
        }

        ulong result = previous;
        for (int offset = 0; offset < count; offset += 8)
        {
            int width = Math.Min(8, count - offset);
            if (!reader.ReadBit())
            {
                result = (result & ~(Mask(width) << offset)) | (reader.ReadBits(width) << offset);
            }
        }

        return result;
    }

    public static void WriteBitfield(BitWriter writer, ulong value, ulong previous, int count)
    {
        ulong mask = Mask(count);
        value &= mask;
        previous &= mask;
        bool[] changes = FromUlong(value ^ previous, count);
        if (UsesRuns(changes))
        {
            writer.WriteBit(false);
            WriteRuns(writer, changes);
            return;
        }

        writer.WriteBit(true);
        WriteBytes(writer, FromUlong(value, count), FromUlong(previous, count));
    }

    public static bool[] ReadArrayMask(BitReader reader, int count)
    {
        bool inverted = reader.ReadBit();
        if (!reader.ReadBit())
        {
            return ReadRuns(reader, count, inverted);
        }

        bool[] result = new bool[count];
        Array.Fill(result, !inverted);
        for (int offset = 0; offset < count; offset += 8)
        {
            int width = Math.Min(8, count - offset);
            if (reader.ReadBit())
            {
                continue;
            }

            ulong bits = reader.ReadBits(width);
            for (int index = 0; index < width; index++)
            {
                result[offset + index] = ((bits >> index) & 1) != 0;
            }
        }

        return result;
    }

    public static void WriteArrayMask(BitWriter writer, bool[] changed)
    {
        bool inverted = Inverted(changed);
        writer.WriteBit(inverted);
        if (UsesRuns(changed))
        {
            writer.WriteBit(false);
            WriteRunPositions(writer, changed, inverted);
            return;
        }

        writer.WriteBit(true);
        bool[] baseline = new bool[changed.Length];
        Array.Fill(baseline, !inverted);
        WriteBytes(writer, changed, baseline);
    }

    private static bool[] ReadRuns(BitReader reader, int count, bool inverted)
    {
        bool[] result = new bool[count];
        Array.Fill(result, !inverted);
        int position = 0;
        for (int step = 0; step <= count; step++)
        {
            position += (int)reader.ReadBounded((ulong)(count - position + 1));
            if (position == count)
            {
                return result;
            }

            result[position] = !result[position];
        }

        throw new InvalidPacketException("Invalid bit runs");
    }

    private static void WriteRuns(BitWriter writer, bool[] changes)
    {
        bool inverted = Inverted(changes);
        writer.WriteBit(inverted);
        WriteRunPositions(writer, changes, inverted);
    }

    private static void WriteRunPositions(BitWriter writer, bool[] changes, bool inverted)
    {
        int count = changes.Length;
        int position = 0;
        for (int index = 0; index < count; index++)
        {
            if (changes[index] != inverted)
            {
                continue;
            }

            writer.WriteBounded((uint)(index - position), (ulong)(count - position + 1));
            position = index;
        }

        writer.WriteBounded((uint)(count - position), (ulong)(count - position + 1));
    }

    private static void WriteBytes(BitWriter writer, bool[] value, bool[] baseline)
    {
        for (int offset = 0; offset < value.Length; offset += 8)
        {
            int width = Math.Min(8, value.Length - offset);
            bool same = value.AsSpan(offset, width).SequenceEqual(baseline.AsSpan(offset, width));
            writer.WriteBit(same);
            if (!same)
            {
                writer.WriteBits(ToUlong(value.AsSpan(offset, width)), width);
            }
        }
    }

    // Fewer than half the bits changed: list the changed ones; otherwise the unchanged ones.
    private static bool Inverted(bool[] changes) => changes.Count(change => change) < changes.Length / 2;

    private static bool UsesRuns(bool[] changes)
    {
        int changed = changes.Count(change => change);
        return changed <= changes.Length / 8 || changed >= 7 * changes.Length / 8;
    }

    private static bool[] FromUlong(ulong value, int count)
    {
        bool[] result = new bool[count];
        for (int index = 0; index < count; index++)
        {
            result[index] = ((value >> index) & 1) != 0;
        }

        return result;
    }

    private static ulong ToUlong(ReadOnlySpan<bool> bits)
    {
        ulong result = 0;
        for (int index = 0; index < bits.Length; index++)
        {
            result |= bits[index] ? 1UL << index : 0;
        }

        return result;
    }
}
