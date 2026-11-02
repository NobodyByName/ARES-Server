using Ares.Structs.Core.Serialization;

namespace Ares.Structs.Net.Replication;

/// <summary>An object id the server exports to the client before referencing it.</summary>
/// <param name="Guid">The id.</param>
public abstract record ObjectExport(ulong Guid)
{
    /// <summary>Outer object id; 0 when the object has none.</summary>
    public abstract ulong Outer { get; }

    /// <summary>Throws unless the record is consistent.</summary>
    public abstract void Validate();

    internal static bool IsPathName(string path) => path.Length != 0 && !path.Contains('\0', StringComparison.Ordinal);
}

/// <summary>A static object: a path inside its outer, and the class checksum the client compares.</summary>
/// <param name="Guid">Static id (odd, 3 or more).</param>
/// <param name="OuterGuid">Outer static id, or 0 for a package.</param>
/// <param name="Path">Object name inside the outer (or the package path).</param>
/// <param name="Checksum">Network checksum of the object's class.</param>
/// <param name="NoLoad">Whether the client must find the object instead of loading it.</param>
public sealed record StaticObjectExport(ulong Guid, ulong OuterGuid, string Path, uint Checksum, bool NoLoad) : ObjectExport(Guid)
{
    /// <inheritdoc />
    public override ulong Outer => OuterGuid;

    /// <inheritdoc />
    public override void Validate()
    {
        if (Guid < 3 || !NetGuid.IsStatic(Guid) || (OuterGuid != 0 && (OuterGuid < 3 || !NetGuid.IsStatic(OuterGuid)))
            || OuterGuid == Guid || !IsPathName(Path))
        {
            throw new InvalidPacketException("Invalid static object export");
        }
    }
}

/// <summary>A spawned actor's id, exported without a path.</summary>
/// <param name="Guid">Dynamic id (even, 2 or more).</param>
public sealed record DynamicActorExport(ulong Guid) : ObjectExport(Guid)
{
    /// <inheritdoc />
    public override ulong Outer => 0;

    /// <inheritdoc />
    public override void Validate()
    {
        if (Guid < 2 || NetGuid.IsStatic(Guid))
        {
            throw new InvalidPacketException("Invalid dynamic actor export");
        }
    }
}

/// <summary>A default subobject of a spawned actor, found by name inside it (never loaded).</summary>
/// <param name="Guid">Dynamic id (even, 2 or more).</param>
/// <param name="OuterGuid">The spawned actor's id.</param>
/// <param name="Path">Subobject name (letters, digits and underscores).</param>
/// <param name="Checksum">Network checksum of the subobject's class.</param>
public sealed record StableSubobjectExport(ulong Guid, ulong OuterGuid, string Path, uint Checksum) : ObjectExport(Guid)
{
    /// <inheritdoc />
    public override ulong Outer => OuterGuid;

    /// <inheritdoc />
    public override void Validate()
    {
        if (Guid < 2 || NetGuid.IsStatic(Guid) || OuterGuid < 2 || NetGuid.IsStatic(OuterGuid) || OuterGuid == Guid
            || Path.Length == 0 || !Path.All(c => char.IsAsciiLetterOrDigit(c) || c == '_'))
        {
            throw new InvalidPacketException("Invalid stable subobject export");
        }
    }
}

/// <summary>
/// A consistent set of object exports: every outer is exported, outer chains have no cycles, and stable
/// subobjects belong to an exported spawned actor.
/// </summary>
public sealed class ObjectExports
{
    /// <summary>Largest number of records.</summary>
    public const int MaxRecords = 4096;

    /// <summary>Largest number of roots in one export payload.</summary>
    public const int MaxRoots = 256;

    private const int MaxDepth = 32;

    private const int PathFlag = 1;
    private const int NoLoadFlag = 2;
    private const int ChecksumFlag = 4;

    private readonly Dictionary<ulong, ObjectExport> _records = [];

    /// <summary>Validates and indexes the records.</summary>
    /// <param name="records">The records; duplicates must be identical.</param>
    public ObjectExports(IEnumerable<ObjectExport> records)
    {
        ArgumentNullException.ThrowIfNull(records);
        foreach (ObjectExport record in records)
        {
            record.Validate();
            if (_records.TryGetValue(record.Guid, out ObjectExport? previous) && previous != record)
            {
                throw new InvalidPacketException("Conflicting object export");
            }

            _records[record.Guid] = record;
        }

        if (_records.Count > MaxRecords)
        {
            throw new InvalidPacketException("Too many object exports");
        }

        foreach (ObjectExport record in _records.Values)
        {
            if (record is StableSubobjectExport subobject && _records.GetValueOrDefault(subobject.OuterGuid) is not DynamicActorExport)
            {
                throw new InvalidPacketException("Stable subobject without its spawned actor");
            }

            HashSet<ulong> seen = [];
            for (ulong current = record.Guid; current != 0; current = _records[current].Outer)
            {
                if (!seen.Add(current) || seen.Count > MaxDepth)
                {
                    throw new InvalidPacketException("Object outer cycle or chain too long");
                }

                if (!_records.ContainsKey(current))
                {
                    throw new InvalidPacketException("Missing outer export");
                }
            }
        }
    }

    /// <summary>Records by id.</summary>
    public IReadOnlyDictionary<ulong, ObjectExport> Records => _records;

    /// <summary>Writes an export payload: each root with its outer chain.</summary>
    /// <param name="writer">Destination.</param>
    /// <param name="roots">Ids to export, 1 to 256.</param>
    public void Write(BitWriter writer, IReadOnlyList<ulong> roots)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(roots);
        if (roots.Count is < 1 or > MaxRoots || roots.Any(guid => !_records.ContainsKey(guid)))
        {
            throw new InvalidPacketException("Invalid export roots");
        }

        writer.WriteBit(false);
        writer.WriteBits((uint)roots.Count, 32);
        foreach (ulong guid in roots)
        {
            WriteRecord(writer, guid);
        }
    }

    /// <summary>Reads an export payload that fills the whole reader.</summary>
    /// <param name="reader">Source.</param>
    /// <returns>The records and the roots, in payload order.</returns>
    public static (ObjectExports Exports, IReadOnlyList<ulong> Roots) Read(BitReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);
        if (reader.ReadBit())
        {
            throw new InvalidPacketException("Replay field exports are not supported");
        }

        uint count = (uint)reader.ReadBits(32);
        if (count is < 1 or > MaxRoots)
        {
            throw new InvalidPacketException("Invalid export count");
        }

        RecordReader records = new(reader);
        ulong[] roots = new ulong[count];
        for (int index = 0; index < roots.Length; index++)
        {
            roots[index] = records.Read();
        }

        if (roots.Contains(0UL) || reader.Remaining != 0)
        {
            throw new InvalidPacketException("Invalid export payload boundary");
        }

        return (new ObjectExports(records.Records.Values), roots);
    }

    // Outer chains are short and acyclic (checked by the constructor), so the recursion is bounded.
    private void WriteRecord(BitWriter writer, ulong guid)
    {
        NetGuid.Write(writer, guid);
        switch (guid == 0 ? null : _records[guid])
        {
            case null:
                return;
            case DynamicActorExport:
                writer.WriteByte(NoLoadFlag | ChecksumFlag);
                return;
            case StaticObjectExport value:
                writer.WriteByte((byte)(PathFlag | ChecksumFlag | (value.NoLoad ? NoLoadFlag : 0)));
                WriteRecord(writer, value.OuterGuid);
                writer.WriteString(value.Path);
                writer.WriteBits(value.Checksum, 32);
                return;
            case StableSubobjectExport value:
                writer.WriteByte(PathFlag | ChecksumFlag | NoLoadFlag);
                WriteRecord(writer, value.OuterGuid);
                writer.WriteString(value.Path);
                writer.WriteBits(value.Checksum, 32);
                return;
        }
    }

    /// <summary>Reads nested export records, limiting their depth so crafted data cannot exhaust the stack.</summary>
    private sealed class RecordReader(BitReader reader)
    {
        private int _depth;

        public Dictionary<ulong, ObjectExport> Records { get; } = [];

        public ulong Read()
        {
            if (++_depth > MaxDepth + 1)
            {
                throw new InvalidPacketException("Export chain too long");
            }

            try
            {
                return ReadRecord();
            }
            finally
            {
                _depth--;
            }
        }

        private ulong ReadRecord()
        {
            ulong guid = NetGuid.Read(reader);
            if (guid == 0)
            {
                return 0;
            }

            byte flags = reader.ReadByte();
            ObjectExport record = flags switch
            {
                NoLoadFlag | ChecksumFlag => new DynamicActorExport(guid),
                (PathFlag | ChecksumFlag) or (PathFlag | ChecksumFlag | NoLoadFlag) => ReadPathRecord(guid, (flags & NoLoadFlag) != 0),
                _ => throw new InvalidPacketException("Expected a full export with checksum"),
            };
            record.Validate();
            if (Records.TryGetValue(guid, out ObjectExport? previous) && previous != record)
            {
                throw new InvalidPacketException("Conflicting object export");
            }

            Records[guid] = record;
            return guid;
        }

        private ObjectExport ReadPathRecord(ulong guid, bool noLoad)
        {
            ulong outer = Read();
            string path = reader.ReadString();
            uint checksum = (uint)reader.ReadBits(32);
            return NetGuid.IsStatic(guid)
                ? new StaticObjectExport(guid, outer, path, checksum, noLoad)
                : noLoad
                    ? new StableSubobjectExport(guid, outer, path, checksum)
                    : throw new InvalidPacketException("Stable subobjects are never loaded");
        }
    }
}
