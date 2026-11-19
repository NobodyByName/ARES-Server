using System.Collections.ObjectModel;
using Ares.Common.Game.Content;
using Ares.Common.Game.Inventory;
using Ares.Structs.Backend.Progression;
using Ares.Structs.Core.Serialization;
using Ares.Structs.Game.Inventory;
using Ares.Structs.Game.Pendulum;

namespace Ares.Raid.World.Inventory;

/// <summary>The selected Host loadout translated into this player's raid containers.</summary>
public sealed partial class RaidInventory
{
    private readonly Dictionary<string, int> _handles = new(StringComparer.Ordinal);
    private readonly Dictionary<(int Container, int Slot), HashSet<int>> _allowedSlots = [];
    private readonly Dictionary<ushort, double> _unitWeights = [];
    private readonly Queue<ContainerItem> _dropped = new();
    private readonly Dictionary<Guid, InventoryItem> _brought = [];
    private readonly ContainerItem? _augment;

    // Container count before the open world container and the mod containers its opening added.
    private int _countBeforeOpen;

    // Maximum durability of an item new in this raid: Host stores durability as a fraction of a maximum of 1.
    private const double NewItemMaxDurability = 1.0;

    /// <summary>Prepares the selected frame, preserving carried item identities and amounts.</summary>
    /// <param name="items">The raid's item rules.</param>
    /// <param name="loadout">The frozen loadout accepted by this raid.</param>
    public RaidInventory(RaidItems items, PlayerLoadout loadout)
    {
        Items = items ?? throw new ArgumentNullException(nameof(items));
        ArgumentNullException.ThrowIfNull(loadout);
        LoadoutDefinitions rules = items.Definitions;
        IReadOnlyDictionary<long, ItemDescription> definitions = rules.ByAsset;
        LoadoutContents parsed;
        try
        {
            parsed = LoadoutTree.Parse(rules, loadout);
        }
        catch (InvalidDataException error)
        {
            throw new WorldPreparationException(error.Message);
        }

        AugmentAssetId = parsed.Augment?.GameAssetId;
        FrameAssetId = parsed.FrameAssetId(rules);
        _augment = parsed.Augment is { } augment ? Carry(augment) : null;
        Dictionary<long, ReadOnlyCollection<ContainerItem?>> branches = parsed.Branches.ToDictionary(branch => branch.Key,
            branch => Array.AsReadOnly<ContainerItem?>([.. branch.Value.Select(item => item is null ? null : Carry(item))]));

        // Each loadout slot filled from an equipment branch.
        LoadoutBranches places = items.LoadoutBranches;
        (int Slot, long Branch)[] loadoutBranches =
        [
            (InventoryBlackboard.Firearm1Slot, places.MainWeapon),
            (InventoryBlackboard.Firearm2Slot, places.SecondaryWeapon),
            (InventoryBlackboard.ArmorSlot, places.Shield),
            (InventoryBlackboard.MeleeSlot, places.Melee),
        ];
        List<ItemContainerState> containers = [];
        ContainerItem?[] equipped = new ContainerItem?[InventoryBlackboard.LoadoutSlotCount];
        foreach ((int slot, long branch) in loadoutBranches)
        {
            equipped[slot] = Branch(branch).Single();
        }

        // Every raider also holds the Raider Tool and empty hands. The client draws them by item (keys 3 and 7),
        // so both exist as raid-only items in their loadout slots; they cannot be moved and add no weight.
        equipped[InventoryBlackboard.MeleeSlot] ??= new ContainerItem(items.RaiderToolIndex, Guid.NewGuid(), 1, 1);
        equipped[InventoryBlackboard.UnarmedSlot] = new ContainerItem(items.UnarmedIndex, Guid.NewGuid(), 1, 1);
        _unitWeights.TryAdd(items.RaiderToolIndex, 0);
        _unitWeights.TryAdd(items.UnarmedIndex, 0);
        Add(nameof(Loadout), equipped);
        foreach ((int slot, long branch) in loadoutBranches)
        {
            Allow(Loadout, slot, branch);
        }

        Add(nameof(Backpack), Branch(places.Backpack), places.Backpack);
        Add(nameof(Belt), Branch(places.QuickUse), places.QuickUse);
        Add(nameof(SafePouch), Branch(places.SafePockets), places.SafePockets);
        Add(nameof(Auxiliary), []);
        for (int container = 0, count = containers.Count; container < count; container++)
        {
            ContainerItem?[] contents = [.. containers[container].Items.Select(item => item is null ? null : WithModContainer(item, containers, []))];
            containers[container] = containers[container] with { Items = Array.AsReadOnly(contents) };
        }

        Containers = containers.AsReadOnly();
        try
        {
            // Loadout guns are stored unloaded; a raid starts with each equipped gun loaded from carried ammo.
            foreach (int slot in (int[])[InventoryBlackboard.Firearm1Slot, InventoryBlackboard.Firearm2Slot])
            {
                // With fewer rounds than the magazine holds, the gun takes them all.
                if (Containers[Loadout].Items[slot] is { AmmoInClip: 0 } gun && items.Traits.Of(gun.AssetIndex).Ammunition is { } ammo
                    && Rounds(ammo) > 0)
                {
                    SetChamber(gun.Id, Math.Min(items.FerroClipSize, Rounds(ammo)), consumeReserve: true);
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
            for (int index = 0; branchAsset is { } asset && index < contents.Count; index++)
            {
                Allow(handle, index, asset);
            }
        }

        // A slot holds the carried items its equipment branch's content slots allow.
        void Allow(int handle, int index, long branchAsset)
        {
            if (definitions[rules.Container(FrameAssetId, branchAsset).SlotAssetId].SlotConfig is SingleItemSlot slot)
            {
                _allowedSlots.Add((handle, index), [.. slot.AllowedSlotAssetIds.Where(rules.SupportsCarriedItem).Select(rules.UsableIndex)]);
            }
        }

        IReadOnlyList<ContainerItem?> Branch(long asset)
        {
            if (branches.TryGetValue(asset, out ReadOnlyCollection<ContainerItem?>? contents))
            {
                return contents;
            }

            ItemSlotArray slots = rules.Container(FrameAssetId, asset);
            return Array.AsReadOnly(new ContainerItem?[checked((int)slots.NumberOfSlots)]);
        }

        // A brought item keeps its identity; its record is kept for reporting it home.
        ContainerItem Carry(InventoryItem item)
        {
            if (!Guid.TryParse(item.InstanceId, out Guid guid) || !_brought.TryAdd(guid, item))
            {
                throw new WorldPreparationException("Unsupported carried inventory item");
            }

            ushort usableIndex = checked((ushort)rules.UsableIndex(item.GameAssetId));
            _unitWeights[usableIndex] = rules.ItemWeight(item.GameAssetId);
            return new ContainerItem(usableIndex, guid, checked((int)item.Amount), DurabilityFraction(item));
        }
    }

    /// <summary>The raid's item rules.</summary>
    public RaidItems Items { get; }

    /// <summary>The equipped augment, or null for the standard frame.</summary>
    public long? AugmentAssetId { get; }

    /// <summary>The inventory frame selected by the equipped augment.</summary>
    public long FrameAssetId { get; }

    /// <summary>The carrying-weight limit selected by the equipped augment.</summary>
    public double MaxWeight => Items.Definitions.MaximumWeight(AugmentAssetId);

    /// <summary>Current carried weight, counting stack amounts and excluding the equipped augment.</summary>
    public double Weight => Carried.SelectMany(container => container.Items).OfType<ContainerItem>()
        .Sum(item => (_unitWeights[item.AssetIndex] * item.Amount) + (item.AmmoInClip * Items.HeavyAmmo.Weight));

    /// <summary>Heavy Ammo available in carried containers, excluding rounds already chambered.</summary>
    public int HeavyAmmo => Rounds(Items.HeavyAmmoIndex);

    /// <summary>Changes a Ferro chamber, optionally loading the added rounds from carried ammunition of the gun atomically.</summary>
    /// <remarks>Loading takes from the first carried stacks and fails when they hold too few rounds.</remarks>
    public bool SetChamber(Guid id, int amount, bool consumeReserve = false)
    {
        if (amount < 0 || amount > Items.FerroClipSize)
        {
            return false;
        }

        // Only carried items load or are loaded: not those of an open world container.
        (int Container, int Slot)[] carried = [.. Enumerable.Range(0, Containers.Count).Where(container => container != OpenLoot)
            .SelectMany(container => Enumerable.Range(0, Containers[container].Items.Count), (container, slot) => (Container: container, Slot: slot))];
        int found = Array.FindIndex(carried, position => Item(position)?.Id == id);
        if (found < 0 || Item(carried[found]) is not { } gun || Items.Traits.Of(gun.AssetIndex).Ammunition is not { } ammunition
            || gun.AmmoInClip == amount)
        {
            return false;
        }

        (int Container, int Slot) target = carried[found];
        (int Container, int Slot)[] ammo = [.. carried.Where(position => Item(position) is { Amount: > 0 } item && item.AssetIndex == ammunition)];
        int needed = consumeReserve ? amount - gun.AmmoInClip : 0;
        if (consumeReserve && (needed <= 0 || needed > Rounds(ammunition)))
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

        ContainerItem? Item((int Container, int Slot) position) => Containers[position.Container].Items[position.Slot];

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
    public double ShieldHealth => Shield is { Durability: > 0 } ? Items.ShieldMaxHealth : 0;

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

    /// <summary>Applies a swap, split or drop, or corrects the client's prediction when it is invalid.</summary>
    /// <param name="command">The requested operation.</param>
    /// <returns>Whether the operation was accepted.</returns>
    public bool Apply(InventoryCommand command) => Apply(command, out _);

    /// <summary>Applies a swap, split or drop, or corrects the client's prediction when it is invalid.</summary>
    /// <param name="command">The requested operation.</param>
    /// <param name="rejection">Why the operation was rejected, or null when it was accepted.</param>
    /// <returns>Whether the operation was accepted.</returns>
    public bool Apply(InventoryCommand command, out string? rejection)
    {
        ArgumentNullException.ThrowIfNull(command);
        rejection = command.Type switch
        {
            InventoryCommandType.SwapItems => Swap(command),
            InventoryCommandType.SplitItems => Split(command),
            InventoryCommandType.DropItem or InventoryCommandType.DropPartialItem => Drop(command),
            _ => "unsupported command",
        };
        Version++;
        Blackboard = ItemContainerState.Encode(Containers, Version, OpenLoot);
        return rejection is null;
    }

    /// <summary>The searched world container the player has open, as an extra container after the player's own.</summary>
    public int? OpenLoot { get; private set; }

    /// <summary>Items of the open world container.</summary>
    public IReadOnlyList<ContainerItem?> LootItems => OpenLoot is { } id ? Containers[id].Items : throw new InvalidOperationException("No open container");

    /// <summary>Opens a searched world container: its slots join the player's containers, any item may go in them.</summary>
    /// <param name="items">Its slots.</param>
    /// <param name="unitWeights">Unit weight of each item type it may hold, for when the player takes one.</param>
    /// <param name="searchStatuses">
    /// Search status of each slot (see <see cref="RevealLootSlots"/>), or null when every item shows at once.
    /// </param>
    /// <returns>The container id the player sees.</returns>
    public int OpenWorldContainer(IReadOnlyList<ContainerItem?> items, IReadOnlyDictionary<ushort, double> unitWeights,
        IReadOnlyList<ItemSearchStatus>? searchStatuses = null)
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

        // Its weapons and armor items get their mod containers before it, so it stays the last container.
        List<ItemContainerState> containers = [.. Containers];
        List<int> free = FreeModContainers();
        _countBeforeOpen = containers.Count;
        ContainerItem?[] slots = [.. items.Select(item => item is null ? null : WithModContainer(item, containers, free))];
        int id = containers.Count;
        containers.Add(new ItemContainerState(id, Array.AsReadOnly(slots))
        {
            Flags = InventoryBlackboard.LootContainerFlags,
            SearchStatuses = searchStatuses is null ? null : Array.AsReadOnly([.. searchStatuses]),
        });
        Containers = containers.AsReadOnly();
        OpenLoot = id;
        Version++;
        Blackboard = ItemContainerState.Encode(Containers, Version, OpenLoot);
        return id;
    }

    /// <summary>Closes the open world container.</summary>
    /// <remarks>
    /// The mod containers its opening added for items that stay behind go with it when they end the set; the returned
    /// items still name their mod containers in this set.
    /// </remarks>
    /// <returns>Its slots as the player left them.</returns>
    public IReadOnlyList<ContainerItem?> CloseWorldContainer()
    {
        IReadOnlyList<ContainerItem?> items = LootItems;
        Containers = [.. Containers.Take(Containers.Count - 1)];
        OpenLoot = null;
        List<int> free = FreeModContainers();
        int kept = Containers.Count;
        while (kept > _countBeforeOpen && free.Contains(kept - 1))
        {
            kept--;
        }

        Containers = [.. Containers.Take(kept)];
        Version++;
        Blackboard = ItemContainerState.Encode(Containers, Version);
        return items;
    }

    /// <summary>Takes the next item removed by an accepted drop that the raid has not yet collected.</summary>
    /// <param name="item">The dropped item: the whole stack with its identity, or the dropped part with a new identity.</param>
    /// <returns>Whether an item was available.</returns>
    public bool TryTakeDropped(out ContainerItem item) => _dropped.TryDequeue(out item!);

    /// <summary>Adds an item picked up from the world: tops up carried stacks of the same item, then uses the first empty backpack slot.</summary>
    /// <param name="item">The picked-up item.</param>
    /// <returns>The amount taken; less than the item's amount when the rest did not fit and stays in the world.</returns>
    public int PickUp(ContainerItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (item.Amount <= 0)
        {
            return 0;
        }

        int remaining = item.Amount;
        int maximum = Items.MaxAmounts.GetValueOrDefault(item.AssetIndex);
        List<ItemContainerState> updated = [.. Containers];
        foreach (int handle in (int[])[Backpack, Belt, SafePouch])
        {
            ContainerItem?[] contents = [.. updated[handle].Items];
            bool changed = false;
            for (int slot = 0; slot < contents.Length && remaining > 0; slot++)
            {
                if (contents[slot] is not { } stack || stack.AssetIndex != item.AssetIndex || stack.Amount >= maximum)
                {
                    continue;
                }

                int added = Math.Min(remaining, maximum - stack.Amount);
                contents[slot] = stack with { Amount = stack.Amount + added };
                remaining -= added;
                changed = true;
            }

            if (changed)
            {
                updated[handle] = updated[handle] with { Items = Array.AsReadOnly(contents) };
            }
        }

        if (remaining > 0)
        {
            ContainerItem?[] contents = [.. updated[Backpack].Items];
            int empty = Array.FindIndex(contents, slot => slot is null);
            List<ItemContainerState> withMods = [.. updated];
            ContainerItem placed = WithModContainer(item with { Amount = remaining }, withMods, FreeModContainers());
            // A new mod container cannot follow an open world container, which must stay last.
            if (empty >= 0 && Allows(new ItemContainerSlot(new ItemContainerHandle(Backpack, 0), (ushort)empty), item)
                && (OpenLoot is null || withMods.Count == updated.Count))
            {
                contents[empty] = placed;
                updated = withMods;
                updated[Backpack] = updated[Backpack] with { Items = Array.AsReadOnly(contents) };
                remaining = 0;
            }
        }

        int taken = item.Amount - remaining;
        if (taken <= 0)
        {
            return taken;
        }

        Containers = updated.AsReadOnly();
        Version++;
        Blackboard = ItemContainerState.Encode(Containers, Version, OpenLoot);
        return taken;
    }

    /// <summary>Lists what the player carries home at the end of the raid, as Host item records placed in Host's loadout.</summary>
    /// <remarks>
    /// Only the player's own containers count: items in an open world container or dropped into the world stay behind,
    /// and the raid's hand items are not kept. An extraction unloads the rounds in carried weapons into the carried items.
    /// </remarks>
    /// <param name="outcome">See <see cref="RaidOutcomes"/>: an extraction keeps the whole loadout, a lost raid only the safe pocket items.</param>
    /// <returns>The carried items; brought items keep their identity, new items have new identities.</returns>
    /// <exception cref="ArgumentException">The outcome is unknown.</exception>
    /// <exception cref="InvalidOperationException">An item sits where Host cannot keep it.</exception>
    public IReadOnlyList<CarriedItem> CarriedHome(string outcome) => CarriedHome(outcome, out _);

    /// <summary>Lists what the player carries home at the end of the raid, as Host item records placed in Host's loadout.</summary>
    /// <remarks>
    /// Only the player's own containers count: items in an open world container or dropped into the world stay behind,
    /// and the raid's hand items are not kept. An extraction unloads the rounds in carried weapons onto carried stacks
    /// of that ammunition (backpack, quick-use, then safe pocket slots), then into new stacks in empty backpack and
    /// quick-use slots. The live inventory does not change.
    /// </remarks>
    /// <param name="outcome">See <see cref="RaidOutcomes"/>: an extraction keeps the whole loadout, a lost raid only the safe pocket items.</param>
    /// <param name="lostRounds">Unloaded rounds that found no room and stay behind.</param>
    /// <returns>The carried items; brought items keep their identity, new items have new identities.</returns>
    /// <exception cref="ArgumentException">The outcome is unknown.</exception>
    /// <exception cref="InvalidOperationException">An item sits where Host cannot keep it.</exception>
    public IReadOnlyList<CarriedItem> CarriedHome(string outcome, out int lostRounds)
    {
        bool extracted = outcome switch
        {
            RaidOutcomes.Extracted => true,
            RaidOutcomes.Lost => false,
            _ => throw new ArgumentException("Unknown raid outcome", nameof(outcome)),
        };
        if (Containers[Auxiliary].Items.Any(item => item is not null))
        {
            throw new InvalidOperationException("Auxiliary items cannot be carried home");
        }

        List<CarriedItem> carried = [];
        Dictionary<ushort, int> unloaded = [];
        if (extracted)
        {
            if (_augment is not null)
            {
                carried.Add(new CarriedItem(CarriedPlaces.Augment, 0, Record(_augment)));
            }

            Equipped(CarriedPlaces.Shield, InventoryBlackboard.ArmorSlot);
            Equipped(CarriedPlaces.MainWeapon, InventoryBlackboard.Firearm1Slot);
            Equipped(CarriedPlaces.SecondaryWeapon, InventoryBlackboard.Firearm2Slot);
            Listed(CarriedPlaces.Backpack, Backpack);
            Listed(CarriedPlaces.QuickUse, Belt);
        }

        Listed(CarriedPlaces.SafePockets, SafePouch);
        lostRounds = 0;
        foreach ((ushort ammo, int rounds) in unloaded)
        {
            lostRounds += Unload(carried, ammo, rounds);
        }

        return carried.AsReadOnly();

        void Equipped(string place, int slot)
        {
            if (Containers[Loadout].Items[slot] is { } item)
            {
                Carry(place, 0, item);
            }
        }

        void Listed(string place, int container)
        {
            IReadOnlyList<ContainerItem?> items = Containers[container].Items;
            for (int slot = 0; slot < items.Count; slot++)
            {
                if (items[slot] is { } item)
                {
                    Carry(place, slot, item);
                }
            }
        }

        void Carry(string place, int slot, ContainerItem item)
        {
            carried.Add(new CarriedItem(place, slot, Record(item)));
            if (!extracted || item.AmmoInClip <= 0 || Items.Traits.Of(item.AssetIndex).Ammunition is not { } ammo)
            {
                return;
            }

            ushort index = checked((ushort)ammo);
            unloaded[index] = unloaded.GetValueOrDefault(index) + item.AmmoInClip;
        }
    }

    private IEnumerable<ItemContainerState> Carried => Containers.Where(container => container.Id != OpenLoot);

    // Rounds of an ammunition item available in carried containers, excluding rounds already chambered.
    private int Rounds(int ammunition) => Carried.SelectMany(container => container.Items).OfType<ContainerItem>()
        .Where(item => item.AssetIndex == ammunition).Sum(item => item.Amount);

    private string? Swap(InventoryCommand command)
    {
        ItemContainerSlot from = command.From;
        ItemContainerSlot to = command.To;
        if (!Valid(from) || !Valid(to))
        {
            return "invalid slot";
        }

        if (Containers[from.Parent.Id].Items[from.Index] is not { } source)
        {
            return "empty source";
        }

        ContainerItem? destination = Containers[to.Parent.Id].Items[to.Index];
        if (!Allows(to, source) || (destination is not null && !Allows(from, destination)))
        {
            return "slot not allowed";
        }

        Place(from, destination, to, source);
        return null;
    }

    // Removes a whole stack, or an amount of it as a new item, and queues it for the raid to place in the world.
    // Hand items stay in the inventory.
    private string? Drop(InventoryCommand command)
    {
        ItemContainerSlot from = command.From;
        if (!Valid(from))
        {
            return "invalid slot";
        }

        if (Containers[from.Parent.Id].Items[from.Index] is not { } source)
        {
            return "empty source";
        }

        if (!Items.MaxAmounts.ContainsKey(source.AssetIndex))
        {
            return "not droppable";
        }

        bool whole = command.Type == InventoryCommandType.DropItem || command.Amount == source.Amount;
        if (!whole && (command.Amount <= 0 || command.Amount > source.Amount))
        {
            return "invalid amount";
        }

        ContainerItem?[] contents = [.. Containers[from.Parent.Id].Items];
        contents[from.Index] = whole ? null : source with { Amount = source.Amount - command.Amount };
        ItemContainerState[] containers = [.. Containers];
        containers[from.Parent.Id] = ShowingLootSlot(containers[from.Parent.Id] with { Items = Array.AsReadOnly(contents) }, from.Index);
        Containers = Array.AsReadOnly(containers);
        _dropped.Enqueue(whole ? source : source with { Id = Guid.NewGuid(), Amount = command.Amount });
        return null;
    }

    // Moves an amount of a stack into an empty slot, as a new stack, or onto a stack of the same item up to its
    // maximum stack size. A zero amount splits half the stack, and the whole stack moves with its identity.
    private string? Split(InventoryCommand command)
    {
        ItemContainerSlot from = command.From;
        ItemContainerSlot to = command.To;
        if (!Valid(from) || !Valid(to) || from == to)
        {
            return "invalid slot";
        }

        if (Containers[from.Parent.Id].Items[from.Index] is not { } source)
        {
            return "empty source";
        }

        int amount = Math.Min(command.Amount > 0 ? command.Amount : source.Amount / 2, source.Amount);
        if (amount <= 0)
        {
            return "invalid amount";
        }

        if (!Allows(to, source))
        {
            return "slot not allowed";
        }

        ContainerItem? destination = Containers[to.Parent.Id].Items[to.Index];
        if (destination is null)
        {
            Place(from, amount == source.Amount ? null : source with { Amount = source.Amount - amount },
                to, amount == source.Amount ? source : source with { Id = Guid.NewGuid(), Amount = amount });
            return null;
        }

        if (destination.AssetIndex != source.AssetIndex)
        {
            return "different item";
        }

        int moved = Math.Min(amount, Items.MaxAmounts.GetValueOrDefault(source.AssetIndex) - destination.Amount);
        if (moved <= 0)
        {
            return "stack full";
        }

        Place(from, moved == source.Amount ? null : source with { Amount = source.Amount - moved },
            to, destination with { Amount = destination.Amount + moved });
        return null;
    }

    private void Place(ItemContainerSlot from, ContainerItem? fromItem, ItemContainerSlot to, ContainerItem? toItem)
    {
        ContainerItem?[] sourceItems = [.. Containers[from.Parent.Id].Items];
        ContainerItem?[] destinationItems = from.Parent == to.Parent ? sourceItems : [.. Containers[to.Parent.Id].Items];
        sourceItems[from.Index] = fromItem;
        destinationItems[to.Index] = toItem;
        ItemContainerState[] containers = [.. Containers];
        containers[from.Parent.Id] = ShowingLootSlot(containers[from.Parent.Id] with { Items = Array.AsReadOnly(sourceItems) }, from.Index);
        containers[to.Parent.Id] = ShowingLootSlot(containers[to.Parent.Id] with { Items = Array.AsReadOnly(destinationItems) }, to.Index);
        Containers = Array.AsReadOnly(containers);
    }

    // A carried item as a Host item record without child slots. Brought items keep their maximum durability and
    // stamps; new items get Host's initial maximum durability and are stamped by Host.
    private InventoryItem Record(ContainerItem item)
    {
        if (!Items.AssetIds.TryGetValue(item.AssetIndex, out long asset))
        {
            throw new InvalidOperationException("Unsupported carried item");
        }

        if (!_brought.TryGetValue(item.Id, out InventoryItem? original))
        {
            return new InventoryItem(item.Id.ToString("D"), asset, item.Amount, [], item.Durability * NewItemMaxDurability,
                NewItemMaxDurability, "", "", 0);
        }

        return original with
        {
            InstanceId = item.Id.ToString("D"),
            GameAssetId = asset,
            Amount = item.Amount,
            Slots = [],
            Durability = item.Durability.Equals(DurabilityFraction(original)) ? original.Durability : item.Durability * original.MaxDurability,
            LockedInRound = "",
        };
    }

    // Adds unloaded rounds to carried items: tops up carried stacks of the ammunition, then fills empty backpack and
    // quick-use slots that allow it with new stacks. Returns the rounds that found no room.
    private int Unload(List<CarriedItem> carried, ushort ammo, int rounds)
    {
        long asset = Items.AssetIds[ammo];
        int maximum = Items.MaxAmounts[ammo];
        foreach (string place in (string[])[CarriedPlaces.Backpack, CarriedPlaces.QuickUse, CarriedPlaces.SafePockets])
        {
            for (int index = 0; index < carried.Count && rounds > 0; index++)
            {
                InventoryItem item = carried[index].Item;
                if (carried[index].Place != place || item.GameAssetId != asset || item.Amount >= maximum)
                {
                    continue;
                }

                int added = (int)Math.Min(rounds, maximum - item.Amount);
                carried[index] = carried[index] with { Item = item with { Amount = item.Amount + added } };
                rounds -= added;
            }
        }

        ContainerItem probe = new(ammo, Guid.Empty, 1, 1);
        foreach ((string place, int container) in ((string Place, int Container)[])[(CarriedPlaces.Backpack, Backpack), (CarriedPlaces.QuickUse, Belt)])
        {
            for (int slot = 0; slot < Containers[container].Items.Count && rounds > 0; slot++)
            {
                if (carried.Any(item => item.Place == place && item.Slot == slot)
                    || !Allows(new ItemContainerSlot(new ItemContainerHandle(container, 0), checked((ushort)slot)), probe))
                {
                    continue;
                }

                ContainerItem stack = probe with { Id = Guid.NewGuid(), Amount = Math.Min(rounds, maximum) };
                carried.Add(new CarriedItem(place, slot, Record(stack)));
                rounds -= stack.Amount;
            }
        }

        return rounds;
    }

    private static double DurabilityFraction(InventoryItem item) => item.MaxDurability == 0 ? 1 : item.Durability / item.MaxDurability;

    // The client indexes the container list with every weapon's and armor item's mod container and stops its whole
    // character tick when the index is invalid. An item entering the set with mod slots gets an empty mod container of
    // its own, which no move can fill: a free one of its slot count (taken from the list), else a new one at the end of
    // the set. Other items name none.
    private ContainerItem WithModContainer(ContainerItem item, List<ItemContainerState> containers, List<int> free)
    {
        if (Items.Traits.Of(item.AssetIndex).ModSlotCount is not { } modSlots)
        {
            return item with { ModContainerId = null };
        }

        int reused = free.FindIndex(id => containers[id].Items.Count == modSlots);
        if (reused >= 0)
        {
            int id = free[reused];
            free.RemoveAt(reused);
            return item with { ModContainerId = id };
        }

        containers.Add(new ItemContainerState(containers.Count, Array.AsReadOnly(new ContainerItem?[modSlots])));
        return item with { ModContainerId = containers.Count - 1 };
    }

    // Mod containers no item of the set names, in id order.
    private List<int> FreeModContainers()
    {
        HashSet<int> named = [.. Containers.SelectMany(container => container.Items).OfType<ContainerItem>().Select(item => item.ModContainerId).OfType<int>()];
        return [.. Enumerable.Range(0, Containers.Count).Where(id => id != OpenLoot && !_handles.ContainsValue(id) && !named.Contains(id))];
    }

    private bool Valid(ItemContainerSlot slot) => slot.Parent is { Revision: 0, Id: >= 0 }
        && slot.Parent.Id < Containers.Count && slot.Index < Containers[slot.Parent.Id].Items.Count;

    private bool Allows(ItemContainerSlot slot, ContainerItem item) => slot.Parent.Id == OpenLoot || _allowedSlots.TryGetValue((slot.Parent.Id, slot.Index), out HashSet<int>? allowed)
        && allowed.Contains(item.AssetIndex);
}
