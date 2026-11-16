using Ares.Common.Control;
using Ares.Common.Control.Server;
using Ares.Host.Services.Matchmaking;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;

namespace Ares.Host.Tests;

public sealed class RaidFleetAdminTests : IAsyncLifetime
{
    private const string OperatorToken = "operator-token-operator-token-operator-token";
    private const string Ticket = "0b7f6c3e-9d0a-4c1b-8e2f-3a4b5c6d7e8f";
    private const string OtherTicket = "1c8f7d4f-0e1b-4d2c-9f30-4b5c6d7e8f90";

    private readonly FakeRaid _raid = new();
    private WebApplication? _app;

    public async ValueTask InitializeAsync()
    {
        WebApplicationBuilder builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        _app = builder.Build();
        _app.MapAllocationApi(_raid, OperatorToken);
        _app.MapAdminApi(_raid, OperatorToken);
        await _app.StartAsync(TestContext.Current.CancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await _app!.DisposeAsync();
    }

    [Fact]
    public async Task Admin_commands_go_to_the_raid_their_id_or_its_start_names()
    {
        using RaidFleet fleet = new(new Starter(_app!), 2, 1, TimeProvider.System);
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

    private sealed class Starter(WebApplication app) : IRaidStarter
    {
        public IRaidServer Start(int slot) => new Server(new SessionAllocatorClient(new Uri("http://localhost"), OperatorToken, app.GetTestServer().CreateHandler()));

        private sealed class Server(SessionAllocatorClient allocator) : IRaidServer
        {
            public SessionAllocatorClient Allocator => allocator;

            public bool HasExited => false;

            public void Dispose() => allocator.Dispose();
        }
    }

    // Every raid this fake hosts has an id starting with the same seven digits.
    private sealed class FakeRaid : IAllocationService, IAdminService
    {
        private readonly Dictionary<string, Allocation> _allocations = [];
        private int _raids;

        public AllocationResult Reserve(AllocationRequest request)
        {
            if (_allocations.TryGetValue(request.TicketId, out Allocation? allocation))
            {
                return AllocationResult.Success(allocation);
            }

            _raids++;
            allocation = new Allocation(request.TicketId, $"0000000{_raids}-0000-4000-8000-{_raids:D12}", request.ScenarioName, request.PartyMemberIds,
                AllocationStates.Available, ServerInitializationStates.Ready, 2,
                new GameServerEndpoint("raid", "127.0.0.1", 7777, "", Convert.ToBase64String(new byte[16]), false, "LOCAL"));
            _allocations[request.TicketId] = allocation;
            return AllocationResult.Success(allocation);
        }

        public AllocationResult Status(AllocationStatusRequest request) =>
            request.TicketId is { } ticket && _allocations.TryGetValue(ticket, out Allocation? allocation)
                ? AllocationResult.Success(allocation)
                : AllocationResult.Failure(StatusCodes.Status404NotFound, "unknown_session_allocation");

        public AllocationResult Settle(AllocationSettleRequest request) => AllocationResult.NoContent();

        public Task<AdminResult> RunAsync(AdminCommand command, CancellationToken cancellationToken) =>
            Task.FromResult(AdminResult.Success([string.Join(' ', [command.Name, .. command.Arguments])]));
    }
}
