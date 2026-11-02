using System.Text.Json;
using System.Text.Json.Serialization;
using Ares.Structs.Backend.Profiles;

namespace Ares.Structs.Backend.Social;

/// <summary>Request of <c>POST /v1/shared/notifications</c> (long poll).</summary>
/// <param name="FromNotificationId">Last notification id the client has seen, as a decimal string.</param>
public sealed record NotificationsRequest([property: JsonPropertyName("fromNotificationId")] string? FromNotificationId);

/// <summary>Response of <c>POST /v1/shared/notifications</c>.</summary>
public sealed record NotificationsResponse(
    [property: JsonPropertyName("notifications")] IReadOnlyList<Notification> Notifications,
    [property: JsonPropertyName("nextFromNotificationId")] string NextFromNotificationId);

/// <summary>One notification. <see cref="Name"/> repeats the payload's discriminator.</summary>
public sealed record Notification(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("payload")] NotificationPayload Payload);

/// <summary>Notification payloads, discriminated by their <c>discriminator</c> member.</summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "discriminator")]
[JsonDerivedType(typeof(PartyArrangeNotification), PartyArrangeNotification.Discriminator)]
[JsonDerivedType(typeof(PartyAttributesChangedNotification), PartyAttributesChangedNotification.Discriminator)]
[JsonDerivedType(typeof(PartyPlayerPlatformSessionIdNotification), PartyPlayerPlatformSessionIdNotification.Discriminator)]
[JsonDerivedType(typeof(PartyPlayerCrossplayEnabledNotification), PartyPlayerCrossplayEnabledNotification.Discriminator)]
[JsonDerivedType(typeof(PartyPlayerReadyForMatchmakingNotification), PartyPlayerReadyForMatchmakingNotification.Discriminator)]
public abstract record NotificationPayload
{
    /// <summary>The discriminator value of this payload type.</summary>
    [JsonIgnore]
    public abstract string Name { get; }
}

/// <summary>A party was created or rearranged.</summary>
public sealed record PartyArrangeNotification(
    [property: JsonPropertyName("party_id")] string PartyId,
    [property: JsonPropertyName("leader_tenancy_user_id")] long LeaderTenancyUserId,
    [property: JsonPropertyName("party_members_ids")] IReadOnlyList<long> PartyMembersIds,
    [property: JsonPropertyName("party_members_data")] IReadOnlyList<PartyArrangeMemberData> PartyMembersData,
    [property: JsonPropertyName("leader_profile")] EmbarkProfile LeaderProfile,
    [property: JsonPropertyName("party")] Party Party) : NotificationPayload
{
    public const string Discriminator = "socialPartyArrangeNotification";

    /// <inheritdoc/>
    [JsonIgnore]
    public override string Name => Discriminator;
}

/// <summary>Member data inside <see cref="PartyArrangeNotification"/>.</summary>
public sealed record PartyArrangeMemberData(
    [property: JsonPropertyName("platformProvider")] string PlatformProvider,
    [property: JsonPropertyName("platformSessionId")] string PlatformSessionId,
    [property: JsonPropertyName("crossplayEnabled")] bool CrossplayEnabled,
    [property: JsonPropertyName("memberProfile")] EmbarkProfile MemberProfile,
    [property: JsonPropertyName("language")] string Language);

/// <summary>Party attributes changed.</summary>
public sealed record PartyAttributesChangedNotification(
    [property: JsonPropertyName("tenancyUserId")] long TenancyUserId,
    [property: JsonPropertyName("partyId")] string PartyId,
    [property: JsonPropertyName("modified")] IReadOnlyDictionary<string, JsonElement> Modified,
    [property: JsonPropertyName("current")] IReadOnlyDictionary<string, JsonElement> Current) : NotificationPayload
{
    public const string Discriminator = "partyAttributesChangedNotification";

    /// <inheritdoc/>
    [JsonIgnore]
    public override string Name => Discriminator;
}

/// <summary>A member's platform session id changed.</summary>
public sealed record PartyPlayerPlatformSessionIdNotification(
    [property: JsonPropertyName("partyId")] string PartyId,
    [property: JsonPropertyName("tenancyUserId")] long TenancyUserId,
    [property: JsonPropertyName("platformSessionId")] string PlatformSessionId) : NotificationPayload
{
    public const string Discriminator = "partyPlayerPlatformSessionIdNotification";

    /// <inheritdoc/>
    [JsonIgnore]
    public override string Name => Discriminator;
}

/// <summary>A member's crossplay setting changed.</summary>
public sealed record PartyPlayerCrossplayEnabledNotification(
    [property: JsonPropertyName("partyId")] string PartyId,
    [property: JsonPropertyName("tenancyUserId")] long TenancyUserId,
    [property: JsonPropertyName("crossplayEnabled")] bool CrossplayEnabled) : NotificationPayload
{
    public const string Discriminator = "partyPlayerCrossplayEnabledNotification";

    /// <inheritdoc/>
    [JsonIgnore]
    public override string Name => Discriminator;
}

/// <summary>A member's matchmaking readiness changed.</summary>
public sealed record PartyPlayerReadyForMatchmakingNotification(
    [property: JsonPropertyName("tenancyUserId")] long TenancyUserId,
    [property: JsonPropertyName("partyId")] string PartyId,
    [property: JsonPropertyName("isReadyForMatchmaking")] bool IsReadyForMatchmaking) : NotificationPayload
{
    public const string Discriminator = "partyPlayerReadyForMatchmakingNotification";

    /// <inheritdoc/>
    [JsonIgnore]
    public override string Name => Discriminator;
}
