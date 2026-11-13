using Ares.Common.Archives;

namespace Ares.Common.Tests;

public sealed class ArchiveKeysTests
{
    /// <summary>Dummy keys: pak key bytes 00..1F, container key words 1..8.</summary>
    private const string DummyFile = """
        # dummy keys
        pak-key = 0x000102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1E1F
        container-key = 0x00000001, 0x00000002, 0x3, 4, 0x00000005, 0x00000006, 0x00000007, 0x00000008
        """;

    /// <summary>Fingerprint of <see cref="DummyFile"/>.</summary>
    private const string DummySha256 = "5c0d2cb321c672c26c4cbdfedb34f517600ce56f34d1b4244039146f63f8b309";

    [Fact]
    public void A_keys_file_parses_and_has_a_stable_fingerprint()
    {
        ArchiveKeys keys = ArchiveKeys.Parse(DummyFile);

        Assert.Equal([.. Enumerable.Range(0, 32).Select(i => (byte)i)], keys.PakKey);
        Assert.Equal([1u, 2u, 3u, 4u, 5u, 6u, 7u, 8u], keys.ContainerKey);
        Assert.Equal(DummySha256, keys.Fingerprint());
    }

    [Fact]
    public void Brace_lists_and_windows_line_ends_are_accepted()
    {
        ArchiveKeys keys = ArchiveKeys.Parse(
            "pak-key=000102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f\r\ncontainer-key=[1,2,3,4,5,6,7,8]\r\n");

        Assert.Equal(ArchiveKeys.Parse(DummyFile).Fingerprint(), keys.Fingerprint());
    }

    [Theory]
    [InlineData("pak-key = 00")]
    [InlineData("container-key = 1, 2, 3, 4, 5, 6, 7, 8")]
    [InlineData("pak-key = 000102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1E1F\ncontainer-key = 1, 2, 3")]
    [InlineData("pak-key = 000102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1E1G\ncontainer-key = 1, 2, 3, 4, 5, 6, 7, 8")]
    [InlineData("pak-key = 000102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1E1F\ncontainer-key = 1, 2, 3, 4, 5, 6, 7, 100000000")]
    [InlineData("pak-key = 000102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1E1F\npak-key = 00\ncontainer-key = 1, 2, 3, 4, 5, 6, 7, 8")]
    [InlineData("pak-key = 000102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1E1F\ncontainer-key = 1, 2, 3, 4, 5, 6, 7, 8\ntoc-key = 000102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1E1F")]
    [InlineData("aes = 00")]
    [InlineData("just text")]
    public void Malformed_keys_files_are_rejected(string text)
    {
        Assert.Throws<FormatException>(() => ArchiveKeys.Parse(text));
    }
}
