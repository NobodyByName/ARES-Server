using System.Text.Json.Serialization;

namespace Ares.Structs.Backend.Eos;

/// <summary>Constants of the Epic Online Services (EOS) SDK endpoints the game uses.</summary>
public static class EosEndpoints
{
    /// <summary>The game's EOS product id (path of the product SDK-config route).</summary>
    public const string GameProductId = "9e8b37541e614575b4de303d2c2e44cf";

    /// <summary>Issuer of EOS access tokens.</summary>
    public const string TokenIssuer = "https://api.epicgames.dev/auth/v1/oauth";

    /// <summary>Audience of EOS access tokens.</summary>
    public const string TokenAudience = "api.epicgames.dev";

    /// <summary>Grant type of an application (non-user) token.</summary>
    public const string ClientCredentialsGrant = "client_credentials";
}

/// <summary>Form fields of <c>POST /auth/v1/oauth/token</c> that the server uses.</summary>
public sealed record EosTokenRequest(string? GrantType, string? DeploymentId, string? Nonce);

/// <summary>Response of <c>POST /auth/v1/oauth/token</c>.</summary>
public sealed record EosTokenResponse(
    [property: JsonPropertyName("access_token")] string AccessToken,
    [property: JsonPropertyName("token_type")] string TokenType,
    [property: JsonPropertyName("expires_in")] int ExpiresIn,
    [property: JsonPropertyName("expires_at")] string ExpiresAt,
    [property: JsonPropertyName("client_id")] string ClientId,
    [property: JsonPropertyName("deployment_id")] string DeploymentId,
    [property: JsonPropertyName("organization_id")] string OrganizationId,
    [property: JsonPropertyName("product_id")] string ProductId,
    [property: JsonPropertyName("sandbox_id")] string SandboxId,
    [property: JsonPropertyName("features")] IReadOnlyList<string> Features,
    [property: JsonPropertyName("nonce"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Nonce = null,
    [property: JsonPropertyName("product_user_id"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? ProductUserId = null,
    [property: JsonPropertyName("organization_user_id"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? OrganizationUserId = null,
    [property: JsonPropertyName("product_user_id_created"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] bool? ProductUserIdCreated = null,
    [property: JsonPropertyName("id_token"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? IdToken = null);

/// <summary>JOSE header of an EOS access token.</summary>
public sealed record EosTokenHeader(
    [property: JsonPropertyName("alg")] string Algorithm,
    [property: JsonPropertyName("typ")] string Type,
    [property: JsonPropertyName("kid")] string KeyId);

/// <summary>Claims of an EOS access token.</summary>
public sealed record EosAccessTokenClaims(
    [property: JsonPropertyName("iss")] string Issuer,
    [property: JsonPropertyName("aud")] string Audience,
    [property: JsonPropertyName("sub")] string Subject,
    [property: JsonPropertyName("clid")] string ClientId,
    [property: JsonPropertyName("dep")] string DeploymentId,
    [property: JsonPropertyName("iat")] long IssuedAt,
    [property: JsonPropertyName("nbf")] long NotBefore,
    [property: JsonPropertyName("exp")] long ExpiresAt,
    [property: JsonPropertyName("jti")] string TokenId,
    [property: JsonPropertyName("puid"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? ProductUserId = null);
