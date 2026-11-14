namespace Ares.Structs.Game.Collision;

/// <summary>Class facts of a <c>ALandscapeStreamingProxy</c>: the actor that owns a part of a landscape in one streaming cell.</summary>
public static class LandscapeStreamingProxyMembers
{
    /// <summary>Script path of the class.</summary>
    public const string ClassPath = "/Script/Landscape.LandscapeStreamingProxy";
}

/// <summary>
/// Members of a <c>ULandscapeHeightfieldCollisionComponent</c>'s unversioned property chain: its own members, then the
/// primitive members, then the scene part at <see cref="ScenePart"/>. The cooked height field follows the properties
/// (read by <see cref="LandscapeCollision.ReadCookedHeightField"/>).
/// </summary>
public static class LandscapeHeightfieldCollisionComponentMembers
{
    /// <summary>Script path of the class.</summary>
    public const string ClassPath = "/Script/Landscape.LandscapeHeightfieldCollisionComponent";

    /// <summary><c>ComponentLayerInfos</c>: array of object references.</summary>
    public const int ComponentLayerInfos = 0;

    /// <summary><c>SectionBaseX</c>: int32, the component's first quad along X in landscape quads.</summary>
    public const int SectionBaseX = 1;

    /// <summary><c>SectionBaseY</c>: int32, the component's first quad along Y in landscape quads.</summary>
    public const int SectionBaseY = 2;

    /// <summary><c>CollisionSizeQuads</c>: int32, quads per side of the collision grid (samples per side minus one).</summary>
    public const int CollisionSizeQuads = 3;

    /// <summary><c>CollisionScale</c>: float, distance between collision samples in landscape units.</summary>
    public const int CollisionScale = 4;

    /// <summary>Unknown member, 16 bytes.</summary>
    public const int Unknown7 = 7;

    /// <summary><c>CachedLocalBox</c>: box of six doubles and a validity byte.</summary>
    public const int CachedLocalBox = 8;

    /// <summary>Unknown member: object reference (4 bytes).</summary>
    public const int Unknown9 = 9;

    /// <summary>
    /// <c>CookedPhysicalMaterials</c>: array of object references, the physical materials the cooked height field's
    /// material indices refer to.
    /// </summary>
    public const int CookedPhysicalMaterials = 10;

    /// <summary>Index of the scene part.</summary>
    public const int ScenePart = 128;

    /// <summary>Bytes of an element of the object reference arrays.</summary>
    public const int ReferenceSize = 4;

    /// <summary>Bytes of the value of a fixed-size own member; <see langword="null"/> for arrays and unknown members.</summary>
    public static int? FixedValueSize(int member) => member switch
    {
        SectionBaseX or SectionBaseY or CollisionSizeQuads or CollisionScale or Unknown9 => 4,
        Unknown7 => 16,
        CachedLocalBox => 49,
        _ => null,
    };

    /// <summary>Whether an own member is an array of object references (an int32 count, then the references).</summary>
    public static bool IsReferenceArray(int member) => member is ComponentLayerInfos or CookedPhysicalMaterials;
}
