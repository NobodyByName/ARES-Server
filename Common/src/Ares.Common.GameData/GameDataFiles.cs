using System.Text.RegularExpressions;

namespace Ares.Common.GameData;

/// <summary>
/// Layout of an operator's game data directory, the folder ARES-GameDataExtract writes from the installed game:
/// <see cref="ManifestFileName"/> and the global tables at the root, each map's files under
/// <c>maps/&lt;map world name&gt;/</c>.
/// </summary>
/// <remarks>
/// File names in the manifest are relative to the directory, use <c>/</c> as the separator, and consist of segments of
/// ASCII letters, digits, <c>_</c>, <c>-</c> and <c>.</c> that do not start with <c>.</c> or <c>-</c>.
/// </remarks>
public static partial class GameDataFiles
{
    /// <summary>File name of the manifest, at the directory's root.</summary>
    public const string ManifestFileName = "manifest.json";

    /// <summary>File extension of JSON tables.</summary>
    public const string TableExtension = ".json";

    /// <summary>File extension of map-collision files (the ARES map-collision format of <c>Ares.Common.Maps</c>).</summary>
    public const string CollisionExtension = ".collision";

    /// <summary>Folder of the per-map folders.</summary>
    public const string MapsDirectory = "maps";

    /// <summary>Name of a file in a map's folder, <c>maps/&lt;map&gt;/&lt;file&gt;</c>.</summary>
    /// <param name="mapName">The map's world name, for example <c>TheDam_02_P</c>.</param>
    /// <param name="fileName">The file's name inside the map's folder.</param>
    public static string MapFile(string mapName, string fileName)
    {
        ArgumentException.ThrowIfNullOrEmpty(mapName);
        ArgumentException.ThrowIfNullOrEmpty(fileName);
        string name = $"{MapsDirectory}/{mapName}/{fileName}";
        return IsValidName(name) ? name : throw new ArgumentException($"'{name}' is not a valid game data file name.", nameof(fileName));
    }

    /// <summary>Name of a map's collision file, <c>maps/&lt;map&gt;/&lt;map&gt;.collision</c>.</summary>
    /// <param name="mapName">The map's world name, as in the collision's map name.</param>
    public static string CollisionPath(string mapName) => MapFile(mapName, mapName + CollisionExtension);

    /// <summary>Whether <paramref name="name"/> is a valid file name for the manifest (see the remarks).</summary>
    public static bool IsValidName(string name) => name != ManifestFileName && NamePattern().IsMatch(name);

    [GeneratedRegex(@"\A[A-Za-z0-9_][A-Za-z0-9_.-]*(?:/[A-Za-z0-9_][A-Za-z0-9_.-]*)*\z")]
    private static partial Regex NamePattern();
}
