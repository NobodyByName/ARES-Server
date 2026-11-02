using System.Net;

namespace Ares.Common.Endpoints;

/// <summary>Loopback checks used to keep development-only listeners and plain HTTP local.</summary>
public static class Loopback
{
    /// <summary>
    /// True for loopback IP literals and for <c>localhost</c>. Other names are not resolved,
    /// so they count as non-loopback.
    /// </summary>
    public static bool IsLoopbackHost(string? host)
    {
        if (string.IsNullOrEmpty(host))
        {
            return false;
        }

        string trimmed = host.StartsWith('[') && host.EndsWith(']') ? host[1..^1] : host;
        return IPAddress.TryParse(trimmed, out IPAddress? address)
            ? IPAddress.IsLoopback(address)
            : string.Equals(trimmed, "localhost", StringComparison.OrdinalIgnoreCase);
    }
}
