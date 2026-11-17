using Ares.Common.Game.Content;
using Ares.Common.Game.Inventory;
using Ares.Common.GameData;
using Ares.Common.Testing;
using Ares.Structs.Backend.Progression;
using Ares.Structs.Game.Inventory;

namespace Ares.Common.Tests;

/// <summary>
/// Real-data tier: the shared item rules built from the loaded game data and the default ARES content equal the
/// transitional ones built from the generated Structs classes (<see cref="GeneratedItemRules.Definitions"/>,
/// <see cref="GeneratedItemRules.Traits"/>), so that Host and Raid switch without a change of behaviour. One deliberate
/// difference: the catalogue has <c>DA_Item_Zipline</c>, which the generated data lists as not cooked. The rules serve its
/// menu description, and keep it out of the held, carried and plain items because ARES does not support it
/// (<see cref="ItemSupport"/>).
/// </summary>
public sealed class ItemRulesRealTests
{
    private const string Zipline = "DA_Item_Zipline";

    private static GameDataSet Data => RealGameData.Data;

    [RealDataFact]
    public void Loadout_definitions_equal_the_generated_ones_and_serve_the_zipline_only_to_the_menu()
    {
        LoadoutDefinitions loaded = AresContent.Defaults.ItemDefinitions(Data);
        LoadoutDefinitions generated = GeneratedItemRules.Definitions;
        ItemDefinition zipline = Data.Items.ByTechnicalName[Zipline];

        Assert.Equal(898, Data.Items.Count);
        Assert.False(ItemSupport.IsSupported(Zipline));
        Assert.Equal([zipline.AssetId], loaded.All.Select(item => item.GameAssetId).Except(generated.All.Select(item => item.GameAssetId)));
        Assert.Equal(zipline.MaxStack, loaded.ByAsset[zipline.AssetId].MaxAmount);
        Assert.Equivalent(generated.All, loaded.All.Where(item => item.GameAssetId != zipline.AssetId), strict: true);
        Assert.False(loaded.SupportsCarriedItem(zipline.AssetId));
        Assert.DoesNotContain(Zipline, loaded.AssetIds.Keys);
        Assert.Equal(generated.AssetIds, loaded.AssetIds);
        Assert.Equal(generated.CarriedItems.Select(Facts), loaded.CarriedItems.Select(Facts));
        Assert.Equal(generated.PlainItems.Select(Facts), loaded.PlainItems.Select(Facts));
        Assert.Equal(generated.AugmentSlotAssetId, loaded.AugmentSlotAssetId);
        Assert.Equal(generated.AugmentFrames, loaded.AugmentFrames);
        Assert.Equal(generated.MaximumWeight(null), loaded.MaximumWeight(null));
        Assert.All(AugmentItems.FrameAssetIds.Keys, augment =>
        {
            Assert.True(loaded.SupportsAugment(augment));
            Assert.Equal((generated.FrameAssetId(augment), generated.MaximumWeight(augment)), (loaded.FrameAssetId(augment), loaded.MaximumWeight(augment)));
        });
        Assert.All(AugmentItems.BranchAssetIds, pair => Assert.Equal(generated.StandardBranch(pair.Key), loaded.StandardBranch(pair.Key)));
    }

    [RealDataFact]
    public void Item_traits_equal_the_generated_ones()
    {
        LoadoutDefinitions definitions = AresContent.Defaults.ItemDefinitions(Data);
        ItemTraitsTable loaded = new(Data, definitions);

        Assert.Equal(GeneratedItemRules.Traits.All, loaded.All);
    }

    private static (int UsableIndex, string TechnicalName, long AssetId, double Weight, int MaxStack) Facts(ItemDefinition item) =>
        (item.UsableIndex, item.TechnicalName, item.AssetId, item.Weight, item.MaxStack);
}
