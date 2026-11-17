using System.Buffers.Binary;

namespace Ares.GameDataExtract.Animations;

/// <summary>Bounds-checked little-endian reads and searches in one export's serialized bytes.</summary>
internal static class ExportBytes
{
    /// <summary>The int32 at <paramref name="at"/>.</summary>
    /// <exception cref="InvalidDataException">The value is outside the export.</exception>
    public static int Int32(ReadOnlySpan<byte> data, int at) => BinaryPrimitives.ReadInt32LittleEndian(Slice(data, at, sizeof(int)));

    /// <summary>The uint16 at <paramref name="at"/>.</summary>
    /// <exception cref="InvalidDataException">The value is outside the export.</exception>
    public static ushort UInt16(ReadOnlySpan<byte> data, int at) => BinaryPrimitives.ReadUInt16LittleEndian(Slice(data, at, sizeof(ushort)));

    /// <summary>The byte at <paramref name="at"/>.</summary>
    /// <exception cref="InvalidDataException">The value is outside the export.</exception>
    public static byte Byte(ReadOnlySpan<byte> data, int at) => Slice(data, at, 1)[0];

    /// <summary>The uint32 at <paramref name="at"/>.</summary>
    /// <exception cref="InvalidDataException">The value is outside the export.</exception>
    public static uint UInt32(ReadOnlySpan<byte> data, int at) => BinaryPrimitives.ReadUInt32LittleEndian(Slice(data, at, sizeof(uint)));

    /// <summary>The float at <paramref name="at"/>.</summary>
    /// <exception cref="InvalidDataException">The value is outside the export.</exception>
    public static float Single(ReadOnlySpan<byte> data, int at) => BinaryPrimitives.ReadSingleLittleEndian(Slice(data, at, sizeof(float)));

    /// <summary>Little-endian bytes of <paramref name="value"/>.</summary>
    public static byte[] Bytes(float value)
    {
        byte[] bytes = new byte[sizeof(float)];
        BinaryPrimitives.WriteSingleLittleEndian(bytes, value);
        return bytes;
    }

    /// <summary>Little-endian bytes of <paramref name="value"/>.</summary>
    public static byte[] Bytes(int value)
    {
        byte[] bytes = new byte[sizeof(int)];
        BinaryPrimitives.WriteInt32LittleEndian(bytes, value);
        return bytes;
    }

    /// <summary>
    /// Offset of the first occurrence of <paramref name="value"/> that starts at or after <paramref name="start"/> and
    /// ends at or before <paramref name="end"/>; −1 when there is none.
    /// </summary>
    public static int Find(ReadOnlySpan<byte> data, ReadOnlySpan<byte> value, int start, int end)
    {
        if (start < 0 || start > data.Length)
        {
            return -1;
        }

        int limit = Math.Min(end, data.Length);
        if (limit - start < value.Length)
        {
            return -1;
        }

        int found = data[start..limit].IndexOf(value);
        return found < 0 ? -1 : start + found;
    }

    /// <summary>Offset of the first occurrence of <paramref name="value"/> at or after <paramref name="start"/>; −1 when there is none.</summary>
    public static int Find(ReadOnlySpan<byte> data, ReadOnlySpan<byte> value, int start = 0) => Find(data, value, start, data.Length);

    private static ReadOnlySpan<byte> Slice(ReadOnlySpan<byte> data, int at, int count)
    {
        return at >= 0 && at <= data.Length - count
            ? data.Slice(at, count)
            : throw new InvalidDataException($"A read of {count} bytes at offset {at} is outside the {data.Length}-byte export");
    }
}
