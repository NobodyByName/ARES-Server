using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using Ares.MapData.Archives;

namespace Ares.MapData.Tests;

/// <summary>A file to store in a synthetic pak.</summary>
internal sealed record SyntheticPakEntry(string Path, byte[] Data, bool Encrypted = false, bool Compressed = false);

/// <summary>Builds small wrapped pak files in the layout <see cref="PakFile"/> reads.</summary>
internal static class SyntheticPak
{
    private const int Version = 11;
    private const int EncryptionGuidSize = 16;
    private const int CompressionNamesSize = 5 * 32;

    /// <summary>
    /// Writes a pak at <paramref name="path"/> holding <paramref name="entries"/> (paths relative to the mount point),
    /// with its index encrypted when <paramref name="encryptIndex"/> is set.
    /// </summary>
    public static void Write(string path, string mountPoint, IReadOnlyList<SyntheticPakEntry> entries, bool encryptIndex = true)
    {
        MemoryStream file = new();
        MemoryStream encoded = new();
        Dictionary<string, List<(string Name, int Location)>> directories = new(StringComparer.Ordinal);
        foreach (SyntheticPakEntry entry in entries)
        {
            int offset = (int)file.Length;
            file.Write(new byte[PakFile.StoredEntryHeaderSize]);
            if (entry.Encrypted)
            {
                byte[] padded = new byte[(entry.Data.Length + 15) & ~15];
                entry.Data.CopyTo(padded, 0);
                file.Write(AesEcb.Encrypt(padded, SyntheticArchives.PakKey));
            }
            else
            {
                file.Write(entry.Data);
            }

            int location = (int)encoded.Length;
            uint bits = (1u << 31) | (1u << 30) | (entry.Encrypted ? 1u << 22 : 0) | (entry.Compressed ? 1u << 23 : 0);
            Write32(encoded, bits);
            Write32(encoded, (uint)offset);
            Write32(encoded, (uint)entry.Data.Length);

            int slash = entry.Path.LastIndexOf('/') + 1;
            string directory = entry.Path[..slash];
            if (!directories.TryGetValue(directory, out List<(string Name, int Location)>? files))
            {
                files = [];
                directories[directory] = files;
            }

            files.Add((entry.Path[slash..], location));
        }

        MemoryStream directoryIndex = new();
        Write32(directoryIndex, (uint)directories.Count);
        foreach ((string directory, List<(string Name, int Location)> files) in directories)
        {
            WriteString(directoryIndex, directory);
            Write32(directoryIndex, (uint)files.Count);
            foreach ((string name, int location) in files)
            {
                WriteString(directoryIndex, name);
                Write32(directoryIndex, (uint)location);
            }
        }

        (long directoryOffset, long directorySize, byte[] directoryHash) = WriteIndexPart(file, directoryIndex.ToArray(), encryptIndex);

        MemoryStream primary = new();
        WriteString(primary, mountPoint);
        Write32(primary, (uint)entries.Count);
        primary.Write(new byte[sizeof(ulong)]);
        Write32(primary, 0);
        Write32(primary, 1);
        Write64(primary, directoryOffset);
        Write64(primary, directorySize);
        primary.Write(directoryHash);
        Write32(primary, (uint)encoded.Length);
        primary.Write(encoded.ToArray());
        Write32(primary, 0);
        (long indexOffset, long indexSize, byte[] indexHash) = WriteIndexPart(file, primary.ToArray(), encryptIndex);

        file.Write(new byte[EncryptionGuidSize]);
        file.WriteByte(encryptIndex ? (byte)1 : (byte)0);
        Write32(file, PakFile.Magic);
        Write32(file, Version);
        Write64(file, indexOffset);
        Write64(file, indexSize);
        file.Write(indexHash);
        file.Write(new byte[CompressionNamesSize]);
        SyntheticArchives.WriteWrapped(path, file.ToArray());
    }

    private static (long Offset, long Size, byte[] Hash) WriteIndexPart(MemoryStream file, byte[] plain, bool encrypt)
    {
        long offset = file.Length;
        if (!encrypt)
        {
            file.Write(plain);
            return (offset, plain.Length, SHA1.HashData(plain));
        }

        byte[] padded = new byte[(plain.Length + 15) & ~15];
        plain.CopyTo(padded, 0);
        byte[] stored = PakIndexCipher.Encrypt(padded, SyntheticArchives.PakKey);
        file.Write(stored);
        return (offset, stored.Length, SHA1.HashData(padded));
    }

    private static void Write32(MemoryStream stream, uint value)
    {
        Span<byte> bytes = stackalloc byte[sizeof(uint)];
        BinaryPrimitives.WriteUInt32LittleEndian(bytes, value);
        stream.Write(bytes);
    }

    private static void Write64(MemoryStream stream, long value)
    {
        Span<byte> bytes = stackalloc byte[sizeof(long)];
        BinaryPrimitives.WriteInt64LittleEndian(bytes, value);
        stream.Write(bytes);
    }

    private static void WriteString(MemoryStream stream, string value)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(value + "\0");
        Write32(stream, (uint)bytes.Length);
        stream.Write(bytes);
    }
}
