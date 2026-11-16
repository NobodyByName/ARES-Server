using Ares.MapData.Packages;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Game.Collision;

namespace Ares.MapData.Components;

/// <summary>
/// Resolves scene components of cooked packages: their template chains (Blueprint component templates), their scene
/// members merged along those chains over the class defaults, and their world transforms (relative transforms along
/// attach chains, attach sockets of parent meshes, absolute members). Results are cached per export, so resolve the
/// cells of one map with one resolver.
/// </summary>
internal sealed class SceneResolver
{
    /// <summary>Most templates a component's template chain may have.</summary>
    public const int MaxTemplateDepth = 16;

    private const int MaxAttachDepth = 16;
    private const int MaxSocketTags = 16;

    private static readonly Dictionary<PackageObjectIndex, (string ClassPath, int ScenePart, NativeTail Tail)> s_sceneClasses = new (string ClassPath, int ScenePart, NativeTail Tail)[]
    {
        (StaticMeshComponentMembers.ClassPath, StaticMeshComponentMembers.ScenePart, NativeTail.StaticMesh),
        (SplineMeshComponentMembers.ClassPath, SplineMeshComponentMembers.ScenePart, NativeTail.StaticMesh),
        (BrushComponentMembers.ClassPath, BrushComponentMembers.ScenePart, NativeTail.NoneOrStaticMesh),
        (BoxComponentMembers.ClassPath, BoxComponentMembers.ScenePart, NativeTail.None),
        (SphereComponentMembers.ClassPath, SphereComponentMembers.ScenePart, NativeTail.None),
        (CapsuleComponentMembers.ClassPath, CapsuleComponentMembers.ScenePart, NativeTail.None),
        (SceneComponentMembers.ClassPath, SceneComponentMembers.ScenePart, NativeTail.None),
        (LevelInstanceComponentMembers.ClassPath, LevelInstanceComponentMembers.ScenePart, NativeTail.None),
        (SplineComponentMembers.ClassPath, SplineComponentMembers.ScenePart, NativeTail.None),
        (LandscapeSplinesComponentMembers.ClassPath, LandscapeSplinesComponentMembers.ScenePart, NativeTail.None),
    }.ToDictionary(entry => PackageObjectIndex.FromScriptPath(entry.ClassPath), entry => entry);

    private static readonly PackageObjectIndex s_staticMeshComponent = PackageObjectIndex.FromScriptPath(StaticMeshComponentMembers.ClassPath);
    private static readonly PackageObjectIndex s_socket = PackageObjectIndex.FromScriptPath(StaticMeshSocketMembers.ClassPath);
    private static readonly PackageObjectIndex s_tagSocket = PackageObjectIndex.FromScriptPath(StaticMeshSocketMembers.GameplayTagSocketClassPath);

    private readonly PackageCache _packages;
    private readonly Func<CookedPackage, ZenExport, ExportRef?> _staticMesh;
    private readonly Dictionary<(CookedPackage, int), SceneMembers> _scenes = [];
    private readonly Dictionary<(CookedPackage, int), Transform> _worlds = [];

    /// <summary>
    /// A resolver that follows references with <paramref name="packages"/> and finds the attach sockets of a static mesh
    /// component in the static mesh that <paramref name="staticMesh"/> gives for it.
    /// </summary>
    public SceneResolver(PackageCache packages, Func<CookedPackage, ZenExport, ExportRef?> staticMesh)
    {
        _packages = packages;
        _staticMesh = staticMesh;
    }

    /// <summary>The class path, scene part index and native tail of a scene component class.</summary>
    /// <exception cref="InvalidDataException">The export's class is not a supported scene component class.</exception>
    public (string ClassPath, int ScenePart, NativeTail Tail) SceneClass(ZenExport export)
    {
        return s_sceneClasses.TryGetValue(export.Class, out (string ClassPath, int ScenePart, NativeTail Tail) found)
            ? found
            : throw new InvalidDataException($"Component class {_packages.ScriptPath(export.Class) ?? export.Class.ToString()} is not supported");
    }

    /// <summary>The scene members the component <paramref name="export"/> stores itself.</summary>
    /// <exception cref="InvalidDataException">The class is not supported or the scene part cannot be decoded.</exception>
    public SceneMembers Scene(CookedPackage package, ZenExport export)
    {
        if (_scenes.TryGetValue((package, export.Index), out SceneMembers? scene))
        {
            return scene;
        }

        (_, int scenePart, NativeTail tail) = SceneClass(export);
        scene = ScenePart.Read(package, export, scenePart, tail);
        _scenes[(package, export.Index)] = scene;
        return scene;
    }

    /// <summary>The component's template, or <see langword="null"/> when it is created from a class default object.</summary>
    /// <exception cref="InvalidDataException">The template cannot be resolved or has another class.</exception>
    public ExportRef? Template(CookedPackage package, ZenExport export)
    {
        if (export.Template.Kind is PackageObjectKind.Null or PackageObjectKind.ScriptImport)
        {
            return null;
        }

        ExportRef template = _packages.Resolve(package, export.Template) ?? throw new InvalidDataException("The component's template cannot be resolved");
        return template.Export.Class == export.Class ? template : throw new InvalidDataException("The component's template has another class");
    }

    /// <summary>The world transform of the scene component <paramref name="export"/>.</summary>
    /// <exception cref="InvalidDataException">The component, a template or an attach parent cannot be decoded.</exception>
    public Transform WorldTransform(CookedPackage package, ZenExport export) => WorldTransform(package, export, 0);

    /// <summary>
    /// The transform of socket <paramref name="name"/> of the static mesh that <paramref name="parent"/> shows, or
    /// <see langword="null"/> when the parent has no such socket (no static mesh, or a mesh without it): like the engine,
    /// the child then attaches to the parent's own transform.
    /// </summary>
    public Transform? SocketTransform(CookedPackage package, ZenExport parent, string name)
    {
        if ((parent.Class == s_staticMeshComponent ? _staticMesh(package, parent) : null) is not { } mesh)
        {
            return null;
        }

        foreach (ZenExport export in mesh.Package.ChildrenOf(mesh.Export.Index))
        {
            if (export.Class != s_socket && export.Class != s_tagSocket)
            {
                continue;
            }

            (string? socketName, Transform transform) = ReadSocket(mesh.Package, export);
            if (string.Equals(socketName, name, StringComparison.OrdinalIgnoreCase))
            {
                return transform;
            }
        }

        return null;
    }

    private Transform WorldTransform(CookedPackage package, ZenExport export, int depth)
    {
        if (_worlds.TryGetValue((package, export.Index), out Transform cached))
        {
            return cached;
        }

        if (depth > MaxAttachDepth)
        {
            throw new InvalidDataException("The attachment chain is too long");
        }

        MergedScene members = Members(package, export, 0);
        Transform own = TransformMath.FromParts(members.Location, members.Rotation, members.Scale);
        ZenExport? parent = members.Parent;
        if (members.ParentName is { } name)
        {
            parent = (export.Outer.Kind == PackageObjectKind.Export ? package.Child(export.Outer.ExportIndex, name) : null)
                ?? throw new InvalidDataException($"The inherited attach parent {name} is not in the actor");
        }

        Transform world = own;
        if (parent is not null)
        {
            if (parent.Index == export.Index)
            {
                throw new InvalidDataException("The component is attached to itself");
            }

            Transform parentWorld = WorldTransform(package, parent, depth + 1);
            if (members.Socket is { } socket && socket != CookedPackage.NoneName && SocketTransform(package, parent, socket) is { } socketTransform)
            {
                parentWorld = TransformMath.Compose(socketTransform, parentWorld);
            }

            world = TransformMath.Compose(own, parentWorld, members.Absolute);
        }

        _worlds[(package, export.Index)] = world;
        return world;
    }

    /// <summary>
    /// A component's scene members, its own over its template chain's, then the class defaults. The attach parent is an
    /// export for the component's own member and a component name of the same actor when inherited from a template.
    /// </summary>
    private MergedScene Members(CookedPackage package, ZenExport export, int depth)
    {
        if (depth > MaxTemplateDepth)
        {
            throw new InvalidDataException("The component's template chain is too long");
        }

        SceneMembers own = Scene(package, export);
        bool parentSet = own.Parent is not null;
        ZenExport? parent = null;
        string? parentName = null;
        if (own.Parent is { } reference and not 0)
        {
            ExportRef target = _packages.Reference(package, reference) ?? throw new InvalidDataException("The attach parent cannot be resolved");
            if (!ReferenceEquals(target.Package, package))
            {
                throw new InvalidDataException("The attach parent is outside the package");
            }

            (parent, parentName) = depth == 0 ? (target.Export, null) : ((ZenExport?)null, target.Export.Name);
        }

        MergedScene inherited = Template(package, export) is { } template ? Members(template.Package, template.Export, depth + 1) : MergedScene.Defaults;
        return new MergedScene(
            own.Location ?? inherited.Location,
            own.Rotation ?? inherited.Rotation,
            own.Scale ?? inherited.Scale,
            own.Socket ?? inherited.Socket,
            parentSet ? parent : inherited.Parent,
            parentSet ? parentName : inherited.ParentName,
            new AbsoluteMembers(
                own.AbsoluteLocation ?? inherited.Absolute.Location,
                own.AbsoluteRotation ?? inherited.Absolute.Rotation,
                own.AbsoluteScale ?? inherited.Absolute.Scale));
    }

    /// <summary>Name and relative transform of a mesh socket export.</summary>
    private static (string? Name, Transform Transform) ReadSocket(CookedPackage package, ZenExport export)
    {
        int first = export.Class == s_tagSocket ? 1 : 0;
        UnversionedHeader header = UnversionedHeader.Read(package.Data, CookedPackage.StartOf(export));
        int at = header.ValuesOffset;
        string? name = null;
        Vector3D location = default;
        Rotator rotation = default;
        Vector3D scale = Vector3D.One;
        foreach (UnversionedMember member in header.Members)
        {
            int index = member.Index - first;
            if (member.Index < first)
            {
                if (member.IsNonZero)
                {
                    uint count = package.ReadUInt32(at);
                    if (count is 0 or > MaxSocketTags)
                    {
                        throw new InvalidDataException($"Socket {package.Header.ExportPath(export.Index)} has {count} tags");
                    }

                    at += sizeof(uint) + (int)(count * SceneComponentMembers.NameSize);
                }

                continue;
            }

            switch (index)
            {
                case StaticMeshSocketMembers.SocketName:
                    name = member.IsNonZero ? package.ReadName(at) : null;
                    at += member.IsNonZero ? SceneComponentMembers.NameSize : 0;
                    break;
                case StaticMeshSocketMembers.RelativeLocation:
                    location = member.IsNonZero ? package.ReadVector(at) : default;
                    at += member.IsNonZero ? SceneComponentMembers.VectorSize : 0;
                    break;
                case StaticMeshSocketMembers.RelativeRotation:
                    rotation = member.IsNonZero ? package.ReadRotator(at) : default;
                    at += member.IsNonZero ? SceneComponentMembers.VectorSize : 0;
                    break;
                case StaticMeshSocketMembers.RelativeScale:
                    scale = member.IsNonZero ? package.ReadVector(at) : default;
                    at += member.IsNonZero ? SceneComponentMembers.VectorSize : 0;
                    break;
                case StaticMeshSocketMembers.Tag:
                    if (member.IsNonZero)
                    {
                        int length = package.ReadInt32(at);
                        at += sizeof(int) + (length >= 0 ? length : -length * sizeof(char));
                    }

                    break;
                default:
                    throw new InvalidDataException($"Socket {package.Header.ExportPath(export.Index)} has an unexpected member {member.Index}");
            }
        }

        return CookedPackage.EndOf(export) - at == StaticMeshSocketMembers.NativeDataSize
            ? (name, TransformMath.FromParts(location, rotation, scale))
            : throw new InvalidDataException($"Socket {package.Header.ExportPath(export.Index)} does not end where expected");
    }

    /// <summary>Scene members merged along a template chain.</summary>
    private sealed record MergedScene(
        Vector3D Location,
        Rotator Rotation,
        Vector3D Scale,
        string? Socket,
        ZenExport? Parent,
        string? ParentName,
        AbsoluteMembers Absolute)
    {
        public static MergedScene Defaults { get; } = new(default, default, Vector3D.One, null, null, null, default);
    }
}
