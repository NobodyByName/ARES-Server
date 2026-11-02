using System.Buffers.Text;
using System.Text;
using Ares.Common.Game.Identity;
using Ares.Common.Security;
using Ares.Structs.Backend.Identity;

namespace Ares.Common.Tests;

public sealed class SecurityTests
{
    private static readonly byte[] s_key = [.. "0123456789abcdef0123456789abcdef"u8];
    private static readonly DateTimeOffset s_now = DateTimeOffset.FromUnixTimeSeconds(1_800_000_000);
    private const string Issuer = "http://127.0.0.1:18080/";

    [Fact]
    public void Signed_tokens_verify()
    {
        string token = Hs256Jwt.Sign("""{"alg":"HS256","typ":"JWT"}"""u8, """{"sub":"1"}"""u8, s_key);

        VerifiedToken verified = Hs256Jwt.Verify(token, s_key);

        Assert.Equal("1", verified.Claims.GetProperty("sub").GetString());
    }

    [Theory]
    [InlineData("""{"alg":"none","typ":"JWT"}""", """{"sub":"1"}""")]
    [InlineData("""{"alg":"HS256","typ":"JWT"}""", """{"sub":"1","sub":"2"}""")]
    [InlineData("""{"alg":"HS256","typ":"JWT"}""", "[1]")]
    public void Unsupported_or_ambiguous_tokens_are_rejected(string header, string claims)
    {
        string token = Hs256Jwt.Sign(Encoding.UTF8.GetBytes(header), Encoding.UTF8.GetBytes(claims), s_key);

        Assert.Throws<TokenRejectedException>(() => Hs256Jwt.Verify(token, s_key));
    }

    [Fact]
    public void Tampered_or_foreign_tokens_are_rejected()
    {
        string token = Hs256Jwt.Sign("""{"alg":"HS256","typ":"JWT"}"""u8, """{"sub":"1"}"""u8, s_key);
        string tampered = token[..token.IndexOf('.')] + "." + Base64Url.EncodeToString("""{"sub":"2"}"""u8) + token[token.LastIndexOf('.')..];

        Assert.Throws<TokenRejectedException>(() => Hs256Jwt.Verify(tampered, s_key));
        Assert.Throws<TokenRejectedException>(() => Hs256Jwt.Verify(token, "another-key-another-key-another-key"u8));
        Assert.Throws<TokenRejectedException>(() => Hs256Jwt.Verify("a.b", s_key));
    }

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

    [Fact]
    public void Operator_tokens_need_32_ascii_characters_and_match_exactly()
    {
        string token = new('x', 32);

        Assert.Equal(token, OperatorToken.Validate(token));
        Assert.Throws<ArgumentException>(() => OperatorToken.Validate(new string('x', 31)));
        Assert.True(OperatorToken.Authorizes("Bearer " + token, token));
        Assert.False(OperatorToken.Authorizes("Bearer " + token + "x", token));
        Assert.False(OperatorToken.Authorizes(null, token));
    }

    private static IdentityTokenClaims Claims(string issuer, long expiresAt)
    {
        long now = s_now.ToUnixTimeSeconds();
        return new IdentityTokenClaims("42", issuer, [EmbarkIdentity.Audience], expiresAt, now - 30, now - 30, "token-id", ["openid"],
            "t", "t", new IdentityTokenExtension("c", EmbarkIdentity.Company, "1", "t", "t", 3, "", false, false, ""));
    }
}
