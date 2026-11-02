namespace Ares.Common.Endpoints;

/// <summary>
/// Validates host names that end up inside a client travel URL (the advertised raid address).
/// IPv4 addresses and DNS names only: URL options, paths, ports and IPv6 are rejected.
/// </summary>
public static class ConnectionHost
{
    /// <summary>Returns <paramref name="value"/> when it is a valid connection host, otherwise throws.</summary>
    /// <exception cref="ArgumentException">The value is not an IPv4 address or DNS name.</exception>
    public static string Validate(string? value)
    {
        if (value is null || value.Length is < 1 or > 253)
        {
            throw new ArgumentException("Invalid advertised session host", nameof(value));
        }

        if (value.Any(c => !(char.IsAsciiLetterOrDigit(c) || c is '.' or '-')))
        {
            throw new ArgumentException("Advertised session host must be an IPv4 address or DNS name", nameof(value));
        }

        if (value[0] is '.' or '-' || value[^1] is '.' or '-')
        {
            throw new ArgumentException("Invalid advertised session host", nameof(value));
        }

        return value;
    }

    /// <summary>True when <paramref name="value"/> is a valid connection host.</summary>
    public static bool IsValid(string? value)
    {
        try
        {
            Validate(value);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}
