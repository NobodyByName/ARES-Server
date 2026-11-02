using System.Numerics;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Core.Serialization;

namespace Ares.Structs.Net.Replication;

/// <summary>
/// A vector quantized for the network, as <c>FVector_NetQuantize100</c> sends it: the components are scaled,
/// rounded to whole numbers and sent with the bit count the largest needs, or sent as full doubles when they are
/// too large to scale. A header of 7 bits carries the bit count (6 bits) and whether the values are scaled.
/// </summary>
public static class QuantizedVector
{
    /// <summary>Scale of <c>FVector_NetQuantize100</c> (two decimal places).</summary>
    public const int Hundredths = 100;

    // Beyond 2^52 scaling adds no precision; beyond 2^62 rounding loses it, so such values go as full doubles.
    private const double MaxValueToScale = 1L << 52;
    private const double MaxScaledValue = 1L << 62;
    private const uint ScaledFlag = 1U << 6;
    private const uint BitCountMask = ScaledFlag - 1;
    private const ulong HeaderLimit = 1U << 7;
    private const int SingleBits = 32;
    private const int DoubleBits = 64;

    /// <summary>Writes a vector.</summary>
    /// <param name="writer">Destination.</param>
    /// <param name="value">The vector; its components must be finite.</param>
    /// <param name="scale">Scale factor (1 to 32767).</param>
    public static void Write(BitWriter writer, Vector3D value, int scale)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentOutOfRangeException.ThrowIfLessThan(scale, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(scale, short.MaxValue);
        double[] components = [value.X, value.Y, value.Z];
        if (components.Any(component => !double.IsFinite(component)))
        {
            throw new InvalidPacketException("Quantized vector components must be finite");
        }

        double[] scaled = [.. components.Select(component => component * scale)];
        if (scaled.Max(Math.Abs) >= MaxScaledValue)
        {
            writer.WriteBounded(ScaledFlag, HeaderLimit);
            foreach (double component in components)
            {
                writer.WriteBits(BitConverter.DoubleToUInt64Bits(component), DoubleBits);
            }

            return;
        }

        bool useScaled = components.Min(Math.Abs) < MaxValueToScale;
        long[] integers = [.. (useScaled ? scaled : components).Select(Round)];
        int bits = integers.Max(BitsNeeded);
        writer.WriteBounded((useScaled ? ScaledFlag : 0) | (uint)bits, HeaderLimit);
        ulong mask = (1UL << bits) - 1;
        foreach (long integer in integers)
        {
            writer.WriteBits((ulong)integer & mask, bits);
        }
    }

    /// <summary>Reads a vector.</summary>
    /// <param name="reader">Source.</param>
    /// <param name="scale">Scale factor (1 to 32767).</param>
    /// <returns>The vector.</returns>
    public static Vector3D Read(BitReader reader, int scale)
    {
        ArgumentNullException.ThrowIfNull(reader);
        ArgumentOutOfRangeException.ThrowIfLessThan(scale, 1);
        uint header = reader.ReadBounded(HeaderLimit);
        int bits = (int)(header & BitCountMask);
        bool flag = (header & ScaledFlag) != 0;
        double[] components = new double[3];
        for (int index = 0; index < components.Length; index++)
        {
            components[index] = bits switch
            {
                > 0 => SignExtend(reader.ReadBits(bits), bits) / (flag ? scale : 1.0),
                _ when flag => BitConverter.UInt64BitsToDouble(reader.ReadBits(DoubleBits)),
                _ => BitConverter.UInt32BitsToSingle((uint)reader.ReadBits(SingleBits)),
            };
        }

        return components.All(double.IsFinite)
            ? new Vector3D(components[0], components[1], components[2])
            : throw new InvalidPacketException("Non-finite quantized vector");
    }

    // Rounds half away from zero, as the engine does.
    private static long Round(double value) => (long)(value + (Math.Sign(value) * 0.5));

    // Bits for a two's-complement value, at least one for the sign.
    private static int BitsNeeded(long value) => 65 - BitOperations.LeadingZeroCount((ulong)(value ^ (value >> 63)));

    private static long SignExtend(ulong value, int bits)
    {
        long sign = 1L << (bits - 1);
        return ((long)value ^ sign) - sign;
    }
}
