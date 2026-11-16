namespace Ares.Structs.Game.Inventory;

/// <summary>
/// One item of the game's usable-item catalogue: the facts its data asset, persistence assets, quality redirectors, UI
/// metadata and item class define.
/// </summary>
/// <param name="UsableIndex">Position of the item in the catalogue: the asset index of an inventory slot that holds it.</param>
/// <param name="TechnicalName">Name of the item's data asset.</param>
/// <param name="DataAsset">Object path of the item's data asset (the item in the world, for example on the ground).</param>
/// <param name="AssetId">
/// Asset id menus and inventories use for the item: its quality-0 asset id when it has quality levels, otherwise
/// <paramref name="BaseAssetId"/>.
/// </param>
/// <param name="BaseAssetId">Asset id of the item's data asset itself.</param>
/// <param name="QualityLevel">Quality level of the item's menu description.</param>
/// <param name="QualityAssetIds">Asset id of each quality level, from quality 0; empty when the item has no quality levels.</param>
/// <param name="Category">Item category.</param>
/// <param name="Rarity">Rarity shown in menus.</param>
/// <param name="MaxStack">Largest stack.</param>
/// <param name="Weight">Weight of one item.</param>
/// <param name="HasDurability">Whether the item wears; durability is a fraction of a maximum of 1.</param>
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
public sealed record UsableItem(
    int UsableIndex,
    string TechnicalName,
    string DataAsset,
    long AssetId,
    long BaseAssetId,
    int QualityLevel,
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
    IReadOnlyList<string> LootAvoidGroups)
{
    /// <summary>Item type of the menu description: generators are outpost stations, every other item is a game item.</summary>
    public string ItemType => Category == ItemCategory.Generator ? "Generator" : "GameItem";

    /// <summary>Whether more than one item fits in a stack.</summary>
    public bool Stackable => MaxStack > 1;

    /// <summary>Tags of the item's menu description: its release and flag tags, without its other asset tags.</summary>
    public IEnumerable<string> DescriptionTags => Tags.Where(tag => tag.StartsWith("Online.Release.", StringComparison.Ordinal)
        || tag.StartsWith("Item.Flag.", StringComparison.Ordinal));
}

/// <summary>Loot container sockets an item may spawn in.</summary>
/// <param name="SocketTag">Socket gameplay tag; sockets tagged with it or a tag under it match. Empty matches no socket.</param>
/// <param name="ExcludeTags">Tags under it whose sockets do not match.</param>
public sealed record LootPlacement(string SocketTag, IReadOnlyList<string> ExcludeTags);

/// <summary>Item categories.</summary>
public enum ItemCategory
{
    Launcher = 0,
    Rifle = 1,
    Consumable = 2,
    LauncherPayload = 3,
    LauncherUniversalPayload = 4,
    RiflePayload = 5,
    Throwable = 6,
    Deployable = 7,
    Modification = 8,
    Gadget = 9,
    Armor = 10,
    CraftingReagent = 11,
    Backpack = 12,
    CraftingPart = 13,
    Misc = 14,
    MeleeWeapon = 15,
    Generator = 16,
    Currency = 17,
    Recipe = 18,
    MetaItem = 19,
    Unarmed = 20,
    Augment = 21,
}

/// <summary>Item rarity, as menus name it.</summary>
public enum ItemRarity
{
    None = 0,
    Common = 1,
    Uncommon = 2,
    Rare = 3,
    Epic = 4,
    Legendary = 5,
    Amplified = 6,
}
