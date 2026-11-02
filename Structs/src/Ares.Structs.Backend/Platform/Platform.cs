using System.Text.Json.Serialization;

namespace Ares.Structs.Backend.Platform;

/// <summary>Response of <c>GET /v1/shared/anticheat/config</c>.</summary>
public sealed record AnticheatConfigResponse(
    [property: JsonPropertyName("data")] string Data,
    [property: JsonPropertyName("signature")] string Signature);

/// <summary>Response of <c>GET /v1/shared/anticheat/config/is-anybrain-eligible</c>.</summary>
public sealed record AnybrainEligibilityResponse(
    [property: JsonPropertyName("eligible")] bool Eligible,
    [property: JsonPropertyName("isEligible")] bool IsEligible);

/// <summary>Response of <c>POST /v1/shared/anticheat/restrictions</c> and <c>GET /v1/shared/is-restricted</c>.</summary>
public sealed record RestrictionsResponse([property: JsonPropertyName("restrictions")] IReadOnlyList<UnknownRestriction> Restrictions);

/// <summary>Response of <c>GET /v1/shared/video-encryption-keys</c>.</summary>
public sealed record VideoEncryptionKeysResponse([property: JsonPropertyName("keys")] UnknownVideoEncryptionKeys Keys);

/// <summary>Response of <c>GET /v1/shared/game-settings</c> and <c>POST /v1/shared/localizations</c>.</summary>
public sealed record EntriesResponse([property: JsonPropertyName("entries")] IReadOnlyList<UnknownEntry> Entries);

public sealed record UnknownRestriction;

public sealed record UnknownVideoEncryptionKeys;

public sealed record UnknownEntry;
