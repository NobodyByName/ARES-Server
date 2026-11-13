using Ares.Common.Maps;

namespace Ares.Raid.World.Collision;

/// <summary>Loads a map's collision from the operator's map data directory.</summary>
public static class MapCollisionLoader
{
    /// <summary>
    /// Reads the collision file of a map, or explains why there is none. Raids run without collision when it is
    /// missing or unusable, so problems are returned instead of thrown.
    /// </summary>
    /// <param name="directory">The configured map data directory, or null when none is configured.</param>
    /// <param name="mapName">The map's world name; the file name is derived from it.</param>
    /// <param name="problem">Why no collision was loaded, or null when it was.</param>
    /// <returns>The map's collision, or null.</returns>
    public static MapCollision? Load(string? directory, string mapName, out string? problem)
    {
        if (directory is null)
        {
            problem = "map-data-dir is not set; raids run without map collision. Write the map files with ARES-MapData and set map-data-dir.";
            return null;
        }

        string fullDirectory = Path.GetFullPath(directory);
        if (!Directory.Exists(fullDirectory))
        {
            problem = $"Map data directory not found: {fullDirectory}; raids run without map collision.";
            return null;
        }

        string path = Path.Combine(fullDirectory, MapDataFiles.CollisionFileName(mapName));
        if (!File.Exists(path))
        {
            problem = $"Map collision file not found: {path}; raids run without map collision. Write it with ARES-MapData.";
            return null;
        }

        try
        {
            using FileStream stream = File.OpenRead(path);
            MapCollision map = MapCollisionFile.Read(stream);
            problem = null;
            return map;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            problem = $"Map collision file {path} cannot be used: {error.Message} Raids run without map collision.";
            return null;
        }
    }
}
