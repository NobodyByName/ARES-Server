using System.Collections.Immutable;

namespace Ares.Common.GameData;

/// <summary>
/// The game's world item class list. The game state flags classes for the client to load by their index in it
/// (<c>PossibleWorldItemClassIndexBitFields</c>). Generated: <c>WorldItemList</c>.
/// </summary>
public sealed class WorldItemClasses
{
    /// <summary>Bits per bitfield word.</summary>
    public const int WordBits = 64;

    private readonly Dictionary<string, int> _indexes;

    internal WorldItemClasses(ImmutableArray<string> classes)
    {
        Classes = classes;
        _indexes = GameDataIndex.Unique(classes.Select((path, index) => (path, index)), "world item class", StringComparer.Ordinal);
    }

    /// <summary>Class paths, in list order: a class's index is its bit.</summary>
    public ImmutableArray<string> Classes { get; }

    /// <summary>Number of bitfield words the client allocates for the list.</summary>
    public int WordCount => (Classes.Length / WordBits) + 1;

    /// <summary>The index of a class in the list, or null when the list does not hold it.</summary>
    /// <param name="classPath">Class path (<c>/Game/Path/BP_Name.BP_Name_C</c>).</param>
    public int? IndexOf(string classPath) => _indexes.TryGetValue(classPath, out int index) ? index : null;
}
