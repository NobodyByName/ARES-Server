namespace Ares.Structs.Game.Inventory;

/// <summary>
/// One item of the game's usable-item catalogue as the installed game defines it: the facts of its data asset,
/// persistence assets, quality redirectors, UI metadata and item class. A row of the game data's usable-item table.
/// </summary>
/// <param name="UsableIndex">Position of the item in the catalogue: the asset index of an inventory slot that holds it.</param>
/// <param name="TechnicalName">Name of the item's data asset.</param>
/// <param name="DataAsset">Object path of the item's data asset (the item in the world, for example on the ground).</param>
/// <param name="AssetId">
/// Asset id menus and inventories use for the item: its quality-0 asset id when it has quality levels, otherwise
/// <paramref name="BaseAssetId"/>.
/// </param>
/// <param name="BaseAssetId">Asset id of the item's data asset itself.</param>
/// <param name="QualityAssetIds">Asset id of each quality level, from quality 0; empty when the item has no quality levels.</param>
/// <param name="Category">Item category.</param>
/// <param name="Rarity">Rarity shown in menus.</param>
/// <param name="MaxStack">Largest stack.</param>
/// <param name="Weight">Weight of one item (the category default when the data asset sets none, see <see cref="ItemDataRules.DefaultWeight"/>).</param>
/// <param name="HasDurability">Whether the item wears (see <see cref="ItemDataRules"/>); durability is a fraction of a maximum of 1.</param>
/// <param name="Unique">Whether a player owns at most one stack of it.</param>
/// <param name="HasChildSlots">Whether the item holds child slots (a weapon's mod slots).</param>
/// <param name="ItemTypeTags">Inventory item type gameplay tags (<c>Inventory.ItemType.*</c>).</param>
/// <param name="DropTags">Loot drop gameplay tags (<c>Item.Drop.*</c>) that loot table queries match.</param>
/// <param name="Tags">Gameplay tags of the item's data asset (release, flags, skins, sizes).</param>
/// <param name="LootPlacements">
/// Loot container sockets the item may spawn in: each names a socket tag (a socket whose tag is it or under it) and the tags under it
/// to skip.
/// </param>
/// <param name="LootAvoidGroups">Drop tag groups the item avoids sharing a container with.</param>
/// <param name="ItemClass">Class path of the item a character holds (<c>ItemClass</c>), for example <c>/Game/…/BP_Item_X.BP_Item_X_C</c>.</param>
/// <param name="EquipSeconds">Time to draw the item, in seconds.</param>
/// <param name="UnequipSeconds">Time to put the item away, in seconds.</param>
/// <param name="UnequipToItemsSeconds">Time to put the item away for a quick-use item, in seconds.</param>
/// <param name="ReEquipFromItemSeconds">Time to draw the item back after a quick-use item, in seconds.</param>
public sealed record ItemDefinition(
    int UsableIndex,
    string TechnicalName,
    string DataAsset,
    long AssetId,
    long BaseAssetId,
    IReadOnlyList<long> QualityAssetIds,
    ItemCategory Category,
    ItemRarity Rarity,
    int MaxStack,
    double Weight,
    bool HasDurability,
    bool Unique,
    bool HasChildSlots,
    IReadOnlyList<string> ItemTypeTags,
    IReadOnlyList<string> DropTags,
    IReadOnlyList<string> Tags,
    IReadOnlyList<LootPlacement> LootPlacements,
    IReadOnlyList<string> LootAvoidGroups,
    string ItemClass,
    double EquipSeconds,
    double UnequipSeconds,
    double UnequipToItemsSeconds,
    double ReEquipFromItemSeconds)
{
    /// <summary>Item type of the menu description: generators are outpost stations, every other item is a game item.</summary>
    public string ItemType => Category == ItemCategory.Generator ? "Generator" : "GameItem";

    /// <summary>Whether more than one item fits in a stack.</summary>
    public bool Stackable => MaxStack > 1;

    /// <summary>Tags of the item's menu description: its release and flag tags, without its other asset tags.</summary>
    public IEnumerable<string> DescriptionTags => Tags.Where(tag => tag.StartsWith("Online.Release.", StringComparison.Ordinal)
        || tag.StartsWith("Item.Flag.", StringComparison.Ordinal));
}
