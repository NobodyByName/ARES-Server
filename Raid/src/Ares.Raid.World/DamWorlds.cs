using System.Security.Cryptography;
using Ares.Raid.Replication;
using Ares.Raid.Transport;
using Ares.Structs.Core.Serialization;
using Ares.Structs.Game.Replication;
using Ares.Structs.Game.World;
using Ares.Structs.Net.Control;
using Ares.Structs.Net.GameplayTags;
using Ares.Structs.Net.Replication;

namespace Ares.Raid.World;

/// <summary>A prepared Dam raid: its seed, round, lighting, spawn, actors and doors.</summary>
public sealed class RaidWorld
{
    /// <summary>Channel of the level script actor.</summary>
    private const int LevelScriptChannel = 4;

    /// <summary>Channel of the game state.</summary>
    private const int GameStateChannel = 5;

    /// <summary>Channel of the world data layers actor.</summary>
    private const int DataLayersChannel = 7;

    /// <summary>First channel of the global actors.</summary>
    private const int FirstGlobalChannel = 8;

    internal RaidWorld(DamWorlds worlds, string matchId, IReadOnlyList<long> players, int seed, string lighting)
    {
        MatchId = matchId;
        Players = players;
        Seed = seed;
        RoundConfiguration round = DamRounds.All[seed % DamRounds.All.Count];
        Lighting = lighting;
        IReadOnlyList<PlayerStart> starts = Spawns.Eligible(round);
        if (starts.Count == 0)
        {
            throw new WorldPreparationException("The round has no eligible player start");
        }

        Spawn = Spawns.Place(starts[seed % starts.Count], 0);
        Imports = worlds.Imports;
        Doors = worlds.Doors;
        Clock = worlds.Clock;
        State = new WorldState(worlds.Replication, Imports, matchId, seed);
        ReplicationContext replication = worlds.Replication;
        (ActiveDataLayers, IReadOnlyList<string> loadedLayers) = DataLayerNames(lighting, round);
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

    /// <summary>Reserved players.</summary>
    public IReadOnlyList<long> Players { get; }

    /// <summary>Session seed.</summary>
    public int Seed { get; }

    /// <summary>Lighting tag.</summary>
    public string Lighting { get; }

    /// <summary>Names of the active data layers: lighting, round content and those active from the start.</summary>
    public IReadOnlyList<string> ActiveDataLayers { get; }

    /// <summary>Where the player spawns.</summary>
    public SpawnPlacement Spawn { get; }

    /// <summary>The world's static exports.</summary>
    public WorldImports Imports { get; }

    /// <summary>The map doors.</summary>
    public DoorCatalog Doors { get; }

    /// <summary>Door state of this raid.</summary>
    public DoorStates DoorStates { get; } = new(DamDoors.All);

    /// <summary>Server actors of this raid.</summary>
    public WorldState State { get; }

    /// <summary>Map actors replicated before the player actors.</summary>
    public IReadOnlyList<StaticWorldActor> StaticActors { get; }

    /// <summary>Spawned world actors replicated before the player actors.</summary>
    public IReadOnlyList<SpawnedWorldActor> SpawnedActors { get; }

    /// <summary>Level packages the client may report: the map and its streaming cells.</summary>
    public static IReadOnlySet<string> LevelPackages { get; } = new HashSet<string>([DamMap.MapPackage, .. DamStreamingCells.Packages], StringComparer.Ordinal);

    /// <summary>Monotonic clock in seconds.</summary>
    public Func<double> Clock { get; }

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
/// The Dam raid of this server: prepared for one reservation at a time, released when it ends. Players of
/// the reservation are welcomed to the map and get their actors after they join it.
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
            [KnownEnums.NetRole, KnownEnums.GamePhase, DamDoors.InteractionState],
            Imports.Classes.ToDictionary(entry => entry.Value, entry => entry.Key));
        MapPaths paths = new();
        Doors = new DoorCatalog(Replication, paths);
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

    /// <inheritdoc />
    public void Prepare(string matchId, IReadOnlyList<long> players)
    {
        ArgumentNullException.ThrowIfNull(matchId);
        ArgumentNullException.ThrowIfNull(players);
        if (players is not [> 0])
        {
            throw new WorldPreparationException("The Dam supports one reserved player");
        }

        lock (_lock)
        {
            if (_current is { } current)
            {
                if (current.MatchId != matchId || !current.Players.SequenceEqual(players))
                {
                    throw new WorldPreparationException("Another reservation owns this server");
                }

                return;
            }

            int seed = _random();
            _current = new RaidWorld(this, matchId, [.. players], seed, _lighting ?? PickLighting(_random()));
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
            return _current?.MatchId == matchId;
        }
    }

    /// <inheritdoc />
    public ControlWelcome? WelcomeFor(SessionPrincipal principal) => Raid(principal) is null ? null : RaidWorld.Welcome;

    /// <inheritdoc />
    public IPlayerChannels StartPlayer(SessionPrincipal principal, LoginViewport viewport, ChannelIo io)
    {
        RaidWorld raid = Raid(principal) ?? throw new InvalidPacketException("The raid ended");
        PlayerChannels channels = new(raid, raid.State.PlayerFor(principal, raid.Spawn.Location), viewport);
        channels.Start(io);
        return channels;
    }

    private RaidWorld? Raid(SessionPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);
        RaidWorld? raid = Current;
        return raid is null || (raid.MatchId == principal.MatchId && raid.Players.Contains(principal.UserId))
            ? raid
            : throw new InvalidPacketException("Player does not own this raid");
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
