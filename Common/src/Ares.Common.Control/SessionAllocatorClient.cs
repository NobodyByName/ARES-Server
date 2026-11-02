using System.Net;
using System.Text.Json;
using Ares.Common.Endpoints;
using Ares.Common.Security;

namespace Ares.Common.Control;

/// <summary>
/// Host side of the allocation API. It only talks to an independently running Raid; it never
/// starts a server or a client. Replies are validated before Host relays anything to players.
/// </summary>
public sealed class SessionAllocatorClient : IDisposable
{
    private readonly HttpClient _http;
    private readonly string _token;

    /// <summary>Creates a client for the Raid control API at <paramref name="baseUrl"/>.</summary>
    /// <param name="baseUrl">Plain HTTP only on loopback; HTTPS otherwise. No path, query or credentials.</param>
    /// <param name="operatorToken">The operator token shared with Raid.</param>
    /// <param name="handler">Optional message handler (tests). Redirects are never followed.</param>
    /// <param name="timeout">Request timeout; 3 seconds by default.</param>
    /// <exception cref="ArgumentException">Invalid URL or token.</exception>
    public SessionAllocatorClient(Uri baseUrl, string operatorToken, HttpMessageHandler? handler = null, TimeSpan? timeout = null)
    {
        ValidateBaseUrl(baseUrl);
        _token = OperatorToken.Validate(operatorToken);
        _http = new HttpClient(handler ?? new SocketsHttpHandler { AllowAutoRedirect = false })
        {
            BaseAddress = new Uri(baseUrl.GetLeftPart(UriPartial.Authority)),
            Timeout = timeout ?? TimeSpan.FromSeconds(3),
        };
    }

    /// <summary>
    /// Reserves a raid for <paramref name="request"/>. Returns null when Raid has no capacity
    /// for it yet (HTTP 409).
    /// </summary>
    /// <exception cref="AllocationUnavailableException">Raid unreachable, rejected the request or replied inconsistently.</exception>
    public async Task<Allocation?> ReserveAsync(AllocationRequest request, CancellationToken cancellationToken = default)
    {
        Allocation? reply = await PostAsync(AllocationApi.ReservePath, request, cancellationToken).ConfigureAwait(false);
        if (reply is not null && (reply.TicketId != request.TicketId || reply.ScenarioName != request.ScenarioName
            || !reply.PartyMemberIds.SequenceEqual(request.PartyMemberIds)))
        {
            throw new AllocationUnavailableException("Session allocation does not match the ticket");
        }

        return reply;
    }

    /// <summary>Polls an existing reservation.</summary>
    /// <exception cref="AllocationExpiredException">Raid no longer knows the reservation (HTTP 404).</exception>
    /// <exception cref="AllocationUnavailableException">Raid unreachable or the reservation's identity changed.</exception>
    public async Task<Allocation> StatusAsync(Allocation allocation, CancellationToken cancellationToken = default)
    {
        Allocation? reply = await PostAsync(AllocationApi.StatusPath, new AllocationStatusRequest(allocation.MatchId), cancellationToken).ConfigureAwait(false);
        if (reply is null || reply.MatchId != allocation.MatchId || reply.TicketId != allocation.TicketId
            || reply.ScenarioName != allocation.ScenarioName || !reply.PartyMemberIds.SequenceEqual(allocation.PartyMemberIds))
        {
            throw new AllocationUnavailableException("Session allocation identity changed");
        }

        return reply.Gameserver.RoutingToken == allocation.Gameserver.RoutingToken
            ? reply
            : throw new AllocationUnavailableException("Session routing credential changed within an allocation");
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _http.Dispose();
    }

    private async Task<Allocation?> PostAsync<TRequest>(string path, TRequest body, CancellationToken cancellationToken)
    {
        HttpResponseMessage response;
        try
        {
            using HttpRequestMessage message = new(HttpMethod.Post, path);
            message.Content = JsonContent(body);
            message.Headers.TryAddWithoutValidation("Authorization", "Bearer " + _token);
            response = await _http.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception error) when (error is HttpRequestException or TaskCanceledException or IOException)
        {
            throw new AllocationUnavailableException("Session allocation service unavailable or incompatible");
        }

        using (response)
        {
            switch (response.StatusCode)
            {
                case HttpStatusCode.Conflict when path == AllocationApi.ReservePath:
                    return null;
                case HttpStatusCode.NotFound when path == AllocationApi.StatusPath:
                    throw new AllocationExpiredException("Session allocation no longer exists");
                default:
                    if (!response.IsSuccessStatusCode)
                    {
                        throw new AllocationUnavailableException("Session allocation service rejected the request");
                    }

                    break;
            }

            try
            {
                byte[] raw = await ReadLimitedAsync(response.Content, cancellationToken).ConfigureAwait(false);
                Allocation reply = JsonSerializer.Deserialize<Allocation>(raw, AllocationApi.JsonOptions)
                    ?? throw new JsonException("Empty allocation reply");
                Validate(reply);
                return reply;
            }
            catch (Exception error) when (error is JsonException or ArgumentException or InvalidDataException or IOException or TaskCanceledException)
            {
                throw new AllocationUnavailableException("Session allocation service unavailable or incompatible");
            }
        }
    }

    private static ByteArrayContent JsonContent<T>(T body)
    {
        ByteArrayContent content = new(JsonSerializer.SerializeToUtf8Bytes(body, AllocationApi.JsonOptions));
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
        return content;
    }

    private static async Task<byte[]> ReadLimitedAsync(HttpContent content, CancellationToken cancellationToken)
    {
        await using Stream stream = await content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        byte[] buffer = new byte[AllocationApi.MaximumBodyBytes + 1];
        int total = 0;
        int read;
        while (total < buffer.Length && (read = await stream.ReadAsync(buffer.AsMemory(total), cancellationToken).ConfigureAwait(false)) > 0)
        {
            total += read;
        }

        return total <= AllocationApi.MaximumBodyBytes
            ? buffer[..total]
            : throw new InvalidDataException("Oversized allocation response");
    }

    /// <summary>Checks a reply the way Host relies on it; see the allocation API notes.</summary>
    /// <exception cref="InvalidDataException">The reply is not a consistent allocation.</exception>
    public static void Validate(Allocation reply)
    {
        if (!IsCanonicalUuid(reply.MatchId) || !IsCanonicalUuid(reply.TicketId) || string.IsNullOrEmpty(reply.ScenarioName))
        {
            throw new InvalidDataException("Unsupported session allocation identity");
        }

        if (reply is not { State: AllocationStates.Waiting, ServerInitializationState: ServerInitializationStates.Initializing }
            and not { State: AllocationStates.Available, ServerInitializationState: ServerInitializationStates.Ready })
        {
            throw new InvalidDataException("Unsupported session allocation state");
        }

        if (reply.PartyMemberIds.Count == 0 || reply.PartyMemberIds.Any(id => id <= 0)
            || !double.IsFinite(reply.PollingInterval) || reply.PollingInterval is < 0.1 or > 30)
        {
            throw new InvalidDataException("Invalid allocation membership or polling interval");
        }

        GameServerEndpoint endpoint = reply.Gameserver;
        ConnectionHost.Validate(endpoint.Host);
        if (endpoint.Port is < 1 or > 65535)
        {
            throw new InvalidDataException("Invalid allocation endpoint");
        }

        if (endpoint.RoutingToken.Length > 0)
        {
            Span<byte> raw = stackalloc byte[18];
            if (endpoint.RoutingToken.Length != 24 || !Convert.TryFromBase64String(endpoint.RoutingToken, raw, out int written)
                || written != 16 || Convert.ToBase64String(raw[..16]) != endpoint.RoutingToken)
            {
                throw new InvalidDataException("Invalid allocation routing credential");
            }
        }

        if (endpoint.ProxyEnabled)
        {
            throw new InvalidDataException("Proxy session allocation is not implemented");
        }
    }

    /// <summary>True for a lowercase, hyphenated UUID string (the only form ARES issues).</summary>
    public static bool IsCanonicalUuid(string? value)
    {
        return value is not null && Guid.TryParseExact(value, "D", out Guid parsed) && parsed.ToString("D") == value;
    }

    private static void ValidateBaseUrl(Uri url)
    {
        if (!url.IsAbsoluteUri || url.Scheme is not ("http" or "https") || string.IsNullOrEmpty(url.Host)
            || url.UserInfo.Length > 0 || url.Query.Length > 0 || url.Fragment.Length > 0
            || url.AbsolutePath is not ("" or "/")
            || (url.Scheme == "http" && !Loopback.IsLoopbackHost(url.Host)))
        {
            throw new ArgumentException("Session control requires loopback HTTP or verified HTTPS", nameof(url));
        }
    }
}

/// <summary>The allocation service could not be used.</summary>
public class AllocationUnavailableException(string message) : Exception(message);

/// <summary>The reservation no longer exists (e.g. Raid restarted).</summary>
public sealed class AllocationExpiredException(string message) : AllocationUnavailableException(message);
