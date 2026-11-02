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
