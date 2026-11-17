using System.Net;
using System.Text.Json;
using Ares.Host.Services.Accounts;
using Ares.Structs.Backend.Progression;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Ares.Host.Tests;

public sealed class PlayerAccountTests : IAsyncLifetime
{
    private readonly HostHarness _host = HostHarness.Synthetic();

    public ValueTask InitializeAsync() => _host.InitializeAsync();

    public ValueTask DisposeAsync() => _host.DisposeAsync();

    [Fact]
    public async Task Invalid_persisted_data_returns_503_without_resetting_the_player()
    {
        await _host.Database.Accounts.UpdateOneAsync(FilterDefinition<BsonDocument>.Empty,
            Builders<BsonDocument>.Update.Set("inventoryVersion", -1L), cancellationToken: TestContext.Current.CancellationToken);
        HttpResponseMessage response = await _host.Client.GetAsync("/v1/shared/profile", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal(-1, _host.Database.Account["inventoryVersion"].AsInt64);
    }

    [Fact]
    public async Task Persistent_keys_are_stored()
    {
        await _host.Post("/v1/shared/set-persistent-player-keys", new { persistentPlayerKeys = new[] { new { key = "UI.Test", value = "1" } } });
        JsonElement keys = await HostHarness.Json(await _host.Post("/v1/shared/get-persistent-player-keys", new { }));

        Assert.Contains(keys.GetProperty("persistentPlayerKeys").EnumerateArray(), key => key.GetProperty("key").GetString() == "UI.Test");
        Assert.Equal("1", _host.Database.Account["settings"]["persistentPlayerKeys"]["UI.Test"]["value"].AsString);
    }

    [Fact]
    public async Task Each_account_signs_in_through_the_launcher_and_keeps_its_own_profile_and_inventory()
    {
        AccountDirectory accounts = new(_host.Database.Store, TimeProvider.System);
        accounts.Create("Alice", "alice-password");
        accounts.Create("Bob", "bob-password");
        (string aliceBearer, _) = await _host.LauncherSignIn("Alice", "alice-password");
        (string bobBearer, _) = await _host.LauncherSignIn("Bob", "bob-password");
        long alice = accounts.Find("Alice")!.UserId;
        _host.Players.Find(alice)!.Inventory.AddItem(ItemTechnicalNames.HeavyAmmo, DateTimeOffset.UnixEpoch, 10);

        JsonElement aliceProfile = await HostHarness.Json(await _host.Get("/v1/shared/profile", aliceBearer));
        JsonElement bobProfile = await HostHarness.Json(await _host.Get("/v1/shared/profile", bobBearer));
        JsonElement aliceItems = await HostHarness.Json(await _host.Get("/v1/pioneer/inventory", aliceBearer));
        JsonElement bobItems = await HostHarness.Json(await _host.Get("/v1/pioneer/inventory", bobBearer));

        Assert.Equal("Alice", aliceProfile.GetProperty("display_name").GetProperty("name").GetString());
        Assert.Equal("Bob", bobProfile.GetProperty("display_name").GetProperty("name").GetString());
        Assert.Equal(alice, aliceProfile.GetProperty("tenancy_user_id").GetInt64());
        Assert.NotEqual(alice, bobProfile.GetProperty("tenancy_user_id").GetInt64());
        long ammo = _host.Catalog.Items.AssetIds[ItemTechnicalNames.HeavyAmmo];
        Assert.Contains(aliceItems.GetProperty("items").EnumerateArray(), item => item.GetProperty("gameAssetId").GetInt64() == ammo);
        Assert.DoesNotContain(bobItems.GetProperty("items").EnumerateArray(), item => item.GetProperty("gameAssetId").GetInt64() == ammo);
    }

    [Fact]
    public async Task Game_sign_in_needs_a_live_launch_key_or_refresh_token_of_an_unbanned_account()
    {
        AccountDirectory accounts = new(_host.Database.Store, TimeProvider.System);
        accounts.Create("Alice", "alice-password");
        (string bearer, string refresh) = await _host.LauncherSignIn("Alice", "alice-password");

        Assert.Equal(HttpStatusCode.OK, (await _host.GameSignIn(("grant_type", "refresh_token"), ("refresh_token", refresh))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _host.GameSignIn(("grant_type", "refresh_token"), ("refresh_token", bearer[7..]))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _host.GameSignIn(("grant_type", "external"))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _host.GameSignIn(("client_secret", Convert.ToBase64String(new byte[32])))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _host.Get("/v1/shared/profile", "Bearer " + refresh)).StatusCode);

        Account banned = accounts.SetBanned("Alice", banned: true)!;
        _host.Players.Update(banned);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _host.Get("/v1/shared/profile", bearer)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _host.GameSignIn(("grant_type", "refresh_token"), ("refresh_token", refresh))).StatusCode);
    }
}
