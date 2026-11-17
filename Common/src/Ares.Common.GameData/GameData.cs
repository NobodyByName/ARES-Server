using System.Collections.Immutable;
using Ares.Structs.Game;

namespace Ares.Common.GameData;

/// <summary>
/// The game data a server runs on, loaded once at startup into an immutable object graph: the item catalogue and menu
/// items, the world item list, loot and movement definitions, each map's data, and <see cref="GameNames"/> resolved
/// against them. Pass the instance to what needs it; there is no global one.
/// </summary>
public sealed class GameData
{
    private readonly Dictionary<string, MapData> _maps;

    private GameData(GameDataContent content, string? source)
    {
        ArgumentNullException.ThrowIfNull(content);
        Source = source;
        Items = new ItemCatalog(content);
        Menu = new MenuItemTables(content, Items);
        WorldItems = new WorldItemClasses(content.WorldItemList);
        Loot = new LootData(content);
        Movement = new MovementTuning(content);
        Maps = [.. content.Maps.Select(map => new MapData(map)).OrderBy(map => map.WorldName, StringComparer.Ordinal)];
        _maps = GameDataIndex.Unique(Maps.Select(map => (map.WorldName, map)), "map", StringComparer.Ordinal);
        Names = new ResolvedGameNames(this);
    }

    /// <summary>Full path of the game data directory the data was loaded from; null for data made in memory.</summary>
    public string? Source { get; }

    /// <summary>The usable-item catalogue and its class tables.</summary>
    public ItemCatalog Items { get; }

    /// <summary>The menu item tables.</summary>
    public MenuItemTables Menu { get; }

    /// <summary>The world item class list.</summary>
    public WorldItemClasses WorldItems { get; }

    /// <summary>The loot definitions.</summary>
    public LootData Loot { get; }

    /// <summary>The movement tuning.</summary>
    public MovementTuning Movement { get; }

    /// <summary>Every map's data, ordered by world name.</summary>
    public ImmutableArray<MapData> Maps { get; }

    /// <summary>The game names a server treats specially, resolved.</summary>
    public ResolvedGameNames Names { get; }

    /// <summary>Opens a game data directory, checks it and loads every table.</summary>
    /// <param name="directory">The configured game data directory.</param>
    /// <exception cref="GameDataException">
    /// The directory cannot be used (see <see cref="GameDataDirectory.Open"/>), a table is missing or malformed, the
    /// tables contradict each other, or a name of <see cref="GameNames"/> is missing; the message tells the operator why.
    /// </exception>
    public static GameData Load(string directory) => Load(GameDataDirectory.Open(directory));

    /// <summary>Loads every table of an opened game data directory.</summary>
    /// <exception cref="GameDataException">See <see cref="Load(string)"/>.</exception>
    public static GameData Load(GameDataDirectory directory)
    {
        ArgumentNullException.ThrowIfNull(directory);
        return new GameData(GameDataReader.Read(directory), directory.FullPath);
    }

    /// <summary>Checks and indexes tables made in memory, for example the tiny made-up data of a logic test.</summary>
    /// <exception cref="GameDataException">The tables contradict each other, or a name of <see cref="GameNames"/> is missing.</exception>
    public static GameData Create(GameDataContent content) => new(content, null);

    /// <summary>The data of a map.</summary>
    /// <param name="worldName">The map's world name, for example <see cref="GameNames.Maps.Dam"/>.</param>
    /// <exception cref="GameDataException">The game data has no such map.</exception>
    public MapData Map(string worldName) => _maps.TryGetValue(worldName, out MapData? map) ? map
        : throw new GameDataException($"The game data has no map '{worldName}'. Run the ARES-GameDataExtract of this ARES version again.");

    /// <summary>The data of a map, or null when the game data has no such map.</summary>
    /// <param name="worldName">The map's world name.</param>
    public MapData? FindMap(string worldName) => _maps.GetValueOrDefault(worldName);
}
