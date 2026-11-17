using System.Text;
using Ares.Common.Archives;
using Ares.GameDataExtract.Archives;
using Ares.GameDataExtract.Registry;

namespace Ares.GameDataExtract.Tests;

public sealed class AssetRegistryTests : IDisposable
{
    private readonly string _directory = Directory.CreateTempSubdirectory("ares-registry-").FullName;

    private static readonly string[] s_names =
        ["/Game/Items", "/Script/Test", "ItemAsset", "/Game/Items/DA_A", "DA_A", "AssetId", "Kind", "Gadget", "Parent", "/Game/Items/DA_B", "DA_B", "Group", "Note"];

    [Fact]
    public void Assets_and_their_tags_read_from_the_registry()
    {
        AssetRegistry registry = AssetRegistry.Parse(Registry());

        Assert.Equal(2, registry.Assets.Count);
        RegistryAsset first = registry.Assets[0];
        Assert.Equal(new RegistryAsset("/Game/Items", "/Game/Items/DA_A", "DA_A", "/Script/Test.ItemAsset", first.TagsHandle, 0x80000000), first);
        Assert.Equal("/Game/Items/DA_A.DA_A", first.ObjectPath);
        Assert.Equal(new Dictionary<string, string>
        {
            ["AssetId"] = "923438116",
            ["Kind"] = "Gadget",
            ["Parent"] = "ItemAsset'/Game/Items/DA_B.DA_B'",
            ["Note"] = "Hello",
        }, registry.Tags(first));
        Assert.Equal(new Dictionary<string, string> { ["Group"] = "Group_2" }, registry.Tags(registry.Assets[1]));
    }

    public void Dispose()
    {
        Directory.Delete(_directory, recursive: true);
    }

    [Fact]
    public void The_registry_loads_from_the_main_pak()
    {
        string path = Path.Combine(_directory, "main.pak");
        SyntheticPak.Write(path, PakFile.InstallMountPrefix, [new SyntheticPakEntry(AssetRegistry.PakPath, Registry(), Encrypted: true)]);
        using PakFile pak = PakFile.Open(path, new ArchiveKeys(SyntheticArchives.PakKey, SyntheticArchives.ContainerKey));

        Assert.Equal("DA_B", AssetRegistry.Load(pak).Assets[1].AssetName);

        string empty = Path.Combine(_directory, "empty.pak");
        SyntheticPak.Write(empty, PakFile.InstallMountPrefix, [new SyntheticPakEntry("Other.bin", [1])]);
        using PakFile other = PakFile.Open(empty, new ArchiveKeys(SyntheticArchives.PakKey, SyntheticArchives.ContainerKey));
        Assert.Throws<InvalidDataException>(() => AssetRegistry.Load(other));
    }

    [Fact]
    public void Another_format_or_version_is_rejected()
    {
        byte[] data = Registry();
        data[0] ^= 1;
        Assert.Throws<InvalidDataException>(() => AssetRegistry.Parse(data));

        byte[] version = Registry();
        version[16] = 21;
        Assert.Throws<InvalidDataException>(() => AssetRegistry.Parse(version));
        Assert.Throws<InvalidDataException>(() => AssetRegistry.Parse(Registry()[..^4]));
    }

    private static byte[] Registry()
    {
        MemoryStream stream = new();
        stream.Write(Convert.FromHexString("E79E7F713A49B0E93291B3880781381B"));
        Words(stream, AssetRegistry.SupportedVersion, 1);
        stream.Write(SyntheticArchives.NameBatch(s_names));

        byte[] ansi = Encoding.UTF8.GetBytes("923438116\0");
        Words(stream, 0x12345679);
        Words(stream, 1, 1, 1, 0, 1, 1, 0, (uint)ansi.Length, 0, 4, 1, 0);
        stream.Write(PropertyBlocks.String("Hello"));
        Words(stream, Name("Gadget"));
        Words(stream, Name("Group") | 0x80000000, 3);
        Words(stream, Name("/Script/Test"), Name("ItemAsset"), Name("DA_B"), Name("/Game/Items/DA_B"));
        Words(stream, 0);
        stream.Write(ansi);
        Words(stream, Name("AssetId"), 0, Name("Kind"), 2, Name("Parent"), 4, Name("Note"), 6);
        Words(stream, Name("Group"), 3);
        Words(stream, 0x87654321);

        Words(stream, 2);
        Asset(stream, "DA_A", "/Game/Items/DA_A", (1UL << 63) | (4UL << 32));
        Asset(stream, "DA_B", "/Game/Items/DA_B", 1UL << 32);
        return stream.ToArray();
    }

    private static void Asset(MemoryStream stream, string name, string package, ulong tags)
    {
        Words(stream, Name("/Game/Items"), Name("/Script/Test"), Name("ItemAsset"), Name(package), Name(name));
        stream.Write(BitConverter.GetBytes(tags));
        Words(stream, 0, 1, 0, 0x80000000);
    }

    private static void Words(MemoryStream stream, params uint[] words)
    {
        foreach (uint word in words)
        {
            stream.Write(BitConverter.GetBytes(word));
        }
    }

    private static uint Name(string name) => (uint)Array.IndexOf(s_names, name);
}
