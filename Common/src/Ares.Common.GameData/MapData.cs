using System.Collections.Immutable;
using Ares.Structs.Game.World;

namespace Ares.Common.GameData;

/// <summary>
/// One map's data, named like its tables (<see cref="MapTables"/>). Generated: the <c>Dam…</c> classes; for example
/// <c>DamLadders.All</c> is <see cref="Ladders"/> and <c>DamLadders.Logic</c> is <see cref="LadderLogic"/>.
/// </summary>
public sealed class MapData
{
    internal MapData(MapContent content)
    {
        World = content.World;
        string levelScriptClass = World.LevelScriptClass;
        int dot = levelScriptClass.IndexOf('.', StringComparison.Ordinal);
        MapPackage = dot > 0 ? levelScriptClass[..dot]
            : throw Malformed($"its level script class '{levelScriptClass}' is not an object path");
        GameMode = content.GameMode;
        StreamingCells = content.StreamingCells;
        LootSocketMeshes = content.LootSocketMeshes;
        LootSockets = content.LootSockets;
        if (LootSockets.FirstOrDefault(socket => (uint)socket.Mesh >= (uint)LootSocketMeshes.Length) is { } socketWithoutMesh)
        {
            throw Malformed($"loot socket {socketWithoutMesh.Actor}.{socketWithoutMesh.Socket} names mesh {socketWithoutMesh.Mesh} of {LootSocketMeshes.Length}");
        }

        PlacedLoot = content.PlacedLoot;
        PlayerStarts = content.PlayerStarts;
        PlayerStartCapsule = content.PlayerStartCapsule;
        Ladders = content.Ladders;
        LadderLogic = content.LadderLogic;
        if (Ladders.Length > 0 && LadderLogic is null)
        {
            throw Malformed("it places ladders but has no ladder logic class");
        }

        ZiplineLines = content.ZiplineLines;
        ZiplineLaunchers = content.ZiplineLaunchers;
        ZiplineDefaults = content.ZiplineDefaults;
        PointsOfInterest = content.PointsOfInterest;
        LootLimiters = content.LootLimiters;
        DynamicLootSpawners = content.DynamicLootSpawners;
        if (content.DynamicLootGroups.FirstOrDefault(group => group.Spawners.Any(index => (uint)index >= (uint)DynamicLootSpawners.Length)) is { } groupWithoutSpawner)
        {
            throw Malformed($"dynamic loot group {groupWithoutSpawner.Id} names a spawner beyond the {DynamicLootSpawners.Length} spawners");
        }

        DynamicLootGroups = [.. content.DynamicLootGroups.Select(group => group.Resolve(DynamicLootSpawners))];
        Doors = content.Doors;
        Rounds = content.Rounds;
        LightingScenarios = content.LightingScenarios;
        RoundLightingGroups = GameDataIndex.Unique(content.RoundLightingGroups.Select(group => (group.Tag, group.Choices)), $"round lighting group of {WorldName}",
            StringComparer.Ordinal);
        ExtractionElevator = content.ExtractionElevator;
        ExtractionButtons = content.ExtractionButtons;
        ExtractionPoints = content.ExtractionPoints;
    }

    /// <summary>The map's world name, for example <c>TheDam_02_P</c> (generated: <c>DamMap.WorldName</c>).</summary>
    public string WorldName => World.WorldName;

    /// <summary>
    /// The map's package, the package of its level script class (generated: <c>DamMap.MapPackage</c>), for example
    /// <c>/Game/Pioneer/Maps/TheDam_02/TheDam_02_P</c>.
    /// </summary>
    public string MapPackage { get; }

    /// <summary>
    /// The map's persistent world objects (generated: <c>DamMap.PersistentLevelName</c>, <c>LevelScriptActorName</c>,
    /// <c>LevelScriptClass</c>, <c>WorldDataLayersActorName</c>).
    /// </summary>
    public MapWorldObjects World { get; }

    /// <summary>The map's game mode, game state and location tag (generated: <c>DamMap.GameModeClass</c>, <c>GameState</c>, <c>LocationTag</c>).</summary>
    public MapGameMode GameMode { get; }

    /// <summary>The generated World Partition cell packages, in ordinal order (generated: <c>DamStreamingCells.Packages</c>).</summary>
    public ImmutableArray<string> StreamingCells { get; }

    /// <summary>The static meshes that carry loot sockets; <see cref="LootSocket.Mesh"/> indexes them (generated: <c>DamLootSockets.Meshes</c>).</summary>
    public ImmutableArray<LootSocketMesh> LootSocketMeshes { get; }

    /// <summary>The loot container sockets (generated: <c>DamLootSockets.All</c>).</summary>
    public ImmutableArray<LootSocket> LootSockets { get; }

    /// <summary>The loot container actors the map places itself (generated: <c>DamPlacedLoot.Containers</c>).</summary>
    public ImmutableArray<PlacedLootContainer> PlacedLoot { get; }

    /// <summary>The authored player starts of the persistent level (generated: <c>DamPlayerStarts.All</c>).</summary>
    public ImmutableArray<PlayerStart> PlayerStarts { get; }

    /// <summary>The capsule of generated group spawn points (generated: <c>DamPlayerStarts.Capsule</c>).</summary>
    public PlayerStartCapsule PlayerStartCapsule { get; }

    /// <summary>The climbable ladders (generated: <c>DamLadders.All</c>).</summary>
    public ImmutableArray<LadderPlacement> Ladders { get; }

    /// <summary>The class spawned for each ladder to make it climbable, null without ladders (generated: <c>DamLadders.Logic</c>).</summary>
    public ArchetypeClass? LadderLogic { get; }

    /// <summary>The rideable ziplines (generated: <c>DamZiplines.Lines</c>).</summary>
    public ImmutableArray<ZiplinePlacement> ZiplineLines { get; }

    /// <summary>The zipline launchers (generated: <c>DamZiplines.Launchers</c>).</summary>
    public ImmutableArray<ActivatableZiplinePlacement> ZiplineLaunchers { get; }

    /// <summary>
    /// What every rideable zipline shares (generated: <c>DamZiplines.ForcedTravelDirectionThresholdHome</c>,
    /// <c>AttachmentOffset</c>, <c>CharacterMeshLocation</c>, <c>CharacterMeshYaw</c>).
    /// </summary>
    public ZiplineLineDefaults ZiplineDefaults { get; }

    /// <summary>The points of interest (generated: <c>DamPointsOfInterest.All</c>).</summary>
    public ImmutableArray<PointOfInterest> PointsOfInterest { get; }

    /// <summary>The loot placement limiters (generated: <c>DamLootLimiters.All</c>).</summary>
    public ImmutableArray<LootLimiter> LootLimiters { get; }

    /// <summary>The spawners of free-standing loot containers (generated: <c>DamDynamicLoot.Spawners</c>).</summary>
    public ImmutableArray<DynamicLootSpawner> DynamicLootSpawners { get; }

    /// <summary>The groups that pick which dynamic loot spawners run, with their spawners resolved (generated: <c>DamDynamicLoot.Groups</c>).</summary>
    public ImmutableArray<DynamicLootGroup> DynamicLootGroups { get; }

    /// <summary>The regular doors (generated: <c>DamDoors.All</c>).</summary>
    public ImmutableArray<DoorPlacement> Doors { get; }

    /// <summary>The round configurations (generated: <c>DamRounds.All</c>).</summary>
    public ImmutableArray<RoundConfiguration> Rounds { get; }

    /// <summary>The scenarios of the lighting lookup (generated: <c>DamLighting.Scenarios</c>).</summary>
    public ImmutableArray<LightingScenario> LightingScenarios { get; }

    /// <summary>The lighting choices of each round lighting group, in asset order (generated: <c>DamLighting.RoundLightingGroups</c>).</summary>
    public IReadOnlyDictionary<string, IReadOnlyList<WeightedLighting>> RoundLightingGroups { get; }

    /// <summary>
    /// What the elevator extraction points share (generated: <c>DamExtraction.ExtractionType</c>, <c>SettingsAsset</c>,
    /// <c>Settings</c>, <c>ZoneHalfExtent</c>, <c>ZoneLocation</c>, <c>ZoneRotation</c>, <c>NoHoldStartedTime</c>,
    /// <c>NoExtractionStartedTime</c>, <c>DoorsSeconds</c>).
    /// </summary>
    public ExtractionElevator ExtractionElevator { get; }

    /// <summary>The buttons of each elevator, in <see cref="ExtractionClasses.Buttons"/> order (generated: <c>DamExtraction.Buttons</c>).</summary>
    public ImmutableArray<ExtractionButton> ExtractionButtons { get; }

    /// <summary>The elevators the persistent level places (generated: <c>DamExtraction.All</c>).</summary>
    public ImmutableArray<ExtractionPointPlacement> ExtractionPoints { get; }

    private GameDataException Malformed(string problem) =>
        new($"The game data of map {World.WorldName} is inconsistent: {problem}. Run the ARES-GameDataExtract of this ARES version again.");
}
