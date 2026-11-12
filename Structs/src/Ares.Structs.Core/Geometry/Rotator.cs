namespace Ares.Structs.Core.Geometry;

/// <summary>An engine rotation in degrees (<c>FRotator</c>), in its serialized order.</summary>
public readonly record struct Rotator(double Pitch, double Yaw, double Roll)
{
    /// <summary>Rotates a vector like the engine's rotation matrix of this rotator.</summary>
    /// <param name="vector">The vector.</param>
    /// <returns>The rotated vector.</returns>
    public Vector3D Rotate(Vector3D vector)
    {
        (double sp, double cp) = Math.SinCos(Pitch * Math.PI / 180);
        (double sy, double cy) = Math.SinCos(Yaw * Math.PI / 180);
        (double sr, double cr) = Math.SinCos(Roll * Math.PI / 180);
        Vector3D x = new(cp * cy, cp * sy, sp);
        Vector3D y = new((sr * sp * cy) - (cr * sy), (sr * sp * sy) + (cr * cy), -sr * cp);
        Vector3D z = new(-((cr * sp * cy) + (sr * sy)), (cy * sr) - (cr * sp * sy), cr * cp);
        return new Vector3D(
            (vector.X * x.X) + (vector.Y * y.X) + (vector.Z * z.X),
            (vector.X * x.Y) + (vector.Y * y.Y) + (vector.Z * z.Y),
            (vector.X * x.Z) + (vector.Y * y.Z) + (vector.Z * z.Z));
    }
}
