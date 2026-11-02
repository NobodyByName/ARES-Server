using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Ares.Common.Game.Identity;
using Ares.Common.Security;
using Ares.Host.Services.Content;
using Ares.Structs.Backend;
using Ares.Structs.Backend.Bootstrap;
using Ares.Structs.Backend.Eos;
using Ares.Structs.Backend.Identity;
using Ares.Structs.Backend.Profiles;
using Ares.Structs.Core;

namespace Ares.Host.Services.Identity;

/// <summary>Synthetic sign-in, EOS and bootstrap replies for the local account.</summary>
public sealed class IdentityService(HostSettings settings, ProfileFactory profiles, TimeProvider time)
{
    /// <summary>
    /// Random per-run key for tokens Host never checks: identity tokens issued without an operator key,
    /// third-party access tokens and EOS tokens.
    /// </summary>
    private static readonly byte[] s_unverifiedKey = RandomNumberGenerator.GetBytes(32);

    /// <summary>Reply of <c>POST /oauth2/token</c>: a token issued by this Host.</summary>
    public OAuthTokenResponse SignIn()
    {
        return OAuth(settings.Issuer, settings.IdentityKey ?? s_unverifiedKey);
    }

    /// <summary>Reply of <c>GET /v1/shared/auth/third-party-access-token</c>.</summary>
    public ThirdPartyAccessTokenResponse ThirdPartyAccessToken()
    {
        string token = OAuth(EmbarkIdentity.OfficialIssuer, s_unverifiedKey).AccessToken;
        long expiresAt = time.GetUtcNow().ToUnixTimeSeconds() + IdentityTokens.LifetimeSeconds;
        return new ThirdPartyAccessTokenResponse(token, token, token, IdentityTokens.LifetimeSeconds, IdentityTokens.LifetimeSeconds, expiresAt);
    }

    /// <summary>Reply of <c>GET /v1/pioneer/raiders</c>.</summary>
    public static RaidersResponse Raiders()
    {
        return new RaidersResponse([new Raider(LocalPlayer.RaiderId, RaiderStates.Active, LocalPlayer.RaiderCreatedAt)]);
    }

    /// <summary>Reply of <c>POST /v1/shared/profile/by-thirdparty-userid</c>: the local profile when its platform id was asked for.</summary>
    public (PlatformProfilesResponse Response, bool Matched) PlatformProfiles(PlatformProfilesRequest request)
    {
        EmbarkProfile profile = profiles.Create();
        bool matched = profile.ThirdPartyUserId.Length > 0 && (request.Ids ?? []).Contains(profile.ThirdPartyUserId);
        Dictionary<string, EmbarkProfile> found = matched ? new Dictionary<string, EmbarkProfile> { [profile.ThirdPartyUserId] = profile } : [];
        return (new PlatformProfilesResponse(found), matched);
    }

    /// <summary>Reply of <c>POST /v1/shared/manifest</c>.</summary>
    public ManifestResponse Manifest(ManifestRequest request)
    {
        long now = time.GetUtcNow().ToUnixTimeSeconds();
        long id = request.ManifestId is { } manifestId and not 0 ? manifestId : BackendContent.Manifest.DefaultId;
        string buildId = request.BuildId ?? GameBuild.BuildIdentifier;
        return new ManifestResponse(id, buildId, request.PlaytestId ?? 0,
            BackendContent.Manifest.PartitioningHash, BackendContent.Manifest.NonPartitioningHash,
            new UnknownManifestBody(), new UnknownManifestBody(), now, now - 3600, now + 86400);
    }

    /// <summary>Reply of <c>POST /v1/shared/build/v2/latest</c>: never an update.</summary>
    public static LatestBuildResponse LatestBuild(LatestBuildRequest request)
    {
        return new LatestBuildResponse(request.BuildId ?? "", UpdateAvailable: false);
    }

    /// <summary>Reply of <c>POST /auth/v1/oauth/token</c> (EOS SDK sign-in), and whether it was a user grant.</summary>
    public (EosTokenResponse Response, bool UserGrant) EosToken(EosTokenRequest request, string? authorization)
    {
        string client = BasicAuthClientId(authorization) ?? BackendContent.Eos.DefaultClientId;
        DateTimeOffset nowTime = time.GetUtcNow();
        long now = nowTime.ToUnixTimeSeconds();
        string deployment = request.DeploymentId ?? BackendContent.Eos.DefaultDeploymentId;
        bool user = request.GrantType != EosEndpoints.ClientCredentialsGrant;
        EosAccessTokenClaims claims = new(EosEndpoints.TokenIssuer, EosEndpoints.TokenAudience, user ? BackendContent.Eos.ProductUserId : client,
            client, deployment, now - 30, now - 30, now + 3600, Guid.NewGuid().ToString("D"), user ? BackendContent.Eos.ProductUserId : null);
        string token = Hs256Jwt.Sign(
            JsonSerializer.SerializeToUtf8Bytes(new EosTokenHeader("HS256", "JWT", BackendContent.Eos.TokenKeyId), BackendJson.Options),
            JsonSerializer.SerializeToUtf8Bytes(claims, BackendJson.Options),
            s_unverifiedKey);
        string expiresAt = DateTimeOffset.FromUnixTimeSeconds(now + 3600).UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture);
        EosTokenResponse response = new(token, "bearer", 3600, expiresAt, client, deployment,
            BackendContent.Eos.OrganizationId, BackendContent.Eos.ProductId, BackendContent.Eos.SandboxId, BackendContent.Eos.Features,
            Nonce: string.IsNullOrEmpty(request.Nonce) ? null : request.Nonce);
        if (user)
        {
            response = response with
            {
                ProductUserId = BackendContent.Eos.ProductUserId,
                OrganizationUserId = BackendContent.Eos.ProductUserId,
                ProductUserIdCreated = false,
                IdToken = token,
            };
        }

        return (response, user);
    }

    private OAuthTokenResponse OAuth(string issuer, byte[] key)
    {
        long now = time.GetUtcNow().ToUnixTimeSeconds();
        string userId = LocalPlayer.TenancyUserId.ToString(CultureInfo.InvariantCulture);
        IdentityTokenClaims claims = new(
            Subject: userId,
            Issuer: issuer,
            Audience: [EmbarkIdentity.Audience],
            ExpiresAt: now + IdentityTokens.LifetimeSeconds,
            IssuedAt: now - 30,
            NotBefore: now - 30,
            TokenId: Guid.NewGuid().ToString("D"),
            Scopes: ["openid", "profile"],
            TenancyId: LocalPlayer.Tenancy,
            TenancyName: LocalPlayer.Tenancy,
            Extension: new IdentityTokenExtension(EmbarkIdentity.Audience, EmbarkIdentity.Company,
                LocalPlayer.EmbarkUserId.ToString(CultureInfo.InvariantCulture), LocalPlayer.Tenancy, LocalPlayer.Tenancy,
                LocalPlayer.ExternalProvider, "", UnknownNxgu: false, UnknownNxgb: false, ""));
        string token = IdentityTokens.Sign(claims, key);
        return new OAuthTokenResponse(token, token, "ares-host-refresh-only", IdentityTokens.LifetimeSeconds, "Bearer", "openid profile");
    }

    private static string? BasicAuthClientId(string? authorization)
    {
        if (authorization is null || !authorization.StartsWith("Basic ", StringComparison.Ordinal))
        {
            return null;
        }

        try
        {
            string decoded = Encoding.UTF8.GetString(Convert.FromBase64String(authorization[6..]));
            return decoded.Split(':', 2)[0];
        }
        catch (FormatException)
        {
            return null;
        }
    }
}
