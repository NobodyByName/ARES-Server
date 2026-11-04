using Ares.Host.Services.Accounts;
using Ares.Host.Services.Content;
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
        (PlayerInventory inventory, long inventoryVersion) = store.Read<PlayerInventory>(LocalPlayer.TenancyUserId, "inventory", InventoryStore.SerializationOptions);
        (AccountStateDocument account, long accountVersion) = store.Read<AccountStateDocument>(LocalPlayer.TenancyUserId, "account", AccountStore.SerializationOptions);
        store.Save(LocalPlayer.TenancyUserId, "account", account with { Profile = new AccountProfileOverrides("Updated Name", 0) }, accountVersion, AccountStore.SerializationOptions);
        store.Save(LocalPlayer.TenancyUserId, "inventory", inventory with { Revision = 7 }, inventoryVersion, InventoryStore.SerializationOptions);
        Assert.Equal("Updated Name", database.Player["account"]["profile"]["displayName"].AsString);
        Assert.Equal(7, database.Player["inventory"]["revision"].AsInt64);
        Assert.Throws<PersistenceConflictException>(() => store.Save(LocalPlayer.TenancyUserId, "inventory", inventory, inventoryVersion, InventoryStore.SerializationOptions));
        string saved = database.Player.ToJson();
        store.Initialize();
        Assert.Equal(saved, database.Player.ToJson());
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
