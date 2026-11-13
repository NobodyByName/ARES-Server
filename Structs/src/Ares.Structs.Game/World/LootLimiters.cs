using Ares.Structs.Core.Geometry;

namespace Ares.Structs.Game.World;

/// <summary>Which points a loot placement limiter covers.</summary>
public enum LootLimiterShape
{
    /// <summary>No point.</summary>
    None,

    /// <summary>Points within <see cref="LootLimiter.Radius"/> of <see cref="LootLimiter.Location"/>.</summary>
    Sphere,

    /// <summary>
    /// Points inside a cube centred on <see cref="LootLimiter.Location"/> and turned by
    /// <see cref="LootLimiter.Rotation"/>, reaching <see cref="LootLimiter.Radius"/> / 2 from the centre along each of
    /// its axes.
    /// </summary>
    Box,

    /// <summary>Points inside any of <see cref="LootLimiter.Volumes"/>.</summary>
    Volumes,
}

/// <summary>The cap a limiter puts on the placements of one gameplay tag.</summary>
/// <param name="Tag">
/// Gameplay tag. A placement counts when one of its socket tags is this tag or a tag below it
/// (<c>Socket.LootContainer.Tech</c> covers <c>Socket.LootContainer.Tech.Server.Door</c>); tag names compare without
/// case.
/// </param>
/// <param name="Unlimited">True when the entry caps nothing.</param>
/// <param name="Limit">
/// Placements the limiter allows for the tag: once that many have spawned on its sockets of the tag, no more spawn
/// there.
/// </param>
public sealed record LootLimit(string Tag, bool Unlimited, int Limit);

/// <summary>A bounding plane in world space: points with <c>Normal · point &lt;= Distance</c> are on its inner side.</summary>
/// <param name="Normal">Unit normal, pointing out of the hull.</param>
/// <param name="Distance">Distance of the plane from the origin along <paramref name="Normal"/>.</param>
public readonly record struct LootVolumePlane(Vector3D Normal, double Distance);

/// <summary>A convex part of a volume: a point is inside when it is on the inner side of every plane.</summary>
/// <param name="Planes">The hull's bounding planes.</param>
public sealed record LootVolumeHull(IReadOnlyList<LootVolumePlane> Planes);

/// <summary>A volume placed in the map's persistent level that a limiter covers.</summary>
/// <param name="Actor">The volume actor.</param>
/// <param name="Hulls">Its collision parts in world space; a point inside any part is inside the volume.</param>
public sealed record LootLimiterVolume(string Actor, IReadOnlyList<LootVolumeHull> Hulls);

/// <summary>
/// A loot placement limiter: an area of a map that caps, per gameplay tag, how many placements spawn on the sockets
/// inside it.
/// </summary>
/// <param name="Cell">
/// Generated World Partition cell that places the limiter (the last segment of its package name), or empty for the
/// map's persistent level.
/// </param>
/// <param name="Actor">The limiter actor.</param>
/// <param name="Name">The name the map gives the limiter (usually the area it covers).</param>
/// <param name="DataLayer">Data layer that streams the limiter, or null when it is always present.</param>
/// <param name="Shape">Which points the limiter covers.</param>
/// <param name="Location">World location of the covered area's centre (sphere and box shapes).</param>
/// <param name="Rotation">World rotation of the covered box, in degrees.</param>
/// <param name="Radius">Sphere radius, or twice the box's half extent.</param>
/// <param name="Volumes">Volumes the limiter covers (volume shape).</param>
/// <param name="Limits">Caps per gameplay tag; every matching entry that is not unlimited applies.</param>
public sealed record LootLimiter(
    string Cell,
    string Actor,
    string Name,
    string? DataLayer,
    LootLimiterShape Shape,
    Vector3D Location,
    Rotator Rotation,
    double Radius,
    IReadOnlyList<LootLimiterVolume> Volumes,
    IReadOnlyList<LootLimit> Limits);
