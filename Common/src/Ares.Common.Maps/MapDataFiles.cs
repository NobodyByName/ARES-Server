namespace Ares.Common.Maps;

/// <summary>Names of the files the map tool writes into an operator's map data directory.</summary>
public static class MapDataFiles
{
    /// <summary>File extension of map-collision files (<see cref="MapCollisionFile"/>).</summary>
    public const string CollisionExtension = ".collision";

    /// <summary>File name of a map's collision, from the map's world name.</summary>
    /// <param name="mapName">The map's world name, as in <see cref="MapCollision.MapName"/>.</param>
    public static string CollisionFileName(string mapName)
    {
        ArgumentException.ThrowIfNullOrEmpty(mapName);
        return mapName + CollisionExtension;
    }
}
