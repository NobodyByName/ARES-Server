using Ares.Structs.Core.Geometry;
using Ares.Structs.Core.Serialization;
using Ares.Structs.Net.Replication;

namespace Ares.Structs.Tests.Net;

public sealed class ReplicationWireTests
{
    [Theory]
    [MemberData(nameof(ReplicationWireVectors.Guids), MemberType = typeof(ReplicationWireVectors))]
    public void Guids_match_the_reference(ulong guid, string encoded)
    {
        BitWriter writer = new();
        NetGuid.Write(writer, guid);
        Assert.Equal(encoded, Convert.ToHexStringLower(writer.ToArray()));
        Assert.Equal(guid, NetGuid.Read(new BitReader(Convert.FromHexString(encoded))));
    }

    [Theory]
    [MemberData(nameof(ReplicationWireVectors.BadGuids), MemberType = typeof(ReplicationWireVectors))]
    public void Malformed_guids_are_rejected(string encoded, string reason)
    {
        Assert.Throws<InvalidPacketException>(() => NetGuid.Read(new BitReader(Convert.FromHexString(encoded))));
        Assert.False(string.IsNullOrEmpty(reason));
    }

    [Fact]
    public void Exports_match_the_reference()
    {
        foreach ((ObjectExport[] records, ulong[] roots, string encoded, long bits) in ReplicationWireVectors.Exports)
        {
            ObjectExports exports = new(records);
            BitWriter writer = new();
            exports.Write(writer, roots);
            Assert.Equal((encoded, bits), (Convert.ToHexStringLower(writer.ToArray()), writer.Length));

            (ObjectExports read, IReadOnlyList<ulong> readRoots) = ObjectExports.Read(new BitReader(Convert.FromHexString(encoded), bits));
            Assert.Equal(roots, readRoots);
            Assert.All(read.Records.Values, record => Assert.Equal(exports.Records[record.Guid], record));
        }
    }

    [Theory]
    [MemberData(nameof(ReplicationWireVectors.BadExports), MemberType = typeof(ReplicationWireVectors))]
    public void Malformed_exports_are_rejected(string encoded, long bits, string reason)
    {
        Assert.Throws<InvalidPacketException>(() => ObjectExports.Read(new BitReader(Convert.FromHexString(encoded), bits)));
        Assert.False(string.IsNullOrEmpty(reason));
    }

    [Fact]
    public void Inconsistent_export_sets_are_rejected()
    {
        StaticObjectExport package = new(3, 0, "/Game/Map", 1, true);
        Assert.Throws<InvalidPacketException>(() => new ObjectExports([new StaticObjectExport(5, 3, "Level", 1, true)]));
        Assert.Throws<InvalidPacketException>(() => new ObjectExports([package, package with { Path = "/Game/Other" }]));
        Assert.Throws<InvalidPacketException>(() => new ObjectExports([new StableSubobjectExport(4, 2, "Comp", 1)]));
        Assert.Throws<InvalidPacketException>(() => new ObjectExports([new DynamicActorExport(2), new StableSubobjectExport(4, 2, "Bad.Name", 1)]));
        Assert.Throws<InvalidPacketException>(() => new ObjectExports([new DynamicActorExport(3)]));
        Assert.Throws<InvalidPacketException>(() => new ObjectExports([package]).Write(new BitWriter(), [5]));
        Assert.Throws<InvalidPacketException>(() => new ObjectExports([package]).Write(new BitWriter(), []));
    }

    [Fact]
    public void Spawns_match_the_reference()
    {
        foreach ((ActorSpawn spawn, string encoded, long bits) in ReplicationWireVectors.Spawns)
        {
            BitWriter writer = new();
            spawn.Write(writer);
            Assert.Equal((encoded, bits), (Convert.ToHexStringLower(writer.ToArray()), writer.Length));
            Assert.Equal(spawn, ActorSpawn.Read(new BitReader(Convert.FromHexString(encoded), bits)));
        }

        Assert.Throws<InvalidPacketException>(() => new ActorSpawn(2, 3, 0, new Vector3D(double.NaN, 0, 0)).Write(new BitWriter()));
    }

    [Theory]
    [InlineData(0.0, 90.0, 0.0, "0406000a0004", 47)]
    [InlineData(90.0, 180.0, 270.0, "04060006000a00180018", 79)]
    public void Spawn_rotations_use_presence_bits_and_compressed_short_axes(double pitch, double yaw, double roll, string encoded, long bits)
    {
        ActorSpawn spawn = new(2, 3, 0, default, new Rotator(pitch, yaw, roll));
        BitWriter writer = new();
        spawn.Write(writer);
        Assert.Equal((encoded, bits), (Convert.ToHexStringLower(writer.ToArray()), writer.Length));
        Assert.Equal(spawn, ActorSpawn.Read(new BitReader(Convert.FromHexString(encoded), bits)));
        Assert.Throws<InvalidPacketException>(() => (spawn with { Rotation = new Rotator(0, double.NaN, 0) }).Write(new BitWriter()));
    }

    [Theory]
    [MemberData(nameof(ReplicationWireVectors.BadSpawns), MemberType = typeof(ReplicationWireVectors))]
    public void Malformed_spawns_are_rejected(string encoded, long bits, string reason)
    {
        Assert.Throws<InvalidPacketException>(() => ActorSpawn.Read(new BitReader(Convert.FromHexString(encoded), bits)));
        Assert.False(string.IsNullOrEmpty(reason));
    }

    [Fact]
    public void Static_references_and_controller_open_match_the_reference()
    {
        BitWriter writer = new();
        new StaticActorReference(0x123457).Write(writer);
        Assert.Equal(ReplicationWireVectors.StaticActor, Convert.ToHexStringLower(writer.ToArray()));
        Assert.Equal(new StaticActorReference(0x123457), StaticActorReference.Read(new BitReader(writer.ToArray(), writer.Length)));
        Assert.Throws<InvalidPacketException>(() => new StaticActorReference(4).Write(new BitWriter()));

        PlayerControllerOpen open = new(3, 0xA1B2C3D4, -42);
        writer = new BitWriter();
        open.Write(writer);
        Assert.Equal(ReplicationWireVectors.ControllerOpen, Convert.ToHexStringLower(writer.ToArray()));
        Assert.Equal(open, PlayerControllerOpen.Read(new BitReader(writer.ToArray())));
    }
}
