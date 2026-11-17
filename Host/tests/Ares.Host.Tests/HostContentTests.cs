using Ares.Common.Game.Content;
using Ares.Host.Services.Inventory;
using Ares.Structs.Backend.Progression;

namespace Ares.Host.Tests;

/// <summary>What the catalogue makes of the ARES content.</summary>
public sealed class HostContentTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "ares-host-content-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    [Fact]
    public void The_catalogue_serves_the_content_on_the_game_data()
    {
        InventoryCatalog catalog = SyntheticHost.Catalog(_directory);
        IReadOnlyDictionary<long, ItemDescription> served = catalog.Items.ByAsset;
        long duck = SyntheticHost.Asset(ItemTechnicalNames.RubberDuck);

        Assert.Equal(AresContent.FileNames, AresContent.Load(_directory).Overrides);
        Assert.Equal(SyntheticHost.OfferId, Assert.Single(catalog.GameData.OfferDescriptions.OfferDescriptions).OfferId);
        Assert.Equal(1, served[duck].QualityLevel);
        Assert.Equal(0, served[SyntheticHost.Asset(ItemTechnicalNames.Wires)].QualityLevel);
        OfferRewardedItem recycled = Assert.Single(served[duck].Recycle!.RewardedItems);
        Assert.Equal((SyntheticHost.Asset(ItemTechnicalNames.MetalParts), 2L), (recycled.GameAssetId, recycled.Amount));
        Assert.Equal(SyntheticHost.UpgradeCoins, Assert.Single(Assert.Single(catalog.GameData.ItemDescriptions.ItemDescriptions,
            item => item.GameAssetId == SyntheticHost.FirstTier).Upgrade!.CostPackage.CostItems).Amount);
        Assert.Equal(4, catalog.StashCapacity(SyntheticHost.FirstTier));
        Assert.Equal(new InventoryCapacities(4, 2, 2, 1), catalog.Capacities(PlayerInventory.Empty with { Stash = new PlayerStash(SyntheticHost.FirstTier, []) }));
    }

    [Fact]
    public void Quality_levels_must_name_items_of_the_game_data()
    {
        _ = SyntheticHost.Content(_directory);
        Write(AresContent.QualityLevelsFile, "{\"DA_Item_Unknown\": 2}");

        Assert.Contains("the game data has no item DA_Item_Unknown", Assert.Throws<AresContentException>(() => SyntheticHost.CatalogOf(AresContent.Load(_directory)))
            .Message, StringComparison.Ordinal);
    }

    private void Write(string file, string text)
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(Path.Combine(_directory, file), text);
    }

}
