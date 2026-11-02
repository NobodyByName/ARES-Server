using System.Collections.Concurrent;
using Ares.Structs.Net.Replication;

namespace Ares.Raid.Replication;

/// <summary>
/// The replication schemas and the class imports of a world: makes property and function codecs for
/// what a connection has mapped, and caches property layouts.
/// </summary>
/// <param name="catalog">Replication schemas.</param>
/// <param name="enums">Enums whose wire width is known.</param>
/// <param name="mappedClasses">Exported class ids with the class they name.</param>
public sealed class ReplicationContext(ReplicationCatalog catalog, IReadOnlyList<EnumDefinition> enums, IReadOnlyDictionary<ulong, string> mappedClasses)
{
    private static readonly Dictionary<ulong, string> s_none = [];

    private readonly ConcurrentDictionary<string, PropertyLayout> _layouts = new(StringComparer.Ordinal);

    /// <summary>The replication schemas.</summary>
    public ReplicationCatalog Catalog { get; } = catalog ?? throw new ArgumentNullException(nameof(catalog));

    /// <summary>The property layout of a class.</summary>
    /// <param name="classPath">Class path.</param>
    /// <returns>The layout.</returns>
    private PropertyLayout Layout(string classPath) => _layouts.GetOrAdd(classPath, path => new PropertyLayout(Catalog, path));

    /// <summary>A property codec.</summary>
    /// <param name="objects">Object ids the client has mapped, with their classes.</param>
    /// <returns>The codec.</returns>
    private PropertyCodec Properties(IReadOnlyDictionary<ulong, string>? objects = null) => new(Values(objects));

    /// <summary>A function-call codec.</summary>
    /// <param name="objects">Object ids the client has mapped, with their classes.</param>
    /// <returns>The codec.</returns>
    public RpcCodec Calls(IReadOnlyDictionary<ulong, string>? objects = null) => new(Values(objects));

    /// <summary>Encodes property values of a class.</summary>
    /// <param name="classPath">Class path.</param>
    /// <param name="values">Values by member path.</param>
    /// <param name="objects">Object ids the client has mapped, with their classes.</param>
    /// <returns>The content block.</returns>
    public ActorContentBlock Encode(string classPath, IReadOnlyDictionary<string, object?> values, IReadOnlyDictionary<ulong, string>? objects = null) =>
        Properties(objects).Encode(Layout(classPath), values);

    private ValueCodec Values(IReadOnlyDictionary<ulong, string>? objects) => new(Catalog, enums, objects ?? s_none, mappedClasses);
}
