namespace Ares.Structs.Game.Movement;

/// <summary>
/// Converts quantized Pendulum rotations. A component is quantized as round(|c| * sqrt(Total)); the largest one
/// is omitted (stored as 0) and follows from the others. A set sign bit marks a non-negative component.
/// </summary>
public static class PendulumRotations
{
    private const byte AllNonNegative = 0b1111;

    /// <summary>The rotation as (X, Y, Z, W) components.</summary>
    /// <param name="quaternion">The quantized rotation.</param>
    /// <param name="field">Its field.</param>
    /// <returns>The components.</returns>
    public static (double X, double Y, double Z, double W) Components(PendulumQuaternion quaternion, PendulumQuaternionField field)
    {
        ArgumentNullException.ThrowIfNull(quaternion);
        ArgumentNullException.ThrowIfNull(field);
        double scale = Math.Sqrt(field.Total);
        double[] values = [quaternion.Component0 / scale, quaternion.Component1 / scale, quaternion.Component2 / scale, quaternion.Component3 / scale];
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

    // A stored component; near a quarter turn it can round up to the field's exclusive bound on coarse fields.
    private static uint Stored(double component, double scale, PendulumQuaternionField field) =>
        Math.Min((uint)Math.Round(component * scale), field.Maximum - 1);
}
