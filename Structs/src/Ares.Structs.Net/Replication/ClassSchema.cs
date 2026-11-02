namespace Ares.Structs.Net.Replication;

/// <summary>
/// A class with initialized replication data: its own replicated properties and network functions.
/// Inherited members come from <see cref="Super"/>.
/// </summary>
/// <param name="Path">Class path, e.g. <c>/Script/Engine.Actor</c>.</param>
/// <param name="Flags">Class flags.</param>
/// <param name="Super">Parent class path; <see langword="null"/> for the root object class.</param>
/// <param name="OwnProperties">Own replicated properties, in replication-record order.</param>
/// <param name="Functions">Own network functions, in field order after the properties.</param>
/// <param name="Checksum">Network checksum of the class: the client compares it when the class is exported.</param>
public sealed record ClassSchema(
    string Path,
    ClassFlags Flags,
    string? Super,
    IReadOnlyList<string> OwnProperties,
    IReadOnlyList<FunctionSchema> Functions,
    uint Checksum)
{
    /// <summary>The class name (last path segment).</summary>
    public string Name { get; } = Path[(Path.LastIndexOf('.') + 1)..];
}

/// <summary>A network function (RPC) of a class.</summary>
/// <param name="Name">Function name.</param>
/// <param name="Flags">Function flags, including its direction.</param>
/// <param name="Parameters">Parameter property paths, in order.</param>
public sealed record FunctionSchema(string Name, FunctionFlags Flags, IReadOnlyList<string> Parameters);

/// <summary>
/// A class known only by ancestry (no replication schema), e.g. a class sent as a reference.
/// </summary>
/// <param name="Path">Class path.</param>
/// <param name="Super">Parent class path.</param>
/// <param name="ExportChecksum">Checksum the game expects when the class is exported, if verified.</param>
public sealed record ReferenceClass(string Path, string Super, uint? ExportChecksum = null);

/// <summary>One replication record of a class: a property, or one element of a static array.</summary>
/// <param name="Property">Property path.</param>
/// <param name="StaticArrayIndex">Element index; 0 for ordinary properties.</param>
public readonly record struct ReplicationRecord(string Property, int StaticArrayIndex);

/// <summary>An enum as replication needs it: the maximum value sets the bit width.</summary>
/// <param name="Path">Enum path.</param>
/// <param name="MaxValue">Maximum value used for the wire width.</param>
/// <param name="Names">Value names in value order, where known.</param>
public sealed record EnumDefinition(string Path, int MaxValue, IReadOnlyList<string> Names);
