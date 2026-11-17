using Ares.Common.Game.Inventory;
using Ares.Host.Services.Inventory;
using Ares.Structs.Backend.Progression;
using Ares.Structs.Game.Inventory;

namespace Ares.Host.Tests;

public sealed class PlainItemTests : IDisposable
{
    private readonly TestDatabase _database = new();

    [Theory]
    [InlineData("DA_Item_AdrenalinShot")]
    [InlineData("DA_Item_BasicStickyGrenade")]
    [InlineData("DA_Item_WeaponMod_MagazineLight_Extended_01")]
    [InlineData("DA_Item_OutpostFurnitureRecipe_BarStool_01_A")]
    [InlineData("DA_Item_Salvage_Sensors")]
    public void The_operator_adds_a_catalogue_item_and_game_data_describes_it(string type)
    {
        UsableItem item = UsableItems.ByTechnicalName[type];
        InventoryStore store = Create();
        store.AddItem(type, DateTimeOffset.UnixEpoch, item.MaxStack);
        OwnedItem stack = Assert.Single(store.Inventory.Stash.Items, owned => owned.Type == type);
        InventoryItem owned = Assert.Single(store.Snapshot.Items, entry => entry.InstanceId == stack.InstanceId);
        Assert.Equal((item.AssetId, (long)item.MaxStack), (owned.GameAssetId, owned.Amount));
        ItemDescription description = Assert.Single(store.GameData().ItemDescriptions.ItemDescriptions, entry => entry.GameAssetId == item.AssetId);
        Assert.Equal(((long)item.MaxStack, item.Stackable), (description.MaxAmount, description.Stackable));
        Assert.Equal(200, store.Commit(Move(stack.InstanceId, Slots(store, LoadoutItems.BackpackSlotAssetId)[0], "backpack"), DateTimeOffset.UnixEpoch).StatusCode);
    }

    [Fact]
    public void Weapons_without_their_own_support_cannot_be_added()
    {
        InventoryStore store = Create();
        string saved = _database.InventoryJson;
        Assert.Throws<InvalidDataException>(() => store.AddItem("DA_Item_SMG_01", DateTimeOffset.UnixEpoch));
        Assert.Equal(saved, _database.InventoryJson);
    }

    [Fact]
    public void A_consumable_fits_a_quick_use_slot_and_a_found_item_settles_home_after_an_extraction()
    {
        InventoryStore store = Create();
        store.AddItem("DA_Item_AdrenalinShot", DateTimeOffset.UnixEpoch, 3);
        OwnedItem shots = store.Inventory.Stash.Items.Single(item => item.Type == "DA_Item_AdrenalinShot");
        Assert.Equal(200, store.Commit(Move(shots.InstanceId, Slots(store, LoadoutItems.BeltSlotAssetId)[0], "quick-use"), DateTimeOffset.UnixEpoch).StatusCode);

        string ticket = Guid.NewGuid().ToString("D");
        PlayerLoadout reserved = store.Reserve(ticket);
        InventoryItem carried = reserved.Items.Single(item => item.InstanceId == shots.InstanceId);
        long sensors = UsableItems.ByTechnicalName["DA_Item_Salvage_Sensors"].AssetId;
        InventoryItem found = new(Guid.NewGuid().ToString("D"), sensors, 2, [], 1, 1, "", "", 0);
        RaidResult result = new(ticket, TestDatabase.UserId, reserved.Revision, RaidOutcomes.Extracted,
            [new CarriedItem(CarriedPlaces.QuickUse, 0, carried with { Slots = [], Amount = 1 }), new CarriedItem(CarriedPlaces.Backpack, 3, found)]);
        Assert.Equal(RaidSettlement.Saved, store.Settle(result, DateTimeOffset.UnixEpoch.AddDays(1)));

        PlayerInventory home = new InventoryStore(_database.Store, TestDatabase.UserId).Inventory;
        OwnedItem shot = Assert.Single(home.Loadout.QuickUse.Items);
        Assert.Equal<(string, string, long)>((shots.InstanceId, "DA_Item_AdrenalinShot", 1), (shot.InstanceId, shot.Type, shot.Quantity));
        OwnedItem loot = Assert.Single(home.Loadout.Backpack.Items);
        Assert.Equal<(string, string, long, int?)>((found.InstanceId, "DA_Item_Salvage_Sensors", 2, 3), (loot.InstanceId, loot.Type, loot.Quantity, loot.Slot));
    }

    [Fact]
    public void Container_loot_of_several_kinds_and_partial_stacks_settles_home_after_an_extraction()
    {
        InventoryStore store = Create();
        string ticket = Guid.NewGuid().ToString("D");
        PlayerLoadout reserved = store.Reserve(ticket);
        (string Type, long Amount)[] found = [("DA_Item_Salvage_ScrapMetal", 5), ("DA_Item_LightAmmo", 10), ("DA_Item_WeaponMod_Muzzle_Brake_01", 1), ("DA_Item_Consumable_Mushroom", 1)];
        CarriedItem[] carried =
        [
            .. found.Select((entry, slot) => new CarriedItem(CarriedPlaces.Backpack, slot,
                new InventoryItem(Guid.NewGuid().ToString("D"), UsableItems.ByTechnicalName[entry.Type].AssetId, entry.Amount, [], 1, 1, "", "", 0))),
        ];
        RaidResult result = new(ticket, TestDatabase.UserId, reserved.Revision, RaidOutcomes.Extracted, carried);
        Assert.Equal(RaidSettlement.Saved, store.Settle(result, DateTimeOffset.UnixEpoch.AddDays(1)));

        PlayerInventory home = new InventoryStore(_database.Store, TestDatabase.UserId).Inventory;
        Assert.Equal(found.Select((entry, slot) => (entry.Type, entry.Amount, (int?)slot)),
            home.Loadout.Backpack.Items.OrderBy(item => item.Slot).Select(item => (item.Type, item.Quantity, item.Slot)));
    }

    private InventoryStore Create()
    {
        InventoryStore store = new(_database.Store, TestDatabase.UserId);
        store.InitializeLayout(DateTimeOffset.UnixEpoch);
        return store;
    }

    private static List<string> Slots(InventoryStore store, long branchAsset)
    {
        InventorySnapshot snapshot = store.Snapshot;
        string frameId = snapshot.Items.Single(item => item.GameAssetId == LoadoutItems.FrameSlotAssetId).Slots[0];
        InventoryItem frame = snapshot.Items.Single(item => item.InstanceId == frameId);
        InventoryItem branch = snapshot.Items.Single(item => frame.Slots.Contains(item.InstanceId) && LoadoutDefinitions.Generated.StandardBranch(item.GameAssetId) == branchAsset);
        return [.. snapshot.Items.Single(item => item.InstanceId == branch.Slots[0]).Slots];
    }

    private static InventoryOperationsRequest Move(string item, string target, string id) => new([new SetItemParents(item, [target])], id);

    public void Dispose() => _database.Dispose();
}
