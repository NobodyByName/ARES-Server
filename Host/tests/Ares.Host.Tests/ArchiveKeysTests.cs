using Ares.Common.Archives;
using Ares.Host.Services.Status;

namespace Ares.Host.Tests;

public sealed class ArchiveKeysTests
{
    /// <summary>Dummy keys: pak key bytes 00..1F, container key words 1..8.</summary>
    internal const string DummyFile = """
        # dummy keys
        pak-key = 0x000102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1E1F
        container-key = 0x00000001, 0x00000002, 0x3, 4, 0x00000005, 0x00000006, 0x00000007, 0x00000008
        """;

    /// <summary>Fingerprint of <see cref="DummyFile"/>.</summary>
    internal const string DummySha256 = "5c0d2cb321c672c26c4cbdfedb34f517600ce56f34d1b4244039146f63f8b309";

    [Fact]
    public void The_served_document_uses_fixed_width_hex()
    {
        ArchiveKeysDocument document = ArchiveKeys.Parse(DummyFile).ToDocument("123");

        Assert.Equal("123", document.GameBuild);
        Assert.Equal("000102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1E1F", document.PakKey);
        Assert.Equal("00000001", document.ContainerKey[0]);
        Assert.Equal(8, document.ContainerKey.Count);
    }
}
