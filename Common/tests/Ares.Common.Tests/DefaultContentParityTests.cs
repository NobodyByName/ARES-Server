using System.Text.Json;
using Ares.Common.Game.Content;
using Ares.Structs.Backend;
using Ares.Structs.Backend.Progression;
using Ares.Structs.Game.Inventory;

namespace Ares.Common.Tests;

/// <summary>Transitional: the ARES default content equals the generated Structs classes it was moved from; it goes with them.</summary>
public sealed class DefaultContentParityTests
{
    [Fact]
    public void The_default_content_equals_the_generated_backend_classes()
    {
        AresContent content = AresContent.Defaults;
        LoadoutBranches branches = content.Loadout.Branches;

        Assert.Equal(Json(StorageItems.Definitions), Json(content.Stash.Definitions));
        Assert.Equal((StorageItems.StashSlotAssetId, StorageItems.StashContainerAssetId), (content.Stash.SlotAssetId, content.Stash.FirstContainerAssetId));
        Assert.Equal(Json(LoadoutItems.Definitions), Json(content.Loadout.Definitions));
        Assert.Equal((LoadoutItems.FrameSlotAssetId, LoadoutItems.StandardFrameAssetId), (content.Loadout.FrameSlotAssetId, content.Loadout.StandardFrameAssetId));
        Assert.Equal(
            (LoadoutItems.BackpackSlotAssetId, LoadoutItems.BeltSlotAssetId, LoadoutItems.SafePocketSlotAssetId, LoadoutItems.Firearm1SlotAssetId,
                LoadoutItems.Firearm2SlotAssetId, LoadoutItems.ArmorSlotAssetId, LoadoutItems.MeleeSlotAssetId),
            (branches.Backpack, branches.QuickUse, branches.SafePockets, branches.MainWeapon, branches.SecondaryWeapon, branches.Shield, branches.Melee));
        Assert.Equal(Json(TraderOffers.Celeste), Json(content.TraderOffers));
        Assert.Equal(Json(ItemRecycling.Outputs.OrderBy(pair => pair.Key)), Json(content.RecycleOutputs.OrderBy(pair => pair.Key)));
        Assert.Equal(UsableItems.All.Where(item => item.QualityLevel != 0).ToDictionary(item => item.TechnicalName, item => (long)item.QualityLevel),
            content.QualityLevels);
    }

    private static string Json<T>(T value) => JsonSerializer.Serialize(value, BackendJson.Options);
}
