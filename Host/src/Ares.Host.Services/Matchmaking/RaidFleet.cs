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
/// A new ticket joins a running raid that still takes parties, else starts a new server. A slot is
/// freed when its server exits, which it does once its raid is over. Every account shares one fleet,
/// so calls take turns.
/// </summary>
/// <param name="starter">Starts the servers.</param>
/// <param name="capacity">Most raids running at once.</param>
/// <param name="players">Most players in one raid; above 1, a new ticket waits for a starting raid instead of starting another.</param>
/// <param name="time">Clock for the startup timeout.</param>
public sealed class RaidFleet(IRaidStarter starter, int capacity, int players, TimeProvider time) : IDisposable
{
    /// <summary>How long a new server may take to answer its allocation API.</summary>
    private static readonly TimeSpan s_startupTimeout = TimeSpan.FromSeconds(30);

    private readonly Dictionary<int, Slot> _slots = [];
    private readonly SemaphoreSlim _lock = new(1, 1);

    /// <summary>
    /// Reserves a raid for <paramref name="request"/>: the ticket's own raid, a running raid that takes
    /// it, or a newly started server. Returns null while a server starts or while every slot is busy; the
    /// client keeps polling.
    /// </summary>
    /// <exception cref="AllocationUnavailableException">The server failed to start or rejected the request.</exception>
    public async Task<Allocation?> ReserveAsync(AllocationRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await ReserveLockedAsync(request, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>Polls a reservation.</summary>
    /// <exception cref="AllocationExpiredException">Its reservation or raid is over.</exception>
    /// <exception cref="AllocationUnavailableException">Its server is unreachable or replied inconsistently.</exception>
    public Task<Allocation> StatusAsync(Allocation allocation, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(allocation);
        return ReservedAsync(allocation.TicketId, item => item.MatchId == allocation.MatchId,
            client => client.StatusAsync(allocation, cancellationToken), cancellationToken);
    }

    /// <summary>Looks up a persisted reservation by its ticket.</summary>
    /// <exception cref="AllocationExpiredException">No running raid holds the ticket.</exception>
    /// <exception cref="AllocationUnavailableException">Its server is unreachable or the reservation differs.</exception>
    public Task<Allocation> StatusByTicketAsync(string ticketId, PlayerLoadout loadout, CancellationToken cancellationToken)
    {
        return ReservedAsync(ticketId, item => item.MatchId is not null,
            client => client.StatusByTicketAsync(ticketId, loadout, cancellationToken), cancellationToken);
    }

    /// <summary>Confirms to the ticket's raid that the players' results are saved, so it drops them. Nothing to confirm once the raid is gone.</summary>
    /// <exception cref="AllocationUnavailableException">Its server is unreachable or rejected the request.</exception>
    public async Task SettleAsync(Allocation allocation, IReadOnlyList<long> userIds, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(allocation);
        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            RemoveExited();
            (_, Slot? slot) = Find(item => item.Tickets.Contains(allocation.TicketId) && item.MatchId == allocation.MatchId);
            if (slot is not null)
            {
                await slot.Server.Allocator.SettleAsync(new AllocationSettleRequest(allocation.TicketId, allocation.MatchId, userIds), cancellationToken).ConfigureAwait(false);
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>Stops the server started for a cancelled ticket that has no raid yet.</summary>
    public async Task CancelAsync(string ticketId, CancellationToken cancellationToken)
    {
        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            (int index, Slot? slot) = Find(item => item.MatchId is null && item.Tickets.Contains(ticketId));
            if (slot is not null)
            {
                Stop(index);
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>Runs an admin command on every running raid, in slot order.</summary>
    /// <param name="command">The command.</param>
    /// <param name="cancellationToken">Cancels the calls.</param>
    /// <returns>Each raid's slot, HTTP status and reply; a raid that cannot be reached gives status 503 and <c>raid_unreachable</c>.</returns>
    public async Task<IReadOnlyList<(int Slot, int StatusCode, AdminReply Reply)>> AdminAsync(AdminCommand command, CancellationToken cancellationToken)
    {
        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            RemoveExited();
            List<(int, int, AdminReply)> replies = [];
            foreach ((int index, Slot slot) in _slots.OrderBy(pair => pair.Key))
            {
                try
                {
                    (int status, AdminReply reply) = await slot.Server.Allocator.AdminAsync(command, cancellationToken).ConfigureAwait(false);
                    replies.Add((index, status, reply));
                }
                catch (AllocationUnavailableException error)
                {
                    replies.Add((index, 503, new AdminReply([error.Message], "raid_unreachable")));
                }
            }

            return replies;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>Stops every server.</summary>
    public void Dispose()
    {
        _lock.Wait();
        try
        {
            foreach (int index in _slots.Keys.ToArray())
            {
                Stop(index);
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<Allocation?> ReserveLockedAsync(AllocationRequest request, CancellationToken cancellationToken)
    {
        RemoveExited();
        (int index, Slot? slot) = Find(item => item.Tickets.Contains(request.TicketId));
        if (slot is null)
        {
            if (await JoinAsync(request, cancellationToken).ConfigureAwait(false) is { } joined)
            {
                return joined;
            }

            if (_slots.Count >= capacity || (players > 1 && _slots.Values.Any(item => item.MatchId is null)))
            {
                return null;
            }

            index = Enumerable.Range(0, capacity).First(free => !_slots.ContainsKey(free));
            slot = new Slot(starter.Start(index), time.GetTimestamp());
            slot.Tickets.Add(request.TicketId);
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
            // The raid no longer holds this ticket; the next poll looks for another raid.
            Leave(index, slot, request.TicketId);
            return null;
        }

        slot.MatchId = allocation.MatchId;
        return allocation;
    }

    /// <summary>Offers the ticket to each running raid that still takes parties.</summary>
    private async Task<Allocation?> JoinAsync(AllocationRequest request, CancellationToken cancellationToken)
    {
        foreach (Slot slot in _slots.OrderBy(pair => pair.Key).Select(pair => pair.Value).Where(item => item.MatchId is not null && !item.Full))
        {
            Allocation? allocation;
            try
            {
                allocation = await slot.Server.Allocator.ReserveAsync(request, cancellationToken).ConfigureAwait(false);
            }
            catch (AllocationUnavailableException)
            {
                allocation = null;
            }

            if (allocation?.MatchId != slot.MatchId)
            {
                // Full, its fill window closed, or it failed this request: it takes no more parties.
                slot.Full = true;
                continue;
            }

            slot.Tickets.Add(request.TicketId);
            return allocation;
        }

        return null;
    }

    private async Task<Allocation> ReservedAsync(string ticketId, Func<Slot, bool> match, Func<SessionAllocatorClient, Task<Allocation>> status,
        CancellationToken cancellationToken)
    {
        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            RemoveExited();
            (int index, Slot? slot) = Find(item => item.Tickets.Contains(ticketId) && match(item));
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
                // This ticket's reservation is over; the server exits on its own once its raid is.
                Leave(index, slot, ticketId);
                throw;
            }
        }
        finally
        {
            _lock.Release();
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

    /// <summary>Forgets a ticket; a raid left without tickets is stopped.</summary>
    private void Leave(int index, Slot slot, string ticketId)
    {
        slot.Tickets.Remove(ticketId);
        if (slot.Tickets.Count == 0)
        {
            Stop(index);
        }
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

    private sealed class Slot(IRaidServer server, long started)
    {
        /// <summary>Tickets reserved in this raid.</summary>
        public HashSet<string> Tickets { get; } = [];

        public IRaidServer Server { get; } = server;

        public long Started { get; } = started;

        public string? MatchId { get; set; }

        /// <summary>True once the raid turned a party away; it is not offered more.</summary>
        public bool Full { get; set; }
    }
}
