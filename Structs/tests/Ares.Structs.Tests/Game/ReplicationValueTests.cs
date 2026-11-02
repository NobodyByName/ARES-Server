using Ares.Structs.Core.Serialization;
using Ares.Structs.Game.Characters;
using Ares.Structs.Game.Replication;
using Ares.Structs.Net.Replication;

namespace Ares.Structs.Tests.Game;

public sealed class ReplicationValueTests
{
    private static ReplicationCatalog Catalog => GameReplication.Catalog;

    private static ValueCodec Values(IReadOnlyDictionary<ulong, string>? mappedObjects = null) =>
        new(Catalog, [KnownEnums.NetRole, KnownEnums.GamePhase], mappedObjects);

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
