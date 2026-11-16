using Ares.Common.Control;
using Ares.Host.Services.Matchmaking;

namespace Ares.Host.Tests;

public sealed class RaidFleetAdminTests : IAsyncLifetime
{
    private const string Ticket = "0b7f6c3e-9d0a-4c1b-8e2f-3a4b5c6d7e8f";
    private const string OtherTicket = "1c8f7d4f-0e1b-4d2c-9f30-4b5c6d7e8f90";

    private readonly FakeRaidStarter _starter = new(SevenDigitRaid(1), SevenDigitRaid(2));

    public async ValueTask InitializeAsync()
    {
        await _starter.StartAsync(TestContext.Current.CancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await _starter.DisposeAsync();
    }

    [Fact]
    public async Task Admin_commands_go_to_the_raid_their_id_or_its_start_names()
    {
        using RaidFleet fleet = new(_starter, 2, 1, TimeProvider.System);
        Assert.Empty(fleet.Running());
        (string? none, int missing, AdminReply notFound) = await fleet.AdminAsync("0", new AdminCommand("players", []), TestContext.Current.CancellationToken);
        Assert.Equal((null, 404, AdminApi.RaidNotFound), (none, missing, notFound.Error));

        Assert.NotNull(await fleet.ReserveAsync(new AllocationRequest(Ticket, "TheDam", [42]), TestContext.Current.CancellationToken));
        Assert.NotNull(await fleet.ReserveAsync(new AllocationRequest(OtherTicket, "TheDam", [43]), TestContext.Current.CancellationToken));
        IReadOnlyList<RunningRaid> running = fleet.Running();
        Assert.Equal([0, 1], running.Select(raid => raid.Slot));
        Assert.Equal([[42L], [43L]], running.Select(raid => raid.Players.ToArray()));
        string second = running[1].MatchId!;

        (string? matchId, int status, AdminReply reply) = await fleet.AdminAsync(second.ToUpperInvariant(), new AdminCommand("teleport", ["43", "1", "2", "3"]),
            TestContext.Current.CancellationToken);
        (string? _, int ambiguous, AdminReply both) = await fleet.AdminAsync(second[..7], new AdminCommand("players", []), TestContext.Current.CancellationToken);
        (string? prefixed, int found, AdminReply _) = await fleet.AdminAsync(second[..8], new AdminCommand("players", []), TestContext.Current.CancellationToken);

        Assert.Equal((second, 200), (matchId, status));
        Assert.Equal("teleport 43 1 2 3", Assert.Single(reply.Lines));
        Assert.Equal((409, AdminApi.RaidAmbiguous), (ambiguous, both.Error));
        Assert.Equal((second, 200), (prefixed, found));
    }

    // One player per raid; every raid on these servers has an id starting with the same seven digits.
    private static FakeRaidServer SevenDigitRaid(int number) => new(() => $"0000000{number}-0000-4000-8000-{number:D12}") { MaxPlayers = 1 };
}
