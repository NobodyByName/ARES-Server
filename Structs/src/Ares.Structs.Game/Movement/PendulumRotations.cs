namespace Ares.Structs.Game.Movement;

/// <summary>
/// Converts quantized Pendulum rotations. A component is quantized as round(|c| * sqrt(Total)); the largest one
/// is omitted (stored as 0) and follows from the others. A set sign bit marks a non-negative component.
/// </summary>
public static class PendulumRotations
{
    private const byte AllNonNegative = 0b1111;

    // How close to straight up or down a ground normal may point before its rotation's X axis is world X.
    private const double NearlyParallel = 1e-4;

    /// <summary>The rotation as (X, Y, Z, W) components.</summary>
    /// <param name="quaternion">The quantized rotation.</param>
    /// <param name="field">Its field.</param>
    /// <returns>The components.</returns>
    public static (double X, double Y, double Z, double W) Components(PendulumQuaternion quaternion, PendulumQuaternionField field)
    {
        ArgumentNullException.ThrowIfNull(quaternion);
        ArgumentNullException.ThrowIfNull(field);
        double scale = Math.Sqrt(field.Total);
        double[] values = [Middle(quaternion.Component0, scale), Middle(quaternion.Component1, scale), Middle(quaternion.Component2, scale), Middle(quaternion.Component3, scale)];
        int omitted = quaternion.Omitted;
        values[omitted] = Math.Sqrt(Math.Max(0.0, 1.0 - values.Where((_, index) => index != omitted).Sum(value => value * value)));
        for (int index = 0; index < 4; index++)
        {
            if ((quaternion.UnknownBits & (1 << index)) == 0)
            {
                values[index] = -values[index];
            }
        }

        return (values[0], values[1], values[2], values[3]);
    }

    /// <summary>The heading (rotation about the vertical axis) of a rotation.</summary>
    /// <param name="quaternion">The quantized rotation.</param>
    /// <param name="field">Its field.</param>
    /// <returns>The heading in radians.</returns>
    public static double Yaw(PendulumQuaternion quaternion, PendulumQuaternionField field)
    {
        (double x, double y, double z, double w) = Components(quaternion, field);
        return Math.Atan2(2.0 * ((w * z) + (x * y)), 1.0 - (2.0 * ((y * y) + (z * z))));
    }

    /// <summary>The pitch (up/down angle, positive up) of a rotation.</summary>
    /// <param name="quaternion">The quantized rotation.</param>
    /// <param name="field">Its field.</param>
    /// <returns>The pitch in radians.</returns>
    public static double Pitch(PendulumQuaternion quaternion, PendulumQuaternionField field)
    {
        (double x, double y, double z, double w) = Components(quaternion, field);
        return Math.Asin(Math.Clamp(2.0 * ((w * y) - (z * x)), -1.0, 1.0));
    }

    /// <summary>A rotation about the vertical axis only.</summary>
    /// <param name="yaw">Heading in radians.</param>
    /// <param name="field">The field it is stored in.</param>
    /// <returns>The quantized rotation.</returns>
    public static PendulumQuaternion FromYaw(double yaw, PendulumQuaternionField field)
    {
        ArgumentNullException.ThrowIfNull(field);
        double scale = Math.Sqrt(field.Total);
        double z = Math.Sin(yaw / 2.0);
        double w = Math.Cos(yaw / 2.0);
        if (w < 0)
        {
            (z, w) = (-z, -w);
        }

        byte signs = z < 0 ? (byte)(AllNonNegative & ~(1 << 2)) : AllNonNegative;
        return Math.Abs(z) <= w
            ? new PendulumQuaternion(0, 0, Stored(Math.Abs(z), scale, field), 0, signs, 0, 1, 2)
            : new PendulumQuaternion(0, 0, 0, Stored(w, scale, field), signs, 0, 1, 3);
    }

    /// <summary>
    /// The rotation a ground normal is stored as: the rotation whose Z axis is the normal, its X axis perpendicular to
    /// world up (world X when the normal points straight up or down).
    /// </summary>
    /// <param name="x">Normal X.</param>
    /// <param name="y">Normal Y.</param>
    /// <param name="z">Normal Z.</param>
    /// <param name="field">The quaternion field.</param>
    /// <returns>The stored quaternion.</returns>
    public static PendulumQuaternion FromGroundNormal(double x, double y, double z, PendulumQuaternionField field)
    {
        ArgumentNullException.ThrowIfNull(field);
        double length = Math.Sqrt((x * x) + (y * y) + (z * z));
        (double nx, double ny, double nz) = length > 0 ? (x / length, y / length, z / length) : (0.0, 0.0, 1.0);
        (double ux, double uz) = Math.Abs(nz) < 1 - NearlyParallel ? (0.0, 1.0) : (1.0, 0.0);

        // X = up × normal, then Y = normal × X.
        (double ax, double ay, double az) = (-(uz * ny), (uz * nx) - (ux * nz), ux * ny);
        double axis = Math.Sqrt((ax * ax) + (ay * ay) + (az * az));
        (ax, ay, az) = (ax / axis, ay / axis, az / axis);
        (double bx, double by, double bz) = ((ny * az) - (nz * ay), (nz * ax) - (nx * az), (nx * ay) - (ny * ax));
        double[,] m = { { ax, ay, az }, { bx, by, bz }, { nx, ny, nz } };
        double[] q = new double[4];
        double trace = m[0, 0] + m[1, 1] + m[2, 2];
        if (trace > 0)
        {
            double inverse = 1 / Math.Sqrt(trace + 1);
            q[3] = 0.5 / inverse;
            double s = 0.5 * inverse;
            (q[0], q[1], q[2]) = ((m[1, 2] - m[2, 1]) * s, (m[2, 0] - m[0, 2]) * s, (m[0, 1] - m[1, 0]) * s);
        }
        else
        {
            int i = m[1, 1] > m[0, 0] ? 1 : 0;
            i = m[2, 2] > m[i, i] ? 2 : i;
            int j = (i + 1) % 3;
            int k = (j + 1) % 3;
            double inverse = 1 / Math.Sqrt(m[i, i] - m[j, j] - m[k, k] + 1);
            q[i] = 0.5 / inverse;
            double s = 0.5 * inverse;
            q[3] = (m[j, k] - m[k, j]) * s;
            q[j] = (m[i, j] + m[j, i]) * s;
            q[k] = (m[i, k] + m[k, i]) * s;
        }

        return Pack(q, field);
    }

    // The three smallest components in increasing size; the largest one (the later one of equals) is left out.
    private static PendulumQuaternion Pack(double[] q, PendulumQuaternionField field)
    {
        double scale = Math.Sqrt(field.Total);
        int omitted = 0;
        for (int index = 1; index < 4; index++)
        {
            omitted = Math.Abs(q[index]) >= Math.Abs(q[omitted]) ? index : omitted;
        }

        int[] kept = [.. Enumerable.Range(0, 4).Where(index => index != omitted).OrderBy(index => Math.Abs(q[index]))];
        uint[] stored = [.. q.Select(component => Stored(Math.Abs(component), scale, field))];
        stored[omitted] = 0;
        byte signs = (byte)Enumerable.Range(0, 4).Where(index => q[index] >= 0).Sum(index => 1 << index);
        return new PendulumQuaternion(stored[0], stored[1], stored[2], stored[3], signs, (byte)kept[0], (byte)kept[1], (byte)kept[2]);
    }

    // A stored component: the whole steps below it, kept under the field's exclusive bound.
    private static uint Stored(double component, double scale, PendulumQuaternionField field) =>
        Math.Min((uint)Math.Floor(component * scale), field.Maximum - 1);

    // A stored component stands for the middle of its step; zero stays zero.
    private static double Middle(uint stored, double scale) => stored == 0 ? 0 : (stored + 0.5) / scale;
}
