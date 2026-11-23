using System.Collections;
using Ares.Common.Game.Enemies;
using Ares.Raid.World.Actors;
using Ares.Raid.World.Players;
using Ares.Raid.World.Replication;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Core.Serialization;
using Ares.Structs.Game.Enemies;
using Ares.Structs.Game.Inventory;
using Ares.Structs.Game.Pendulum;
using Ares.Structs.Game.Replication;
using Ares.Structs.Game.World;
using Ares.Structs.Net.Replication;
using Ares.Structs.Net.Session;

namespace Ares.Raid.Tests.World;

public sealed class ArcTests
{
    [Fact]
    public void A_spawned_wasp_hovers_in_front_of_the_character_and_reaches_its_client_in_the_game_s_spawn_shape()
    {
        ScriptedPlayer session = new();
        Vector3D at = session.Member.Spawn.Location;
        (uint frame, double time) = Ready(session, at);
        session.Sent.Clear();

        Assert.Equal(AdminRefusal.None, session.Channels.SpawnArc(Arcs.Wasp, out WorldArc? spawned));
        WorldArc arc = Assert.IsType<WorldArc>(spawned);
        Vector3D feet = session.Channels.CharacterPosition!.Value;
        double yaw = session.Channels.CharacterYaw!.Value;
        Assert.Equal(new Vector3D(Math.Round(feet.X + (350 * Math.Cos(yaw))), Math.Round(feet.Y + (350 * Math.Sin(yaw))), Math.Round(feet.Z + (2 * TestGameData.Dam.PlayerStartCapsule.HalfHeight))),
            arc.Location);
        Assert.Equal(((yaw * 180 / Math.PI) + 180 + 360) % 360, arc.Rotation.Yaw, 1);

        (frame, time) = Stand(session, frame, time, at, 3);
        Assert.True(session.Channels.HasArc(arc.Index));
        Assert.Equal(1, session.Channels.ArcsReady);
        Assert.Empty(session.Channels.ArcFailures);

        // Ids as the game lays them out: the actor, an unused id, four attribute sets, the ability system.
        Assert.False(NetGuid.IsStatic(arc.Guid));
        Assert.Equal([arc.Guid + 4, arc.Guid + 6, arc.Guid + 8, arc.Guid + 10], arc.AttributeSets);
        Assert.Equal(arc.Guid + 12, arc.AbilitySystem);

        // The class default and the attribute set classes: those the world already exports keep their ids.
        WorldArcs arcs = session.Raid.Arcs;
        ulong archetype = arcs.Archetype(Arcs.Wasp);
        ChannelBunch exports = Assert.Single(session.Sent, bunch => bunch.PackageExports && Exports(bunch).Roots.Contains(archetype));
        (ObjectExports records, IReadOnlyList<ulong> roots) = Exports(exports);
        Assert.Equal([archetype, .. arcs.AttributeSetClasses], roots);
        StaticObjectExport classDefault = Assert.IsType<StaticObjectExport>(records.Records[archetype]);
        Assert.Equal(("Default__C_LightDrone_02_C", 0U), (classDefault.Path, classDefault.Checksum));
        Assert.Equal("/Game/Pioneer/Enemies/LightDrone/Wasp/C_LightDrone_02", Assert.IsType<StaticObjectExport>(records.Records[classDefault.OuterGuid]).Path);
        StaticObjectExport partSet = Assert.IsType<StaticObjectExport>(records.Records[arcs.AttributeSetClasses[0]]);
        Assert.Equal(("ConstructableDamageModelAttributeSet_PartSet0", 0U), (partSet.Path, partSet.Checksum));
        Assert.Equal("/Script/EmbarkConstructable", Assert.IsType<StaticObjectExport>(records.Records[partSet.OuterGuid]).Path);
        StaticObjectExport constructable = Assert.IsType<StaticObjectExport>(records.Records[arcs.AttributeSetClasses[1]]);
        Assert.Equal(("PioneerConstructableAttributeSet", 0x7ED6FA3DU), (constructable.Path, constructable.Checksum));
        Assert.Equal(session.Raid.Imports.Classes[ClassPaths.PioneerAttackerAttributeSet], arcs.AttributeSetClasses[2]);
        Assert.Equal(session.Raid.Imports.Classes[ClassPaths.PioneerCharacterAttributeSet], arcs.AttributeSetClasses[3]);

        // The opening: archetype at the spawn transform, simulated proxy of an authority, its own instigator, no owner or
        // controller, its blackboard's first frame, then the four attribute sets created empty inside it.
        ChannelBunch open = Assert.Single(session.Sent, bunch => Spawns(bunch, arc));
        BitReader reader = new(open.Payload, open.PayloadBits);
        Assert.Equal([archetype, .. arcs.AttributeSetClasses], MustMapGuids.Read(reader).Guids);
        BitWriter spawn = new();
        new ActorSpawn(arc.Guid, archetype, 0, arc.Location, arc.Rotation).Write(spawn);
        Assert.Equal(spawn.ToArray(), Bits(reader, spawn.Length));
        ActorContentBlock properties = session.Raid.State.Replication.Encode(Arcs.ActorClass, new Dictionary<string, object?>
        {
            [PropertyNames.RemoteRole] = (ulong)NetRole.SimulatedProxy,
            [PropertyNames.Role] = (ulong)NetRole.Authority,
            [PropertyNames.Instigator] = arc.Guid,
        }, new Dictionary<ulong, string> { [arc.Guid] = Arcs.ActorClass });
        ActorContentBlock first = WorldArcs.Frame(arc, 0, ArcFrames.First, WorldArcs.State(arc, arcs.Pose(arc)), new Dictionary<int, PendulumSlot>());
        ActorContentBlock actor = ActorContentBlock.Read(reader);
        ActorContentBlock expected = WorldState.Merge(properties, first);
        Assert.Equal((expected.PayloadBits, true), (actor.PayloadBits, actor.HasRepLayout));
        Assert.Equal(expected.Payload, actor.Payload);
        for (int index = 0; index < Arcs.AttributeSets.Count; index++)
        {
            DynamicSubobjectContent set = DynamicSubobjectContent.Read(reader);
            Assert.Equal((arc.AttributeSets[index], arcs.AttributeSetClasses[index], (ulong?)null), (set.Guid, set.ClassGuid, set.OuterGuid));
            Assert.Equal((false, 0L), (set.Content.HasRepLayout, set.Content.PayloadBits));
        }

        Assert.Equal(0, reader.Remaining);

        // The ability system is named inside the actor, then lists the attribute sets with the ARC as owner and avatar.
        ChannelBunch naming = Assert.Single(session.Sent, bunch => bunch.PackageExports && Exports(bunch).Roots.Contains(arc.AbilitySystem));
        StableSubobjectExport component = Assert.IsType<StableSubobjectExport>(Exports(naming).Exports.Records[arc.AbilitySystem]);
        Assert.Equal((arc.Guid, "AbilitySystem", 0xC2AC3758U), (component.OuterGuid, component.Path, component.Checksum));
        ChannelBunch abilities = Assert.Single(session.Sent, bunch => bunch.Channel == open.Channel && bunch is { Opened: false, CloseReason: null });
        reader = new BitReader(abilities.Payload, abilities.PayloadBits);
        StableSubobjectContent content = StableSubobjectContent.Read(reader);
        Assert.Equal(0, reader.Remaining);
        Assert.Equal(arc.AbilitySystem, content.Guid);
        Dictionary<ulong, string> objects = new() { [arc.Guid] = Arcs.ActorClass };
        foreach (ulong set in arc.AttributeSets)
        {
            objects[set] = ClassPaths.AttributeSet;
        }

        Dictionary<string, object?> values = Decode(Arcs.AbilitySystemClass, content.Content, objects);
        Assert.Equal(3, values.Count);
        Assert.Equal((arc.Guid, arc.Guid), ((ulong)values[PropertyNames.OwnerActor]!, (ulong)values[PropertyNames.AvatarActor]!));
        Assert.Equal(arc.AttributeSets.Select(guid => (object?)guid), ((IEnumerable)values[PropertyNames.SpawnedAttributes]!).Cast<object?>());

        // The channel stays open: nothing more is sent while the ARC stands there.
        int sent = session.Sent.Count(bunch => bunch.Channel == open.Channel);
        Stand(session, frame, time, at, 2);
        Assert.Equal(sent, session.Sent.Count(bunch => bunch.Channel == open.Channel));
        Assert.True(session.Channels.HasArc(arc.Index));
    }

    [Fact]
    public void A_wasp_spawned_at_a_spawner_flies_its_route_and_its_client_follows_it_frame_by_frame()
    {
        ScriptedPlayer session = new();
        Vector3D at = session.Member.Spawn.Location;
        (uint frame, double time) = Ready(session, at);
        ArcSpawner spawner = TestGameData.Raid.ArcSpawners.First(item => item is { EnemyTable: "EGT_StaticWorld_WaspSingle", Routes.Count: 1 });
        Assert.Equal(AdminRefusal.NoSuchSpawner, session.Channels.SpawnArcAt(Arcs.Wasp, "no-such-spawner", out _));
        Assert.Equal(AdminRefusal.SpawnerLacksArc,
            session.Channels.SpawnArcAt(Arcs.Wasp, TestGameData.Raid.ArcSpawners.First(item => !item.EnemyTypes.Contains(Arcs.Wasp.EnemyType)).Id, out _));
        Assert.Equal(AdminRefusal.None, session.Channels.SpawnArcAt(Arcs.Wasp, spawner.Id.ToUpperInvariant(), out WorldArc? spawned));
        WorldArc arc = Assert.IsType<WorldArc>(spawned);
        Assert.Same(spawner, arc.Spawner);
        // It starts at the home point of its route nearest the spawner.
        SplinePathPlacement route = TestGameData.Dam.SplinePaths.Single(path => path.Name == spawner.Routes[0]);
        ArcHomePoints homes = ArcRoutes.HomePoints(spawner, route, Math.Max(1.4 * spawner.GroupSearchRadius, 1000));
        Assert.Equal(homes.Points[homes.Nearest(spawner.Location)], arc.Location);

        // The player stands next to it, so it opens on the player's client.
        session.Raid.Arcs.Advance(time);
        (frame, time) = Stand(session, frame, time, arc.Location, 3);
        Assert.True(session.Channels.HasArc(arc.Index));
        int channel = session.Sent.Single(bunch => Spawns(bunch, arc)).Channel;
        session.Sent.Clear();
        Vector3D start = session.Raid.Arcs.Pose(arc).Location;
        Stand(session, frame, time, arc.Location, 6);
        Vector3D end = session.Raid.Arcs.Pose(arc).Location;
        Assert.True(Distance(start, end) > 100, "the wasp moved");

        // Unreliable frames, each coded against an earlier one, the last at the ARC's current position.
        ChannelBunch[] frames = [.. session.Sent.Where(bunch => bunch.Channel == channel && bunch is { Opened: false, CloseReason: null })];
        Assert.True(frames.Length >= 10, $"{frames.Length} frames");
        ArcBlackboard blackboard = Arcs.Wasp.Blackboard;
        uint latest = 0;
        foreach (ChannelBunch bunch in frames)
        {
            ActorContentBlock block = ActorContentBlock.Read(new BitReader(bunch.Payload, bunch.PayloadBits));
            ActorFieldPayload payload = ActorFieldPayload.Read(new BitReader(block.Payload, block.PayloadBits), blackboard.FieldCount);
            Assert.Equal(blackboard.Field, payload.Index);
            BitReader reader = new(payload.Payload, payload.PayloadBits);
            Assert.True(reader.ReadBit());
            BlackboardUpdateHeader header = BlackboardUpdate.ReadHeader(reader);
            Assert.Equal(PendulumFieldFlags.Others, header.Fields);
            Assert.True(header.Latest > latest && header.Baseline < header.Latest);
            latest = header.Latest;
        }
    }

    [Fact]
    public void Despawned_arcs_are_destroyed_on_the_client_and_distant_ones_never_reach_it()
    {
        ScriptedPlayer session = new();
        Vector3D at = session.Member.Spawn.Location;
        (uint frame, double time) = Ready(session, at);
        WorldArc near = session.Raid.Arcs.Spawn(Arcs.Wasp, at with { X = at.X + 500 }, default)!;
        WorldArc far = session.Raid.Arcs.Spawn(Arcs.Wasp, at with { X = at.X + Arcs.NetCullDistance + 1 }, default)!;
        (frame, time) = Stand(session, frame, time, at, 3);
        Assert.True(session.Channels.HasArc(near.Index));
        Assert.False(session.Channels.HasArc(far.Index));
        Assert.DoesNotContain(session.Sent, bunch => Spawns(bunch, far));
        int channel = session.Sent.Single(bunch => Spawns(bunch, near)).Channel;

        Assert.Equal(2, session.Channels.DespawnArcs());
        Stand(session, frame, time, at, 2);
        Assert.False(session.Channels.HasArc(near.Index));
        Assert.Equal(0, session.Channels.ArcChannelsInUse);
        ChannelBunch close = session.Sent.Last(bunch => bunch.Channel == channel);
        Assert.Equal(Bunches.Destroyed, close.CloseReason);
        Assert.Equal(0, session.Channels.DespawnArcs());
    }

    [Fact]
    public void A_raid_spawns_a_bounded_number_of_arcs_with_ids_clear_of_the_pickups()
    {
        ScriptedPlayer session = new();
        WorldArc first = session.Raid.Arcs.Spawn(Arcs.Wasp, session.Member.Spawn.Location, default)!;
        ushort duck = checked((ushort)TestGameData.Items.Definitions.UsableIndex(TestGameData.RubberDuck.AssetId));
        WorldPickup pickup = session.Raid.Pickups.Drop(1, 0, new ContainerItem(duck, Guid.NewGuid(), 1, 1), session.Member.Spawn.Location, default, 0);
        Assert.True(first.Guid > pickup.Guid + (6UL * 10_000_000));
        for (int index = 1; index < WorldArcs.MaxArcs; index++)
        {
            Assert.NotNull(session.Raid.Arcs.Spawn(Arcs.Wasp, session.Member.Spawn.Location, default));
        }

        Assert.Null(session.Raid.Arcs.Spawn(Arcs.Wasp, session.Member.Spawn.Location, default));
        Assert.Same(Arcs.Wasp, Arcs.Find("WASP"));
        Assert.Null(Arcs.Find("hornet"));
    }

    private static (ObjectExports Exports, IReadOnlyList<ulong> Roots) Exports(ChannelBunch bunch) => ObjectExports.Read(new BitReader(bunch.Payload, bunch.PayloadBits));

    // Whether a bunch opens a channel spawning the ARC.
    private static bool Spawns(ChannelBunch bunch, WorldArc arc)
    {
        if (!bunch.Opened || bunch.CloseReason is not null || !bunch.MustBeMapped)
        {
            return false;
        }

        BitReader reader = new(bunch.Payload, bunch.PayloadBits);
        MustMapGuids.Read(reader);
        return NetGuid.Read(reader) == arc.Guid;
    }

    private static Dictionary<string, object?> Decode(string classPath, ActorContentBlock block, Dictionary<ulong, string> objects) =>
        new PropertyCodec(new ValueCodec(GameReplication.Catalog, [KnownEnums.NetRole], objects))
            .Decode(new PropertyLayout(GameReplication.Catalog, classPath), block, swapActorRoles: false);

    private static (uint Frame, double At) Ready(ScriptedPlayer session, Vector3D at)
    {
        session.Possess();
        (uint frame, double time) = session.Settle(30, 100, at);
        return Stand(session, frame, time, at, 16);
    }

    // Standing inputs at a place, each followed by a service 0.3 s later, for some seconds.
    private static (uint Frame, double At) Stand(ScriptedPlayer session, uint frame, double at, Vector3D position, double seconds)
    {
        for (double end = at + seconds; at < end; at += 0.3)
        {
            session.Input(frame, frame + 3, position: position);
            session.Service(at);
            frame += 3;
        }

        return (frame, at);
    }

    private static double Distance(Vector3D a, Vector3D b) =>
        Math.Sqrt(((a.X - b.X) * (a.X - b.X)) + ((a.Y - b.Y) * (a.Y - b.Y)) + ((a.Z - b.Z) * (a.Z - b.Z)));

    private static byte[] Bits(BitReader reader, long bits)
    {
        BitWriter copy = new();
        copy.WriteBits(reader.ReadBitArray(bits), bits);
        return copy.ToArray();
    }
}
