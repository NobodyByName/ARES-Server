using System.Buffers.Binary;
using System.Text;

namespace Ares.MapData.Archives;

/// <summary>
/// Little-endian reader over a byte span. Every read past the end throws <see cref="InvalidDataException"/>,
/// so malformed archive data never reads outside its buffer.
/// </summary>
internal ref struct ByteReader(ReadOnlySpan<byte> data, int position = 0)
{
    private const int MaxStringLength = 65536;

    private readonly ReadOnlySpan<byte> _data = data;

    /// <summary>The offset of the next read.</summary>
    public int Position { get; private set; } = position;

    /// <summary>Bytes left after <see cref="Position"/>.</summary>
    public readonly int Remaining => _data.Length - Position;

    /// <summary>Reads <paramref name="count"/> bytes.</summary>
    public ReadOnlySpan<byte> ReadBytes(int count)
    {
        if (count < 0 || count > Remaining)
        {
            throw new InvalidDataException($"Read of {count} bytes at offset {Position} is outside the data ({_data.Length} bytes)");
        }

        ReadOnlySpan<byte> bytes = _data.Slice(Position, count);
        Position += count;
        return bytes;
    }

    /// <summary>Skips <paramref name="count"/> bytes.</summary>
    public void Skip(int count)
    {
        ReadBytes(count);
    }

    public byte ReadByte() => ReadBytes(1)[0];

    public int ReadInt32() => BinaryPrimitives.ReadInt32LittleEndian(ReadBytes(sizeof(int)));

    public uint ReadUInt32() => BinaryPrimitives.ReadUInt32LittleEndian(ReadBytes(sizeof(uint)));

    public ulong ReadUInt64() => BinaryPrimitives.ReadUInt64LittleEndian(ReadBytes(sizeof(ulong)));

    /// <summary>Reads a non-negative 32-bit count no larger than <paramref name="maximum"/>.</summary>
    public int ReadCount(int maximum)
    {
        int count = ReadInt32();
        return count >= 0 && count <= maximum
            ? count
            : throw new InvalidDataException($"Count {count} at offset {Position - sizeof(int)} is outside 0..{maximum}");
    }

    /// <summary>
    /// Reads a length-prefixed string: a positive length means UTF-8 bytes, a negative one UTF-16 code units.
    /// The length includes a terminating zero, which is dropped.
    /// </summary>
    public string ReadString()
    {
        int length = ReadInt32();
        if (length is > MaxStringLength or < -MaxStringLength)
        {
            throw new InvalidDataException($"String length {length} at offset {Position - sizeof(int)} is too large");
        }

        string text = length < 0
            ? Encoding.Unicode.GetString(ReadBytes(-length * sizeof(char)))
            : Encoding.UTF8.GetString(ReadBytes(length));
        return text.TrimEnd('\0');
    }
}
