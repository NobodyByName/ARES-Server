using Ares.Host.Services.Accounts;
using Ares.Host.Services.Inventory;
using Ares.Host.Services.Persistence;
using Ares.Structs.Backend.Accounts;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Ares.Host.Tests;

public sealed class PersistenceTests
{
    [Fact]
    public void Initialization_leaves_existing_player_data_unchanged()
    {
        using TestDatabase database = new();
        HostDatabase store = database.Store;
        InventoryStore inventory = new(store, TestDatabase.UserId);
        inventory.InitializeLayout(DateTimeOffset.UnixEpoch);
        string saved = database.Account.ToJson();
        store.Initialize();
        Assert.Equal(saved, database.Account.ToJson());
        Assert.Equal(1, new InventoryStore(store, TestDatabase.UserId).Inventory.ItemCount);
    }

    [Fact]
    public void A_stale_writer_is_rejected_and_disjoint_sections_do_not_overwrite_each_other()
    {
        using TestDatabase database = new();
        HostDatabase store = database.Store;
        (PlayerInventory inventory, long version) = store.Read<PlayerInventory>(TestDatabase.UserId, "inventory", InventoryStore.SerializationOptions);
        AccountStore account = new(store, TestDatabase.UserId);
        account.SetKeys([new PersistentPlayerKeyWrite("UI.External", "1")]);
        store.Save(TestDatabase.UserId, "inventory", inventory with { Revision = 5 }, version, InventoryStore.SerializationOptions);
        Assert.Contains(account.GetKeys(), key => key.Key == "UI.External");
        Assert.Throws<PersistenceConflictException>(() => store.Save(TestDatabase.UserId, "inventory", inventory, version, InventoryStore.SerializationOptions));
        Assert.Equal(5, database.Account["inventory"]["revision"].AsInt64);
    }

    [Fact]
    public void Running_stores_observe_external_updates_and_preserve_64_bit_ids()
    {
        using TestDatabase database = new();
        InventoryStore inventory = new(database.Store, TestDatabase.UserId);
        inventory.InitializeLayout(DateTimeOffset.UnixEpoch);
        AccountStore account = new(database.Store, TestDatabase.UserId);
        database.Accounts.UpdateOne(FilterDefinition<BsonDocument>.Empty,
            Builders<BsonDocument>.Update.Set("settings.persistentPlayerKeys.Website", new BsonDocument { ["value"] = "1", ["etag"] = "web" })
                .Inc("settingsVersion", 1L).Set("inventory.revision", 42L).Inc("inventoryVersion", 1L),
            cancellationToken: TestContext.Current.CancellationToken);
        Assert.Contains(account.GetKeys(), key => key.ETag == "web");
        Assert.Equal(42, inventory.Snapshot.Revision);
        Assert.Equal(TestDatabase.UserId, inventory.Snapshot.UserId);
        Assert.True(database.Account["inventory"]["revision"].IsInt64);
        inventory.InitializeLayout(DateTimeOffset.UnixEpoch);
        Assert.Equal(42, inventory.Snapshot.Revision);
    }

    [Fact]
    public void Matchmaking_preferences_round_trip_in_memory_without_database_writes()
    {
        using TestDatabase database = new();
        AccountStore account = new(database.Store, TestDatabase.UserId);
        string saved = database.Account.ToJson();
        account.SetKeys([
            new PersistentPlayerKeyWrite(MatchmakingPreferenceKeys.SearchSettings, "{\"generatedLoadout\":false}"),
            new PersistentPlayerKeyWrite(MatchmakingPreferenceKeys.MapSelection, "GameMode.Location.TheDam|false"),
        ]);
        Assert.Equal(saved, database.Account.ToJson());
        Assert.Contains(account.GetKeys(), entry => entry.Key == MatchmakingPreferenceKeys.SearchSettings);
        Assert.DoesNotContain(new AccountStore(database.Store, TestDatabase.UserId).GetKeys(), entry => entry.Key == MatchmakingPreferenceKeys.SearchSettings);
        account.SetKeys([new PersistentPlayerKeyWrite("UI.Test", "stored"), new PersistentPlayerKeyWrite(MatchmakingPreferenceKeys.MapSelection, "other")]);
        Assert.Contains(new AccountStore(database.Store, TestDatabase.UserId).GetKeys(), entry => entry is { Key: "UI.Test", Value: "stored" });
        Assert.False(database.Account["settings"]["persistentPlayerKeys"].AsBsonDocument.Contains(MatchmakingPreferenceKeys.MapSelection));
    }

    [Fact]
    public void Player_key_names_are_not_interpreted_as_Extended_JSON_type_markers()
    {
        using TestDatabase database = new();
        AccountStore account = new(database.Store, TestDatabase.UserId);
        (IReadOnlyList<PersistentPlayerKey>? updated, string? error) = account.SetKeys([new PersistentPlayerKeyWrite("$date", "not-a-date")]);
        Assert.Null(error);
        Assert.Single(updated!);
        Assert.Contains(new AccountStore(database.Store, TestDatabase.UserId).GetKeys(), key => key is { Key: "$date", Value: "not-a-date" });
        Assert.True(database.Account["settings"]["persistentPlayerKeys"].IsBsonDocument);
    }

    [Fact]
    public void Invalid_versions_and_deleted_players_fail_instead_of_resetting()
    {
        using TestDatabase database = new();
        InventoryStore inventory = new(database.Store, TestDatabase.UserId);
        database.Accounts.UpdateOne(FilterDefinition<BsonDocument>.Empty, Builders<BsonDocument>.Update.Set("inventoryVersion", -1L),
            cancellationToken: TestContext.Current.CancellationToken);
        Assert.Throws<InvalidDataException>(() => inventory.Snapshot);
        database.Accounts.DeleteMany(FilterDefinition<BsonDocument>.Empty, TestContext.Current.CancellationToken);
        Assert.Throws<InvalidDataException>(() => inventory.Snapshot);
    }
}
