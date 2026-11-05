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
        Assert.Equal(2, attributes.Count);
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
        Assert.Single(inventory.Containers.SelectMany(container => container.Items).OfType<ContainerItem>());
        Assert.Equal(4, loadout.Items.Single(item => item.InstanceId == s_duckId.ToString("D")).Amount);
        Assert.All(new RaidInventory(new PlayerLoadout(43, 0, [])).Containers.SelectMany(container => container.Items), Assert.Null);
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
        Assert.Equal(session.Raid.Inventory.Backpack, handles[$"{PropertyNames.Backpack}.{PropertyNames.Id}"]);
        Assert.Equal((ushort)0, handles[$"{PropertyNames.Backpack}.{PropertyNames.Revision}"]);
        Assert.Equal(session.Raid.Inventory.Loadout, handles[$"{PropertyNames.Loadout}.{PropertyNames.Id}"]);

        session.Service(100);
        Assert.Equal(1, session.Channels.BlackboardFrames);
        uint tick = session.Channels.InputFrame!.Value;
        IReadOnlyDictionary<int, PendulumSlot> state = PawnBlackboardFrames.State(tick, new PawnMotion(session.Raid.Spawn.Location), 0, session.Raid.Inventory.Blackboard);
        IReadOnlyDictionary<int, PendulumSlot> original = PawnBlackboardFrames.State(tick, new PawnMotion(session.Raid.Spawn.Location), 0);
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

        IReadOnlyDictionary<int, PendulumSlot> after = PawnBlackboardFrames.State(tick + 1, new PawnMotion(session.Raid.Spawn.Location), 123, session.Raid.Inventory.Blackboard);
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
        Assert.Equal(4, inventory.Containers.SelectMany(container => container.Items).Sum(item => item?.Amount ?? 0));
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
            valid with { Type = InventoryCommandType.DropItem },
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
        session.Input(30, 33, position: session.Raid.Spawn.Location);
        session.Service(100);
        Assert.Equal(1, session.Channels.BlackboardFrames);
        session.Input(33, 34, position: session.Raid.Spawn.Location, inventory: Swap(session.Raid.Inventory.Backpack, 3, session.Raid.Inventory.Backpack, 7));
        Assert.Equal(1, session.Channels.InventoryMoves);
        session.Service(100.1);
        Assert.Equal(1, session.Channels.BlackboardFrames);
        session.Input(34, 35, position: session.Raid.Spawn.Location);
        session.Service(100.2);
        Assert.Equal(2, session.Channels.BlackboardFrames);
        Assert.Equal(1, session.Channels.InventoryMoves);
        Assert.Equal(0, session.Channels.RejectedInventoryRequests);
        session.Input(34, 35, position: session.Raid.Spawn.Location);
        Assert.Equal(1, session.Channels.InventoryMoves);
        Assert.NotNull(session.Raid.Inventory.Containers[session.Raid.Inventory.Backpack].Items[7]);
        session.Input(35, 36, position: session.Raid.Spawn.Location, inventory: Swap(session.Raid.Inventory.Backpack, 7, session.Raid.Inventory.Loadout, 0));
        session.Input(36, 37, position: session.Raid.Spawn.Location);
        session.Service(100.3);
        Assert.Equal(3, session.Channels.BlackboardFrames);
        Assert.Equal(1, session.Channels.RejectedInventoryRequests);
        Assert.NotNull(session.Raid.Inventory.Containers[session.Raid.Inventory.Backpack].Items[7]);
        Assert.All(session.Raid.Inventory.Containers[session.Raid.Inventory.Loadout].Items, Assert.Null);
    }

    [Fact]
    public void Discarded_packet_copies_and_malformed_input_do_not_apply_inventory_requests()
    {
        ScriptedPlayer session = new(loadout: StandardLoadout());
        session.Possess();
        InventoryCommand command = Swap(session.Raid.Inventory.Backpack, 3, session.Raid.Inventory.Backpack, 7);
        byte[] payload = ScriptedPlayer.InventoryPayload(30, 31, command);
        IPlayerChannels speculative = session.Channels.Copy();
        Assert.True(speculative.ObserveDirect(PendulumInput.DirectStreamKind, payload));
        Assert.Equal(1U, session.Raid.Inventory.Version);
        Assert.Null(session.Raid.Inventory.Containers[session.Raid.Inventory.Backpack].Items[7]);

        Assert.True(session.Channels.ObserveDirect(PendulumInput.DirectStreamKind, [.. payload, 0xFF]));
        session.Channels.Commit();
        Assert.Equal(1, session.Channels.InputDecodeFailures);
        Assert.Equal(1U, session.Raid.Inventory.Version);
        Assert.True(session.Channels.ObserveDirect(PendulumInput.DirectStreamKind, payload));
        session.Channels.Commit();
        Assert.Equal(1, session.Channels.InventoryMoves);
        Assert.NotNull(session.Raid.Inventory.Containers[session.Raid.Inventory.Backpack].Items[7]);
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

    [Theory]
    [InlineData(AugmentItems.CombatMk1AssetId, 16, 4)]
    [InlineData(AugmentItems.LootingMk1AssetId, 18, 4)]
    [InlineData(AugmentItems.TacticalMk1AssetId, 15, 5)]
    public void An_equipped_augment_selects_the_raid_frame_and_replication_uses_its_capacities(long asset, int backpack, int belt)
    {
        PlayerLoadout loadout = StandardLoadout(asset);
        ScriptedPlayer session = new(loadout: loadout);
        RaidInventory inventory = session.Raid.Inventory;
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
