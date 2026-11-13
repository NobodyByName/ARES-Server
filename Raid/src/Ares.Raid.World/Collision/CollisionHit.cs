using System.Numerics;
using Ares.Common.Maps;
using Ares.Structs.Core.Geometry;

namespace Ares.Raid.World.Collision;

/// <summary>What a collision query hit.</summary>
public enum CollisionHitKind
{
    /// <summary>A placed body; the index is in <see cref="MapCollision.Placements"/>.</summary>
    Placement,

    /// <summary>A landscape height-field tile; the index is in <see cref="MapCollision.HeightFields"/>.</summary>
    HeightField,
}

/// <summary>The first surface a collision query reaches.</summary>
/// <param name="Distance">Distance travelled along the query direction, in centimetres.</param>
/// <param name="Point">World position of the hit: where a ray meets the surface, or where a swept shape touches it.</param>
/// <param name="Normal">
/// Unit world normal at the hit. For a ray it is the surface normal facing the ray; a ray that starts inside a solid
/// shape hits it at distance 0 with the normal opposite to the ray. For a swept shape it points from the contact
/// towards the shape (the surface normal on a face, rounded on edges and corners); for a sweep that starts
/// penetrating it is the direction that pushes the shape out.
/// </param>
/// <param name="Kind">Whether a placed body or a height-field tile was hit.</param>
/// <param name="Index">Index of the placement or tile in the map, according to <paramref name="Kind"/>.</param>
/// <param name="StartPenetrating">Whether a swept shape already penetrated the surface at its start (distance 0).</param>
/// <param name="PenetrationDepth">
/// For a sweep that starts penetrating, how far the shape must move along <paramref name="Normal"/> to clear that
/// surface, in centimetres; 0 otherwise.
/// </param>
public readonly record struct CollisionHit(
    double Distance,
    Vector3D Point,
    Vector3 Normal,
    CollisionHitKind Kind,
    int Index,
    bool StartPenetrating = false,
    double PenetrationDepth = 0);
