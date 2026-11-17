using Ares.GameDataExtract.Packages;
using Ares.GameDataExtract.Profiles;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Game.Collision;

namespace Ares.GameDataExtract.Components;

/// <summary>
/// Reads the collision components of cooked cell packages: their world transforms (resolved by <see cref="Scenes"/>),
/// meshes or owned body setups, and collision settings. Members a component does not store come from its template
/// chain (Blueprint component templates), then the class default. Results are cached per export, so read the cells of
/// one map with one reader.
/// </summary>
internal sealed partial class ComponentReader
{
    private const int MaxCollisionTemplateDepth = 12;
    private const int BodyInstanceSearchBytes = 800;

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

    private readonly PackageCache _packages;

    /// <summary>A reader that loads and follows packages with <paramref name="packages"/>.</summary>
    public ComponentReader(PackageCache packages)
    {
        _packages = packages;
        Scenes = new SceneResolver(packages, StaticMesh);
    }

    /// <summary>Resolves the template chains, scene members and world transforms of the components this reader reads.</summary>
    public SceneResolver Scenes { get; }

    /// <summary>Reads every collision component of the package at <paramref name="cellPath"/>.</summary>
    /// <exception cref="InvalidDataException">The package is not in the game archives.</exception>
    public CellComponents ReadCell(string cellPath)
    {
        CookedPackage cell = _packages.Load(cellPath) ?? throw new InvalidDataException($"{cellPath} is not in the game archives");
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

    /// <summary>The collision settings of the collision component <paramref name="export"/>, merged along its template chain.</summary>
    /// <exception cref="InvalidDataException">The component or a template cannot be decoded, or the chain does not end at a class default object.</exception>
    public CollisionSettings Collision(CookedPackage package, ZenExport export)
    {
        string classPath = Scenes.SceneClass(export).ClassPath;
        bool? useDefault = null;
        string? profile = null;
        CollisionEnabled? enabled = null;
        CollisionChannel? objectType = null;
        IReadOnlyList<CollisionResponseOverride>? responses = null;
        StoredReference? physMaterial = null;
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
            physMaterial ??= body?.PhysMaterialOverride;

            PackageObjectIndex template = level.Template;
            if (template.IsNull)
            {
                throw new InvalidDataException("The component has no template");
            }

            if (template.Kind == PackageObjectKind.ScriptImport)
            {
                string defaultObject = _packages.ScriptPath(template) ?? template.ToString();
                return new CollisionSettings(useDefault, profile, enabled, objectType, responses, defaultObject,
                    ComponentCollisionDefaults.Find(defaultObject, classPath), physMaterial);
            }

            ExportRef next = _packages.Resolve(current, template) ?? throw new InvalidDataException("The component's template cannot be resolved");
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

    /// <summary>
    /// The static mesh a static mesh or spline mesh component shows (its own member or its nearest template's), or
    /// <see langword="null"/>.
    /// </summary>
    /// <exception cref="InvalidDataException">The component or a template cannot be decoded.</exception>
    public ExportRef? StaticMesh(CookedPackage package, ZenExport export)
    {
        int chainStart = MeshChainStart(export);
        int meshIndex = chainStart + StaticMeshComponentMembers.StaticMesh;
        for (int depth = 0; ; depth++)
        {
            if (depth > SceneResolver.MaxTemplateDepth)
            {
                throw new InvalidDataException("The component's template chain is too long");
            }

            UnversionedHeader header = UnversionedHeader.Read(package.Data, CookedPackage.StartOf(export));
            if (chainStart == 0 && header.Members.Any(member => member.Index < meshIndex && member.Index != StaticMeshComponentMembers.Unknown0))
            {
                throw new InvalidDataException("A member before StaticMesh has an unknown size");
            }

            if (!header.Contains(meshIndex))
            {
                if (Scenes.Template(package, export) is not { } template)
                {
                    return null;
                }

                (package, export) = (template.Package, template.Export);
                continue;
            }

            int value = header.IsNonZero(meshIndex) ? package.ReadInt32(MeshValueOffset(package, export, header, chainStart)) : 0;
            return _packages.Reference(package, value);
        }
    }

    /// <summary>
    /// Offset of the stored <c>StaticMesh</c> value of a component: every member before it must have a known size (in a
    /// spline mesh component, its own decoded members and then the static mesh part's).
    /// </summary>
    /// <exception cref="InvalidDataException">The value cannot be located.</exception>
    private static int MeshValueOffset(CookedPackage package, ZenExport export, UnversionedHeader header, int chainStart)
    {
        int meshIndex = chainStart + StaticMeshComponentMembers.StaticMesh;
        if (chainStart == 0)
        {
            return ValueOffset(header, meshIndex, StaticMeshComponentMembers.FixedValueSize);
        }

        return SplineOwnMembersEnd(package, export, header) + header.Members
            .Where(member => member.IsNonZero && member.Index >= chainStart && member.Index < meshIndex)
            .Sum(member => StaticMeshComponentMembers.FixedValueSize(member.Index - chainStart)
                ?? throw new InvalidDataException($"Member {member.Index} before StaticMesh has an unknown size"));
    }

    /// <summary>
    /// The override materials of a static mesh or spline mesh component (its own <c>OverrideMaterials</c> or its nearest
    /// template's), or <see langword="null"/> when none in the chain stores them.
    /// </summary>
    /// <exception cref="InvalidDataException">A member before the override materials has an unknown size, or a template cannot be resolved.</exception>
    public IReadOnlyList<StoredReference>? OverrideMaterials(CookedPackage package, ZenExport export)
    {
        int chainStart = MeshChainStart(export);
        for (int depth = 0; ; depth++)
        {
            if (depth > SceneResolver.MaxTemplateDepth)
            {
                throw new InvalidDataException("The component's template chain is too long");
            }

            UnversionedHeader header = UnversionedHeader.Read(package.Data, CookedPackage.StartOf(export));
            int index = chainStart + StaticMeshComponentMembers.OverrideMaterials;
            if (header.Contains(index))
            {
                return header.IsNonZero(index) ? ReadOverrideMaterials(package, export, header, chainStart, MaterialsSearchStart(package, export, header, chainStart)) : [];
            }

            if (Scenes.Template(package, export) is not { } template)
            {
                return null;
            }

            (package, export) = (template.Package, template.Export);
        }
    }

    /// <summary>Index where the static mesh component chain starts in a static mesh or spline mesh component's chain.</summary>
    private static int MeshChainStart(ZenExport export)
    {
        return CollisionKindOf(export) switch
        {
            CollisionComponentKind.StaticMesh => 0,
            CollisionComponentKind.SplineMesh => SplineMeshComponentMembers.StaticMeshPart,
            CollisionComponentKind.Brush or CollisionComponentKind.Box or CollisionComponentKind.Sphere or CollisionComponentKind.Capsule or null =>
                throw new InvalidDataException("The component is not a static mesh or spline mesh component"),
            _ => throw new InvalidDataException("The component is not a static mesh or spline mesh component"),
        };
    }

    /// <summary>
    /// Where stepping forwards to the override materials starts: the first value of a static mesh component; in a spline
    /// mesh component, which must then store its static mesh, the value after it. Returns the offset and the first member
    /// index still to step over.
    /// </summary>
    private static (int At, int FirstMember) MaterialsSearchStart(CookedPackage package, ZenExport export, UnversionedHeader header, int chainStart)
    {
        int meshIndex = chainStart + StaticMeshComponentMembers.StaticMesh;
        if (chainStart == 0)
        {
            return (header.ValuesOffset, 0);
        }

        return header.IsNonZero(meshIndex)
            ? (MeshValueOffset(package, export, header, chainStart) + SceneComponentMembers.ReferenceSize, meshIndex + 1)
            : throw new InvalidDataException("The override materials of a spline mesh component that stores no mesh cannot be located");
    }

    private static List<StoredReference> ReadOverrideMaterials(CookedPackage package, ZenExport export, UnversionedHeader header, int chainStart,
        (int At, int FirstMember) start)
    {
        const int MaxMaterials = 256;
        int end = CookedPackage.EndOf(export);
        int index = chainStart + StaticMeshComponentMembers.OverrideMaterials;
        int at = start.At;
        foreach (UnversionedMember member in header.Members.Where(member => member.IsNonZero && member.Index >= start.FirstMember && member.Index < index))
        {
            int own = member.Index - chainStart;
            at = own == StaticMeshComponentMembers.StreamingTextureData
                ? SkipStreamingTextureData(package, at, end)
                : at + ((own >= 0 ? StaticMeshComponentMembers.FixedValueSize(own) : null)
                    ?? throw new InvalidDataException($"Member {member.Index} before the override materials has an unknown size"));
        }

        int count = package.ReadInt32(at);
        if (count is < 0 or > MaxMaterials || at + sizeof(int) + ((long)count * sizeof(int)) > end)
        {
            throw new InvalidDataException($"Invalid override material count {count}");
        }

        List<StoredReference> materials = new(count);
        for (int slot = 0; slot < count; slot++)
        {
            materials.Add(new StoredReference(package, package.ReadInt32(at + sizeof(int) + (slot * sizeof(int)))));
        }

        return materials;
    }

    /// <summary>Steps over a <c>StreamingTextureData</c> array: a count, then structs framed by their own headers.</summary>
    private static int SkipStreamingTextureData(CookedPackage package, int at, int end)
    {
        int count = package.ReadInt32(at);
        if (count < 0 || count > (end - at) / sizeof(short))
        {
            throw new InvalidDataException($"Invalid streaming texture data count {count}");
        }

        at += sizeof(int);
        for (int i = 0; i < count; i++)
        {
            UnversionedHeader element = UnversionedHeader.Read(package.Data, at, StreamingTextureBuildInfoMembers.MemberCount);
            at = element.ValuesOffset + (element.NonZeroCount * StreamingTextureBuildInfoMembers.ValueSize);
        }

        return at <= end ? at : throw new InvalidDataException("The streaming texture data runs past the export");
    }

    private PlacedComponent ReadPlaced(CookedPackage cell, ZenExport export, CollisionComponentKind kind)
    {
        Transform world = Scenes.WorldTransform(cell, export);
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

    /// <summary>The collision members one export stores: <c>bUseDefaultCollision</c> (static mesh components) and its body instance.</summary>
    private (bool? UseDefault, BodyInstanceValues? Body) OwnCollision(CookedPackage package, ZenExport export)
    {
        (_, int scenePart, _) = Scenes.SceneClass(export);
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

        int bodyEnd = Scenes.Scene(package, export).ScenePartOffset;
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
}
