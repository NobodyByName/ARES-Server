using System.Text.Json;
using Ares.Common.Game.Content;
using Ares.Common.GameData;
using Ares.Common.Testing;
using Ares.Host.Services.Inventory;
using Ares.Structs.Backend;
using Ares.Structs.Backend.Progression;
using Ares.Structs.Game;
using Ares.Structs.Game.Inventory;

namespace Ares.Host.Tests;

/// <summary>
/// Tiny made-up game data and content that Host runs on, for tests that need no real items: the synthetic game data
/// (<see cref="SyntheticGameData"/>) with every item <see cref="ItemTechnicalNames"/> names (stacks of up to 1000), loadout frames of seven
/// branches and the augment structures, and content files written to a folder (so they load as operator overrides) with
/// a two-tier stash, the standard frame, one recycling yield, one offer, one quality level and loot settings that spawn and
/// fill nothing. Values are invented.
/// </summary>
internal static class SyntheticHost
{
    /// <summary>Asset id of the synthetic stash slot; the tiers, content slot and frame slot follow.</summary>
    private const long StashSlot = 4_000_001;

    internal const long FirstTier = 4_000_002;

    private const long SecondTier = 4_000_003;

    private const long FrameSlot = 4_000_010;

    /// <summary>Coin price of the synthetic stash upgrade.</summary>
    internal const long UpgradeCoins = 100;

    /// <summary>Asset id of the synthetic offer.</summary>
    internal const long OfferId = 5_000_001;

    private const int BranchCount = 7;

    private static readonly Lazy<GameDataSet> s_data = new(BuildData, LazyThreadSafetyMode.ExecutionAndPublication);

    /// <summary>The synthetic game data.</summary>
    internal static GameDataSet Data => s_data.Value;

    /// <summary>Writes the synthetic content files to <paramref name="directory"/> (created when missing) and loads them.</summary>
    internal static AresContent Content(string directory)
    {
        Directory.CreateDirectory(directory);
        GameDataSet data = Data;
        long[] items = [.. data.Items.All.Select(item => item.AssetId)];
        long coins = data.Names.Coins.AssetId;
        long standard = data.Names.StandardLoadoutFrame.AssetId;
        long[] branches = [.. Branches(standard)];
        const long ContentSlot = 4_000_004;
        Write(directory, AresContent.StashFile, new
        {
            stashSlot = StashSlot,
            firstContainer = FirstTier,
            definitions = new[]
            {
                Structure(StashSlot, new SingleItemSlot([FirstTier, SecondTier], true, FirstTier)),
                Structure(FirstTier, new ItemSlotArray(ContentSlot, 4)) with
                {
                    Upgrade = new ItemUpgradeDescription(SecondTier, new OfferCostPackage([new OfferCostItem(coins, UpgradeCoins, 1.0)]),
                        new ItemAmountsOfferCost([new ItemAssetAmount(coins, UpgradeCoins)]), new ItemUpgradeRequirements([], [])),
                },
                Structure(SecondTier, new ItemSlotArray(ContentSlot, 6)),
                Structure(ContentSlot, new SingleItemSlot(items, false, 0)),
            },
        });
        Write(directory, AresContent.LoadoutFile, new
        {
            frameSlot = FrameSlot,
            standardFrame = standard,
            branches = new
            {
                backpack = branches[0],
                quickUse = branches[1],
                safePockets = branches[2],
                mainWeapon = branches[3],
                secondaryWeapon = branches[4],
                shield = branches[5],
                melee = branches[6],
            },
            definitions = (ItemDescription[])
            [
                Structure(FrameSlot, new SingleItemSlot([standard, .. data.Menu.Augments.FrameAssetIds.Values], true, standard)),
                .. Frame(standard, items).Select(row => row.Description),
            ],
        });
        Write(directory, AresContent.RecyclingFile, new[]
        {
            new { gameAssetId = Asset(ItemTechnicalNames.RubberDuck), outputs = new[] { new ItemAssetAmount(Asset(ItemTechnicalNames.MetalParts), 2) } },
        });
        Write(directory, AresContent.TraderOffersFile, new[]
        {
            new OfferDescription(OfferId, "NPC", "Metal Parts", "", 6_000_001, 0, true, [],
                new OfferCostPackage([new OfferCostItem(coins, 10, 1.0)]), new ItemAmountsOfferCost([new ItemAssetAmount(coins, 10)]),
                new OfferRewardPackage([new OfferRewardedItem(Asset(ItemTechnicalNames.MetalParts), 1, 1.0, [], "", 0, false)]),
                1, OfferDescription.NoDate, OfferDescription.NoDate, new OfferLimit(0, 0)),
        });
        Write(directory, AresContent.QualityLevelsFile, new Dictionary<string, long> { [ItemTechnicalNames.RubberDuck] = 1 });
        Write(directory, AresContent.LootSpawnFile, new
        {
            chances = new Dictionary<string, double>(),
            areaMultipliers = new Dictionary<string, double>(),
            groupCaps = true,
            limiters = true,
            spawnerClasses = Array.Empty<string>(),
            looseChances = new Dictionary<string, double>(),
        });
        Write(directory, AresContent.LootContentsFile, new
        {
            excludedDropTags = Array.Empty<string>(),
            tiers = new[] { new { name = "Other", weight = 1 } },
            firstStackTier = (string?)null,
            tablePools = Array.Empty<object>(),
            extraStack = new { chance = 0, entries = Array.Empty<object>() },
            typePools = Array.Empty<object>(),
            looseItems = new { tables = Array.Empty<string>(), amount = new { min = 1, max = 1 }, pools = Array.Empty<object>() },
        });
        Write(directory, AresContent.LootRulesFile, new { rules = Array.Empty<object>() });
        return AresContent.Load(directory);
    }

    /// <summary>A catalogue of the synthetic game data and content, with the content's stash upgrade prices.</summary>
    internal static InventoryCatalog Catalog(string directory) => CatalogOf(Content(directory));

    /// <summary>A catalogue of the synthetic game data and <paramref name="content"/>, with the content's stash upgrade prices.</summary>
    internal static InventoryCatalog CatalogOf(AresContent content) => new(Data, content, StashUpgradePrices.Of(content.Stash));

    internal static long Asset(string technicalName) => Data.Items.ByTechnicalName[technicalName].AssetId;

    private static GameDataSet BuildData()
    {
        SyntheticGameData data = new();
        foreach (string name in ItemTechnicalNames.All)
        {
            _ = data.AddItem(name, item => item with { MaxStack = 1000 });
        }

        return data.With(content =>
        {
            LoadoutFrameDefinition[] frames =
            [
                .. content.LoadoutFrames.Select(frame => frame with { Containers = [.. Branches(frame.AssetId).Select((branch, type) => new LoadoutFrameContainer(type, branch))] }),
            ];
            long[] items = [.. content.UsableItems.Select(item => item.AssetId)];
            long[] augments = [.. content.Augments.Select(augment => augment.AssetId)];
            MenuItem[] structures = [.. content.MenuAugments.Where(row => !content.Augments.Any(augment => augment.TechnicalName == row.TechnicalName))];
            long slot = Named(GameNames.ItemStructures.AugmentSlot);
            long container = Named(GameNames.ItemStructures.AugmentContainer);
            long contentSlot = Named(GameNames.ItemStructures.AugmentContentSlot);
            return content with
            {
                LoadoutFrames = [.. frames],
                MenuAugments =
                [
                    .. content.MenuAugments.Where(row => content.Augments.Any(augment => augment.TechnicalName == row.TechnicalName)),
                    new MenuItem(GameNames.ItemStructures.AugmentSlot, Structure(slot, new SingleItemSlot([container], true, container))),
                    new MenuItem(GameNames.ItemStructures.AugmentContainer, Structure(container, new ItemSlotArray(contentSlot, 1))),
                    new MenuItem(GameNames.ItemStructures.AugmentContentSlot, Structure(contentSlot, new SingleItemSlot(augments, false, 0))),
                    .. content.Augments.SelectMany(augment => Frame(augment.FrameAssetId, items)),
                ],
            };

            long Named(string name) => structures.Single(row => row.TechnicalName == name).Description.GameAssetId;
        }).Build();
    }

    // The branch of each container type of a frame.
    private static IEnumerable<long> Branches(long frame) => Enumerable.Range(0, BranchCount).Select(type => (frame * 10) + type);

    // A frame, its branches, each branch's container and the container's content slot. Each container holds two items,
    // except the safe pockets, weapons and shield, which hold one.
    private static IEnumerable<MenuItem> Frame(long frame, long[] items)
    {
        long[] branches = [.. Branches(frame)];
        yield return new MenuItem($"Synthetic_Frame_{frame}", Structure(frame, new StructuralItemSlots(branches)));
        foreach ((long branch, int type) in branches.Select((branch, type) => (branch, type)))
        {
            long container = branch * 10;
            yield return new MenuItem($"Synthetic_Branch_{branch}", Structure(branch, new SingleItemSlot([container], true, container)));
            yield return new MenuItem($"Synthetic_Container_{container}", Structure(container, new ItemSlotArray(container + 1, type < 2 ? 2 : 1)));
            yield return new MenuItem($"Synthetic_Slot_{container + 1}", Structure(container + 1, new SingleItemSlot(items, false, 0)));
        }
    }

    private static ItemDescription Structure(long assetId, ItemSlotConfiguration slots) =>
        new(assetId, "InventoryStructure", 1, false, true, [], []) { SlotConfig = slots };

    private static void Write<T>(string directory, string file, T value) =>
        File.WriteAllText(Path.Combine(directory, file), JsonSerializer.Serialize(value, BackendJson.Options));
}
