namespace Ares.Structs.Core.Geometry;

/// <summary>An engine rotation in degrees (<c>FRotator</c>), in its serialized order.</summary>
public readonly record struct Rotator(double Pitch, double Yaw, double Roll);
