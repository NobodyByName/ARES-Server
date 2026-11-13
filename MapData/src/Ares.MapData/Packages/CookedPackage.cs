using System.Buffers.Binary;
using Ares.Structs.Core.Geometry;

namespace Ares.MapData.Packages;

/// <summary>
/// A cooked package: its parsed header and the whole package chunk, with helpers to read property values at offsets
/// in the chunk and to find exports by outer and name or by public export hash.
/// </summary>
internal sealed class CookedPackage
{
    /// <summary>Display form of the empty name.</summary>
    public const string NoneName = "None";

    private const int NameIndexMask = 0x3FFFFFFF;
    private const uint MaxNameNumber = 10000;

    private readonly Func<byte[]> _load;
    private Dictionary<(int Outer, string Name), ZenExport>? _children;
    private Dictionary<ulong, ZenExport>? _publicExports;

    /// <summary>A package whose chunk is already in memory.</summary>
    private CookedPackage(ZenPackage header, byte[] data)
        : this(header, () => data)
    {
    }

    /// <summary>A package whose chunk <paramref name="load"/> reads on first use of <see cref="Data"/>.</summary>
    public CookedPackage(ZenPackage header, Func<byte[]> load)
    {
        Header = header;
        _load = load;
    }

    public ZenPackage Header { get; }

    /// <summary>The package chunk from its start; export offsets index into it.</summary>
    public byte[] Data => field ??= _load();

    /// <summary>Package path.</summary>
    public string Name => Header.Name;

    public IReadOnlyList<ZenExport> Exports => Header.Exports;

    /// <summary>Parses a whole package chunk.</summary>
    /// <exception cref="InvalidDataException">The header is malformed.</exception>
    public static CookedPackage Parse(byte[] data) => new(ZenPackage.Parse(data), data);

    /// <summary>Offset of the first byte after <paramref name="export"/>'s serialized bytes.</summary>
    public static int EndOf(ZenExport export) => checked((int)(export.SerialOffset + export.SerialSize));

    /// <summary>Offset of <paramref name="export"/>'s serialized bytes.</summary>
    public static int StartOf(ZenExport export) => checked((int)export.SerialOffset);

    /// <summary>The export named <paramref name="name"/> whose outer is export <paramref name="outer"/>; the last one when several share the name.</summary>
    public ZenExport? Child(int outer, string name)
    {
        _children ??= ChildrenByName(Exports);
        return _children.GetValueOrDefault((outer, name));
    }

    /// <summary>Exports whose outer is export <paramref name="outer"/>, in export order.</summary>
    public IEnumerable<ZenExport> ChildrenOf(int outer)
    {
        return Exports.Where(export => export.Outer.Kind == PackageObjectKind.Export && export.Outer.ExportIndex == outer);
    }

    /// <summary>The export with public export hash <paramref name="hash"/>, if any.</summary>
    public ZenExport? PublicExport(ulong hash)
    {
        _publicExports ??= Exports.Where(export => export.PublicExportHash != 0)
            .GroupBy(export => export.PublicExportHash)
            .ToDictionary(group => group.Key, group => group.Last());
        return _publicExports.GetValueOrDefault(hash);
    }

    /// <summary>Reads an int32 at <paramref name="at"/>.</summary>
    public int ReadInt32(int at) => BinaryPrimitives.ReadInt32LittleEndian(Bytes(at, sizeof(int)));

    /// <summary>Reads a uint32 at <paramref name="at"/>.</summary>
    public uint ReadUInt32(int at) => BinaryPrimitives.ReadUInt32LittleEndian(Bytes(at, sizeof(uint)));

    /// <summary>Reads a float at <paramref name="at"/>.</summary>
    public float ReadSingle(int at) => BinaryPrimitives.ReadSingleLittleEndian(Bytes(at, sizeof(float)));

    /// <summary>Reads a double at <paramref name="at"/>.</summary>
    public double ReadDouble(int at) => BinaryPrimitives.ReadDoubleLittleEndian(Bytes(at, sizeof(double)));

    /// <summary>Reads three doubles at <paramref name="at"/>.</summary>
    public Vector3D ReadVector(int at) => new(ReadDouble(at), ReadDouble(at + 8), ReadDouble(at + 16));

    /// <summary>Reads a rotator (pitch, yaw, roll as doubles) at <paramref name="at"/>.</summary>
    public Rotator ReadRotator(int at) => new(ReadDouble(at), ReadDouble(at + 8), ReadDouble(at + 16));

    /// <summary>Reads a byte at <paramref name="at"/>.</summary>
    public byte ReadByte(int at) => Bytes(at, 1)[0];

    /// <summary>The display form of the name (name-map index and number) stored at <paramref name="at"/>.</summary>
    /// <exception cref="InvalidDataException">The index is outside the name map.</exception>
    public string ReadName(int at)
    {
        uint index = ReadUInt32(at) & NameIndexMask;
        return index < Header.Names.Count
            ? NameBatch.Display(Header.Names[(int)index], ReadUInt32(at + 4))
            : throw new InvalidDataException($"{Name}: name index {index} at offset {at} is outside the name map");
    }

    /// <summary>Whether the eight bytes at <paramref name="at"/> look like a stored name: an index in the name map and a plausible number.</summary>
    public bool IsName(int at)
    {
        return at >= 0 && at + 8 <= Data.Length && ReadUInt32(at) < Header.Names.Count && ReadUInt32(at + 4) < MaxNameNumber;
    }

    /// <summary>Whether the int32 at <paramref name="at"/> is a reference to an export of this package.</summary>
    public bool IsExportReference(int at)
    {
        int value = ReadInt32(at);
        return value > 0 && value <= Exports.Count;
    }

    /// <summary>Whether <paramref name="value"/>, read as an object reference, refers to an import of this package.</summary>
    public bool IsImportReference(int value) => value < 0 && -(long)value <= Header.Imports.Count;

    private ReadOnlySpan<byte> Bytes(int at, int count)
    {
        return at >= 0 && at <= Data.Length - count
            ? Data.AsSpan(at, count)
            : throw new InvalidDataException($"{Name}: read of {count} bytes at offset {at} is outside the package");
    }

    /// <summary>The exports whose outer is an export, by outer and name; the last one wins when several share both.</summary>
    private static Dictionary<(int Outer, string Name), ZenExport> ChildrenByName(IReadOnlyList<ZenExport> exports)
    {
        Dictionary<(int Outer, string Name), ZenExport> children = [];
        foreach (ZenExport export in exports)
        {
            if (export.Outer.Kind == PackageObjectKind.Export)
            {
                children[(export.Outer.ExportIndex, export.Name)] = export;
            }
        }

        return children;
    }
}
