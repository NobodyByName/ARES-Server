using Ares.Common.Game.Inventory;
using Ares.Host.Services.Inventory;
using Ares.Host.Services.Persistence;
using Ares.Structs.Backend.Progression;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Ares.Host.Tests;

public sealed class InventoryStoreTests : IDisposable
{
    private readonly TestDatabase _database = new();

    [Fact]
    public void Positive_stash_capacity_prevents_repeated_provisioning_even_after_items_are_removed()
    {
        InventoryStore store = Create();
        OwnedItem duck = Assert.Single(store.Inventory.Stash.Items);
        Assert.Equal(ItemTechnicalNames.RubberDuck, duck.Type);
        Assert.Equal(StorageItems.StashSlotCount, store.Inventory.Stash.SlotCount);
        Assert.Equal(PlayerInventory.Empty.Loadout.BackpackCapacity, store.Inventory.Loadout.BackpackCapacity);
        long revision = store.Inventory.Revision;
        store.InitializeLayout(DateTimeOffset.UnixEpoch.AddDays(1));
        Assert.Equal(revision, store.Inventory.Revision);
        PlayerInventory cleared = store.Inventory with { Stash = store.Inventory.Stash with { Items = [] } };
        store.Import(cleared, revision);
        InventoryStore restarted = new(_database.Store, TestDatabase.UserId);
        restarted.InitializeLayout(DateTimeOffset.UnixEpoch.AddDays(2));
        Assert.Equal(0, restarted.Inventory.ItemCount);
        Assert.Equal(StorageItems.StashSlotCount, restarted.Inventory.Stash.SlotCount);
        Assert.Equal(revision + 1, restarted.Inventory.Revision);
    }

    [Fact]
    public void Empty_slots_and_shared_definitions_are_generated_with_stable_ids_not_persisted()
    {
        InventoryStore store = Create();
        InventorySnapshot snapshot = store.Snapshot;
        Assert.Equal(227, snapshot.Items.Count);
        Assert.Equal(123, snapshot.Items.Count(item => item.GameAssetId == StorageItems.ContentSlotAssetId));
        Assert.Equal(1, store.Inventory.ItemCount);
        BsonDocument saved = _database.Account["inventory"].AsBsonDocument;
        Assert.False(saved.Contains("snapshot"));
        Assert.False(saved.Contains("definitions"));
        InventorySnapshot restarted = new InventoryStore(_database.Store, TestDatabase.UserId).Snapshot;
        Assert.True(InventorySnapshots.Same(new PlayerLoadout(snapshot.UserId, snapshot.Revision, snapshot.Items),
            new PlayerLoadout(restarted.UserId, restarted.Revision, restarted.Items)));
        Assert.DoesNotContain(InventorySnapshots.Loadout(snapshot).Items, item => item.GameAssetId == StorageItems.RubberDuckAssetId);
    }

    [Fact]
    public void Stash_capacity_can_grow_without_adding_stored_slot_objects()
    {
        InventoryStore store = Create();
        string itemId = Assert.Single(store.Inventory.Stash.Items).InstanceId;
        store.Import(store.Inventory with { Stash = store.Inventory.Stash with { SlotCount = 128 } }, store.Inventory.Revision);
        InventoryStore restarted = new(_database.Store, TestDatabase.UserId);
        Assert.Equal(128, restarted.Inventory.Stash.SlotCount);
        Assert.Equal(itemId, Assert.Single(restarted.Inventory.Stash.Items).InstanceId);
        InventoryItem stash = Assert.Single(restarted.Snapshot.Items, item => item.GameAssetId == StorageItems.StashContainerAssetId);
        Assert.Equal(128, stash.Slots.Count);
        ItemDescription definition = Assert.Single(restarted.GameData().ItemDescriptions.ItemDescriptions, item => item.GameAssetId == StorageItems.StashContainerAssetId);
        Assert.Equal(128, Assert.IsType<ItemSlotArray>(definition.SlotConfig).NumberOfSlots);
        Assert.Single(_database.Account["inventory"]["stash"]["items"].AsBsonArray);
    }

    [Fact]
    public void Saved_loadout_has_four_single_equipment_slots_and_three_item_containers()
    {
        InventoryStore store = Create();
        BsonDocument loadout = _database.Account["inventory"]["loadout"].AsBsonDocument;
        Assert.Equal(["augment", "backpack", "mainWeapon", "quickUse", "safePockets", "secondaryWeapon", "shield"], loadout.Names.Order(StringComparer.Ordinal));
        foreach (string slot in new[] { "augment", "shield", "mainWeapon", "secondaryWeapon" })
        {
            Assert.True(loadout[slot].IsBsonNull);
        }

        foreach (string container in new[] { "backpack", "quickUse", "safePockets" })
        {
            Assert.Equal(["items"], loadout[container].AsBsonDocument.Names.Order(StringComparer.Ordinal));
            Assert.Empty(loadout[container]["items"].AsBsonArray);
        }

        PlayerInventory state = store.Inventory;
        OwnedItem duck = Assert.Single(state.Stash.Items);
        state = state with { Stash = state.Stash with { Items = [] } };
        Assert.Throws<InvalidDataException>(() => store.Import(state with { Loadout = state.Loadout with { Augment = duck } }, state.Revision));
        Assert.Throws<InvalidDataException>(() => store.Import(state with { Loadout = state.Loadout with { Shield = duck } }, state.Revision));
        Assert.Throws<InvalidDataException>(() => store.Import(state with { Loadout = state.Loadout with { MainWeapon = duck } }, state.Revision));
        Assert.Throws<InvalidDataException>(() => store.Import(state with { Loadout = state.Loadout with { SecondaryWeapon = duck } }, state.Revision));
    }

    [Fact]
    public void Loadout_capacities_are_derived_from_shared_definitions_and_handoffs_store_only_items()
    {
        InventoryStore store = Create();
        foreach (long branch in new[] { LoadoutItems.BackpackSlotAssetId, LoadoutItems.BeltSlotAssetId, LoadoutItems.SafePocketSlotAssetId })
        {
            SingleItemSlot slot = Assert.IsType<SingleItemSlot>(InventoryCatalog.ByAsset[branch].SlotConfig);
            int capacity = checked((int)Assert.IsType<ItemSlotArray>(InventoryCatalog.ByAsset[slot.DefaultSlotAssetId].SlotConfig).NumberOfSlots);
            InventoryItem container = Assert.Single(store.Snapshot.Items, item => item.GameAssetId == slot.DefaultSlotAssetId);
            Assert.Equal(capacity, container.Slots.Count);
            ItemDescription definition = Assert.Single(store.GameData().ItemDescriptions.ItemDescriptions, item => item.GameAssetId == slot.DefaultSlotAssetId);
            Assert.Equal(capacity, Assert.IsType<ItemSlotArray>(definition.SlotConfig).NumberOfSlots);
        }

        string ticket = Guid.NewGuid().ToString("D");
        PlayerLoadout frozen = store.Reserve(ticket);
        InventoryStore restarted = new(_database.Store, TestDatabase.UserId);
        Assert.True(InventorySnapshots.Same(frozen, restarted.Reserve(ticket)));
        BsonDocument record = _database.Native.GetCollection<BsonDocument>("raidReservations")
            .Find(new BsonDocument("_id", ticket)).Single(TestContext.Current.CancellationToken);
        foreach (string container in new[] { "backpack", "quickUse", "safePockets" })
        {
            Assert.Equal(["items"], record["loadout"][container].AsBsonDocument.Names);
        }
    }

    [Fact]
    public void Items_must_fit_the_derived_loadout_capacity()
    {
        InventoryStore store = Create();
        int capacity = store.Inventory.Loadout.BackpackCapacity;
        OwnedItem duck = Assert.Single(store.Inventory.Stash.Items) with { Slot = capacity - 1 };
        PlayerLoadoutState loadout = store.Inventory.Loadout with { Backpack = new PlayerItemContainer([duck]) };
        store.Import(store.Inventory with { Stash = store.Inventory.Stash with { Items = [] }, Loadout = loadout }, store.Inventory.Revision);
        string saved = _database.InventoryJson;
        Assert.Throws<InvalidDataException>(() => store.Import(store.Inventory with
        {
            Loadout = loadout with { Backpack = new PlayerItemContainer([duck with { Slot = capacity }]) },
        }, store.Inventory.Revision));
        Assert.Equal(saved, _database.InventoryJson);
        Assert.Equal(capacity - 1, Assert.Single(new InventoryStore(_database.Store, TestDatabase.UserId).Inventory.Loadout.Backpack.Items).Slot);
    }

    [Fact]
    public void Reservations_survive_restarts_outside_the_player_inventory_and_release_atomically()
    {
        InventoryStore store = Create();
        string ticket = Guid.NewGuid().ToString("D");
        PlayerLoadout frozen = store.Reserve(ticket);
        InventoryStore restarted = new(_database.Store, TestDatabase.UserId);
        Assert.True(InventorySnapshots.Same(frozen, restarted.Reserve(ticket)));
        Assert.Equal(ticket, restarted.ReservedTicketId);
        BsonDocument reservation = Assert.Single(_database.Native.GetCollection<BsonDocument>("raidReservations").Find(FilterDefinition<BsonDocument>.Empty).ToList(TestContext.Current.CancellationToken));
        Assert.True(reservation.Contains("loadout"));
        Assert.False(_database.Account["inventory"].AsBsonDocument.Contains("reservation"));
        Assert.Throws<InvalidOperationException>(() => restarted.Reserve(Guid.NewGuid().ToString("D")));
        Assert.Throws<InvalidOperationException>(() => restarted.Import(restarted.Inventory, restarted.Inventory.Revision));
        restarted.Release(Guid.NewGuid().ToString("D"));
        Assert.Equal(ticket, restarted.ReservedTicketId);
        restarted.Release(ticket);
        Assert.Null(new InventoryStore(_database.Store, TestDatabase.UserId).ReservedTicketId);
        Assert.Equal(0, _database.Native.GetCollection<BsonDocument>("raidReservations").CountDocuments(FilterDefinition<BsonDocument>.Empty,
            cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public void A_failed_reservation_write_rolls_back_the_operational_record_too()
    {
        InventoryStore store = Create();
        _database.Native.RunCommand<BsonDocument>(new BsonDocument
        {
            ["collMod"] = "accounts",
            ["validator"] = new BsonDocument("inventory.raidTicketId", new BsonDocument("$exists", false)),
        }, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Throws<PersistenceException>(() => store.Reserve(Guid.NewGuid().ToString("D")));
        Assert.Null(store.ReservedTicketId);
        Assert.Equal(0, _database.Native.GetCollection<BsonDocument>("raidReservations").CountDocuments(FilterDefinition<BsonDocument>.Empty,
            cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public void Invalid_quantities_types_positions_and_stale_imports_leave_the_database_unchanged()
    {
        InventoryStore store = Create();
        PlayerInventory state = store.Inventory;
        OwnedItem duck = Assert.Single(state.Stash.Items);
        string saved = _database.InventoryJson;
        Assert.Throws<InvalidOperationException>(() => store.Import(state, state.Revision - 1));
        Assert.Throws<InvalidDataException>(() => store.Import(state with { Stash = state.Stash with { Items = [duck with { Quantity = 16 }] } }, state.Revision));
        Assert.Throws<InvalidDataException>(() => store.Import(state with { Stash = state.Stash with { Items = [duck with { Type = "UnknownItem" }] } }, state.Revision));
        Assert.Throws<InvalidDataException>(() => store.Import(state with { Stash = state.Stash with { Items = [duck with { Slot = 0 }] } }, state.Revision));
        Assert.Throws<InvalidDataException>(() => store.Import(state with { Loadout = state.Loadout with { Backpack = new PlayerItemContainer([duck with { Slot = 99 }]) } }, state.Revision));
        Assert.Throws<InvalidDataException>(() => store.Import(state with { Stash = state.Stash with { Items = [duck, duck] } }, state.Revision));
        Assert.Equal(saved, _database.InventoryJson);
    }

    [Fact]
    public void A_rejected_database_write_does_not_publish_the_new_state()
    {
        InventoryStore store = Create();
        long revision = store.Inventory.Revision;
        _database.Native.RunCommand<BsonDocument>(new BsonDocument
        {
            ["collMod"] = "accounts",
            ["validator"] = new BsonDocument("inventory.revision", revision),
        }, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Throws<PersistenceException>(() => store.Import(store.Inventory with { Stash = store.Inventory.Stash with { SlotCount = 128 } }, revision));
        Assert.Equal(revision, store.Inventory.Revision);
        Assert.Equal(StorageItems.StashSlotCount, store.Inventory.Stash.SlotCount);
    }

    [Fact]
    public void Invalid_documents_fail_instead_of_resetting_player_data()
    {
        InventoryStore store = Create();
        BsonDocument inventory = _database.Account["inventory"].AsBsonDocument;
        inventory["stash"]["items"][0]["type"] = "unknown";
        _database.ReplaceInventory(inventory.ToJson());
        Assert.Throws<InvalidDataException>(() => new InventoryStore(_database.Store, TestDatabase.UserId));
        Assert.Throws<InvalidDataException>(() => store.Snapshot);
    }

    [Fact]
    public void Settle_extracted_loot_lands_in_the_backpack_and_releases_the_reservation_once()
    {
        InventoryStore store = Equipped();
        store.AddItem(ItemTechnicalNames.RubberDuck, DateTimeOffset.UnixEpoch.AddDays(1));
        string ticket = Guid.NewGuid().ToString("D");
        PlayerLoadout reserved = store.Reserve(ticket);
        PlayerInventory before = store.Inventory;
        OwnedItem augment = before.Loadout.Augment!;
        CarriedItem duck = NewDuck(CarriedPlaces.Backpack, 3);
        DateTimeOffset now = DateTimeOffset.UnixEpoch.AddDays(3);
        RaidResult result = new(ticket, TestDatabase.UserId, reserved.Revision, RaidOutcomes.Extracted,
            [Carried(CarriedPlaces.Augment, 0, augment), Carried(CarriedPlaces.SafePockets, 0, before.Loadout.SafePockets.Items[0]), duck]);

        Assert.Equal(RaidSettlement.Saved, store.Settle(result, now));

        InventoryStore restarted = new(_database.Store, TestDatabase.UserId);
        PlayerInventory after = restarted.Inventory;
        Assert.Equal(before.Revision + 1, after.Revision);
        Assert.Null(after.RaidTicketId);
        Assert.Equal(before.Stash.SlotCount, after.Stash.SlotCount);
        Assert.Equal(before.Stash.Items, after.Stash.Items);
        Assert.Equal(augment, after.Loadout.Augment);
        Assert.Equal(before.Loadout.SafePockets.Items, after.Loadout.SafePockets.Items);
        OwnedItem looted = Assert.Single(after.Loadout.Backpack.Items);
        Assert.Equal(duck.Item.InstanceId, looted.InstanceId);
        Assert.Equal(ItemTechnicalNames.RubberDuck, looted.Type);
        Assert.Equal(3, looted.Slot);
        Assert.Equal(now.ToUnixTimeMilliseconds(), looted.UpdatedAt);
        Assert.NotEqual(augment.ETag, looted.ETag);
        Assert.Equal(0, ReservationCount());

        string saved = _database.InventoryJson;
        Assert.Equal(RaidSettlement.NotReserved, restarted.Settle(result, now.AddSeconds(1)));
        Assert.Equal(saved, _database.InventoryJson);
    }

    [Fact]
    public void Settle_rejects_a_stale_revision_and_keeps_the_reservation()
    {
        InventoryStore store = Equipped();
        string ticket = Guid.NewGuid().ToString("D");
        PlayerLoadout reserved = store.Reserve(ticket);
        string saved = _database.InventoryJson;
        RaidResult result = new(ticket, TestDatabase.UserId, reserved.Revision - 1, RaidOutcomes.Extracted, [NewDuck(CarriedPlaces.Backpack, 0)]);
        Assert.Throws<InvalidDataException>(() => store.Settle(result, DateTimeOffset.UnixEpoch));
        Assert.Equal(saved, _database.InventoryJson);
        Assert.Equal(ticket, store.ReservedTicketId);
        Assert.Equal(1, ReservationCount());
    }

    [Theory]
    [InlineData("unsupported asset")]
    [InlineData("unsupported place")]
    [InlineData("over capacity")]
    [InlineData("over max stack")]
    [InlineData("stash id")]
    public void Settle_rejects_items_that_do_not_fit_and_saves_nothing(string problem)
    {
        InventoryStore store = Create();
        string ticket = Guid.NewGuid().ToString("D");
        PlayerLoadout reserved = store.Reserve(ticket);
        OwnedItem stashDuck = Assert.Single(store.Inventory.Stash.Items);
        CarriedItem duck = NewDuck(CarriedPlaces.Backpack, 0);
        CarriedItem invalid = problem switch
        {
            "unsupported asset" => duck with { Item = duck.Item with { GameAssetId = StorageItems.StashContainerAssetId } },
            "unsupported place" => duck with { Place = CarriedPlaces.Augment },
            "over capacity" => duck with { Slot = store.Inventory.Loadout.BackpackCapacity },
            "over max stack" => duck with { Item = duck.Item with { Amount = InventoryCatalog.ByAsset[StorageItems.RubberDuckAssetId].MaxAmount + 1 } },
            _ => Carried(CarriedPlaces.Backpack, 0, stashDuck),
        };
        string saved = _database.InventoryJson;
        RaidResult result = new(ticket, TestDatabase.UserId, reserved.Revision, RaidOutcomes.Extracted, [invalid]);
        Assert.Throws<InvalidDataException>(() => store.Settle(result, DateTimeOffset.UnixEpoch));
        Assert.Equal(saved, _database.InventoryJson);
        Assert.Equal(ticket, new InventoryStore(_database.Store, TestDatabase.UserId).ReservedTicketId);
        Assert.Equal(1, ReservationCount());
    }

    [Fact]
    public void Settle_a_lost_raid_keeps_only_the_safe_pocket_items()
    {
        InventoryStore store = Equipped();
        string ticket = Guid.NewGuid().ToString("D");
        PlayerLoadout reserved = store.Reserve(ticket);
        OwnedItem pocketDuck = Assert.Single(store.Inventory.Loadout.SafePockets.Items);
        RaidResult result = new(ticket, TestDatabase.UserId, reserved.Revision, RaidOutcomes.Lost, [Carried(CarriedPlaces.SafePockets, 0, pocketDuck)]);
        Assert.Equal(RaidSettlement.Saved, store.Settle(result, DateTimeOffset.UnixEpoch.AddDays(3)));
        PlayerLoadoutState loadout = new InventoryStore(_database.Store, TestDatabase.UserId).Inventory.Loadout;
        Assert.Null(loadout.Augment);
        Assert.Empty(loadout.Backpack.Items);
        Assert.Equal(pocketDuck, Assert.Single(loadout.SafePockets.Items));
    }

    [Fact]
    public void An_empty_loadout_is_reserved_and_settles()
    {
        InventoryStore store = Create();
        Assert.Equal(store.Inventory.Stash.Items.Count, store.Inventory.ItemCount);
        string ticket = Guid.NewGuid().ToString("D");
        PlayerLoadout reserved = store.Reserve(ticket);
        Assert.Equal(ticket, store.ReservedTicketId);
        Assert.Equal(1, ReservationCount());
        RaidResult result = new(ticket, TestDatabase.UserId, reserved.Revision, RaidOutcomes.Extracted, [NewDuck(CarriedPlaces.QuickUse, 1)]);
        Assert.Equal(RaidSettlement.Saved, store.Settle(result, DateTimeOffset.UnixEpoch.AddDays(3)));
        PlayerInventory after = new InventoryStore(_database.Store, TestDatabase.UserId).Inventory;
        Assert.Null(after.RaidTicketId);
        Assert.Equal(reserved.Revision + 1, after.Revision);
        Assert.Equal(1, Assert.Single(after.Loadout.QuickUse.Items).Slot);
        Assert.Equal(0, ReservationCount());
    }

    private InventoryStore Equipped()
    {
        InventoryStore store = Create();
        store.AddItem(ItemTechnicalNames.CombatMk1, DateTimeOffset.UnixEpoch);
        PlayerInventory inventory = store.Inventory;
        OwnedItem augment = Assert.Single(inventory.Stash.Items, item => item.Type == ItemTechnicalNames.CombatMk1);
        OwnedItem duck = Assert.Single(inventory.Stash.Items, item => item.Type == ItemTechnicalNames.RubberDuck);
        store.Import(inventory with
        {
            Stash = inventory.Stash with { Items = [] },
            Loadout = inventory.Loadout with { Augment = augment, SafePockets = new PlayerItemContainer([duck with { Slot = 0 }]) },
        }, inventory.Revision);
        return store;
    }

    private static CarriedItem Carried(string place, int slot, OwnedItem item) => new(place, slot,
        new InventoryItem(item.InstanceId, ItemTechnicalNames.AssetIds[item.Type], item.Quantity, [], item.Durability, item.MaxDurability, "", item.ETag, item.UpdatedAt));

    private static CarriedItem NewDuck(string place, int slot) => new(place, slot,
        new InventoryItem(Guid.NewGuid().ToString("D"), StorageItems.RubberDuckAssetId, 1, [], StorageItems.InitialDurability,
            StorageItems.InitialMaxDurability, "", "", 0));

    private long ReservationCount() => _database.Native.GetCollection<BsonDocument>("raidReservations")
        .CountDocuments(FilterDefinition<BsonDocument>.Empty, cancellationToken: TestContext.Current.CancellationToken);

    private InventoryStore Create()
    {
        InventoryStore store = new(_database.Store, TestDatabase.UserId);
        store.InitializeLayout(DateTimeOffset.UnixEpoch);
        return store;
    }

    public void Dispose() => _database.Dispose();
}
