using Ares.Structs.Core.Serialization;

namespace Ares.Structs.Tests.Core;

public sealed class BitStreamTests
{
    [Theory]
    [MemberData(nameof(BitStreamVectors.Bounded), MemberType = typeof(BitStreamVectors))]
    public void Bounded_integers_match_the_reference(ulong maximum, uint value, string bits)
    {
        BitWriter writer = new();
        writer.WriteBounded(value, maximum);
        Assert.Equal(bits, BitString(writer));
        Assert.Equal(value, Reader(bits).ReadBounded(maximum));
    }

    [Theory]
    [MemberData(nameof(BitStreamVectors.Packed), MemberType = typeof(BitStreamVectors))]
    public void Packed_integers_match_the_reference(uint value, string bits)
    {
        BitWriter writer = new();
        writer.WritePacked(value);
        Assert.Equal(bits, BitString(writer));
        Assert.Equal(value, Reader(bits).ReadPacked());
    }

    [Theory]
    [MemberData(nameof(BitStreamVectors.Strings), MemberType = typeof(BitStreamVectors))]
    public void Strings_match_the_reference(string text, string bits)
    {
        BitWriter writer = new();
        writer.WriteString(text);
        Assert.Equal(bits, BitString(writer));
        BitReader reader = Reader(bits);
        Assert.Equal(text, reader.ReadString());
        Assert.Equal(0, reader.Remaining);
    }

    [Theory]
    [MemberData(nameof(BitStreamVectors.Malformed), MemberType = typeof(BitStreamVectors))]
    public void Malformed_input_is_rejected(string kind, ulong maximum, string bits, string reason)
    {
        BitReader reader = Reader(bits);
        Action read = kind switch
        {
            "bounded" => () => reader.ReadBounded(maximum),
            "packed" => () => reader.ReadPacked(),
            _ => () => reader.ReadString(),
        };
        Assert.Throws<InvalidPacketException>(read);
        Assert.False(string.IsNullOrEmpty(reason));
    }

    [Fact]
    public void Unaligned_fields_match_the_reference_and_read_back()
    {
        BitWriter writer = new();
        foreach (MixedField field in BitStreamVectors.MixedFields)
        {
            switch (field.Kind)
            {
                case MixedKind.Bits:
                case MixedKind.Bit:
                    writer.WriteBits(field.Value, field.Width);
                    break;
                case MixedKind.Packed:
                    writer.WritePacked((uint)field.Value);
                    break;
                case MixedKind.Bounded:
                    writer.WriteBounded((uint)field.Value, field.Maximum);
                    break;
                default:
                    writer.WriteString(field.Text!);
                    break;
            }
        }

        Assert.Equal(BitStreamVectors.MixedLength, writer.Length);
        Assert.Equal(BitStreamVectors.MixedHex, Convert.ToHexStringLower(writer.ToArray()));

        BitReader reader = new(writer.ToArray(), writer.Length);
        foreach (MixedField field in BitStreamVectors.MixedFields)
        {
            object actual = field.Kind switch
            {
                MixedKind.Bits or MixedKind.Bit => reader.ReadBits(field.Width),
                MixedKind.Packed => (ulong)reader.ReadPacked(),
                MixedKind.Bounded => (ulong)reader.ReadBounded(field.Maximum),
                _ => reader.ReadString(),
            };
            Assert.Equal(field.Kind == MixedKind.String ? field.Text : field.Value, actual);
        }

        Assert.Equal(0, reader.Remaining);
    }

    [Fact]
    public void Out_of_range_values_are_rejected()
    {
        BitWriter writer = new();
        Assert.Throws<InvalidPacketException>(() => writer.WriteBits(8, 3));
        Assert.Throws<InvalidPacketException>(() => writer.WriteBounded(5, 5));
        Assert.Throws<InvalidPacketException>(() => writer.WriteString("a\0b"));
        Assert.Throws<InvalidPacketException>(() => writer.WriteString(new string('a', 4096)));
        Assert.Throws<ArgumentOutOfRangeException>(() => writer.WriteBounded(0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => writer.WriteBounded(0, (1UL << 32) + 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => writer.WriteBits(0, 65));
        Assert.Equal(0, writer.Length);

        BitReader reader = new([0xFF], 7);
        Assert.Equal(0x7FUL, reader.ReadBits(7));
        Assert.Throws<InvalidPacketException>(() => reader.ReadBit());
        Assert.Throws<InvalidPacketException>(() => new BitReader([0], 9));
    }

    [Fact]
    public void Bits_copy_between_streams()
    {
        BitWriter source = new();
        source.WriteBits(0x15, 5);
        source.WriteString("copy");
        BitWriter target = new();
        target.WriteBit(true);
        target.WriteBits(source.ToArray(), source.Length);
        BitReader reader = new(target.ToArray(), target.Length);
        Assert.True(reader.ReadBit());
        Assert.Equal(0x15UL, reader.ReadBits(5));
        Assert.Equal("copy", reader.ReadString());
    }

    private static string BitString(BitWriter writer)
    {
        BitReader reader = new(writer.ToArray(), writer.Length);
        char[] bits = new char[writer.Length];
        for (int index = 0; index < bits.Length; index++)
        {
            bits[index] = reader.ReadBit() ? '1' : '0';
        }

        return new string(bits);
    }

    private static BitReader Reader(string bits)
    {
        BitWriter writer = new();
        foreach (char bit in bits)
        {
            writer.WriteBit(bit == '1');
        }

        return new BitReader(writer.ToArray(), writer.Length);
    }
}
