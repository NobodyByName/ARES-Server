using System.Text.Json;
using System.Text.Json.Nodes;
using Ares.Common.Control;
using Ares.Common.Game.Inventory;
using Ares.Common.Logging;
using Ares.Host.Services.Content;
using Ares.Host.Services.Identity;
using Ares.Host.Services.Inventory;
using Ares.Host.Services.Social;
using Ares.Structs.Backend.Matchmaking;
using Ares.Structs.Backend.Profiles;
using Ares.Structs.Backend.Progression;
using Ares.Structs.Net.Replication;
using GameServerStates = Ares.Structs.Backend.Matchmaking.GameServerStates;
using ServerInitializationStates = Ares.Structs.Backend.Matchmaking.ServerInitializationStates;

namespace Ares.Host.Services.Matchmaking;

/// <summary>
/// Normal queue lifecycle: start a ticket, start a Raid server and reserve its raid while the
/// client polls, relay the game server, and cancel. Without configured Raid servers, tickets
/// stay <c>MATCHING</c>.
/// </summary>
public sealed class MatchmakingService(BackendGate gate, PartyService party, ProfileFactory profiles, RaidFleet? raids, JsonEventLog log,
    InventoryStore inventory)
{
    /// <summary>Seconds between client polls.</summary>
    private const double PollingInterval = 2.0;

    private MatchTicket? _ticket;
    private string? _cancelledTicketId;
    private long _revision;
    private Allocation? _assignment;
    private bool _assignmentExpired;

    /// <summary>True when Host starts Raid servers.</summary>
    public bool HasAllocator => raids is not null;

    /// <summary>Reconciles a persisted inventory lock independently of menu or matchmaking traffic.</summary>
    public async Task ReconcileInventoryAsync(CancellationToken cancellationToken)
    {
        using BackendGate.Releaser held = await gate.EnterAsync(cancellationToken).ConfigureAwait(false);
        await RefreshAssignmentLockedAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Players in the current live raid assignment (0 or the party size).</summary>
    public (bool Active, int Players) CurrentRaid
    {
        get
        {
            Allocation? assignment = _assignment;
            bool active = assignment is not null && !_assignmentExpired;
            return (active, active ? assignment!.PartyMemberIds.Count : 0);
        }
    }

    /// <summary><c>POST /v1/shared/match/start</c>.</summary>
    public async Task<ServiceReply> StartAsync(MatchStartRequest request, CancellationToken cancellationToken)
    {
        MatchmakingScenario? scenario = BackendContent.Scenarios.MatchmakingScenarios.FirstOrDefault(row => row.Name == request.MatchmakingScenario);
        if (scenario is null)
        {
            return ServiceReply.Error(400, "unknown_local_scenario");
        }

        if (!TryGetSquadSize(request.PreferredSquadSize, out long squadSize) || squadSize < 1 || squadSize > scenario.Parameters.MaxSquadSize)
        {
            return ServiceReply.Error(400, "invalid_local_squad_size");
        }

        using BackendGate.Releaser held = await gate.EnterAsync(cancellationToken).ConfigureAwait(false);
        if (!party.HasPartyLocked)
        {
            return ServiceReply.Error(409, "no_local_party");
        }

        // A Raid restart loses the reservation: start a fresh ticket instead of
        // handing the client a dead match again.
        try
        {
            await RefreshAssignmentLockedAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (AllocationUnavailableException)
        {
        }

        if (_assignmentExpired)
        {
            _ticket = null;
            _assignment = null;
            _assignmentExpired = false;
        }

        if (_ticket is not null)
        {
            return _ticket.ScenarioName != scenario.Name
                ? ServiceReply.Error(409, "local_matchmaking_already_started")
                : ServiceReply.Ok(_ticket);
        }

        string region = !string.IsNullOrEmpty(request.PreferredRegion) ? request.PreferredRegion
            : !string.IsNullOrEmpty(request.Region) ? request.Region : "europe";
        long user = party.LeaderLocked;
        _ticket = new MatchTicket(inventory.ReservedTicketId ?? Guid.NewGuid().ToString("D"), PollingInterval, region, MatchStates.Matching, 0, [user], [user], scenario.Name);
        _cancelledTicketId = null;
        _assignment = null;
        _revision++;
        log.Write("local_matchmaking", "started");
        return ServiceReply.Ok(_ticket);
    }

    /// <summary>Commits menu edits before the next allocation freezes the selected loadout.</summary>
    public async Task<ServiceReply> CommitInventoryAsync(InventoryOperationsRequest request, DateTimeOffset now, CancellationToken cancellationToken)
    {
        using BackendGate.Releaser held = await gate.EnterAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // Ready saves inventory before starting matchmaking. Reconcile the previous
            // assignment here too, so a confirmed lost raid cannot reject that save.
            await RefreshAssignmentLockedAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (AllocationUnavailableException)
        {
            return ServiceReply.Error(503, "session_allocation_unavailable");
        }

        return inventory.Commit(request, now);
    }

    /// <summary><c>POST /v1/shared/match/status</c>: reserves a raid while the ticket has none.</summary>
    public async Task<ServiceReply> StatusAsync(TicketRequest request, CancellationToken cancellationToken)
    {
        using BackendGate.Releaser held = await gate.EnterAsync(cancellationToken).ConfigureAwait(false);
        return await StatusLockedAsync(request.TicketId, allocate: true, cancellationToken).ConfigureAwait(false);
    }

    /// <summary><c>POST /v1/shared/gameserver/status</c>: relays the reserved raid's state and endpoint.</summary>
    public async Task<ServiceReply> GameServerAsync(GameServerStatusRequest request, CancellationToken cancellationToken)
    {
        using BackendGate.Releaser held = await gate.EnterAsync(cancellationToken).ConfigureAwait(false);
        if (_assignment is null || raids is null || request.MatchId != _assignment.MatchId)
        {
            return ServiceReply.Error(404, "unknown_local_match");
        }

        Allocation allocation;
        try
        {
            allocation = await raids.StatusAsync(_assignment, cancellationToken).ConfigureAwait(false);
        }
        catch (AllocationExpiredException)
        {
            // The client treats EXPIRED as a failed allocation. Never keep returning a stale
            // endpoint after the Raid server lost the reservation.
            inventory.Release(_assignment.TicketId);
            _assignmentExpired = true;
            return ServiceReply.Ok(new GameServerStatusResponse(GameServerStates.Expired, _assignment.MatchId, PollingInterval, ServerInitializationStates.Failure));
        }
        catch (AllocationUnavailableException)
        {
            return ServiceReply.Error(503, "session_allocation_unavailable");
        }

        GameServerEndpoint endpoint = allocation.Gameserver;
        return ServiceReply.Ok(new GameServerStatusResponse(allocation.State, allocation.MatchId, allocation.PollingInterval, allocation.ServerInitializationState,
            new GameServer(endpoint.Name, endpoint.Host, endpoint.Port, endpoint.SecretKey, endpoint.RoutingToken, endpoint.ProxyEnabled, endpoint.DatacenterIcaoCode)));
    }

    /// <summary><c>POST /v1/shared/squad/layout</c>.</summary>
    public async Task<ServiceReply> SquadLayoutAsync(TicketRequest request, CancellationToken cancellationToken)
    {
        using BackendGate.Releaser held = await gate.EnterAsync(cancellationToken).ConfigureAwait(false);
        string? ticketId = request.TicketId == "" && _assignment is not null ? _ticket?.TicketId : request.TicketId;
        ServiceReply status = await StatusLockedAsync(ticketId, allocate: false, cancellationToken).ConfigureAwait(false);
        if (status.StatusCode != 200)
        {
            return status;
        }

        return _assignment is null
            ? ServiceReply.Error(409, "local_match_not_assigned")
            : ServiceReply.Ok(new SquadLayoutResponse([new Squad(_assignment.MatchId, [new SquadMember(profiles.Create())])], 1, 1, 1, 1));
    }

    /// <summary><c>POST /v1/shared/match/v2/cancel</c>.</summary>
    public async Task<ServiceReply> CancelAsync(TicketRequest request, CancellationToken cancellationToken)
    {
        using BackendGate.Releaser held = await gate.EnterAsync(cancellationToken).ConfigureAwait(false);
        if (_cancelledTicketId is not null && request.TicketId == _cancelledTicketId)
        {
            return ServiceReply.Ok(new CancelMatchResponse(_cancelledTicketId, HasAssignment: false));
        }

        ServiceReply status = await StatusLockedAsync(request.TicketId, allocate: false, cancellationToken).ConfigureAwait(false);
        if (status.StatusCode != 200)
        {
            return status;
        }

        MatchTicket ticket = (MatchTicket)status.Body;
        if (ticket.MatchState == MatchStates.Completed && !_assignmentExpired)
        {
            return ServiceReply.Ok(new CancelMatchResponse(ticket.TicketId, HasAssignment: true));
        }

        raids?.Cancel(ticket.TicketId);
        _ticket = null;
        _assignment = null;
        _assignmentExpired = false;
        _cancelledTicketId = ticket.TicketId;
        _revision++;
        party.SetReadyLocked(false);
        log.Write("local_matchmaking", "cancelled");
        return ServiceReply.Ok(new CancelMatchResponse(ticket.TicketId, HasAssignment: false));
    }

    /// <summary><c>POST /v1/shared/player/activity</c>.</summary>
    public async Task<PlayerActivityResponse> ActivityAsync(CancellationToken cancellationToken)
    {
        using BackendGate.Releaser held = await gate.EnterAsync(cancellationToken).ConfigureAwait(false);
        MatchTicket? ticket = _assignmentExpired ? null : _ticket;
        bool assigned = ticket?.MatchState == MatchStates.Completed;
        string state = assigned ? PlayerActivityStates.InMatch : ticket is not null ? PlayerActivityStates.Matchmaking : PlayerActivityStates.Idle;
        return new PlayerActivityResponse(1, state, $"ares-host-activity-{_revision}", ticket?.TicketId ?? "",
            assigned ? _assignment!.MatchId : "", "", "", new ScenarioMetadata(ticket?.ScenarioName ?? "", AllowReconnects: false));
    }

    private async Task RefreshAssignmentLockedAsync(CancellationToken cancellationToken)
    {
        if (_assignmentExpired || raids is null)
        {
            return;
        }

        string? ticket = _assignment?.TicketId ?? inventory.ReservedTicketId;
        if (ticket is null)
        {
            return;
        }

        try
        {
            if (_assignment is not null)
            {
                await raids.StatusAsync(_assignment, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await raids.StatusByTicketAsync(ticket, inventory.Reserve(ticket), cancellationToken).ConfigureAwait(false);
            }
        }
        catch (AllocationExpiredException)
        {
            inventory.Release(ticket);
            _assignmentExpired = _assignment is not null;
            log.Write("inventory_reservation", "released_lost_raid");
        }
    }

    private async Task<ServiceReply> StatusLockedAsync(string? ticketId, bool allocate, CancellationToken cancellationToken)
    {
        if (_ticket is null || ticketId != _ticket.TicketId)
        {
            return ServiceReply.Error(404, "unknown_local_ticket");
        }

        if (!allocate || raids is null || _assignment is not null)
        {
            return ServiceReply.Ok(_ticket);
        }

        Allocation? allocation;
        try
        {
            PlayerLoadout loadout = inventory.Reserve(_ticket.TicketId);
            EmbarkProfile profile = profiles.Create();
            CompactPlayerProfile compact = new(new TenancyUserId((ulong)profile.TenancyUserId), profile.DisplayName.Name, profile.DisplayName.Discriminator,
                profile.ThirdPartyUserId, profile.ThirdPartyLastSeenAccountName);
            allocation = await raids.ReserveAsync(new AllocationRequest(_ticket.TicketId, _ticket.ScenarioName, _ticket.PartyMemberIds, [loadout], [compact]), cancellationToken).ConfigureAwait(false);
        }
        catch (InvalidOperationException)
        {
            return ServiceReply.Error(409, "inventory_reserved");
        }
        catch (AllocationConflictException)
        {
            return ServiceReply.Error(409, "session_allocation_conflict");
        }
        catch (AllocationUnavailableException)
        {
            return ServiceReply.Error(503, "session_allocation_unavailable");
        }

        if (allocation is null)
        {
            inventory.Release(_ticket.TicketId);
            return ServiceReply.Ok(_ticket);
        }

        _assignment = allocation;
        _ticket = _ticket with
        {
            MatchState = MatchStates.Completed,
            RedirectInfo = new RedirectInfo(RedirectTargets.GameServer, allocation.MatchId),
        };
        _revision++;
        log.Write(new JsonObject { ["local_matchmaking"] = "session_allocated", ["session_state"] = allocation.State });

        return ServiceReply.Ok(_ticket);
    }

    private static bool TryGetSquadSize(JsonElement? value, out long size)
    {
        size = 1;
        if (value is null)
        {
            return true;
        }

        JsonElement element = value.Value;
        string raw = element.GetRawText();
        return element.ValueKind == JsonValueKind.Number && raw.IndexOfAny(['.', 'e', 'E']) < 0 && element.TryGetInt64(out size);
    }
}
