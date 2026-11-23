using Ares.Common.Game.Enemies;
using Ares.Raid.World.Collision;
using Ares.Raid.World.Players;
using Ares.Raid.World.Replication;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Core.Serialization;
using Ares.Structs.Game.Enemies;
using Ares.Structs.Game.Pendulum;
using Ares.Structs.Game.Replication;
using Ares.Structs.Game.World;
using Ares.Structs.Net.Replication;
using Ares.Structs.Net.Session;

namespace Ares.Raid.World.Actors;

/// <summary>An ARC in the raid.</summary>
/// <param name="Index">Index in <see cref="WorldArcs.All"/>.</param>
/// <param name="Class">The ARC.</param>
/// <param name="Guid">The actor (dynamic id); its attribute sets and ability system take ids after it.</param>
/// <param name="Location">Where the actor was spawned.</param>
/// <param name="Rotation">The actor's rotation at its spawn.</param>
public sealed record WorldArc(int Index, ArcClass Class, ulong Guid, Vector3D Location, Rotator Rotation)
{
    /// <summary>The spawner it came from, or null for an ARC the operator placed by hand.</summary>
    public ArcSpawner? Spawner { get; init; }

    /// <summary>The attribute sets, in <see cref="Arcs.AttributeSets"/> order.</summary>
    public IReadOnlyList<ulong> AttributeSets => [.. Arcs.AttributeSets.Select((_, index) => Guid + WorldArcs.FirstAttributeSetOffset + (2 * (ulong)index))];

    /// <summary>The ability system component.</summary>
    public ulong AbilitySystem => Guid + WorldArcs.FirstAttributeSetOffset + (2 * (ulong)Arcs.AttributeSets.Count);

    /// <summary>The actor is gone: every client that has it destroys it.</summary>
    public bool Destroyed { get; init; }
}

/// <summary>
/// The ARCs of a raid. An ARC placed by hand stays where it was spawned; one spawned at a spawner flies its idle
/// movement (<see cref="ArcFlight"/>). Neither has a controller or abilities; each stays until it is despawned or the
/// raid ends. Clients receive an ARC's state as blackboard frames (<see cref="ArcBlackboard"/>).
/// </summary>
public sealed class WorldArcs
{
    /// <summary>Offset of an ARC's first attribute set from its actor id; the id between them stays unused.</summary>
    internal const ulong FirstAttributeSetOffset = 4;

    /// <summary>Most ARCs a raid spawns.</summary>
    public const int MaxArcs = 4096;

    // Static ids of the ARC class defaults and attribute set classes: clear of the other static ranges.
    private const ulong FirstExportGuid = 0x3E0001;

    // Dynamic ids of the ARCs: far above the players, ladders, loot containers and the pickups, which grow upward from
    // 0x400000 six ids per drop and would need tens of millions of drops to get here.
    private const ulong FirstGuid = 0x10000000;
    private const ulong GuidStride = 16;

    private readonly ReplicationContext _replication;
    private readonly ObjectExports _exports;
    private readonly Dictionary<ArcClass, ulong> _archetypes = [];
    private readonly ulong[] _attributeSetClasses;
    private readonly List<WorldArc> _arcs = [];
    private readonly List<ArcFlight?> _flights = [];

    /// <summary>Creates the raid's empty set of ARCs.</summary>
    /// <param name="replication">Replication context.</param>
    /// <param name="imports">The world's static exports; packages and classes it already exports keep their ids.</param>
    public WorldArcs(ReplicationContext replication, WorldImports imports)
    {
        _replication = replication ?? throw new ArgumentNullException(nameof(replication));
        ArgumentNullException.ThrowIfNull(imports);
        ReplicationCatalog catalog = replication.Catalog;
        Dictionary<ulong, ObjectExport> records = [];
        Dictionary<string, StaticObjectExport> packages = imports.Exports.Records.Values.OfType<StaticObjectExport>().Where(record => record.OuterGuid == 0)
            .GroupBy(record => record.Path, StringComparer.Ordinal).ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        ulong next = FirstExportGuid;
        foreach (ArcClass arc in Arcs.All)
        {
            (string package, string name) = Split(arc.Class.Archetype);
            _archetypes[arc] = Add(Package(package, 0).Guid, name, 0);
        }

        uint packageChecksum = catalog.Class(ClassPaths.Package).Checksum;
        _attributeSetClasses = [.. Arcs.AttributeSets.Select(path =>
        {
            if (imports.Classes.TryGetValue(path, out ulong exported))
            {
                ObjectExport record = imports.Exports.Records[exported];
                records[record.Guid] = record;
                records[record.Outer] = imports.Exports.Records[record.Outer];
                return exported;
            }

            (string package, string name) = Split(path);
            uint checksum = catalog.Classes.TryGetValue(path, out ClassSchema? schema) ? schema.Checksum : 0;
            return Add(Package(package, packageChecksum).Guid, name, checksum);
        })];
        _exports = new ObjectExports(records.Values);
        return;

        StaticObjectExport Package(string path, uint checksum)
        {
            if (!packages.TryGetValue(path, out StaticObjectExport? package))
            {
                package = new StaticObjectExport(next, 0, path, checksum, false);
                next += 2;
                packages[path] = package;
            }

            records[package.Guid] = package;
            return package;
        }

        ulong Add(ulong outer, string name, uint checksum)
        {
            StaticObjectExport record = new(next, outer, name, checksum, false);
            next += 2;
            records[record.Guid] = record;
            return record.Guid;
        }
    }

    /// <summary>Every ARC of the raid, also those despawned, by index.</summary>
    public IReadOnlyList<WorldArc> All => _arcs;

    /// <summary>Static id of an ARC's class default, the archetype of its actor.</summary>
    /// <param name="arc">The ARC.</param>
    /// <returns>The id.</returns>
    public ulong Archetype(ArcClass arc) => _archetypes[arc];

    /// <summary>Static ids of the attribute set classes, in <see cref="Arcs.AttributeSets"/> order.</summary>
    public IReadOnlyList<ulong> AttributeSetClasses => _attributeSetClasses;

    /// <summary>Spawns an ARC.</summary>
    /// <param name="arc">The ARC.</param>
    /// <param name="location">Where it stands.</param>
    /// <param name="rotation">Its rotation.</param>
    /// <returns>The spawned ARC, or null when the raid spawned <see cref="MaxArcs"/> already.</returns>
    public WorldArc? Spawn(ArcClass arc, Vector3D location, Rotator rotation)
    {
        ArgumentNullException.ThrowIfNull(arc);
        if (_arcs.Count >= MaxArcs)
        {
            return null;
        }

        WorldArc spawned = new(_arcs.Count, arc, FirstGuid + (GuidStride * (ulong)_arcs.Count), location, rotation);
        _arcs.Add(spawned);
        _flights.Add(null);
        return spawned;
    }

    /// <summary>
    /// Spawns an ARC at a spawner: at the home point nearest the spawner (on its route, see <see cref="ArcRoutes"/>), with
    /// no rotation, flying its idle movement. A spawner with several routes uses one of them, drawn with the seed.
    /// </summary>
    /// <param name="arc">The ARC.</param>
    /// <param name="spawner">The spawner.</param>
    /// <param name="paths">The map's spline paths.</param>
    /// <param name="profile">How the ARC moves.</param>
    /// <param name="collision">The map's collision, or null.</param>
    /// <param name="seed">Seed of its route and its movement.</param>
    /// <returns>The spawned ARC, or null when the raid spawned <see cref="MaxArcs"/> already.</returns>
    public WorldArc? Spawn(ArcClass arc, ArcSpawner spawner, IReadOnlyList<SplinePathPlacement> paths, ArcMovementProfile profile, CollisionWorld? collision, int seed)
    {
        ArgumentNullException.ThrowIfNull(arc);
        ArgumentNullException.ThrowIfNull(spawner);
        ArgumentNullException.ThrowIfNull(paths);
        ArgumentNullException.ThrowIfNull(profile);
        if (_arcs.Count >= MaxArcs)
        {
            return null;
        }

        Random random = new(seed);
        SplinePathPlacement[] routes = [.. spawner.Routes.Select(name => paths.FirstOrDefault(path => path.Name.Equals(name, StringComparison.Ordinal))).OfType<SplinePathPlacement>()];
        SplinePathPlacement? route = routes.Length == 0 ? null : routes[random.Next(routes.Length)];
        double spacing = Math.Max(profile.HomePointSpacing * spawner.GroupSearchRadius, profile.MinHomePointSpacing);
        ArcHomePoints homes = ArcRoutes.HomePoints(spawner, route, spacing);
        int home = homes.Nearest(spawner.Location);
        WorldArc spawned = new(_arcs.Count, arc, FirstGuid + (GuidStride * (ulong)_arcs.Count), homes.Points[home], default) { Spawner = spawner };
        _arcs.Add(spawned);
        _flights.Add(new ArcFlight(profile, homes, home, spawner.GroupSearchRadius, collision, random.Next()));
        return spawned;
    }

    /// <summary>Moves every flying ARC on to a time on the raid clock.</summary>
    /// <param name="now">Raid clock, in seconds.</param>
    public void Advance(double now)
    {
        for (int index = 0; index < _arcs.Count; index++)
        {
            if (!_arcs[index].Destroyed)
            {
                _flights[index]?.Advance(now);
            }
        }
    }

    /// <summary>Where an ARC is now.</summary>
    /// <param name="arc">The ARC.</param>
    /// <returns>Its pose; an ARC placed by hand stays at its spawn.</returns>
    public ArcPose Pose(WorldArc arc)
    {
        ArgumentNullException.ThrowIfNull(arc);
        return _flights[arc.Index]?.Pose ?? new ArcPose(arc.Location, default, arc.Rotation.Yaw * Math.PI / 180, arc.Rotation.Pitch * Math.PI / 180, false);
    }

    /// <summary>Despawns every ARC.</summary>
    /// <returns>How many ARCs were despawned.</returns>
    public int DespawnAll()
    {
        int despawned = 0;
        for (int index = 0; index < _arcs.Count; index++)
        {
            if (_arcs[index].Destroyed)
            {
                continue;
            }

            _arcs[index] = _arcs[index] with { Destroyed = true };
            despawned++;
        }

        return despawned;
    }

    /// <summary>Whether an ARC is relevant to a player at a location: it exists and lies within its net cull distance.</summary>
    /// <param name="arc">The ARC.</param>
    /// <param name="location">The player's location.</param>
    /// <returns>True when the player's client should have it.</returns>
    public bool Relevant(WorldArc arc, Vector3D location)
    {
        ArgumentNullException.ThrowIfNull(arc);
        return !arc.Destroyed && ArcRoutes.Distance(Pose(arc).Location, location) <= Arcs.NetCullDistance;
    }

    /// <summary>The static objects an ARC's channel needs: its class default and the attribute set classes.</summary>
    /// <param name="arc">The ARC.</param>
    /// <returns>The ids.</returns>
    public IReadOnlyList<ulong> Roots(WorldArc arc)
    {
        ArgumentNullException.ThrowIfNull(arc);
        return [Archetype(arc.Class), .. _attributeSetClasses];
    }

    /// <summary>Exports ARC class defaults and attribute set classes by name; the client loads them.</summary>
    /// <param name="roots">Ids from <see cref="Roots"/>.</param>
    /// <returns>The bunch.</returns>
    public ChannelBunch ExportBunch(IReadOnlyList<ulong> roots) => Bunches.Exports(_exports, roots);

    /// <summary>
    /// Opens an ARC's channel: spawns the actor where it is with its roles (the client simulates it, the server is
    /// the authority), itself as instigator and the first (reset) frame of its blackboard, and creates its attribute sets.
    /// </summary>
    /// <param name="arc">The ARC.</param>
    /// <param name="channel">Channel index.</param>
    /// <param name="sequence">Reliable sequence number.</param>
    /// <param name="state">Its blackboard state (<see cref="State"/>), sent as the reset frame <see cref="ArcFrames.First"/>.</param>
    /// <returns>The bunch.</returns>
    public ChannelBunch OpenBunch(WorldArc arc, int channel, int sequence, IReadOnlyDictionary<int, PendulumSlot> state)
    {
        ArgumentNullException.ThrowIfNull(arc);
        ActorContentBlock actor = _replication.Encode(Arcs.ActorClass, new Dictionary<string, object?>
        {
            [PropertyNames.RemoteRole] = (ulong)NetRole.SimulatedProxy,
            [PropertyNames.Role] = (ulong)NetRole.Authority,
            [PropertyNames.Instigator] = arc.Guid,
        }, new Dictionary<ulong, string> { [arc.Guid] = Arcs.ActorClass });
        actor = WorldState.Merge(actor, Frame(arc, 0, ArcFrames.First, state, new Dictionary<int, PendulumSlot>()));
        ArcPose pose = Pose(arc);
        ActorSpawn spawn = new(arc.Guid, Archetype(arc.Class), 0, pose.Location, arc.Rotation);
        return new ActorInitialization(spawn, null, [actor])
        {
            Subobjects = [.. arc.AttributeSets.Select((guid, index) => new DynamicSubobjectContent(guid, _attributeSetClasses[index], new ActorContentBlock([], 0, false), null))],
        }.ToBunch(channel, sequence, _attributeSetClasses);
    }

    /// <summary>Names the ARC's ability system component.</summary>
    /// <param name="arc">The ARC.</param>
    /// <returns>The bunch.</returns>
    public static ChannelBunch ComponentExportBunch(WorldArc arc)
    {
        ArgumentNullException.ThrowIfNull(arc);
        ObjectExports exports = new([
            new DynamicActorExport(arc.Guid),
            new StableSubobjectExport(arc.AbilitySystem, arc.Guid, Arcs.AbilitySystemComponent, Arcs.AbilitySystemExportChecksum),
        ]);
        return Bunches.Exports(exports, [arc.AbilitySystem]);
    }

    /// <summary>Sends the ability system's actor info and the attribute sets it spawned.</summary>
    /// <param name="arc">The ARC.</param>
    /// <param name="channel">Channel index.</param>
    /// <returns>The bunch.</returns>
    public ChannelBunch AbilitySystemBunch(WorldArc arc, int channel)
    {
        ArgumentNullException.ThrowIfNull(arc);
        Dictionary<ulong, string> objects = new() { [arc.Guid] = Arcs.ActorClass };
        IReadOnlyList<ulong> sets = arc.AttributeSets;
        for (int index = 0; index < sets.Count; index++)
        {
            // A set class without a replication schema is known here by its base class.
            string path = Arcs.AttributeSets[index];
            objects[sets[index]] = _replication.Catalog.Classes.ContainsKey(path) ? path : ClassPaths.AttributeSet;
        }

        ActorContentBlock content = _replication.Encode(Arcs.AbilitySystemClass, new Dictionary<string, object?>
        {
            [PropertyNames.OwnerActor] = arc.Guid,
            [PropertyNames.AvatarActor] = arc.Guid,
            [PropertyNames.SpawnedAttributes] = sets.Select(guid => (object?)guid).ToArray(),
        }, objects);
        return Bunches.Actor(channel, writer => new StableSubobjectContent(arc.AbilitySystem, content).Write(writer));
    }

    private static (string Outer, string Name) Split(string path)
    {
        int dot = path.LastIndexOf('.');
        return (path[..dot], path[(dot + 1)..]);
    }

    /// <summary>An ARC's blackboard state in a pose (see <see cref="ArcFrames.State"/>).</summary>
    /// <param name="arc">The ARC.</param>
    /// <param name="pose">Its pose.</param>
    /// <returns>The slots by index.</returns>
    public static IReadOnlyDictionary<int, PendulumSlot> State(WorldArc arc, ArcPose pose)
    {
        ArgumentNullException.ThrowIfNull(arc);
        return ArcFrames.State(arc.Class.Blackboard, pose);
    }

    /// <summary>A blackboard frame of an ARC: a reset when there is no baseline, otherwise a delta from it.</summary>
    /// <param name="arc">The ARC.</param>
    /// <param name="baseline">Frame number the delta is coded against (0 for a reset).</param>
    /// <param name="frame">The frame's number.</param>
    /// <param name="state">The frame's state.</param>
    /// <param name="previous">The baseline's state (empty for a reset).</param>
    /// <returns>The actor content (the blackboard field).</returns>
    public static ActorContentBlock Frame(WorldArc arc, uint baseline, uint frame, IReadOnlyDictionary<int, PendulumSlot> state, IReadOnlyDictionary<int, PendulumSlot> previous)
    {
        ArgumentNullException.ThrowIfNull(arc);
        ArcBlackboard blackboard = arc.Class.Blackboard;
        BitWriter payload = new();
        ArcFrames.Write(payload, baseline, frame, state, previous);
        BitWriter writer = new();
        new ActorFieldPayload(blackboard.Field, payload.ToArray(), payload.Length).Write(writer, blackboard.FieldCount);
        return new ActorContentBlock(writer.ToArray(), writer.Length, false);
    }
}
