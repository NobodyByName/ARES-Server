using System.Collections.Immutable;
using System.Numerics;

namespace Ares.Structs.Game.Collision;

/// <summary>An axis-aligned box in a shape's local space, in centimetres.</summary>
/// <param name="Minimum">Smallest corner.</param>
/// <param name="Maximum">Largest corner.</param>
public readonly record struct BoundingBox(Vector3 Minimum, Vector3 Maximum);

/// <summary>One face of a convex hull: its plane and its vertices, in order around the face.</summary>
/// <param name="Plane">Face plane; points inside the hull have a negative distance (<c>Normal · p + D &lt; 0</c>).</param>
/// <param name="FirstIndex">Offset of the face's first vertex index in <see cref="ConvexHull.PolygonVertexIndices"/>.</param>
/// <param name="IndexCount">Number of vertices of the face.</param>
public readonly record struct HullPolygon(Plane Plane, int FirstIndex, int IndexCount);

/// <summary>A convex collision hull (PhysX convex mesh) in the body's local space.</summary>
/// <param name="Vertices">Hull vertices, at most 255.</param>
/// <param name="Polygons">Hull faces.</param>
/// <param name="PolygonVertexIndices">Vertex indices of all faces, addressed by <see cref="HullPolygon"/>.</param>
/// <param name="Bounds">Local bounds of the hull.</param>
public sealed record ConvexHull(
    ImmutableArray<Vector3> Vertices,
    ImmutableArray<HullPolygon> Polygons,
    ImmutableArray<byte> PolygonVertexIndices,
    BoundingBox Bounds)
{
    /// <summary>Vertex indices of one face, in order around it.</summary>
    public ReadOnlySpan<byte> PolygonVertices(HullPolygon polygon) => PolygonVertexIndices.AsSpan(polygon.FirstIndex, polygon.IndexCount);
}

/// <summary>A collision triangle mesh (PhysX triangle mesh) in the body's local space.</summary>
/// <param name="Vertices">Mesh vertices.</param>
/// <param name="Indices">Three vertex indices per triangle.</param>
/// <param name="MaterialIndices">Material slot per triangle, or empty when the mesh stores none.</param>
/// <param name="Bounds">Local bounds of the mesh.</param>
public sealed record TriangleMesh(
    ImmutableArray<Vector3> Vertices,
    ImmutableArray<int> Indices,
    ImmutableArray<ushort> MaterialIndices,
    BoundingBox Bounds)
{
    /// <summary>Number of triangles.</summary>
    public int TriangleCount => Indices.Length / 3;
}

/// <summary>
/// The cooked geometry of one body: convex hulls for simple collision, the same hulls mirrored for components with a
/// negative-determinant scale, and triangle meshes for complex collision. A hull entry is <see langword="null"/> when
/// its element could not be cooked.
/// </summary>
/// <param name="ConvexHulls">Hulls of the body's convex elements, in element order.</param>
/// <param name="MirroredConvexHulls">Hulls cooked from the elements mirrored on X, used instead of
/// <paramref name="ConvexHulls"/> when the component scale flips handedness; empty when the body is never mirrored.</param>
/// <param name="TriangleMeshes">Triangle meshes for complex collision, used as simple collision only when the body
/// collides complex-as-simple.</param>
public sealed record CookedBodyGeometry(
    ImmutableArray<ConvexHull?> ConvexHulls,
    ImmutableArray<ConvexHull?> MirroredConvexHulls,
    ImmutableArray<TriangleMesh> TriangleMeshes);

/// <summary>One height-field sample: the height and the materials of the cell whose first corner it is.</summary>
/// <param name="Height">Height in height-field units.</param>
/// <param name="MaterialIndex0">First triangle's material in bits 0–6, tessellation flag in bit 7.</param>
/// <param name="MaterialIndex1">Second triangle's material in bits 0–6; bit 7 is reserved.</param>
public readonly record struct HeightFieldSample(short Height, byte MaterialIndex0, byte MaterialIndex1)
{
    /// <summary>Material index that makes a triangle a hole (no collision).</summary>
    public const byte HoleMaterial = 127;

    /// <summary>Material of the cell's first triangle.</summary>
    public byte FirstTriangleMaterial => (byte)(MaterialIndex0 & 0x7F);

    /// <summary>Material of the cell's second triangle.</summary>
    public byte SecondTriangleMaterial => (byte)(MaterialIndex1 & 0x7F);

    /// <summary>
    /// Whether both triangles of the cell share its first corner: the diagonal runs from (row, column) to
    /// (row + 1, column + 1). Otherwise it runs from (row, column + 1) to (row + 1, column).
    /// </summary>
    public bool SharesFirstCorner => (MaterialIndex0 & 0x80) != 0;

    /// <summary>Whether the cell's first triangle is a hole.</summary>
    public bool FirstTriangleIsHole => FirstTriangleMaterial == HoleMaterial;

    /// <summary>Whether the cell's second triangle is a hole.</summary>
    public bool SecondTriangleIsHole => SecondTriangleMaterial == HoleMaterial;
}

/// <summary>
/// A PhysX height field: a grid of <see cref="Rows"/> × <see cref="Columns"/> samples, row-major. In the field's
/// own space a sample sits at (row, height, column) before the shape's scale.
/// </summary>
/// <param name="Rows">Number of sample rows (at least 2).</param>
/// <param name="Columns">Number of sample columns (at least 2).</param>
/// <param name="Thickness">Thickness below the surface for collision, in height units.</param>
/// <param name="ConvexEdgeThreshold">Edges flatter than this are not treated as convex edges.</param>
/// <param name="Flags">Height-field flags (bit 0: no boundary edges).</param>
/// <param name="Bounds">Field bounds in its own space: (0, minimum height, 0) to (rows − 1, maximum height, columns − 1).</param>
/// <param name="MinimumHeight">Lowest sample height.</param>
/// <param name="MaximumHeight">Highest sample height.</param>
/// <param name="Samples">Samples, row-major.</param>
public sealed record HeightField(
    int Rows,
    int Columns,
    float Thickness,
    float ConvexEdgeThreshold,
    ushort Flags,
    BoundingBox Bounds,
    float MinimumHeight,
    float MaximumHeight,
    ImmutableArray<HeightFieldSample> Samples)
{
    /// <summary>The sample at a row and column.</summary>
    public HeightFieldSample Sample(int row, int column)
    {
        if ((uint)row >= (uint)Rows || (uint)column >= (uint)Columns)
        {
            throw new ArgumentOutOfRangeException(nameof(row), "Row or column outside the height field.");
        }

        return Samples[(row * Columns) + column];
    }
}
