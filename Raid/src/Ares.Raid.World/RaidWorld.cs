using Ares.Common.Game.Inventory;
using Ares.Common.GameData;
using Ares.Raid.World.Actors;
using Ares.Raid.World.Collision;
using Ares.Raid.World.Loot;
using Ares.Raid.World.Players;
using Ares.Raid.World.Replication;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Game.Replication;
using Ares.Structs.Game.World;
using Ares.Structs.Net.Control;
using Ares.Structs.Net.GameplayTags;
using Ares.Structs.Net.Replication;

namespace Ares.Raid.World;

/// <summary>A prepared Dam raid: its seed, round, lighting, players, actors, doors, ladders, loot, loose items and elevators.</summary>
public sealed class RaidWorld
{
    private readonly Lock _lock = new();
    private readonly IReadOnlyList<PlayerStart> _starts;
    private readonly List<RaidPlayer> _players = [];

    internal RaidWorld(DamWorlds worlds, string matchId, int seed, string lighting)
    {
        MatchId = matchId;
        Seed = seed;
        GameData = worlds.GameData;
        MapData map = GameData.Map;
        RoundConfiguration round = map.Rounds[seed % map.Rounds.Length];
        Lighting = lighting;
        Welcome = new ControlWelcome(map.MapPackage, map.GameMode.GameModeClass, "");
        LevelPackages = new HashSet<string>([map.MapPackage, .. map.StreamingCells], StringComparer.Ordinal);
        DoorStates = new DoorStates(map.Doors);
        _starts = Spawns.Eligible(map, round);
        if (_starts.Count == 0)
        {
            throw new WorldPreparationException("The round has no eligible player start");
        }

        Imports = worlds.Imports;
        Doors = worlds.Doors;
        ExploitAreas = new ExploitAreaStates(Doors.Areas, DoorStates, GameData.Settings.ExploitTrapResetSeconds);
        Ladders = worlds.Ladders;
        Extraction = worlds.Extraction;
        ExtractionStates = new ExtractionStates(map, GameData.Settings, Extraction.Points.Count);
        Clock = worlds.Clock;
        StartedAt = Clock();
        State = new WorldState(worlds.Replication, Imports, GameData, matchId, seed);
        ReplicationContext replication = worlds.Replication;
        (ActiveDataLayers, IReadOnlyList<string> loadedLayers) = DataLayerNames(map, lighting, round);
        Loot = new LootCatalog(GameData, replication, round.Name, ActiveDataLayers, seed, worlds.LootRules);
        LootStates = new LootStates(Loot, GameData.Items);
        ulong lootEnd = Loot.Containers.Count == 0 ? 0 : Loot.Containers.Max(container => container.Components.Append(container.Guid).Max());
        Pickups = new WorldPickups(replication, State, GameData.Items, lootEnd + 2);
        Arcs = new WorldArcs(replication, Imports);
        Collision = worlds.MapCollision is null ? null : new CollisionWorld(worlds.MapCollision, ActiveDataLayers);
        LooseLoot = LooseLoot.Roll(GameData.LooseRolls, map.LooseItemSockets, worlds.LootRules, GameData.LootRules, ActiveDataLayers, seed, Collision);
        LooseLoot.Place(Pickups);
        Channels = new ChannelLayout(Imports.GlobalActors.Count, Doors.Doors.Count, Ladders.Ladders.Count, Extraction.Points.Count, MaxPlayers, Doors.Areas.Count);
        StaticActors =
        [
            new StaticWorldActor(ChannelLayout.LevelScript, Imports.LevelScriptGuid, map.World.LevelScriptClass, [replication.Encode(map.World.LevelScriptClass, new Dictionary<string, object?>
            {
                [PropertyNames.Role] = (ulong)NetRole.Authority,
                [PropertyNames.RemoteRole] = (ulong)NetRole.SimulatedProxy,
                [PropertyNames.SelectedLighting] = new GameplayTag(lighting),
            })]),
            DataLayers(replication, Imports.DataLayersGuid, ActiveDataLayers, loadedLayers),

            // The game spawns these on both sides with stable names; replicating them gives the client its
            // usual simulated-proxy roles over them.
            .. Imports.GlobalActors.Select((actor, index) => new StaticWorldActor(
                Channels.Globals.Channel(index),
                actor.Guid,
                actor.Actor.Class,
                [replication.Encode(actor.Actor.Class, new Dictionary<string, object?> { [PropertyNames.Role] = (ulong)NetRole.Authority, [PropertyNames.RemoteRole] = (ulong)NetRole.SimulatedProxy })])),
        ];
        SpawnedActors = [State.GameStateActor(ChannelLayout.GameState, Loot.WorldItemClassBits)];
    }

    /// <summary>Match id of the reservation.</summary>
    public string MatchId { get; }

    /// <summary>The game data the raid runs on.</summary>
    public RaidGameData GameData { get; }

    /// <summary>Every player of the raid has left it.</summary>
    public bool Ended
    {
        get
        {
            lock (_lock)
            {
                return _players.Count > 0 && _players.TrueForAll(player => player.Left);
            }
        }
    }

    /// <summary>The raid's players, in the order their parties joined.</summary>
    public IReadOnlyList<RaidPlayer> Players
    {
        get
        {
            lock (_lock)
            {
                return [.. _players];
            }
        }
    }

    /// <summary>A player of the raid.</summary>
    /// <param name="id">User id.</param>
    /// <returns>The player, or null when not in this raid.</returns>
    public RaidPlayer? Player(long id)
    {
        lock (_lock)
        {
            return _players.Find(player => player.Id == id);
        }
    }

    /// <summary>Session seed.</summary>
    public int Seed { get; }

    /// <summary>Lighting tag.</summary>
    public string Lighting { get; }

    /// <summary>Names of the active data layers: lighting, round content and those active from the start.</summary>
    public IReadOnlyList<string> ActiveDataLayers { get; }

    /// <summary>The world's static exports.</summary>
    public WorldImports Imports { get; }

    /// <summary>The map doors.</summary>
    public DoorCatalog Doors { get; }

    /// <summary>The map ladders.</summary>
    public LadderCatalog Ladders { get; }

    /// <summary>The loot containers of this raid.</summary>
    public LootCatalog Loot { get; }

    /// <summary>The raid's static collision (base map and active data layers), or null when the map has none.</summary>
    public CollisionWorld? Collision { get; }

    /// <summary>The extraction elevators.</summary>
    public ExtractionCatalog Extraction { get; }

    /// <summary>Extraction elevator state of this raid.</summary>
    public ExtractionStates ExtractionStates { get; }

    /// <summary>
    /// The players still in the raid (not left, not departed with an elevator) whose last reported location is inside an
    /// elevator's extraction zone.
    /// </summary>
    /// <param name="elevator">Elevator index.</param>
    /// <returns>Their user ids.</returns>
    public IReadOnlyList<long> PlayersInside(int elevator)
    {
        ExtractionPoint point = Extraction.Points[elevator];
        return [.. Players.Where(player => player is { Left: false, Departed: false, Motion: { } motion } && point.Contains(motion.Motion.Position)).Select(player => player.Id)];
    }

    /// <summary>The current world locations of the players still in the raid.</summary>
    /// <returns>Their last reported locations.</returns>
    public IEnumerable<Vector3D> PlayerLocations() =>
        Players.Where(player => player is { Left: false, Departed: false, Motion: not null }).Select(player => player.Motion!.Motion.Position);

    /// <summary>Door state of this raid.</summary>
    public DoorStates DoorStates { get; }

    /// <summary>The locked rooms' exploit prevention areas of this raid.</summary>
    public ExploitAreaStates ExploitAreas { get; }

    /// <summary>Loot container contents of this raid.</summary>
    public LootStates LootStates { get; }

    /// <summary>The loose items the raid placed on the map's sockets at its start (the first pickups).</summary>
    public LooseLoot LooseLoot { get; }

    /// <summary>The item stacks lying in this raid.</summary>
    public WorldPickups Pickups { get; }

    /// <summary>The ARCs in the raid.</summary>
    public WorldArcs Arcs { get; }

    /// <summary>Most players the raid can hold: one per spawn point of its player start.</summary>
    private int MaxPlayers => _starts[Seed % _starts.Count].SpawnPoints.Count;

    /// <summary>Which actor channel carries what for the raid's connections.</summary>
    public ChannelLayout Channels { get; }

    /// <summary>Server actors of this raid.</summary>
    public WorldState State { get; }

    /// <summary>Map actors replicated before the player actors.</summary>
    public IReadOnlyList<StaticWorldActor> StaticActors { get; }

    /// <summary>Spawned world actors replicated before the player actors.</summary>
    public IReadOnlyList<SpawnedWorldActor> SpawnedActors { get; }

    /// <summary>
    /// Adds a party: it spawns at the group start the seed selects, one spawn point each.
    /// </summary>
    internal void AddParty(IReadOnlyList<long> players, IReadOnlyList<PlayerLoadout> loadouts, IReadOnlyList<CompactPlayerProfile> profiles)
    {
        lock (_lock)
        {
            if (_players.Exists(player => players.Contains(player.Id)))
            {
                throw new WorldPreparationException("A player of the party is already in this raid");
            }

            // Temporary: every party shares the seed's group start, each player on the next free spawn point, so
            // players meet right away while parties cannot be joined yet.
            PlayerStart start = _starts[Seed % _starts.Count];
            int firstSlot = _players.Count;
            if (firstSlot + players.Count > MaxPlayers)
            {
                throw new WorldPreparationException("The shared player start has no free spawn point");
            }

            _players.AddRange(players.Select((id, slot) => new RaidPlayer(GameData, id, loadouts[slot], profiles, Spawns.Place(GameData.Map, start, firstSlot + slot))));
        }
    }

    /// <summary>Whether a party is already in this raid with the same loadouts and profiles.</summary>
    internal bool HasParty(IReadOnlyList<long> players, IReadOnlyList<PlayerLoadout> loadouts, IReadOnlyList<CompactPlayerProfile> profiles) =>
        players.Select(Player).Zip(loadouts).All(pair => pair.First is { } player
            && InventorySnapshots.Same(player.Loadout, pair.Second) && player.Profiles.SequenceEqual(profiles));

    /// <summary>Level packages the client may report: the map and its streaming cells.</summary>
    public IReadOnlySet<string> LevelPackages { get; }

    /// <summary>Monotonic clock in seconds.</summary>
    public Func<double> Clock { get; }

    /// <summary>The raid's world time (seconds since it was prepared) at a clock reading.</summary>
    /// <param name="now">Clock reading.</param>
    /// <returns>World time in seconds.</returns>
    public double WorldSeconds(double now) => Math.Max(0, now - StartedAt);

    // When the raid was prepared, on Clock: the start of its world time.
    private double StartedAt { get; }

    /// <summary>The Welcome that sends a client to this world.</summary>
    public ControlWelcome Welcome { get; }

    // The selected lighting layer, the round's layers present on this map (locked doors, toxic swamp…) and
    // the layers active from the start are active; the other layers that start loaded stay loaded.
    private static (string[] Active, string[] Loaded) DataLayerNames(MapData map, string lighting, RoundConfiguration round)
    {
        LightingScenario scenario = map.LightingScenarios.Single(row => row.Tag == lighting);
        string[] lightingLayers = [.. DamDataLayers.All.Where(layer => Package(layer) == scenario.DataLayerPackage).Select(layer => layer.Name)];
        if (lightingLayers.Length != 1)
        {
            throw new WorldPreparationException("The lighting has no unique data layer");
        }

        HashSet<string> roundPackages = [.. round.DataLayers.Select(layer => layer.Package)];
        string[] selected = [.. lightingLayers, .. DamDataLayers.All.Where(layer => roundPackages.Contains(Package(layer))).Select(layer => layer.Name)];
        string[] active = [.. selected.Concat(DamDataLayers.All.Where(layer => layer.InitialRuntimeState == DataLayerRuntimeState.Activated).Select(layer => layer.Name))
            .Distinct().Order(StringComparer.Ordinal)];
        string[] loaded = [.. DamDataLayers.All.Where(layer => layer.InitialRuntimeState == DataLayerRuntimeState.Loaded && !selected.Contains(layer.Name))
            .Select(layer => layer.Name).Order(StringComparer.Ordinal)];
        return (active, loaded);

        static string Package(DataLayer layer) => layer.Asset[..layer.Asset.LastIndexOf('.')];
    }

    // Every Dam layer is a root layer, so the effective state is the target state.
    private static StaticWorldActor DataLayers(ReplicationContext replication, ulong guid, IReadOnlyList<string> activeNames, IReadOnlyList<string> loadedNames)
    {
        object?[] active = [.. activeNames.Select(object? (name) => new NetworkName(name, 0))];
        object?[] loaded = [.. loadedNames.Select(object? (name) => new NetworkName(name, 0))];
        return new StaticWorldActor(ChannelLayout.DataLayers, guid, MapWorldClasses.WorldDataLayers, [replication.Encode(MapWorldClasses.WorldDataLayers, new Dictionary<string, object?>
        {
            [PropertyNames.Role] = (ulong)NetRole.Authority,
            [PropertyNames.RemoteRole] = (ulong)NetRole.SimulatedProxy,
            [PropertyNames.RepActiveDataLayerNames] = active,
            [PropertyNames.RepLoadedDataLayerNames] = loaded,
            [PropertyNames.RepEffectiveActiveDataLayerNames] = active,
            [PropertyNames.RepEffectiveLoadedDataLayerNames] = loaded,
        })]);
    }
}
