using System.Security.Cryptography;
using Ares.Common.Game.Identity;
using Ares.Common.Game.Inventory;
using Ares.Common.Maps;
using Ares.Raid.Replication;
using Ares.Raid.Transport;
using Ares.Raid.World.Collision;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Core.Serialization;
using Ares.Structs.Game.Replication;
using Ares.Structs.Game.World;
using Ares.Structs.Net.Control;
using Ares.Structs.Net.GameplayTags;
using Ares.Structs.Net.Replication;

namespace Ares.Raid.World;

/// <summary>A prepared Dam raid: its seed, round, lighting, players, actors, doors, ladders, loot and elevators.</summary>
public sealed class RaidWorld
{
    private readonly Lock _lock = new();
    private readonly IReadOnlyList<PlayerStart> _starts;
    private readonly List<RaidPlayer> _players = [];

    /// <summary>Channel of the level script actor.</summary>
    private const int LevelScriptChannel = 4;

    /// <summary>Channel of the game state.</summary>
    public const int GameStateChannel = 5;

    /// <summary>Channel of the world data layers actor.</summary>
    private const int DataLayersChannel = 7;

    /// <summary>First channel of the global actors.</summary>
    private const int FirstGlobalChannel = 8;

    internal RaidWorld(DamWorlds worlds, string matchId, int seed, string lighting)
    {
        MatchId = matchId;
        Seed = seed;
        RoundConfiguration round = DamRounds.All[seed % DamRounds.All.Count];
        Lighting = lighting;
        _starts = Spawns.Eligible(round);
        if (_starts.Count == 0)
        {
            throw new WorldPreparationException("The round has no eligible player start");
        }

        Imports = worlds.Imports;
        Doors = worlds.Doors;
        Ladders = worlds.Ladders;
        Loot = worlds.Loot;
        Extraction = worlds.Extraction;
        ExtractionStates = new ExtractionStates(Extraction.Points.Count);
        Clock = worlds.Clock;
        StartedAt = Clock();
        State = new WorldState(worlds.Replication, Imports, matchId, seed);
        ReplicationContext replication = worlds.Replication;
        (ActiveDataLayers, IReadOnlyList<string> loadedLayers) = DataLayerNames(lighting, round);
        ulong lootEnd = Loot.Containers.Count == 0 ? 0 : Loot.Containers.Max(container => container.Components.Append(container.Guid).Max());
        Pickups = new WorldPickups(replication, State, lootEnd + 2);
        Collision = worlds.MapCollision is null ? null : new CollisionWorld(worlds.MapCollision, ActiveDataLayers);
        StaticActors =
        [
            new StaticWorldActor(LevelScriptChannel, Imports.LevelScriptGuid, DamMap.LevelScriptClass, [replication.Encode(DamMap.LevelScriptClass, new Dictionary<string, object?>
            {
                [PropertyNames.Role] = (ulong)NetRole.Authority,
                [PropertyNames.RemoteRole] = (ulong)NetRole.SimulatedProxy,
                [PropertyNames.SelectedLighting] = new GameplayTag(lighting),
            })]),
            DataLayers(replication, Imports.DataLayersGuid, ActiveDataLayers, loadedLayers),

            // The game spawns these on both sides with stable names; replicating them gives the client its
            // usual simulated-proxy roles over them.
            .. Imports.GlobalActors.Select((actor, index) => new StaticWorldActor(
                FirstGlobalChannel + index,
                actor.Guid,
                actor.Actor.Class,
                [replication.Encode(actor.Actor.Class, new Dictionary<string, object?> { [PropertyNames.Role] = (ulong)NetRole.Authority, [PropertyNames.RemoteRole] = (ulong)NetRole.SimulatedProxy })])),
        ];
        SpawnedActors = [State.GameStateActor(GameStateChannel)];
    }

    /// <summary>Match id of the reservation.</summary>
    public string MatchId { get; }

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

    /// <summary>The loot containers.</summary>
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

    /// <summary>Door state of this raid.</summary>
    public DoorStates DoorStates { get; } = new(DamDoors.All);

    /// <summary>Loot container contents of this raid.</summary>
    public LootStates LootStates { get; } = new(DamLoot.All.Count);

    /// <summary>The item stacks lying in this raid.</summary>
    public WorldPickups Pickups { get; }

    /// <summary>Most players the raid can hold: one per spawn point of its player start.</summary>
    public int MaxPlayers => _starts[Seed % _starts.Count].SpawnPoints.Count;

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

            _players.AddRange(players.Select((id, slot) => new RaidPlayer(id, loadouts[slot], profiles, Spawns.Place(start, firstSlot + slot))));
        }
    }

    /// <summary>Whether a party is already in this raid with the same loadouts and profiles.</summary>
    internal bool HasParty(IReadOnlyList<long> players, IReadOnlyList<PlayerLoadout> loadouts, IReadOnlyList<CompactPlayerProfile> profiles) =>
        players.Select(Player).Zip(loadouts).All(pair => pair.First is { } player
            && InventorySnapshots.Same(player.Loadout, pair.Second) && player.Profiles.SequenceEqual(profiles));

    /// <summary>Level packages the client may report: the map and its streaming cells.</summary>
    public static IReadOnlySet<string> LevelPackages { get; } = new HashSet<string>([DamMap.MapPackage, .. DamStreamingCells.Packages], StringComparer.Ordinal);

    /// <summary>Monotonic clock in seconds.</summary>
    public Func<double> Clock { get; }

    /// <summary>The raid's world time (seconds since it was prepared) at a clock reading.</summary>
    /// <param name="now">Clock reading.</param>
    /// <returns>World time in seconds.</returns>
    public double WorldSeconds(double now) => Math.Max(0, now - StartedAt);

    // When the raid was prepared, on Clock: the start of its world time.
    private double StartedAt { get; }

    /// <summary>The Welcome that sends a client to this world.</summary>
    public static ControlWelcome Welcome { get; } = new(DamMap.MapPackage, DamMap.GameModeClass, "");

    // The selected lighting layer, the round's layers present on this map (locked doors, toxic swamp…) and
    // the layers active from the start are active; the other layers that start loaded stay loaded.
    private static (string[] Active, string[] Loaded) DataLayerNames(string lighting, RoundConfiguration round)
    {
        LightingScenario scenario = DamLighting.Scenarios.Single(row => row.Tag == lighting);
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
        return new StaticWorldActor(DataLayersChannel, guid, DamMap.WorldDataLayersClass, [replication.Encode(DamMap.WorldDataLayersClass, new Dictionary<string, object?>
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

/// <summary>
/// The Dam raid of this server: prepared by the first party's reservation, joined by later parties, released
/// when its reservations end. Players of the raid are welcomed to the map and get their actors after they
/// join it.
/// </summary>
public sealed class DamWorlds : IWorldLifecycle, IRaidWorld
{
    private readonly Lock _lock = new();
    private readonly string? _lighting;
    private readonly Func<int> _random;
    private RaidWorld? _current;

    /// <summary>Creates the world host.</summary>
    /// <param name="clock">Monotonic clock in seconds.</param>
    /// <param name="lighting">Lighting tag for every raid; null picks one by the authored weights.</param>
    /// <param name="random">Random numbers from 0 to 2^31 - 2 (seeds, lighting); a secure generator by default.</param>
    public DamWorlds(Func<double> clock, string? lighting = null, Func<int>? random = null)
    {
        Clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _random = random ?? (() => RandomNumberGenerator.GetInt32(int.MaxValue));
        if (lighting is not null && !Lightings.Contains(lighting))
        {
            throw new ArgumentException("Not a lighting of this map.", nameof(lighting));
        }

        _lighting = lighting;
        Imports = new WorldImports(GameReplication.Catalog);
        Replication = new ReplicationContext(
            GameReplication.Catalog,
            [KnownEnums.NetRole, KnownEnums.GamePhase, KnownEnums.ViewTargetBlendFunction, DamDoors.InteractionState, DamExtraction.StateEnum],
            Imports.Classes.ToDictionary(entry => entry.Value, entry => entry.Key));
        MapPaths paths = new();
        Doors = new DoorCatalog(Replication, paths);
        Ladders = new LadderCatalog(Replication, paths);
        Extraction = new ExtractionCatalog(Replication, paths);
        Loot = new LootCatalog(Replication);
    }

    /// <summary>Lightings a raid can use: those of the map's location with a weight.</summary>
    public static IReadOnlyList<string> Lightings { get; } =
        [.. DamLighting.Scenarios.Where(row => row.Weight > 0 && row.FilterTags.Contains(DamMap.LocationTag)).Select(row => row.Tag)];

    /// <summary>The prepared raid, if any.</summary>
    public RaidWorld? Current
    {
        get
        {
            lock (_lock)
            {
                return _current;
            }
        }
    }

    internal Func<double> Clock { get; }

    internal WorldImports Imports { get; }

    internal ReplicationContext Replication { get; }

    internal DoorCatalog Doors { get; }

    internal LadderCatalog Ladders { get; }

    internal LootCatalog Loot { get; }

    internal ExtractionCatalog Extraction { get; }

    /// <inheritdoc />
    public void Prepare(string matchId, IReadOnlyList<long> players, IReadOnlyList<PlayerLoadout>? loadouts = null, IReadOnlyList<CompactPlayerProfile>? profiles = null)
    {
        ArgumentNullException.ThrowIfNull(matchId);
        (IReadOnlyList<PlayerLoadout> snapshots, IReadOnlyList<CompactPlayerProfile> profileSnapshots) = Party(players, loadouts, profiles);
        lock (_lock)
        {
            if (_current is { } current)
            {
                if (current.MatchId != matchId || !current.HasParty(players, snapshots, profileSnapshots))
                {
                    throw new WorldPreparationException("Another reservation owns this server");
                }

                return;
            }

            int seed = _random();
            RaidWorld raid = new(this, matchId, seed, _lighting ?? PickLighting(_random()));
            raid.AddParty(players, snapshots, profileSnapshots);
            _current = raid;
        }
    }

    /// <inheritdoc />
    public void Join(string matchId, IReadOnlyList<long> players, IReadOnlyList<PlayerLoadout>? loadouts = null, IReadOnlyList<CompactPlayerProfile>? profiles = null)
    {
        ArgumentNullException.ThrowIfNull(matchId);
        (IReadOnlyList<PlayerLoadout> snapshots, IReadOnlyList<CompactPlayerProfile> profileSnapshots) = Party(players, loadouts, profiles);
        lock (_lock)
        {
            if (_current is not { Ended: false } current || current.MatchId != matchId)
            {
                throw new WorldPreparationException("The raid is not running");
            }

            current.AddParty(players, snapshots, profileSnapshots);
        }
    }

    /// <inheritdoc />
    public void Leave(string matchId, IReadOnlyList<long> players)
    {
        ArgumentNullException.ThrowIfNull(players);
        lock (_lock)
        {
            if (_current?.MatchId != matchId)
            {
                return;
            }

            foreach (long id in players)
            {
                _current.Player(id)?.Leave();
            }
        }
    }

    /// <inheritdoc />
    public void Release(string matchId)
    {
        lock (_lock)
        {
            if (_current?.MatchId == matchId)
            {
                _current = null;
            }
        }
    }

    /// <inheritdoc />
    public bool AcceptingPlayers(string matchId)
    {
        lock (_lock)
        {
            return _current is { Ended: false } current && current.MatchId == matchId;
        }
    }

    /// <inheritdoc />
    public bool HasLeft(string matchId, long player)
    {
        lock (_lock)
        {
            return _current?.MatchId == matchId && _current.Player(player) is { Left: true };
        }
    }

    /// <inheritdoc />
    public ControlWelcome? WelcomeFor(SessionPrincipal principal) => Raid(principal) is null ? null : RaidWorld.Welcome;

    /// <inheritdoc />
    public IPlayerChannels StartPlayer(SessionPrincipal principal, PlayerLogin login, ChannelIo io)
    {
        ArgumentNullException.ThrowIfNull(login);
        (RaidWorld raid, RaidPlayer member) = Raid(principal) ?? throw new InvalidPacketException("The player has left the raid");
        SpawnPlacement spawn = member.Spawn;
        PlayerChannels channels = new(raid, member, raid.State.PlayerFor(principal, login.UniqueId, spawn.Location, new Rotator(0, spawn.Yaw, 0), member.Inventory), login);
        channels.Start(io);
        return channels;
    }

    // Parties are one player each until party invites exist: a reservation's routing credential admits its first player.
    private static (IReadOnlyList<PlayerLoadout> Loadouts, IReadOnlyList<CompactPlayerProfile> Profiles) Party(IReadOnlyList<long> players,
        IReadOnlyList<PlayerLoadout>? loadouts, IReadOnlyList<CompactPlayerProfile>? profiles)
    {
        ArgumentNullException.ThrowIfNull(players);
        if (players is not [> 0])
        {
            throw new WorldPreparationException("The Dam supports parties of one player");
        }

        try
        {
            return (InventorySnapshots.ForPlayers(players, loadouts), SquadProfiles.ForPlayers(players, profiles));
        }
        catch (InvalidDataException error)
        {
            throw new WorldPreparationException(error.Message);
        }
    }

    // The raid and the principal's player in it, or null when the raid is gone or the player has left.
    private (RaidWorld Raid, RaidPlayer Player)? Raid(SessionPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);
        RaidWorld? raid = Current;
        if (raid is null)
        {
            return null;
        }

        RaidPlayer member = raid.MatchId == principal.MatchId && raid.Player(principal.UserId) is { } player
            ? player
            : throw new InvalidPacketException("Player does not own this raid");
        return member.Left ? null : (raid, member);
    }

    // A weighted pick among the location's lightings.
    private static string PickLighting(int random)
    {
        LightingScenario[] rows = [.. DamLighting.Scenarios.Where(row => Lightings.Contains(row.Tag))];
        double point = random / (double)int.MaxValue * rows.Sum(row => row.Weight);
        foreach (LightingScenario row in rows)
        {
            if (point < row.Weight)
            {
                return row.Tag;
            }

            point -= row.Weight;
        }

        return rows[^1].Tag;
    }
}
