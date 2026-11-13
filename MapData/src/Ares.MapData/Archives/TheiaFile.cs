using System.Buffers.Binary;

namespace Ares.MapData.Archives;

/// <summary>
/// Read-only access to a wrapped game data file. Page keys come from the sibling <c>.meta</c> file; the file is
/// opened for reading only and shared with other readers.
/// </summary>
internal sealed class TheiaFile : IDisposable
{
    /// <summary>Bytes per page; every page has its own key.</summary>
    public const int PageSize = 65536;

    /// <summary>Extension of the metadata file next to the data file.</summary>
    public const string MetadataExtension = ".meta";

    /// <summary>Metadata magic.</summary>
    public static ReadOnlySpan<byte> Magic => "metadat0"u8;

    /// <summary>Offset of the data file's size in the metadata.</summary>
    public const int SizeOffset = 80;

    /// <summary>Offset of the 32-byte seed of the file's master key in the metadata.</summary>
    public const int SeedOffset = 96;

    /// <summary>Offset of the first page record in the metadata.</summary>
    public const int PagesOffset = 512;

    /// <summary>Bytes per page record; the page key is its second half.</summary>
    public const int PageRecordSize = 64;

    private const int SeedBytes = 32;
    private const int PageKeyOffset = 32;

    private readonly FileStream _stream;
    private readonly byte[] _metadata;
    private readonly uint[] _containerKey;
    private readonly uint[] _master;

    /// <summary>Opens <paramref name="path"/> and reads its metadata.</summary>
    /// <exception cref="InvalidDataException">The metadata is missing, malformed or describes another file size.</exception>
    public TheiaFile(string path, IReadOnlyList<uint> containerKey)
    {
        if (containerKey.Count != TheiaCipher.KeyWords)
        {
            throw new ArgumentException($"The container key must have {TheiaCipher.KeyWords} words", nameof(containerKey));
        }

        string metadataPath = path + MetadataExtension;
        if (!File.Exists(metadataPath))
        {
            throw new InvalidDataException($"{Path.GetFileName(metadataPath)} is missing");
        }

        _metadata = File.ReadAllBytes(metadataPath);
        _stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        Length = _stream.Length;
        if (_metadata.Length < PagesOffset || !_metadata.AsSpan(0, Magic.Length).SequenceEqual(Magic)
            || BinaryPrimitives.ReadInt64LittleEndian(_metadata.AsSpan(SizeOffset)) != Length)
        {
            _stream.Dispose();
            throw new InvalidDataException($"{Path.GetFileName(metadataPath)} does not describe {Path.GetFileName(path)}");
        }

        _containerKey = [.. containerKey];
        byte[] seed = new byte[2 * SeedBytes];
        _metadata.AsSpan(SeedOffset, SeedBytes).CopyTo(seed);
        _master = TheiaCipher.Hash64(seed);
    }

    /// <summary>Size of the data file in bytes.</summary>
    public long Length { get; }

    /// <summary>Reads and decrypts <paramref name="destination"/>.Length bytes starting at <paramref name="offset"/>.</summary>
    public void Read(long offset, Span<byte> destination)
    {
        if (offset < 0 || offset > Length - destination.Length)
        {
            throw new InvalidDataException($"Read of {destination.Length} bytes at {offset} is outside the data file ({Length} bytes)");
        }

        _stream.Seek(offset, SeekOrigin.Begin);
        _stream.ReadExactly(destination);
        Span<uint> pageKey = stackalloc uint[TheiaCipher.KeyWords];
        int at = 0;
        while (at < destination.Length)
        {
            long page = (offset + at) / PageSize;
            int within = (int)((offset + at) % PageSize);
            int count = Math.Min(destination.Length - at, PageSize - within);
            long keyOffset = PagesOffset + (page * PageRecordSize) + PageKeyOffset;
            if (keyOffset + (TheiaCipher.KeyWords * sizeof(uint)) > _metadata.Length)
            {
                throw new InvalidDataException($"Page {page} has no key in the metadata");
            }

            for (int i = 0; i < TheiaCipher.KeyWords; i++)
            {
                pageKey[i] = BinaryPrimitives.ReadUInt32LittleEndian(_metadata.AsSpan((int)keyOffset + (i * sizeof(uint))));
            }

            TheiaCipher.DecryptPage(destination.Slice(at, count), (uint)within, _containerKey, _master, pageKey);
            at += count;
        }
    }

    public void Dispose()
    {
        _stream.Dispose();
    }
}
