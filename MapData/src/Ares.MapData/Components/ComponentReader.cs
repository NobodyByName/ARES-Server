using Ares.MapData.Packages;
using Ares.MapData.Profiles;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Game.Collision;

namespace Ares.MapData.Components;

/// <summary>
/// Reads the collision components of cooked cell packages: their world transforms (relative transforms along attach
/// chains, attach sockets of parent meshes, absolute members), meshes or owned body setups, and collision settings.
/// Members a component does not store come from its template chain (Blueprint component templates), then the class
/// default. Results are cached per export, so read the cells of one map with one reader.
/// </summary>
internal sealed partial class ComponentReader(PackageCache packages)
{
    private const int MaxTemplateDepth = 16;
    private const int MaxCollisionTemplateDepth = 12;
    private const int MaxAttachDepth = 16;
    private const int BodyInstanceSearchBytes = 800;
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

    private static readonly Dictionary<PackageObjectIndex, CollisionComponentKind> s_collisionClasses = new()
    {
        [PackageObjectIndex.FromScriptPath(StaticMeshComponentMembers.ClassPath)] = CollisionComponentKind.StaticMesh,
        [PackageObjectIndex.FromScriptPath(SplineMeshComponentMembers.ClassPath)] = CollisionComponentKind.SplineMesh,
        [PackageObjectIndex.FromScriptPath(BrushComponentMembers.ClassPath)] = CollisionComponentKind.Brush,
        [PackageObjectIndex.FromScriptPath(BoxComponentMembers.ClassPath)] = CollisionComponentKind.Box,
        [PackageObjectIndex.FromScriptPath(SphereComponentMembers.ClassPath)] = CollisionComponentKind.Sphere,
        [PackageObjectIndex.FromScriptPath(CapsuleComponentMembers.ClassPath)] = CollisionComponentKind.Capsule,
    };

    private static readonly PackageObjectIndex s_staticMeshComponent = PackageObjectIndex.FromScriptPath(StaticMeshComponentMembers.ClassPath);
    private static readonly PackageObjectIndex s_bodySetup = PackageObjectIndex.FromScriptPath(CollisionObjectClasses.BodySetup);
    private static readonly PackageObjectIndex s_staticMesh = PackageObjectIndex.FromScriptPath(CollisionObjectClasses.StaticMesh);
    private static readonly PackageObjectIndex s_socket = PackageObjectIndex.FromScriptPath(StaticMeshSocketMembers.ClassPath);
    private static readonly PackageObjectIndex s_tagSocket = PackageObjectIndex.FromScriptPath(StaticMeshSocketMembers.GameplayTagSocketClassPath);

    private readonly Dictionary<(CookedPackage, int), SceneMembers> _scenes = [];
    private readonly Dictionary<(CookedPackage, int), Transform> _worlds = [];

    /// <summary>Reads every collision component of the package at <paramref name="cellPath"/>.</summary>
    /// <exception cref="InvalidDataException">The package is not in the game archives.</exception>
    public CellComponents ReadCell(string cellPath)
    {
        CookedPackage cell = packages.Load(cellPath) ?? throw new InvalidDataException($"{cellPath} is not in the game archives");
        List<PlacedComponent> placed = [];
        List<InstancedComponent> instanced = [];
        List<ComponentFailure> failures = [];
        foreach (ZenExport export in cell.Exports)
        {
            try
            {
                if (s_collisionClasses.TryGetValue(export.Class, out CollisionComponentKind kind))
                {
                    placed.Add(ReadPlaced(cell, export, kind));
                }
                else if (s_instancedClasses.TryGetValue(export.Class, out InstancedComponentKind instancedKind))
                {
                    instanced.Add(ReadInstanced(cell, export, instancedKind));
                }
            }
            catch (InvalidDataException error)
            {
                failures.Add(new ComponentFailure(cell.Name, export.Index, cell.Header.ExportPath(export.Index), error.Message));
            }
        }

        return new CellComponents(cell.Name, placed, instanced, failures);
    }

    /// <summary>The class of a component export with its own collision, or <see langword="null"/> for other exports.</summary>
    public static CollisionComponentKind? CollisionKindOf(ZenExport export) =>
        s_collisionClasses.TryGetValue(export.Class, out CollisionComponentKind kind) ? kind : null;

    /// <summary>The world transform of the scene component <paramref name="export"/>.</summary>
    /// <exception cref="InvalidDataException">The component, a template or an attach parent cannot be decoded.</exception>
    public Transform WorldTransform(CookedPackage package, ZenExport export) => WorldTransform(package, export, 0);

    /// <summary>The collision settings of the collision component <paramref name="export"/>, merged along its template chain.</summary>
    /// <exception cref="InvalidDataException">The component or a template cannot be decoded, or the chain does not end at a class default object.</exception>
    public CollisionSettings Collision(CookedPackage package, ZenExport export)
    {
        string classPath = SceneClass(export).ClassPath;
        bool? useDefault = null;
        string? profile = null;
        CollisionEnabled? enabled = null;
        CollisionChannel? objectType = null;
        IReadOnlyList<CollisionResponseOverride>? responses = null;
        CookedPackage current = package;
        ZenExport level = export;
        for (int depth = 0; ; depth++)
        {
            (bool? ownUseDefault, BodyInstanceValues? body) = OwnCollision(current, level);
            useDefault ??= ownUseDefault;
            profile ??= body?.ProfileName;
            enabled ??= body?.CollisionEnabled;
            objectType ??= body?.ObjectType;
            responses ??= body?.Responses;

            PackageObjectIndex template = level.Template;
            if (template.IsNull)
            {
                throw new InvalidDataException("The component has no template");
            }

            if (template.Kind == PackageObjectKind.ScriptImport)
            {
                string defaultObject = packages.ScriptPath(template) ?? template.ToString();
                return new CollisionSettings(useDefault, profile, enabled, objectType, responses, defaultObject,
                    ComponentCollisionDefaults.Find(defaultObject, classPath));
            }

            ExportRef next = packages.Resolve(current, template) ?? throw new InvalidDataException("The component's template cannot be resolved");
            if (next.Export.Class != level.Class)
            {
                throw new InvalidDataException("The component's template has another class");
            }

            if (depth + 1 > MaxCollisionTemplateDepth)
            {
                throw new InvalidDataException("The component's template chain is too long");
            }

            (current, level) = (next.Package, next.Export);
        }
    }

    /// <summary>The static mesh a static mesh component shows (its own member or its nearest template's), or <see langword="null"/>.</summary>
    /// <exception cref="InvalidDataException">The component or a template cannot be decoded.</exception>
    public ExportRef? StaticMesh(CookedPackage package, ZenExport export)
    {
        for (int depth = 0; ; depth++)
        {
            if (depth > MaxTemplateDepth)
            {
                throw new InvalidDataException("The component's template chain is too long");
            }

            if (export.Class != s_staticMeshComponent)
            {
                throw new InvalidDataException("The component is not a static mesh component");
            }

            UnversionedHeader header = UnversionedHeader.Read(package.Data, CookedPackage.StartOf(export));
            if (header.Members.Any(member => member.Index < StaticMeshComponentMembers.StaticMesh && member.Index != StaticMeshComponentMembers.Unknown0))
            {
                throw new InvalidDataException("A member before StaticMesh has an unknown size");
            }

            if (!header.Contains(StaticMeshComponentMembers.StaticMesh))
            {
                if (Template(package, export) is not { } template)
                {
                    return null;
                }

                (package, export) = (template.Package, template.Export);
                continue;
            }

            int value = header.IsNonZero(StaticMeshComponentMembers.StaticMesh)
                ? package.ReadInt32(ValueOffset(header, StaticMeshComponentMembers.StaticMesh, StaticMeshComponentMembers.FixedValueSize))
                : 0;
            return packages.Reference(package, value);
        }
    }

    private PlacedComponent ReadPlaced(CookedPackage cell, ZenExport export, CollisionComponentKind kind)
    {
        Transform world = WorldTransform(cell, export);
        string? mesh = null;
        string? bodySetup = null;
        switch (kind)
        {
            case CollisionComponentKind.StaticMesh:
                mesh = StaticMesh(cell, export)?.Path;
                break;
            case CollisionComponentKind.SplineMesh or CollisionComponentKind.Brush:
            {
                ZenExport[] owned = [.. cell.ChildrenOf(export.Index).Where(child => child.Class == s_bodySetup)];
                bodySetup = owned.Length == 1 ? cell.Header.ExportPath(owned[0].Index) : null;
                break;
            }
        }

        return new PlacedComponent(cell.Name, export.Index, cell.Header.ExportPath(export.Index), kind, world, mesh, bodySetup, Collision(cell, export));
    }

    private (string ClassPath, int ScenePart, NativeTail Tail) SceneClass(ZenExport export)
    {
        return s_sceneClasses.TryGetValue(export.Class, out (string ClassPath, int ScenePart, NativeTail Tail) found)
            ? found
            : throw new InvalidDataException($"Component class {packages.ScriptPath(export.Class) ?? export.Class.ToString()} is not supported");
    }

    private SceneMembers Scene(CookedPackage package, ZenExport export)
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

    /// <summary>The collision members one export stores: <c>bUseDefaultCollision</c> (static mesh components) and its body instance.</summary>
    private (bool? UseDefault, BodyInstanceValues? Body) OwnCollision(CookedPackage package, ZenExport export)
    {
        (_, int scenePart, _) = SceneClass(export);
        UnversionedHeader header = UnversionedHeader.Read(package.Data, CookedPackage.StartOf(export));
        bool? useDefault = null;
        if (export.Class == s_staticMeshComponent && header.Contains(StaticMeshComponentMembers.UseDefaultCollision))
        {
            useDefault = header.IsNonZero(StaticMeshComponentMembers.UseDefaultCollision)
                && package.ReadByte(ValueOffset(header, StaticMeshComponentMembers.UseDefaultCollision, StaticMeshComponentMembers.FixedValueSize)) != 0;
        }

        int bodyIndex = scenePart - PrimitiveComponentMembers.BodyInstanceBeforeScenePart;
        if (!header.IsNonZero(bodyIndex))
        {
            return (useDefault, null);
        }

        int bodyEnd = Scene(package, export).ScenePartOffset;
        IReadOnlyList<(int Start, BodyInstanceValues Values)> hits =
            BodyInstanceReader.FindEndingAt(package, Math.Max(header.ValuesOffset, bodyEnd - BodyInstanceSearchBytes), bodyEnd);
        return hits.Count == 1 ? (useDefault, hits[0].Values) : throw new InvalidDataException($"The body instance has {hits.Count} readings");
    }

    /// <summary>Offset of member <paramref name="index"/>'s value when every stored member before it has a known fixed size.</summary>
    private static int ValueOffset(UnversionedHeader header, int index, Func<int, int?> fixedSize)
    {
        return header.ValuesOffset + header.Members
            .TakeWhile(member => member.Index < index)
            .Where(member => member.IsNonZero)
            .Sum(member => fixedSize(member.Index) ?? throw new InvalidDataException($"Member {member.Index} before member {index} has an unknown size"));
    }

    /// <summary>The component's template, or <see langword="null"/> when it is created from a class default object.</summary>
    private ExportRef? Template(CookedPackage package, ZenExport export)
    {
        if (export.Template.Kind is PackageObjectKind.Null or PackageObjectKind.ScriptImport)
        {
            return null;
        }

        ExportRef template = packages.Resolve(package, export.Template) ?? throw new InvalidDataException("The component's template cannot be resolved");
        return template.Export.Class == export.Class ? template : throw new InvalidDataException("The component's template has another class");
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
            ExportRef target = packages.Reference(package, reference) ?? throw new InvalidDataException("The attach parent cannot be resolved");
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

    /// <summary>
    /// The transform of socket <paramref name="name"/> of the static mesh that <paramref name="parent"/> shows, or
    /// <see langword="null"/> when the parent has no such socket (no static mesh, or a mesh without it): like the engine,
    /// the child then attaches to the parent's own transform.
    /// </summary>
    private Transform? SocketTransform(CookedPackage package, ZenExport parent, string name)
    {
        if ((parent.Class == s_staticMeshComponent ? StaticMesh(package, parent) : null) is not { } mesh)
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
