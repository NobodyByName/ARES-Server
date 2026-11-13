using System.Net;
using System.Text;
using System.Text.Json;
using Ares.Common.Control;
using Ares.Common.Control.Server;
using Ares.Common.Game.Inventory;
using Ares.Structs.Backend.Progression;
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
    public async Task A_persisted_ticket_can_be_polled_without_creating_a_new_reservation()
    {
        using SessionAllocatorClient client = Client(Token);
        PlayerLoadout loadout = new(42, 7, []);
        Allocation reserved = (await client.ReserveAsync(new AllocationRequest(s_ticket, "TheDam", [42], [loadout]), TestContext.Current.CancellationToken))!;

        Allocation found = await client.StatusByTicketAsync(s_ticket, loadout, TestContext.Current.CancellationToken);
        Assert.Equal(reserved.MatchId, found.MatchId);
        await Assert.ThrowsAsync<AllocationUnavailableException>(() => client.StatusByTicketAsync(s_ticket, loadout with { Revision = 8 }, TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<AllocationUnavailableException>(() => client.StatusByTicketAsync(s_ticket, loadout with { UserId = 43 }, TestContext.Current.CancellationToken));
        _raid.Forget();
        await Assert.ThrowsAsync<AllocationExpiredException>(() => client.StatusByTicketAsync(s_ticket, loadout, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task An_unsupported_ticket_lookup_does_not_report_a_lost_reservation()
    {
        using SessionAllocatorClient client = Client(Token);
        _raid.MatchLookupOnly = true;
        await Assert.ThrowsAsync<AllocationUnavailableException>(() => client.StatusByTicketAsync(s_ticket, new PlayerLoadout(42, 7, []), TestContext.Current.CancellationToken));
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

    private static InventoryItem Item(string id, double durability = 1, double maxDurability = 1, IReadOnlyList<string>? slots = null, long amount = 1)
    {
        return new InventoryItem(id, 5, amount, slots ?? [], durability, maxDurability, "", "", 0);
    }

    private static RaidResult Result(string outcome = RaidOutcomes.Extracted, params CarriedItem[] items)
    {
        return new RaidResult(s_ticket, 42, 7, outcome, items);
    }

    private static Allocation WithResults(params RaidResult[] results)
    {
        return new Allocation(s_ticket, Guid.NewGuid().ToString("D"), "TheDam", [42], AllocationStates.Available,
            ServerInitializationStates.Ready, 2.0, new GameServerEndpoint("raid", "127.0.0.1", 7777, "", "", false, "LOCAL"), null, results);
    }

    [Fact]
    public void Allocation_results_round_trip_and_are_optional()
    {
        Allocation allocation = WithResults(Result(RaidOutcomes.Extracted, new CarriedItem(CarriedPlaces.Backpack, 3, Item("a"))));
        byte[] json = JsonSerializer.SerializeToUtf8Bytes(allocation, AllocationApi.JsonOptions);
        Allocation back = JsonSerializer.Deserialize<Allocation>(json, AllocationApi.JsonOptions)!;
        RaidResult result = Assert.Single(back.Results!);
        CarriedItem carried = Assert.Single(result.Items);
        Assert.Equal((CarriedPlaces.Backpack, 3, "a"), (carried.Place, carried.Slot, carried.Item.InstanceId));
        Assert.Equal(7, result.Revision);
        SessionAllocatorClient.Validate(back);

        string withoutResults = JsonSerializer.Serialize(allocation with { Results = null }, AllocationApi.JsonOptions).Replace(",\"results\":null", "", StringComparison.Ordinal);
        Assert.DoesNotContain("results", withoutResults, StringComparison.Ordinal);
        Assert.Null(JsonSerializer.Deserialize<Allocation>(withoutResults, AllocationApi.JsonOptions)!.Results);
    }

    [Fact]
    public void Results_must_belong_to_the_allocation()
    {
        Assert.Throws<InvalidDataException>(() => SessionAllocatorClient.Validate(WithResults(Result() with { UserId = 43 })));
        Assert.Throws<InvalidDataException>(() => SessionAllocatorClient.Validate(WithResults(Result() with { TicketId = Guid.NewGuid().ToString("D") })));
    }

    private static CarriedItem Carried(string place, int slot, InventoryItem? item = null) => new(place, slot, item ?? Item("a"));

    private static RaidResult[] BuildBadResults()
    {
        return
        [
            Result() with { TicketId = "" },
            Result() with { TicketId = s_ticket.ToUpperInvariant() },
            Result() with { UserId = 0 },
            Result() with { Revision = -1 },
            Result("SURRENDERED"),
            Result(RaidOutcomes.Extracted, Carried("pockets", 0)),
            Result(RaidOutcomes.Extracted, Carried(CarriedPlaces.Backpack, -1)),
            Result(RaidOutcomes.Extracted, Carried(CarriedPlaces.Shield, 1)),
            Result(RaidOutcomes.Extracted, Carried(CarriedPlaces.Backpack, 1), Carried(CarriedPlaces.Backpack, 1, Item("b"))),
            Result(RaidOutcomes.Extracted, Carried(CarriedPlaces.MainWeapon, 0), Carried(CarriedPlaces.MainWeapon, 0, Item("b"))),
            Result(RaidOutcomes.Extracted, Carried(CarriedPlaces.Backpack, 0), Carried(CarriedPlaces.QuickUse, 0)),
            Result(RaidOutcomes.Extracted, Carried(CarriedPlaces.Backpack, 0, Item("a", slots: ["b"]))),
            Result(RaidOutcomes.Extracted, Carried(CarriedPlaces.Backpack, 0, Item("a", amount: 0))),
            Result(RaidOutcomes.Extracted, Carried(CarriedPlaces.Backpack, 0, Item("a", durability: double.NaN))),
            Result(RaidOutcomes.Extracted, Carried(CarriedPlaces.Backpack, 0, Item("a", durability: -1))),
            Result(RaidOutcomes.Extracted, Carried(CarriedPlaces.Backpack, 0, Item("a", durability: 2, maxDurability: 1))),
            Result(RaidOutcomes.Extracted, Carried(CarriedPlaces.Backpack, 0, Item(""))),
            Result(RaidOutcomes.Extracted, [.. Enumerable.Range(0, InventorySnapshots.MaximumItems + 1).Select(index => Carried(CarriedPlaces.Backpack, index, Item("i" + index)))]),
            Result(RaidOutcomes.Lost, Carried(CarriedPlaces.Backpack, 0)),
        ];
    }

    public static TheoryData<int> BadResults => [.. Enumerable.Range(0, BuildBadResults().Length)];

    [Theory]
    [MemberData(nameof(BadResults))]
    public void Malformed_results_are_rejected(int index)
    {
        RaidResult result = BuildBadResults()[index];
        Assert.Throws<InvalidDataException>(() => RaidResults.Validate(result));
    }

    [Fact]
    public void Well_formed_results_are_accepted()
    {
        RaidResults.Validate(Result(RaidOutcomes.Extracted, new CarriedItem(CarriedPlaces.Augment, 0, Item("a")), new CarriedItem(CarriedPlaces.SafePockets, 1, Item("b"))));
        RaidResults.Validate(Result(RaidOutcomes.Lost, new CarriedItem(CarriedPlaces.SafePockets, 0, Item("a"))));
    }

    [Theory]
    [InlineData(HttpStatusCode.NoContent, true)]
    [InlineData(HttpStatusCode.OK, true)]
    [InlineData(HttpStatusCode.NotFound, true)]
    [InlineData(HttpStatusCode.InternalServerError, false)]
    public async Task Settle_posts_to_the_settle_path(HttpStatusCode status, bool succeeds)
    {
        StubHandler handler = new(status);
        using SessionAllocatorClient client = new(new Uri("http://localhost"), Token, handler);
        Task call = client.SettleAsync(new AllocationSettleRequest(s_ticket, Guid.NewGuid().ToString("D"), [42]), TestContext.Current.CancellationToken);
        if (succeeds)
        {
            await call;
        }
        else
        {
            await Assert.ThrowsAsync<AllocationUnavailableException>(() => call);
        }

        Assert.Equal(AllocationApi.SettlePath, handler.Path);
    }

    [Fact]
    public async Task Settle_replies_without_a_body_and_404_for_an_unknown_reservation()
    {
        using SessionAllocatorClient client = Client(Token);
        Allocation reserved = (await client.ReserveAsync(new AllocationRequest(s_ticket, "TheDam", [42]), TestContext.Current.CancellationToken))!;
        using HttpClient http = new(_app!.GetTestServer().CreateHandler());
        http.BaseAddress = new Uri("http://localhost");
        http.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", "Bearer " + Token);

        using HttpResponseMessage settled = await Settle(http, new AllocationSettleRequest(s_ticket, reserved.MatchId, [42]));
        using HttpResponseMessage unknown = await Settle(http, new AllocationSettleRequest(s_ticket, Guid.NewGuid().ToString("D"), [42]));
        http.DefaultRequestHeaders.Authorization = null;
        using HttpResponseMessage anonymous = await Settle(http, new AllocationSettleRequest(s_ticket, reserved.MatchId, [42]));

        Assert.Equal(HttpStatusCode.NoContent, settled.StatusCode);
        Assert.Empty(await settled.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken));
        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
        await client.SettleAsync(new AllocationSettleRequest(s_ticket, reserved.MatchId, [42]), TestContext.Current.CancellationToken);
    }

    private static async Task<HttpResponseMessage> Settle(HttpClient http, AllocationSettleRequest request)
    {
        using StringContent content = new(JsonSerializer.Serialize(request, AllocationApi.JsonOptions), Encoding.UTF8, "application/json");
        return await http.PostAsync(AllocationApi.SettlePath, content, TestContext.Current.CancellationToken);
    }

    private sealed class StubHandler(HttpStatusCode status) : HttpMessageHandler
    {
        public string? Path { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Path = request.RequestUri!.AbsolutePath;
            return Task.FromResult(new HttpResponseMessage(status));
        }
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

        public bool MatchLookupOnly { get; set; }

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
            if (MatchLookupOnly && request.MatchId is null)
            {
                return AllocationResult.Failure(400, "invalid_allocation_request");
            }

            return _allocation is not null && (request.MatchId is null ? _allocation.TicketId == request.TicketId : _allocation.MatchId == request.MatchId)
                ? AllocationResult.Success(_allocation)
                : AllocationResult.Failure(404, "unknown_session_allocation");
        }

        public AllocationResult Settle(AllocationSettleRequest request)
        {
            return _allocation is not null && _allocation.TicketId == request.TicketId && _allocation.MatchId == request.MatchId
                ? AllocationResult.NoContent()
                : AllocationResult.Failure(404, "unknown_session_allocation");
        }
    }
}
