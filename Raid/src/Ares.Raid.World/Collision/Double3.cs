using System.Numerics;
using Ares.Structs.Core.Geometry;
using Quaternion = System.Numerics.Quaternion;

namespace Ares.Raid.World.Collision;

/// <summary>A double-precision vector for collision math on world positions.</summary>
internal readonly record struct Double3(double X, double Y, double Z)
{
    public static Double3 operator +(Double3 left, Double3 right) => new(left.X + right.X, left.Y + right.Y, left.Z + right.Z);

    public static Double3 operator -(Double3 left, Double3 right) => new(left.X - right.X, left.Y - right.Y, left.Z - right.Z);

    public static Double3 operator -(Double3 value) => new(-value.X, -value.Y, -value.Z);

    public static Double3 operator *(Double3 value, double factor) => new(value.X * factor, value.Y * factor, value.Z * factor);

    public static Double3 From(Vector3 value) => new(value.X, value.Y, value.Z);

    public static Double3 From(Vector3D value) => new(value.X, value.Y, value.Z);

    public Vector3D ToVector3D() => new(X, Y, Z);

    public Vector3 ToVector3() => new((float)X, (float)Y, (float)Z);

    public double LengthSquared() => (X * X) + (Y * Y) + (Z * Z);

    public double Length() => Math.Sqrt(LengthSquared());

    public Double3 Normalized()
    {
        double length = Length();
        return length > 0 ? this * (1 / length) : this;
    }

    public static double Dot(Double3 left, Double3 right) => (left.X * right.X) + (left.Y * right.Y) + (left.Z * right.Z);

    public static Double3 Cross(Double3 left, Double3 right) =>
        new((left.Y * right.Z) - (left.Z * right.Y), (left.Z * right.X) - (left.X * right.Z), (left.X * right.Y) - (left.Y * right.X));

    /// <summary>Multiplies component by component.</summary>
    public Double3 Scale(Vector3 factor) => new(X * factor.X, Y * factor.Y, Z * factor.Z);

    /// <summary>The absolute value of each component.</summary>
    public Double3 Abs() => new(Math.Abs(X), Math.Abs(Y), Math.Abs(Z));

    public static Double3 Min(Double3 left, Double3 right) => new(Math.Min(left.X, right.X), Math.Min(left.Y, right.Y), Math.Min(left.Z, right.Z));

    public static Double3 Max(Double3 left, Double3 right) => new(Math.Max(left.X, right.X), Math.Max(left.Y, right.Y), Math.Max(left.Z, right.Z));

    /// <summary>Divides component by component.</summary>
    public Double3 DivideBy(Vector3 divisor) => new(X / divisor.X, Y / divisor.Y, Z / divisor.Z);

    /// <summary>Rotates the vector by a unit quaternion.</summary>
    public Double3 Rotate(Quaternion rotation)
    {
        Double3 axis = new(rotation.X, rotation.Y, rotation.Z);
        Double3 twice = Cross(axis, this) * 2;
        return this + (twice * rotation.W) + Cross(axis, twice);
    }

    /// <summary>Rotates the vector by the inverse of a unit quaternion.</summary>
    public Double3 RotateInverse(Quaternion rotation) => Rotate(Quaternion.Conjugate(rotation));
}
