using System.Text.Json;
using Ares.Common.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Ares.Common.Control.Server;

/// <summary>Raid-side reservation logic behind the allocation API.</summary>
public interface IAllocationService
{
    /// <summary>Reserves (or re-reads the existing reservation for) a ticket.</summary>
    AllocationResult Reserve(AllocationRequest request);

    /// <summary>Reads a reservation by match id.</summary>
    AllocationResult Status(AllocationStatusRequest request);
}

/// <summary>Outcome of an allocation call: an allocation, or an HTTP error status with a reason.</summary>
public sealed record AllocationResult
{
    private AllocationResult(int statusCode, Allocation? allocation, string? error)
    {
        StatusCode = statusCode;
        Allocation = allocation;
        Error = error;
    }

    /// <summary>HTTP status code of the reply.</summary>
    public int StatusCode { get; }

    /// <summary>The allocation, on success.</summary>
    public Allocation? Allocation { get; }

    /// <summary>The machine-readable error, on failure.</summary>
    public string? Error { get; }

    /// <summary>A successful reply.</summary>
    public static AllocationResult Success(Allocation allocation)
    {
        return new AllocationResult(StatusCodes.Status200OK, allocation, null);
    }

    /// <summary>A rejected call, e.g. 409 <c>session_capacity_reached</c>.</summary>
    public static AllocationResult Failure(int statusCode, string error)
    {
        return new AllocationResult(statusCode, null, error);
    }
}

/// <summary>Maps the operator-authenticated allocation API onto an ASP.NET application.</summary>
public static class AllocationEndpoints
{
    /// <summary>
    /// Adds <c>POST /allocations/reserve</c> and <c>POST /allocations/status</c>. Every call must carry
    /// <c>Authorization: Bearer {operatorToken}</c>; bodies are limited to 256 KiB of JSON.
    /// </summary>
    public static IEndpointRouteBuilder MapAllocationApi(this IEndpointRouteBuilder endpoints, IAllocationService service, string operatorToken)
    {
        string token = OperatorToken.Validate(operatorToken);
        endpoints.MapPost(AllocationApi.ReservePath, Task<IResult> (HttpContext context) =>
            HandleAsync<AllocationRequest>(context, token, service.Reserve));
        endpoints.MapPost(AllocationApi.StatusPath, Task<IResult> (HttpContext context) =>
            HandleAsync<AllocationStatusRequest>(context, token, service.Status));
        return endpoints;
    }

    private static async Task<IResult> HandleAsync<TRequest>(HttpContext context, string token, Func<TRequest, AllocationResult> action)
    {
        if (!OperatorToken.Authorizes(context.Request.Headers.Authorization.ToString(), token))
        {
            return Reply(StatusCodes.Status401Unauthorized, "operator_authentication_required");
        }

        long? length = context.Request.ContentLength;
        if (length is null or < 0 or > AllocationApi.MaximumBodyBytes)
        {
            return Reply(StatusCodes.Status413PayloadTooLarge, "invalid_allocation_request_size");
        }

        TRequest? request;
        try
        {
            byte[] body = new byte[length.Value];
            await context.Request.Body.ReadExactlyAsync(body, context.RequestAborted).ConfigureAwait(false);
            request = JsonSerializer.Deserialize<TRequest>(body, AllocationApi.JsonOptions);
        }
        catch (Exception error) when (error is JsonException or IOException or EndOfStreamException)
        {
            request = default;
        }

        if (request is null)
        {
            return Reply(StatusCodes.Status400BadRequest, "invalid_allocation_request");
        }

        AllocationResult result = action(request);
        return result.Allocation is not null
            ? Results.Json(result.Allocation, AllocationApi.JsonOptions, statusCode: result.StatusCode)
            : Reply(result.StatusCode, result.Error ?? "allocation_failed");
    }

    private static IResult Reply(int statusCode, string error)
    {
        return Results.Json(new AllocationError(error), AllocationApi.JsonOptions, statusCode: statusCode);
    }
}
