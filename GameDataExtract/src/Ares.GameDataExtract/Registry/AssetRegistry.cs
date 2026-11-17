using System.Text;
using Ares.GameDataExtract.Archives;
using Ares.GameDataExtract.Packages;
using Ares.Structs.Core;

namespace Ares.GameDataExtract.Registry;

/// <summary>One asset the registry lists.</summary>
/// <param name="PackagePath">Folder of the package, for example <c>/Game/Items</c>.</param>
/// <param name="PackageName">The package, for example <c>/Game/Items/DA_Item</c>.</param>
/// <param name="AssetName">The asset in it, for example <c>DA_Item</c>.</param>
/// <param name="ClassPath">The asset's class, for example <c>/Script/Engine.DataAsset</c>.</param>
/// <param name="TagsHandle">Where its tags are in the tag store.</param>
/// <param name="PackageFlags">Flags of the package.</param>
internal sealed record RegistryAsset(string PackagePath, string PackageName, string AssetName, string ClassPath, ulong TagsHandle, uint PackageFlags)
{
    /// <summary>The asset's object path, for example <c>/Game/Items/DA_Item.DA_Item</c>.</summary>
    public string ObjectPath => $"{PackageName}.{AssetName}";
}

/// <summary>
/// The installed game's asset registry (<c>AssetRegistry.bin</c>): every cooked asset with its class and tags. The file
/// holds a format GUID and version, a name batch, the tag store (tag values in typed tables, tag maps as runs of key
/// and value pairs) and the asset list; what follows the asset list (dependencies, package data) is not read.
/// </summary>
internal sealed class AssetRegistry
{
    /// <summary>Path of the registry in the main pak.</summary>
    public const string PakPath = GameBuild.ProjectName + "/AssetRegistry.bin";

    /// <summary>Registry format version read.</summary>
    public const uint SupportedVersion = 22;

    private const uint StoreBeginMagic = 0x12345679;
    private const uint StoreEndMagic = 0x87654321;
    private const uint NumberedNameFlag = 0x80000000;
    private const int StoreCountFields = 11;
    private const int MaxCount = 4_000_000;
    private const int ValueKindBits = 3;
    private const int ValueKindMask = (1 << ValueKindBits) - 1;
    private const int TagCountShift = 32;
    private const int TagCountMask = 0xFFFF;
    private const int NumberlessTagsBit = 63;
    private const int PathParts = 4;

    private static readonly byte[] s_formatGuid = Convert.FromHexString("E79E7F713A49B0E93291B3880781381B");

    private readonly string[] _names;
    private readonly Store _store;

    private AssetRegistry(string[] names, Store store, RegistryAsset[] assets)
    {
        _names = names;
        _store = store;
        Assets = assets;
    }

    /// <summary>Every asset, in registry order.</summary>
    public IReadOnlyList<RegistryAsset> Assets { get; }

    /// <summary>Reads the registry from the game's main pak.</summary>
    /// <exception cref="InvalidDataException">The registry is missing or malformed.</exception>
    public static AssetRegistry Load(PakFile pak)
    {
        try
        {
            return Parse(pak.Read(PakPath));
        }
        catch (FileNotFoundException error)
        {
            throw new InvalidDataException($"{PakPath} is not in the pak", error);
        }
    }

    /// <summary>Parses a registry file.</summary>
    /// <exception cref="InvalidDataException">The data is not a registry of the supported version, or is malformed.</exception>
    public static AssetRegistry Parse(ReadOnlySpan<byte> data)
    {
        ByteReader reader = new(data);
        if (!reader.ReadBytes(s_formatGuid.Length).SequenceEqual(s_formatGuid))
        {
            throw new InvalidDataException("Not an asset registry");
        }

        uint version = reader.ReadUInt32();
        uint filtered = reader.ReadUInt32();
        if (version != SupportedVersion || filtered > 1)
        {
            throw new InvalidDataException($"Asset registry version {version} is not supported");
        }

        string[] names = NameBatch.Read(ref reader);
        Store store = Store.Read(ref reader, names);
        RegistryAsset[] assets = new RegistryAsset[reader.ReadCount(MaxCount)];
        for (int i = 0; i < assets.Length; i++)
        {
            string path = ReadName(ref reader, names);
            string classPackage = ReadName(ref reader, names);
            string className = ReadName(ref reader, names);
            string package = ReadName(ref reader, names);
            string asset = ReadName(ref reader, names);
            if (filtered == 0)
            {
                ReadName(ref reader, names);
            }

            ulong tags = reader.ReadUInt64();
            int bundles = reader.ReadCount(MaxCount);
            for (int bundle = 0; bundle < bundles; bundle++)
            {
                ReadName(ref reader, names);
                int paths = reader.ReadCount(MaxCount);
                for (int j = 0; j < paths; j++)
                {
                    ReadName(ref reader, names);
                    ReadName(ref reader, names);
                    reader.ReadString();
                }
            }

            reader.Skip(reader.ReadCount(MaxCount) * sizeof(int));
            assets[i] = new RegistryAsset(path, package, asset, $"{classPackage}.{className}", tags, reader.ReadUInt32());
        }

        return new AssetRegistry(names, store, assets);
    }

    /// <summary>
    /// The tags of <paramref name="asset"/> by key. Values read as text: strings and names as stored, object paths as
    /// <c>Class'Package.Object'</c> (or <c>Package.Object</c> without a class).
    /// </summary>
    /// <exception cref="InvalidDataException">The asset's tag map or a value lies outside the tag store.</exception>
    public IReadOnlyDictionary<string, string> Tags(RegistryAsset asset)
    {
        ulong handle = asset.TagsHandle;
        int count = (int)((handle >> TagCountShift) & TagCountMask);
        long start = (uint)handle;
        bool numberless = (handle >> NumberlessTagsBit) != 0;
        IReadOnlyList<(string Key, uint Value)> pairs = numberless ? _store.NumberlessPairs : _store.NumberedPairs;
        if (start + count > pairs.Count)
        {
            throw new InvalidDataException($"{asset.ObjectPath}: tag map outside the tag store");
        }

        Dictionary<string, string> tags = new(count, StringComparer.Ordinal);
        for (int i = 0; i < count; i++)
        {
            (string key, uint value) = pairs[(int)start + i];
            tags[key] = Value(value);
        }

        return tags;
    }

    private string Value(uint id)
    {
        int index = (int)(id >> ValueKindBits);
        return (id & ValueKindMask) switch
        {
            0 => TerminatedString(_store.Ansi, At(_store.AnsiOffsets, index), 1, Encoding.UTF8),
            1 => TerminatedString(_store.Wide, At(_store.WideOffsets, index) * sizeof(char), sizeof(char), Encoding.Unicode),
            2 => _names[At(_store.NumberlessNames, index)],
            3 => At(_store.NumberedNames, index),
            4 => ExportPath([.. At(_store.NumberlessPaths, index).Select(name => _names[name])]),
            5 => ExportPath(At(_store.NumberedPaths, index)),
            6 => At(_store.Texts, index),
            _ => throw new InvalidDataException($"Tag value kind {id & ValueKindMask} is not supported"),
        };
    }

    private static T At<T>(IReadOnlyList<T> values, int index) =>
        index < values.Count ? values[index] : throw new InvalidDataException($"Tag value {index} is outside the tag store");

    private static string ExportPath(IReadOnlyList<string> path)
    {
        (string cls, string obj, string package) = (path[1], path[2], path[3]);
        string text = obj == CookedPackage.NoneName ? package : $"{package}.{obj}";
        return cls == CookedPackage.NoneName ? text : $"{cls}'{text}'";
    }

    private static string TerminatedString(byte[] data, long offset, int unit, Encoding encoding)
    {
        for (long end = offset; end + unit <= data.Length; end += unit)
        {
            if (data.AsSpan((int)end, unit).IndexOfAnyExcept((byte)0) < 0)
            {
                return encoding.GetString(data, (int)offset, (int)(end - offset));
            }
        }

        throw new InvalidDataException("A tag string runs past the tag store");
    }

    private static string ReadName(ref ByteReader reader, string[] names)
    {
        uint index = reader.ReadUInt32();
        uint number = (index & NumberedNameFlag) != 0 ? reader.ReadUInt32() : 0;
        index &= ~NumberedNameFlag;
        return index < names.Length ? NameBatch.Display(names[index], number) : throw new InvalidDataException($"Name index {index} is outside the name batch");
    }

    /// <summary>The tag store: tag values by kind, and tag maps as runs of key and value-id pairs.</summary>
    private sealed record Store(
        string[] Texts,
        uint[] NumberlessNames,
        string[] NumberedNames,
        uint[][] NumberlessPaths,
        string[][] NumberedPaths,
        uint[] AnsiOffsets,
        uint[] WideOffsets,
        byte[] Ansi,
        byte[] Wide,
        (string Key, uint Value)[] NumberlessPairs,
        (string Key, uint Value)[] NumberedPairs)
    {
        public static Store Read(ref ByteReader reader, string[] names)
        {
            if (reader.ReadUInt32() != StoreBeginMagic)
            {
                throw new InvalidDataException("Asset registry tag store does not start where expected");
            }

            int[] counts = new int[StoreCountFields];
            for (int i = 0; i < counts.Length; i++)
            {
                counts[i] = reader.ReadCount(int.MaxValue / sizeof(char));
            }

            reader.Skip(sizeof(uint));
            string[] texts = new string[counts[4]];
            for (int i = 0; i < texts.Length; i++)
            {
                texts[i] = reader.ReadString();
            }

            uint[] numberlessNames = ReadWords(ref reader, counts[0]);
            if (numberlessNames.Any(name => name >= names.Length))
            {
                throw new InvalidDataException("A tag name is outside the name batch");
            }

            string[] numberedNames = new string[counts[1]];
            for (int i = 0; i < numberedNames.Length; i++)
            {
                numberedNames[i] = ReadName(ref reader, names);
            }

            uint[][] numberlessPaths = new uint[counts[2]][];
            for (int i = 0; i < numberlessPaths.Length; i++)
            {
                numberlessPaths[i] = ReadWords(ref reader, PathParts);
                if (numberlessPaths[i].Any(name => name >= names.Length))
                {
                    throw new InvalidDataException("A tag path names outside the name batch");
                }
            }

            string[][] numberedPaths = new string[counts[3]][];
            for (int i = 0; i < numberedPaths.Length; i++)
            {
                numberedPaths[i] = [ReadName(ref reader, names), ReadName(ref reader, names), ReadName(ref reader, names), ReadName(ref reader, names)];
            }

            uint[] ansiOffsets = ReadWords(ref reader, counts[5]);
            uint[] wideOffsets = ReadWords(ref reader, counts[6]);
            byte[] ansi = reader.ReadBytes(counts[7]).ToArray();
            byte[] wide = reader.ReadBytes(counts[8] * sizeof(char)).ToArray();
            (string, uint)[] numberlessPairs = new (string, uint)[counts[9]];
            for (int i = 0; i < numberlessPairs.Length; i++)
            {
                uint key = reader.ReadUInt32();
                numberlessPairs[i] = (key < names.Length ? names[key] : throw new InvalidDataException("A tag key is outside the name batch"), reader.ReadUInt32());
            }

            (string, uint)[] numberedPairs = new (string, uint)[counts[10]];
            for (int i = 0; i < numberedPairs.Length; i++)
            {
                numberedPairs[i] = (ReadName(ref reader, names), reader.ReadUInt32());
            }

            if (reader.ReadUInt32() != StoreEndMagic)
            {
                throw new InvalidDataException("Asset registry tag store does not end where expected");
            }

            return new Store(texts, numberlessNames, numberedNames, numberlessPaths, numberedPaths, ansiOffsets, wideOffsets, ansi, wide,
                numberlessPairs, numberedPairs);
        }

        private static uint[] ReadWords(ref ByteReader reader, int count)
        {
            if (count > reader.Remaining / sizeof(uint))
            {
                throw new InvalidDataException("Asset registry tag store is truncated");
            }

            uint[] words = new uint[count];
            for (int i = 0; i < count; i++)
            {
                words[i] = reader.ReadUInt32();
            }

            return words;
        }
    }
}
