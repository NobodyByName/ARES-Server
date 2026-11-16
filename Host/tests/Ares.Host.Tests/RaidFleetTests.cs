using Ares.Common.Control;
using Ares.Host.Services.Matchmaking;

namespace Ares.Host.Tests;

public sealed class RaidFleetTests : IAsyncLifetime
{
    private const string Ticket = "0b7f6c3e-9d0a-4c1b-8e2f-3a4b5c6d7e8f";
    private const string OtherTicket = "1b7f6c3e-9d0a-4c1b-8e2f-3a4b5c6d7e8f";
    private const string ThirdTicket = "2b7f6c3e-9d0a-4c1b-8e2f-3a4b5c6d7e8f";

    private readonly ManualTime _time = new();
    private readonly FakeRaidStarter _starter = new(new FakeRaidServer { MaxPlayers = 1 }, new FakeRaidServer { MaxPlayers = 1 });

    public async ValueTask InitializeAsync()
    {
        await _starter.StartAsync(TestContext.Current.CancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await _starter.DisposeAsync();
    }

    [Fact]
    public async Task A_ticket_starts_one_raid_server_and_waits_while_it_starts()
    {
        using RaidFleet fleet = new(_starter, 1, 1, _time);
        _starter.SetReady(false);
        Assert.Null(await fleet.ReserveAsync(Request(Ticket), TestContext.Current.CancellationToken));
        Assert.Null(await fleet.ReserveAsync(Request(Ticket), TestContext.Current.CancellationToken));
        Assert.Equal([0], _starter.Started);

        _starter.SetReady(true);
        Allocation allocation = (await fleet.ReserveAsync(Request(Ticket), TestContext.Current.CancellationToken))!;
        Assert.Equal(Ticket, (await fleet.StatusAsync(allocation, TestContext.Current.CancellationToken)).TicketId);
        Assert.Equal([0], _starter.Started);
    }

    [Fact]
    public async Task A_server_that_never_answers_is_stopped()
    {
        using RaidFleet fleet = new(_starter, 1, 1, _time);
        _starter.SetReady(false);
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
        _starter.SetReady(false);
        Assert.Null(await fleet.ReserveAsync(Request(Ticket), TestContext.Current.CancellationToken));
        await fleet.CancelAsync(Ticket, TestContext.Current.CancellationToken);
        Assert.Equal(1, _starter.Stopped);
        Assert.Null(await fleet.ReserveAsync(Request(OtherTicket), TestContext.Current.CancellationToken));
        Assert.Equal([0, 0], _starter.Started);
    }

    [Fact]
    public async Task A_ticket_joins_a_running_raid_that_has_room()
    {
        _starter.SetMaxPlayers(2);
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
        _starter.SetMaxPlayers(2);
        _starter.SetReady(false);
        using RaidFleet fleet = new(_starter, 2, 2, _time);
        Assert.Null(await fleet.ReserveAsync(Request(Ticket), TestContext.Current.CancellationToken));
        Assert.Null(await fleet.ReserveAsync(Request(OtherTicket, 43), TestContext.Current.CancellationToken));
        Assert.Equal([0], _starter.Started);

        _starter.SetReady(true);
        Allocation first = (await fleet.ReserveAsync(Request(Ticket), TestContext.Current.CancellationToken))!;
        Assert.Equal(first.MatchId, (await fleet.ReserveAsync(Request(OtherTicket, 43), TestContext.Current.CancellationToken))!.MatchId);
        Assert.Equal([0], _starter.Started);
    }

    [Fact]
    public async Task One_party_leaving_keeps_the_raid_running_for_the_others()
    {
        _starter.SetMaxPlayers(2);
        using RaidFleet fleet = new(_starter, 1, 2, _time);
        Allocation first = (await fleet.ReserveAsync(Request(Ticket), TestContext.Current.CancellationToken))!;
        Allocation second = (await fleet.ReserveAsync(Request(OtherTicket, 43), TestContext.Current.CancellationToken))!;
        _starter.Servers[0].Drop(Ticket);

        await Assert.ThrowsAsync<AllocationExpiredException>(() => fleet.StatusAsync(first, TestContext.Current.CancellationToken));
        Assert.Equal(0, _starter.Stopped);
        Assert.Equal(OtherTicket, (await fleet.StatusAsync(second, TestContext.Current.CancellationToken)).TicketId);

        _starter.Servers[0].Drop(OtherTicket);
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
}
