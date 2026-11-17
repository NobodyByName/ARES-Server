using System.Security.Cryptography;
using Ares.Common.Game.Identity;
using Ares.Common.Game.Inventory;
using Ares.Common.GameData;
using Ares.Common.Maps;
using Ares.Raid.Transport;
using Ares.Raid.World.Actors;
using Ares.Raid.World.Loot;
using Ares.Raid.World.Players;
using Ares.Raid.World.Replication;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Core.Serialization;
using Ares.Structs.Game.Replication;
using Ares.Structs.Game.World;
using Ares.Structs.Net.Control;
using Ares.Structs.Net.Replication;

namespace Ares.Raid.World;

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
    /// <param name="lootRules">How each raid picks the sockets that get a container; <see cref="LootSpawnRules.Dam"/> by default.</param>
    /// <param name="collision">The map's static collision, or null when the operator has none; each raid indexes it.</param>
    /// <param name="gameData">The game data the server loaded at startup; raids do not read it yet (null in tests that predate it).</param>
    public DamWorlds(Func<double> clock, string? lighting = null, Func<int>? random = null, LootSpawnRules? lootRules = null, MapCollision? collision = null,
        GameData? gameData = null)
    {
        Clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _random = random ?? (() => RandomNumberGenerator.GetInt32(int.MaxValue));
        if (lighting is not null && !Lightings.Contains(lighting))
        {
            throw new ArgumentException("Not a lighting of this map.", nameof(lighting));
        }

        _lighting = lighting;
        MapCollision = collision;
        GameData = gameData;
        LootRules = lootRules ?? LootSpawnRules.Dam;
        Imports = new WorldImports(GameReplication.Catalog);
        Replication = new ReplicationContext(
            GameReplication.Catalog,
            [KnownEnums.NetRole, KnownEnums.GamePhase, KnownEnums.ViewTargetBlendFunction, DamDoors.InteractionState, DamExtraction.StateEnum],
            Imports.Classes.ToDictionary(entry => entry.Value, entry => entry.Key));
        MapPaths paths = new();
        Doors = new DoorCatalog(Replication, paths);
        Ladders = new LadderCatalog(Replication, paths);
        Extraction = new ExtractionCatalog(Replication, paths);
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

    internal ExtractionCatalog Extraction { get; }

    internal MapCollision? MapCollision { get; }

    /// <summary>The game data the server loaded at startup, or null.</summary>
    public GameData? GameData { get; }

    internal LootSpawnRules LootRules { get; }

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
    public PlayerRaidEnd? Result(string matchId, long player)
    {
        lock (_lock)
        {
            return _current?.MatchId == matchId && _current.Player(player) is { Left: true } member ? member.End : null;
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
        member.Connect();
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
