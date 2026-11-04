using System.Globalization;
using Ares.Common.Game.Inventory;
using Ares.Raid.World;
using Ares.Raid.World.Inventory;
using Ares.Raid.World.Movement;
using Ares.Structs.Backend.Progression;
using Ares.Structs.Core.Serialization;
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

    internal static PlayerLoadout StandardLoadout()
    {
        Dictionary<long, ItemDescription> definitions = LoadoutItems.Definitions.Concat(StorageItems.Definitions).ToDictionary(item => item.GameAssetId);
        List<InventoryItem> items = [];
        int sequence = 1;
        Create(LoadoutItems.FrameSlotAssetId);
        InventoryItem backpack = items.Single(item => item.GameAssetId == LoadoutItems.BackpackAssetId);
        int slot = items.FindIndex(item => item.InstanceId == backpack.Slots[3]);
        items[slot] = items[slot] with { Slots = [s_duckId.ToString("D")] };
        items.Add(new InventoryItem(s_duckId.ToString("D"), StorageItems.RubberDuckAssetId, 4, [], 50, 100, "", "v1", 0));
        return new PlayerLoadout(42, 7, [.. items]);

        string Create(long asset)
        {
            string id = sequence++.ToString("x8", CultureInfo.InvariantCulture) + "-0000-0000-0000-000000000000";
            IReadOnlyList<string> children = definitions[asset].SlotConfig switch
            {
                SingleItemSlot { DefaultSlotAssetId: not 0 } single => [Create(single.DefaultSlotAssetId)],
                StructuralItemSlots structure => [.. structure.SlotAssetIds.Select(Create)],
                ItemSlotArray array => [.. Enumerable.Range(0, (int)array.NumberOfSlots).Select(_ => Create(array.SlotAssetId))],
                _ => [],
            };
            items.Add(new InventoryItem(id, asset, 1, children, 0, 0, "", "v1", 0));
            return id;
        }
    }
}
