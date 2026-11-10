using System.Diagnostics;
using System.Security.Cryptography;
using Ares.Common.Control;
using Ares.Common.Control.Server;
using Ares.Common.Game.Identity;
using Ares.Common.Game.Inventory;
using Ares.Raid.Transport;
using Ares.Raid.World;
using Ares.Structs.Net.Replication;
using ScenarioNames = Ares.Structs.Backend.Matchmaking.ScenarioNames;

namespace Ares.Raid;

/// <summary>
/// The raid's reservations, one per matchmaking ticket. The first ticket prepares the raid; later
/// tickets join it while it has room and its fill window is open. Each reservation expires unless its
/// player keeps it alive and admits that player through its own routing credential. A Raid process
/// hosts one raid: it closes once every reservation is released, or when none arrives in time.
/// </summary>
public sealed class RaidAllocations : IAllocationService, IRouteResolver
{
    /// <summary>Seconds a reservation lives without activity.</summary>
    private const double DefaultInitializationTimeout = 120;

    /// <summary>Seconds after the first reservation during which other parties may join.</summary>
    private const double DefaultFillWindow = 60;

    private const double PollingInterval = 2.0;

    private readonly Lock _lock = new();
    private readonly GameServerEndpoint _endpoint;
    private readonly IWorldLifecycle _worlds;
    private readonly Func<double> _clock;
    private readonly double _timeout;
    private readonly int _maxPlayers;
    private readonly double _fillWindow;
    private readonly double _idleDeadline;
    private readonly List<Reservation> _parties = [];
    private string? _matchId;
    private double _fillDeadline;
    private bool _released;

    /// <summary>Creates the service.</summary>
    /// <param name="endpoint">Where players connect (its routing token is filled in per reservation).</param>
    /// <param name="worlds">Prepares the raid world.</param>
    /// <param name="clock">Monotonic clock in seconds.</param>
    /// <param name="maxPlayers">Most players in the raid, across every party.</param>
    /// <param name="initializationTimeout">Seconds a reservation lives without activity, and the first one may take to arrive (up to 600).</param>
    /// <param name="fillWindow">Seconds after the first reservation during which other parties may join (0 to 600).</param>
    public RaidAllocations(GameServerEndpoint endpoint, IWorldLifecycle worlds, Func<double>? clock = null, int maxPlayers = 1,
        double initializationTimeout = DefaultInitializationTimeout, double fillWindow = DefaultFillWindow)
    {
        _endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
        _worlds = worlds ?? throw new ArgumentNullException(nameof(worlds));
        if (initializationTimeout is <= 0 or > 600)
        {
            throw new ArgumentOutOfRangeException(nameof(initializationTimeout));
        }

        if (fillWindow is < 0 or > 600)
        {
            throw new ArgumentOutOfRangeException(nameof(fillWindow));
        }

        ArgumentOutOfRangeException.ThrowIfLessThan(maxPlayers, 1);

        long started = Stopwatch.GetTimestamp();
        _clock = clock ?? (() => Stopwatch.GetElapsedTime(started).TotalSeconds);
        _timeout = initializationTimeout;
        _maxPlayers = maxPlayers;
        _fillWindow = fillWindow;
        _idleDeadline = _clock() + initializationTimeout;
    }

    /// <summary>True once this raid is over: every reservation was released, or none arrived in time.</summary>
    public bool Closed
    {
        get
        {
            lock (_lock)
            {
                Maintain();
                return _released || (_matchId is null && _clock() >= _idleDeadline);
            }
        }
    }

    /// <inheritdoc />
    public AllocationResult Reserve(AllocationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!SessionAllocatorClient.IsCanonicalUuid(request.TicketId) || request.ScenarioName != ScenarioNames.TheDam
            || request.PartyMemberIds.Count < 1 || request.PartyMemberIds.Count > _maxPlayers || request.PartyMemberIds.Any(player => player <= 0))
        {
            return AllocationResult.Failure(StatusCodes.Status400BadRequest, "unsupported_session_allocation");
        }

        IReadOnlyList<PlayerLoadout> loadouts;
        try
        {
            loadouts = InventorySnapshots.ForPlayers(request.PartyMemberIds, request.Loadouts);
        }
        catch (InvalidDataException)
        {
            return AllocationResult.Failure(StatusCodes.Status400BadRequest, "invalid_session_loadout");
        }

        IReadOnlyList<CompactPlayerProfile> profiles;
        try
        {
            profiles = SquadProfiles.ForPlayers(request.PartyMemberIds, request.Profiles);
        }
        catch (InvalidDataException)
        {
            return AllocationResult.Failure(StatusCodes.Status400BadRequest, "invalid_session_profiles");
        }

        lock (_lock)
        {
            Maintain();
            if (_parties.Find(item => item.TicketId == request.TicketId) is { } existing)
            {
                return !existing.Players.SequenceEqual(request.PartyMemberIds)
                    || !existing.Profiles.SequenceEqual(profiles)
                    || !existing.Loadouts.Zip(loadouts).All(pair => InventorySnapshots.Same(pair.First, pair.Second))
                    ? AllocationResult.Failure(StatusCodes.Status409Conflict, "session_allocation_conflict")
                    : AllocationResult.Success(Status(existing));
            }

            if (_parties.Any(item => item.Players.Intersect(request.PartyMemberIds).Any()))
            {
                return AllocationResult.Failure(StatusCodes.Status409Conflict, "session_allocation_conflict");
            }

            return _matchId is null ? Open(request, loadouts, profiles) : Join(_matchId, request, loadouts, profiles);
        }
    }

    /// <inheritdoc />
    public AllocationResult Status(AllocationStatusRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!SessionAllocatorClient.IsCanonicalUuid(request.TicketId)
            || (request.MatchId is not null && !SessionAllocatorClient.IsCanonicalUuid(request.MatchId)))
        {
            return AllocationResult.Failure(StatusCodes.Status400BadRequest, "invalid_session_lookup");
        }

        lock (_lock)
        {
            Maintain();
            return _parties.Find(party => party.TicketId == request.TicketId) is { } item && (request.MatchId is null || _matchId == request.MatchId)
                ? AllocationResult.Success(Status(item))
                : AllocationResult.Failure(StatusCodes.Status404NotFound, "unknown_session_allocation");
        }
    }

    /// <inheritdoc />
    public SessionPrincipal? Route(ReadOnlySpan<byte> token, bool renew)
    {
        if (token.Length != RoutingAuthenticator.TokenBytes)
        {
            return null;
        }

        lock (_lock)
        {
            Maintain();
            if (_matchId is not { } matchId || !_worlds.AcceptingPlayers(matchId))
            {
                return null;
            }

            foreach (Reservation item in _parties)
            {
                if (!CryptographicOperations.FixedTimeEquals(token, item.RoutingToken))
                {
                    continue;
                }

                if (renew)
                {
                    item.Deadline = _clock() + _timeout;
                }

                return new SessionPrincipal(item.Players[0], matchId);
            }

            return null;
        }
    }

    private AllocationResult Open(AllocationRequest request, IReadOnlyList<PlayerLoadout> loadouts, IReadOnlyList<CompactPlayerProfile> profiles)
    {
        if (_released || _clock() >= _idleDeadline)
        {
            return AllocationResult.Failure(StatusCodes.Status409Conflict, "session_capacity_reached");
        }

        string matchId = Guid.NewGuid().ToString();
        Reservation candidate = Candidate(request, loadouts, profiles);
        try
        {
            _worlds.Prepare(matchId, candidate.Players, candidate.Loadouts, candidate.Profiles.Count == 0 ? null : candidate.Profiles);
        }
        catch (WorldPreparationException)
        {
            _worlds.Release(matchId);
            return AllocationResult.Failure(StatusCodes.Status503ServiceUnavailable, "world_initialization_failed");
        }

        _matchId = matchId;
        _fillDeadline = _clock() + _fillWindow;
        _parties.Add(candidate);
        return AllocationResult.Success(Status(candidate));
    }

    private AllocationResult Join(string matchId, AllocationRequest request, IReadOnlyList<PlayerLoadout> loadouts, IReadOnlyList<CompactPlayerProfile> profiles)
    {
        if (_clock() >= _fillDeadline || _parties.Sum(item => item.Players.Length) + request.PartyMemberIds.Count > _maxPlayers
            || !_worlds.AcceptingPlayers(matchId))
        {
            return AllocationResult.Failure(StatusCodes.Status409Conflict, "session_capacity_reached");
        }

        Reservation candidate = Candidate(request, loadouts, profiles);
        try
        {
            _worlds.Join(matchId, candidate.Players, candidate.Loadouts, candidate.Profiles.Count == 0 ? null : candidate.Profiles);
        }
        catch (WorldPreparationException)
        {
            // The raid keeps its other parties; this one is sent to another raid.
            return AllocationResult.Failure(StatusCodes.Status409Conflict, "session_capacity_reached");
        }

        _parties.Add(candidate);
        return AllocationResult.Success(Status(candidate));
    }

    private Reservation Candidate(AllocationRequest request, IReadOnlyList<PlayerLoadout> loadouts, IReadOnlyList<CompactPlayerProfile> profiles)
    {
        return new Reservation(request.TicketId, [.. request.PartyMemberIds], RandomNumberGenerator.GetBytes(RoutingAuthenticator.TokenBytes),
            loadouts, profiles)
        {
            Deadline = _clock() + _timeout,
        };
    }

    private void Maintain()
    {
        if (_matchId is not { } matchId)
        {
            return;
        }

        // A party whose players all left ends; one that expired is taken out of the world first.
        double now = _clock();
        _parties.RemoveAll(item => item.Players.All(player => _worlds.HasLeft(matchId, player)));
        foreach (Reservation item in _parties.Where(item => now >= item.Deadline))
        {
            _worlds.Leave(matchId, item.Players);
        }

        _parties.RemoveAll(item => now >= item.Deadline);

        if (_parties.Count > 0)
        {
            return;
        }

        _worlds.Release(matchId);
        _matchId = null;
        _released = true;
    }

    private Allocation Status(Reservation item)
    {
        string matchId = _matchId!;
        bool accepting = _worlds.AcceptingPlayers(matchId);
        return new Allocation(
            item.TicketId,
            matchId,
            ScenarioNames.TheDam,
            item.Players,
            accepting ? AllocationStates.Available : AllocationStates.Waiting,
            accepting ? ServerInitializationStates.Ready : ServerInitializationStates.Initializing,
            PollingInterval,
            _endpoint with { RoutingToken = Convert.ToBase64String(item.RoutingToken) },
            [.. item.Loadouts.Select(InventorySnapshots.Receipt)]);
    }

    private sealed record Reservation(string TicketId, long[] Players, byte[] RoutingToken, IReadOnlyList<PlayerLoadout> Loadouts, IReadOnlyList<CompactPlayerProfile> Profiles)
    {
        public double Deadline { get; set; }
    }
}
