using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using Ares.MapData.Archives;
using Ares.MapData.Packages;

namespace Ares.MapData.Tests;

/// <summary>A chunk to store in a synthetic container.</summary>
internal sealed record SyntheticChunk(IoChunkId Id, byte[] Data, string? FilePath = null);

/// <summary>Builds small containers, packages and script object tables in the formats the readers parse.</summary>
internal static class SyntheticArchives
{
    public const string MountPoint = "../../../";

    public static byte[] TocKey { get; } = [.. Enumerable.Range(0, 32).Select(i => (byte)(0xA0 + i))];

    public static byte[] PakKey { get; } = [.. Enumerable.Range(0, 32).Select(i => (byte)i)];

    public static uint[] ContainerKey { get; } = [1, 2, 3, 4, 5, 6, 7, 8];

    /// <summary>Writes <c>name.utoc</c>, <c>name.ucas</c> and <c>name.ucas.meta</c> into <paramref name="directory"/>.</summary>
    public static void WriteContainer(string directory, string name, IReadOnlyList<SyntheticChunk> chunks, int blockSize = 256,
        IoContainerFlags flags = IoContainerFlags.Indexed | IoContainerFlags.Signed, byte compressionMethod = 0)
    {
        bool encrypted = flags.HasFlag(IoContainerFlags.Encrypted);
        List<(long Offset, int Size, byte[] Stored)> blocks = [];
        List<(long Offset, long Length)> locations = [];
        MemoryStream data = new();
        foreach (SyntheticChunk chunk in chunks)
        {
            locations.Add(((long)blocks.Count * blockSize, chunk.Data.Length));
            for (int at = 0; at < chunk.Data.Length; at += blockSize)
            {
                int size = Math.Min(blockSize, chunk.Data.Length - at);
                byte[] stored = new byte[(size + 15) & ~15];
                chunk.Data.AsSpan(at, size).CopyTo(stored);
                if (encrypted)
                {
                    stored = AesEcb.Encrypt(stored, PakKey);
                }

                blocks.Add((data.Length, size, stored));
                data.Write(stored);
            }
        }

        WriteWrapped(Path.Combine(directory, name + IoStoreContainer.DataExtension), data.ToArray());

        MemoryStream body = new();
        foreach (SyntheticChunk chunk in chunks)
        {
            byte[] id = new byte[IoChunkId.Size];
            chunk.Id.Write(id);
            body.Write(id);
        }

        foreach ((long offset, long length) in locations)
        {
            body.Write(UInt40BigEndian(offset));
            body.Write(UInt40BigEndian(length));
        }

        foreach ((long offset, int size, _) in blocks)
        {
            byte[] entry = new byte[IoStoreToc.BlockEntrySize];
            BinaryPrimitives.WriteInt64LittleEndian(entry, offset);
            entry[5] = (byte)size;
            entry[6] = (byte)(size >> 8);
            entry[7] = (byte)(size >> 16);
            entry[8] = (byte)size;
            entry[9] = (byte)(size >> 8);
            entry[10] = (byte)(size >> 16);
            entry[11] = compressionMethod;
            body.Write(entry);
        }

        int methodCount = compressionMethod == 0 ? 0 : 1;
        const int MethodNameLength = 32;
        body.Write(new byte[methodCount * MethodNameLength]);
        if (flags.HasFlag(IoContainerFlags.Signed))
        {
            const int SignatureSize = 8;
            body.Write(BitConverter.GetBytes(SignatureSize));
            body.Write(new byte[2 * SignatureSize]);
            foreach ((_, _, byte[] stored) in blocks)
            {
                body.Write(SHA1.HashData(stored));
            }
        }

        byte[] directoryIndex = [];
        if (flags.HasFlag(IoContainerFlags.Indexed))
        {
            directoryIndex = DirectoryIndex([.. chunks.Select((chunk, index) => (chunk.FilePath, index)).Where(file => file.FilePath is not null)
                .Select(file => (file.FilePath!, file.index))]);
            if (encrypted)
            {
                directoryIndex = AesEcb.Encrypt(Pad16(directoryIndex), PakKey);
            }
        }

        body.Write(directoryIndex);
        body.Write(new byte[chunks.Count * IoStoreToc.ChunkMetaSize]);

        byte[] header = new byte[IoStoreToc.HeaderSize];
        IoStoreToc.Magic.CopyTo(header);
        header[IoStoreToc.Field.Version] = IoStoreToc.SupportedVersion;
        WriteHeader(header, IoStoreToc.Field.TocHeaderSize, IoStoreToc.HeaderSize);
        WriteHeader(header, IoStoreToc.Field.EntryCount, chunks.Count);
        WriteHeader(header, IoStoreToc.Field.CompressionBlockCount, blocks.Count);
        WriteHeader(header, IoStoreToc.Field.CompressionBlockEntrySize, IoStoreToc.BlockEntrySize);
        WriteHeader(header, IoStoreToc.Field.CompressionMethodCount, methodCount);
        WriteHeader(header, IoStoreToc.Field.CompressionMethodNameLength, MethodNameLength);
        WriteHeader(header, IoStoreToc.Field.CompressionBlockSize, blockSize);
        WriteHeader(header, IoStoreToc.Field.DirectoryIndexSize, directoryIndex.Length);
        WriteHeader(header, IoStoreToc.Field.PartitionCount, 1);
        header[IoStoreToc.Field.ContainerFlags] = (byte)flags;
        File.WriteAllBytes(Path.Combine(directory, name + IoStoreContainer.TocExtension), [.. header, .. AesEcb.Encrypt(Pad16(body.ToArray()), TocKey)]);
    }

    /// <summary>A name batch holding <paramref name="names"/>.</summary>
    private static byte[] NameBatch(IReadOnlyList<string> names)
    {
        MemoryStream stream = new();
        stream.Write(BitConverter.GetBytes(names.Count));
        if (names.Count == 0)
        {
            return stream.ToArray();
        }

        byte[][] strings = [.. names.Select(name => Encoding.UTF8.GetBytes(name))];
        stream.Write(BitConverter.GetBytes(strings.Sum(bytes => bytes.Length)));
        stream.Write(new byte[sizeof(ulong) * (names.Count + 1)]);
        foreach (byte[] bytes in strings)
        {
            stream.WriteByte((byte)(bytes.Length >> 8));
            stream.WriteByte((byte)bytes.Length);
        }

        foreach (byte[] bytes in strings)
        {
            stream.Write(bytes);
        }

        return stream.ToArray();
    }

    /// <summary>A script objects chunk: each entry is a name, its id path and its outer's path (null for a script package).</summary>
    public static byte[] ScriptObjects(IReadOnlyList<(string Name, string Path, string? OuterPath)> objects)
    {
        MemoryStream stream = new();
        stream.Write(NameBatch([.. objects.Select(entry => entry.Name)]));
        stream.Write(BitConverter.GetBytes(objects.Count));
        for (int i = 0; i < objects.Count; i++)
        {
            (_, string path, string? outer) = objects[i];
            stream.Write(BitConverter.GetBytes(i));
            stream.Write(BitConverter.GetBytes(0));
            stream.Write(BitConverter.GetBytes(PackageObjectIndex.FromScriptPath(path).Value));
            stream.Write(BitConverter.GetBytes(outer is null ? PackageObjectIndex.Null.Value : PackageObjectIndex.FromScriptPath(outer).Value));
            stream.Write(BitConverter.GetBytes(PackageObjectIndex.Null.Value));
        }

        return stream.ToArray();
    }

    /// <summary>
    /// A cooked package chunk. Each export has a name, outer, class, public export hash and serialized bytes, and the
    /// template <paramref name="templates"/> gives it (none when not given); the package imports from
    /// <paramref name="importedPackages"/> with the given public export hashes. The name map holds the package path, the
    /// export names, then <paramref name="extraNames"/>.
    /// </summary>
    public static byte[] Package(string packagePath, IReadOnlyList<(string Name, PackageObjectIndex Outer, PackageObjectIndex Class, ulong PublicHash, byte[] Payload)> exports,
        IReadOnlyList<string>? importedPackages = null, IReadOnlyList<ulong>? importedHashes = null, IReadOnlyList<PackageObjectIndex>? imports = null,
        IReadOnlyList<string>? extraNames = null, IReadOnlyDictionary<int, PackageObjectIndex>? templates = null)
    {
        List<string> names = [packagePath, .. exports.Select(export => export.Name), .. extraNames ?? []];
        byte[] nameMap = NameBatch(names);
        byte[] hashes = [.. (importedHashes ?? []).SelectMany(BitConverter.GetBytes)];
        byte[] importMap = [.. (imports ?? []).SelectMany(index => BitConverter.GetBytes(index.Value))];
        int hashesOffset = ZenPackage.SummarySize + nameMap.Length;
        int importsOffset = hashesOffset + hashes.Length;
        int exportsOffset = importsOffset + importMap.Length;
        int bundleOffset = exportsOffset + (exports.Count * ZenPackage.ExportEntrySize);
        int dependencyOffset = bundleOffset + (exports.Count * 8);
        MemoryStream importedNames = new();
        importedNames.Write(NameBatch(importedPackages ?? []));
        for (int i = 0; i < (importedPackages?.Count ?? 0); i++)
        {
            importedNames.Write(BitConverter.GetBytes(0));
        }

        int headerSize = dependencyOffset + (int)importedNames.Length;
        byte[] summary = new byte[ZenPackage.SummarySize];
        BinaryPrimitives.WriteInt32LittleEndian(summary.AsSpan(ZenPackage.Field.HeaderSize), headerSize);
        BinaryPrimitives.WriteInt32LittleEndian(summary.AsSpan(ZenPackage.Field.ImportedPublicExportHashesOffset), hashesOffset);
        BinaryPrimitives.WriteInt32LittleEndian(summary.AsSpan(ZenPackage.Field.ImportMapOffset), importsOffset);
        BinaryPrimitives.WriteInt32LittleEndian(summary.AsSpan(ZenPackage.Field.ExportMapOffset), exportsOffset);
        BinaryPrimitives.WriteInt32LittleEndian(summary.AsSpan(ZenPackage.Field.ExportBundleEntriesOffset), bundleOffset);
        BinaryPrimitives.WriteInt32LittleEndian(summary.AsSpan(ZenPackage.Field.DependencyBundleHeadersOffset), dependencyOffset);
        BinaryPrimitives.WriteInt32LittleEndian(summary.AsSpan(ZenPackage.Field.DependencyBundleEntriesOffset), dependencyOffset);
        BinaryPrimitives.WriteInt32LittleEndian(summary.AsSpan(ZenPackage.Field.ImportedPackageNamesOffset), dependencyOffset);

        MemoryStream stream = new();
        stream.Write(summary);
        stream.Write(nameMap);
        stream.Write(hashes);
        stream.Write(importMap);
        long serial = 0;
        for (int i = 0; i < exports.Count; i++)
        {
            (_, PackageObjectIndex outer, PackageObjectIndex cls, ulong publicHash, byte[] payload) = exports[i];
            stream.Write(BitConverter.GetBytes(serial));
            stream.Write(BitConverter.GetBytes((long)payload.Length));
            stream.Write(BitConverter.GetBytes(i + 1));
            stream.Write(BitConverter.GetBytes(0));
            stream.Write(BitConverter.GetBytes(outer.Value));
            stream.Write(BitConverter.GetBytes(cls.Value));
            stream.Write(BitConverter.GetBytes(PackageObjectIndex.Null.Value));
            stream.Write(BitConverter.GetBytes((templates?.GetValueOrDefault(i, PackageObjectIndex.Null) ?? PackageObjectIndex.Null).Value));
            stream.Write(BitConverter.GetBytes(publicHash));
            stream.Write(new byte[8]);
            serial += payload.Length;
        }

        for (int i = 0; i < exports.Count; i++)
        {
            stream.Write(BitConverter.GetBytes(i));
            stream.Write(BitConverter.GetBytes(1));
        }

        stream.Write(importedNames.ToArray());
        foreach ((_, _, _, _, byte[] payload) in exports)
        {
            stream.Write(payload);
        }

        return stream.ToArray();
    }

    /// <summary>Writes <paramref name="plain"/> as a wrapped data file at <paramref name="path"/> with its metadata file.</summary>
    public static void WriteWrapped(string path, byte[] plain)
    {
        int pages = Math.Max(1, (plain.Length + TheiaFile.PageSize - 1) / TheiaFile.PageSize);
        byte[] metadata = new byte[TheiaFile.PagesOffset + (pages * TheiaFile.PageRecordSize)];
        TheiaFile.Magic.CopyTo(metadata);
        BinaryPrimitives.WriteInt64LittleEndian(metadata.AsSpan(TheiaFile.SizeOffset), plain.Length);
        Random random = new(plain.Length);
        random.NextBytes(metadata.AsSpan(TheiaFile.SeedOffset, 32));
        random.NextBytes(metadata.AsSpan(TheiaFile.PagesOffset));
        byte[] seed = new byte[64];
        metadata.AsSpan(TheiaFile.SeedOffset, 32).CopyTo(seed);
        uint[] master = TheiaCipher.Hash64(seed);
        byte[] stored = new byte[plain.Length];
        for (int page = 0; page < pages; page++)
        {
            int start = page * TheiaFile.PageSize;
            int count = Math.Min(TheiaFile.PageSize, plain.Length - start);
            uint[] pageKey = new uint[8];
            for (int i = 0; i < 8; i++)
            {
                pageKey[i] = BinaryPrimitives.ReadUInt32LittleEndian(metadata.AsSpan(TheiaFile.PagesOffset + (page * TheiaFile.PageRecordSize) + 32 + (i * 4)));
            }

            byte[] negatedStream = new byte[count];
            TheiaCipher.DecryptPage(negatedStream, 0, ContainerKey, master, pageKey);
            for (int i = 0; i < count; i++)
            {
                stored[start + i] = (byte)(plain[start + i] - negatedStream[i]);
            }
        }

        File.WriteAllBytes(path, stored);
        File.WriteAllBytes(path + TheiaFile.MetadataExtension, metadata);
    }

    private static byte[] DirectoryIndex(IReadOnlyList<(string Path, int Index)> files)
    {
        List<string> strings = [];
        List<(string Name, List<int> Children, List<(string Name, int Index)> Files)> directories = [("", [], [])];
        foreach ((string path, int index) in files)
        {
            string[] parts = path[MountPoint.Length..].Split('/');
            int current = 0;
            foreach (string part in parts[..^1])
            {
                int child = directories[current].Children.FirstOrDefault(candidate => directories[candidate].Name == part, -1);
                if (child < 0)
                {
                    child = directories.Count;
                    directories.Add((part, [], []));
                    directories[current].Children.Add(child);
                }

                current = child;
            }

            directories[current].Files.Add((parts[^1], index));
        }

        List<(uint Name, uint Next, uint Index)> fileEntries = [];
        MemoryStream stream = new();
        WriteString(stream, MountPoint);
        stream.Write(BitConverter.GetBytes(directories.Count));
        for (int i = 0; i < directories.Count; i++)
        {
            (string name, List<int> children, List<(string Name, int Index)> directoryFiles) = directories[i];
            uint firstFile = directoryFiles.Count == 0 ? IoStoreToc.NoEntry : (uint)fileEntries.Count;
            for (int f = 0; f < directoryFiles.Count; f++)
            {
                uint next = f + 1 < directoryFiles.Count ? (uint)(fileEntries.Count + 1) : IoStoreToc.NoEntry;
                fileEntries.Add(((uint)StringIndex(directoryFiles[f].Name), next, (uint)directoryFiles[f].Index));
            }

            uint sibling = IoStoreToc.NoEntry;
            foreach ((string _, List<int> otherChildren, _) in directories)
            {
                int position = otherChildren.IndexOf(i);
                if (position >= 0 && position + 1 < otherChildren.Count)
                {
                    sibling = (uint)otherChildren[position + 1];
                }
            }

            stream.Write(BitConverter.GetBytes(i == 0 ? IoStoreToc.NoEntry : (uint)StringIndex(name)));
            stream.Write(BitConverter.GetBytes(children.Count == 0 ? IoStoreToc.NoEntry : (uint)children[0]));
            stream.Write(BitConverter.GetBytes(sibling));
            stream.Write(BitConverter.GetBytes(firstFile));
        }

        stream.Write(BitConverter.GetBytes(fileEntries.Count));
        foreach ((uint name, uint next, uint index) in fileEntries)
        {
            stream.Write(BitConverter.GetBytes(name));
            stream.Write(BitConverter.GetBytes(next));
            stream.Write(BitConverter.GetBytes(index));
        }

        stream.Write(BitConverter.GetBytes(strings.Count));
        foreach (string value in strings)
        {
            WriteString(stream, value);
        }

        return stream.ToArray();

        int StringIndex(string value)
        {
            int at = strings.IndexOf(value);
            if (at >= 0)
            {
                return at;
            }

            strings.Add(value);
            return strings.Count - 1;
        }
    }

    private static void WriteString(MemoryStream stream, string value)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(value + "\0");
        stream.Write(BitConverter.GetBytes(bytes.Length));
        stream.Write(bytes);
    }

    private static void WriteHeader(byte[] header, int offset, int value)
    {
        BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(offset), value);
    }

    private static byte[] UInt40BigEndian(long value)
    {
        return [(byte)(value >> 32), (byte)(value >> 24), (byte)(value >> 16), (byte)(value >> 8), (byte)value];
    }

    private static byte[] Pad16(byte[] data)
    {
        byte[] padded = new byte[(data.Length + 15) & ~15];
        data.CopyTo(padded, 0);
        return padded;
    }
}
