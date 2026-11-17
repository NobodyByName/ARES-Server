using System.Collections.Immutable;
using Ares.Structs.Game.World;

namespace Ares.Common.GameData;

/// <summary>
/// The rows of one map's tables (<see cref="MapTables"/>), as read from <c>maps/&lt;map&gt;/</c> or written by hand for
/// a test. <see cref="GameDataSet.Create"/> checks and indexes them into a <see cref="MapData"/>.
/// </summary>
/// <remarks>Each member is named after its table and holds its rows in the table's order.</remarks>
public sealed record MapContent
{
    /// <summary>The one row of <see cref="MapTables.World"/>; its world name names the map.</summary>
    public required MapWorldObjects World { get; init; }

    /// <summary>The one row of <see cref="MapTables.GameMode"/>.</summary>
    public required MapGameMode GameMode { get; init; }

    /// <summary><see cref="MapTables.StreamingCells"/>.</summary>
    public ImmutableArray<string> StreamingCells { get; init; } = [];

    /// <summary><see cref="MapTables.LootSocketMeshes"/>.</summary>
    public ImmutableArray<LootSocketMesh> LootSocketMeshes { get; init; } = [];

    /// <summary><see cref="MapTables.LootSockets"/>.</summary>
    public ImmutableArray<LootSocket> LootSockets { get; init; } = [];

    /// <summary><see cref="MapTables.PlacedLoot"/>.</summary>
    public ImmutableArray<PlacedLootContainer> PlacedLoot { get; init; } = [];

    /// <summary><see cref="MapTables.PlayerStarts"/>.</summary>
    public ImmutableArray<PlayerStart> PlayerStarts { get; init; } = [];

    /// <summary>The one row of <see cref="MapTables.PlayerStartCapsule"/>.</summary>
    public required PlayerStartCapsule PlayerStartCapsule { get; init; }

    /// <summary><see cref="MapTables.Ladders"/>.</summary>
    public ImmutableArray<LadderPlacement> Ladders { get; init; } = [];

    /// <summary>The row of <see cref="MapTables.LadderLogic"/>, or null for a map without ladders.</summary>
    public ArchetypeClass? LadderLogic { get; init; }

    /// <summary><see cref="MapTables.ZiplineLines"/>.</summary>
    public ImmutableArray<ZiplinePlacement> ZiplineLines { get; init; } = [];

    /// <summary><see cref="MapTables.ZiplineLaunchers"/>.</summary>
    public ImmutableArray<ActivatableZiplinePlacement> ZiplineLaunchers { get; init; } = [];

    /// <summary>The one row of <see cref="MapTables.ZiplineDefaults"/>.</summary>
    public required ZiplineLineDefaults ZiplineDefaults { get; init; }

    /// <summary><see cref="MapTables.PointsOfInterest"/>.</summary>
    public ImmutableArray<PointOfInterest> PointsOfInterest { get; init; } = [];

    /// <summary><see cref="MapTables.LootLimiters"/>.</summary>
    public ImmutableArray<LootLimiter> LootLimiters { get; init; } = [];

    /// <summary><see cref="MapTables.DynamicLootSpawners"/>.</summary>
    public ImmutableArray<DynamicLootSpawner> DynamicLootSpawners { get; init; } = [];

    /// <summary><see cref="MapTables.DynamicLootGroups"/>; their spawner indices index <see cref="DynamicLootSpawners"/>.</summary>
    public ImmutableArray<DynamicLootGroupEntry> DynamicLootGroups { get; init; } = [];

    /// <summary><see cref="MapTables.Doors"/>.</summary>
    public ImmutableArray<DoorPlacement> Doors { get; init; } = [];

    /// <summary><see cref="MapTables.Rounds"/>.</summary>
    public ImmutableArray<RoundConfiguration> Rounds { get; init; } = [];

    /// <summary><see cref="MapTables.LightingScenarios"/>.</summary>
    public ImmutableArray<LightingScenario> LightingScenarios { get; init; } = [];

    /// <summary><see cref="MapTables.RoundLightingGroups"/>.</summary>
    public ImmutableArray<RoundLightingGroup> RoundLightingGroups { get; init; } = [];

    /// <summary>The one row of <see cref="MapTables.ExtractionElevator"/>.</summary>
    public required ExtractionElevator ExtractionElevator { get; init; }

    /// <summary><see cref="MapTables.ExtractionButtons"/>.</summary>
    public ImmutableArray<ExtractionButton> ExtractionButtons { get; init; } = [];

    /// <summary><see cref="MapTables.ExtractionPoints"/>.</summary>
    public ImmutableArray<ExtractionPointPlacement> ExtractionPoints { get; init; } = [];
}
