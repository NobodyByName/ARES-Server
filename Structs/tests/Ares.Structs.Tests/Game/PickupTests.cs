using Ares.Structs.Core.Geometry;
using Ares.Structs.Core.Serialization;
using Ares.Structs.Game.Characters;
using Ares.Structs.Game.Loot;
using Ares.Structs.Game.Replication;
using Ares.Structs.Game.World;
using Ares.Structs.Net.Replication;

namespace Ares.Structs.Tests.Game;

public sealed class PickupTests
{
    private const ulong Pickup = 1000;
    private const ulong RootCollider = Pickup + 4;
    private const ulong DataAsset = 101;
    private const ulong Picker = 2000;

    private static ReplicationCatalog Catalog => GameReplication.Catalog;

    private static PropertyCodec Codec() => new(new ValueCodec(
        Catalog,
        [KnownEnums.NetRole, KnownEnums.ItemSources, DamDoors.InteractionState],
        new Dictionary<ulong, string>
        {
            [DataAsset] = Pickups.DataAssetClass,
            [Picker] = PlayerClasses.Pawn.Class,
            [RootCollider] = ClassPaths.StaticMeshComponent,
        }));

    private static PickupProperties PlayerDrop() => new()
    {
        Role = NetRole.Authority,
        RemoteRole = NetRole.SimulatedProxy,
        Movement = new RepMovement(new Vector3D(1000, -2000, 300), new Rotator(0, 90, 0)) { RepPhysics = true },
        DataAsset = DataAsset,
        Item = new PickupItem
        {
            DataAssetIndex = 12,
            Quality = 0,
            Amount = 25,
            AmmoInClip = 0,
            Guid = new Guid("01234567-89ab-cdef-fedc-ba9876543210"),
            ItemSource = 20,
        },
        DropServerWorldTime = 60.5,
        DroppingPlayerId = 300,
    };

    [Fact]
    public void Pickup_class_checksum_is_the_blueprint_export_checksum()
    {
        uint checksum = Catalog.Class(Pickups.ActorClass).Checksum;
        Assert.Equal(0x04CBEEF5U, checksum);
        Assert.Equal(checksum, Catalog.ReferenceClasses[Pickups.Class.Class].ExportChecksum);
        Assert.True(Catalog.IsA(Pickups.Class.Class, ClassPaths.EmbarkActor));
        Assert.True(Catalog.IsA(Pickups.DataAssetClass, ClassPaths.PrimaryDataAsset));
    }

    [Fact]
    public void Pickup_members_use_the_wire_handles()
    {
        PropertyLayout layout = new(Catalog, Pickups.ActorClass);
        const string Item = PropertyNames.ContainedItemBb;
        (string Path, int Handle)[] expected =
        [
            (PropertyNames.ReplicateMovement, 1),
            (PropertyNames.RemoteRole, 5),
            (PropertyNames.ReplicatedMovement, 12),
            (PropertyNames.Role, 14),
            (PropertyNames.DefaultPickupDataAsset, 17),
            ($"{Item}.{PropertyNames.DataAssetIndex}", 18),
            ($"{Item}.{PropertyNames.Quality}", 19),
            ($"{Item}.{PropertyNames.Amount}.{PropertyNames.Value}", 23),
            ($"{Item}.{PropertyNames.QuantizedDurability}", 24),
            ($"{Item}.{PropertyNames.AmmoInClip}", 25),
            ($"{Item}.{PropertyNames.Guid}.{PropertyNames.A}", 27),
            ($"{Item}.{PropertyNames.Guid}.{PropertyNames.D}", 30),
            ($"{Item}.{PropertyNames.ItemSource}", 31),
            (PropertyNames.ContainedModVisualIndices, 34),
            (PropertyNames.DropServerWorldTime, 35),
            (PropertyNames.DroppingPlayerId, 36),
            (PropertyNames.PickedUpBy, 37),
        ];
        foreach ((string path, int handle) in expected)
        {
            Assert.Equal(handle, layout.Command(path).Handle);
        }
    }

    [Fact]
    public void Player_drop_round_trips_through_the_property_codec()
    {
        PropertyCodec codec = Codec();
        PropertyLayout layout = new(Catalog, Pickups.ActorClass);
        PickupProperties drop = PlayerDrop();

        ActorContentBlock block = codec.Encode(layout, drop.ToValues());
        Dictionary<string, object?> decoded = codec.Decode(layout, block, swapActorRoles: false);

        Assert.Equal(
            [5, 12, 14, 17, 18, 19, 23, 25, 27, 28, 29, 30, 31, 35, 36],
            decoded.Keys.Select(path => layout.Command(path).Handle).Order());
        Assert.Equal(drop, PickupProperties.FromValues(decoded));
    }

    [Fact]
    public void Item_identity_words_follow_the_hexadecimal_groups()
    {
        Dictionary<string, object?> values = PlayerDrop().ToValues();
        const string Guid = $"{PropertyNames.ContainedItemBb}.{PropertyNames.Guid}";

        Assert.Equal(0x01234567, values[$"{Guid}.{PropertyNames.A}"]);
        Assert.Equal(unchecked((int)0x89abcdef), values[$"{Guid}.{PropertyNames.B}"]);
        Assert.Equal(unchecked((int)0xfedcba98), values[$"{Guid}.{PropertyNames.C}"]);
        Assert.Equal(0x76543210, values[$"{Guid}.{PropertyNames.D}"]);
    }

    [Fact]
    public void Pickup_state_sends_only_the_changed_members()
    {
        PropertyCodec codec = Codec();
        PropertyLayout layout = new(Catalog, Pickups.ActorClass);
        PickupProperties pickedUp = new()
        {
            ReplicateMovement = false,
            Item = new PickupItem { Amount = 0 },
            PickedUpBy = Picker,
        };

        Dictionary<string, object?> decoded = codec.Decode(layout, codec.Encode(layout, pickedUp.ToValues()), swapActorRoles: false);

        Assert.Equal([1, 23, 37], decoded.Keys.Select(path => layout.Command(path).Handle).Order());
        Assert.Equal(pickedUp, PickupProperties.FromValues(decoded));
    }

    [Fact]
    public void Item_sources_use_five_bits()
    {
        PropertyDefinition property = Catalog.Property($"{StructPaths.BbItemContainerItem}.{PropertyNames.ItemSource}");
        ValueCodec values = new(Catalog, [KnownEnums.ItemSources]);
        BitWriter writer = new();

        values.Write(writer, property, 20UL);

        Assert.Equal(5, writer.Length);
        Assert.Equal(20UL, Assert.IsType<ulong>(values.Read(new BitReader(writer.ToArray(), writer.Length), property)));
    }

    [Fact]
    public void Interaction_points_its_prompt_at_the_root_collider()
    {
        PropertyCodec codec = Codec();
        PropertyLayout layout = new(Catalog, Pickups.InteractionClass);
        Dictionary<string, object?> values = new()
        {
            [PropertyNames.AttachParent] = RootCollider,
            [PropertyNames.CurrentInteractionState] = (ulong)CurrentInteractionState.Idle,
            [PropertyNames.ProxyPromptComponentOrStyle] = new ComponentOrStyle(RootCollider),
        };

        Dictionary<string, object?> decoded = codec.Decode(layout, codec.Encode(layout, values), swapActorRoles: false);

        Assert.Equal(values, decoded);
    }

    [Fact]
    public void Component_reference_is_two_flags_and_the_component_id()
    {
        BitWriter writer = new();
        new ComponentOrStyle(RootCollider).Write(writer);
        BitReader reader = new(writer.ToArray(), writer.Length);

        Assert.True(reader.ReadBit());
        Assert.False(reader.ReadBit());
        Assert.Equal(RootCollider, NetGuid.Read(reader));
        Assert.Equal(0, reader.Remaining);
    }

    [Fact]
    public void Style_references_are_rejected()
    {
        BitWriter writer = new();
        writer.WriteBit(true);
        writer.WriteBit(true);
        NetGuid.Write(writer, RootCollider);

        Assert.Throws<InvalidPacketException>(() => ComponentOrStyle.Read(new BitReader(writer.ToArray(), writer.Length)));
    }

    [Fact]
    public void Movement_at_rest_without_physics_has_no_angular_velocity()
    {
        RepMovement movement = new(new Vector3D(-1500, 2500, -400), new Rotator(357.1875, 90, 2.8125));
        BitWriter writer = new();
        movement.Write(writer);
        BitReader reader = new(writer.ToArray(), writer.Length);

        Assert.Equal(0UL, reader.ReadBits(4));
        Assert.Equal(new Vector3D(-1500, 2500, -400), QuantizedVector.Read(reader, 1));
        Assert.Equal(new Rotator(357.1875, 90, 2.8125), new Rotator(Axis(reader), Axis(reader), Axis(reader)));
        Assert.Equal(default, QuantizedVector.Read(reader, 1));
        Assert.False(reader.ReadBit());
        Assert.Equal(0, reader.Remaining);
        Assert.Equal(movement, RepMovement.Read(new BitReader(writer.ToArray(), writer.Length)));
    }

    [Fact]
    public void Movement_with_physics_sends_angular_velocity_and_flags()
    {
        RepMovement movement = new(new Vector3D(1000, -2000, 210), new Rotator(45, 180, 0))
        {
            LinearVelocity = new Vector3D(-2, 1, -98),
            AngularVelocity = new Vector3D(6, -15, 0),
            RepPhysics = true,
            SimulatedPhysicSleep = true,
            ServerFrame = 1234,
        };
        BitWriter writer = new();
        movement.Write(writer);

        Assert.Equal(0b0111UL, new BitReader(writer.ToArray(), writer.Length).ReadBits(4));
        Assert.Equal(movement, RepMovement.Read(new BitReader(writer.ToArray(), writer.Length)));
    }

    [Fact]
    public void Movement_rejects_angular_velocity_without_physics_and_a_set_trailing_bit()
    {
        RepMovement spinning = new(default, default) { AngularVelocity = new Vector3D(1, 0, 0) };
        Assert.Throws<InvalidPacketException>(() => spinning.Write(new BitWriter()));

        BitWriter writer = new();
        new RepMovement(default, default).Write(writer);
        byte[] data = writer.ToArray();
        long last = writer.Length - 1;
        data[last / 8] |= (byte)(1 << (int)(last % 8));
        Assert.Throws<InvalidPacketException>(() => RepMovement.Read(new BitReader(data, writer.Length)));
    }

    private static double Axis(BitReader reader) => reader.ReadBit() ? reader.ReadByte() * 360.0 / 256 : 0;
}
