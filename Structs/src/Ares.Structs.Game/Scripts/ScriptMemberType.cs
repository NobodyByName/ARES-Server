using Ares.Structs.Core.Assets;

namespace Ares.Structs.Game.Scripts;

/// <summary>
/// The declared type of a script member, before struct names are resolved (see <see cref="ScriptTypeSchemas"/>): how
/// it is stored (<see cref="Kind"/>), the name of an enum, struct or delegate signature with its prefix
/// (<see cref="TypeName"/>), and the element, key and value types of containers. Object references of every flavor
/// (hard, weak, class, interface) are <see cref="Object"/>; soft object and class pointers are <see cref="SoftObject"/>.
/// An unknown type (<see cref="IsUnknown"/>) is a struct whose layout is never known; <see cref="TypeName"/> names it.
/// </summary>
public sealed record ScriptMemberType
{
    private ScriptMemberType(PropertyKind kind, string? typeName = null, ScriptMemberType? inner = null, ScriptMemberType? value = null, bool isUnknown = false)
    {
        Kind = kind;
        TypeName = typeName;
        Inner = inner;
        Value = value;
        IsUnknown = isUnknown;
    }

    public static ScriptMemberType Bool { get; } = new(PropertyKind.Bool);

    public static ScriptMemberType Byte { get; } = new(PropertyKind.Byte);

    public static ScriptMemberType Int8 { get; } = new(PropertyKind.Int8);

    public static ScriptMemberType Int16 { get; } = new(PropertyKind.Int16);

    public static ScriptMemberType UInt16 { get; } = new(PropertyKind.UInt16);

    public static ScriptMemberType Int32 { get; } = new(PropertyKind.Int32);

    public static ScriptMemberType UInt32 { get; } = new(PropertyKind.UInt32);

    public static ScriptMemberType Int64 { get; } = new(PropertyKind.Int64);

    public static ScriptMemberType UInt64 { get; } = new(PropertyKind.UInt64);

    public static ScriptMemberType Float { get; } = new(PropertyKind.Float);

    public static ScriptMemberType Double { get; } = new(PropertyKind.Double);

    public static ScriptMemberType Name { get; } = new(PropertyKind.Name);

    public static ScriptMemberType String { get; } = new(PropertyKind.String);

    public static ScriptMemberType Text { get; } = new(PropertyKind.Text);

    public static ScriptMemberType Object { get; } = new(PropertyKind.Object);

    public static ScriptMemberType SoftObject { get; } = new(PropertyKind.SoftObject);

    /// <summary>The kind of storage: a primitive, name, string, text, object, soft object, enum, struct, delegate or container.</summary>
    public PropertyKind Kind { get; }

    /// <summary>Name of the enum, struct or delegate signature, or what an unknown type is; <see langword="null"/> otherwise.</summary>
    public string? TypeName { get; }

    /// <summary>Element type of an array or set, key type of a map.</summary>
    public ScriptMemberType? Inner { get; }

    /// <summary>Value type of a map.</summary>
    public ScriptMemberType? Value { get; }

    /// <summary>Whether this is a type whose stored layout is never known.</summary>
    public bool IsUnknown { get; }

    /// <summary>The enum <paramref name="name"/> (for example <c>EItemRarity</c>).</summary>
    public static ScriptMemberType Enum(string name) => new(PropertyKind.Enum, name);

    /// <summary>The struct <paramref name="name"/> (with its <c>F</c> prefix): an engine, native or script struct.</summary>
    public static ScriptMemberType Struct(string name) => new(PropertyKind.Struct, name);

    /// <summary>A delegate of script signature <paramref name="signature"/>.</summary>
    public static ScriptMemberType Delegate(string signature) => new(PropertyKind.Delegate, signature);

    /// <summary>A type whose stored layout is not known, described by <paramref name="name"/>.</summary>
    public static ScriptMemberType Unknown(string name) => new(PropertyKind.Struct, name, isUnknown: true);

    /// <summary>An array of <paramref name="element"/>.</summary>
    public static ScriptMemberType ArrayOf(ScriptMemberType element) => new(PropertyKind.Array, inner: element);

    /// <summary>A set of <paramref name="element"/>.</summary>
    public static ScriptMemberType SetOf(ScriptMemberType element) => new(PropertyKind.Set, inner: element);

    /// <summary>A map from <paramref name="key"/> to <paramref name="value"/>.</summary>
    public static ScriptMemberType MapOf(ScriptMemberType key, ScriptMemberType value) => new(PropertyKind.Map, inner: key, value: value);
}
