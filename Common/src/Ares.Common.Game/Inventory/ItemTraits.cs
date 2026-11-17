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

/// <summary>The traits of every item the servers handle: carried items, hand items and held gadgets.</summary>
public static class ItemTraitsTable
{
    private static readonly Dictionary<int, string> s_heldClasses = new()
    {
        [WeaponItems.FerroIUsableIndex] = FerroDefaults.ItemClass,
        [HandItems.RaiderToolUsableIndex] = HandItems.RaiderToolItemClass,
        [HandItems.UnarmedUsableIndex] = HandItems.UnarmedItemClass,
        [GadgetItems.SnapHookUsableIndex] = GadgetItems.SnapHookItemClass,
    };

    private static readonly Dictionary<int, QuickUseTimes> s_quickUse = new()
    {
        [GadgetItems.SnapHookUsableIndex] = new QuickUseTimes(GadgetItems.SnapHookUnequipSeconds, GadgetItems.SnapHookEquipSeconds),
    };

    private static readonly Dictionary<int, int> s_ammunition = WeaponAmmunition.ByWeapon
        .ToDictionary(pair => UsableItems.ByAssetId[pair.Key].UsableIndex, pair => UsableItems.ByAssetId[pair.Value].UsableIndex);

    // Carried weapons and armor get a mod container with one slot per mod slot of their definition.
    private static readonly Dictionary<int, int> s_modSlots = LoadoutDefinitions.CarriedItems
        .Where(item => item.Category is ItemCategory.Launcher or ItemCategory.Rifle or ItemCategory.Armor)
        .ToDictionary(item => item.UsableIndex,
            item => LoadoutDefinitions.ByAsset[item.AssetId].SlotConfig is StructuralItemSlots slots ? slots.SlotAssetIds.Count : 0);

    /// <summary>
    /// Traits of the handled items: the held items first, in the order the raid exports their classes, then the quick-use
    /// and carried items.
    /// </summary>
    public static IReadOnlyList<ItemTraits> All { get; } =
    [
        .. s_heldClasses.Keys.Union(s_quickUse.Keys).Union(LoadoutDefinitions.CarriedItems.Select(item => item.UsableIndex))
            .Select(index => new ItemTraits(index)
            {
                Ammunition = s_ammunition.TryGetValue(index, out int ammunition) ? ammunition : null,
                ModSlotCount = s_modSlots.TryGetValue(index, out int modSlots) ? modSlots : null,
                HeldClass = s_heldClasses.GetValueOrDefault(index),
                QuickUse = s_quickUse.GetValueOrDefault(index),
            }),
    ];

    private static readonly Dictionary<int, ItemTraits> s_byUsableIndex = All.ToDictionary(traits => traits.UsableIndex);

    /// <summary>The traits of an item; an item the servers do not handle has none.</summary>
    /// <param name="usableIndex">Index of the item in the usable-item catalogue.</param>
    public static ItemTraits Of(int usableIndex) => s_byUsableIndex.GetValueOrDefault(usableIndex) ?? new ItemTraits(usableIndex);
}
