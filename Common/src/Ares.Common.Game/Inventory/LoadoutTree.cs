using System.Globalization;
using Ares.Structs.Backend.Progression;

namespace Ares.Common.Game.Inventory;

/// <summary>A content slot of the loadout tree.</summary>
/// <param name="Branch">The standard equipment branch holding the slot, or <see cref="LoadoutDefinitions.AugmentSlotAssetId"/> for the augment slot.</param>
/// <param name="Index">Index of the slot in its branch's container.</param>
/// <param name="FrameAssetId">The frame holding the branch; null for the augment slot.</param>
/// <param name="AllowedAssetIds">Asset ids of the items the slot may hold.</param>
public sealed record LoadoutSlot(long Branch, int Index, long? FrameAssetId, IReadOnlyList<long> AllowedAssetIds);

/// <summary>The selected loadout as typed slots. Items carry no child slots: their mod slot nodes belong to the tree.</summary>
/// <param name="Augment">The equipped augment, or null.</param>
/// <param name="Branches">
/// The items in each standard equipment branch's slots by slot index, null for an empty slot. A missing branch or slot is empty.
/// </param>
public sealed record LoadoutContents(InventoryItem? Augment, IReadOnlyDictionary<long, IReadOnlyList<InventoryItem?>> Branches)
{
    /// <summary>The inventory frame selected by the equipped augment.</summary>
    /// <param name="definitions">The shared item definitions.</param>
    public long FrameAssetId(LoadoutDefinitions definitions)
    {
        ArgumentNullException.ThrowIfNull(definitions);
        return definitions.FrameAssetId(Augment?.GameAssetId);
    }
}

/// <summary>
/// The item tree of a player's loadout, as Host builds it for the menu and a raid and Raid reads it back. Two roots: the
/// frame slot, holding the frame selected by the augment, and the augment slot. A frame holds its equipment branches in
/// definition order; a branch, like the augment slot, holds one container with its content slots; a content slot holds at
/// most one item allowed by its definition, and an item holds one empty node per mod slot.
/// </summary>
public sealed class LoadoutTree
{
    private LoadoutTree(IReadOnlyList<InventoryItem> items, IReadOnlyList<string> roots, IReadOnlyDictionary<string, LoadoutSlot> slots)
    {
        Items = items;
        Roots = roots;
        Slots = slots;
    }

    /// <summary>Every node, each after its children: every frame (the menu changes frames locally), then the two roots.</summary>
    public IReadOnlyList<InventoryItem> Items { get; }

    /// <summary>The frame slot and augment slot roots: the loadout selection.</summary>
    public IReadOnlyList<string> Roots { get; }

    /// <summary>The content slots of every frame and the augment slot, by node id.</summary>
    public IReadOnlyDictionary<string, LoadoutSlot> Slots { get; }

    /// <summary>Builds the tree; only the selected frame holds the contents.</summary>
    /// <param name="definitions">The shared item definitions.</param>
    /// <param name="contents">The items in the loadout's slots.</param>
    /// <param name="structure">
    /// Creates a structure node from its asset id, its path (stable and unique within the tree, for example
    /// <c>carried/container/-1935122902/container/3</c>) and its child ids.
    /// </param>
    /// <exception cref="InvalidDataException">An item does not fit its slot.</exception>
    public static LoadoutTree Build(LoadoutDefinitions definitions, LoadoutContents contents, Func<long, string, IReadOnlyList<string>, InventoryItem> structure)
    {
        ArgumentNullException.ThrowIfNull(definitions);
        ArgumentNullException.ThrowIfNull(contents);
        ArgumentNullException.ThrowIfNull(structure);
        IReadOnlyDictionary<long, ItemDescription> descriptions = definitions.ByAsset;
        long selected = contents.FrameAssetId(definitions);
        List<InventoryItem> items = [];
        Dictionary<string, LoadoutSlot> slots = new(StringComparer.Ordinal);
        Dictionary<long, string> frames = [];
        foreach (long frame in definitions.AugmentFrames.Prepend(LoadoutItems.StandardFrameAssetId))
        {
            string path = frame == LoadoutItems.StandardFrameAssetId ? "carried/container" : "frames/" + Text(frame);
            if (descriptions[frame].SlotConfig is not StructuralItemSlots branches)
            {
                throw new InvalidDataException("Unsupported inventory structure");
            }

            frames.Add(frame, Node(frame, path, [.. branches.SlotAssetIds.Select(branch =>
            {
                long standard = definitions.StandardBranch(branch);
                return Branch(branch, path + "/" + Text(standard), standard, frame,
                    frame == selected ? contents.Branches.GetValueOrDefault(standard) ?? [] : []);
            })]));
        }

        string carried = Node(LoadoutItems.FrameSlotAssetId, "carried", [frames[selected]]);
        string augment = Branch(definitions.AugmentSlotAssetId, "augment", definitions.AugmentSlotAssetId, null, [contents.Augment]);
        return new LoadoutTree(items.AsReadOnly(), [carried, augment], slots.AsReadOnly());

        string Branch(long asset, string path, long standard, long? frame, IReadOnlyList<InventoryItem?> branchContents)
        {
            if (descriptions[asset].SlotConfig is not SingleItemSlot { DefaultSlotAssetId: not 0, IsRequired: true } branch
                || descriptions[branch.DefaultSlotAssetId].SlotConfig is not ItemSlotArray container
                || descriptions[container.SlotAssetId].SlotConfig is not SingleItemSlot { IsRequired: false } slot)
            {
                throw new InvalidDataException("Unsupported inventory structure");
            }

            string containerPath = path + "/container";
            List<string> children = [];
            for (int index = 0; index < container.NumberOfSlots; index++)
            {
                children.Add(Slot(container.SlotAssetId, containerPath + "/" + Text(index), new LoadoutSlot(standard, index, frame, slot.AllowedSlotAssetIds),
                    branchContents.ElementAtOrDefault(index)));
            }

            return Node(asset, path, [Node(branch.DefaultSlotAssetId, containerPath, children)]);
        }

        string Slot(long asset, string path, LoadoutSlot slot, InventoryItem? item)
        {
            List<string> children = [];
            if (item is not null)
            {
                if (!slot.AllowedAssetIds.Contains(item.GameAssetId))
                {
                    throw new InvalidDataException("Item is incompatible with its equipment slot");
                }

                // The menu shows a weapon's mod slots only from these child nodes. They stay empty until mods are supported.
                IReadOnlyList<string> modSlots = descriptions[item.GameAssetId].SlotConfig is StructuralItemSlots weaponSlots
                    ? [.. weaponSlots.SlotAssetIds.Select((modSlot, index) => Node(modSlot, "items/" + item.InstanceId + "/" + Text(index), []))]
                    : [];
                items.Add(item with { Slots = modSlots });
                children.Add(item.InstanceId);
            }

            string id = Node(asset, path, children);
            slots.Add(id, slot);
            return id;
        }

        string Node(long asset, string path, IReadOnlyList<string> children)
        {
            InventoryItem node = structure(asset, path, children);
            items.Add(node);
            return node.InstanceId;
        }
    }

    /// <summary>Reads a raid handoff back into typed slots; an empty loadout has no augment and empty branches.</summary>
    /// <param name="definitions">The shared item definitions.</param>
    /// <param name="loadout">The item tree of the selected roots.</param>
    /// <exception cref="InvalidDataException">The tree does not have the loadout structure or holds an item a raid cannot carry.</exception>
    public static LoadoutContents Parse(LoadoutDefinitions definitions, PlayerLoadout loadout)
    {
        ArgumentNullException.ThrowIfNull(definitions);
        ArgumentNullException.ThrowIfNull(loadout);
        Dictionary<string, InventoryItem> items = loadout.Items.ToDictionary(item => item.InstanceId, StringComparer.Ordinal);
        IReadOnlyDictionary<long, ItemDescription> descriptions = definitions.ByAsset;
        long augmentSlot = definitions.AugmentSlotAssetId;
        Dictionary<long, IReadOnlyList<InventoryItem?>> branches = [];
        if (items.Count == 0)
        {
            return new LoadoutContents(null, branches.AsReadOnly());
        }

        HashSet<string> visited = new(StringComparer.Ordinal);
        HashSet<string> children = [.. items.Values.SelectMany(item => item.Slots)];
        InventoryItem[] roots = [.. items.Values.Where(item => !children.Contains(item.InstanceId))];
        if (roots.Count(item => item.GameAssetId == LoadoutItems.FrameSlotAssetId) != 1
            || roots.Count(item => item.GameAssetId == augmentSlot) > 1
            || roots.Any(item => item.GameAssetId != LoadoutItems.FrameSlotAssetId && item.GameAssetId != augmentSlot))
        {
            throw new InvalidDataException("Player loadout requires a frame and an optional augment structure");
        }

        InventoryItem? augment = roots.SingleOrDefault(item => item.GameAssetId == augmentSlot) is { } augmentRoot
            ? Branch(augmentRoot).Single()
            : null;
        if (augment is not null && !definitions.SupportsAugment(augment.GameAssetId))
        {
            throw new InvalidDataException("Unsupported equipped augment");
        }

        InventoryItem frame = Child(roots.Single(item => item.GameAssetId == LoadoutItems.FrameSlotAssetId), definitions.FrameAssetId(augment?.GameAssetId));
        if (descriptions[frame.GameAssetId].SlotConfig is not StructuralItemSlots frameSlots || frame.Slots.Count != frameSlots.SlotAssetIds.Count)
        {
            throw new InvalidDataException("Invalid player frame branches");
        }

        foreach ((string id, long asset) in frame.Slots.Zip(frameSlots.SlotAssetIds))
        {
            if (items[id].GameAssetId != asset)
            {
                throw new InvalidDataException("Invalid player frame branch");
            }

            branches[definitions.StandardBranch(asset)] = Branch(items[id]);
        }

        return visited.Count == items.Count
            ? new LoadoutContents(augment, branches.AsReadOnly())
            : throw new InvalidDataException("Player loadout contains unsupported items");

        // A branch, or the augment slot, holding one container of content slots.
        IReadOnlyList<InventoryItem?> Branch(InventoryItem branch)
        {
            if (descriptions[branch.GameAssetId].SlotConfig is not SingleItemSlot configuration)
            {
                throw new InvalidDataException("Invalid player frame branch");
            }

            InventoryItem container = Child(branch, configuration.DefaultSlotAssetId);
            if (descriptions[container.GameAssetId].SlotConfig is not ItemSlotArray slots || container.Slots.Count != slots.NumberOfSlots)
            {
                throw new InvalidDataException("Invalid player container size");
            }

            return [.. container.Slots.Select(slot => ReadSlot(items[slot], slots.SlotAssetId))];
        }

        InventoryItem Child(InventoryItem parent, long asset)
        {
            visited.Add(parent.InstanceId);
            if (parent.Amount != 1 || parent.Slots is not [{ } childId] || items[childId].GameAssetId != asset || items[childId].Amount != 1)
            {
                throw new InvalidDataException("Invalid player loadout structure");
            }

            visited.Add(childId);
            return items[childId];
        }

        InventoryItem? ReadSlot(InventoryItem slot, long asset)
        {
            visited.Add(slot.InstanceId);
            if (slot.GameAssetId != asset || slot.Amount != 1 || slot.Slots.Count > 1)
            {
                throw new InvalidDataException("Invalid player inventory slot");
            }

            if (slot.Slots.Count == 0)
            {
                return null;
            }

            InventoryItem item = items[slot.Slots[0]];
            visited.Add(item.InstanceId);
            if (!definitions.SupportsCarriedItem(item.GameAssetId) || !EmptyModSlots(item)
                || item.Amount <= 0 || item.Amount > descriptions[item.GameAssetId].MaxAmount
                || descriptions[asset].SlotConfig is not SingleItemSlot allowed || !allowed.AllowedSlotAssetIds.Contains(item.GameAssetId))
            {
                throw new InvalidDataException("Unsupported carried inventory item");
            }

            return item with { Slots = [] };
        }

        // An item carries one empty node per mod slot; mods themselves are not supported yet.
        bool EmptyModSlots(InventoryItem item)
        {
            IReadOnlyList<long> expected = descriptions[item.GameAssetId].SlotConfig is StructuralItemSlots modSlots ? modSlots.SlotAssetIds : [];
            if (item.Slots.Count != expected.Count)
            {
                return false;
            }

            foreach ((string id, long asset) in item.Slots.Zip(expected))
            {
                if (items[id] is not { Amount: 1, Slots.Count: 0 } modSlot || modSlot.GameAssetId != asset)
                {
                    return false;
                }

                visited.Add(id);
            }

            return true;
        }
    }

    private static string Text(long value) => value.ToString(CultureInfo.InvariantCulture);
}
