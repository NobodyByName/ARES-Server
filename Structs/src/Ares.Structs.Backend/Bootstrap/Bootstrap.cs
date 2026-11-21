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
    [property: JsonPropertyName("partitioningBody")] UnknownManifestBody PartitioningBody,
    [property: JsonPropertyName("nonPartitioningBody")] UnknownManifestBody NonPartitioningBody,
    [property: JsonPropertyName("retrievedAt")] long RetrievedAt,
    [property: JsonPropertyName("validFrom")] long ValidFrom,
    [property: JsonPropertyName("validUntil")] long ValidUntil);

/// <summary>Manifest body; members not decoded yet.</summary>
public sealed record UnknownManifestBody;

/// <summary>Request of <c>POST /v1/shared/build/v2/latest</c>.</summary>
public sealed record LatestBuildRequest(
    [property: JsonPropertyName("appId")] string? AppId,
    [property: JsonPropertyName("buildId")] string? BuildId,
    [property: JsonPropertyName("storeDeploymentTarget")] string? StoreDeploymentTarget);

/// <summary>Response of <c>POST /v1/shared/build/v2/latest</c>.</summary>
public sealed record LatestBuildResponse(
    [property: JsonPropertyName("buildId")] string BuildId,
    [property: JsonPropertyName("updateAvailable")] bool UpdateAvailable);
