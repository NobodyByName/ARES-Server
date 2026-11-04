using Ares.Host.Services.Accounts;
using Ares.Host.Services.Content;
using Ares.Host.Services.Inventory;
using Ares.Host.Services.Persistence;
using Ares.Structs.Backend.Accounts;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Ares.Host.Tests;

public sealed class PersistenceTests
{
    [Fact]
    public void Initialization_creates_a_new_account_and_leaves_existing_data_unchanged()
    {
        using TestDatabase database = new();
        HostDatabase store = database.Store;
        InventoryStore inventory = new(store);
        inventory.InitializeLayout(DateTimeOffset.UnixEpoch);
        string saved = database.Player.ToJson();
        store.Initialize();
        Assert.Equal(saved, database.Player.ToJson());
        Assert.Equal(1, new InventoryStore(store).Inventory.ItemCount);
    }

    [Fact]
    public void A_stale_writer_is_rejected_and_disjoint_sections_do_not_overwrite_each_other()
    {
        using TestDatabase database = new();
        HostDatabase store = database.Store;
        (PlayerInventory inventory, long version) = store.Read<PlayerInventory>(LocalPlayer.TenancyUserId, "inventory", InventoryStore.SerializationOptions);
        AccountStore account = new(store, TimeProvider.System);
        account.SetDisplayName("External Name");
        store.Save(LocalPlayer.TenancyUserId, "inventory", inventory with { Revision = 5 }, version, InventoryStore.SerializationOptions);
        Assert.Equal("External Name", account.Profile.DisplayName);
        Assert.Throws<PersistenceConflictException>(() => store.Save(LocalPlayer.TenancyUserId, "inventory", inventory, version, InventoryStore.SerializationOptions));
        Assert.Equal(5, database.Player["inventory"]["revision"].AsInt64);
    }

    [Fact]
    public void Running_stores_observe_external_updates_and_preserve_64_bit_ids()
    {
        using TestDatabase database = new();
        InventoryStore inventory = new(database.Store);
        inventory.InitializeLayout(DateTimeOffset.UnixEpoch);
        AccountStore account = new(database.Store, TimeProvider.System);
        database.Players.UpdateOne(FilterDefinition<BsonDocument>.Empty,
            Builders<BsonDocument>.Update.Set("account.profile.displayName", "Website Name")
                .Inc("accountVersion", 1L).Set("inventory.revision", 42L).Inc("inventoryVersion", 1L),
            cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal("Website Name", account.Profile.DisplayName);
        Assert.Equal(42, inventory.Snapshot.Revision);
        Assert.Equal(LocalPlayer.TenancyUserId, inventory.Snapshot.UserId);
        Assert.True(database.Player["inventory"]["revision"].IsInt64);
        inventory.InitializeLayout(DateTimeOffset.UnixEpoch);
        Assert.Equal(42, inventory.Snapshot.Revision);

        Assert.Equal("Website Name", account.Profile.DisplayName);
    }

    [Fact]
    public void Player_key_names_are_not_interpreted_as_Extended_JSON_type_markers()
    {
        using TestDatabase database = new();
        AccountStore account = new(database.Store, TimeProvider.System);
        (IReadOnlyList<PersistentPlayerKey>? updated, string? error) = account.SetKeys([new PersistentPlayerKeyWrite("$date", "not-a-date")]);
        Assert.Null(error);
        Assert.Single(updated!);
        Assert.Contains(new AccountStore(database.Store, TimeProvider.System).GetKeys(), key => key is { Key: "$date", Value: "not-a-date" });
        Assert.True(database.Player["account"]["persistentPlayerKeys"].IsBsonDocument);
    }

    [Fact]
    public void Invalid_versions_and_deleted_players_fail_instead_of_resetting()
    {
        using TestDatabase database = new();
        InventoryStore inventory = new(database.Store);
        database.Players.UpdateOne(FilterDefinition<BsonDocument>.Empty, Builders<BsonDocument>.Update.Set("inventoryVersion", -1L),
            cancellationToken: TestContext.Current.CancellationToken);
        Assert.Throws<InvalidDataException>(() => inventory.Snapshot);
        database.Players.DeleteMany(FilterDefinition<BsonDocument>.Empty, TestContext.Current.CancellationToken);
        Assert.Throws<InvalidDataException>(() => inventory.Snapshot);
    }
}
