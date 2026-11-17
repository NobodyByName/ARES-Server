namespace Ares.Common.GameData;

/// <summary>Indexes of game data rows.</summary>
internal static class GameDataIndex
{
    /// <summary>A dictionary of unique keys, in row order; a repeated key makes the data unusable.</summary>
    /// <param name="entries">Keys and values.</param>
    /// <param name="what">What a key names, for the error, for example <c>usable item</c>.</param>
    /// <param name="comparer">The key comparer, or null for the default one.</param>
    public static Dictionary<TKey, TValue> Unique<TKey, TValue>(IEnumerable<(TKey Key, TValue Value)> entries, string what, IEqualityComparer<TKey>? comparer = null)
        where TKey : notnull
    {
        Dictionary<TKey, TValue> index = new(comparer);
        foreach ((TKey key, TValue value) in entries)
        {
            if (!index.TryAdd(key, value))
            {
                throw new GameDataException($"The game data lists the {what} '{key}' twice. Run the ARES-GameDataExtract of this ARES version again.");
            }
        }

        return index;
    }
}
