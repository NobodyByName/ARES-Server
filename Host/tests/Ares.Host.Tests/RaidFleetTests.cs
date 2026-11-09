using Ares.Common.Control;
using Ares.Common.Control.Server;
using Ares.Host.Services.Matchmaking;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;

namespace Ares.Host.Tests;

public sealed class RaidFleetTests : IAsyncLifetime
{
    private const string OperatorToken = "operator-token-operator-token-operator-token";
    private const string Ticket = "0b7f6c3e-9d0a-4c1b-8e2f-3a4b5c6d7e8f";
    private const string OtherTicket = "1b7f6c3e-9d0a-4c1b-8e2f-3a4b5c6d7e8f";

    private readonly ManualTime _time = new();
    private readonly Starter _starter = new();

    public async ValueTask InitializeAsync()
    {
        WebApplicationBuilder builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        _starter.App = builder.Build();
        _starter.App.MapAllocationApi(_starter, OperatorToken);
        await _starter.App.StartAsync(TestContext.Current.CancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await _starter.App!.DisposeAsync();
    }

    [Fact]
    public async Task A_ticket_starts_one_raid_server_and_waits_while_it_starts()
    {
        using RaidFleet fleet = new(_starter, 1, _time);
        _starter.Ready = false;
        Assert.Null(await fleet.ReserveAsync(Request(Ticket), TestContext.Current.CancellationToken));
        Assert.Null(await fleet.ReserveAsync(Request(Ticket), TestContext.Current.CancellationToken));
        Assert.Equal([0], _starter.Started);

        _starter.Ready = true;
        Allocation allocation = (await fleet.ReserveAsync(Request(Ticket), TestContext.Current.CancellationToken))!;
        Assert.Equal(Ticket, (await fleet.StatusAsync(allocation, TestContext.Current.CancellationToken)).TicketId);
        Assert.Equal([0], _starter.Started);
    }

    [Fact]
    public async Task A_server_that_never_answers_is_stopped()
    {
        using RaidFleet fleet = new(_starter, 1, _time);
        _starter.Ready = false;
        Assert.Null(await fleet.ReserveAsync(Request(Ticket), TestContext.Current.CancellationToken));
        _time.Advance(TimeSpan.FromSeconds(31));
        await Assert.ThrowsAsync<AllocationUnavailableException>(() => fleet.ReserveAsync(Request(Ticket), TestContext.Current.CancellationToken));
        Assert.Equal(1, _starter.Stopped);
    }

    [Fact]
    public async Task Capacity_limits_raids_and_an_exited_server_frees_its_slot()
    {
        using RaidFleet fleet = new(_starter, 1, _time);
        Allocation allocation = (await fleet.ReserveAsync(Request(Ticket), TestContext.Current.CancellationToken))!;
        Assert.Null(await fleet.ReserveAsync(Request(OtherTicket), TestContext.Current.CancellationToken));

        _starter.Exited = true;
        await Assert.ThrowsAsync<AllocationExpiredException>(() => fleet.StatusAsync(allocation, TestContext.Current.CancellationToken));
        _starter.Exited = false;
        Assert.NotNull(await fleet.ReserveAsync(Request(OtherTicket), TestContext.Current.CancellationToken));
        Assert.Equal([0, 0], _starter.Started);
    }

    [Fact]
    public async Task Each_raid_gets_its_own_slot()
    {
        using RaidFleet fleet = new(_starter, 2, _time);
        Assert.NotNull(await fleet.ReserveAsync(Request(Ticket), TestContext.Current.CancellationToken));
        Assert.NotNull(await fleet.ReserveAsync(Request(OtherTicket), TestContext.Current.CancellationToken));
        Assert.Equal([0, 1], _starter.Started);
    }

    [Fact]
    public async Task Cancelling_a_ticket_stops_its_starting_server()
    {
        using RaidFleet fleet = new(_starter, 1, _time);
        _starter.Ready = false;
        Assert.Null(await fleet.ReserveAsync(Request(Ticket), TestContext.Current.CancellationToken));
        fleet.Cancel(Ticket);
        Assert.Equal(1, _starter.Stopped);
        Assert.Null(await fleet.ReserveAsync(Request(OtherTicket), TestContext.Current.CancellationToken));
        Assert.Equal([0, 0], _starter.Started);
    }

    private static AllocationRequest Request(string ticket) => new(ticket, "TheDam", [42]);

    private sealed class ManualTime : TimeProvider
    {
        private long _ticks;

        public override long TimestampFrequency => TimeSpan.TicksPerSecond;

        public override long GetTimestamp() => _ticks;

        public void Advance(TimeSpan delta) => _ticks += delta.Ticks;
    }

    /// <summary>Fake Raid servers sharing one in-memory allocation API that answers once <see cref="Ready"/>.</summary>
    private sealed class Starter : IRaidStarter, IAllocationService
    {
        private readonly Dictionary<string, Allocation> _byTicket = [];

        public WebApplication? App { get; set; }

        public bool Ready { get; set; } = true;

        public bool Exited { get; set; }

        public List<int> Started { get; } = [];

        public int Stopped { get; private set; }

        public IRaidServer Start(int slot)
        {
            Started.Add(slot);
            return new Server(this, new SessionAllocatorClient(new Uri("http://localhost"), OperatorToken, App!.GetTestServer().CreateHandler()));
        }

        public AllocationResult Reserve(AllocationRequest request)
        {
            if (!Ready)
            {
                return AllocationResult.Failure(StatusCodes.Status503ServiceUnavailable, "starting");
            }

            if (_byTicket.TryGetValue(request.TicketId, out Allocation? existing))
            {
                return AllocationResult.Success(existing);
            }

            Allocation allocation = new(request.TicketId, Guid.NewGuid().ToString(), request.ScenarioName, request.PartyMemberIds,
                AllocationStates.Available, ServerInitializationStates.Ready, 2,
                new GameServerEndpoint("raid", "127.0.0.1", 7777, "", Convert.ToBase64String(new byte[16]), false, "LOCAL"));
            _byTicket[request.TicketId] = allocation;
            return AllocationResult.Success(allocation);
        }

        public AllocationResult Status(AllocationStatusRequest request)
        {
            Allocation? allocation = _byTicket.Values.FirstOrDefault(item => item.MatchId == request.MatchId || item.TicketId == request.TicketId);
            return allocation is null
                ? AllocationResult.Failure(StatusCodes.Status404NotFound, "unknown_session_allocation")
                : AllocationResult.Success(allocation);
        }

        private sealed class Server(Starter owner, SessionAllocatorClient allocator) : IRaidServer
        {
            public SessionAllocatorClient Allocator => allocator;

            public bool HasExited => owner.Exited;

            public void Dispose()
            {
                owner.Stopped++;
                allocator.Dispose();
            }
        }
    }
}
