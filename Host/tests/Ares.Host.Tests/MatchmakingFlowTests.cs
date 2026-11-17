using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Ares.Host.Services.Accounts;
using Ares.Host.Services.Inventory;
using Ares.Structs.Backend.Progression;
using Ares.Structs.Net.Replication;

namespace Ares.Host.Tests;

public sealed class MatchmakingFlowTests : IAsyncLifetime
{
    private readonly HostHarness _host = new();

    public ValueTask InitializeAsync() => _host.InitializeAsync();

    public ValueTask DisposeAsync() => _host.DisposeAsync();

    [Fact]
    public async Task Normal_flow_reaches_an_assigned_raid()
    {
        string bearer = await _host.SignIn();
        await _host.Post("/v1/shared/social/party/v2/create", new { crossplay_enabled = true, platform_session_id = "s", language = "en" });
        await _host.Post("/v1/shared/social/party/set-ready-for-matchmaking", new { is_ready = true });

        JsonElement ticket = await HostHarness.Json(await _host.Post("/v1/shared/match/start", new { matchmakingScenario = "TheDam", preferredSquadSize = 1 }, bearer));
        string ticketId = ticket.GetProperty("ticketId").GetString()!;
        JsonElement status = await HostHarness.Json(await _host.Post("/v1/shared/match/status", new { ticketId }, bearer));
        string matchId = status.GetProperty("redirectInfo").GetProperty("matchId").GetString()!;
        JsonElement gameServer = await HostHarness.Json(await _host.Post("/v1/shared/gameserver/status", new { matchId }, bearer));
        JsonElement activity = await HostHarness.Json(await _host.Post("/v1/shared/player/activity", new { }, bearer));
        JsonElement publicStatus = await _host.GetJson("/ares/v1/status");

        Assert.Equal("MATCHING", ticket.GetProperty("matchState").GetString());
        Assert.Equal("COMPLETED", status.GetProperty("matchState").GetString());
        Assert.Equal("AVAILABLE", gameServer.GetProperty("state").GetString());
        Assert.Equal(7777, gameServer.GetProperty("gameserver").GetProperty("port").GetInt32());
        Assert.Equal("IN_MATCH", activity.GetProperty("currentState").GetString());
        Assert.Equal(1, publicStatus.GetProperty("raids").GetProperty("active").GetInt32());
        Assert.Equal([1000000000000000000L], _host.Raid.LastRequest!.PartyMemberIds);
    }

    [Fact]
    public async Task Lost_reservations_report_expired()
    {
        string bearer = await _host.SignIn();
        await _host.Post("/v1/shared/social/party/v2/create", new { });
        string ticketId = (await HostHarness.Json(await _host.Post("/v1/shared/match/start", new { matchmakingScenario = "TheDam" }, bearer))).GetProperty("ticketId").GetString()!;
        string matchId = (await HostHarness.Json(await _host.Post("/v1/shared/match/status", new { ticketId }, bearer))).GetProperty("redirectInfo").GetProperty("matchId").GetString()!;
        _host.Raid.Forget();

        JsonElement gameServer = await HostHarness.Json(await _host.Post("/v1/shared/gameserver/status", new { matchId }, bearer));

        Assert.Equal("EXPIRED", gameServer.GetProperty("state").GetString());
        Assert.False(gameServer.TryGetProperty("gameserver", out _));
    }

    [Fact]
    public async Task Persisted_inventory_is_authenticated_and_the_selected_tree_crosses_the_allocation_api()
    {
        InventoryStore inventory = _host.HostInventory();
        long revision = inventory.Snapshot.Revision;
        PlayerInventory state = inventory.Inventory;
        OwnedItem duck = Assert.Single(state.Stash.Items);
        inventory.Import(state with
        {
            Stash = state.Stash with { Items = [duck with { InstanceId = "stash", Quantity = 3 }] },
            Loadout = state.Loadout with { Backpack = new PlayerItemContainer([duck with { InstanceId = "carried", Quantity = 2, Slot = 0 }]) },
        }, revision);
        using HttpClient anonymous = _host.Anonymous();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/v1/pioneer/inventory", TestContext.Current.CancellationToken)).StatusCode);
        string bearer = await _host.SignIn();
        using HttpRequestMessage read = new(HttpMethod.Get, "/v1/pioneer/inventory");
        read.Headers.Authorization = AuthenticationHeaderValue.Parse(bearer);
        JsonElement items = await HostHarness.Json(await _host.Client.SendAsync(read, TestContext.Current.CancellationToken));
        Assert.Equal(228, items.GetProperty("items").GetArrayLength());
        JsonElement definitions = await _host.GetJson("/v1/pioneer/game-data");
        Assert.Contains(definitions.GetProperty("items-description").GetProperty("itemDescriptions").EnumerateArray(),
            item => item.GetProperty("gameAssetId").GetInt64() == TestCatalog.Asset(ItemTechnicalNames.RubberDuck));

        await _host.Post("/v1/shared/social/party/v2/create", new { });
        string ticket = (await HostHarness.Json(await _host.Post("/v1/shared/match/start", new { matchmakingScenario = "TheDam" }, bearer))).GetProperty("ticketId").GetString()!;
        await _host.Post("/v1/shared/match/status", new { ticketId = ticket }, bearer);
        Assert.Equal("carried", Assert.Single(Assert.Single(_host.Raid.LastRequest!.Loadouts!).Items, item => item.GameAssetId == TestCatalog.Asset(ItemTechnicalNames.RubberDuck)).InstanceId);
        Assert.Equal(inventory.Snapshot.UserId, _host.Raid.LastRequest.Loadouts![0].UserId);
        Assert.Equal(ticket, new InventoryStore(_host.Database.Store, TestDatabase.UserId, TestCatalog.Real).ReservedTicketId);
        Assert.Throws<InvalidOperationException>(() => inventory.Import(inventory.Inventory with { Stash = inventory.Inventory.Stash with { Items = [] } }, inventory.Snapshot.Revision));

        _host.Raid.Forget();
        await _host.Post("/v1/shared/match/start", new { matchmakingScenario = "TheDam" }, bearer);
        Assert.Null(inventory.ReservedTicketId);
        Assert.Equal(2, inventory.Inventory.ItemCount);
    }

    [Fact]
    public async Task A_renamed_account_shows_and_crosses_the_allocation_api_with_its_new_name()
    {
        string bearer = await _host.SignIn();
        Account renamedAccount = new AccountDirectory(_host.Database.Store, TimeProvider.System).Rename(TestDatabase.AccountName, "Squad.Raider").Account!;
        _host.Players.Update(renamedAccount);
        JsonElement renamed = await HostHarness.Json(await _host.Get("/v1/shared/profile", bearer));
        await _host.Post("/v1/shared/social/party/v2/create", new { });
        string ticket = (await HostHarness.Json(await _host.Post("/v1/shared/match/start", new { matchmakingScenario = "TheDam" }, bearer))).GetProperty("ticketId").GetString()!;
        await _host.Post("/v1/shared/match/status", new { ticketId = ticket }, bearer);

        Assert.Equal("Squad.Raider", renamed.GetProperty("display_name").GetProperty("name").GetString());
        CompactPlayerProfile profile = Assert.Single(_host.Raid.LastRequest!.Profiles!);
        Assert.Equal((ulong)renamed.GetProperty("tenancy_user_id").GetInt64(), profile.TenancyUserId.Value);
        Assert.Equal("Squad.Raider", profile.DisplayName);
        Assert.Equal(renamed.GetProperty("display_name").GetProperty("discriminator").GetString(), profile.DisplayNameDiscriminator);
        Assert.Equal(renamed.GetProperty("third_party_user_id").GetString(), profile.ThirdPartyUserId);
        Assert.Equal(renamed.GetProperty("third_party_last_seen_account_name").GetString(), profile.ThirdPartyLastSeenAccountName);
    }

    [Fact]
    public async Task Party_changes_are_delivered_as_notifications()
    {
        await _host.Post("/v1/shared/social/party/v2/create", new { });
        await _host.Post("/v1/shared/social/party/set-ready-for-matchmaking", new { is_ready = true });

        JsonElement reply = await HostHarness.Json(await _host.Post("/v1/shared/notifications", new { fromNotificationId = "0" }));
        string[] names = [.. reply.GetProperty("notifications").EnumerateArray().Select(item => item.GetProperty("name").GetString()!)];

        Assert.Equal(["socialPartyArrangeNotification", "partyPlayerReadyForMatchmakingNotification"], names);
        Assert.Equal("2", reply.GetProperty("nextFromNotificationId").GetString());
    }
}
