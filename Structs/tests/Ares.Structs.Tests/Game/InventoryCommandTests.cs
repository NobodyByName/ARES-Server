using Ares.Structs.Core.Serialization;
using Ares.Structs.Game.Inventory;
using Ares.Structs.Game.Movement;

namespace Ares.Structs.Tests.Game;

public sealed class InventoryCommandTests
{
    [Fact]
    public void Swap_input_decodes_container_generations_and_slot_indices()
    {
        byte[] value = Convert.FromHexString("010000800100008000000080ffffff7f002000000000000000000400ffff04");
        PendulumSlot slot = new(InventoryInput.Slot, 0, 1, InventoryInput.Descriptor, value);
        InventoryCommand? command = InventoryCommand.Read(slot);
        Assert.Equal(new InventoryCommand(InventoryCommandType.SwapItems,
            new ItemContainerSlot(new ItemContainerHandle(1, 0), 0),
            new ItemContainerSlot(new ItemContainerHandle(1, 0), 4), 0,
            new ItemContainerHandle(-1, ushort.MaxValue)), command);
        value[30] = 0;
        Assert.Null(InventoryCommand.Read(slot));
    }

    [Fact]
    public void Unrecognized_commands_and_incorrect_descriptors_are_rejected()
    {
        byte[] value = PendulumCodec.Default.DefaultValue(InventoryInput.Descriptor);
        value[30] = 6;
        PendulumSlot slot = new(InventoryInput.Slot, 0, 1, InventoryInput.Descriptor, value);
        Assert.Throws<InvalidPacketException>(() => InventoryCommand.Read(slot));
        Assert.Throws<InvalidPacketException>(() => InventoryCommand.Read(slot with { DescriptorId = InventoryBlackboard.Descriptor }));
        Assert.Throws<InvalidPacketException>(() => InventoryCommand.Read(slot with { Value = [] }));
    }
}
