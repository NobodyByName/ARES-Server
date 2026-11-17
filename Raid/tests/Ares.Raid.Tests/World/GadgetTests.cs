using System.Globalization;
using Ares.Common.Game.Inventory;
using Ares.Raid.World.Inventory;
using Ares.Raid.World.Movement;
using Ares.Structs.Backend.Progression;
using Ares.Structs.Core.Serialization;
using Ares.Structs.Game.Inventory;
using Ares.Structs.Game.Movement;
using Ares.Structs.Game.Pendulum;
using Ares.Structs.Game.Replication;
using Ares.Structs.Net.Replication;

namespace Ares.Raid.Tests.World;

public sealed class GadgetTests
{
    [Theory]
    [InlineData(null, 0)]
    [InlineData(AugmentItems.CombatMk1AssetId, 3)]
    [InlineData(AugmentItems.LootingMk1AssetId, 1)]
    [InlineData(AugmentItems.TacticalMk1AssetId, 4)]
    public void A_snap_hook_in_quick_use_keeps_its_identity_and_is_sent_once_the_containers_exist(long? augment, int slot)
    {
        PlayerLoadout loadout = WithSnapHook(augment, slot);
        InventoryItem hook = loadout.Items.Single(item => item.GameAssetId == GadgetItems.SnapHookAssetId);
        ScriptedPlayer session = new(loadout: loadout);
        session.Possess();
        RaidInventory inventory = session.Member.Inventory;
        Assert.Equal(new ContainerItem(GadgetItems.SnapHookUsableIndex, Guid.Parse(hook.InstanceId), 1, 0.5), inventory.Containers[inventory.Belt].Items[slot]);
        Assert.Equal(6.2, inventory.Weight, precision: 6);

        int position = InventoryBlackboard.LoadoutSlotCount + inventory.Containers[inventory.Backpack].Items.Count + slot;
        session.Sent.Clear();
        session.Input(30, 33, position: session.Member.Spawn.Location);
        session.Service(100);
        Dictionary<int, PendulumSlot> first = session.PawnFrame(session.SpawnState);
        Assert.Equal(ushort.MaxValue, ShieldTests.AssetIndex(first, position));
        session.Input(33, 36, position: session.Member.Spawn.Location);
        session.Sent.Clear();
        session.Service(100.1);
        Assert.Equal(GadgetItems.SnapHookUsableIndex, ShieldTests.AssetIndex(session.PawnFrame(first), position));
    }

    [Fact]
    public void A_snap_hook_moves_between_quick_use_slots_and_the_backpack_but_not_into_equipment_slots()
    {
        RaidInventory inventory = new(WithSnapHook(null, 0));
        ContainerItem hook = inventory.Containers[inventory.Belt].Items[0]!;
        Assert.False(inventory.Apply(ShieldTests.Swap(inventory.Belt, 0, inventory.Loadout, InventoryBlackboard.Firearm1Slot)));
        Assert.False(inventory.Apply(ShieldTests.Swap(inventory.Belt, 0, inventory.Loadout, InventoryBlackboard.ArmorSlot)));
        Assert.True(inventory.Apply(ShieldTests.Swap(inventory.Belt, 0, inventory.Belt, 3)));
        Assert.Equal(hook, inventory.Containers[inventory.Belt].Items[3]);
        Assert.True(inventory.Apply(ShieldTests.Swap(inventory.Belt, 3, inventory.Backpack, 5)));
        Assert.Equal(hook, inventory.Containers[inventory.Backpack].Items[5]);
        Assert.Null(inventory.Containers[inventory.Belt].Items[3]);
        Assert.True(inventory.Apply(ShieldTests.Swap(inventory.Backpack, 5, inventory.Belt, 2)));
        Assert.Equal(hook, inventory.Containers[inventory.Belt].Items[2]);
        Assert.Equal(6.2, inventory.Weight, precision: 6);
    }

    [Fact]
    public void An_extraction_brings_the_snap_hook_home_in_its_current_quick_use_slot()
    {
        PlayerLoadout loadout = WithSnapHook(null, 0);
        InventoryItem hook = loadout.Items.Single(item => item.GameAssetId == GadgetItems.SnapHookAssetId);
        RaidInventory inventory = new(loadout);
        Assert.True(inventory.Apply(ShieldTests.Swap(inventory.Belt, 0, inventory.Belt, 2)));
        IReadOnlyList<CarriedItem> carried = inventory.CarriedHome(RaidOutcomes.Extracted, out _);
        RaidResults.Validate(new RaidResult(Guid.NewGuid().ToString("D"), 42, 7, RaidOutcomes.Extracted, carried));
        CarriedItem home = Assert.Single(carried, item => item.Item.GameAssetId == GadgetItems.SnapHookAssetId);
        Assert.Equal((CarriedPlaces.QuickUse, 2, hook), (home.Place, home.Slot, home.Item));
    }

    [Fact]
    public void A_carried_snap_hook_gets_a_held_item_object_the_client_knows_with_the_item_base_export_checksum()
    {
        ScriptedPlayer session = new(loadout: AddSnapHook(WeaponTests.WithWeapons(), 1));
        session.Flush("exports");
        ulong classGuid = session.Raid.Imports.Classes[GadgetItems.SnapHookItemClass];
        StaticObjectExport exported = Assert.Single(session.Sent.Where(bunch => bunch.PackageExports)
            .SelectMany(bunch => ObjectExports.Read(new BitReader(bunch.Payload, bunch.PayloadBits)).Exports.Records.Values)
            .OfType<StaticObjectExport>(), item => item.Guid == classGuid);
        Assert.Equal(0xD0674436U, exported.Checksum);
        Assert.True(GameReplication.Catalog.IsA(GadgetItems.SnapHookItemClass, ClassPaths.ItemBase));

        session.Possess();
        RaidInventory inventory = session.Member.Inventory;
        ContainerItem hook = inventory.Containers[inventory.Belt].Items[1]!;
        ulong itemGuid = session.Player.HeldItems[hook.Id];
        DynamicSubobjectContent created = Assert.Single(session.Raid.State.WeaponItemCreation(session.Player, inventory), item => item.Guid == itemGuid);
        Assert.Equal(classGuid, created.ClassGuid);
        Assert.Equal(session.Player.Inventory, created.OuterGuid);

        Dictionary<ulong, string> objects = session.Raid.State.Objects([]);
        foreach (ulong guid in session.Player.HeldItems.Values)
        {
            objects[guid] = ClassPaths.ItemBase;
        }

        PropertyCodec codec = new(new ValueCodec(GameReplication.Catalog, [KnownEnums.NetRole], objects));
        Dictionary<string, object?> values = codec.Decode(new PropertyLayout(GameReplication.Catalog, ClassPaths.ItemBase), created.Content);
        Assert.Equal(-1, values[$"{PropertyNames.SubItemContainer}.{PropertyNames.Id}"]);
        StableSubobjectContent known = session.Raid.State.WeaponItemReferences(session.Player);
        Assert.Contains(itemGuid, Assert.IsType<IReadOnlyList<object?>>(codec.Decode(
            new PropertyLayout(GameReplication.Catalog, ClassPaths.InventoryComponent), known.Content)[PropertyNames.KnownReplicatedItems], exactMatch: false));
    }

    /// <summary>
    /// The client's own prediction of a run of switches between the Ferro, the Snap Hook and empty hands, tick by tick, with
    /// the Ferro put away for the hook and drawn back after it in its node's offhand times (0.25 s and 0.2 s):
    /// first and last tick, then the inventory state's item in hand, target, item put away, offhand, phase, last wanted item,
    /// handled equip change, equip timer at the first tick (20 less each tick while switching), gadget power, and the
    /// slots after the other variables (descriptor, or '-' for a slot laid out without one, at page x pages).
    /// </summary>
    private static readonly string[] s_switches =
    [
        "6548 6878 11 254 6 254 0 6 12 0 255 -",
        "6879 6886 11 12 11 11 1 11 13 150 255 12:80@34x2",
        "6887 6897 12 254 11 11 2 11 13 210 255 12:80@34x2",
        "6898 6956 12 254 11 11 0 11 13 0 255 12:80@34x2",
        "6957 6962 12 11 12 11 1 11 14 120 255 12:80@34x2",
        "6963 6968 11 254 12 254 2 11 14 120 255 12:80@34x2",
        "6969 7059 11 254 12 254 0 11 14 0 255 12:80@34x2",
        "7060 7067 11 13 11 11 1 11 15 150 255 12:-@34x2,13:80@36x2",
        "7068 7078 13 254 11 11 2 11 15 210 255 12:-@34x2,13:80@36x2",
        "7079 7130 13 254 11 11 0 11 15 0 255 12:-@34x2,13:80@36x2",
        "7131 7132 13 6 13 11 1 6 16 120 255 12:-@34x2,13:80@36x2",
        "7133 7136 13 6 13 11 1 6 17 80 255 12:-@34x2,13:80@36x2",
        "7137 7142 11 6 13 254 2 6 17 120 255 12:-@34x2,13:80@36x2",
        "7143 7143 11 6 13 254 0 6 17 0 255 12:-@34x2,13:80@36x2",
        "7144 7164 11 6 11 254 1 11 17 420 255 12:-@34x2,13:-@36x2",
        "7165 7170 6 254 11 254 2 11 17 120 255 12:-@34x2,13:-@36x2",
        "7171 7251 6 254 11 254 0 11 17 0 255 12:-@34x2,13:-@36x2",
        "7252 7257 6 12 6 6 1 6 18 120 255 12:80@34x2,13:-@36x2",
        "7258 7268 12 254 6 6 2 6 18 210 255 12:80@34x2,13:-@36x2",
        "7269 7349 12 254 6 6 0 6 18 0 255 12:80@34x2,13:-@36x2",
        "7350 7355 12 11 12 6 1 11 19 120 255 12:80@34x2,13:-@36x2",
        "7356 7361 6 11 12 254 2 11 19 120 255 12:80@34x2,13:-@36x2",
        "7362 7362 6 11 12 254 0 11 19 0 255 12:80@34x2,13:-@36x2",
        "7363 7368 6 11 6 254 1 6 19 120 255 12:-@34x2,13:-@36x2",
        "7369 7407 11 254 6 254 2 6 19 780 255 12:-@34x2,13:-@36x2",
        "7408 7469 11 254 6 254 0 6 19 0 255 12:-@34x2,13:-@36x2",
    ];

    /// <summary>The client's Snap Hook node while the hook is not thrown (its item object field zeroed).</summary>
    private const string IdleItemNode = "ffffff03ffffff03ffffff03000000001a000080ffffff7fffffff7fffffff7fffffff7fffffff7f000000002803ffff000100ff";

    [Fact]
    public void Snap_hook_switches_give_the_inventory_state_item_nodes_and_layout_the_client_predicts_on_every_tick()
    {
        (_, RaidWeapons weapons, Dictionary<Guid, ulong> objects) = Prepare(durability: 100);
        // Empty hands, then the Ferro, set the state the sequence starts from.
        (uint Frame, ulong Target, byte Change)[] requests =
            [(6400, Unarmed, 11), (6450, Ferro, 12), (6879, Hook, 13), (6957, Ferro, 14), (7060, Hook, 15), (7131, Unarmed, 16), (7133, Unarmed, 17),
                (7252, Hook, 18), (7350, Ferro, 19)];
        Dictionary<uint, string> expected = [];
        foreach (string row in s_switches)
        {
            string[] cells = row.Split(' ');
            uint first = uint.Parse(cells[0], CultureInfo.InvariantCulture);
            uint timer = uint.Parse(cells[9], CultureInfo.InvariantCulture);
            for (uint tick = first; tick <= uint.Parse(cells[1], CultureInfo.InvariantCulture); tick++)
            {
                uint left = cells[6] == "0" ? 0 : timer - ((tick - first) * PawnBlackboard.TimePerTick);
                expected[tick] = string.Join(' ', [.. cells[2..9], left.ToString(CultureInfo.InvariantCulture), .. cells[10..]]);
            }
        }

        byte[] idle = new byte[PendulumRegistry.Descriptors[QuickItemProtocol.ItemNodeDescriptor].Size];
        Convert.FromHexString(IdleItemNode).CopyTo(idle, 0);
        PendulumField link = PendulumRegistry.Descriptors[QuickItemProtocol.ItemNodeDescriptor].Fields[QuickItemProtocol.ItemNodeItemInstanceField];
        List<string> differences = [];
        int compared = 0;
        for (uint frame = 6390; frame <= 7470; frame++)
        {
            (_, ulong target, byte change) = requests.LastOrDefault(request => request.Frame <= frame);
            weapons.Apply(new WeaponInput(frame, 1UL << WeaponProtocol.AllowPrimaryBit, target, change), objects);
            // As Raid sends them: the frame for the tick before the latest input.
            uint tick = frame - 1;
            if (!expected.TryGetValue(tick, out string? truth))
            {
                continue;
            }

            compared++;
            Assert.False(weapons.HoldsFrames(tick));
            IReadOnlyDictionary<int, PendulumSlot> state = PawnBlackboardFrames.State(tick, new PawnMotion(default), 0, weapons: weapons.Variables(tick, objects),
                emptySlots: weapons.EmptyItemSlots(tick));
            string raid = Describe(state);
            if (raid != truth)
            {
                differences.Add($"{tick}: {truth} / {raid}");
            }

            foreach (PendulumSlot node in state.Values.Where(slot => slot.DescriptorId == QuickItemProtocol.ItemNodeDescriptor))
            {
                Assert.Equal(Hook, PendulumCodec.Integer(link, node.Value));
                byte[] value = [.. node.Value];
                Array.Clear(value, link.Offset, 16);
                if (!value.AsSpan().SequenceEqual(idle))
                {
                    differences.Add($"{tick}: item node {Convert.ToHexString(value)}");
                }
            }
        }

        Assert.Equal(922, compared);
        Assert.True(differences.Count == 0, $"{differences.Count} ticks differ (tick: client / Raid):\n{string.Join('\n', differences.Take(20))}");
    }

    [Fact]
    public void A_thrown_snap_hook_costs_power_and_holds_frames_until_the_client_may_use_it_again()
    {
        (_, RaidWeapons weapons, Dictionary<Guid, ulong> objects) = Prepare();
        const ulong Allowed = 1UL << WeaponProtocol.AllowPrimaryBit;
        const ulong Throw = Allowed | (1UL << WeaponProtocol.FireDownBit) | (1UL << WeaponProtocol.FirePressedBit);
        Run(30, 39, Allowed, 0);
        // The Ferro goes to the offhand at once and the hook is drawn over 11 frames.
        Run(40, 60, Allowed, 1);
        Assert.Equal(WeaponProtocol.EquipPhaseIdle, Switch(weapons.Variables(60, objects)).Phase);

        // A throw costs 60 of 100 and power recharges 4.5 a second from the same frame; the client takes over meanwhile.
        Run(61, 61, Throw, 1);
        Assert.Equal(1, weapons.HookThrows);
        List<ulong> powers = [Power()];
        for (uint frame = 62; frame <= 71; frame++)
        {
            Run(frame, frame, 0, 1);
            powers.Add(Power());
        }

        Assert.Equal([102UL, 103, 103, 104, 104, 104, 105, 105, 105, 106, 106], powers);
        // A hook that hits nothing is cleared after its flight out and back (frame 99) and gives its power back; recharging
        // stopped from 0.35 s after the throw. The client allows the next use on the frame after.
        Run(72, 99, 0, 1);
        Assert.True(weapons.HoldsFrames(99));
        Assert.Throws<InvalidOperationException>(() => weapons.Variables(99, objects));
        Run(100, 100, Allowed, 1);
        Assert.False(weapons.HoldsFrames(100));
        Assert.Equal(SnapHookParameters.PowerMaximum, weapons.GadgetPower);
        Assert.Equal(255UL, Inventory(weapons.Variables(100, objects), QuickItemProtocol.GadgetPowerField));

        // A hook that holds on gives nothing back: the client allows the next use once power is back at the cost.
        Run(101, 101, Throw, 1);
        Run(102, 234, 0, 1);
        Assert.True(weapons.HoldsFrames(234));
        Run(235, 235, Allowed, 1);
        Assert.False(weapons.HoldsFrames(235));
        Assert.Equal(154UL, Inventory(weapons.Variables(235, objects), QuickItemProtocol.GadgetPowerField));
        Assert.Equal(12UL, Inventory(weapons.Variables(235, objects), WeaponProtocol.InventoryCurrentField));

        // Calling the hook back in flight refunds too.
        Run(236, 236, Throw, 1);
        Run(237, 242, 0, 1);
        Run(243, 243, Allowed, 1);
        Assert.Equal(157UL, Inventory(weapons.Variables(243, objects), QuickItemProtocol.GadgetPowerField));
        Assert.Equal(3, weapons.HookThrows);
        return;

        void Run(uint from, uint to, ulong flags, byte change)
        {
            for (uint frame = from; frame <= to; frame++)
            {
                weapons.Apply(new WeaponInput(frame, flags, change == 0 ? 0 : Hook, change), objects);
            }
        }

        ulong Power() => (ulong)Math.Round(weapons.GadgetPower / SnapHookParameters.PowerMaximum * QuickItemProtocol.GadgetPowerMaximum, MidpointRounding.AwayFromZero);
    }

    [Fact]
    public void A_snap_hook_outside_the_belt_or_unknown_to_the_client_is_not_drawn()
    {
        (RaidInventory inventory, RaidWeapons weapons, Dictionary<Guid, ulong> objects) = Prepare();
        weapons.Apply(new WeaponInput(30, 0, 0, 0), objects);
        Assert.True(inventory.Apply(ShieldTests.Swap(inventory.Belt, 0, inventory.Backpack, 6)));
        weapons.Apply(new WeaponInput(40, 0, Hook, 1), objects);
        weapons.Apply(new WeaponInput(41, 0, Hook, 1));
        Assert.Equal(0, weapons.QuickItemRequests);
        Assert.False(weapons.HoldsFrames(41));
        Assert.Equal((WeaponProtocol.EquipPhaseIdle, WeaponProtocol.FirstWeaponSlot, WeaponProtocol.NoItemHandle, WeaponProtocol.FirstWeaponSlot),
            Switch(weapons.Variables(41, objects)));

        Assert.True(inventory.Apply(ShieldTests.Swap(inventory.Backpack, 6, inventory.Belt, 2)));
        weapons.Apply(new WeaponInput(50, 0, Hook, 2), new Dictionary<Guid, ulong>(objects.Where(entry => entry.Value != Hook)));
        Assert.Equal(0, weapons.QuickItemRequests);
        Assert.False(weapons.HoldsFrames(50));
    }

    [Theory]
    [InlineData(50u)]
    [InlineData(80u)]
    public void Moving_the_snap_hook_out_of_the_belt_while_it_is_drawn_or_in_hand_leaves_empty_hands(uint movedAt)
    {
        (RaidInventory inventory, RaidWeapons weapons, Dictionary<Guid, ulong> objects) = Prepare();
        weapons.Apply(new WeaponInput(30, 0, 0, 0), objects);
        weapons.Apply(new WeaponInput(40, 0, Hook, 1), objects);
        weapons.Apply(new WeaponInput(movedAt - 1, 0, Hook, 1), objects);
        Assert.Contains(weapons.Variables(movedAt - 1, objects), variable => variable.DescriptorId == QuickItemProtocol.ItemNodeDescriptor);
        uint version = weapons.Version;
        Assert.True(inventory.Apply(ShieldTests.Swap(inventory.Belt, 0, inventory.Backpack, 6)));
        weapons.Apply(new WeaponInput(movedAt, 0, Hook, 1), objects);
        Assert.NotEqual(version, weapons.Version);
        weapons.Apply(new WeaponInput(120, 0, Hook, 1), objects);
        Assert.False(weapons.Armed);
        Assert.False(weapons.HoldsFrames(120));
        Assert.Equal((WeaponProtocol.EquipPhaseIdle, WeaponProtocol.UnarmedHandle, WeaponProtocol.NoItemHandle, WeaponProtocol.FirstWeaponSlot),
            Switch(weapons.Variables(120, objects)));
        Assert.DoesNotContain(weapons.Variables(120, objects), variable => variable.DescriptorId == QuickItemProtocol.ItemNodeDescriptor);
        Assert.Equal(12, Assert.Single(weapons.EmptyItemSlots(120)).Slot);
    }

    [Fact]
    public void Moving_the_snap_hook_after_switching_back_to_the_ferro_drops_its_node_and_changes_the_frame()
    {
        (RaidInventory inventory, RaidWeapons weapons, Dictionary<Guid, ulong> objects) = Prepare();
        weapons.Apply(new WeaponInput(30, 0, 0, 0), objects);
        weapons.Apply(new WeaponInput(40, 0, Hook, 1), objects);
        weapons.Apply(new WeaponInput(70, 0, Ferro, 2), objects);
        // The hook is put away over 6 frames, then the Ferro comes back from the offhand over 6 more; the hook keeps its node
        // while the inventory state names it as the item put away.
        weapons.Apply(new WeaponInput(85, 0, Ferro, 2), objects);
        Assert.Equal((WeaponProtocol.EquipPhaseIdle, WeaponProtocol.FirstWeaponSlot, WeaponProtocol.NoItemHandle, WeaponProtocol.FirstWeaponSlot),
            Switch(weapons.Variables(85, objects)));
        Assert.Equal(12UL, Inventory(weapons.Variables(85, objects), WeaponProtocol.InventoryUnequipFromField));
        uint version = weapons.Version;

        Assert.True(inventory.Apply(ShieldTests.Swap(inventory.Belt, 0, inventory.Backpack, 6)));
        weapons.Apply(new WeaponInput(86, 0, Ferro, 2), objects);
        Assert.NotEqual(version, weapons.Version);
        Assert.False(weapons.HoldsFrames(86));
        Assert.Equal((ulong)WeaponProtocol.FirstWeaponSlot, Inventory(weapons.Variables(86, objects), WeaponProtocol.InventoryUnequipFromField));
        Assert.DoesNotContain(weapons.Variables(86, objects), variable => variable.DescriptorId == QuickItemProtocol.ItemNodeDescriptor);
        Assert.True(weapons.Armed);
    }

    [Fact]
    public void Pawn_frames_go_out_while_the_snap_hook_is_drawn_and_carry_its_linked_item_node()
    {
        ScriptedPlayer session = new(loadout: AddSnapHook(WeaponTests.WithWeapons(), 0));
        session.Possess();
        RaidInventory inventory = session.Member.Inventory;
        ulong hook = session.Player.HeldItems[inventory.Containers[inventory.Belt].Items[0]!.Id];
        session.Sent.Clear();
        session.Input(30, 33, position: session.Member.Spawn.Location);
        session.Service(100);
        Dictionary<int, PendulumSlot> frame = session.PawnFrame(session.SpawnState);
        session.Input(33, 36, position: session.Member.Spawn.Location);
        session.Sent.Clear();
        session.Service(100.1);
        frame = session.PawnFrame(frame);
        session.Service(100.2);
        session.Service(100.3);
        session.Input(36, 39, position: session.Member.Spawn.Location);
        session.Sent.Clear();
        session.Service(100.4);
        frame = session.PawnFrame(frame);

        int sent = session.Channels.BlackboardFrames;
        double now = 100.5;
        session.Equip(40, hook, 1);
        for (uint latest = 42; latest <= 75; latest += 3)
        {
            session.AcknowledgeSent();
            session.Input(latest - 3, latest, position: session.Member.Spawn.Location);
            session.Sent.Clear();
            session.Service(now += 0.1);
            if (session.Sent.Any(item => item.Channel == 6 && (item.Payload[0] & 3) == 2))
            {
                frame = session.PawnFrame(frame);
            }
        }

        Assert.True(session.Channels.BlackboardFrames > sent);
        Assert.Equal(0, session.Channels.QuickItemHolds);
        IReadOnlyList<PendulumField> fields = PendulumRegistry.Descriptors[WeaponProtocol.InventoryDescriptor].Fields;
        Assert.Equal(12UL, PendulumCodec.Integer(fields[WeaponProtocol.InventoryCurrentField], frame[WeaponProtocol.InventorySlot].Value));
        PendulumSlot node = frame[12];
        Assert.Equal((QuickItemProtocol.ItemNodeDescriptor, 34, QuickItemProtocol.ItemNodePages), (node.DescriptorId, node.Position, node.Size));
        Assert.Equal(hook, PendulumCodec.Integer(PendulumRegistry.Descriptors[QuickItemProtocol.ItemNodeDescriptor].Fields[QuickItemProtocol.ItemNodeItemInstanceField],
            node.Value));
        Assert.Equal(0, session.Channels.InputDecodeFailures);
    }

    private const ulong Ferro = 30;
    private const ulong Unarmed = 34;
    private const ulong Hook = 36;

    private static string Describe(IReadOnlyDictionary<int, PendulumSlot> state)
    {
        byte[] node = state[WeaponProtocol.InventorySlot].Value;
        IReadOnlyList<PendulumField> fields = PendulumRegistry.Descriptors[WeaponProtocol.InventoryDescriptor].Fields;
        ulong[] values = [.. new[]
        {
            WeaponProtocol.InventoryCurrentField, WeaponProtocol.InventoryTargetField, WeaponProtocol.InventoryUnequipFromField,
            WeaponProtocol.InventoryOffhandField, WeaponProtocol.InventoryEquipPhaseField, WeaponProtocol.InventoryLastWantedField,
            WeaponProtocol.InventoryEquipChangeField, WeaponProtocol.InventoryEquipTimerField, QuickItemProtocol.GadgetPowerField,
        }.Select(field => PendulumCodec.Integer(fields[field], node))];
        values[7] ^= 0x80000000U;
        string items = string.Join(',', state.Values.Where(slot => slot.Slot > WeaponProtocol.FirstWeaponSlot).OrderBy(slot => slot.Slot)
            .Select(slot => string.Create(CultureInfo.InvariantCulture, $"{slot.Slot}:{(slot.DescriptorId is { } id ? id.ToString(CultureInfo.InvariantCulture) : "-")}@{slot.Position}x{slot.Size}")));
        return string.Join(' ', [.. values.Select(value => value.ToString(CultureInfo.InvariantCulture)), items.Length == 0 ? "-" : items]);
    }

    private static (RaidInventory Inventory, RaidWeapons Weapons, Dictionary<Guid, ulong> Objects) Prepare(long durability = 50)
    {
        RaidInventory inventory = new(AddSnapHook(WeaponTests.WithWeapons(), 0, durability));
        ContainerItem?[] slots = [.. inventory.Containers[inventory.Loadout].Items];
        Dictionary<Guid, ulong> objects = new()
        {
            [slots[InventoryBlackboard.Firearm1Slot]!.Id] = Ferro,
            [slots[InventoryBlackboard.UnarmedSlot]!.Id] = Unarmed,
            [inventory.Containers[inventory.Belt].Items[0]!.Id] = Hook,
        };
        return (inventory, new RaidWeapons(inventory), objects);
    }

    private static (int Phase, int Current, int Target, int LastWanted) Switch(IReadOnlyList<PendulumVariable> variables)
    {
        PendulumVariable node = variables.Single(item => item.Slot == WeaponProtocol.InventorySlot);
        byte[] value = PendulumCodec.Default.Value(node.DescriptorId, node.Values);
        IReadOnlyList<PendulumField> fields = PendulumRegistry.Descriptors[node.DescriptorId].Fields;
        return ((int)PendulumCodec.Integer(fields[WeaponProtocol.InventoryEquipPhaseField], value),
            (int)PendulumCodec.Integer(fields[WeaponProtocol.InventoryCurrentField], value),
            (int)PendulumCodec.Integer(fields[WeaponProtocol.InventoryTargetField], value),
            (int)PendulumCodec.Integer(fields[WeaponProtocol.InventoryLastWantedField], value));
    }

    private static ulong Inventory(IReadOnlyList<PendulumVariable> variables, int field)
    {
        PendulumVariable node = variables.Single(item => item.Slot == WeaponProtocol.InventorySlot);
        return PendulumCodec.Integer(PendulumRegistry.Descriptors[node.DescriptorId].Fields[field], PendulumCodec.Default.Value(node.DescriptorId, node.Values));
    }

    private static PlayerLoadout WithSnapHook(long? augment, int slot) => AddSnapHook(InventoryTests.StandardLoadout(augment), slot);

    internal static PlayerLoadout AddSnapHook(PlayerLoadout loadout, int slot, long durability = 50)
    {
        InventoryItem branch = loadout.Items.Single(item => LoadoutDefinitions.Generated.StandardBranch(item.GameAssetId) == LoadoutItems.BeltSlotAssetId);
        InventoryItem container = loadout.Items.Single(item => item.InstanceId == branch.Slots[0]);
        string target = container.Slots[slot];
        InventoryItem hook = new(Guid.NewGuid().ToString("D"), GadgetItems.SnapHookAssetId, 1, [], durability, 100, "", "hook", 0);
        return loadout with { Items = [.. loadout.Items.Select(item => item.InstanceId == target ? item with { Slots = [hook.InstanceId] } : item), hook] };
    }
}
