using Ares.Structs.Backend.Progression;
using Ares.Structs.Game.Inventory;

namespace Ares.Common.Game.Inventory;

/// <summary>Shared item definitions and the inventory frame selected by an equipped augment.</summary>
public static class LoadoutDefinitions
{
    private static readonly ItemDescription[] s_definitions =
    [
        .. new[] { StorageItems.Definitions, LoadoutItems.Definitions, AugmentItems.Definitions, ShieldItems.Definitions, WeaponItems.Definitions,
                GadgetItems.Definitions, SalvageItems.Definitions, CurrencyItems.Definitions }
            .SelectMany(definitions => definitions),
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

    /// <summary>Selects the standard frame when empty, or the supported augment's frame.</summary>
    public static long FrameAssetId(long? augmentAssetId) => augmentAssetId is null ? LoadoutItems.StandardFrameAssetId
        : AugmentItems.FrameAssetIds.TryGetValue(augmentAssetId.Value, out long frame) ? frame
        : throw new InvalidDataException("Unsupported augment");

    /// <summary>The carrying limit applied by loadout initialization, including the unaugmented fallback.</summary>
    public static double MaximumWeight(long? augmentAssetId) => augmentAssetId is null ? AugmentItems.DefaultMaxWeight
        : AugmentItems.MaxWeights.TryGetValue(augmentAssetId.Value, out double weight) ? weight
        : throw new InvalidDataException("Unsupported augment");

    /// <summary>Unit weight of a supported gameplay item.</summary>
    public static double ItemWeight(long assetId) => assetId == StorageItems.RubberDuckAssetId ? StorageItems.RubberDuckWeight
        : assetId == ShieldItems.LightShieldAssetId ? ShieldItems.LightShieldWeight
        : assetId == WeaponItems.FerroIAssetId ? WeaponItems.FerroIWeight
        : assetId == WeaponItems.HeavyAmmoAssetId ? WeaponItems.HeavyAmmoWeight
        : assetId == GadgetItems.SnapHookAssetId ? GadgetItems.SnapHookWeight
        : AugmentItems.ItemWeights.TryGetValue(assetId, out double weight) || SalvageItems.ItemWeights.TryGetValue(assetId, out weight) ? weight
        : throw new InvalidDataException("Unsupported carried inventory item");

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
    public static int UsableIndex(long assetId) => assetId == StorageItems.RubberDuckAssetId ? InventoryBlackboard.RubberDuckIndex
        : assetId == ShieldItems.LightShieldAssetId ? ShieldItems.LightShieldUsableIndex
        : assetId == WeaponItems.FerroIAssetId ? WeaponItems.FerroIUsableIndex
        : assetId == WeaponItems.HeavyAmmoAssetId ? WeaponItems.HeavyAmmoUsableIndex
        : assetId == GadgetItems.SnapHookAssetId ? GadgetItems.SnapHookUsableIndex
        : AugmentItems.UsableIndices.TryGetValue(assetId, out int index) || SalvageItems.UsableIndices.TryGetValue(assetId, out index) ? index
        : throw new InvalidDataException("Unsupported carried inventory item");

    /// <summary>Whether the item has a supported Raid catalogue and weight mapping.</summary>
    public static bool SupportsCarriedItem(long assetId) => assetId is StorageItems.RubberDuckAssetId or ShieldItems.LightShieldAssetId
        or WeaponItems.FerroIAssetId or WeaponItems.HeavyAmmoAssetId or GadgetItems.SnapHookAssetId || AugmentItems.UsableIndices.ContainsKey(assetId)
        || SalvageItems.UsableIndices.ContainsKey(assetId);
}
