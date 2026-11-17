using Ares.Structs.Backend.Progression;
using Ares.Structs.Game.Inventory;

namespace Ares.Common.GameData;

/// <summary>
/// File names of the global JSON tables (at the root of the game data directory, see <see cref="GameDataFiles"/>) and
/// their row types in <see cref="GameDataJsonContext"/>.
/// </summary>
public static class GameTables
{
    /// <summary>
    /// The usable-item catalogue (<see cref="ItemDefinition"/>), by usable index: every item data asset of the asset
    /// registry, so the row count is the catalogue's size.
    /// </summary>
    public const string UsableItems = "usable-items.json";

    /// <summary>The item value cache (<see cref="ItemValue"/>), in the cache's order.</summary>
    public const string ItemValues = "item-values.json";

    /// <summary>
    /// The world item class list (class path strings), in list order: a class's index is its bit in the game state's
    /// preload bitfield.
    /// </summary>
    public const string WorldItemList = "world-item-list.json";

    /// <summary>Every augment (<see cref="AugmentDefinition"/>), by usable index.</summary>
    public const string Augments = "augments.json";

    /// <summary>Every loadout frame (<see cref="LoadoutFrameDefinition"/>), ordered by technical name (ordinal).</summary>
    public const string LoadoutFrames = "loadout-frames.json";

    /// <summary>Every firearm item class (<see cref="FirearmDefinition"/>), ordered by class path (ordinal).</summary>
    public const string Firearms = "firearms.json";

    /// <summary>Every shield item class (<see cref="ShieldDefinition"/>), ordered by class path (ordinal).</summary>
    public const string Shields = "shields.json";

    /// <summary>
    /// Menu definitions (<see cref="MenuItem"/>) of the tier-1 augments and the inventory structures of their loadout
    /// frames and augment slot, ordered by asset id.
    /// </summary>
    public const string MenuAugments = "menu-augments.json";

    /// <summary>Menu definitions (<see cref="MenuItem"/>) of the currency items.</summary>
    public const string MenuCurrency = "menu-currency.json";

    /// <summary>Menu definitions (<see cref="MenuItem"/>) of the gadget items.</summary>
    public const string MenuGadgets = "menu-gadgets.json";

    /// <summary>Menu definitions (<see cref="MenuItem"/>) of the salvage materials, by usable index.</summary>
    public const string MenuSalvage = "menu-salvage.json";

    /// <summary>Menu definitions (<see cref="MenuItem"/>) of the shield items.</summary>
    public const string MenuShields = "menu-shields.json";

    /// <summary>Menu definitions (<see cref="MenuItem"/>) of the firearm and ammunition items, then the firearms' mod slots in slot order.</summary>
    public const string MenuWeapons = "menu-weapons.json";
}
