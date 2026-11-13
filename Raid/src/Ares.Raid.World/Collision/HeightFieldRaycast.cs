using Ares.Common.Maps;

namespace Ares.Raid.World.Collision;

/// <summary>
/// Ray test against one height-field tile: walks the grid cells the ray crosses in X/Y order and tests the cell's two
/// triangles, honouring its diagonal and holes. Both triangle sides collide; the normal faces the ray.
/// </summary>
internal static class HeightFieldRaycast
{
    /// <summary>
    /// Finds the first hit within [<paramref name="enter"/>, <paramref name="exit"/>], the part of the ray inside the
    /// tile's bounds, nearer than <paramref name="maxT"/>.
    /// </summary>
    public static bool Raycast(HeightFieldTile tile, Double3 origin, Double3 direction, double enter, double exit, double maxT, out double t, out Double3 normal)
    {
        t = 0;
        normal = default;
        Double3 relative = origin - Double3.From(tile.Origin);
        double spacingX = tile.SpacingX;
        double spacingY = tile.SpacingY;
        int cellsX = tile.CountX - 1;
        int cellsY = tile.CountY - 1;
        int cellX = Math.Clamp((int)Math.Floor((relative.X + (enter * direction.X)) / spacingX), 0, cellsX - 1);
        int cellY = Math.Clamp((int)Math.Floor((relative.Y + (enter * direction.Y)) / spacingY), 0, cellsY - 1);
        int stepX = Math.Sign(direction.X);
        int stepY = Math.Sign(direction.Y);
        double nextX = stepX == 0 ? double.PositiveInfinity : (((cellX + (stepX > 0 ? 1 : 0)) * spacingX) - relative.X) / direction.X;
        double nextY = stepY == 0 ? double.PositiveInfinity : (((cellY + (stepY > 0 ? 1 : 0)) * spacingY) - relative.Y) / direction.Y;
        double deltaX = stepX == 0 ? double.PositiveInfinity : spacingX / Math.Abs(direction.X);
        double deltaY = stepY == 0 ? double.PositiveInfinity : spacingY / Math.Abs(direction.Y);
        double limit = Math.Min(exit, maxT);
        while (true)
        {
            if (Cell(tile, cellX, cellY, relative, direction, maxT, out t, out normal))
            {
                return true;
            }

            if (Math.Min(nextX, nextY) > limit)
            {
                return false;
            }

            if (nextX < nextY)
            {
                cellX += stepX;
                nextX += deltaX;
            }
            else
            {
                cellY += stepY;
                nextY += deltaY;
            }

            if ((uint)cellX >= (uint)cellsX || (uint)cellY >= (uint)cellsY)
            {
                return false;
            }
        }
    }

    private static bool Cell(HeightFieldTile tile, int x, int y, Double3 origin, Double3 direction, double maxT, out double t, out Double3 normal)
    {
        t = maxT;
        normal = default;
        bool hit = false;
        if (CellTriangle(tile, x, y, false, out Double3 a, out Double3 b, out Double3 c)
            && ShapeRaycasts.Triangle(a, b, c, origin, direction, t, out double low, out Double3 lowNormal))
        {
            t = low;
            normal = lowNormal;
            hit = true;
        }

        if (!CellTriangle(tile, x, y, true, out a, out b, out c)
            || !ShapeRaycasts.Triangle(a, b, c, origin, direction, t, out double high, out Double3 highNormal))
        {
            return hit;
        }

        t = high;
        normal = highNormal;
        return true;
    }

    /// <summary>
    /// The corners of the low-Y or high-Y triangle of grid cell (<paramref name="x"/>, <paramref name="y"/>), relative to
    /// the tile origin, following the cell's diagonal; false when that triangle is a hole.
    /// </summary>
    public static bool CellTriangle(HeightFieldTile tile, int x, int y, bool highY, out Double3 a, out Double3 b, out Double3 c)
    {
        ReadOnlySpan<short> heights = tile.Heights.AsSpan();
        HeightFieldCellFlags flags = tile.CellFlags[(y * (tile.CountX - 1)) + x];
        bool fromLowCorner = (flags & HeightFieldCellFlags.DiagonalFromLowCorner) != 0;
        if ((flags & (highY ? HeightFieldCellFlags.HighYTriangleHole : HeightFieldCellFlags.LowYTriangleHole)) != 0)
        {
            a = default;
            b = default;
            c = default;
            return false;
        }

        if (highY)
        {
            a = fromLowCorner ? Corner(tile, heights, x, y) : Corner(tile, heights, x + 1, y);
            b = Corner(tile, heights, x + 1, y + 1);
            c = Corner(tile, heights, x, y + 1);
        }
        else
        {
            a = Corner(tile, heights, x, y);
            b = Corner(tile, heights, x + 1, y);
            c = fromLowCorner ? Corner(tile, heights, x + 1, y + 1) : Corner(tile, heights, x, y + 1);
        }

        return true;
    }

    /// <summary>Sample (x, y) relative to the tile origin.</summary>
    private static Double3 Corner(HeightFieldTile tile, ReadOnlySpan<short> heights, int x, int y) =>
        new(x * (double)tile.SpacingX, y * (double)tile.SpacingY, heights[(y * tile.CountX) + x] * (double)tile.HeightScale);
}
