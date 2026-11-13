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
    private const string ThirdTicket = "2b7f6c3e-9d0a-4c1b-8e2f-3a4b5c6d7e8f";

    private readonly ManualTime _time = new();
    private readonly Starter _starter = new();

    public async ValueTask InitializeAsync()
    {
        foreach (FakeRaid raid in _starter.Raids)
        {
            WebApplicationBuilder builder = WebApplication.CreateSlimBuilder();
            builder.WebHost.UseTestServer();
            raid.App = builder.Build();
            raid.App.MapAllocationApi(raid, OperatorToken);
            await raid.App.StartAsync(TestContext.Current.CancellationToken);
        }
    }

    public async ValueTask DisposeAsync()
    {
        foreach (FakeRaid raid in _starter.Raids)
        {
            await raid.App!.DisposeAsync();
        }
    }

    [Fact]
    public async Task A_ticket_starts_one_raid_server_and_waits_while_it_starts()
    {
        using RaidFleet fleet = new(_starter, 1, 1, _time);
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
        using RaidFleet fleet = new(_starter, 1, 1, _time);
        _starter.Ready = false;
        Assert.Null(await fleet.ReserveAsync(Request(Ticket), TestContext.Current.CancellationToken));
        _time.Advance(TimeSpan.FromSeconds(31));
        await Assert.ThrowsAsync<AllocationUnavailableException>(() => fleet.ReserveAsync(Request(Ticket), TestContext.Current.CancellationToken));
        Assert.Equal(1, _starter.Stopped);
    }

    [Fact]
    public async Task Capacity_limits_raids_and_an_exited_server_frees_its_slot()
    {
        using RaidFleet fleet = new(_starter, 1, 1, _time);
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
        using RaidFleet fleet = new(_starter, 2, 1, _time);
        Assert.NotNull(await fleet.ReserveAsync(Request(Ticket), TestContext.Current.CancellationToken));
        Assert.NotNull(await fleet.ReserveAsync(Request(OtherTicket), TestContext.Current.CancellationToken));
        Assert.Equal([0, 1], _starter.Started);
    }

    [Fact]
    public async Task Cancelling_a_ticket_stops_its_starting_server()
    {
        using RaidFleet fleet = new(_starter, 1, 1, _time);
        _starter.Ready = false;
        Assert.Null(await fleet.ReserveAsync(Request(Ticket), TestContext.Current.CancellationToken));
        await fleet.CancelAsync(Ticket, TestContext.Current.CancellationToken);
        Assert.Equal(1, _starter.Stopped);
        Assert.Null(await fleet.ReserveAsync(Request(OtherTicket), TestContext.Current.CancellationToken));
        Assert.Equal([0, 0], _starter.Started);
    }

    [Fact]
    public async Task A_ticket_joins_a_running_raid_that_has_room()
    {
        _starter.MaxPlayers = 2;
        using RaidFleet fleet = new(_starter, 2, 2, _time);
        Allocation first = (await fleet.ReserveAsync(Request(Ticket), TestContext.Current.CancellationToken))!;
        Allocation second = (await fleet.ReserveAsync(Request(OtherTicket, 43), TestContext.Current.CancellationToken))!;
        Assert.Equal(first.MatchId, second.MatchId);
        Assert.Equal([0], _starter.Started);

        Allocation third = (await fleet.ReserveAsync(Request(ThirdTicket, 44), TestContext.Current.CancellationToken))!;
        Assert.NotEqual(first.MatchId, third.MatchId);
        Assert.Equal([0, 1], _starter.Started);
    }

    [Fact]
    public async Task A_ticket_waits_for_a_starting_raid_instead_of_starting_another()
    {
        _starter.MaxPlayers = 2;
        _starter.Ready = false;
        using RaidFleet fleet = new(_starter, 2, 2, _time);
        Assert.Null(await fleet.ReserveAsync(Request(Ticket), TestContext.Current.CancellationToken));
        Assert.Null(await fleet.ReserveAsync(Request(OtherTicket, 43), TestContext.Current.CancellationToken));
        Assert.Equal([0], _starter.Started);

        _starter.Ready = true;
        Allocation first = (await fleet.ReserveAsync(Request(Ticket), TestContext.Current.CancellationToken))!;
        Assert.Equal(first.MatchId, (await fleet.ReserveAsync(Request(OtherTicket, 43), TestContext.Current.CancellationToken))!.MatchId);
        Assert.Equal([0], _starter.Started);
    }

    [Fact]
    public async Task One_party_leaving_keeps_the_raid_running_for_the_others()
    {
        _starter.MaxPlayers = 2;
        using RaidFleet fleet = new(_starter, 1, 2, _time);
        Allocation first = (await fleet.ReserveAsync(Request(Ticket), TestContext.Current.CancellationToken))!;
        Allocation second = (await fleet.ReserveAsync(Request(OtherTicket, 43), TestContext.Current.CancellationToken))!;
        _starter.Raids[0].Drop(Ticket);

        await Assert.ThrowsAsync<AllocationExpiredException>(() => fleet.StatusAsync(first, TestContext.Current.CancellationToken));
        Assert.Equal(0, _starter.Stopped);
        Assert.Equal(OtherTicket, (await fleet.StatusAsync(second, TestContext.Current.CancellationToken)).TicketId);

        _starter.Raids[0].Drop(OtherTicket);
        await Assert.ThrowsAsync<AllocationExpiredException>(() => fleet.StatusAsync(second, TestContext.Current.CancellationToken));
        Assert.Equal(1, _starter.Stopped);
    }

    private static AllocationRequest Request(string ticket, long player = 42) => new(ticket, "TheDam", [player]);

    private sealed class ManualTime : TimeProvider
    {
        private long _ticks;

        public override long TimestampFrequency => TimeSpan.TicksPerSecond;

        public override long GetTimestamp() => _ticks;

        public void Advance(TimeSpan delta) => _ticks += delta.Ticks;
    }

    /// <summary>Starts fake Raid servers, one per slot, that answer once <see cref="Ready"/>.</summary>
    private sealed class Starter : IRaidStarter
    {
        public Starter()
        {
            Raids = [new FakeRaid(this), new FakeRaid(this)];
        }

        public FakeRaid[] Raids { get; }

        public bool Ready { get; set; } = true;

        public bool Exited { get; set; }

        public int MaxPlayers { get; set; } = 1;

        public List<int> Started { get; } = [];

        public int Stopped { get; private set; }

        public IRaidServer Start(int slot)
        {
            Started.Add(slot);
            FakeRaid raid = Raids[slot];
            raid.Reset();
            return new Server(this, new SessionAllocatorClient(new Uri("http://localhost"), OperatorToken, raid.App!.GetTestServer().CreateHandler()));
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

    /// <summary>One fake Raid server: one raid that takes parties up to the starter's player limit.</summary>
    private sealed class FakeRaid(Starter owner) : IAllocationService
    {
        private readonly Dictionary<string, Allocation> _byTicket = [];
        private string _matchId = Guid.NewGuid().ToString();

        public WebApplication? App { get; set; }

        public void Reset()
        {
            _byTicket.Clear();
            _matchId = Guid.NewGuid().ToString();
        }

        public void Drop(string ticketId) => _byTicket.Remove(ticketId);

        public AllocationResult Reserve(AllocationRequest request)
        {
            if (!owner.Ready)
            {
                return AllocationResult.Failure(StatusCodes.Status503ServiceUnavailable, "starting");
            }

            if (_byTicket.TryGetValue(request.TicketId, out Allocation? existing))
            {
                return AllocationResult.Success(existing);
            }

            if (_byTicket.Count >= owner.MaxPlayers)
            {
                return AllocationResult.Failure(StatusCodes.Status409Conflict, "session_capacity_reached");
            }

            Allocation allocation = new(request.TicketId, _matchId, request.ScenarioName, request.PartyMemberIds,
                AllocationStates.Available, ServerInitializationStates.Ready, 2,
                new GameServerEndpoint("raid", "127.0.0.1", 7777, "", Convert.ToBase64String(new byte[16]), false, "LOCAL"));
            _byTicket[request.TicketId] = allocation;
            return AllocationResult.Success(allocation);
        }

        public AllocationResult Status(AllocationStatusRequest request)
        {
            return request.TicketId is not null && _byTicket.TryGetValue(request.TicketId, out Allocation? allocation)
                ? AllocationResult.Success(allocation)
                : AllocationResult.Failure(StatusCodes.Status404NotFound, "unknown_session_allocation");
        }

        public AllocationResult Settle(AllocationSettleRequest request)
        {
            return _byTicket.TryGetValue(request.TicketId, out Allocation? allocation) && allocation.MatchId == request.MatchId
                ? AllocationResult.NoContent()
                : AllocationResult.Failure(StatusCodes.Status404NotFound, "unknown_session_allocation");
        }
    }
}
