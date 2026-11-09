using Ares.Common.Control;
using Ares.Common.Game.Inventory;

namespace Ares.Host.Services.Matchmaking;

/// <summary>A started Raid server. It hosts one raid and exits once that raid is over.</summary>
public interface IRaidServer : IDisposable
{
    /// <summary>The server's allocation API.</summary>
    SessionAllocatorClient Allocator { get; }

    /// <summary>True once the server has exited.</summary>
    bool HasExited { get; }
}

/// <summary>Starts Raid servers.</summary>
public interface IRaidStarter
{
    /// <summary>Starts the Raid server for <paramref name="slot"/>; each slot has its own ports.</summary>
    /// <param name="slot">0 up to the raid capacity minus one.</param>
    /// <exception cref="AllocationUnavailableException">The server could not be started.</exception>
    IRaidServer Start(int slot);
}

/// <summary>
/// The Raid servers Host starts during matchmaking: one per raid, at most <c>capacity</c> at once.
/// A slot is freed when its server exits, which it does once its raid is over. Callers hold the
/// backend gate.
/// </summary>
/// <param name="starter">Starts the servers.</param>
/// <param name="capacity">Most raids running at once.</param>
/// <param name="time">Clock for the startup timeout.</param>
public sealed class RaidFleet(IRaidStarter starter, int capacity, TimeProvider time) : IDisposable
{
    /// <summary>How long a new server may take to answer its allocation API.</summary>
    private static readonly TimeSpan s_startupTimeout = TimeSpan.FromSeconds(30);

    private readonly Dictionary<int, Slot> _slots = [];

    /// <summary>
    /// Reserves a raid for <paramref name="request"/>, starting a Raid server for a new ticket. Returns
    /// null while the server starts or while every slot is busy; the client keeps polling.
    /// </summary>
    /// <exception cref="AllocationUnavailableException">The server failed to start or rejected the request.</exception>
    public async Task<Allocation?> ReserveAsync(AllocationRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        RemoveExited();
        (int index, Slot? slot) = Find(item => item.TicketId == request.TicketId);
        if (slot is null)
        {
            if (_slots.Count >= capacity)
            {
                return null;
            }

            index = Enumerable.Range(0, capacity).First(free => !_slots.ContainsKey(free));
            slot = new Slot(request.TicketId, starter.Start(index), time.GetTimestamp());
            _slots[index] = slot;
        }

        Allocation? allocation;
        try
        {
            allocation = await slot.Server.Allocator.ReserveAsync(request, cancellationToken).ConfigureAwait(false);
        }
        catch (AllocationUnavailableException error) when (error is not AllocationConflictException && slot.MatchId is null
            && !slot.Server.HasExited && time.GetElapsedTime(slot.Started) < s_startupTimeout)
        {
            return null;
        }
        catch (AllocationUnavailableException) when (slot.MatchId is null)
        {
            Stop(index);
            throw;
        }

        if (allocation is null)
        {
            // The server's raid is already over; the next poll starts a fresh one.
            Stop(index);
            return null;
        }

        slot.MatchId = allocation.MatchId;
        return allocation;
    }

    /// <summary>Polls a reservation.</summary>
    /// <exception cref="AllocationExpiredException">Its raid is over.</exception>
    /// <exception cref="AllocationUnavailableException">Its server is unreachable or replied inconsistently.</exception>
    public Task<Allocation> StatusAsync(Allocation allocation, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(allocation);
        return ReservedAsync(item => item.MatchId == allocation.MatchId, client => client.StatusAsync(allocation, cancellationToken));
    }

    /// <summary>Looks up a persisted reservation by its ticket.</summary>
    /// <exception cref="AllocationExpiredException">No running raid holds the ticket.</exception>
    /// <exception cref="AllocationUnavailableException">Its server is unreachable or the reservation differs.</exception>
    public Task<Allocation> StatusByTicketAsync(string ticketId, PlayerLoadout loadout, CancellationToken cancellationToken)
    {
        return ReservedAsync(item => item.MatchId is not null && item.TicketId == ticketId,
            client => client.StatusByTicketAsync(ticketId, loadout, cancellationToken));
    }

    /// <summary>Stops the server started for a cancelled ticket that has no raid yet.</summary>
    public void Cancel(string ticketId)
    {
        (int index, Slot? slot) = Find(item => item.TicketId == ticketId && item.MatchId is null);
        if (slot is not null)
        {
            Stop(index);
        }
    }

    /// <summary>Stops every server.</summary>
    public void Dispose()
    {
        foreach (int index in _slots.Keys.ToArray())
        {
            Stop(index);
        }
    }

    private async Task<Allocation> ReservedAsync(Func<Slot, bool> match, Func<SessionAllocatorClient, Task<Allocation>> status)
    {
        RemoveExited();
        (int index, Slot? slot) = Find(match);
        if (slot is null)
        {
            throw new AllocationExpiredException("The raid is over");
        }

        try
        {
            return await status(slot.Server.Allocator).ConfigureAwait(false);
        }
        catch (AllocationExpiredException)
        {
            // The raid is over; its server is about to exit on its own.
            Stop(index);
            throw;
        }
    }

    private (int Index, Slot? Slot) Find(Func<Slot, bool> match)
    {
        foreach ((int index, Slot slot) in _slots)
        {
            if (match(slot))
            {
                return (index, slot);
            }
        }

        return (-1, null);
    }

    private void RemoveExited()
    {
        foreach ((int index, Slot slot) in _slots.ToArray())
        {
            if (slot.Server.HasExited)
            {
                Stop(index);
            }
        }
    }

    private void Stop(int index)
    {
        if (_slots.Remove(index, out Slot? slot))
        {
            slot.Server.Dispose();
        }
    }

    private sealed class Slot(string ticketId, IRaidServer server, long started)
    {
        public string TicketId { get; } = ticketId;

        public IRaidServer Server { get; } = server;

        public long Started { get; } = started;

        public string? MatchId { get; set; }
    }
}
