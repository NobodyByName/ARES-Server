using Ares.Structs.Backend.Progression;
using Ares.Structs.Game;
using Ares.Structs.Game.Inventory;

namespace Ares.Common.Game.Inventory;

/// <summary>
/// Transitional: the shared item rules built from the generated Structs item classes, for the Host and Raid code that
/// does not receive the loaded game data yet. Host and Raid switch to instances built from their
/// <see cref="GameData.GameDataSet"/>; this file goes with the generated classes.
/// </summary>
public sealed partial class LoadoutDefinitions
{
    /// <summary>The definitions of the generated Structs item classes (transitional, see <see cref="GeneratedItemData"/>).</summary>
    public static LoadoutDefinitions Generated { get; } = new(GeneratedItemData.Sources);
}

/// <summary>Transitional: see <see cref="LoadoutDefinitions"/>.</summary>
public sealed partial class ItemTraitsTable
{
    /// <summary>The traits of the generated Structs item classes (transitional, see <see cref="GeneratedItemData"/>).</summary>
    public static ItemTraitsTable Generated { get; } = new(GeneratedItemData.Sources, LoadoutDefinitions.Generated);
}

/// <summary>
/// Transitional: the <see cref="ItemDataSources"/> of the generated Structs item classes. The catalogue items carry no
/// item class or timings except the hand items, the Ferro I and the Snap Hook, which is all the item rules read.
/// </summary>
internal static class GeneratedItemData
{
    /// <summary>The sources.</summary>
    public static ItemDataSources Sources { get; } = Create();

    private static ItemDataSources Create()
    {
        Dictionary<string, Func<ItemDefinition, ItemDefinition>> named = new(StringComparer.Ordinal)
        {
            [GameNames.Items.FerroI] = item => item with { ItemClass = FerroDefaults.ItemClass },
            [GameNames.Items.RaiderTool] = item => item with { ItemClass = HandItems.RaiderToolItemClass },
            [GameNames.Items.Unarmed] = item => item with { ItemClass = HandItems.UnarmedItemClass },
            [GameNames.Items.SnapHook] = item => item with
            {
                ItemClass = GadgetItems.SnapHookItemClass,
                EquipSeconds = GadgetItems.SnapHookEquipSeconds,
                UnequipSeconds = GadgetItems.SnapHookUnequipSeconds,
            },
        };
        ItemDefinition[] catalogue =
        [
            .. UsableItems.All.Select(item =>
            {
                ItemDefinition definition = new(item.UsableIndex, item.TechnicalName, item.DataAsset, item.AssetId, item.BaseAssetId, item.QualityAssetIds,
                    item.Category, item.Rarity, item.MaxStack, item.Weight, item.HasDurability, item.Unique, item.HasChildSlots, item.ItemTypeTags,
                    item.DropTags, item.Tags, item.LootPlacements, item.LootAvoidGroups, "", 0, 0, 0, 0);
                return named.TryGetValue(item.TechnicalName, out Func<ItemDefinition, ItemDefinition>? configure) ? configure(definition) : definition;
            }),
        ];
        Dictionary<long, ItemDefinition> byAssetId = catalogue
            .SelectMany(item => item.QualityAssetIds.Count > 0 ? item.QualityAssetIds : [item.AssetId], (item, assetId) => (item, assetId))
            .ToDictionary(entry => entry.assetId, entry => entry.item);
        Dictionary<string, ItemDefinition> byTechnicalName = catalogue.ToDictionary(item => item.TechnicalName, StringComparer.Ordinal);
        return new ItemDataSources(catalogue, byAssetId, byTechnicalName,
            [
                .. StorageItems.Definitions, .. LoadoutItems.Definitions, .. AugmentItems.Definitions, .. ShieldItems.Definitions, .. WeaponItems.Definitions,
                .. GadgetItems.Definitions, .. SalvageItems.Definitions, .. CurrencyItems.Definitions,
            ],
            ItemValues.ScrapValues, CurrencyItems.CoinsAssetId, AugmentItems.FrameAssetIds, AugmentItems.MaxWeights, AugmentItems.BranchAssetIds,
            AugmentItems.AugmentSlotAssetId, byTechnicalName[GameNames.Items.FerroI], byTechnicalName[GameNames.Items.LightShield],
            byTechnicalName[GameNames.Items.RaiderTool], byTechnicalName[GameNames.Items.Unarmed], byTechnicalName[GameNames.Items.SnapHook]);
    }
}
