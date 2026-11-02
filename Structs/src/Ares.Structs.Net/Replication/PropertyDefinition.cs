namespace Ares.Structs.Net.Replication;

/// <summary>
/// One reflected property of a class, struct or function, as needed to replicate it.
/// <see cref="Path"/> is the owner's path plus the property name, e.g. <c>/Script/Engine.Actor.Role</c>.
/// </summary>
/// <param name="Path">Owner path and property name.</param>
/// <param name="Kind">Property type.</param>
/// <param name="CppType">Native type name, an input of the replication checksums.</param>
/// <param name="Flags">Property flags.</param>
public sealed record PropertyDefinition(string Path, PropertyKind Kind, string CppType, PropertyFlags Flags)
{
    /// <summary>The property name (last path segment).</summary>
    public string Name { get; } = Path[(Path.LastIndexOf('.') + 1)..];

    /// <summary>Static array size; 1 for ordinary properties.</summary>
    public int ArrayDim { get; init; } = 1;

    /// <summary>
    /// Position among the properties declared by the same owner, in the game's native property layout order (ties by name).
    /// Blueprint class checksums order their own properties by it.
    /// </summary>
    public int LayoutIndex { get; init; }

    /// <summary>Referenced type: struct, enum, or object class.</summary>
    public string? TypePath { get; init; }

    /// <summary>Required base class of a class property's value.</summary>
    public string? MetaClassPath { get; init; }

    /// <summary>Element property of an array.</summary>
    public string? Inner { get; init; }

    /// <summary>Key property of a map.</summary>
    public string? MapKey { get; init; }

    /// <summary>Value property of a map.</summary>
    public string? MapValue { get; init; }

    /// <summary>Underlying integer property of an enum property.</summary>
    public string? Underlying { get; init; }

    /// <summary>Struct flags of a struct property's type.</summary>
    public StructFlags StructFlags { get; init; }

    /// <summary>
    /// Members of a struct property's type, including inherited ones, in layout order (the order they replicate in).
    /// </summary>
    public IReadOnlyList<string> Children { get; init; } = [];
}
