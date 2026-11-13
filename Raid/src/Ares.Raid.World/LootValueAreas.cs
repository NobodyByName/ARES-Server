using Ares.Structs.Core.Geometry;
using Ares.Structs.Game.World;

namespace Ares.Raid.World;

/// <summary>Whether a point lies inside a map volume made of convex parts.</summary>
public static class LootVolumeRules
{
    /// <summary>Whether a point is inside any of the convex parts; a point with a coordinate that is not finite never is.</summary>
    /// <param name="hulls">The volume's convex parts.</param>
    /// <param name="point">World location.</param>
    /// <returns>True when the point is inside a part.</returns>
    public static bool Contains(IReadOnlyList<LootVolumeHull> hulls, Vector3D point)
    {
        ArgumentNullException.ThrowIfNull(hulls);
        return double.IsFinite(point.X) && double.IsFinite(point.Y) && double.IsFinite(point.Z)
            && hulls.Any(hull => hull.Planes.All(plane => (plane.Normal.X * point.X) + (plane.Normal.Y * point.Y) + (plane.Normal.Z * point.Z) <= plane.Distance));
    }
}

/// <summary>The areas of a map that show a loot value, and the loot value at a point.</summary>
public sealed class LootValueAreas
{
    private readonly PointOfInterest[] _areas;

    /// <summary>Keeps the areas with a loot value, the highest value first.</summary>
    /// <param name="areas">The map's points of interest.</param>
    public LootValueAreas(IReadOnlyList<PointOfInterest> areas)
    {
        ArgumentNullException.ThrowIfNull(areas);
        _areas = [.. areas.Where(area => area.LootValue != PointOfInterestLootValue.Unknown).OrderByDescending(area => area.LootValue)];
    }

    /// <summary>The loot value at a point: the highest among the areas whose volume contains it.</summary>
    /// <param name="point">World location.</param>
    /// <returns>The loot value, <see cref="PointOfInterestLootValue.Unknown"/> outside every area with one.</returns>
    public PointOfInterestLootValue At(Vector3D point)
    {
        return _areas.FirstOrDefault(area => LootVolumeRules.Contains(area.Hulls, point))?.LootValue ?? PointOfInterestLootValue.Unknown;
    }
}
