using System.Text.Json.Serialization;
using Ares.Structs.Backend.Profiles;

namespace Ares.Structs.Backend.Identity;

/// <summary>Constants of the game's identity (OAuth) service.</summary>
public static class EmbarkIdentity
{
    /// <summary>Audience the game requests and the session server checks.</summary>
    public const string Audience = "embark-pioneer";

    /// <summary>Issuer of official tokens.</summary>
    public const string OfficialIssuer = "https://auth.embark.net/";

    /// <summary>Value of <c>ext.company</c>.</summary>
    public const string Company = "embark";

    /// <summary>Default tenancy of the live game.</summary>
    public const string LiveTenancy = "pioneer-live";
}

/// <summary>External identity providers (<c>ext_provider_id</c>).</summary>
public static class ExternalProviders
{
    /// <summary>Steam.</summary>
    public const int Steam = 3;

    /// <summary>Steam, as named in party member data.</summary>
    public const string SteamName = "steam";
}

/// <summary>Form fields of <c>POST /oauth2/token</c>.</summary>
public static class OAuthTokenForm
{
    /// <summary>The configured <c>[EmbarkOnline] ClientSecret</c>.</summary>
    public const string ClientSecret = "client_secret";

    /// <summary>The grant type.</summary>
    public const string GrantType = "grant_type";

    /// <summary>A previously issued refresh token.</summary>
    public const string RefreshToken = "refresh_token";

    /// <summary>The <see cref="GrantType"/> that renews a sign-in with <see cref="RefreshToken"/>.</summary>
    public const string RefreshTokenGrant = "refresh_token";
}

/// <summary>Response of <c>POST /oauth2/token</c>.</summary>
public sealed record OAuthTokenResponse(
    [property: JsonPropertyName("access_token")] string AccessToken,
    [property: JsonPropertyName("id_token")] string IdToken,
    [property: JsonPropertyName("refresh_token")] string RefreshToken,
    [property: JsonPropertyName("expires_in")] int ExpiresIn,
    [property: JsonPropertyName("token_type")] string TokenType,
    [property: JsonPropertyName("scope")] string Scope);

/// <summary>JOSE header of an identity token.</summary>
public sealed record IdentityTokenHeader(
    [property: JsonPropertyName("alg")] string Algorithm,
    [property: JsonPropertyName("typ")] string Type,
    [property: JsonPropertyName("kid")] string KeyId,
    [property: JsonPropertyName("kid_version")] string KeyVersion);

/// <summary>Claims of an identity (access/id) token.</summary>
public sealed record IdentityTokenClaims(
    [property: JsonPropertyName("sub")] string Subject,
    [property: JsonPropertyName("iss")] string Issuer,
    [property: JsonPropertyName("aud")] IReadOnlyList<string> Audience,
    [property: JsonPropertyName("exp")] long ExpiresAt,
    [property: JsonPropertyName("iat")] long IssuedAt,
    [property: JsonPropertyName("nbf")] long NotBefore,
    [property: JsonPropertyName("jti")] string TokenId,
    [property: JsonPropertyName("scp")] IReadOnlyList<string> Scopes,
    [property: JsonPropertyName("tenancy_id")] string TenancyId,
    [property: JsonPropertyName("tenancy_name")] string TenancyName,
    [property: JsonPropertyName("ext")] IdentityTokenExtension Extension);

/// <summary>The <c>ext</c> claim of an identity token.</summary>
public sealed record IdentityTokenExtension(
    [property: JsonPropertyName("client_id")] string ClientId,
    [property: JsonPropertyName("company")] string Company,
    [property: JsonPropertyName("embark_user_id")] string EmbarkUserId,
    [property: JsonPropertyName("tenancy_id")] string TenancyId,
    [property: JsonPropertyName("tenancy_name")] string TenancyName,
    [property: JsonPropertyName("ext_provider_id")] int ExternalProviderId,
    [property: JsonPropertyName("nxsid")] string UnknownNxsid,
    [property: JsonPropertyName("nxgu")] bool UnknownNxgu,
    [property: JsonPropertyName("nxgb")] bool UnknownNxgb,
    [property: JsonPropertyName("nxgr")] string UnknownNxgr);

/// <summary>
/// Response of <c>GET /v1/shared/auth/third-party-access-token</c>. The token is repeated under
/// several names because different client readers use different ones.
/// </summary>
public sealed record ThirdPartyAccessTokenResponse(
    [property: JsonPropertyName("token")] string Token,
    [property: JsonPropertyName("access_token")] string AccessToken,
    [property: JsonPropertyName("accessToken")] string AccessTokenCamel,
    [property: JsonPropertyName("expires_in")] int ExpiresIn,
    [property: JsonPropertyName("expiresIn")] int ExpiresInCamel,
    [property: JsonPropertyName("expiresAt")] long ExpiresAt);

/// <summary>Request of <c>POST /v1/shared/profile/by-thirdparty-userid</c>.</summary>
public sealed record PlatformProfilesRequest(
    [property: JsonPropertyName("ids")] IReadOnlyList<string>? Ids,
    [property: JsonPropertyName("provider")] string? Provider);

/// <summary>Response of <c>POST /v1/shared/profile/by-thirdparty-userid</c>, keyed by platform user id.</summary>
public sealed record PlatformProfilesResponse(
    [property: JsonPropertyName("profiles")] IReadOnlyDictionary<string, EmbarkProfile> Profiles);
