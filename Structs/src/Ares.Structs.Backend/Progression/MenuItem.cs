namespace Ares.Structs.Backend.Progression;

/// <summary>A menu item definition and the data asset it comes from.</summary>
/// <param name="TechnicalName">Name of the item's or inventory structure's data asset.</param>
/// <param name="Description">The definition the backend gives menus.</param>
public sealed record MenuItem(string TechnicalName, ItemDescription Description);

/// <summary>An item's values in the game's item value cache (<c>DA_LootValueCache</c>).</summary>
/// <param name="AssetId">The item's asset id.</param>
/// <param name="Name">The item's display name, as the cache records it.</param>
/// <param name="ScrapValue">Coins one item sells for at full durability.</param>
/// <param name="EconomyValue">The item's economy value; -1 when unset.</param>
public sealed record ItemValue(long AssetId, string Name, int ScrapValue, int EconomyValue);
