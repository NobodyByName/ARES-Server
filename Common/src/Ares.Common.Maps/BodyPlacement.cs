using System.Collections.Immutable;
using System.Numerics;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Game.Collision;
using Quaternion = System.Numerics.Quaternion;

namespace Ares.Common.Maps;

/// <summary>What a placed body blocks.</summary>
[Flags]
public enum PlacementFlags : uint
{
    /// <summary>Blocks nothing.</summary>
    None = 0,

    /// <summary>Blocks player movement.</summary>
    BlocksPawn = 1,

    /// <summary>Blocks the traces used for vaulting and mantling.</summary>
    BlocksClimbable = 2,
}

/// <summary>
/// One body placed in the world. A body-space point <c>p</c> lies at
/// <c>Translation + Rotation · (Scale ∘ p)</c>; the scale may be negative or non-uniform.
/// </summary>
/// <param name="BodyIndex">Index in <see cref="MapCollision.Bodies"/>.</param>
/// <param name="CellIndex">Index in <see cref="MapCollision.Cells"/> of the cell the placement belongs to.</param>
/// <param name="Translation">World position of the body's origin, in centimetres.</param>
/// <param name="Rotation">World rotation (unit quaternion).</param>
/// <param name="Scale">World scale per body axis.</param>
/// <param name="Flags">What the placement blocks.</param>
/// <param name="Bounds">
/// World bounds relative to <paramref name="Translation"/>, rounded outwards, computed from the body when the map is
/// built or read. They contain every shape of the body as placed; spheres and capsules are bounded with the largest
/// absolute scale component.
/// </param>
public readonly record struct BodyPlacement(
    int BodyIndex,
    int CellIndex,
    Vector3D Translation,
    Quaternion Rotation,
    Vector3 Scale,
    PlacementFlags Flags,
    BoundingBox Bounds)
{
    /// <summary>
    /// Surface type of the placement's boxes, spheres, capsules and convex hulls, and of triangles whose material slot
    /// <see cref="MaterialSurfaces"/> does not cover.
    /// </summary>
    public PhysicalSurfaceType Surface { get; init; }

    /// <summary>
    /// Surface type of each material slot, which the body's triangle meshes name per triangle; empty for placements
    /// without triangle meshes.
    /// </summary>
    public ImmutableArray<PhysicalSurfaceType> MaterialSurfaces { get; init; } = [];

    /// <summary>The surface type of a triangle of the body's triangle mesh <paramref name="mesh"/>.</summary>
    /// <param name="mesh">The triangle mesh, one of the placed body's.</param>
    /// <param name="triangle">Index of the triangle in the mesh.</param>
    public PhysicalSurfaceType TriangleSurface(TriangleMesh mesh, int triangle)
    {
        ArgumentNullException.ThrowIfNull(mesh);
        int slot = mesh.MaterialIndices.IsEmpty ? 0 : mesh.MaterialIndices[triangle];
        return !MaterialSurfaces.IsDefault && slot < MaterialSurfaces.Length ? MaterialSurfaces[slot] : Surface;
    }

    /// <summary>Smallest world corner of the placement's bounds.</summary>
    public Vector3D WorldMinimum => new(Translation.X + Bounds.Minimum.X, Translation.Y + Bounds.Minimum.Y, Translation.Z + Bounds.Minimum.Z);

    /// <summary>Largest world corner of the placement's bounds.</summary>
    public Vector3D WorldMaximum => new(Translation.X + Bounds.Maximum.X, Translation.Y + Bounds.Maximum.Y, Translation.Z + Bounds.Maximum.Z);
}
