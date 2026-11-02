using System.Buffers.Binary;
using Ares.Structs.Core.Serialization;

namespace Ares.Structs.Game.Movement;

/// <summary>A blackboard frame slot: where its value sits in the frame data, its descriptor and its value.</summary>
/// <param name="Slot">Slot index (0 to 63).</param>
/// <param name="Position">Data position in 256-byte units (255 when removed).</param>
/// <param name="Size">Data size in 256-byte units.</param>
/// <param name="DescriptorId">Descriptor id, or null when the slot has none yet.</param>
/// <param name="Value">Value bytes (the descriptor's size).</param>
public sealed record PendulumSlot(int Slot, int Position, int Size, int? DescriptorId, byte[] Value);

/// <summary>
/// Pendulum blackboard frames: slot layout, descriptors and values, each coded against the previous frame
/// (or the descriptor defaults). Writing makes the same coding choices as the game's own writer.
/// </summary>
public sealed class PendulumCodec
{
    /// <summary>Number of slots in a frame.</summary>
    public const int SlotCount = 64;

    private const int RemovedPosition = 255;
    private const int QuaternionBytes = 21;

    private readonly IReadOnlyDictionary<int, PendulumDescriptor> _descriptors;
    private readonly Dictionary<int, byte[]> _defaults = [];
    private readonly int _registryCount;
    private readonly PendulumFieldFlags _serialized;
    private readonly PendulumFieldFlags _delta;

    /// <summary>Creates a codec.</summary>
    /// <param name="registryCount">Number of descriptors in the registry (the wire range of a descriptor id).</param>
    /// <param name="descriptors">Known descriptors by id.</param>
    /// <param name="serialized">Fields whose flags intersect this are sent.</param>
    /// <param name="delta">Fields whose flags intersect this are coded against the previous frame, the others against their default.</param>
    public PendulumCodec(
        int registryCount,
        IReadOnlyDictionary<int, PendulumDescriptor> descriptors,
        PendulumFieldFlags serialized = PendulumFieldFlags.Replicated,
        PendulumFieldFlags delta = PendulumFieldFlags.Replicated)
    {
        _registryCount = registryCount;
        _descriptors = descriptors ?? throw new ArgumentNullException(nameof(descriptors));
        _serialized = serialized;
        _delta = delta;
    }

    /// <summary>The codec for the game's registry, with every field sent and coded against the previous frame.</summary>
    public static PendulumCodec Default { get; } = new(PendulumRegistry.Count, PendulumRegistry.Descriptors);

    /// <summary>The default value of a descriptor.</summary>
    /// <param name="descriptorId">Descriptor id.</param>
    /// <returns>A new copy of the default value.</returns>
    public byte[] DefaultValue(int descriptorId) => [.. DefaultOf(descriptorId)];

    /// <summary>A descriptor's value: its defaults with <paramref name="values"/> applied.</summary>
    /// <param name="descriptorId">Descriptor id.</param>
    /// <param name="values">Field values.</param>
    /// <returns>The value bytes.</returns>
    public byte[] Value(int descriptorId, IEnumerable<PendulumValue> values)
    {
        byte[] buffer = DefaultValue(descriptorId);
        Apply(Descriptor(descriptorId).Fields, buffer, values);
        return buffer;
    }

    /// <summary>Applies field values to a value buffer.</summary>
    /// <param name="fields">Fields of the value.</param>
    /// <param name="buffer">The value bytes.</param>
    /// <param name="values">Field values.</param>
    public static void Apply(IReadOnlyList<PendulumField> fields, byte[] buffer, IEnumerable<PendulumValue> values)
    {
        ArgumentNullException.ThrowIfNull(fields);
        ArgumentNullException.ThrowIfNull(values);
        foreach (PendulumValue value in values)
        {
            switch (fields[value.Field], value)
            {
                case (PendulumQuaternionField field, PendulumQuaternionValue quaternion):
                    PutQuaternion(buffer, field.Offset, quaternion.Value);
                    break;
                case (PendulumArrayField field, PendulumArrayValue array):
                    byte[] empty = ElementDefault(field);
                    foreach (PendulumArrayElement element in array.Elements)
                    {
                        byte[] item = [.. empty];
                        Apply(field.ElementFields, item, element.Values);
                        item.CopyTo(buffer, field.Offset + (element.Index * field.Stride));
                    }

                    break;
                case (var field, PendulumIntegerValue integer):
                    PutInteger(buffer, field, integer.Value);
                    break;
                default:
                    throw new InvalidPacketException("Value does not match its field");
            }
        }
    }

    /// <summary>Reads an integer field from a value buffer (the inverse of <see cref="Apply"/> for integers).</summary>
    /// <param name="field">The field.</param>
    /// <param name="buffer">The value bytes.</param>
    /// <returns>The field value.</returns>
    public static ulong Integer(PendulumField field, byte[] buffer)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        return field switch
        {
            PendulumScalarField scalar => Get(buffer, scalar.Offset, scalar.Width),
            PendulumBitSetField set => Get(buffer, set.Offset, set.BitCount / 8),
            PendulumBitfieldField bits => Get(buffer, bits.Offset, (bits.BitCount + 7) / 8) & Mask(bits.BitCount),
            PendulumBitField bit => buffer[bit.Offset] & 1UL,
            PendulumBoundedField bounded => Get(buffer, bounded.Offset, bounded.Width),
            PendulumObjectField objectField => Get(buffer, objectField.Offset, 8),
            _ => throw new InvalidPacketException("Not an integer field"),
        };
    }

    /// <summary>Reads a frame coded against <paramref name="previous"/>.</summary>
    /// <param name="reader">Source.</param>
    /// <param name="previous">The previous frame's slots by index (empty for none).</param>
    /// <returns>The frame's slots by index.</returns>
    public Dictionary<int, PendulumSlot> ReadFrame(BitReader reader, IReadOnlyDictionary<int, PendulumSlot> previous)
    {
        ArgumentNullException.ThrowIfNull(reader);
        ArgumentNullException.ThrowIfNull(previous);
        Dictionary<int, PendulumSlot> slots = new(previous);
        ulong layout = reader.ReadBit() ? ReadSparseMask(reader) : 0;
        if (layout != 0)
        {
            ulong removed = ReadSparseMask(reader);
            for (int slot = 0; slot < SlotCount; slot++)
            {
                if (!IsSet(layout, slot))
                {
                    continue;
                }

                int position = IsSet(removed, slot) ? RemovedPosition : (int)reader.ReadBounded(64);
                int size = (int)reader.ReadBounded(63);
                PendulumSlot old = slots.GetValueOrDefault(slot) ?? new PendulumSlot(slot, RemovedPosition, 0, null, []);
                slots[slot] = old with { Position = position, Size = size };
            }
        }

        ulong descriptors = reader.ReadBit() ? ReadSparseMask(reader) : 0;
        ulong fresh = 0;
        for (int slot = 0; slot < SlotCount; slot++)
        {
            if (!IsSet(descriptors, slot))
            {
                continue;
            }

            PendulumSlot old = slots.GetValueOrDefault(slot) ?? new PendulumSlot(slot, RemovedPosition, 0, null, []);
            if (!reader.ReadBit())
            {
                slots.Remove(old.Position == RemovedPosition ? slot : throw new InvalidPacketException("Live slot without a descriptor"));
                continue;
            }

            int descriptorId = (int)reader.ReadBounded((ulong)_registryCount);
            slots[slot] = old with { DescriptorId = descriptorId, Value = DefaultValue(descriptorId) };
            fresh |= 1UL << slot;
        }

        ulong changed = ReadSparseMask(reader);
        for (int slot = 0; slot < SlotCount; slot++)
        {
            if (!IsSet(fresh | changed, slot))
            {
                continue;
            }

            PendulumSlot value = slots.GetValueOrDefault(slot) ?? throw new InvalidPacketException("Invalid slot");
            if (value.DescriptorId is not { } id || value.Position == RemovedPosition || value.Size == 0 || value.Position + value.Size > SlotCount)
            {
                throw new InvalidPacketException("Invalid slot");
            }

            byte[] baseline = IsSet(fresh | layout, slot) ? DefaultValue(id) : value.Value;
            slots[slot] = value with { Value = ReadValue(reader, id, baseline) };
        }

        return slots;
    }

    /// <summary>Writes <paramref name="slots"/> coded against <paramref name="previous"/>.</summary>
    /// <param name="writer">Destination.</param>
    /// <param name="slots">The frame's slots.</param>
    /// <param name="previous">The previous frame's slots by index (empty for none).</param>
    public void WriteFrame(BitWriter writer, IEnumerable<PendulumSlot> slots, IReadOnlyDictionary<int, PendulumSlot> previous)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(previous);
        Dictionary<int, PendulumSlot> frame = (slots ?? throw new ArgumentNullException(nameof(slots))).ToDictionary(slot => slot.Slot);
        ulong layout = 0;
        ulong descriptors = 0;
        foreach (PendulumSlot slot in frame.Values)
        {
            PendulumSlot? old = previous.GetValueOrDefault(slot.Slot);
            if ((old?.Position ?? RemovedPosition, old?.Size ?? 0) != (slot.Position, slot.Size))
            {
                layout |= 1UL << slot.Slot;
            }

            if (old?.DescriptorId != slot.DescriptorId)
            {
                descriptors |= 1UL << slot.Slot;
            }
        }

        writer.WriteBit(layout != 0);
        if (layout != 0)
        {
            WriteSparseMask(writer, layout);
            WriteSparseMask(writer, 0);
            foreach (PendulumSlot slot in Ordered(frame, layout))
            {
                writer.WriteBounded((uint)slot.Position, 64);
                writer.WriteBounded((uint)slot.Size, 63);
            }
        }

        writer.WriteBit(descriptors != 0);
        if (descriptors != 0)
        {
            WriteSparseMask(writer, descriptors);
            foreach (PendulumSlot slot in Ordered(frame, descriptors))
            {
                writer.WriteBit(true);
                writer.WriteBounded((uint)(slot.DescriptorId ?? throw new InvalidPacketException("Slot without a descriptor")), (ulong)_registryCount);
            }
        }

        // New descriptors and changed slots are sent; a new descriptor or layout restarts from the default.
        ulong changed = frame.Values
            .Where(slot => !IsSet(descriptors, slot.Slot) && (IsSet(layout, slot.Slot) || !previous[slot.Slot].Value.AsSpan().SequenceEqual(slot.Value)))
            .Aggregate(0UL, (mask, slot) => mask | (1UL << slot.Slot));

        WriteSparseMask(writer, changed);
        foreach (PendulumSlot slot in Ordered(frame, descriptors | changed))
        {
            int id = slot.DescriptorId ?? throw new InvalidPacketException("Slot without a descriptor");
            byte[] baseline = IsSet(descriptors | layout, slot.Slot) ? DefaultOf(id) : previous[slot.Slot].Value;
            WriteValue(writer, id, slot.Value, baseline);
        }
    }

    /// <summary>Reads one value coded against <paramref name="previous"/>.</summary>
    /// <param name="reader">Source.</param>
    /// <param name="descriptorId">Descriptor id.</param>
    /// <param name="previous">Previous value.</param>
    /// <returns>The value.</returns>
    public byte[] ReadValue(BitReader reader, int descriptorId, byte[] previous) =>
        ReadFields(reader, Descriptor(descriptorId).Fields, previous, DefaultOf(descriptorId));

    /// <summary>Writes one value coded against <paramref name="previous"/>.</summary>
    /// <param name="writer">Destination.</param>
    /// <param name="descriptorId">Descriptor id.</param>
    /// <param name="value">Value.</param>
    /// <param name="previous">Previous value.</param>
    public void WriteValue(BitWriter writer, int descriptorId, byte[] value, byte[] previous) =>
        WriteFields(writer, Descriptor(descriptorId).Fields, value, previous, DefaultOf(descriptorId));

    private PendulumDescriptor Descriptor(int id) =>
        _descriptors.TryGetValue(id, out PendulumDescriptor? descriptor) ? descriptor : throw new InvalidPacketException("Unknown descriptor");

    private byte[] DefaultOf(int id)
    {
        if (_defaults.TryGetValue(id, out byte[]? value))
        {
            return value;
        }

        PendulumDescriptor descriptor = Descriptor(id);
        value = new byte[descriptor.Size];
        SetDefaults(descriptor.Fields, value);
        _defaults[id] = value;
        return value;
    }

    private static IEnumerable<PendulumSlot> Ordered(Dictionary<int, PendulumSlot> frame, ulong mask) =>
        frame.Values.Where(slot => IsSet(mask, slot.Slot)).OrderBy(slot => slot.Slot);

    private static bool IsSet(ulong mask, int bit) => ((mask >> bit) & 1) != 0;

    // Values.

    private bool Sent(PendulumField field) => (field.Flags & _serialized) != 0;

    private byte[] BaselineOf(PendulumField field, byte[] previous, byte[] defaults) => (field.Flags & _delta) != 0 ? previous : defaults;

    private byte[] ReadFields(BitReader reader, IReadOnlyList<PendulumField> fields, byte[] previous, byte[] defaults)
    {
        byte[] data = [.. previous];
        foreach (PendulumField field in fields.Where(Sent))
        {
            byte[] baseline = BaselineOf(field, previous, defaults);
            Array.Copy(baseline, field.Offset, data, field.Offset, Extent(field));
            switch (field)
            {
                case PendulumScalarField scalar:
                    Put(data, scalar.Offset, scalar.Width, ReadScalar(reader, Get(data, scalar.Offset, scalar.Width), scalar.Maximum, scalar.Delta, true));
                    break;
                case PendulumBitSetField set:
                    if (reader.ReadBit())
                    {
                        Put(data, set.Offset, set.BitCount / 8, ReadBitfield(reader, Get(data, set.Offset, set.BitCount / 8), set.BitCount));
                    }

                    break;
                case PendulumBitfieldField bits:
                    int width = (bits.BitCount + 7) / 8;
                    ulong mask = Mask(bits.BitCount);
                    ulong stored = Get(data, bits.Offset, width);
                    Put(data, bits.Offset, width, (stored & ~mask) | (ReadBitfield(reader, stored, bits.BitCount) & mask));
                    break;
                case PendulumBitField bit:
                    data[bit.Offset] = reader.ReadBit() ? (byte)1 : (byte)0;
                    break;
                case PendulumBoundedField bounded:
                    if (reader.ReadBit())
                    {
                        Put(data, bounded.Offset, bounded.Width, reader.ReadBounded(bounded.Maximum));
                    }

                    break;
                case PendulumObjectField objectField:
                    if (reader.ReadBit())
                    {
                        Put(data, objectField.Offset, 8, reader.ReadPacked());
                    }

                    break;
                case PendulumNameField:
                    if (reader.ReadBit())
                    {
                        throw new InvalidPacketException("Changed name values are not supported");
                    }

                    break;
                case PendulumQuaternionField quaternion:
                    ReadQuaternion(reader, data, quaternion);
                    break;
                case PendulumArrayField array:
                    ReadArray(reader, data, array, baseline, fields);
                    break;
                default:
                    throw new InvalidPacketException("Unsupported field");
            }
        }

        return data;
    }

    private void WriteFields(BitWriter writer, IReadOnlyList<PendulumField> fields, byte[] value, byte[] previous, byte[] defaults)
    {
        foreach (PendulumField field in fields.Where(Sent))
        {
            byte[] baseline = BaselineOf(field, previous, defaults);
            switch (field)
            {
                case PendulumScalarField scalar:
                    WriteScalar(writer, Get(value, scalar.Offset, scalar.Width), Get(baseline, scalar.Offset, scalar.Width), scalar.Maximum, scalar.Delta, true);
                    break;
                case PendulumBitSetField set:
                    ulong newSet = Get(value, set.Offset, set.BitCount / 8);
                    ulong oldSet = Get(baseline, set.Offset, set.BitCount / 8);
                    writer.WriteBit(newSet != oldSet);
                    if (newSet != oldSet)
                    {
                        WriteBitfield(writer, newSet, oldSet, set.BitCount);
                    }

                    break;
                case PendulumBitfieldField bits:
                    int width = (bits.BitCount + 7) / 8;
                    WriteBitfield(writer, Get(value, bits.Offset, width), Get(baseline, bits.Offset, width), bits.BitCount);
                    break;
                case PendulumBitField bit:
                    writer.WriteBit((value[bit.Offset] & 1) != 0);
                    break;
                case PendulumBoundedField bounded:
                    ulong newBounded = Get(value, bounded.Offset, bounded.Width);
                    writer.WriteBit(newBounded != Get(baseline, bounded.Offset, bounded.Width));
                    if (newBounded != Get(baseline, bounded.Offset, bounded.Width))
                    {
                        writer.WriteBounded((uint)newBounded, bounded.Maximum);
                    }

                    break;
                case PendulumObjectField objectField:
                    ulong newObject = Get(value, objectField.Offset, 8);
                    writer.WriteBit(newObject != Get(baseline, objectField.Offset, 8));
                    if (newObject != Get(baseline, objectField.Offset, 8))
                    {
                        writer.WritePacked(checked((uint)newObject));
                    }

                    break;
                case PendulumNameField name:
                    writer.WriteBit(Get(value, name.Offset, 8) == Get(baseline, name.Offset, 8)
                        ? false
                        : throw new InvalidPacketException("Changed name values are not supported"));
                    break;
                case PendulumQuaternionField quaternion:
                    WriteQuaternion(writer, value, baseline, quaternion);
                    break;
                case PendulumArrayField array:
                    WriteArray(writer, value, baseline, array, fields);
                    break;
                default:
                    throw new InvalidPacketException("Unsupported field");
            }
        }
    }

    private static ulong ReadScalar(BitReader reader, ulong previous, uint maximum, uint delta, bool unchanged)
    {
        if (unchanged && reader.ReadBit())
        {
            return previous;
        }

        if (!reader.ReadBit())
        {
            return reader.ReadBounded(maximum);
        }

        bool positive = reader.ReadBit();
        long change = reader.ReadBounded(delta);
        return (ulong)((long)previous + (positive ? change : -change));
    }

    private static void WriteScalar(BitWriter writer, ulong value, ulong previous, uint maximum, uint delta, bool unchanged)
    {
        if (unchanged)
        {
            writer.WriteBit(value == previous);
            if (value == previous)
            {
                return;
            }
        }

        long change = (long)value - (long)previous;
        // Quaternion components with a delta limit of 1 never use the delta coding.
        bool useDelta = Math.Abs(change) < delta && (unchanged || delta > 1);
        writer.WriteBit(useDelta);
        if (useDelta)
        {
            writer.WriteBit(change > 0);
            writer.WriteBounded((uint)Math.Abs(change), delta);
        }
        else
        {
            writer.WriteBounded(checked((uint)value), maximum);
        }
    }

    // Bit fields: a byte coding (each byte unchanged, or its new bits) or a run coding (the positions of
    // the changed or unchanged bits). The writer uses runs when few or almost all bits change.

    private static ulong ReadBitfield(BitReader reader, ulong previous, int count)
    {
        if (!reader.ReadBit())
        {
            bool inverted = reader.ReadBit();
            return previous ^ ToUlong(ReadRuns(reader, count, inverted));
        }

        ulong result = previous;
        for (int offset = 0; offset < count; offset += 8)
        {
            int width = Math.Min(8, count - offset);
            if (!reader.ReadBit())
            {
                result = (result & ~(Mask(width) << offset)) | (reader.ReadBits(width) << offset);
            }
        }

        return result;
    }

    private static void WriteBitfield(BitWriter writer, ulong value, ulong previous, int count)
    {
        ulong mask = Mask(count);
        value &= mask;
        previous &= mask;
        bool[] changes = FromUlong(value ^ previous, count);
        if (UsesRuns(changes))
        {
            writer.WriteBit(false);
            WriteRuns(writer, changes);
            return;
        }

        writer.WriteBit(true);
        WriteBytes(writer, FromUlong(value, count), FromUlong(previous, count));
    }

    private static bool[] ReadArrayMask(BitReader reader, int count)
    {
        bool inverted = reader.ReadBit();
        if (!reader.ReadBit())
        {
            return ReadRuns(reader, count, inverted);
        }

        bool[] result = new bool[count];
        Array.Fill(result, !inverted);
        for (int offset = 0; offset < count; offset += 8)
        {
            int width = Math.Min(8, count - offset);
            if (reader.ReadBit())
            {
                continue;
            }

            ulong bits = reader.ReadBits(width);
            for (int index = 0; index < width; index++)
            {
                result[offset + index] = ((bits >> index) & 1) != 0;
            }
        }

        return result;
    }

    private static void WriteArrayMask(BitWriter writer, bool[] changed)
    {
        bool inverted = Inverted(changed);
        writer.WriteBit(inverted);
        if (UsesRuns(changed))
        {
            writer.WriteBit(false);
            WriteRunPositions(writer, changed, inverted);
            return;
        }

        writer.WriteBit(true);
        bool[] baseline = new bool[changed.Length];
        Array.Fill(baseline, !inverted);
        WriteBytes(writer, changed, baseline);
    }

    private static bool[] ReadRuns(BitReader reader, int count, bool inverted)
    {
        bool[] result = new bool[count];
        Array.Fill(result, !inverted);
        int position = 0;
        for (int step = 0; step <= count; step++)
        {
            position += (int)reader.ReadBounded((ulong)(count - position + 1));
            if (position == count)
            {
                return result;
            }

            result[position] = !result[position];
        }

        throw new InvalidPacketException("Invalid bit runs");
    }

    private static void WriteRuns(BitWriter writer, bool[] changes)
    {
        bool inverted = Inverted(changes);
        writer.WriteBit(inverted);
        WriteRunPositions(writer, changes, inverted);
    }

    private static void WriteRunPositions(BitWriter writer, bool[] changes, bool inverted)
    {
        int count = changes.Length;
        int position = 0;
        for (int index = 0; index < count; index++)
        {
            if (changes[index] != inverted)
            {
                continue;
            }

            writer.WriteBounded((uint)(index - position), (ulong)(count - position + 1));
            position = index;
        }

        writer.WriteBounded((uint)(count - position), (ulong)(count - position + 1));
    }

    private static void WriteBytes(BitWriter writer, bool[] value, bool[] baseline)
    {
        for (int offset = 0; offset < value.Length; offset += 8)
        {
            int width = Math.Min(8, value.Length - offset);
            bool same = value.AsSpan(offset, width).SequenceEqual(baseline.AsSpan(offset, width));
            writer.WriteBit(same);
            if (!same)
            {
                writer.WriteBits(ToUlong(value.AsSpan(offset, width)), width);
            }
        }
    }

    // Fewer than half the bits changed: list the changed ones; otherwise the unchanged ones.
    private static bool Inverted(bool[] changes) => changes.Count(change => change) < changes.Length / 2;

    private static bool UsesRuns(bool[] changes)
    {
        int changed = changes.Count(change => change);
        return changed <= changes.Length / 8 || changed >= 7 * changes.Length / 8;
    }

    // Quaternions: the three smallest components, the largest first; later components are bounded by
    // what the earlier ones leave of the total.

    private static void ReadQuaternion(BitReader reader, byte[] data, PendulumQuaternionField field)
    {
        int at = field.Offset;
        if (!reader.ReadBit())
        {
            return;
        }

        int oldOmitted = data[at + 20];
        if (reader.ReadBit())
        {
            data[at + 16] = (byte)reader.ReadBits(4);
            int smallest = (int)reader.ReadBounded(4);
            int middle = (int)reader.ReadBounded(4);
            int largest = (int)reader.ReadBounded(4);
            if (smallest == middle || middle == largest || smallest == largest)
            {
                throw new InvalidPacketException("Invalid quaternion order");
            }

            data[at + 17] = (byte)smallest;
            data[at + 18] = (byte)middle;
            data[at + 19] = (byte)largest;
            data[at + 20] = (byte)(6 - smallest - middle - largest);
        }

        int omitted = data[at + 20];
        if (omitted != oldOmitted)
        {
            Put(data, at + (4 * oldOmitted), 4, ReadScalar(reader, field.Maximum / 2, field.Maximum, field.Delta, true));
            for (int index = 0; index < 4; index++)
            {
                if (index != omitted && index != oldOmitted)
                {
                    Put(data, at + (4 * index), 4, ReadScalar(reader, Get(data, at + (4 * index), 4), field.Maximum, field.Delta, true));
                }
            }

            return;
        }

        int[] order = [data[at + 19], data[at + 18], data[at + 17]];
        ulong first = ReadScalar(reader, Get(data, at + (4 * order[0]), 4), field.Maximum, field.Delta, false);
        Put(data, at + (4 * order[0]), 4, first);
        long remaining = (long)field.Total - (2 * (long)first * (long)first);
        uint bound = Limit(remaining, first);
        ulong second = bound >= 2 ? ReadScalar(reader, Get(data, at + (4 * order[1]), 4), bound, bound / 16, false) : 0;
        Put(data, at + (4 * order[1]), 4, second);
        bound = Limit(remaining - ((long)second * (long)second), second);
        ulong third = bound >= 2 ? ReadScalar(reader, Get(data, at + (4 * order[2]), 4), bound, bound / 16, false) : 0;
        Put(data, at + (4 * order[2]), 4, third);
    }

    private static void WriteQuaternion(BitWriter writer, byte[] value, byte[] baseline, PendulumQuaternionField field)
    {
        int at = field.Offset;
        ReadOnlySpan<byte> current = value.AsSpan(at, QuaternionBytes);
        ReadOnlySpan<byte> old = baseline.AsSpan(at, QuaternionBytes);
        writer.WriteBit(!current.SequenceEqual(old));
        if (current.SequenceEqual(old))
        {
            return;
        }

        bool reordered = !current[16..].SequenceEqual(old[16..]);
        writer.WriteBit(reordered);
        if (reordered)
        {
            writer.WriteBits(current[16], 4);
            writer.WriteBounded(current[17], 4);
            writer.WriteBounded(current[18], 4);
            writer.WriteBounded(current[19], 4);
        }

        int omitted = current[20];
        int oldOmitted = old[20];
        if (omitted != oldOmitted)
        {
            WriteScalar(writer, Get(value, at + (4 * oldOmitted), 4), field.Maximum / 2, field.Maximum, field.Delta, true);
            for (int index = 0; index < 4; index++)
            {
                if (index != omitted && index != oldOmitted)
                {
                    WriteScalar(writer, Get(value, at + (4 * index), 4), Get(baseline, at + (4 * index), 4), field.Maximum, field.Delta, true);
                }
            }

            return;
        }

        int[] order = [current[19], current[18], current[17]];
        ulong first = Get(value, at + (4 * order[0]), 4);
        WriteScalar(writer, first, Get(baseline, at + (4 * order[0]), 4), field.Maximum, field.Delta, false);
        long remaining = (long)field.Total - (2 * (long)first * (long)first);
        uint bound = Limit(remaining, first);
        ulong second = Get(value, at + (4 * order[1]), 4);
        if (bound >= 2)
        {
            WriteScalar(writer, second, Get(baseline, at + (4 * order[1]), 4), bound, bound / 16, false);
        }

        bound = Limit(remaining - ((long)second * (long)second), second);
        if (bound >= 2)
        {
            WriteScalar(writer, Get(value, at + (4 * order[2]), 4), Get(baseline, at + (4 * order[2]), 4), bound, bound / 16, false);
        }
    }

    private static uint Limit(long remaining, ulong preceding)
    {
        long value = Math.Max(remaining, 1) - 1;
        int length = value == 0 ? 0 : 64 - System.Numerics.BitOperations.LeadingZeroCount((ulong)value);
        int power = (length + 1) / 2;
        return (uint)Math.Min(1UL << power, preceding) + 1;
    }

    // Arrays: an element mask over max(old, new count); added elements always follow, against the default.

    private void ReadArray(BitReader reader, byte[] data, PendulumArrayField field, byte[] baseline, IReadOnlyList<PendulumField> siblings)
    {
        (int count, int oldCount) = Counts(field, data, baseline, siblings);
        byte[] empty = ElementDefault(field);
        bool[] changed = ReadArrayMask(reader, Math.Max(count, oldCount));
        for (int index = 0; index < changed.Length; index++)
        {
            int at = field.Offset + (index * field.Stride);
            byte[]? item = changed[index]
                ? ReadFields(reader, field.ElementFields, baseline[at..(at + field.Stride)], empty)
                : index >= oldCount && index < count
                    ? ReadFields(reader, field.ElementFields, empty, empty)
                    : index >= count && index < oldCount ? empty : null;
            item?.CopyTo(data, at);
        }
    }

    private void WriteArray(BitWriter writer, byte[] value, byte[] baseline, PendulumArrayField field, IReadOnlyList<PendulumField> siblings)
    {
        (int count, int oldCount) = Counts(field, value, baseline, siblings);
        byte[] empty = ElementDefault(field);
        bool[] changed = new bool[Math.Max(count, oldCount)];
        for (int index = 0; index < Math.Min(count, oldCount); index++)
        {
            changed[index] = !SameFields(field.ElementFields, Item(value, field, index), Item(baseline, field, index));
        }

        WriteArrayMask(writer, changed);
        for (int index = 0; index < changed.Length; index++)
        {
            if (changed[index])
            {
                WriteFields(writer, field.ElementFields, Item(value, field, index), Item(baseline, field, index), empty);
            }
            else if (index >= oldCount && index < count)
            {
                WriteFields(writer, field.ElementFields, Item(value, field, index), empty, empty);
            }
        }
    }

    private static byte[] Item(byte[] value, PendulumArrayField field, int index) =>
        value[(field.Offset + (index * field.Stride))..(field.Offset + ((index + 1) * field.Stride))];

    private static (int Count, int OldCount) Counts(PendulumArrayField field, byte[] value, byte[] baseline, IReadOnlyList<PendulumField> siblings)
    {
        if (field.CountField is not { } index)
        {
            return (field.Capacity, field.Capacity);
        }

        PendulumScalarField counter = siblings[index] as PendulumScalarField ?? throw new InvalidPacketException("Invalid array count field");
        ulong count = Get(value, counter.Offset, counter.Width);
        ulong oldCount = Get(baseline, counter.Offset, counter.Width);
        return count <= (ulong)field.Capacity && oldCount <= (ulong)field.Capacity
            ? ((int)count, (int)oldCount)
            : throw new InvalidPacketException("Array count above its capacity");
    }

    // Field-level comparison: bytes no field covers (padding) are never sent, so they never count as a change.
    private static bool SameFields(IReadOnlyList<PendulumField> fields, byte[] value, byte[] other) =>
        fields.All(field => field switch
        {
            PendulumBitfieldField bits => ((Get(value, bits.Offset, (bits.BitCount + 7) / 8) ^ Get(other, bits.Offset, (bits.BitCount + 7) / 8)) & Mask(bits.BitCount)) == 0,
            PendulumBitField bit => ((value[bit.Offset] ^ other[bit.Offset]) & 1) == 0,
            PendulumArrayField array => Enumerable.Range(0, array.Capacity).All(index => SameFields(array.ElementFields, Item(value, array, index), Item(other, array, index))),
            _ => value.AsSpan(field.Offset, Extent(field)).SequenceEqual(other.AsSpan(field.Offset, Extent(field))),
        });

    // Defaults and raw access.

    private static byte[] ElementDefault(PendulumArrayField field)
    {
        byte[] element = new byte[field.Stride];
        SetDefaults(field.ElementFields, element);
        return element;
    }

    private static void SetDefaults(IReadOnlyList<PendulumField> fields, byte[] buffer)
    {
        foreach (PendulumField field in fields)
        {
            switch (field)
            {
                case PendulumScalarField scalar:
                    Put(buffer, scalar.Offset, scalar.Width, scalar.Default);
                    break;
                case PendulumBitSetField set:
                    Put(buffer, set.Offset, set.BitCount / 8, set.Default);
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

    private static void PutInteger(byte[] buffer, PendulumField field, ulong value)
    {
        switch (field)
        {
            case PendulumScalarField scalar:
                Put(buffer, scalar.Offset, scalar.Width, value);
                break;
            case PendulumBitSetField set:
                Put(buffer, set.Offset, set.BitCount / 8, value);
                break;
            case PendulumBitfieldField bits:
                int width = (bits.BitCount + 7) / 8;
                ulong mask = Mask(bits.BitCount);
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

    private static void PutQuaternion(byte[] buffer, int at, PendulumQuaternion quaternion)
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

    private static int Extent(PendulumField field) => field switch
    {
        PendulumScalarField scalar => scalar.Width,
        PendulumBoundedField bounded => bounded.Width,
        PendulumBitSetField set => set.BitCount / 8,
        PendulumBitfieldField bits => (bits.BitCount + 7) / 8,
        PendulumBitField => 1,
        PendulumObjectField or PendulumNameField => 8,
        PendulumQuaternionField => QuaternionBytes,
        PendulumArrayField array => array.Capacity * array.Stride,
        _ => throw new InvalidPacketException("Unsupported field"),
    };

    private static ulong Get(byte[] data, int at, int width)
    {
        Span<byte> value = stackalloc byte[8];
        data.AsSpan(at, width).CopyTo(value);
        return BinaryPrimitives.ReadUInt64LittleEndian(value);
    }

    private static void Put(byte[] data, int at, int width, ulong value)
    {
        Span<byte> bytes = stackalloc byte[8];
        BinaryPrimitives.WriteUInt64LittleEndian(bytes, value);
        bytes[..width].CopyTo(data.AsSpan(at, width));
    }

    private static ulong Mask(int count) => count >= 64 ? ulong.MaxValue : (1UL << count) - 1;

    private static ulong ReadSparseMask(BitReader reader)
    {
        ulong result = 0;
        for (int offset = 0; offset < SlotCount; offset += 8)
        {
            if (!reader.ReadBit())
            {
                result |= reader.ReadBits(8) << offset;
            }
        }

        return result;
    }

    private static void WriteSparseMask(BitWriter writer, ulong mask)
    {
        for (int offset = 0; offset < SlotCount; offset += 8)
        {
            ulong unit = (mask >> offset) & 0xFF;
            writer.WriteBit(unit == 0);
            if (unit != 0)
            {
                writer.WriteBits(unit, 8);
            }
        }
    }

    private static bool[] FromUlong(ulong value, int count)
    {
        bool[] result = new bool[count];
        for (int index = 0; index < count; index++)
        {
            result[index] = ((value >> index) & 1) != 0;
        }

        return result;
    }

    private static ulong ToUlong(ReadOnlySpan<bool> bits)
    {
        ulong result = 0;
        for (int index = 0; index < bits.Length; index++)
        {
            result |= bits[index] ? 1UL << index : 0;
        }

        return result;
    }
}
