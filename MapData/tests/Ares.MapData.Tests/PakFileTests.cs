using Ares.Common.Archives;
using Ares.MapData.Archives;

namespace Ares.MapData.Tests;

public sealed class PakFileTests : IDisposable
{
    private readonly string _directory = Directory.CreateTempSubdirectory("ares-mapdata-").FullName;

    private static ArchiveKeys Keys => new(SyntheticArchives.PakKey, SyntheticArchives.ContainerKey);

    public void Dispose()
    {
        Directory.Delete(_directory, recursive: true);
    }

    [Fact]
    public void The_index_cipher_round_trips()
    {
        byte[] plain = [.. Enumerable.Range(0, 64).Select(i => (byte)(i * 7))];

        byte[] encrypted = PakIndexCipher.Encrypt(plain, SyntheticArchives.PakKey);

        Assert.NotEqual(plain, encrypted);
        Assert.NotEqual(AesEcb.Encrypt(plain, SyntheticArchives.PakKey), encrypted);
        Assert.Equal(plain, PakIndexCipher.Decrypt(encrypted, SyntheticArchives.PakKey));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Entries_are_listed_relative_to_the_install_folder_and_read_back(bool encryptIndex)
    {
        byte[] plain = Bytes(70_000, 3);
        byte[] secret = Bytes(37, 5);
        string path = Path.Combine(_directory, "test.pak");
        SyntheticPak.Write(path, "../../../",
        [
            new SyntheticPakEntry("Engine/Config/Base.ini", plain),
            new SyntheticPakEntry("Game/Config/Secret.ini", secret, Encrypted: true),
        ], encryptIndex);

        using PakFile pak = PakFile.Open(path, Keys);

        Assert.Equal(["Engine/Config/Base.ini", "Game/Config/Secret.ini"], pak.Paths.Order(StringComparer.Ordinal));
        Assert.True(pak.Contains("Game/Config/Secret.ini"));
        Assert.Equal(plain, pak.Read("Engine/Config/Base.ini"));
        Assert.Equal(secret, pak.Read("Game/Config/Secret.ini"));
        Assert.Throws<FileNotFoundException>(() => pak.Read("Game/Config/Missing.ini"));
    }

    [Fact]
    public void A_mount_point_outside_the_install_folder_is_kept()
    {
        string path = Path.Combine(_directory, "test.pak");
        SyntheticPak.Write(path, "/Mount/", [new SyntheticPakEntry("A.txt", [1, 2, 3])]);

        using PakFile pak = PakFile.Open(path, Keys);

        Assert.Equal([1, 2, 3], pak.Read("/Mount/A.txt"));
    }

    [Fact]
    public void A_wrong_pak_key_is_reported()
    {
        string path = Path.Combine(_directory, "test.pak");
        SyntheticPak.Write(path, "../../../", [new SyntheticPakEntry("A.txt", [1])]);
        ArchiveKeys wrong = new([.. SyntheticArchives.PakKey.Select(b => (byte)(b ^ 1))], SyntheticArchives.ContainerKey);

        InvalidDataException error = Assert.Throws<InvalidDataException>(() => PakFile.Open(path, wrong));

        Assert.Contains("pak-key", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Compressed_entries_are_rejected()
    {
        string path = Path.Combine(_directory, "test.pak");
        SyntheticPak.Write(path, "../../../", [new SyntheticPakEntry("A.txt", [1, 2], Compressed: true)]);

        using PakFile pak = PakFile.Open(path, Keys);

        Assert.Throws<InvalidDataException>(() => pak.Read("A.txt"));
    }

    private static byte[] Bytes(int count, int seed)
    {
        return [.. Enumerable.Range(0, count).Select(i => (byte)((i * seed) + 1))];
    }
}
