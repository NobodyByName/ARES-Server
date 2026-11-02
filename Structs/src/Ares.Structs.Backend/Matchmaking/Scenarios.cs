using System.Text.Json.Serialization;

namespace Ares.Structs.Backend.Matchmaking;

/// <summary>Response of <c>POST /v1/shared/scenarios</c>: the selectable matchmaking scenarios.</summary>
public sealed record ScenariosResponse(
    [property: JsonPropertyName("matchmakingScenarios")] IReadOnlyList<MatchmakingScenario> MatchmakingScenarios);

/// <summary>A selectable map/mode.</summary>
public sealed record MatchmakingScenario(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("parameters")] ScenarioParameters Parameters,
    [property: JsonPropertyName("UISettings")] ScenarioUiSettings UiSettings,
    [property: JsonPropertyName("pioneerSettings")] ScenarioGameSettings GameSettings);

/// <summary>Matchmaking parameters of a scenario.</summary>
public sealed record ScenarioParameters(
    [property: JsonPropertyName("gameMode")] string GameMode,
    [property: JsonPropertyName("mapName")] string MapName,
    [property: JsonPropertyName("roundsRequiredToPlay")] int RoundsRequiredToPlay,
    [property: JsonPropertyName("characterLevelRequiredToPlay")] int CharacterLevelRequiredToPlay,
    [property: JsonPropertyName("maxSquadSize")] int MaxSquadSize,
    [property: JsonPropertyName("featureFlags")] UnknownFeatureFlags FeatureFlags);

/// <summary>Menu visibility of a scenario.</summary>
public sealed record ScenarioUiSettings(
    [property: JsonPropertyName("visible")] bool Visible,
    [property: JsonPropertyName("disabled")] bool Disabled,
    [property: JsonPropertyName("active")] bool Active,
    [property: JsonPropertyName("visiblePeriods")] IReadOnlyList<UnknownSchedulePeriod> VisiblePeriods,
    [property: JsonPropertyName("activePeriods")] IReadOnlyList<UnknownSchedulePeriod> ActivePeriods);

/// <summary>Game-specific scenario settings (<c>pioneerSettings</c>).</summary>
public sealed record ScenarioGameSettings(
    [property: JsonPropertyName("mapAssetId")] int MapAssetId,
    [property: JsonPropertyName("mapConditions")] MapConditions MapConditions);

/// <summary>Optional map conditions of a scenario.</summary>
public sealed record MapConditions(
    [property: JsonPropertyName("isOptIn")] bool IsOptIn,
    [property: JsonPropertyName("conditionSettings")] IReadOnlyList<UnknownMapConditionSetting> ConditionSettings);

/// <summary>Scenario feature flags; members not decoded yet.</summary>
public sealed record UnknownFeatureFlags;

/// <summary>A visibility/activity period; members not decoded yet.</summary>
public sealed record UnknownSchedulePeriod;

/// <summary>A map condition setting; members not decoded yet.</summary>
public sealed record UnknownMapConditionSetting;

/// <summary>Scenario, game mode and map names.</summary>
public static class ScenarioNames
{
    /// <summary>The Dam Battlegrounds scenario.</summary>
    public const string TheDam = "TheDam";

    /// <summary>The game mode of regular raids.</summary>
    public const string RaidGameMode = "pioneer";
}
