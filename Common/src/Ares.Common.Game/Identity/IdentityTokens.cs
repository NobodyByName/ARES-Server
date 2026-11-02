using System.Globalization;
using System.Text.Json;
using Ares.Common.Security;
using Ares.Structs.Backend;
using Ares.Structs.Backend.Identity;

namespace Ares.Common.Game.Identity;

/// <summary>A player identity proven by a verified identity token.</summary>
/// <param name="UserId">Tenancy user id (the token subject).</param>
/// <param name="TokenId">The token's <c>jti</c>.</param>
public sealed record IdentityPrincipal(long UserId, string TokenId);

/// <summary>
/// Identity tokens issued by Host (OAuth login) and checked by Host's protected routes and by Raid
/// before admitting a player. Both sides share the signing key and the issuer (Host's public origin + "/").
/// </summary>
public static class IdentityTokens
{
    /// <summary>Key id ARES writes into token headers.</summary>
    public const string KeyId = "ares-host";

    /// <summary>Lifetime of issued tokens, in seconds.</summary>
    public const int LifetimeSeconds = 3600;

    /// <summary>Tolerated clock skew for <c>iat</c>, in seconds.</summary>
    public const int IssuedAtSkewSeconds = 30;

    /// <summary>Signs <paramref name="claims"/> with HS256.</summary>
    public static string Sign(IdentityTokenClaims claims, ReadOnlySpan<byte> key)
    {
        byte[] header = JsonSerializer.SerializeToUtf8Bytes(new IdentityTokenHeader("HS256", "JWT", KeyId, "1"), BackendJson.Options);
        byte[] body = JsonSerializer.SerializeToUtf8Bytes(claims, BackendJson.Options);
        return Hs256Jwt.Sign(header, body, key);
    }

    /// <summary>
    /// Verifies signature, issuer, audience, lifetime and subject of an identity token.
    /// </summary>
    /// <param name="token">The bearer token.</param>
    /// <param name="key">Shared signing key.</param>
    /// <param name="issuer">Expected <c>iss</c>, ending with "/".</param>
    /// <param name="now">Current time.</param>
    /// <param name="audience">Required audience entry.</param>
    /// <exception cref="TokenRejectedException">The token is not acceptable.</exception>
    public static IdentityPrincipal Validate(string? token, ReadOnlySpan<byte> key, string issuer, DateTimeOffset now, string audience = EmbarkIdentity.Audience)
    {
        JsonElement claims = Hs256Jwt.Verify(token, key).Claims;
        double seconds = now.ToUnixTimeMilliseconds() / 1000.0;
        double issuedAt = Time(claims, "iat");
        double notBefore = Time(claims, "nbf");
        double expiresAt = Time(claims, "exp");
        if (!HasString(claims, "iss", issuer) || !HasAudience(claims, audience)
            || expiresAt <= seconds || notBefore > seconds || issuedAt > seconds + IssuedAtSkewSeconds || expiresAt <= issuedAt)
        {
            throw new TokenRejectedException("Identity issuer, audience or lifetime rejected");
        }

        if (!claims.TryGetProperty("sub", out JsonElement subject) || subject.ValueKind != JsonValueKind.String
            || !IsUserId(subject.GetString(), out long userId)
            || !claims.TryGetProperty("jti", out JsonElement tokenId) || tokenId.ValueKind != JsonValueKind.String
            || tokenId.GetString() is not { Length: >= 1 and <= 128 } id)
        {
            throw new TokenRejectedException("Invalid identity subject");
        }

        return new IdentityPrincipal(userId, id);
    }

    private static double Time(JsonElement claims, string name)
    {
        return claims.TryGetProperty(name, out JsonElement value) && value.ValueKind == JsonValueKind.Number
            && value.TryGetDouble(out double result) && double.IsFinite(result)
            ? result
            : throw new TokenRejectedException("Invalid identity lifetime");
    }

    private static bool HasString(JsonElement claims, string name, string expected)
    {
        return claims.TryGetProperty(name, out JsonElement value) && value.ValueKind == JsonValueKind.String && value.GetString() == expected;
    }

    private static bool HasAudience(JsonElement claims, string audience)
    {
        if (!claims.TryGetProperty("aud", out JsonElement value))
        {
            return false;
        }

        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString() == audience,
            JsonValueKind.Array => value.EnumerateArray().Any(item => item.ValueKind == JsonValueKind.String && item.GetString() == audience),
            _ => false,
        };
    }

    /// <summary>True for a decimal tenancy user id: 1–20 digits, no leading zero, within <see cref="long"/>.</summary>
    public static bool IsUserId(string? text, out long userId)
    {
        userId = 0;
        return text is { Length: >= 1 and <= 20 } && text[0] is >= '1' and <= '9' && text.All(char.IsAsciiDigit)
            && long.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out userId);
    }
}
