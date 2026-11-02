using System.Text;

namespace Ares.Structs.Core.Serialization;

/// <summary>
/// Reads network data least-significant bit first. Every read is bounds-checked and throws
/// <see cref="InvalidPacketException"/> on truncated or malformed data.
/// </summary>
public sealed class BitReader
{
    private static readonly Encoding s_ascii = Encoding.GetEncoding(
        "us-ascii", EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);

    private static readonly Encoding s_utf16 = new UnicodeEncoding(bigEndian: false, byteOrderMark: false, throwOnInvalidBytes: true);

    private readonly byte[] _data;

    /// <summary>Reads all bits of <paramref name="data"/>.</summary>
    /// <param name="data">The data.</param>
    public BitReader(byte[] data)
        : this(data, (data ?? throw new ArgumentNullException(nameof(data))).Length * 8L)
    {
    }

    /// <summary>Reads the first <paramref name="length"/> bits of <paramref name="data"/>.</summary>
    /// <param name="data">The data.</param>
    /// <param name="length">Number of valid bits.</param>
    public BitReader(byte[] data, long length)
    {
        ArgumentNullException.ThrowIfNull(data);
        if (length < 0 || length > data.Length * 8L)
        {
            throw new InvalidPacketException("Invalid bit length");
        }

        _data = data;
        Length = length;
    }

    /// <summary>Number of valid bits.</summary>
    public long Length { get; }

    /// <summary>Bits read so far.</summary>
    public long Position { get; private set; }

    /// <summary>Bits left to read.</summary>
    public long Remaining => Length - Position;

    /// <summary>Reads <paramref name="count"/> bits (0 to 64), first bit in the lowest place.</summary>
    /// <param name="count">Number of bits.</param>
    /// <returns>The value.</returns>
    public ulong ReadBits(int count)
    {
        if (count is < 0 or > 64)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }

        Require(count);
        ulong value = 0;
        for (int index = 0; index < count; index++)
        {
            long at = Position + index;
            value |= (ulong)((_data[at >> 3] >> (int)(at & 7)) & 1) << index;
        }

        Position += count;
        return value;
    }

    /// <summary>Reads one bit.</summary>
    /// <returns>The bit.</returns>
    public bool ReadBit() => ReadBits(1) != 0;

    /// <summary>Reads 8 bits.</summary>
    /// <returns>The byte.</returns>
    public byte ReadByte() => (byte)ReadBits(8);

    /// <summary>Reads <paramref name="count"/> bytes, which need not be byte-aligned.</summary>
    /// <param name="count">Number of bytes.</param>
    /// <returns>The bytes.</returns>
    public byte[] ReadBytes(int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        Require(count * 8L);
        byte[] result = new byte[count];
        for (int index = 0; index < count; index++)
        {
            result[index] = ReadByte();
        }

        return result;
    }

    /// <summary>Reads <paramref name="count"/> bits into bytes, first bit in the lowest place of the first byte.</summary>
    /// <param name="count">Number of bits.</param>
    /// <returns>The bits; unused bits of the last byte are zero.</returns>
    public byte[] ReadBitArray(long count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        Require(count);
        BitWriter result = new();
        while (result.Length < count)
        {
            int chunk = (int)Math.Min(64, count - result.Length);
            result.WriteBits(ReadBits(chunk), chunk);
        }

        return result.ToArray();
    }

    /// <summary>
    /// Reads an integer in [0, <paramref name="maximum"/>): its binary places from the lowest up, where a
    /// place is sent only if setting it can still give a value below the maximum. A power-of-two maximum
    /// takes a fixed width; other maxima take one bit less for some values.
    /// </summary>
    /// <param name="maximum">Exclusive upper bound, 1 to 2^32. A maximum of 1 reads nothing.</param>
    /// <returns>The value.</returns>
    public uint ReadBounded(ulong maximum)
    {
        BoundedRange.Check(maximum);
        ulong value = 0;
        for (ulong place = 1; value + place < maximum; place <<= 1)
        {
            if (ReadBit())
            {
                value |= place;
            }
        }

        return (uint)value;
    }

    /// <summary>Reads a packed unsigned integer: 7 value bits per byte, lowest first, low bit set when more follow.</summary>
    /// <returns>The value.</returns>
    public uint ReadPacked()
    {
        uint value = 0;
        for (int index = 0; index < 5; index++)
        {
            byte unit = ReadByte();
            if (index == 4 && unit > 30)
            {
                throw new InvalidPacketException("Packed integer overflow");
            }

            value |= (uint)(unit >> 1) << (index * 7);
            if ((unit & 1) == 0)
            {
                return value;
            }
        }

        throw new InvalidPacketException("Unterminated packed integer");
    }

    /// <summary>
    /// Reads a string: a signed 32-bit length that counts the terminator (negative for UTF-16 code units,
    /// positive for ASCII bytes, zero for empty), then the characters and the terminator.
    /// </summary>
    /// <param name="limit">Largest accepted length, terminator included.</param>
    /// <returns>The string.</returns>
    public string ReadString(int limit = 4096)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(limit);
        int length = (int)(uint)ReadBits(32);
        if (length < -limit || length > limit)
        {
            throw new InvalidPacketException("String length above its limit");
        }

        if (length == 0)
        {
            return "";
        }

        int unit = length < 0 ? 2 : 1;
        int count = Math.Abs(length) * unit;
        Require(count * 8L);
        byte[] raw = ReadBytes(count);
        if (raw.AsSpan(count - unit).ContainsAnyExcept((byte)0))
        {
            throw new InvalidPacketException("Unterminated string");
        }

        string text;
        try
        {
            text = (unit == 2 ? s_utf16 : s_ascii).GetString(raw, 0, count - unit);
        }
        catch (DecoderFallbackException error)
        {
            throw new InvalidPacketException("Malformed string", error);
        }
        catch (ArgumentException error)
        {
            throw new InvalidPacketException("Malformed string", error);
        }

        return !text.Contains('\0', StringComparison.Ordinal)
            ? text
            : throw new InvalidPacketException("Embedded string terminator");
    }

    private void Require(long bits)
    {
        if (bits > Remaining)
        {
            throw new InvalidPacketException("Truncated data");
        }
    }
}
