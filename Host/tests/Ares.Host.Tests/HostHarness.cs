using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Numerics;
using System.Security.Cryptography;
using System.Text.Json;
using Ares.Common.Archives;
using Ares.Common.Game.Inventory;
using Ares.Common.Logging;
using Ares.Host.Services;
using Ares.Host.Services.Accounts;
using Ares.Host.Services.Inventory;
using Ares.Host.Services.Players;
using Ares.Structs.Backend.Progression;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Ares.Host.Tests;

/// <summary>
/// A Host on a test server with its own test database, event log and one shared <see cref="FakeRaidServer"/>
/// behind every raid slot. <see cref="Client"/> is signed in as the database's test account.
/// </summary>
internal sealed class HostHarness : IAsyncLifetime
{
    private readonly StringWriter _log = new();
    private readonly FakeRaidStarter _raids;
    private WebApplication? _app;
    private HttpClient? _client;

    public HostHarness()
    {
        _raids = new FakeRaidStarter(Raid);
    }

    public TestDatabase Database { get; } = new();

    public FakeRaidServer Raid { get; } = new();

    /// <summary>The Host's JSON event log so far.</summary>
    public string Log => _log.ToString();

    private WebApplication App => _app!;

    /// <summary>A client signed in as <see cref="TestDatabase.UserId"/>.</summary>
    public HttpClient Client => _client!;

    public PlayerRegistry Players => App.Services.GetRequiredService<PlayerRegistry>();

    public async ValueTask InitializeAsync()
    {
        await _raids.StartAsync(TestContext.Current.CancellationToken);
        await StartHost();
    }

    /// <summary>Stops the Host and starts a new one on the same database and Raid server.</summary>
    public async Task RestartAsync()
    {
        _client!.Dispose();
        await _app!.DisposeAsync();
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
                MongoDbDatabase = Database.Name,
                RouteDelayScale = 0,
            },
            Bind = IPAddress.Loopback,
            Port = 18080,
        };
        _ = Database.Store;
        _app = HostApplication.Build(configuration, new JsonEventLog(_log), raidStarter: _raids,
            configureBuilder: builder => builder.WebHost.UseTestServer());
        await _app.StartAsync(TestContext.Current.CancellationToken);
        _client?.Dispose();
        _client = _app.GetTestClient();
        _client.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse(await SignIn());
    }

    public HttpClient Anonymous() => App.GetTestClient();

    public InventoryStore HostInventory() => Players.Find(TestDatabase.UserId)!.Inventory;

    public async Task<string> SignIn()
    {
        string key = new AccountDirectory(Database.Store, TimeProvider.System).IssueLaunchKey(TestDatabase.UserId);
        using HttpClient client = Anonymous();
        JsonElement token = await Json(await client.PostAsync("/oauth2/token",
            new FormUrlEncodedContent([new KeyValuePair<string, string>("grant_type", "external"), new KeyValuePair<string, string>("client_secret", key)]), TestContext.Current.CancellationToken));
        return "Bearer " + token.GetProperty("access_token").GetString();
    }

    public async Task<(string Bearer, string Refresh)> LauncherSignIn(string name, string password)
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

    public async Task<HttpResponseMessage> GameSignIn(params (string Name, string Value)[] fields)
    {
        using HttpClient client = Anonymous();
        return await client.PostAsync("/oauth2/token", new FormUrlEncodedContent(fields.Select(field => new KeyValuePair<string, string>(field.Name, field.Value))),
            TestContext.Current.CancellationToken);
    }

    public async Task<HttpResponseMessage> Get(string path, string authorization)
    {
        using HttpRequestMessage request = new(HttpMethod.Get, path);
        request.Headers.Authorization = AuthenticationHeaderValue.Parse(authorization);
        using HttpClient client = Anonymous();
        return await client.SendAsync(request, TestContext.Current.CancellationToken);
    }

    /// <summary>Posts <paramref name="body"/> as JSON on <see cref="Client"/>, as another player when <paramref name="authorization"/> is given.</summary>
    public async Task<HttpResponseMessage> Post(string path, object body, string? authorization = null)
    {
        using HttpRequestMessage request = new(HttpMethod.Post, path);
        request.Content = JsonContent.Create(body);
        if (authorization is not null)
        {
            request.Headers.Authorization = AuthenticationHeaderValue.Parse(authorization);
        }

        return await Client.SendAsync(request, TestContext.Current.CancellationToken);
    }

    public async Task<JsonElement> GetJson(string path)
    {
        return await Json(await Client.GetAsync(path, TestContext.Current.CancellationToken));
    }

    public static async Task<JsonElement> Json(HttpResponseMessage response)
    {
        using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        return document.RootElement.Clone();
    }

    /// <summary>Starts a ticket for TheDam and returns its id.</summary>
    public async Task<string> StartMatch(string bearer)
    {
        HttpResponseMessage response = await Post("/v1/shared/match/start", new { matchmakingScenario = "TheDam" }, bearer);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await Json(response)).GetProperty("ticketId").GetString()!;
    }

    /// <summary>
    /// Equips a Looting Mk1 augment with the duck in its backpack and a Combat Mk1 in the stash, then reserves a raid
    /// for that loadout. Returns the reserved ticket and a menu request swapping the augments.
    /// </summary>
    public async Task<(string Bearer, InventoryStore Store, string Ticket, InventoryOperationsRequest Swap)> ReserveLootingAugment()
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

    public async ValueTask DisposeAsync()
    {
        _client?.Dispose();
        if (_app is not null)
        {
            await _app.DisposeAsync();
        }

        await _raids.DisposeAsync();
        Database.Dispose();
        await _log.DisposeAsync();
    }
}
