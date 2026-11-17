using System.Collections.Immutable;
using Ares.Common.GameData;
using Ares.Structs.Backend.Progression;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Game;
using Ares.Structs.Game.Inventory;
using Ares.Structs.Game.Loot;
using Ares.Structs.Game.Movement;
using Ares.Structs.Game.World;

namespace Ares.Common.Testing;

/// <summary>
/// Builds tiny made-up game data for logic tests, never copied from extracted files. The default holds every name of
/// <see cref="GameNames"/> (so that the data resolves) and one map, <see cref="GameNames.Maps.Dam"/>, with one door,
/// ladder, loot socket and placed loot container and one player start. Values are invented: a test
/// that depends on one sets it itself, through <see cref="AddItem"/>, <see cref="WithMap"/> or <see cref="With"/>.
/// </summary>
public sealed class SyntheticGameData
{
    /// <summary>Asset id of the first made-up item; the others follow.</summary>
    public const long FirstAssetId = 1_000_001;

    /// <summary>Container type of the made-up loot socket and its loot container class.</summary>
    public const string ContainerTag = "Socket.LootContainer.Synthetic";

    /// <summary>Class path of the made-up loot container class.</summary>
    public const string ContainerClass = "/Game/Synthetic/BP_Container.BP_Container_C";

    private const string Cell = "/Game/Synthetic/Cells/Cell_0";

    private static readonly string[] s_items =
    [
        GameNames.Items.Unarmed, GameNames.Items.RaiderTool, GameNames.Items.FerroI, GameNames.Items.HeavyAmmo, GameNames.Items.SnapHook,
        GameNames.Items.LightShield, GameNames.Items.Coins, GameNames.Items.CombatMk1, GameNames.Items.LootingMk1, GameNames.Items.TacticalMk1,
    ];

    private readonly List<(string Name, Func<ItemDefinition, ItemDefinition> Configure)> _items = [.. s_items.Select(name => (name, Same<ItemDefinition>()))];
    private Func<MapContent, MapContent> _map = Same<MapContent>();
    private Func<GameDataContent, GameDataContent> _content = Same<GameDataContent>();

    /// <summary>The default made-up data.</summary>
    public static GameDataSet Create() => new SyntheticGameData().Build();

    /// <summary>
    /// Adds an item, or changes one of <see cref="GameNames.Items"/>. Usable indices follow the technical names' order
    /// ignoring case, as in the game; asset ids count from <see cref="FirstAssetId"/> in the order items were added.
    /// </summary>
    /// <param name="technicalName">The item's technical name.</param>
    /// <param name="configure">Changes the made-up item (a misc item of one, weighing 1); its usable index is set afterwards.</param>
    public SyntheticGameData AddItem(string technicalName, Func<ItemDefinition, ItemDefinition>? configure = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(technicalName);
        int existing = _items.FindIndex(item => item.Name == technicalName);
        (string, Func<ItemDefinition, ItemDefinition>) entry = (technicalName, configure ?? Same<ItemDefinition>());
        if (existing >= 0)
        {
            _items[existing] = entry;
        }
        else
        {
            _items.Add(entry);
        }

        return this;
    }

    /// <summary>Changes the map.</summary>
    public SyntheticGameData WithMap(Func<MapContent, MapContent> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        Func<MapContent, MapContent> previous = _map;
        _map = map => configure(previous(map));
        return this;
    }

    /// <summary>Changes the tables, last: anything the other methods do not cover.</summary>
    public SyntheticGameData With(Func<GameDataContent, GameDataContent> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        Func<GameDataContent, GameDataContent> previous = _content;
        _content = content => configure(previous(content));
        return this;
    }

    /// <summary>The tables.</summary>
    public GameDataContent BuildContent()
    {
        ItemDefinition[] made = [.. _items.Select((entry, index) => entry.Configure(Item(entry.Name, FirstAssetId + index)))];
        ImmutableArray<ItemDefinition> items = [.. made.OrderBy(item => item.TechnicalName, StringComparer.OrdinalIgnoreCase).Select((item, index) => item with { UsableIndex = index })];
        Dictionary<string, ItemDefinition> byName = items.ToDictionary(item => item.TechnicalName, StringComparer.Ordinal);

        LoadoutFrameDefinition standard = Frame(GameNames.ItemStructures.StandardLoadoutFrame, 2_000_001);
        AugmentDefinition[] augments =
        [
            .. new[] { GameNames.Items.CombatMk1, GameNames.Items.LootingMk1, GameNames.Items.TacticalMk1 }
                .Select((name, index) => new AugmentDefinition(name, Named(name).AssetId, 2_000_002 + index, 40.0 + (5 * index))),
        ];
        LoadoutFrameDefinition[] frames = [standard, .. augments.Select(augment => Frame(augment.TechnicalName + "_Frame", augment.FrameAssetId))];
        GameDataContent content = new()
        {
            UsableItems = items,
            ItemValues = [.. items.Select(item => new ItemValue(item.AssetId, item.TechnicalName, 10, -1))],
            WorldItemList = [.. items.Select(item => item.ItemClass)],
            Augments = [.. augments.OrderBy(augment => Named(augment.TechnicalName).UsableIndex)],
            LoadoutFrames = [.. frames.OrderBy(frame => frame.TechnicalName, StringComparer.Ordinal)],
            Firearms = [new FirearmDefinition(Named(GameNames.Items.FerroI).ItemClass, 1, 50, 2.5, [0.5], 1.0, 1.5, [3_000_001, 3_000_002, 3_000_003])],
            Shields = [new ShieldDefinition(Named(GameNames.Items.LightShield).ItemClass, 1, 40, 0.4, 40, 0)],
            MenuAugments =
            [
                .. augments.Select(augment => Menu(Named(augment.TechnicalName))),
                Structure(GameNames.ItemStructures.AugmentSlot, 3_000_004),
                Structure(GameNames.ItemStructures.AugmentContainer, 3_000_005),
                Structure(GameNames.ItemStructures.AugmentContentSlot, 3_000_006),
            ],
            MenuCurrency = [Menu(Named(GameNames.Items.Coins))],
            MenuGadgets = [Menu(Named(GameNames.Items.SnapHook))],
            MenuShields = [Menu(Named(GameNames.Items.LightShield))],
            MenuWeapons =
            [
                Menu(Named(GameNames.Items.FerroI)), Menu(Named(GameNames.Items.HeavyAmmo)),
                Structure(GameNames.ItemStructures.MuzzleModSlot, 3_000_001),
                Structure(GameNames.ItemStructures.UnderBarrelModSlot, 3_000_002),
                Structure(GameNames.ItemStructures.StockModSlot, 3_000_003),
            ],
            DodgeRolls = [.. Enum.GetValues<DodgeRollKind>().Select(kind => new DodgeRollSequence(300 + (int)kind, 600, 15, 0.1, 30, [0f, 1f], []))],
            VaultDefinitions = [.. MovementAssets.VaultPackages.Select(_ => new VaultDefinitionSet(1.0, [], [], []))],
            HeavyLandingLevels = [],
            RecoveryLandingLevels = [],
            GraphStateTags = GraphStateTags(),
            SnapHookItem = new SnapHookItemDefaults("/Game/Synthetic/DA_Item_SnapHook", 1800, 60),
            SprintBurstCurve = [new WeightedCurveKey(0f, 0f, 0f, 0f, 0f, 0f), new WeightedCurveKey(0.5f, 1f, 0f, 0f, 0f, 0f)],
            LootContainerTypes = [new LootContainerType(ContainerTag, [new ArchetypeClass(ContainerClass, "/Game/Synthetic/BP_Container.Default__BP_Container_C")], [], 2)],
            LootContainerClasses =
            [
                new LootContainerClass(new ArchetypeClass(ContainerClass, "/Game/Synthetic/BP_Container.Default__BP_Container_C"), "/Script/Angelscript.LootContainer",
                    [], new Vector3D(0, 0, 50), new Vector3D(0, 0, 50), null, 0.5, 1.0, false, 200, 150, 8),
            ],
            LootContainerDefaults = new LootContainerDefaults(20000, [new LootTargetStacks(2, 1.0)]),
            StaticLootSpawnTweakables = new LootSpawnTweakables(0.5, 1.0, 0.5, 0.4, 0.025, 0.022, new Dictionary<int, double> { [1] = 1.0 }),
            InteractionAnimationContainers =
            [
                new InteractionAnimationContainer(GameNames.InteractionAnimations.DoorBreach, InteractionAlignment.LookAtMatrixComponentUp, true, false, false, 50, false, false),
            ],
            TaggedInteractionAnimationContainers = [new TaggedInteractionAnimationContainer([], GameNames.InteractionAnimations.DoorBreach)],
            Maps = [_map(DefaultMap())],
        };
        return _content(content);

        ItemDefinition Named(string name) => byName[name];
    }

    /// <summary>The checked and indexed data.</summary>
    public GameDataSet Build() => GameDataSet.Create(BuildContent());

    private static Func<T, T> Same<T>() => value => value;

    // The graph's tag list: every name of GameNames.GraphTags, those the upper tag word holds after made-up tags that fill
    // the graph tag word (GraphStateFlags groups each tag in its word).
    private static ImmutableArray<string> GraphStateTags()
    {
        string[] upper =
        [
            GameNames.GraphTags.AbilityBlockEquipItemInteract, GameNames.GraphTags.AbilityBlockFirePrimaryInteract, GameNames.GraphTags.AbilityBlockFireSecondaryInteract,
            GameNames.GraphTags.AbilityBlockJumpInteract, GameNames.GraphTags.AbilityBlockReloadInteract, GameNames.GraphTags.StatusImmobilizedInteract,
            GameNames.GraphTags.UiSkillStateActiveSprintBoost, GameNames.GraphTags.StatusExhausted,
        ];
        string[] lower =
        [
            .. typeof(GameNames.GraphTags).GetFields().Select(field => (string)field.GetRawConstantValue()!).Except(upper).Order(StringComparer.Ordinal),
        ];
        return [.. lower, .. Enumerable.Range(0, MovementAssets.GraphTagWordBits - lower.Length).Select(index => $"Synthetic.Tag{index}"), .. upper];
    }

    private static ItemDefinition Item(string name, long assetId) => new(0, name, $"/Game/Synthetic/{name}", assetId, assetId, [], ItemCategory.Misc, ItemRarity.Common, 1,
        1.0, false, false, false, [], [], [], [], [], $"/Game/Synthetic/BP_{name}.BP_{name}_C", 0.2, 0.2, 0.2, 0.2);

    private static LoadoutFrameDefinition Frame(string name, long assetId) => new(name, assetId, [new LoadoutFrameContainer(0, assetId * 10), new LoadoutFrameContainer(1, (assetId * 10) + 1)]);

    private static MenuItem Menu(ItemDefinition item) => new(item.TechnicalName, new ItemDescription(item.AssetId, item.ItemType, item.MaxStack, item.Stackable, item.Unique, [], []));

    private static MenuItem Structure(string name, long assetId) => new(name, new ItemDescription(assetId, "InventoryStructure", 1, false, true, [], []));

    private static MapContent DefaultMap()
    {
        const string Map = GameNames.Maps.Dam;
        return new MapContent
        {
            World = new MapWorldObjects(Map, "PersistentLevel", $"{Map}_C_1", $"/Game/Synthetic/Maps/{Map}.{Map}_C", "WorldDataLayers"),
            GameMode = new MapGameMode("/Game/Synthetic/BP_GameMode.BP_GameMode_C",
                new ArchetypeClass("/Script/Angelscript.SyntheticGameState", "/Script/Angelscript.Default__SyntheticGameState"), "GameMode.Location.Synthetic"),
            StreamingCells = [Cell],
            LootSocketMeshes = [new LootSocketMesh("/Game/Synthetic/SM_Shelf.SM_Shelf", [null])],
            LootSockets = [new LootSocket(Cell, "Shelf_1", "StaticMeshComponent0", "LootSocket_1", ContainerTag, new Vector3D(100, 0, 0), new Rotator(0, 0, 0), Vector3D.One, 0, [], null)],
            PlacedLoot = [new PlacedLootContainer(Cell, "Container_1", ContainerClass, new Vector3D(200, 0, 0), new Rotator(0, 90, 0), Vector3D.One, null)],
            PlayerStarts = [new PlayerStart("PlayerStart_1", "PlayerStart", [], "CollisionCapsule", new Vector3D(0, 0, 100), new Rotator(0, 0, 0), [])],
            PlayerStartCapsule = new PlayerStartCapsule(92, 40),
            Ladders = [new LadderPlacement(Cell, "Ladder_1", 400, new Vector3D(300, 0, 0), 180)],
            LadderLogic = new ArchetypeClass("/Game/Synthetic/BP_Ladder_Logic.BP_Ladder_Logic_C", "/Game/Synthetic/BP_Ladder_Logic.Default__BP_Ladder_Logic_C"),
            ZiplineDefaults = new ZiplineLineDefaults(500, new Transform(Quaternion.Identity, new Vector3D(0, 0, 225), Vector3D.One), new Vector3D(0, 0, -95), 270),
            Doors = [new DoorPlacement(Cell, "Door_1")],
            ExtractionElevator = new ExtractionElevator("GameMode.Salvage.ExtractionPoint.Elevator", "/Game/Synthetic/DA_ExtractionPointSettings",
                new ExtractionSettings(10, 20, 30, 2, 5), new Vector3D(500, 400, 160), new Vector3D(0, 0, 60), new Rotator(0, 0, 0), -1, -1, 5),
        };
    }
}
