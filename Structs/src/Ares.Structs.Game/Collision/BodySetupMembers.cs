namespace Ares.Structs.Game.Collision;

/// <summary>Which collision a body offers to simple and complex queries (<c>ECollisionTraceFlag</c>).</summary>
public enum CollisionTraceFlag : byte
{
    /// <summary>The project's default applies: simple queries use the simple shapes, complex queries the triangle mesh.</summary>
    UseDefault = 0,

    /// <summary>Simple queries use the simple shapes, complex queries the triangle mesh.</summary>
    SimpleAndComplex = 1,

    /// <summary>Both kinds of query use the simple shapes.</summary>
    SimpleAsComplex = 2,

    /// <summary>Both kinds of query use the triangle mesh.</summary>
    ComplexAsSimple = 3,
}

/// <summary>Members of the unversioned <c>UBodySetup</c> chain (a mesh's or component's collision shapes and settings).</summary>
public static class BodySetupMembers
{
    /// <summary><c>AggGeom</c>: an <c>FKAggregateGeom</c> struct (see <see cref="AggregateGeometryMembers"/>), the simple shapes.</summary>
    public const int AggregateGeometry = 0;

    /// <summary>Unknown member, one byte.</summary>
    public const int Unknown4 = 4;

    /// <summary>Unknown member, one byte.</summary>
    public const int Unknown7 = 7;

    /// <summary><c>PhysMaterial</c>: object reference (4 bytes), the physical material of the simple shapes.</summary>
    public const int PhysMaterial = 10;

    /// <summary><c>WalkableSlopeOverride</c>: an <c>FWalkableSlopeOverride</c> struct (see <see cref="WalkableSlopeOverrideMembers"/>).</summary>
    public const int WalkableSlopeOverride = 11;

    /// <summary><c>DefaultInstance</c>: an <c>FBodyInstance</c> struct (see <see cref="BodyInstanceMembers"/>), the collision of components that use the body's defaults.</summary>
    public const int DefaultInstance = 12;

    /// <summary><c>BuildScale3D</c>: vector of three doubles.</summary>
    public const int BuildScale3D = 13;

    /// <summary>Unknown member, one byte.</summary>
    public const int Unknown15 = 15;

    /// <summary><c>CollisionTraceFlag</c>: one byte, a <see cref="Collision.CollisionTraceFlag"/>.</summary>
    public const int CollisionTraceFlag = 16;

    /// <summary>Bytes of the value of a fixed-size member; <see langword="null"/> for structs and unknown members.</summary>
    public static int? FixedValueSize(int member) => member switch
    {
        Unknown4 or Unknown7 or Unknown15 or CollisionTraceFlag => 1,
        PhysMaterial => 4,
        BuildScale3D => 24,
        _ => null,
    };
}

/// <summary>Members of the unversioned <c>FKAggregateGeom</c> struct: arrays of simple shape elements.</summary>
public static class AggregateGeometryMembers
{
    /// <summary><c>SphereElems</c>: array of <c>FKSphereElem</c> (see <see cref="SphereElementMembers"/>).</summary>
    public const int SphereElements = 0;

    /// <summary><c>BoxElems</c>: array of <c>FKBoxElem</c> (see <see cref="BoxElementMembers"/>).</summary>
    public const int BoxElements = 1;

    /// <summary><c>SphylElems</c>: array of <c>FKSphylElem</c> capsules (see <see cref="CapsuleElementMembers"/>).</summary>
    public const int CapsuleElements = 2;

    /// <summary><c>ConvexElems</c>: array of <c>FKConvexElem</c> (see <see cref="ConvexElementMembers"/>).</summary>
    public const int ConvexElements = 3;

    /// <summary><c>TaperedCapsuleElems</c>: array of <c>FKTaperedCapsuleElem</c>.</summary>
    public const int TaperedCapsuleElements = 4;
}

/// <summary>
/// Members every shape element struct ends with (<c>FKShapeElem</c>), relative to the element's
/// <c>ShapeElementPart</c> index.
/// </summary>
public static class ShapeElementMembers
{
    /// <summary><c>RestOffset</c>: float.</summary>
    public const int RestOffset = 0;

    /// <summary><c>Name</c>: name (8 bytes).</summary>
    public const int Name = 1;

    /// <summary>Unknown member, one byte.</summary>
    public const int Unknown2 = 2;

    /// <summary>Unknown member, one byte.</summary>
    public const int Unknown3 = 3;

    /// <summary>Members of the part.</summary>
    public const int MemberCount = 4;

    /// <summary>Bytes of the value of a member of the part.</summary>
    public static int? FixedValueSize(int member) => member switch
    {
        RestOffset => 4,
        Name => 8,
        Unknown2 or Unknown3 => 1,
        _ => null,
    };
}

/// <summary>Members of the unversioned <c>FKSphereElem</c> struct.</summary>
public static class SphereElementMembers
{
    /// <summary><c>Center</c>: vector of three doubles.</summary>
    public const int Center = 0;

    /// <summary><c>Radius</c>: float.</summary>
    public const int Radius = 1;

    /// <summary>Index where the <see cref="ShapeElementMembers"/> part starts.</summary>
    public const int ShapeElementPart = 2;
}

/// <summary>Members of the unversioned <c>FKBoxElem</c> struct: a box of full size X × Y × Z around its centre.</summary>
public static class BoxElementMembers
{
    /// <summary><c>Center</c>: vector of three doubles.</summary>
    public const int Center = 0;

    /// <summary><c>Rotation</c>: rotator of three doubles (pitch, yaw, roll).</summary>
    public const int Rotation = 1;

    /// <summary><c>X</c>: float, the full size along the box's X axis.</summary>
    public const int X = 2;

    /// <summary><c>Y</c>: float, the full size along the box's Y axis.</summary>
    public const int Y = 3;

    /// <summary><c>Z</c>: float, the full size along the box's Z axis.</summary>
    public const int Z = 4;

    /// <summary>Index where the <see cref="ShapeElementMembers"/> part starts.</summary>
    public const int ShapeElementPart = 5;
}

/// <summary>Members of the unversioned <c>FKSphylElem</c> struct: a capsule along its local Z axis.</summary>
public static class CapsuleElementMembers
{
    /// <summary><c>Center</c>: vector of three doubles.</summary>
    public const int Center = 0;

    /// <summary><c>Rotation</c>: rotator of three doubles (pitch, yaw, roll).</summary>
    public const int Rotation = 1;

    /// <summary><c>Radius</c>: float.</summary>
    public const int Radius = 2;

    /// <summary><c>Length</c>: float, the length of the cylinder between the two caps' centres.</summary>
    public const int Length = 3;

    /// <summary>Index where the <see cref="ShapeElementMembers"/> part starts.</summary>
    public const int ShapeElementPart = 4;
}

/// <summary>Members of the unversioned <c>FKConvexElem</c> struct.</summary>
public static class ConvexElementMembers
{
    /// <summary><c>VertexData</c>: array of vectors of three doubles.</summary>
    public const int VertexData = 0;

    /// <summary><c>IndexData</c>: array of int32.</summary>
    public const int IndexData = 1;

    /// <summary><c>ElemBox</c>: a box (two vectors of three doubles and a validity byte).</summary>
    public const int ElementBox = 2;

    /// <summary><c>Transform</c>: an <c>FTransform</c> struct (see <see cref="TransformMembers"/>).</summary>
    public const int Transform = 3;

    /// <summary>Index where the <see cref="ShapeElementMembers"/> part starts.</summary>
    public const int ShapeElementPart = 4;

    /// <summary>Bytes of an <see cref="ElementBox"/> value.</summary>
    public const int ElementBoxSize = 49;
}

/// <summary>Members of the unversioned <c>FTransform</c> struct.</summary>
public static class TransformMembers
{
    /// <summary><c>Rotation</c>: quaternion of four doubles (X, Y, Z, W).</summary>
    public const int Rotation = 0;

    /// <summary><c>Translation</c>: vector of three doubles.</summary>
    public const int Translation = 1;

    /// <summary><c>Scale3D</c>: vector of three doubles.</summary>
    public const int Scale3D = 2;

    /// <summary>Members of the struct.</summary>
    public const int MemberCount = 3;
}
