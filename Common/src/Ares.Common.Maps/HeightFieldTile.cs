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
public sealed record HeightFieldTile(
    int CellIndex,
    Vector3D Origin,
    float SpacingX,
    float SpacingY,
    float HeightScale,
    int CountX,
    int CountY,
    ImmutableArray<short> Heights,
    ImmutableArray<HeightFieldCellFlags> CellFlags)
{
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
    public static HeightFieldTile FromLandscape(
        int cellIndex,
        HeightField field,
        Vector3D origin,
        float spacingX,
        float spacingY,
        float heightScale)
    {
        ArgumentNullException.ThrowIfNull(field);
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
            ImmutableCollectionsMarshal.AsImmutableArray(cells));
    }
}
