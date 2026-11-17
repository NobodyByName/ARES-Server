using Ares.Common.Game.Inventory;
using Ares.Raid.World.Inventory;
using Ares.Structs.Backend.Progression;
using Ares.Structs.Game.Inventory;

namespace Ares.Raid.Tests.World;

/// <summary>Pins the per-item answers the raid gives for every item: held class, mod container, loadout slots, ammunition and quick use.</summary>
public sealed class ItemTraitsTests
{
    private static readonly long[] s_branches = [TestGameData.Loadout.Branches.Backpack, TestGameData.Loadout.Branches.MainWeapon, TestGameData.Loadout.Branches.Shield];

    [Fact]
    public void Held_item_classes_are_the_firearm_hand_items_and_snap_hook()
    {
        Dictionary<ushort, string> expected = new()
        {
            [TestGameData.Items.FerroIIndex] = TestGameData.Items.FerroI.ItemClass,
            [TestGameData.Items.RaiderToolIndex] = TestGameData.Items.RaiderTool.ItemClass,
            [TestGameData.Items.UnarmedIndex] = TestGameData.Items.Unarmed.ItemClass,
            [TestGameData.Items.SnapHookIndex] = TestGameData.Items.SnapHook.ItemClass,
        };
        Assert.Equal(expected.OrderBy(entry => entry.Key), TestGameData.Items.HeldItemClasses.OrderBy(entry => entry.Key));
    }

    [Fact]
    public void Only_the_ferro_and_light_shield_get_a_mod_container_of_their_mod_slot_count()
    {
        Dictionary<int, int> modSlots = [];
        foreach (ItemDefinition carried in TestGameData.Items.Definitions.CarriedItems)
        {
            RaidInventory inventory = new(TestGameData.Items, Placed(carried.AssetId));
            foreach (ContainerItem item in inventory.Containers.SelectMany(container => container.Items).OfType<ContainerItem>())
            {
                if (item.ModContainerId is { } mods)
                {
                    modSlots[item.AssetIndex] = inventory.Containers[mods].Items.Count;
                }
            }
        }

        Assert.Equal([(TestGameData.Data.Names.LightShield.UsableIndex, TestGameData.Items.LightShield.ModSlotCount), (TestGameData.Items.FerroIIndex, TestGameData.Items.FerroModSlotCount)],
            modSlots.OrderBy(entry => entry.Key).Select(entry => (entry.Key, entry.Value)));
        Assert.Equal(3, TestGameData.Items.FerroModSlotCount);
        Assert.Equal(0, TestGameData.Items.LightShield.ModSlotCount);
    }

    [Fact]
    public void Only_the_ferro_fits_the_firearm_slots_and_only_the_light_shield_the_armor_slot()
    {
        List<(int Item, int Slot)> accepted = [];
        foreach (ItemDefinition usable in TestGameData.Data.Items.All)
        {
            for (int slot = 0; slot < InventoryBlackboard.LoadoutSlotCount; slot++)
            {
                RaidInventory inventory = new(TestGameData.Items, new PlayerLoadout(43, 0, []));
                int loot = inventory.OpenWorldContainer([Item(usable.UsableIndex)], new Dictionary<ushort, double> { [(ushort)usable.UsableIndex] = 0 });
                if (inventory.Apply(ShieldTests.Swap(loot, 0, inventory.Loadout, slot)))
                {
                    accepted.Add((usable.UsableIndex, slot));
                }
            }
        }

        Assert.Equal([(TestGameData.Data.Names.LightShield.UsableIndex, InventoryBlackboard.ArmorSlot), (TestGameData.Items.FerroIIndex, InventoryBlackboard.Firearm1Slot),
            (TestGameData.Items.FerroIIndex, InventoryBlackboard.Firearm2Slot)], accepted.Order());
    }

    [Fact]
    public void Only_the_ferro_unloads_its_rounds_as_heavy_ammo_on_extraction()
    {
        List<(int Item, long Ammo, long Rounds)> unloaded = [];
        foreach (ItemDefinition usable in TestGameData.Items.Definitions.CarriedItems)
        {
            RaidInventory inventory = new(TestGameData.Items, new PlayerLoadout(43, 0, []));
            int loot = inventory.OpenWorldContainer([Item(usable.UsableIndex) with { AmmoInClip = 1 }],
                new Dictionary<ushort, double> { [(ushort)usable.UsableIndex] = 0 });
            if (!Store(inventory, loot))
            {
                continue;
            }

            inventory.CloseWorldContainer();
            IReadOnlyList<CarriedItem> home = inventory.CarriedHome(RaidOutcomes.Extracted, out int lost);
            unloaded.AddRange(home.Where(item => item.Item.GameAssetId != usable.AssetId)
                .Select(added => (usable.UsableIndex, added.Item.GameAssetId, added.Item.Amount)));

            Assert.Equal(0, lost);
        }

        Assert.Equal([(TestGameData.Items.FerroIIndex, TestGameData.Items.HeavyAmmo.AssetId, 1L)], unloaded);
    }

    [Fact]
    public void Only_the_snap_hook_is_drawn_from_the_belt()
    {
        List<int> quick = [];
        foreach (ItemDefinition usable in TestGameData.Items.Definitions.CarriedItems)
        {
            RaidInventory inventory = new(TestGameData.Items, new PlayerLoadout(43, 0, []));
            ContainerItem item = Item(usable.UsableIndex);
            int loot = inventory.OpenWorldContainer([item], new Dictionary<ushort, double> { [(ushort)usable.UsableIndex] = 0 });
            if (!inventory.Apply(ShieldTests.Swap(loot, 0, inventory.Belt, 0)))
            {
                continue;
            }

            inventory.CloseWorldContainer();
            RaidWeapons weapons = new(inventory, TestGameData.Data);
            Dictionary<Guid, ulong> objects = new() { [item.Id] = 77 };
            weapons.Apply(new WeaponInput(30, 0, 0, 0), objects);
            weapons.Apply(new WeaponInput(40, 0, 77, 1), objects);
            if (weapons.QuickItemRequests > 0)
            {
                quick.Add(usable.UsableIndex);
            }
        }

        Assert.Equal([TestGameData.Items.SnapHookIndex], quick);
    }

    // Moves the loot item into the first loadout, backpack or belt slot that takes it.
    private static bool Store(RaidInventory inventory, int loot) =>
        Enumerable.Range(0, InventoryBlackboard.LoadoutSlotCount).Any(slot => inventory.Apply(ShieldTests.Swap(loot, 0, inventory.Loadout, slot)))
        || inventory.Apply(ShieldTests.Swap(loot, 0, inventory.Backpack, 0))
        || inventory.Apply(ShieldTests.Swap(loot, 0, inventory.Belt, 0));

    private static ContainerItem Item(int usableIndex) => new(checked((ushort)usableIndex), Guid.NewGuid(), 1, 1);

    // The standard loadout with the item in the first backpack, firearm or armor slot that allows it, with an empty node per mod slot.
    private static PlayerLoadout Placed(long asset)
    {
        PlayerLoadout loadout = InventoryTests.StandardLoadout();
        long branchAsset = s_branches.First(branch => ((SingleItemSlot)TestGameData.Items.Definitions.ByAsset[TestGameData.Items.Definitions.Container(TestGameData.Loadout.StandardFrameAssetId, branch)
            .SlotAssetId].SlotConfig!).AllowedSlotAssetIds.Contains(asset));
        InventoryItem branchNode = loadout.Items.Single(item => TestGameData.Items.Definitions.StandardBranch(item.GameAssetId) == branchAsset);
        InventoryItem container = loadout.Items.Single(item => item.InstanceId == branchNode.Slots[0]);
        string slot = container.Slots[^1];
        InventoryItem[] modSlots = TestGameData.Items.Definitions.ByAsset[asset].SlotConfig is StructuralItemSlots slots
            ? [.. slots.SlotAssetIds.Select(slotAsset => new InventoryItem(Guid.NewGuid().ToString("D"), slotAsset, 1, [], 1, 1, "", "pin", 0))]
            : [];
        InventoryItem owned = new(Guid.NewGuid().ToString("D"), asset, 1, [.. modSlots.Select(modSlot => modSlot.InstanceId)], 1, 1, "", "pin", 0);
        return loadout with { Items = [.. loadout.Items.Select(item => item.InstanceId == slot ? item with { Slots = [owned.InstanceId] } : item), owned, .. modSlots] };
    }
}
