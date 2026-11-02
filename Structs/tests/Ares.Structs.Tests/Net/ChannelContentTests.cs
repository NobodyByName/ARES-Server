using Ares.Structs.Core.Serialization;
using Ares.Structs.Net.Replication;
using Ares.Structs.Net.Session;

namespace Ares.Structs.Tests.Net;

public sealed class ChannelContentTests
{
    [Fact]
    public void Content_blocks_match_the_reference()
    {
        foreach ((ActorContentBlock block, string encoded, long bits) in ChannelContentVectors.Blocks)
        {
            Assert.Equal((encoded, bits), Encode(block.Write));
            ActorContentBlock read = ActorContentBlock.Read(Reader(encoded, bits));
            Assert.Equal((encoded, bits), Encode(read.Write));
        }
    }

    [Fact]
    public void Field_payloads_match_the_reference()
    {
        foreach ((ActorFieldPayload field, int count, string encoded, long bits) in ChannelContentVectors.Fields)
        {
            Assert.Equal((encoded, bits), Encode(writer => field.Write(writer, count)));
            ActorFieldPayload read = ActorFieldPayload.Read(Reader(encoded, bits), count);
            Assert.Equal(field.Index, read.Index);
            Assert.Equal((encoded, bits), Encode(writer => read.Write(writer, count)));
        }
    }

    [Fact]
    public void Must_map_ids_match_the_reference()
    {
        foreach ((ulong[] guids, string encoded, long bits) in ChannelContentVectors.MustMaps)
        {
            Assert.Equal((encoded, bits), Encode(new MustMapGuids(guids).Write));
            Assert.Equal(guids, MustMapGuids.Read(Reader(encoded, bits)).Guids);
        }
    }

    [Fact]
    public void Actor_initializations_match_the_reference()
    {
        foreach ((ChannelBunch bunch, string encoded, long bits) in ChannelContentVectors.Initializations)
        {
            Assert.Equal((encoded, bits), Encode(writer => bunch.Write(writer, ChannelContentVectors.MaxPacketBytes)));
        }
    }

    [Fact]
    public void Subobject_content_matches_the_reference()
    {
        StableSubobjectContent stable = StableSubobjectContent.Read(Reader(ChannelContentVectors.StableSubobject, ChannelContentVectors.StableSubobjectBits));
        Assert.Equal(22UL, stable.Guid);
        Assert.Equal((ChannelContentVectors.StableSubobject, ChannelContentVectors.StableSubobjectBits), Encode(stable.Write));

        DynamicSubobjectContent dynamic = DynamicSubobjectContent.Read(Reader(ChannelContentVectors.DynamicSubobject, ChannelContentVectors.DynamicSubobjectBits));
        Assert.Equal((24UL, 13UL, (ulong?)null), (dynamic.Guid, dynamic.ClassGuid, dynamic.OuterGuid));
        Assert.Equal((ChannelContentVectors.DynamicSubobject, ChannelContentVectors.DynamicSubobjectBits), Encode(dynamic.Write));

        DynamicSubobjectContent withOuter = DynamicSubobjectContent.Read(
            Reader(ChannelContentVectors.DynamicSubobjectWithOuter, ChannelContentVectors.DynamicSubobjectWithOuterBits));
        Assert.Equal((26UL, 15UL, (ulong?)22UL), (withOuter.Guid, withOuter.ClassGuid, withOuter.OuterGuid));
        Assert.Equal((ChannelContentVectors.DynamicSubobjectWithOuter, ChannelContentVectors.DynamicSubobjectWithOuterBits), Encode(withOuter.Write));

        StableSubobjectContent client = StableSubobjectContent.ReadFromClient(Reader(ChannelContentVectors.ClientSubobject, ChannelContentVectors.ClientSubobjectBits));
        Assert.Equal((30UL, true, 5L, (byte)31), (client.Guid, client.Content.HasRepLayout, client.Content.PayloadBits, client.Content.Payload[0]));

        ClientSubobjectReference path = ClientSubobjectReference.Read(Reader(ChannelContentVectors.ClientPathReference, ChannelContentVectors.ClientPathReferenceBits));
        Assert.Equal((20UL, "NewComp", 3L, (byte)5), (path.Outer, path.Name, path.Content.PayloadBits, path.Content.Payload[0]));
    }

    [Fact]
    public void Names_and_visibility_match_the_reference()
    {
        foreach ((NetworkName name, string encoded, long bits) in ChannelContentVectors.Names)
        {
            Assert.Equal((encoded, bits), Encode(name.Write));
            Assert.Equal(name, NetworkName.Read(Reader(encoded, bits)));
        }

        foreach ((LevelVisibility visibility, string encoded, long bits) in ChannelContentVectors.Visibilities)
        {
            Assert.Equal((encoded, bits), Encode(visibility.Write));
            LevelVisibility read = LevelVisibility.Read(Reader(encoded, bits));
            string file = visibility.File == "None" ? visibility.Package : visibility.File;
            Assert.Equal(visibility with { File = file }, read);
        }
    }

    [Theory]
    [MemberData(nameof(ChannelContentVectors.Malformed), MemberType = typeof(ChannelContentVectors))]
    public void Malformed_content_is_rejected(string kind, int argument, string encoded, long bits, string reason)
    {
        BitReader reader = Reader(encoded, bits);
        Action read = kind switch
        {
            "block" => () => ActorContentBlock.Read(reader),
            "field" => () => ActorFieldPayload.Read(reader, argument),
            "mustmap" => () => MustMapGuids.Read(reader),
            "stable" => () => StableSubobjectContent.Read(reader),
            "dynamic" => () => DynamicSubobjectContent.Read(reader),
            "clientpath" => () => ClientSubobjectReference.Read(reader),
            "name" => () => NetworkName.Read(reader),
            "transaction" => () => VisibilityTransaction.Read(reader),
            _ => () => LevelVisibility.Read(reader),
        };
        Assert.Throws<InvalidPacketException>(read);
        Assert.False(string.IsNullOrEmpty(reason));
    }

    [Fact]
    public void Invalid_initializations_are_rejected()
    {
        ActorSpawn spawn = new(20, 11, 0, default);
        Assert.Throws<InvalidPacketException>(() => new ActorInitialization(spawn, null, []).ToBunch(0, 1, []));
        Assert.Throws<InvalidPacketException>(() => new ActorInitialization(spawn, null, []).ToBunch(3, 1, [13, 13]));
        Assert.Throws<InvalidPacketException>(() => new StaticActorInitialization(new StaticActorReference(5), []).ToBunch(3, 1));
        Assert.Throws<InvalidPacketException>(() => new ActorFieldPayload(4, [], 0).Write(new BitWriter(), 4));
        Assert.Throws<InvalidPacketException>(() => new LevelVisibility("A", "None", true, true, new VisibilityTransaction(0, false)).Write(new BitWriter()));
        Assert.Throws<InvalidPacketException>(() => new NetworkName("", 0).Write(new BitWriter()));
    }

    private static BitReader Reader(string encoded, long bits) => new(Convert.FromHexString(encoded), bits);

    private static (string Encoded, long Bits) Encode(Action<BitWriter> write)
    {
        BitWriter writer = new();
        write(writer);
        return (Convert.ToHexStringLower(writer.ToArray()), writer.Length);
    }
}
