using Ares.Structs.Core.Serialization;
using Ares.Structs.Game.Movement;

namespace Ares.Structs.Game.Inventory;

/// <summary>A container's identity and lifetime revision.</summary>
/// <param name="Id">Container id.</param>
/// <param name="Revision">Container lifetime revision.</param>
public readonly record struct ItemContainerHandle(int Id, ushort Revision);

/// <summary>A slot in a container.</summary>
/// <param name="Parent">The container.</param>
/// <param name="Index">Slot index.</param>
public readonly record struct ItemContainerSlot(ItemContainerHandle Parent, ushort Index);

/// <summary>An inventory request from one player input frame.</summary>
/// <param name="Type">Requested operation.</param>
/// <param name="From">Source slot.</param>
/// <param name="To">Destination slot.</param>
/// <param name="Amount">Amount or count requested by the operation.</param>
/// <param name="Target">Target container for a fill operation.</param>
public sealed record InventoryCommand(InventoryCommandType Type, ItemContainerSlot From, ItemContainerSlot To, int Amount, ItemContainerHandle Target)
{
    /// <summary>Reads a command, or null when this input state has none.</summary>
    /// <param name="slot">The inventory input state.</param>
    /// <returns>The command.</returns>
    public static InventoryCommand? Read(PendulumSlot slot)
    {
        ArgumentNullException.ThrowIfNull(slot);
        if (slot.Slot != InventoryInput.Slot || slot.DescriptorId != InventoryInput.Descriptor
            || slot.Value.Length != PendulumRegistry.Descriptors[InventoryInput.Descriptor].Size)
        {
            throw new InvalidPacketException("Invalid inventory input state");
        }

        IReadOnlyList<PendulumField> fields = PendulumRegistry.Descriptors[InventoryInput.Descriptor].Fields;
        InventoryCommandType type = (InventoryCommandType)Integer(InventoryInput.CommandField);
        if (type == InventoryCommandType.None)
        {
            return null;
        }

        if (!Enum.IsDefined(type))
        {
            throw new InvalidPacketException("Unknown inventory command");
        }

        return new InventoryCommand(type,
            new ItemContainerSlot(new ItemContainerHandle(Signed(InventoryInput.FromContainerField), (ushort)Integer(InventoryInput.FromRevisionField)), (ushort)Integer(InventoryInput.FromSlotField)),
            new ItemContainerSlot(new ItemContainerHandle(Signed(InventoryInput.ToContainerField), (ushort)Integer(InventoryInput.ToRevisionField)), (ushort)Integer(InventoryInput.ToSlotField)),
            Signed(InventoryInput.AmountField),
            new ItemContainerHandle(Signed(InventoryInput.TargetContainerField), (ushort)Integer(InventoryInput.TargetRevisionField)));

        ulong Integer(int field) => PendulumCodec.Integer(fields[field], slot.Value);

        int Signed(int field) => unchecked((int)((uint)Integer(field) ^ 0x80000000U));
    }
}
