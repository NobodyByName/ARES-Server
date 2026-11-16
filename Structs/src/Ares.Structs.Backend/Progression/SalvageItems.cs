namespace Ares.Structs.Backend.Progression;

/// <summary>Supported salvage material definitions.</summary>
public static class SalvageItems
{
    /// <summary>Item definitions and their allowed child slots.</summary>
    public static IReadOnlyList<ItemDescription> Definitions { get; } =
    [
        new(1039185396L, "GameItem", 15L, true, false,
            ["Online.Release.Base"], [])
        {
            BaseGameAssetId = 1039185396L,
            QualityLevel = 0L,
            SlotConfig = new NoItemSlots(),
        },
        new(-668588724L, "GameItem", 15L, true, false,
            ["Online.Release.Base"], [])
        {
            BaseGameAssetId = -668588724L,
            QualityLevel = 0L,
            SlotConfig = new NoItemSlots(),
        },
        new(-1565478080L, "GameItem", 50L, true, false,
            ["Online.Release.Base"], [])
        {
            BaseGameAssetId = -1565478080L,
            QualityLevel = 0L,
            SlotConfig = new NoItemSlots(),
        },
        new(1224404530L, "GameItem", 15L, true, false,
            ["Online.Release.Base"], [])
        {
            BaseGameAssetId = 1224404530L,
            QualityLevel = 0L,
            SlotConfig = new NoItemSlots(),
        },
        new(-923201933L, "GameItem", 10L, true, false,
            ["Online.Release.Base"], [])
        {
            BaseGameAssetId = -923201933L,
            QualityLevel = 0L,
            SlotConfig = new NoItemSlots(),
        },
        new(1625205858L, "GameItem", 10L, true, false,
            ["Online.Release.Base"], [])
        {
            BaseGameAssetId = 1625205858L,
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
        new(-574262619L, "GameItem", 10L, true, false,
            ["Online.Release.Base"], [])
        {
            BaseGameAssetId = -574262619L,
            QualityLevel = 0L,
            SlotConfig = new NoItemSlots(),
        },
        new(-863904193L, "GameItem", 15L, true, false,
            ["Online.Release.Base"], [])
        {
            BaseGameAssetId = -863904193L,
            QualityLevel = 0L,
            SlotConfig = new NoItemSlots(),
        },
        new(1370841463L, "GameItem", 15L, true, false,
            ["Online.Release.Base"], [])
        {
            BaseGameAssetId = 1370841463L,
            QualityLevel = 0L,
            SlotConfig = new NoItemSlots(),
        },
        new(1959573223L, "GameItem", 10L, true, false,
            ["Online.Release.Base"], [])
        {
            BaseGameAssetId = 1959573223L,
            QualityLevel = 0L,
            SlotConfig = new NoItemSlots(),
        },
        new(1741060583L, "GameItem", 15L, true, false,
            ["Online.Release.Base"], [])
        {
            BaseGameAssetId = 1741060583L,
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
        new(-1517601046L, "GameItem", 15L, true, false,
            ["Online.Release.Base"], [])
        {
            BaseGameAssetId = -1517601046L,
            QualityLevel = 0L,
            SlotConfig = new NoItemSlots(),
        },
        new(-633851525L, "GameItem", 15L, true, false,
            ["Online.Release.Base"], [])
        {
            BaseGameAssetId = -633851525L,
            QualityLevel = 0L,
            SlotConfig = new NoItemSlots(),
        },
        new(-799395426L, "GameItem", 10L, true, false,
            ["Online.Release.Base"], [])
        {
            BaseGameAssetId = -799395426L,
            QualityLevel = 0L,
            SlotConfig = new NoItemSlots(),
        },
    ];

    /// <summary>Supported salvage technical names.</summary>
    public static IReadOnlyDictionary<string, long> ItemAssetIds { get; } = new Dictionary<string, long>
    {
        ["DA_Item_Salvage_Battery"] = 1039185396L,
        ["DA_Item_Salvage_Canister"] = -668588724L,
        ["DA_Item_Salvage_Chemicals"] = -1565478080L,
        ["DA_Item_Salvage_DuctTape"] = 1224404530L,
        ["DA_Item_Salvage_ElectricalComponents"] = -923201933L,
        ["DA_Item_Salvage_ExplosiveMixture"] = 1625205858L,
        ["DA_Item_Salvage_Fabric"] = -1580114534L,
        ["DA_Item_Salvage_GunParts"] = -574262619L,
        ["DA_Item_Salvage_Magnet"] = -863904193L,
        ["DA_Item_Salvage_Mat_ARC_Metal"] = 1370841463L,
        ["DA_Item_Salvage_MechanicalComponents"] = 1959573223L,
        ["DA_Item_Salvage_Oil"] = 1741060583L,
        ["DA_Item_Salvage_Planks"] = 1086410133L,
        ["DA_Item_Salvage_Plastic"] = 828947161L,
        ["DA_Item_Salvage_Rubber"] = -2048050889L,
        ["DA_Item_Salvage_ScrapMetal"] = -1437131902L,
        ["DA_Item_Salvage_Seeds"] = 1051518749L,
        ["DA_Item_Salvage_SheetMetal"] = -730174445L,
        ["DA_Item_Salvage_SteelSpring"] = -1517601046L,
        ["DA_Item_Salvage_Wires"] = -633851525L,
        ["DA_Item_Salvage_WovenCloth"] = -799395426L,
    };

    /// <summary>Usable-item indices of supported salvage.</summary>
    public static IReadOnlyDictionary<long, int> UsableIndices { get; } = new Dictionary<long, int>
    {
        [1039185396L] = 450,
        [-668588724L] = 467,
        [-1565478080L] = 469,
        [1224404530L] = 496,
        [-923201933L] = 497,
        [1625205858L] = 505,
        [-1580114534L] = 507,
        [-574262619L] = 540,
        [-863904193L] = 554,
        [1370841463L] = 559,
        [1959573223L] = 588,
        [1741060583L] = 607,
        [1086410133L] = 616,
        [828947161L] = 617,
        [-2048050889L] = 690,
        [-1437131902L] = 704,
        [1051518749L] = 706,
        [-730174445L] = 709,
        [-1517601046L] = 724,
        [-633851525L] = 786,
        [-799395426L] = 787,
    };

    /// <summary>Unit weight of supported salvage.</summary>
    public static IReadOnlyDictionary<long, double> ItemWeights { get; } = new Dictionary<long, double>
    {
        [1039185396L] = 0.25,
        [-668588724L] = 0.25,
        [-1565478080L] = 0.1,
        [1224404530L] = 0.25,
        [-923201933L] = 0.5,
        [1625205858L] = 0.5,
        [-1580114534L] = 0.1,
        [-574262619L] = 0.3,
        [-863904193L] = 0.25,
        [1370841463L] = 0.25,
        [1959573223L] = 0.5,
        [1741060583L] = 0.25,
        [1086410133L] = 0.1,
        [828947161L] = 0.1,
        [-2048050889L] = 0.1,
        [-1437131902L] = 0.1,
        [1051518749L] = 0.05,
        [-730174445L] = 0.1,
        [-1517601046L] = 0.25,
        [-633851525L] = 0.25,
        [-799395426L] = 0.25,
    };
}
