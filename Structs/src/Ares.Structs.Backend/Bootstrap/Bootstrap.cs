using System.Text.Json.Serialization;

namespace Ares.Structs.Backend.Bootstrap;

/// <summary>Request of <c>POST /v1/shared/manifest</c>.</summary>
public sealed record ManifestRequest(
    [property: JsonPropertyName("app_id")] string? AppId,
    [property: JsonPropertyName("artifact_id")] string? ArtifactId,
    [property: JsonPropertyName("build_id")] string? BuildId,
    [property: JsonPropertyName("manifest_id")] long? ManifestId,
    [property: JsonPropertyName("playtest_id")] long? PlaytestId,
    [property: JsonPropertyName("store_deployment_target")] string? StoreDeploymentTarget);

/// <summary>Response of <c>POST /v1/shared/manifest</c>: the remote build manifest.</summary>
public sealed record ManifestResponse(
    [property: JsonPropertyName("id")] long Id,
    [property: JsonPropertyName("buildId")] string BuildId,
    [property: JsonPropertyName("playtestId")] long PlaytestId,
    [property: JsonPropertyName("partitioningHash")] string PartitioningHash,
    [property: JsonPropertyName("nonPartitioningHash")] string NonPartitioningHash,
    [property: JsonPropertyName("partitioningBody")] IReadOnlyDictionary<string, string> PartitioningBody,
    [property: JsonPropertyName("nonPartitioningBody")] IReadOnlyDictionary<string, string> NonPartitioningBody,
    [property: JsonPropertyName("retrievedAt")] long RetrievedAt,
    [property: JsonPropertyName("validFrom")] long ValidFrom,
    [property: JsonPropertyName("validUntil")] long ValidUntil);

/// <summary>Keys of the manifest bodies (<see cref="ManifestResponse.PartitioningBody"/>, <see cref="ManifestResponse.NonPartitioningBody"/>).</summary>
public static class ManifestKeys
{
    /// <summary>
    /// Prefix of a game flag: the prefix plus the name. A client feature flag named this way, valued <c>true</c> or
    /// <c>false</c>, overrides the client's default for that flag.
    /// </summary>
    public const string GameFlagPrefix = "gameflags.";
}

/// <summary>Client feature flags a backend can override through <see cref="ManifestKeys.GameFlagPrefix"/>.</summary>
public static class ClientFeatureFlagNames
{
    /// <summary>Main menu timer that fetches the whole inventory again every 15 seconds.</summary>
    public const string PeriodicSyncOfInventory = "periodic_sync_of_inventory";
}

/// <summary>Request of <c>POST /v1/shared/build/v2/latest</c>.</summary>
public sealed record LatestBuildRequest(
    [property: JsonPropertyName("appId")] string? AppId,
    [property: JsonPropertyName("buildId")] string? BuildId,
    [property: JsonPropertyName("storeDeploymentTarget")] string? StoreDeploymentTarget);

/// <summary>Response of <c>POST /v1/shared/build/v2/latest</c>.</summary>
public sealed record LatestBuildResponse(
    [property: JsonPropertyName("buildId")] string BuildId,
    [property: JsonPropertyName("updateAvailable")] bool UpdateAvailable);
