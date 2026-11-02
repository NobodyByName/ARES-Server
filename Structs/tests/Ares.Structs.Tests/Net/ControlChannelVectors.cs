using Ares.Structs.Net.Control;
using Ares.Structs.Net.Session;

namespace Ares.Structs.Tests.Net;

/// <summary>Reference encodings for the control channel tests (bytes in hex).</summary>
internal static class ControlChannelVectors
{
    /// <summary>Message, encoded message.</summary>
    public static IReadOnlyList<(ControlMessage Message, string Encoded)> Messages { get; } =
    [
        (new ControlHello(26159202U, "tok", 3), "000162288f0104000000746f6b000300"),
        (new ControlHello(1U, "", 0), "000101000000000000000000"),
        (new ControlWelcome("/Game/Maps/Dam", "/Script/Mode.Mode", ""), "010f0000002f47616d652f4d6170732f44616d00120000002f5363726970742f4d6f64652e4d6f64650000000000"),
        (new ControlWelcome("a", "b", "redirect"), "0102000000610002000000620009000000726564697265637400"),
        (new ControlChallenge("challenge"), "030a0000006368616c6c656e676500"),
        (new ControlNetspeed(100000), "04a0860100"),
        (new ControlNetspeed(-5), "04fbffffff"),
        (new ControlFailure("nope"), "06050000006e6f706500"),
        (new ControlJoin(), "09"),
        (new ControlCloseReason("Kicked"), "17070000004b69636b656400"),
        (new ControlProductUserIdRequest(), "51"),
        (new ControlProductUserIdResponse("puid", "tttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttttt"), "520500000070756964002d01000074747474747474747474747474747474747474747474747474747474747474747474747474747474747474747474747474747474747474747474747474747474747474747474747474747474747474747474747474747474747474747474747474747474747474747474747474747474747474747474747474747474747474747474747474747474747474747474747474747474747474747474747474747474747474747474747474747474747474747474747474747474747474747474747474747474747474747474747474747474747474747474747474747474747474747474747474747474747474747474747474747474747474747474747474747474747474747474747474747474747474747474747474747474747474747474747474747474747474747474747400"),
        (new ControlProofRequest("nonce"), "57060000006e6f6e636500"),
        (new ControlProofResponse("proof"), "580600000070726f6f6600"),
        (new ControlLogin("resp", "?Name=x", new UniqueNetId(0, "", "", false, false), "WIN"), "05050000007265737000080000003f4e616d653d7800030400000057494e00"),
        (new ControlLogin("resp", "?Name=x", new UniqueNetId(5, "abc123", "", false, false), "WIN"), "05050000007265737000080000003f4e616d653d78002807000000616263313233000400000057494e00"),
        (new ControlLogin("resp", "?Name=x", new UniqueNetId(2, "00ff10", "", true, false), "WIN"), "05050000007265737000080000003f4e616d653d7800110300ff100400000057494e00"),
        (new ControlLogin("resp", "?Name=x", new UniqueNetId(2, "abc", "", true, true), "WIN"), "05050000007265737000080000003f4e616d653d780015020abc0400000057494e00"),
        (new ControlLogin("resp", "?Name=x", new UniqueNetId(31, "player-1", "EOS", false, false), "WIN"), "05050000007265737000080000003f4e616d653d7800f804000000454f530009000000706c617965722d31000400000057494e00"),
        (new ControlLogin("resp", "?Name=x", new UniqueNetId(31, "0123456789abcdef", "Steam", true, false), "WIN"), "05050000007265737000080000003f4e616d653d7800f906000000537465616d00080123456789abcdef0400000057494e00"),
    ];

    /// <summary>Malformed message, what is wrong.</summary>
    public static TheoryData<string, string> BadMessages => new()
    {
        { "000000000000000000000000", "big-endian hello" },
        { "c8", "unknown message type" },
        { "05000000000000000007", "empty unique id with extra flags" },
        { "050000000000000000f1", "online services unique id" },
        { "05000000000000000014", "padded unique id that is not encoded" },
        { "0500000000000000001100", "empty encoded unique id" },
        { "05000000000000000015011a", "bad unique id padding" },
        { "0500000000000000001000000000", "empty string unique id" },
        { "050000000000000000f8050000004e6f6e6500", "unique id named None" },
    };

    /// <summary>Largest packet used by the vectors.</summary>
    public const int MaxPacketBytes = 1024;

    /// <summary>Bunch, encoded bunch, bit length.</summary>
    public static IReadOnlyList<(ChannelBunch Bunch, string Encoded, long Bits)> Bunches { get; } =
    [
        (new ChannelBunch(0, Convert.FromHexString("6335a0d4d108820a"), 64) { Name = ChannelName.Control, Sequence = 5 }, "044001ff0580c0580d28753482a002", 118),
        (new ChannelBunch(0, Convert.FromHexString("94a86571e3c91581b6c93a01ce2c586a18e0d4a60052643a65d9cfdf12b9ac42e46ee836350a"), 300) { Name = ChannelName.Control, Sequence = 1023, Opened = true }, "1300ffff17600994a86571e3c91581b6c93a01ce2c586a18e0d4a60052643a65d9cfdf12b9ac42e46ee836350a", 356),
        (new ChannelBunch(0, Convert.FromHexString("00"), 1) { Name = ChannelName.Control, Sequence = 0, CloseReason = 3 }, "1d0100c07f810000", 61),
        (new ChannelBunch(3, Convert.FromHexString("e41e9660a745753e"), 64) { Name = ChannelName.Actor, Sequence = 17, Opened = true, PackageExports = true, MustBeMapped = true }, "d36011640602e41e9660a745753e", 112),
        (new ChannelBunch(3, Convert.FromHexString(""), 0), "30000000", 27),
        (new ChannelBunch(200, Convert.FromHexString("43c7077886cd9fbeb82d42537e59ceea79dbf6801671748de614aeab02ba624513140bd4460a"), 300) { ReplicationPaused = true }, "8a14004b183a3ec0336cfef4c56d119af2cb7256cfdbb607b488a36b34a7705d15d0152b9aa058a03652", 335),
        (new ChannelBunch(32767, Convert.FromHexString("50f85c6a43bb1e74cd4cd594efe26ef57a184a2055ce209740418138e12240f26b758c02330c"), 300) { Name = ChannelName.Actor, Opened = true }, "e3ff5f00995902143e97dad0ae075d335335e5bbb85bbd1e8612489533c8255050204eb80890fc5a1da3c00c03", 354),
        (new ChannelBunch(9, Convert.FromHexString("6f82280583a6d4df"), 64) { CloseReason = 14 }, "75240004de04510a064da9bf01", 97),
    ];

    /// <summary>Malformed bunch, bit length, what is wrong.</summary>
    public static TheoryData<string, long, string> BadBunches => new()
    {
        { "3020", 14, "fragmented bunch" },
        { "3400003501", 33, "unknown channel name" },
        { "00000000", 27, "unreliable control channel" },
        { "440000ff0500", 41, "control name on another channel" },
        { "088c2300", 30, "channel index out of range" },
        { "34000099e903", 46, "truncated payload" },
    };

    /// <summary>From server, packet, encoded packet, bit length.</summary>
    public static IReadOnlyList<(bool FromServer, ConnectionPacket Packet, string Encoded, long Bits)> Packets { get; } =
    [
        (true, new ConnectionPacket(new NotificationHeader(1067, 14053, [2482883256U, 885185152U]), []), "516eaf10b8cafd9380dac23400", 97),
        (true, new ConnectionPacket(new NotificationHeader(16098, 9093, [3481581187U, 2104901561U, 326801271U]), [new ChannelBunch(0, Convert.FromHexString("94a86571e3c91581b6c93a01ce2c586a18e0d4a60052643a65d9cfdf12b9ac42e46ee836350a"), 300) { Name = ChannelName.Control, Sequence = 1023, Opened = true }]) { JitterClock = 1023, ServerFrameTime = 200 }, "52388afb83b684cfb93f767d77977a13ff8f3c01f0ff7f019640895a16379e5c11689bac13e0cc82a586014e6d0a2045a65396fdfc2d91cb2a44ee866e53a3", 504),
        (true, new ConnectionPacket(new NotificationHeader(11834, 1460, [3694363523U, 2590683945U]), [new ChannelBunch(0, Convert.FromHexString("00"), 1) { Name = ChannelName.Control, Sequence = 0, CloseReason = 3 }, new ChannelBunch(3, Convert.FromHexString("e41e9660a745753e"), 64) { Name = ChannelName.Actor, Sequence = 17, Opened = true, PackageExports = true, MustBeMapped = true }]) { JitterClock = 5 }, "415be8b8838333dc29b36a9a0bd0110000fc170800a6c122c80c04c83d2cc14e8bea7c00", 281),
        (false, new ConnectionPacket(new NotificationHeader(12504, 13803, [1126660590U, 1557301086U]), [new ChannelBunch(0, Convert.FromHexString("6335a0d4d108820a"), 64) { Name = ChannelName.Control, Sequence = 5 }, new ChannelBunch(0, Convert.FromHexString("94a86571e3c91581b6c93a01ce2c586a18e0d4a60052643a65d9cfdf12b9ac42e46ee836350a"), 300) { Name = ChannelName.Control, Sequence = 1023, Opened = true }, new ChannelBunch(0, Convert.FromHexString("00"), 1) { Name = ChannelName.Control, Sequence = 0, CloseReason = 3 }]) { JitterClock = 7, FrameTimeRequested = true }, "b15e63c3ee7927435e87d25c0f480014f05f00088cd580524723082a4c00fcff5f802550a296c58d275704da26eb0438b360a96180539b024891e994653f7f4be4b20a91bba1dbd468470000f05f200000", 643),
        (false, new ConnectionPacket(new NotificationHeader(14972, 7844, [2634218640U, 17416711U]), []) { JitterClock = 0 }, "41eaf1e990fc029d07c209010100", 108),
        (false, new ConnectionPacket(new NotificationHeader(4390, 6388, [2303584948U, 1031575033U]), [new ChannelBunch(0, Convert.FromHexString("00"), 1) { Name = ChannelName.Control, Sequence = 0, CloseReason = 3 }]), "418f9944b4ea4d89f9957c3d3a020080ff020100", 158),
    ];
}
