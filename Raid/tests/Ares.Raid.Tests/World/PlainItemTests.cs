using Ares.Common.Game.Inventory;
using Ares.Raid.World.Inventory;
using Ares.Raid.World.Loot;
using Ares.Raid.World.Replication;
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
        Assert.Equal(LoadoutDefinitions.Generated.CarriedItems.Count, LootStates.UnitWeights.Count);
        Assert.All(LoadoutDefinitions.Generated.CarriedItems, item => Assert.Equal(item.Weight, LootStates.UnitWeights[checked((ushort)item.UsableIndex)]));
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
        InventoryItem backpackBranch = loadout.Items.Single(item => LoadoutDefinitions.Generated.StandardBranch(item.GameAssetId) == LoadoutItems.BackpackSlotAssetId);
        InventoryItem backpack = loadout.Items.Single(item => item.InstanceId == backpackBranch.Slots[0]);
        string target = backpack.Slots[0];
        InventoryItem brought = new(Guid.NewGuid().ToString("D"), shot.AssetId, 4, [], 1, 1, "", "shot", 0);
        loadout = loadout with { Items = [.. loadout.Items.Select(item => item.InstanceId == target ? item with { Slots = [brought.InstanceId] } : item), brought] };

        RaidInventory inventory = new(loadout);
        ContainerItem carried = inventory.Containers[inventory.Backpack].Items[0]!;
        Assert.Equal((checked((ushort)shot.UsableIndex), Guid.Parse(brought.InstanceId), 4), (carried.AssetIndex, carried.Id, carried.Amount));
        Assert.Contains(inventory.CarriedHome(RaidOutcomes.Extracted), item => item.Item == brought);
    }

    [Fact]
    public void A_raid_rolls_the_same_contents_for_a_seed_and_a_duck_only_where_no_item_fits()
    {
        ScriptedPlayer session = new();
        ReplicationContext replication = session.Worlds.Current!.State.Replication;
        string round = DamRounds.All[0].Name;
        LootCatalog first = new(replication, round, [], 1234, LootSpawnRules.Dam);
        LootCatalog second = new(replication, round, [], 1234, LootSpawnRules.Dam);
        Assert.Equal(first.Containers.Select(Contents), second.Containers.Select(Contents));
        Assert.Contains(first.Containers, container => container.Contents.Stacks.Count >= 2);

        // Every socket of the scripted raid has a container, and every item kind on Dam's sockets has items for it.
        LootCatalog every = session.Raid.Loot;
        LootStates states = session.Raid.LootStates;
        Assert.Empty(every.Placeholders);
        Assert.All(every.Containers, container => Assert.NotEmpty(states.Items(container.Index).OfType<ContainerItem>()));
        LootContainer cabinet = every.Containers.First(container => container.Tag == "Socket.LootContainer.Tech.Server.Door");
        Assert.StartsWith($"#{cabinet.Index} BP_SocketContainer_Gvt_Server_01_x400z250_Door tag Socket.LootContainer.Tech.Server.Door value Unknown pool Tech: ",
            states.Describe(cabinet.Index), StringComparison.Ordinal);

        // A container whose tag no item accepts holds the duck, and counts as a placeholder of its class.
        LootContainerClass wreck = LootContainerClasses.All.Single(type => type.Container.Class.EndsWith(".BP_SocketContainer_EnemyWreck_02_A_C", StringComparison.Ordinal));
        LootContainer spawned = Assert.IsType<LootContainer>(every.Spawn(wreck, cabinet.Location, default));
        Assert.Equal(("Socket.LootContainer.ARC.Wreck02", "ARC", 0), (spawned.Tag, spawned.Contents.Pool, spawned.Contents.Stacks.Count));
        Assert.Equal([(InventoryBlackboard.RubberDuckIndex, 1)], states.Items(spawned.Index).OfType<ContainerItem>().Select(item => (item.AssetIndex, item.Amount)));
        Assert.Equal(1, every.Placeholders["BP_SocketContainer_EnemyWreck_02_A"]);
        Assert.EndsWith($"pool ARC (placeholder duck): 1x {ItemTechnicalNames.RubberDuck}", states.Describe(spawned.Index), StringComparison.Ordinal);
    }

    [Fact]
    public void Several_item_kinds_from_a_container_split_merge_and_come_home_with_their_amounts()
    {
        UsableItem metal = UsableItems.ByTechnicalName["DA_Item_Salvage_ScrapMetal"];
        UsableItem sensors = UsableItems.ByTechnicalName["DA_Item_Salvage_Sensors"];
        UsableItem ammo = UsableItems.ByTechnicalName["DA_Item_LightAmmo"];
        UsableItem mod = UsableItems.ByTechnicalName["DA_Item_WeaponMod_Muzzle_Brake_01"];
        RaidInventory inventory = new(new PlayerLoadout(43, 0, []));
        ContainerItem?[] slots = new ContainerItem?[8];
        slots[0] = new ContainerItem(Index(metal), Guid.NewGuid(), 7, 1);
        slots[1] = new ContainerItem(Index(sensors), Guid.NewGuid(), 1, 1);
        slots[2] = new ContainerItem(Index(ammo), Guid.NewGuid(), 25, 1);
        slots[3] = new ContainerItem(Index(mod), Guid.NewGuid(), 1, 1);
        int loot = inventory.OpenWorldContainer(slots, LootStates.UnitWeights);
        int backpack = inventory.Backpack;

        // Split part of a stack out, then merge more of it onto the new stack; take a whole stack and part of another.
        Assert.True(inventory.Apply(Split(loot, 0, backpack, 0, 3)));
        Assert.True(inventory.Apply(Split(loot, 0, backpack, 0, 2)));
        Assert.True(inventory.Apply(Swap(loot, 1, backpack, 1)));
        Assert.True(inventory.Apply(Split(loot, 2, backpack, 2, 10)));
        Assert.False(inventory.Apply(Split(loot, 3, backpack, 2, 1), out string? rejection));
        Assert.Equal("different item", rejection);
        Assert.True(inventory.Apply(Swap(loot, 3, backpack, 3)));

        // A stack can also go back into the container.
        Assert.True(inventory.Apply(Swap(backpack, 3, loot, 5)));
        Assert.Equal([(Index(metal), 2), (Index(ammo), 15), (Index(mod), 1)], inventory.LootItems.OfType<ContainerItem>().Select(item => (item.AssetIndex, item.Amount)));
        Assert.Equal((5 * metal.Weight) + sensors.Weight + (10 * ammo.Weight), inventory.Weight, 6);

        IReadOnlyList<ContainerItem?> left = inventory.CloseWorldContainer();
        Assert.Equal(3, left.Count(item => item is not null));
        IReadOnlyList<CarriedItem> home = inventory.CarriedHome(RaidOutcomes.Extracted);
        RaidResults.Validate(new RaidResult(Guid.NewGuid().ToString("D"), 42, 7, RaidOutcomes.Extracted, home));
        Assert.Equal([(CarriedPlaces.Backpack, 0, metal.AssetId, 5L), (CarriedPlaces.Backpack, 1, sensors.AssetId, 1L), (CarriedPlaces.Backpack, 2, ammo.AssetId, 10L)],
            home.Select(item => (item.Place, item.Slot, item.Item.GameAssetId, item.Item.Amount)));
        Assert.All(home, item => Assert.Equal(StorageItems.InitialMaxDurability, item.Item.Durability));
    }

    private static ushort Index(UsableItem item) => checked((ushort)item.UsableIndex);

    private static (string? Pool, string[] Items) Contents(LootContainer container) =>
        (container.Contents.Pool, [.. container.Contents.Stacks.Select(stack => $"{stack.Amount}x {stack.Item.TechnicalName} {stack.Durability}")]);

    private static InventoryCommand Split(int fromContainer, ushort fromSlot, int toContainer, ushort toSlot, int amount) => new(InventoryCommandType.SplitItems,
        new ItemContainerSlot(new ItemContainerHandle(fromContainer, 0), fromSlot),
        new ItemContainerSlot(new ItemContainerHandle(toContainer, 0), toSlot), amount, new ItemContainerHandle(-1, ushort.MaxValue));

    private static InventoryCommand Swap(int fromContainer, ushort fromSlot, int toContainer, ushort toSlot) => new(InventoryCommandType.SwapItems,
        new ItemContainerSlot(new ItemContainerHandle(fromContainer, 0), fromSlot),
        new ItemContainerSlot(new ItemContainerHandle(toContainer, 0), toSlot), 0, new ItemContainerHandle(-1, ushort.MaxValue));
}
