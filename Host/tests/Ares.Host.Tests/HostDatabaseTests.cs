using Ares.Host.Services.Accounts;
using Ares.Host.Services.Inventory;
using Ares.Host.Services.Persistence;
using MongoDB.Bson;

namespace Ares.Host.Tests;

public sealed class HostDatabaseTests
{
    [Fact]
    public void Sections_have_independent_versions_and_reject_stale_updates()
    {
        using TestDatabase database = new();
        HostDatabase store = database.Store;
        (PlayerInventory inventory, long inventoryVersion) = store.Read<PlayerInventory>(TestDatabase.UserId, "inventory", InventoryStore.SerializationOptions);
        (AccountStateDocument account, long accountVersion) = store.Read<AccountStateDocument>(TestDatabase.UserId, "settings", AccountStore.SerializationOptions);
        store.Save(TestDatabase.UserId, "settings", account with { Revision = 3 }, accountVersion, AccountStore.SerializationOptions);
        store.Save(TestDatabase.UserId, "inventory", inventory with { Revision = 7 }, inventoryVersion, InventoryStore.SerializationOptions);
        Assert.Equal(3, database.Account["settings"]["revision"].AsInt64);
        Assert.Equal(7, database.Account["inventory"]["revision"].AsInt64);
        Assert.Throws<PersistenceConflictException>(() => store.Save(TestDatabase.UserId, "inventory", inventory, inventoryVersion, InventoryStore.SerializationOptions));
        string saved = database.Account.ToJson();
        store.Initialize();
        Assert.Equal(saved, database.Account.ToJson());
    }

    [Theory]
    [InlineData("not-a-uri", "ares_host")]
    [InlineData("mongodb://localhost", "admin")]
    [InlineData("mongodb://localhost", "with.dot")]
    [InlineData("mongodb://localhost", "")]
    public void Invalid_connection_settings_are_rejected(string uri, string name)
    {
        Assert.Throws<ArgumentException>(() => HostDatabase.ValidateConfiguration(uri, name));
    }
}
