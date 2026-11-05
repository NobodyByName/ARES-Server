namespace Ares.Structs.Backend.Progression;

/// <summary>Technical asset names of supported inventory items.</summary>
public static class ItemTechnicalNames
{
    public const string RubberDuck = "DA_Item_Salvage_Trinket_RubberDuck_Standard";
    public const string CombatMk1 = "DA_Item_Augment_Combat_T1";
    public const string LootingMk1 = "DA_Item_Augment_Looting_T1";
    public const string TacticalMk1 = "DA_Item_Augment_Utility_T1";

    /// <summary>Protocol asset IDs indexed by technical item name.</summary>
    public static IReadOnlyDictionary<string, long> AssetIds { get; } = new Dictionary<string, long>(StringComparer.Ordinal)
    {
        [RubberDuck] = StorageItems.RubberDuckAssetId,
    }.Concat(AugmentItems.ItemAssetIds).ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
}
