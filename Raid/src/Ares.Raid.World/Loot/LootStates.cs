using System.Globalization;
using Ares.Raid.World.Inventory;
using Ares.Structs.Game.Inventory;

namespace Ares.Raid.World.Loot;

/// <summary>
/// What each loot container of a raid holds, shared by its players: a container starts with the contents drawn at its
/// creation and keeps what the players who opened it left inside, and the time it was first opened.
/// </summary>
/// <param name="catalog">The raid's containers.</param>
/// <param name="raidItems">The raid's item rules: the item names and the placeholder rubber duck.</param>
public sealed class LootStates(LootCatalog catalog, RaidItems raidItems)
{
    private readonly Dictionary<int, IReadOnlyList<ContainerItem?>> _items = [];
    private readonly Dictionary<int, double> _openedAt = [];
    private readonly List<int> _openings = [];
    private readonly List<string> _events = [];

    /// <summary>The containers in the order they were first opened: every player's view of them changed then.</summary>
    public IReadOnlyList<int> Openings => _openings;

    /// <summary>One line per first opening, oldest first: the container, its tag, pool and what it held.</summary>
    public IReadOnlyList<string> Events => _events;

    /// <summary>
    /// A container's slots: its drawn stacks from the first slot on, or one placeholder rubber duck when it drew none.
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
            slots[0] = new ContainerItem(raidItems.RubberDuckIndex, Guid.NewGuid(), 1, 1);
        }

        for (int slot = 0; slot < stacks.Count && slot < slots.Length; slot++)
        {
            slots[slot] = new ContainerItem(checked((ushort)stacks[slot].Item.UsableIndex), Guid.NewGuid(), stacks[slot].Amount, stacks[slot].Durability);
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
        return $"#{index} {container.ClassName} tag {container.Tag ?? "-"} value {container.LootValue} pool {pool}: {(held.Length == 0 ? "empty" : held)}";
    }

    // The technical name of a carried item, or its usable index.
    private string Name(ushort index) => raidItems.Carried.TryGetValue(index, out ItemDefinition? item) ? item.TechnicalName : $"item {index}";

    /// <summary>Keeps what a player left in a container.</summary>
    /// <param name="index">Container index.</param>
    /// <param name="items">Its slots.</param>
    public void Store(int index, IReadOnlyList<ContainerItem?> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        _ = Items(index);
        _items[index] = Array.AsReadOnly([.. items]);
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
}
