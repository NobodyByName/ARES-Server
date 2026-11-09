using Ares.Host.Services.Accounts;
using Ares.Host.Services.Content;
using Ares.Structs.Backend.Profiles;

namespace Ares.Host.Services.Identity;

/// <summary>Builds an account's profile. The display name is the account name; only operators rename accounts.</summary>
public sealed class ProfileFactory(Func<Account> current, TimeProvider time)
{
    /// <summary>The account's tenancy user id.</summary>
    public long UserId => current().UserId;

    /// <summary>The current profile.</summary>
    public EmbarkProfile Create()
    {
        Account account = current();
        long now = time.GetUtcNow().ToUnixTimeSeconds();
        string displayName = account.Name;
        return new EmbarkProfile(
            TenancyUserId: account.UserId,
            AccountId: account.Record.EmbarkUserId,
            CreatedAt: account.Record.CreatedAt,
            DisplayName: new DisplayName(displayName, PlayerDefaults.Discriminator),
            DisplayNameCooldownEndsAt: now + (30 * 86400),
            Email: PlayerDefaults.Email,
            EmailIsVerified: PlayerDefaults.EmailIsVerified,
            EmailVerifiedAt: account.Record.CreatedAt,
            ThirdPartyUserId: PlayerDefaults.ThirdPartyUserId,
            ThirdPartyLastSeenAccountName: displayName,
            TosVersionSeen: PlayerDefaults.TosVersionSeen,
            DateOfBirth: PlayerDefaults.DateOfBirth,
            CountryCode: PlayerDefaults.CountryCode,
            IsSpender: false,
            VoiceChatModerationConsent: PlayerDefaults.VoiceChatModerationConsent);
    }
}
