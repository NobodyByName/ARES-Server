using Ares.Structs.Game.Inventory;

namespace Ares.Raid.World.Players;

/// <summary>
/// One player's reveal of the hidden items of the container they have open, and the items they have seen in each
/// container this raid. Opening a container hides the occupied slots whose items the player has not seen there, lowest
/// slot first; after the container's delay one slot at a time is revealed (two when the double reveal rolls), each
/// taking the reveal time. Both times scale with the player's reveal speed. Closing the container stops the reveal:
/// the items still in plain view count as seen, so a reopen reveals only the rest.
/// </summary>
internal sealed class LootReveal
{
    private Dictionary<int, HashSet<Guid>> _seen = [];
    private List<int> _queue = [];
    private int _container;
    private double _searchingAt;
    private double _nextAt;
    private double _interval;

    /// <summary>A copy for speculative packet processing.</summary>
    /// <returns>The copy.</returns>
    public LootReveal Copy()
    {
        LootReveal copy = (LootReveal)MemberwiseClone();
        copy._seen = _seen.ToDictionary(entry => entry.Key, entry => new HashSet<Guid>(entry.Value));
        copy._queue = [.. _queue];
        return copy;
    }

    /// <summary>Starts the reveal of a container the player opens.</summary>
    /// <param name="container">Container index.</param>
    /// <param name="items">Its slots.</param>
    /// <param name="now">Raid clock.</param>
    /// <param name="delaySeconds">Wait from the opening until the first slot starts to reveal.</param>
    /// <param name="durationMultiplier">The player's reveal speed: it multiplies the delay and each reveal.</param>
    /// <returns>The search status of each slot, or null when the player has seen every item in it.</returns>
    public IReadOnlyList<ItemSearchStatus>? Start(int container, IReadOnlyList<ContainerItem?> items, double now, double delaySeconds, double durationMultiplier)
    {
        HashSet<Guid> seen = Seen(container);
        _container = container;
        _queue = [.. Enumerable.Range(0, items.Count).Where(slot => items[slot] is { } item && !seen.Contains(item.Id))];
        _interval = ItemRevealTime.Default.Seconds * durationMultiplier;
        _searchingAt = now + (delaySeconds * durationMultiplier);
        _nextAt = _searchingAt + _interval;
        if (_queue.Count == 0)
        {
            return null;
        }

        ItemSearchStatus[] statuses = new ItemSearchStatus[items.Count];
        foreach (int slot in _queue)
        {
            statuses[slot] = ItemSearchStatus.Hidden;
        }

        statuses[_queue[0]] = ItemSearchStatus.HiddenSearching;
        return statuses;
    }

    /// <summary>The open container's sound: searching from the end of its delay until its last reveal.</summary>
    /// <param name="now">Raid clock.</param>
    /// <returns>The sound.</returns>
    public ItemContainerSoundState Sound(double now) =>
        _queue.Count > 0 && now >= _searchingAt ? ItemContainerSoundState.OpenedSearching : ItemContainerSoundState.Opened;

    /// <summary>Takes the next reveal once it is due; the revealed items count as seen.</summary>
    /// <param name="now">Raid clock.</param>
    /// <param name="items">The open container's slots now.</param>
    /// <param name="statuses">Their search statuses now (null when all are shown).</param>
    /// <param name="doubles">Rolls whether this reveal shows two items.</param>
    /// <returns>The slots to show and the slot revealed next, or null when no reveal is due.</returns>
    public (IReadOnlyList<int> Revealed, int? Searching)? Next(double now, IReadOnlyList<ContainerItem?> items, IReadOnlyList<ItemSearchStatus>? statuses, Func<bool> doubles)
    {
        // A slot the player emptied or filled is in plain view already.
        _queue.RemoveAll(slot => items[slot] is null || statuses?[slot] is null or ItemSearchStatus.Shown);
        if (_queue.Count == 0 || now < _nextAt)
        {
            return null;
        }

        int[] revealed = [.. _queue.Take(_queue.Count > 1 && doubles() ? 2 : 1)];
        _queue.RemoveRange(0, revealed.Length);
        HashSet<Guid> seen = Seen(_container);
        foreach (int slot in revealed)
        {
            seen.Add(items[slot]!.Id);
        }

        _nextAt += _interval;
        return (revealed, _queue.Count > 0 ? _queue[0] : null);
    }

    /// <summary>Stops the reveal as the container closes: the items in plain view count as seen.</summary>
    /// <param name="items">The open container's slots as the player leaves them.</param>
    /// <param name="statuses">Their search statuses (null when all are shown).</param>
    public void Stop(IReadOnlyList<ContainerItem?> items, IReadOnlyList<ItemSearchStatus>? statuses)
    {
        HashSet<Guid> seen = Seen(_container);
        for (int slot = 0; slot < items.Count; slot++)
        {
            if (items[slot] is { } item && (statuses is null || statuses[slot] == ItemSearchStatus.Shown))
            {
                seen.Add(item.Id);
            }
        }

        _queue.Clear();
    }

    private HashSet<Guid> Seen(int container)
    {
        if (!_seen.TryGetValue(container, out HashSet<Guid>? seen))
        {
            _seen[container] = seen = [];
        }

        return seen;
    }
}
