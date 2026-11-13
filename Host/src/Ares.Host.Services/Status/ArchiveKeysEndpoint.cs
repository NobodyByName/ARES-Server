using System.Globalization;
using System.Text.Json.Serialization;
using Ares.Common.Archives;

namespace Ares.Host.Services.Status;

/// <summary>
/// Serves the operator's archive keys. ARES-Launcher needs them to read the player's own game files
/// and fetches them from <see cref="Path"/>.
/// </summary>
public static class ArchiveKeysEndpoint
{
    /// <summary>Path ARES-Launcher fetches the keys from.</summary>
    public const string Path = "/ares/v1/archive-keys";

    /// <summary>The document served at <see cref="Path"/>.</summary>
    public static ArchiveKeysDocument ToDocument(this ArchiveKeys keys, string gameBuild)
    {
        return new ArchiveKeysDocument(gameBuild, Convert.ToHexString(keys.PakKey),
            [.. keys.ContainerKey.Select(word => word.ToString("X8", CultureInfo.InvariantCulture))]);
    }
}

/// <summary>Reply of <see cref="ArchiveKeysEndpoint.Path"/>. ARES-Launcher keeps its own copy of this contract.</summary>
public sealed record ArchiveKeysDocument(
    [property: JsonPropertyName("game_build")] string GameBuild,
    [property: JsonPropertyName("pak_key")] string PakKey,
    [property: JsonPropertyName("container_key")] IReadOnlyList<string> ContainerKey);
