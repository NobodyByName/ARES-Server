using Ares.Structs.Core.Geometry;
using Ares.Structs.Core.Serialization;
using Ares.Structs.Net.Replication;

namespace Ares.Structs.Tests.Net;

public sealed class QuantizedVectorTests
{
    [Fact]
    public void A_unit_vector_is_sent_as_eight_bit_hundredths()
    {
        // Header 72 (8 bits per component, scaled) in 7 bits, then 100 three times in 8 bits each.
        BitWriter writer = new();
        QuantizedVector.Write(writer, Vector3D.One, QuantizedVector.Hundredths);
        Assert.Equal(("48323232", 31L), (Convert.ToHexStringLower(writer.ToArray()), writer.Length));
    }

    [Fact]
    public void A_zero_vector_takes_one_bit_per_component()
    {
        BitWriter writer = new();
        QuantizedVector.Write(writer, default, QuantizedVector.Hundredths);
        Assert.Equal(10, writer.Length);
        Assert.Equal(default, QuantizedVector.Read(new BitReader(writer.ToArray(), writer.Length), QuantizedVector.Hundredths));
    }

    [Theory]
    [InlineData(-12.345, 0.0, 987654.321, -12.35, 0.0, 987654.32)]
    [InlineData(0.004, -0.005, 0.015, 0.0, -0.01, 0.02)]
    [InlineData(1e17, -2.5, 3.0, 1e17, -2.5, 3.0)]
    public void Values_round_to_hundredths_or_go_whole_when_too_large(double x, double y, double z, double ex, double ey, double ez)
    {
        BitWriter writer = new();
        QuantizedVector.Write(writer, new Vector3D(x, y, z), QuantizedVector.Hundredths);
        BitReader reader = new(writer.ToArray(), writer.Length);
        Vector3D read = QuantizedVector.Read(reader, QuantizedVector.Hundredths);
        Assert.Equal(0, reader.Remaining);
        Assert.Equal(ex, read.X, 9);
        Assert.Equal(ey, read.Y, 9);
        Assert.Equal(ez, read.Z, 9);
    }

    [Fact]
    public void Unscaled_single_precision_vectors_are_read()
    {
        BitWriter writer = new();
        writer.WriteBounded(0, 128);
        foreach (float component in (float[])[1.5f, -2.25f, 8.0f])
        {
            writer.WriteBits(BitConverter.SingleToUInt32Bits(component), 32);
        }

        Assert.Equal(new Vector3D(1.5, -2.25, 8.0), QuantizedVector.Read(new BitReader(writer.ToArray(), writer.Length), QuantizedVector.Hundredths));
    }

    [Fact]
    public void Non_finite_components_are_rejected()
    {
        Assert.Throws<InvalidPacketException>(() => QuantizedVector.Write(new BitWriter(), new Vector3D(double.NaN, 0, 0), QuantizedVector.Hundredths));
    }
}
