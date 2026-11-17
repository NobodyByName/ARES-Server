using System.Text;
using Ares.GameDataExtract.Packages;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Game.World;

namespace Ares.GameDataExtract.World;

/// <summary>
/// Reads the stored values of a cooked export one after the other, from the first value after its unversioned header:
/// the caller knows each listed member's kind and reads it in chain order. Reads past the export's end fail.
/// </summary>
internal sealed class PlacedActorValues
{
    private const int NameSize = 8;
    private const int ReferenceSize = 4;
    private const int VectorSize = 24;
    private const int QuaternionSize = 32;

    private readonly CookedPackage _package;
    private readonly int _end;

    /// <summary>A reader of <paramref name="export"/>'s values, starting after <paramref name="header"/>.</summary>
    public PlacedActorValues(CookedPackage package, ZenExport export, UnversionedHeader header)
    {
        _package = package;
        _end = CookedPackage.EndOf(export);
        Offset = header.ValuesOffset;
    }

    /// <summary>Offset of the next value.</summary>
    public int Offset { get; private set; }

    /// <summary>An object reference (0 none, positive an export, negative an import).</summary>
    public int Reference() => _package.ReadInt32(Advance(ReferenceSize));

    /// <summary>A one-byte value (a bool or a byte enum).</summary>
    public byte Byte() => _package.ReadByte(Advance(1));

    /// <summary>A 32-bit integer.</summary>
    public int Int32() => _package.ReadInt32(Advance(sizeof(int)));

    /// <summary>A name, spelled as stored.</summary>
    public string Name() => _package.ReadName(Advance(NameSize));

    /// <summary>The raw words of a name: its name map index and number.</summary>
    public (uint Index, uint Number) NameWords()
    {
        int at = Advance(NameSize);
        return (_package.ReadUInt32(at), _package.ReadUInt32(at + sizeof(uint)));
    }

    /// <summary>Moves past <paramref name="size"/> bytes.</summary>
    public void Skip(int size) => Advance(size);

    /// <summary>Bytes left before the export's end.</summary>
    public int Remaining => _end - Offset;

    /// <summary>A double, with a zero given as positive zero.</summary>
    public double Double() => _package.ReadDouble(Advance(sizeof(double))) + 0.0;

    /// <summary>A vector of three doubles, each zero given as positive zero.</summary>
    public Vector3D Vector()
    {
        Vector3D value = _package.ReadVector(Advance(VectorSize));
        return new Vector3D(value.X + 0.0, value.Y + 0.0, value.Z + 0.0);
    }

    /// <summary>A quaternion of four doubles (X, Y, Z, W).</summary>
    public Quaternion Quaternion()
    {
        int at = Advance(QuaternionSize);
        return new Quaternion(_package.ReadDouble(at), _package.ReadDouble(at + 8), _package.ReadDouble(at + 16), _package.ReadDouble(at + 24));
    }

    /// <summary>An array count, checked to be at most <paramref name="max"/>.</summary>
    /// <exception cref="InvalidDataException">The count is negative or above <paramref name="max"/>.</exception>
    public int Count(int max)
    {
        int count = _package.ReadInt32(Advance(sizeof(int)));
        return count >= 0 && count <= max ? count : throw new InvalidDataException($"Implausible array size {count} at offset {Offset - sizeof(int)}");
    }

    /// <summary>An array of object references of at most <paramref name="max"/> elements.</summary>
    public int[] References(int max)
    {
        int count = Count(max);
        return [.. Enumerable.Range(0, count).Select(_ => Reference())];
    }

    /// <summary>A gameplay tag container: a count of at most <paramref name="max"/> and that many names, spelled as stored.</summary>
    public string[] GameplayTags(int max)
    {
        int count = Count(max);
        return [.. Enumerable.Range(0, count).Select(_ => _package.ReadName(Advance(NameSize)))];
    }

    /// <summary>
    /// A string: an int32 length counting the terminating zero, then that many one-byte characters (a negative length
    /// counts two-byte characters).
    /// </summary>
    /// <exception cref="InvalidDataException">The string is empty, unterminated or runs past the export.</exception>
    public string String()
    {
        int length = _package.ReadInt32(Advance(sizeof(int)));
        if (length is 0 or int.MinValue)
        {
            throw new InvalidDataException($"Unsupported string length {length} at offset {Offset - sizeof(int)}");
        }

        bool wide = length < 0;
        int size = wide ? -length * sizeof(char) : length;
        int at = Advance(size);
        ReadOnlySpan<byte> bytes = _package.Data.AsSpan(at, size);
        int terminator = wide ? sizeof(char) : 1;
        if (bytes[^terminator..].ContainsAnyExcept((byte)0))
        {
            throw new InvalidDataException($"The string at offset {at} is not terminated");
        }

        return wide ? Encoding.Unicode.GetString(bytes[..^terminator]) : Encoding.Latin1.GetString(bytes[..^terminator]);
    }

    /// <summary>
    /// Reads the native data that closes a placed actor (see <see cref="PlacedActorNativeData"/>) and returns the actor's
    /// label.
    /// </summary>
    /// <exception cref="InvalidDataException">The values do not end at the native data, or the native data differs.</exception>
    public string ActorTrailer()
    {
        if (Offset + (2 * sizeof(int)) > _end
            || _package.ReadInt32(Offset) != PlacedActorNativeData.FirstWord
            || _package.ReadInt32(Offset + sizeof(int)) != PlacedActorNativeData.SecondWord)
        {
            throw new InvalidDataException("The values do not end at the actor's native data");
        }

        Offset += 2 * sizeof(int);
        int length = _package.ReadInt32(Offset);
        if (length is < PlacedActorNativeData.MinLabelLength or > PlacedActorNativeData.MaxLabelLength)
        {
            throw new InvalidDataException($"The actor label has length {length}");
        }

        string label = String();
        return Offset + PlacedActorNativeData.TailSize == _end
            ? label
            : throw new InvalidDataException($"The actor's native data ends {_end - Offset} bytes before the export end, expected {PlacedActorNativeData.TailSize}");
    }

    /// <summary>Reads an unversioned struct header at the current offset and moves past it.</summary>
    public UnversionedHeader StructHeader(int memberCount)
    {
        UnversionedHeader header = UnversionedHeader.Read(_package.Data, Offset, memberCount);
        if (header.ValuesOffset > _end)
        {
            throw new InvalidDataException($"The struct header at offset {Offset} runs past the export");
        }

        Offset = header.ValuesOffset;
        return header;
    }

    private int Advance(int size)
    {
        int at = Offset;
        if (at + size > _end)
        {
            throw new InvalidDataException($"A value at offset {at} runs past the export");
        }

        Offset = at + size;
        return at;
    }
}
