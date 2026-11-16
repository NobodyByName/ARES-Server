using System.Collections.Concurrent;
using Ares.Structs.Core.Serialization;

namespace Ares.Structs.Game.Pendulum;

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

    private readonly IReadOnlyDictionary<int, PendulumDescriptor> _descriptors;
    // Shared by every user of Default, possibly on several threads.
    private readonly ConcurrentDictionary<int, byte[]> _defaults = new();
    private readonly int _registryCount;
    private readonly PendulumFieldFlags _delta;

    /// <summary>Creates a codec.</summary>
    /// <param name="registryCount">Number of descriptors in the registry (the wire range of a descriptor id).</param>
    /// <param name="descriptors">Known descriptors by id.</param>
    /// <param name="serialized">Fields whose flags intersect this are sent.</param>
    /// <param name="delta">Fields whose flags intersect this are coded against the previous frame, the others against their default.</param>
    public PendulumCodec(
        int registryCount,
        IReadOnlyDictionary<int, PendulumDescriptor> descriptors,
        PendulumFieldFlags serialized = PendulumFieldFlags.Owner,
        PendulumFieldFlags delta = PendulumFieldFlags.Owner)
    {
        _registryCount = registryCount;
        _descriptors = descriptors ?? throw new ArgumentNullException(nameof(descriptors));
        Fields = serialized;
        _delta = delta;
    }

    /// <summary>The codec for the game's registry as the owning client reads it (its input and its own pawn frames).</summary>
    public static PendulumCodec Default { get; } = new(PendulumRegistry.Count, PendulumRegistry.Descriptors);

    /// <summary>The codec for other clients' copies of a character: only the fields sent to other players.</summary>
    public static PendulumCodec Others { get; } = new(PendulumRegistry.Count, PendulumRegistry.Descriptors, PendulumFieldFlags.Others, PendulumFieldFlags.Others);

    /// <summary>The field set this codec sends and reads; a blackboard update's header names it to the receiver.</summary>
    public PendulumFieldFlags Fields { get; }

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
                    PendulumLayout.PutQuaternion(buffer, field.Offset, quaternion.Value);
                    break;
                case (PendulumArrayField field, PendulumArrayValue array):
                    byte[] empty = PendulumLayout.ElementDefault(field);
                    foreach (PendulumArrayElement element in array.Elements)
                    {
                        byte[] item = [.. empty];
                        Apply(field.ElementFields, item, element.Values);
                        item.CopyTo(buffer, field.Offset + (element.Index * field.Stride));
                    }

                    break;
                case (var field, PendulumIntegerValue integer):
                    PendulumLayout.PutInteger(buffer, field, integer.Value);
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
            PendulumScalarField scalar => PendulumLayout.Get(buffer, scalar.Offset, scalar.Width),
            PendulumBitSetField set => PendulumLayout.Get(buffer, set.Offset, (set.BitCount + 7) / 8),
            PendulumBitfieldField bits => PendulumLayout.Get(buffer, bits.Offset, (bits.BitCount + 7) / 8) & PendulumMasks.Mask(bits.BitCount),
            PendulumBitField bit => buffer[bit.Offset] & 1UL,
            PendulumBoundedField bounded => PendulumLayout.Get(buffer, bounded.Offset, bounded.Width),
            PendulumObjectField objectField => PendulumLayout.Get(buffer, objectField.Offset, 8),
            _ => throw new InvalidPacketException("Not an integer field"),
        };
    }

    /// <summary>Reads a quaternion field from a value buffer (the inverse of <see cref="Apply"/> for quaternions).</summary>
    /// <param name="field">The field.</param>
    /// <param name="buffer">The value bytes.</param>
    /// <returns>The quaternion.</returns>
    public static PendulumQuaternion Quaternion(PendulumQuaternionField field, byte[] buffer)
    {
        ArgumentNullException.ThrowIfNull(field);
        ArgumentNullException.ThrowIfNull(buffer);
        int at = field.Offset;
        return new PendulumQuaternion(
            (uint)PendulumLayout.Get(buffer, at, 4),
            (uint)PendulumLayout.Get(buffer, at + 4, 4),
            (uint)PendulumLayout.Get(buffer, at + 8, 4),
            (uint)PendulumLayout.Get(buffer, at + 12, 4),
            buffer[at + 16],
            buffer[at + 17],
            buffer[at + 18],
            buffer[at + 19]);
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
        ulong layout = reader.ReadBit() ? PendulumMasks.ReadSparseMask(reader) : 0;
        if (layout != 0)
        {
            ulong removed = PendulumMasks.ReadSparseMask(reader);
            for (int slot = 0; slot < SlotCount; slot++)
            {
                if (!PendulumMasks.IsSet(layout, slot))
                {
                    continue;
                }

                int position = PendulumMasks.IsSet(removed, slot) ? RemovedPosition : (int)reader.ReadBounded(64);
                int size = (int)reader.ReadBounded(63);
                PendulumSlot old = slots.GetValueOrDefault(slot) ?? new PendulumSlot(slot, RemovedPosition, 0, null, []);
                slots[slot] = old with { Position = position, Size = size };
            }
        }

        ulong descriptors = reader.ReadBit() ? PendulumMasks.ReadSparseMask(reader) : 0;
        ulong fresh = 0;
        for (int slot = 0; slot < SlotCount; slot++)
        {
            if (!PendulumMasks.IsSet(descriptors, slot))
            {
                continue;
            }

            PendulumSlot old = slots.GetValueOrDefault(slot) ?? new PendulumSlot(slot, RemovedPosition, 0, null, []);
            if (!reader.ReadBit())
            {
                // A slot that keeps its place in the layout can stay without a variable.
                if (old.Position == RemovedPosition)
                {
                    slots.Remove(slot);
                }
                else
                {
                    slots[slot] = old with { DescriptorId = null, Value = [] };
                }

                continue;
            }

            int descriptorId = (int)reader.ReadBounded((ulong)_registryCount);
            slots[slot] = old with { DescriptorId = descriptorId, Value = DefaultValue(descriptorId) };
            fresh |= 1UL << slot;
        }

        ulong changed = PendulumMasks.ReadSparseMask(reader);
        for (int slot = 0; slot < SlotCount; slot++)
        {
            if (!PendulumMasks.IsSet(fresh | changed, slot))
            {
                continue;
            }

            PendulumSlot value = slots.GetValueOrDefault(slot) ?? throw new InvalidPacketException("Invalid slot");
            if (value.DescriptorId is not { } id || value.Position == RemovedPosition || value.Size == 0 || value.Position + value.Size > SlotCount)
            {
                throw new InvalidPacketException("Invalid slot");
            }

            byte[] baseline = PendulumMasks.IsSet(fresh | layout, slot) ? DefaultValue(id) : value.Value;
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
            PendulumMasks.WriteSparseMask(writer, layout);
            PendulumMasks.WriteSparseMask(writer, 0);
            foreach (PendulumSlot slot in Ordered(frame, layout))
            {
                writer.WriteBounded((uint)slot.Position, 64);
                writer.WriteBounded((uint)slot.Size, 63);
            }
        }

        writer.WriteBit(descriptors != 0);
        if (descriptors != 0)
        {
            PendulumMasks.WriteSparseMask(writer, descriptors);
            foreach (PendulumSlot slot in Ordered(frame, descriptors))
            {
                // A laid-out slot without a variable is sent as a removed descriptor.
                writer.WriteBit(slot.DescriptorId is not null);
                if (slot.DescriptorId is { } id)
                {
                    writer.WriteBounded((uint)id, (ulong)_registryCount);
                }
            }
        }

        // New descriptors and changed slots are sent; a new descriptor or layout restarts from the default.
        ulong changed = frame.Values
            .Where(slot => slot.DescriptorId is not null && !PendulumMasks.IsSet(descriptors, slot.Slot)
                && (PendulumMasks.IsSet(layout, slot.Slot) || !previous[slot.Slot].Value.AsSpan().SequenceEqual(slot.Value)))
            .Aggregate(0UL, (mask, slot) => mask | (1UL << slot.Slot));

        PendulumMasks.WriteSparseMask(writer, changed);
        foreach (PendulumSlot slot in Ordered(frame, descriptors | changed).Where(slot => slot.DescriptorId is not null))
        {
            int id = slot.DescriptorId!.Value;
            byte[] baseline = PendulumMasks.IsSet(descriptors | layout, slot.Slot) ? DefaultOf(id) : previous[slot.Slot].Value;
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

    private byte[] DefaultOf(int id) => _defaults.GetOrAdd(id, key =>
    {
        PendulumDescriptor descriptor = Descriptor(key);
        byte[] value = new byte[descriptor.Size];
        PendulumLayout.SetDefaults(descriptor.Fields, value);
        return value;
    });

    private static IEnumerable<PendulumSlot> Ordered(Dictionary<int, PendulumSlot> frame, ulong mask) =>
        frame.Values.Where(slot => PendulumMasks.IsSet(mask, slot.Slot)).OrderBy(slot => slot.Slot);

    // Values.

    private bool Sent(PendulumField field) => (field.Flags & Fields) != 0;

    private byte[] BaselineOf(PendulumField field, byte[] previous, byte[] defaults) => (field.Flags & _delta) != 0 ? previous : defaults;

    private byte[] ReadFields(BitReader reader, IReadOnlyList<PendulumField> fields, byte[] previous, byte[] defaults)
    {
        byte[] data = [.. previous];
        foreach (PendulumField field in fields.Where(Sent))
        {
            byte[] baseline = BaselineOf(field, previous, defaults);
            Array.Copy(baseline, field.Offset, data, field.Offset, PendulumLayout.Extent(field));
            switch (field)
            {
                case PendulumScalarField scalar:
                    PendulumLayout.Put(data, scalar.Offset, scalar.Width, ReadScalar(reader, PendulumLayout.Get(data, scalar.Offset, scalar.Width), scalar.Maximum, scalar.Delta, true));
                    break;
                case PendulumBitSetField set:
                    if (reader.ReadBit())
                    {
                        PendulumLayout.Put(data, set.Offset, (set.BitCount + 7) / 8, PendulumMasks.ReadBitfield(reader, PendulumLayout.Get(data, set.Offset, (set.BitCount + 7) / 8), set.BitCount));
                    }

                    break;
                case PendulumBitfieldField bits:
                    int width = (bits.BitCount + 7) / 8;
                    ulong mask = PendulumMasks.Mask(bits.BitCount);
                    ulong stored = PendulumLayout.Get(data, bits.Offset, width);
                    PendulumLayout.Put(data, bits.Offset, width, (stored & ~mask) | (PendulumMasks.ReadBitfield(reader, stored, bits.BitCount) & mask));
                    break;
                case PendulumBitField bit:
                    data[bit.Offset] = reader.ReadBit() ? (byte)1 : (byte)0;
                    break;
                case PendulumBoundedField bounded:
                    if (reader.ReadBit())
                    {
                        PendulumLayout.Put(data, bounded.Offset, bounded.Width, reader.ReadBounded(bounded.Maximum));
                    }

                    break;
                case PendulumObjectField objectField:
                    if (reader.ReadBit())
                    {
                        PendulumLayout.Put(data, objectField.Offset, 8, ReadObject(reader));
                    }

                    break;
                case PendulumNameField nameField:
                    if (reader.ReadBit())
                    {
                        PendulumLayout.Put(data, nameField.Offset, 8, ReadName(reader));
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
                    WriteScalar(writer, PendulumLayout.Get(value, scalar.Offset, scalar.Width), PendulumLayout.Get(baseline, scalar.Offset, scalar.Width), scalar.Maximum, scalar.Delta, true);
                    break;
                case PendulumBitSetField set:
                    ulong newSet = PendulumLayout.Get(value, set.Offset, (set.BitCount + 7) / 8);
                    ulong oldSet = PendulumLayout.Get(baseline, set.Offset, (set.BitCount + 7) / 8);
                    writer.WriteBit(newSet != oldSet);
                    if (newSet != oldSet)
                    {
                        PendulumMasks.WriteBitfield(writer, newSet, oldSet, set.BitCount);
                    }

                    break;
                case PendulumBitfieldField bits:
                    int width = (bits.BitCount + 7) / 8;
                    PendulumMasks.WriteBitfield(writer, PendulumLayout.Get(value, bits.Offset, width), PendulumLayout.Get(baseline, bits.Offset, width), bits.BitCount);
                    break;
                case PendulumBitField bit:
                    writer.WriteBit((value[bit.Offset] & 1) != 0);
                    break;
                case PendulumBoundedField bounded:
                    ulong newBounded = PendulumLayout.Get(value, bounded.Offset, bounded.Width);
                    writer.WriteBit(newBounded != PendulumLayout.Get(baseline, bounded.Offset, bounded.Width));
                    if (newBounded != PendulumLayout.Get(baseline, bounded.Offset, bounded.Width))
                    {
                        writer.WriteBounded((uint)newBounded, bounded.Maximum);
                    }

                    break;
                case PendulumObjectField objectField:
                    ulong newObject = PendulumLayout.Get(value, objectField.Offset, 8);
                    writer.WriteBit(newObject != PendulumLayout.Get(baseline, objectField.Offset, 8));
                    if (newObject != PendulumLayout.Get(baseline, objectField.Offset, 8))
                    {
                        writer.WritePacked(checked((uint)newObject));
                    }

                    break;
                case PendulumNameField name:
                    writer.WriteBit(PendulumLayout.Get(value, name.Offset, 8) == PendulumLayout.Get(baseline, name.Offset, 8)
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
            return Bounded(reader, maximum);
        }

        bool positive = reader.ReadBit();
        long change = Bounded(reader, delta);
        return (ulong)((long)previous + (positive ? change : -change));
    }

    // A bounded integer as the game reads it: a bound of 0 or 1 (possible for computed quaternion bounds) has no bits.
    private static uint Bounded(BitReader reader, uint maximum) => maximum <= 1 ? 0 : reader.ReadBounded(maximum);

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

    // Quaternions: the three smallest components, the largest first; later components are bounded by
    // what the earlier ones leave of the total.

    // An object reference as the engine sends it: its packed network id; id 1 (by path) carries export flags, then
    // with a path its outer reference, the path and, when flagged, a checksum. Kept as the id.
    private static ulong ReadObject(BitReader reader, int depth = 0)
    {
        uint id = reader.ReadPacked();
        if (id != 1)
        {
            return id;
        }

        uint flags = (uint)reader.ReadBits(8);
        if ((flags & 1) == 0)
        {
            return id;
        }

        if (depth > 16)
        {
            throw new InvalidPacketException("Object path too deep");
        }

        ReadObject(reader, depth + 1);
        reader.ReadString();
        if ((flags & 4) != 0)
        {
            reader.ReadBits(32);
        }

        return id;
    }

    // A name as the engine sends it: a built-in name's packed index, or its text and number. Kept as the built-in
    // index, or as a hash of the text and number with the top bit set (ARES never sends names, it only reads them).
    private static ulong ReadName(BitReader reader)
    {
        if (reader.ReadBit())
        {
            return reader.ReadPacked();
        }

        string text = reader.ReadString();
        uint number = (uint)reader.ReadBits(32);
        ulong hash = 14695981039346656037UL;
        foreach (char character in text.AsSpan())
        {
            hash = (hash ^ character) * 1099511628211UL;
        }

        return ((hash ^ number) * 1099511628211UL) | (1UL << 63);
    }

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
            data[at + 17] = (byte)smallest;
            data[at + 18] = (byte)middle;
            data[at + 19] = (byte)largest;

            // As the game reads it: the omitted component is the first index none of the three names; repeated
            // indices are accepted.
            data[at + 20] = (byte)Enumerable.Range(0, 4).First(index => index != smallest && index != middle && index != largest);
        }

        int omitted = data[at + 20];
        if (omitted != oldOmitted)
        {
            PendulumLayout.Put(data, at + (4 * oldOmitted), 4, ReadScalar(reader, field.Maximum / 2, field.Maximum, field.Delta, true));
            for (int index = 0; index < 4; index++)
            {
                if (index != omitted && index != oldOmitted)
                {
                    PendulumLayout.Put(data, at + (4 * index), 4, ReadScalar(reader, PendulumLayout.Get(data, at + (4 * index), 4), field.Maximum, field.Delta, true));
                }
            }

            return;
        }

        int[] order = [data[at + 19], data[at + 18], data[at + 17]];
        ulong first = ReadScalar(reader, PendulumLayout.Get(data, at + (4 * order[0]), 4), field.Maximum, field.Delta, false);
        PendulumLayout.Put(data, at + (4 * order[0]), 4, first);
        long remaining = (long)field.Total - (2 * (long)first * (long)first);
        uint bound = Limit(remaining, first);
        ulong second = bound >= 2 ? ReadScalar(reader, PendulumLayout.Get(data, at + (4 * order[1]), 4), bound, bound / 16, false) : 0;
        PendulumLayout.Put(data, at + (4 * order[1]), 4, second);
        bound = Limit(remaining - ((long)second * (long)second), second);
        ulong third = bound >= 2 ? ReadScalar(reader, PendulumLayout.Get(data, at + (4 * order[2]), 4), bound, bound / 16, false) : 0;
        PendulumLayout.Put(data, at + (4 * order[2]), 4, third);
    }

    private static void WriteQuaternion(BitWriter writer, byte[] value, byte[] baseline, PendulumQuaternionField field)
    {
        int at = field.Offset;
        ReadOnlySpan<byte> current = value.AsSpan(at, PendulumLayout.QuaternionBytes);
        ReadOnlySpan<byte> old = baseline.AsSpan(at, PendulumLayout.QuaternionBytes);
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
            WriteScalar(writer, PendulumLayout.Get(value, at + (4 * oldOmitted), 4), field.Maximum / 2, field.Maximum, field.Delta, true);
            for (int index = 0; index < 4; index++)
            {
                if (index != omitted && index != oldOmitted)
                {
                    WriteScalar(writer, PendulumLayout.Get(value, at + (4 * index), 4), PendulumLayout.Get(baseline, at + (4 * index), 4), field.Maximum, field.Delta, true);
                }
            }

            return;
        }

        int[] order = [current[19], current[18], current[17]];
        ulong first = PendulumLayout.Get(value, at + (4 * order[0]), 4);
        WriteScalar(writer, first, PendulumLayout.Get(baseline, at + (4 * order[0]), 4), field.Maximum, field.Delta, false);
        long remaining = (long)field.Total - (2 * (long)first * (long)first);
        uint bound = Limit(remaining, first);
        ulong second = PendulumLayout.Get(value, at + (4 * order[1]), 4);
        if (bound >= 2)
        {
            WriteScalar(writer, second, PendulumLayout.Get(baseline, at + (4 * order[1]), 4), bound, bound / 16, false);
        }

        bound = Limit(remaining - ((long)second * (long)second), second);
        if (bound >= 2)
        {
            WriteScalar(writer, PendulumLayout.Get(value, at + (4 * order[2]), 4), PendulumLayout.Get(baseline, at + (4 * order[2]), 4), bound, bound / 16, false);
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
        byte[] empty = PendulumLayout.ElementDefault(field);
        bool[] changed = PendulumMasks.ReadArrayMask(reader, Math.Max(count, oldCount));
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
        byte[] empty = PendulumLayout.ElementDefault(field);
        bool[] changed = new bool[Math.Max(count, oldCount)];
        for (int index = 0; index < Math.Min(count, oldCount); index++)
        {
            changed[index] = !PendulumLayout.SameFields(field.ElementFields, PendulumLayout.Item(value, field, index), PendulumLayout.Item(baseline, field, index));
        }

        PendulumMasks.WriteArrayMask(writer, changed);
        for (int index = 0; index < changed.Length; index++)
        {
            if (changed[index])
            {
                WriteFields(writer, field.ElementFields, PendulumLayout.Item(value, field, index), PendulumLayout.Item(baseline, field, index), empty);
            }
            else if (index >= oldCount && index < count)
            {
                WriteFields(writer, field.ElementFields, PendulumLayout.Item(value, field, index), empty, empty);
            }
        }
    }

    // The element count and the count it is coded against. As the game reads it, the old count comes from the
    // baseline only when the count field itself is delta-coded; otherwise every element up to the count is new.
    private (int Count, int OldCount) Counts(PendulumArrayField field, byte[] value, byte[] baseline, IReadOnlyList<PendulumField> siblings)
    {
        if (field.CountField is not { } index)
        {
            return (field.Capacity, field.Capacity);
        }

        PendulumScalarField counter = siblings[index] as PendulumScalarField ?? throw new InvalidPacketException("Invalid array count field");
        ulong count = PendulumLayout.Get(value, counter.Offset, counter.Width);
        ulong oldCount = (counter.Flags & _delta) != 0 ? PendulumLayout.Get(baseline, counter.Offset, counter.Width) : 0;
        return count <= (ulong)field.Capacity && oldCount <= (ulong)field.Capacity
            ? ((int)count, (int)oldCount)
            : throw new InvalidPacketException("Array count above its capacity");
    }
}
