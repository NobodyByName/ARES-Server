using Ares.Common.Game.Inventory;
using Ares.Host.Services.Inventory;
using Ares.Structs.Backend.Progression;

namespace Ares.Host.Tests;

public sealed class SalvageTests : IDisposable
{
    private readonly TestDatabase _database = new();

    [Theory]
    [InlineData(ItemTechnicalNames.AssortedSeeds, 100)]
    [InlineData(ItemTechnicalNames.Chemicals, 50)]
    [InlineData(ItemTechnicalNames.Fabric, 50)]
    [InlineData(ItemTechnicalNames.Planks, 50)]
    [InlineData(ItemTechnicalNames.PlasticParts, 50)]
    [InlineData(ItemTechnicalNames.RubberParts, 50)]
    [InlineData(ItemTechnicalNames.SheetMetal, 50)]
    [InlineData(ItemTechnicalNames.MetalParts, 50)]
    [InlineData(ItemTechnicalNames.Wires, 15)]
    [InlineData(ItemTechnicalNames.Battery, 15)]
    [InlineData(ItemTechnicalNames.SteelSpring, 15)]
    [InlineData(ItemTechnicalNames.DuctTape, 15)]
    [InlineData(ItemTechnicalNames.Oil, 15)]
    [InlineData(ItemTechnicalNames.Canister, 15)]
    [InlineData(ItemTechnicalNames.Magnet, 15)]
    [InlineData(ItemTechnicalNames.DurableCloth, 10)]
    [InlineData(ItemTechnicalNames.MechanicalComponents, 10)]
    [InlineData(ItemTechnicalNames.ElectricalComponents, 10)]
    [InlineData(ItemTechnicalNames.SimpleGunParts, 10)]
    [InlineData(ItemTechnicalNames.CrudeExplosives, 10)]
    [InlineData(ItemTechnicalNames.ArcAlloy, 15)]
    public void The_operator_adds_a_full_salvage_stack_to_the_stash_and_game_data_describes_it(string type, long maxStack)
    {
        InventoryStore store = Create();
        long assetId = ItemTechnicalNames.AssetIds[type];
        store.AddItem(type, DateTimeOffset.UnixEpoch, maxStack);
        OwnedItem stack = Assert.Single(store.Inventory.Stash.Items, item => item.Type == type);
        Assert.Equal(maxStack, stack.Quantity);
        InventoryItem owned = Assert.Single(store.Snapshot.Items, item => item.InstanceId == stack.InstanceId);
        Assert.Equal((assetId, maxStack), (owned.GameAssetId, owned.Amount));
        ItemDescription description = Assert.Single(store.GameData().ItemDescriptions.ItemDescriptions, item => item.GameAssetId == assetId);
        Assert.Equal((maxStack, true), (description.MaxAmount, description.Stackable));
        Assert.True(LoadoutDefinitions.SupportsCarriedItem(assetId));
        Assert.Equal(SalvageItems.ItemWeights[assetId], LoadoutDefinitions.ItemWeight(assetId));
        Assert.Equal(SalvageItems.UsableIndices[assetId], LoadoutDefinitions.UsableIndex(assetId));
        Assert.Equal(type, ItemTechnicalNames.DataAssets[type].Split('.')[^1]);
    }

    [Theory]
    [InlineData(ItemTechnicalNames.AssortedSeeds, 0)]
    [InlineData(ItemTechnicalNames.AssortedSeeds, -1)]
    [InlineData(ItemTechnicalNames.AssortedSeeds, 101)]
    [InlineData(ItemTechnicalNames.Chemicals, 51)]
    [InlineData(ItemTechnicalNames.MetalParts, 51)]
    public void Salvage_grants_outside_the_stack_limit_preserve_the_database(string type, long quantity)
    {
        InventoryStore store = Create();
        string saved = _database.InventoryJson;
        Assert.Throws<InvalidDataException>(() => store.AddItem(type, DateTimeOffset.UnixEpoch, quantity));
        Assert.Equal(saved, _database.InventoryJson);
    }

    [Theory]
    [InlineData(ItemTechnicalNames.Fabric, 200)]
    [InlineData(ItemTechnicalNames.Chemicals, 400)]
    [InlineData(ItemTechnicalNames.MetalParts, 400)]
    public void Only_fabric_fits_a_quick_use_slot(string type, int status)
    {
        InventoryStore store = Create();
        store.AddItem(type, DateTimeOffset.UnixEpoch, 5);
        OwnedItem stack = store.Inventory.Stash.Items.Single(item => item.Type == type);
        Assert.Equal(status, store.Commit(Move(stack.InstanceId, Slots(store, LoadoutItems.BeltSlotAssetId)[0], "quick-use"), DateTimeOffset.UnixEpoch).StatusCode);
    }

    [Fact]
    public void A_salvage_stack_in_the_backpack_is_reserved_with_its_amount()
    {
        InventoryStore store = Create();
        store.AddItem(ItemTechnicalNames.Planks, DateTimeOffset.UnixEpoch, 30);
        OwnedItem planks = store.Inventory.Stash.Items.Single(item => item.Type == ItemTechnicalNames.Planks);
        Assert.Equal(200, store.Commit(Move(planks.InstanceId, Slots(store, LoadoutItems.BackpackSlotAssetId)[0], "backpack"), DateTimeOffset.UnixEpoch).StatusCode);
        string ticket = Guid.NewGuid().ToString("D");
        InventoryItem carried = Assert.Single(store.Reserve(ticket).Items, item => item.GameAssetId == SalvageItems.ItemAssetIds[ItemTechnicalNames.Planks]);
        Assert.Equal((planks.InstanceId, 30L), (carried.InstanceId, carried.Amount));
        store.Release(ticket);
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
        InventoryItem branch = snapshot.Items.Single(item => frame.Slots.Contains(item.InstanceId) && LoadoutDefinitions.StandardBranch(item.GameAssetId) == branchAsset);
        return [.. snapshot.Items.Single(item => item.InstanceId == branch.Slots[0]).Slots];
    }

    private static InventoryOperationsRequest Move(string item, string target, string id) => new([new SetItemParents(item, [target])], id);

    public void Dispose() => _database.Dispose();
}
