namespace Ares.Structs.Core.Assets;

/// <summary>How a struct with a native serializer is stored instead of an unversioned property block.</summary>
public enum NativeLayout
{
    /// <summary>Stored as its own unversioned property block.</summary>
    None,

    /// <summary>Three doubles.</summary>
    Vector,

    /// <summary>Three floats.</summary>
    Vector3f,

    /// <summary>Two doubles.</summary>
    Vector2D,

    /// <summary>Three doubles: pitch, yaw, roll.</summary>
    Rotator,

    /// <summary>Four doubles: X, Y, Z, W.</summary>
    Quat,

    /// <summary>Four floats: R, G, B, A.</summary>
    LinearColor,

    /// <summary>Sixteen bytes.</summary>
    Guid,

    /// <summary>Package name, asset name, then the sub-path string.</summary>
    SoftObjectPath,

    /// <summary>A four-byte count, then that many tag names.</summary>
    GameplayTagContainer,

    /// <summary>A struct whose layout is not known: a stored value of it cannot be read.</summary>
    Unknown,
}

/// <summary>
/// The property chain of a class or struct as an unversioned property block indexes it: the members the type declares
/// first, then its super type's chain. A member array takes one index per element. Unknown members are
/// <see langword="null"/>: a block that stores a value for one cannot be read past it.
/// </summary>
public sealed class StructSchema
{
    private readonly (PropertyField? Field, int Element)[] _chain;

    /// <summary>A type declaring <paramref name="members"/> (unknown ones <see langword="null"/>) over <paramref name="super"/>.</summary>
    /// <exception cref="ArgumentException">A known member name appears twice in the chain.</exception>
    public StructSchema(string name, IReadOnlyList<PropertyField?> members, StructSchema? super = null)
        : this(name, members, super, NativeLayout.None)
    {
    }

    private StructSchema(string name, IReadOnlyList<PropertyField?> members, StructSchema? super, NativeLayout native)
    {
        Name = name;
        Members = members;
        Super = super;
        Native = native;
        List<(PropertyField?, int)> chain = [];
        foreach (PropertyField? member in members)
        {
            for (int element = 0; element < (member?.ArrayDim ?? 1); element++)
            {
                chain.Add((member, element));
            }
        }

        chain.AddRange(super?._chain ?? []);
        _chain = [.. chain];
        string? repeated = _chain.Where(entry => entry is { Field: not null, Element: 0 })
            .GroupBy(entry => entry.Field!.Name, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1)?.Key;
        if (repeated is not null)
        {
            throw new ArgumentException($"{name}: member {repeated} appears twice in the chain", nameof(members));
        }
    }

    /// <summary>The type's name, for example <c>Vector</c> or <c>ItemDataAsset</c>.</summary>
    public string Name { get; }

    /// <summary>The members the type itself declares, in chain order.</summary>
    public IReadOnlyList<PropertyField?> Members { get; }

    /// <summary>The super type, whose chain follows the declared members.</summary>
    public StructSchema? Super { get; }

    /// <summary>The native layout of a struct stored without a property block; <see cref="NativeLayout.None"/> otherwise.</summary>
    public NativeLayout Native { get; }

    /// <summary>Number of chain indices.</summary>
    public int ChainLength => _chain.Length;

    /// <summary>A struct stored natively as <paramref name="layout"/>.</summary>
    public static StructSchema NativeStruct(string name, NativeLayout layout) => new(name, [], null, layout);

    /// <summary>A struct whose layout is not known (see <see cref="NativeLayout.Unknown"/>).</summary>
    public static StructSchema UnknownStruct(string name) => new(name, [], null, NativeLayout.Unknown);

    /// <summary>The member at chain index <paramref name="index"/> and its element, or a null field when unknown or past the chain.</summary>
    public (PropertyField? Field, int Element) At(int index) => index >= 0 && index < _chain.Length ? _chain[index] : (null, 0);
}
