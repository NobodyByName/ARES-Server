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
    public async Task Admin_commands_reach_every_running_raid()
    {
        using RaidFleet fleet = new(new Starter(_app!), 1, 1, TimeProvider.System);
        Assert.Empty(await fleet.AdminAsync(new AdminCommand("players", []), TestContext.Current.CancellationToken));

        Assert.NotNull(await fleet.ReserveAsync(new AllocationRequest(Ticket, "TheDam", [42]), TestContext.Current.CancellationToken));
        (int slot, int status, AdminReply reply) = Assert.Single(await fleet.AdminAsync(new AdminCommand("teleport", ["42", "1", "2", "3"]), TestContext.Current.CancellationToken));

        Assert.Equal((0, 200), (slot, status));
        Assert.Equal("teleport 42 1 2 3", Assert.Single(reply.Lines));
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

    private sealed class FakeRaid : IAllocationService, IAdminService
    {
        private Allocation? _allocation;

        public AllocationResult Reserve(AllocationRequest request)
        {
            _allocation = new Allocation(request.TicketId, Guid.NewGuid().ToString(), request.ScenarioName, request.PartyMemberIds,
                AllocationStates.Available, ServerInitializationStates.Ready, 2,
                new GameServerEndpoint("raid", "127.0.0.1", 7777, "", Convert.ToBase64String(new byte[16]), false, "LOCAL"));
            return AllocationResult.Success(_allocation);
        }

        public AllocationResult Status(AllocationStatusRequest request) =>
            _allocation is null ? AllocationResult.Failure(StatusCodes.Status404NotFound, "unknown_session_allocation") : AllocationResult.Success(_allocation);

        public AllocationResult Settle(AllocationSettleRequest request) => AllocationResult.NoContent();

        public Task<AdminResult> RunAsync(AdminCommand command, CancellationToken cancellationToken) =>
            Task.FromResult(AdminResult.Success([string.Join(' ', [command.Name, .. command.Arguments])]));
    }
}
