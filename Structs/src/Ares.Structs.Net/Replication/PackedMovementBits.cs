using Ares.Structs.Core.Serialization;

namespace Ares.Structs.Net.Replication;

/// <summary>
/// The character movement RPC bit container: a packed bit count, then exactly that many bits. The bits
/// themselves are the game's move data and are not interpreted here.
/// </summary>
public sealed class PackedMovementBits : IEquatable<PackedMovementBits>
{
    /// <summary>Largest number of bits.</summary>
    public const int MaxBits = 4096;

    private readonly byte[] _data;

    /// <summary>Creates a container; unused bits of the last byte must be zero.</summary>
    /// <param name="data">The bits.</param>
    /// <param name="bitCount">Number of bits.</param>
    public PackedMovementBits(byte[] data, int bitCount)
    {
        ArgumentNullException.ThrowIfNull(data);
        if (bitCount is < 0 or > MaxBits || data.Length != (bitCount + 7) / 8 || (bitCount % 8 != 0 && data[^1] >> (bitCount % 8) != 0))
        {
            throw new InvalidPacketException("Invalid movement bit container");
        }

        _data = [.. data];
        BitCount = bitCount;
    }

    /// <summary>The empty container.</summary>
    public static PackedMovementBits Empty { get; } = new([], 0);

    /// <summary>Number of bits.</summary>
    public int BitCount { get; }

    /// <summary>The bits.</summary>
    public ReadOnlySpan<byte> Data => _data;

    /// <summary>Writes the container.</summary>
    /// <param name="writer">Destination.</param>
    public void Write(BitWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WritePacked((uint)BitCount);
        writer.WriteBits(_data, BitCount);
    }

    /// <summary>Reads a container.</summary>
    /// <param name="reader">Source.</param>
    /// <returns>The container.</returns>
    public static PackedMovementBits Read(BitReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);
        uint count = reader.ReadPacked();
        return count <= MaxBits && count <= reader.Remaining
            ? new PackedMovementBits(reader.ReadBitArray(count), (int)count)
            : throw new InvalidPacketException("Oversized or truncated movement bits");
    }

    /// <inheritdoc />
    public bool Equals(PackedMovementBits? other) => other is not null && BitCount == other.BitCount && _data.AsSpan().SequenceEqual(other._data);

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as PackedMovementBits);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(BitCount, _data.Length);
}
