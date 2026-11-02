using Ares.Structs.Core.Serialization;

namespace Ares.Structs.Net.Replication;

/// <summary>Direction of a network function call.</summary>
public enum RpcDirection
{
    /// <summary>Client to server.</summary>
    Server,

    /// <summary>Server to the owning client.</summary>
    Client,

    /// <summary>Server to every client.</summary>
    Multicast,
}

/// <summary>A field of a class: a replicated property or a network function.</summary>
/// <param name="Index">Index in the class's field table (the wire index of a field payload).</param>
/// <param name="Name">Field name.</param>
/// <param name="Property">Property path, for property fields.</param>
/// <param name="Function">Function schema, for network functions.</param>
public sealed record NetField(int Index, string Name, string? Property, FunctionSchema? Function)
{
    /// <summary>Returns the field when it is a network function callable in <paramref name="direction"/>.</summary>
    /// <param name="direction">Call direction.</param>
    /// <returns>This field.</returns>
    public NetField RequireDirection(RpcDirection direction)
    {
        FunctionFlags required = direction switch
        {
            RpcDirection.Server => FunctionFlags.NetServer,
            RpcDirection.Client => FunctionFlags.NetClient,
            _ => FunctionFlags.NetMulticast,
        };
        return Function is not null && (Function.Flags & required) != 0
            ? this
            : throw new InvalidPacketException("Function direction does not match");
    }
}

/// <summary>
/// The field table of a class: for each class from the root down, its replicated properties, then its
/// network functions.
/// </summary>
public sealed class NetFieldTable
{
    private const int MaxFields = 65535;

    private readonly NetField[] _fields;

    /// <summary>Builds the table of <paramref name="classPath"/>.</summary>
    /// <param name="catalog">Replication schemas.</param>
    /// <param name="classPath">Class path.</param>
    public NetFieldTable(ReplicationCatalog catalog, string classPath)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        List<ClassSchema> chain = [];
        for (string? path = classPath; path is not null; path = catalog.Class(path).Super)
        {
            chain.Insert(0, catalog.Class(path));
        }

        List<NetField> fields = [];
        foreach (ClassSchema schema in chain)
        {
            HashSet<string> names = new(StringComparer.Ordinal);
            foreach (string property in schema.OwnProperties)
            {
                string name = catalog.Property(property).Name;
                fields.Add(names.Add(name) ? new NetField(fields.Count, name, property, null) : throw new InvalidPacketException("Duplicate field"));
            }

            foreach (FunctionSchema function in schema.Functions)
            {
                fields.Add(names.Add(function.Name) ? new NetField(fields.Count, function.Name, null, function) : throw new InvalidPacketException("Duplicate field"));
            }
        }

        _fields = fields.Count <= MaxFields ? [.. fields] : throw new InvalidPacketException("Too many fields");
    }

    /// <summary>Number of fields.</summary>
    public int Count => _fields.Length;

    /// <summary>The most derived field named <paramref name="name"/>.</summary>
    /// <param name="name">Field name.</param>
    /// <returns>The field.</returns>
    public NetField Field(string name) =>
        _fields.LastOrDefault(field => field.Name == name) ?? throw new InvalidPacketException("Unknown field");

    /// <summary>The field at <paramref name="index"/>.</summary>
    /// <param name="index">Field index.</param>
    /// <returns>The field.</returns>
    public NetField FieldAt(int index) => index >= 0 && index < _fields.Length ? _fields[index] : throw new InvalidPacketException("Unknown field index");
}
