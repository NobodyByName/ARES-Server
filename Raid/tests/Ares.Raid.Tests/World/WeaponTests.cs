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

public sealed class WeaponTests
{
    private const ulong Fire = (1UL << WeaponProtocol.FireDownBit) | (1UL << WeaponProtocol.AllowPrimaryBit);
    private const ulong Reload = 1UL << WeaponProtocol.ReloadDownBit;

    [Theory]
    [InlineData(FerroDefaults.ItemClass, 0xD0674436U)]
    [InlineData(HandItems.UnarmedItemClass, 0xD0674436U)]
    [InlineData(HandItems.RaiderToolItemClass, 0xB63AEA3CU)]
    public void Held_item_class_exports_carry_their_own_network_checksums(string itemClass, uint expected)
    {
        ScriptedPlayer session = new(loadout: WithWeapons());
        session.Flush("exports");
        ulong guid = session.Raid.Imports.Classes[itemClass];
        StaticObjectExport exported = Assert.Single(session.Sent.Where(bunch => bunch.PackageExports)
            .SelectMany(bunch => ObjectExports.Read(new BitReader(bunch.Payload, bunch.PayloadBits)).Exports.Records.Values)
            .OfType<StaticObjectExport>(), item => item.Guid == guid);
        Assert.Equal(expected, exported.Checksum);
    }

    [Fact]
    public void Populated_containers_precede_item_creation_registration_and_simulation_references()
    {
        ScriptedPlayer session = new(loadout: WithWeapons());
        session.Possess();
        ContainerItem gun = session.Member.Inventory.Containers[session.Member.Inventory.Loadout].Items[0]!;
        ulong itemGuid = session.Player.HeldItems[gun.Id];
        Assert.DoesNotContain(session.Sent, bunch => bunch.Channel == 6 && Subobject(bunch) == itemGuid);
        session.Sent.Clear();
        session.Input(30, 33, position: session.Member.Spawn.Location);
        session.Service(100);
        Dictionary<int, PendulumSlot> initial = session.PawnFrame(session.SpawnState);
        Assert.Equal((ulong)WeaponProtocol.FirstWeaponSlot, Inventory(initial, WeaponProtocol.InventoryCurrentField));
        Assert.Equal(0UL, ItemReference(initial, WeaponProtocol.FirstWeaponSlot));
        session.Input(33, 36, position: session.Member.Spawn.Location);
        session.Sent.Clear();
        session.Service(100.1);
        Dictionary<int, PendulumSlot> populated = session.PawnFrame(initial);
        Assert.Equal(0UL, ItemReference(populated, WeaponProtocol.FirstWeaponSlot));
        Assert.DoesNotContain(session.Sent, bunch => bunch.Channel == 6 && Subobject(bunch) == itemGuid);

        session.Sent.Clear();
        session.Service(100.2);
        ChannelBunch created = Assert.Single(session.Sent, bunch => bunch.Channel == 6 && Subobject(bunch) == itemGuid);
        DynamicSubobjectContent item = DynamicSubobjectContent.Read(new BitReader(created.Payload, created.PayloadBits));
        Assert.Equal(session.Raid.Imports.Classes[FerroDefaults.ItemClass], item.ClassGuid);
        Assert.Equal(session.Player.Inventory, item.OuterGuid);
        Dictionary<ulong, string> objects = session.Raid.State.Objects([]);
        foreach (ulong guid in session.Player.HeldItems.Values)
        {
            objects[guid] = ClassPaths.ItemBase;
        }
        PropertyCodec codec = new(new ValueCodec(GameReplication.Catalog, [KnownEnums.NetRole], objects));
        Dictionary<string, object?> values = codec.Decode(new PropertyLayout(GameReplication.Catalog, ClassPaths.ItemBase), item.Content);
        string expected = gun.Id.ToString("N");
        Assert.Equal(expected, string.Concat(((string[])[PropertyNames.A, PropertyNames.B, PropertyNames.C, PropertyNames.D])
            .Select(name => unchecked((uint)Assert.IsType<int>(values[$"{PropertyNames.Id}.{name}"])).ToString("x8", System.Globalization.CultureInfo.InvariantCulture))));
        Assert.Equal(gun.Durability, values[PropertyNames.DurabilityVal]);
        Assert.Equal(0, values[PropertyNames.QualityLevel]);
        Assert.DoesNotContain(session.Sent, IsReference);
        session.Sent.Clear();
        session.Service(100.3);
        ChannelBunch references = Assert.Single(session.Sent, IsReference);
        StableSubobjectContent known = StableSubobjectContent.Read(new BitReader(references.Payload, references.PayloadBits));
        Assert.Equal(session.Player.HeldItems.Values.Select(guid => (object?)guid), Assert.IsType<IReadOnlyList<object?>>(codec.Decode(
            new PropertyLayout(GameReplication.Catalog, ClassPaths.InventoryComponent), known.Content)[PropertyNames.KnownReplicatedItems], exactMatch: false));
        session.Input(36, 39, position: session.Member.Spawn.Location);
        session.Sent.Clear();
        session.Service(100.4);
        Dictionary<int, PendulumSlot> linked = session.PawnFrame(populated);
        Assert.Equal(itemGuid, ItemReference(linked, WeaponProtocol.FirstWeaponSlot));
        foreach ((int slot, int handle, int field) in ((int, int, int)[])
            [(InventoryBlackboard.MeleeSlot, HandItems.RaiderToolHandle, HandItems.RaiderToolItemInstanceField),
                (InventoryBlackboard.UnarmedSlot, WeaponProtocol.UnarmedHandle, HandItems.UnarmedItemInstanceField)])
        {
            ContainerItem hand = session.Member.Inventory.Containers[session.Member.Inventory.Loadout].Items[slot]!;
            PendulumSlot node = linked[handle];
            Assert.Equal(session.Player.HeldItems[hand.Id], PendulumCodec.Integer(
                PendulumRegistry.Descriptors[Assert.NotNull(node.DescriptorId)].Fields[field], node.Value));
        }
        Assert.True(session.Member.Inventory.Apply(Swap(session.Member.Inventory.Loadout, 0, session.Member.Inventory.Loadout, 1)));
        session.Input(39, 42, position: session.Member.Spawn.Location);
        session.Sent.Clear();
        session.Service(100.5);
        Dictionary<int, PendulumSlot> moved = session.PawnFrame(linked);
        Assert.Equal(itemGuid, ItemReference(moved, WeaponProtocol.FirstWeaponSlot + 1));
        return;

        static ulong Inventory(Dictionary<int, PendulumSlot> frame, int field) => PendulumCodec.Integer(
            PendulumRegistry.Descriptors[WeaponProtocol.InventoryDescriptor].Fields[field], frame[WeaponProtocol.InventorySlot].Value);

        static ulong ItemReference(Dictionary<int, PendulumSlot> frame, int slot) => PendulumCodec.Integer(
            PendulumRegistry.Descriptors[WeaponProtocol.WeaponDescriptor].Fields[WeaponProtocol.WeaponItemInstanceField], frame[slot].Value);

        bool IsReference(ChannelBunch bunch)
        {
            if (bunch.Channel != 6 || Subobject(bunch) != session.Player.Inventory)
            {
                return false;
            }

            StableSubobjectContent content = StableSubobjectContent.Read(new BitReader(bunch.Payload, bunch.PayloadBits));
            return codec.Decode(new PropertyLayout(GameReplication.Catalog, ClassPaths.InventoryComponent), content.Content)
                .ContainsKey(PropertyNames.KnownReplicatedItems);
        }

        static ulong Subobject(ChannelBunch bunch)
        {
            BitReader reader = new(bunch.Payload, bunch.PayloadBits);
            reader.ReadBit();
            return reader.ReadBit() ? 0 : NetGuid.Read(reader);
        }
    }

    [Fact]
    public void Weapon_replication_uses_simulation_time_units_and_the_items_default_model()
    {
        RaidInventory inventory = new(WithWeapons());
        Empty(inventory);
        RaidWeapons weapons = new(inventory);
        weapons.Apply(new WeaponInput(30, 0, 0, 0));
        Assert.Equal(780UL, Read(WeaponProtocol.WeaponEquipTimeField, 30));
        Assert.Equal(420UL, Read(WeaponProtocol.WeaponUnequipTimeField, 30));
        Assert.Equal(600UL, Read(WeaponProtocol.WeaponZoomTimeField, 30));
        Assert.Equal(0x7FFFFFFFUL, Read(WeaponProtocol.WeaponVisualIndexField, 30));
        Assert.Equal(42597UL, Read(WeaponProtocol.WeaponDurabilityField, 30));
        Assert.Equal((ulong)FerroDefaults.ModSlotCount, Read(WeaponProtocol.WeaponModSlotCountField, 30));
        Assert.All(ModSlotStates(30), state => Assert.Equal(WeaponProtocol.EmptyModSlot, state));

        weapons.Apply(new WeaponInput(100, Reload, 0, 0));
        Assert.Equal(1725UL, Read(WeaponProtocol.WeaponReloadTimeField, 100));
        Assert.Equal(0x80000000UL + 1740, Read(WeaponProtocol.WeaponReadyTimeField, 100));
        Assert.Equal(0x80000000UL + 1720, Read(WeaponProtocol.WeaponReadyTimeField, 101));
        weapons.Apply(new WeaponInput(187, 0, 0, 0));
        Assert.Equal(0x80000000UL, Read(WeaponProtocol.WeaponReadyTimeField, 187));
        return;

        ulong Read(int field, uint frame)
        {
            PendulumVariable node = weapons.Variables(frame).Single(item => item.Slot == WeaponProtocol.FirstWeaponSlot);
            byte[] bytes = PendulumCodec.Default.Value(node.DescriptorId, node.Values);
            return PendulumCodec.Integer(PendulumRegistry.Descriptors[node.DescriptorId].Fields[field], bytes);
        }

        byte[] ModSlotStates(uint frame)
        {
            PendulumVariable node = weapons.Variables(frame).Single(item => item.Slot == WeaponProtocol.FirstWeaponSlot);
            byte[] bytes = PendulumCodec.Default.Value(node.DescriptorId, node.Values);
            int offset = PendulumRegistry.Descriptors[node.DescriptorId].Fields[WeaponProtocol.WeaponModSlotStatesField].Offset;
            return bytes[offset..(offset + FerroDefaults.ModSlotCount)];
        }
    }

    [Fact]
    public void Equip_requests_draw_the_named_ferro_keep_it_for_other_targets_and_report_the_handled_count()
    {
        RaidInventory inventory = new(WithWeapons());
        RaidWeapons weapons = new(inventory);
        ContainerItem gun = inventory.Containers[inventory.Loadout].Items[0]!;
        Dictionary<Guid, ulong> objects = new() { [gun.Id] = 30 };
        weapons.Apply(new WeaponInput(30, 0, 0, 0), objects);
        Assert.Equal((ulong)WeaponProtocol.FirstWeaponSlot, Read(WeaponProtocol.InventoryCurrentField));
        Assert.Equal(0UL, Read(WeaponProtocol.InventoryEquipChangeField));

        // A target that is not a carried Ferro keeps the gun drawn but is still reported as handled.
        weapons.Apply(new WeaponInput(40, 0, 99, 1), objects);
        Assert.Equal((ulong)WeaponProtocol.FirstWeaponSlot, Read(WeaponProtocol.InventoryCurrentField));
        Assert.Equal(1UL, Read(WeaponProtocol.InventoryEquipChangeField));

        uint version = weapons.Version;
        weapons.Apply(new WeaponInput(40, 0, 30, 2), objects);
        Assert.Equal(version, weapons.Version);

        weapons.Apply(new WeaponInput(50, 0, 30, 2), objects);
        Assert.Equal((ulong)WeaponProtocol.FirstWeaponSlot, Read(WeaponProtocol.InventoryCurrentField));
        Assert.Equal(2UL, Read(WeaponProtocol.InventoryEquipChangeField));
        return;

        ulong Read(int field)
        {
            PendulumVariable node = weapons.Variables(50).Single(item => item.Slot == WeaponProtocol.InventorySlot);
            byte[] bytes = PendulumCodec.Default.Value(node.DescriptorId, node.Values);
            return PendulumCodec.Integer(PendulumRegistry.Descriptors[node.DescriptorId].Fields[field], bytes);
        }
    }

    [Fact]
    public void Reload_transfers_one_round_once_and_firing_consumes_it_without_duplicating_replayed_inputs()
    {
        RaidInventory inventory = new(WithWeapons());
        Empty(inventory);
        RaidWeapons weapons = new(inventory);
        weapons.Apply(new WeaponInput(30, 0, 0, 0));
        weapons.Apply(new WeaponInput(100, Reload, 0, 0));
        Assert.Equal(59, inventory.HeavyAmmo);
        weapons.Apply(new WeaponInput(186, 0, 0, 0));
        Assert.Equal(59, inventory.HeavyAmmo);
        weapons.Apply(new WeaponInput(187, 0, 0, 0));
        Assert.Equal(58, inventory.HeavyAmmo);
        Assert.Equal(1, inventory.Containers[inventory.Loadout].Items[0]!.AmmoInClip);
        Assert.Equal(12.15, inventory.Weight, precision: 6);
        weapons.Apply(new WeaponInput(188, Fire, 0, 0));
        weapons.Apply(new WeaponInput(188, Fire, 0, 0));
        Assert.Equal(1, weapons.ShotsFired);
        Assert.Equal(1, weapons.ReloadsCompleted);
        Assert.Equal(0, inventory.Containers[inventory.Loadout].Items[0]!.AmmoInClip);
        Assert.Equal(12.1, inventory.Weight, precision: 6);
        weapons.Apply(new WeaponInput(224, Fire, 0, 0));
        weapons.Apply(new WeaponInput(311, Fire, 0, 0));
        Assert.Equal(2, weapons.ReloadsCompleted);
        Assert.Equal(1, weapons.ShotsFired);
        weapons.Apply(new WeaponInput(312, 0, 0, 0));
        weapons.Apply(new WeaponInput(313, Fire, 0, 0));
        Assert.Equal(2, weapons.ShotsFired);
        Assert.Equal(57, inventory.HeavyAmmo);
    }

    [Fact]
    public void The_gun_in_hand_at_spawn_fires_at_once_but_blocked_fire_cannot_consume_a_chambered_round()
    {
        RaidInventory inventory = new(WithWeapons());
        Guid gun = inventory.Containers[inventory.Loadout].Items[0]!.Id;
        Assert.Equal(1, inventory.Containers[inventory.Loadout].Items[0]!.AmmoInClip);
        RaidWeapons weapons = new(inventory);
        weapons.Apply(new WeaponInput(30, Fire | (1UL << WeaponProtocol.FireBlockedBit), 0, 0));
        Assert.True(weapons.Armed);
        Assert.Equal(0, weapons.ShotsFired);
        Assert.False(inventory.SetChamber(gun, 2));
        Assert.Equal(59, inventory.HeavyAmmo);
        weapons.Apply(new WeaponInput(31, 0, 0, 0));
        weapons.Apply(new WeaponInput(32, Fire, 0, 0));
        Assert.Equal(1, weapons.ShotsFired);
    }

    [Fact]
    public void A_broken_ferro_cannot_fire_and_empty_reserves_cannot_complete_a_reload()
    {
        PlayerLoadout loadout = WithWeapons();
        loadout = loadout with { Items = [.. loadout.Items.Select(item => item.GameAssetId == WeaponItems.FerroIAssetId ? item with { Durability = 0 } : item)] };
        RaidInventory inventory = new(loadout);
        RaidWeapons weapons = new(inventory);
        weapons.Apply(new WeaponInput(30, 0, 0, 0));
        weapons.Apply(new WeaponInput(100, Fire, 0, 0));
        Assert.Equal(0, weapons.ShotsFired);
        Assert.Equal(1, inventory.Containers[inventory.Loadout].Items[0]!.AmmoInClip);

        string ammo = loadout.Items.Single(item => item.GameAssetId == WeaponItems.HeavyAmmoAssetId).InstanceId;
        loadout = loadout with { Items = [.. loadout.Items.Where(item => item.InstanceId != ammo).Select(item => item.Slots.Contains(ammo) ? item with { Slots = [] } : item)] };
        inventory = new RaidInventory(loadout);
        weapons = new RaidWeapons(inventory);
        weapons.Apply(new WeaponInput(30, 0, 0, 0));
        weapons.Apply(new WeaponInput(100, Reload, 0, 0));
        weapons.Apply(new WeaponInput(400, 0, 0, 0));
        Assert.Equal(0, inventory.HeavyAmmo);
        Assert.Equal(0, weapons.ReloadsCompleted);
        Assert.Equal(0, inventory.Containers[inventory.Loadout].Items[0]!.AmmoInClip);
    }

    [Fact]
    public void A_raid_starts_with_the_gun_loaded_from_carried_ammo_and_an_emptied_stack_leaves_its_slot()
    {
        PlayerLoadout loadout = WithWeapons();
        loadout = loadout with { Items = [.. loadout.Items.Select(item => item.GameAssetId == WeaponItems.HeavyAmmoAssetId ? item with { Amount = FerroDefaults.ClipSize } : item)] };
        RaidInventory inventory = new(loadout);
        Assert.Equal(FerroDefaults.ClipSize, inventory.Containers[inventory.Loadout].Items[0]!.AmmoInClip);
        Assert.Equal(0, inventory.HeavyAmmo);
        Assert.Null(inventory.Containers[inventory.Backpack].Items[5]);
    }

    [Fact]
    public void Removing_a_gun_during_reload_cancels_the_transfer_and_keeps_its_reserve()
    {
        RaidInventory inventory = new(WithWeapons());
        Empty(inventory);
        RaidWeapons weapons = new(inventory);
        weapons.Apply(new WeaponInput(30, 0, 0, 0));
        weapons.Apply(new WeaponInput(100, Reload, 0, 0));
        Assert.True(inventory.Apply(Swap(inventory.Loadout, 0, inventory.Backpack, 4)));
        weapons.Apply(new WeaponInput(101, 0, 0, 0));
        weapons.Apply(new WeaponInput(400, 0, 0, 0));
        Assert.Equal(0, weapons.ReloadsCompleted);
        Assert.Equal(59, inventory.HeavyAmmo);
        Assert.Equal(0, inventory.Containers[inventory.Backpack].Items[4]!.AmmoInClip);
    }

    [Fact]
    public void Native_input_packets_drive_reload_and_shot_counters_after_the_initial_container_frames()
    {
        ScriptedPlayer session = new(loadout: WithWeapons());
        session.Possess();
        Empty(session.Member.Inventory);
        session.Input(30, 33, position: session.Member.Spawn.Location);
        session.Service(100);
        session.Input(33, 36, position: session.Member.Spawn.Location);
        session.Service(100.1);
        session.Input(36, 100, position: session.Member.Spawn.Location, weaponFlags: Reload);
        session.Input(100, 103, position: session.Member.Spawn.Location);
        session.Input(103, 245, position: session.Member.Spawn.Location);
        Assert.Equal(1, session.Channels.ReloadsCompleted);
        session.Input(245, 246, position: session.Member.Spawn.Location, weaponFlags: Fire);
        session.Input(246, 249, position: session.Member.Spawn.Location);
        Assert.Equal(1, session.Channels.ShotsFired);
        Assert.Equal(0, session.Channels.InputDecodeFailures);
        Assert.Equal(58, session.Member.Inventory.HeavyAmmo);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(AugmentItems.CombatMk1AssetId)]
    [InlineData(AugmentItems.LootingMk1AssetId)]
    [InlineData(AugmentItems.TacticalMk1AssetId)]
    public void Ferro_and_heavy_ammo_prepare_and_join_with_their_identity_condition_amount_and_weight(long? augment)
    {
        PlayerLoadout loadout = WithWeapons(augment);
        ScriptedPlayer session = new(loadout: loadout);
        session.Possess();
        RaidInventory inventory = session.Member.Inventory;
        InventoryItem gun = loadout.Items.Single(item => item.GameAssetId == WeaponItems.FerroIAssetId);
        InventoryItem ammo = loadout.Items.Single(item => item.GameAssetId == WeaponItems.HeavyAmmoAssetId);
        Assert.Equal(new ContainerItem(WeaponItems.FerroIUsableIndex, Guid.Parse(gun.InstanceId), 1, .65) { AmmoInClip = 1, ModContainerId = 5 }, inventory.Containers[inventory.Loadout].Items[InventoryBlackboard.Firearm1Slot]);
        Assert.Equal(new ContainerItem(WeaponItems.HeavyAmmoUsableIndex, Guid.Parse(ammo.InstanceId), 59, 1), inventory.Containers[inventory.Backpack].Items[5]);
        Assert.Equal(12.2, inventory.Weight, precision: 6);

        session.Sent.Clear();
        session.Input(30, 33, position: session.Member.Spawn.Location);
        session.Service(100);
        Dictionary<int, PendulumSlot> first = session.PawnFrame(session.SpawnState);
        Assert.Equal(ushort.MaxValue, AssetIndex(first, InventoryBlackboard.Firearm1Slot));
        session.Input(33, 36, position: session.Member.Spawn.Location);
        session.Sent.Clear();
        session.Service(100.1);
        Dictionary<int, PendulumSlot> second = session.PawnFrame(first);
        Assert.Equal(WeaponItems.FerroIUsableIndex, AssetIndex(second, InventoryBlackboard.Firearm1Slot));
        Assert.Equal(WeaponItems.HeavyAmmoUsableIndex, AssetIndex(second, InventoryBlackboard.LoadoutSlotCount + 5));
        IReadOnlyList<PendulumField> weaponFields = PendulumRegistry.Descriptors[WeaponProtocol.WeaponDescriptor].Fields;
        Assert.Equal((ulong)WeaponItems.FerroIUsableIndex, PendulumCodec.Integer(weaponFields[WeaponProtocol.WeaponAssetField], second[WeaponProtocol.FirstWeaponSlot].Value));
    }

    [Fact]
    public void A_ferro_has_its_own_empty_mod_container_which_follows_it_and_accepts_no_moves()
    {
        RaidInventory inventory = new(WithWeapons());
        ContainerItem gun = inventory.Containers[inventory.Loadout].Items[InventoryBlackboard.Firearm1Slot]!;
        ItemContainerState mods = inventory.Containers[Assert.IsType<int>(gun.ModContainerId)];
        Assert.Equal(FerroDefaults.ModSlotCount, mods.Items.Count);
        Assert.All(mods.Items, Assert.Null);
        Assert.All(inventory.Containers.SelectMany(container => container.Items).OfType<ContainerItem>()
            .Where(item => item.AssetIndex != WeaponItems.FerroIUsableIndex), item => Assert.Null(item.ModContainerId));
        Assert.True(inventory.Apply(Swap(inventory.Loadout, InventoryBlackboard.Firearm1Slot, inventory.Loadout, InventoryBlackboard.Firearm2Slot)));
        Assert.Equal(mods.Id, inventory.Containers[inventory.Loadout].Items[InventoryBlackboard.Firearm2Slot]!.ModContainerId);
        Assert.False(inventory.Apply(Swap(inventory.Loadout, InventoryBlackboard.Firearm2Slot, mods.Id, 0)));
        Assert.False(inventory.Apply(Swap(inventory.Backpack, 5, mods.Id, 0)));
    }

    [Fact]
    public void Carried_weapons_can_move_between_backpack_and_both_weapon_slots_and_ammo_stays_in_compatible_slots()
    {
        RaidInventory inventory = new(WithWeapons());
        ContainerItem gun = inventory.Containers[inventory.Loadout].Items[InventoryBlackboard.Firearm1Slot]!;
        ContainerItem ammo = inventory.Containers[inventory.Backpack].Items[5]!;
        Assert.True(inventory.Apply(Swap(inventory.Loadout, InventoryBlackboard.Firearm1Slot, inventory.Loadout, InventoryBlackboard.Firearm2Slot)));
        Assert.True(inventory.Apply(Swap(inventory.Loadout, InventoryBlackboard.Firearm2Slot, inventory.Backpack, 4)));
        Assert.Equal(gun, inventory.Containers[inventory.Backpack].Items[4]);
        Assert.False(inventory.Apply(Swap(inventory.Backpack, 4, inventory.Belt, 0)));
        Assert.False(inventory.Apply(Swap(inventory.Backpack, 4, inventory.Loadout, InventoryBlackboard.ArmorSlot)));
        Assert.False(inventory.Apply(Swap(inventory.Backpack, 5, inventory.Loadout, InventoryBlackboard.Firearm1Slot)));
        Assert.False(inventory.Apply(Swap(inventory.Backpack, 5, inventory.Belt, 0)));
        Assert.True(inventory.Apply(Swap(inventory.Backpack, 5, inventory.SafePouch, 0)));
        Assert.Equal(ammo, inventory.Containers[inventory.SafePouch].Items[0]);
        Assert.True(inventory.Apply(Swap(inventory.Backpack, 4, inventory.Loadout, InventoryBlackboard.Firearm1Slot)));
        Assert.Equal(gun, inventory.Containers[inventory.Loadout].Items[InventoryBlackboard.Firearm1Slot]);
        Assert.Equal(12.2, inventory.Weight, precision: 6);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(61)]
    public void Invalid_ammunition_stacks_are_rejected_before_world_preparation(long amount)
    {
        PlayerLoadout loadout = WithWeapons();
        loadout = loadout with { Items = [.. loadout.Items.Select(item => item.GameAssetId == WeaponItems.HeavyAmmoAssetId ? item with { Amount = amount } : item)] };
        Assert.Throws<WorldPreparationException>(() => new RaidInventory(loadout));
    }

    [Fact]
    public void Ferro_mod_slots_must_be_present_in_order_and_empty()
    {
        PlayerLoadout loadout = WithWeapons();
        InventoryItem gun = loadout.Items.Single(item => item.GameAssetId == WeaponItems.FerroIAssetId);
        Assert.Equal(StructuralSlots(WeaponItems.FerroIAssetId), [.. gun.Slots.Select(id => loadout.Items.Single(item => item.InstanceId == id).GameAssetId)]);
        RaidInventory inventory = new(loadout);
        Assert.NotNull(inventory.Containers[inventory.Loadout].Items[InventoryBlackboard.Firearm1Slot]);

        PlayerLoadout missing = loadout with { Items = [.. loadout.Items.Where(item => !gun.Slots.Contains(item.InstanceId)).Select(item => item == gun ? item with { Slots = [] } : item)] };
        Assert.Throws<WorldPreparationException>(() => new RaidInventory(missing));
        PlayerLoadout reordered = loadout with { Items = [.. loadout.Items.Select(item => item == gun ? item with { Slots = [.. gun.Slots.Reverse()] } : item)] };
        Assert.Throws<WorldPreparationException>(() => new RaidInventory(reordered));
        InventoryItem mod = new(Guid.NewGuid().ToString("D"), ((SingleItemSlot)LoadoutDefinitions.ByAsset[WeaponItems.MuzzleModSlotAssetId].SlotConfig!).AllowedSlotAssetIds[0],
            1, [], 1, 1, "", "weapon-test", 0);
        PlayerLoadout filled = loadout with { Items = [.. loadout.Items.Select(item => item.InstanceId == gun.Slots[0] ? item with { Slots = [mod.InstanceId] } : item), mod] };
        Assert.Throws<WorldPreparationException>(() => new RaidInventory(filled));
        return;

        static IReadOnlyList<long> StructuralSlots(long asset) => ((StructuralItemSlots)LoadoutDefinitions.ByAsset[asset].SlotConfig!).SlotAssetIds;
    }

    [Fact]
    public void Ammo_without_a_firearm_still_stages_and_registers_both_hand_items()
    {
        PlayerLoadout loadout = WithWeapons();
        InventoryItem gun = loadout.Items.Single(item => item.GameAssetId == WeaponItems.FerroIAssetId);
        loadout = loadout with
        {
            Items = [.. loadout.Items.Where(item => item != gun && !gun.Slots.Contains(item.InstanceId))
                .Select(item => item.Slots.Contains(gun.InstanceId) ? item with { Slots = [] } : item)],
        };
        ScriptedPlayer session = new(loadout: loadout);
        RaidInventory inventory = session.Member.Inventory;
        Assert.NotNull(inventory.InitialBlackboard);
        Assert.Equal(4.2, inventory.Weight, precision: 6);
        Assert.Equal(2, session.Player.HeldItems.Count);
        session.Possess();
        session.Settle(30, 100);
        Assert.Equal(0, session.Channels.InputDecodeFailures);
    }

    [Fact]
    public void The_raider_tool_and_empty_hands_can_be_equipped_and_cannot_be_moved_out_of_the_loadout()
    {
        RaidInventory inventory = new(WithWeapons());
        RaidWeapons weapons = new(inventory);
        ContainerItem?[] slots = [.. inventory.Containers[inventory.Loadout].Items];
        Dictionary<Guid, ulong> objects = new()
        {
            [slots[InventoryBlackboard.Firearm1Slot]!.Id] = 30,
            [slots[InventoryBlackboard.MeleeSlot]!.Id] = 32,
            [slots[InventoryBlackboard.UnarmedSlot]!.Id] = 34,
        };
        // A raid starts with the equipped weapon drawn, before any weapon input arrives.
        Assert.True(weapons.Armed);
        weapons.Apply(new WeaponInput(30, 0, 0, 0), objects);
        Assert.True(weapons.Armed);
        Equip(40, 32, 1, HandItems.RaiderToolHandle);
        Equip(200, 34, 2, WeaponProtocol.UnarmedHandle);
        Equip(400, 30, 3, WeaponProtocol.FirstWeaponSlot);
        Assert.True(weapons.Armed);
        Assert.False(inventory.Apply(Swap(inventory.Loadout, InventoryBlackboard.MeleeSlot, inventory.Backpack, 4)));
        Assert.False(inventory.Apply(Swap(inventory.Loadout, InventoryBlackboard.UnarmedSlot, inventory.Backpack, 4)));
        Assert.Equal(slots, inventory.Containers[inventory.Loadout].Items);
        Assert.Equal(12.2, inventory.Weight, precision: 6);
        return;

        void Equip(uint frame, ulong target, byte change, int handle)
        {
            weapons.Apply(new WeaponInput(frame, 0, target, change), objects);
            frame += 100;
            weapons.Apply(new WeaponInput(frame, 0, target, change), objects);
            PendulumVariable node = weapons.Variables(frame, objects).Single(item => item.Slot == WeaponProtocol.InventorySlot);
            byte[] value = PendulumCodec.Default.Value(node.DescriptorId, node.Values);
            IReadOnlyList<PendulumField> fields = PendulumRegistry.Descriptors[node.DescriptorId].Fields;
            Assert.Equal((ulong)handle, PendulumCodec.Integer(fields[WeaponProtocol.InventoryCurrentField], value));
            Assert.Equal((ulong)WeaponProtocol.NoItemHandle, PendulumCodec.Integer(fields[WeaponProtocol.InventoryTargetField], value));
            Assert.Equal(change, PendulumCodec.Integer(fields[WeaponProtocol.InventoryEquipChangeField], value));
            // Only empty hands move at unarmed speeds; the Raider Tool moves like a weapon.
            Assert.Equal(handle != WeaponProtocol.UnarmedHandle, weapons.Armed);
        }
    }

    [Fact]
    public void Switching_puts_the_held_item_away_then_draws_the_next()
    {
        RaidInventory inventory = new(WithWeapons());
        RaidWeapons weapons = new(inventory);
        ContainerItem?[] slots = [.. inventory.Containers[inventory.Loadout].Items];
        Dictionary<Guid, ulong> objects = new()
        {
            [slots[InventoryBlackboard.Firearm1Slot]!.Id] = 30,
            [slots[InventoryBlackboard.UnarmedSlot]!.Id] = 34,
        };
        const int Ferro = WeaponProtocol.FirstWeaponSlot;
        const int Hands = WeaponProtocol.UnarmedHandle;
        weapons.Apply(new WeaponInput(30, 0, 0, 0), objects);
        Expect(30, WeaponProtocol.EquipPhaseIdle, Ferro, WeaponProtocol.NoItemHandle, 0);

        // The Ferro counts down 420 units (0.7 s at 600 units a second).
        weapons.Apply(new WeaponInput(40, 0, 34, 1), objects);
        Expect(40, WeaponProtocol.EquipPhaseUnequipping, Ferro, Hands, 420);
        Assert.True(weapons.Armed);
        Expect(60, WeaponProtocol.EquipPhaseUnequipping, Ferro, Hands, 20);

        // Empty hands are drawn at once over 120 units; the Ferro stays as the item put away.
        Expect(61, WeaponProtocol.EquipPhaseEquipping, Hands, WeaponProtocol.NoItemHandle, 120);
        Assert.False(weapons.Armed);
        Expect(66, WeaponProtocol.EquipPhaseEquipping, Hands, WeaponProtocol.NoItemHandle, 20);
        Expect(67, WeaponProtocol.EquipPhaseIdle, Hands, WeaponProtocol.NoItemHandle, 0);
        return;

        void Expect(uint frame, int phase, int current, int target, int units)
        {
            weapons.Apply(new WeaponInput(frame, 0, 34, 1), objects);
            PendulumVariable node = weapons.Variables(frame, objects).Single(item => item.Slot == WeaponProtocol.InventorySlot);
            byte[] value = PendulumCodec.Default.Value(node.DescriptorId, node.Values);
            IReadOnlyList<PendulumField> fields = PendulumRegistry.Descriptors[node.DescriptorId].Fields;
            Assert.Equal((ulong)phase, PendulumCodec.Integer(fields[WeaponProtocol.InventoryEquipPhaseField], value));
            Assert.Equal((ulong)current, PendulumCodec.Integer(fields[WeaponProtocol.InventoryCurrentField], value));
            Assert.Equal((ulong)target, PendulumCodec.Integer(fields[WeaponProtocol.InventoryTargetField], value));
            Assert.Equal((ulong)Ferro, PendulumCodec.Integer(fields[WeaponProtocol.InventoryUnequipFromField], value));
            Assert.Equal((ulong)Ferro, PendulumCodec.Integer(fields[WeaponProtocol.InventoryLastWantedField], value));
            Assert.Equal((ulong)WeaponProtocol.NoItemHandle, PendulumCodec.Integer(fields[WeaponProtocol.InventoryOffhandField], value));
            Assert.Equal((uint)units ^ 0x80000000U, PendulumCodec.Integer(fields[WeaponProtocol.InventoryEquipTimerField], value));
        }
    }

    [Fact]
    public void A_gun_cannot_fire_while_it_is_being_put_away()
    {
        RaidInventory inventory = new(WithWeapons());
        RaidWeapons weapons = new(inventory);
        ContainerItem?[] slots = [.. inventory.Containers[inventory.Loadout].Items];
        Dictionary<Guid, ulong> objects = new() { [slots[InventoryBlackboard.Firearm1Slot]!.Id] = 30, [slots[InventoryBlackboard.UnarmedSlot]!.Id] = 34 };
        weapons.Apply(new WeaponInput(30, 0, 0, 0), objects);
        weapons.Apply(new WeaponInput(40, 0, 34, 1), objects);
        weapons.Apply(new WeaponInput(41, Fire, 34, 1), objects);
        Assert.Equal(0, weapons.ShotsFired);
    }

    internal static PlayerLoadout WithWeapons(long? augment = null)
    {
        PlayerLoadout loadout = InventoryTests.StandardLoadout(augment);
        loadout = Add(loadout, LoadoutItems.Firearm1SlotAssetId, 0, WeaponItems.FerroIAssetId, 1, .65);
        return Add(loadout, LoadoutItems.BackpackSlotAssetId, 5, WeaponItems.HeavyAmmoAssetId, 60, 1);

        static PlayerLoadout Add(PlayerLoadout source, long branchAsset, int index, long asset, long amount, double condition)
        {
            InventoryItem branch = source.Items.Single(item => LoadoutDefinitions.StandardBranch(item.GameAssetId) == branchAsset);
            InventoryItem container = source.Items.Single(item => item.InstanceId == branch.Slots[0]);
            string slot = container.Slots[index];
            // Like the menu inventory, a weapon carries one empty node per mod slot.
            InventoryItem[] modSlots = LoadoutDefinitions.ByAsset[asset].SlotConfig is StructuralItemSlots slots
                ? [.. slots.SlotAssetIds.Select(slotAsset => new InventoryItem(Guid.NewGuid().ToString("D"), slotAsset, 1, [], 1, 1, "", "weapon-test", 0))]
                : [];
            InventoryItem owned = new(Guid.NewGuid().ToString("D"), asset, amount, [.. modSlots.Select(modSlot => modSlot.InstanceId)], condition, 1, "", "weapon-test", 0);
            return source with { Items = [.. source.Items.Select(item => item.InstanceId == slot ? item with { Slots = [owned.InstanceId] } : item), owned, .. modSlots] };
        }
    }

    /// <summary>Spends the round a raid starts with, so a test can reload from an empty gun.</summary>
    private static void Empty(RaidInventory inventory) =>
        Assert.True(inventory.SetChamber(inventory.Containers[inventory.Loadout].Items[0]!.Id, 0));

    private static InventoryCommand Swap(int from, int fromSlot, int to, int toSlot) => new(InventoryCommandType.SwapItems,
        new ItemContainerSlot(new ItemContainerHandle(from, 0), checked((ushort)fromSlot)),
        new ItemContainerSlot(new ItemContainerHandle(to, 0), checked((ushort)toSlot)), 0, new ItemContainerHandle(-1, ushort.MaxValue));

    private static int AssetIndex(Dictionary<int, PendulumSlot> frame, int slot)
    {
        PendulumArrayField items = (PendulumArrayField)PendulumRegistry.Descriptors[InventoryBlackboard.Descriptor].Fields[InventoryBlackboard.SlotsField];
        byte[] item = [.. frame[InventoryBlackboard.Slot].Value.AsSpan(items.Offset + (slot * items.Stride), items.Stride)];
        return checked((int)PendulumCodec.Integer(items.ElementFields[InventoryBlackboard.AssetIndexField], item));
    }
}
