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
        // Every raider also holds the Raider Tool and empty hands. The client draws them by item (keys 3 and 7),
        // so both exist as raid-only items in their loadout slots; they cannot be moved and add no weight.
        equipped[InventoryBlackboard.MeleeSlot] ??= new ContainerItem(HandItems.RaiderToolUsableIndex, Guid.NewGuid(), 1, 1);
        equipped[InventoryBlackboard.UnarmedSlot] = new ContainerItem(HandItems.UnarmedUsableIndex, Guid.NewGuid(), 1, 1);
        _unitWeights.TryAdd(HandItems.RaiderToolUsableIndex, 0);
        _unitWeights.TryAdd(HandItems.UnarmedUsableIndex, 0);
        Add(nameof(Loadout), equipped);
        _allowedSlots.Add((Loadout, InventoryBlackboard.Firearm1Slot), [WeaponItems.FerroIUsableIndex]);
        _allowedSlots.Add((Loadout, InventoryBlackboard.Firearm2Slot), [WeaponItems.FerroIUsableIndex]);
        _allowedSlots.Add((Loadout, InventoryBlackboard.ArmorSlot), [ShieldItems.LightShieldUsableIndex]);
        Add(nameof(Backpack), Branch(LoadoutItems.BackpackSlotAssetId), LoadoutItems.BackpackSlotAssetId);
        Add(nameof(Belt), Branch(LoadoutItems.BeltSlotAssetId), LoadoutItems.BeltSlotAssetId);
        Add(nameof(SafePouch), Branch(LoadoutItems.SafePocketSlotAssetId), LoadoutItems.SafePocketSlotAssetId);
        Add(nameof(Auxiliary), []);
        // The client indexes the container list with every weapon's and armor item's mod container and stops
        // its whole character tick when the index is invalid. Each such item gets its own empty mod container,
        // which no move can fill.
        for (int container = 0, count = containers.Count; container < count; container++)
        {
            ContainerItem?[] contents = [.. containers[container].Items];
            for (int slot = 0; slot < contents.Length; slot++)
            {
                if (contents[slot] is not { } item || ModSlotCount(item.AssetIndex) is not { } modSlots)
                {
                    continue;
                }

                contents[slot] = item with { ModContainerId = containers.Count };
                containers.Add(new ItemContainerState(containers.Count, Array.AsReadOnly(new ContainerItem?[modSlots])));
            }

            containers[container] = containers[container] with { Items = Array.AsReadOnly(contents) };
        }

        Containers = containers.AsReadOnly();
        try
        {
            // Loadout guns are stored unloaded; a raid starts with each equipped gun loaded from carried ammo.
            foreach (int slot in (int[])[InventoryBlackboard.Firearm1Slot, InventoryBlackboard.Firearm2Slot])
            {
                // With fewer rounds than the magazine holds, the gun takes them all.
                if (Containers[Loadout].Items[slot] is { AssetIndex: WeaponItems.FerroIUsableIndex, AmmoInClip: 0 } gun && HeavyAmmo > 0)
                {
                    SetChamber(gun.Id, Math.Min(FerroDefaults.ClipSize, HeavyAmmo), consumeReserve: true);
                }
            }

            // The stock client must finish container creation and install its loadout slot queries before
            // it receives equipment items, and every loadout holds hand items. Deliver contents in the next
            // acknowledged frame; owned items and their positions stay in the authoritative inventory.
            InitialBlackboard = ItemContainerState.Encode([.. Containers.Select(container => container with
            {
                Items = Array.AsReadOnly(new ContainerItem?[container.Items.Count]),
            })]);
            Version = 2;
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
                .Where(LoadoutDefinitions.SupportsCarriedItem).Select(LoadoutDefinitions.UsableIndex)];
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
            if (!LoadoutDefinitions.SupportsCarriedItem(item.GameAssetId) || !EmptyModSlots(item)
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

        // A weapon carries one empty node per mod slot; mods themselves are not supported yet.
        bool EmptyModSlots(InventoryItem item)
        {
            IReadOnlyList<long> expected = definitions[item.GameAssetId].SlotConfig is StructuralItemSlots modSlots ? modSlots.SlotAssetIds : [];
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

    /// <summary>The equipped augment, or null for the standard frame.</summary>
    public long? AugmentAssetId { get; }

    /// <summary>The inventory frame selected by the equipped augment.</summary>
    public long FrameAssetId { get; } = LoadoutItems.StandardFrameAssetId;

    /// <summary>The carrying-weight limit selected by the equipped augment.</summary>
    public double MaxWeight => LoadoutDefinitions.MaximumWeight(AugmentAssetId);

    /// <summary>Current carried weight, counting stack amounts and excluding the equipped augment.</summary>
    public double Weight => Carried.SelectMany(container => container.Items).OfType<ContainerItem>()
        .Sum(item => (_unitWeights[item.AssetIndex] * item.Amount) + (item.AmmoInClip * WeaponItems.HeavyAmmoWeight));

    /// <summary>Heavy Ammo available in carried containers, excluding rounds already chambered.</summary>
    public int HeavyAmmo => Carried.SelectMany(container => container.Items).OfType<ContainerItem>()
        .Where(item => item.AssetIndex == WeaponItems.HeavyAmmoUsableIndex).Sum(item => item.Amount);

    /// <summary>Changes a Ferro chamber, optionally loading the added rounds from carried Heavy Ammo atomically.</summary>
    /// <remarks>Loading takes from the first carried stacks and fails when they hold too few rounds.</remarks>
    public bool SetChamber(Guid id, int amount, bool consumeReserve = false)
    {
        if (amount is < 0 or > FerroDefaults.ClipSize)
        {
            return false;
        }

        (int Container, int Slot)? gun = null;
        List<(int Container, int Slot)> ammo = [];
        for (int container = 0; container < Containers.Count; container++)
        {
            // Only carried items load or are loaded: not those of an open world container.
            if (container == OpenLoot)
            {
                continue;
            }

            for (int slot = 0; slot < Containers[container].Items.Count; slot++)
            {
                ContainerItem? item = Containers[container].Items[slot];
                if (item is { AssetIndex: WeaponItems.FerroIUsableIndex } && item.Id == id)
                {
                    gun = (container, slot);
                }

                if (item is { AssetIndex: WeaponItems.HeavyAmmoUsableIndex, Amount: > 0 })
                {
                    ammo.Add((container, slot));
                }
            }
        }

        if (gun is not { } target || Containers[target.Container].Items[target.Slot]!.AmmoInClip == amount)
        {
            return false;
        }

        int needed = consumeReserve ? amount - Containers[target.Container].Items[target.Slot]!.AmmoInClip : 0;
        if (consumeReserve && (needed <= 0 || needed > HeavyAmmo))
        {
            return false;
        }

        ItemContainerState[] updated = [.. Containers];
        Replace(target, updated[target.Container].Items[target.Slot]! with { AmmoInClip = amount });
        foreach ((int Container, int Slot) source in ammo)
        {
            if (needed == 0)
            {
                break;
            }

            ContainerItem item = updated[source.Container].Items[source.Slot]!;
            int taken = Math.Min(needed, item.Amount);
            Replace(source, item.Amount == taken ? null : item with { Amount = item.Amount - taken });
            needed -= taken;
        }

        Containers = Array.AsReadOnly(updated);
        Version++;
        Blackboard = ItemContainerState.Encode(Containers, Version, OpenLoot);
        return true;

        void Replace((int Container, int Slot) position, ContainerItem? item)
        {
            ContainerItem?[] contents = [.. updated[position.Container].Items];
            contents[position.Slot] = item;
            updated[position.Container] = updated[position.Container] with { Items = Array.AsReadOnly(contents) };
        }
    }

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

    /// <summary>Empty container creation, sent before the equipment contents.</summary>
    public PendulumVariable InitialBlackboard { get; }

    /// <summary>Container update counter, including corrections for rejected requests.</summary>
    public uint Version { get; private set; }

    /// <summary>Applies a whole-stack swap, or corrects the client's prediction when it is invalid.</summary>
    /// <param name="command">The requested operation.</param>
    /// <returns>Whether the swap was accepted.</returns>
    public bool Apply(InventoryCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        bool applied = Swap(command);
        Version++;
        Blackboard = ItemContainerState.Encode(Containers, Version, OpenLoot);
        return applied;
    }

    /// <summary>The searched world container the player has open, as an extra container after the player's own.</summary>
    public int? OpenLoot { get; private set; }

    /// <summary>Items of the open world container.</summary>
    public IReadOnlyList<ContainerItem?> LootItems => OpenLoot is { } id ? Containers[id].Items : throw new InvalidOperationException("No open container");

    /// <summary>Opens a searched world container: its slots join the player's containers, any item may go in them.</summary>
    /// <param name="items">Its slots.</param>
    /// <param name="unitWeights">Unit weight of each item type it may hold, for when the player takes one.</param>
    /// <returns>The container id the player sees.</returns>
    public int OpenWorldContainer(IReadOnlyList<ContainerItem?> items, IReadOnlyDictionary<ushort, double> unitWeights)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(unitWeights);
        foreach ((ushort index, double weight) in unitWeights)
        {
            _unitWeights.TryAdd(index, weight);
        }

        if (OpenLoot is not null)
        {
            throw new InvalidOperationException("A container is already open");
        }

        int id = Containers.Count;
        Containers = Array.AsReadOnly([.. Containers, new ItemContainerState(id, Array.AsReadOnly<ContainerItem?>([.. items])) { Flags = InventoryBlackboard.LootContainerFlags }]);
        OpenLoot = id;
        Version++;
        Blackboard = ItemContainerState.Encode(Containers, Version, OpenLoot);
        return id;
    }

    /// <summary>Closes the open world container.</summary>
    /// <returns>Its slots as the player left them.</returns>
    public IReadOnlyList<ContainerItem?> CloseWorldContainer()
    {
        IReadOnlyList<ContainerItem?> items = LootItems;
        Containers = [.. Containers.Take(Containers.Count - 1)];
        OpenLoot = null;
        Version++;
        Blackboard = ItemContainerState.Encode(Containers, Version);
        return items;
    }

    private IEnumerable<ItemContainerState> Carried => Containers.Where(container => container.Id != OpenLoot);

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

    private static int? ModSlotCount(ushort assetIndex) => assetIndex switch
    {
        WeaponItems.FerroIUsableIndex => FerroDefaults.ModSlotCount,
        ShieldItems.LightShieldUsableIndex => ShieldDefaults.ModSlotCount,
        _ => null,
    };

    private bool Valid(ItemContainerSlot slot) => slot.Parent is { Revision: 0, Id: >= 0 }
        && slot.Parent.Id < Containers.Count && slot.Index < Containers[slot.Parent.Id].Items.Count;

    private bool Allows(ItemContainerSlot slot, ContainerItem item) => slot.Parent.Id == OpenLoot || _allowedSlots.TryGetValue((slot.Parent.Id, slot.Index), out HashSet<int>? allowed)
        && allowed.Contains(item.AssetIndex);
}
