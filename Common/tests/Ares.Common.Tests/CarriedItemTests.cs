using Ares.Structs.Backend.Progression;
using Ares.Structs.Game.Inventory;

namespace Ares.Common.Tests;

public sealed class CarriedItemTests
{
    [Fact]
    public void Every_catalogue_item_without_its_own_gameplay_is_carried_with_its_catalogue_facts()
    {
        UsableItem[] plain =
        [
            .. UsableItems.All.Where(item => item is { HasChildSlots: false, Category: ItemCategory.Misc or ItemCategory.Recipe or ItemCategory.Throwable
                or ItemCategory.Modification or ItemCategory.Consumable or ItemCategory.Deployable or ItemCategory.Gadget or ItemCategory.RiflePayload }),
        ];
        Assert.Equal(691, plain.Length);
        Assert.All(plain, item =>
        {
            Assert.True(GeneratedItemRules.Definitions.SupportsCarriedItem(item.AssetId));
            Assert.Equal((item.UsableIndex, item.Weight), (GeneratedItemRules.Definitions.UsableIndex(item.AssetId), GeneratedItemRules.Definitions.ItemWeight(item.AssetId)));
            Assert.Equal(item.AssetId, GeneratedItemRules.Definitions.AssetIds[item.TechnicalName]);
            ItemDescription description = GeneratedItemRules.Definitions.ByAsset[item.AssetId];
            Assert.Equal((item.ItemType, item.MaxStack, item.Stackable, item.Unique, item.BaseAssetId, item.QualityLevel),
                (description.ItemType, (int)description.MaxAmount, description.Stackable, description.Unique, description.BaseGameAssetId, (int)description.QualityLevel));
            Assert.Equal(item.DescriptionTags, description.Tags);
            Assert.IsType<NoItemSlots>(description.SlotConfig);
        });
        Assert.Equal(plain.Length + 2 + AugmentItems.UsableIndices.Count, GeneratedItemRules.Definitions.CarriedItems.Count);
        Assert.Equal(GeneratedItemRules.Definitions.CarriedItems.Count, GeneratedItemRules.Definitions.CarriedItems.Select(item => item.UsableIndex).Distinct().Count());
    }

    [Fact]
    public void Items_with_their_own_gameplay_keep_their_current_support()
    {
        long[] supported =
        [
            StorageItems.RubberDuckAssetId, ShieldItems.LightShieldAssetId, WeaponItems.FerroIAssetId, WeaponItems.HeavyAmmoAssetId, GadgetItems.SnapHookAssetId,
            .. AugmentItems.UsableIndices.Keys, .. SalvageItems.UsableIndices.Keys,
        ];
        Assert.All(supported, asset => Assert.True(GeneratedItemRules.Definitions.SupportsCarriedItem(asset)));
        Assert.Equal(WeaponItems.FerroIUsableIndex, GeneratedItemRules.Definitions.UsableIndex(WeaponItems.FerroIAssetId));
        Assert.Equal(ShieldItems.LightShieldWeight, GeneratedItemRules.Definitions.ItemWeight(ShieldItems.LightShieldAssetId));
        Assert.IsType<StructuralItemSlots>(GeneratedItemRules.Definitions.ByAsset[WeaponItems.FerroIAssetId].SlotConfig);

        UsableItem[] unsupported =
        [
            .. UsableItems.All.Where(item => item.Category is ItemCategory.Rifle or ItemCategory.Armor or ItemCategory.Augment or ItemCategory.Currency
                or ItemCategory.Generator && !supported.Contains(item.AssetId)),
        ];
        Assert.NotEmpty(unsupported);
        Assert.All(unsupported, item =>
        {
            Assert.False(GeneratedItemRules.Definitions.SupportsCarriedItem(item.AssetId));
            Assert.Throws<InvalidDataException>(() => GeneratedItemRules.Definitions.UsableIndex(item.AssetId));
        });
        Assert.False(GeneratedItemRules.Definitions.SupportsCarriedItem(UsableItems.ByTechnicalName[ItemTechnicalNames.FerroI].QualityAssetIds[1]));
        Assert.Equal(CurrencyItems.CoinsAssetId, GeneratedItemRules.Definitions.AssetIds[ItemTechnicalNames.Coins]);
    }

    [Fact]
    public void Recycling_a_plain_item_is_described_once_every_output_is_supported()
    {
        ItemDescription[] recyclable = [.. GeneratedItemRules.Definitions.All.Where(item => item.Recycle is not null)];
        Assert.Contains(recyclable, item => UsableItems.ByAssetId.TryGetValue(item.GameAssetId, out UsableItem? usable) && usable.Category == ItemCategory.Misc
            && !SalvageItems.UsableIndices.ContainsKey(item.GameAssetId));
        Assert.All(recyclable.SelectMany(item => item.Recycle!.RewardedItems), output => Assert.True(GeneratedItemRules.Definitions.ByAsset.ContainsKey(output.GameAssetId)));
    }
}
