using Ares.GameDataExtract.Packages;
using Ares.Structs.Game.World;

namespace Ares.GameDataExtract.World;

/// <summary>
/// The objects placed in a map package's persistent level (the persistent level itself, or a World Partition cell's
/// level): its actors, the exports whose outer is a level of the package's world, and their direct subobjects.
/// </summary>
internal static class PlacedActors
{
    private static readonly PackageObjectIndex s_level = PackageObjectIndex.FromScriptPath(MapWorldClasses.Level);

    /// <summary>The actors of <paramref name="package"/>'s levels, in export order.</summary>
    public static IEnumerable<ZenExport> Actors(CookedPackage package)
    {
        HashSet<int> levels = Levels(package);
        return package.Exports.Where(export => IsIn(export, levels));
    }

    /// <summary>The placed actor that <paramref name="export"/> is a direct subobject of, if it is one.</summary>
    public static ZenExport? ActorOf(CookedPackage package, ZenExport export, HashSet<int> levels)
    {
        if (export.Outer.Kind != PackageObjectKind.Export)
        {
            return null;
        }

        ZenExport outer = package.Exports[export.Outer.ExportIndex];
        return IsIn(outer, levels) ? outer : null;
    }

    /// <summary>Export indices of <paramref name="package"/>'s levels: exports of the level class whose outer is a top-level export (the world).</summary>
    public static HashSet<int> Levels(CookedPackage package)
    {
        return [.. package.Exports
            .Where(export => export.Class == s_level && export.Outer.Kind == PackageObjectKind.Export && package.Exports[export.Outer.ExportIndex].Outer.IsNull)
            .Select(export => export.Index)];
    }

    private static bool IsIn(ZenExport export, HashSet<int> levels) => export.Outer.Kind == PackageObjectKind.Export && levels.Contains(export.Outer.ExportIndex);
}
