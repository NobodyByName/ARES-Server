using Ares.Structs.Core.Serialization;
using Ares.Structs.Game.Inventory;
using Ares.Structs.Game.Pendulum;

namespace Ares.Structs.Tests.Game;

public sealed class ItemContainerStateTests
{
    [Fact]
    public void Container_and_slot_capacities_are_enforced()
    {
        Assert.Throws<InvalidPacketException>(() => ItemContainerState.Encode([new ItemContainerState(1, []), new ItemContainerState(1, [])]));
        Assert.Throws<InvalidPacketException>(() => ItemContainerState.Encode([new ItemContainerState(-1, [])]));
        Assert.Throws<InvalidPacketException>(() => ItemContainerState.Encode([.. Enumerable.Range(0, 33).Select(index => new ItemContainerState(index, []))]));
        Assert.Throws<InvalidPacketException>(() => ItemContainerState.Encode([new ItemContainerState(1, new ContainerItem?[129])]));
        PendulumVariable state = ItemContainerState.Encode([new ItemContainerState(0, new ContainerItem?[128])]);
        BitWriter writer = new();
        PendulumSlot slot = new(state.Slot, state.Position, state.Size, state.DescriptorId, PendulumCodec.Default.Value(state.DescriptorId, state.Values));
        PendulumCodec.Default.WriteFrame(writer, [slot], new Dictionary<int, PendulumSlot>());
        BitReader reader = new(writer.ToArray(), writer.Length);
        Assert.Equal(slot.Value, Assert.Single(PendulumCodec.Default.ReadFrame(reader, new Dictionary<int, PendulumSlot>())).Value.Value);
        Assert.Equal(0, reader.Remaining);
    }

    [Fact]
    public void An_item_references_its_mod_container_by_position_in_the_set()
    {
        ContainerItem gun = new(InventoryBlackboard.RubberDuckIndex, Guid.Parse("01234567-89ab-cdef-1234-56789abcdef0"), 1, 1) { ModContainerId = 9 };
        PendulumVariable state = ItemContainerState.Encode([new ItemContainerState(4, [gun]), new ItemContainerState(9, new ContainerItem?[3])]);
        PendulumArrayValue slots = Assert.IsType<PendulumArrayValue>(state.Values.Single(value => value.Field == InventoryBlackboard.SlotsField));
        PendulumIntegerValue mods = Assert.IsType<PendulumIntegerValue>(Assert.Single(slots.Elements).Values
            .Single(value => value.Field == InventoryBlackboard.SubcontainerField));
        Assert.Equal(1UL, mods.Value);
        Assert.Throws<InvalidPacketException>(() => ItemContainerState.Encode([new ItemContainerState(4, [gun])]));
    }

    [Theory]
    [InlineData(0, 1.0)]
    [InlineData(1000, 1.0)]
    [InlineData(1, -0.1)]
    [InlineData(1, 1.1)]
    [InlineData(1, double.NaN)]
    public void Invalid_item_values_are_rejected(int amount, double durability)
    {
        ContainerItem item = new(InventoryBlackboard.RubberDuckIndex, Guid.Parse("01234567-89ab-cdef-1234-56789abcdef0"), amount, durability);
        Assert.Throws<InvalidPacketException>(() => ItemContainerState.Encode([new ItemContainerState(0, [item])]));
    }

    [Fact]
    public void Hidden_slots_send_only_their_search_status()
    {
        ContainerItem[] loot = [.. Enumerable.Range(1, 4).Select(index => new ContainerItem(InventoryBlackboard.RubberDuckIndex, Guid.Parse($"01234567-89ab-cdef-1234-56789abcdef{index}"), 1, 1))];
        ItemContainerState searching = new(9, [.. loot, null]) { Flags = InventoryBlackboard.LootContainerFlags, SearchStatuses = [ItemSearchStatus.HiddenSearching, ItemSearchStatus.Hidden, ItemSearchStatus.Hidden, ItemSearchStatus.Hidden, ItemSearchStatus.Shown] };
        PendulumVariable state = ItemContainerState.Encode([new ItemContainerState(4, new ContainerItem?[2]), searching], open: 9);
        PendulumArrayValue slots = Assert.IsType<PendulumArrayValue>(state.Values.Single(value => value.Field == InventoryBlackboard.SlotsField));
        Assert.Equal([2, 3, 4, 5], slots.Elements.Select(element => element.Index));
        PendulumIntegerValue[] statuses = [.. slots.Elements.Select(element => Assert.IsType<PendulumIntegerValue>(Assert.Single(element.Values)))];
        Assert.All(statuses, value => Assert.Equal(InventoryBlackboard.SearchStatusField, value.Field));
        Assert.Equal([1UL, 2, 2, 2], statuses.Select(value => value.Value));
        Assert.Equal(26UL, Assert.IsType<PendulumIntegerValue>(state.Values.Single(value => value.Field == InventoryBlackboard.RevealTimeField)).Value);
        RoundTrips(state);

        PendulumVariable revealed = ItemContainerState.Encode([searching with { SearchStatuses = [ItemSearchStatus.Shown, ItemSearchStatus.HiddenSearching, ItemSearchStatus.Hidden, ItemSearchStatus.Hidden, ItemSearchStatus.Shown] }], open: 9);
        PendulumArrayElement[] elements = [.. Assert.IsType<PendulumArrayValue>(revealed.Values.Single(value => value.Field == InventoryBlackboard.SlotsField)).Elements];
        Assert.DoesNotContain(elements[0].Values, value => value.Field == InventoryBlackboard.SearchStatusField);
        Assert.Equal(PlainSlot(loot[0]).Values, elements[0].Values);
        Assert.Equal(1UL, Assert.IsType<PendulumIntegerValue>(Assert.Single(elements[1].Values)).Value);
        RoundTrips(revealed);
    }

    [Fact]
    public void Search_statuses_must_match_the_slots()
    {
        ContainerItem item = new(InventoryBlackboard.RubberDuckIndex, Guid.Parse("01234567-89ab-cdef-1234-56789abcdef0"), 1, 1);
        Assert.Throws<InvalidPacketException>(() => ItemContainerState.Encode([new ItemContainerState(0, [item, null]) { SearchStatuses = [ItemSearchStatus.Hidden] }]));
        Assert.Throws<InvalidPacketException>(() => ItemContainerState.Encode([new ItemContainerState(0, [item, null]) { SearchStatuses = [ItemSearchStatus.Shown, ItemSearchStatus.Hidden] }]));
        Assert.Throws<InvalidPacketException>(() => ItemContainerState.Encode([new ItemContainerState(0, [item]) { SearchStatuses = [(ItemSearchStatus)3] }]));
        Assert.Equal(PlainSlot(item).Values, PlainSlot(item, ItemSearchStatus.Shown).Values);
    }

    [Fact]
    public void The_dirty_count_follows_the_modification_counter_unless_given()
    {
        ItemContainerState[] containers = [new(0, new ContainerItem?[2]), new(3, new ContainerItem?[1])];
        Assert.Equal([7UL, 7UL, 7UL], Counters(ItemContainerState.Encode(containers, 7)));
        Assert.Equal([9UL, 7UL, 7UL], Counters(ItemContainerState.Encode(containers, 7, dirtyCount: 9)));
        return;

        // The dirty count, then each container's modification counter.
        static ulong[] Counters(PendulumVariable state)
        {
            PendulumIntegerValue dirty = Assert.IsType<PendulumIntegerValue>(state.Values.Single(value => value.Field == InventoryBlackboard.DirtyCountField));
            PendulumArrayValue array = Assert.IsType<PendulumArrayValue>(state.Values.Single(value => value.Field == InventoryBlackboard.ContainersField));
            return [dirty.Value, .. array.Elements.Select(element => Assert.IsType<PendulumIntegerValue>(element.Values
                .Single(value => value.Field == InventoryBlackboard.ContainerMutationField)).Value)];
        }
    }

    [Fact]
    public void Only_the_default_reveal_time_is_encoded()
    {
        Assert.Equal(1.0, ItemRevealTime.Default.Seconds);
        Assert.Same(ItemRevealTime.Default, ItemRevealTime.FromSeconds(1.0));
        Assert.Throws<ArgumentOutOfRangeException>(() => ItemRevealTime.FromSeconds(0.5));
        PendulumVariable state = ItemContainerState.Encode([new ItemContainerState(0, [])], revealTime: ItemRevealTime.FromSeconds(1.0));
        Assert.Equal(26UL, Assert.IsType<PendulumIntegerValue>(state.Values.Single(value => value.Field == InventoryBlackboard.RevealTimeField)).Value);
    }

    private static PendulumArrayElement PlainSlot(ContainerItem item, ItemSearchStatus? status = null)
    {
        ItemContainerState container = new(0, [item]) { SearchStatuses = status is { } shown ? [shown] : null };
        PendulumArrayValue slots = Assert.IsType<PendulumArrayValue>(ItemContainerState.Encode([container])
            .Values.Single(value => value.Field == InventoryBlackboard.SlotsField));
        return Assert.Single(slots.Elements);
    }

    private static void RoundTrips(PendulumVariable state)
    {
        BitWriter writer = new();
        PendulumSlot slot = new(state.Slot, state.Position, state.Size, state.DescriptorId, PendulumCodec.Default.Value(state.DescriptorId, state.Values));
        PendulumCodec.Default.WriteFrame(writer, [slot], new Dictionary<int, PendulumSlot>());
        BitReader reader = new(writer.ToArray(), writer.Length);
        Assert.Equal(slot.Value, Assert.Single(PendulumCodec.Default.ReadFrame(reader, new Dictionary<int, PendulumSlot>())).Value.Value);
        Assert.Equal(0, reader.Remaining);
    }
}
