namespace Ares.Structs.Backend.Profiles;

/// <summary>Request paths of the profile API.</summary>
public static class ProfilePaths
{
    /// <summary><c>GET</c>; reply <see cref="EmbarkProfile"/>.</summary>
    public const string Profile = "/v1/shared/profile";

    /// <summary><c>GET</c>; reply <see cref="RaidersResponse"/>.</summary>
    public const string Raiders = "/v1/pioneer/raiders";
}
