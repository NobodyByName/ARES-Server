using Ares.Structs.Core.Serialization;
using Ares.Structs.Net.Control;
using Ares.Structs.Net.Session;

namespace Ares.Structs.Tests.Net;

public sealed class ControlChannelTests
{
    private const int MaxPacketBytes = ControlChannelVectors.MaxPacketBytes;

    [Fact]
    public void Control_messages_match_the_reference()
    {
        foreach ((ControlMessage message, string encoded) in ControlChannelVectors.Messages)
        {
            BitWriter writer = new();
            message.Write(writer);
            Assert.Equal(encoded, Convert.ToHexStringLower(writer.ToArray()));
            Assert.Equal((byte)message.Type, writer.ToArray()[0]);

            BitReader reader = new(Convert.FromHexString(encoded));
            Assert.Equal(message, ControlMessage.Read(reader));
            Assert.Equal(0, reader.Remaining);
        }
    }

    [Fact]
    public void Actor_channel_failures_carry_a_32_bit_channel_index()
    {
        BitWriter writer = new();
        new ControlActorChannelFailure(70).Write(writer);
        Assert.Equal("1046000000", Convert.ToHexStringLower(writer.ToArray()));
        Assert.Equal(new ControlActorChannelFailure(70), ControlMessage.Read(new BitReader(writer.ToArray())));
        Assert.Throws<InvalidPacketException>(() => ControlMessage.Read(new BitReader([16, 0, 0, 0, 0x80])));
    }

    [Theory]
    [MemberData(nameof(ControlChannelVectors.BadMessages), MemberType = typeof(ControlChannelVectors))]
    public void Malformed_messages_are_rejected(string encoded, string reason)
    {
        Assert.Throws<InvalidPacketException>(() => ControlMessage.Read(new BitReader(Convert.FromHexString(encoded))));
        Assert.False(string.IsNullOrEmpty(reason));
    }

    [Fact]
    public void Bunches_match_the_reference()
    {
        foreach ((ChannelBunch bunch, string encoded, long bits) in ControlChannelVectors.Bunches)
        {
            (string actual, long length) = Encode(writer => bunch.Write(writer, MaxPacketBytes));
            Assert.Equal((encoded, bits), (actual, length));

            BitReader reader = new(Convert.FromHexString(encoded), bits);
            ChannelBunch read = ChannelBunch.Read(reader, MaxPacketBytes);
            Assert.Equal(0, reader.Remaining);
            Assert.Equal((encoded, bits), Encode(writer => read.Write(writer, MaxPacketBytes)));
        }
    }

    [Theory]
    [MemberData(nameof(ControlChannelVectors.BadBunches), MemberType = typeof(ControlChannelVectors))]
    public void Malformed_bunches_are_rejected(string encoded, long bits, string reason)
    {
        Assert.Throws<InvalidPacketException>(() => ChannelBunch.Read(new BitReader(Convert.FromHexString(encoded), bits), MaxPacketBytes));
        Assert.False(string.IsNullOrEmpty(reason));
    }

    [Fact]
    public void Packets_match_the_reference()
    {
        foreach ((bool fromServer, ConnectionPacket packet, string encoded, long bits) in ControlChannelVectors.Packets)
        {
            Assert.Equal((encoded, bits), Encode(writer => packet.Write(writer, fromServer, MaxPacketBytes)));

            ConnectionPacket read = ConnectionPacket.Read(new BitReader(Convert.FromHexString(encoded), bits), fromServer, MaxPacketBytes);
            Assert.Equal(packet.Bunches.Count, read.Bunches.Count);
            Assert.Equal((encoded, bits), Encode(writer => read.Write(writer, fromServer, MaxPacketBytes)));
        }
    }

    [Fact]
    public void Inconsistent_values_are_rejected_when_writing()
    {
        BitWriter writer = new();
        NotificationHeader header = new(1, 2, [0]);
        Assert.Throws<InvalidPacketException>(() => new ConnectionPacket(header, []) { FrameTimeRequested = true }.Write(writer, true, MaxPacketBytes));
        Assert.Throws<InvalidPacketException>(() => new ConnectionPacket(header, []) { JitterClock = 1, ServerFrameTime = 3 }.Write(writer, false, MaxPacketBytes));
        Assert.Throws<InvalidPacketException>(() => new ConnectionPacket(header, []) { ServerFrameTime = 3 }.Write(writer, true, MaxPacketBytes));
        Assert.Throws<InvalidPacketException>(() => new ChannelBunch(0, [], 0).Write(writer, MaxPacketBytes));
        Assert.Throws<InvalidPacketException>(() => new ChannelBunch(3, [], 0) { Name = ChannelName.Control, Sequence = 1 }.Write(writer, MaxPacketBytes));
        Assert.Throws<InvalidPacketException>(() => new ChannelBunch(3, [], 0) { Name = ChannelName.Actor }.Write(writer, MaxPacketBytes));
        Assert.Throws<InvalidPacketException>(() => new ChannelBunch(3, [], 9).Write(writer, MaxPacketBytes));
        Assert.Throws<InvalidPacketException>(() => new UniqueNetId(30, "ab", "", true, false).Write(new BitWriter()));
        Assert.Throws<InvalidPacketException>(() => new UniqueNetId(31, "ab", "None", false, false).Write(new BitWriter()));
        Assert.Throws<ArgumentOutOfRangeException>(() => ChannelBunch.Control(0, [], 0).Write(writer, 10));
    }

    private static (string Encoded, long Bits) Encode(Action<BitWriter> write)
    {
        BitWriter writer = new();
        write(writer);
        return (Convert.ToHexStringLower(writer.ToArray()), writer.Length);
    }
}
