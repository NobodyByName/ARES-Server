using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Ares.Host.Services.Status;
using Ares.Structs.Core;

namespace Ares.Host.Tests;

public sealed class HostRouteTests : IAsyncLifetime
{
    private readonly HostHarness _host = HostHarness.Synthetic();

    public ValueTask InitializeAsync() => _host.InitializeAsync();

    public ValueTask DisposeAsync() => _host.DisposeAsync();

    [Fact]
    public async Task Archive_keys_are_served_without_sign_in()
    {
        JsonElement keys = await _host.GetJson(ArchiveKeysEndpoint.Path);

        Assert.Equal(GameBuild.SteamBuildId.ToString(CultureInfo.InvariantCulture), keys.GetProperty("game_build").GetString());
        Assert.Equal("000102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1E1F", keys.GetProperty("pak_key").GetString());
        Assert.Equal(8, keys.GetProperty("container_key").GetArrayLength());
    }

    [Fact]
    public async Task Status_health_and_unknown_routes()
    {
        JsonElement status = await _host.GetJson("/ares/v1/status");
        HttpResponseMessage unknown = await _host.Client.GetAsync("/nope", TestContext.Current.CancellationToken);

        Assert.Equal(1, status.GetProperty("protocol").GetInt32());
        Assert.True(status.GetProperty("matchmaking").GetBoolean());
        Assert.Equal(0, status.GetProperty("raids").GetProperty("active").GetInt32());
        Assert.Equal("ARES-Host", (await _host.GetJson("/health")).GetProperty("service").GetString());
        Assert.Equal(HttpStatusCode.NotImplemented, unknown.StatusCode);
        Assert.Equal("ARES-Host", unknown.Headers.Server.ToString());
    }

    [Fact]
    public async Task Routes_match_method_and_path_exactly()
    {
        using HttpClient client = _host.Anonymous();
        HttpResponseMessage statusByPost = await client.PostAsJsonAsync(PublicStatus.Path, new { }, TestContext.Current.CancellationToken);
        HttpResponseMessage scenariosByGet = await client.GetAsync("/v1/shared/scenarios", TestContext.Current.CancellationToken);
        HttpResponseMessage trailingSlash = await client.PostAsJsonAsync("/v1/shared/heartbeat/", new { }, TestContext.Current.CancellationToken);
        HttpResponseMessage healthByPost = await client.PostAsJsonAsync("/health", new { }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotImplemented, statusByPost.StatusCode);
        Assert.Equal(HttpStatusCode.NotImplemented, scenariosByGet.StatusCode);
        Assert.Equal(HttpStatusCode.NotImplemented, trailingSlash.StatusCode);
        Assert.Equal(HttpStatusCode.OK, healthByPost.StatusCode);
    }

    [Fact]
    public async Task Every_backend_route_except_sign_in_needs_a_signed_in_player()
    {
        using HttpClient client = _host.Anonymous();
        foreach (string path in new[] { "/v1/shared/player/activity", "/v1/shared/manifest", "/v1/shared/scenarios", "/v1/shared/heartbeat",
                     "/v1/shared/get-persistent-player-keys", "/auth/v1/oauth/token" })
        {
            HttpResponseMessage anonymous = await client.PostAsJsonAsync(path, new { }, TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
        }

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/v1/shared/game-settings", TestContext.Current.CancellationToken)).StatusCode);
        HttpResponseMessage forged = await _host.Post("/v1/shared/player/activity", new { }, "Bearer a.b.c");
        Assert.Equal(HttpStatusCode.Unauthorized, forged.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _host.Post("/v1/shared/player/activity", new { }, await _host.SignIn())).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _host.Post("/v1/shared/scenarios", new { })).StatusCode);
    }
}
