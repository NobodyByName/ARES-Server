using Ares.Common.Game.Inventory;
using Ares.Common.GameData;
using Ares.Common.Testing;
using Ares.Structs.Backend.Progression;
using Ares.Structs.Game.Inventory;

namespace Ares.Common.Tests;

/// <summary>Real-data tier: which catalogue items the item rules of the default ARES content carry.</summary>
public sealed class CarriedItemTests
{
    private static readonly Lazy<LoadoutDefinitions> s_definitions = new(() => ShippedContent.Value.ItemDefinitions(RealGameData.Data));

    private static LoadoutDefinitions Definitions => s_definitions.Value;

    private static GameDataSet Data => RealGameData.Data;

    [RealDataFact]
    public void Every_catalogue_item_without_its_own_gameplay_is_carried_with_its_catalogue_facts()
    {
        ItemDefinition[] plain =
        [
            .. Data.Items.All.Where(item => ItemSupport.IsSupported(item.TechnicalName) && item is { HasChildSlots: false, Category: ItemCategory.Misc
                or ItemCategory.Recipe or ItemCategory.Throwable or ItemCategory.Modification or ItemCategory.Consumable or ItemCategory.Deployable
                or ItemCategory.Gadget or ItemCategory.RiflePayload }),
        ];
        Assert.NotEmpty(plain);
        Assert.All(plain, item =>
        {
            Assert.True(Definitions.SupportsCarriedItem(item.AssetId));
            Assert.Equal((item.UsableIndex, item.Weight), (Definitions.UsableIndex(item.AssetId), Definitions.ItemWeight(item.AssetId)));
            Assert.Equal(item.AssetId, Definitions.AssetIds[item.TechnicalName]);
            ItemDescription description = Definitions.ByAsset[item.AssetId];
            Assert.Equal((item.ItemType, item.MaxStack, item.Stackable, item.Unique, item.BaseAssetId),
                (description.ItemType, (int)description.MaxAmount, description.Stackable, description.Unique, description.BaseGameAssetId));
            Assert.Equal(item.DescriptionTags, description.Tags);
            Assert.IsType<NoItemSlots>(description.SlotConfig);
        });
        Assert.Equal(plain.Length + 2 + Data.Menu.Augments.UsableIndices.Count, Definitions.CarriedItems.Count);
        Assert.Equal(Definitions.CarriedItems.Count, Definitions.CarriedItems.Select(item => item.UsableIndex).Distinct().Count());
    }

    [RealDataFact]
    public void Items_with_their_own_gameplay_keep_their_current_support()
    {
        ResolvedGameNames names = Data.Names;
        long[] supported =
        [
            Data.Items.ByTechnicalName[ItemTechnicalNames.RubberDuck].AssetId, names.LightShield.AssetId, names.FerroI.AssetId, names.HeavyAmmo.AssetId,
            names.SnapHook.AssetId, .. Data.Menu.Augments.UsableIndices.Keys, .. Data.Menu.Salvage.UsableIndices.Keys,
        ];
        Assert.All(supported, asset => Assert.True(Definitions.SupportsCarriedItem(asset)));
        Assert.Equal(names.FerroI.UsableIndex, Definitions.UsableIndex(names.FerroI.AssetId));
        Assert.Equal(names.LightShield.Weight, Definitions.ItemWeight(names.LightShield.AssetId));
        Assert.IsType<StructuralItemSlots>(Definitions.ByAsset[names.FerroI.AssetId].SlotConfig);

        ItemDefinition[] unsupported =
        [
            .. Data.Items.All.Where(item => item.Category is ItemCategory.Rifle or ItemCategory.Armor or ItemCategory.Augment or ItemCategory.Currency
                or ItemCategory.Generator && !supported.Contains(item.AssetId)),
        ];
        Assert.NotEmpty(unsupported);
        Assert.All(unsupported, item =>
        {
            Assert.False(Definitions.SupportsCarriedItem(item.AssetId));
            Assert.Throws<InvalidDataException>(() => Definitions.UsableIndex(item.AssetId));
        });
        Assert.False(Definitions.SupportsCarriedItem(names.FerroI.QualityAssetIds[1]));
        Assert.Equal(names.Coins.AssetId, Definitions.AssetIds[ItemTechnicalNames.Coins]);
    }

    [RealDataFact]
    public void Recycling_a_plain_item_is_described_once_every_output_is_supported()
    {
        ItemDescription[] recyclable = [.. Definitions.All.Where(item => item.Recycle is not null)];
        Assert.Contains(recyclable, item => Data.Items.ByAssetId.TryGetValue(item.GameAssetId, out ItemDefinition? usable) && usable.Category == ItemCategory.Misc
            && !Data.Menu.Salvage.UsableIndices.ContainsKey(item.GameAssetId));
        Assert.All(recyclable.SelectMany(item => item.Recycle!.RewardedItems), output => Assert.True(Definitions.ByAsset.ContainsKey(output.GameAssetId)));
    }
}
