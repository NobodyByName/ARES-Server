namespace Ares.Structs.Game.Collision;

/// <summary>The collision a component has when neither it nor its templates set any: its class default object's.</summary>
/// <param name="ProfileName">Collision profile name.</param>
/// <param name="UseDefaultCollision">Whether a static mesh component takes its collision from its mesh's body setup.</param>
public sealed record ComponentCollisionDefault(string ProfileName, bool UseDefaultCollision);

/// <summary>
/// Collision defaults of the native and script class default objects that placed components are created from. Script
/// class default objects are not cooked; a static mesh component of a script actor behaves like the static mesh actor's,
/// and other script components keep their engine class's constructor default.
/// </summary>
public static class ComponentCollisionDefaults
{
    private const string ScriptDefaultPrefix = "/Script/Angelscript.Default__";
    private const string ScatterActorStaticMeshComponent = "/Script/Angelscript.Default__StaticMeshScatterActor:StaticMeshComponent0";
    private const string VolumeBrushComponent = "BrushComponent0";
    private const string StaticMeshActorComponent = "/Script/Engine.Default__StaticMeshActor:StaticMeshComponent0";
    private const string StaticMeshComponent = "/Script/Engine.Default__StaticMeshComponent";

    private static readonly Dictionary<string, ComponentCollisionDefault> s_native = new(StringComparer.Ordinal)
    {
        [StaticMeshActorComponent] = new ComponentCollisionDefault("BlockAll", true),
        [StaticMeshComponent] = new ComponentCollisionDefault("BlockAllDynamic", false),
        ["/Script/Engine.Default__SplineMeshComponent"] = new ComponentCollisionDefault("BlockAllDynamic", false),
        ["/Script/Engine.Default__BlockingVolume:BrushComponent0"] = new ComponentCollisionDefault("BlockingVolume", false),
        ["/Script/Engine.Default__TriggerVolume:BrushComponent0"] = new ComponentCollisionDefault("Trigger", false),
        ["/Script/Engine.Default__PostProcessVolume:BrushComponent0"] = new ComponentCollisionDefault("NoCollision", false),
        ["/Script/NavigationSystem.Default__NavModifierVolume:BrushComponent0"] = new ComponentCollisionDefault("NoCollision", false),
        ["/Script/NavigationSystem.Default__NavMeshBoundsVolume:BrushComponent0"] = new ComponentCollisionDefault("NoCollision", false),
        ["/Script/Engine.Default__BoxReflectionCapture:DrawBox0"] = new ComponentCollisionDefault("NoCollision", false),
        ["/Script/Engine.Default__BoxReflectionCapture:DrawBox1"] = new ComponentCollisionDefault("NoCollision", false),
        ["/Script/Engine.Default__BoxComponent"] = new ComponentCollisionDefault("OverlapAllDynamic", false),
        ["/Script/Engine.Default__SphereComponent"] = new ComponentCollisionDefault("OverlapAllDynamic", false),
        ["/Script/Engine.Default__CapsuleComponent"] = new ComponentCollisionDefault("OverlapAllDynamic", false),
        ["/Script/Engine.Default__TriggerBox:CollisionComp"] = new ComponentCollisionDefault("Trigger", false),
    };

    /// <summary>
    /// The collision default of instanced static mesh components (scatter, instanced and hierarchical instanced ones),
    /// which keep the static mesh component's.
    /// </summary>
    public static ComponentCollisionDefault InstancedStaticMesh => s_native[StaticMeshComponent];

    /// <summary>
    /// The collision default of the class default object <paramref name="defaultObjectPath"/> (for example
    /// <c>/Script/Engine.Default__StaticMeshActor:StaticMeshComponent0</c>) for a component of class
    /// <paramref name="componentClassPath"/>; <see langword="null"/> when the default object is not known.
    /// </summary>
    public static ComponentCollisionDefault? Find(string defaultObjectPath, string componentClassPath)
    {
        if (s_native.TryGetValue(defaultObjectPath, out ComponentCollisionDefault? known))
        {
            return known;
        }

        if (!defaultObjectPath.Contains(ScriptDefaultPrefix, StringComparison.Ordinal))
        {
            return null;
        }

        if (defaultObjectPath.EndsWith(ScatterActorStaticMeshComponent, StringComparison.Ordinal))
        {
            return s_native[StaticMeshActorComponent];
        }

        string subobject = defaultObjectPath[(defaultObjectPath.LastIndexOfAny([':', '.']) + 1)..];
        if (subobject == VolumeBrushComponent)
        {
            return new ComponentCollisionDefault("OverlapAll", false);
        }

        return componentClassPath switch
        {
            StaticMeshComponentMembers.ClassPath or SplineMeshComponentMembers.ClassPath => new ComponentCollisionDefault("BlockAllDynamic", false),
            BrushComponentMembers.ClassPath => new ComponentCollisionDefault("OverlapAll", false),
            _ => new ComponentCollisionDefault("OverlapAllDynamic", false),
        };
    }
}
