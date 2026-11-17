using Ares.Common.GameData;
using Ares.Structs.Backend.Progression;
using Ares.Structs.Game.Inventory;

namespace Ares.Common.Game.Inventory;

/// <summary>
/// Shared item definitions and the inventory frame selected by an equipped augment, built from the game data once at
/// startup and passed to what needs it.
/// </summary>
public sealed partial class LoadoutDefinitions
{
    private readonly IReadOnlyDictionary<long, ItemDefinition> _catalogue;
    private readonly IReadOnlyDictionary<long, long> _augmentFrames;
    private readonly IReadOnlyDictionary<long, double> _augmentMaxWeights;
    private readonly IReadOnlyDictionary<long, long> _augmentBranches;
    private readonly HashSet<long> _carried;

    /// <summary>Builds the definitions from loaded game data.</summary>
    /// <param name="data">The game data.</param>
    public LoadoutDefinitions(GameDataSet data)
        : this(ItemDataSources.From(data))
    {
    }

    internal LoadoutDefinitions(ItemDataSources sources)
    {
        _catalogue = sources.CatalogueByAssetId;
        _augmentFrames = sources.AugmentFrames;
        _augmentMaxWeights = sources.AugmentMaxWeights;
        _augmentBranches = sources.AugmentBranches;
        AugmentSlotAssetId = sources.AugmentSlotAssetId;

        // Catalogue items Host and Raid hold without item-specific code: items without child slots, outside the categories
        // with gameplay of their own (weapons, shields, augments, hand items, currencies, outpost stations, progress counters).
        ItemDefinition[] plainItems =
        [
            .. sources.Catalogue.Where(item => item is { HasChildSlots: false, Category: not (ItemCategory.Launcher or ItemCategory.Rifle or ItemCategory.Armor
                or ItemCategory.Augment or ItemCategory.MeleeWeapon or ItemCategory.Unarmed or ItemCategory.Currency or ItemCategory.Generator
                or ItemCategory.MetaItem) }),
        ];
        ItemDescription[] definitions =
        [
            .. sources.MenuDefinitions,
            .. plainItems.ExceptBy(sources.MenuDefinitions.Select(definition => definition.GameAssetId), item => item.AssetId).Select(Description),
        ];
        _carried = [.. plainItems.Select(item => item.AssetId), sources.LightShield.AssetId, sources.FerroI.AssetId, .. sources.AugmentFrames.Keys];
        HashSet<long> supported = [.. definitions.Select(definition => definition.GameAssetId)];
        All =
        [
            .. definitions.Select(definition => definition with
            {
                Scrap = ItemRewardPackages.Scrap(sources.ScrapValues.GetValueOrDefault(definition.GameAssetId), sources.CoinsAssetId),
                Recycle = ItemRewardPackages.Recycle(definition.GameAssetId) is { } recycle && recycle.RewardedItems.All(item => supported.Contains(item.GameAssetId))
                    ? recycle : null,
            }),
        ];
        ByAsset = All.ToDictionary(item => item.GameAssetId);
        AssetIds = ItemTechnicalNames.All.Select(name => KeyValuePair.Create(name, sources.CatalogueByTechnicalName[name].AssetId))
            .Concat(plainItems.Where(item => !ItemTechnicalNames.All.Contains(item.TechnicalName, StringComparer.Ordinal))
                .Select(item => KeyValuePair.Create(item.TechnicalName, item.AssetId)))
            .ToDictionary(StringComparer.Ordinal);
        CarriedItems = [.. _carried.Select(asset => _catalogue[asset]).OrderBy(item => item.UsableIndex)];
        PlainItems = [.. plainItems.OrderBy(item => item.UsableIndex)];
    }

    /// <summary>
    /// Supported item definitions, each with its sell value when it has one, and its recycle package when it has one
    /// and every recycled item is supported.
    /// </summary>
    public IReadOnlyList<ItemDescription> All { get; }

    /// <summary>The supported item definitions by asset id.</summary>
    public IReadOnlyDictionary<long, ItemDescription> ByAsset { get; }

    /// <summary>Asset ids of the supported items, by technical name.</summary>
    public IReadOnlyDictionary<string, long> AssetIds { get; }

    /// <summary>Asset id of the loadout's augment slot.</summary>
    public long AugmentSlotAssetId { get; }

    /// <summary>The catalogue items players can carry in a raid, by usable index.</summary>
    public IReadOnlyList<ItemDefinition> CarriedItems { get; }

    /// <summary>
    /// The carried items without gameplay of their own (no child slots, no weapon, shield or augment), by usable index:
    /// any slot, a loot container included, holds them as plain stacks.
    /// </summary>
    public IReadOnlyList<ItemDefinition> PlainItems { get; }

    /// <summary>Selects the standard frame when empty, or the supported augment's frame.</summary>
    public long FrameAssetId(long? augmentAssetId) => augmentAssetId is null ? LoadoutItems.StandardFrameAssetId
        : _augmentFrames.TryGetValue(augmentAssetId.Value, out long frame) ? frame
        : throw new InvalidDataException("Unsupported augment");

    /// <summary>The carrying limit applied by loadout initialization, including the unaugmented fallback.</summary>
    public double MaximumWeight(long? augmentAssetId) => augmentAssetId is null ? ItemDataRules.DefaultMaxLoadoutWeight
        : _augmentMaxWeights.TryGetValue(augmentAssetId.Value, out double weight) ? weight
        : throw new InvalidDataException("Unsupported augment");

    /// <summary>Unit weight of a supported carried item.</summary>
    public double ItemWeight(long assetId) => CarriedItem(assetId).Weight;

    /// <summary>Maps a frame-specific branch to its standard equipment location.</summary>
    public long StandardBranch(long branchAssetId) => _augmentBranches.GetValueOrDefault(branchAssetId, branchAssetId);

    /// <summary>Finds an equipment branch in the selected frame.</summary>
    public long Branch(long frameAssetId, long standardBranchAssetId) => ((StructuralItemSlots)ByAsset[frameAssetId].SlotConfig!).SlotAssetIds
        .Single(branch => StandardBranch(branch) == standardBranchAssetId);

    /// <summary>Returns the content-slot layout of a frame's equipment branch.</summary>
    public ItemSlotArray Container(long frameAssetId, long standardBranchAssetId)
    {
        long branch = Branch(frameAssetId, standardBranchAssetId);
        return (ItemSlotArray)ByAsset[((SingleItemSlot)ByAsset[branch].SlotConfig!).DefaultSlotAssetId].SlotConfig!;
    }

    /// <summary>Resolves a supported carried item to the game's usable-item catalogue.</summary>
    public int UsableIndex(long assetId) => CarriedItem(assetId).UsableIndex;

    /// <summary>Whether players can carry the item in a raid.</summary>
    public bool SupportsCarriedItem(long assetId) => _carried.Contains(assetId);

    /// <summary>Whether a loadout may equip the augment.</summary>
    public bool SupportsAugment(long assetId) => _augmentFrames.ContainsKey(assetId);

    /// <summary>The loadout frames of the supported augments, in augment order.</summary>
    public IEnumerable<long> AugmentFrames => _augmentFrames.Values;

    private ItemDefinition CarriedItem(long assetId) => SupportsCarriedItem(assetId) ? _catalogue[assetId]
        : throw new InvalidDataException("Unsupported carried inventory item");

    // The menu description of a catalogue item without child slots. The published quality level of every such item is 0.
    private static ItemDescription Description(ItemDefinition item) => new(item.AssetId, item.ItemType, item.MaxStack, item.Stackable, item.Unique,
        [.. item.DescriptionTags], [])
    {
        BaseGameAssetId = item.BaseAssetId,
        SlotConfig = new NoItemSlots(),
    };
}
