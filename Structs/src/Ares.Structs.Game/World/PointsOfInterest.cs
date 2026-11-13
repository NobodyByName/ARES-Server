namespace Ares.Structs.Game.World;

/// <summary>
/// The loot value the map shows for a point of interest (the game's <c>EPointOfInterestEnemyThreatLevel</c>; the
/// map tooltip labels it "Loot value").
/// </summary>
public enum PointOfInterestLootValue
{
    /// <summary>No loot value is shown.</summary>
    Unknown,

    /// <summary>Low loot value.</summary>
    Low,

    /// <summary>Medium loot value.</summary>
    Medium,

    /// <summary>High loot value.</summary>
    High,
}

/// <summary>A named area of a map: a point-of-interest volume placed in the map's persistent level.</summary>
/// <param name="Actor">The volume actor.</param>
/// <param name="Name">String-table key of the area's name (<c>ST_Location</c>), or null when it has none.</param>
/// <param name="Parent">The enclosing point of interest's actor, or null for a top-level area.</param>
/// <param name="LootValue">The loot value the map shows for the area.</param>
/// <param name="LootThemes">Loot categories the map lists for the area (<c>UI.ItemClassification.Theme.*</c> tags).</param>
/// <param name="Hulls">The volume's collision parts in world space; a point inside any part is inside the area.</param>
public sealed record PointOfInterest(
    string Actor,
    string? Name,
    string? Parent,
    PointOfInterestLootValue LootValue,
    IReadOnlyList<string> LootThemes,
    IReadOnlyList<LootVolumeHull> Hulls);
