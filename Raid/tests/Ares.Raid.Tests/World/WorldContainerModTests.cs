using Ares.Common.Game.Inventory;
using Ares.Raid.World.Inventory;
using Ares.Raid.World.Loot;
using Ares.Structs.Game.Inventory;

namespace Ares.Raid.Tests.World;

public sealed class WorldContainerModTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void A_weapon_or_shield_in_a_world_container_gets_a_mod_container_before_it_and_can_be_taken_home(bool shield)
    {
        ushort index = shield ? checked((ushort)TestGameData.Data.Names.LightShield.UsableIndex) : TestGameData.Items.FerroIIndex;
        int modSlots = shield ? TestGameData.Items.LightShield.ModSlotCount : TestGameData.Items.FerroModSlotCount;
        LootStates states = new ScriptedPlayer().Raid.LootStates;
        ContainerItem found = new(index, Guid.NewGuid(), 1, .5) { ModContainerId = 99 };
        states.Store(0, [found, null]);
        Assert.Null(states.Items(0)[0]!.ModContainerId);

        RaidInventory inventory = new(TestGameData.Items, WeaponTests.WithWeapons());
        int own = inventory.Containers.Count;
        int loot = inventory.OpenWorldContainer(states.Items(0), TestGameData.Items.UnitWeights);
        Assert.Equal(own + 1, loot);
        Assert.Equal(own, inventory.LootItems[0]!.ModContainerId);
        Assert.Equal(modSlots, inventory.Containers[own].Items.Count);
        Assert.All(inventory.Containers[own].Items, Assert.Null);
        _ = ItemContainerState.Encode(inventory.Containers, 1, loot);

        // Left behind, its mod container goes with the world container.
        states.Store(0, inventory.CloseWorldContainer());
        Assert.Equal(own, inventory.Containers.Count);
        Assert.Equal(loot, inventory.OpenWorldContainer(states.Items(0), TestGameData.Items.UnitWeights));
        Assert.True(inventory.Apply(ShieldTests.Swap(loot, 0, inventory.Backpack, 0)));
        _ = ItemContainerState.Encode(inventory.Containers, 1, loot);
        states.Store(0, inventory.CloseWorldContainer());

        ContainerItem taken = inventory.Containers[inventory.Backpack].Items[0]!;
        Assert.Equal((found.Id, own), (taken.Id, taken.ModContainerId));
        Assert.Equal(own + 1, inventory.Containers.Count);
        Assert.Null(states.Items(0)[0]);
        _ = ItemContainerState.Encode(inventory.Containers);
        Assert.Equal(own + 1, inventory.OpenWorldContainer(states.Items(0), TestGameData.Items.UnitWeights));
        inventory.CloseWorldContainer();

        IReadOnlyList<CarriedItem> home = inventory.CarriedHome(RaidOutcomes.Extracted);
        RaidResults.Validate(new RaidResult(Guid.NewGuid().ToString("D"), 42, 7, RaidOutcomes.Extracted, home));
        Assert.Contains(home, item => item.Place == CarriedPlaces.Backpack && item.Item.InstanceId == found.Id.ToString("D")
            && item.Item.GameAssetId == TestGameData.Items.AssetIds[index]);
    }

    [Fact]
    public void A_carried_weapon_left_in_a_world_container_and_taken_back_reuses_its_freed_mod_container()
    {
        RaidInventory inventory = new(TestGameData.Items, WeaponTests.WithWeapons());
        int own = inventory.Containers.Count;
        ContainerItem gun = inventory.Containers[inventory.Loadout].Items[InventoryBlackboard.Firearm1Slot]!;
        LootStates states = new ScriptedPlayer().Raid.LootStates;
        states.Store(0, [null]);
        for (int round = 0; round < 3; round++)
        {
            int loot = inventory.OpenWorldContainer(states.Items(0), TestGameData.Items.UnitWeights);
            Assert.True(inventory.Apply(ShieldTests.Swap(inventory.Loadout, InventoryBlackboard.Firearm1Slot, loot, 0)));
            states.Store(0, inventory.CloseWorldContainer());
            Assert.Null(states.Items(0)[0]!.ModContainerId);

            Assert.Equal(own, inventory.OpenWorldContainer(states.Items(0), TestGameData.Items.UnitWeights));
            Assert.Equal(gun.ModContainerId, inventory.LootItems[0]!.ModContainerId);
            Assert.True(inventory.Apply(ShieldTests.Swap(own, 0, inventory.Loadout, InventoryBlackboard.Firearm1Slot)));
            states.Store(0, inventory.CloseWorldContainer());
            Assert.Equal(own, inventory.Containers.Count);
        }
    }

    [Fact]
    public void A_weapon_dropped_by_one_player_gets_a_mod_container_in_the_set_of_the_player_who_picks_it_up()
    {
        RaidInventory dropper = new(TestGameData.Items, WeaponTests.WithWeapons());
        InventoryCommand swap = ShieldTests.Swap(dropper.Loadout, InventoryBlackboard.Firearm1Slot, dropper.Loadout, InventoryBlackboard.Firearm1Slot);
        InventoryCommand drop = swap with { Type = InventoryCommandType.DropItem };
        Assert.True(dropper.Apply(drop));
        Assert.True(dropper.TryTakeDropped(out ContainerItem dropped));
        Assert.NotNull(dropped.ModContainerId);

        RaidInventory picker = new(TestGameData.Items, InventoryTests.StandardLoadout());
        int own = picker.Containers.Count;
        Assert.True(own <= dropped.ModContainerId);

        // With a world container open, the set has no room for a new mod container.
        int loot = picker.OpenWorldContainer([null], TestGameData.Items.UnitWeights);
        Assert.Equal(0, picker.PickUp(dropped));
        picker.CloseWorldContainer();

        Assert.Equal(1, picker.PickUp(dropped));
        ContainerItem held = Assert.Single(picker.Containers[picker.Backpack].Items.OfType<ContainerItem>(), item => item.Id == dropped.Id);
        Assert.Equal(own, held.ModContainerId);
        Assert.Equal(TestGameData.Items.FerroModSlotCount, picker.Containers[own].Items.Count);
        Assert.Equal(own, loot);
        _ = ItemContainerState.Encode(picker.Containers);
    }
}
