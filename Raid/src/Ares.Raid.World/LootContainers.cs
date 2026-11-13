using Ares.Common.Game.Inventory;
using Ares.Raid.Replication;
using Ares.Structs.Backend.Progression;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Game.Inventory;
using Ares.Structs.Game.Loot;
using Ares.Structs.Game.Replication;
using Ares.Structs.Game.World;
using Ares.Structs.Net.Replication;
using Ares.Structs.Net.Session;

namespace Ares.Raid.World;

/// <summary>Where a loot container of a raid comes from.</summary>
public enum LootContainerSource
{
    /// <summary>A container socket of a prop.</summary>
    Socket,

    /// <summary>A point of a dynamic loot spawner.</summary>
    Spawner,

    /// <summary>A container actor the map places.</summary>
    Placed,

    /// <summary>A place where the game server was seen to spawn one.</summary>
    Observed,

    /// <summary>A container an operator spawned during the raid.</summary>
    Operator,
}

/// <summary>A loot container the server spawns: at a socket, at a spawner's point or where the map places it.</summary>
/// <param name="Index">Index in <see cref="LootCatalog.Containers"/>.</param>
/// <param name="Source">Where it comes from.</param>
/// <param name="Location">World location.</param>
/// <param name="Rotation">World rotation in degrees.</param>
/// <param name="Socket">The socket it stands on, for a socket container; null otherwise.</param>
/// <param name="Class">Its class and how it is searched.</param>
/// <param name="Guid">The container actor (dynamic id).</param>
/// <param name="Archetype">Exported id of its class default.</param>
/// <param name="SearchAnimation">Exported id of the animation set of its search, or 0 when it has none.</param>
/// <param name="Components">Its components (dynamic ids), in <see cref="LootContainerClass.Components"/> order.</param>
public sealed record LootContainer(int Index, LootContainerSource Source, Vector3D Location, Rotator Rotation, LootSocket? Socket, LootContainerClass Class, ulong Guid, ulong Archetype, ulong SearchAnimation, IReadOnlyList<ulong> Components)
{
    /// <summary>The search interaction.</summary>
    public ulong Interaction => Components[ComponentIndex(LootContainerClasses.InteractionComponent)];

    /// <summary>The item container.</summary>
    public ulong ItemContainer => Components[ComponentIndex(LootContainerClasses.ItemContainerComponent)];

    /// <summary>Class of the search interaction.</summary>
    public string InteractionClass => Class.Components[ComponentIndex(LootContainerClasses.InteractionComponent)].Class;

    /// <summary>Class of the item container.</summary>
    public string ItemContainerClass => Class.Components[ComponentIndex(LootContainerClasses.ItemContainerComponent)].Class;

    /// <summary>Where the container's prompt is: an open container measures the player's distance from it.</summary>
    public Vector3D Prompt
    {
        get
        {
            Vector3D offset = Rotation.Rotate(Class.PromptLocation);
            return new Vector3D(Location.X + offset.X, Location.Y + offset.Y, Location.Z + offset.Z);
        }
    }

    /// <summary>
    /// Distance from the prompt beyond which an open container closes: the prompt distance while the character is
    /// held at the container, plus the server's leeway.
    /// </summary>
    public double CloseDistance => Class.OpenedPromptDistance + LootInteractions.ServerPromptLeeway;

    private int ComponentIndex(string name)
    {
        for (int index = 0; index < Class.Components.Count; index++)
        {
            if (Class.Components[index].Name == name)
            {
                return index;
            }
        }

        throw new InvalidOperationException($"The container class has no {name}");
    }
}

/// <summary>What a player's client shows of a loot container.</summary>
/// <param name="State">State of the search interaction.</param>
/// <param name="HoldStartedAt">Server time the search hold starts, while a search of this player runs.</param>
/// <param name="OpenedAt">Server time the container was first opened, once it has been.</param>
/// <param name="ItemContainer">Id of the container among the player's containers, while this player has it open.</param>
public readonly record struct LootView(CurrentInteractionState State, double? HoldStartedAt, double? OpenedAt, int? ItemContainer);

/// <summary>How a raid's dynamic loot spawners turned into containers.</summary>
/// <param name="Spawners">Spawners of the map's groups.</param>
/// <param name="Run">Spawners an active group picked.</param>
/// <param name="NotPicked">Spawners no active group picked (groups of other rounds, inactive groups, or not drawn).</param>
/// <param name="InactiveLayer">Spawners that stream with a data layer the round leaves inactive.</param>
/// <param name="Spawned">Containers the spawners that ran placed.</param>
/// <param name="ClassDisabled">Containers not placed because the drawn class is not enabled for spawners.</param>
/// <param name="ClassUnsupported">Containers not placed because the server cannot serve the drawn class yet.</param>
public readonly record struct LootSpawnerCounts(int Spawners, int Run, int NotPicked, int InactiveLayer, int Spawned, int ClassDisabled, int ClassUnsupported);

/// <summary>How a raid's placed (or observed) containers turned into containers.</summary>
/// <param name="Containers">Containers the map data lists.</param>
/// <param name="Spawned">Containers spawned.</param>
/// <param name="InactiveLayer">Containers that stream with a data layer the round leaves inactive.</param>
/// <param name="Unsupported">Containers whose class the server cannot serve yet.</param>
/// <param name="NoContainerType">Containers whose class no container type lists (no tag to roll).</param>
/// <param name="SkippedByRoll">Servable containers whose spawn roll failed.</param>
public readonly record struct LootPlacedCounts(int Containers, int Spawned, int InactiveLayer, int Unsupported, int NoContainerType, int SkippedByRoll);

/// <summary>How a raid's container sockets and free-standing containers turned into containers.</summary>
/// <param name="Sockets">Container sockets of the map.</param>
/// <param name="Spawned">Containers spawned on sockets.</param>
/// <param name="InactiveLayer">Sockets skipped because their prop streams with a data layer the round leaves inactive.</param>
/// <param name="NoContainerType">Sockets skipped because no container type has their tag.</param>
/// <param name="Unsupported">Sockets skipped because the server cannot serve any of their classes yet.</param>
/// <param name="Classes">Distinct classes of the spawned containers, from every source.</param>
/// <param name="SkippedByRoll">Servable sockets whose spawn roll failed.</param>
/// <param name="SkippedByGroupCap">Servable sockets whose group was full.</param>
/// <param name="SkippedByLimiter">Servable sockets under a placement limiter cap that was reached.</param>
/// <param name="SpawnedByLootValue">Containers spawned on sockets, by the loot value of the area around their socket.</param>
/// <param name="Spawners">The dynamic loot spawners.</param>
/// <param name="Placed">The containers the map places.</param>
/// <param name="Observed">The containers seen at places the map data does not hold.</param>
public readonly record struct LootSpawnCounts(int Sockets, int Spawned, int InactiveLayer, int NoContainerType, int Unsupported, int Classes, int SkippedByRoll, int SkippedByGroupCap, int SkippedByLimiter, IReadOnlyDictionary<PointOfInterestLootValue, int> SpawnedByLootValue, LootSpawnerCounts Spawners, LootPlacedCounts Placed, LootPlacedCounts Observed);

/// <summary>
/// The loot containers of a raid: among the container sockets of the active data layers whose class the server can
/// serve, those the raid's spawn rolls pick (<see cref="LootSpawnSelection"/>); then the containers the round's dynamic
/// loot spawners place (<see cref="DynamicLootRuns"/>); then the placed and observed containers whose roll succeeds.
/// Ids continue across the sources. A connection gets each container in four steps: the class default (loadable, as the
/// containers are not part of the map), the spawned actor at its place, the names of its components, then the state of
/// its search.
/// </summary>
public sealed class LootCatalog
{
    // Static ids of the container classes and dynamic ids of the containers: clear of the other ranges.
    private const ulong FirstClassGuid = 0x2F0001;
    private const ulong FirstGuid = 0x380000;

    // Where the raid's pickups start: containers spawned during the raid stay below it.
    private const ulong EndGuid = 0x400000;

    private readonly ReplicationContext _replication;
    private readonly ObjectExports _classExports;
    private readonly Dictionary<string, ulong> _exported;
    private readonly List<LootContainer> _containers = [];
    private ulong _nextGuid = FirstGuid;
    private readonly Dictionary<ulong, LootContainer> _interactions = [];
    private readonly Dictionary<(long X, long Y), List<LootContainer>> _cells = [];

    /// <summary>Spawns the containers of a Dam raid.</summary>
    /// <param name="replication">Replication context.</param>
    /// <param name="round">Name of the raid's round configuration: it picks the dynamic loot groups.</param>
    /// <param name="activeDataLayers">Names of the raid's active data layers.</param>
    /// <param name="seed">The raid's seed: it rolls the sockets and picks among a socket's classes when there are several.</param>
    /// <param name="rules">Spawn chances, limiters and free-standing containers.</param>
    public LootCatalog(ReplicationContext replication, string round, IReadOnlyCollection<string> activeDataLayers, int seed, LootSpawnRules rules)
        : this(replication, DamLootSockets.All, DamLootSockets.Meshes, round, activeDataLayers, seed, rules)
    {
    }

    /// <summary>Spawns the containers of a raid on the sockets of the active data layers.</summary>
    /// <param name="replication">Replication context.</param>
    /// <param name="sockets">The map's container sockets.</param>
    /// <param name="meshes">The meshes <see cref="LootSocket.Mesh"/> indexes.</param>
    /// <param name="round">Name of the raid's round configuration: it picks the dynamic loot groups.</param>
    /// <param name="activeDataLayers">Names of the raid's active data layers.</param>
    /// <param name="seed">
    /// The raid's seed: it rolls the sockets, picks among a socket's classes when there are several, picks the spawners
    /// and rolls the placed containers.
    /// </param>
    /// <param name="rules">Spawn chances, limiters and free-standing containers.</param>
    public LootCatalog(ReplicationContext replication, IReadOnlyList<LootSocket> sockets, IReadOnlyList<LootSocketMesh> meshes, string round, IReadOnlyCollection<string> activeDataLayers, int seed, LootSpawnRules rules)
    {
        _replication = replication ?? throw new ArgumentNullException(nameof(replication));
        ArgumentNullException.ThrowIfNull(sockets);
        ArgumentNullException.ThrowIfNull(meshes);
        ArgumentNullException.ThrowIfNull(round);
        ArgumentNullException.ThrowIfNull(activeDataLayers);
        ArgumentNullException.ThrowIfNull(rules);
        HashSet<string> active = new(activeDataLayers, StringComparer.Ordinal);
        Random random = new(seed);
        List<StaticObjectExport> classes = [];
        Dictionary<string, ulong> paths = new(StringComparer.Ordinal);
        List<LootContainer> containers = _containers;
        List<(LootSocket Socket, List<LootContainerClass> Classes)> servable = [];
        (int inactive, int untyped, int unsupported) = (0, 0, 0);
        foreach (LootSocket socket in sockets)
        {
            if (socket.DataLayer is { } layer && !active.Contains(layer))
            {
                inactive++;
                continue;
            }

            IReadOnlyList<ArchetypeClass> candidates = LootSocketClasses.Candidates(socket, meshes);
            if (candidates.Count == 0)
            {
                untyped++;
                continue;
            }

            List<LootContainerClass> supported = [.. candidates.Select(candidate => LootContainerClasses.Find(candidate.Class)).OfType<LootContainerClass>()];
            if (supported.Count == 0)
            {
                unsupported++;
                continue;
            }

            servable.Add((socket, supported));
        }

        LootSpawnSelection selection = LootSpawnSelection.Select([.. servable.Select(entry => entry.Socket)], rules, activeDataLayers, random);
        for (int candidate = 0; candidate < servable.Count; candidate++)
        {
            if (!selection.Spawns[candidate])
            {
                continue;
            }

            (LootSocket socket, List<LootContainerClass> supported) = servable[candidate];
            LootContainerClass type = supported.Count == 1 ? supported[0] : supported[random.Next(supported.Count)];
            Add(LootContainerSource.Socket, socket.Location, socket.Rotation, socket, type);
        }

        int onSockets = containers.Count;
        FreeStandingLoot free = rules.FreeStanding ?? FreeStandingLoot.None;
        LootSpawnerCounts spawners = Spawners();
        LootValueAreas areas = new(rules.Areas);
        LootPlacedCounts placed = Placed(LootContainerSource.Placed, [.. free.Placed.Select(entry => (entry.Class, entry.Location, (Rotator?)entry.Rotation, entry.DataLayer))]);
        LootPlacedCounts observed = Placed(LootContainerSource.Observed, [.. free.Observed.Select(entry => (entry.Class, entry.Location, entry.Rotation, (string?)null))]);

        // Every servable class may also be spawned during the raid, so each has its export and its world item bit.
        ulong[] worldItems = new ulong[WorldItemList.WordCount];
        foreach (LootContainerClass type in LootContainerClasses.All)
        {
            _ = Export(type.Container.Archetype);
            if (type.SearchAnimation is { } animation)
            {
                _ = Export(animation);
            }

            int bit = WorldItemList.IndexOf(type.Container.Class) ?? throw new InvalidOperationException($"{type.Container.Class} is not a world item class");
            worldItems[bit / WorldItemList.WordBits] |= 1UL << (bit % WorldItemList.WordBits);
        }

        WorldItemClassBits = worldItems;

        _classExports = new ObjectExports(classes);
        _exported = paths;
        Seed = seed;
        FreeStandingAreas names = new(rules.Areas);
        FreeStanding = [.. containers.Where(container => container.Source != LootContainerSource.Socket).Select(container => names.Describe(container.Class.Container.Class, container.Location))];
        Counts = new LootSpawnCounts(sockets.Count, onSockets, inactive, untyped, unsupported, containers.Select(container => container.Class).Distinct().Count(), selection.SkippedByRoll, selection.SkippedByGroupCap, selection.SkippedByLimiter, Enum.GetValues<PointOfInterestLootValue>().ToDictionary(value => value, selection.Spawned), spawners, placed, observed);
        return;

        void Add(LootContainerSource source, Vector3D location, Rotator rotation, LootSocket? socket, LootContainerClass type)
        {
            ulong archetype = Export(type.Container.Archetype);
            ulong animation = type.SearchAnimation is { } path ? Export(path) : 0;
            Place(source, location, rotation, socket, type, archetype, animation);
        }

        // The round's spawners: picked by their groups from every listed spawner, then those of inactive data layers
        // dropped; a drawn class that is not enabled or not servable places nothing (the other draws stay as they are).
        LootSpawnerCounts Spawners()
        {
            IReadOnlyList<DynamicLootSpawner> all = free.Spawners;
            IReadOnlyList<DynamicLootSpawner> picked = DynamicLootRuns.Pick(free.Groups, round, random);
            int layer = all.Count(spawner => spawner.DataLayer is { } name && !active.Contains(name));
            (int run, int spawned, int disabled, int unservable) = (0, 0, 0, 0);
            foreach (DynamicLootSpawner spawner in picked.Where(spawner => spawner.DataLayer is null || active.Contains(spawner.DataLayer)))
            {
                run++;
                foreach (DynamicLootPlacement placement in DynamicLootRuns.Run(spawner, random))
                {
                    if (!free.SpawnerClassEnabled(placement.Class))
                    {
                        disabled++;
                    }
                    else if (LootContainerClasses.Find(placement.Class) is not { } type)
                    {
                        unservable++;
                    }
                    else
                    {
                        spawned++;
                        Add(LootContainerSource.Spawner, placement.Location, placement.Rotation, null, type);
                    }
                }
            }

            return new LootSpawnerCounts(all.Count, run, all.Count - run - layer, layer, spawned, disabled, unservable);
        }

        // Containers at fixed places: each rolls the chance of its class's container tag in the area's loot value.
        LootPlacedCounts Placed(LootContainerSource source, List<(string Class, Vector3D Location, Rotator? Rotation, string? DataLayer)> entries)
        {
            (int spawned, int layer, int unservable, int untagged, int rolls) = (0, 0, 0, 0, 0);
            foreach ((string path, Vector3D location, Rotator? rotation, string? dataLayer) in entries)
            {
                if (dataLayer is not null && !active.Contains(dataLayer))
                {
                    layer++;
                }
                else if (LootContainerClasses.Find(path) is not { } type)
                {
                    unservable++;
                }
                else if (LootSocketClasses.ContainerTag(path) is not { } tag)
                {
                    untagged++;
                }
                else if (rules.Rolls && random.NextDouble() >= rules.Chance(tag, areas.At(location)))
                {
                    rolls++;
                }
                else
                {
                    spawned++;
                    Add(source, location, rotation ?? default, null, type);
                }
            }

            return new LootPlacedCounts(entries.Count, spawned, layer, unservable, untagged, rolls);
        }

        // An object path exported by name: its package, then the object in it.
        ulong Export(string path)
        {
            if (paths.TryGetValue(path, out ulong exported))
            {
                return exported;
            }

            int dot = path.LastIndexOf('.');
            ulong package = FirstClassGuid + (2 * (ulong)classes.Count);
            classes.Add(new StaticObjectExport(package, 0, path[..dot], 0, false));
            exported = FirstClassGuid + (2 * (ulong)classes.Count);
            classes.Add(new StaticObjectExport(exported, package, path[(dot + 1)..], 0, false));
            return paths[path] = exported;
        }
    }

    /// <summary>All containers, including those spawned during the raid.</summary>
    public IReadOnlyList<LootContainer> Containers => _containers;

    /// <summary>How the sockets turned into containers.</summary>
    public LootSpawnCounts Counts { get; }

    /// <summary>The seed the raid's containers were rolled with.</summary>
    public int Seed { get; }

    /// <summary>
    /// The game state's world item class bitfield: the bit of every container class the raid can spawn, so the client
    /// loads those classes before it receives the containers.
    /// </summary>
    public IReadOnlyList<ulong> WorldItemClassBits { get; }

    /// <summary>The spawned containers that do not stand on prop sockets, with their areas.</summary>
    public IReadOnlyList<FreeStandingContainer> FreeStanding { get; }

    /// <summary>
    /// Spawns a container during the raid. Every player within its relevance gets it like the others; it holds the
    /// same contents as any container.
    /// </summary>
    /// <param name="type">Its class.</param>
    /// <param name="location">World location.</param>
    /// <param name="rotation">World rotation in degrees.</param>
    /// <returns>The container, or null when the raid has no ids left for it.</returns>
    public LootContainer? Spawn(LootContainerClass type, Vector3D location, Rotator rotation)
    {
        ArgumentNullException.ThrowIfNull(type);
        if (_nextGuid + (2 * (ulong)(type.Components.Count + 1)) > EndGuid)
        {
            return null;
        }

        ulong animation = type.SearchAnimation is { } path ? _exported[path] : 0;
        return Place(LootContainerSource.Operator, location, rotation, null, type, _exported[type.Container.Archetype], animation);
    }

    /// <summary>The container a search interaction belongs to.</summary>
    /// <param name="interaction">Interaction id.</param>
    /// <returns>The container, or null.</returns>
    public LootContainer? ByInteraction(ulong interaction) => _interactions.GetValueOrDefault(interaction);

    /// <summary>The containers relevant to a player at a location: those within the containers' net cull distance.</summary>
    /// <param name="location">The player's location.</param>
    /// <returns>The containers, nearest first.</returns>
    public IReadOnlyList<LootContainer> Relevant(Vector3D location)
    {
        const double Limit = LootContainerClasses.NetCullDistance * LootContainerClasses.NetCullDistance;
        (long x, long y) = Cell(location);
        List<(LootContainer Container, double Distance)> found = [];
        for (long cellX = x - 1; cellX <= x + 1; cellX++)
        {
            for (long cellY = y - 1; cellY <= y + 1; cellY++)
            {
                if (!_cells.TryGetValue((cellX, cellY), out List<LootContainer>? inCell))
                {
                    continue;
                }

                found.AddRange(inCell
                    .Select(container => (Container: container, Distance: DistanceSquared(container.Location, location)))
                    .Where(entry => entry.Distance <= Limit));
            }
        }

        return [.. found.OrderBy(entry => entry.Distance).ThenBy(entry => entry.Container.Index).Select(entry => entry.Container)];
    }

    /// <summary>Exports the container's class default and the animation set of its search.</summary>
    /// <param name="container">The container.</param>
    /// <returns>The bunch.</returns>
    public ChannelBunch ExportBunch(LootContainer container)
    {
        ArgumentNullException.ThrowIfNull(container);
        return Bunches.Exports(_classExports, container.SearchAnimation == 0 ? [container.Archetype] : [container.Archetype, container.SearchAnimation]);
    }

    /// <summary>
    /// Opens the container's channel: spawns the actor at its place, or, for a client that keeps it dormant, names the
    /// actor it already has.
    /// </summary>
    /// <param name="container">The container.</param>
    /// <param name="channel">Channel index.</param>
    /// <param name="sequence">Reliable sequence number.</param>
    /// <param name="openedAt">Server time it was first opened, once it has been (it then shows open).</param>
    /// <returns>The bunch.</returns>
    public ChannelBunch OpenBunch(LootContainer container, int channel, int sequence, double? openedAt = null)
    {
        ArgumentNullException.ThrowIfNull(container);
        Dictionary<string, object?> values = new()
        {
            [PropertyNames.Role] = (ulong)NetRole.Authority,
            [PropertyNames.RemoteRole] = (ulong)NetRole.SimulatedProxy,
        };
        if (openedAt is { } opened)
        {
            values[PropertyNames.CurrentOpenTime] = opened;
        }

        ActorContentBlock actor = _replication.Encode(container.Class.ActorClass, values);
        ActorSpawn spawn = new(container.Guid, container.Archetype, 0, container.Location, container.Rotation);
        return new ActorInitialization(spawn, null, [actor]).ToBunch(channel, sequence, []);
    }

    /// <summary>Names the container's components.</summary>
    /// <param name="container">The container.</param>
    /// <returns>The bunch.</returns>
    public static ChannelBunch ComponentExportBunch(LootContainer container)
    {
        ArgumentNullException.ThrowIfNull(container);
        ObjectExports exports = new([
            new DynamicActorExport(container.Guid),
            .. container.Components.Select((part, index) => new StableSubobjectExport(part, container.Guid, container.Class.Components[index].Name, 0)),
        ]);
        return Bunches.Exports(exports, container.Components);
    }

    /// <summary>
    /// Sends what a player sees of the container: the search's state (and its hold while this player searches), once
    /// opened the open container and the search as opened, and the item container while this player has it open.
    /// </summary>
    /// <param name="container">The container.</param>
    /// <param name="channel">Channel index.</param>
    /// <param name="view">What the player sees.</param>
    /// <returns>The bunch.</returns>
    public ChannelBunch StateBunch(LootContainer container, int channel, LootView view)
    {
        ArgumentNullException.ThrowIfNull(container);
        Dictionary<string, object?> values = new()
        {
            [PropertyNames.CurrentInteractionState] = (ulong)view.State,
            [PropertyNames.InteractionServerHoldStartedTime] = view.HoldStartedAt ?? LootInteractions.NoHoldStartedTime,
        };

        if (view.OpenedAt is { } opened)
        {
            Opened(values, opened);
        }

        ActorContentBlock search = _replication.Encode(container.InteractionClass, values);
        ActorContentBlock? actor = view.OpenedAt is { } openedAt
            ? _replication.Encode(container.Class.ActorClass, new Dictionary<string, object?> { [PropertyNames.CurrentOpenTime] = openedAt })
            : null;
        ActorContentBlock? items = view.ItemContainer is { } id
            ? _replication.Encode(container.ItemContainerClass, new Dictionary<string, object?>
            {
                [$"{PropertyNames.Content}.{PropertyNames.Id}"] = id,
                [$"{PropertyNames.Content}.{PropertyNames.Revision}"] = (ushort)0,
            })
            : null;
        return Bunches.Actor(channel, writer =>
        {
            actor?.Write(writer);
            new StableSubobjectContent(container.Interaction, search).Write(writer);
            if (items is not null)
            {
                new StableSubobjectContent(container.ItemContainer, items).Write(writer);
            }
        });
    }

    // What changes on a search interaction once its container has been opened: the breach is over for the whole
    // raid, so the next search needs no raider tool, no hold and no breach animation (the prompt becomes "Search").
    private static void Opened(Dictionary<string, object?> values, double firstOpenedAt)
    {
        values[PropertyNames.HasBeenOpened] = true;
        values[PropertyNames.FirstOpenedServerTime] = firstOpenedAt;
        values[PropertyNames.EquipRaiderToolDuringInitialization] = false;
        values[PropertyNames.HoldDuration] = LootInteractions.OpenedHoldSeconds;
        values[PropertyNames.UseAnimationContainer] = false;
        values[PropertyNames.UseWaitDelayForAnimationNonLocal] = false;
    }

    // Gives a container the next ids and indexes it by its interaction and its place.
    private LootContainer Place(LootContainerSource source, Vector3D location, Rotator rotation, LootSocket? socket, LootContainerClass type, ulong archetype, ulong animation)
    {
        ulong guid = _nextGuid;
        ulong[] parts = [.. type.Components.Select((_, index) => guid + (2 * (ulong)(index + 1)))];
        _nextGuid = guid + (2 * (ulong)(type.Components.Count + 1));
        LootContainer container = new(_containers.Count, source, location, rotation, socket, type, guid, archetype, animation, parts);
        _containers.Add(container);
        _interactions[container.Interaction] = container;
        (long X, long Y) cell = Cell(location);
        if (!_cells.TryGetValue(cell, out List<LootContainer>? inCell))
        {
            _cells[cell] = inCell = [];
        }

        inCell.Add(container);
        return container;
    }

    private static (long X, long Y) Cell(Vector3D location) =>
        ((long)Math.Floor(location.X / LootContainerClasses.NetCullDistance), (long)Math.Floor(location.Y / LootContainerClasses.NetCullDistance));

    private static double DistanceSquared(Vector3D a, Vector3D b) =>
        ((a.X - b.X) * (a.X - b.X)) + ((a.Y - b.Y) * (a.Y - b.Y)) + ((a.Z - b.Z) * (a.Z - b.Z));
}

/// <summary>
/// What each loot container of a raid holds, shared by its players: a container keeps what the players who
/// opened it left inside, and the time it was first opened.
/// </summary>
/// <param name="catalog">The raid's containers.</param>
public sealed class LootStates(LootCatalog catalog)
{
    private readonly Dictionary<int, IReadOnlyList<ContainerItem?>> _items = [];
    private readonly Dictionary<int, double> _openedAt = [];
    private readonly List<int> _openings = [];

    private static readonly ushort s_duck = checked((ushort)LoadoutDefinitions.UsableIndex(StorageItems.RubberDuckAssetId));

    /// <summary>Unit weight of each item type a container may hold.</summary>
    public static IReadOnlyDictionary<ushort, double> UnitWeights { get; } = new Dictionary<ushort, double>
    {
        [s_duck] = LoadoutDefinitions.ItemWeight(StorageItems.RubberDuckAssetId),
    };

    /// <summary>The containers in the order they were first opened: every player's view of them changed then.</summary>
    public IReadOnlyList<int> Openings => _openings;

    /// <summary>A container's slots: one rubber duck until real loot tables are supported.</summary>
    /// <param name="index">Container index.</param>
    /// <returns>Its slots.</returns>
    public IReadOnlyList<ContainerItem?> Items(int index)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, catalog.Containers.Count);
        if (_items.TryGetValue(index, out IReadOnlyList<ContainerItem?>? items))
        {
            return items;
        }

        ContainerItem?[] slots = new ContainerItem?[catalog.Containers[index].Class.SlotCount];
        slots[0] = new ContainerItem(s_duck, Guid.NewGuid(), 1, 1);
        return _items[index] = Array.AsReadOnly(slots);
    }

    /// <summary>Keeps what a player left in a container.</summary>
    /// <param name="index">Container index.</param>
    /// <param name="items">Its slots.</param>
    public void Store(int index, IReadOnlyList<ContainerItem?> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        _ = Items(index);
        _items[index] = Array.AsReadOnly([.. items]);
    }

    /// <summary>When a container was first opened, if it has been.</summary>
    /// <param name="index">Container index.</param>
    /// <returns>Server time, or null.</returns>
    public double? OpenedAt(int index) => _openedAt.TryGetValue(index, out double at) ? at : null;

    /// <summary>Marks a container opened; opening it again keeps the first time.</summary>
    /// <param name="index">Container index.</param>
    /// <param name="at">Server time.</param>
    public void Open(int index, double at)
    {
        if (_openedAt.TryAdd(index, at))
        {
            _openings.Add(index);
        }
    }
}
