using System.Buffers.Binary;
using Ares.MapData.Archives;

namespace Ares.MapData.Packages;

/// <summary>One export of a cooked package: its name, references and where its serialized bytes are in the package chunk.</summary>
/// <param name="Index">Index in the export map.</param>
/// <param name="Name">Object name.</param>
/// <param name="Outer">Outer object; null for a top-level object of the package.</param>
/// <param name="Class">Class of the object.</param>
/// <param name="Super">Super struct, for class and struct exports.</param>
/// <param name="Template">Archetype the object was created from.</param>
/// <param name="PublicExportHash">Hash other packages import this export by; zero when the export is not public.</param>
/// <param name="SerialOffset">Offset of the serialized bytes from the start of the package chunk.</param>
/// <param name="SerialSize">Length of the serialized bytes.</param>
/// <param name="ObjectFlags">Object flags.</param>
/// <param name="FilterFlags">Client/server filter flags.</param>
internal sealed record ZenExport(
    int Index,
    string Name,
    PackageObjectIndex Outer,
    PackageObjectIndex Class,
    PackageObjectIndex Super,
    PackageObjectIndex Template,
    ulong PublicExportHash,
    long SerialOffset,
    long SerialSize,
    uint ObjectFlags,
    byte FilterFlags);

/// <summary>An export bundle command: create or serialize one export.</summary>
internal readonly record struct ExportBundleEntry(int ExportIndex, uint Command);

/// <summary>
/// The header of a cooked package as stored in an I/O store container (the package chunk up to
/// <see cref="HeaderSize"/>): summary, name map, imports, export map and export bundle entries. The exports'
/// serialized bytes follow the header in the same chunk.
/// </summary>
internal sealed class ZenPackage
{
    /// <summary>Size of the fixed summary at the start of the chunk.</summary>
    public const int SummarySize = 60;

    /// <summary>Size of one stored export map entry.</summary>
    public const int ExportEntrySize = 72;

    /// <summary>Summary field offsets.</summary>
    public static class Field
    {
        public const int HasVersioningInfo = 0;
        public const int HeaderSize = 4;
        public const int NameIndex = 8;
        public const int NameNumber = 12;
        public const int PackageFlags = 16;
        public const int ImportedPublicExportHashesOffset = 24;
        public const int ImportMapOffset = 28;
        public const int ExportMapOffset = 32;
        public const int ExportBundleEntriesOffset = 36;
        public const int DependencyBundleHeadersOffset = 40;
        public const int DependencyBundleEntriesOffset = 44;
        public const int ImportedPackageNamesOffset = 48;
    }

    private const int NameIndexMask = 0x3FFFFFFF;
    private const int MaxDepth = 64;

    private ZenPackage(string name, int headerSize, uint packageFlags, string[] names, ulong[] importedPublicExportHashes,
        PackageObjectIndex[] imports, ZenExport[] exports, ExportBundleEntry[] exportBundle, string[] importedPackageNames)
    {
        Name = name;
        HeaderSize = headerSize;
        PackageFlags = packageFlags;
        Names = names;
        ImportedPublicExportHashes = importedPublicExportHashes;
        Imports = imports;
        Exports = exports;
        ExportBundle = exportBundle;
        ImportedPackageNames = importedPackageNames;
    }

    /// <summary>Package path, for example <c>/Game/Maps/Example</c>.</summary>
    public string Name { get; }

    /// <summary>Bytes of the header; the exports' serialized bytes come after it.</summary>
    public int HeaderSize { get; }

    public uint PackageFlags { get; }

    public IReadOnlyList<string> Names { get; }

    public IReadOnlyList<ulong> ImportedPublicExportHashes { get; }

    /// <summary>The import map: script imports and imports of other packages' public exports.</summary>
    public IReadOnlyList<PackageObjectIndex> Imports { get; }

    public IReadOnlyList<ZenExport> Exports { get; }

    /// <summary>Create and serialize commands, in load order.</summary>
    public IReadOnlyList<ExportBundleEntry> ExportBundle { get; }

    /// <summary>Paths of the packages this one imports from.</summary>
    public IReadOnlyList<string> ImportedPackageNames { get; }

    /// <summary>The header size stored in the first <see cref="SummarySize"/> bytes of a package chunk.</summary>
    public static int ReadHeaderSize(ReadOnlySpan<byte> summary)
    {
        if (summary.Length < SummarySize)
        {
            throw new InvalidDataException("Package chunk is shorter than its summary");
        }

        uint size = BinaryPrimitives.ReadUInt32LittleEndian(summary[Field.HeaderSize..]);
        return size is >= SummarySize and <= int.MaxValue ? (int)size : throw new InvalidDataException($"Package header size {size} is invalid");
    }

    /// <summary>Parses a package header.</summary>
    /// <param name="data">The package chunk from its start; at least the header.</param>
    /// <exception cref="InvalidDataException">The header is malformed or carries versioning information.</exception>
    public static ZenPackage Parse(ReadOnlySpan<byte> data)
    {
        int headerSize = ReadHeaderSize(data);
        if (data.Length < headerSize)
        {
            throw new InvalidDataException("Package data is shorter than its header");
        }

        ReadOnlySpan<byte> header = data[..headerSize];
        if (Summary(header, Field.HasVersioningInfo) != 0)
        {
            throw new InvalidDataException("Packages with versioning information are not supported");
        }

        int hashesOffset = Summary(header, Field.ImportedPublicExportHashesOffset);
        int importsOffset = Summary(header, Field.ImportMapOffset);
        int exportsOffset = Summary(header, Field.ExportMapOffset);
        int bundleOffset = Summary(header, Field.ExportBundleEntriesOffset);
        int dependencyOffset = Summary(header, Field.DependencyBundleHeadersOffset);
        int importedNamesOffset = Summary(header, Field.ImportedPackageNamesOffset);
        if (hashesOffset < SummarySize || importsOffset < hashesOffset || exportsOffset < importsOffset || bundleOffset < exportsOffset
            || dependencyOffset < bundleOffset || importedNamesOffset > headerSize)
        {
            throw new InvalidDataException("Package header sections are out of order");
        }

        ByteReader reader = new(header, SummarySize);
        string[] names = NameBatch.Read(ref reader);
        string name = NameOf(names, Summary(header, Field.NameIndex), BinaryPrimitives.ReadUInt32LittleEndian(header[Field.NameNumber..]));

        ulong[] hashes = new ulong[(importsOffset - hashesOffset) / sizeof(ulong)];
        for (int i = 0; i < hashes.Length; i++)
        {
            hashes[i] = BinaryPrimitives.ReadUInt64LittleEndian(header[(hashesOffset + (i * sizeof(ulong)))..]);
        }

        PackageObjectIndex[] imports = new PackageObjectIndex[(exportsOffset - importsOffset) / sizeof(ulong)];
        for (int i = 0; i < imports.Length; i++)
        {
            imports[i] = new PackageObjectIndex(BinaryPrimitives.ReadUInt64LittleEndian(header[(importsOffset + (i * sizeof(ulong)))..]));
        }

        if ((bundleOffset - exportsOffset) % ExportEntrySize != 0)
        {
            throw new InvalidDataException("Package export map is not a whole number of entries");
        }

        ZenExport[] exports = new ZenExport[(bundleOffset - exportsOffset) / ExportEntrySize];
        for (int i = 0; i < exports.Length; i++)
        {
            ByteReader entry = new(header, exportsOffset + (i * ExportEntrySize));
            ulong serialOffset = entry.ReadUInt64();
            ulong serialSize = entry.ReadUInt64();
            int nameIndex = (int)(entry.ReadUInt32() & NameIndexMask);
            uint nameNumber = entry.ReadUInt32();
            PackageObjectIndex outer = new(entry.ReadUInt64());
            PackageObjectIndex cls = new(entry.ReadUInt64());
            PackageObjectIndex super = new(entry.ReadUInt64());
            PackageObjectIndex template = new(entry.ReadUInt64());
            ulong publicHash = entry.ReadUInt64();
            uint objectFlags = entry.ReadUInt32();
            byte filterFlags = entry.ReadByte();
            if (serialOffset > long.MaxValue - (ulong)headerSize || serialSize > long.MaxValue)
            {
                throw new InvalidDataException($"Export {i} has an invalid serial range");
            }

            exports[i] = new ZenExport(i, NameOf(names, nameIndex, nameNumber), outer, cls, super, template, publicHash,
                headerSize + (long)serialOffset, (long)serialSize, objectFlags, filterFlags);
        }

        ExportBundleEntry[] bundle = new ExportBundleEntry[(dependencyOffset - bundleOffset) / (2 * sizeof(uint))];
        for (int i = 0; i < bundle.Length; i++)
        {
            ByteReader entry = new(header, bundleOffset + (i * 2 * sizeof(uint)));
            bundle[i] = new ExportBundleEntry(entry.ReadInt32(), entry.ReadUInt32());
        }

        ByteReader importedReader = new(header, importedNamesOffset);
        string[] importedBase = NameBatch.Read(ref importedReader);
        string[] importedNames = new string[importedBase.Length];
        for (int i = 0; i < importedBase.Length; i++)
        {
            importedNames[i] = NameBatch.Display(importedBase[i], importedReader.ReadUInt32());
        }

        return new ZenPackage(name, headerSize, BinaryPrimitives.ReadUInt32LittleEndian(header[Field.PackageFlags..]), names, hashes,
            imports, exports, bundle, importedNames);
    }

    /// <summary>
    /// The path of export <paramref name="index"/> inside this package, for example <c>/Game/Maps/Example.Example:PersistentLevel.Actor_1</c>.
    /// </summary>
    public string ExportPath(int index)
    {
        List<string> names = [];
        PackageObjectIndex current = PackageObjectIndex.FromExport(index);
        while (!current.IsNull)
        {
            if (current.Kind != PackageObjectKind.Export || current.ExportIndex >= Exports.Count || names.Count > MaxDepth)
            {
                throw new InvalidDataException($"Export {index} of {Name} has an invalid outer chain");
            }

            ZenExport export = Exports[current.ExportIndex];
            names.Add(export.Name);
            current = export.Outer;
        }

        names.Add(Name);
        return ObjectPath.Join(names);
    }

    private static int Summary(ReadOnlySpan<byte> header, int offset)
    {
        uint value = BinaryPrimitives.ReadUInt32LittleEndian(header[offset..]);
        return value <= int.MaxValue ? (int)value : throw new InvalidDataException("Package summary field is out of range");
    }

    private static string NameOf(string[] names, int index, uint number)
    {
        int masked = index & NameIndexMask;
        return masked < names.Length
            ? NameBatch.Display(names[masked], number)
            : throw new InvalidDataException($"Name index {masked} is outside the name map");
    }
}
