using Ares.Common.GameData;
using Ares.Structs.Backend.Progression;
using Ares.Structs.Game.Inventory;

namespace Ares.Common.Game.Inventory;

/// <summary>Seconds a quick-use item takes to put away and to draw.</summary>
/// <param name="UnequipSeconds">Time to put the item away.</param>
/// <param name="EquipSeconds">Time to draw the item.</param>
public sealed record QuickUseTimes(double UnequipSeconds, double EquipSeconds);

/// <summary>Gameplay facts of one supported item, looked up by its usable index.</summary>
/// <param name="UsableIndex">Index of the item in the usable-item catalogue.</param>
public sealed record ItemTraits(int UsableIndex)
{
    /// <summary>Usable index of the ammunition the item loads; null for items without rounds.</summary>
    public int? Ammunition { get; init; }

    /// <summary>
    /// Slots of the item's mod container; null for items without one. The client expects one for every weapon and armor
    /// item, empty when the item has no mod slots.
    /// </summary>
    public int? ModSlotCount { get; init; }

    /// <summary>Gameplay class of the item in a character's hands; null for items a character does not hold.</summary>
    public string? HeldClass { get; init; }

    /// <summary>Draw and put-away times of an item drawn from the quick-use belt; null for other items.</summary>
    public QuickUseTimes? QuickUse { get; init; }
}

/// <summary>
/// The traits of every item the servers handle: carried items, hand items and held gadgets. Built from the game data once
/// at startup and passed to what needs it.
/// </summary>
public sealed partial class ItemTraitsTable
{
    private readonly Dictionary<int, ItemTraits> _byUsableIndex;

    /// <summary>Builds the traits from loaded game data.</summary>
    /// <param name="data">The game data.</param>
    /// <param name="definitions">The shared item definitions built from the same data.</param>
    public ItemTraitsTable(GameDataSet data, LoadoutDefinitions definitions)
        : this(ItemDataSources.From(data), definitions)
    {
    }

    internal ItemTraitsTable(ItemDataSources sources, LoadoutDefinitions definitions)
    {
        ArgumentNullException.ThrowIfNull(definitions);
        Dictionary<int, string> heldClasses = new()
        {
            [sources.FerroI.UsableIndex] = sources.FerroI.ItemClass,
            [sources.RaiderTool.UsableIndex] = sources.RaiderTool.ItemClass,
            [sources.Unarmed.UsableIndex] = sources.Unarmed.ItemClass,
            [sources.SnapHook.UsableIndex] = sources.SnapHook.ItemClass,
        };
        Dictionary<int, QuickUseTimes> quickUse = new()
        {
            [sources.SnapHook.UsableIndex] = new QuickUseTimes(sources.SnapHook.UnequipSeconds, sources.SnapHook.EquipSeconds),
        };
        Dictionary<int, int> ammunition = WeaponAmmunition.ByWeapon.ToDictionary(pair => sources.CatalogueByTechnicalName[pair.Key].UsableIndex,
            pair => sources.CatalogueByTechnicalName[pair.Value].UsableIndex);

        // Carried weapons and armor get a mod container with one slot per mod slot of their definition.
        Dictionary<int, int> modSlots = definitions.CarriedItems
            .Where(item => item.Category is ItemCategory.Launcher or ItemCategory.Rifle or ItemCategory.Armor)
            .ToDictionary(item => item.UsableIndex,
                item => definitions.ByAsset[item.AssetId].SlotConfig is StructuralItemSlots slots ? slots.SlotAssetIds.Count : 0);
        All =
        [
            .. heldClasses.Keys.Union(quickUse.Keys).Union(definitions.CarriedItems.Select(item => item.UsableIndex))
                .Select(index => new ItemTraits(index)
                {
                    Ammunition = ammunition.TryGetValue(index, out int rounds) ? rounds : null,
                    ModSlotCount = modSlots.TryGetValue(index, out int slots) ? slots : null,
                    HeldClass = heldClasses.GetValueOrDefault(index),
                    QuickUse = quickUse.GetValueOrDefault(index),
                }),
        ];
        _byUsableIndex = All.ToDictionary(traits => traits.UsableIndex);
    }

    /// <summary>
    /// Traits of the handled items: the held items first, in the order the raid exports their classes, then the quick-use
    /// and carried items.
    /// </summary>
    public IReadOnlyList<ItemTraits> All { get; }

    /// <summary>The traits of an item; an item the servers do not handle has none.</summary>
    /// <param name="usableIndex">Index of the item in the usable-item catalogue.</param>
    public ItemTraits Of(int usableIndex) => _byUsableIndex.GetValueOrDefault(usableIndex) ?? new ItemTraits(usableIndex);
}
