using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Ares.Common.Game.Identity;
using Ares.Common.Logging;
using Ares.Common.Security;
using Ares.Host.Services;
using Ares.Host.Services.Content;
using Ares.Host.Services.Matchmaking;
using Ares.Host.Services.Persistence;
using Ares.Host.Services.Routing;
using Ares.Host.Services.Status;
using Ares.Host.Services.Telemetry;
using Ares.Structs.Backend;
using Ares.Structs.Backend.Telemetry;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;

namespace Ares.Host;

/// <summary>
/// Handles every request: logs its shape (never tokens, cookies, query values or bodies), serves
/// the public status, health and telemetry endpoints, dispatches backend routes and answers
/// anything unknown with HTTP 501.
/// </summary>
public sealed class RequestDispatcher(HostSettings settings, BackendRoutes routes, MatchmakingService matchmaking, JsonEventLog log, TimeProvider time)
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
            await WriteJsonAsync(context, 413, new ErrorResponse("request_too_large")).ConfigureAwait(false);
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
            log.Write(RequestEvent(request, method, path, body.Length, declared, backend.Json, form));
        }

        int status;
        object reply;
        switch (path)
        {
            case PublicStatus.Path when method == HttpMethods.Get:
                (status, reply) = (200, PublicStatusDocument());
                break;
            case ArchiveKeys.Path when method == HttpMethods.Get:
                (status, reply) = (200, settings.ArchiveKeys.ToDocument(settings.GameBuild));
                break;
            case "/health":
                (status, reply) = (200, new JsonObject { ["service"] = "ARES-Host" });
                break;
            case UiTelemetry.TransferBatchedPath:
                await TelemetryAsync(context, body).ConfigureAwait(false);
                return;
            default:
                (status, reply) = await DispatchAsync(backend, context.RequestAborted).ConfigureAwait(false);
                break;
        }

        log.Write(new JsonObject { ["response"] = $"{method} {path}", ["status"] = status });
        await WriteJsonAsync(context, status, reply).ConfigureAwait(false);
    }

    private async Task TelemetryAsync(HttpContext context, byte[] body)
    {
        foreach (JsonObject item in UiTelemetryLog.Project(body))
        {
            log.Write(new JsonObject { ["time"] = Now(), ["client_ui_event"] = item });
        }

        byte[] payload = GrpcWeb.EmptyOkResponse();
        context.Response.StatusCode = 200;
        context.Response.ContentType = GrpcWeb.ContentType;
        context.Response.ContentLength = payload.Length;
        await context.Response.Body.WriteAsync(payload, context.RequestAborted).ConfigureAwait(false);
    }

    private async Task<(int Status, object Reply)> DispatchAsync(BackendRequest request, CancellationToken cancellationToken)
    {
        BackendRoute? route = routes.Find(request.Method, request.Path);
        if (route is null)
        {
            return (501, new ErrorResponse("unimplemented_local_route"));
        }

        if (route.RequiresIdentity && settings.IdentityKey is not null && !IsLocalPlayer(request.Authorization))
        {
            return (401, new ErrorResponse("backend_identity_required"));
        }

        if (route.Delay > TimeSpan.Zero && settings.RouteDelayScale > 0)
        {
            await Task.Delay(route.Delay * settings.RouteDelayScale, time, cancellationToken).ConfigureAwait(false);
        }

        try
        {
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

    private bool IsLocalPlayer(string? authorization)
    {
        if (authorization is null || !authorization.StartsWith("Bearer ", StringComparison.Ordinal))
        {
            return false;
        }

        try
        {
            IdentityPrincipal principal = IdentityTokens.Validate(authorization[7..], settings.IdentityKey, settings.Issuer, time.GetUtcNow());
            return principal.UserId == LocalPlayer.TenancyUserId;
        }
        catch (TokenRejectedException)
        {
            return false;
        }
    }

    private PublicStatus PublicStatusDocument()
    {
        (bool active, int players) = matchmaking.CurrentRaid;
        return new PublicStatus(1, settings.ServerName, settings.Motd, settings.GameBuild,
            new RaidCounts(active ? 1 : 0, settings.RaidCapacity), players, matchmaking.HasAllocator);
    }

    private JsonObject RequestEvent(HttpRequest request, string method, string path, int bytes, long declared, JsonElement? json, IReadOnlyDictionary<string, string>? form)
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

    private static async Task WriteJsonAsync(HttpContext context, int status, object body)
    {
        byte[] payload = body is JsonNode node
            ? Encoding.UTF8.GetBytes(node.ToJsonString())
            : JsonSerializer.SerializeToUtf8Bytes(body, body.GetType(), BackendJson.Options);
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/json";
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
