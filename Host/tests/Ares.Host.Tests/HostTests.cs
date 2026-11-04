using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Ares.Common.Control;
using Ares.Common.Control.Server;
using Ares.Common.Game.Inventory;
using Ares.Common.Logging;
using Ares.Host.Services;
using Ares.Host.Services.Inventory;
using Ares.Host.Services.Status;
using Ares.Structs.Backend.Progression;
using Ares.Structs.Core;
using Ares.Structs.Net.Replication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Ares.Host.Tests;

public sealed class HostTests : IAsyncLifetime
{
    private const string OperatorToken = "operator-token-operator-token-operator-token";
    private readonly TestDatabase _database = new();
    private readonly FakeRaid _raid = new();
    private WebApplication? _raidApp;
    private WebApplication? _host;
    private HttpClient? _client;

    public async ValueTask InitializeAsync()
    {
        WebApplicationBuilder raidBuilder = WebApplication.CreateSlimBuilder();
        raidBuilder.WebHost.UseTestServer();
        _raidApp = raidBuilder.Build();
        _raidApp.MapAllocationApi(_raid, OperatorToken);
        await _raidApp.StartAsync(TestContext.Current.CancellationToken);

        HostConfiguration configuration = new()
        {
            Settings = new HostSettings
            {
                PublicOrigin = "http://127.0.0.1:18080",
                IdentityKey = [.. "identity-key-identity-key-identity-key"u8],
                ArchiveKeys = ArchiveKeys.Parse(ArchiveKeysTests.DummyFile),
                DataDirectory = _database.Directory,
                MongoDbUri = TestDatabase.ConnectionUri,
                MongoDbDatabase = _database.Name,
                RouteDelayScale = 0,
            },
            Bind = IPAddress.Loopback,
            Port = 18080,
            SessionControlUrl = new Uri("http://localhost"),
            OperatorToken = OperatorToken,
        };
        _host = HostApplication.Build(configuration, new JsonEventLog(TextWriter.Null), allocatorHandler: _raidApp.GetTestServer().CreateHandler(),
            configureBuilder: builder => builder.WebHost.UseTestServer());
        await _host.StartAsync(TestContext.Current.CancellationToken);
        _client = _host.GetTestClient();
    }

    public async ValueTask DisposeAsync()
    {
        _client!.Dispose();
        await _host!.DisposeAsync();
        await _raidApp!.DisposeAsync();
        _database.Dispose();
    }

    [Fact]
    public async Task Archive_keys_are_served_without_sign_in()
    {
        JsonElement keys = await GetJson(ArchiveKeys.Path);

        Assert.Equal(GameBuild.SteamBuildId.ToString(CultureInfo.InvariantCulture), keys.GetProperty("game_build").GetString());
        Assert.Equal("000102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1E1F", keys.GetProperty("pak_key").GetString());
        Assert.Equal(8, keys.GetProperty("container_key").GetArrayLength());
    }

    [Fact]
    public async Task Status_health_and_unknown_routes()
    {
        JsonElement status = await GetJson("/ares/v1/status");
        HttpResponseMessage unknown = await _client!.GetAsync("/nope", TestContext.Current.CancellationToken);

        Assert.Equal(1, status.GetProperty("protocol").GetInt32());
        Assert.True(status.GetProperty("matchmaking").GetBoolean());
        Assert.Equal(0, status.GetProperty("raids").GetProperty("active").GetInt32());
        Assert.Equal("ARES-Host", (await GetJson("/health")).GetProperty("service").GetString());
        Assert.Equal(HttpStatusCode.NotImplemented, unknown.StatusCode);
        Assert.Equal("ARES-Host", unknown.Headers.Server.ToString());
    }

    [Fact]
    public async Task Matchmaking_needs_the_local_players_identity()
    {
        HttpResponseMessage anonymous = await Post("/v1/shared/player/activity", new { });
        HttpResponseMessage forged = await Post("/v1/shared/player/activity", new { }, "Bearer a.b.c");

        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, forged.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Post("/v1/shared/player/activity", new { }, await SignIn())).StatusCode);
    }

    [Fact]
    public async Task Normal_flow_reaches_an_assigned_raid()
    {
        string bearer = await SignIn();
        await Post("/v1/shared/social/party/v2/create", new { crossplay_enabled = true, platform_session_id = "s", language = "en" });
        await Post("/v1/shared/social/party/set-ready-for-matchmaking", new { is_ready = true });

        JsonElement ticket = await Json(await Post("/v1/shared/match/start", new { matchmakingScenario = "TheDam", preferredSquadSize = 1 }, bearer));
        string ticketId = ticket.GetProperty("ticketId").GetString()!;
        JsonElement status = await Json(await Post("/v1/shared/match/status", new { ticketId }, bearer));
        string matchId = status.GetProperty("redirectInfo").GetProperty("matchId").GetString()!;
        JsonElement gameServer = await Json(await Post("/v1/shared/gameserver/status", new { matchId }, bearer));
        JsonElement activity = await Json(await Post("/v1/shared/player/activity", new { }, bearer));
        JsonElement publicStatus = await GetJson("/ares/v1/status");

        Assert.Equal("MATCHING", ticket.GetProperty("matchState").GetString());
        Assert.Equal("COMPLETED", status.GetProperty("matchState").GetString());
        Assert.Equal("AVAILABLE", gameServer.GetProperty("state").GetString());
        Assert.Equal(7777, gameServer.GetProperty("gameserver").GetProperty("port").GetInt32());
        Assert.Equal("IN_MATCH", activity.GetProperty("currentState").GetString());
        Assert.Equal(1, publicStatus.GetProperty("raids").GetProperty("active").GetInt32());
        Assert.Equal([1000000000000000000L], _raid.LastRequest!.PartyMemberIds);
    }

    [Fact]
    public async Task Lost_reservations_report_expired()
    {
        string bearer = await SignIn();
        await Post("/v1/shared/social/party/v2/create", new { });
        string ticketId = (await Json(await Post("/v1/shared/match/start", new { matchmakingScenario = "TheDam" }, bearer))).GetProperty("ticketId").GetString()!;
        string matchId = (await Json(await Post("/v1/shared/match/status", new { ticketId }, bearer))).GetProperty("redirectInfo").GetProperty("matchId").GetString()!;
        _raid.Forget();

        JsonElement gameServer = await Json(await Post("/v1/shared/gameserver/status", new { matchId }, bearer));

        Assert.Equal("EXPIRED", gameServer.GetProperty("state").GetString());
        Assert.False(gameServer.TryGetProperty("gameserver", out _));
    }

    [Fact]
    public async Task Persisted_inventory_is_authenticated_and_the_selected_tree_crosses_the_allocation_api()
    {
        InventoryStore inventory = _host!.Services.GetRequiredService<InventoryStore>();
        long revision = inventory.Snapshot.Revision;
        PlayerInventory state = inventory.Inventory;
        OwnedItem duck = Assert.Single(state.Stash.Items);
        inventory.Import(state with
        {
            Stash = state.Stash with { Items = [duck with { InstanceId = "stash", Quantity = 3 }] },
            Loadout = state.Loadout with { Backpack = new PlayerItemContainer([duck with { InstanceId = "carried", Quantity = 2, Slot = 0 }]) },
        }, revision);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _client!.GetAsync("/v1/pioneer/inventory", TestContext.Current.CancellationToken)).StatusCode);
        string bearer = await SignIn();
        using HttpRequestMessage read = new(HttpMethod.Get, "/v1/pioneer/inventory");
        read.Headers.Authorization = AuthenticationHeaderValue.Parse(bearer);
        JsonElement items = await Json(await _client.SendAsync(read, TestContext.Current.CancellationToken));
        Assert.Equal(103, items.GetProperty("items").GetArrayLength());
        JsonElement definitions = await GetJson("/v1/pioneer/game-data");
        Assert.Contains(definitions.GetProperty("items-description").GetProperty("itemDescriptions").EnumerateArray(),
            item => item.GetProperty("gameAssetId").GetInt64() == StorageItems.RubberDuckAssetId);

        await Post("/v1/shared/social/party/v2/create", new { });
        string ticket = (await Json(await Post("/v1/shared/match/start", new { matchmakingScenario = "TheDam" }, bearer))).GetProperty("ticketId").GetString()!;
        await Post("/v1/shared/match/status", new { ticketId = ticket }, bearer);
        Assert.Equal("carried", Assert.Single(Assert.Single(_raid.LastRequest!.Loadouts!).Items, item => item.GameAssetId == StorageItems.RubberDuckAssetId).InstanceId);
        Assert.Equal(inventory.Snapshot.UserId, _raid.LastRequest.Loadouts![0].UserId);
        Assert.Equal(ticket, new InventoryStore(_database.Store).ReservedTicketId);
        Assert.Throws<InvalidOperationException>(() => inventory.Import(inventory.Inventory with { Stash = inventory.Inventory.Stash with { Items = [] } }, inventory.Snapshot.Revision));

        _raid.Forget();
        await Post("/v1/shared/match/start", new { matchmakingScenario = "TheDam" }, bearer);
        Assert.Null(inventory.ReservedTicketId);
        Assert.Equal(2, inventory.Inventory.ItemCount);
    }

    [Fact]
    public async Task Menu_transfers_require_identity_and_return_persisted_changes()
    {
        const string Path = "/v1/pioneer/v1/batch-operation";
        string bearer = await SignIn();
        InventoryStore store = _host!.Services.GetRequiredService<InventoryStore>();
        InventoryItem duck = store.Snapshot.Items.Single(item => item.GameAssetId == StorageItems.RubberDuckAssetId);
        string stash = store.Snapshot.Items.Single(item => item.Slots.Contains(duck.InstanceId)).InstanceId;
        InventoryItem backpack = store.Snapshot.Items.Single(item => item.GameAssetId == LoadoutItems.BackpackAssetId);
        string destination = backpack.Slots[0];
        object body = new
        {
            requestId = "stock-menu-request",
            operations = new[] { new { discriminator = "setParents", itemInstanceId = duck.InstanceId, parentInstanceIds = new[] { destination } } },
        };
        Assert.Equal(HttpStatusCode.Unauthorized, (await Post(Path, body)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Post(Path, new { requestId = "malformed", operations = new[] { new { discriminator = "unknown" } } }, bearer)).StatusCode);
        JsonElement response = await Json(await Post(Path, body, bearer));
        Assert.Equal(3, response.GetProperty("changeset").GetProperty("items").GetProperty("updated").GetArrayLength());
        InventoryStore persisted = new(_database.Store);
        Assert.Contains(duck.InstanceId, persisted.Snapshot.Items.Single(item => item.InstanceId == destination).Slots);
        long revision = persisted.Snapshot.Revision;
        Assert.Equal(HttpStatusCode.OK, (await Post(Path, body, bearer)).StatusCode);
        Assert.Equal(revision, new InventoryStore(_database.Store).Snapshot.Revision);
        object stockBody = new
        {
            requestId = "",
            operations = new[] { new { discriminator = "setParents", itemInstanceId = duck.InstanceId, parentInstanceIds = new[] { stash } } },
        };
        Assert.Equal(179, JsonSerializer.SerializeToUtf8Bytes(stockBody).Length);
        Assert.Equal(HttpStatusCode.OK, (await Post(Path, stockBody, bearer)).StatusCode);
        revision = store.Snapshot.Revision;
        Assert.Equal(HttpStatusCode.OK, (await Post(Path, stockBody, bearer)).StatusCode);
        Assert.Equal(revision, store.Snapshot.Revision);
        Assert.Contains(duck.InstanceId, new InventoryStore(_database.Store).Snapshot.Items.Single(item => item.InstanceId == stash).Slots);
    }

    [Fact]
    public async Task A_new_host_serves_the_duck_storage_and_changes_the_manifest_when_definitions_change()
    {
        string bearer = await SignIn();
        using HttpRequestMessage read = new(HttpMethod.Get, "/v1/pioneer/inventory");
        read.Headers.Authorization = AuthenticationHeaderValue.Parse(bearer);
        JsonElement inventory = await Json(await _client!.SendAsync(read, TestContext.Current.CancellationToken));
        JsonElement duck = Assert.Single(inventory.GetProperty("items").EnumerateArray(), item => item.GetProperty("gameAssetId").GetInt64() == StorageItems.RubberDuckAssetId);
        Assert.Equal(1, duck.GetProperty("amount").GetInt64());

        JsonElement data = await GetJson("/v1/pioneer/game-data");
        JsonElement definitions = data.GetProperty("items-description").GetProperty("itemDescriptions");
        JsonElement duckDefinition = Assert.Single(definitions.EnumerateArray(), item => item.GetProperty("gameAssetId").GetInt64() == StorageItems.RubberDuckAssetId);
        Assert.Equal("GameItem", duckDefinition.GetProperty("itemType").GetString());
        Assert.Equal(15, duckDefinition.GetProperty("maxAmount").GetInt64());
        Assert.Equal("noSlot", duckDefinition.GetProperty("slotConfig").GetProperty("discriminator").GetString());
        JsonElement containerDefinition = Assert.Single(definitions.EnumerateArray(), item => item.GetProperty("gameAssetId").GetInt64() == StorageItems.StashContainerAssetId);
        Assert.Equal(StorageItems.StashSlotCount, containerDefinition.GetProperty("slotConfig").GetProperty("numberOfSlots").GetInt64());

        JsonElement before = await Json(await Post("/v1/shared/manifest", new { }));
        InventoryStore store = _host!.Services.GetRequiredService<InventoryStore>();
        store.Import(store.Inventory with { Stash = store.Inventory.Stash with { SlotCount = 128 } }, store.Snapshot.Revision);
        JsonElement after = await Json(await Post("/v1/shared/manifest", new { }));
        Assert.NotEqual(before.GetProperty("partitioning_hash").GetString(), after.GetProperty("partitioning_hash").GetString());
        Assert.NotEqual(before.GetProperty("non_partitioning_hash").GetString(), after.GetProperty("non_partitioning_hash").GetString());
    }

    [Fact]
    public async Task Party_changes_are_delivered_as_notifications()
    {
        await Post("/v1/shared/social/party/v2/create", new { });
        await Post("/v1/shared/social/party/set-ready-for-matchmaking", new { is_ready = true });

        JsonElement reply = await Json(await Post("/v1/shared/notifications", new { fromNotificationId = "0" }));
        string[] names = [.. reply.GetProperty("notifications").EnumerateArray().Select(item => item.GetProperty("name").GetString()!)];

        Assert.Equal(["socialPartyArrangeNotification", "partyPlayerReadyForMatchmakingNotification"], names);
        Assert.Equal("2", reply.GetProperty("nextFromNotificationId").GetString());
    }

    [Fact]
    public async Task Persistent_keys_and_display_name_are_stored()
    {
        await Post("/v1/shared/set-persistent-player-keys", new { persistentPlayerKeys = new[] { new { key = "UI.Test", value = "1" } } });
        HttpResponseMessage badName = await Put("/v1/shared/profile/display-name", new { displayName = "x#" });
        JsonElement profile = await Json(await Put("/v1/shared/profile/display-name", new { displayName = "New Name" }));
        JsonElement keys = await Json(await Post("/v1/shared/get-persistent-player-keys", new { }));

        Assert.Equal(HttpStatusCode.BadRequest, badName.StatusCode);
        Assert.Equal("New Name", profile.GetProperty("display_name").GetProperty("name").GetString());
        Assert.Contains(keys.GetProperty("persistentPlayerKeys").EnumerateArray(), key => key.GetProperty("key").GetString() == "UI.Test");
        Assert.True(File.Exists(Path.Combine(_database.Directory, "account-state.json")));
    }

    [Fact]
    public async Task The_current_display_name_crosses_the_allocation_api_as_a_squad_profile()
    {
        string bearer = await SignIn();
        JsonElement renamed = await Json(await Put("/v1/shared/profile/display-name", new { displayName = "Squad Raider" }));
        await Post("/v1/shared/social/party/v2/create", new { });
        string ticket = (await Json(await Post("/v1/shared/match/start", new { matchmakingScenario = "TheDam" }, bearer))).GetProperty("ticketId").GetString()!;
        await Post("/v1/shared/match/status", new { ticketId = ticket }, bearer);

        CompactPlayerProfile profile = Assert.Single(_raid.LastRequest!.Profiles!);
        Assert.Equal((ulong)renamed.GetProperty("tenancy_user_id").GetInt64(), profile.TenancyUserId.Value);
        Assert.Equal("Squad Raider", profile.DisplayName);
        Assert.Equal(renamed.GetProperty("display_name").GetProperty("discriminator").GetString(), profile.DisplayNameDiscriminator);
        Assert.Equal(renamed.GetProperty("third_party_user_id").GetString(), profile.ThirdPartyUserId);
        Assert.Equal(renamed.GetProperty("third_party_last_seen_account_name").GetString(), profile.ThirdPartyLastSeenAccountName);
    }

    private async Task<string> SignIn()
    {
        JsonElement token = await Json(await _client!.PostAsync("/oauth2/token",
            new FormUrlEncodedContent([new KeyValuePair<string, string>("grant_type", "external")]), TestContext.Current.CancellationToken));
        return "Bearer " + token.GetProperty("access_token").GetString();
    }

    private async Task<HttpResponseMessage> Post(string path, object body, string? authorization = null)
    {
        using HttpRequestMessage request = new(HttpMethod.Post, path);
        request.Content = JsonContent.Create(body);
        if (authorization is not null)
        {
            request.Headers.Authorization = AuthenticationHeaderValue.Parse(authorization);
        }

        return await _client!.SendAsync(request, TestContext.Current.CancellationToken);
    }

    private async Task<HttpResponseMessage> Put(string path, object body)
    {
        return await _client!.PutAsJsonAsync(path, body, TestContext.Current.CancellationToken);
    }

    private async Task<JsonElement> GetJson(string path)
    {
        return await Json(await _client!.GetAsync(path, TestContext.Current.CancellationToken));
    }

    private static async Task<JsonElement> Json(HttpResponseMessage response)
    {
        using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        return document.RootElement.Clone();
    }

    private sealed class FakeRaid : IAllocationService
    {
        private Allocation? _allocation;

        public AllocationRequest? LastRequest { get; private set; }

        public void Forget()
        {
            _allocation = null;
        }

        public AllocationResult Reserve(AllocationRequest request)
        {
            LastRequest = request;
            _allocation ??= new Allocation(request.TicketId, Guid.NewGuid().ToString("D"), request.ScenarioName, request.PartyMemberIds,
                AllocationStates.Available, ServerInitializationStates.Ready, 2.0, new GameServerEndpoint("raid", "127.0.0.1", 7777, "", "", false, "LOCAL"),
                request.Loadouts?.Select(InventorySnapshots.Receipt).ToArray());
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
