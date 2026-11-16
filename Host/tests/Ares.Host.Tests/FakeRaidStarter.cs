using Ares.Common.Control;
using Ares.Host.Services.Matchmaking;

namespace Ares.Host.Tests;

/// <summary>
/// Hands Host or a <see cref="RaidFleet"/> in-memory <see cref="FakeRaidServer"/>s instead of starting processes.
/// With one server every slot shares it and it keeps its raids across starts. With several, slot N gets server N
/// and starting the slot resets that server, as a fresh process hosts no raid yet.
/// </summary>
internal sealed class FakeRaidStarter(params FakeRaidServer[] servers) : IRaidStarter, IAsyncDisposable
{
    public IReadOnlyList<FakeRaidServer> Servers => servers;

    /// <summary>Sets <see cref="FakeRaidServer.Ready"/> on every server.</summary>
    /// <param name="ready">Whether the servers report ready.</param>
    public void SetReady(bool ready)
    {
        foreach (FakeRaidServer server in servers)
        {
            server.Ready = ready;
        }
    }

    /// <summary>Sets <see cref="FakeRaidServer.MaxPlayers"/> on every server.</summary>
    /// <param name="maxPlayers">Most players in one raid.</param>
    public void SetMaxPlayers(int maxPlayers)
    {
        foreach (FakeRaidServer server in servers)
        {
            server.MaxPlayers = maxPlayers;
        }
    }

    /// <summary>While true, every started server reports it has exited.</summary>
    public bool Exited { get; set; }

    /// <summary>The slots started, in order.</summary>
    public List<int> Started { get; } = [];

    /// <summary>How many started servers were stopped.</summary>
    public int Stopped { get; private set; }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        foreach (FakeRaidServer server in servers)
        {
            await server.StartAsync(cancellationToken);
        }
    }

    public IRaidServer Start(int slot)
    {
        Started.Add(slot);
        FakeRaidServer server = servers.Length == 1 ? servers[0] : servers[slot];
        if (servers.Length > 1)
        {
            server.Forget();
        }

        return new Server(this, server.CreateAllocator());
    }

    public async ValueTask DisposeAsync()
    {
        foreach (FakeRaidServer server in servers)
        {
            await server.DisposeAsync();
        }
    }

    private sealed class Server(FakeRaidStarter owner, SessionAllocatorClient allocator) : IRaidServer
    {
        public SessionAllocatorClient Allocator => allocator;

        public bool HasExited => owner.Exited;

        public void Dispose()
        {
            owner.Stopped++;
            allocator.Dispose();
        }
    }
}
