namespace Ares.Structs.Core.Assets;

/// <summary>How a property value is stored in a cooked unversioned property block.</summary>
public enum PropertyKind
{
    /// <summary>One byte, zero or one.</summary>
    Bool,

    /// <summary>One unsigned byte (also a byte-backed enum without its enum).</summary>
    Byte,

    /// <summary>One signed byte.</summary>
    Int8,

    /// <summary>Two-byte signed integer.</summary>
    Int16,

    /// <summary>Two-byte unsigned integer.</summary>
    UInt16,

    /// <summary>Four-byte signed integer.</summary>
    Int32,

    /// <summary>Four-byte unsigned integer.</summary>
    UInt32,

    /// <summary>Eight-byte signed integer.</summary>
    Int64,

    /// <summary>Eight-byte unsigned integer.</summary>
    UInt64,

    /// <summary>Four-byte float.</summary>
    Float,

    /// <summary>Eight-byte float.</summary>
    Double,

    /// <summary>A name: name-map index and number (eight bytes).</summary>
    Name,

    /// <summary>A length-prefixed string (positive length UTF-8, negative UTF-16), terminating zero included.</summary>
    String,

    /// <summary>
    /// Localized text: four bytes of flags, a one-byte history type, then the history (none: a four-byte flag and, when
    /// set, a culture-invariant string; base: namespace, key and source strings; string table entry: table name and key).
    /// </summary>
    Text,

    /// <summary>An object reference (object, class, weak object or interface property): a four-byte package index.</summary>
    Object,

    /// <summary>A soft object or soft class path: package name, asset name, then the sub-path string.</summary>
    SoftObject,

    /// <summary>An enum, stored as its underlying property (<see cref="PropertyType.Inner"/>).</summary>
    Enum,

    /// <summary>A four-byte element count, then the elements (<see cref="PropertyType.Inner"/>).</summary>
    Array,

    /// <summary>A four-byte count of removed elements (zero in cooked data), a four-byte count, then the elements.</summary>
    Set,

    /// <summary>A four-byte count of removed keys (zero in cooked data), a four-byte count, then key and value pairs.</summary>
    Map,

    /// <summary>A struct (<see cref="PropertyType.Struct"/>): its own unversioned block, or its native layout.</summary>
    Struct,

    /// <summary>A delegate: the bound object's package index and the function name.</summary>
    Delegate,

    /// <summary>A multicast delegate: a four-byte count, then that many delegates.</summary>
    MulticastDelegate,

    /// <summary>A member whose meaning is unknown but whose stored size is fixed (<see cref="PropertyType.Size"/> bytes).</summary>
    Opaque,
}

/// <summary>The stored type of a property: its kind and, for containers, enums and structs, what it holds.</summary>
public sealed record PropertyType
{
    private PropertyType(PropertyKind kind, PropertyType? inner = null, PropertyType? value = null, StructSchema? schema = null, int size = 0)
    {
        Kind = kind;
        Inner = inner;
        Value = value;
        Struct = schema;
        Size = size;
    }

    public static PropertyType Bool { get; } = new(PropertyKind.Bool);

    public static PropertyType Byte { get; } = new(PropertyKind.Byte);

    public static PropertyType Int8 { get; } = new(PropertyKind.Int8);

    public static PropertyType Int16 { get; } = new(PropertyKind.Int16);

    public static PropertyType UInt16 { get; } = new(PropertyKind.UInt16);

    public static PropertyType Int32 { get; } = new(PropertyKind.Int32);

    public static PropertyType UInt32 { get; } = new(PropertyKind.UInt32);

    public static PropertyType Int64 { get; } = new(PropertyKind.Int64);

    public static PropertyType UInt64 { get; } = new(PropertyKind.UInt64);

    public static PropertyType Float { get; } = new(PropertyKind.Float);

    public static PropertyType Double { get; } = new(PropertyKind.Double);

    public static PropertyType Name { get; } = new(PropertyKind.Name);

    public static PropertyType String { get; } = new(PropertyKind.String);

    public static PropertyType Text { get; } = new(PropertyKind.Text);

    public static PropertyType Object { get; } = new(PropertyKind.Object);

    public static PropertyType SoftObject { get; } = new(PropertyKind.SoftObject);

    public static PropertyType Delegate { get; } = new(PropertyKind.Delegate);

    public static PropertyType MulticastDelegate { get; } = new(PropertyKind.MulticastDelegate);

    /// <summary>An enum stored as one byte, the usual underlying type.</summary>
    public static PropertyType ByteEnum { get; } = new(PropertyKind.Enum, Byte);

    /// <summary>The kind of storage.</summary>
    public PropertyKind Kind { get; }

    /// <summary>Element type of an array or set, key type of a map, underlying type of an enum.</summary>
    public PropertyType? Inner { get; }

    /// <summary>Value type of a map.</summary>
    public PropertyType? Value { get; }

    /// <summary>The struct of a struct property.</summary>
    public StructSchema? Struct { get; }

    /// <summary>Stored size of an <see cref="PropertyKind.Opaque"/> member.</summary>
    public int Size { get; }

    /// <summary>An array of <paramref name="element"/>.</summary>
    public static PropertyType ArrayOf(PropertyType element) => new(PropertyKind.Array, element);

    /// <summary>A set of <paramref name="element"/>.</summary>
    public static PropertyType SetOf(PropertyType element) => new(PropertyKind.Set, element);

    /// <summary>A map from <paramref name="key"/> to <paramref name="value"/>.</summary>
    public static PropertyType MapOf(PropertyType key, PropertyType value) => new(PropertyKind.Map, key, value);

    /// <summary>An enum stored as <paramref name="underlying"/>.</summary>
    public static PropertyType EnumOf(PropertyType underlying) => new(PropertyKind.Enum, underlying);

    /// <summary>A struct of <paramref name="schema"/>.</summary>
    public static PropertyType StructOf(StructSchema schema) => new(PropertyKind.Struct, schema: schema);

    /// <summary>A member of unknown meaning stored in <paramref name="size"/> bytes.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="size"/> is not positive.</exception>
    public static PropertyType Opaque(int size)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(size);
        return new PropertyType(PropertyKind.Opaque, size: size);
    }
}

/// <summary>A member of a class or struct property chain.</summary>
/// <param name="Name">The member's name.</param>
/// <param name="Type">How its value is stored.</param>
/// <param name="ArrayDim">Elements of a fixed-size member array; each takes one chain index.</param>
public sealed record PropertyField(string Name, PropertyType Type, int ArrayDim = 1);
