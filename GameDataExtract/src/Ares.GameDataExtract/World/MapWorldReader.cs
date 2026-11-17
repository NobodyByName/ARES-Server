using Ares.GameDataExtract.Packages;
using Ares.Structs.Game.World;

namespace Ares.GameDataExtract.World;

/// <summary>
/// Reads a World Partition map's persistent world objects from its map package: the world (the package's only
/// top-level world export, named like the package), its persistent level, and in that level the level script actor
/// (the only actor of a class the map package defines) and the world data layers actor.
/// </summary>
internal static class MapWorldReader
{
    private static readonly PackageObjectIndex s_world = PackageObjectIndex.FromScriptPath(MapWorldClasses.World);
    private static readonly PackageObjectIndex s_level = PackageObjectIndex.FromScriptPath(MapWorldClasses.Level);
    private static readonly PackageObjectIndex s_worldDataLayers = PackageObjectIndex.FromScriptPath(MapWorldClasses.WorldDataLayers);

    /// <summary>The persistent world objects of map package <paramref name="map"/>.</summary>
    /// <exception cref="InvalidDataException">An object is missing or not unique, or the world is not named like the package.</exception>
    public static MapWorldObjects Read(CookedPackage map)
    {
        ZenExport world = Single(map, map.Exports.Where(export => export.Outer.IsNull && export.Class == s_world), "world");
        string leaf = map.Name[(map.Name.LastIndexOf('/') + 1)..];
        if (world.Name != leaf)
        {
            throw new InvalidDataException($"{map.Name}: the world is named {world.Name}, not like the package");
        }

        ZenExport level = Single(map, map.ChildrenOf(world.Index).Where(export => export.Class == s_level), "persistent level");
        ZenExport[] actors = [.. map.ChildrenOf(level.Index)];
        ZenExport script = Single(map, actors.Where(actor => actor.Class.Kind == PackageObjectKind.Export), "level script actor");
        ZenExport dataLayers = Single(map, actors.Where(actor => actor.Class == s_worldDataLayers), "world data layers actor");
        return new MapWorldObjects(world.Name, level.Name, script.Name, map.Header.ExportPath(script.Class.ExportIndex), dataLayers.Name);
    }

    private static ZenExport Single(CookedPackage map, IEnumerable<ZenExport> candidates, string what)
    {
        ZenExport[] found = [.. candidates.Take(2)];
        return found.Length == 1 ? found[0] : throw new InvalidDataException($"{map.Name} has {(found.Length == 0 ? "no" : "more than one")} {what}");
    }
}
