using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Ares.Host.Services.Inventory;
using Ares.Structs.Backend.Progression;

namespace Ares.Host.Tests;

public sealed class MenuInventoryTests : IAsyncLifetime
{
    private readonly HostHarness _host = new();

    public ValueTask InitializeAsync() => _host.InitializeAsync();

    public ValueTask DisposeAsync() => _host.DisposeAsync();

    [Fact]
    public async Task Game_data_lists_celestes_offers_and_buying_one_is_saved()
    {
        JsonElement offers = (await _host.GetJson("/v1/pioneer/game-data")).GetProperty("offer-descriptions").GetProperty("offerDescriptions");
        Assert.Equal(TestCatalog.Content.TraderOffers.Select(offer => offer.OfferId), offers.EnumerateArray().Select(offer => offer.GetProperty("offerId").GetInt64()));
        Assert.All(offers.EnumerateArray(), offer => Assert.Equal(TestCatalog.CelesteAssetId, offer.GetProperty("ownerGameAssetId").GetInt64()));

        OfferDescription bought = TestCatalog.Content.TraderOffers[0];
        long seeds = Assert.Single(Assert.IsType<ItemAmountsOfferCost>(bought.Cost).ItemAmounts).Amount;
        _host.HostInventory().AddItem(ItemTechnicalNames.AssortedSeeds, DateTimeOffset.UnixEpoch, seeds);
        object body = new { requestId = "buy", operations = new[] { new { discriminator = "acceptOffer", offerId = bought.OfferId, times = 1, payment = Array.Empty<object>(), paymentByAmounts = Array.Empty<object>() } } };
        JsonElement items = (await HostHarness.Json(await _host.Post("/v1/pioneer/v1/batch-operation", body, await _host.SignIn()))).GetProperty("changeset").GetProperty("items");
        JsonElement reward = Assert.Single(items.GetProperty("created").EnumerateArray());
        Assert.Equal((bought.RewardPackage.RewardedItems[0].GameAssetId, 1L), (reward.GetProperty("gameAssetId").GetInt64(), reward.GetProperty("amount").GetInt64()));
        Assert.Equal(1, items.GetProperty("deleted").GetArrayLength());
        Assert.DoesNotContain(_host.HostInventory().Inventory.Stash.Items, item => item.Type == ItemTechnicalNames.AssortedSeeds);
    }

    [Fact]
    public async Task An_undecodable_batch_operation_is_logged_as_received_and_rejected()
    {
        object body = new
        {
            requestId = "unknown",
            operations = new object[]
            {
                new { discriminator = "acceptOffer", offerId = TestCatalog.Content.TraderOffers[0].OfferId, times = 1, payment = Array.Empty<object>(), paymentByAmounts = Array.Empty<object>() },
                new { discriminator = "notYetDecoded" },
            },
        };
        Assert.Equal(HttpStatusCode.BadRequest, (await _host.Post("/v1/pioneer/v1/batch-operation", body, await _host.SignIn())).StatusCode);
        string logged = Assert.Single(_host.Log.Split('\n'), line => line.Contains("undecoded_batch_operation", StringComparison.Ordinal));
        Assert.Contains("\"notYetDecoded\"", logged, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_scrap_operation_with_members_Host_does_not_know_is_logged_as_received()
    {
        object body = new
        {
            requestId = "scrap-shape",
            operations = new object[] { new { discriminator = "scrap", instanceId = "item", amount = 1, unexpectedMember = true } },
        };
        Assert.Equal(HttpStatusCode.BadRequest, (await _host.Post("/v1/pioneer/v1/batch-operation", body, await _host.SignIn())).StatusCode);
        string logged = Assert.Single(_host.Log.Split('\n'), line => line.Contains("undecoded_batch_operation", StringComparison.Ordinal));
        Assert.Contains("\"unexpectedMember\"", logged, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Menu_transfers_require_identity_and_return_persisted_changes()
    {
        const string Path = "/v1/pioneer/v1/batch-operation";
        string bearer = await _host.SignIn();
        InventoryStore store = _host.HostInventory();
        InventoryItem duck = store.Snapshot.Items.Single(item => item.GameAssetId == TestCatalog.Asset(ItemTechnicalNames.RubberDuck));
        string stash = store.Snapshot.Items.Single(item => item.Slots.Contains(duck.InstanceId)).InstanceId;
        InventoryItem backpack = store.Snapshot.Items.Single(item => item.GameAssetId == TestCatalog.BackpackContainer);
        string destination = backpack.Slots[0];
        object body = new
        {
            requestId = "stock-menu-request",
            operations = new[] { new { discriminator = "setParents", itemInstanceId = duck.InstanceId, parentInstanceIds = new[] { destination } } },
        };
        using HttpClient anonymous = _host.Anonymous();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.PostAsJsonAsync(Path, body, TestContext.Current.CancellationToken)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await _host.Post(Path, new { requestId = "malformed", operations = new[] { new { discriminator = "unknown" } } }, bearer)).StatusCode);
        JsonElement response = await HostHarness.Json(await _host.Post(Path, body, bearer));
        Assert.Equal(3, response.GetProperty("changeset").GetProperty("items").GetProperty("updated").GetArrayLength());
        InventoryStore persisted = new(_host.Database.Store, TestDatabase.UserId, TestCatalog.Real);
        Assert.Contains(duck.InstanceId, persisted.Snapshot.Items.Single(item => item.InstanceId == destination).Slots);
        long revision = persisted.Snapshot.Revision;
        Assert.Equal(HttpStatusCode.OK, (await _host.Post(Path, body, bearer)).StatusCode);
        Assert.Equal(revision, new InventoryStore(_host.Database.Store, TestDatabase.UserId, TestCatalog.Real).Snapshot.Revision);
        object stockBody = new
        {
            requestId = "",
            operations = new[] { new { discriminator = "setParents", itemInstanceId = duck.InstanceId, parentInstanceIds = new[] { stash } } },
        };
        Assert.Equal(179, JsonSerializer.SerializeToUtf8Bytes(stockBody).Length);
        Assert.Equal(HttpStatusCode.OK, (await _host.Post(Path, stockBody, bearer)).StatusCode);
        revision = store.Snapshot.Revision;
        Assert.Equal(HttpStatusCode.OK, (await _host.Post(Path, stockBody, bearer)).StatusCode);
        Assert.Equal(revision, store.Snapshot.Revision);
        Assert.Contains(duck.InstanceId, new InventoryStore(_host.Database.Store, TestDatabase.UserId, TestCatalog.Real).Snapshot.Items.Single(item => item.InstanceId == stash).Slots);
    }

    [Fact]
    public async Task A_new_host_serves_the_duck_storage_and_changes_the_manifest_when_definitions_change()
    {
        string bearer = await _host.SignIn();
        using HttpRequestMessage read = new(HttpMethod.Get, "/v1/pioneer/inventory");
        read.Headers.Authorization = AuthenticationHeaderValue.Parse(bearer);
        JsonElement inventory = await HostHarness.Json(await _host.Client.SendAsync(read, TestContext.Current.CancellationToken));
        JsonElement duck = Assert.Single(inventory.GetProperty("items").EnumerateArray(), item => item.GetProperty("gameAssetId").GetInt64() == TestCatalog.Asset(ItemTechnicalNames.RubberDuck));
        Assert.Equal(1, duck.GetProperty("amount").GetInt64());

        JsonElement data = await _host.GetJson("/v1/pioneer/game-data");
        JsonElement definitions = data.GetProperty("items-description").GetProperty("itemDescriptions");
        JsonElement duckDefinition = Assert.Single(definitions.EnumerateArray(), item => item.GetProperty("gameAssetId").GetInt64() == TestCatalog.Asset(ItemTechnicalNames.RubberDuck));
        Assert.Equal("GameItem", duckDefinition.GetProperty("itemType").GetString());
        Assert.Equal(15, duckDefinition.GetProperty("maxAmount").GetInt64());
        Assert.Equal("noSlot", duckDefinition.GetProperty("slotConfig").GetProperty("discriminator").GetString());
        JsonElement containerDefinition = Assert.Single(definitions.EnumerateArray(), item => item.GetProperty("gameAssetId").GetInt64() == TestCatalog.StashContainer);
        Assert.Equal(TestCatalog.StashSlotCount, containerDefinition.GetProperty("slotConfig").GetProperty("numberOfSlots").GetInt64());
        JsonElement upgrade = containerDefinition.GetProperty("upgrade");
        Assert.Equal(TestStash.Next(TestCatalog.StashContainer), upgrade.GetProperty("nextItemGameAssetId").GetInt64());
        Assert.Equal(TestCatalog.Asset(ItemTechnicalNames.Coins), upgrade.GetProperty("costPackage").GetProperty("costItems")[0].GetProperty("gameAssetId").GetInt64());
        Assert.Equal("itemAmounts", upgrade.GetProperty("cost").GetProperty("discriminant").GetString());
        Assert.Equal(0, upgrade.GetProperty("requirements").GetProperty("projecRequirements").GetArrayLength());
    }
}
