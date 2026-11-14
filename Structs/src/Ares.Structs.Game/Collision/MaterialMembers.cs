namespace Ares.Structs.Game.Collision;

/// <summary>Members of a <c>UPhysicalMaterial</c>'s unversioned property chain.</summary>
public static class PhysicalMaterialMembers
{
    /// <summary>Script path of the class.</summary>
    public const string ClassPath = "/Script/PhysicsCore.PhysicalMaterial";

    /// <summary><c>Friction</c>: float.</summary>
    public const int Friction = 0;

    /// <summary><c>StaticFriction</c>: float.</summary>
    public const int StaticFriction = 1;

    /// <summary><c>FrictionCombineMode</c>: one byte.</summary>
    public const int FrictionCombineMode = 2;

    /// <summary><c>bOverrideFrictionCombineMode</c>: one byte.</summary>
    public const int OverrideFrictionCombineMode = 3;

    /// <summary><c>Restitution</c>: float.</summary>
    public const int Restitution = 4;

    /// <summary><c>RestitutionCombineMode</c>: one byte.</summary>
    public const int RestitutionCombineMode = 5;

    /// <summary><c>bOverrideRestitutionCombineMode</c>: one byte.</summary>
    public const int OverrideRestitutionCombineMode = 6;

    /// <summary><c>Density</c>: float.</summary>
    public const int Density = 7;

    /// <summary><c>RaiseMassToPower</c>: float.</summary>
    public const int RaiseMassToPower = 11;

    /// <summary><c>SurfaceType</c>: one byte, a <see cref="PhysicalSurfaceType"/>.</summary>
    public const int SurfaceType = 14;

    /// <summary>Bytes of the value of a fixed-size member before <see cref="SurfaceType"/>; <see langword="null"/> when unknown.</summary>
    public static int? FixedValueSize(int member) => member switch
    {
        Friction or StaticFriction or Restitution or Density or RaiseMassToPower => 4,
        FrictionCombineMode or OverrideFrictionCombineMode or RestitutionCombineMode or OverrideRestitutionCombineMode => 1,
        _ => null,
    };
}

/// <summary>Members of a <c>UMaterial</c>'s unversioned property chain.</summary>
public static class MaterialMembers
{
    /// <summary>Script path of the class.</summary>
    public const string ClassPath = "/Script/Engine.Material";

    /// <summary><c>PhysMaterial</c>: object reference (4 bytes), the material's physical material.</summary>
    public const int PhysMaterial = 0;
}

/// <summary>
/// Members of the <c>UMaterialInstance</c> part of a material instance's unversioned property chain, relative to the
/// class's <c>MaterialInstancePart</c>. An instance without its own physical material takes its parent's.
/// </summary>
public static class MaterialInstanceMembers
{
    /// <summary><c>PhysMaterial</c>: object reference (4 bytes), the instance's own physical material.</summary>
    public const int PhysMaterial = 0;

    /// <summary><c>PhysicalMaterialMap</c>: eight object references (4 bytes each).</summary>
    public const int PhysicalMaterialMap = 1;

    /// <summary>Number of <see cref="PhysicalMaterialMap"/> entries.</summary>
    public const int PhysicalMaterialMapLength = 8;

    /// <summary><c>Parent</c>: object reference (4 bytes), the material or instance this one derives from.</summary>
    public const int Parent = PhysicalMaterialMap + PhysicalMaterialMapLength;

    /// <summary>Bytes of an object reference.</summary>
    public const int ReferenceSize = 4;
}

/// <summary>Chain facts of a <c>UMaterialInstanceConstant</c>.</summary>
public static class MaterialInstanceConstantMembers
{
    /// <summary>Script path of the class.</summary>
    public const string ClassPath = "/Script/Engine.MaterialInstanceConstant";

    /// <summary>Index where the <see cref="MaterialInstanceMembers"/> part starts.</summary>
    public const int MaterialInstancePart = 1;
}

/// <summary>Chain facts of a <c>UMaterialInstanceDynamic</c>.</summary>
public static class MaterialInstanceDynamicMembers
{
    /// <summary>Script path of the class.</summary>
    public const string ClassPath = "/Script/Engine.MaterialInstanceDynamic";

    /// <summary>Index where the <see cref="MaterialInstanceMembers"/> part starts.</summary>
    public const int MaterialInstancePart = 0;
}

/// <summary>Members of a <c>UStaticMesh</c>'s unversioned property chain.</summary>
public static class StaticMeshMembers
{
    /// <summary>
    /// <c>StaticMaterials</c>: array of <c>FStaticMaterial</c> structs (see <see cref="StaticMaterialMembers"/>), the mesh's
    /// material slots; cooked triangle meshes store a slot per triangle.
    /// </summary>
    public const int StaticMaterials = 3;
}

/// <summary>Members of the unversioned <c>FStaticMaterial</c> struct: one material slot of a static mesh.</summary>
public static class StaticMaterialMembers
{
    /// <summary><c>MaterialInterface</c>: object reference (4 bytes).</summary>
    public const int MaterialInterface = 0;

    /// <summary><c>MaterialSlotName</c>: name (8 bytes).</summary>
    public const int MaterialSlotName = 1;

    /// <summary><c>ImportedMaterialSlotName</c>: name (8 bytes).</summary>
    public const int ImportedMaterialSlotName = 2;

    /// <summary><c>UVChannelData</c>: an <c>FMeshUVChannelInfo</c> struct (see <see cref="MeshUvChannelInfoMembers"/>).</summary>
    public const int UvChannelData = 3;

    /// <summary>Unknown member.</summary>
    public const int Unknown4 = 4;

    /// <summary>Members of the struct's chain.</summary>
    public const int MemberCount = 5;

    /// <summary>Bytes of the value of a fixed-size member; <see langword="null"/> for the struct member and unknown members.</summary>
    public static int? FixedValueSize(int member) => member switch
    {
        MaterialInterface => 4,
        MaterialSlotName or ImportedMaterialSlotName => 8,
        _ => null,
    };
}

/// <summary>Members of the unversioned <c>FMeshUVChannelInfo</c> struct.</summary>
public static class MeshUvChannelInfoMembers
{
    /// <summary><c>bInitialized</c>: one byte.</summary>
    public const int Initialized = 0;

    /// <summary><c>bOverrideDensities</c>: one byte.</summary>
    public const int OverrideDensities = 1;

    /// <summary><c>LocalUVDensities</c>: four floats, members 2 to 5.</summary>
    public const int LocalUvDensities = 2;

    /// <summary>Members of the struct's chain.</summary>
    public const int MemberCount = 6;

    /// <summary>Bytes of the value of a member.</summary>
    public static int ValueSize(int member) => member is Initialized or OverrideDensities ? 1 : sizeof(float);
}
