using Ares.Common.Control;
using Ares.Common.Control.Server;
using Ares.Common.Game.Inventory;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;

namespace Ares.Common.Tests;

public sealed class AllocationApiTests : IAsyncLifetime
{
    private const string Token = "operator-token-operator-token-operator-token";
    private static readonly string s_ticket = Guid.NewGuid().ToString("D");
    private readonly FakeRaid _raid = new();
    private WebApplication? _app;

    public async ValueTask InitializeAsync()
    {
        WebApplicationBuilder builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        _app = builder.Build();
        _app.MapAllocationApi(_raid, Token);
        await _app.StartAsync(TestContext.Current.CancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await _app!.DisposeAsync();
    }

    [Fact]
    public async Task Reserve_and_status_round_trip()
    {
        using SessionAllocatorClient client = Client(Token);

        Allocation? reserved = await client.ReserveAsync(new AllocationRequest(s_ticket, "TheDam", [42]), TestContext.Current.CancellationToken);
        Allocation status = await client.StatusAsync(reserved!, TestContext.Current.CancellationToken);

        Assert.Equal(s_ticket, reserved!.TicketId);
        Assert.Equal(AllocationStates.Available, status.State);
        Assert.Equal("127.0.0.1", status.Gameserver.Host);
    }

    [Fact]
    public async Task Capacity_conflicts_and_lost_reservations_are_reported()
    {
        using SessionAllocatorClient client = Client(Token);
        _raid.Full = true;
        Allocation? none = await client.ReserveAsync(new AllocationRequest(s_ticket, "TheDam", [42]), TestContext.Current.CancellationToken);
        _raid.Full = false;
        Allocation reserved = (await client.ReserveAsync(new AllocationRequest(s_ticket, "TheDam", [42]), TestContext.Current.CancellationToken))!;
        _raid.Forget();

        Assert.Null(none);
        await Assert.ThrowsAsync<AllocationExpiredException>(() => client.StatusAsync(reserved, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Wrong_operator_token_is_rejected()
    {
        using SessionAllocatorClient client = Client(new string('y', 40));

        await Assert.ThrowsAsync<AllocationUnavailableException>(() =>
            client.ReserveAsync(new AllocationRequest(s_ticket, "TheDam", [42]), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task A_loadout_conflict_is_not_reported_as_full_capacity()
    {
        using SessionAllocatorClient client = Client(Token);
        _raid.Conflict = true;
        await Assert.ThrowsAsync<AllocationConflictException>(() =>
            client.ReserveAsync(new AllocationRequest(s_ticket, "TheDam", [42]), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task A_peer_must_acknowledge_the_exact_loadout()
    {
        using SessionAllocatorClient client = Client(Token);
        AllocationRequest request = new(s_ticket, "TheDam", [42], [new PlayerLoadout(42, 7, [])]);
        _raid.OmitLoadouts = true;
        await Assert.ThrowsAsync<AllocationUnavailableException>(() => client.ReserveAsync(request, TestContext.Current.CancellationToken));
        _raid.OmitLoadouts = false;
        Allocation reserved = (await client.ReserveAsync(request, TestContext.Current.CancellationToken))!;
        Assert.Equal(InventorySnapshots.Receipt(request.Loadouts![0]), Assert.Single(reserved.Loadouts!));
    }

    [Fact]
    public async Task Inconsistent_replies_are_rejected()
    {
        using SessionAllocatorClient client = Client(Token);
        _raid.Proxy = true;

        await Assert.ThrowsAsync<AllocationUnavailableException>(() =>
            client.ReserveAsync(new AllocationRequest(s_ticket, "TheDam", [42]), TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData("http://192.0.2.1:17778")]
    [InlineData("http://127.0.0.1:17778/path")]
    [InlineData("ftp://127.0.0.1")]
    public void Remote_plain_http_and_odd_urls_are_refused(string url)
    {
        Assert.Throws<ArgumentException>(() => new SessionAllocatorClient(new Uri(url), Token));
    }

    private SessionAllocatorClient Client(string token)
    {
        return new SessionAllocatorClient(new Uri("http://localhost"), token, _app!.GetTestServer().CreateHandler());
    }

    private sealed class FakeRaid : IAllocationService
    {
        private Allocation? _allocation;

        public bool Full { get; set; }

        public bool Proxy { get; set; }

        public bool Conflict { get; set; }

        public bool OmitLoadouts { get; set; }

        public void Forget()
        {
            _allocation = null;
        }

        public AllocationResult Reserve(AllocationRequest request)
        {
            if (Conflict)
            {
                return AllocationResult.Failure(409, "session_allocation_conflict");
            }

            if (Full)
            {
                return AllocationResult.Failure(409, "session_capacity_reached");
            }

            _allocation = new Allocation(request.TicketId, Guid.NewGuid().ToString("D"), request.ScenarioName, request.PartyMemberIds,
                AllocationStates.Available, ServerInitializationStates.Ready, 2.0,
                new GameServerEndpoint("raid", "127.0.0.1", 7777, "", Convert.ToBase64String(new byte[16]), Proxy, "LOCAL"),
                OmitLoadouts ? null : request.Loadouts?.Select(InventorySnapshots.Receipt).ToArray());
            return AllocationResult.Success(_allocation);
        }

        public AllocationResult Status(AllocationStatusRequest request)
        {
            return _allocation is not null && _allocation.MatchId == request.MatchId
                ? AllocationResult.Success(_allocation)
                : AllocationResult.Failure(404, "unknown_session_allocation");
        }
    }
}
