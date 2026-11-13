using Ares.MapData.Packages;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Game.Collision;

namespace Ares.MapData.Components;

/// <summary>
/// Instanced static mesh components. A component either stores its mesh, body instance and per-instance data itself
/// (scatter components and native ones) or only its attach parent, with the rest in its template (instanced components
/// of packed level Blueprints). The per-instance data is native data after the properties: the element size, a count
/// and one matrix per instance in component space; each instance's world matrix is that matrix followed by the
/// component's world transform: its relative transform (its own scene members over its template's) on its attach parent's.
/// </summary>
internal sealed partial class ComponentReader
{
    private const int InstanceSearchTail = 8;
    private const int BodySearchBytes = 36;

    private static readonly Dictionary<PackageObjectIndex, InstancedComponentKind> s_instancedClasses = new()
    {
        [PackageObjectIndex.FromScriptPath(ScatterMeshComponentMembers.ClassPath)] = InstancedComponentKind.Scatter,
        [PackageObjectIndex.FromScriptPath(InstancedStaticMeshComponentMembers.ClassPath)] = InstancedComponentKind.Instanced,
        [PackageObjectIndex.FromScriptPath(HierarchicalInstancedStaticMeshComponentMembers.ClassPath)] = InstancedComponentKind.HierarchicalInstanced,
    };

    private readonly Dictionary<(CookedPackage, int), InstanceBlock> _templateInstances = [];
    private readonly Dictionary<(CookedPackage, int), ExportRef?> _meshReferences = [];

    /// <summary>Reads an instanced static mesh component placed in <paramref name="cell"/>.</summary>
    /// <exception cref="InvalidDataException">The component or its template cannot be decoded.</exception>
    public InstancedComponent ReadInstanced(CookedPackage cell, ZenExport export, InstancedComponentKind kind)
    {
        int start = CookedPackage.StartOf(export);
        int end = CookedPackage.EndOf(export);
        UnversionedHeader header = UnversionedHeader.Read(cell.Data, start);
        InstanceBlock block;
        InstanceScene scene;
        string? templatePath = null;
        int parent;
        if (FindInstanceArray(cell, header.ValuesOffset, end, null) is not null)
        {
            int? count = header.Members.Count > 0 && header.Members[0].Index == HierarchicalInstancedStaticMeshComponentMembers.SortedInstances
                ? cell.ReadInt32(header.ValuesOffset)
                : null;
            block = ReadInstanceBlock(cell, export, kind, count);
            parent = block.BodyEnd is { } bodyEnd ? cell.ReadInt32(bodyEnd) : 0;
            scene = block.Scene;
        }
        else
        {
            ExportRef template = Template(cell, export) ?? throw new InvalidDataException("The component stores no instances and has no template");
            templatePath = template.Path;
            if (!_templateInstances.TryGetValue((template.Package, template.Export.Index), out InstanceBlock? cached))
            {
                cached = ReadInstanceBlock(template.Package, template.Export, kind, null);
                _templateInstances[(template.Package, template.Export.Index)] = cached;
            }

            (int parentAt, parent, BodyInstanceValues? ownBody) = OwnAttachParent(cell, export, header, kind, end);
            block = ownBody is null ? cached : cached with { Body = Merge(ownBody, cached.Body) };
            int scenePart = StaticMeshChainStart(kind) + StaticMeshComponentMembers.ScenePart;
            int? sceneStart = header.IsNonZero(scenePart + SceneComponentMembers.AttachParent) ? parentAt : null;
            scene = ReadInstanceScene(cell, header, scenePart, sceneStart, end).Over(cached.Scene);
        }

        if (parent <= 0 || parent > cell.Exports.Count)
        {
            throw new InvalidDataException("The component's attach parent is not an export");
        }

        ZenExport parentExport = cell.Exports[parent - 1];
        Transform world = WorldTransform(cell, parentExport);
        if (scene.Socket is { } socket && socket != CookedPackage.NoneName && SocketTransform(cell, parentExport, socket) is { } socketTransform)
        {
            world = TransformMath.Compose(socketTransform, world);
        }

        AbsoluteMembers absolute = new(scene.AbsoluteLocation ?? false, scene.AbsoluteRotation ?? false, scene.AbsoluteScale ?? false);
        Vector3D location = scene.Location ?? default;
        Rotator rotation = scene.Rotation ?? default;
        Vector3D scale = scene.Scale ?? Vector3D.One;
        if (location != default || rotation != default || scale != Vector3D.One || absolute.Any)
        {
            world = TransformMath.Compose(TransformMath.FromParts(location, rotation, scale), world, absolute);
        }

        TransformMatrix componentWorld = TransformMath.ToMatrix(world);
        TransformMatrix[] instances = [.. block.Instances.Select(instance => instance.Then(componentWorld))];
        return new InstancedComponent(cell.Name, export.Index, cell.Header.ExportPath(export.Index), kind, block.Mesh.Path, block.Body, templatePath, instances);
    }

    /// <summary>
    /// The mesh, body instance and instances of a block that stores them. The mesh is the first static mesh import
    /// reference followed by a body instance: right after the members between the mesh and the body instance when their
    /// sizes are known, else the first body instance with a profile within a few bytes.
    /// </summary>
    private InstanceBlock ReadInstanceBlock(CookedPackage package, ZenExport export, InstancedComponentKind kind, int? count)
    {
        int start = CookedPackage.StartOf(export);
        int end = CookedPackage.EndOf(export);
        UnversionedHeader header = UnversionedHeader.Read(package.Data, start);
        (int arrayAt, int instanceCount) = FindInstanceArray(package, header.ValuesOffset, end, count)
            ?? throw new InvalidDataException("The component has no instance array");

        List<(int Offset, ExportRef Mesh)> meshes = [];
        for (int at = header.ValuesOffset; at < arrayAt - 4; at++)
        {
            if (MeshReference(package, package.ReadInt32(at)) is { } mesh)
            {
                meshes.Add((at, mesh));
            }
        }

        if (meshes.Count == 0)
        {
            throw new InvalidDataException("The component has no static mesh reference");
        }

        int chainStart = StaticMeshChainStart(kind);
        int meshIndex = chainStart + StaticMeshComponentMembers.StaticMesh;
        int bodyIndex = chainStart + StaticMeshComponentMembers.BodyInstance;
        int? between = header.Contains(meshIndex)
            ? header.Members
                .Where(member => member.IsNonZero && member.Index > meshIndex && member.Index < bodyIndex)
                .Aggregate((int?)0, (sum, member) => StaticMeshComponentMembers.FixedValueSize(member.Index - chainStart) is { } size ? sum + size : null)
            : 0;

        foreach ((int offset, ExportRef mesh) in meshes)
        {
            IEnumerable<int> candidates = between is { } known
                ? [offset + 4 + known]
                : Enumerable.Range(offset + 4, Math.Max(0, Math.Min(offset + 4 + BodySearchBytes, arrayAt) - (offset + 4)));
            foreach (int candidate in candidates)
            {
                if (BodyInstanceReader.TryRead(package, candidate, out int bodyEnd) is { } body && (between is not null || body.ProfileName is not null))
                {
                    return new InstanceBlock(mesh, body, bodyEnd, ReadInstances(package, arrayAt, instanceCount),
                        ReadInstanceScene(package, header, chainStart + StaticMeshComponentMembers.ScenePart, bodyEnd, arrayAt));
                }
            }
        }

        return new InstanceBlock(meshes[^1].Mesh, null, null, ReadInstances(package, arrayAt, instanceCount),
            ReadInstanceScene(package, header, chainStart + StaticMeshComponentMembers.ScenePart, null, arrayAt));
    }

    /// <summary>
    /// The per-instance data array in [<paramref name="start"/>, <paramref name="end"/>): the first element-size word
    /// followed by a positive count (equal to <paramref name="count"/> when given) whose first and last matrices have
    /// an affine last column.
    /// </summary>
    private static (int At, int Count)? FindInstanceArray(CookedPackage package, int start, int end, int? count)
    {
        const int ElementSize = InstancedStaticMeshComponentMembers.InstanceDataElementSize;
        for (int at = start; at + sizeof(int) <= end - InstanceSearchTail; at++)
        {
            if (package.ReadInt32(at) != ElementSize)
            {
                continue;
            }

            int found = package.ReadInt32(at + 4);
            if ((count is { } expected && found != expected) || found <= 0 || at + 8 + ((long)ElementSize * found) > end)
            {
                continue;
            }

            if (IsAffine(package, at + 8) && IsAffine(package, at + 8 + (ElementSize * (found - 1))))
            {
                return (at, found);
            }
        }

        return null;
    }

    private static bool IsAffine(CookedPackage package, int matrix)
    {
        return package.ReadDouble(matrix + 120) is 1.0 && package.ReadDouble(matrix + 24) is 0.0;
    }

    private static TransformMatrix[] ReadInstances(CookedPackage package, int arrayAt, int count)
    {
        TransformMatrix[] instances = new TransformMatrix[count];
        for (int i = 0; i < count; i++)
        {
            int at = arrayAt + 8 + (InstancedStaticMeshComponentMembers.InstanceDataElementSize * i);
            instances[i] = new TransformMatrix(package.ReadVector(at), package.ReadVector(at + 32), package.ReadVector(at + 64), package.ReadVector(at + 96));
        }

        return instances;
    }

    /// <summary>The static mesh an object reference value refers to, when it is an import of a static mesh.</summary>
    private ExportRef? MeshReference(CookedPackage package, int value)
    {
        if (!package.IsImportReference(value))
        {
            return null;
        }

        if (_meshReferences.TryGetValue((package, value), out ExportRef? mesh))
        {
            return mesh;
        }

        try
        {
            mesh = package.Header.Imports[-value - 1].Kind == PackageObjectKind.PackageImport ? packages.Reference(package, value) : null;
        }
        catch (InvalidDataException)
        {
            mesh = null;
        }

        mesh = mesh is { } found && found.Export.Class == s_staticMesh ? mesh : null;
        _meshReferences[(package, value)] = mesh;
        return mesh;
    }

    /// <summary>Index where the static mesh component chain starts in the chain of a <paramref name="kind"/> component.</summary>
    private static int StaticMeshChainStart(InstancedComponentKind kind) => kind == InstancedComponentKind.Instanced
        ? InstancedStaticMeshComponentMembers.OwnMembers
        : HierarchicalInstancedStaticMeshComponentMembers.StaticMeshComponentChainStart;

    /// <summary>
    /// The attach parent of a component whose instances are in its template: the one reference in its values to another
    /// component of the same actor. When the component stores its own body instance right before the attach parent, only
    /// a reference where that body instance ends counts, and the body instance is returned too. Returns the reference's
    /// offset as well.
    /// </summary>
    private static (int At, int Parent, BodyInstanceValues? Body) OwnAttachParent(CookedPackage cell, ZenExport export, UnversionedHeader header,
        InstancedComponentKind kind, int end)
    {
        int chainStart = StaticMeshChainStart(kind);
        int bodyIndex = chainStart + StaticMeshComponentMembers.BodyInstance;
        int parentIndex = chainStart + StaticMeshComponentMembers.ScenePart + SceneComponentMembers.AttachParent;
        List<(int At, int Value)> candidates = SiblingReferences(cell, export, header.ValuesOffset, end);
        if (!header.IsNonZero(bodyIndex))
        {
            return OnlyAttachParent(candidates, null);
        }

        if (header.Members.Any(member => member.IsNonZero && member.Index > bodyIndex && member.Index < parentIndex) || !header.IsNonZero(parentIndex))
        {
            throw new InvalidDataException("The component's own body instance cannot be located");
        }

        List<(int At, int Value, BodyInstanceValues Body)> framed = [];
        foreach ((int at, int value) in candidates)
        {
            IReadOnlyList<(int Start, BodyInstanceValues Values)> readings = BodyInstanceReader.FindEndingAt(cell, header.ValuesOffset, at);
            if (readings.Count == 1)
            {
                framed.Add((at, value, readings[0].Values));
            }
        }

        return OnlyAttachParent([.. framed.Select(candidate => (candidate.At, candidate.Value))], framed.Count == 1 ? framed[0].Body : null);
    }

    private static (int At, int Parent, BodyInstanceValues? Body) OnlyAttachParent(List<(int At, int Value)> candidates, BodyInstanceValues? body) =>
        candidates.Count == 1 ? (candidates[0].At, candidates[0].Value, body) : throw new InvalidDataException($"The attach parent has {candidates.Count} candidates");

    /// <summary>Each offset in the block's values holding a reference to another component of the same actor, with that reference.</summary>
    private static List<(int At, int Value)> SiblingReferences(CookedPackage cell, ZenExport export, int start, int end)
    {
        if (export.Outer.Kind != PackageObjectKind.Export)
        {
            throw new InvalidDataException("The component has no actor");
        }

        HashSet<int> siblings = [.. cell.ChildrenOf(export.Outer.ExportIndex)
            .GroupBy(child => child.Name).Select(group => group.Last())
            .Where(child => child.Index != export.Index)
            .Select(child => child.Index + 1)];
        List<(int At, int Value)> found = [];
        for (int at = start; at < end - 3; at++)
        {
            int value = cell.ReadInt32(at);
            if (siblings.Contains(value))
            {
                found.Add((at, value));
            }
        }

        return found;
    }

    /// <summary>A component's own body instance members over the ones its template stores.</summary>
    private static BodyInstanceValues Merge(BodyInstanceValues own, BodyInstanceValues? inherited)
    {
        return new BodyInstanceValues(
            own.ObjectType ?? inherited?.ObjectType,
            own.CollisionEnabled ?? inherited?.CollisionEnabled,
            own.ProfileName ?? inherited?.ProfileName,
            own.Responses ?? inherited?.Responses);
    }

    /// <summary>
    /// The scene members up to <c>bAbsoluteScale</c> that a block stores, read forwards from <paramref name="start"/>, where
    /// its scene part's values begin (<see langword="null"/> when unknown), up to <paramref name="limit"/>.
    /// </summary>
    /// <exception cref="InvalidDataException">The block stores such a member that cannot be located or stepped over.</exception>
    private static InstanceScene ReadInstanceScene(CookedPackage package, UnversionedHeader header, int scenePart, int? start, int limit)
    {
        Vector3D? location = null;
        Rotator? rotation = null;
        Vector3D? scale = null;
        string? socket = null;
        bool?[] absolute = new bool?[3];
        int? at = start;
        foreach (UnversionedMember member in header.Members)
        {
            int offset = member.Index - scenePart;
            if (offset < 0)
            {
                continue;
            }

            if (offset > SceneComponentMembers.AbsoluteScale)
            {
                break;
            }

            if (!member.IsNonZero)
            {
                switch (offset)
                {
                    case SceneComponentMembers.RelativeLocation:
                        location = default(Vector3D);
                        break;
                    case SceneComponentMembers.RelativeRotation:
                        rotation = default(Rotator);
                        break;
                    case SceneComponentMembers.RelativeScale3D:
                        scale = default(Vector3D);
                        break;
                    case >= SceneComponentMembers.AbsoluteLocation and <= SceneComponentMembers.AbsoluteScale:
                        absolute[offset - SceneComponentMembers.AbsoluteLocation] = false;
                        break;
                }

                continue;
            }

            int position = at ?? throw new InvalidDataException("The instanced component's scene part cannot be located");
            int size = offset switch
            {
                SceneComponentMembers.AttachParent => SceneComponentMembers.ReferenceSize,
                SceneComponentMembers.AttachSocketName => SceneComponentMembers.NameSize,
                SceneComponentMembers.RelativeLocation or SceneComponentMembers.RelativeRotation or SceneComponentMembers.RelativeScale3D =>
                    SceneComponentMembers.VectorSize,
                SceneComponentMembers.AttachChildren or SceneComponentMembers.ClientAttachedChildren => ReferenceArraySize(package, position, limit),
                _ => SceneComponentMembers.FixedValueSize(offset) ?? throw new InvalidDataException($"Scene member +{offset} cannot be stepped over"),
            };
            if (position > limit - size)
            {
                throw new InvalidDataException("The instanced component's scene part runs past its values");
            }

            switch (offset)
            {
                case SceneComponentMembers.AttachSocketName:
                    socket = package.ReadName(position);
                    break;
                case SceneComponentMembers.RelativeLocation:
                    location = package.ReadVector(position);
                    break;
                case SceneComponentMembers.RelativeRotation:
                    rotation = package.ReadRotator(position);
                    break;
                case SceneComponentMembers.RelativeScale3D:
                    scale = package.ReadVector(position);
                    break;
                case >= SceneComponentMembers.AbsoluteLocation and <= SceneComponentMembers.AbsoluteScale:
                    absolute[offset - SceneComponentMembers.AbsoluteLocation] = package.ReadByte(position) != 0;
                    break;
            }

            at = position + size;
        }

        return new InstanceScene(location, rotation, scale, socket, absolute[0], absolute[1], absolute[2]);
    }

    /// <summary>Bytes of the export-reference array at <paramref name="at"/>: its count and that many references.</summary>
    private static int ReferenceArraySize(CookedPackage package, int at, int limit)
    {
        int count = package.ReadInt32(at);
        return count >= 0 && count <= (limit - at - sizeof(int)) / SceneComponentMembers.ReferenceSize
            ? sizeof(int) + (count * SceneComponentMembers.ReferenceSize)
            : throw new InvalidDataException($"The instanced component's attached children count {count} does not fit");
    }

    /// <summary>What a block that stores instances holds.</summary>
    private sealed record InstanceBlock(ExportRef Mesh, BodyInstanceValues? Body, int? BodyEnd, IReadOnlyList<TransformMatrix> Instances, InstanceScene Scene);

    /// <summary>The relative transform members an instanced block stores (members it does not store are <see langword="null"/>).</summary>
    private sealed record InstanceScene(
        Vector3D? Location,
        Rotator? Rotation,
        Vector3D? Scale,
        string? Socket,
        bool? AbsoluteLocation,
        bool? AbsoluteRotation,
        bool? AbsoluteScale)
    {
        /// <summary>These members over the ones <paramref name="inherited"/> stores.</summary>
        public InstanceScene Over(InstanceScene inherited) => new(
            Location ?? inherited.Location,
            Rotation ?? inherited.Rotation,
            Scale ?? inherited.Scale,
            Socket ?? inherited.Socket,
            AbsoluteLocation ?? inherited.AbsoluteLocation,
            AbsoluteRotation ?? inherited.AbsoluteRotation,
            AbsoluteScale ?? inherited.AbsoluteScale);
    }
}
