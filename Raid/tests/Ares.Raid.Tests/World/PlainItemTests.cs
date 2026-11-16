using Ares.Common.Game.Inventory;
using Ares.Raid.Replication;
using Ares.Raid.World;
using Ares.Raid.World.Inventory;
using Ares.Structs.Backend.Progression;
using Ares.Structs.Game.Inventory;
using Ares.Structs.Game.Loot;
using Ares.Structs.Game.World;

namespace Ares.Raid.Tests.World;

public sealed class PlainItemTests
{
    [Fact]
    public void Loot_containers_know_the_weight_of_every_carried_item()
    {
        Assert.Equal(LoadoutDefinitions.CarriedItems.Count, LootStates.UnitWeights.Count);
        Assert.All(LoadoutDefinitions.CarriedItems, item => Assert.Equal(item.Weight, LootStates.UnitWeights[checked((ushort)item.UsableIndex)]));
    }

    [Fact]
    public void A_catalogue_item_taken_from_a_container_counts_its_weight_and_comes_home_as_its_asset()
    {
        UsableItem sensors = UsableItems.ByTechnicalName["DA_Item_Salvage_Sensors"];
        UsableItem grenade = UsableItems.ByTechnicalName["DA_Item_BasicStickyGrenade"];
        RaidInventory inventory = new(new PlayerLoadout(43, 0, []));
        ContainerItem found = new(checked((ushort)sensors.UsableIndex), Guid.NewGuid(), 2, 1);
        ContainerItem thrown = new(checked((ushort)grenade.UsableIndex), Guid.NewGuid(), 3, 1);
        int loot = inventory.OpenWorldContainer([found, thrown], LootStates.UnitWeights);
        Assert.True(inventory.Apply(Swap(loot, 0, inventory.Backpack, 0)));
        Assert.True(inventory.Apply(Swap(loot, 1, inventory.Backpack, 1)));
        Assert.Equal((2 * sensors.Weight) + (3 * grenade.Weight), inventory.Weight, 6);
        Assert.False(inventory.Apply(Swap(inventory.Backpack, 0, inventory.Loadout, InventoryBlackboard.ArmorSlot), out string? rejection));
        Assert.Equal("slot not allowed", rejection);
        inventory.CloseWorldContainer();

        IReadOnlyList<CarriedItem> home = inventory.CarriedHome(RaidOutcomes.Extracted);
        RaidResults.Validate(new RaidResult(Guid.NewGuid().ToString("D"), 42, 7, RaidOutcomes.Extracted, home));
        Assert.Equal([(sensors.AssetId, 2L), (grenade.AssetId, 3L)], home.Select(item => (item.Item.GameAssetId, item.Item.Amount)));
    }

    [Fact]
    public void A_brought_catalogue_item_keeps_its_identity_in_the_backpack()
    {
        UsableItem shot = UsableItems.ByTechnicalName["DA_Item_AdrenalinShot"];
        PlayerLoadout loadout = InventoryTests.StandardLoadout();
        InventoryItem backpackBranch = loadout.Items.Single(item => LoadoutDefinitions.StandardBranch(item.GameAssetId) == LoadoutItems.BackpackSlotAssetId);
        InventoryItem backpack = loadout.Items.Single(item => item.InstanceId == backpackBranch.Slots[0]);
        string target = backpack.Slots[0];
        InventoryItem brought = new(Guid.NewGuid().ToString("D"), shot.AssetId, 4, [], 1, 1, "", "shot", 0);
        loadout = loadout with { Items = [.. loadout.Items.Select(item => item.InstanceId == target ? item with { Slots = [brought.InstanceId] } : item), brought] };

        RaidInventory inventory = new(loadout);
        ContainerItem carried = inventory.Containers[inventory.Backpack].Items[0]!;
        Assert.Equal((checked((ushort)shot.UsableIndex), Guid.Parse(brought.InstanceId), 4), (carried.AssetIndex, carried.Id, carried.Amount));
        Assert.Contains(inventory.CarriedHome(RaidOutcomes.Extracted), item => item.Item == brought);
    }

    private static InventoryCommand Swap(int fromContainer, ushort fromSlot, int toContainer, ushort toSlot) => new(InventoryCommandType.SwapItems,
        new ItemContainerSlot(new ItemContainerHandle(fromContainer, 0), fromSlot),
        new ItemContainerSlot(new ItemContainerHandle(toContainer, 0), toSlot), 0, new ItemContainerHandle(-1, ushort.MaxValue));
}
