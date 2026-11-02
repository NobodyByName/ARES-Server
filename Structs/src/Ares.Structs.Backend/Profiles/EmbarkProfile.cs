using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ares.Structs.Backend.Profiles;

/// <summary>
/// A player's account profile (<c>GET /v1/shared/profile</c>, also embedded in party and squad data).
/// On the wire every member appears twice, once snake_case and once camelCase: the shared
/// service has used both generated snake_case and reflected camelCase models.
/// </summary>
[JsonConverter(typeof(EmbarkProfileConverter))]
public sealed record EmbarkProfile(
    long TenancyUserId,
    long AccountId,
    long CreatedAt,
    DisplayName DisplayName,
    long DisplayNameCooldownEndsAt,
    string Email,
    bool EmailIsVerified,
    long EmailVerifiedAt,
    string ThirdPartyUserId,
    string ThirdPartyLastSeenAccountName,
    string TosVersionSeen,
    string DateOfBirth,
    string CountryCode,
    bool IsSpender,
    bool VoiceChatModerationConsent);

/// <summary>Display name and its discriminator (the <c>#0001</c> suffix).</summary>
public sealed record DisplayName(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("discriminator")] string Discriminator);

/// <summary>Writes <see cref="EmbarkProfile"/> with snake_case members followed by camelCase duplicates.</summary>
public sealed class EmbarkProfileConverter : JsonConverter<EmbarkProfile>
{
    /// <inheritdoc/>
    public override EmbarkProfile Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotSupportedException("Profiles are only written by the server");
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, EmbarkProfile value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        WriteMembers(writer, value, options, camelCase: false);
        WriteMembers(writer, value, options, camelCase: true);
        writer.WriteEndObject();
    }

    private static void WriteMembers(Utf8JsonWriter writer, EmbarkProfile value, JsonSerializerOptions options, bool camelCase)
    {
        writer.WriteNumber(camelCase ? "tenancyUserId" : "tenancy_user_id", value.TenancyUserId);
        writer.WriteNumber(camelCase ? "accountId" : "account_id", value.AccountId);
        writer.WriteNumber(camelCase ? "createdAt" : "created_at", value.CreatedAt);
        writer.WritePropertyName(camelCase ? "displayName" : "display_name");
        JsonSerializer.Serialize(writer, value.DisplayName, options);
        writer.WriteNumber(camelCase ? "displayNameCooldownEndsAt" : "display_name_cooldown_ends_at", value.DisplayNameCooldownEndsAt);
        if (!camelCase)
        {
            // "email" is the same in both namings and appears once.
            writer.WriteString("email", value.Email);
        }

        writer.WriteBoolean(camelCase ? "emailIsVerified" : "email_is_verified", value.EmailIsVerified);
        writer.WriteNumber(camelCase ? "emailVerifiedAt" : "email_verified_at", value.EmailVerifiedAt);
        writer.WriteString(camelCase ? "thirdPartyUserId" : "third_party_user_id", value.ThirdPartyUserId);
        writer.WriteString(camelCase ? "thirdPartyLastSeenAccountName" : "third_party_last_seen_account_name", value.ThirdPartyLastSeenAccountName);
        writer.WriteString(camelCase ? "tosVersionSeen" : "tos_version_seen", value.TosVersionSeen);
        writer.WriteString(camelCase ? "dateOfBirth" : "date_of_birth", value.DateOfBirth);
        writer.WriteString(camelCase ? "countryCode" : "country_code", value.CountryCode);
        writer.WriteBoolean(camelCase ? "isSpender" : "is_spender", value.IsSpender);
        writer.WriteBoolean(camelCase ? "voiceChatModerationConsent" : "voice_chat_moderation_consent", value.VoiceChatModerationConsent);
    }
}

/// <summary>Response of <c>GET /v1/pioneer/raiders</c>: the account's characters.</summary>
public sealed record RaidersResponse([property: JsonPropertyName("raiders")] IReadOnlyList<Raider> Raiders);

/// <summary>One character of an account.</summary>
public sealed record Raider(
    [property: JsonPropertyName("raiderId")] string RaiderId,
    [property: JsonPropertyName("state")] string State,
    [property: JsonPropertyName("createdAt")] long CreatedAt);

/// <summary>Values of <see cref="Raider.State"/>.</summary>
public static class RaiderStates
{
    /// <summary>A usable character.</summary>
    public const string Active = "ACTIVE";
}
