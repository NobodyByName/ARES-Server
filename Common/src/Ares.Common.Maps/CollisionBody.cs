using System.Collections.Immutable;
using System.Numerics;
using Ares.Structs.Game.Collision;

namespace Ares.Common.Maps;

/// <summary>
/// A collision body in its own local space, in centimetres: the shapes one asset (a static mesh, or a component's own
/// body such as a spline mesh or a brush) collides with. Every shape of a body collides; triangle meshes are present
/// only for bodies that use their complex collision as simple collision.
/// </summary>
/// <param name="Name">Identifies the body's source; unique within a map.</param>
/// <param name="Boxes">Oriented boxes.</param>
/// <param name="Spheres">Spheres.</param>
/// <param name="Capsules">Capsules.</param>
/// <param name="ConvexHulls">Convex hulls. A placement whose scale flips handedness mirrors them through its scale.</param>
/// <param name="TriangleMeshes">Triangle meshes; their material indices are not kept.</param>
public sealed record CollisionBody(
    string Name,
    ImmutableArray<CollisionBox> Boxes,
    ImmutableArray<CollisionSphere> Spheres,
    ImmutableArray<CollisionCapsule> Capsules,
    ImmutableArray<ConvexHull> ConvexHulls,
    ImmutableArray<TriangleMesh> TriangleMeshes);

/// <summary>An oriented box in body space.</summary>
/// <param name="Center">Box centre.</param>
/// <param name="Rotation">Box orientation.</param>
/// <param name="HalfExtents">Half the box size along its own axes.</param>
public readonly record struct CollisionBox(Vector3 Center, Quaternion Rotation, Vector3 HalfExtents);

/// <summary>A sphere in body space.</summary>
/// <param name="Center">Sphere centre.</param>
/// <param name="Radius">Sphere radius.</param>
public readonly record struct CollisionSphere(Vector3 Center, float Radius);

/// <summary>A capsule in body space: a segment along the capsule's local Z axis, swept by a sphere.</summary>
/// <param name="Center">Centre of the segment.</param>
/// <param name="Rotation">Capsule orientation.</param>
/// <param name="Radius">Radius of the swept sphere.</param>
/// <param name="HalfLength">Half the segment length (the distance from the centre to each cap's centre).</param>
public readonly record struct CollisionCapsule(Vector3 Center, Quaternion Rotation, float Radius, float HalfLength);
