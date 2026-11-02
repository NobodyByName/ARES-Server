using Ares.Structs.Core.Geometry;
using Ares.Structs.Net.Replication;

namespace Ares.Structs.Tests.Net;

/// <summary>Reference encodings for the replication wire tests (bytes in hex).</summary>
internal static class ReplicationWireVectors
{
    /// <summary>GUID, encoded GUID.</summary>
    public static TheoryData<ulong, string> Guids => new()
    {
        { 0UL, "00" },
        { 1UL, "02" },
        { 2UL, "04" },
        { 3UL, "06" },
        { 127UL, "fe" },
        { 128UL, "0102" },
        { 16383UL, "fffe" },
        { 16384UL, "010102" },
        { 4294967295UL, "ffffffff1e" },
        { 4294967296UL, "0101010120" },
        { 1098524073332020107UL, "174f5d0363755f3f1e" },
        { 3434298343398869075UL, "a72105b1b9098ba95e" },
        { 5596765429999857094UL, "8db79f252f21dbab9a" },
        { 9223372036854775807UL, "fffffffffffffffffe" },
        { 9223372036854775808UL, "01010101010101010102" },
        { 10835337665455605072UL, "a11523af5b8d6d5f2d02" },
        { 17796639924064796018UL, "e5f555570d6f2ffbed02" },
        { 18446744073709551615UL, "ffffffffffffffffff02" },
    };

    /// <summary>Malformed GUID, what is wrong.</summary>
    public static TheoryData<string, string> BadGuids => new()
    {
        { "ffffffffffffffffff04", "GUID above 64 bits" },
        { "ffffffffffffffffffff", "GUID that never ends" },
        { "ffffff", "truncated GUID" },
    };

    /// <summary>Export records, roots, encoded exports, bit length.</summary>
    public static IReadOnlyList<(ObjectExport[] Records, ulong[] Roots, string Encoded, long Bits)> Exports { get; } =
    [
        ([new StaticObjectExport(3UL, 0UL, "/Game/Maps/Dam", 286331153U, true)], [3UL], "020000000c0e001e0000005e8ec2daca5e9ac2e0e65e88c2da002222222200", 241),
        ([new StaticObjectExport(3UL, 0UL, "/Game/Maps/Dam", 286331153U, true), new StaticObjectExport(5UL, 3UL, "Dam", 572662306U, true), new StaticObjectExport(7UL, 5UL, "PersistentLevel", 0U, false)], [7UL], "020000001c0a140e0c0e001e0000005e8ec2daca5e9ac2e0e65e88c2da00222222220800000088c2da004444444420000000a0cae4e6d2e6e8cadce898caeccad8000000000000", 561),
        ([new StaticObjectExport(3UL, 0UL, "/Game/Maps/Dam", 286331153U, true), new StaticObjectExport(5UL, 3UL, "Dam", 572662306U, true), new StaticObjectExport(7UL, 5UL, "PersistentLevel", 0U, false), new StaticObjectExport(9UL, 0UL, "/Script/Engine", 305419896U, false), new StaticObjectExport(11UL, 9UL, "Default__Actor", 3735928559U, false)], [7UL, 11UL, 5UL], "060000001c0a140e0c0e001e0000005e8ec2daca5e9ac2e0e65e88c2da00222222220800000088c2da004444444420000000a0cae4e6d2e6e8cadce898caeccad800000000002c0a240a001e0000005ea6c6e4d2e0e85e8adcced2dcca00f0ac68241e00000088caccc2ead8e8bebe82c6e8dee400de7d5bbd150e0c0e001e0000005e8ec2daca5e9ac2e0e65e88c2da00222222220800000088c2da004444444400", 1289),
        ([new DynamicActorExport(20UL), new StableSubobjectExport(22UL, 20UL, "CharMoveComp", 3405691582U)], [22UL], "02000000580e500c1a00000086d0c2e49adeecca86dedae0007c75fd9501", 233),
        ([new DynamicActorExport(20UL)], [20UL], "02000000500c00", 49),
        ([new StaticObjectExport(9UL, 0UL, "/Script/Engine", 305419896U, false), new StaticObjectExport(11UL, 9UL, "Default__Actor", 3735928559U, false), new DynamicActorExport(20UL), new StableSubobjectExport(22UL, 20UL, "CharMoveComp", 3405691582U)], [11UL, 22UL, 20UL], "060000002c0a240a001e0000005ea6c6e4d2e0e85e8adcced2dcca00f0ac68241e00000088caccc2ead8e8bebe82c6e8dee400de7d5bbd590e500c1a00000086d0c2e49adeecca86dedae0007c75fd95510c00", 657),
    ];

    /// <summary>Malformed exports, bit length, what is wrong.</summary>
    public static TheoryData<string, long, string> BadExports => new()
    {
        { "030000000c0a0004000000f0000000000000", 137, "replay exports" },
        { "0000000000", 33, "no roots" },
        { "020000000c0200", 49, "export without path and checksum" },
        { "020000000000", 41, "null root" },
        { "02000000100a0004000000f0000000000000", 137, "stable subobject that may load" },
        { "02000000100e0004000000f0000000000000", 137, "stable subobject without actor root" },
        { "020000000c0a0004000000f0000000000002", 138, "trailing bits" },
        { "040000000c0a0004000000f000000000000c0a0004000000f2000000000000", 241, "conflicting definitions" },
        { "020000000c0a0c0a0004000000f0000000000004000000f0000000000000", 233, "object as its own outer" },
    };

    /// <summary>Spawn, encoded spawn, bit length.</summary>
    public static IReadOnlyList<(ActorSpawn Spawn, string Encoded, long Bits)> Spawns { get; } =
    [
        (new ActorSpawn(2UL, 3UL, 0UL, new Vector3D(0.0, 0.0, 0.0)), "04060000", 28),
        (new ActorSpawn(20UL, 11UL, 7UL, new Vector3D(100.5, -2.25, 30000.0)), "28160e01000000008064010100000000000800030000000030750301", 221),
        (new ActorSpawn(18446744073709551614UL, 18446744073709551615UL, 0UL, new Vector3D(0.0, 0.0, 1e-09)), "fdffffffffffffffff02ffffffffffffffffff020001000000000000000000000000000000545a9ba02fb844f800", 365),
    ];

    /// <summary>Malformed spawn, bit length, what is wrong.</summary>
    public static TheoryData<string, long, string> BadSpawns => new()
    {
        { "04060003", 26, "quantized location" },
        { "04060002", 28, "initial rotation" },
        { "04060000", 29, "trailing bits" },
        { "06060000", 28, "static GUID for a spawned actor" },
        { "04080000", 28, "dynamic archetype" },
        { "04060200", 28, "level GUID 1" },
    };

    /// <summary>Static actor reference to GUID 1193047.</summary>
    public const string StaticActor = "afd190";

    /// <summary>Controller open suffix: player 3, handshake 0xA1B2C3D4, connection -42.</summary>
    public const string ControllerOpen = "03d4c3b2a1d6ffffff";
}
