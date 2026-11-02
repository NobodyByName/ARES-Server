using Ares.Host.Services.Accounts;
using Ares.Host.Services.Content;
using Ares.Structs.Backend.Profiles;

namespace Ares.Host.Services.Identity;

/// <summary>Builds the local account's profile, applying stored display-name changes.</summary>
public sealed class ProfileFactory(AccountStore accounts, TimeProvider time)
{
    /// <summary>The current profile.</summary>
    public EmbarkProfile Create()
    {
        long now = time.GetUtcNow().ToUnixTimeSeconds();
        AccountProfileOverrides overrides = accounts.Profile;
        string displayName = overrides.DisplayName ?? LocalPlayer.DisplayName;
        return new EmbarkProfile(
            TenancyUserId: LocalPlayer.TenancyUserId,
            AccountId: LocalPlayer.EmbarkUserId,
            CreatedAt: now - 86400,
            DisplayName: new DisplayName(displayName, LocalPlayer.Discriminator),
            DisplayNameCooldownEndsAt: overrides.DisplayNameCooldownEndsAt ?? now + (30 * 86400),
            Email: LocalPlayer.Email,
            EmailIsVerified: LocalPlayer.EmailIsVerified,
            EmailVerifiedAt: now - 86400,
            ThirdPartyUserId: LocalPlayer.ThirdPartyUserId,
            ThirdPartyLastSeenAccountName: displayName,
            TosVersionSeen: LocalPlayer.TosVersionSeen,
            DateOfBirth: LocalPlayer.DateOfBirth,
            CountryCode: LocalPlayer.CountryCode,
            IsSpender: false,
            VoiceChatModerationConsent: LocalPlayer.VoiceChatModerationConsent);
    }
}
