namespace Ares.Structs.Backend.Identity;

/// <summary>Request paths of the Embark identity API.</summary>
public static class IdentityPaths
{
    /// <summary><c>POST</c> with an <see cref="OAuthTokenForm"/> body; reply <see cref="OAuthTokenResponse"/>.</summary>
    public const string Token = "/oauth2/token";

    /// <summary><c>GET</c>; reply <see cref="ThirdPartyAccessTokenResponse"/>.</summary>
    public const string ThirdPartyAccessToken = "/v1/shared/auth/third-party-access-token";

    /// <summary><c>POST</c> <see cref="PlatformProfilesRequest"/>; reply <see cref="PlatformProfilesResponse"/>.</summary>
    public const string PlatformProfiles = "/v1/shared/profile/by-thirdparty-userid";

    /// <summary><c>POST</c>; reply format not decoded yet.</summary>
    public const string TenancyUserSync = "/v1/pioneer/tenancy-user/sync";
}
