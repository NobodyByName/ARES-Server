using System.Globalization;
using Ares.Structs.Core.Serialization;
using Ares.Structs.Game.Movement;

namespace Ares.Structs.Game.Inventory;

/// <summary>An occupied slot in the player's replicated containers.</summary>
/// <param name="AssetIndex">Index in the usable item table.</param>
/// <param name="Id">Item identity.</param>
/// <param name="Amount">Stack amount.</param>
/// <param name="Durability">Durability as a fraction of maximum durability.</param>
public sealed record ContainerItem(ushort AssetIndex, Guid Id, int Amount, double Durability);

/// <summary>A container and its slots, including empty slots.</summary>
/// <param name="Id">Container id.</param>
/// <param name="Items">Slots in order.</param>
public sealed record ItemContainerState(int Id, IReadOnlyList<ContainerItem?> Items)
{
    /// <summary>Encodes the complete container set into its pawn blackboard variable.</summary>
    /// <param name="containers">Containers in order.</param>
    /// <param name="mutation">Mutation counter shared by these containers.</param>
    /// <returns>The blackboard variable.</returns>
    public static PendulumVariable Encode(IReadOnlyList<ItemContainerState> containers, uint mutation = 1)
    {
        ArgumentNullException.ThrowIfNull(containers);
        PendulumVariable template = PawnBlackboard.InitialState.Single(variable => variable.Slot == InventoryBlackboard.Slot);
        IReadOnlyList<PendulumField> fields = PendulumRegistry.Descriptors[template.DescriptorId].Fields;
        PendulumArrayField containerArray = (PendulumArrayField)fields[InventoryBlackboard.ContainersField];
        PendulumArrayField itemArray = (PendulumArrayField)fields[InventoryBlackboard.SlotsField];
        int slotCount = containers.Sum(container => container.Items.Count);
        if (containers.Count > containerArray.Capacity || slotCount > itemArray.Capacity
            || containers.Any(container => container.Id < 0) || containers.Select(container => container.Id).Distinct().Count() != containers.Count)
        {
            throw new InvalidPacketException("Invalid player container set");
        }

        List<PendulumArrayElement> encodedContainers = [];
        List<PendulumArrayElement> items = [];
        int slot = 0;
        foreach (ItemContainerState container in containers)
        {
            encodedContainers.Add(new PendulumArrayElement(encodedContainers.Count,
            [
                new PendulumIntegerValue(InventoryBlackboard.ContainerIdField, Signed(container.Id)),
                new PendulumIntegerValue(InventoryBlackboard.ContainerFlagsField, InventoryBlackboard.ContainerFlags),
                new PendulumIntegerValue(InventoryBlackboard.ContainerMutationField, mutation),
                new PendulumIntegerValue(InventoryBlackboard.ContainerRevisionField, 0),
                new PendulumIntegerValue(InventoryBlackboard.ContainerSizeField, (ulong)container.Items.Count),
            ]));
            foreach (ContainerItem? item in container.Items)
            {
                if (item is not null)
                {
                    PendulumScalarField amount = (PendulumScalarField)itemArray.ElementFields[InventoryBlackboard.AmountField];
                    if (item.Amount <= 0 || (ulong)item.Amount >= amount.Maximum || item.AssetIndex == ushort.MaxValue
                        || !double.IsFinite(item.Durability) || item.Durability is < 0 or > 1 || item.Id == Guid.Empty)
                    {
                        throw new InvalidPacketException("Invalid player container item");
                    }

                    List<PendulumValue> values =
                    [
                        new PendulumIntegerValue(InventoryBlackboard.AmountField, (ulong)item.Amount),
                        new PendulumIntegerValue(InventoryBlackboard.QualityField, 128),
                        new PendulumIntegerValue(InventoryBlackboard.DurabilityField, (ulong)Math.Round(item.Durability * InventoryBlackboard.DurabilityScale) + 128),
                        new PendulumIntegerValue(InventoryBlackboard.AssetIndexField, item.AssetIndex),
                    ];
                    string guid = item.Id.ToString("N");
                    for (int word = 0; word < 4; word++)
                    {
                        uint value = uint.Parse(guid.AsSpan(word * 8, 8), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                        ulong encoded = value ^ 0x80000000U;
                        if (encoded == uint.MaxValue)
                        {
                            throw new InvalidPacketException("Item identity outside the container encoding range");
                        }

                        values.Add(new PendulumIntegerValue(InventoryBlackboard.GuidAField + word, encoded));
                    }

                    items.Add(new PendulumArrayElement(slot, values));
                }

                slot++;
            }
        }

        return template with
        {
            Values =
            [
                new PendulumIntegerValue(InventoryBlackboard.RevealTimeField, 0),
                new PendulumIntegerValue(InventoryBlackboard.ContainerCountField, (ulong)containers.Count),
                new PendulumIntegerValue(InventoryBlackboard.SlotCountField, (ulong)slotCount),
                new PendulumIntegerValue(InventoryBlackboard.DirtyCountField, (ushort)mutation),
                new PendulumArrayValue(InventoryBlackboard.ContainersField, encodedContainers),
                new PendulumArrayValue(InventoryBlackboard.SlotsField, items),
            ],
        };
    }

    private static ulong Signed(int value) => (uint)value ^ 0x80000000U;
}
