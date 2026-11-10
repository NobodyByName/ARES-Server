using Ares.Structs.Core.Serialization;
using Ares.Structs.Game.Movement;

namespace Ares.Structs.Tests.Game;

// Field encodings as the game reads them in other characters' frames.
public sealed class PendulumFieldReadTests
{
    private const PendulumFieldFlags Both = PendulumFieldFlags.Owner | PendulumFieldFlags.Others;

    [Fact]
    public void A_rotation_order_with_a_repeated_index_omits_the_first_unnamed_component()
    {
        PendulumCodec codec = Codec(new PendulumQuaternionField(0, 64U, 8U, 8100UL, new PendulumQuaternion(0U, 0U, 0U, 0U, 15, 0, 1, 2), Both));
        BitWriter writer = new();
        writer.WriteBit(true); // changed
        writer.WriteBit(true); // new order
        writer.WriteBits(15, 4);
        writer.WriteBounded(0, 4);
        writer.WriteBounded(1, 4);
        writer.WriteBounded(0, 4);
        writer.WriteBits(0b111, 3); // the previously omitted component and the other two: unchanged

        BitReader reader = new(writer.ToArray(), writer.Length);
        byte[] value = codec.ReadValue(reader, 1, new byte[24]);

        Assert.Equal(2, value[20]);
        Assert.Equal(0, reader.Remaining);
    }

    [Fact]
    public void A_changed_name_is_read_as_the_engine_sends_it()
    {
        PendulumCodec codec = Codec(new PendulumNameField(0, Both));
        BitWriter writer = new();
        writer.WriteBit(true); // changed
        writer.WriteBit(true); // built-in name
        writer.WritePacked(30);
        writer.WriteBit(true);
        writer.WriteBit(false); // text name
        writer.WriteString("Montage");
        writer.WriteBits(2, 32);

        BitReader reader = new(writer.ToArray(), writer.Length);
        Assert.Equal(30UL, BitConverter.ToUInt64(codec.ReadValue(reader, 1, new byte[8])));
        Assert.NotEqual(0UL, BitConverter.ToUInt64(codec.ReadValue(reader, 1, new byte[8])));
        Assert.Equal(0, reader.Remaining);
    }

    [Fact]
    public void An_object_sent_by_path_carries_its_outer_and_path()
    {
        PendulumCodec codec = Codec(new PendulumObjectField(0, Both));
        BitWriter writer = new();
        writer.WriteBit(true); // changed
        writer.WritePacked(1); // by path
        writer.WriteByte(5); // has a path and a checksum
        writer.WritePacked(7); // outer
        writer.WriteString("Default__Item");
        writer.WriteBits(0x12345678, 32);

        BitReader reader = new(writer.ToArray(), writer.Length);
        Assert.Equal(1UL, BitConverter.ToUInt64(codec.ReadValue(reader, 1, new byte[8])));
        Assert.Equal(0, reader.Remaining);
    }

    [Fact]
    public void Other_clients_get_only_the_fields_sent_to_others()
    {
        PendulumScalarField ownerOnly = new(0, 4, 256U, 8U, 0U, PendulumFieldFlags.Owner);
        PendulumScalarField shared = new(4, 4, 256U, 8U, 0U, Both);
        Dictionary<int, PendulumDescriptor> descriptors = new() { [1] = new PendulumDescriptor(1, 8, [ownerOnly, shared]) };
        PendulumCodec owner = new(2, descriptors);
        PendulumCodec others = new(2, descriptors, PendulumFieldFlags.Others, PendulumFieldFlags.Others);
        byte[] value = [100, 0, 0, 0, 200, 0, 0, 0];

        BitWriter writer = new();
        others.WriteValue(writer, 1, value, new byte[8]);
        BitWriter both = new();
        owner.WriteValue(both, 1, value, new byte[8]);
        byte[] read = others.ReadValue(new BitReader(writer.ToArray(), writer.Length), 1, new byte[8]);

        Assert.True(writer.Length < both.Length);
        Assert.Equal([0, 0, 0, 0, 200, 0, 0, 0], read);
    }

    private static PendulumCodec Codec(PendulumField field) =>
        new(2, new Dictionary<int, PendulumDescriptor> { [1] = new(1, 24, [field]) }, PendulumFieldFlags.Others, PendulumFieldFlags.Others);
}
