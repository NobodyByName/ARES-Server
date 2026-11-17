using System.Net;
using Ares.Common.Game.Inventory;
using Ares.Host.Services.Inventory;
using Ares.Structs.Backend.Progression;

namespace Ares.Host.Tests;

public sealed class RaidReservationTests : IAsyncLifetime
{
    private readonly HostHarness _host = new();

    public ValueTask InitializeAsync() => _host.InitializeAsync();

    public ValueTask DisposeAsync() => _host.DisposeAsync();

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task The_ready_inventory_swap_releases_a_lost_raid_before_saving_and_freezes_the_new_loadout(bool startFirst, bool restartHost)
    {
        (string bearer, InventoryStore store, string oldTicket, InventoryOperationsRequest swap) = await _host.ReserveLootingAugment();
        _host.Raid.Forget();
        if (restartHost)
        {
            await _host.RestartAsync();
            store = _host.HostInventory();
            await _host.Post("/v1/shared/social/party/v2/create", new { });
        }

        string? ticket = startFirst ? await _host.StartMatch(bearer) : null;

        Assert.Equal(HttpStatusCode.OK, (await _host.Post("/v1/pioneer/v1/batch-operation", swap, bearer)).StatusCode);
        Assert.Null(store.ReservedTicketId);
        Assert.Equal(ItemTechnicalNames.CombatMk1, new InventoryStore(_host.Database.Store, TestDatabase.UserId, TestCatalog.Real).Inventory.Loadout.Augment!.Type);
        ticket ??= await _host.StartMatch(bearer);
        Assert.NotEqual(oldTicket, ticket);
        Assert.Equal(HttpStatusCode.OK, (await _host.Post("/v1/shared/match/status", new { ticketId = ticket }, bearer)).StatusCode);

        PlayerLoadout loadout = Assert.Single(_host.Raid.LastRequest!.Loadouts!);
        Assert.Equal(store.Snapshot.Revision, loadout.Revision);
        Assert.Equal("combat", Assert.Single(loadout.Items, item => item.GameAssetId == TestCatalog.Asset(ItemTechnicalNames.CombatMk1)).InstanceId);
        Assert.Contains(loadout.Items, item => item.GameAssetId == TestCatalog.AugmentFrames[TestCatalog.Asset(ItemTechnicalNames.CombatMk1)]);
        Assert.DoesNotContain(loadout.Items, item => item.GameAssetId == TestCatalog.Asset(ItemTechnicalNames.LootingMk1));
        Assert.Single(loadout.Items, item => item.GameAssetId == TestCatalog.Asset(ItemTechnicalNames.RubberDuck));
        Assert.Equal(ticket, store.ReservedTicketId);
        Assert.Equal(3, store.Inventory.ItemCount);
        Assert.Equal("looting", Assert.Single(store.Inventory.Stash.Items).InstanceId);
    }

    [Theory]
    [InlineData(false, HttpStatusCode.Conflict)]
    [InlineData(true, HttpStatusCode.ServiceUnavailable)]
    public async Task A_ready_swap_preserves_a_live_or_unreachable_raid_reservation(bool unavailable, HttpStatusCode expected)
    {
        (string bearer, InventoryStore store, string ticket, InventoryOperationsRequest swap) = await _host.ReserveLootingAugment();
        _host.Raid.StatusUnavailable = unavailable;
        string saved = _host.Database.InventoryJson;

        Assert.Equal(expected, (await _host.Post("/v1/pioneer/v1/batch-operation", swap, bearer)).StatusCode);

        Assert.Equal(saved, _host.Database.InventoryJson);
        Assert.Equal(ticket, store.ReservedTicketId);
        Assert.Equal(ItemTechnicalNames.LootingMk1, store.Inventory.Loadout.Augment!.Type);
        Assert.Equal("looting", Assert.Single(Assert.Single(_host.Raid.LastRequest!.Loadouts!).Items,
            item => item.GameAssetId == TestCatalog.Asset(ItemTechnicalNames.LootingMk1)).InstanceId);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Startup_releases_the_reservation_of_a_raid_stopped_with_the_previous_Host(bool unavailable)
    {
        (_, InventoryStore store, _, _) = await _host.ReserveLootingAugment();
        PlayerInventory before = store.Inventory;
        _host.Raid.StatusUnavailable = unavailable;
        await _host.RestartAsync();
        store = _host.HostInventory();

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
        await _host.ReserveLootingAugment();
        InventoryStore store = _host.HostInventory();
        Assert.NotNull(store.ReservedTicketId);
        _host.Raid.Forget();

        using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(10));
        while (store.ReservedTicketId is not null)
        {
            await Task.Delay(50, timeout.Token);
        }

        store.AddItem(ItemTechnicalNames.CombatMk1, DateTimeOffset.UnixEpoch);
        Assert.Contains(store.Inventory.Stash.Items, item => item.Type == ItemTechnicalNames.CombatMk1);
    }
}
