using Ares.Structs.Core.Serialization;
using Ares.Structs.Game.Characters;
using Ares.Structs.Game.Replication;
using Ares.Structs.Net.Replication;

namespace Ares.Structs.Tests.Game;

public sealed class SquadProfileTests
{
    private const string Encoded = "bd37af269e158d041400000004c995cd0114000000c4c8ccd00024000000c0b185d199bdc9b5011400000004c995cd0100";
    private const int ProfileBits = 386;

    private static CompactPlayerProfile Profile => new(new TenancyUserId(0x0123456789abcdef), "Ares", "1234", "platform", "Ares");

    [Fact]
    public void A_compact_profile_has_the_expected_bit_layout()
    {
        BitWriter writer = new();
        Profile.Write(writer);
        Assert.Equal((Encoded, ProfileBits), (Convert.ToHexStringLower(writer.ToArray()), writer.Length));
        BitReader reader = new(Convert.FromHexString(Encoded), ProfileBits);
        Assert.Equal(Profile, CompactPlayerProfile.Read(reader));
        Assert.Equal(0, reader.Remaining);
        for (int length = 0; length < ProfileBits; length++)
        {
            Assert.Throws<InvalidPacketException>(() => CompactPlayerProfile.Read(new BitReader(writer.ToArray(), length)));
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(3)]
    public void Unsupported_tenancy_id_encodings_are_rejected(byte encoding) =>
        Assert.Throws<InvalidPacketException>(() => TenancyUserId.Read(new BitReader([encoding, 0, 0, 0, 0, 0, 0, 0, 0], 66)));

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(12)]
    public void Squad_profile_calls_preserve_every_profile(int count)
    {
        RpcCodec codec = new(new ValueCodec(GameReplication.Catalog, []));
        object?[] profiles = [.. Enumerable.Repeat<object?>(Profile, count)];
        ActorContentBlock block = codec.Call(PlayerClasses.PlayerController.Class, FunctionNames.ClientReceiveSquadPlayersProfiles,
            new Dictionary<string, object?> { [PropertyNames.Profiles] = profiles }, RpcDirection.Client);
        (_, Dictionary<string, object?> decoded) = codec.ReadCall(PlayerClasses.PlayerController.Class, block, RpcDirection.Client);
        Assert.Equal(profiles, Assert.IsType<IReadOnlyList<object?>>(decoded[PropertyNames.Profiles], exactMatch: false));
    }

    [Fact]
    public void Profile_arrays_are_bounded_on_read_and_write()
    {
        ValueCodec values = new(GameReplication.Catalog, []);
        PropertyDefinition property = GameReplication.Catalog.Property(ClassPaths.EmbarkPlayerController + "." + FunctionNames.ClientReceiveSquadPlayersProfiles + "." + PropertyNames.Profiles);
        Assert.Throws<InvalidPacketException>(() => values.Write(new BitWriter(), property, Enumerable.Repeat<object?>(Profile, 129).ToArray()));
        Assert.Throws<InvalidPacketException>(() => values.Read(new BitReader([129, 0]), property));
    }
}
