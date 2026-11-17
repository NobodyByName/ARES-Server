using Ares.Common.Maps;
using Ares.Structs.Game.World;

namespace Ares.GameDataExtract.World;

/// <summary>How the map tables name a map's cells and their data layers.</summary>
internal static class MapCells
{
    /// <summary>Prefix of the generated cell packages of map package <paramref name="mapPackage"/>.</summary>
    private static string Prefix(string mapPackage) => $"{mapPackage}/{RuntimeCellMembers.GeneratedFolder}/";

    /// <summary>
    /// The cell's name in the tables: the last segment of a generated cell package of <paramref name="mapPackage"/>, empty
    /// for the map package itself (the persistent level).
    /// </summary>
    /// <exception cref="InvalidDataException">The cell is neither the map package nor one of its generated cell packages.</exception>
    public static string Name(MapCell cell, string mapPackage)
    {
        string prefix = Prefix(mapPackage);
        return cell.PackageName == mapPackage ? string.Empty
            : cell.PackageName.StartsWith(prefix, StringComparison.Ordinal) ? cell.PackageName[prefix.Length..]
            : throw new InvalidDataException($"{cell.PackageName} is not a cell of {mapPackage}");
    }

    /// <summary>The cell's data layer, or <see langword="null"/> for a base-map cell.</summary>
    /// <exception cref="InvalidDataException">The cell belongs to more than one data layer.</exception>
    public static string? DataLayer(MapCell cell)
    {
        return cell.DataLayers.Length <= 1
            ? cell.DataLayers.FirstOrDefault()
            : throw new InvalidDataException($"{cell.PackageName} belongs to {cell.DataLayers.Length} data layers");
    }
}
