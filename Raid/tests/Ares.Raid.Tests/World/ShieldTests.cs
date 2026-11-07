using Ares.Common.Game.Inventory;
using Ares.Raid.World;
using Ares.Raid.World.Inventory;
using Ares.Structs.Backend.Progression;
using Ares.Structs.Core.Serialization;
using Ares.Structs.Game.Inventory;
using Ares.Structs.Game.Movement;
using Ares.Structs.Game.Replication;
using Ares.Structs.Net.Replication;
using Ares.Structs.Net.Session;

namespace Ares.Raid.Tests.World;

public sealed class ShieldTests
{
    [Theory]
    [InlineData(null)]
    [InlineData(AugmentItems.CombatMk1AssetId)]
    [InlineData(AugmentItems.LootingMk1AssetId)]
    [InlineData(AugmentItems.TacticalMk1AssetId)]
    public void Container_creation_is_acknowledged_before_the_initial_armor_contents_are_sent(long? augment)
    {
        ScriptedPlayer session = new(loadout: WithShield(augment));
        session.Possess();
        session.Sent.Clear();
        session.Input(30, 33, position: session.Raid.Spawn.Location);
        session.Service(100);
        Dictionary<int, PendulumSlot> first = session.PawnFrame(session.SpawnState);
        Assert.Equal(ushort.MaxValue, AssetIndex(first, InventoryBlackboard.ArmorSlot));
        Assert.Equal(ushort.MaxValue, AssetIndex(first, InventoryBlackboard.LoadoutSlotCount + 3));
        Assert.NotNull(session.Raid.Inventory.Shield);

        session.Input(33, 36, position: session.Raid.Spawn.Location);
        session.Sent.Clear();
        session.Service(100.1);
        Dictionary<int, PendulumSlot> second = session.PawnFrame(first);
        Assert.Equal(ShieldItems.LightShieldUsableIndex, AssetIndex(second, InventoryBlackboard.ArmorSlot));
        Assert.Equal(InventoryBlackboard.RubberDuckIndex, AssetIndex(second, InventoryBlackboard.LoadoutSlotCount + 3));
        Assert.Equal(3, session.Channels.BlackboardFrames);
        session.Input(36, 39, position: session.Raid.Spawn.Location);
        session.Service(100.2);
        Assert.Equal(3, session.Channels.BlackboardFrames);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(AugmentItems.CombatMk1AssetId)]
    [InlineData(AugmentItems.LootingMk1AssetId)]
    [InlineData(AugmentItems.TacticalMk1AssetId)]
    public void An_equipped_light_shield_keeps_its_identity_and_initializes_the_raid_hud(long? augment)
    {
        PlayerLoadout loadout = WithShield(augment);
        InventoryItem selected = loadout.Items.Single(item => item.GameAssetId == ShieldItems.LightShieldAssetId);
        ScriptedPlayer session = new(loadout: loadout);
        session.Possess();
        RaidInventory inventory = session.Raid.Inventory;
        Assert.Equal(new ContainerItem(3, Guid.Parse(selected.InstanceId), 1, 0.5) { ModContainerId = 5 }, inventory.Shield);
        Assert.Empty(inventory.Containers[5].Items);
        Assert.Equal(6.2, inventory.Weight, precision: 6);
        Dictionary<string, object?> values = ShieldProperties(session, Assert.Single(ShieldDeliveries(session)).Content);
        Assert.Equal(40.0, values[$"{PropertyNames.Armor}.{PropertyNames.Health}"]);
        Assert.Equal(40.0, values[$"{PropertyNames.Armor}.{PropertyNames.CurrentMaxArmor}"]);
        Assert.Equal(40.0, values[$"{PropertyNames.Armor}.{PropertyNames.HealthPerPlate}"]);
        StableSubobjectExport component = Assert.Single(session.Raid.State.InventoryExports(session.Player).Records.Values.OfType<StableSubobjectExport>(), entry => entry.Guid == session.Player.Health);
        Assert.Equal("HealthComponent", component.Path);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(0.01, 40)]
    [InlineData(100, 40)]
    public void Broken_shields_have_no_available_capacity_and_positive_durability_keeps_full_capacity(double durability, double health)
    {
        PlayerLoadout loadout = WithShield();
        loadout = loadout with { Items = [.. loadout.Items.Select(item => item.GameAssetId == ShieldItems.LightShieldAssetId ? item with { Durability = durability } : item)] };
        RaidInventory inventory = new(loadout);
        Assert.NotNull(inventory.Shield);
        Assert.Equal(health, inventory.ShieldHealth);
    }

    [Fact]
    public void Carried_shields_can_be_equipped_and_removed_but_cannot_enter_other_equipment_or_quick_use_slots()
    {
        RaidInventory inventory = new(WithShield(inBackpack: true));
        ContainerItem shield = inventory.Containers[inventory.Backpack].Items[5]!;
        Assert.Null(inventory.Shield);
        Assert.Equal(0, inventory.ShieldHealth);
        Assert.False(inventory.Apply(Swap(inventory.Backpack, 5, inventory.Belt, 0)));
        Assert.False(inventory.Apply(Swap(inventory.Backpack, 5, inventory.Loadout, InventoryBlackboard.Firearm1Slot)));
        Assert.False(inventory.Apply(Swap(inventory.Backpack, 3, inventory.Loadout, InventoryBlackboard.ArmorSlot)));
        Assert.True(inventory.Apply(Swap(inventory.Backpack, 5, inventory.Loadout, InventoryBlackboard.ArmorSlot)));
        Assert.Equal(shield, inventory.Shield);
        Assert.Equal(40, inventory.ShieldHealth);
        Assert.False(inventory.Apply(Swap(inventory.Backpack, 3, inventory.Loadout, InventoryBlackboard.ArmorSlot)));
        Assert.True(inventory.Apply(Swap(inventory.Loadout, InventoryBlackboard.ArmorSlot, inventory.Backpack, 5)));
        Assert.Equal(shield, inventory.Containers[inventory.Backpack].Items[5]);
        Assert.Null(inventory.Shield);
        Assert.Equal(0, inventory.ShieldHealth);
        Assert.Equal(6.2, inventory.Weight, precision: 6);
    }

    [Fact]
    public void Equipping_and_removing_in_a_running_session_replicates_shield_health_with_the_inventory_correction()
    {
        ScriptedPlayer session = new(loadout: WithShield(inBackpack: true));
        session.Possess();
        Assert.Equal(0.0, ShieldProperties(session, Assert.Single(ShieldDeliveries(session)).Content)[$"{PropertyNames.Armor}.{PropertyNames.Health}"]);
        (uint start, double at) = session.Settle(30, 100);
        session.Input(start, start + 1, position: session.Raid.Spawn.Location, inventory: Swap(session.Raid.Inventory.Backpack, 5, session.Raid.Inventory.Loadout, InventoryBlackboard.ArmorSlot));
        session.Input(start + 1, start + 2, position: session.Raid.Spawn.Location);
        session.Service(at);
        Assert.Equal(2, ShieldDeliveries(session).Count);
        Assert.Equal(40.0, ShieldProperties(session, ShieldDeliveries(session)[^1].Content)[$"{PropertyNames.Armor}.{PropertyNames.Health}"]);
        session.Input(start + 2, start + 3, position: session.Raid.Spawn.Location, inventory: Swap(session.Raid.Inventory.Loadout, InventoryBlackboard.ArmorSlot, session.Raid.Inventory.Backpack, 5));
        session.Input(start + 3, start + 4, position: session.Raid.Spawn.Location);
        session.Service(at + 0.1);
        Assert.Equal(3, ShieldDeliveries(session).Count);
        Assert.Equal(0.0, ShieldProperties(session, ShieldDeliveries(session)[^1].Content)[$"{PropertyNames.Armor}.{PropertyNames.CurrentMaxArmor}"]);
    }

    [Theory]
    [InlineData(103801093L)]
    [InlineData(-2097404337L)]
    public void Unsupported_shields_are_rejected_before_world_preparation(long asset)
    {
        PlayerLoadout loadout = WithShield();
        loadout = loadout with { Items = [.. loadout.Items.Select(item => item.GameAssetId == ShieldItems.LightShieldAssetId ? item with { GameAssetId = asset } : item)] };
        Assert.Throws<WorldPreparationException>(() => new RaidInventory(loadout));
    }

    private static List<StableSubobjectContent> ShieldDeliveries(ScriptedPlayer session)
    {
        List<StableSubobjectContent> contents = [];
        foreach (ChannelBunch bunch in session.Sent.Where(bunch => bunch is { Channel: 6, Opened: false, PackageExports: false } && (bunch.Payload[0] & 2) == 0))
        {
            BitReader reader = new(bunch.Payload, bunch.PayloadBits);
            reader.ReadBits(2);
            if (NetGuid.Read(reader) == session.Player.Health)
            {
                contents.Add(StableSubobjectContent.Read(new BitReader(bunch.Payload, bunch.PayloadBits)));
            }
        }

        return contents;
    }

    private static int AssetIndex(Dictionary<int, PendulumSlot> frame, int slot)
    {
        PendulumArrayField items = (PendulumArrayField)PendulumRegistry.Descriptors[InventoryBlackboard.Descriptor].Fields[InventoryBlackboard.SlotsField];
        byte[] value = frame[InventoryBlackboard.Slot].Value;
        byte[] item = [.. value.AsSpan(items.Offset + (slot * items.Stride), items.Stride)];
        return checked((int)PendulumCodec.Integer(items.ElementFields[InventoryBlackboard.AssetIndexField], item));
    }

    private static Dictionary<string, object?> ShieldProperties(ScriptedPlayer session, ActorContentBlock content)
    {
        PropertyCodec codec = new(new ValueCodec(session.Raid.State.Replication.Catalog, []));
        return codec.Decode(new PropertyLayout(GameReplication.Catalog, ClassPaths.HealthComponent), content);
    }

    private static PlayerLoadout WithShield(long? augment = null, bool inBackpack = false)
    {
        PlayerLoadout loadout = InventoryTests.StandardLoadout(augment);
        InventoryItem branch = loadout.Items.Single(item => LoadoutDefinitions.StandardBranch(item.GameAssetId) == (inBackpack ? LoadoutItems.BackpackSlotAssetId : LoadoutItems.ArmorSlotAssetId));
        InventoryItem container = loadout.Items.Single(item => item.InstanceId == branch.Slots[0]);
        string slot = container.Slots[inBackpack ? 5 : 0];
        InventoryItem shield = new(Guid.NewGuid().ToString("D"), ShieldItems.LightShieldAssetId, 1, [], 50, 100, "", "shield", 0);
        return loadout with { Items = [.. loadout.Items.Select(item => item.InstanceId == slot ? item with { Slots = [shield.InstanceId] } : item), shield] };
    }

    private static InventoryCommand Swap(int from, int fromSlot, int to, int toSlot) => new(InventoryCommandType.SwapItems,
        new ItemContainerSlot(new ItemContainerHandle(from, 0), checked((ushort)fromSlot)),
        new ItemContainerSlot(new ItemContainerHandle(to, 0), checked((ushort)toSlot)), 0, new ItemContainerHandle(-1, ushort.MaxValue));
}
