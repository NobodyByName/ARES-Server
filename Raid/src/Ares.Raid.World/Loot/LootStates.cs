using System.Globalization;
using Ares.Common.Game.Inventory;
using Ares.Structs.Backend.Progression;
using Ares.Structs.Game.Inventory;

namespace Ares.Raid.World.Loot;

/// <summary>
/// What each loot container of a raid holds, shared by its players: a container starts with the contents drawn at its
/// creation and keeps what the players who opened it left inside, and the time it was first opened.
/// </summary>
/// <param name="catalog">The raid's containers.</param>
public sealed class LootStates(LootCatalog catalog)
{
    private readonly Dictionary<int, IReadOnlyList<ContainerItem?>> _items = [];
    private readonly Dictionary<int, double> _openedAt = [];
    private readonly List<int> _openings = [];
    private readonly List<string> _events = [];

    private static readonly ushort s_duck = checked((ushort)LoadoutDefinitions.UsableIndex(StorageItems.RubberDuckAssetId));

    private static readonly Dictionary<ushort, string> s_names = LoadoutDefinitions.CarriedItems
        .ToDictionary(item => checked((ushort)item.UsableIndex), item => item.TechnicalName);

    /// <summary>Unit weight of each item type a container may hold.</summary>
    public static IReadOnlyDictionary<ushort, double> UnitWeights { get; } = LoadoutDefinitions.CarriedItems
        .ToDictionary(item => checked((ushort)item.UsableIndex), item => item.Weight);

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
            slots[0] = new ContainerItem(s_duck, Guid.NewGuid(), 1, 1);
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
        string items = string.Join(", ", Items(index).OfType<ContainerItem>()
            .Select(item => string.Create(CultureInfo.InvariantCulture, $"{item.Amount}x {s_names.GetValueOrDefault(item.AssetIndex, $"item {item.AssetIndex}")}")));
        string pool = (container.Contents.Pool ?? "none") + (container.Contents.Stacks.Count == 0 ? " (placeholder duck)" : "");
        return $"#{index} {container.ClassName} tag {container.Tag ?? "-"} value {container.LootValue} pool {pool}: {(items.Length == 0 ? "empty" : items)}";
    }

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
