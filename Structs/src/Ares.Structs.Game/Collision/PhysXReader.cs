using System.Buffers.Binary;
using System.Numerics;
using System.Text;

namespace Ares.Structs.Game.Collision;

/// <summary>
/// Bounds-checked little-endian reader for PhysX cooked streams. Every read past the end, and every count whose
/// elements cannot fit in the remaining bytes, throws <see cref="InvalidDataException"/>.
/// </summary>
internal ref struct PhysXReader(ReadOnlySpan<byte> data)
{
    private readonly ReadOnlySpan<byte> _data = data;

    public int Position { get; private set; }

    public readonly int Remaining => _data.Length - Position;

    public byte ReadByte() => ReadBytes(1)[0];

    public ushort ReadUInt16() => BinaryPrimitives.ReadUInt16LittleEndian(ReadBytes(2));

    public short ReadInt16() => BinaryPrimitives.ReadInt16LittleEndian(ReadBytes(2));

    public uint ReadUInt32() => BinaryPrimitives.ReadUInt32LittleEndian(ReadBytes(4));

    public int ReadInt32() => BinaryPrimitives.ReadInt32LittleEndian(ReadBytes(4));

    public float ReadSingle() => BinaryPrimitives.ReadSingleLittleEndian(ReadBytes(4));

    public Vector3 ReadVector3()
    {
        float x = ReadSingle();
        float y = ReadSingle();
        float z = ReadSingle();
        return new Vector3(x, y, z);
    }

    public BoundingBox ReadBounds()
    {
        Vector3 minimum = ReadVector3();
        Vector3 maximum = ReadVector3();
        return new BoundingBox(minimum, maximum);
    }

    public ReadOnlySpan<byte> ReadBytes(int count)
    {
        if (count < 0 || count > Remaining)
        {
            throw new InvalidDataException($"Cooked data truncated at byte {Position}.");
        }

        ReadOnlySpan<byte> result = _data.Slice(Position, count);
        Position += count;
        return result;
    }

    public void Skip(long count) => ReadBytes(Fit(count));

    /// <summary>Reads an unsigned 32-bit element count and checks that the elements fit in the remaining bytes.</summary>
    public int ReadCount(int elementSize) => CheckCount(ReadUInt32(), elementSize);

    /// <summary>Reads a signed 32-bit element count, rejecting negative values, and checks that the elements fit.</summary>
    public int ReadSignedCount(int elementSize)
    {
        int count = ReadInt32();
        return count >= 0
            ? CheckCount((uint)count, elementSize)
            : throw new InvalidDataException($"Negative count {count} at byte {Position - 4}.");
    }

    public readonly int CheckCount(uint count, int elementSize) => count * elementSize <= Remaining
        ? (int)count
        : throw new InvalidDataException($"Count {count} at byte {Position} exceeds the remaining {Remaining} bytes.");

    /// <summary>
    /// Reads a PhysX chunk header: a three-letter stream identifier and an endianness byte (1 = little-endian), the
    /// four-letter chunk name, then the version.
    /// </summary>
    /// <returns>The chunk version.</returns>
    public uint ReadHeader(ReadOnlySpan<byte> stream, ReadOnlySpan<byte> chunk)
    {
        int at = Position;
        ReadOnlySpan<byte> header = ReadBytes(8);
        if (!header[..3].SequenceEqual(stream) || !header[4..].SequenceEqual(chunk))
        {
            throw new InvalidDataException($"Expected chunk {Encoding.ASCII.GetString(chunk)} at byte {at}.");
        }

        return header[3] == 1
            ? ReadUInt32()
            : throw new InvalidDataException($"Chunk {Encoding.ASCII.GetString(chunk)} is not little-endian.");
    }

    /// <summary>Reads a chunk header and requires one version.</summary>
    public void ReadHeader(ReadOnlySpan<byte> stream, ReadOnlySpan<byte> chunk, uint version)
    {
        uint found = ReadHeader(stream, chunk);
        if (found != version)
        {
            throw new InvalidDataException($"Unsupported {Encoding.ASCII.GetString(chunk)} version {found}; expected {version}.");
        }
    }

    /// <summary>Skips an index array stored with the narrowest width that holds <paramref name="maximumIndex"/>.</summary>
    public void SkipIndices(uint maximumIndex, int count) => Skip((long)count * IndexWidth(maximumIndex));

    public readonly void EnsureEnd()
    {
        if (Remaining != 0)
        {
            throw new InvalidDataException($"{Remaining} bytes left after the cooked data.");
        }
    }

    /// <summary>Width in bytes PhysX uses for an index array whose largest value is <paramref name="maximumIndex"/>.</summary>
    public static int IndexWidth(uint maximumIndex) => maximumIndex <= byte.MaxValue ? 1 : maximumIndex <= ushort.MaxValue ? 2 : 4;

    private readonly int Fit(long count)
    {
        if (count < 0 || count > Remaining)
        {
            throw new InvalidDataException($"Cooked data truncated at byte {Position}.");
        }

        return (int)count;
    }
}
