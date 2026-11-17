using System.Collections;

namespace Ares.Raid.World.Players;

/// <summary>A diagnostic log of at most a fixed number of entries, oldest first: a new entry drops the oldest once full.</summary>
/// <param name="capacity">Most entries kept.</param>
internal sealed class BoundedLog(int capacity) : IReadOnlyList<string>
{
    private readonly List<string> _entries = [];

    /// <summary>Entries kept.</summary>
    public int Count => _entries.Count;

    /// <summary>Whether the log holds as many entries as it keeps.</summary>
    public bool IsFull => _entries.Count >= capacity;

    /// <summary>An entry, oldest first.</summary>
    /// <param name="index">Place in the log.</param>
    public string this[int index] => _entries[index];

    /// <summary>Adds an entry, dropping the oldest when the log is full.</summary>
    /// <param name="entry">The entry.</param>
    public void Add(string entry)
    {
        if (_entries.Count == capacity)
        {
            _entries.RemoveAt(0);
        }

        _entries.Add(entry);
    }

    /// <summary>A copy for speculative packet processing.</summary>
    /// <returns>The copy.</returns>
    public BoundedLog Copy()
    {
        BoundedLog copy = new(capacity);
        copy._entries.AddRange(_entries);
        return copy;
    }

    /// <inheritdoc />
    public IEnumerator<string> GetEnumerator() => _entries.GetEnumerator();

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
