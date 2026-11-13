using System.Buffers.Binary;

namespace Ares.MapData.Archives;

/// <summary>Container flags in the table of contents header.</summary>
[Flags]
internal enum IoContainerFlags : byte
{
    None = 0,
    Encrypted = 2,
    Signed = 4,
    Indexed = 8,
}

/// <summary>Where a chunk's bytes are in the container's uncompressed address space.</summary>
internal readonly record struct IoOffsetAndLength(long Offset, long Length);

/// <summary>
/// One storage block of a container's data file: where it starts, its stored and uncompressed sizes and its
/// compression method (0 means stored as is).
/// </summary>
internal readonly record struct IoCompressionBlock(long Offset, int CompressedSize, int UncompressedSize, byte Method);

/// <summary>
/// The table of contents of an I/O store container (<c>.utoc</c>): chunk ids, chunk locations, storage blocks,
/// block hashes and the directory index of file paths. Everything after the fixed header is AES-256 (ECB) encrypted
/// with the table key.
/// </summary>
internal sealed class IoStoreToc
{
    /// <summary>Magic at the start of the header.</summary>
    public static ReadOnlySpan<byte> Magic => "-==--==--==--==-"u8;

    /// <summary>The only supported table version.</summary>
    public const byte SupportedVersion = 8;

    /// <summary>Size of the fixed, unencrypted header.</summary>
    public const int HeaderSize = 144;

    /// <summary>Size of one stored storage block entry.</summary>
    public const int BlockEntrySize = 12;

    /// <summary>Size of one stored chunk location (40-bit big-endian offset and length).</summary>
    private const int OffsetAndLengthSize = 10;

    /// <summary>Size of one stored chunk meta entry (hash, flags, padding).</summary>
    public const int ChunkMetaSize = 24;

    /// <summary>Size of one block hash (SHA-1).</summary>
    private const int BlockHashSize = 20;

    /// <summary>Directory and file index terminator.</summary>
    public const uint NoEntry = uint.MaxValue;

    /// <summary>Header field offsets.</summary>
    public static class Field
    {
        public const int Version = 16;
        public const int TocHeaderSize = 20;
        public const int EntryCount = 24;
        public const int CompressionBlockCount = 28;
        public const int CompressionBlockEntrySize = 32;
        public const int CompressionMethodCount = 36;
        public const int CompressionMethodNameLength = 40;
        public const int CompressionBlockSize = 44;
        public const int DirectoryIndexSize = 48;
        public const int PartitionCount = 52;
        public const int ContainerFlags = 80;
        public const int PerfectHashSeedCount = 84;
        public const int ChunksWithoutPerfectHashCount = 96;
    }

    private const int MaxEntries = 2_000_000;
    private const int MaxBlocks = 10_000_000;
    private const int MaxMethods = 32;
    private const int MaxMethodNameLength = 256;
    private const int MaxSignatureSize = 4096;
    private const int MaxDirectoryDepth = 64;
    private const int MaxBlockSize = 1 << 24;

    private IoStoreToc(IoContainerFlags flags, int compressionBlockSize, IoChunkId[] chunkIds, IoOffsetAndLength[] chunks,
        IoCompressionBlock[] blocks, byte[][] blockHashes, IReadOnlyDictionary<string, int> files)
    {
        Flags = flags;
        CompressionBlockSize = compressionBlockSize;
        ChunkIds = chunkIds;
        Chunks = chunks;
        Blocks = blocks;
        BlockHashes = blockHashes;
        Files = files;
    }

    public IoContainerFlags Flags { get; }

    /// <summary>Uncompressed bytes per storage block.</summary>
    public int CompressionBlockSize { get; }

    /// <summary>Chunk ids, one per table entry.</summary>
    public IReadOnlyList<IoChunkId> ChunkIds { get; }

    /// <summary>Chunk locations, one per table entry.</summary>
    public IReadOnlyList<IoOffsetAndLength> Chunks { get; }

    public IReadOnlyList<IoCompressionBlock> Blocks { get; }

    /// <summary>SHA-1 of each stored block (padded to 16 bytes), or empty when the container is not signed.</summary>
    public IReadOnlyList<byte[]> BlockHashes { get; }

    /// <summary>File paths of the directory index (mount point included) and the table entry each one names.</summary>
    public IReadOnlyDictionary<string, int> Files { get; }

    /// <summary>Parses a table of contents.</summary>
    /// <param name="data">The whole <c>.utoc</c> file.</param>
    /// <param name="tocKey">The 32-byte key of everything after the header.</param>
    /// <param name="pakKey">The 32-byte key of encrypted containers' directory index and blocks.</param>
    /// <exception cref="InvalidDataException">The table is malformed or unsupported.</exception>
    /// <exception cref="WrongTocKeyException"><paramref name="tocKey"/> does not decrypt the table.</exception>
    public static IoStoreToc Parse(ReadOnlySpan<byte> data, byte[] tocKey, byte[] pakKey)
    {
        if (data.Length < HeaderSize || !data[..Magic.Length].SequenceEqual(Magic))
        {
            throw new InvalidDataException("Not an I/O store table of contents");
        }

        if (data[Field.Version] != SupportedVersion)
        {
            throw new InvalidDataException($"Table version {data[Field.Version]} is not supported (expected {SupportedVersion})");
        }

        int entryCount = Header(data, Field.EntryCount);
        int blockCount = Header(data, Field.CompressionBlockCount);
        int methodCount = Header(data, Field.CompressionMethodCount);
        int methodNameLength = Header(data, Field.CompressionMethodNameLength);
        int blockSize = Header(data, Field.CompressionBlockSize);
        int directorySize = Header(data, Field.DirectoryIndexSize);
        int seedCount = Header(data, Field.PerfectHashSeedCount);
        int withoutPerfectHash = Header(data, Field.ChunksWithoutPerfectHashCount);
        IoContainerFlags flags = (IoContainerFlags)data[Field.ContainerFlags];
        if (Header(data, Field.TocHeaderSize) != HeaderSize || Header(data, Field.CompressionBlockEntrySize) != BlockEntrySize
            || Header(data, Field.PartitionCount) != 1 || entryCount > MaxEntries || blockCount > MaxBlocks
            || methodCount > MaxMethods || methodNameLength > MaxMethodNameLength || blockSize is <= 0 or > MaxBlockSize
            || seedCount > entryCount * 2 || withoutPerfectHash > entryCount)
        {
            throw new InvalidDataException("Table header is outside the supported layout");
        }

        if ((data.Length - HeaderSize) % AesEcb.BlockSize != 0)
        {
            throw new InvalidDataException("Table body is not a whole number of AES blocks");
        }

        byte[] body = AesEcb.Decrypt(data[HeaderSize..], tocKey);
        ByteReader reader = new(body);
        IoChunkId[] chunkIds = new IoChunkId[entryCount];
        for (int i = 0; i < entryCount; i++)
        {
            chunkIds[i] = IoChunkId.Read(reader.ReadBytes(IoChunkId.Size));
        }

        IoOffsetAndLength[] chunks = new IoOffsetAndLength[entryCount];
        for (int i = 0; i < entryCount; i++)
        {
            ReadOnlySpan<byte> entry = reader.ReadBytes(OffsetAndLengthSize);
            chunks[i] = new IoOffsetAndLength(ReadUInt40BigEndian(entry), ReadUInt40BigEndian(entry[5..]));
        }

        reader.Skip((seedCount + withoutPerfectHash) * sizeof(int));
        IoCompressionBlock[] blocks = new IoCompressionBlock[blockCount];
        for (int i = 0; i < blockCount; i++)
        {
            ReadOnlySpan<byte> entry = reader.ReadBytes(BlockEntrySize);
            IoCompressionBlock block = new(ReadUInt40LittleEndian(entry), ReadUInt24LittleEndian(entry[5..]), ReadUInt24LittleEndian(entry[8..]), entry[11]);
            if (block.CompressedSize <= 0 || block.CompressedSize > blockSize || block.UncompressedSize <= 0
                || block.UncompressedSize > blockSize || block.Method > methodCount)
            {
                throw new WrongTocKeyException($"Storage block {i} is outside the supported layout; the TOC key is wrong");
            }

            blocks[i] = block;
        }

        reader.Skip(methodCount * methodNameLength);
        byte[][] blockHashes = [];
        if (flags.HasFlag(IoContainerFlags.Signed))
        {
            int signatureSize = reader.ReadCount(MaxSignatureSize);
            reader.Skip(2 * signatureSize);
            blockHashes = new byte[blockCount][];
            for (int i = 0; i < blockCount; i++)
            {
                blockHashes[i] = [.. reader.ReadBytes(BlockHashSize)];
            }
        }

        ReadOnlySpan<byte> directory = reader.ReadBytes(directorySize);
        Dictionary<string, int> files = new(StringComparer.Ordinal);
        if (flags.HasFlag(IoContainerFlags.Indexed) && directorySize > 0)
        {
            byte[] plain = flags.HasFlag(IoContainerFlags.Encrypted) ? AesEcb.Decrypt(directory, pakKey) : [.. directory];
            ReadDirectoryIndex(plain, entryCount, files);
        }

        reader.Skip(entryCount * ChunkMetaSize);
        if (reader.Remaining >= AesEcb.BlockSize)
        {
            throw new InvalidDataException("Table has unexpected data after the chunk metadata");
        }

        long addressSpace = (long)blockCount * blockSize;
        for (int i = 0; i < entryCount; i++)
        {
            if (chunks[i].Offset + chunks[i].Length > addressSpace)
            {
                throw new InvalidDataException($"Chunk {i} lies outside the container's blocks");
            }
        }

        return new IoStoreToc(flags, blockSize, chunkIds, chunks, blocks, blockHashes, files);
    }

    private static int Header(ReadOnlySpan<byte> data, int offset)
    {
        uint value = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        return value <= int.MaxValue ? (int)value : throw new InvalidDataException("Table header field is out of range");
    }

    private static void ReadDirectoryIndex(ReadOnlySpan<byte> data, int entryCount, Dictionary<string, int> files)
    {
        ByteReader reader = new(data);
        string mountPoint = reader.ReadString();
        int directoryCount = reader.ReadCount(MaxEntries);
        (uint Name, uint FirstChild, uint NextSibling, uint FirstFile)[] directories = new (uint, uint, uint, uint)[directoryCount];
        for (int i = 0; i < directoryCount; i++)
        {
            directories[i] = (reader.ReadUInt32(), reader.ReadUInt32(), reader.ReadUInt32(), reader.ReadUInt32());
        }

        int fileCount = reader.ReadCount(MaxEntries);
        (uint Name, uint NextFile, uint TocIndex)[] fileEntries = new (uint, uint, uint)[fileCount];
        for (int i = 0; i < fileCount; i++)
        {
            fileEntries[i] = (reader.ReadUInt32(), reader.ReadUInt32(), reader.ReadUInt32());
        }

        int stringCount = reader.ReadCount(MaxEntries);
        string[] strings = new string[stringCount];
        for (int i = 0; i < stringCount; i++)
        {
            strings[i] = reader.ReadString();
        }

        if (directoryCount == 0 || reader.Remaining >= AesEcb.BlockSize)
        {
            throw new InvalidDataException("Directory index is malformed");
        }

        bool[] visitedDirectories = new bool[directoryCount];
        bool[] visitedFiles = new bool[fileCount];
        Stack<(uint Index, string Prefix, int Depth)> pending = new();
        pending.Push((0, mountPoint, 0));
        while (pending.TryPop(out (uint Index, string Prefix, int Depth) item))
        {
            if (item.Index >= directoryCount || visitedDirectories[item.Index] || item.Depth > MaxDirectoryDepth)
            {
                throw new InvalidDataException("Directory index has a cycle or an entry out of range");
            }

            visitedDirectories[item.Index] = true;
            (uint name, uint child, _, uint file) = directories[item.Index];
            string prefix = item.Index == 0 ? item.Prefix : item.Prefix + EntryName(strings, name) + "/";
            while (file != NoEntry)
            {
                if (file >= fileCount || visitedFiles[file])
                {
                    throw new InvalidDataException("Directory index has a file cycle or a file out of range");
                }

                visitedFiles[file] = true;
                (uint fileName, uint next, uint tocIndex) = fileEntries[file];
                string path = prefix + EntryName(strings, fileName);
                if (tocIndex >= entryCount || !files.TryAdd(path, (int)tocIndex))
                {
                    throw new InvalidDataException($"Directory index entry {path} is out of range or repeated");
                }

                file = next;
            }

            while (child != NoEntry)
            {
                if (child >= directoryCount)
                {
                    throw new InvalidDataException("Directory index child is out of range");
                }

                pending.Push((child, prefix, item.Depth + 1));
                child = directories[child].NextSibling;
            }
        }

        if (visitedDirectories.Contains(false) || visitedFiles.Contains(false))
        {
            throw new InvalidDataException("Directory index has unreachable entries");
        }
    }

    private static string EntryName(string[] strings, uint index)
    {
        return index < strings.Length && strings[index].IndexOfAny(['/', '\\', '\0']) < 0 && strings[index].Length > 0
            ? strings[index]
            : throw new InvalidDataException("Directory index name is invalid");
    }

    private static long ReadUInt40BigEndian(ReadOnlySpan<byte> data)
    {
        return ((long)data[0] << 32) | ((long)data[1] << 24) | ((long)data[2] << 16) | ((long)data[3] << 8) | data[4];
    }

    private static long ReadUInt40LittleEndian(ReadOnlySpan<byte> data)
    {
        return ((long)data[4] << 32) | BinaryPrimitives.ReadUInt32LittleEndian(data);
    }

    private static int ReadUInt24LittleEndian(ReadOnlySpan<byte> data)
    {
        return data[0] | (data[1] << 8) | (data[2] << 16);
    }
}
