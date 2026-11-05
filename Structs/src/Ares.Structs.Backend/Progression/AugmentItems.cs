namespace Ares.Structs.Backend.Progression;

/// <summary>Tier-1 augments and their player inventory frames.</summary>
public static class AugmentItems
{
    /// <summary>Asset id of AugmentSlot.</summary>
    public const long AugmentSlotAssetId = 129937576L;

    /// <summary>Asset id of AugmentContainer.</summary>
    public const long AugmentContainerAssetId = -1101416870L;

    /// <summary>Asset id of AugmentContentSlot.</summary>
    public const long AugmentContentSlotAssetId = -875777364L;

    /// <summary>Asset id of CombatMk1.</summary>
    public const long CombatMk1AssetId = 50992628L;

    /// <summary>Asset id of LootingMk1.</summary>
    public const long LootingMk1AssetId = 1680690244L;

    /// <summary>Asset id of TacticalMk1.</summary>
    public const long TacticalMk1AssetId = 966773769L;

    /// <summary>Item definitions and their allowed child slots.</summary>
    public static IReadOnlyList<ItemDescription> Definitions { get; } =
    [
        new(-2143848294L, "InventoryStructure", 1L, false, true,
            ["Item.Flag.Unique", "Item.Flag.Persistent", "Online.Release.Base"], [])
        {
            BaseGameAssetId = 0L,
            QualityLevel = 0L,
            SlotConfig = new ItemSlotArray(-3680536L, 1L),
        },
        new(-2142772724L, "InventoryStructure", 1L, false, true,
            ["Item.Flag.Unique", "Item.Flag.Persistent", "Online.Release.Base"], [])
        {
            BaseGameAssetId = 0L,
            QualityLevel = 0L,
            SlotConfig = new SingleItemSlot(
            [
                -1711433724L,
            ], true, -1711433724L),
        },
        new(-2114213383L, "InventoryStructure", 1L, false, true,
            ["Item.Flag.Unique", "Item.Flag.Persistent", "Online.Release.Base"], [])
        {
            BaseGameAssetId = 0L,
            QualityLevel = 0L,
            SlotConfig = new SingleItemSlot(
            [
                1360021217L,
            ], true, 1360021217L),
        },
        new(-1989158897L, "InventoryStructure", 1L, false, true,
            ["Item.Flag.Unique", "Item.Flag.Persistent", "Online.Release.Base"], [])
        {
            BaseGameAssetId = 0L,
            QualityLevel = 0L,
            SlotConfig = new ItemSlotArray(-666604429L, 1L),
        },
        new(-1719547500L, "InventoryStructure", 1L, false, true,
            ["Item.Flag.Unique", "Item.Flag.Persistent", "Online.Release.Base"], [])
        {
            BaseGameAssetId = 0L,
            QualityLevel = 0L,
            SlotConfig = new ItemSlotArray(-666604429L, 1L),
        },
        new(-1711433724L, "InventoryStructure", 1L, false, true,
            ["Item.Flag.Unique", "Item.Flag.Persistent", "Online.Release.Base"], [])
        {
            BaseGameAssetId = 0L,
            QualityLevel = 0L,
            SlotConfig = new ItemSlotArray(-620731692L, 1L),
        },
        new(-1475517378L, "InventoryStructure", 1L, false, true,
            ["Item.Flag.Unique", "Item.Flag.Persistent", "Online.Release.Base"], [])
        {
            BaseGameAssetId = 0L,
            QualityLevel = 0L,
            SlotConfig = new ItemSlotArray(1440007245L, 16L),
        },
        new(-1449979565L, "InventoryStructure", 1L, false, true,
            ["Item.Flag.Unique", "Item.Flag.Persistent", "Online.Release.Base"], [])
        {
            BaseGameAssetId = 0L,
            QualityLevel = 0L,
            SlotConfig = new ItemSlotArray(-620731692L, 1L),
        },
        new(-1444511024L, "InventoryStructure", 1L, false, true,
            ["Item.Flag.Unique", "Item.Flag.Persistent", "Online.Release.Base"], [])
        {
            BaseGameAssetId = 0L,
            QualityLevel = 0L,
            SlotConfig = new SingleItemSlot(
            [
                -2143848294L,
            ], true, -2143848294L),
        },
        new(-1406187830L, "InventoryStructure", 1L, false, true,
            ["Item.Flag.Unique", "Item.Flag.Persistent", "Online.Release.Base"], [])
        {
            BaseGameAssetId = 0L,
            QualityLevel = 0L,
            SlotConfig = new ItemSlotArray(-1277506061L, 4L),
        },
        new(-1351184816L, "InventoryStructure", 1L, false, true,
            ["Item.Flag.Unique", "Item.Flag.Persistent", "Online.Release.Base"], [])
        {
            BaseGameAssetId = 0L,
            QualityLevel = 0L,
            SlotConfig = new SingleItemSlot(
            [
                202906145L,
            ], true, 202906145L),
        },
        new(-1347093084L, "InventoryStructure", 1L, false, true,
            ["Item.Flag.Unique", "Item.Flag.Persistent", "Online.Release.Base"], [])
        {
            BaseGameAssetId = 0L,
            QualityLevel = 0L,
            SlotConfig = new SingleItemSlot(
            [
                -35282746L,
            ], true, -35282746L),
        },
        new(-1101416870L, "InventoryStructure", 1L, false, true,
            ["Item.Flag.Unique", "Online.Release.Base"], [])
        {
            BaseGameAssetId = 0L,
            QualityLevel = 0L,
            SlotConfig = new ItemSlotArray(-875777364L, 1L),
        },
        new(-1088427979L, "InventoryStructure", 1L, false, true,
            ["Item.Flag.Unique", "Item.Flag.Persistent", "Online.Release.Base"], [])
        {
            BaseGameAssetId = 0L,
            QualityLevel = 0L,
            SlotConfig = new SingleItemSlot(
            [
                22040762L,
            ], true, 22040762L),
        },
        new(-904081899L, "InventoryStructure", 1L, false, true,
            ["Item.Flag.Unique", "Item.Flag.Persistent", "Online.Release.Base"], [])
        {
            BaseGameAssetId = 0L,
            QualityLevel = 0L,
            SlotConfig = new SingleItemSlot(
            [
                -213901314L,
            ], true, -213901314L),
        },
        new(-901902086L, "InventoryStructure", 1L, false, true,
            ["Item.Flag.Unique", "Item.Flag.Persistent", "Online.Release.Base"], [])
        {
            BaseGameAssetId = 0L,
            QualityLevel = 0L,
            SlotConfig = new SingleItemSlot(
            [
                -1475517378L,
            ], true, -1475517378L),
        },
        new(-875777364L, "InventoryStructure", 1L, false, false,
            ["Item.Flag.Persistent", "Online.Release.Base"], [])
        {
            BaseGameAssetId = 0L,
            QualityLevel = 0L,
            SlotConfig = new SingleItemSlot(
            [
                50992628L, -1489261627L, -468025815L, -1068447725L, 1369416424L, 1680690244L, -1156830089L, 433079423L,
                303567898L, -1328695953L, 947411243L, 966773769L, -1240487285L, 1101815928L, -1561038837L, -735920468L,
            ], false, 0L),
        },
        new(-636707363L, "InventoryStructure", 1L, false, true,
            ["Item.Flag.Unique", "Item.Flag.Persistent", "Online.Release.Base"], [])
        {
            BaseGameAssetId = 0L,
            QualityLevel = 0L,
            SlotConfig = new StructuralItemSlots([-1088427979L, -2142772724L, -2114213383L, -402660824L, 974676317L, 1413630316L, 260494636L]),
        },
        new(-560137962L, "InventoryStructure", 1L, false, true,
            ["Item.Flag.Unique", "Item.Flag.Persistent", "Online.Release.Base"], [])
        {
            BaseGameAssetId = 0L,
            QualityLevel = 0L,
            SlotConfig = new SingleItemSlot(
            [
                -1406187830L,
            ], true, -1406187830L),
        },
        new(-448345380L, "InventoryStructure", 1L, false, true,
            ["Item.Flag.Unique", "Item.Flag.Persistent", "Online.Release.Base"], [])
        {
            BaseGameAssetId = 0L,
            QualityLevel = 0L,
            SlotConfig = new StructuralItemSlots([-1351184816L, 1638597279L, 713048728L, 2135242484L, 411623810L, -206583984L, 591271901L]),
        },
        new(-402660824L, "InventoryStructure", 1L, false, true,
            ["Item.Flag.Unique", "Item.Flag.Persistent", "Online.Release.Base"], [])
        {
            BaseGameAssetId = 0L,
            QualityLevel = 0L,
            SlotConfig = new SingleItemSlot(
            [
                301923184L,
            ], true, 301923184L),
        },
        new(-213901314L, "InventoryStructure", 1L, false, true,
            ["Item.Flag.Unique", "Item.Flag.Persistent", "Online.Release.Base"], [])
        {
            BaseGameAssetId = 0L,
            QualityLevel = 0L,
            SlotConfig = new ItemSlotArray(-620731692L, 1L),
        },
        new(-206583984L, "InventoryStructure", 1L, false, true,
            ["Item.Flag.Unique", "Item.Flag.Persistent", "Online.Release.Base"], [])
        {
            BaseGameAssetId = 0L,
            QualityLevel = 0L,
            SlotConfig = new SingleItemSlot(
            [
                -1989158897L,
            ], true, -1989158897L),
        },
        new(-166008386L, "InventoryStructure", 1L, false, true,
            ["Item.Flag.Unique", "Item.Flag.Persistent", "Online.Release.Base"], [])
        {
            BaseGameAssetId = 0L,
            QualityLevel = 0L,
            SlotConfig = new ItemSlotArray(-1277506061L, 5L),
        },
        new(-35282746L, "InventoryStructure", 1L, false, true,
            ["Item.Flag.Unique", "Item.Flag.Persistent", "Online.Release.Base"], [])
        {
            BaseGameAssetId = 0L,
            QualityLevel = 0L,
            SlotConfig = new ItemSlotArray(719434862L, 1L),
        },
        new(22040762L, "InventoryStructure", 1L, false, true,
            ["Item.Flag.Unique", "Item.Flag.Persistent", "Online.Release.Base"], [])
        {
            BaseGameAssetId = 0L,
            QualityLevel = 0L,
            SlotConfig = new ItemSlotArray(1440007245L, 15L),
        },
        new(50992628L, "GameItem", 1L, false, false,
            ["Online.Release.Base"], [])
        {
            BaseGameAssetId = 50992628L,
            QualityLevel = 0L,
            SlotConfig = new NoItemSlots(),
        },
        new(56420823L, "InventoryStructure", 1L, false, true,
            ["Item.Flag.Unique", "Item.Flag.Persistent", "Online.Release.Base"], [])
        {
            BaseGameAssetId = 0L,
            QualityLevel = 0L,
            SlotConfig = new ItemSlotArray(-666604429L, 1L),
        },
        new(129937576L, "InventoryStructure", 1L, false, true,
            ["Item.Flag.Persistent", "Item.Flag.Unique", "Online.Release.Base"], [])
        {
            BaseGameAssetId = 0L,
            QualityLevel = 0L,
            SlotConfig = new SingleItemSlot(
            [
                -1101416870L,
            ], true, -1101416870L),
        },
        new(184018578L, "InventoryStructure", 1L, false, true,
            ["Item.Flag.Unique", "Item.Flag.Persistent", "Online.Release.Base"], [])
        {
            BaseGameAssetId = 0L,
            QualityLevel = 0L,
            SlotConfig = new ItemSlotArray(-1537698006L, 1L),
        },
        new(202906145L, "InventoryStructure", 1L, false, true,
            ["Item.Flag.Unique", "Item.Flag.Persistent", "Online.Release.Base"], [])
        {
            BaseGameAssetId = 0L,
            QualityLevel = 0L,
            SlotConfig = new ItemSlotArray(1440007245L, 18L),
        },
        new(260494636L, "InventoryStructure", 1L, false, true,
            ["Item.Flag.Unique", "Item.Flag.Persistent", "Online.Release.Base"], [])
        {
            BaseGameAssetId = 0L,
            QualityLevel = 0L,
            SlotConfig = new SingleItemSlot(
            [
                1442945311L,
            ], true, 1442945311L),
        },
        new(301923184L, "InventoryStructure", 1L, false, true,
            ["Item.Flag.Unique", "Item.Flag.Persistent", "Online.Release.Base"], [])
        {
            BaseGameAssetId = 0L,
            QualityLevel = 0L,
            SlotConfig = new ItemSlotArray(719434862L, 1L),
        },
        new(411623810L, "InventoryStructure", 1L, false, true,
            ["Item.Flag.Unique", "Item.Flag.Persistent", "Online.Release.Base"], [])
        {
            BaseGameAssetId = 0L,
            QualityLevel = 0L,
            SlotConfig = new SingleItemSlot(
            [
                1454995512L,
            ], true, 1454995512L),
        },
        new(485800788L, "InventoryStructure", 1L, false, true,
            ["Item.Flag.Unique", "Item.Flag.Persistent", "Online.Release.Base"], [])
        {
            BaseGameAssetId = 0L,
            QualityLevel = 0L,
            SlotConfig = new StructuralItemSlots([-901902086L, 563421940L, -904081899L, -1347093084L, -560137962L, 819401007L, -1444511024L]),
        },
        new(563421940L, "InventoryStructure", 1L, false, true,
            ["Item.Flag.Unique", "Item.Flag.Persistent", "Online.Release.Base"], [])
        {
            BaseGameAssetId = 0L,
            QualityLevel = 0L,
            SlotConfig = new SingleItemSlot(
            [
                2039479872L,
            ], true, 2039479872L),
        },
        new(591271901L, "InventoryStructure", 1L, false, true,
            ["Item.Flag.Unique", "Item.Flag.Persistent", "Online.Release.Base"], [])
        {
            BaseGameAssetId = 0L,
            QualityLevel = 0L,
            SlotConfig = new SingleItemSlot(
            [
                634916076L,
            ], true, 634916076L),
        },
        new(634916076L, "InventoryStructure", 1L, false, true,
            ["Item.Flag.Unique", "Item.Flag.Persistent", "Online.Release.Base"], [])
        {
            BaseGameAssetId = 0L,
            QualityLevel = 0L,
            SlotConfig = new ItemSlotArray(-3680536L, 1L),
        },
        new(713048728L, "InventoryStructure", 1L, false, true,
            ["Item.Flag.Unique", "Item.Flag.Persistent", "Online.Release.Base"], [])
        {
            BaseGameAssetId = 0L,
            QualityLevel = 0L,
            SlotConfig = new SingleItemSlot(
            [
                1833604446L,
            ], true, 1833604446L),
        },
        new(719434862L, "InventoryStructure", 1L, false, false,
            ["Item.Flag.Persistent", "Online.Release.Base"], [])
        {
            BaseGameAssetId = 0L,
            QualityLevel = 0L,
            SlotConfig = new SingleItemSlot(
            [
                -2097404337L, 9197506L,
            ], false, 0L),
        },
        new(819401007L, "InventoryStructure", 1L, false, true,
            ["Item.Flag.Unique", "Item.Flag.Persistent", "Online.Release.Base"], [])
        {
            BaseGameAssetId = 0L,
            QualityLevel = 0L,
            SlotConfig = new SingleItemSlot(
            [
                -1719547500L,
            ], true, -1719547500L),
        },
        new(966773769L, "GameItem", 1L, false, false,
            ["Online.Release.Base"], [])
        {
            BaseGameAssetId = 966773769L,
            QualityLevel = 0L,
            SlotConfig = new NoItemSlots(),
        },
        new(974676317L, "InventoryStructure", 1L, false, true,
            ["Item.Flag.Unique", "Item.Flag.Persistent", "Online.Release.Base"], [])
        {
            BaseGameAssetId = 0L,
            QualityLevel = 0L,
            SlotConfig = new SingleItemSlot(
            [
                -166008386L,
            ], true, -166008386L),
        },
        new(1360021217L, "InventoryStructure", 1L, false, true,
            ["Item.Flag.Unique", "Item.Flag.Persistent", "Online.Release.Base"], [])
        {
            BaseGameAssetId = 0L,
            QualityLevel = 0L,
            SlotConfig = new ItemSlotArray(-620731692L, 1L),
        },
        new(1413630316L, "InventoryStructure", 1L, false, true,
            ["Item.Flag.Unique", "Item.Flag.Persistent", "Online.Release.Base"], [])
        {
            BaseGameAssetId = 0L,
            QualityLevel = 0L,
            SlotConfig = new SingleItemSlot(
            [
                56420823L,
            ], true, 56420823L),
        },
        new(1442945311L, "InventoryStructure", 1L, false, true,
            ["Item.Flag.Unique", "Item.Flag.Persistent", "Online.Release.Base"], [])
        {
            BaseGameAssetId = 0L,
            QualityLevel = 0L,
            SlotConfig = new ItemSlotArray(-3680536L, 1L),
        },
        new(1454995512L, "InventoryStructure", 1L, false, true,
            ["Item.Flag.Unique", "Item.Flag.Persistent", "Online.Release.Base"], [])
        {
            BaseGameAssetId = 0L,
            QualityLevel = 0L,
            SlotConfig = new ItemSlotArray(-1277506061L, 4L),
        },
        new(1638597279L, "InventoryStructure", 1L, false, true,
            ["Item.Flag.Unique", "Item.Flag.Persistent", "Online.Release.Base"], [])
        {
            BaseGameAssetId = 0L,
            QualityLevel = 0L,
            SlotConfig = new SingleItemSlot(
            [
                -1449979565L,
            ], true, -1449979565L),
        },
        new(1680690244L, "GameItem", 1L, false, false,
            ["Online.Release.Base"], [])
        {
            BaseGameAssetId = 1680690244L,
            QualityLevel = 0L,
            SlotConfig = new NoItemSlots(),
        },
        new(1833604446L, "InventoryStructure", 1L, false, true,
            ["Item.Flag.Unique", "Item.Flag.Persistent", "Online.Release.Base"], [])
        {
            BaseGameAssetId = 0L,
            QualityLevel = 0L,
            SlotConfig = new ItemSlotArray(-620731692L, 1L),
        },
        new(2039479872L, "InventoryStructure", 1L, false, true,
            ["Item.Flag.Unique", "Item.Flag.Persistent", "Online.Release.Base"], [])
        {
            BaseGameAssetId = 0L,
            QualityLevel = 0L,
            SlotConfig = new ItemSlotArray(-620731692L, 1L),
        },
        new(2135242484L, "InventoryStructure", 1L, false, true,
            ["Item.Flag.Unique", "Item.Flag.Persistent", "Online.Release.Base"], [])
        {
            BaseGameAssetId = 0L,
            QualityLevel = 0L,
            SlotConfig = new SingleItemSlot(
            [
                184018578L,
            ], true, 184018578L),
        },
    ];

    /// <summary>Carrying limit without an equipped augment.</summary>
    public const double DefaultMaxWeight = 30.0;

    /// <summary>Supported augment technical names.</summary>
    public static IReadOnlyDictionary<string, long> ItemAssetIds { get; } = new Dictionary<string, long>
    {
        ["DA_Item_Augment_Combat_T1"] = 50992628L,
        ["DA_Item_Augment_Looting_T1"] = 1680690244L,
        ["DA_Item_Augment_Utility_T1"] = 966773769L,
    };

    /// <summary>Player frame selected by each augment.</summary>
    public static IReadOnlyDictionary<long, long> FrameAssetIds { get; } = new Dictionary<long, long>
    {
        [50992628L] = 485800788L,
        [1680690244L] = -448345380L,
        [966773769L] = -636707363L,
    };

    /// <summary>Standard equipment branch corresponding to each augment frame branch.</summary>
    public static IReadOnlyDictionary<long, long> BranchAssetIds { get; } = new Dictionary<long, long>
    {
        [-901902086L] = -1935122902L,
        [563421940L] = -891583501L,
        [-904081899L] = 789222533L,
        [-1347093084L] = 1333996162L,
        [-560137962L] = 1687384280L,
        [819401007L] = -588056456L,
        [-1444511024L] = -451662226L,
        [-1351184816L] = -1935122902L,
        [1638597279L] = -891583501L,
        [713048728L] = 789222533L,
        [2135242484L] = 1333996162L,
        [411623810L] = 1687384280L,
        [-206583984L] = -588056456L,
        [591271901L] = -451662226L,
        [-1088427979L] = -1935122902L,
        [-2142772724L] = -891583501L,
        [-2114213383L] = 789222533L,
        [-402660824L] = 1333996162L,
        [974676317L] = 1687384280L,
        [1413630316L] = -588056456L,
        [260494636L] = -451662226L,
    };

    /// <summary>Usable-item indices of supported augments.</summary>
    public static IReadOnlyDictionary<long, int> UsableIndices { get; } = new Dictionary<long, int>
    {
        [50992628L] = 28,
        [1680690244L] = 33,
        [966773769L] = 39,
    };

    /// <summary>Carrying limit selected by each augment.</summary>
    public static IReadOnlyDictionary<long, double> MaxWeights { get; } = new Dictionary<long, double>
    {
        [50992628L] = 45.0,
        [1680690244L] = 50.0,
        [966773769L] = 40.0,
    };

    /// <summary>Unit weight of supported augments when carried as items.</summary>
    public static IReadOnlyDictionary<long, double> ItemWeights { get; } = new Dictionary<long, double>
    {
        [50992628L] = 2.0,
        [1680690244L] = 1.0,
        [966773769L] = 2.0,
    };
}
