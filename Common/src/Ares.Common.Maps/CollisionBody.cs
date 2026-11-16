using System.Collections.Immutable;
using System.Numerics;
using Ares.Structs.Game.Collision;

namespace Ares.Common.Maps;

/// <summary>
/// A collision body in its own local space, in centimetres: the shapes one asset (a static mesh, or a component's own
/// body such as a spline mesh or a brush) collides with. The boxes, spheres, capsules, convex hulls and triangle meshes
/// are the body's simple collision, which simple queries hit; triangle meshes are among them only for bodies that use
/// their complex collision as simple collision. <see cref="TraceFlag"/> decides what complex queries hit.
/// </summary>
/// <param name="Name">Identifies the body's source; unique within a map.</param>
/// <param name="Boxes">Oriented boxes.</param>
/// <param name="Spheres">Spheres.</param>
/// <param name="Capsules">Capsules.</param>
/// <param name="ConvexHulls">Convex hulls. A placement whose scale flips handedness mirrors them through its scale.</param>
/// <param name="TriangleMeshes">Triangle meshes of the simple collision, with their material slot per triangle.</param>
public sealed record CollisionBody(
    string Name,
    ImmutableArray<CollisionBox> Boxes,
    ImmutableArray<CollisionSphere> Spheres,
    ImmutableArray<CollisionCapsule> Capsules,
    ImmutableArray<ConvexHull> ConvexHulls,
    ImmutableArray<TriangleMesh> TriangleMeshes)
{
    /// <summary>
    /// Which collision the body offers to simple and complex queries; never <see cref="CollisionTraceFlag.UseDefault"/>.
    /// With <see cref="CollisionTraceFlag.SimpleAndComplex"/> complex queries hit only <see cref="ComplexMeshes"/> (a
    /// body without them is invisible to complex queries); otherwise they hit the simple collision.
    /// </summary>
    public CollisionTraceFlag TraceFlag { get; init; } = CollisionTraceFlag.SimpleAndComplex;

    /// <summary>
    /// Triangle meshes that only complex queries hit, with their material slot per triangle; empty unless
    /// <see cref="TraceFlag"/> is <see cref="CollisionTraceFlag.SimpleAndComplex"/>.
    /// </summary>
    public ImmutableArray<TriangleMesh> ComplexMeshes { get; init; } = [];

    /// <summary>Whether complex queries hit the simple collision rather than <see cref="ComplexMeshes"/>.</summary>
    public bool UsesSimpleAsComplex => TraceFlag != CollisionTraceFlag.SimpleAndComplex;
}

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
