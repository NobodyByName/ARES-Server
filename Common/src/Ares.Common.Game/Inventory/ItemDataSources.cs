using Ares.Common.GameData;
using Ares.Structs.Backend.Progression;
using Ares.Structs.Game.Inventory;

namespace Ares.Common.Game.Inventory;

/// <summary>The game data the shared item rules (<see cref="LoadoutDefinitions"/>, <see cref="ItemTraitsTable"/>) are built from.</summary>
/// <param name="Catalogue">The usable-item catalogue, by usable index.</param>
/// <param name="CatalogueByAssetId">Catalogue items by menu asset id, including every quality level's asset id.</param>
/// <param name="CatalogueByTechnicalName">Catalogue items by technical name.</param>
/// <param name="MenuDefinitions">
/// The menu item definitions in serving order: storage, loadout structures, augments, shields, weapons, gadgets, salvage,
/// currency.
/// </param>
/// <param name="ScrapValues">Scrap value of each item by asset id.</param>
/// <param name="CoinsAssetId">Asset id of the coins item.</param>
/// <param name="AugmentFrames">Loadout frame selected by each supported augment, by asset id.</param>
/// <param name="AugmentMaxWeights">Carrying limit selected by each supported augment, by asset id.</param>
/// <param name="AugmentBranches">The standard-frame branch of each branch of an augment's frame, by container slot asset id.</param>
/// <param name="AugmentSlotAssetId">Asset id of the loadout's augment slot.</param>
/// <param name="FerroI">The Ferro I.</param>
/// <param name="LightShield">The Light Shield.</param>
/// <param name="RaiderTool">The Raider Tool.</param>
/// <param name="Unarmed">The empty-handed item.</param>
/// <param name="SnapHook">The Snap Hook.</param>
/// <param name="Content">The backend item content the menu definitions, frames, recycle packages and quality levels come from.</param>
internal sealed record ItemDataSources(
    IReadOnlyList<ItemDefinition> Catalogue,
    IReadOnlyDictionary<long, ItemDefinition> CatalogueByAssetId,
    IReadOnlyDictionary<string, ItemDefinition> CatalogueByTechnicalName,
    IReadOnlyList<ItemDescription> MenuDefinitions,
    IReadOnlyDictionary<long, int> ScrapValues,
    long CoinsAssetId,
    IReadOnlyDictionary<long, long> AugmentFrames,
    IReadOnlyDictionary<long, double> AugmentMaxWeights,
    IReadOnlyDictionary<long, long> AugmentBranches,
    long AugmentSlotAssetId,
    ItemDefinition FerroI,
    ItemDefinition LightShield,
    ItemDefinition RaiderTool,
    ItemDefinition Unarmed,
    ItemDefinition SnapHook,
    BackendItemContent Content)
{
    /// <summary>The sources of loaded game data and backend item content.</summary>
    public static ItemDataSources From(GameDataSet data, BackendItemContent content)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(content);
        MenuItemTables menu = data.Menu;
        ResolvedGameNames names = data.Names;
        return new ItemDataSources(data.Items.All, data.Items.ByAssetId, data.Items.ByTechnicalName,
            [
                .. content.Storage, .. content.Loadout, .. menu.Augments.Definitions, .. menu.Shields.Definitions, .. menu.Weapons.Definitions,
                .. menu.Gadgets.Definitions, .. menu.Salvage.Definitions, .. menu.Currency.Definitions,
            ],
            data.Items.ScrapValues, names.Coins.AssetId, menu.Augments.FrameAssetIds, menu.Augments.MaxWeights, menu.Augments.BranchAssetIds, names.AugmentSlotAssetId,
            names.FerroI, names.LightShield, names.RaiderTool, names.Unarmed, names.SnapHook, content);
    }
}
