using System.Text.Json;
using Ares.Common.Game.Content;
using Ares.Structs.Backend;
using Ares.Structs.Backend.Progression;

namespace Ares.Common.Tests;

/// <summary>The ARES content: the built-in defaults and operator overrides.</summary>
public sealed class AresContentTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "ares-content-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    [Fact]
    public void The_defaults_load_from_the_build_with_the_stash_chain_and_equipment_places()
    {
        AresContent content = AresContent.Defaults;

        Assert.Null(content.Directory);
        Assert.Empty(content.Overrides);
        Assert.Equal(content.Stash.FirstContainerAssetId, content.Stash.Tiers[0].GameAssetId);
        Assert.Equal(Enumerable.Range(0, 12).Select(tier => 64L + (24 * tier)), content.Stash.Tiers.Select(tier => ((ItemSlotArray)tier.SlotConfig!).NumberOfSlots));
        Assert.Equal(12, content.Stash.Tiers.Count);
        Assert.Equal(content.Loadout.FrameBranches.Order(), content.Loadout.Branches.All.Order());
        Assert.NotEmpty(content.TraderOffers);
        Assert.NotEmpty(content.RecycleOutputs);
        Assert.All(content.QualityLevels.Values, level => Assert.InRange(level, 1, 7));
    }

    [Fact]
    public void A_file_in_the_content_folder_replaces_its_default_and_the_other_files_stay_built_in()
    {
        OfferDescription offer = AresContent.Defaults.TraderOffers[0] with
        {
            Cost = new ItemAmountsOfferCost([new ItemAssetAmount(1, 99)]),
        };
        Write(AresContent.TraderOffersFile, JsonSerializer.Serialize(new[] { offer }, BackendJson.Options));

        AresContent content = AresContent.Load(_directory);

        Assert.Equal(_directory, content.Directory);
        Assert.Equal([AresContent.TraderOffersFile], content.Overrides);
        Assert.Equal(99, Assert.IsType<ItemAmountsOfferCost>(Assert.Single(content.TraderOffers).Cost).ItemAmounts[0].Amount);
        Assert.Equal(Json(AresContent.Defaults.Stash.Definitions), Json(content.Stash.Definitions));
        Assert.Equal(Json(AresContent.Defaults.Loadout.Definitions), Json(content.Loadout.Definitions));
        Assert.Equal(AresContent.Defaults.RecycleOutputs.Count, content.RecycleOutputs.Count);
    }

    [Theory]
    [InlineData("trader-offer.json", "[]", "not a content file")]
    [InlineData(AresContent.TraderOffersFile, "[{]", "trader-offers.json is not valid content")]
    [InlineData(AresContent.QualityLevelsFile, "{\"DA_Item_Unknown\": -1}", "must be zero or more")]
    [InlineData(AresContent.RecyclingFile, "[{\"gameAssetId\": 1, \"outputs\": [], \"extra\": 0}]", "is not valid content")]
    [InlineData(AresContent.RecyclingFile, "[{\"gameAssetId\": 1, \"outputs\": []}]", "must give at least one item")]
    [InlineData(AresContent.StashFile, "{\"stashSlot\": 1, \"firstContainer\": 2, \"definitions\": []}", "stashSlot 1 must be defined")]
    public void A_content_folder_with_an_unknown_or_invalid_file_is_refused(string file, string text, string expected)
    {
        Write(file, text);

        Assert.Contains(expected, Assert.Throws<AresContentException>(() => AresContent.Load(_directory)).Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_missing_content_folder_is_refused()
    {
        Assert.Contains("does not exist", Assert.Throws<AresContentException>(() => AresContent.Load(_directory)).Message, StringComparison.Ordinal);
    }

    private void Write(string file, string text)
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(Path.Combine(_directory, file), text);
    }

    private static string Json<T>(T value) => JsonSerializer.Serialize(value, BackendJson.Options);
}
