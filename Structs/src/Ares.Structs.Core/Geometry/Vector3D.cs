namespace Ares.Structs.Core.Geometry;

/// <summary>A double-precision 3D vector (engine <c>FVector</c>), in centimetres for locations.</summary>
public readonly record struct Vector3D(double X, double Y, double Z)
{
    /// <summary>The unit scale (1, 1, 1).</summary>
    public static Vector3D One => new(1, 1, 1);
}
