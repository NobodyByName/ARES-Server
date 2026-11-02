using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Ares.Common.Security;

/// <summary>
/// Compact HS256 JSON Web Tokens: signing and strict verification of the signature and encoding.
/// Claim semantics (issuer, audience, lifetime, subject) are checked by the caller.
/// </summary>
public static class Hs256Jwt
{
    /// <summary>Longest token accepted for verification.</summary>
    public const int MaximumTokenLength = 8192;

    /// <summary>Signs a token from already serialized UTF-8 JSON header and claims.</summary>
    public static string Sign(ReadOnlySpan<byte> headerJson, ReadOnlySpan<byte> claimsJson, ReadOnlySpan<byte> key)
    {
        string unsigned = Base64Url.EncodeToString(headerJson) + "." + Base64Url.EncodeToString(claimsJson);
        byte[] signature = HMACSHA256.HashData(key, Encoding.ASCII.GetBytes(unsigned));
        return unsigned + "." + Base64Url.EncodeToString(signature);
    }

    /// <summary>
    /// Verifies the token's encoding, header (<c>alg</c> HS256, <c>typ</c> JWT) and signature, and
    /// returns its header and claims. Objects with duplicate member names are rejected.
    /// </summary>
    /// <exception cref="TokenRejectedException">Any check failed.</exception>
    public static VerifiedToken Verify(string? token, ReadOnlySpan<byte> key)
    {
        if (token is null || token.Length is < 1 or > MaximumTokenLength || !Ascii.IsValid(token))
        {
            throw new TokenRejectedException("Missing or oversized identity token");
        }

        string[] parts = token.Split('.');
        if (parts.Length != 3)
        {
            throw new TokenRejectedException("Invalid identity token");
        }

        JsonElement header = ParseObject(DecodeSegment(parts[0]));
        if (!IsString(header, "alg", "HS256") || !IsString(header, "typ", "JWT"))
        {
            throw new TokenRejectedException("Unsupported token algorithm");
        }

        byte[] signature = DecodeSegment(parts[2]);
        byte[] expected = HMACSHA256.HashData(key, Encoding.ASCII.GetBytes(parts[0] + "." + parts[1]));
        return CryptographicOperations.FixedTimeEquals(expected, signature)
            ? new VerifiedToken(header, ParseObject(DecodeSegment(parts[1])))
            : throw new TokenRejectedException("Invalid identity signature");
    }

    private static bool IsString(JsonElement value, string name, string expected)
    {
        return value.TryGetProperty(name, out JsonElement member)
            && member.ValueKind == JsonValueKind.String
            && member.GetString() == expected;
    }

    private static byte[] DecodeSegment(string segment)
    {
        if (segment.Length == 0 || segment.Any(c => !(char.IsAsciiLetterOrDigit(c) || c is '-' or '_')))
        {
            throw new TokenRejectedException("Invalid token encoding");
        }

        try
        {
            return Base64Url.DecodeFromChars(segment);
        }
        catch (FormatException)
        {
            throw new TokenRejectedException("Invalid token encoding");
        }
    }

    private static JsonElement ParseObject(byte[] utf8)
    {
        JsonElement value;
        try
        {
            using JsonDocument document = JsonDocument.Parse(utf8);
            value = document.RootElement.Clone();
        }
        catch (JsonException)
        {
            throw new TokenRejectedException("Invalid token JSON");
        }

        if (value.ValueKind != JsonValueKind.Object)
        {
            throw new TokenRejectedException("Invalid token object");
        }

        RejectDuplicateMembers(value);
        return value;
    }

    private static void RejectDuplicateMembers(JsonElement value)
    {
        switch (value.ValueKind)
        {
            case JsonValueKind.Object:
                HashSet<string> names = new(StringComparer.Ordinal);
                foreach (JsonProperty property in value.EnumerateObject())
                {
                    if (!names.Add(property.Name))
                    {
                        throw new TokenRejectedException("Duplicate token claim");
                    }

                    RejectDuplicateMembers(property.Value);
                }

                break;
            case JsonValueKind.Array:
                foreach (JsonElement item in value.EnumerateArray())
                {
                    RejectDuplicateMembers(item);
                }

                break;
        }
    }
}

/// <summary>A token whose encoding and signature were verified.</summary>
/// <param name="Header">The decoded header object.</param>
/// <param name="Claims">The decoded claims object; not yet semantically validated.</param>
public sealed record VerifiedToken(JsonElement Header, JsonElement Claims);

/// <summary>A token or credential failed verification.</summary>
public sealed class TokenRejectedException(string message) : Exception(message);
