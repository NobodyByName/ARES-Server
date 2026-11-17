using System.Collections.Immutable;
using Ares.Structs.Backend.Progression;
using Ares.Structs.Game;
using Ares.Structs.Game.Inventory;

namespace Ares.Common.GameData;

/// <summary>The menu item tables: the backend item definitions of the items and inventory structures a server serves.</summary>
public sealed class MenuItemTables
{
    internal MenuItemTables(GameDataContent content, ItemCatalog items)
    {
        Currency = new MenuTable(content.MenuCurrency, GameTables.MenuCurrency);
        Gadgets = new MenuTable(content.MenuGadgets, GameTables.MenuGadgets);
        Salvage = new SalvageMenu(content.MenuSalvage, items);
        Shields = new MenuTable(content.MenuShields, GameTables.MenuShields);
        Weapons = new MenuTable(content.MenuWeapons, GameTables.MenuWeapons);
        Augments = new AugmentMenu(content.MenuAugments, items);
    }

    /// <summary>The currency items (generated: <c>CurrencyItems</c>).</summary>
    public MenuTable Currency { get; }

    /// <summary>The gadget items (generated: <c>GadgetItems</c>).</summary>
    public MenuTable Gadgets { get; }

    /// <summary>The salvage materials (generated: <c>SalvageItems</c>).</summary>
    public SalvageMenu Salvage { get; }

    /// <summary>The shield items (generated: <c>ShieldItems</c>).</summary>
    public MenuTable Shields { get; }

    /// <summary>The firearm and ammunition items and the firearms' mod slots (generated: <c>WeaponItems</c>).</summary>
    public MenuTable Weapons { get; }

    /// <summary>The tier-1 augments and the inventory structures of their frames (generated: <c>AugmentItems</c>).</summary>
    public AugmentMenu Augments { get; }
}

/// <summary>One menu item table.</summary>
public class MenuTable
{
    internal MenuTable(ImmutableArray<MenuItem> rows, string table)
    {
        Items = rows;
        Definitions = [.. rows.Select(row => row.Description)];
        AssetIds = GameDataIndex.Unique(rows.Select(row => (row.TechnicalName, row.Description.GameAssetId)), $"menu item of {table}", StringComparer.Ordinal);
    }

    /// <summary>The rows, in table order.</summary>
    public ImmutableArray<MenuItem> Items { get; }

    /// <summary>The rows' item definitions, in table order (generated: <c>…Items.Definitions</c>).</summary>
    public ImmutableArray<ItemDescription> Definitions { get; }

    /// <summary>The asset id of each row by technical name.</summary>
    public IReadOnlyDictionary<string, long> AssetIds { get; }

    /// <summary>The catalogue item of a menu row.</summary>
    private protected static ItemDefinition CatalogueItem(ItemCatalog items, MenuItem row, string table) =>
        items.ByTechnicalName.TryGetValue(row.TechnicalName, out ItemDefinition? item) && item.AssetId == row.Description.GameAssetId
            ? item
            : throw new GameDataException($"Menu item {row.TechnicalName} of {table} has no usable item of the same asset id. Run the ARES-GameDataExtract of this ARES version again.");
}

/// <summary>The salvage menu table, with the catalogue values of its items.</summary>
public sealed class SalvageMenu : MenuTable
{
    internal SalvageMenu(ImmutableArray<MenuItem> rows, ItemCatalog items)
        : base(rows, GameTables.MenuSalvage)
    {
        ItemDefinition[] salvage = [.. rows.Select(row => CatalogueItem(items, row, GameTables.MenuSalvage))];
        ItemAssetIds = salvage.ToDictionary(item => item.TechnicalName, item => item.AssetId, StringComparer.Ordinal);
        UsableIndices = salvage.ToDictionary(item => item.AssetId, item => item.UsableIndex);
        ItemWeights = salvage.ToDictionary(item => item.AssetId, item => item.Weight);
    }

    /// <summary>Asset id of each salvage material by technical name (generated: <c>SalvageItems.ItemAssetIds</c>).</summary>
    public IReadOnlyDictionary<string, long> ItemAssetIds { get; }

    /// <summary>Usable index of each salvage material by asset id (generated: <c>SalvageItems.UsableIndices</c>).</summary>
    public IReadOnlyDictionary<long, int> UsableIndices { get; }

    /// <summary>Unit weight of each salvage material by asset id (generated: <c>SalvageItems.ItemWeights</c>).</summary>
    public IReadOnlyDictionary<long, double> ItemWeights { get; }
}

/// <summary>
/// The augment menu table, with what each augment it lists (an augment of the augment table) selects: its loadout frame,
/// carrying limit, and the standard-frame branch of each of its frame's branches.
/// </summary>
public sealed class AugmentMenu : MenuTable
{
    internal AugmentMenu(ImmutableArray<MenuItem> rows, ItemCatalog items)
        : base(rows, GameTables.MenuAugments)
    {
        (ItemDefinition Item, AugmentDefinition Augment)[] augments =
        [
            .. rows.Where(row => items.AugmentsByTechnicalName.ContainsKey(row.TechnicalName))
                .Select(row => (Item: CatalogueItem(items, row, GameTables.MenuAugments), Augment: items.AugmentsByTechnicalName[row.TechnicalName]))
                .OrderBy(entry => entry.Item.TechnicalName, StringComparer.Ordinal),
        ];
        ItemAssetIds = augments.ToDictionary(entry => entry.Item.TechnicalName, entry => entry.Item.AssetId, StringComparer.Ordinal);
        UsableIndices = augments.ToDictionary(entry => entry.Item.AssetId, entry => entry.Item.UsableIndex);
        ItemWeights = augments.ToDictionary(entry => entry.Item.AssetId, entry => entry.Item.Weight);
        FrameAssetIds = augments.ToDictionary(entry => entry.Item.AssetId, entry => entry.Augment.FrameAssetId);
        MaxWeights = augments.ToDictionary(entry => entry.Item.AssetId, entry => entry.Augment.MaxWeight);
        BranchAssetIds = Branches(items, augments.Select(entry => entry.Augment));
    }

    /// <summary>Asset id of each augment by technical name (generated: <c>AugmentItems.ItemAssetIds</c>).</summary>
    public IReadOnlyDictionary<string, long> ItemAssetIds { get; }

    /// <summary>Usable index of each augment by asset id (generated: <c>AugmentItems.UsableIndices</c>).</summary>
    public IReadOnlyDictionary<long, int> UsableIndices { get; }

    /// <summary>Unit weight of each augment carried as an item, by asset id (generated: <c>AugmentItems.ItemWeights</c>).</summary>
    public IReadOnlyDictionary<long, double> ItemWeights { get; }

    /// <summary>Loadout frame selected by each augment, by asset id (generated: <c>AugmentItems.FrameAssetIds</c>).</summary>
    public IReadOnlyDictionary<long, long> FrameAssetIds { get; }

    /// <summary>Carrying limit selected by each augment, by asset id (generated: <c>AugmentItems.MaxWeights</c>).</summary>
    public IReadOnlyDictionary<long, double> MaxWeights { get; }

    /// <summary>
    /// The branch of the standard loadout frame (<see cref="GameNames.ItemStructures.StandardLoadoutFrame"/>) that matches
    /// each branch of an augment's frame, by container slot asset id (generated: <c>AugmentItems.BranchAssetIds</c>).
    /// </summary>
    public IReadOnlyDictionary<long, long> BranchAssetIds { get; }

    private static Dictionary<long, long> Branches(ItemCatalog items, IEnumerable<AugmentDefinition> augments)
    {
        if (!items.LoadoutFramesByTechnicalName.TryGetValue(GameNames.ItemStructures.StandardLoadoutFrame, out LoadoutFrameDefinition? standard))
        {
            throw ResolvedGameNames.Missing("loadout frame", GameNames.ItemStructures.StandardLoadoutFrame, "GameNames.ItemStructures.StandardLoadoutFrame");
        }

        Dictionary<int, long> standardBranches = GameDataIndex.Unique(standard.Containers.Select(container => (container.Type, container.ContainerSlotAssetId)),
            $"container type of {standard.TechnicalName} with type");
        Dictionary<long, long> branches = [];
        foreach (AugmentDefinition augment in augments)
        {
            LoadoutFrameDefinition frame = items.LoadoutFramesByAssetId.TryGetValue(augment.FrameAssetId, out LoadoutFrameDefinition? found) ? found
                : throw new GameDataException($"Augment {augment.TechnicalName} selects loadout frame {augment.FrameAssetId}, which the loadout frame table does not list. Run the ARES-GameDataExtract of this ARES version again.");
            foreach (LoadoutFrameContainer container in frame.Containers)
            {
                branches[container.ContainerSlotAssetId] = standardBranches.TryGetValue(container.Type, out long branch) ? branch
                    : throw new GameDataException($"Loadout frame {frame.TechnicalName} has a container of type {container.Type}, which {standard.TechnicalName} does not have. Run the ARES-GameDataExtract of this ARES version again.");
            }
        }

        return branches;
    }
}
