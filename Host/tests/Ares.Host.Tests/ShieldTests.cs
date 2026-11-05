using Ares.Common.Game.Inventory;
using Ares.Host.Services;
using Ares.Host.Services.Inventory;
using Ares.Structs.Backend.Progression;

namespace Ares.Host.Tests;

public sealed class ShieldTests : IDisposable
{
    private readonly TestDatabase _database = new();

    [Theory]
    [InlineData(null)]
    [InlineData(ItemTechnicalNames.CombatMk1)]
    [InlineData(ItemTechnicalNames.LootingMk1)]
    [InlineData(ItemTechnicalNames.TacticalMk1)]
    public void A_light_shield_can_be_equipped_persisted_reserved_and_removed_with_each_frame(string? augmentType)
    {
        InventoryStore store = Create();
        if (augmentType is not null)
        {
            store.AddItem(augmentType, DateTimeOffset.UnixEpoch);
            OwnedItem augment = store.Inventory.Stash.Items.Single(item => item.Type == augmentType);
            Assert.Equal(200, store.Commit(Move(augment.InstanceId, store.Snapshot.Items.Single(item => item.GameAssetId == AugmentItems.AugmentContentSlotAssetId).InstanceId, "augment"), DateTimeOffset.UnixEpoch).StatusCode);
        }

        OwnedItem shield = store.Inventory.Stash.Items.Single(item => item.Type == ItemTechnicalNames.LightShield);
        InventorySnapshot before = store.Snapshot;
        ServiceReply reply = store.Commit(Move(shield.InstanceId, Slot(store, LoadoutItems.ArmorSlotAssetId), "equip"), DateTimeOffset.UnixEpoch);
        Assert.Equal(200, reply.StatusCode);
        AssertChanges(before, reply, store.Snapshot);
        Assert.Equal(shield.InstanceId, store.Inventory.Loadout.Shield!.InstanceId);
        Assert.DoesNotContain(store.Inventory.Stash.Items, item => item.InstanceId == shield.InstanceId);
        InventoryStore restarted = new(_database.Store);
        Assert.Equal(store.Inventory.Loadout.Shield, restarted.Inventory.Loadout.Shield);
        string ticket = Guid.NewGuid().ToString("D");
        PlayerLoadout handoff = restarted.Reserve(ticket);
        InventoryItem carried = Assert.Single(handoff.Items, item => item.GameAssetId == ShieldItems.LightShieldAssetId);
        Assert.Equal(shield.InstanceId, carried.InstanceId);
        Assert.Equal(shield.Durability, carried.Durability);
        Assert.Equal(shield.MaxDurability, carried.MaxDurability);
        Assert.Equal(409, restarted.Commit(Move(shield.InstanceId, StashSlot(restarted), "locked"), DateTimeOffset.UnixEpoch).StatusCode);
        restarted.Release(ticket);
        Assert.Equal(200, restarted.Commit(Move(shield.InstanceId, StashSlot(restarted), "remove"), DateTimeOffset.UnixEpoch).StatusCode);
        Assert.Null(restarted.Inventory.Loadout.Shield);
        Assert.Contains(restarted.Inventory.Stash.Items, item => item.InstanceId == shield.InstanceId);
    }

    [Fact]
    public void Shield_swaps_are_atomic_idempotent_and_preserve_durability()
    {
        InventoryStore store = Create();
        OwnedItem first = store.Inventory.Stash.Items.Single(item => item.Type == ItemTechnicalNames.LightShield);
        store.AddItem(ItemTechnicalNames.LightShield, DateTimeOffset.UnixEpoch);
        OwnedItem second = store.Inventory.Stash.Items.Single(item => item.Type == ItemTechnicalNames.LightShield && item.InstanceId != first.InstanceId);
        Assert.Equal(200, store.Commit(Move(first.InstanceId, Slot(store, LoadoutItems.ArmorSlotAssetId), "equip"), DateTimeOffset.UnixEpoch).StatusCode);
        string saved = _database.InventoryJson;
        Assert.Equal(400, store.Commit(Move(second.InstanceId, Slot(store, LoadoutItems.ArmorSlotAssetId), "occupied"), DateTimeOffset.UnixEpoch).StatusCode);
        Assert.Equal(saved, _database.InventoryJson);
        InventorySnapshot before = store.Snapshot;
        InventoryOperationsRequest swap = new([new SetItemParents(second.InstanceId, [Slot(store, LoadoutItems.ArmorSlotAssetId)]), new SetItemParents(first.InstanceId, [StashSlot(store)])], "swap");
        ServiceReply reply = store.Commit(swap, DateTimeOffset.UnixEpoch);
        Assert.Equal(200, reply.StatusCode);
        AssertChanges(before, reply, store.Snapshot);
        Assert.Equal(second.InstanceId, store.Inventory.Loadout.Shield!.InstanceId);
        Assert.Equal(first.Durability, store.Inventory.Stash.Items.Single(item => item.InstanceId == first.InstanceId).Durability);
        long revision = store.Inventory.Revision;
        Assert.Equal(200, new InventoryStore(_database.Store).Commit(swap, DateTimeOffset.UnixEpoch).StatusCode);
        Assert.Equal(revision, store.Inventory.Revision);
    }

    [Fact]
    public void An_augment_change_migrates_the_equipped_shield_to_its_new_frame()
    {
        InventoryStore store = Create();
        OwnedItem shield = store.Inventory.Stash.Items.Single(item => item.Type == ItemTechnicalNames.LightShield);
        Assert.Equal(200, store.Commit(Move(shield.InstanceId, Slot(store, LoadoutItems.ArmorSlotAssetId), "shield"), DateTimeOffset.UnixEpoch).StatusCode);
        store.AddItem(ItemTechnicalNames.CombatMk1, DateTimeOffset.UnixEpoch);
        OwnedItem augment = store.Inventory.Stash.Items.Single(item => item.Type == ItemTechnicalNames.CombatMk1);
        string oldSlot = Slot(store, LoadoutItems.ArmorSlotAssetId);
        string nextSlot = Slot(store, LoadoutItems.ArmorSlotAssetId, AugmentItems.FrameAssetIds[AugmentItems.CombatMk1AssetId]);
        InventoryOperationsRequest migrate = new([new SetItemParents(shield.InstanceId, [nextSlot]), new SetItemParents(augment.InstanceId, [store.Snapshot.Items.Single(item => item.GameAssetId == AugmentItems.AugmentContentSlotAssetId).InstanceId])], "migrate");
        Assert.Equal(200, store.Commit(migrate, DateTimeOffset.UnixEpoch).StatusCode);
        Assert.Equal(shield.InstanceId, store.Inventory.Loadout.Shield!.InstanceId);
        Assert.Empty(store.Snapshot.Items.Single(item => item.InstanceId == oldSlot).Slots);
        Assert.Equal([shield.InstanceId], store.Snapshot.Items.Single(item => item.InstanceId == nextSlot).Slots);
    }

    [Fact]
    public void Invalid_equipment_and_unsupported_shields_leave_persistence_unchanged()
    {
        InventoryStore store = Create();
        OwnedItem shield = store.Inventory.Stash.Items.Single(item => item.Type == ItemTechnicalNames.LightShield);
        OwnedItem duck = store.Inventory.Stash.Items.Single(item => item.Type == ItemTechnicalNames.RubberDuck);
        string saved = _database.InventoryJson;
        Assert.Equal(400, store.Commit(Move(duck.InstanceId, Slot(store, LoadoutItems.ArmorSlotAssetId), "duck"), DateTimeOffset.UnixEpoch).StatusCode);
        Assert.Equal(400, store.Commit(Move(shield.InstanceId, Slot(store, LoadoutItems.BeltSlotAssetId), "belt"), DateTimeOffset.UnixEpoch).StatusCode);
        Assert.Equal(400, store.Commit(Move(shield.InstanceId, Slot(store, LoadoutItems.Firearm1SlotAssetId), "weapon"), DateTimeOffset.UnixEpoch).StatusCode);
        foreach (string type in new[] { "DA_Item_Armor_MediumBody", "DA_Item_Armor_HeavyBody" })
        {
            Assert.Throws<InvalidDataException>(() => store.AddItem(type, DateTimeOffset.UnixEpoch));
            Assert.Throws<InvalidDataException>(() => store.Import(store.Inventory with { Loadout = store.Inventory.Loadout with { Shield = shield with { Type = type } } }, store.Inventory.Revision));
        }

        Assert.Equal(saved, _database.InventoryJson);
        Assert.Equal(200, store.Commit(Move(shield.InstanceId, Slot(store, LoadoutItems.BackpackSlotAssetId), "carry"), DateTimeOffset.UnixEpoch).StatusCode);
        Assert.Null(store.Inventory.Loadout.Shield);
        Assert.Equal(shield.InstanceId, Assert.Single(store.Inventory.Loadout.Backpack.Items).InstanceId);
    }

    private InventoryStore Create()
    {
        InventoryStore store = new(_database.Store);
        store.InitializeLayout(DateTimeOffset.UnixEpoch);
        store.AddItem(ItemTechnicalNames.LightShield, DateTimeOffset.UnixEpoch);
        return store;
    }

    private static string Slot(InventoryStore store, long branchAsset, long? frameAsset = null)
    {
        InventorySnapshot snapshot = store.Snapshot;
        string frameId = snapshot.Items.Single(item => item.GameAssetId == LoadoutItems.FrameSlotAssetId).Slots[0];
        InventoryItem frame = frameAsset is { } asset ? snapshot.Items.Single(item => item.GameAssetId == asset) : snapshot.Items.Single(item => item.InstanceId == frameId);
        InventoryItem branch = snapshot.Items.Single(item => frame.Slots.Contains(item.InstanceId) && LoadoutDefinitions.StandardBranch(item.GameAssetId) == branchAsset);
        return snapshot.Items.Single(item => item.InstanceId == branch.Slots[0]).Slots[0];
    }

    private static string StashSlot(InventoryStore store) => store.Snapshot.Items.Single(item => item.GameAssetId == StorageItems.StashContainerAssetId).Slots[0];
    private static InventoryOperationsRequest Move(string item, string target, string id) => new([new SetItemParents(item, [target])], id);

    private static void AssertChanges(InventorySnapshot before, ServiceReply reply, InventorySnapshot after)
    {
        ItemChanges changes = Assert.IsType<InventoryOperationsResponse>(reply.Body).Changeset.Items;
        Dictionary<string, InventoryItem> items = before.Items.ToDictionary(item => item.InstanceId, StringComparer.Ordinal);
        foreach (string id in changes.Deleted)
        {
            Assert.True(items.Remove(id));
        }

        foreach (InventoryItem item in changes.Created)
        {
            Assert.True(items.TryAdd(item.InstanceId, item));
        }

        foreach (InventoryItem item in changes.Updated)
        {
            Assert.True(items.ContainsKey(item.InstanceId));
            items[item.InstanceId] = item;
        }

        Assert.True(InventorySnapshots.Same(new PlayerLoadout(after.UserId, after.Revision, [.. items.Values]), new PlayerLoadout(after.UserId, after.Revision, after.Items)));
    }

    public void Dispose() => _database.Dispose();
}
