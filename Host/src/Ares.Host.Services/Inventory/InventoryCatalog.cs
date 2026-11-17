using System.Collections.ObjectModel;
using Ares.Common.Game.Content;
using Ares.Common.Game.Inventory;
using Ares.Common.GameData;
using Ares.Host.Services.Content;
using Ares.Structs.Backend.Progression;

namespace Ares.Host.Services.Inventory;

/// <summary>The slots of a player's stash and carried containers.</summary>
public sealed record InventoryCapacities(int Stash, int Backpack, int QuickUse, int SafePockets);

/// <summary>
/// The item definitions Host serves and the places owned items take in them: the shared definitions built from the game
/// data and Host's content, with this Host's stash upgrade prices. Built once at startup and passed to what needs it.
/// </summary>
public sealed class InventoryCatalog
{
    private readonly Dictionary<long, string> _technicalNames;
    private readonly IReadOnlyDictionary<string, long> _currencies;
    private readonly Dictionary<long, InventoryPosition?> _branches;
    private readonly Dictionary<long, ItemUpgradeDescription> _upgrades;
    private readonly Dictionary<long, OfferDescription> _offers;
    private readonly LoadoutBranches _carried;
    private readonly int _walletCapacity;

    /// <summary>Builds the catalogue.</summary>
    /// <param name="data">The game data.</param>
    /// <param name="content">Host's content.</param>
    /// <param name="stashUpgrades">The stash upgrade prices, for the tiers of <paramref name="content"/>.</param>
    /// <exception cref="AresContentException">The content does not fit the game data or the prices.</exception>
    public InventoryCatalog(GameDataSet data, AresContent content, StashUpgradePrices stashUpgrades)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(stashUpgrades);
        if (!stashUpgrades.Tiers.SequenceEqual(content.Stash.Tiers.Select(tier => tier.GameAssetId)))
        {
            throw new AresContentException("The stash upgrade prices are for other stash tiers than the content's.");
        }

        Items = content.ItemDefinitions(data);
        _technicalNames = Items.AssetIds.ToDictionary(pair => pair.Value, pair => pair.Key);
        _currencies = data.Menu.Currency.AssetIds;
        _walletCapacity = data.Menu.Currency.Definitions.Length;
        _carried = content.Loadout.Branches;
        // Branches of the standard frame without a carried place stay empty: the melee branch.
        _branches = content.Loadout.FrameBranches.ToDictionary(branch => branch, InventoryPosition? (_) => null);
        _branches[_carried.Backpack] = new InventoryPosition(InventoryArea.Backpack, 0);
        _branches[_carried.SafePockets] = new InventoryPosition(InventoryArea.SafePockets, 0);
        _branches[_carried.QuickUse] = new InventoryPosition(InventoryArea.QuickUse, 0);
        _branches[_carried.MainWeapon] = new InventoryPosition(InventoryArea.MainWeapon, 0);
        _branches[_carried.SecondaryWeapon] = new InventoryPosition(InventoryArea.SecondaryWeapon, 0);
        _branches[_carried.Shield] = new InventoryPosition(InventoryArea.Shield, 0);
        StashSlotAssetId = content.Stash.SlotAssetId;
        FirstStashContainerAssetId = content.Stash.FirstContainerAssetId;
        AugmentContentSlotAssetId = data.Names.AugmentContentSlotAssetId;
        long coins = data.Names.Coins.AssetId;
        _upgrades = content.Stash.Tiers.Zip(stashUpgrades.Coins).ToDictionary(pair => pair.First.GameAssetId, pair => pair.First.Upgrade! with
        {
            CostPackage = new OfferCostPackage([new OfferCostItem(coins, pair.Second, OwnedItem.FullDurability)]),
            Cost = new ItemAmountsOfferCost([new ItemAssetAmount(coins, pair.Second)]),
        });
        GameData = BackendContent.GameData(content.TraderOffers, Items.All.Select(definition =>
            _upgrades.TryGetValue(definition.GameAssetId, out ItemUpgradeDescription? upgrade) ? definition with { Upgrade = upgrade } : definition));
        _offers = GameData.OfferDescriptions.OfferDescriptions.ToDictionary(offer => offer.OfferId);
        ItemNames = new ReadOnlyCollection<string>([.. Items.AssetIds.Keys]);
    }

    /// <summary>The shared item definitions.</summary>
    public LoadoutDefinitions Items { get; }

    /// <summary>The definition tables Host serves: every supported item and stash tier with its upgrade price, and the trader offers.</summary>
    public GameDataResponse GameData { get; }

    /// <summary>Technical names of the supported items.</summary>
    public IReadOnlyList<string> ItemNames { get; }

    /// <summary>The stash slot, the root holding the owned container tier.</summary>
    internal long StashSlotAssetId { get; }

    /// <summary>The container tier a new stash starts with.</summary>
    internal long FirstStashContainerAssetId { get; }

    /// <summary>The augment container's content slot.</summary>
    internal long AugmentContentSlotAssetId { get; }

    internal IReadOnlyDictionary<long, ItemDescription> ByAsset => Items.ByAsset;

    /// <summary>The slots of a player's stash and carried containers.</summary>
    public InventoryCapacities Capacities(PlayerInventory inventory)
    {
        ArgumentNullException.ThrowIfNull(inventory);
        return new InventoryCapacities(StashCapacity(inventory.Stash.ContainerAssetId), LoadoutCapacity(inventory.Loadout, InventoryArea.Backpack),
            LoadoutCapacity(inventory.Loadout, InventoryArea.QuickUse), LoadoutCapacity(inventory.Loadout, InventoryArea.SafePockets));
    }

    internal int Capacity(PlayerInventory inventory, InventoryArea area) => area switch
    {
        InventoryArea.Stash => StashCapacity(inventory.Stash.ContainerAssetId),
        InventoryArea.Wallet => _walletCapacity,
        _ => LoadoutCapacity(inventory.Loadout, area),
    };

    /// <summary>Slots of a stash container tier, or zero for an uninitialized stash.</summary>
    internal int StashCapacity(long containerAssetId) => containerAssetId == 0 ? 0
        : ((SingleItemSlot)ByAsset[StashSlotAssetId].SlotConfig!).AllowedSlotAssetIds.Contains(containerAssetId)
            && ByAsset.TryGetValue(containerAssetId, out ItemDescription? container) && container.SlotConfig is ItemSlotArray slots
            ? checked((int)slots.NumberOfSlots)
            : throw new InvalidDataException("Unsupported stash container");

    /// <summary>Whether the item is a currency, owned in the wallet rather than in a container.</summary>
    internal bool IsCurrency(string name) => _currencies.ContainsKey(name);

    internal long FrameAssetId(PlayerLoadoutState loadout) => Items.FrameAssetId(loadout.Augment is { } augment ? AssetId(augment.Type) : null);

    internal int LoadoutCapacity(PlayerLoadoutState loadout, InventoryArea area) => area switch
    {
        InventoryArea.Backpack => CapacityOf(loadout, _carried.Backpack),
        InventoryArea.QuickUse => CapacityOf(loadout, _carried.QuickUse),
        InventoryArea.SafePockets => CapacityOf(loadout, _carried.SafePockets),
        InventoryArea.Augment or InventoryArea.Shield or InventoryArea.MainWeapon or InventoryArea.SecondaryWeapon => 1,
        _ => throw new ArgumentOutOfRangeException(nameof(area)),
    };

    internal long AssetId(string name) => Items.AssetIds.TryGetValue(name, out long assetId) ? assetId
        : throw new InvalidDataException("Unknown inventory item technical name");

    internal string TechnicalName(long assetId) => _technicalNames.TryGetValue(assetId, out string? name) ? name
        : throw new InvalidDataException("Unsupported inventory item asset");

    /// <summary>The place an equipment branch holds; null for a branch that stays empty (the melee branch).</summary>
    internal InventoryPosition? Branch(long assetId) => _branches.TryGetValue(Items.StandardBranch(assetId), out InventoryPosition? position) ? position
        : throw new InvalidDataException("Unsupported equipment branch");

    /// <summary>The upgrade out of a stash tier, or null for the last tier or an item that is not a stash tier.</summary>
    internal ItemUpgradeDescription? Upgrade(long assetId) => _upgrades.GetValueOrDefault(assetId);

    /// <summary>The offer of an offer id, or null when no offer has it.</summary>
    internal OfferDescription? Offer(long offerId) => _offers.GetValueOrDefault(offerId);

    private int CapacityOf(PlayerLoadoutState loadout, long branch) => checked((int)Items.Container(FrameAssetId(loadout), branch).NumberOfSlots);
}
