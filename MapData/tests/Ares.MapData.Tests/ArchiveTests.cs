using Ares.MapData.Archives;
using Ares.MapData.Packages;

namespace Ares.MapData.Tests;

public sealed class ArchiveTests : IDisposable
{
    private readonly string _directory = Directory.CreateTempSubdirectory("ares-mapdata-").FullName;

    public void Dispose()
    {
        Directory.Delete(_directory, recursive: true);
    }

    [Fact]
    public void The_blake3_compression_matches_the_reference_vector()
    {
        uint[] output = new uint[16];
        TheiaCipher.Compress([0x6a09e667, 0xbb67ae85, 0x3c6ef372, 0xa54ff53a, 0x510e527f, 0x9b05688c, 0x1f83d9ab, 0x5be0cd19], new uint[16], 0, 0, 11, output);

        byte[] digest = [.. output[..8].SelectMany(BitConverter.GetBytes)];
        Assert.Equal("af1349b9f5f9a1a6a0404dea36dcc9499bcb25c9adc112b7cc9a93cae41f3262", Convert.ToHexStringLower(digest));
    }

    [Fact]
    public void Aes_ecb_round_trips()
    {
        byte[] plain = [.. Enumerable.Range(0, 48).Select(i => (byte)(i * 5))];

        byte[] encrypted = AesEcb.Encrypt(plain, SyntheticArchives.PakKey);

        Assert.NotEqual(plain, encrypted);
        Assert.Equal(plain, AesEcb.Decrypt(encrypted, SyntheticArchives.PakKey));
    }

    [Fact]
    public void A_table_of_contents_parses_chunks_blocks_and_the_directory_index()
    {
        byte[] first = Bytes(600, 1);
        byte[] second = Bytes(10, 2);
        IoChunkId firstId = new(0x1122334455667788, 0, IoChunkType.ExportBundleData);
        IoChunkId secondId = new(0x99, 3, IoChunkType.BulkData);
        SyntheticArchives.WriteContainer(_directory, "test",
        [
            new SyntheticChunk(firstId, first, "../../../Game/Content/Maps/A.umap"),
            new SyntheticChunk(secondId, second, "../../../Game/Content/Maps/Sub/B.ubulk"),
        ]);

        IoStoreToc toc = IoStoreToc.Parse(File.ReadAllBytes(Path.Combine(_directory, "test.utoc")), SyntheticArchives.TocKey, SyntheticArchives.PakKey);

        Assert.Equal([firstId, secondId], toc.ChunkIds);
        Assert.Equal(new IoOffsetAndLength(0, 600), toc.Chunks[0]);
        Assert.Equal(new IoOffsetAndLength(768, 10), toc.Chunks[1]);
        Assert.Equal(4, toc.Blocks.Count);
        Assert.Equal(4, toc.BlockHashes.Count);
        Assert.Equal(256, toc.CompressionBlockSize);
        Assert.Equal(0, toc.Files["../../../Game/Content/Maps/A.umap"]);
        Assert.Equal(1, toc.Files["../../../Game/Content/Maps/Sub/B.ubulk"]);
    }

    [Fact]
    public void A_wrong_table_key_is_rejected()
    {
        SyntheticArchives.WriteContainer(_directory, "test", [new SyntheticChunk(new IoChunkId(1, 0, IoChunkType.ExportBundleData), Bytes(40, 1), "../../../Game/Content/A.uasset")]);
        byte[] wrongKey = new byte[32];

        Assert.Throws<WrongTocKeyException>(() => IoStoreToc.Parse(File.ReadAllBytes(Path.Combine(_directory, "test.utoc")), wrongKey, SyntheticArchives.PakKey));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Chunks_read_back_across_blocks(bool encrypted)
    {
        byte[] data = Bytes(1000, 7);
        IoContainerFlags flags = IoContainerFlags.Indexed | IoContainerFlags.Signed | (encrypted ? IoContainerFlags.Encrypted : IoContainerFlags.None);
        SyntheticArchives.WriteContainer(_directory, "test",
            [new SyntheticChunk(new IoChunkId(5, 0, IoChunkType.BulkData), Bytes(300, 3), "../../../Game/Content/X.ubulk"), new SyntheticChunk(new IoChunkId(6, 0, IoChunkType.BulkData), data, "../../../Game/Content/Y.ubulk")],
            flags: flags);
        using IoStoreContainer container = IoStoreContainer.Open(Path.Combine(_directory, "test.utoc"), Keys(), TableKey());

        Assert.Equal(data, container.ReadChunk(1));
        byte[] middle = new byte[300];
        container.ReadChunk(1, 200, middle);
        Assert.Equal(data[200..500], middle);
        Assert.Equal(1, container.IndexOf(new IoChunkId(6, 0, IoChunkType.BulkData)));
    }

    [Fact]
    public void Compressed_blocks_fail_clearly()
    {
        SyntheticArchives.WriteContainer(_directory, "test", [new SyntheticChunk(new IoChunkId(5, 0, IoChunkType.BulkData), Bytes(100, 3), "../../../Game/Content/X.ubulk")],
            compressionMethod: 1);
        using IoStoreContainer container = IoStoreContainer.Open(Path.Combine(_directory, "test.utoc"), Keys(), TableKey());

        InvalidDataException error = Assert.Throws<InvalidDataException>(() => container.ReadChunk(0));
        Assert.Contains("compressed", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_wrong_container_key_fails_the_block_hash()
    {
        SyntheticArchives.WriteContainer(_directory, "test", [new SyntheticChunk(new IoChunkId(5, 0, IoChunkType.BulkData), Bytes(100, 3), "../../../Game/Content/X.ubulk")]);
        Common.Archives.ArchiveKeys keys = Keys() with { ContainerKey = [9, 9, 9, 9, 9, 9, 9, 9] };
        using IoStoreContainer container = IoStoreContainer.Open(Path.Combine(_directory, "test.utoc"), keys, TableKey());

        InvalidDataException error = Assert.Throws<InvalidDataException>(() => container.ReadChunk(0));
        Assert.Contains("container-key", error.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(0, 0x9ae16a3b2f90404fUL)]
    [InlineData(1, 0xbec09cc447469d8bUL)]
    [InlineData(3, 0x40b4412e33eefd64UL)]
    [InlineData(4, 0xf3c7ee05197a7b92UL)]
    [InlineData(8, 0x0c97ecbc1e9d2626UL)]
    [InlineData(9, 0xe51cf38098a5bfffUL)]
    [InlineData(16, 0x7eda0ed8c837a168UL)]
    [InlineData(17, 0xc9518fe63ebf6127UL)]
    [InlineData(32, 0xa53f806f23a314e8UL)]
    [InlineData(33, 0x0d861240f8a6c64bUL)]
    [InlineData(64, 0x1ccefea99389ae75UL)]
    [InlineData(65, 0xacf6a69f1f078342UL)]
    [InlineData(100, 0x90d13909657dcf12UL)]
    [InlineData(200, 0x6099c264b552b496UL)]
    public void CityHash64_matches_reference_values(int length, ulong expected)
    {
        byte[] data = [.. Enumerable.Range(0, length).Select(i => (byte)((i * 7) + 3))];

        Assert.Equal(expected, CityHash.Hash64(data));
    }

    [Fact]
    public void Package_and_script_ids_hash_lower_cased_paths()
    {
        Assert.Equal(0x54864f7a2e8e1c54UL, PackageId.FromPath("/Game/Pioneer/Maps/Test"));
        Assert.Equal(PackageId.FromPath("/game/pioneer/maps/test"), PackageId.FromPath("/Game/Pioneer/Maps/Test"));
        PackageObjectIndex script = PackageObjectIndex.FromScriptPath("/Script/Engine.StaticMeshComponent");
        Assert.Equal(0x57defc7857c66b65UL, script.Value);
        Assert.Equal(PackageObjectKind.ScriptImport, script.Kind);
    }

    private static byte[] Bytes(int length, int seed) => [.. Enumerable.Range(0, length).Select(i => (byte)((i * 31) + seed))];

    internal static Common.Archives.ArchiveKeys Keys()
    {
        return new Common.Archives.ArchiveKeys(SyntheticArchives.PakKey, SyntheticArchives.ContainerKey);
    }

    internal static TocKey TableKey() => TocKey.Parse(Convert.ToHexString(SyntheticArchives.TocKey));
}
