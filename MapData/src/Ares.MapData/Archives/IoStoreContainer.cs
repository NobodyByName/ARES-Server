using System.Security.Cryptography;
using Ares.Common.Archives;

namespace Ares.MapData.Archives;

/// <summary>
/// One read-only I/O store container: its table of contents (<c>.utoc</c>) and its data file (<c>.ucas</c>),
/// which is opened on the first chunk read. Only stored (uncompressed) blocks are supported.
/// </summary>
internal sealed class IoStoreContainer : IDisposable
{
    /// <summary>Extension of the table of contents.</summary>
    public const string TocExtension = ".utoc";

    /// <summary>Extension of the data file.</summary>
    public const string DataExtension = ".ucas";

    private readonly string _dataPath;
    private readonly IReadOnlyList<uint> _containerKey;
    private readonly byte[] _pakKey;
    private TheiaFile? _data;

    private IoStoreContainer(string name, string dataPath, IoStoreToc toc, ArchiveKeys keys)
    {
        Name = name;
        Toc = toc;
        _dataPath = dataPath;
        _containerKey = keys.ContainerKey;
        _pakKey = keys.PakKey;
    }

    /// <summary>File name of the table of contents without its extension.</summary>
    public string Name { get; }

    public IoStoreToc Toc { get; }

    /// <summary>Opens the container whose table of contents is <paramref name="tocPath"/>.</summary>
    /// <exception cref="InvalidDataException">The table is malformed or unsupported.</exception>
    /// <exception cref="WrongTocKeyException"><paramref name="tocKey"/> does not decrypt the table.</exception>
    public static IoStoreContainer Open(string tocPath, ArchiveKeys keys, TocKey tocKey)
    {
        IoStoreToc toc = IoStoreToc.Parse(File.ReadAllBytes(tocPath), tocKey.Bytes, keys.PakKey);
        return new IoStoreContainer(Path.GetFileNameWithoutExtension(tocPath), Path.ChangeExtension(tocPath, DataExtension), toc, keys);
    }

    /// <summary>Index of the first table entry with chunk id <paramref name="id"/>, or -1.</summary>
    public int IndexOf(IoChunkId id)
    {
        for (int i = 0; i < Toc.ChunkIds.Count; i++)
        {
            if (Toc.ChunkIds[i] == id)
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>Length of the chunk of table entry <paramref name="entry"/>.</summary>
    private long ChunkLength(int entry) => Toc.Chunks[CheckedEntry(entry)].Length;

    /// <summary>Reads the whole chunk of table entry <paramref name="entry"/>.</summary>
    public byte[] ReadChunk(int entry)
    {
        long length = ChunkLength(entry);
        if (length > Array.MaxLength)
        {
            throw new InvalidDataException($"Chunk {entry} of {Name} is too large to read at once ({length} bytes)");
        }

        byte[] result = new byte[length];
        ReadChunk(entry, 0, result);
        return result;
    }

    /// <summary>Reads <paramref name="destination"/>.Length bytes of a chunk, starting <paramref name="offset"/> bytes into it.</summary>
    /// <exception cref="InvalidDataException">The range is outside the chunk, a block is compressed or a block hash differs.</exception>
    public void ReadChunk(int entry, long offset, Span<byte> destination)
    {
        IoOffsetAndLength chunk = Toc.Chunks[CheckedEntry(entry)];
        if (offset < 0 || offset > chunk.Length - destination.Length)
        {
            throw new InvalidDataException($"Read of {destination.Length} bytes at {offset} is outside chunk {entry} of {Name} ({chunk.Length} bytes)");
        }

        _data ??= new TheiaFile(_dataPath, _containerKey);
        bool encrypted = Toc.Flags.HasFlag(IoContainerFlags.Encrypted);
        long position = chunk.Offset + offset;
        int written = 0;
        byte[]? stored = null;
        while (written < destination.Length)
        {
            int blockIndex = (int)(position / Toc.CompressionBlockSize);
            int within = (int)(position % Toc.CompressionBlockSize);
            IoCompressionBlock block = Toc.Blocks[blockIndex];
            if (block.Method != 0)
            {
                throw new InvalidDataException($"Block {blockIndex} of {Name} is compressed; compressed containers are not supported");
            }

            if (block.CompressedSize != block.UncompressedSize || within >= block.UncompressedSize)
            {
                throw new InvalidDataException($"Block {blockIndex} of {Name} does not cover the requested bytes");
            }

            int storedSize = (block.CompressedSize + AesEcb.BlockSize - 1) & ~(AesEcb.BlockSize - 1);
            if (stored is null || stored.Length != storedSize)
            {
                stored = new byte[storedSize];
            }

            _data.Read(block.Offset, stored);
            if (Toc.BlockHashes.Count > 0 && !SHA1.HashData(stored).AsSpan().SequenceEqual(Toc.BlockHashes[blockIndex]))
            {
                throw new InvalidDataException($"Block {blockIndex} of {Name} does not match its hash; check the container-key");
            }

            ReadOnlySpan<byte> plain = encrypted ? AesEcb.Decrypt(stored, _pakKey) : stored;
            int count = Math.Min(destination.Length - written, block.UncompressedSize - within);
            plain.Slice(within, count).CopyTo(destination[written..]);
            written += count;
            position += count;
        }
    }

    public void Dispose()
    {
        _data?.Dispose();
    }

    private int CheckedEntry(int entry)
    {
        return entry >= 0 && entry < Toc.Chunks.Count
            ? entry
            : throw new ArgumentOutOfRangeException(nameof(entry), $"{Name} has no table entry {entry}");
    }
}
