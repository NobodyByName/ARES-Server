using System.Globalization;
using Ares.Common.Game.Inventory;
using Ares.Raid.Transport;
using Ares.Raid.World;
using Ares.Raid.World.Inventory;
using Ares.Raid.World.Movement;
using Ares.Structs.Backend.Progression;
using Ares.Structs.Core.Serialization;
using Ares.Structs.Game.Characters;
using Ares.Structs.Game.Inventory;
using Ares.Structs.Game.Movement;
using Ares.Structs.Game.Replication;
using Ares.Structs.Net.Replication;
using Ares.Structs.Net.Session;

namespace Ares.Raid.Tests.World;

public sealed class InventoryTests
{
    private static readonly Guid s_duckId = Guid.Parse("01234567-89ab-cdef-1234-56789abcdef0");
    private static readonly int[] s_sizes = [5, 10, 4, 1, 0];

    [Theory]
    [InlineData(null, 30f)]
    [InlineData(AugmentItems.CombatMk1AssetId, 45f)]
    [InlineData(AugmentItems.LootingMk1AssetId, 50f)]
    [InlineData(AugmentItems.TacticalMk1AssetId, 40f)]
    public void The_join_replicates_the_equipped_augments_weight_limit_in_the_character_attributes(long? augment, float expected)
    {
        ScriptedPlayer session = new(loadout: StandardLoadout(augment));
        session.Possess();
        Dictionary<string, object?> attributes = CharacterAttributes(session);
        Assert.Equal(expected, attributes[$"{PropertyNames.MaxWeight}.{PropertyNames.BaseValue}"]);
        Assert.Equal(expected, attributes[$"{PropertyNames.MaxWeight}.{PropertyNames.CurrentValue}"]);
        Assert.Equal(1.2f, attributes[$"{PropertyNames.Weight}.{PropertyNames.BaseValue}"]);
        Assert.Equal(1.2f, attributes[$"{PropertyNames.Weight}.{PropertyNames.CurrentValue}"]);
        Assert.Equal(4, attributes.Count);
    }

    [Theory]
    [InlineData(AugmentItems.LootingMk1AssetId, null, 1, 0.3f)]
    [InlineData(AugmentItems.CombatMk1AssetId, AugmentItems.LootingMk1AssetId, 1, 1.3f)]
    [InlineData(AugmentItems.LootingMk1AssetId, AugmentItems.TacticalMk1AssetId, 4, 3.2f)]
    public void Carried_weight_in_the_join_counts_stack_amounts_and_spares_but_excludes_the_equipped_augment(long augment, long? spare, int ducks, float expected)
    {
        PlayerLoadout loadout = StandardLoadout(augment, spare);
        loadout = loadout with { Items = [.. loadout.Items.Select(item => item.GameAssetId == StorageItems.RubberDuckAssetId ? item with { Amount = ducks } : item)] };
        ScriptedPlayer session = new(loadout: loadout);
        session.Possess();
        Dictionary<string, object?> attributes = CharacterAttributes(session);
        Assert.Equal(expected, attributes[$"{PropertyNames.Weight}.{PropertyNames.BaseValue}"]);
        Assert.Equal(expected, attributes[$"{PropertyNames.Weight}.{PropertyNames.CurrentValue}"]);
    }

    [Fact]
    public void Carried_weight_is_preserved_across_supported_container_moves()
    {
        RaidInventory inventory = new(StandardLoadout(AugmentItems.LootingMk1AssetId, AugmentItems.TacticalMk1AssetId));
        Assert.Equal(3.2, inventory.Weight, precision: 6);
        Assert.True(inventory.Apply(Swap(inventory.Backpack, 3, inventory.Belt, 0)));
        Assert.Equal(3.2, inventory.Weight, precision: 6);
        Assert.True(inventory.Apply(Swap(inventory.Belt, 0, inventory.SafePouch, 0)));
        Assert.Equal(3.2, inventory.Weight, precision: 6);
        Assert.False(inventory.Apply(Swap(inventory.Backpack, 4, inventory.Belt, 0)));
        Assert.Equal(3.2, inventory.Weight, precision: 6);
        Assert.Equal(0, new RaidInventory(new PlayerLoadout(43, 0, [])).Weight);
    }

    [Fact]
    public void Selected_items_keep_their_identity_amount_and_slot_in_independent_raid_containers()
    {
        PlayerLoadout loadout = StandardLoadout();
        RaidInventory inventory = new(loadout);
        Assert.Equal(s_sizes, inventory.Containers.Select(container => container.Items.Count));
        Assert.Equal(Enumerable.Range(0, 5), inventory.Containers.Select(container => container.Id));
        ContainerItem duck = Assert.IsType<ContainerItem>(inventory.Containers[inventory.Backpack].Items[3]);
        Assert.Equal(new ContainerItem(InventoryBlackboard.RubberDuckIndex, s_duckId, 4, 0.5), duck);
        Assert.Equal(3, inventory.Containers.SelectMany(container => container.Items).OfType<ContainerItem>().Count());
        Assert.Equal(4, loadout.Items.Single(item => item.InstanceId == s_duckId.ToString("D")).Amount);
        RaidInventory empty = new(new PlayerLoadout(43, 0, []));
        Assert.Equal([HandItems.RaiderToolUsableIndex, HandItems.UnarmedUsableIndex],
            empty.Containers.SelectMany(container => container.Items).OfType<ContainerItem>().Select(item => (int)item.AssetIndex));
    }

    [Fact]
    public void The_actual_join_delivers_handles_before_possession_and_the_first_frame_carries_the_same_containers()
    {
        ScriptedPlayer session = new(loadout: StandardLoadout());
        session.Possess();
        int restart = session.Sent.FindLastIndex(bunch => bunch is { Channel: 2, PackageExports: false });
        int component = session.Sent.FindIndex(bunch =>
            bunch is { Channel: 6, Opened: false, PackageExports: false }
            && (bunch.Payload[0] & 2) == 0
            && StableSubobjectContent.Read(new BitReader(bunch.Payload, bunch.PayloadBits)).Guid == session.Player.Inventory);
        Assert.InRange(component, 0, restart - 1);
        ChannelBunch delivery = session.Sent[component];
        StableSubobjectContent content = StableSubobjectContent.Read(new BitReader(delivery.Payload, delivery.PayloadBits));
        PropertyCodec codec = new(new ValueCodec(GameReplication.Catalog, []));
        Dictionary<string, object?> handles = codec.Decode(new PropertyLayout(GameReplication.Catalog, ClassPaths.InventoryComponent), content.Content);
        Assert.Equal(LoadoutItems.StandardFrameAssetId, handles[PropertyNames.EquippedLoadoutFrameItemAssetId]);
        Assert.Equal(session.Member.Inventory.Backpack, handles[$"{PropertyNames.Backpack}.{PropertyNames.Id}"]);
        Assert.Equal((ushort)0, handles[$"{PropertyNames.Backpack}.{PropertyNames.Revision}"]);
        Assert.Equal(session.Member.Inventory.Loadout, handles[$"{PropertyNames.Loadout}.{PropertyNames.Id}"]);

        session.Service(100);
        Assert.Equal(2, session.Channels.BlackboardFrames);
        uint tick = session.Channels.InputFrame!.Value;
        IReadOnlyDictionary<int, PendulumSlot> state = PawnBlackboardFrames.State(tick, new PawnMotion(session.Member.Spawn.Location), 0, session.Member.Inventory.Blackboard);
        IReadOnlyDictionary<int, PendulumSlot> original = PawnBlackboardFrames.State(tick, new PawnMotion(session.Member.Spawn.Location), 0);
        Assert.All(state.Values.Where(slot => slot.Slot != InventoryBlackboard.Slot), slot => Assert.Equal(original[slot.Slot].Value, slot.Value));
        BitWriter writer = new();
        PawnBlackboardFrames.Write(writer, state, new Dictionary<int, PendulumSlot>());
        BitReader reader = new(writer.ToArray(), writer.Length);
        Dictionary<int, PendulumSlot> decoded = PendulumCodec.Default.ReadFrame(reader, new Dictionary<int, PendulumSlot>());
        Assert.Equal(0, reader.Remaining);
        PendulumSlot slots = decoded[InventoryBlackboard.Slot];
        IReadOnlyList<PendulumField> fields = PendulumRegistry.Descriptors[InventoryBlackboard.Descriptor].Fields;
        Assert.Equal(5UL, PendulumCodec.Integer(fields[InventoryBlackboard.ContainerCountField], slots.Value));
        Assert.Equal(20UL, PendulumCodec.Integer(fields[InventoryBlackboard.SlotCountField], slots.Value));
        PendulumArrayField items = (PendulumArrayField)fields[InventoryBlackboard.SlotsField];
        byte[] item = [.. slots.Value.AsSpan(items.Offset + (8 * items.Stride), items.Stride)];
        Assert.Equal((ulong)InventoryBlackboard.RubberDuckIndex, PendulumCodec.Integer(items.ElementFields[InventoryBlackboard.AssetIndexField], item));
        Assert.Equal(4UL, PendulumCodec.Integer(items.ElementFields[InventoryBlackboard.AmountField], item));
        Assert.Equal(0x81234567UL, PendulumCodec.Integer(items.ElementFields[InventoryBlackboard.GuidAField], item));
        Assert.Equal(0x09abcdefUL, PendulumCodec.Integer(items.ElementFields[InventoryBlackboard.GuidAField + 1], item));

        IReadOnlyDictionary<int, PendulumSlot> after = PawnBlackboardFrames.State(tick + 1, new PawnMotion(session.Member.Spawn.Location), 123, session.Member.Inventory.Blackboard);
        BitWriter delta = new();
        PawnBlackboardFrames.Write(delta, after, decoded);
        Dictionary<int, PendulumSlot> next = PendulumCodec.Default.ReadFrame(new BitReader(delta.ToArray(), delta.Length), decoded);
        Assert.Equal(slots.Value, next[InventoryBlackboard.Slot].Value);
    }

    [Fact]
    public void Unsupported_or_malformed_loadouts_fail_before_a_world_is_reserved()
    {
        PlayerLoadout loadout = StandardLoadout();
        InventoryItem[] items = [.. loadout.Items];
        int duck = Array.FindIndex(items, item => item.GameAssetId == StorageItems.RubberDuckAssetId);
        items[duck] = items[duck] with { GameAssetId = 42 };
        DamWorlds worlds = new(() => 0, "Lighting.GreyMorning", () => 0);
        Assert.Throws<WorldPreparationException>(() => worlds.Prepare("inventory", [42], [loadout with { Items = items }]));
        Assert.Null(worlds.Current);
        items[duck] = loadout.Items[duck] with { Amount = 16 };
        Assert.Throws<WorldPreparationException>(() => worlds.Prepare("inventory", [42], [loadout with { Items = items }]));
        Assert.Null(worlds.Current);
    }

    [Fact]
    public void Whole_stack_moves_preserve_identity_and_amount_without_changing_previous_snapshots()
    {
        PlayerLoadout loadout = StandardLoadout();
        RaidInventory inventory = new(loadout);
        IReadOnlyList<ItemContainerState> before = inventory.Containers;
        ContainerItem? duck = before[inventory.Backpack].Items[3];
        Assert.True(inventory.Apply(Swap(inventory.Backpack, 3, inventory.Backpack, 7)));
        Assert.Null(inventory.Containers[inventory.Backpack].Items[3]);
        Assert.Equal(duck, inventory.Containers[inventory.Backpack].Items[7]);
        Assert.Equal(duck, before[inventory.Backpack].Items[3]);
        Assert.Null(before[inventory.Backpack].Items[7]);
        Assert.True(inventory.Apply(Swap(inventory.Backpack, 7, inventory.Belt, 0)));
        Assert.Equal(duck, inventory.Containers[inventory.Belt].Items[0]);
        Assert.Null(inventory.Containers[inventory.Backpack].Items[7]);
        Assert.Equal(6, inventory.Containers.SelectMany(container => container.Items).Sum(item => item?.Amount ?? 0));
        Assert.Equal(4, loadout.Items.Single(item => item.InstanceId == s_duckId.ToString("D")).Amount);
    }

    [Fact]
    public void Invalid_handles_destinations_and_unsupported_operations_correct_without_changing_contents()
    {
        RaidInventory inventory = new(StandardLoadout());
        ContainerItem?[] original = [.. inventory.Containers.SelectMany(container => container.Items)];
        InventoryCommand valid = Swap(inventory.Backpack, 3, inventory.Backpack, 7);
        InventoryCommand[] invalid =
        [
            Swap(inventory.Backpack, 3, inventory.Loadout, 0),
            Swap(inventory.Backpack, 3, 99, 0),
            Swap(inventory.Backpack, 3, inventory.Backpack, 99),
            Swap(inventory.Backpack, 0, inventory.Backpack, 7),
            valid with { From = valid.From with { Parent = valid.From.Parent with { Revision = 1 } } },
            valid with { To = valid.To with { Parent = valid.To.Parent with { Revision = 1 } } },
        ];
        foreach (InventoryCommand command in invalid)
        {
            uint version = inventory.Version;
            Assert.False(inventory.Apply(command));
            Assert.Equal(version + 1, inventory.Version);
            Assert.Equal(original, inventory.Containers.SelectMany(container => container.Items));
        }
    }

    [Fact]
    public void Move_input_is_applied_once_and_the_correction_covers_the_request_before_client_replay()
    {
        ScriptedPlayer session = new(loadout: StandardLoadout());
        session.Possess();
        (uint start, double at) = session.Settle(30, 100);
        session.Input(start, start + 1, position: session.Member.Spawn.Location, inventory: Swap(session.Member.Inventory.Backpack, 3, session.Member.Inventory.Backpack, 7));
        Assert.Equal(1, session.Channels.InventoryMoves);
        session.Service(at);
        Assert.Equal(ScriptedPlayer.SettledFrames, session.Channels.BlackboardFrames);
        session.Input(start + 1, start + 2, position: session.Member.Spawn.Location);
        session.Service(at + 0.1);
        Assert.Equal(ScriptedPlayer.SettledFrames + 1, session.Channels.BlackboardFrames);
        Assert.Equal(1, session.Channels.InventoryMoves);
        Assert.Equal(0, session.Channels.RejectedInventoryRequests);
        session.Input(start + 1, start + 2, position: session.Member.Spawn.Location);
        Assert.Equal(1, session.Channels.InventoryMoves);
        Assert.NotNull(session.Member.Inventory.Containers[session.Member.Inventory.Backpack].Items[7]);
        session.Input(start + 2, start + 3, position: session.Member.Spawn.Location, inventory: Swap(session.Member.Inventory.Backpack, 7, session.Member.Inventory.Loadout, 0));
        session.Input(start + 3, start + 4, position: session.Member.Spawn.Location);
        session.Service(at + 0.2);
        Assert.Equal(ScriptedPlayer.SettledFrames + 2, session.Channels.BlackboardFrames);
        Assert.Equal(1, session.Channels.RejectedInventoryRequests);
        Assert.NotNull(session.Member.Inventory.Containers[session.Member.Inventory.Backpack].Items[7]);
        Assert.Null(session.Member.Inventory.Containers[session.Member.Inventory.Loadout].Items[InventoryBlackboard.Firearm1Slot]);
    }

    [Fact]
    public void Discarded_packet_copies_and_malformed_input_do_not_apply_inventory_requests()
    {
        ScriptedPlayer session = new(loadout: StandardLoadout());
        session.Possess();
        InventoryCommand command = Swap(session.Member.Inventory.Backpack, 3, session.Member.Inventory.Backpack, 7);
        byte[] payload = ScriptedPlayer.InventoryPayload(30, 31, command);
        IPlayerChannels speculative = session.Channels.Copy();
        Assert.True(speculative.ObserveDirect(PendulumInput.DirectStreamKind, payload));
        Assert.Equal(2U, session.Member.Inventory.Version);
        Assert.Null(session.Member.Inventory.Containers[session.Member.Inventory.Backpack].Items[7]);

        Assert.True(session.Channels.ObserveDirect(PendulumInput.DirectStreamKind, [.. payload, 0xFF]));
        session.Channels.Commit();
        Assert.Equal(1, session.Channels.InputDecodeFailures);
        Assert.Equal(2U, session.Member.Inventory.Version);
        Assert.True(session.Channels.ObserveDirect(PendulumInput.DirectStreamKind, payload));
        session.Channels.Commit();
        Assert.Equal(1, session.Channels.InventoryMoves);
        Assert.NotNull(session.Member.Inventory.Containers[session.Member.Inventory.Backpack].Items[7]);
    }

    private static Dictionary<string, object?> CharacterAttributes(ScriptedPlayer session)
    {
        int index = CharacterDefaults.AttributeSets.Select(set => set.Class).ToList().IndexOf(ClassPaths.PioneerCharacterAttributeSet);
        ulong guid = session.Player.AttributeSets[index];
        DynamicSubobjectContent? character = null;
        foreach (ChannelBunch bunch in session.Sent.Where(bunch => bunch is { Channel: 6, PackageExports: false, Opened: false }))
        {
            try
            {
                DynamicSubobjectContent content = DynamicSubobjectContent.Read(new BitReader(bunch.Payload, bunch.PayloadBits));
                if (content.Guid != guid)
                {
                    continue;
                }

                character = content;
                break;
            }
            catch (InvalidPacketException)
            {
                // Other pawn-channel bunches contain stable subobjects or RPCs.
            }
        }

        Assert.NotNull(character);
        PropertyCodec codec = new(new ValueCodec(GameReplication.Catalog, []));
        return codec.Decode(new PropertyLayout(GameReplication.Catalog, ClassPaths.PioneerCharacterAttributeSet), character.Content);
    }

    private static InventoryCommand Swap(int fromContainer, ushort fromSlot, int toContainer, ushort toSlot) => new(InventoryCommandType.SwapItems,
        new ItemContainerSlot(new ItemContainerHandle(fromContainer, 0), fromSlot),
        new ItemContainerSlot(new ItemContainerHandle(toContainer, 0), toSlot), 0, new ItemContainerHandle(-1, ushort.MaxValue));

    private static InventoryCommand Split(int fromContainer, ushort fromSlot, int toContainer, ushort toSlot, int amount) =>
        Swap(fromContainer, fromSlot, toContainer, toSlot) with { Type = InventoryCommandType.SplitItems, Amount = amount };

    private static InventoryCommand Drop(int container, ushort slot, int amount = 0) =>
        Swap(container, slot, container, slot) with { Type = amount == 0 ? InventoryCommandType.DropItem : InventoryCommandType.DropPartialItem, Amount = amount };

    private static ContainerItem LootDuck(int amount) =>
        new(checked((ushort)LoadoutDefinitions.UsableIndex(StorageItems.RubberDuckAssetId)), Guid.NewGuid(), amount, 1);

    [Fact]
    public void Ducks_taken_from_several_containers_stack_on_the_carried_duck_up_to_the_maximum_stack()
    {
        RaidInventory inventory = new(new PlayerLoadout(43, 0, []));
        int maximum = checked((int)LoadoutDefinitions.ByAsset[StorageItems.RubberDuckAssetId].MaxAmount);
        ContainerItem first = LootDuck(1);
        int loot = inventory.OpenWorldContainer([first, null], LootStates.UnitWeights);
        Assert.True(inventory.Apply(Swap(loot, 0, inventory.Backpack, 0)));
        Assert.Null(inventory.CloseWorldContainer()[0]);

        loot = inventory.OpenWorldContainer([LootDuck(1)], LootStates.UnitWeights);
        Assert.True(inventory.Apply(Split(loot, 0, inventory.Backpack, 0, 1), out string? rejection));
        Assert.Null(rejection);
        Assert.Equal(first with { Amount = 2 }, inventory.Containers[inventory.Backpack].Items[0]);
        Assert.Null(inventory.CloseWorldContainer()[0]);

        loot = inventory.OpenWorldContainer([LootDuck(maximum)], LootStates.UnitWeights);
        Assert.True(inventory.Apply(Split(loot, 0, inventory.Backpack, 0, maximum)));
        Assert.Equal(maximum, inventory.Containers[inventory.Backpack].Items[0]!.Amount);
        Assert.Equal(2, inventory.LootItems[0]!.Amount);
        Assert.False(inventory.Apply(Split(loot, 0, inventory.Backpack, 0, 2), out rejection));
        Assert.Equal("stack full", rejection);
        Assert.False(inventory.Apply(Split(loot, 0, inventory.Loadout, InventoryBlackboard.ArmorSlot, 2), out rejection));
        Assert.Equal("slot not allowed", rejection);
        Assert.Equal(2, inventory.LootItems[0]!.Amount);
        Assert.Equal(maximum * StorageItems.RubberDuckWeight, inventory.Weight, 6);
    }

    [Fact]
    public void Dropping_a_whole_stack_empties_the_slot_and_hands_over_the_same_item()
    {
        RaidInventory inventory = new(new PlayerLoadout(43, 0, []));
        ContainerItem ducks = LootDuck(3);
        Assert.Equal(3, inventory.PickUp(ducks));
        uint version = inventory.Version;
        Assert.True(inventory.Apply(Drop(inventory.Backpack, 0), out string? rejection));
        Assert.Null(rejection);
        Assert.Equal(version + 1, inventory.Version);
        Assert.Null(inventory.Containers[inventory.Backpack].Items[0]);
        Assert.True(inventory.TryTakeDropped(out ContainerItem dropped));
        Assert.Equal(ducks, dropped);
        Assert.False(inventory.TryTakeDropped(out _));

        ContainerItem partial = LootDuck(2);
        Assert.Equal(2, inventory.PickUp(partial));
        Assert.True(inventory.Apply(Drop(inventory.Backpack, 0, 2)));
        Assert.Null(inventory.Containers[inventory.Backpack].Items[0]);
        Assert.True(inventory.TryTakeDropped(out dropped));
        Assert.Equal(partial, dropped);
    }

    [Fact]
    public void Dropping_part_of_a_stack_leaves_the_rest_and_hands_over_a_new_item()
    {
        RaidInventory inventory = new(new PlayerLoadout(43, 0, []));
        ContainerItem ducks = LootDuck(5);
        inventory.PickUp(ducks);
        Assert.True(inventory.Apply(Drop(inventory.Backpack, 0, 2)));
        Assert.Equal(ducks with { Amount = 3 }, inventory.Containers[inventory.Backpack].Items[0]);
        Assert.True(inventory.TryTakeDropped(out ContainerItem dropped));
        Assert.Equal(ducks with { Id = dropped.Id, Amount = 2 }, dropped);
        Assert.NotEqual(ducks.Id, dropped.Id);
    }

    [Fact]
    public void Malformed_drops_are_rejected_without_changing_the_containers_or_queueing_an_item()
    {
        RaidInventory inventory = new(new PlayerLoadout(43, 0, []));
        inventory.PickUp(LootDuck(5));
        ContainerItem?[] before = [.. inventory.Containers.SelectMany(container => container.Items)];
        (InventoryCommand Command, string Reason)[] invalid =
        [
            (Drop(inventory.Backpack, 1), "empty source"),
            (Drop(inventory.Backpack, 1, 1), "empty source"),
            (Drop(99, 0), "invalid slot"),
            (Drop(inventory.Backpack, 99), "invalid slot"),
            (Drop(inventory.Backpack, 0) with { From = Drop(inventory.Backpack, 0).From with { Parent = new ItemContainerHandle(inventory.Backpack, 1) } }, "invalid slot"),
            (Drop(inventory.Backpack, 0, -1), "invalid amount"),
            (Drop(inventory.Backpack, 0, 6), "invalid amount"),
        ];
        foreach ((InventoryCommand command, string reason) in invalid)
        {
            Assert.False(inventory.Apply(command, out string? rejection));
            Assert.Equal(reason, rejection);
            Assert.Equal(before, inventory.Containers.SelectMany(container => container.Items));
            Assert.False(inventory.TryTakeDropped(out _));
        }
    }

    [Fact]
    public void Picking_up_tops_up_existing_stacks_then_uses_the_first_empty_backpack_slot()
    {
        RaidInventory inventory = new(new PlayerLoadout(43, 0, []));
        int maximum = checked((int)LoadoutDefinitions.ByAsset[StorageItems.RubberDuckAssetId].MaxAmount);
        ContainerItem carried = LootDuck(maximum - 2);
        Assert.Equal(carried.Amount, inventory.PickUp(carried));
        uint version = inventory.Version;
        ContainerItem found = LootDuck(5);
        Assert.Equal(5, inventory.PickUp(found));
        Assert.Equal(version + 1, inventory.Version);
        Assert.Equal(carried with { Amount = maximum }, inventory.Containers[inventory.Backpack].Items[0]);
        Assert.Equal(found with { Amount = 3 }, inventory.Containers[inventory.Backpack].Items[1]);
    }

    [Fact]
    public void Picking_up_into_an_empty_slot_keeps_the_items_identity()
    {
        RaidInventory inventory = new(new PlayerLoadout(43, 0, []));
        ContainerItem found = LootDuck(2);
        Assert.Equal(2, inventory.PickUp(found));
        Assert.Equal(found, inventory.Containers[inventory.Backpack].Items[0]);
    }

    [Fact]
    public void Picking_up_with_a_full_backpack_takes_only_what_tops_up_stacks()
    {
        RaidInventory inventory = new(new PlayerLoadout(43, 0, []));
        int maximum = checked((int)LoadoutDefinitions.ByAsset[StorageItems.RubberDuckAssetId].MaxAmount);
        int slots = inventory.Containers[inventory.Backpack].Items.Count;
        for (int slot = 0; slot < slots; slot++)
        {
            Assert.Equal(maximum, inventory.PickUp(LootDuck(maximum)));
        }

        uint version = inventory.Version;
        Assert.Equal(0, inventory.PickUp(LootDuck(4)));
        Assert.Equal(version, inventory.Version);

        Assert.True(inventory.Apply(Drop(inventory.Backpack, 0, 3)));
        Assert.Equal(3, inventory.PickUp(LootDuck(4)));
        Assert.Equal(maximum, inventory.Containers[inventory.Backpack].Items[0]!.Amount);
        Assert.All(inventory.Containers[inventory.Backpack].Items, Assert.NotNull);
    }

    [Fact]
    public void A_split_moves_part_of_a_stack_into_an_empty_slot_as_a_new_stack()
    {
        RaidInventory inventory = new(new PlayerLoadout(43, 0, []));
        ContainerItem pair = LootDuck(2);
        int loot = inventory.OpenWorldContainer([pair, null], LootStates.UnitWeights);
        Assert.True(inventory.Apply(Swap(loot, 0, inventory.Backpack, 0)));
        Assert.True(inventory.Apply(Split(inventory.Backpack, 0, inventory.Backpack, 1, 1)));
        Assert.Equal(pair with { Amount = 1 }, inventory.Containers[inventory.Backpack].Items[0]);
        ContainerItem split = inventory.Containers[inventory.Backpack].Items[1]!;
        Assert.Equal(pair with { Id = split.Id, Amount = 1 }, split);
        Assert.NotEqual(pair.Id, split.Id);

        ContainerItem?[] before = [.. inventory.Containers.SelectMany(container => container.Items)];
        (InventoryCommand Command, string Reason)[] invalid =
        [
            (Split(inventory.Backpack, 0, inventory.Backpack, 0, 1), "invalid slot"),
            (Split(inventory.Backpack, 0, 99, 0, 1), "invalid slot"),
            (Split(inventory.Backpack, 2, inventory.Backpack, 3, 1), "empty source"),
            (Split(inventory.Backpack, 0, inventory.Backpack, 3, 0), "invalid amount"),
            (Split(inventory.Backpack, 0, inventory.Belt, 0, 1) with { Type = InventoryCommandType.MoveFillItem }, "unsupported command"),
        ];
        foreach ((InventoryCommand command, string reason) in invalid)
        {
            Assert.False(inventory.Apply(command, out string? rejection));
            Assert.Equal(reason, rejection);
            Assert.Equal(before, inventory.Containers.SelectMany(container => container.Items));
        }

        Assert.True(inventory.Apply(Split(inventory.Backpack, 1, loot, 1, 5)));
        Assert.Equal(split, inventory.LootItems[1]);
        Assert.Null(inventory.Containers[inventory.Backpack].Items[1]);
    }

    [Theory]
    [InlineData(AugmentItems.CombatMk1AssetId, 16, 4)]
    [InlineData(AugmentItems.LootingMk1AssetId, 18, 4)]
    [InlineData(AugmentItems.TacticalMk1AssetId, 15, 5)]
    public void An_equipped_augment_selects_the_raid_frame_and_replication_uses_its_capacities(long asset, int backpack, int belt)
    {
        PlayerLoadout loadout = StandardLoadout(asset);
        ScriptedPlayer session = new(loadout: loadout);
        RaidInventory inventory = session.Member.Inventory;
        Assert.Equal(asset, inventory.AugmentAssetId);
        Assert.Equal(AugmentItems.FrameAssetIds[asset], inventory.FrameAssetId);
        Assert.Equal([5, backpack, belt, 1, 0], inventory.Containers.Select(container => container.Items.Count));
        Assert.Equal(s_duckId, inventory.Containers[inventory.Backpack].Items[3]!.Id);
        session.Possess();
        StableSubobjectContent content = session.Raid.State.InventoryContent(session.Player, inventory);
        PropertyCodec codec = new(new ValueCodec(GameReplication.Catalog, []));
        Dictionary<string, object?> properties = codec.Decode(new PropertyLayout(GameReplication.Catalog, ClassPaths.InventoryComponent), content.Content);
        Assert.Equal(inventory.FrameAssetId, properties[PropertyNames.EquippedLoadoutFrameItemAssetId]);
        Assert.True(inventory.Apply(Swap(inventory.Backpack, 3, inventory.Backpack, checked((ushort)(backpack - 1)))));
        Assert.Equal(s_duckId, inventory.Containers[inventory.Backpack].Items[backpack - 1]!.Id);
    }

    [Fact]
    public void Spare_augments_keep_their_item_identity_and_cannot_move_into_quick_use_slots()
    {
        RaidInventory inventory = new(StandardLoadout(AugmentItems.CombatMk1AssetId, AugmentItems.TacticalMk1AssetId));
        ContainerItem spare = inventory.Containers[inventory.Backpack].Items[4]!;
        Assert.Equal(AugmentItems.UsableIndices[AugmentItems.TacticalMk1AssetId], spare.AssetIndex);
        Assert.Equal(0.25, spare.Durability);
        Assert.False(inventory.Apply(Swap(inventory.Backpack, 4, inventory.Belt, 0)));
        Assert.True(inventory.Apply(Swap(inventory.Backpack, 4, inventory.Backpack, 15)));
        Assert.Equal(spare, inventory.Containers[inventory.Backpack].Items[15]);
    }

    [Fact]
    public void A_frame_that_does_not_match_the_equipped_augment_is_rejected()
    {
        PlayerLoadout loadout = StandardLoadout(AugmentItems.CombatMk1AssetId);
        InventoryItem[] items = [.. loadout.Items.Select(item => item.GameAssetId == AugmentItems.CombatMk1AssetId ? item with { GameAssetId = AugmentItems.TacticalMk1AssetId } : item)];
        Assert.Throws<WorldPreparationException>(() => new RaidInventory(loadout with { Items = items }));
    }

    [Fact]
    public void An_extraction_carries_the_whole_loadout_with_brought_identities_and_new_loot()
    {
        PlayerLoadout loadout = WeaponTests.WithWeapons(AugmentItems.CombatMk1AssetId);
        InventoryItem armorBranch = loadout.Items.Single(item => LoadoutDefinitions.StandardBranch(item.GameAssetId) == LoadoutItems.ArmorSlotAssetId);
        string armorSlot = loadout.Items.Single(item => item.InstanceId == armorBranch.Slots[0]).Slots[0];
        InventoryItem shield = new(Guid.NewGuid().ToString("D"), ShieldItems.LightShieldAssetId, 1, [], 50, 100, "", "shield", 0);
        loadout = loadout with { Items = [.. loadout.Items.Select(item => item.InstanceId == armorSlot ? item with { Slots = [shield.InstanceId] } : item), shield] };
        RaidInventory inventory = new(loadout);
        ContainerItem loot = LootDuck(1);
        int container = inventory.OpenWorldContainer([loot, LootDuck(2)], LootStates.UnitWeights);
        Assert.True(inventory.Apply(Swap(container, 0, inventory.Backpack, 0)));

        IReadOnlyList<CarriedItem> carried = CarriedHome(inventory, RaidOutcomes.Extracted);
        InventoryItem augment = loadout.Items.Single(item => item.GameAssetId == AugmentItems.CombatMk1AssetId);
        InventoryItem gun = loadout.Items.Single(item => item.GameAssetId == WeaponItems.FerroIAssetId);
        InventoryItem ammo = loadout.Items.Single(item => item.GameAssetId == WeaponItems.HeavyAmmoAssetId);
        InventoryItem duck = loadout.Items.Single(item => item.InstanceId == s_duckId.ToString("D"));
        Assert.Equal(
        [
            (CarriedPlaces.Augment, 0, augment),
            (CarriedPlaces.Shield, 0, shield),
            (CarriedPlaces.MainWeapon, 0, gun with { Slots = [] }),
            (CarriedPlaces.Backpack, 0, new InventoryItem(loot.Id.ToString("D"), StorageItems.RubberDuckAssetId, 1, [], 1, 1, "", "", 0)),
            (CarriedPlaces.Backpack, 3, duck),
            (CarriedPlaces.Backpack, 5, ammo with { Slots = [] }),
        ], carried.Select(item => (item.Place, item.Slot, item.Item with { Slots = [] })));
        Assert.All(carried, item => Assert.Empty(item.Item.Slots));

        Assert.True(inventory.Apply(Swap(inventory.Loadout, InventoryBlackboard.Firearm1Slot, inventory.Loadout, InventoryBlackboard.Firearm2Slot)));
        Assert.Equal((CarriedPlaces.SecondaryWeapon, 0, gun.InstanceId), CarriedHome(inventory, RaidOutcomes.Extracted)
            .Where(item => item.Item.GameAssetId == WeaponItems.FerroIAssetId).Select(item => (item.Place, item.Slot, item.Item.InstanceId)).Single());
    }

    [Fact]
    public void A_split_stack_comes_home_as_two_items_and_a_dropped_part_stays_behind()
    {
        RaidInventory inventory = new(StandardLoadout());
        Assert.True(inventory.Apply(Split(inventory.Backpack, 3, inventory.Belt, 0, 1)));
        Assert.True(inventory.Apply(Drop(inventory.Backpack, 3, 1)));
        Assert.True(inventory.TryTakeDropped(out ContainerItem dropped));
        Guid split = inventory.Containers[inventory.Belt].Items[0]!.Id;

        IReadOnlyList<CarriedItem> carried = CarriedHome(inventory, RaidOutcomes.Extracted);
        Assert.Equal(
        [
            (CarriedPlaces.Backpack, 3, s_duckId.ToString("D"), 2L, 50.0, 100.0),
            (CarriedPlaces.QuickUse, 0, split.ToString("D"), 1L, 0.5, 1.0),
        ], carried.Select(item => (item.Place, item.Slot, item.Item.InstanceId, item.Item.Amount, item.Item.Durability, item.Item.MaxDurability)));
        Assert.DoesNotContain(carried, item => item.Item.InstanceId == dropped.Id.ToString("D"));
    }

    [Fact]
    public void A_brought_item_picked_up_again_keeps_its_identity_with_its_new_amount_and_durability()
    {
        PlayerLoadout loadout = StandardLoadout();
        RaidInventory inventory = new(loadout);
        Assert.True(inventory.Apply(Drop(inventory.Backpack, 3)));
        Assert.True(inventory.TryTakeDropped(out ContainerItem dropped));
        Assert.Empty(CarriedHome(inventory, RaidOutcomes.Extracted));

        Assert.Equal(3, inventory.PickUp(dropped with { Amount = 3, Durability = 0.25 }));
        InventoryItem duck = loadout.Items.Single(item => item.InstanceId == s_duckId.ToString("D"));
        CarriedItem carried = Assert.Single(CarriedHome(inventory, RaidOutcomes.Extracted));
        Assert.Equal((CarriedPlaces.Backpack, 0), (carried.Place, carried.Slot));
        Assert.Equal(duck with { Amount = 3, Durability = 25, Slots = carried.Item.Slots }, carried.Item);
    }

    [Fact]
    public void A_lost_raid_carries_exactly_the_safe_pocket_items_and_loses_the_augment()
    {
        PlayerLoadout loadout = StandardLoadout(AugmentItems.CombatMk1AssetId);
        RaidInventory inventory = new(loadout);
        Assert.Empty(CarriedHome(inventory, RaidOutcomes.Lost));
        Assert.True(inventory.Apply(Split(inventory.Backpack, 3, inventory.SafePouch, 0, 1)));
        ContainerItem pocket = inventory.Containers[inventory.SafePouch].Items[0]!;

        CarriedItem carried = Assert.Single(CarriedHome(inventory, RaidOutcomes.Lost));
        Assert.Equal((CarriedPlaces.SafePockets, 0, pocket.Id.ToString("D"), 1L), (carried.Place, carried.Slot, carried.Item.InstanceId, carried.Item.Amount));
        string augment = loadout.Items.Single(item => item.GameAssetId == AugmentItems.CombatMk1AssetId).InstanceId;
        Assert.Equal([CarriedPlaces.Augment, CarriedPlaces.Backpack, CarriedPlaces.SafePockets], CarriedHome(inventory, RaidOutcomes.Extracted).Select(item => item.Place));
        Assert.Equal(augment, CarriedHome(inventory, RaidOutcomes.Extracted)[0].Item.InstanceId);
    }

    [Fact]
    public void An_empty_loadout_with_a_picked_up_duck_carries_one_backpack_item()
    {
        RaidInventory inventory = new(new PlayerLoadout(43, 0, []));
        ContainerItem ducks = LootDuck(2);
        Assert.Equal(2, inventory.PickUp(ducks));
        CarriedItem carried = Assert.Single(CarriedHome(inventory, RaidOutcomes.Extracted));
        Assert.Equal((CarriedPlaces.Backpack, 0), (carried.Place, carried.Slot));
        Assert.Equal(new InventoryItem(ducks.Id.ToString("D"), StorageItems.RubberDuckAssetId, 2, carried.Item.Slots, 1, 1, "", "", 0), carried.Item);
        Assert.Empty(carried.Item.Slots);
        Assert.Empty(CarriedHome(inventory, RaidOutcomes.Lost));
        Assert.Throws<ArgumentException>(() => inventory.CarriedHome("UNKNOWN"));
    }

    [Fact]
    public void An_extraction_unloads_the_chambered_round_onto_its_stack_without_changing_the_inventory()
    {
        PlayerLoadout loadout = WeaponTests.WithWeapons();
        InventoryItem ammo = loadout.Items.Single(item => item.GameAssetId == WeaponItems.HeavyAmmoAssetId);
        RaidInventory inventory = new(loadout);
        ContainerItem?[] before = [.. inventory.Containers.SelectMany(container => container.Items)];
        Assert.Equal(1, inventory.Containers[inventory.Loadout].Items[InventoryBlackboard.Firearm1Slot]!.AmmoInClip);

        IReadOnlyList<CarriedItem> carried = CarriedHome(inventory, RaidOutcomes.Extracted, out int lost);
        Assert.Equal(0, lost);
        Assert.Equal((CarriedPlaces.Backpack, 5, ammo.Amount), Ammo(carried).Select(item => (item.Place, item.Slot, item.Item.Amount)).Single());
        Assert.Equal(ammo.InstanceId, Ammo(carried).Single().Item.InstanceId);
        Assert.Equal(before, inventory.Containers.SelectMany(container => container.Items));

        Assert.True(inventory.SetChamber(inventory.Containers[inventory.Loadout].Items[InventoryBlackboard.Firearm1Slot]!.Id, 0));
        Assert.Equal(ammo.Amount - 1, Ammo(CarriedHome(inventory, RaidOutcomes.Extracted, out lost)).Single().Item.Amount);
        Assert.Equal(0, lost);
        Assert.Empty(CarriedHome(inventory, RaidOutcomes.Lost, out lost));
        Assert.Equal(0, lost);
    }

    [Fact]
    public void Rounds_of_two_loaded_guns_top_up_the_stack_then_start_a_new_stack_in_the_first_empty_backpack_slot()
    {
        PlayerLoadout loadout = WeaponTests.WithWeapons();
        InventoryItem ammo = loadout.Items.Single(item => item.GameAssetId == WeaponItems.HeavyAmmoAssetId);
        RaidInventory inventory = new(loadout);
        ContainerItem second = new(WeaponItems.FerroIUsableIndex, Guid.NewGuid(), 1, 1) { AmmoInClip = 1 };
        int container = inventory.OpenWorldContainer([second], LootStates.UnitWeights);
        Assert.True(inventory.Apply(Swap(container, 0, inventory.Loadout, InventoryBlackboard.Firearm2Slot)));

        IReadOnlyList<CarriedItem> carried = CarriedHome(inventory, RaidOutcomes.Extracted, out int lost);
        Assert.Equal(0, lost);
        CarriedItem[] stacks = [.. Ammo(carried).OrderBy(item => item.Slot)];
        Assert.Equal([(0, 1L), (5, ammo.Amount)], stacks.Select(item => (item.Slot, item.Item.Amount)));
        Assert.Equal(ammo.InstanceId, stacks[1].Item.InstanceId);
        Assert.NotEqual(ammo.InstanceId, stacks[0].Item.InstanceId);
        Assert.Equal(("", 0L), (stacks[0].Item.ETag, stacks[0].Item.UpdatedAt));
        Assert.Contains(carried, item => item.Place == CarriedPlaces.SecondaryWeapon && item.Item.InstanceId == second.Id.ToString("D"));
    }

    [Fact]
    public void Without_a_carried_stack_the_round_goes_into_a_new_stack_in_the_first_empty_backpack_slot()
    {
        RaidInventory inventory = new(WeaponTests.WithWeapons());
        Assert.True(inventory.Apply(Drop(inventory.Backpack, 5)));
        Assert.True(inventory.TryTakeDropped(out ContainerItem dropped));

        CarriedItem stack = Ammo(CarriedHome(inventory, RaidOutcomes.Extracted, out int lost)).Single();
        Assert.Equal(0, lost);
        Assert.Equal((CarriedPlaces.Backpack, 0, 1L), (stack.Place, stack.Slot, stack.Item.Amount));
        Assert.NotEqual(dropped.Id.ToString("D"), stack.Item.InstanceId);
    }

    [Fact]
    public void With_full_backpack_stacks_the_round_tops_up_the_safe_pocket_and_otherwise_is_lost()
    {
        RaidInventory inventory = new(WeaponTests.WithWeapons());
        int maximum = RaidInventory.MaxAmount(WeaponItems.HeavyAmmoUsableIndex);
        Assert.Equal(1, inventory.PickUp(LootAmmo(1)));
        while (inventory.PickUp(LootAmmo(maximum)) > 0)
        {
        }

        Assert.DoesNotContain(null, inventory.Containers[inventory.Backpack].Items);
        IReadOnlyList<CarriedItem> carried = CarriedHome(inventory, RaidOutcomes.Extracted, out int lost);
        Assert.Equal(1, lost);
        Assert.All(Ammo(carried), item => Assert.Equal((CarriedPlaces.Backpack, (long)maximum), (item.Place, item.Item.Amount)));
        Assert.DoesNotContain(carried, item => item.Place is CarriedPlaces.QuickUse or CarriedPlaces.SafePockets);

        Assert.True(inventory.Apply(Split(inventory.Backpack, 5, inventory.SafePouch, 0, 1)));
        Assert.Equal(1, inventory.PickUp(LootAmmo(1)));
        carried = CarriedHome(inventory, RaidOutcomes.Extracted, out lost);
        Assert.Equal(0, lost);
        Assert.Equal(2, Ammo(carried).Single(item => item.Place == CarriedPlaces.SafePockets).Item.Amount);
    }

    private static ContainerItem LootAmmo(int amount) => new(WeaponItems.HeavyAmmoUsableIndex, Guid.NewGuid(), amount, 1);

    private static IEnumerable<CarriedItem> Ammo(IReadOnlyList<CarriedItem> carried) =>
        carried.Where(item => item.Item.GameAssetId == WeaponItems.HeavyAmmoAssetId);

    private static IReadOnlyList<CarriedItem> CarriedHome(RaidInventory inventory, string outcome) => CarriedHome(inventory, outcome, out _);

    // What the player carries home, checked as Host would receive it.
    private static IReadOnlyList<CarriedItem> CarriedHome(RaidInventory inventory, string outcome, out int lostRounds)
    {
        IReadOnlyList<CarriedItem> carried = inventory.CarriedHome(outcome, out lostRounds);
        RaidResults.Validate(new RaidResult(Guid.NewGuid().ToString("D"), 42, 7, outcome, carried));
        return carried;
    }

    internal static PlayerLoadout StandardLoadout(long? augmentAssetId = null, long? spareAugmentAssetId = null)
    {
        IReadOnlyDictionary<long, ItemDescription> definitions = LoadoutDefinitions.ByAsset;
        List<InventoryItem> items = [];
        Queue<int> sequence = new(Enumerable.Range(1, InventorySnapshots.MaximumItems));
        Create(LoadoutItems.FrameSlotAssetId);
        InventoryItem backpackBranch = items.Single(item => LoadoutDefinitions.StandardBranch(item.GameAssetId) == LoadoutItems.BackpackSlotAssetId);
        InventoryItem backpack = items.Single(item => item.InstanceId == backpackBranch.Slots[0]);
        int slot = items.FindIndex(item => item.InstanceId == backpack.Slots[3]);
        items[slot] = items[slot] with { Slots = [s_duckId.ToString("D")] };
        items.Add(new InventoryItem(s_duckId.ToString("D"), StorageItems.RubberDuckAssetId, 4, [], 50, 100, "", "v1", 0));
        if (augmentAssetId is { } augment)
        {
            Create(AugmentItems.AugmentSlotAssetId);
            AddAugment(items.Single(item => item.GameAssetId == AugmentItems.AugmentContentSlotAssetId).InstanceId, augment);
        }

        if (spareAugmentAssetId is { } spare)
        {
            AddAugment(backpack.Slots[4], spare);
        }

        return new PlayerLoadout(42, 7, [.. items]);

        void AddAugment(string parentId, long asset)
        {
            string id = Guid.NewGuid().ToString("D");
            int parent = items.FindIndex(item => item.InstanceId == parentId);
            items[parent] = items[parent] with { Slots = [id] };
            items.Add(new InventoryItem(id, asset, 1, [], 25, 100, "", "augment", 0));
        }

        string Create(long asset)
        {
            string id = sequence.Dequeue().ToString("x8", CultureInfo.InvariantCulture) + "-0000-0000-0000-000000000000";
            IReadOnlyList<string> children = definitions[asset].SlotConfig switch
            {
                SingleItemSlot { DefaultSlotAssetId: not 0 } single => [Create(asset == LoadoutItems.FrameSlotAssetId ? LoadoutDefinitions.FrameAssetId(augmentAssetId) : single.DefaultSlotAssetId)],
                StructuralItemSlots structure => [.. structure.SlotAssetIds.Select(Create)],
                ItemSlotArray array => [.. Enumerable.Range(0, (int)array.NumberOfSlots).Select(_ => Create(array.SlotAssetId))],
                _ => [],
            };
            items.Add(new InventoryItem(id, asset, 1, children, 0, 0, "", "v1", 0));
            return id;
        }
    }
}
