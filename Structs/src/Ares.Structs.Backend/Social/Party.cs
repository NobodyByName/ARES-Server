using System.Text.Json;
using System.Text.Json.Serialization;
using Ares.Structs.Backend.Profiles;

namespace Ares.Structs.Backend.Social;

/// <summary>A party as returned by party routes and embedded in notifications.</summary>
public sealed record Party(
    [property: JsonPropertyName("party_id")] string PartyId,
    [property: JsonPropertyName("leader_tenancy_user_id")] long LeaderTenancyUserId,
    [property: JsonPropertyName("party_members")] IReadOnlyList<PartyMember> PartyMembers,
    [property: JsonPropertyName("party_member_matchmaking_readiness")] IReadOnlyList<PartyMemberReadiness> PartyMemberMatchmakingReadiness,
    [property: JsonPropertyName("members_data")] IReadOnlyList<PartyMemberData> MembersData,
    [property: JsonPropertyName("attributes")] IReadOnlyDictionary<string, JsonElement> Attributes,
    [property: JsonPropertyName("join_secret")] string JoinSecret,
    [property: JsonPropertyName("party_privacy")] string PartyPrivacy,
    [property: JsonPropertyName("invite_permission")] string InvitePermission);

/// <summary>A party member's profile.</summary>
public sealed record PartyMember([property: JsonPropertyName("profile")] EmbarkProfile Profile);

/// <summary>A party member's matchmaking readiness.</summary>
public sealed record PartyMemberReadiness(
    [property: JsonPropertyName("tenancy_user_id")] long TenancyUserId,
    [property: JsonPropertyName("is_ready_for_matchmaking")] bool IsReadyForMatchmaking);

/// <summary>Per-member settings of a party.</summary>
public sealed record PartyMemberData(
    [property: JsonPropertyName("tenancy_user_id")] long TenancyUserId,
    [property: JsonPropertyName("ready_for_matchmaking")] bool ReadyForMatchmaking,
    [property: JsonPropertyName("crossplay_enabled")] bool CrossplayEnabled,
    [property: JsonPropertyName("platform_provider")] string PlatformProvider,
    [property: JsonPropertyName("platform_session_id")] string PlatformSessionId,
    [property: JsonPropertyName("language")] string Language);

/// <summary>Values of <see cref="Party.PartyPrivacy"/>.</summary>
public static class PartyPrivacies
{
    /// <summary>Only invited players can join.</summary>
    public const string InviteOnly = "INVITE_ONLY";
}

/// <summary>Values of <see cref="Party.InvitePermission"/>.</summary>
public static class InvitePermissions
{
    /// <summary>Any member may invite.</summary>
    public const string AnyMember = "ANY_MEMBER";
}

/// <summary>Request of <c>POST /v1/shared/social/party/v2/create</c>.</summary>
public sealed record CreatePartyRequest(
    [property: JsonPropertyName("crossplay_enabled")] bool? CrossplayEnabled,
    [property: JsonPropertyName("crossplay")] string? UnknownCrossplay,
    [property: JsonPropertyName("invite_permission")] string? InvitePermission,
    [property: JsonPropertyName("language")] string? Language,
    [property: JsonPropertyName("platform_session_id")] string? PlatformSessionId);

/// <summary>Response of <c>POST /v1/shared/social/party/v2/create</c>.</summary>
public sealed record CreatePartyResponse(
    [property: JsonPropertyName("party")] Party Party,
    [property: JsonPropertyName("max_party_size")] int MaxPartySize);

/// <summary>Request of <c>PATCH /v1/shared/social/party/v2/update-party-attributes</c>.</summary>
public sealed record UpdatePartyAttributesRequest(
    [property: JsonPropertyName("party_id")] string? PartyId,
    [property: JsonPropertyName("tenancy_user_id")] long? TenancyUserId,
    [property: JsonPropertyName("attributes")] IReadOnlyDictionary<string, JsonElement>? Attributes);

/// <summary>Request of <c>POST /v1/shared/social/party/v2/update-party-member-data</c>. Absent members are left unchanged.</summary>
public sealed record UpdatePartyMemberDataRequest(
    [property: JsonPropertyName("party_id")] string? PartyId,
    [property: JsonPropertyName("updated_member_tenancy_user_id")] long? UpdatedMemberTenancyUserId,
    [property: JsonPropertyName("crossplay_enabled")] bool? CrossplayEnabled,
    [property: JsonPropertyName("crossplay")] string? UnknownCrossplay,
    [property: JsonPropertyName("platform_session_id")] string? PlatformSessionId,
    [property: JsonPropertyName("language")] string? Language,
    [property: JsonPropertyName("ready_for_matchmaking")] bool? ReadyForMatchmaking);

/// <summary>Request of <c>POST /v1/shared/social/party/set-ready-for-matchmaking</c>.</summary>
public sealed record SetReadyForMatchmakingRequest([property: JsonPropertyName("is_ready")] bool? IsReady);
