using System.Diagnostics;
using System.Text.Json.Nodes;

namespace Ares.Common.GameData;

/// <summary>How Host and Raid load the operator's game data when they start: they refuse to start without it.</summary>
public static class GameDataStartup
{
    /// <summary>Loads the configured game data directory.</summary>
    /// <param name="directory">The <c>game-data-dir</c> setting, or null when it is not set.</param>
    /// <param name="loaded">What was loaded, as fields of the server's startup event.</param>
    /// <exception cref="GameDataException">
    /// <c>game-data-dir</c> is not set, or the data cannot be used; the message tells the operator why and what to do.
    /// </exception>
    public static GameDataSet Load(string? directory, out JsonObject loaded)
    {
        if (directory is null)
        {
            throw new GameDataException("game-data-dir is not set. ARES needs the game data that ARES-GameDataExtract writes from an installed copy of "
                + "the supported game build: run ARES-GameDataExtract extract --game <game folder> --keys <archive keys file> --toc-key <TOC key file> "
                + "--out <folder>, then set game-data-dir to that folder.");
        }

        long started = Stopwatch.GetTimestamp();
        GameDataDirectory opened = GameDataDirectory.Open(directory);
        GameDataSet data = GameDataSet.Load(opened);
        loaded = new JsonObject
        {
            ["game_data"] = "loaded",
            ["directory"] = opened.FullPath,
            ["game_build"] = opened.Manifest.GameBuildId,
            ["extractor_version"] = opened.Manifest.ExtractorVersion,
            ["items"] = data.Items.Count,
            ["maps"] = string.Join(", ", data.Maps.Select(map => map.WorldName)),
            ["load_ms"] = (long)Stopwatch.GetElapsedTime(started).TotalMilliseconds,
        };
        return data;
    }
}
