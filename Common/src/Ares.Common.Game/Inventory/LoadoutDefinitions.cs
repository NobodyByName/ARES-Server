using Ares.Structs.Backend.Progression;
using Ares.Structs.Game.Inventory;

namespace Ares.Common.Game.Inventory;

/// <summary>Shared item definitions and the inventory frame selected by an equipped augment.</summary>
public static class LoadoutDefinitions
{
    // Catalogue items Host and Raid hold without item-specific code: items without child slots, outside the categories
    // with gameplay of their own (weapons, shields, augments, hand items, currencies, outpost stations, progress counters).
    private static readonly UsableItem[] s_plainItems =
    [
        .. UsableItems.All.Where(item => item is { HasChildSlots: false, Category: not (ItemCategory.Launcher or ItemCategory.Rifle or ItemCategory.Armor
            or ItemCategory.Augment or ItemCategory.MeleeWeapon or ItemCategory.Unarmed or ItemCategory.Currency or ItemCategory.Generator
            or ItemCategory.MetaItem) }),
    ];

    private static readonly ItemDescription[] s_itemDefinitions =
    [
        .. new[] { StorageItems.Definitions, LoadoutItems.Definitions, AugmentItems.Definitions, ShieldItems.Definitions, WeaponItems.Definitions,
                GadgetItems.Definitions, SalvageItems.Definitions, CurrencyItems.Definitions }
            .SelectMany(definitions => definitions),
    ];

    private static readonly ItemDescription[] s_definitions =
    [
        .. s_itemDefinitions,
        .. s_plainItems.ExceptBy(s_itemDefinitions.Select(definition => definition.GameAssetId), item => item.AssetId).Select(Description),
    ];

    private static readonly HashSet<long> s_carried =
    [
        .. s_plainItems.Select(item => item.AssetId), ShieldItems.LightShieldAssetId, WeaponItems.FerroIAssetId, .. AugmentItems.UsableIndices.Keys,
    ];

    private static readonly HashSet<long> s_supported = [.. s_definitions.Select(definition => definition.GameAssetId)];

    /// <summary>
    /// Supported item definitions, each with its sell value when it has one, and its recycle package when it has one
    /// and every recycled item is supported.
    /// </summary>
    public static IReadOnlyList<ItemDescription> All { get; } =
    [
        .. s_definitions.Select(definition => definition with
        {
            Scrap = ItemSale.Scrap(definition.GameAssetId),
            Recycle = ItemRecycle.Package(definition.GameAssetId) is { } recycle && recycle.RewardedItems.All(item => s_supported.Contains(item.GameAssetId))
                ? recycle : null,
        }),
    ];

    public static IReadOnlyDictionary<long, ItemDescription> ByAsset { get; } = All.ToDictionary(item => item.GameAssetId);

    /// <summary>Asset ids of the supported items, by technical name.</summary>
    public static IReadOnlyDictionary<string, long> AssetIds { get; } = ItemTechnicalNames.AssetIds
        .Concat(s_plainItems.Where(item => !ItemTechnicalNames.AssetIds.ContainsKey(item.TechnicalName))
            .Select(item => KeyValuePair.Create(item.TechnicalName, item.AssetId)))
        .ToDictionary(StringComparer.Ordinal);

    /// <summary>The catalogue items players can carry in a raid, by usable index.</summary>
    public static IReadOnlyList<UsableItem> CarriedItems { get; } = [.. s_carried.Select(asset => UsableItems.ByAssetId[asset]).OrderBy(item => item.UsableIndex)];

    /// <summary>Selects the standard frame when empty, or the supported augment's frame.</summary>
    public static long FrameAssetId(long? augmentAssetId) => augmentAssetId is null ? LoadoutItems.StandardFrameAssetId
        : AugmentItems.FrameAssetIds.TryGetValue(augmentAssetId.Value, out long frame) ? frame
        : throw new InvalidDataException("Unsupported augment");

    /// <summary>The carrying limit applied by loadout initialization, including the unaugmented fallback.</summary>
    public static double MaximumWeight(long? augmentAssetId) => augmentAssetId is null ? AugmentItems.DefaultMaxWeight
        : AugmentItems.MaxWeights.TryGetValue(augmentAssetId.Value, out double weight) ? weight
        : throw new InvalidDataException("Unsupported augment");

    /// <summary>Unit weight of a supported carried item.</summary>
    public static double ItemWeight(long assetId) => CarriedItem(assetId).Weight;

    /// <summary>Maps a frame-specific branch to its standard equipment location.</summary>
    public static long StandardBranch(long branchAssetId) => AugmentItems.BranchAssetIds.GetValueOrDefault(branchAssetId, branchAssetId);

    /// <summary>Finds an equipment branch in the selected frame.</summary>
    public static long Branch(long frameAssetId, long standardBranchAssetId) => ((StructuralItemSlots)ByAsset[frameAssetId].SlotConfig!).SlotAssetIds
        .Single(branch => StandardBranch(branch) == standardBranchAssetId);

    /// <summary>Returns the content-slot layout of a frame's equipment branch.</summary>
    public static ItemSlotArray Container(long frameAssetId, long standardBranchAssetId)
    {
        long branch = Branch(frameAssetId, standardBranchAssetId);
        return (ItemSlotArray)ByAsset[((SingleItemSlot)ByAsset[branch].SlotConfig!).DefaultSlotAssetId].SlotConfig!;
    }

    /// <summary>Resolves a supported carried item to the game's usable-item catalogue.</summary>
    public static int UsableIndex(long assetId) => CarriedItem(assetId).UsableIndex;

    /// <summary>Whether players can carry the item in a raid.</summary>
    public static bool SupportsCarriedItem(long assetId) => s_carried.Contains(assetId);

    private static UsableItem CarriedItem(long assetId) => SupportsCarriedItem(assetId) ? UsableItems.ByAssetId[assetId]
        : throw new InvalidDataException("Unsupported carried inventory item");

    // The menu description of a catalogue item without child slots.
    private static ItemDescription Description(UsableItem item) => new(item.AssetId, item.ItemType, item.MaxStack, item.Stackable, item.Unique,
        [.. item.DescriptionTags], [])
    {
        BaseGameAssetId = item.BaseAssetId,
        QualityLevel = item.QualityLevel,
        SlotConfig = new NoItemSlots(),
    };
}
