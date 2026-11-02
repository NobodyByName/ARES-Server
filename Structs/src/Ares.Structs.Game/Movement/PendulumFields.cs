namespace Ares.Structs.Game.Movement;

/// <summary>
/// A Pendulum descriptor: the layout of one blackboard variable and how its changes are encoded.
/// </summary>
/// <param name="Id">Descriptor id in the registry (sent on the wire).</param>
/// <param name="Size">Size of the variable's value in bytes.</param>
/// <param name="Fields">Encoded fields, in wire order, with their default values.</param>
public sealed record PendulumDescriptor(int Id, int Size, IReadOnlyList<PendulumField> Fields);

/// <summary>Per-field flags.</summary>
[Flags]
public enum PendulumFieldFlags
{
    /// <summary>No flags.</summary>
    None = 0,

    /// <summary>Set on every field; both the client input and the server pawn streams send these fields.</summary>
    Replicated = 1,

    /// <summary>Set on some fields only. Meaning not established yet.</summary>
    Unknown1 = 2,
}

/// <summary>An encoded field of a Pendulum value.</summary>
/// <param name="Offset">Byte offset in the value.</param>
/// <param name="Flags">Field flags.</param>
public abstract record PendulumField(int Offset, PendulumFieldFlags Flags);

/// <summary>An unsigned integer sent as unchanged, a small delta or a full value.</summary>
/// <param name="Offset">Byte offset in the value.</param>
/// <param name="Width">Width in bytes.</param>
/// <param name="Maximum">Exclusive upper bound of a full value.</param>
/// <param name="Delta">Exclusive upper bound of a delta.</param>
/// <param name="Default">Default value.</param>
/// <param name="Flags">Field flags.</param>
public sealed record PendulumScalarField(int Offset, int Width, uint Maximum, uint Delta, uint Default, PendulumFieldFlags Flags)
    : PendulumField(Offset, Flags);

/// <summary>A set of bits sent with a changed bit, then coded like a bit field.</summary>
/// <param name="Offset">Byte offset in the value.</param>
/// <param name="BitCount">Number of bits (a multiple of 8).</param>
/// <param name="Default">Default bits.</param>
/// <param name="Flags">Field flags.</param>
public sealed record PendulumBitSetField(int Offset, int BitCount, ulong Default, PendulumFieldFlags Flags)
    : PendulumField(Offset, Flags);

/// <summary>Packed bools, sent as changed bits.</summary>
/// <param name="Offset">Byte offset in the value.</param>
/// <param name="BitCount">Number of bits.</param>
/// <param name="Default">Default bits.</param>
/// <param name="Flags">Field flags.</param>
public sealed record PendulumBitfieldField(int Offset, int BitCount, ulong Default, PendulumFieldFlags Flags)
    : PendulumField(Offset, Flags);

/// <summary>A single bit, always sent.</summary>
/// <param name="Offset">Byte offset in the value.</param>
/// <param name="Default">Default value.</param>
/// <param name="Flags">Field flags.</param>
public sealed record PendulumBitField(int Offset, bool Default, PendulumFieldFlags Flags) : PendulumField(Offset, Flags);

/// <summary>An integer sent with a changed bit, then as a full value.</summary>
/// <param name="Offset">Byte offset in the value.</param>
/// <param name="Width">Width in bytes.</param>
/// <param name="Maximum">Exclusive upper bound of the value.</param>
/// <param name="Default">Default value.</param>
/// <param name="Flags">Field flags.</param>
public sealed record PendulumBoundedField(int Offset, int Width, uint Maximum, uint Default, PendulumFieldFlags Flags)
    : PendulumField(Offset, Flags);

/// <summary>An object reference (network id) sent with a changed bit. Defaults to no object.</summary>
/// <param name="Offset">Byte offset in the value.</param>
/// <param name="Flags">Field flags.</param>
public sealed record PendulumObjectField(int Offset, PendulumFieldFlags Flags) : PendulumField(Offset, Flags);

/// <summary>A name sent with a changed bit. Defaults to no name.</summary>
/// <param name="Offset">Byte offset in the value.</param>
/// <param name="Flags">Field flags.</param>
public sealed record PendulumNameField(int Offset, PendulumFieldFlags Flags) : PendulumField(Offset, Flags);

/// <summary>A quantized rotation sent as its three smallest components.</summary>
/// <param name="Offset">Byte offset in the value.</param>
/// <param name="Maximum">Exclusive upper bound of a component.</param>
/// <param name="Delta">Exclusive upper bound of a component delta.</param>
/// <param name="Total">Sum of the squared quantized components.</param>
/// <param name="Default">Default value.</param>
/// <param name="Flags">Field flags.</param>
public sealed record PendulumQuaternionField(int Offset, uint Maximum, uint Delta, ulong Total, PendulumQuaternion Default, PendulumFieldFlags Flags)
    : PendulumField(Offset, Flags);

/// <summary>
/// A quantized rotation. The omitted (largest) component is not sent; it follows from <see cref="PendulumQuaternionField.Total"/>.
/// </summary>
/// <param name="Component0">Quantized component 0.</param>
/// <param name="Component1">Quantized component 1.</param>
/// <param name="Component2">Quantized component 2.</param>
/// <param name="Component3">Quantized component 3.</param>
/// <param name="UnknownBits">Four bits sent with the component order. Probably component signs.</param>
/// <param name="Smallest">Index of the smallest sent component.</param>
/// <param name="Middle">Index of the middle sent component.</param>
/// <param name="Largest">Index of the largest sent component.</param>
public sealed record PendulumQuaternion(
    uint Component0,
    uint Component1,
    uint Component2,
    uint Component3,
    byte UnknownBits,
    byte Smallest,
    byte Middle,
    byte Largest)
{
    /// <summary>Index of the component that is not sent.</summary>
    public int Omitted => 6 - Smallest - Middle - Largest;
}

/// <summary>An array of nested values, sent with an element change mask.</summary>
/// <param name="Offset">Byte offset of the first element in the value.</param>
/// <param name="Capacity">Maximum number of elements.</param>
/// <param name="Stride">Size of one element in bytes.</param>
/// <param name="CountField">Index of the scalar field holding the element count, or null when all elements are always present.</param>
/// <param name="ElementFields">Encoded fields of one element, with their default values.</param>
/// <param name="Flags">Field flags.</param>
public sealed record PendulumArrayField(
    int Offset,
    int Capacity,
    int Stride,
    int? CountField,
    IReadOnlyList<PendulumField> ElementFields,
    PendulumFieldFlags Flags) : PendulumField(Offset, Flags);

/// <summary>A variable in a blackboard frame.</summary>
/// <param name="Slot">Frame slot.</param>
/// <param name="Position">Position of the value in the frame's data, in 256-byte units.</param>
/// <param name="Size">Space the value takes in the frame's data, in 256-byte units.</param>
/// <param name="DescriptorId">Descriptor of the value.</param>
/// <param name="Values">Fields that differ from their defaults.</param>
public sealed record PendulumVariable(int Slot, int Position, int Size, int DescriptorId, IReadOnlyList<PendulumValue> Values);

/// <summary>The value of one field.</summary>
/// <param name="Field">Index of the field in its descriptor's (or array element's) field list.</param>
public abstract record PendulumValue(int Field);

/// <summary>The value of a scalar, bounded, bit, bit field, bit set or object field.</summary>
/// <param name="Field">Index of the field.</param>
/// <param name="Value">The value.</param>
public sealed record PendulumIntegerValue(int Field, ulong Value) : PendulumValue(Field);

/// <summary>The value of a quaternion field.</summary>
/// <param name="Field">Index of the field.</param>
/// <param name="Value">The value.</param>
public sealed record PendulumQuaternionValue(int Field, PendulumQuaternion Value) : PendulumValue(Field);

/// <summary>The value of an array field: the elements that differ from the element defaults.</summary>
/// <param name="Field">Index of the field.</param>
/// <param name="Elements">Changed elements.</param>
public sealed record PendulumArrayValue(int Field, IReadOnlyList<PendulumArrayElement> Elements) : PendulumValue(Field);

/// <summary>One array element.</summary>
/// <param name="Index">Element index.</param>
/// <param name="Values">Element fields that differ from their defaults.</param>
public sealed record PendulumArrayElement(int Index, IReadOnlyList<PendulumValue> Values);
