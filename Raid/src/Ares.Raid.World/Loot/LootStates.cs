using System.Globalization;
using Ares.Raid.World.Inventory;
using Ares.Structs.Game.Inventory;

namespace Ares.Raid.World.Loot;

/// <summary>
/// What each loot container of a raid holds, shared by its players: a container starts with the contents drawn at its
/// creation and keeps what the players who opened it left inside, and the time it was first opened. One player at a
/// time holds a container, from the press of a search until they close it; its sound is what everyone near it hears.
/// </summary>
/// <param name="catalog">The raid's containers.</param>
/// <param name="raidItems">The raid's item rules: the item names and the placeholder rubber duck.</param>
public sealed class LootStates(LootCatalog catalog, RaidItems raidItems)
{
    // Turns the raid's seed into the seed of the reveal rolls, so they draw apart from the spawn and content rolls.
    private const int RevealSeedSalt = 0x53454152;

    private readonly Dictionary<int, IReadOnlyList<ContainerItem?>> _items = [];
    private readonly Dictionary<int, double> _openedAt = [];
    private readonly List<int> _openings = [];
    private readonly List<string> _events = [];
    private readonly Dictionary<int, LootHold> _holds = [];
    private readonly List<int> _changes = [];
    private readonly Random _reveals = new(catalog.Seed ^ RevealSeedSalt);

    /// <summary>The containers in the order they were first opened: every player's view of them changed then.</summary>
    public IReadOnlyList<int> Openings => _openings;

    /// <summary>
    /// Containers in the order their holder or sound changed (one entry per change): every player's view of them changed then.
    /// </summary>
    public IReadOnlyList<int> Changes => _changes;

    /// <summary>One line per first opening, oldest first: the container, its tag, pool and what it held.</summary>
    public IReadOnlyList<string> Events => _events;

    /// <summary>
    /// A container's slots: its drawn stacks from the first slot on, or one placeholder rubber duck when it drew none.
    /// Its items come from a loot box (<see cref="ItemSource.PickupLootBox"/>) and keep that source wherever they go.
    /// </summary>
    /// <param name="index">Container index.</param>
    /// <returns>Its slots.</returns>
    public IReadOnlyList<ContainerItem?> Items(int index)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, catalog.Containers.Count);
        if (_items.TryGetValue(index, out IReadOnlyList<ContainerItem?>? items))
        {
            return items;
        }

        LootContainer container = catalog.Containers[index];
        ContainerItem?[] slots = new ContainerItem?[container.Class.SlotCount];
        IReadOnlyList<LootStack> stacks = container.Contents.Stacks;
        if (stacks.Count == 0)
        {
            slots[0] = new ContainerItem(raidItems.RubberDuckIndex, Guid.NewGuid(), 1, 1) { Source = ItemSource.PickupLootBox };
        }

        for (int slot = 0; slot < stacks.Count && slot < slots.Length; slot++)
        {
            slots[slot] = new ContainerItem(checked((ushort)stacks[slot].Item.UsableIndex), Guid.NewGuid(), stacks[slot].Amount, stacks[slot].Durability)
            {
                Source = ItemSource.PickupLootBox,
            };
        }

        return _items[index] = Array.AsReadOnly(slots);
    }

    /// <summary>Describes a container and what it holds now: class, tag, loot value, pool and items.</summary>
    /// <param name="index">Container index.</param>
    /// <returns>One line.</returns>
    public string Describe(int index)
    {
        LootContainer container = catalog.Containers[index];
        string held = string.Join(", ", Items(index).OfType<ContainerItem>()
            .Select(item => string.Create(CultureInfo.InvariantCulture, $"{item.Amount}x {Name(item.AssetIndex)}")));
        string pool = (container.Contents.Pool ?? "none") + (container.Contents.Stacks.Count == 0 ? " (placeholder duck)" : "");
        return $"#{index} {container.ClassName} id {container.Id ?? "-"} tag {container.Tag ?? "-"} value {container.LootValue} pool {pool}: {(held.Length == 0 ? "empty" : held)}";
    }

    // The technical name of a carried item, or its usable index.
    private string Name(ushort index) => raidItems.Carried.TryGetValue(index, out ItemDefinition? item) ? item.TechnicalName : $"item {index}";

    /// <summary>Keeps what a player left in a container, without the mod containers items had in that player's set.</summary>
    /// <param name="index">Container index.</param>
    /// <param name="items">Its slots.</param>
    public void Store(int index, IReadOnlyList<ContainerItem?> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        _ = Items(index);
        _items[index] = Array.AsReadOnly([.. items.Select(item => item is null ? null : item with { ModContainerId = null })]);
    }

    /// <summary>When a container was first opened, if it has been.</summary>
    /// <param name="index">Container index.</param>
    /// <returns>Server time, or null.</returns>
    public double? OpenedAt(int index) => _openedAt.TryGetValue(index, out double at) ? at : null;

    /// <summary>Marks a container opened; opening it again keeps the first time.</summary>
    /// <param name="index">Container index.</param>
    /// <param name="at">Server time.</param>
    public void Open(int index, double at)
    {
        if (!_openedAt.TryAdd(index, at))
        {
            return;
        }

        _openings.Add(index);
        _events.Add($"open {Describe(index)}");
    }

    /// <summary>Who holds a container now, if anyone.</summary>
    /// <param name="index">Container index.</param>
    /// <returns>The hold, or null.</returns>
    public LootHold? HoldOf(int index) => _holds.TryGetValue(index, out LootHold hold) ? hold : null;

    /// <summary>A player starts a search of a container, silent until its hold starts; nobody else may hold it meanwhile.</summary>
    /// <param name="index">Container index.</param>
    /// <param name="player">The player's user id.</param>
    /// <param name="holdStartsAt">Server time the search hold starts.</param>
    /// <returns>False when another player holds the container.</returns>
    public bool TryHold(int index, long player, double holdStartsAt)
    {
        if (_holds.TryGetValue(index, out LootHold hold) && hold.Player != player)
        {
            return false;
        }

        _holds[index] = new LootHold(player, holdStartsAt, ItemContainerSoundState.None);
        _changes.Add(index);
        return true;
    }

    /// <summary>Sets the sound of a held container.</summary>
    /// <param name="index">Container index.</param>
    /// <param name="sound">The sound everyone near it hears.</param>
    public void SetSound(int index, ItemContainerSoundState sound)
    {
        if (!_holds.TryGetValue(index, out LootHold hold) || hold.Sound == sound)
        {
            return;
        }

        _holds[index] = hold with { Sound = sound };
        _changes.Add(index);
    }

    /// <summary>The player's search or open container ends: the container falls silent and anyone may search it.</summary>
    /// <param name="index">Container index.</param>
    /// <param name="player">The player's user id.</param>
    public void Release(int index, long player)
    {
        if (!_holds.TryGetValue(index, out LootHold hold) || hold.Player != player)
        {
            return;
        }

        _holds.Remove(index);
        _changes.Add(index);
    }

    /// <summary>Whether one reveal of a search shows two items at once.</summary>
    /// <param name="chance">The searching player's chance of a double reveal.</param>
    /// <returns>True when it does.</returns>
    public bool RollsDoubleReveal(double chance) => chance > 0 && _reveals.NextDouble() < chance;
}

/// <summary>The player holding a loot container: searching it, then with it open.</summary>
/// <param name="Player">The player's user id.</param>
/// <param name="HoldStartsAt">Server time the search hold starts.</param>
/// <param name="Sound">The container's sound.</param>
public readonly record struct LootHold(long Player, double HoldStartsAt, ItemContainerSoundState Sound);
