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
    private readonly Dictionary<(int Container, int Slot), HashSet<int>> _allowedSlots = [];
    private readonly Dictionary<ushort, double> _unitWeights = [];

    /// <summary>Prepares the selected frame, preserving carried item identities and amounts.</summary>
    /// <param name="loadout">The frozen loadout accepted by this raid.</param>
    public RaidInventory(PlayerLoadout loadout)
    {
        ArgumentNullException.ThrowIfNull(loadout);
        Dictionary<string, InventoryItem> items = loadout.Items.ToDictionary(item => item.InstanceId, StringComparer.Ordinal);
        IReadOnlyDictionary<long, ItemDescription> definitions = LoadoutDefinitions.ByAsset;
        HashSet<string> visited = new(StringComparer.Ordinal);
        Dictionary<long, IReadOnlyList<ContainerItem?>> branches = [];
        if (items.Count > 0)
        {
            HashSet<string> children = [.. items.Values.SelectMany(item => item.Slots)];
            InventoryItem[] roots = [.. items.Values.Where(item => !children.Contains(item.InstanceId))];
            if (roots.Count(item => item.GameAssetId == LoadoutItems.FrameSlotAssetId) != 1
                || roots.Count(item => item.GameAssetId == AugmentItems.AugmentSlotAssetId) > 1
                || roots.Any(item => item.GameAssetId is not (LoadoutItems.FrameSlotAssetId or AugmentItems.AugmentSlotAssetId)))
            {
                throw new WorldPreparationException("Raid inventory requires a player frame and optional augment structure");
            }

            InventoryItem root = roots.Single(item => item.GameAssetId == LoadoutItems.FrameSlotAssetId);
            InventoryItem? augmentRoot = roots.SingleOrDefault(item => item.GameAssetId == AugmentItems.AugmentSlotAssetId);
            if (augmentRoot is not null)
            {
                InventoryItem container = Child(augmentRoot, AugmentItems.AugmentContainerAssetId);
                if (container.Slots is not [{ } slotId])
                {
                    throw new WorldPreparationException("Invalid augment container");
                }

                InventoryItem slot = items[slotId];
                ReadSlot(slot, AugmentItems.AugmentContentSlotAssetId);
                if (slot.Slots is [{ } augmentId])
                {
                    AugmentAssetId = items[augmentId].GameAssetId;
                    if (!AugmentItems.FrameAssetIds.ContainsKey(AugmentAssetId.Value))
                    {
                        throw new WorldPreparationException("Unsupported equipped augment");
                    }
                }
            }

            FrameAssetId = LoadoutDefinitions.FrameAssetId(AugmentAssetId);
            InventoryItem frame = Child(root, FrameAssetId);
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

                branches[LoadoutDefinitions.StandardBranch(asset)] = Array.AsReadOnly<ContainerItem?>([.. container.Slots.Select(slot => ReadSlot(items[slot], slots.SlotAssetId))]);
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
        _allowedSlots.Add((Loadout, InventoryBlackboard.ArmorSlot), [ShieldItems.LightShieldUsableIndex]);
        Add(nameof(Backpack), Branch(LoadoutItems.BackpackSlotAssetId), LoadoutItems.BackpackSlotAssetId);
        Add(nameof(Belt), Branch(LoadoutItems.BeltSlotAssetId), LoadoutItems.BeltSlotAssetId);
        Add(nameof(SafePouch), Branch(LoadoutItems.SafePocketSlotAssetId), LoadoutItems.SafePocketSlotAssetId);
        Add(nameof(Auxiliary), []);
        Containers = containers.AsReadOnly();
        try
        {
            if (Containers.SelectMany(container => container.Items).Any(item => item?.AssetIndex == ShieldItems.LightShieldUsableIndex))
            {
                // The stock client must finish container creation and install its loadout slot
                // queries before it receives armor items. Deliver contents in the next acknowledged
                // frame; owned items and their positions stay in the authoritative inventory.
                InitialBlackboard = ItemContainerState.Encode([.. Containers.Select(container => container with
                {
                    Items = Array.AsReadOnly(new ContainerItem?[container.Items.Count]),
                })]);
                Version = 2;
            }

            Blackboard = ItemContainerState.Encode(Containers, Version);
        }
        catch (InvalidPacketException error)
        {
            throw new WorldPreparationException(error.Message);
        }

        return;

        void Add(string name, IReadOnlyList<ContainerItem?> contents, long? branchAsset = null)
        {
            int handle = containers.Count;
            _handles.Add(name, handle);
            containers.Add(new ItemContainerState(handle, Array.AsReadOnly<ContainerItem?>([.. contents])));
            if (branchAsset is not { } asset || definitions[LoadoutDefinitions.Container(FrameAssetId, asset).SlotAssetId].SlotConfig is not SingleItemSlot slot)
            {
                return;
            }

            HashSet<int> allowed = [.. slot.AllowedSlotAssetIds
                .Where(id => id is StorageItems.RubberDuckAssetId or ShieldItems.LightShieldAssetId || AugmentItems.UsableIndices.ContainsKey(id)).Select(LoadoutDefinitions.UsableIndex)];
            for (int index = 0; index < contents.Count; index++)
            {
                _allowedSlots.Add((handle, index), allowed);
            }
        }

        IReadOnlyList<ContainerItem?> Branch(long asset)
        {
            if (branches.TryGetValue(asset, out IReadOnlyList<ContainerItem?>? contents))
            {
                return contents;
            }

            ItemSlotArray slots = LoadoutDefinitions.Container(FrameAssetId, asset);
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
            if ((item.GameAssetId is not (StorageItems.RubberDuckAssetId or ShieldItems.LightShieldAssetId) && !AugmentItems.UsableIndices.ContainsKey(item.GameAssetId)) || item.Slots.Count != 0
                || item.Amount <= 0 || item.Amount > definitions[item.GameAssetId].MaxAmount
                || definitions[asset].SlotConfig is not SingleItemSlot allowed || !allowed.AllowedSlotAssetIds.Contains(item.GameAssetId)
                || !Guid.TryParse(item.InstanceId, out Guid guid))
            {
                throw new WorldPreparationException("Unsupported carried inventory item");
            }

            ushort usableIndex = checked((ushort)LoadoutDefinitions.UsableIndex(item.GameAssetId));
            _unitWeights[usableIndex] = LoadoutDefinitions.ItemWeight(item.GameAssetId);
            return new ContainerItem(usableIndex, guid, checked((int)item.Amount), item.MaxDurability == 0 ? 1 : item.Durability / item.MaxDurability);
        }
    }

    /// <summary>The equipped augment, or null for the standard frame.</summary>
    public long? AugmentAssetId { get; }

    /// <summary>The inventory frame selected by the equipped augment.</summary>
    public long FrameAssetId { get; } = LoadoutItems.StandardFrameAssetId;

    /// <summary>The carrying-weight limit selected by the equipped augment.</summary>
    public double MaxWeight => LoadoutDefinitions.MaximumWeight(AugmentAssetId);

    /// <summary>Current carried weight, counting stack amounts and excluding the equipped augment.</summary>
    public double Weight => Containers.SelectMany(container => container.Items).OfType<ContainerItem>()
        .Sum(item => _unitWeights[item.AssetIndex] * item.Amount);

    /// <summary>The equipped Light Shield; a broken shield remains equipped with no available health.</summary>
    public ContainerItem? Shield => Containers[Loadout].Items[InventoryBlackboard.ArmorSlot];

    /// <summary>Available shield capacity. Positive item durability permits the full authored capacity.</summary>
    public double ShieldHealth => Shield is { Durability: > 0 } ? ShieldDefaults.MaxHealth : 0;

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

    /// <summary>The current containers; previous snapshots are never changed.</summary>
    public IReadOnlyList<ItemContainerState> Containers { get; private set; }

    /// <summary>The current container state carried in each pawn frame.</summary>
    public PendulumVariable Blackboard { get; private set; }

    /// <summary>Empty container creation before armor contents, or null when one frame is sufficient.</summary>
    public PendulumVariable? InitialBlackboard { get; }

    /// <summary>Container update counter, including corrections for rejected requests.</summary>
    public uint Version { get; private set; } = 1;

    /// <summary>Applies a whole-stack swap, or corrects the client's prediction when it is invalid.</summary>
    /// <param name="command">The requested operation.</param>
    /// <returns>Whether the swap was accepted.</returns>
    public bool Apply(InventoryCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        bool applied = Swap(command);
        Version++;
        Blackboard = ItemContainerState.Encode(Containers, Version);
        return applied;
    }

    private bool Swap(InventoryCommand command)
    {
        ItemContainerSlot from = command.From;
        ItemContainerSlot to = command.To;
        if (command.Type != InventoryCommandType.SwapItems || !Valid(from) || !Valid(to)
            || Containers[from.Parent.Id].Items[from.Index] is not { } source)
        {
            return false;
        }

        ContainerItem? destination = Containers[to.Parent.Id].Items[to.Index];
        if (!Allows(to, source) || (destination is not null && !Allows(from, destination)))
        {
            return false;
        }

        ContainerItem?[] sourceItems = [.. Containers[from.Parent.Id].Items];
        ContainerItem?[] destinationItems = from.Parent == to.Parent ? sourceItems : [.. Containers[to.Parent.Id].Items];
        sourceItems[from.Index] = destination;
        destinationItems[to.Index] = source;
        ItemContainerState[] containers = [.. Containers];
        containers[from.Parent.Id] = containers[from.Parent.Id] with { Items = Array.AsReadOnly(sourceItems) };
        containers[to.Parent.Id] = containers[to.Parent.Id] with { Items = Array.AsReadOnly(destinationItems) };
        Containers = Array.AsReadOnly(containers);
        return true;
    }

    private bool Valid(ItemContainerSlot slot) => slot.Parent is { Revision: 0, Id: >= 0 }
        && slot.Parent.Id < Containers.Count && slot.Index < Containers[slot.Parent.Id].Items.Count;

    private bool Allows(ItemContainerSlot slot, ContainerItem item) => _allowedSlots.TryGetValue((slot.Parent.Id, slot.Index), out HashSet<int>? allowed)
        && allowed.Contains(item.AssetIndex);
}
