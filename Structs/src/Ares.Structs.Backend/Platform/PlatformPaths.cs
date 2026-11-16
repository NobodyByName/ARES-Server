namespace Ares.Structs.Backend.Platform;

/// <summary>Request paths of the platform services: anti-cheat, settings, localization and connectivity.</summary>
public static class PlatformPaths
{
    /// <summary><c>GET</c>; reply <see cref="AnticheatConfigResponse"/>.</summary>
    public const string AnticheatConfig = "/v1/shared/anticheat/config";

    /// <summary><c>GET</c>; reply <see cref="AnybrainEligibilityResponse"/>.</summary>
    public const string AnybrainEligibility = "/v1/shared/anticheat/config/is-anybrain-eligible";

    /// <summary><c>POST</c>; reply <see cref="RestrictionsResponse"/>.</summary>
    public const string AnticheatRestrictions = "/v1/shared/anticheat/restrictions";

    /// <summary><c>GET</c>; reply <see cref="RestrictionsResponse"/>.</summary>
    public const string IsRestricted = "/v1/shared/is-restricted";

    /// <summary><c>GET</c>; reply <see cref="VideoEncryptionKeysResponse"/>.</summary>
    public const string VideoEncryptionKeys = "/v1/shared/video-encryption-keys";

    /// <summary><c>GET</c>; reply <see cref="EntriesResponse"/>.</summary>
    public const string GameSettings = "/v1/shared/game-settings";

    /// <summary><c>POST</c>; reply <see cref="EntriesResponse"/>.</summary>
    public const string Localizations = "/v1/shared/localizations";

    /// <summary><c>POST</c>; reply format not decoded yet.</summary>
    public const string Announcements = "/v1/shared/announcements";

    /// <summary><c>POST</c>; reply format not decoded yet.</summary>
    public const string Heartbeat = "/v1/shared/heartbeat";

    /// <summary><c>GET</c>; reply format not decoded yet.</summary>
    public const string Proxy = "/v1/shared/proxy";

    /// <summary><c>GET</c>; reply format not decoded yet.</summary>
    public const string Quilkin = "/v1/shared/quilkin";

    /// <summary><c>POST</c>; purpose and reply format not decoded yet.</summary>
    public const string UnknownUpdate = "/api/v3/update/";

    /// <summary><c>POST</c>; purpose and reply format not decoded yet.</summary>
    public const string UnknownNotify = "/api/v1/notify/";
}
