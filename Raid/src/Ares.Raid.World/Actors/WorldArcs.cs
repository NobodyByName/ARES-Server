using System.Globalization;
using Ares.Raid.World.Replication;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Game.Enemies;
using Ares.Structs.Game.Replication;
using Ares.Structs.Net.Replication;
using Ares.Structs.Net.Session;

namespace Ares.Raid.World.Actors;

/// <summary>An ARC in the raid.</summary>
/// <param name="Index">Index in <see cref="WorldArcs.All"/>.</param>
/// <param name="Class">The ARC.</param>
/// <param name="Guid">The actor (dynamic id); its attribute sets and ability system take ids after it.</param>
/// <param name="Location">Where the actor stands.</param>
/// <param name="Rotation">The actor's rotation.</param>
public sealed record WorldArc(int Index, ArcClass Class, ulong Guid, Vector3D Location, Rotator Rotation)
{
    /// <summary>The attribute sets, in <see cref="Arcs.AttributeSets"/> order.</summary>
    public IReadOnlyList<ulong> AttributeSets => [.. Arcs.AttributeSets.Select((_, index) => Guid + WorldArcs.FirstAttributeSetOffset + (2 * (ulong)index))];

    /// <summary>The ability system component.</summary>
    public ulong AbilitySystem => Guid + WorldArcs.FirstAttributeSetOffset + (2 * (ulong)Arcs.AttributeSets.Count);

    /// <summary>The actor is gone: every client that has it destroys it.</summary>
    public bool Destroyed { get; init; }
}

/// <summary>
/// The ARCs of a raid. An ARC stands where it was spawned: it has no controller, movement or abilities, and stays
/// until it is despawned or the raid ends.
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
    private readonly List<string> _events = [];

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

    /// <summary>What happened to the ARCs, oldest first, one line each.</summary>
    public IReadOnlyList<string> Events => _events;

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
        _events.Add(string.Create(CultureInfo.InvariantCulture,
            $"spawn #{spawned.Index} {arc.Name} at ({location.X:0},{location.Y:0},{location.Z:0}) guid 0x{spawned.Guid:x}"));
        return spawned;
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
            _events.Add($"despawn #{index}");
            despawned++;
        }

        return despawned;
    }

    /// <summary>Whether an ARC is relevant to a player at a location: it exists and lies within its net cull distance.</summary>
    /// <param name="arc">The ARC.</param>
    /// <param name="location">The player's location.</param>
    /// <returns>True when the player's client should have it.</returns>
    public static bool Relevant(WorldArc arc, Vector3D location)
    {
        ArgumentNullException.ThrowIfNull(arc);
        return !arc.Destroyed && Distance(arc.Location, location) <= Arcs.NetCullDistance;
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
    /// Opens an ARC's channel: spawns the actor where it stands with its roles (the client simulates it, the server is
    /// the authority) and itself as instigator, and creates its attribute sets.
    /// </summary>
    /// <param name="arc">The ARC.</param>
    /// <param name="channel">Channel index.</param>
    /// <param name="sequence">Reliable sequence number.</param>
    /// <returns>The bunch.</returns>
    public ChannelBunch OpenBunch(WorldArc arc, int channel, int sequence)
    {
        ArgumentNullException.ThrowIfNull(arc);
        ActorContentBlock actor = _replication.Encode(Arcs.ActorClass, new Dictionary<string, object?>
        {
            [PropertyNames.RemoteRole] = (ulong)NetRole.SimulatedProxy,
            [PropertyNames.Role] = (ulong)NetRole.Authority,
            [PropertyNames.Instigator] = arc.Guid,
        }, new Dictionary<ulong, string> { [arc.Guid] = Arcs.ActorClass });
        ActorSpawn spawn = new(arc.Guid, Archetype(arc.Class), 0, arc.Location, arc.Rotation);
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

    private static double Distance(Vector3D a, Vector3D b) =>
        Math.Sqrt(((a.X - b.X) * (a.X - b.X)) + ((a.Y - b.Y) * (a.Y - b.Y)) + ((a.Z - b.Z) * (a.Z - b.Z)));
}
