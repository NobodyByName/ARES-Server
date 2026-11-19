using System.Collections.ObjectModel;
using Ares.Common.Game.Content;
using Ares.Structs.Backend.Progression;

namespace Ares.Host.Services.Inventory;

/// <summary>
/// The coin price of each stash upgrade, in tier order. The tier chain comes from the stash content
/// (<see cref="StashContent.Tiers"/>); the operator can change the prices.
/// </summary>
public sealed class StashUpgradePrices
{
    /// <summary>Uses <paramref name="coins"/>[i] as the price of upgrading from tier i + 1 to tier i + 2 of <paramref name="stash"/>.</summary>
    /// <exception cref="ArgumentException">Not one non-negative price per upgrade.</exception>
    public StashUpgradePrices(StashContent stash, IReadOnlyList<long> coins)
    {
        ArgumentNullException.ThrowIfNull(stash);
        ArgumentNullException.ThrowIfNull(coins);
        if (coins.Count != stash.Tiers.Count - 1 || coins.Any(price => price < 0))
        {
            throw new ArgumentException($"Stash upgrades need {stash.Tiers.Count - 1} coin prices of zero or more", nameof(coins));
        }

        Tiers = new ReadOnlyCollection<long>([.. stash.Tiers.Select(tier => tier.GameAssetId)]);
        Coins = new ReadOnlyCollection<long>([.. coins]);
    }

    /// <summary>Price of each upgrade, from the first tier on.</summary>
    public IReadOnlyList<long> Coins { get; }

    /// <summary>Asset ids of the stash tiers the prices are for, from the first.</summary>
    internal IReadOnlyList<long> Tiers { get; }

    /// <summary>The prices the stash content's own upgrades name.</summary>
    /// <param name="stash">The stash content.</param>
    public static StashUpgradePrices Of(StashContent stash)
    {
        ArgumentNullException.ThrowIfNull(stash);
        return new StashUpgradePrices(stash, [.. stash.Tiers.SkipLast(1).Select(tier => ((ItemAmountsOfferCost)tier.Upgrade!.Cost).ItemAmounts[0].Amount)]);
    }
}
