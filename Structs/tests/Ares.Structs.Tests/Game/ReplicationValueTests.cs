using Ares.Structs.Core.Serialization;
using Ares.Structs.Game.Characters;
using Ares.Structs.Game.Replication;
using Ares.Structs.Net.Control;
using Ares.Structs.Net.Replication;

namespace Ares.Structs.Tests.Game;

public sealed class ReplicationValueTests
{
    private static ReplicationCatalog Catalog => GameReplication.Catalog;

    private static ValueCodec Values(IReadOnlyDictionary<ulong, string>? mappedObjects = null) =>
        new(Catalog, [KnownEnums.NetRole, KnownEnums.GamePhase, KnownEnums.ViewTargetBlendFunction], mappedObjects);

    [Theory]
    [InlineData(-128, 0x80)]
    [InlineData(-1, 0xff)]
    [InlineData(0, 0x00)]
    [InlineData(1, 0x01)]
    [InlineData(127, 0x7f)]
    public void Signed_bytes_use_eight_bits_and_preserve_the_sign(int value, byte encoded)
    {
        ValueCodec codec = Values();
        PropertyDefinition property = Catalog.Property(StructPaths.QuantizedVectorInt8 + ".X");
        BitWriter writer = new();
        codec.Write(writer, property, (sbyte)value);
        Assert.Equal(8, writer.Length);
        Assert.Equal(new[] { encoded }, writer.ToArray());
        Assert.Equal((sbyte)value, Assert.IsType<sbyte>(codec.Read(new BitReader([encoded], 8), property)));
    }

    [Fact]
    public void Property_blocks_match_the_reference_for_every_class()
    {
        PropertyCodec codec = new(Values());
        foreach ((string classPath, Dictionary<string, object?> changes, string encoded, long bits) in ReplicationValueVectors.Properties)
        {
            PropertyLayout layout = new(Catalog, classPath);
            foreach ((string path, object? value) in changes)
            {
                PropertyDefinition property = Catalog.Property(layout.Command(path).Property);
                Assert.True(
                    Accepts(property, value),
                    $"{classPath} {path}: {property.Kind} rejects {value?.GetType().Name}");
            }

            ActorContentBlock block = codec.Encode(layout, changes);
            Assert.Equal((encoded, bits), (Convert.ToHexStringLower(block.Payload), block.PayloadBits));

            Dictionary<string, object?> decoded = codec.Decode(layout, block, swapActorRoles: false);
            Assert.Equal(changes.Keys.Order(StringComparer.Ordinal), decoded.Keys.Order(StringComparer.Ordinal));
            ActorContentBlock again = codec.Encode(layout, decoded);
            Assert.Equal(encoded, Convert.ToHexStringLower(again.Payload));
        }
    }

    [Fact]
    public void Function_calls_match_the_reference_for_every_class()
    {
        RpcCodec codec = new(Values(new Dictionary<ulong, string> { [20] = PlayerClasses.Pawn.Class }));
        foreach ((string classPath, string function, RpcDirection direction, Dictionary<string, object?> parameters, string encoded, long bits)
                 in ReplicationValueVectors.Rpcs)
        {
            ActorContentBlock block = codec.Call(classPath, function, parameters, direction);
            Assert.Equal((encoded, bits), (Convert.ToHexStringLower(block.Payload), block.PayloadBits));

            (NetField field, ActorFieldPayload payload) = Assert.Single(codec.ReadFields(classPath, block, direction));
            Assert.Equal(function, field.Name);
            if (field.Function!.Parameters.Any(path => Catalog.Property(path).Kind == PropertyKind.TextProperty))
            {
                continue;
            }

            Dictionary<string, object?> decoded = codec.ReadParameters(field, payload);
            Assert.Equal(encoded, Convert.ToHexStringLower(codec.Call(classPath, function, decoded, direction).Payload));
        }
    }

    [Fact]
    public void Struct_parameters_without_their_own_serializer_are_sent_as_their_members()
    {
        RpcCodec codec = new(Values(new Dictionary<ulong, string> { [13280] = PlayerClasses.Pawn.Class }));
        Dictionary<string, object?> transition = new()
        {
            [PropertyNames.BlendTime] = 0f,
            [PropertyNames.BlendFunction] = (ulong)ViewTargetBlendFunction.Cubic,
            [PropertyNames.BlendExp] = 2f,
            [PropertyNames.LockOutgoing] = false,
        };
        ActorContentBlock block = codec.Call(PlayerClasses.PlayerController.Class, FunctionNames.ClientSetViewTarget,
            new Dictionary<string, object?> { [PropertyNames.A] = 13280UL, [PropertyNames.TransitionParams] = transition }, RpcDirection.Client);

        (NetField field, ActorFieldPayload payload) = Assert.Single(codec.ReadFields(PlayerClasses.PlayerController.Class, block, RpcDirection.Client));
        Assert.Equal(("839d030000000400000008", 86L), (Convert.ToHexStringLower(payload.Payload), payload.PayloadBits));
        Dictionary<string, object?> decoded = codec.ReadParameters(field, payload);
        Assert.Equal(13280UL, decoded[PropertyNames.A]);
        Assert.Equal(transition, Assert.IsType<Dictionary<string, object?>>(decoded[PropertyNames.TransitionParams]));
    }

    [Fact]
    public void Parameters_at_their_default_are_omitted_and_decode_with_their_type()
    {
        RpcCodec codec = new(Values());
        foreach ((string classPath, string function, RpcDirection direction, Dictionary<string, object?> parameters, _, _) in ReplicationValueVectors.Rpcs)
        {
            Dictionary<string, object?> defaults = parameters.ToDictionary(pair => pair.Key, pair => Zero(pair.Value));
            if (defaults.Values.Any(value => value is null))
            {
                continue;
            }

            (NetField field, ActorFieldPayload payload) = Assert.Single(codec.ReadFields(classPath, codec.Call(classPath, function, defaults, direction), direction));
            if (field.Function!.Parameters.Any(path => Catalog.Property(path).Kind == PropertyKind.TextProperty))
            {
                continue;
            }

            Assert.All(codec.ReadParameters(field, payload), pair => Assert.Equal(defaults[pair.Key], pair.Value));
        }
    }

    private static object? Zero(object? value) => value switch
    {
        float => 0f,
        double => 0d,
        int => 0,
        uint => 0U,
        long => 0L,
        ulong => 0UL,
        short => (short)0,
        ushort => (ushort)0,
        string => "",
        NetworkName => NetworkName.None,
        bool => false,
        _ => null,
    };

    private static bool Accepts(PropertyDefinition property, object? value)
    {
        try
        {
            if (property.Kind != PropertyKind.ArrayProperty)
            {
                Values().Write(new BitWriter(), property, value);
            }

            return true;
        }
        catch (InvalidPacketException)
        {
            return false;
        }
    }

    [Fact]
    public void A_player_state_unique_id_round_trips()
    {
        PropertyCodec codec = new(Values());
        PropertyLayout layout = new(Catalog, PlayerClasses.PlayerState.Class);
        foreach (UniqueNetId id in (UniqueNetId[])[new UniqueNetId(5, "42", "", false, false), new UniqueNetId(31, "0a1b", "Steam", true, false), UniqueNetId.Empty])
        {
            ActorContentBlock block = codec.Encode(layout, new Dictionary<string, object?> { [PropertyNames.UniqueId] = id });
            Assert.Equal(id, codec.Decode(layout, block, swapActorRoles: false)[PropertyNames.UniqueId]);
        }
    }

    [Fact]
    public void Object_parameters_need_a_mapped_object_of_the_right_class()
    {
        RpcCodec codec = new(Values(new Dictionary<ulong, string> { [20] = PlayerClasses.Pawn.Class }));
        Dictionary<string, object?> restart = new() { ["NewPawn"] = 20UL };
        ActorContentBlock block = codec.Call("/Script/Engine.PlayerController", "ClientRestart", restart, RpcDirection.Client);
        Assert.Equal((ReplicationValueVectors.ClientRestart, ReplicationValueVectors.ClientRestartBits), (Convert.ToHexStringLower(block.Payload), block.PayloadBits));
        Assert.Equal(20UL, codec.ReadCall("/Script/Engine.PlayerController", block, RpcDirection.Client).Parameters["NewPawn"]);

        Assert.Throws<InvalidPacketException>(() => codec.Call("/Script/Engine.PlayerController", "ClientRestart", new Dictionary<string, object?> { ["NewPawn"] = 22UL }, RpcDirection.Client));
        Assert.Throws<InvalidPacketException>(() => codec.Call("/Script/Engine.PlayerController", "ClientRestart", restart, RpcDirection.Server));
        Assert.Throws<InvalidPacketException>(() => codec.Call("/Script/Engine.PlayerController", "ClientRestart", new Dictionary<string, object?>(), RpcDirection.Client));
    }
}
