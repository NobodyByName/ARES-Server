using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Numerics;
using System.Security.Cryptography;
using System.Text.Json;
using Ares.Common.Control;
using Ares.Common.Control.Server;
using Ares.Common.Game.Inventory;
using Ares.Common.Logging;
using Ares.Host.Services;
using Ares.Host.Services.Accounts;
using Ares.Host.Services.Inventory;
using Ares.Host.Services.Matchmaking;
using Ares.Host.Services.Players;
using Ares.Host.Services.Status;
using Ares.Structs.Backend.Progression;
using Ares.Structs.Core;
using Ares.Structs.Net.Replication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;

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

        await StartHost();
    }

    private async Task StartHost()
    {
        HostConfiguration configuration = new()
        {
            Settings = new HostSettings
            {
                PublicOrigin = "http://127.0.0.1:18080",
                IdentityKey = [.. "identity-key-identity-key-identity-key"u8],
                ArchiveKeys = ArchiveKeys.Parse(ArchiveKeysTests.DummyFile),
                MongoDbUri = TestDatabase.ConnectionUri,
                MongoDbDatabase = _database.Name,
                RouteDelayScale = 0,
            },
            Bind = IPAddress.Loopback,
            Port = 18080,
        };
        _ = _database.Store;
        _host = HostApplication.Build(configuration, new JsonEventLog(TextWriter.Null), raidStarter: new TestRaidStarter(_raidApp!),
            configureBuilder: builder => builder.WebHost.UseTestServer());
        await _host.StartAsync(TestContext.Current.CancellationToken);
        _client?.Dispose();
        _client = _host.GetTestClient();
        _client.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse(await SignIn());
    }

    private HttpClient Anonymous() => _host!.GetTestClient();

    private InventoryStore HostInventory() => _host!.Services.GetRequiredService<PlayerRegistry>().Find(TestDatabase.UserId)!.Inventory;

    public async ValueTask DisposeAsync()
    {
        _client!.Dispose();
        await _host!.DisposeAsync();
        await _raidApp!.DisposeAsync();
        _database.Dispose();
    }

    [Fact]
    public async Task Invalid_persisted_data_returns_503_without_resetting_the_player()
    {
        await _database.Accounts.UpdateOneAsync(FilterDefinition<BsonDocument>.Empty,
            Builders<BsonDocument>.Update.Set("inventoryVersion", -1L), cancellationToken: TestContext.Current.CancellationToken);
        HttpResponseMessage response = await _client!.GetAsync("/v1/shared/profile", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal(-1, _database.Account["inventoryVersion"].AsInt64);
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
    public async Task Every_backend_route_except_sign_in_needs_a_signed_in_player()
    {
        using HttpClient client = Anonymous();
        foreach (string path in new[] { "/v1/shared/player/activity", "/v1/shared/manifest", "/v1/shared/scenarios", "/v1/shared/heartbeat",
                     "/v1/shared/get-persistent-player-keys", "/auth/v1/oauth/token" })
        {
            HttpResponseMessage anonymous = await client.PostAsJsonAsync(path, new { }, TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
        }

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/v1/shared/game-settings", TestContext.Current.CancellationToken)).StatusCode);
        HttpResponseMessage forged = await Post("/v1/shared/player/activity", new { }, "Bearer a.b.c");
        Assert.Equal(HttpStatusCode.Unauthorized, forged.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Post("/v1/shared/player/activity", new { }, await SignIn())).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Post("/v1/shared/scenarios", new { })).StatusCode);
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
        InventoryStore inventory = HostInventory();
        long revision = inventory.Snapshot.Revision;
        PlayerInventory state = inventory.Inventory;
        OwnedItem duck = Assert.Single(state.Stash.Items);
        inventory.Import(state with
        {
            Stash = state.Stash with { Items = [duck with { InstanceId = "stash", Quantity = 3 }] },
            Loadout = state.Loadout with { Backpack = new PlayerItemContainer([duck with { InstanceId = "carried", Quantity = 2, Slot = 0 }]) },
        }, revision);
        using HttpClient anonymous = Anonymous();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/v1/pioneer/inventory", TestContext.Current.CancellationToken)).StatusCode);
        string bearer = await SignIn();
        using HttpRequestMessage read = new(HttpMethod.Get, "/v1/pioneer/inventory");
        read.Headers.Authorization = AuthenticationHeaderValue.Parse(bearer);
        JsonElement items = await Json(await _client!.SendAsync(read, TestContext.Current.CancellationToken));
        Assert.Equal(228, items.GetProperty("items").GetArrayLength());
        JsonElement definitions = await GetJson("/v1/pioneer/game-data");
        Assert.Contains(definitions.GetProperty("items-description").GetProperty("itemDescriptions").EnumerateArray(),
            item => item.GetProperty("gameAssetId").GetInt64() == StorageItems.RubberDuckAssetId);

        await Post("/v1/shared/social/party/v2/create", new { });
        string ticket = (await Json(await Post("/v1/shared/match/start", new { matchmakingScenario = "TheDam" }, bearer))).GetProperty("ticketId").GetString()!;
        await Post("/v1/shared/match/status", new { ticketId = ticket }, bearer);
        Assert.Equal("carried", Assert.Single(Assert.Single(_raid.LastRequest!.Loadouts!).Items, item => item.GameAssetId == StorageItems.RubberDuckAssetId).InstanceId);
        Assert.Equal(inventory.Snapshot.UserId, _raid.LastRequest.Loadouts![0].UserId);
        Assert.Equal(ticket, new InventoryStore(_database.Store, TestDatabase.UserId).ReservedTicketId);
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
        InventoryStore store = HostInventory();
        InventoryItem duck = store.Snapshot.Items.Single(item => item.GameAssetId == StorageItems.RubberDuckAssetId);
        string stash = store.Snapshot.Items.Single(item => item.Slots.Contains(duck.InstanceId)).InstanceId;
        InventoryItem backpack = store.Snapshot.Items.Single(item => item.GameAssetId == LoadoutItems.BackpackAssetId);
        string destination = backpack.Slots[0];
        object body = new
        {
            requestId = "stock-menu-request",
            operations = new[] { new { discriminator = "setParents", itemInstanceId = duck.InstanceId, parentInstanceIds = new[] { destination } } },
        };
        using HttpClient anonymous = Anonymous();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.PostAsJsonAsync(Path, body, TestContext.Current.CancellationToken)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Post(Path, new { requestId = "malformed", operations = new[] { new { discriminator = "unknown" } } }, bearer)).StatusCode);
        JsonElement response = await Json(await Post(Path, body, bearer));
        Assert.Equal(3, response.GetProperty("changeset").GetProperty("items").GetProperty("updated").GetArrayLength());
        InventoryStore persisted = new(_database.Store, TestDatabase.UserId);
        Assert.Contains(duck.InstanceId, persisted.Snapshot.Items.Single(item => item.InstanceId == destination).Slots);
        long revision = persisted.Snapshot.Revision;
        Assert.Equal(HttpStatusCode.OK, (await Post(Path, body, bearer)).StatusCode);
        Assert.Equal(revision, new InventoryStore(_database.Store, TestDatabase.UserId).Snapshot.Revision);
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
        Assert.Contains(duck.InstanceId, new InventoryStore(_database.Store, TestDatabase.UserId).Snapshot.Items.Single(item => item.InstanceId == stash).Slots);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task The_ready_inventory_swap_releases_a_lost_raid_before_saving_and_freezes_the_new_loadout(bool startFirst, bool restartHost)
    {
        (string bearer, InventoryStore store, string oldTicket, InventoryOperationsRequest swap) = await ReserveLootingAugment();
        _raid.Forget();
        if (restartHost)
        {
            _client!.Dispose();
            await _host!.DisposeAsync();
            await StartHost();
            store = HostInventory();
            await Post("/v1/shared/social/party/v2/create", new { });
        }

        string? ticket = startFirst ? await StartMatch(bearer) : null;

        Assert.Equal(HttpStatusCode.OK, (await Post("/v1/pioneer/v1/batch-operation", swap, bearer)).StatusCode);
        Assert.Null(store.ReservedTicketId);
        Assert.Equal(ItemTechnicalNames.CombatMk1, new InventoryStore(_database.Store, TestDatabase.UserId).Inventory.Loadout.Augment!.Type);
        ticket ??= await StartMatch(bearer);
        Assert.NotEqual(oldTicket, ticket);
        Assert.Equal(HttpStatusCode.OK, (await Post("/v1/shared/match/status", new { ticketId = ticket }, bearer)).StatusCode);

        PlayerLoadout loadout = Assert.Single(_raid.LastRequest!.Loadouts!);
        Assert.Equal(store.Snapshot.Revision, loadout.Revision);
        Assert.Equal("combat", Assert.Single(loadout.Items, item => item.GameAssetId == AugmentItems.CombatMk1AssetId).InstanceId);
        Assert.Contains(loadout.Items, item => item.GameAssetId == AugmentItems.FrameAssetIds[AugmentItems.CombatMk1AssetId]);
        Assert.DoesNotContain(loadout.Items, item => item.GameAssetId == AugmentItems.LootingMk1AssetId);
        Assert.Single(loadout.Items, item => item.GameAssetId == StorageItems.RubberDuckAssetId);
        Assert.Equal(ticket, store.ReservedTicketId);
        Assert.Equal(3, store.Inventory.ItemCount);
        Assert.Equal("looting", Assert.Single(store.Inventory.Stash.Items).InstanceId);
    }

    [Theory]
    [InlineData(false, HttpStatusCode.Conflict)]
    [InlineData(true, HttpStatusCode.ServiceUnavailable)]
    public async Task A_ready_swap_preserves_a_live_or_unreachable_raid_reservation(bool unavailable, HttpStatusCode expected)
    {
        (string bearer, InventoryStore store, string ticket, InventoryOperationsRequest swap) = await ReserveLootingAugment();
        _raid.StatusUnavailable = unavailable;
        string saved = _database.InventoryJson;

        Assert.Equal(expected, (await Post("/v1/pioneer/v1/batch-operation", swap, bearer)).StatusCode);

        Assert.Equal(saved, _database.InventoryJson);
        Assert.Equal(ticket, store.ReservedTicketId);
        Assert.Equal(ItemTechnicalNames.LootingMk1, store.Inventory.Loadout.Augment!.Type);
        Assert.Equal("looting", Assert.Single(Assert.Single(_raid.LastRequest!.Loadouts!).Items,
            item => item.GameAssetId == AugmentItems.LootingMk1AssetId).InstanceId);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Startup_releases_the_reservation_of_a_raid_stopped_with_the_previous_Host(bool unavailable)
    {
        (_, InventoryStore store, _, _) = await ReserveLootingAugment();
        PlayerInventory before = store.Inventory;
        _raid.StatusUnavailable = unavailable;
        _client!.Dispose();
        await _host!.DisposeAsync();
        await StartHost();
        store = HostInventory();

        Assert.Null(store.ReservedTicketId);
        Assert.Equal(before.Revision, store.Inventory.Revision);
        Assert.Equal(before.Loadout.Augment, store.Inventory.Loadout.Augment);
        Assert.Equal(before.Stash.Items, store.Inventory.Stash.Items);
        store.AddItem(ItemTechnicalNames.CombatMk1, DateTimeOffset.UnixEpoch);
        Assert.Contains(store.Inventory.Stash.Items, item => item.Type == ItemTechnicalNames.CombatMk1);
    }

    [Fact]
    public async Task Recovery_releases_the_reservation_once_the_raid_ends_without_waiting_for_client_traffic()
    {
        await ReserveLootingAugment();
        InventoryStore store = HostInventory();
        Assert.NotNull(store.ReservedTicketId);
        _raid.Forget();

        using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(10));
        while (store.ReservedTicketId is not null)
        {
            await Task.Delay(50, timeout.Token);
        }

        store.AddItem(ItemTechnicalNames.CombatMk1, DateTimeOffset.UnixEpoch);
        Assert.Contains(store.Inventory.Stash.Items, item => item.Type == ItemTechnicalNames.CombatMk1);
    }

    private async Task<(string Bearer, InventoryStore Store, string Ticket, InventoryOperationsRequest Swap)> ReserveLootingAugment()
    {
        string bearer = await SignIn();
        InventoryStore store = HostInventory();
        PlayerInventory state = store.Inventory;
        OwnedItem duck = Assert.Single(state.Stash.Items);
        OwnedItem combat = duck with { InstanceId = "combat", Type = ItemTechnicalNames.CombatMk1 };
        OwnedItem looting = duck with { InstanceId = "looting", Type = ItemTechnicalNames.LootingMk1 };
        store.Import(state with
        {
            Stash = state.Stash with { Items = [combat] },
            Loadout = state.Loadout with { Augment = looting, Backpack = new PlayerItemContainer([duck with { Slot = 0 }]) },
        }, state.Revision);
        InventorySnapshot snapshot = store.Snapshot;
        string augmentSlot = snapshot.Items.Single(item => item.GameAssetId == AugmentItems.AugmentContentSlotAssetId).InstanceId;
        string stashSlot = snapshot.Items.Single(item => item.GameAssetId == StorageItems.StashContainerAssetId).Slots[0];
        InventoryItem frame = snapshot.Items.Single(item => item.GameAssetId == AugmentItems.FrameAssetIds[AugmentItems.CombatMk1AssetId]);
        InventoryItem backpack = snapshot.Items.Single(item => frame.Slots.Contains(item.InstanceId)
            && LoadoutDefinitions.StandardBranch(item.GameAssetId) == LoadoutItems.BackpackSlotAssetId);
        string backpackSlot = snapshot.Items.Single(item => item.InstanceId == backpack.Slots[0]).Slots[0];
        InventoryOperationsRequest swap = new(
            [new SetItemParents(combat.InstanceId, [augmentSlot]), new SetItemParents(looting.InstanceId, [stashSlot]),
                new SetItemParents(duck.InstanceId, [backpackSlot])], "");
        await Post("/v1/shared/social/party/v2/create", new { });
        string ticket = await StartMatch(bearer);
        Assert.Equal(HttpStatusCode.OK, (await Post("/v1/shared/match/status", new { ticketId = ticket }, bearer)).StatusCode);
        Assert.Equal(ticket, store.ReservedTicketId);
        return (bearer, store, ticket, swap);
    }

    private async Task<string> StartMatch(string bearer)
    {
        HttpResponseMessage response = await Post("/v1/shared/match/start", new { matchmakingScenario = "TheDam" }, bearer);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await Json(response)).GetProperty("ticketId").GetString()!;
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
        InventoryStore store = HostInventory();
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
    public async Task Persistent_keys_are_stored()
    {
        await Post("/v1/shared/set-persistent-player-keys", new { persistentPlayerKeys = new[] { new { key = "UI.Test", value = "1" } } });
        JsonElement keys = await Json(await Post("/v1/shared/get-persistent-player-keys", new { }));

        Assert.Contains(keys.GetProperty("persistentPlayerKeys").EnumerateArray(), key => key.GetProperty("key").GetString() == "UI.Test");
        Assert.Equal("1", _database.Account["settings"]["persistentPlayerKeys"]["UI.Test"]["value"].AsString);
    }

    [Fact]
    public async Task A_renamed_account_shows_and_crosses_the_allocation_api_with_its_new_name()
    {
        string bearer = await SignIn();
        Account renamedAccount = new AccountDirectory(_database.Store, TimeProvider.System).Rename(TestDatabase.AccountName, "Squad.Raider").Account!;
        _host!.Services.GetRequiredService<PlayerRegistry>().Update(renamedAccount);
        JsonElement renamed = await Json(await Get("/v1/shared/profile", bearer));
        await Post("/v1/shared/social/party/v2/create", new { });
        string ticket = (await Json(await Post("/v1/shared/match/start", new { matchmakingScenario = "TheDam" }, bearer))).GetProperty("ticketId").GetString()!;
        await Post("/v1/shared/match/status", new { ticketId = ticket }, bearer);

        Assert.Equal("Squad.Raider", renamed.GetProperty("display_name").GetProperty("name").GetString());
        CompactPlayerProfile profile = Assert.Single(_raid.LastRequest!.Profiles!);
        Assert.Equal((ulong)renamed.GetProperty("tenancy_user_id").GetInt64(), profile.TenancyUserId.Value);
        Assert.Equal("Squad.Raider", profile.DisplayName);
        Assert.Equal(renamed.GetProperty("display_name").GetProperty("discriminator").GetString(), profile.DisplayNameDiscriminator);
        Assert.Equal(renamed.GetProperty("third_party_user_id").GetString(), profile.ThirdPartyUserId);
        Assert.Equal(renamed.GetProperty("third_party_last_seen_account_name").GetString(), profile.ThirdPartyLastSeenAccountName);
    }

    [Fact]
    public async Task Each_account_signs_in_through_the_launcher_and_keeps_its_own_profile_and_inventory()
    {
        AccountDirectory accounts = new(_database.Store, TimeProvider.System);
        accounts.Create("Alice", "alice-password");
        accounts.Create("Bob", "bob-password");
        (string aliceBearer, _) = await LauncherSignIn("Alice", "alice-password");
        (string bobBearer, _) = await LauncherSignIn("Bob", "bob-password");
        long alice = accounts.Find("Alice")!.UserId;
        _host!.Services.GetRequiredService<PlayerRegistry>().Find(alice)!.Inventory.AddItem(ItemTechnicalNames.HeavyAmmo, DateTimeOffset.UnixEpoch, 10);

        JsonElement aliceProfile = await Json(await Get("/v1/shared/profile", aliceBearer));
        JsonElement bobProfile = await Json(await Get("/v1/shared/profile", bobBearer));
        JsonElement aliceItems = await Json(await Get("/v1/pioneer/inventory", aliceBearer));
        JsonElement bobItems = await Json(await Get("/v1/pioneer/inventory", bobBearer));

        Assert.Equal("Alice", aliceProfile.GetProperty("display_name").GetProperty("name").GetString());
        Assert.Equal("Bob", bobProfile.GetProperty("display_name").GetProperty("name").GetString());
        Assert.Equal(alice, aliceProfile.GetProperty("tenancy_user_id").GetInt64());
        Assert.NotEqual(alice, bobProfile.GetProperty("tenancy_user_id").GetInt64());
        long ammo = ItemTechnicalNames.AssetIds[ItemTechnicalNames.HeavyAmmo];
        Assert.Contains(aliceItems.GetProperty("items").EnumerateArray(), item => item.GetProperty("gameAssetId").GetInt64() == ammo);
        Assert.DoesNotContain(bobItems.GetProperty("items").EnumerateArray(), item => item.GetProperty("gameAssetId").GetInt64() == ammo);
    }

    [Fact]
    public async Task Game_sign_in_needs_a_live_launch_key_or_refresh_token_of_an_unbanned_account()
    {
        AccountDirectory accounts = new(_database.Store, TimeProvider.System);
        accounts.Create("Alice", "alice-password");
        (string bearer, string refresh) = await LauncherSignIn("Alice", "alice-password");

        Assert.Equal(HttpStatusCode.OK, (await GameSignIn(("grant_type", "refresh_token"), ("refresh_token", refresh))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await GameSignIn(("grant_type", "refresh_token"), ("refresh_token", bearer[7..]))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await GameSignIn(("grant_type", "external"))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await GameSignIn(("client_secret", Convert.ToBase64String(new byte[32])))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Get("/v1/shared/profile", "Bearer " + refresh)).StatusCode);

        Account banned = accounts.SetBanned("Alice", banned: true)!;
        _host!.Services.GetRequiredService<PlayerRegistry>().Update(banned);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Get("/v1/shared/profile", bearer)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await GameSignIn(("grant_type", "refresh_token"), ("refresh_token", refresh))).StatusCode);
    }

    private async Task<(string Bearer, string Refresh)> LauncherSignIn(string name, string password)
    {
        using HttpClient client = Anonymous();
        BigInteger secret = Srp.Number(RandomNumberGenerator.GetBytes(32));
        byte[] clientPublic = Srp.Pad(BigInteger.ModPow(Srp.G, secret, Srp.N));
        HttpResponseMessage started = await client.PostAsJsonAsync(SignInService.StartPath, new { name, a = Convert.ToBase64String(clientPublic) },
            TestContext.Current.CancellationToken);
        SignInStartResponse challenge = (await started.Content.ReadFromJsonAsync<SignInStartResponse>(TestContext.Current.CancellationToken))!;
        (byte[] proof, byte[] serverProof) = SrpClient.Prove(name, password, secret, clientPublic, challenge);
        HttpResponseMessage finished = await client.PostAsJsonAsync(SignInService.FinishPath,
            new { session = challenge.Session, proof = Convert.ToBase64String(proof) }, TestContext.Current.CancellationToken);
        SignInFinishResponse result = (await finished.Content.ReadFromJsonAsync<SignInFinishResponse>(TestContext.Current.CancellationToken))!;
        Assert.Equal(serverProof, Convert.FromBase64String(result.Proof));
        JsonElement token = await Json(await GameSignIn(("grant_type", "external"), ("client_secret", result.LaunchKey)));
        return ("Bearer " + token.GetProperty("access_token").GetString(), token.GetProperty("refresh_token").GetString()!);
    }

    private async Task<HttpResponseMessage> GameSignIn(params (string Name, string Value)[] fields)
    {
        using HttpClient client = Anonymous();
        return await client.PostAsync("/oauth2/token", new FormUrlEncodedContent(fields.Select(field => new KeyValuePair<string, string>(field.Name, field.Value))),
            TestContext.Current.CancellationToken);
    }

    private async Task<HttpResponseMessage> Get(string path, string authorization)
    {
        using HttpRequestMessage request = new(HttpMethod.Get, path);
        request.Headers.Authorization = AuthenticationHeaderValue.Parse(authorization);
        using HttpClient client = Anonymous();
        return await client.SendAsync(request, TestContext.Current.CancellationToken);
    }

    private async Task<string> SignIn()
    {
        string key = new AccountDirectory(_database.Store, TimeProvider.System).IssueLaunchKey(TestDatabase.UserId);
        using HttpClient client = Anonymous();
        JsonElement token = await Json(await client.PostAsync("/oauth2/token",
            new FormUrlEncodedContent([new KeyValuePair<string, string>("grant_type", "external"), new KeyValuePair<string, string>("client_secret", key)]), TestContext.Current.CancellationToken));
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

        public bool StatusUnavailable { get; set; }

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
            if (StatusUnavailable)
            {
                return AllocationResult.Failure(503, "test_unavailable");
            }

            return _allocation is not null && (request.MatchId is null ? _allocation.TicketId == request.TicketId : _allocation.MatchId == request.MatchId)
                ? AllocationResult.Success(_allocation)
                : AllocationResult.Failure(404, "unknown_session_allocation");
        }
    }

    /// <summary>Hands Host the in-memory Raid allocation API instead of starting a process.</summary>
    private sealed class TestRaidStarter(WebApplication raid) : IRaidStarter
    {
        public IRaidServer Start(int slot) => new Server(new SessionAllocatorClient(new Uri("http://localhost"), OperatorToken, raid.GetTestServer().CreateHandler()));

        private sealed class Server(SessionAllocatorClient allocator) : IRaidServer
        {
            public SessionAllocatorClient Allocator => allocator;

            public bool HasExited => false;

            public void Dispose() => allocator.Dispose();
        }
    }
}
