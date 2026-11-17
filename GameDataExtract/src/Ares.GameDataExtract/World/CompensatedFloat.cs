using Ares.Structs.Core.Geometry;

namespace Ares.GameDataExtract.World;

/// <summary>
/// Floating-point sums and distances the hull tables are computed with, fixed so that values that depend on rounding
/// come out the same bit for bit: <see cref="Sum"/> is Neumaier's compensated summation and <see cref="Distance"/> a
/// scaled, compensated sum of squares with a differential correction of its square root (correctly rounded in
/// practice).
/// </summary>
internal static class CompensatedFloat
{
    /// <summary>The compensated sum of <paramref name="values"/>, in order.</summary>
    public static double Sum(IEnumerable<double> values)
    {
        double sum = 0.0;
        double compensation = 0.0;
        foreach (double value in values)
        {
            double total = sum + value;
            compensation += Math.Abs(sum) >= Math.Abs(value) ? sum - total + value : value - total + sum;
            sum = total;
        }

        return compensation != 0 && double.IsFinite(compensation) ? sum + compensation : sum;
    }

    /// <summary>The dot product of <paramref name="a"/> and <paramref name="b"/>, summed with <see cref="Sum"/>.</summary>
    public static double Dot(Vector3D a, Vector3D b) => Sum([a.X * b.X, a.Y * b.Y, a.Z * b.Z]);

    /// <summary>The Euclidean distance between <paramref name="a"/> and <paramref name="b"/>.</summary>
    public static double Distance(Vector3D a, Vector3D b)
    {
        double[] differences = [Math.Abs(a.X - b.X), Math.Abs(a.Y - b.Y), Math.Abs(a.Z - b.Z)];
        double max = differences.Max();
        if (!double.IsFinite(max) || max == 0.0)
        {
            return max;
        }

        int exponent = Math.ILogB(max) + 1;
        if (exponent < -1023)
        {
            // Not reached for map coordinates: subnormal differences.
            return Math.Sqrt(Sum(differences.Select(difference => difference * difference)));
        }

        double scale = Math.ScaleB(1.0, -exponent);
        double sum = 1.0;
        double low1 = 0.0;
        double low2 = 0.0;
        foreach (double difference in differences)
        {
            double x = difference * scale;
            (double square, double squareLow) = Multiply(x, x);
            (sum, double sumLow) = FastSum(sum, square);
            low1 += squareLow;
            low2 += sumLow;
        }

        double root = Math.Sqrt(sum - 1.0 + (low1 + low2));
        (double product, double productLow) = Multiply(-root, root);
        (sum, double lastLow) = FastSum(sum, product);
        low1 += productLow;
        low2 += lastLow;
        double residual = sum - 1.0 + (low1 + low2);
        root += residual / (2.0 * root);
        return root / scale;
    }

    private static (double High, double Low) Multiply(double x, double y)
    {
        double high = x * y;
        return (high, Math.FusedMultiplyAdd(x, y, -high));
    }

    private static (double High, double Low) FastSum(double a, double b)
    {
        double high = a + b;
        return (high, a - high + b);
    }
}
