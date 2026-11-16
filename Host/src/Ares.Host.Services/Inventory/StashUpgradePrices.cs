using System.Collections.ObjectModel;
using Ares.Host.Services.Content;
using Ares.Structs.Backend.Progression;

namespace Ares.Host.Services.Inventory;

/// <summary>
/// The coin price of each stash upgrade, in tier order, and the definition tables that announce them. The tier chain
/// comes from the shared stash definitions; the operator can change the prices.
/// </summary>
public sealed class StashUpgradePrices
{
    private static readonly ReadOnlyCollection<ItemDescription> s_tiers = Chain();

    private readonly Dictionary<long, ItemUpgradeDescription> _upgrades;

    /// <summary>Uses <paramref name="coins"/>[i] as the price of upgrading from tier i + 1 to tier i + 2.</summary>
    /// <exception cref="ArgumentException">Not one non-negative price per upgrade.</exception>
    public StashUpgradePrices(IReadOnlyList<long> coins)
    {
        if (coins.Count != s_tiers.Count - 1 || coins.Any(price => price < 0))
        {
            throw new ArgumentException($"Stash upgrades need {s_tiers.Count - 1} coin prices of zero or more", nameof(coins));
        }

        Coins = new ReadOnlyCollection<long>([.. coins]);
        _upgrades = s_tiers.Zip(coins).ToDictionary(pair => pair.First.GameAssetId, pair => pair.First.Upgrade! with
        {
            CostPackage = new OfferCostPackage([new OfferCostItem(CurrencyItems.CoinsAssetId, pair.Second, StorageItems.InitialDurability)]),
            Cost = new ItemAmountsOfferCost([new ItemAssetAmount(CurrencyItems.CoinsAssetId, pair.Second)]),
        });
        IEnumerable<ItemDescription> definitions = InventoryCatalog.Definitions.Select(definition =>
            _upgrades.TryGetValue(definition.GameAssetId, out ItemUpgradeDescription? upgrade) ? definition with { Upgrade = upgrade } : definition);
        GameData = BackendContent.GameData with
        {
            ItemDescriptions = new ItemDescriptionTable([.. BackendContent.GameData.ItemDescriptions.ItemDescriptions, .. definitions]),
        };
    }

    /// <summary>The prices of the shared stash definitions.</summary>
    public static StashUpgradePrices Default { get; } = new([.. s_tiers.SkipLast(1).Select(tier =>
        ((ItemAmountsOfferCost)tier.Upgrade!.Cost).ItemAmounts.Single(price => price.GameAssetId == CurrencyItems.CoinsAssetId).Amount)]);

    /// <summary>Price of each upgrade, from the first tier on.</summary>
    public IReadOnlyList<long> Coins { get; }

    /// <summary>Shared definition tables announcing these prices.</summary>
    internal GameDataResponse GameData { get; }

    /// <summary>The upgrade out of a stash tier, or null for the last tier or an item that is not a stash tier.</summary>
    internal ItemUpgradeDescription? Upgrade(long assetId) => _upgrades.GetValueOrDefault(assetId);

    private static ReadOnlyCollection<ItemDescription> Chain()
    {
        List<ItemDescription> tiers = [InventoryCatalog.ByAsset[StorageItems.StashContainerAssetId]];
        while (tiers[^1].Upgrade is { NextItemGameAssetId: not 0 } upgrade)
        {
            tiers.Add(InventoryCatalog.ByAsset[upgrade.NextItemGameAssetId]);
        }

        return tiers.AsReadOnly();
    }
}
