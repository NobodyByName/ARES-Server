namespace Ares.Structs.Game.World;

/// <summary>A lighting scenario a map can use.</summary>
/// <param name="Tag">Lighting tag, e.g. <c>Lighting.GreyMorning</c>.</param>
/// <param name="Weight">Authored selection weight.</param>
/// <param name="FilterTags">Location tags the scenario is limited to.</param>
/// <param name="DataLayerPackage">Package of the data layer that applies the lighting.</param>
/// <param name="DataLayerExportHash">Public export hash of that data layer asset.</param>
public sealed record LightingScenario(
    string Tag,
    double Weight,
    IReadOnlyList<string> FilterTags,
    string DataLayerPackage,
    ulong DataLayerExportHash);

/// <summary>A lighting choice of a round lighting group.</summary>
/// <param name="Tag">Lighting tag.</param>
/// <param name="Weight">Authored selection weight.</param>
public readonly record struct WeightedLighting(string Tag, double Weight);

/// <summary>A round lighting group: the lighting choices a round configuration's lighting tag selects from.</summary>
/// <param name="Tag">Group tag, e.g. <c>RoundConfigurationLighting.NightRaid</c>.</param>
/// <param name="Choices">Lighting choices with their authored weights, in asset order.</param>
public sealed record RoundLightingGroup(string Tag, IReadOnlyList<WeightedLighting> Choices);

/// <summary>Members of the lighting assets a map's level script names, by script member name.</summary>
public static class LightingAssetMembers
{
    /// <summary><c>ULevelLightingScenariosDataAsset.LightingScenarios</c>: map of lighting tag to scenario.</summary>
    public const string LightingScenarios = "LightingScenarios";

    /// <summary><c>FLightingScenario.Level</c>: soft reference to a lighting level (none in current maps).</summary>
    public const string Level = "Level";

    /// <summary><c>FLightingScenario.FilterTags</c>: location tags the scenario is limited to.</summary>
    public const string FilterTags = "FilterTags";

    /// <summary><c>FLightingScenario.DataLayer</c>: the data layer asset that applies the lighting.</summary>
    public const string DataLayer = "DataLayer";

    /// <summary><c>FLightingScenario.Weight</c> and <c>FRoundConfigurationLightingScenarioEntry.Weight</c>: authored weight.</summary>
    public const string Weight = "Weight";

    /// <summary><c>URoundConfigurationLightingScenarioDataAsset.LightingScenarioEntries</c>: map of group tag to group.</summary>
    public const string LightingScenarioEntries = "LightingScenarioEntries";

    /// <summary><c>FRoundConfigurationLightingScenarioConfig.Entries</c>: the group's choices.</summary>
    public const string Entries = "Entries";

    /// <summary><c>FRoundConfigurationLightingScenarioEntry.ScenarioName</c>: the lighting tag of a choice.</summary>
    public const string ScenarioName = "ScenarioName";
}
