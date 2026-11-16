using Ares.Common.Control;
using Ares.Common.Control.Server;
using Ares.Common.Game.Inventory;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;

namespace Ares.Host.Tests;

/// <summary>
/// One in-memory Raid server serving the allocation and admin APIs on a test server. It hosts one raid at a
/// time: every ticket joins it until <see cref="MaxPlayers"/> tickets are reserved. Admin commands echo back
/// as their reply line.
/// </summary>
internal sealed class FakeRaidServer : IAllocationService, IAdminService, IAsyncDisposable
{
    private const string OperatorToken = "operator-token-operator-token-operator-token";

    private readonly Lock _gate = new();
    private readonly Dictionary<string, Allocation> _allocations = [];
    private readonly Func<string> _newMatchId;
    private WebApplication? _app;

    /// <param name="newMatchId">Names each new raid; a random GUID by default.</param>
    public FakeRaidServer(Func<string>? newMatchId = null)
    {
        _newMatchId = newMatchId ?? (() => Guid.NewGuid().ToString("D"));
        MatchId = _newMatchId();
    }

    /// <summary>The match id of the current raid.</summary>
    public string MatchId { get; private set; }

    /// <summary>While false, reservations answer 503 as from a server still starting.</summary>
    public bool Ready { get; set; } = true;

    /// <summary>Most tickets the raid takes; further reservations answer 409.</summary>
    public int MaxPlayers { get; set; } = int.MaxValue;

    /// <summary>While true, status polls answer 503.</summary>
    public bool StatusUnavailable { get; set; }

    /// <summary>Results reported on every status poll until cleared.</summary>
    public IReadOnlyList<RaidResult>? Results { get; set; }

    /// <summary>How many settle confirmations to refuse before accepting.</summary>
    public int SettleFailures { get; set; }

    /// <summary>Accepted settle confirmations, in order.</summary>
    public List<AllocationSettleRequest> Settled { get; } = [];

    /// <summary>The last reservation request received.</summary>
    public AllocationRequest? LastRequest { get; private set; }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        WebApplicationBuilder builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        _app = builder.Build();
        _app.MapAllocationApi(this, OperatorToken);
        _app.MapAdminApi(this, OperatorToken);
        await _app.StartAsync(cancellationToken);
    }

    /// <summary>A client of this server's allocation API, as Host gets from a started Raid process.</summary>
    public SessionAllocatorClient CreateAllocator() =>
        new(new Uri("http://localhost"), OperatorToken, _app!.GetTestServer().CreateHandler());

    /// <summary>Ends the current raid: every reservation is gone and the next ticket starts a new raid.</summary>
    public void Forget()
    {
        lock (_gate)
        {
            _allocations.Clear();
            MatchId = _newMatchId();
        }
    }

    /// <summary>Removes one ticket's reservation, as when its party leaves the raid.</summary>
    public void Drop(string ticketId)
    {
        lock (_gate)
        {
            _allocations.Remove(ticketId);
        }
    }

    public AllocationResult Reserve(AllocationRequest request)
    {
        lock (_gate)
        {
            LastRequest = request;
            if (!Ready)
            {
                return AllocationResult.Failure(StatusCodes.Status503ServiceUnavailable, "starting");
            }

            if (_allocations.TryGetValue(request.TicketId, out Allocation? existing))
            {
                return AllocationResult.Success(existing);
            }

            if (_allocations.Count >= MaxPlayers)
            {
                return AllocationResult.Failure(StatusCodes.Status409Conflict, "session_capacity_reached");
            }

            Allocation allocation = new(request.TicketId, MatchId, request.ScenarioName, request.PartyMemberIds,
                AllocationStates.Available, ServerInitializationStates.Ready, 2,
                new GameServerEndpoint("raid", "127.0.0.1", 7777, "", Convert.ToBase64String(new byte[16]), false, "LOCAL"),
                request.Loadouts?.Select(InventorySnapshots.Receipt).ToArray());
            _allocations[request.TicketId] = allocation;
            return AllocationResult.Success(allocation);
        }
    }

    public AllocationResult Status(AllocationStatusRequest request)
    {
        if (StatusUnavailable)
        {
            return AllocationResult.Failure(StatusCodes.Status503ServiceUnavailable, "test_unavailable");
        }

        lock (_gate)
        {
            return request.TicketId is not null && _allocations.TryGetValue(request.TicketId, out Allocation? allocation)
                && (request.MatchId is null || allocation.MatchId == request.MatchId)
                ? AllocationResult.Success(Results is null ? allocation : allocation with { Results = Results })
                : AllocationResult.Failure(StatusCodes.Status404NotFound, "unknown_session_allocation");
        }
    }

    public AllocationResult Settle(AllocationSettleRequest request)
    {
        lock (_gate)
        {
            if (!_allocations.TryGetValue(request.TicketId, out Allocation? allocation) || allocation.MatchId != request.MatchId)
            {
                return AllocationResult.Failure(StatusCodes.Status404NotFound, "unknown_session_allocation");
            }

            if (SettleFailures > 0)
            {
                SettleFailures--;
                return AllocationResult.Failure(StatusCodes.Status503ServiceUnavailable, "test_unavailable");
            }

            Settled.Add(request);
            return AllocationResult.NoContent();
        }
    }

    public Task<AdminResult> RunAsync(AdminCommand command, CancellationToken cancellationToken) =>
        Task.FromResult(AdminResult.Success([string.Join(' ', [command.Name, .. command.Arguments])]));

    public async ValueTask DisposeAsync()
    {
        if (_app is not null)
        {
            await _app.DisposeAsync();
        }
    }
}
