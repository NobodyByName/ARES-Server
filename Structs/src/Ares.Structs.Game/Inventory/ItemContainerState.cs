using System.Globalization;
using Ares.Structs.Core.Serialization;
using Ares.Structs.Game.Movement;
using Ares.Structs.Game.Pendulum;

namespace Ares.Structs.Game.Inventory;

/// <summary>An occupied slot in the player's replicated containers.</summary>
/// <param name="AssetIndex">Index in the usable item table.</param>
/// <param name="Id">Item identity.</param>
/// <param name="Amount">Stack amount.</param>
/// <param name="Durability">Durability as a fraction of maximum durability.</param>
public sealed record ContainerItem(ushort AssetIndex, Guid Id, int Amount, double Durability)
{
    /// <summary>Rounds loaded into this item's magazine or chamber.</summary>
    public int AmmoInClip { get; init; }

    /// <summary>Id of the container in the same set that holds this item's mods; weapons and armor need one.</summary>
    public int? ModContainerId { get; init; }

    /// <summary>Where the item came from; <see langword="null"/> leaves it unsent.</summary>
    public ItemSource? Source { get; init; }
}

/// <summary>A container and its slots, including empty slots.</summary>
/// <param name="Id">Container id.</param>
/// <param name="Items">Slots in order.</param>
public sealed record ItemContainerState(int Id, IReadOnlyList<ContainerItem?> Items)
{
    /// <summary>Container flags: <see cref="InventoryBlackboard.ContainerFlags"/> for the player's own containers.</summary>
    public int Flags { get; init; } = InventoryBlackboard.ContainerFlags;

    /// <summary>
    /// Search status of each slot, in the order of <see cref="Items"/>; <see langword="null"/> when every slot is shown.
    /// A hidden slot sends only its status, none of its item; empty slots are always shown.
    /// </summary>
    public IReadOnlyList<ItemSearchStatus>? SearchStatuses { get; init; }

    /// <summary>Encodes the complete container set into its pawn blackboard variable.</summary>
    /// <param name="containers">Containers in order.</param>
    /// <param name="mutation">Modification counter shared by these containers.</param>
    /// <param name="open">The world container the player has open, if any (one of <paramref name="containers"/>).</param>
    /// <param name="revealTime">Time one hidden slot takes to reveal; <see cref="ItemRevealTime.Default"/> when omitted.</param>
    /// <param name="dirtyCount">
    /// Update counter of the whole set, which the client follows to apply any change, including one that leaves every
    /// container's modification counter alone (a search status); <paramref name="mutation"/> when omitted.
    /// </param>
    /// <returns>The blackboard variable.</returns>
    public static PendulumVariable Encode(IReadOnlyList<ItemContainerState> containers, uint mutation = 1, int? open = null,
        ItemRevealTime? revealTime = null, uint? dirtyCount = null)
    {
        ArgumentNullException.ThrowIfNull(containers);
        PendulumVariable template = PawnBlackboard.InitialState.Single(variable => variable.Slot == InventoryBlackboard.Slot);
        IReadOnlyList<PendulumField> fields = PendulumRegistry.Descriptors[template.DescriptorId].Fields;
        PendulumArrayField containerArray = (PendulumArrayField)fields[InventoryBlackboard.ContainersField];
        PendulumArrayField itemArray = (PendulumArrayField)fields[InventoryBlackboard.SlotsField];
        int slotCount = containers.Sum(container => container.Items.Count);
        if (containers.Count > containerArray.Capacity || slotCount > itemArray.Capacity
            || containers.Any(container => container.Id < 0) || containers.Select(container => container.Id).Distinct().Count() != containers.Count
            || (open is { } openId && containers.All(container => container.Id != openId))
            || !containers.All(HasValidSearchStatuses))
        {
            throw new InvalidPacketException("Invalid player container set");
        }

        Dictionary<int, int> positions = containers.Select((container, position) => (container.Id, position))
            .ToDictionary(entry => entry.Id, entry => entry.position);
        List<PendulumArrayElement> encodedContainers = [];
        List<PendulumArrayElement> items = [];
        int slot = 0;
        foreach (ItemContainerState container in containers)
        {
            encodedContainers.Add(new PendulumArrayElement(encodedContainers.Count,
            [
                new PendulumIntegerValue(InventoryBlackboard.ContainerIdField, Signed(container.Id)),
                new PendulumIntegerValue(InventoryBlackboard.ContainerFlagsField, (ulong)container.Flags),
                new PendulumIntegerValue(InventoryBlackboard.ContainerMutationField, mutation),
                new PendulumIntegerValue(InventoryBlackboard.ContainerRevisionField, 0),
                new PendulumIntegerValue(InventoryBlackboard.ContainerSizeField, (ulong)container.Items.Count),
            ]));
            for (int index = 0; index < container.Items.Count; index++)
            {
                ContainerItem? item = container.Items[index];
                if (item is not null)
                {
                    PendulumScalarField amount = (PendulumScalarField)itemArray.ElementFields[InventoryBlackboard.AmountField];
                    if (item.Amount <= 0 || (ulong)item.Amount >= amount.Maximum || item.AssetIndex == ushort.MaxValue
                        || !double.IsFinite(item.Durability) || item.Durability is < 0 or > 1 || item.Id == Guid.Empty
                        || item.AmmoInClip is < 0 or > ushort.MaxValue
                        || item.Source is { } source && !Enum.IsDefined(source)
                        || item.ModContainerId is { } mods && !positions.ContainsKey(mods))
                    {
                        throw new InvalidPacketException("Invalid player container item");
                    }

                    ItemSearchStatus status = container.SearchStatuses?[index] ?? ItemSearchStatus.Shown;
                    items.Add(new PendulumArrayElement(slot, status == ItemSearchStatus.Shown
                        ? ItemValues(item, positions)
                        : [new PendulumIntegerValue(InventoryBlackboard.SearchStatusField, (ulong)status)]));
                }

                slot++;
            }
        }

        List<PendulumValue> current = open is { } id
            ? [new PendulumIntegerValue(InventoryBlackboard.CurrentContainerIdField, Signed(id)), new PendulumIntegerValue(InventoryBlackboard.CurrentContainerRevisionField, 0)]
            : [];
        return template with
        {
            Values =
            [
                .. current,
                new PendulumIntegerValue(InventoryBlackboard.RevealTimeField, RevealTimeValue(revealTime ?? ItemRevealTime.Default, fields)),
                new PendulumIntegerValue(InventoryBlackboard.ContainerCountField, (ulong)containers.Count),
                new PendulumIntegerValue(InventoryBlackboard.SlotCountField, (ulong)slotCount),
                new PendulumIntegerValue(InventoryBlackboard.DirtyCountField, (ushort)(dirtyCount ?? mutation)),
                new PendulumArrayValue(InventoryBlackboard.ContainersField, encodedContainers),
                new PendulumArrayValue(InventoryBlackboard.SlotsField, items),
            ],
        };
    }

    private static bool HasValidSearchStatuses(ItemContainerState container) => container.SearchStatuses is not { } statuses
        || (statuses.Count == container.Items.Count
            && Enumerable.Range(0, statuses.Count).All(index => statuses[index] == ItemSearchStatus.Shown
                || (Enum.IsDefined(statuses[index]) && container.Items[index] is not null)));

    private static List<PendulumValue> ItemValues(ContainerItem item, Dictionary<int, int> positions)
    {
        List<PendulumValue> values =
        [
            new PendulumIntegerValue(InventoryBlackboard.AmountField, (ulong)item.Amount),
            new PendulumIntegerValue(InventoryBlackboard.QualityField, 128),
            new PendulumIntegerValue(InventoryBlackboard.DurabilityField, (ulong)Math.Round(item.Durability * InventoryBlackboard.DurabilityScale) + 128),
            new PendulumIntegerValue(InventoryBlackboard.AssetIndexField, item.AssetIndex),
            new PendulumIntegerValue(InventoryBlackboard.AmmoField, (ulong)item.AmmoInClip),
        ];
        if (item.Source is { } source)
        {
            values.Add(new PendulumIntegerValue(InventoryBlackboard.ItemSourceField, (ulong)source));
        }

        if (item.ModContainerId is { } modContainer)
        {
            values.Add(new PendulumIntegerValue(InventoryBlackboard.SubcontainerField, (ulong)positions[modContainer]));
        }

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

        return values;
    }

    // The default reveal time is the field's default value, the only encoding known.
    private static ulong RevealTimeValue(ItemRevealTime revealTime, IReadOnlyList<PendulumField> fields) => revealTime == ItemRevealTime.Default
        ? ((PendulumScalarField)fields[InventoryBlackboard.RevealTimeField]).Default
        : throw new InvalidPacketException("Reveal time outside the container encoding range");

    private static ulong Signed(int value) => (uint)value ^ 0x80000000U;
}
