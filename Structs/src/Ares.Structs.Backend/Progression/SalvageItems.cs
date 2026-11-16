namespace Ares.Structs.Backend.Progression;

/// <summary>Supported basic salvage material definitions.</summary>
public static class SalvageItems
{
    /// <summary>Item definitions and their allowed child slots.</summary>
    public static IReadOnlyList<ItemDescription> Definitions { get; } =
    [
        new(-1565478080L, "GameItem", 50L, true, false,
            ["Online.Release.Base"], [])
        {
            BaseGameAssetId = -1565478080L,
            QualityLevel = 0L,
            SlotConfig = new NoItemSlots(),
        },
        new(-1580114534L, "GameItem", 50L, true, false,
            ["Online.Release.Base"], [])
        {
            BaseGameAssetId = -1580114534L,
            QualityLevel = 0L,
            SlotConfig = new NoItemSlots(),
        },
        new(1086410133L, "GameItem", 50L, true, false,
            ["Online.Release.2.0"], [])
        {
            BaseGameAssetId = 1086410133L,
            QualityLevel = 0L,
            SlotConfig = new NoItemSlots(),
        },
        new(828947161L, "GameItem", 50L, true, false,
            ["Online.Release.Base"], [])
        {
            BaseGameAssetId = 828947161L,
            QualityLevel = 0L,
            SlotConfig = new NoItemSlots(),
        },
        new(-2048050889L, "GameItem", 50L, true, false,
            ["Online.Release.Base"], [])
        {
            BaseGameAssetId = -2048050889L,
            QualityLevel = 0L,
            SlotConfig = new NoItemSlots(),
        },
        new(-1437131902L, "GameItem", 50L, true, false,
            ["Online.Release.Base"], [])
        {
            BaseGameAssetId = -1437131902L,
            QualityLevel = 0L,
            SlotConfig = new NoItemSlots(),
        },
        new(1051518749L, "GameItem", 100L, true, false,
            ["Online.Release.Base"], [])
        {
            BaseGameAssetId = 1051518749L,
            QualityLevel = 0L,
            SlotConfig = new NoItemSlots(),
        },
        new(-730174445L, "GameItem", 50L, true, false,
            ["Online.Release.2.0"], [])
        {
            BaseGameAssetId = -730174445L,
            QualityLevel = 0L,
            SlotConfig = new NoItemSlots(),
        },
    ];

    /// <summary>Supported salvage technical names.</summary>
    public static IReadOnlyDictionary<string, long> ItemAssetIds { get; } = new Dictionary<string, long>
    {
        ["DA_Item_Salvage_Chemicals"] = -1565478080L,
        ["DA_Item_Salvage_Fabric"] = -1580114534L,
        ["DA_Item_Salvage_Planks"] = 1086410133L,
        ["DA_Item_Salvage_Plastic"] = 828947161L,
        ["DA_Item_Salvage_Rubber"] = -2048050889L,
        ["DA_Item_Salvage_ScrapMetal"] = -1437131902L,
        ["DA_Item_Salvage_Seeds"] = 1051518749L,
        ["DA_Item_Salvage_SheetMetal"] = -730174445L,
    };

    /// <summary>Usable-item indices of supported salvage.</summary>
    public static IReadOnlyDictionary<long, int> UsableIndices { get; } = new Dictionary<long, int>
    {
        [-1565478080L] = 469,
        [-1580114534L] = 507,
        [1086410133L] = 616,
        [828947161L] = 617,
        [-2048050889L] = 690,
        [-1437131902L] = 704,
        [1051518749L] = 706,
        [-730174445L] = 709,
    };

    /// <summary>Unit weight of supported salvage.</summary>
    public static IReadOnlyDictionary<long, double> ItemWeights { get; } = new Dictionary<long, double>
    {
        [-1565478080L] = 0.1,
        [-1580114534L] = 0.1,
        [1086410133L] = 0.1,
        [828947161L] = 0.1,
        [-2048050889L] = 0.1,
        [-1437131902L] = 0.1,
        [1051518749L] = 0.05,
        [-730174445L] = 0.1,
    };
}
