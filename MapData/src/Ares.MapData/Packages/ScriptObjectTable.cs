using Ares.MapData.Archives;

namespace Ares.MapData.Packages;

/// <summary>A native script object: its name, its outer (null for a script package) and the class of its default object.</summary>
internal sealed record ScriptObject(string Name, PackageObjectIndex Id, PackageObjectIndex Outer, PackageObjectIndex DefaultObjectClass);

/// <summary>
/// The global table of native script objects (the global container's script objects chunk), keyed by global id.
/// Cooked packages refer to native classes and objects through these ids.
/// </summary>
internal sealed class ScriptObjectTable
{
    private const int NameIndexMask = 0x3FFFFFFF;
    private const int MaxEntries = 4_000_000;
    private const int MaxDepth = 64;

    private readonly Dictionary<PackageObjectIndex, ScriptObject> _objects;

    private ScriptObjectTable(Dictionary<PackageObjectIndex, ScriptObject> objects)
    {
        _objects = objects;
    }

    public int Count => _objects.Count;

    /// <summary>Parses a script objects chunk.</summary>
    /// <exception cref="InvalidDataException">The chunk is malformed.</exception>
    public static ScriptObjectTable Parse(ReadOnlySpan<byte> data)
    {
        ByteReader reader = new(data);
        string[] names = NameBatch.Read(ref reader);
        int count = reader.ReadCount(MaxEntries);
        Dictionary<PackageObjectIndex, ScriptObject> objects = new(count);
        for (int i = 0; i < count; i++)
        {
            uint nameIndex = reader.ReadUInt32() & NameIndexMask;
            uint number = reader.ReadUInt32();
            PackageObjectIndex id = new(reader.ReadUInt64());
            PackageObjectIndex outer = new(reader.ReadUInt64());
            PackageObjectIndex defaultObjectClass = new(reader.ReadUInt64());
            if (nameIndex >= names.Length)
            {
                throw new InvalidDataException($"Script object {i} has a name index out of range");
            }

            if (!objects.TryAdd(id, new ScriptObject(NameBatch.Display(names[nameIndex], number), id, outer, defaultObjectClass)))
            {
                throw new InvalidDataException($"Script object id {id} appears twice");
            }
        }

        return reader.Remaining == 0 ? new ScriptObjectTable(objects) : throw new InvalidDataException("Script objects chunk has trailing data");
    }

    /// <summary>
    /// The object path of script object <paramref name="id"/> (for example <c>/Script/Engine.StaticMeshComponent</c>),
    /// or <see langword="null"/> when the id or one of its outers is not in the table.
    /// </summary>
    public string? PathOf(PackageObjectIndex id)
    {
        List<string> names = [];
        PackageObjectIndex current = id;
        while (!current.IsNull)
        {
            if (names.Count > MaxDepth || !_objects.TryGetValue(current, out ScriptObject? entry))
            {
                return null;
            }

            names.Add(entry.Name);
            current = entry.Outer;
        }

        return ObjectPath.Join(names);
    }
}
