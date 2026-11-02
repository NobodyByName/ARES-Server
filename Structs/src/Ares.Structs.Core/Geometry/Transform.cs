namespace Ares.Structs.Core.Geometry;

/// <summary>An engine transform (<c>FTransform</c>): rotation, then translation and scale.</summary>
public readonly record struct Transform(Quaternion Rotation, Vector3D Translation, Vector3D Scale3D);
