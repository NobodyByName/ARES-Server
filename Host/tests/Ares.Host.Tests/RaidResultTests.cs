using System.Net;
using System.Text.Json;
using Ares.Common.Control;
using Ares.Common.Game.Inventory;
using Ares.Host.Services.Inventory;
using Ares.Structs.Backend.Progression;

namespace Ares.Host.Tests;

public sealed class RaidResultTests : IAsyncLifetime
{
    private readonly HostHarness _host = new();

    public ValueTask InitializeAsync() => _host.InitializeAsync();

    public ValueTask DisposeAsync() => _host.DisposeAsync();

    [Fact]
    public async Task A_finished_raid_seen_on_the_inventory_read_is_saved_and_confirmed_to_Raid()
    {
        (string bearer, InventoryStore store, string ticket, _) = await _host.ReserveLootingAugment();
        RaidResult result = ExtractedResult(store, ticket, TestDatabase.UserId);
        _host.Raid.Results = [result];

        JsonElement items = await HostHarness.Json(await _host.Get("/v1/pioneer/inventory", bearer));

        Assert.Null(store.ReservedTicketId);
        Assert.Equal(store.Snapshot.Items.Count, items.GetProperty("items").GetArrayLength());
        Assert.Equal(result.Items[1].Item.InstanceId, Assert.Single(store.Inventory.Loadout.Backpack.Items).InstanceId);
        AllocationSettleRequest settled = _host.Raid.Settled[0];
        Assert.Equal((ticket, _host.Raid.MatchId), (settled.TicketId, settled.MatchId));
        Assert.Equal([TestDatabase.UserId], settled.UserIds);
    }

    [Fact]
    public async Task A_confirmation_Raid_did_not_receive_is_repeated_without_saving_twice_and_the_next_raid_can_start()
    {
        (string bearer, InventoryStore store, string ticket, _) = await _host.ReserveLootingAugment();
        _host.Raid.Results = [ExtractedResult(store, ticket, TestDatabase.UserId)];
        _host.Raid.SettleFailures = 1;

        Assert.Equal(HttpStatusCode.OK, (await _host.Get("/v1/pioneer/inventory", bearer)).StatusCode);
        Assert.Null(store.ReservedTicketId);
        long revision = store.Inventory.Revision;

        using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(10));
        while (_host.Raid.Settled.Count == 0)
        {
            await Task.Delay(50, timeout.Token);
        }

        Assert.Equal(revision, store.Inventory.Revision);
        Assert.Equal(ticket, _host.Raid.Settled[0].TicketId);
        Assert.NotEqual(ticket, await _host.StartMatch(bearer));
    }

    [Fact]
    public async Task An_invalid_raid_result_is_not_confirmed_and_keeps_the_reservation()
    {
        (string bearer, InventoryStore store, string ticket, _) = await _host.ReserveLootingAugment();
        RaidResult result = ExtractedResult(store, ticket, TestDatabase.UserId);
        _host.Raid.Results = [result with { Revision = result.Revision - 1 }];
        string saved = _host.Database.InventoryJson;

        Assert.Equal(HttpStatusCode.OK, (await _host.Get("/v1/pioneer/inventory", bearer)).StatusCode);

        Assert.Equal(ticket, store.ReservedTicketId);
        Assert.Equal(saved, _host.Database.InventoryJson);
        Assert.Empty(_host.Raid.Settled);
    }

    [Fact]
    public async Task A_result_for_another_player_is_ignored()
    {
        (string bearer, InventoryStore store, string ticket, _) = await _host.ReserveLootingAugment();
        _host.Raid.Results = [ExtractedResult(store, ticket, TestDatabase.UserId + 1)];
        string saved = _host.Database.InventoryJson;

        Assert.Equal(HttpStatusCode.OK, (await _host.Get("/v1/pioneer/inventory", bearer)).StatusCode);

        Assert.Equal(ticket, store.ReservedTicketId);
        Assert.Equal(saved, _host.Database.InventoryJson);
        Assert.Empty(_host.Raid.Settled);
    }

    [Fact]
    public async Task The_inventory_read_serves_the_saved_inventory_while_Raid_is_unreachable()
    {
        (string bearer, InventoryStore store, string ticket, _) = await _host.ReserveLootingAugment();
        _host.Raid.StatusUnavailable = true;

        JsonElement items = await HostHarness.Json(await _host.Get("/v1/pioneer/inventory", bearer));

        Assert.Equal(ticket, store.ReservedTicketId);
        Assert.Equal(store.Snapshot.Items.Count, items.GetProperty("items").GetArrayLength());
    }

    private static RaidResult ExtractedResult(InventoryStore store, string ticket, long userId)
    {
        OwnedItem augment = store.Inventory.Loadout.Augment!;
        CarriedItem carriedAugment = new(CarriedPlaces.Augment, 0,
            new InventoryItem(augment.InstanceId, LoadoutDefinitions.Generated.AssetIds[augment.Type], augment.Quantity, [], augment.Durability, augment.MaxDurability, "", augment.ETag, augment.UpdatedAt));
        CarriedItem looted = new(CarriedPlaces.Backpack, 0,
            new InventoryItem("looted-duck", StorageItems.RubberDuckAssetId, 2, [], StorageItems.InitialDurability, StorageItems.InitialMaxDurability, "", "", 0));
        return new RaidResult(ticket, userId, store.Snapshot.Revision, RaidOutcomes.Extracted, [carriedAugment, looted]);
    }
}
