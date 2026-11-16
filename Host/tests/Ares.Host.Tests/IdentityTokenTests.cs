using Ares.Common.Security;
using Ares.Host.Services.Identity;
using Ares.Structs.Backend.Identity;

namespace Ares.Host.Tests;

public sealed class IdentityTokenTests
{
    private static readonly byte[] s_key = [.. "0123456789abcdef0123456789abcdef"u8];
    private static readonly DateTimeOffset s_now = DateTimeOffset.FromUnixTimeSeconds(1_800_000_000);
    private const string Issuer = "http://127.0.0.1:18080/";

    [Fact]
    public void Identity_tokens_round_trip()
    {
        string token = IdentityTokens.Sign(Claims(Issuer, expiresAt: s_now.ToUnixTimeSeconds() + 3600), s_key);

        IdentityPrincipal principal = IdentityTokens.Validate(token, s_key, Issuer, s_now);

        Assert.Equal(new IdentityPrincipal(42, "token-id"), principal);
    }

    [Fact]
    public void Identity_tokens_with_wrong_issuer_or_lifetime_are_rejected()
    {
        string otherIssuer = IdentityTokens.Sign(Claims("https://elsewhere.invalid/", s_now.ToUnixTimeSeconds() + 3600), s_key);
        string expired = IdentityTokens.Sign(Claims(Issuer, s_now.ToUnixTimeSeconds() - 1), s_key);

        Assert.Throws<TokenRejectedException>(() => IdentityTokens.Validate(otherIssuer, s_key, Issuer, s_now));
        Assert.Throws<TokenRejectedException>(() => IdentityTokens.Validate(expired, s_key, Issuer, s_now));
    }

    [Theory]
    [InlineData("1", true)]
    [InlineData("1000000000000000000", true)]
    [InlineData("0", false)]
    [InlineData("01", false)]
    [InlineData("99999999999999999999", false)]
    [InlineData("12a", false)]
    public void User_ids_are_positive_decimal_longs(string text, bool valid)
    {
        Assert.Equal(valid, IdentityTokens.IsUserId(text, out _));
    }

    private static IdentityTokenClaims Claims(string issuer, long expiresAt)
    {
        long now = s_now.ToUnixTimeSeconds();
        return new IdentityTokenClaims("42", issuer, [EmbarkIdentity.Audience], expiresAt, now - 30, now - 30, "token-id", ["openid"],
            "t", "t", new IdentityTokenExtension("c", EmbarkIdentity.Company, "1", "t", "t", 3, "", false, false, ""));
    }
}
