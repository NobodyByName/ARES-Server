using System.Collections.Immutable;
using System.Runtime.InteropServices;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Game.Collision;

namespace Ares.Common.Maps;

/// <summary>
/// How a height-field grid cell is split into two triangles and which of them collide. Grid cell (x, y) has the corners
/// (x, y), (x + 1, y), (x, y + 1) and (x + 1, y + 1). Its low-Y triangle is the one with the edge (x, y)–(x + 1, y);
/// its high-Y triangle is the one with the edge (x, y + 1)–(x + 1, y + 1).
/// </summary>
[Flags]
public enum HeightFieldCellFlags : byte
{
    /// <summary>Diagonal from (x + 1, y) to (x, y + 1); both triangles collide.</summary>
    None = 0,

    /// <summary>The diagonal runs from (x, y) to (x + 1, y + 1) instead of from (x + 1, y) to (x, y + 1).</summary>
    DiagonalFromLowCorner = 1,

    /// <summary>The low-Y triangle is a hole (no collision).</summary>
    LowYTriangleHole = 2,

    /// <summary>The high-Y triangle is a hole (no collision).</summary>
    HighYTriangleHole = 4,
}

/// <summary>
/// One landscape collision tile: an axis-aligned grid of <see cref="CountX"/> × <see cref="CountY"/> height samples.
/// Sample (x, y) lies at <c>Origin + (x · SpacingX, y · SpacingY, height · HeightScale)</c> in world space.
/// </summary>
/// <param name="CellIndex">Index in <see cref="MapCollision.Cells"/> of the cell the tile belongs to.</param>
/// <param name="Origin">World position of sample (0, 0) at height 0, in centimetres.</param>
/// <param name="SpacingX">Distance between samples along world X (positive).</param>
/// <param name="SpacingY">Distance between samples along world Y (positive).</param>
/// <param name="HeightScale">World Z per height unit.</param>
/// <param name="CountX">Number of samples along X (at least 2).</param>
/// <param name="CountY">Number of samples along Y (at least 2).</param>
/// <param name="Heights">Sample heights; sample (x, y) is at index <c>y · CountX + x</c>.</param>
/// <param name="CellFlags">Grid cell flags; cell (x, y) is at index <c>y · (CountX − 1) + x</c>.</param>
/// <param name="CellSurfaces">
/// Surface type of each grid cell (both its triangles), indexed like <paramref name="CellFlags"/>; empty when every cell
/// is <see cref="PhysicalSurfaceType.Default"/>.
/// </param>
public sealed record HeightFieldTile(
    int CellIndex,
    Vector3D Origin,
    float SpacingX,
    float SpacingY,
    float HeightScale,
    int CountX,
    int CountY,
    ImmutableArray<short> Heights,
    ImmutableArray<HeightFieldCellFlags> CellFlags,
    ImmutableArray<PhysicalSurfaceType> CellSurfaces = default)
{
    /// <summary>Most distinct surface types a tile may hold.</summary>
    public const int MaximumSurfaceTypes = 32;

    /// <summary>Height of sample (x, y), in height units.</summary>
    public short Height(int x, int y)
    {
        if ((uint)x >= (uint)CountX || (uint)y >= (uint)CountY)
        {
            throw new ArgumentOutOfRangeException(nameof(x), "Sample outside the tile.");
        }

        return Heights[(y * CountX) + x];
    }

    /// <summary>World position of sample (x, y).</summary>
    public Vector3D SamplePosition(int x, int y)
    {
        short height = Height(x, y);
        return new Vector3D(Origin.X + (x * (double)SpacingX), Origin.Y + (y * (double)SpacingY), Origin.Z + (height * (double)HeightScale));
    }

    /// <summary>Flags of grid cell (x, y).</summary>
    public HeightFieldCellFlags Cell(int x, int y)
    {
        if ((uint)x >= (uint)(CountX - 1) || (uint)y >= (uint)(CountY - 1))
        {
            throw new ArgumentOutOfRangeException(nameof(x), "Grid cell outside the tile.");
        }

        return CellFlags[(y * (CountX - 1)) + x];
    }

    /// <summary>Surface type of grid cell (x, y).</summary>
    public PhysicalSurfaceType Surface(int x, int y)
    {
        if ((uint)x >= (uint)(CountX - 1) || (uint)y >= (uint)(CountY - 1))
        {
            throw new ArgumentOutOfRangeException(nameof(x), "Grid cell outside the tile.");
        }

        return CellSurfaces.IsDefaultOrEmpty ? PhysicalSurfaceType.Default : CellSurfaces[(y * (CountX - 1)) + x];
    }

    /// <summary>
    /// Surface type of the grid cell under a world position, clamped to the tile, for a hit on the tile's surface.
    /// </summary>
    public PhysicalSurfaceType SurfaceAt(double worldX, double worldY)
    {
        if (CellSurfaces.IsDefaultOrEmpty)
        {
            return PhysicalSurfaceType.Default;
        }

        int x = Math.Clamp((int)Math.Floor((worldX - Origin.X) / SpacingX), 0, CountX - 2);
        int y = Math.Clamp((int)Math.Floor((worldY - Origin.Y) / SpacingY), 0, CountY - 2);
        return CellSurfaces[(y * (CountX - 1)) + x];
    }

    /// <summary>
    /// Builds a tile from a landscape collision component's height field, whose rows run along the landscape X axis
    /// reversed (see <see cref="LandscapeCollision.SampleIndex"/>) and whose columns run along Y.
    /// </summary>
    /// <param name="cellIndex">Index of the cell that owns the component.</param>
    /// <param name="field">The component's height field.</param>
    /// <param name="origin">World position of landscape sample (0, 0) at height 0.</param>
    /// <param name="spacingX">World distance between samples along X.</param>
    /// <param name="spacingY">World distance between samples along Y.</param>
    /// <param name="heightScale">World Z per height unit.</param>
    /// <param name="materialSurfaces">
    /// Surface type of each of the field's material indices; when empty, every cell has <see cref="PhysicalSurfaceType.Default"/>.
    /// </param>
    /// <exception cref="ArgumentException">A cell's triangles have different materials, or a material has no surface type.</exception>
    public static HeightFieldTile FromLandscape(
        int cellIndex,
        HeightField field,
        Vector3D origin,
        float spacingX,
        float spacingY,
        float heightScale,
        IReadOnlyList<PhysicalSurfaceType> materialSurfaces)
    {
        ArgumentNullException.ThrowIfNull(field);
        ArgumentNullException.ThrowIfNull(materialSurfaces);
        int countX = field.Rows;
        int countY = field.Columns;
        short[] heights = new short[countX * countY];
        for (int y = 0; y < countY; y++)
        {
            for (int x = 0; x < countX; x++)
            {
                heights[(y * countX) + x] = field.Samples[LandscapeCollision.SampleIndex(field, x, y)].Height;
            }
        }

        // Grid cell (x, y) is the field cell whose first corner is row rows − 2 − x, column y: that corner is landscape
        // sample (x + 1, y). The field's first triangle holds the edge (x, y)–(x + 1, y), so it is the low-Y triangle.
        HeightFieldCellFlags[] cells = new HeightFieldCellFlags[(countX - 1) * (countY - 1)];
        PhysicalSurfaceType[] surfaces = new PhysicalSurfaceType[cells.Length];
        for (int y = 0; y < countY - 1; y++)
        {
            for (int x = 0; x < countX - 1; x++)
            {
                HeightFieldSample sample = field.Sample(countX - 2 - x, y);
                HeightFieldCellFlags flags = HeightFieldCellFlags.None;
                if (!sample.SharesFirstCorner)
                {
                    flags |= HeightFieldCellFlags.DiagonalFromLowCorner;
                }

                if (sample.FirstTriangleIsHole)
                {
                    flags |= HeightFieldCellFlags.LowYTriangleHole;
                }

                if (sample.SecondTriangleIsHole)
                {
                    flags |= HeightFieldCellFlags.HighYTriangleHole;
                }

                cells[(y * (countX - 1)) + x] = flags;
                surfaces[(y * (countX - 1)) + x] = CellSurface(sample, materialSurfaces);
            }
        }

        return new HeightFieldTile(
            cellIndex,
            origin,
            spacingX,
            spacingY,
            heightScale,
            countX,
            countY,
            ImmutableCollectionsMarshal.AsImmutableArray(heights),
            ImmutableCollectionsMarshal.AsImmutableArray(cells),
            ImmutableCollectionsMarshal.AsImmutableArray(surfaces));
    }

    /// <summary>The surface type of a field cell: that of its triangles' material (holes have none).</summary>
    private static PhysicalSurfaceType CellSurface(HeightFieldSample sample, IReadOnlyList<PhysicalSurfaceType> materialSurfaces)
    {
        if (sample is { FirstTriangleIsHole: true, SecondTriangleIsHole: true })
        {
            return PhysicalSurfaceType.Default;
        }

        byte material = sample.FirstTriangleIsHole ? sample.SecondTriangleMaterial : sample.FirstTriangleMaterial;
        if (sample is { FirstTriangleIsHole: false, SecondTriangleIsHole: false } && sample.SecondTriangleMaterial != material)
        {
            throw new ArgumentException($"A height-field cell's triangles have different materials ({material}, {sample.SecondTriangleMaterial}).");
        }

        if (materialSurfaces.Count == 0)
        {
            return PhysicalSurfaceType.Default;
        }

        return material < materialSurfaces.Count
            ? materialSurfaces[material]
            : throw new ArgumentException($"Height-field material {material} has no surface type ({materialSurfaces.Count} known).");
    }
}
