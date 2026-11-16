namespace Ares.Structs.Backend.Social;

/// <summary>Request paths of the social API: party, notifications, presence, friends and inbox.</summary>
public static class SocialPaths
{
    /// <summary><c>POST</c> <see cref="CreatePartyRequest"/>; reply <see cref="CreatePartyResponse"/>.</summary>
    public const string CreateParty = "/v1/shared/social/party/v2/create";

    /// <summary><c>PATCH</c> <see cref="UpdatePartyAttributesRequest"/>.</summary>
    public const string UpdatePartyAttributes = "/v1/shared/social/party/v2/update-party-attributes";

    /// <summary><c>POST</c> <see cref="UpdatePartyMemberDataRequest"/>.</summary>
    public const string UpdatePartyMemberData = "/v1/shared/social/party/v2/update-party-member-data";

    /// <summary><c>POST</c> <see cref="SetReadyForMatchmakingRequest"/>.</summary>
    public const string SetReadyForMatchmaking = "/v1/shared/social/party/set-ready-for-matchmaking";

    /// <summary><c>POST</c>; reply format not decoded yet.</summary>
    public const string LeaveParty = "/v1/shared/social/party/leave";

    /// <summary><c>POST</c> <see cref="NotificationsRequest"/>; reply <see cref="NotificationsResponse"/>.</summary>
    public const string Notifications = "/v1/shared/notifications";

    /// <summary><c>POST</c>; reply format not decoded yet.</summary>
    public const string SetRichPresence = "/v1/shared/social/presence/set-rich-presence";

    /// <summary><c>POST</c>; reply format not decoded yet.</summary>
    public const string GetRichPresence = "/v1/shared/social/presence/get-rich-presence";

    /// <summary><c>POST</c>; reply format not decoded yet.</summary>
    public const string PresenceAllowlist = "/v1/shared/social/presence/allowlist";

    /// <summary><c>GET</c>; reply format not decoded yet.</summary>
    public const string BlockedPlayers = "/v1/shared/social/block-players/blocked-players";

    /// <summary><c>GET</c>; reply format not decoded yet.</summary>
    public const string Friends = "/v1/shared/social/friends/get-friends";

    /// <summary><c>GET</c>; reply format not decoded yet.</summary>
    public const string RecentlyPlayedWith = "/v1/shared/social/recently-played-with";

    /// <summary><c>POST</c>; reply format not decoded yet.</summary>
    public const string InboxMessages = "/v1/shared/inbox/get-messages";
}
