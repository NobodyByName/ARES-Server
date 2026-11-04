using Ares.Structs.Backend.Identity;

namespace Ares.Host.Services.Content;

/// <summary>
/// The synthetic account identity used for every local sign-in.
/// </summary>
public static class LocalPlayer
{
    public const long TenancyUserId = 1000000000000000000;
    public const long EmbarkUserId = 1000000000000000256;
    public const string DisplayName = "ARES-Enjoyer";
    public const string Discriminator = "0001";
    public const string ThirdPartyUserId = "0";
    public const string RaiderId = "00000000-0000-4000-8000-000000000001";
    public const long RaiderCreatedAt = 946684800; // 2000-01-01T00:00:00Z
    public const string TosVersionSeen = "3";
    public const string DateOfBirth = "2000-01-01";
    public const string CountryCode = "SE";
    public const string Email = "local.raider@example.invalid";
    public const bool EmailIsVerified = true;
    public const bool VoiceChatModerationConsent = true;
    public const string Tenancy = EmbarkIdentity.LiveTenancy;
    public const int ExternalProvider = ExternalProviders.Steam;

    /// <summary>The single synthetic party's id.</summary>
    public const string PartyId = "00000000-0000-4000-8000-000000000002";
}
