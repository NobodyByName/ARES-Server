using Ares.Common.Game.Inventory;
using Ares.Common.GameData;
using Ares.Common.Testing;
using Ares.Structs.Backend.Progression;
using Ares.Structs.Game.Inventory;

namespace Ares.Common.Tests;

/// <summary>
/// Real-data tier: the shared item rules built from the loaded game data equal the transitional ones built from the
/// generated Structs classes (<see cref="LoadoutDefinitions.Generated"/>, <see cref="ItemTraitsTable.Generated"/>), so that
/// Host and Raid switch without a change of behaviour. The one known difference: the catalogue has <c>DA_Item_Zipline</c>,
/// which the generated data lists as not cooked; it is a plain item of the loaded data only.
/// </summary>
public sealed class ItemRulesRealTests
{
    private const string Zipline = "DA_Item_Zipline";

    private static GameDataSet Data => RealGameData.Data;

    private static long ZiplineAssetId => Data.Items.ByTechnicalName[Zipline].AssetId;

    [RealDataFact]
    public void Loadout_definitions_equal_the_generated_ones()
    {
        LoadoutDefinitions loaded = new(Data);
        LoadoutDefinitions generated = LoadoutDefinitions.Generated;

        Assert.Equivalent(generated.All, loaded.All.Where(definition => definition.GameAssetId != ZiplineAssetId), strict: true);
        Assert.Equal(generated.AssetIds, loaded.AssetIds.Where(pair => pair.Key != Zipline));
        Assert.Equal(generated.CarriedItems.Select(Facts), loaded.CarriedItems.Where(item => item.TechnicalName != Zipline).Select(Facts));
        Assert.Equal(generated.PlainItems.Select(Facts), loaded.PlainItems.Where(item => item.TechnicalName != Zipline).Select(Facts));
        Assert.Contains(loaded.PlainItems, item => item.TechnicalName == Zipline);
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
        LoadoutDefinitions definitions = new(Data);
        ItemTraitsTable loaded = new(Data, definitions);
        int zipline = Data.Items.ByTechnicalName[Zipline].UsableIndex;

        Assert.Equal(ItemTraitsTable.Generated.All, loaded.All.Where(traits => traits.UsableIndex != zipline));
    }

    private static (int UsableIndex, string TechnicalName, long AssetId, double Weight, int MaxStack) Facts(ItemDefinition item) =>
        (item.UsableIndex, item.TechnicalName, item.AssetId, item.Weight, item.MaxStack);
}
