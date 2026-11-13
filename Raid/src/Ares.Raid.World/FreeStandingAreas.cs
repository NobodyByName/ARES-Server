using Ares.Structs.Core.Geometry;
using Ares.Structs.Game.World;

namespace Ares.Raid.World;

/// <summary>A spawned container that does not stand on a prop socket, with the named area it is in.</summary>
/// <param name="Class">Short name of the container class.</param>
/// <param name="Area">The innermost named area containing the container, without the <c>ID_LOCATION_</c> prefix; null outside every named area.</param>
/// <param name="ParentArea">The next named area above <paramref name="Area"/>, formatted the same way; null when there is none.</param>
/// <param name="Location">World location.</param>
public sealed record FreeStandingContainer(string Class, string? Area, string? ParentArea, Vector3D Location);

/// <summary>Finds the named points of interest a location is in.</summary>
public sealed class FreeStandingAreas
{
    private const string NamePrefix = "ID_LOCATION_";
    private const string SocketPrefix = "BP_SocketContainer_";
    private const string SalvagePrefix = "BP_SalvageContainer_";
    private const string ClassSuffix = "_C";

    private readonly PointOfInterest[] _areas;
    private readonly Dictionary<string, PointOfInterest> _byActor;

    /// <summary>Keeps the points of interest to search.</summary>
    /// <param name="areas">The map's points of interest.</param>
    public FreeStandingAreas(IReadOnlyList<PointOfInterest> areas)
    {
        ArgumentNullException.ThrowIfNull(areas);
        _areas = [.. areas];
        _byActor = [];
        foreach (PointOfInterest area in _areas)
        {
            _byActor.TryAdd(area.Actor, area);
        }
    }

    /// <summary>Describes a container: its class, its area and the area above it.</summary>
    /// <param name="classPath">Container class path.</param>
    /// <param name="location">World location.</param>
    /// <returns>The description.</returns>
    public FreeStandingContainer Describe(string classPath, Vector3D location)
    {
        ArgumentNullException.ThrowIfNull(classPath);
        PointOfInterest? inner = null;
        int innerDepth = -1;
        foreach (PointOfInterest area in _areas)
        {
            if (area.Name is null || !LootVolumeRules.Contains(area.Hulls, location))
            {
                continue;
            }

            int depth = Chain(area).Count();
            if (depth > innerDepth)
            {
                (inner, innerDepth) = (area, depth);
            }
        }

        string? parent = inner is null ? null : Chain(inner).Skip(1).FirstOrDefault(area => area.Name is not null)?.Name;
        return new FreeStandingContainer(ShortName(classPath), Format(inner?.Name), Format(parent), location);
    }

    private static string? Format(string? name) => name is null ? null : name.StartsWith(NamePrefix, StringComparison.Ordinal) ? name[NamePrefix.Length..] : name;

    /// <summary>A container class without its path, socket or salvage prefix and class suffix.</summary>
    /// <param name="classPath">Class path.</param>
    /// <returns>The short name.</returns>
    internal static string ShortName(string classPath)
    {
        string name = classPath[(classPath.LastIndexOfAny(['.', '/']) + 1)..];
        foreach (string prefix in new[] { SocketPrefix, SalvagePrefix })
        {
            if (!name.StartsWith(prefix, StringComparison.Ordinal))
            {
                continue;
            }

            name = name[prefix.Length..];
            break;
        }

        return name.EndsWith(ClassSuffix, StringComparison.Ordinal) ? name[..^ClassSuffix.Length] : name;
    }

    // The area and its ancestors, innermost first; a loop in the data ends the chain.
    private IEnumerable<PointOfInterest> Chain(PointOfInterest start)
    {
        HashSet<string> seen = [];
        PointOfInterest? area = start;
        while (area is not null && seen.Add(area.Actor))
        {
            yield return area;
            area = area.Parent is { } parent && _byActor.TryGetValue(parent, out PointOfInterest? up) ? up : null;
        }
    }
}
