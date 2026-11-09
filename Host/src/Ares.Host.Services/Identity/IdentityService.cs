using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Ares.Common.Game.Identity;
using Ares.Common.Security;
using Ares.Host.Services.Accounts;
using Ares.Host.Services.Content;
using Ares.Host.Services.Routing;
using Ares.Structs.Backend;
using Ares.Structs.Backend.Bootstrap;
using Ares.Structs.Backend.Eos;
using Ares.Structs.Backend.Identity;
using Ares.Structs.Backend.Profiles;
using Ares.Structs.Core;

namespace Ares.Host.Services.Identity;

/// <summary>Sign-in of ARES accounts, token checks, and synthetic EOS and bootstrap replies.</summary>
public sealed class IdentityService(HostSettings settings, AccountDirectory accounts, TimeProvider time)
{
    /// <summary>
    /// Per-run signing key for synthetic third-party and EOS tokens, and identity tokens when the
    /// operator configured no identity key.
    /// </summary>
    private static readonly byte[] s_unverifiedKey = RandomNumberGenerator.GetBytes(32);

    /// <summary>Audience of refresh tokens, so they never pass as access tokens.</summary>
    private const string RefreshAudience = "ares-host-refresh";

    private byte[] IdentityKey => settings.IdentityKey ?? s_unverifiedKey;

    /// <summary>
    /// Reply of <c>POST /oauth2/token</c>: tokens for the account owning the launch key in
    /// <c>client_secret</c>, or for the account of a valid refresh token. Null when neither
    /// names an unbanned account.
    /// </summary>
    public (OAuthTokenResponse? Response, Account? Account) SignIn(BackendRequest request)
    {
        Account? account = request.FormValue(OAuthTokenForm.GrantType) == OAuthTokenForm.RefreshTokenGrant
            ? RefreshTokenOwner(request.FormValue(OAuthTokenForm.RefreshToken))
            : accounts.RedeemLaunchKey(request.FormValue(OAuthTokenForm.ClientSecret));
        return account is null ? (null, null) : (OAuth(account, settings.Issuer, IdentityKey), account);
    }

    /// <summary>The user id of a valid <c>Bearer</c> identity token, or null.</summary>
    public long? Authenticate(string? authorization)
    {
        if (authorization is null || !authorization.StartsWith("Bearer ", StringComparison.Ordinal))
        {
            return null;
        }

        try
        {
            return IdentityTokens.Validate(authorization[7..], IdentityKey, settings.Issuer, time.GetUtcNow()).UserId;
        }
        catch (TokenRejectedException)
        {
            return null;
        }
    }

    /// <summary>Reply of <c>GET /v1/shared/auth/third-party-access-token</c>.</summary>
    public ThirdPartyAccessTokenResponse ThirdPartyAccessToken(Account account)
    {
        string token = OAuth(account, EmbarkIdentity.OfficialIssuer, s_unverifiedKey).AccessToken;
        long expiresAt = time.GetUtcNow().ToUnixTimeSeconds() + IdentityTokens.LifetimeSeconds;
        return new ThirdPartyAccessTokenResponse(token, token, token, IdentityTokens.LifetimeSeconds, IdentityTokens.LifetimeSeconds, expiresAt);
    }

    /// <summary>Reply of <c>GET /v1/pioneer/raiders</c>.</summary>
    public static RaidersResponse Raiders(long userId)
    {
        return new RaidersResponse([new Raider(PlayerDefaults.RaiderId(userId), RaiderStates.Active, PlayerDefaults.RaiderCreatedAt)]);
    }

    /// <summary>Reply of <c>POST /v1/shared/profile/by-thirdparty-userid</c>: the caller's profile when its platform id was asked for.</summary>
    public static (PlatformProfilesResponse Response, bool Matched) PlatformProfiles(EmbarkProfile profile, PlatformProfilesRequest request)
    {
        bool matched = profile.ThirdPartyUserId.Length > 0 && (request.Ids ?? []).Contains(profile.ThirdPartyUserId);
        Dictionary<string, EmbarkProfile> found = matched ? new Dictionary<string, EmbarkProfile> { [profile.ThirdPartyUserId] = profile } : [];
        return (new PlatformProfilesResponse(found), matched);
    }

    /// <summary>Reply of <c>POST /v1/shared/manifest</c>.</summary>
    public ManifestResponse Manifest(ManifestRequest request, string contentHash)
    {
        long now = time.GetUtcNow().ToUnixTimeSeconds();
        long id = request.ManifestId is { } manifestId and not 0 ? manifestId : BackendContent.Manifest.DefaultId;
        string buildId = request.BuildId ?? GameBuild.BuildIdentifier;
        return new ManifestResponse(id, buildId, request.PlaytestId ?? 0,
            contentHash, contentHash,
            new UnknownManifestBody(), new UnknownManifestBody(), now, now - 3600, now + 86400);
    }

    /// <summary>Reports the requested build as current for <c>POST /v1/shared/build/v2/latest</c>.</summary>
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

    private Account? RefreshTokenOwner(string? token)
    {
        try
        {
            long userId = IdentityTokens.Validate(token, IdentityKey, settings.Issuer, time.GetUtcNow(), RefreshAudience).UserId;
            return accounts.Find(userId) is { Record.Banned: false } account ? account : null;
        }
        catch (TokenRejectedException)
        {
            return null;
        }
    }

    private OAuthTokenResponse OAuth(Account account, string issuer, byte[] key)
    {
        string token = IdentityTokens.Sign(Claims(account, issuer, EmbarkIdentity.Audience, IdentityTokens.LifetimeSeconds), key);
        string refresh = IdentityTokens.Sign(Claims(account, issuer, RefreshAudience, (int)AccountDirectory.LaunchKeySession.TotalSeconds), key);
        return new OAuthTokenResponse(token, token, refresh, IdentityTokens.LifetimeSeconds, "Bearer", "openid profile");
    }

    private IdentityTokenClaims Claims(Account account, string issuer, string audience, int lifetime)
    {
        long now = time.GetUtcNow().ToUnixTimeSeconds();
        return new IdentityTokenClaims(
            Subject: account.UserId.ToString(CultureInfo.InvariantCulture),
            Issuer: issuer,
            Audience: [audience],
            ExpiresAt: now + lifetime,
            IssuedAt: now - 30,
            NotBefore: now - 30,
            TokenId: Guid.NewGuid().ToString("D"),
            Scopes: ["openid", "profile"],
            TenancyId: PlayerDefaults.Tenancy,
            TenancyName: PlayerDefaults.Tenancy,
            Extension: new IdentityTokenExtension(EmbarkIdentity.Audience, EmbarkIdentity.Company,
                account.Record.EmbarkUserId.ToString(CultureInfo.InvariantCulture), PlayerDefaults.Tenancy, PlayerDefaults.Tenancy,
                PlayerDefaults.ExternalProvider, "", UnknownNxgu: false, UnknownNxgb: false, ""));
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
