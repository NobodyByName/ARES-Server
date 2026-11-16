using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Ares.Common.Logging;
using Ares.Host.Services;
using Ares.Host.Services.Identity;
using Ares.Host.Services.Persistence;
using Ares.Host.Services.Players;
using Ares.Host.Services.Routing;
using Ares.Host.Services.Status;
using Ares.Structs.Backend;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;

namespace Ares.Host;

/// <summary>
/// Handles every request: logs request metadata, dispatches it through <see cref="BackendRoutes"/> and
/// answers anything unknown with HTTP 501. Raw (non-JSON) replies are sent without a response log entry.
/// </summary>
public sealed class RequestDispatcher(HostSettings settings, BackendRoutes routes, IdentityService identity, PlayerRegistry players,
    JsonEventLog log, TimeProvider time)
{
    /// <summary>Largest accepted request body.</summary>
    public const int MaximumBodyBytes = 1048576;

    /// <summary>Handles one request.</summary>
    public async Task HandleAsync(HttpContext context)
    {
        HttpRequest request = context.Request;
        string method = request.Method;
        string path = request.Path.Value ?? "/";
        context.Response.Headers.Server = "ARES-Host";

        long declared = request.ContentLength ?? 0;
        byte[]? body = await ReadBodyAsync(request, context.RequestAborted).ConfigureAwait(false);
        if (body is null)
        {
            log.Write(new JsonObject { ["http_error"] = 413, ["method"] = method, ["path"] = path });
            await WriteAsync(context, 413, new ErrorResponse("request_too_large")).ConfigureAwait(false);
            return;
        }

        string contentType = request.ContentType ?? "";
        Dictionary<string, string>? form = null;
        BackendRequest backend = new(method, path, request.Headers.Authorization.ToString() is { Length: > 0 } auth ? auth : null, body, null);
        if (backend.Json is null && contentType.StartsWith("application/x-www-form-urlencoded", StringComparison.Ordinal))
        {
            form = ParseForm(body);
            backend = new BackendRequest(method, path, backend.Authorization, body, form);
        }

        if (path != PublicStatus.Path)
        {
            log.Write(RequestEvent(request, method, path, body.Length, declared, backend.Json, form, backend.Authorization));
        }

        (int status, object reply) = await DispatchAsync(backend, context.RequestAborted).ConfigureAwait(false);
        if (reply is not RawReply)
        {
            log.Write(new JsonObject { ["response"] = $"{method} {path}", ["status"] = status });
        }

        await WriteAsync(context, status, reply).ConfigureAwait(false);
    }

    private async Task<(int Status, object Reply)> DispatchAsync(BackendRequest request, CancellationToken cancellationToken)
    {
        BackendRoute? route = routes.Find(request.Method, request.Path);
        if (route is null)
        {
            return (501, new ErrorResponse("unimplemented_local_route"));
        }

        try
        {
            if (route.RequiresIdentity)
            {
                request.Player = identity.Authenticate(request.Authorization) is { } userId ? players.SignedIn(userId) : null;
                if (request.Player is null)
                {
                    return (401, new ErrorResponse("backend_identity_required"));
                }
            }

            if (route.Delay > TimeSpan.Zero && settings.RouteDelayScale > 0)
            {
                await Task.Delay(route.Delay * settings.RouteDelayScale, time, cancellationToken).ConfigureAwait(false);
            }

            ServiceReply reply = await route.Handler(request, cancellationToken).ConfigureAwait(false);
            return (reply.StatusCode, reply.Body);
        }
        catch (PersistenceConflictException)
        {
            return (409, new ErrorResponse("player_data_changed"));
        }
        catch (PersistenceException error)
        {
            log.Write("database_error", error.Message);
            return (503, new ErrorResponse("database_unavailable"));
        }
        catch (InvalidDataException)
        {
            log.Write("database_error", "Invalid persisted player data");
            return (503, new ErrorResponse("invalid_player_data"));
        }
    }

    private JsonObject RequestEvent(HttpRequest request, string method, string path, int bytes, long declared, JsonElement? json,
        IReadOnlyDictionary<string, string>? form, string? authorization)
    {
        string[] queryKeys =
        [
            .. QueryHelpers.ParseQuery(request.QueryString.Value)
                .Where(pair => pair.Value.Any(item => !string.IsNullOrEmpty(item)))
                .Select(pair => pair.Key)
                .Order(StringComparer.Ordinal),
        ];
        JsonArray bodyKeys = [];
        JsonObject bodyTypes = [];
        JsonObject stringLengths = [];
        JsonObject? nestedKeys = null;
        if (json is { } value)
        {
            nestedKeys = [];
            foreach (JsonProperty property in value.EnumerateObject().OrderBy(property => property.Name, StringComparer.Ordinal))
            {
                bodyKeys.Add(property.Name);
                bodyTypes[property.Name] = JsonTypeName(property.Value);
                if (property.Value.ValueKind == JsonValueKind.String && property.Value.GetString() is { } text)
                {
                    stringLengths[property.Name] = text.Length;
                }

                if (property.Value.ValueKind == JsonValueKind.Object)
                {
                    nestedKeys[property.Name] = new JsonArray(property.Value.EnumerateObject()
                        .Select(child => child.Name).Order(StringComparer.Ordinal).Select(name => (JsonNode?)name).ToArray());
                }
            }
        }
        else if (form is not null)
        {
            nestedKeys = [];
            foreach (string key in form.Keys.Order(StringComparer.Ordinal))
            {
                bodyKeys.Add(key);
                bodyTypes[key] = "string";
            }
        }

        JsonObject entry = new()
        {
            ["time"] = Now(),
            ["method"] = method,
            ["path"] = path,
            ["transport"] = request.Protocol == "HTTP/2" ? "h2" : "http/1.1",
            ["host"] = request.Headers.Host.ToString(),
            ["query_keys"] = new JsonArray(queryKeys.Select(key => (JsonNode?)key).ToArray()),
            ["body_keys"] = bodyKeys,
            ["body_types"] = bodyTypes,
            ["string_lengths"] = stringLengths,
            ["content_type"] = request.ContentType,
            ["auth_scheme"] = authorization?.Split(' ', 2)[0],
            ["bytes"] = bytes,
            ["declared_bytes"] = declared,
        };
        if (nestedKeys is not null)
        {
            entry["nested_keys"] = nestedKeys;
        }

        return entry;
    }

    private static string JsonTypeName(JsonElement value)
    {
        return value.ValueKind switch
        {
            JsonValueKind.String => "string",
            JsonValueKind.Number => value.GetRawText().IndexOfAny(['.', 'e', 'E']) >= 0 ? "number" : "integer",
            JsonValueKind.True or JsonValueKind.False => "boolean",
            JsonValueKind.Object => "object",
            JsonValueKind.Array => "array",
            _ => "null",
        };
    }

    private double Now()
    {
        return time.GetUtcNow().ToUnixTimeMilliseconds() / 1000.0;
    }

    private static Dictionary<string, string> ParseForm(byte[] body)
    {
        Dictionary<string, string> form = new(StringComparer.Ordinal);
        foreach ((string key, StringValues values) in QueryHelpers.ParseQuery(Encoding.UTF8.GetString(body)))
        {
            form[key] = values.LastOrDefault() ?? "";
        }

        return form;
    }

    private static async Task<byte[]?> ReadBodyAsync(HttpRequest request, CancellationToken cancellationToken)
    {
        if (request.ContentLength > MaximumBodyBytes)
        {
            return null;
        }

        using MemoryStream buffer = new();
        byte[] chunk = new byte[16384];
        int read;
        while ((read = await request.Body.ReadAsync(chunk, cancellationToken).ConfigureAwait(false)) > 0)
        {
            if (buffer.Length + read > MaximumBodyBytes)
            {
                return null;
            }

            buffer.Write(chunk, 0, read);
        }

        return buffer.ToArray();
    }

    private static async Task WriteAsync(HttpContext context, int status, object body)
    {
        (byte[] payload, string contentType) = body switch
        {
            RawReply raw => (raw.Payload, raw.ContentType),
            JsonNode node => (Encoding.UTF8.GetBytes(node.ToJsonString()), "application/json"),
            _ => (JsonSerializer.SerializeToUtf8Bytes(body, body.GetType(), BackendJson.Options), "application/json"),
        };
        context.Response.StatusCode = status;
        context.Response.ContentType = contentType;
        context.Response.ContentLength = payload.Length;
        try
        {
            await context.Response.Body.WriteAsync(payload, context.RequestAborted).ConfigureAwait(false);
        }
        catch (Exception error) when (error is IOException or OperationCanceledException)
        {
            // Client went away.
        }
    }
}
