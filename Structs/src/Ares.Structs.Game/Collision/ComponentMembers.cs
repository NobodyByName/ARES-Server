namespace Ares.Structs.Game.Collision;

/// <summary>
/// Members of the <c>USceneComponent</c> part of a component's unversioned property chain. A component's chain lists
/// its own class's members first and its parents' after them, so the scene part starts at a class-specific index
/// (<c>ScenePart</c> of each component class); the indices here are relative to that start.
/// </summary>
public static class SceneComponentMembers
{
    /// <summary>Script path of the class.</summary>
    public const string ClassPath = "/Script/Engine.SceneComponent";

    /// <summary>Index of the scene part in this class's own chain.</summary>
    public const int ScenePart = 0;

    /// <summary><c>AttachParent</c>: export reference (4 bytes, 0 for none).</summary>
    public const int AttachParent = 1;

    /// <summary><c>AttachSocketName</c>: name (8 bytes).</summary>
    public const int AttachSocketName = 2;

    /// <summary>Unknown member, 2 bytes.</summary>
    public const int Unknown3 = 3;

    /// <summary><c>AttachChildren</c>: array of export references.</summary>
    public const int AttachChildren = 4;

    /// <summary><c>ClientAttachedChildren</c>: array of export references.</summary>
    public const int ClientAttachedChildren = 5;

    /// <summary><c>RelativeLocation</c>: vector of three doubles.</summary>
    public const int RelativeLocation = 6;

    /// <summary><c>RelativeRotation</c>: rotator of three doubles (pitch, yaw, roll).</summary>
    public const int RelativeRotation = 7;

    /// <summary><c>RelativeScale3D</c>: vector of three doubles.</summary>
    public const int RelativeScale3D = 8;

    /// <summary>Unknown member, 24 bytes.</summary>
    public const int Unknown9 = 9;

    /// <summary><c>bAbsoluteLocation</c>: one byte.</summary>
    public const int AbsoluteLocation = 11;

    /// <summary><c>bAbsoluteRotation</c>: one byte.</summary>
    public const int AbsoluteRotation = 12;

    /// <summary><c>bAbsoluteScale</c>: one byte.</summary>
    public const int AbsoluteScale = 13;

    /// <summary><c>bVisible</c>: one byte.</summary>
    public const int Visible = 14;

    /// <summary>Unknown member, one byte.</summary>
    public const int Unknown21 = 21;

    /// <summary><c>Mobility</c>: one byte.</summary>
    public const int Mobility = 29;

    /// <summary>Unknown member: a small struct of one-byte members, framed by its own unversioned header.</summary>
    public const int Unknown32 = 32;

    /// <summary><c>ComponentTags</c>: array of names.</summary>
    public const int ComponentTags = 33;

    /// <summary>Unknown member, 8 bytes.</summary>
    public const int Unknown34 = 34;

    /// <summary>Unknown member, one byte.</summary>
    public const int Unknown36 = 36;

    /// <summary>Unknown member, one byte.</summary>
    public const int Unknown38 = 38;

    /// <summary>Unknown member, one byte.</summary>
    public const int Unknown39 = 39;

    /// <summary>Unknown member, one byte.</summary>
    public const int Unknown48 = 48;

    /// <summary>Unknown member, one byte.</summary>
    public const int Unknown50 = 50;

    /// <summary>Bytes of a location, rotation or scale value.</summary>
    public const int VectorSize = 24;

    /// <summary>Bytes of an export reference.</summary>
    public const int ReferenceSize = 4;

    /// <summary>Bytes of a name.</summary>
    public const int NameSize = 8;

    /// <summary>
    /// Bytes of the value of a fixed-size scene member other than the location, rotation, scale, attach parent and
    /// socket name; <see langword="null"/> for arrays, structs and unknown members.
    /// </summary>
    public static int? FixedValueSize(int member) => member switch
    {
        Unknown3 => 2,
        Unknown9 => VectorSize,
        AbsoluteLocation or AbsoluteRotation or AbsoluteScale or Visible or Unknown21 or Mobility => 1,
        Unknown34 => 8,
        Unknown36 or Unknown38 or Unknown39 or Unknown48 or Unknown50 => 1,
        _ => null,
    };
}

/// <summary>Members of the <c>UPrimitiveComponent</c> part of a component's chain, which comes right before the scene part.</summary>
public static class PrimitiveComponentMembers
{
    /// <summary><c>BodyInstance</c> (an <c>FBodyInstance</c> struct) is this many members before the scene part.</summary>
    public const int BodyInstanceBeforeScenePart = 16;
}

/// <summary>Members of a <c>UStaticMeshComponent</c>'s own chain (its own, mesh and primitive members, then the scene part).</summary>
public static class StaticMeshComponentMembers
{
    /// <summary>Script path of the class.</summary>
    public const string ClassPath = "/Script/Engine.StaticMeshComponent";

    /// <summary>Unknown member, 4 bytes.</summary>
    public const int Unknown0 = 0;

    /// <summary><c>StaticMesh</c>: object reference (4 bytes).</summary>
    public const int StaticMesh = 4;

    /// <summary>Unknown member, one byte.</summary>
    public const int Unknown11 = 11;

    /// <summary><c>bUseDefaultCollision</c>: one byte; when set the mesh's own body setup decides the collision.</summary>
    public const int UseDefaultCollision = 24;

    /// <summary>Unknown member, one byte.</summary>
    public const int Unknown63 = 63;

    /// <summary>Unknown member, one byte.</summary>
    public const int Unknown66 = 66;

    /// <summary>Unknown member, one byte.</summary>
    public const int Unknown101 = 101;

    /// <summary>Index of the scene part.</summary>
    public const int ScenePart = 173;

    /// <summary>Index of <c>BodyInstance</c>.</summary>
    public const int BodyInstance = ScenePart - PrimitiveComponentMembers.BodyInstanceBeforeScenePart;

    /// <summary>Bytes of the value of a fixed-size member before <see cref="BodyInstance"/>; <see langword="null"/> when unknown.</summary>
    public static int? FixedValueSize(int member) => member switch
    {
        Unknown0 or StaticMesh => 4,
        Unknown11 or UseDefaultCollision or Unknown63 or Unknown66 or Unknown101 => 1,
        _ => null,
    };
}

/// <summary>Chain facts of a <c>USplineMeshComponent</c> (a static mesh component bent along a spline).</summary>
public static class SplineMeshComponentMembers
{
    /// <summary>Script path of the class.</summary>
    public const string ClassPath = "/Script/Engine.SplineMeshComponent";

    /// <summary>Index of the scene part.</summary>
    public const int ScenePart = 185;
}

/// <summary>Chain facts of a <c>UBrushComponent</c> (the collision of volumes such as blocking volumes).</summary>
public static class BrushComponentMembers
{
    /// <summary>Script path of the class.</summary>
    public const string ClassPath = "/Script/Engine.BrushComponent";

    /// <summary>Index of the scene part.</summary>
    public const int ScenePart = 119;
}

/// <summary>Chain facts of a <c>UBoxComponent</c>.</summary>
public static class BoxComponentMembers
{
    /// <summary>Script path of the class.</summary>
    public const string ClassPath = "/Script/Engine.BoxComponent";

    /// <summary>Path of the class default object, which the default extent below belongs to.</summary>
    public const string DefaultObject = "/Script/Engine.Default__BoxComponent";

    /// <summary><c>BoxExtent</c>: half the box size along each component axis, a vector of three doubles.</summary>
    public const int BoxExtent = 0;

    /// <summary>The class default's <c>BoxExtent</c> on every axis.</summary>
    public const double DefaultBoxExtent = 32;

    /// <summary>Index of the scene part.</summary>
    public const int ScenePart = 126;
}

/// <summary>Chain facts of a <c>USphereComponent</c>.</summary>
public static class SphereComponentMembers
{
    /// <summary>Script path of the class.</summary>
    public const string ClassPath = "/Script/Engine.SphereComponent";

    /// <summary>Path of the class default object, which the default radius below belongs to.</summary>
    public const string DefaultObject = "/Script/Engine.Default__SphereComponent";

    /// <summary><c>SphereRadius</c>: float.</summary>
    public const int SphereRadius = 0;

    /// <summary>The class default's <c>SphereRadius</c>.</summary>
    public const float DefaultSphereRadius = 32;

    /// <summary>Index of the scene part.</summary>
    public const int ScenePart = 126;
}

/// <summary>Chain facts of a <c>UCapsuleComponent</c>: a capsule along the component's Z axis.</summary>
public static class CapsuleComponentMembers
{
    /// <summary>Script path of the class.</summary>
    public const string ClassPath = "/Script/Engine.CapsuleComponent";

    /// <summary>Path of the class default object, which the default sizes below belong to.</summary>
    public const string DefaultObject = "/Script/Engine.Default__CapsuleComponent";

    /// <summary><c>CapsuleHalfHeight</c>: float, half the capsule's height including its caps.</summary>
    public const int CapsuleHalfHeight = 0;

    /// <summary><c>CapsuleRadius</c>: float.</summary>
    public const int CapsuleRadius = 1;

    /// <summary>The class default's <c>CapsuleHalfHeight</c>.</summary>
    public const float DefaultCapsuleHalfHeight = 44;

    /// <summary>The class default's <c>CapsuleRadius</c>.</summary>
    public const float DefaultCapsuleRadius = 22;

    /// <summary>Index of the scene part.</summary>
    public const int ScenePart = 127;
}

/// <summary>Chain facts of a <c>USplineComponent</c> (a parent that spline meshes attach to).</summary>
public static class SplineComponentMembers
{
    /// <summary>Script path of the class.</summary>
    public const string ClassPath = "/Script/Engine.SplineComponent";

    /// <summary>Index of the scene part.</summary>
    public const int ScenePart = 135;
}

/// <summary>Chain facts of a <c>ULandscapeSplinesComponent</c>.</summary>
public static class LandscapeSplinesComponentMembers
{
    /// <summary>Script path of the class.</summary>
    public const string ClassPath = "/Script/Landscape.LandscapeSplinesComponent";

    /// <summary>Index of the scene part.</summary>
    public const int ScenePart = 120;
}

/// <summary>Chain facts of a <c>ULevelInstanceComponent</c> (the root of a placed level instance).</summary>
public static class LevelInstanceComponentMembers
{
    /// <summary>Script path of the class.</summary>
    public const string ClassPath = "/Script/Engine.LevelInstanceComponent";

    /// <summary>Index of the scene part.</summary>
    public const int ScenePart = 0;
}

/// <summary>
/// Members of a <c>UInstancedStaticMeshComponent</c>'s chain: its own members, then the
/// <see cref="StaticMeshComponentMembers"/> chain. The instances are native data after the properties.
/// </summary>
public static class InstancedStaticMeshComponentMembers
{
    /// <summary>Script path of the class.</summary>
    public const string ClassPath = "/Script/Engine.InstancedStaticMeshComponent";

    /// <summary>Number of the class's own members before the static mesh component chain.</summary>
    public const int OwnMembers = 20;

    /// <summary>
    /// Bytes of one element of the native per-instance data array (<c>PerInstanceSMData</c>): an <c>FMatrix</c> of 16
    /// doubles in component space, rows X, Y and Z axes then the origin.
    /// </summary>
    public const int InstanceDataElementSize = 128;
}

/// <summary>
/// Members of a <c>UHierarchicalInstancedStaticMeshComponent</c>'s chain: its own members, then the
/// <see cref="InstancedStaticMeshComponentMembers"/> chain.
/// </summary>
public static class HierarchicalInstancedStaticMeshComponentMembers
{
    /// <summary>Script path of the class.</summary>
    public const string ClassPath = "/Script/Engine.HierarchicalInstancedStaticMeshComponent";

    /// <summary>Number of the class's own members before the instanced static mesh component chain.</summary>
    public const int OwnMembers = 13;

    /// <summary><c>SortedInstances</c>: array of int32, one per instance.</summary>
    public const int SortedInstances = 2;

    /// <summary>Members before the static mesh component chain.</summary>
    public const int StaticMeshComponentChainStart = OwnMembers + InstancedStaticMeshComponentMembers.OwnMembers;
}

/// <summary>Chain facts of the scatter tool's <c>UScatterMeshComponent</c>: a hierarchical instanced static mesh component without own members.</summary>
public static class ScatterMeshComponentMembers
{
    /// <summary>Script path of the class.</summary>
    public const string ClassPath = "/Script/EmbarkScatter.ScatterMeshComponent";
}
