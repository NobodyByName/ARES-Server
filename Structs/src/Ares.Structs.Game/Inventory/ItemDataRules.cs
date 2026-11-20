namespace Ares.Structs.Game.Inventory;

/// <summary>
/// Rules of the item data asset scripts that item definitions follow, written out by hand from the script code:
/// <c>ItemCategory::GetDefaultWeight</c>, <c>UItemDataAsset::ItemHasDurability</c>, <c>CalculateMaxLoadoutWeight</c>
/// (a free script function) and the defaults of members whose values cooked assets leave out.
/// </summary>
public static class ItemDataRules
{
    /// <summary><c>MaxStackCount</c> of an item data asset that sets none (the native class default).</summary>
    public const int DefaultMaxStack = 1;

    /// <summary><c>EDevelopmentAvailability</c> value of items players can get (every cooked item has it).</summary>
    public const byte UserFacingAvailability = 2;

    /// <summary>Carrying limit without an equipped augment (the literal of <c>CalculateMaxLoadoutWeight</c>).</summary>
    public const double DefaultMaxLoadoutWeight = 30.0;

    /// <summary><c>EquipTime</c> of an item data asset that sets none, in seconds.</summary>
    public const double DefaultEquipSeconds = 0.5;

    /// <summary><c>UnequipTime</c> of an item data asset that sets none, in seconds.</summary>
    public const double DefaultUnequipSeconds = 0.3;

    /// <summary><c>UnequipTimeToItems</c> of an item data asset that sets none, in seconds.</summary>
    public const double DefaultUnequipToItemsSeconds = 0.1;

    /// <summary><c>ReEquipTimeFromItem</c> of an item data asset that sets none, in seconds.</summary>
    public const double DefaultReEquipFromItemSeconds = 0.2;

    /// <summary><c>MaxWeight</c> of an augment data asset (<c>UAugmentItemDataAsset</c>) that sets none.</summary>
    public const double DefaultAugmentMaxWeight = 50.0;

    /// <summary>Prefix of every loot drop tag (<see cref="ItemDefinition.DropTags"/>).</summary>
    public const string DropTagPrefix = "Item.Drop.";

    /// <summary>Gameplay tag that makes an item unique besides <c>bMustBeUniqueInInventory</c>.</summary>
    public const string UniqueTag = "Item.Flag.Unique";

    /// <summary>Script class whose items (and subclasses') hold child slots: a weapon's mod slots.</summary>
    public const string ChildSlotItemClass = "UWeaponItem";

    /// <summary>The durability use rates of an item data asset; a positive one makes the item wear.</summary>
    public static IReadOnlyList<string> DurabilityRates { get; } =
    [
        "DurabilityConsumedOnUse",
        "DurabilityConsumedPerSecondWhileUsed",
        "DurabilityConsumedOnSecondaryUse",
        "DurabilityConsumedPerSecondWhileSecondaryUsed",
    ];

    /// <summary>Weight of one item of <paramref name="category"/> whose data asset sets no positive weight (<c>ItemCategory::GetDefaultWeight</c>).</summary>
    public static double DefaultWeight(ItemCategory category) => category switch
    {
        ItemCategory.Launcher or ItemCategory.Rifle => 4.0,
        ItemCategory.LauncherPayload or ItemCategory.LauncherUniversalPayload or ItemCategory.RiflePayload => 0.05,
        ItemCategory.Modification or ItemCategory.CraftingReagent or ItemCategory.CraftingPart or ItemCategory.Misc => 0.25,
        ItemCategory.Armor => 5.0,
        ItemCategory.Backpack => 1.0,
        ItemCategory.Currency => 0.001,
        _ => 0.0,
    };

    /// <summary>Whether items of <paramref name="category"/> always wear, whatever their durability use rates.</summary>
    public static bool IsDurableCategory(ItemCategory category) =>
        category is ItemCategory.Rifle or ItemCategory.Armor or ItemCategory.Augment or ItemCategory.Gadget;
}
