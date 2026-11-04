using Ares.Structs.Core.Serialization;
using Ares.Structs.Game.Inventory;
using Ares.Structs.Game.Movement;

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
}
