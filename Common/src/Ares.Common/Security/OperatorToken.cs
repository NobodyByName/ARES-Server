using System.Security.Cryptography;
using System.Text;

namespace Ares.Common.Security;

/// <summary>
/// The shared secret an operator gives both Host and Raid to authenticate the allocation API.
/// It never belongs in a player's connection profile.
/// </summary>
public static class OperatorToken
{
    /// <summary>Minimum token length in ASCII characters.</summary>
    public const int MinimumLength = 32;

    /// <summary>Returns <paramref name="token"/> when it is long enough and ASCII, otherwise throws.</summary>
    /// <exception cref="ArgumentException">The token is too short or not ASCII.</exception>
    public static string Validate(string? token)
    {
        return token is not null && token.Length >= MinimumLength && Ascii.IsValid(token)
            ? token
            : throw new ArgumentException($"Operator token must contain at least {MinimumLength} ASCII characters", nameof(token));
    }

    /// <summary>Constant-time check that an Authorization header carries <c>Bearer {token}</c>.</summary>
    public static bool Authorizes(string? authorizationHeader, string token)
    {
        byte[] supplied = Encoding.UTF8.GetBytes(authorizationHeader ?? "");
        byte[] expected = Encoding.ASCII.GetBytes("Bearer " + token);
        return CryptographicOperations.FixedTimeEquals(supplied, expected);
    }
}
