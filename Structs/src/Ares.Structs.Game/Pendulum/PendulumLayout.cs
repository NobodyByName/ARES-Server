using System.Buffers.Binary;
using Ares.Structs.Core.Serialization;

namespace Ares.Structs.Game.Pendulum;

/// <summary>Where Pendulum fields sit in a value buffer: extents, defaults and little-endian access.</summary>
internal static class PendulumLayout
{
    /// <summary>Bytes of a quaternion field: four 4-byte components, the unknown bits and the four order bytes.</summary>
    public const int QuaternionBytes = 21;

    public static byte[] Item(byte[] value, PendulumArrayField field, int index) =>
        value[(field.Offset + (index * field.Stride))..(field.Offset + ((index + 1) * field.Stride))];

    // Field-level comparison: bytes no field covers (padding) are never sent, so they never count as a change.
    public static bool SameFields(IReadOnlyList<PendulumField> fields, byte[] value, byte[] other) =>
        fields.All(field => field switch
        {
            PendulumBitfieldField bits => ((Get(value, bits.Offset, (bits.BitCount + 7) / 8) ^ Get(other, bits.Offset, (bits.BitCount + 7) / 8)) & PendulumMasks.Mask(bits.BitCount)) == 0,
            PendulumBitField bit => ((value[bit.Offset] ^ other[bit.Offset]) & 1) == 0,
            PendulumArrayField array => Enumerable.Range(0, array.Capacity).All(index => SameFields(array.ElementFields, Item(value, array, index), Item(other, array, index))),
            _ => value.AsSpan(field.Offset, Extent(field)).SequenceEqual(other.AsSpan(field.Offset, Extent(field))),
        });

    public static byte[] ElementDefault(PendulumArrayField field)
    {
        byte[] element = new byte[field.Stride];
        SetDefaults(field.ElementFields, element);
        return element;
    }

    public static void SetDefaults(IReadOnlyList<PendulumField> fields, byte[] buffer)
    {
        foreach (PendulumField field in fields)
        {
            switch (field)
            {
                case PendulumScalarField scalar:
                    Put(buffer, scalar.Offset, scalar.Width, scalar.Default);
                    break;
                case PendulumBitSetField set:
                    Put(buffer, set.Offset, (set.BitCount + 7) / 8, set.Default);
                    break;
                case PendulumBitfieldField bits:
                    Put(buffer, bits.Offset, (bits.BitCount + 7) / 8, bits.Default);
                    break;
                case PendulumBitField bit:
                    buffer[bit.Offset] = bit.Default ? (byte)1 : (byte)0;
                    break;
                case PendulumBoundedField bounded:
                    Put(buffer, bounded.Offset, bounded.Width, bounded.Default);
                    break;
                case PendulumQuaternionField quaternion:
                    PutQuaternion(buffer, quaternion.Offset, quaternion.Default);
                    break;
                case PendulumArrayField array:
                    byte[] element = ElementDefault(array);
                    for (int index = 0; index < array.Capacity; index++)
                    {
                        element.CopyTo(buffer, array.Offset + (index * array.Stride));
                    }

                    break;
            }
        }
    }

    public static void PutInteger(byte[] buffer, PendulumField field, ulong value)
    {
        switch (field)
        {
            case PendulumScalarField scalar:
                Put(buffer, scalar.Offset, scalar.Width, value);
                break;
            case PendulumBitSetField set:
                Put(buffer, set.Offset, (set.BitCount + 7) / 8, value);
                break;
            case PendulumBitfieldField bits:
                int width = (bits.BitCount + 7) / 8;
                ulong mask = PendulumMasks.Mask(bits.BitCount);
                Put(buffer, bits.Offset, width, (Get(buffer, bits.Offset, width) & ~mask) | (value & mask));
                break;
            case PendulumBitField bit:
                buffer[bit.Offset] = (byte)(value & 1);
                break;
            case PendulumBoundedField bounded:
                Put(buffer, bounded.Offset, bounded.Width, value);
                break;
            case PendulumObjectField objectField:
                Put(buffer, objectField.Offset, 8, value);
                break;
            default:
                throw new InvalidPacketException("Integer value for a non-integer field");
        }
    }

    public static void PutQuaternion(byte[] buffer, int at, PendulumQuaternion quaternion)
    {
        Put(buffer, at, 4, quaternion.Component0);
        Put(buffer, at + 4, 4, quaternion.Component1);
        Put(buffer, at + 8, 4, quaternion.Component2);
        Put(buffer, at + 12, 4, quaternion.Component3);
        buffer[at + 16] = quaternion.UnknownBits;
        buffer[at + 17] = quaternion.Smallest;
        buffer[at + 18] = quaternion.Middle;
        buffer[at + 19] = quaternion.Largest;
        buffer[at + 20] = (byte)quaternion.Omitted;
    }

    public static int Extent(PendulumField field) => field switch
    {
        PendulumScalarField scalar => scalar.Width,
        PendulumBoundedField bounded => bounded.Width,
        PendulumBitSetField set => (set.BitCount + 7) / 8,
        PendulumBitfieldField bits => (bits.BitCount + 7) / 8,
        PendulumBitField => 1,
        PendulumObjectField or PendulumNameField => 8,
        PendulumQuaternionField => QuaternionBytes,
        PendulumArrayField array => array.Capacity * array.Stride,
        _ => throw new InvalidPacketException("Unsupported field"),
    };

    public static ulong Get(byte[] data, int at, int width)
    {
        Span<byte> value = stackalloc byte[8];
        data.AsSpan(at, width).CopyTo(value);
        return BinaryPrimitives.ReadUInt64LittleEndian(value);
    }

    public static void Put(byte[] data, int at, int width, ulong value)
    {
        Span<byte> bytes = stackalloc byte[8];
        BinaryPrimitives.WriteUInt64LittleEndian(bytes, value);
        bytes[..width].CopyTo(data.AsSpan(at, width));
    }
}
