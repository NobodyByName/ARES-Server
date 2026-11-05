using Ares.Common.Control;
using Ares.Host.Services.Matchmaking;
using Ares.Host.Services.Persistence;

namespace Ares.Host;

/// <summary>Recovers inventory locks even when no game client is connected.</summary>
internal sealed class InventoryReservationRecovery(MatchmakingService matchmaking, TimeProvider time) : BackgroundService
{
    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        // Complete the first check before Host starts accepting requests and console commands.
        await ReconcileAsync(cancellationToken).ConfigureAwait(false);
        await base.StartAsync(cancellationToken).ConfigureAwait(false);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using PeriodicTimer timer = new(TimeSpan.FromSeconds(2), time);
        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
        {
            await ReconcileAsync(stoppingToken).ConfigureAwait(false);
        }
    }

    private async Task ReconcileAsync(CancellationToken cancellationToken)
    {
        try
        {
            await matchmaking.ReconcileInventoryAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception error) when (error is AllocationUnavailableException or PersistenceException or InvalidDataException)
        {
            // Host can start before Raid. Keep the lock until Raid confirms the old ticket is
            // gone, and retry after Raid and storage become usable; a Host restart alone proves nothing.
        }
    }
}
