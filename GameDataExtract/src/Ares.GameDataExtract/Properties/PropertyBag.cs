using Ares.Structs.Core.Assets;

namespace Ares.GameDataExtract.Properties;

/// <summary>
/// The decoded members of a class or struct property block: only the members the block lists, by name (a member array
/// element as <c>Name[i]</c>). A member the block marks as zero holds the zero value of its type. Values are
/// <see cref="bool"/>, the integer and float types of their <see cref="PropertyKind"/>, <see cref="string"/> (names
/// and strings), <see cref="TextValue"/>, <see cref="ObjectReference"/>, <see cref="SoftObjectPath"/>,
/// <see cref="DelegateValue"/>, <see cref="PropertyBag"/> (structs), arrays and sets as <c>object[]</c>, maps as
/// <c>KeyValuePair&lt;object, object&gt;[]</c>, opaque members as <c>byte[]</c>, and native structs as described by
/// <see cref="PropertyReader"/>.
/// </summary>
internal sealed class PropertyBag
{
    private readonly Dictionary<string, object> _values;

    /// <summary>A bag of <paramref name="schema"/> holding <paramref name="values"/>.</summary>
    public PropertyBag(StructSchema schema, Dictionary<string, object> values, bool isZero = false)
    {
        Schema = schema;
        _values = values;
        IsZero = isZero;
    }

    /// <summary>The schema the block was read with (for a merged bag, the most derived one).</summary>
    private StructSchema Schema { get; }

    /// <summary>Whether this is a struct the block marks as zero as a whole (no member stored).</summary>
    public bool IsZero { get; }

    /// <summary>Names of the members held, in block order.</summary>
    public IEnumerable<string> Names => _values.Keys;

    /// <summary>Whether the bag holds member <paramref name="name"/>.</summary>
    public bool Contains(string name) => _values.ContainsKey(name);

    /// <summary>The value of member <paramref name="name"/>, or <paramref name="fallback"/> when the bag does not hold it.</summary>
    /// <exception cref="InvalidDataException">The member holds a value of another type.</exception>
    public T Get<T>(string name, T fallback) => _values.TryGetValue(name, out object? value) ? Typed<T>(name, value) : fallback;

    /// <summary>The value of member <paramref name="name"/>.</summary>
    /// <exception cref="InvalidDataException">The bag does not hold the member, or it holds a value of another type.</exception>
    public T Get<T>(string name)
    {
        return _values.TryGetValue(name, out object? value) ? Typed<T>(name, value) : throw new InvalidDataException($"{Schema.Name} does not set {name}");
    }

    /// <summary>
    /// This bag's members over <paramref name="inherited"/>'s (an archetype's): members only the archetype holds are kept,
    /// and a struct member both hold (not marked zero here) merges member by member, as a block stores a struct as its
    /// differences from the archetype's. Other values replace the archetype's whole.
    /// </summary>
    public PropertyBag Over(PropertyBag inherited)
    {
        if (IsZero)
        {
            return this;
        }

        Dictionary<string, object> merged = new(inherited._values, StringComparer.Ordinal);
        foreach ((string name, object value) in _values)
        {
            merged[name] = value is PropertyBag own && merged.GetValueOrDefault(name) is PropertyBag archetype ? own.Over(archetype) : value;
        }

        return new PropertyBag(Schema, merged);
    }

    private T Typed<T>(string name, object value)
    {
        return value is T typed ? typed : throw new InvalidDataException($"{Schema.Name}.{name} holds a {value.GetType().Name}, not a {typeof(T).Name}");
    }
}
