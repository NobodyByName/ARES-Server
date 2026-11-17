using Ares.Common.Game.Content;
using Ares.Common.Game.Inventory;
using Ares.Common.GameData;
using Ares.Common.Testing;
using Ares.Host.Services.Inventory;
using Ares.Structs.Backend.Progression;

namespace Ares.Host.Tests;

/// <summary>
/// The real-data tier of the Host scenario tests: the inventory catalogue of the real game data (<see cref="RealGameData"/>)
/// and the default content, and the asset ids the tests name. A member that reads the game data skips the calling test
/// when no game data directory is configured; the content members need no game data.
/// </summary>
internal static class TestCatalog
{
    private static readonly Lazy<InventoryCatalog> s_real = new(() => new InventoryCatalog(RealGameData.Data, AresContent.Defaults, StashUpgradePrices.Default),
        LazyThreadSafetyMode.ExecutionAndPublication);

    /// <summary>The real game data; skips the calling test without it.</summary>
    internal static GameDataSet Data => RealGameData.Require();

    /// <summary>The catalogue of the real game data, the default content and the default stash upgrade prices.</summary>
    internal static InventoryCatalog Real
    {
        get
        {
            _ = RealGameData.Require();
            return s_real.Value;
        }
    }

    /// <summary>The shared item definitions of <see cref="Real"/>.</summary>
    internal static LoadoutDefinitions Definitions => Real.Items;

    /// <summary>The default content.</summary>
    internal static AresContent Content => AresContent.Defaults;

    internal static long StashSlot => Content.Stash.SlotAssetId;

    internal static long StashContainer => Content.Stash.FirstContainerAssetId;

    internal static long StashContentSlot => ((ItemSlotArray)Content.Stash.Tiers[0].SlotConfig!).SlotAssetId;

    /// <summary>Slots of the first stash tier.</summary>
    internal static int StashSlotCount => checked((int)((ItemSlotArray)Content.Stash.Tiers[0].SlotConfig!).NumberOfSlots);

    internal static long FrameSlot => Content.Loadout.FrameSlotAssetId;

    internal static long StandardFrame => Content.Loadout.StandardFrameAssetId;

    /// <summary>The standard frame's branch of each carried place.</summary>
    internal static LoadoutBranches Branches => Content.Loadout.Branches;

    /// <summary>The container of the standard frame's backpack branch.</summary>
    internal static long BackpackContainer => ((SingleItemSlot)Content.Loadout.Definitions.Single(definition => definition.GameAssetId == Branches.Backpack)
        .SlotConfig!).DefaultSlotAssetId;

    /// <summary>The trader every default offer belongs to: Celeste.</summary>
    internal static long CelesteAssetId => Content.TraderOffers.Select(offer => offer.OwnerGameAssetId).Distinct().Single();

    internal static long AugmentContentSlot => Data.Names.AugmentContentSlotAssetId;

    /// <summary>Loadout frame selected by each supported augment, by asset id.</summary>
    internal static IReadOnlyDictionary<long, long> AugmentFrames => Data.Menu.Augments.FrameAssetIds;

    /// <summary>Asset id of a supported item.</summary>
    internal static long Asset(string technicalName) => Definitions.AssetIds[technicalName];
}
