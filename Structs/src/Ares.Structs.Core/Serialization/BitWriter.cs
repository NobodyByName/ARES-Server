using System.Text;

namespace Ares.Structs.Core.Serialization;

/// <summary>
/// Writes network data least-significant bit first; the counterpart of <see cref="BitReader"/>.
/// Values outside their field throw <see cref="InvalidPacketException"/>.
/// </summary>
public sealed class BitWriter
{
    private static readonly Encoding s_utf16 = new UnicodeEncoding(bigEndian: false, byteOrderMark: false, throwOnInvalidBytes: true);

    private byte[] _data = new byte[64];

    /// <summary>Bits written so far.</summary>
    public long Length { get; private set; }

    /// <summary>Writes the low <paramref name="count"/> bits (0 to 64) of <paramref name="value"/>, lowest first.</summary>
    /// <param name="value">The value; must fit in <paramref name="count"/> bits.</param>
    /// <param name="count">Number of bits.</param>
    public void WriteBits(ulong value, int count)
    {
        switch (count)
        {
            case < 0 or > 64:
                throw new ArgumentOutOfRangeException(nameof(count));
            case < 64 when value >> count != 0:
                throw new InvalidPacketException("Value outside its bit width");
        }

        Reserve(count);
        for (int index = 0; index < count; index++)
        {
            long at = Length + index;
            byte mask = (byte)(1 << (int)(at & 7));
            if (((value >> index) & 1) != 0)
            {
                _data[at >> 3] |= mask;
            }
            else
            {
                _data[at >> 3] &= (byte)~mask;
            }
        }

        Length += count;
    }

    /// <summary>Writes one bit.</summary>
    /// <param name="value">The bit.</param>
    public void WriteBit(bool value) => WriteBits(value ? 1UL : 0UL, 1);

    /// <summary>Writes 8 bits.</summary>
    /// <param name="value">The byte.</param>
    public void WriteByte(byte value) => WriteBits(value, 8);

    /// <summary>Writes bytes, which need not be byte-aligned.</summary>
    /// <param name="values">The bytes.</param>
    public void WriteBytes(ReadOnlySpan<byte> values)
    {
        foreach (byte value in values)
        {
            WriteByte(value);
        }
    }

    /// <summary>Writes the first <paramref name="count"/> bits of another bit sequence.</summary>
    /// <param name="data">Source bytes.</param>
    /// <param name="count">Number of bits.</param>
    public void WriteBits(byte[] data, long count)
    {
        BitReader source = new(data, count);
        while (source.Remaining > 0)
        {
            int chunk = (int)Math.Min(64, source.Remaining);
            WriteBits(source.ReadBits(chunk), chunk);
        }
    }

    /// <summary>Writes an integer in [0, <paramref name="maximum"/>); see <see cref="BitReader.ReadBounded"/>.</summary>
    /// <param name="value">The value.</param>
    /// <param name="maximum">Exclusive upper bound, 1 to 2^32.</param>
    public void WriteBounded(uint value, ulong maximum)
    {
        BoundedRange.Check(maximum);
        if (value >= maximum)
        {
            throw new InvalidPacketException("Value outside its range");
        }

        ulong written = 0;
        for (ulong place = 1; written + place < maximum; place <<= 1)
        {
            bool set = (value & place) != 0;
            WriteBit(set);
            if (set)
            {
                written |= place;
            }
        }
    }

    /// <summary>Writes a packed unsigned integer; see <see cref="BitReader.ReadPacked"/>.</summary>
    /// <param name="value">The value.</param>
    public void WritePacked(uint value)
    {
        while (true)
        {
            uint rest = value >> 7;
            WriteByte((byte)(((value & 0x7F) << 1) | (rest != 0 ? 1U : 0U)));
            if (rest == 0)
            {
                return;
            }

            value = rest;
        }
    }

    /// <summary>Writes a string: ASCII when every character is ASCII, otherwise UTF-16; see <see cref="BitReader.ReadString"/>.</summary>
    /// <param name="text">The string; must not contain a terminator.</param>
    /// <param name="limit">Largest accepted length, terminator included.</param>
    public void WriteString(string text, int limit = 4096)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (text.Contains('\0', StringComparison.Ordinal))
        {
            throw new InvalidPacketException("Embedded string terminator");
        }

        if (text.Length == 0)
        {
            WriteBits(0, 32);
            return;
        }

        bool wide = !Ascii.IsValid(text);
        byte[] data;
        try
        {
            data = wide ? s_utf16.GetBytes(text) : Encoding.ASCII.GetBytes(text);
        }
        catch (EncoderFallbackException error)
        {
            throw new InvalidPacketException("Malformed string", error);
        }

        int units = (data.Length / (wide ? 2 : 1)) + 1;
        if (units > limit)
        {
            throw new InvalidPacketException("String length above its limit");
        }

        WriteBits((uint)(wide ? -units : units), 32);
        WriteBytes(data);
        WriteBytes(wide ? "\0\0"u8 : "\0"u8);
    }

    /// <summary>The written bits as bytes; unused bits of the last byte are zero.</summary>
    /// <returns>A copy of the data.</returns>
    public byte[] ToArray() => _data[..(int)((Length + 7) >> 3)];

    private void Reserve(int count)
    {
        long bytes = (Length + count + 7) >> 3;
        if (bytes > _data.Length)
        {
            Array.Resize(ref _data, (int)Math.Max(bytes, _data.Length * 2L));
        }
    }
}

/// <summary>Range checks shared by the bounded-integer reader and writer.</summary>
internal static class BoundedRange
{
    /// <summary>Largest exclusive maximum (2^32).</summary>
    public const ulong Limit = 1UL << 32;

    public static void Check(ulong maximum)
    {
        if (maximum is 0 or > Limit)
        {
            throw new ArgumentOutOfRangeException(nameof(maximum), maximum, "The maximum must be 1 to 2^32.");
        }
    }
}
