namespace Ares.Structs.Core.Geometry;

/// <summary>A double-precision rotation quaternion (engine <c>FQuat</c>), in its serialized order.</summary>
public readonly record struct Quaternion(double X, double Y, double Z, double W)
{
    /// <summary>No rotation.</summary>
    public static Quaternion Identity => new(0, 0, 0, 1);
}
