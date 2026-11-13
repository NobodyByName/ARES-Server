using System.Buffers.Binary;
using System.Security.Cryptography;
using Ares.Common.Archives;

namespace Ares.MapData.Archives;

/// <summary>
/// One read-only pak file: its footer, its (optionally encrypted) index and its directory index. Entries are found by
/// their path relative to the game's install folder and read when asked for; only stored (uncompressed) entries are
/// supported. Never writes to the game's files.
/// </summary>
internal sealed class PakFile : IDisposable
{
    /// <summary>Footer magic.</summary>
    public const uint Magic = 0x5A6F12E1;

    /// <summary>The footer is found within this many bytes of the end of the file.</summary>
    private const int FooterSearchSize = 1024;

    /// <summary>Bytes of the index hash in the footer and in each secondary index reference.</summary>
    private const int HashSize = 20;

    /// <summary>Bytes of the record stored before an uncompressed entry's data.</summary>
    public const int StoredEntryHeaderSize = 53;

    /// <summary>
    /// Mount point prefix of files under the game's install folder; paths are listed and looked up without it.
    /// </summary>
    public const string InstallMountPrefix = "../../../";

    /// <summary>Directory index location of a deleted entry.</summary>
    private const int DeletedLocation = int.MinValue;

    private const int BlockSizeBits = 0x3F;
    private const int CompressionMethodShift = 23;
    private const int CompressionMethodBits = 0x3F;
    private const uint EncryptedFlag = 1u << 22;
    private const uint OffsetFits32Flag = 1u << 31;
    private const uint SizeFits32Flag = 1u << 30;
    private const int MaxIndexSize = 256 << 20;
    private const int MaxFileCount = 1 << 22;

    private readonly TheiaFile _data;
    private readonly byte[] _pakKey;
    private readonly byte[] _encodedEntries;
    private readonly Dictionary<string, int> _files;

    private PakFile(TheiaFile data, byte[] pakKey, byte[] encodedEntries, Dictionary<string, int> files)
    {
        _data = data;
        _pakKey = pakKey;
        _encodedEntries = encodedEntries;
        _files = files;
    }

    /// <summary>Paths of the entries, relative to the game's install folder when mounted under it.</summary>
    public IReadOnlyCollection<string> Paths => _files.Keys;

    /// <summary>Opens the pak file at <paramref name="path"/> and reads its index.</summary>
    /// <exception cref="InvalidDataException">The file, its metadata or its index is malformed, or the pak key is wrong.</exception>
    public static PakFile Open(string path, ArchiveKeys keys)
    {
        TheiaFile data = new(path, keys.ContainerKey);
        try
        {
            (byte[] encodedEntries, Dictionary<string, int> files) = ReadIndex(data, keys.PakKey);
            return new PakFile(data, keys.PakKey, encodedEntries, files);
        }
        catch
        {
            data.Dispose();
            throw;
        }
    }

    /// <summary>Whether the pak has an entry at <paramref name="path"/>.</summary>
    public bool Contains(string path) => _files.ContainsKey(path);

    /// <summary>Reads the entry at <paramref name="path"/>.</summary>
    /// <exception cref="FileNotFoundException">The pak has no such entry.</exception>
    /// <exception cref="InvalidDataException">The entry is malformed or compressed.</exception>
    public byte[] Read(string path)
    {
        if (!_files.TryGetValue(path, out int location))
        {
            throw new FileNotFoundException($"{path} is not in the pak", path);
        }

        if (location < 0)
        {
            throw new InvalidDataException($"{path} has an unencoded index entry, which is not supported");
        }

        ByteReader reader = new(_encodedEntries, location);
        uint bits = reader.ReadUInt32();
        if ((bits & BlockSizeBits) == BlockSizeBits)
        {
            reader.Skip(sizeof(uint));
        }

        long offset = (bits & OffsetFits32Flag) != 0 ? reader.ReadUInt32() : checked((long)reader.ReadUInt64());
        long size = (bits & SizeFits32Flag) != 0 ? reader.ReadUInt32() : checked((long)reader.ReadUInt64());
        if (((bits >> CompressionMethodShift) & CompressionMethodBits) != 0)
        {
            throw new InvalidDataException($"{path} is compressed, which is not supported");
        }

        bool encrypted = (bits & EncryptedFlag) != 0;
        long stored = encrypted ? (size + AesEcb.BlockSize - 1) & ~(long)(AesEcb.BlockSize - 1) : size;
        if (stored > int.MaxValue || offset + StoredEntryHeaderSize > _data.Length - stored)
        {
            throw new InvalidDataException($"{path} lies outside the pak");
        }

        byte[] raw = new byte[stored];
        _data.Read(offset + StoredEntryHeaderSize, raw);
        return encrypted ? AesEcb.Decrypt(raw, _pakKey)[..(int)size] : raw;
    }

    public void Dispose()
    {
        _data.Dispose();
    }

    private static (byte[] EncodedEntries, Dictionary<string, int> Files) ReadIndex(TheiaFile data, byte[] pakKey)
    {
        int tailSize = (int)Math.Min(FooterSearchSize, data.Length);
        byte[] tail = new byte[tailSize];
        data.Read(data.Length - tailSize, tail);
        Span<byte> magic = stackalloc byte[sizeof(uint)];
        BinaryPrimitives.WriteUInt32LittleEndian(magic, Magic);
        int at = tail.AsSpan().LastIndexOf(magic);
        if (at < 1)
        {
            throw new InvalidDataException("Pak footer not found");
        }

        ByteReader footer = new(tail, at + magic.Length);
        footer.Skip(sizeof(uint));
        long indexOffset = checked((long)footer.ReadUInt64());
        long indexSize = checked((long)footer.ReadUInt64());
        byte[] indexHash = [.. footer.ReadBytes(HashSize)];
        bool encryptedIndex = tail[at - 1] != 0;

        byte[] primary = ReadIndexPart(data, pakKey, encryptedIndex, indexOffset, indexSize, indexHash);
        ByteReader reader = new(primary);
        string mountPoint = reader.ReadString();
        int count = reader.ReadCount(MaxFileCount);
        reader.Skip(sizeof(ulong));
        if (reader.ReadUInt32() != 0)
        {
            reader.Skip((2 * sizeof(long)) + HashSize);
        }

        if (reader.ReadUInt32() == 0)
        {
            throw new InvalidDataException("Pak has no directory index");
        }

        long directoryOffset = checked((long)reader.ReadUInt64());
        long directorySize = checked((long)reader.ReadUInt64());
        byte[] directoryHash = [.. reader.ReadBytes(HashSize)];
        byte[] encodedEntries = [.. reader.ReadBytes(reader.ReadCount(MaxIndexSize))];

        string prefix = mountPoint.StartsWith(InstallMountPrefix, StringComparison.Ordinal) ? mountPoint[InstallMountPrefix.Length..] : mountPoint;
        byte[] directory = ReadIndexPart(data, pakKey, encryptedIndex, directoryOffset, directorySize, directoryHash);
        ByteReader directoryReader = new(directory);
        Dictionary<string, int> files = new(StringComparer.Ordinal);
        int directoryCount = directoryReader.ReadCount(MaxFileCount);
        for (int i = 0; i < directoryCount; i++)
        {
            string directoryName = directoryReader.ReadString();
            int fileCount = directoryReader.ReadCount(MaxFileCount);
            for (int j = 0; j < fileCount; j++)
            {
                string fileName = directoryReader.ReadString();
                int location = directoryReader.ReadInt32();
                if (location != DeletedLocation)
                {
                    files[prefix + directoryName + fileName] = location;
                }
            }
        }

        return files.Count == count
            ? (encodedEntries, files)
            : throw new InvalidDataException($"Pak directory index lists {files.Count} files, its index {count}");
    }

    private static byte[] ReadIndexPart(TheiaFile data, byte[] pakKey, bool encrypted, long offset, long size, byte[] hash)
    {
        if (size is < 0 or > MaxIndexSize || offset < 0 || offset > data.Length - size)
        {
            throw new InvalidDataException("Pak index lies outside the file");
        }

        byte[] raw = new byte[size];
        data.Read(offset, raw);
        byte[] plain = encrypted ? PakIndexCipher.Decrypt(raw, pakKey) : raw;
        return SHA1.HashData(plain).AsSpan().SequenceEqual(hash)
            ? plain
            : throw new InvalidDataException("Pak index hash mismatch; check the pak-key");
    }
}
