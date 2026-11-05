namespace Ares.Host.Services;

/// <summary>
/// One async lock shared by party, matchmaking and menu inventory commits. Matchmaking reads
/// party readiness and freezes inventory as part of its own transitions.
/// </summary>
public sealed class BackendGate : IDisposable
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    /// <summary>Waits for the gate. Dispose the result to release it.</summary>
    public async Task<Releaser> EnterAsync(CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        return new Releaser(_semaphore);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _semaphore.Dispose();
    }

    /// <summary>Releases the gate when disposed.</summary>
    public readonly struct Releaser(SemaphoreSlim semaphore) : IDisposable
    {
        /// <inheritdoc/>
        public void Dispose()
        {
            semaphore.Release();
        }
    }
}

/// <summary>A status code and JSON body produced by a backend service.</summary>
/// <param name="StatusCode">HTTP status.</param>
/// <param name="Body">Object serialized with the backend JSON settings.</param>
public sealed record ServiceReply(int StatusCode, object Body)
{
    /// <summary>200 with <paramref name="body"/>.</summary>
    public static ServiceReply Ok(object body)
    {
        return new ServiceReply(200, body);
    }

    /// <summary>An error status with <c>{"error": code}</c>.</summary>
    public static ServiceReply Error(int statusCode, string code)
    {
        return new ServiceReply(statusCode, new Structs.Backend.ErrorResponse(code));
    }
}
