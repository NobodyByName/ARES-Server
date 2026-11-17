using System.Collections.Immutable;
using System.Text.Json.Serialization.Metadata;
using Ares.Structs.Game.World;

namespace Ares.Common.GameData;

/// <summary>Reads every table of a game data directory: the global tables and the tables of each map its manifest lists.</summary>
internal static class GameDataReader
{
    private const string WorldFile = "world.json";

    private static readonly GameDataJsonContext s_json = GameDataJsonContext.Default;

    /// <summary>Reads the tables.</summary>
    /// <exception cref="GameDataException">A table is missing, changed since extraction, malformed, or a one-row table does not have one row.</exception>
    public static GameDataContent Read(GameDataDirectory directory) => new()
    {
        UsableItems = directory.ReadTable(GameTables.UsableItems, s_json.ItemDefinition),
        ItemValues = directory.ReadTable(GameTables.ItemValues, s_json.ItemValue),
        WorldItemList = directory.ReadTable(GameTables.WorldItemList, s_json.String),
        Augments = directory.ReadTable(GameTables.Augments, s_json.AugmentDefinition),
        LoadoutFrames = directory.ReadTable(GameTables.LoadoutFrames, s_json.LoadoutFrameDefinition),
        Firearms = directory.ReadTable(GameTables.Firearms, s_json.FirearmDefinition),
        Shields = directory.ReadTable(GameTables.Shields, s_json.ShieldDefinition),
        MenuAugments = directory.ReadTable(GameTables.MenuAugments, s_json.MenuItem),
        MenuCurrency = directory.ReadTable(GameTables.MenuCurrency, s_json.MenuItem),
        MenuGadgets = directory.ReadTable(GameTables.MenuGadgets, s_json.MenuItem),
        MenuSalvage = directory.ReadTable(GameTables.MenuSalvage, s_json.MenuItem),
        MenuShields = directory.ReadTable(GameTables.MenuShields, s_json.MenuItem),
        MenuWeapons = directory.ReadTable(GameTables.MenuWeapons, s_json.MenuItem),
        DodgeRolls = directory.ReadTable(GameTables.DodgeRolls, s_json.DodgeRollSequence),
        VaultDefinitions = directory.ReadTable(GameTables.VaultDefinitions, s_json.VaultDefinitionSet),
        HeavyLandingLevels = directory.ReadTable(GameTables.HeavyLandingLevels, s_json.HeavyLandingLevel),
        RecoveryLandingLevels = directory.ReadTable(GameTables.RecoveryLandingLevels, s_json.HeavyLandingLevel),
        GraphStateTags = directory.ReadTable(GameTables.GraphStateTags, s_json.String),
        SnapHookItem = Single(directory, GameTables.SnapHookItem, s_json.SnapHookItemDefaults),
        SprintBurstCurve = directory.ReadTable(GameTables.SprintBurstCurve, s_json.WeightedCurveKey),
        LootContainerTypes = directory.ReadTable(GameTables.LootContainerTypes, s_json.LootContainerType),
        LootContainerClasses = directory.ReadTable(GameTables.LootContainerClasses, s_json.LootContainerClass),
        LootContainerDefaults = Single(directory, GameTables.LootContainerDefaults, s_json.LootContainerDefaults),
        StaticValueLootTables = directory.ReadTable(GameTables.StaticValueLootTables, s_json.StaticValueLootTable),
        SkillLootTables = directory.ReadTable(GameTables.SkillLootTables, s_json.SkillLootTable),
        StaticLootSpawnTweakables = Single(directory, GameTables.StaticLootSpawnTweakables, s_json.LootSpawnTweakables),
        InteractionAnimationContainers = directory.ReadTable(GameTables.InteractionAnimationContainers, s_json.InteractionAnimationContainer),
        TaggedInteractionAnimationContainers = directory.ReadTable(GameTables.TaggedInteractionAnimationContainers, s_json.TaggedInteractionAnimationContainer),
        Maps = [.. MapNames(directory.Manifest).Select(name => ReadMap(directory, name))],
    };

    /// <summary>World names of the maps the manifest has a world table for, ordered by name (ordinal).</summary>
    private static IEnumerable<string> MapNames(GameDataManifest manifest) => manifest.Files
        .Select(file => file.Name.Split('/'))
        .Where(segments => segments is [GameDataFiles.MapsDirectory, _, WorldFile])
        .Select(segments => segments[1])
        .Order(StringComparer.Ordinal);

    private static MapContent ReadMap(GameDataDirectory directory, string map)
    {
        ImmutableArray<ArchetypeClass> ladderLogic = directory.ReadTable(MapTables.LadderLogic(map), s_json.ArchetypeClass);
        return new MapContent
        {
            World = Single(directory, MapTables.World(map), s_json.MapWorldObjects),
            GameMode = Single(directory, MapTables.GameMode(map), s_json.MapGameMode),
            StreamingCells = directory.ReadTable(MapTables.StreamingCells(map), s_json.String),
            LootSocketMeshes = directory.ReadTable(MapTables.LootSocketMeshes(map), s_json.LootSocketMesh),
            LootSockets = directory.ReadTable(MapTables.LootSockets(map), s_json.LootSocket),
            PlacedLoot = directory.ReadTable(MapTables.PlacedLoot(map), s_json.PlacedLootContainer),
            PlayerStarts = directory.ReadTable(MapTables.PlayerStarts(map), s_json.PlayerStart),
            PlayerStartCapsule = Single(directory, MapTables.PlayerStartCapsule(map), s_json.PlayerStartCapsule),
            Ladders = directory.ReadTable(MapTables.Ladders(map), s_json.LadderPlacement),
            LadderLogic = ladderLogic.Length switch
            {
                0 => null,
                1 => ladderLogic[0],
                _ => throw OneRowExpected(directory, MapTables.LadderLogic(map), ladderLogic.Length, "at most one row"),
            },
            ZiplineLines = directory.ReadTable(MapTables.ZiplineLines(map), s_json.ZiplinePlacement),
            ZiplineLaunchers = directory.ReadTable(MapTables.ZiplineLaunchers(map), s_json.ActivatableZiplinePlacement),
            ZiplineDefaults = Single(directory, MapTables.ZiplineDefaults(map), s_json.ZiplineLineDefaults),
            PointsOfInterest = directory.ReadTable(MapTables.PointsOfInterest(map), s_json.PointOfInterest),
            LootLimiters = directory.ReadTable(MapTables.LootLimiters(map), s_json.LootLimiter),
            DynamicLootSpawners = directory.ReadTable(MapTables.DynamicLootSpawners(map), s_json.DynamicLootSpawner),
            DynamicLootGroups = directory.ReadTable(MapTables.DynamicLootGroups(map), s_json.DynamicLootGroupEntry),
            Doors = directory.ReadTable(MapTables.Doors(map), s_json.DoorPlacement),
            Rounds = directory.ReadTable(MapTables.Rounds(map), s_json.RoundConfiguration),
            LightingScenarios = directory.ReadTable(MapTables.LightingScenarios(map), s_json.LightingScenario),
            RoundLightingGroups = directory.ReadTable(MapTables.RoundLightingGroups(map), s_json.RoundLightingGroup),
            ExtractionElevator = Single(directory, MapTables.ExtractionElevator(map), s_json.ExtractionElevator),
            ExtractionButtons = directory.ReadTable(MapTables.ExtractionButtons(map), s_json.ExtractionButton),
            ExtractionPoints = directory.ReadTable(MapTables.ExtractionPoints(map), s_json.ExtractionPointPlacement),
        };
    }

    private static TRow Single<TRow>(GameDataDirectory directory, string name, JsonTypeInfo<TRow> rowType)
    {
        ImmutableArray<TRow> rows = directory.ReadTable(name, rowType);
        return rows.Length == 1 ? rows[0] : throw OneRowExpected(directory, name, rows.Length, "one row");
    }

    private static GameDataException OneRowExpected(GameDataDirectory directory, string name, int rows, string expected) =>
        new($"Game data table {directory.PathOf(name)} holds {rows} rows, but ARES expects {expected}. Run the ARES-GameDataExtract of this ARES version again.");
}
