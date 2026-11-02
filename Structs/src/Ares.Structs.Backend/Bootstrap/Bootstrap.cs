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
    [property: JsonPropertyName("build_id")] string BuildId,
    [property: JsonPropertyName("playtest_id")] long PlaytestId,
    [property: JsonPropertyName("partitioning_hash")] string PartitioningHash,
    [property: JsonPropertyName("non_partitioning_hash")] string NonPartitioningHash,
    [property: JsonPropertyName("partitioning_body")] UnknownManifestBody PartitioningBody,
    [property: JsonPropertyName("non_partitioning_body")] UnknownManifestBody NonPartitioningBody,
    [property: JsonPropertyName("retrieved_at")] long RetrievedAt,
    [property: JsonPropertyName("valid_from")] long ValidFrom,
    [property: JsonPropertyName("valid_until")] long ValidUntil);

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
