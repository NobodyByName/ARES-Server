using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Ares.Structs.Backend.Identity;

namespace Ares.Host.Services.Content;

/// <summary>Profile values every account shares, and ids derived from an account's user id.</summary>
public static class PlayerDefaults
{
    public const string Discriminator = "0001";
    public const string ThirdPartyUserId = "0";
    public const long RaiderCreatedAt = 946684800; // 2000-01-01T00:00:00Z
    public const string TosVersionSeen = "3";
    public const string DateOfBirth = "2000-01-01";
    public const string CountryCode = "SE";
    public const string Email = "local.raider@example.invalid";
    public const bool EmailIsVerified = true;
    public const bool VoiceChatModerationConsent = true;
    public const string Tenancy = EmbarkIdentity.LiveTenancy;
    public const int ExternalProvider = ExternalProviders.Steam;

    /// <summary>The account's single raider.</summary>
    public static string RaiderId(long userId) => DerivedId("raider", userId);

    /// <summary>The party led by <paramref name="leader"/>.</summary>
    public static string PartyId(long leader) => DerivedId("party", leader);

    private static string DerivedId(string kind, long userId)
    {
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(kind + "/" + userId.ToString(CultureInfo.InvariantCulture)));
        hash[7] = (byte)((hash[7] & 0x0F) | 0x40);
        hash[8] = (byte)((hash[8] & 0x3F) | 0x80);
        return new Guid(hash.AsSpan(0, 16)).ToString("D");
    }
}
