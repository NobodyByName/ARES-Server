using Ares.Common.Game.Inventory;
using Ares.Structs.Backend.Progression;
using Ares.Structs.Core.Serialization;
using Ares.Structs.Game.Inventory;
using Ares.Structs.Game.Movement;

namespace Ares.Raid.World.Inventory;

/// <summary>The selected Host loadout translated into this player's raid containers.</summary>
public sealed class RaidInventory
{
    private readonly Dictionary<string, int> _handles = new(StringComparer.Ordinal);

    /// <summary>Prepares the standard loadout, preserving carried item identities and amounts.</summary>
    /// <param name="loadout">The frozen loadout accepted by this raid.</param>
    public RaidInventory(PlayerLoadout loadout)
    {
        ArgumentNullException.ThrowIfNull(loadout);
        Dictionary<string, InventoryItem> items = loadout.Items.ToDictionary(item => item.InstanceId, StringComparer.Ordinal);
        Dictionary<long, ItemDescription> definitions = LoadoutItems.Definitions.Concat(StorageItems.Definitions).ToDictionary(item => item.GameAssetId);
        HashSet<string> visited = new(StringComparer.Ordinal);
        Dictionary<long, IReadOnlyList<ContainerItem?>> branches = [];
        if (items.Count > 0)
        {
            HashSet<string> children = [.. items.Values.SelectMany(item => item.Slots)];
            InventoryItem[] roots = [.. items.Values.Where(item => !children.Contains(item.InstanceId))];
            if (roots is not [{ GameAssetId: LoadoutItems.FrameSlotAssetId } root])
            {
                throw new WorldPreparationException("Raid inventory requires a standard player frame");
            }

            InventoryItem frame = Child(root, LoadoutItems.StandardFrameAssetId);
            if (definitions[frame.GameAssetId].SlotConfig is not StructuralItemSlots frameSlots || frame.Slots.Count != frameSlots.SlotAssetIds.Count)
            {
                throw new WorldPreparationException("Invalid player frame branches");
            }

            foreach ((string id, long asset) in frame.Slots.Zip(frameSlots.SlotAssetIds))
            {
                InventoryItem branch = items[id];
                if (branch.GameAssetId != asset || definitions[asset].SlotConfig is not SingleItemSlot configuration)
                {
                    throw new WorldPreparationException("Invalid player frame branch");
                }

                InventoryItem container = Child(branch, configuration.DefaultSlotAssetId);
                if (definitions[container.GameAssetId].SlotConfig is not ItemSlotArray slots || container.Slots.Count != slots.NumberOfSlots)
                {
                    throw new WorldPreparationException("Invalid player container size");
                }

                branches[asset] = Array.AsReadOnly<ContainerItem?>([.. container.Slots.Select(slot => ReadSlot(items[slot], slots.SlotAssetId))]);
            }

            if (visited.Count != items.Count)
            {
                throw new WorldPreparationException("Player loadout contains unsupported items");
            }
        }

        List<ItemContainerState> containers = [];
        ContainerItem?[] equipped = new ContainerItem?[InventoryBlackboard.LoadoutSlotCount];
        equipped[InventoryBlackboard.Firearm1Slot] = Branch(LoadoutItems.Firearm1SlotAssetId).Single();
        equipped[InventoryBlackboard.Firearm2Slot] = Branch(LoadoutItems.Firearm2SlotAssetId).Single();
        equipped[InventoryBlackboard.ArmorSlot] = Branch(LoadoutItems.ArmorSlotAssetId).Single();
        equipped[InventoryBlackboard.MeleeSlot] = Branch(LoadoutItems.MeleeSlotAssetId).Single();
        Add(nameof(Loadout), equipped);
        Add(nameof(Backpack), Branch(LoadoutItems.BackpackSlotAssetId));
        Add(nameof(Belt), Branch(LoadoutItems.BeltSlotAssetId));
        Add(nameof(SafePouch), Branch(LoadoutItems.SafePocketSlotAssetId));
        Add(nameof(Auxiliary), []);
        Containers = containers.AsReadOnly();
        try
        {
            Blackboard = ItemContainerState.Encode(Containers);
        }
        catch (InvalidPacketException error)
        {
            throw new WorldPreparationException(error.Message);
        }

        void Add(string name, IReadOnlyList<ContainerItem?> contents)
        {
            int handle = containers.Count;
            _handles.Add(name, handle);
            containers.Add(new ItemContainerState(handle, Array.AsReadOnly<ContainerItem?>([.. contents])));
        }

        IReadOnlyList<ContainerItem?> Branch(long asset)
        {
            if (branches.TryGetValue(asset, out IReadOnlyList<ContainerItem?>? contents))
            {
                return contents;
            }

            if (definitions[asset].SlotConfig is not SingleItemSlot branch || definitions[branch.DefaultSlotAssetId].SlotConfig is not ItemSlotArray slots)
            {
                throw new WorldPreparationException("Invalid standard container definitions");
            }
            return Array.AsReadOnly(new ContainerItem?[checked((int)slots.NumberOfSlots)]);
        }

        InventoryItem Child(InventoryItem parent, long asset)
        {
            visited.Add(parent.InstanceId);
            if (parent.Amount != 1 || parent.Slots is not [{ } childId] || items[childId].GameAssetId != asset || items[childId].Amount != 1)
            {
                throw new WorldPreparationException("Invalid player loadout structure");
            }

            InventoryItem child = items[childId];
            visited.Add(childId);
            return child;
        }

        ContainerItem? ReadSlot(InventoryItem slot, long asset)
        {
            visited.Add(slot.InstanceId);
            if (slot.GameAssetId != asset || slot.Amount != 1 || slot.Slots.Count > 1)
            {
                throw new WorldPreparationException("Invalid player inventory slot");
            }

            if (slot.Slots.Count == 0)
            {
                return null;
            }

            InventoryItem item = items[slot.Slots[0]];
            visited.Add(item.InstanceId);
            if (item.GameAssetId != StorageItems.RubberDuckAssetId || item.Slots.Count != 0
                || item.Amount > StorageItems.Definitions.Single(definition => definition.GameAssetId == item.GameAssetId).MaxAmount
                || definitions[asset].SlotConfig is not SingleItemSlot allowed || !allowed.AllowedSlotAssetIds.Contains(item.GameAssetId)
                || !Guid.TryParse(item.InstanceId, out Guid guid))
            {
                throw new WorldPreparationException("Unsupported carried inventory item");
            }

            return new ContainerItem(InventoryBlackboard.RubberDuckIndex, guid, checked((int)item.Amount), item.MaxDurability == 0 ? 1 : item.Durability / item.MaxDurability);
        }
    }

    /// <summary>Equipment container handle.</summary>
    public int Loadout => _handles[nameof(Loadout)];

    /// <summary>Backpack container handle.</summary>
    public int Backpack => _handles[nameof(Backpack)];

    /// <summary>Quick-use container handle.</summary>
    public int Belt => _handles[nameof(Belt)];

    /// <summary>Safe-pocket container handle.</summary>
    public int SafePouch => _handles[nameof(SafePouch)];

    /// <summary>Auxiliary container handle.</summary>
    public int Auxiliary => _handles[nameof(Auxiliary)];

    /// <summary>The immutable initial containers.</summary>
    public IReadOnlyList<ItemContainerState> Containers { get; }

    /// <summary>The initial container state carried in each pawn frame.</summary>
    public PendulumVariable Blackboard { get; }

}
