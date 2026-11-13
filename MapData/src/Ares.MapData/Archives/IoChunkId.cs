using System.Buffers.Binary;

namespace Ares.MapData.Archives;

/// <summary>Kinds of chunks an I/O store container holds.</summary>
internal enum IoChunkType : byte
{
    ExportBundleData = 1,
    BulkData = 2,
    ScriptObjects = 5,
}

/// <summary>
/// Identity of a chunk: a 64-bit id (for package data, the package id), a 16-bit index and the chunk type.
/// Stored as 12 bytes: the id little-endian, the index big-endian, one padding byte and the type.
/// </summary>
internal readonly record struct IoChunkId(ulong Id, ushort Index, IoChunkType Type)
{
    /// <summary>Bytes of a stored chunk id.</summary>
    public const int Size = 12;

    /// <summary>Reads a stored chunk id.</summary>
    public static IoChunkId Read(ReadOnlySpan<byte> data)
    {
        return new IoChunkId(
            BinaryPrimitives.ReadUInt64LittleEndian(data),
            BinaryPrimitives.ReadUInt16BigEndian(data[8..]),
            (IoChunkType)data[11]);
    }

    /// <summary>Writes the stored form into <paramref name="destination"/>.</summary>
    public void Write(Span<byte> destination)
    {
        BinaryPrimitives.WriteUInt64LittleEndian(destination, Id);
        BinaryPrimitives.WriteUInt16BigEndian(destination[8..], Index);
        destination[10] = 0;
        destination[11] = (byte)Type;
    }
}
