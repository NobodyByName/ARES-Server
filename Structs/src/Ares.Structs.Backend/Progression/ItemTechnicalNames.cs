namespace Ares.Structs.Backend.Progression;

/// <summary>Technical asset names of supported inventory items.</summary>
public static class ItemTechnicalNames
{
    public const string RubberDuck = "DA_Item_Salvage_Trinket_RubberDuck_Standard";

    /// <summary>Protocol asset IDs indexed by technical item name.</summary>
    public static IReadOnlyDictionary<string, long> AssetIds { get; } = new Dictionary<string, long>(StringComparer.Ordinal)
    {
        [RubberDuck] = StorageItems.RubberDuckAssetId,
    };
}
