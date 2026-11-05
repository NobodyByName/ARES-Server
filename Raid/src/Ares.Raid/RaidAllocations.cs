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
/// The raid's single reservation: reserved by the Host for one player, expiring unless the player keeps
/// it alive, and admitting that player through its routing credential.
/// </summary>
public sealed class RaidAllocations : IAllocationService, IRouteResolver
{
    /// <summary>Seconds a reservation lives without activity.</summary>
    private const double DefaultInitializationTimeout = 120;

    private const double PollingInterval = 2.0;

    private readonly Lock _lock = new();
    private readonly GameServerEndpoint _endpoint;
    private readonly IWorldLifecycle _worlds;
    private readonly Func<double> _clock;
    private readonly double _timeout;
    private Reservation? _current;

    /// <summary>Creates the service.</summary>
    /// <param name="endpoint">Where players connect (its routing token is filled in per reservation).</param>
    /// <param name="worlds">Prepares the raid world.</param>
    /// <param name="clock">Monotonic clock in seconds.</param>
    /// <param name="initializationTimeout">Seconds a reservation lives without activity (up to 600).</param>
    public RaidAllocations(GameServerEndpoint endpoint, IWorldLifecycle worlds, Func<double>? clock = null, double initializationTimeout = DefaultInitializationTimeout)
    {
        _endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
        _worlds = worlds ?? throw new ArgumentNullException(nameof(worlds));
        if (initializationTimeout is <= 0 or > 600)
        {
            throw new ArgumentOutOfRangeException(nameof(initializationTimeout));
        }

        long started = Stopwatch.GetTimestamp();
        _clock = clock ?? (() => Stopwatch.GetElapsedTime(started).TotalSeconds);
        _timeout = initializationTimeout;
    }

    /// <inheritdoc />
    public AllocationResult Reserve(AllocationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!SessionAllocatorClient.IsCanonicalUuid(request.TicketId) || request.ScenarioName != ScenarioNames.TheDam
            || request.PartyMemberIds is not [> 0])
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
            if (_current is { } existing)
            {
                return existing.TicketId != request.TicketId
                    ? AllocationResult.Failure(StatusCodes.Status409Conflict, "session_capacity_reached")
                    : !existing.Players.SequenceEqual(request.PartyMemberIds)
                        || !existing.Profiles.SequenceEqual(profiles)
                        || !existing.Loadouts.Zip(loadouts).All(pair => InventorySnapshots.Same(pair.First, pair.Second))
                        ? AllocationResult.Failure(StatusCodes.Status409Conflict, "session_allocation_conflict")
                        : AllocationResult.Success(Status(existing));
            }

            Reservation candidate = new(request.TicketId, Guid.NewGuid().ToString(), [.. request.PartyMemberIds],
                RandomNumberGenerator.GetBytes(RoutingAuthenticator.TokenBytes), loadouts, profiles)
            {
                Deadline = _clock() + _timeout,
            };
            try
            {
                _worlds.Prepare(candidate.MatchId, candidate.Players, candidate.Loadouts, candidate.Profiles.Count == 0 ? null : candidate.Profiles);
            }
            catch (WorldPreparationException)
            {
                _worlds.Release(candidate.MatchId);
                return AllocationResult.Failure(StatusCodes.Status503ServiceUnavailable, "world_initialization_failed");
            }

            _current = candidate;
            return AllocationResult.Success(Status(candidate));
        }
    }

    /// <inheritdoc />
    public AllocationResult Status(AllocationStatusRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        bool byTicket = request.MatchId is null;
        if (byTicket ? !SessionAllocatorClient.IsCanonicalUuid(request.TicketId)
            : request.TicketId is not null || !SessionAllocatorClient.IsCanonicalUuid(request.MatchId))
        {
            return AllocationResult.Failure(StatusCodes.Status400BadRequest, "invalid_session_lookup");
        }

        lock (_lock)
        {
            Maintain();
            return _current is { } item && (byTicket ? item.TicketId == request.TicketId : item.MatchId == request.MatchId)
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
            if (_current is not { } item || !_worlds.AcceptingPlayers(item.MatchId) || !CryptographicOperations.FixedTimeEquals(token, item.RoutingToken))
            {
                return null;
            }

            if (renew)
            {
                item.Deadline = _clock() + _timeout;
            }

            return new SessionPrincipal(item.Players[0], item.MatchId);
        }
    }

    private void Maintain()
    {
        if (_current is not { } item || (!_worlds.HasEnded(item.MatchId) && _clock() < item.Deadline))
        {
            return;
        }

        _worlds.Release(item.MatchId);
        _current = null;
    }

    private Allocation Status(Reservation item)
    {
        bool accepting = _worlds.AcceptingPlayers(item.MatchId);
        return new Allocation(
            item.TicketId,
            item.MatchId,
            ScenarioNames.TheDam,
            item.Players,
            accepting ? AllocationStates.Available : AllocationStates.Waiting,
            accepting ? ServerInitializationStates.Ready : ServerInitializationStates.Initializing,
            PollingInterval,
            _endpoint with { RoutingToken = Convert.ToBase64String(item.RoutingToken) },
            [.. item.Loadouts.Select(InventorySnapshots.Receipt)]);
    }

    private sealed record Reservation(string TicketId, string MatchId, long[] Players, byte[] RoutingToken, IReadOnlyList<PlayerLoadout> Loadouts, IReadOnlyList<CompactPlayerProfile> Profiles)
    {
        public double Deadline { get; set; }
    }
}
