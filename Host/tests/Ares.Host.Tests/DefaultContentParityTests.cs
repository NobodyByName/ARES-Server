using System.Text.Json;
using Ares.Common.Game.Content;
using Ares.Common.Testing;
using Ares.Host.Services.Content;
using Ares.Host.Services.Inventory;
using Ares.Structs.Backend;
using Ares.Structs.Backend.Eos;
using Ares.Structs.Backend.Progression;
using Ares.Structs.Game.Inventory;

namespace Ares.Host.Tests;

/// <summary>
/// Transitional: the Host EOS defaults equal the generated Structs class they were moved from, and Host serves the
/// definitions of the game data and the default content's offers. Together with the Common parity tests
/// of the content and the item rules, this pins Host's served definitions to the generated classes. The only Host code
/// that reads those classes; it goes when they are deleted.
/// </summary>
public sealed class DefaultContentParityTests
{
    [Fact]
    public void The_eos_defaults_equal_the_generated_class() =>
        Assert.Equal(Json(EosSdkDefaults.Configuration), Json(EosSdkConfigurationDefaults.Configuration));

    [RealDataFact]
    public void Host_serves_the_definitions_of_the_default_content_and_the_extracted_zipline_without_holding_it()
    {
        InventoryCatalog catalog = TestCatalog.Real;
        StashUpgradePrices prices = StashUpgradePrices.Default;
        IReadOnlyList<ItemDescription> tiers = AresContent.Defaults.Stash.Tiers;
        Dictionary<long, ItemUpgradeDescription> upgrades = tiers.Zip(prices.Coins).ToDictionary(pair => pair.First.GameAssetId, pair => pair.First.Upgrade! with
        {
            CostPackage = new OfferCostPackage([new OfferCostItem(CurrencyItems.CoinsAssetId, pair.Second, 1.0)]),
            Cost = new ItemAmountsOfferCost([new ItemAssetAmount(CurrencyItems.CoinsAssetId, pair.Second)]),
        });
        GameDataResponse expected = BackendContent.GameData(TraderOffers.Celeste, AresContent.Defaults.ItemDefinitions(TestCatalog.Data).All.Select(definition =>
            upgrades.TryGetValue(definition.GameAssetId, out ItemUpgradeDescription? upgrade) ? definition with { Upgrade = upgrade } : definition));
        ItemDefinition zipline = TestCatalog.Data.Items.ByTechnicalName["DA_Item_Zipline"];

        Assert.Equal(Json(expected), Json(catalog.GameData));
        Assert.Contains(catalog.GameData.ItemDescriptions.ItemDescriptions, item => item.GameAssetId == zipline.AssetId);
        Assert.DoesNotContain(zipline.TechnicalName, catalog.ItemNames);
        Assert.False(catalog.Items.SupportsCarriedItem(zipline.AssetId));
    }

    private static string Json<T>(T value) => JsonSerializer.Serialize(value, BackendJson.Options);
}
