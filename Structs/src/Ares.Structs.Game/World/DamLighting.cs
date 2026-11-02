namespace Ares.Structs.Game.World;

/// <summary>Lighting scenarios of the Dam map and the round lighting groups (authored weights).</summary>
public static class DamLighting
{
    /// <summary>Scenarios in the map's lighting lookup.</summary>
    public static IReadOnlyList<LightingScenario> Scenarios { get; } =
    [
        new("Lighting.Rainstorm", 1.0, ["GameMode.Location.TheDam"], "/Game/Pioneer/Maps/TheDam_01/DataLayers/TheDam_01_Lighting_Rainstorm", 0x2957052B70FD3DD8UL),
        new("Lighting.GreyMorning", 1.0, ["GameMode.Location.TheDam"], "/Game/Pioneer/Maps/TheDam_01/DataLayers/TheDam_01_Lighting_GreyMorning", 0x9248F63F07681E74UL),
        new("Lighting.Night", 0.0, ["GameMode.Location.TheDam"], "/Game/Pioneer/Maps/TheDam_01/DataLayers/TheDam_01_Lighting_Night", 0xA2BD528C5AF4AACUL),
        new("Lighting.PinkSunrise", 1.0, ["GameMode.Location.TheDam"], "/Game/Pioneer/Maps/TheDam_01/DataLayers/TheDam_01_Lighting_PinkSunrise", 0xEF14D424F818A1E2UL),
        new("Lighting.HazyDay", 0.0, ["GameMode.Location.TheDam"], "/Game/Pioneer/Maps/TheDam_01/DataLayers/TheDam_01_Lighting_HazyDay", 0x4EFE67C2432EBD7CUL),
        new("Lighting.Foggy", 0.0, ["GameMode.Location.TheDam"], "/Game/Pioneer/Maps/TheDam_01/DataLayers/TheDam_01_Lighting_Foggy", 0xB32561BD770C2E8DUL),
        new("Lighting.ForestFire", 0.0, ["GameMode.Location.TheDam"], "/Game/Pioneer/Maps/TheDam_01/DataLayers/TheDam_01_Lighting_ForestFire", 0xD0D46C2AA80FEBD9UL),
        new("Lighting.Default", 0.0, [], "/Game/Pioneer/Maps/TheDam_01/DataLayers/TheDam_01_Lighting_DefaultL", 0x726EA775CF148FE8UL),
        new("Lighting.Thunderstorm", 0.0, ["GameMode.Location.TheDam"], "/Game/Pioneer/Maps/TheDam_01/DataLayers/TheDam_01_Lighting_Thunderstorm", 0x67EE5C8411EAD68BUL),
        new("Lighting.WinterSnow.Dam", 0.0, [], "/Game/Pioneer/Maps/TheDam_01/DataLayers/TheDam_01_Lighting_WinterSnowDam", 0xD9E720AED3F5B54EUL),
        new("Lighting.WindyStorm", 0.0, [], "/Game/Pioneer/Maps/TheDam_01/DataLayers/TheDam_01_Lighting_WindyStorm", 0x6320FC7D54AF3251UL),
        new("Lighting.frigate.thedam", 0.0, [], "/Game/Pioneer/Maps/TheDam_02/DataLayers/TheDam_02_Lighting_Frigate_Overcast", 0x466188494A081D46UL),
    ];

    /// <summary>Lighting choices of each round lighting group.</summary>
    public static IReadOnlyDictionary<string, IReadOnlyList<WeightedLighting>> RoundLightingGroups { get; } =
        new Dictionary<string, IReadOnlyList<WeightedLighting>>
        {
            ["RoundConfigurationLighting.ProbeHeist"] = [new WeightedLighting("Lighting.Rainstorm", 1.0)],
            ["RoundConfigurationLighting.CacheThemAll"] = [new WeightedLighting("Lighting.ForestFire", 1.0)],
            ["RoundConfigurationLighting.Harvester"] = [new WeightedLighting("Lighting.Rainstorm", 1.0), new WeightedLighting("Lighting.PinkSunrise", 1.0), new WeightedLighting("Lighting.ForestFire", 1.0)],
            ["RoundConfigurationLighting.HarvestSeason"] = [new WeightedLighting("Lighting.GreyMorning", 1.0), new WeightedLighting("Lighting.PinkSunrise", 2.0)],
            ["RoundConfigurationLighting.NightRaid"] = [new WeightedLighting("Lighting.Night", 1.0)],
            ["RoundConfigurationLighting.FTUE"] = [new WeightedLighting("Lighting.GreyMorning", 1.0), new WeightedLighting("Lighting.PinkSunrise", 1.0)],
            ["RoundConfigurationLighting.ToxicSwamp"] = [new WeightedLighting("Lighting.Foggy", 1.0)],
            ["RoundConfigurationLighting.ElectromagneticStorm"] = [new WeightedLighting("Lighting.Thunderstorm", 1.0)],
            ["RoundConfigurationLighting.ColdSnap"] = [new WeightedLighting("Lighting.WinterSnow.Dam", 1.0)],
            ["RoundConfigurationLighting.WindStorm"] = [new WeightedLighting("Lighting.WindyStorm", 1.0)],
            ["RoundConfigurationLighting.Extractor"] = [new WeightedLighting("Lighting.Foggy", 1.0), new WeightedLighting("Lighting.PinkSunrise", 1.0)],
            ["RoundConfigurationLighting.frigate"] = [new WeightedLighting("Lighting.frigate.thedam", 1.0)],
        };
}
