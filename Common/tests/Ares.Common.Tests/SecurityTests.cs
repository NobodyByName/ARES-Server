using System.Buffers.Text;
using System.Text;
using Ares.Common.Security;

namespace Ares.Common.Tests;

public sealed class SecurityTests
{
    private static readonly byte[] s_key = [.. "0123456789abcdef0123456789abcdef"u8];

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
    public void Operator_tokens_need_32_ascii_characters_and_match_exactly()
    {
        string token = new('x', 32);

        Assert.Equal(token, OperatorToken.Validate(token));
        Assert.Throws<ArgumentException>(() => OperatorToken.Validate(new string('x', 31)));
        Assert.True(OperatorToken.Authorizes("Bearer " + token, token));
        Assert.False(OperatorToken.Authorizes("Bearer " + token + "x", token));
        Assert.False(OperatorToken.Authorizes(null, token));
    }
}
