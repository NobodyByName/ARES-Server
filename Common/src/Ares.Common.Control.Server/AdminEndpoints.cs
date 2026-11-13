using System.Text.Json;
using Ares.Common.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Ares.Common.Control.Server;

/// <summary>Raid-side logic behind the admin API.</summary>
public interface IAdminService
{
    /// <summary>Runs an admin command.</summary>
    /// <param name="command">The command; its name and arguments are present but not yet checked.</param>
    /// <param name="cancellationToken">Cancelled when the caller goes away.</param>
    /// <returns>The reply and its HTTP status.</returns>
    Task<AdminResult> RunAsync(AdminCommand command, CancellationToken cancellationToken);
}

/// <summary>Outcome of an admin command: the reply and its HTTP status code.</summary>
/// <param name="StatusCode">HTTP status code.</param>
/// <param name="Reply">The reply.</param>
public sealed record AdminResult(int StatusCode, AdminReply Reply)
{
    /// <summary>A command that ran.</summary>
    /// <param name="lines">What it did.</param>
    /// <returns>The result.</returns>
    public static AdminResult Success(IReadOnlyList<string> lines) => new(StatusCodes.Status200OK, new AdminReply(lines));

    /// <summary>A refused command.</summary>
    /// <param name="statusCode">HTTP status code.</param>
    /// <param name="error">Machine-readable reason.</param>
    /// <param name="lines">Readable explanation.</param>
    /// <returns>The result.</returns>
    public static AdminResult Failure(int statusCode, string error, IReadOnlyList<string> lines) => new(statusCode, new AdminReply(lines, error));
}

/// <summary>Maps the operator-authenticated admin API onto an ASP.NET application.</summary>
public static class AdminEndpoints
{
    /// <summary>
    /// Adds <c>POST /admin/command</c>. Every call must carry <c>Authorization: Bearer {operatorToken}</c>; bodies are
    /// limited to 4 KiB of JSON.
    /// </summary>
    /// <param name="endpoints">The application.</param>
    /// <param name="service">Runs the commands.</param>
    /// <param name="operatorToken">The operator token shared with Host.</param>
    /// <returns>The application.</returns>
    public static IEndpointRouteBuilder MapAdminApi(this IEndpointRouteBuilder endpoints, IAdminService service, string operatorToken)
    {
        ArgumentNullException.ThrowIfNull(service);
        string token = OperatorToken.Validate(operatorToken);
        endpoints.MapPost(AdminApi.CommandPath, Task<IResult> (HttpContext context) => HandleAsync(context, token, service));
        return endpoints;
    }

    private static async Task<IResult> HandleAsync(HttpContext context, string token, IAdminService service)
    {
        if (!OperatorToken.Authorizes(context.Request.Headers.Authorization.ToString(), token))
        {
            return Reply(AdminResult.Failure(StatusCodes.Status401Unauthorized, AdminApi.AuthenticationRequired, ["Operator token required."]));
        }

        long? length = context.Request.ContentLength;
        if (length is null or < 0 or > AdminApi.MaximumRequestBytes)
        {
            return Reply(AdminResult.Failure(StatusCodes.Status413PayloadTooLarge, AdminApi.InvalidRequest, ["Request body missing or too large."]));
        }

        AdminCommand? command;
        try
        {
            byte[] body = new byte[length.Value];
            await context.Request.Body.ReadExactlyAsync(body, context.RequestAborted).ConfigureAwait(false);
            command = JsonSerializer.Deserialize<AdminCommand>(body, AllocationApi.JsonOptions);
        }
        catch (Exception error) when (error is JsonException or IOException or EndOfStreamException)
        {
            command = null;
        }

        if (command is null || command.Arguments.Count > AdminApi.MaximumArguments)
        {
            return Reply(AdminResult.Failure(StatusCodes.Status400BadRequest, AdminApi.InvalidRequest, ["Expected {\"name\": ..., \"arguments\": [...]}."]));
        }

        return Reply(await service.RunAsync(command, context.RequestAborted).ConfigureAwait(false));
    }

    private static IResult Reply(AdminResult result) => Results.Json(result.Reply, AllocationApi.JsonOptions, statusCode: result.StatusCode);
}
