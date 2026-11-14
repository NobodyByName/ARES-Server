using System.Collections.Immutable;
using Ares.Common.Maps;
using Ares.MapData.Packages;
using Ares.Structs.Game.Collision;

namespace Ares.MapData.Materials;

/// <summary>
/// The physical surface types of the physical materials, materials and static mesh material slots one extraction meets,
/// each read once. A material's physical material is its own, else (for a material instance) its parent's; a material
/// chain without one, and a missing material, give the engine's default physical material, whose surface type is
/// <see cref="PhysicalSurfaceType.Default"/>.
/// </summary>
internal sealed class SurfaceLibrary
{
    private const int MaxParentDepth = 32;
    private const int MaxMaterialSlots = 256;

    private static readonly PackageObjectIndex s_physicalMaterial = PackageObjectIndex.FromScriptPath(PhysicalMaterialMembers.ClassPath);
    private static readonly PackageObjectIndex s_material = PackageObjectIndex.FromScriptPath(MaterialMembers.ClassPath);
    private static readonly PackageObjectIndex s_materialInstanceConstant = PackageObjectIndex.FromScriptPath(MaterialInstanceConstantMembers.ClassPath);
    private static readonly PackageObjectIndex s_materialInstanceDynamic = PackageObjectIndex.FromScriptPath(MaterialInstanceDynamicMembers.ClassPath);
    private static readonly PackageObjectIndex s_staticMesh = PackageObjectIndex.FromScriptPath(CollisionObjectClasses.StaticMesh);

    private readonly PackageCache _packages;
    private readonly Dictionary<string, PhysicalSurfaceType> _physicalMaterials = new(StringComparer.Ordinal);
    private readonly Dictionary<string, PhysicalSurfaceType> _materials = new(StringComparer.Ordinal);
    private readonly Dictionary<string, ImmutableArray<ExportRef?>> _meshMaterials = new(StringComparer.Ordinal);

    /// <summary>A library over the packages of <paramref name="packages"/>.</summary>
    public SurfaceLibrary(PackageCache packages)
    {
        _packages = packages;
    }

    /// <summary>
    /// The surface type of the physical material a stored reference names, or <see langword="null"/> when nothing is
    /// stored or the reference is none.
    /// </summary>
    /// <exception cref="InvalidDataException">The reference is not a physical material or the material does not decode.</exception>
    public PhysicalSurfaceType? OfPhysicalMaterial(StoredReference? reference) =>
        reference is { } stored && _packages.Reference(stored) is { } material ? OfPhysicalMaterial(material) : null;

    /// <summary>
    /// The surface types of a component's collision: <see cref="BodyPlacement.Surface"/> from the body instance's
    /// physical material override, else the body setup's physical material, else the first material slot's, else
    /// <see cref="PhysicalSurfaceType.Default"/>; and, for a body with triangle meshes, the surface type of each material
    /// slot (<see cref="BodyPlacement.MaterialSurfaces"/>).
    /// </summary>
    /// <param name="bodyOverride">The component's physical material override as stored, if any.</param>
    /// <param name="bodyMaterial">The body setup's physical material as stored, if any.</param>
    /// <param name="materials">The component's material per slot (none for components that show no mesh).</param>
    /// <param name="hasTriangleMeshes">Whether the body collides through triangle meshes.</param>
    /// <exception cref="InvalidDataException">A physical material or material does not decode.</exception>
    public (PhysicalSurfaceType Surface, ImmutableArray<PhysicalSurfaceType> MaterialSurfaces) OfCollision(
        StoredReference? bodyOverride,
        StoredReference? bodyMaterial,
        IReadOnlyList<ExportRef?> materials,
        bool hasTriangleMeshes)
    {
        PhysicalSurfaceType surface = OfPhysicalMaterial(bodyOverride)
            ?? OfPhysicalMaterial(bodyMaterial)
            ?? (materials.Count > 0 ? OfMaterial(materials[0]) : PhysicalSurfaceType.Default);
        return (surface, hasTriangleMeshes ? [.. materials.Select(OfMaterial)] : []);
    }

    /// <summary>The surface type of a physical material export.</summary>
    /// <exception cref="InvalidDataException">The export is not a physical material or does not decode.</exception>
    public PhysicalSurfaceType OfPhysicalMaterial(ExportRef material)
    {
        if (_physicalMaterials.TryGetValue(material.Path, out PhysicalSurfaceType surface))
        {
            return surface;
        }

        if (material.Export.Class != s_physicalMaterial)
        {
            throw new InvalidDataException($"{material.Path} is a {ClassName(material)}, not a physical material");
        }

        CookedPackage package = material.Package;
        UnversionedHeader header = UnversionedHeader.Read(package.Data, CookedPackage.StartOf(material.Export));
        surface = PhysicalSurfaceType.Default;
        if (header.IsNonZero(PhysicalMaterialMembers.SurfaceType))
        {
            byte value = package.ReadByte(ValueOffset(header, PhysicalMaterialMembers.SurfaceType, PhysicalMaterialMembers.FixedValueSize));
            surface = value <= (byte)PhysicalSurfaceType.Ice
                ? (PhysicalSurfaceType)value
                : throw new InvalidDataException($"{material.Path} has surface type {value}");
        }

        _physicalMaterials[material.Path] = surface;
        return surface;
    }

    /// <summary>
    /// The surface type of a material or material instance: that of its physical material, else its parent's, else
    /// <see cref="PhysicalSurfaceType.Default"/>. A missing material gives <see cref="PhysicalSurfaceType.Default"/>.
    /// </summary>
    /// <exception cref="InvalidDataException">The export is not a known material class or does not decode.</exception>
    public PhysicalSurfaceType OfMaterial(ExportRef? material) => material is { } found ? OfMaterial(found, 0) : PhysicalSurfaceType.Default;

    /// <summary>
    /// The materials of a static mesh's slots (its <c>StaticMaterials</c>), in slot order; a slot without a material is
    /// <see langword="null"/>.
    /// </summary>
    /// <param name="meshPath">Object path of the static mesh.</param>
    /// <exception cref="InvalidDataException">The mesh does not exist or its material slots do not decode.</exception>
    public ImmutableArray<ExportRef?> MeshMaterials(string meshPath)
    {
        if (_meshMaterials.TryGetValue(meshPath, out ImmutableArray<ExportRef?> materials))
        {
            return materials;
        }

        int dot = meshPath.IndexOf('.', meshPath.LastIndexOf('/') + 1);
        if (dot < 0)
        {
            throw new InvalidDataException($"{meshPath} is not an object path");
        }

        CookedPackage package = _packages.Load(meshPath[..dot]) ?? throw new InvalidDataException($"{meshPath[..dot]} is not in the game archives");
        ZenExport mesh = package.Exports.FirstOrDefault(export => export.Class == s_staticMesh && package.Header.ExportPath(export.Index) == meshPath)
            ?? throw new InvalidDataException($"{package.Name} has no static mesh {meshPath}");
        materials = ReadMeshMaterials(package, mesh);
        _meshMaterials[meshPath] = materials;
        return materials;
    }

    /// <summary>Resolves stored object references to material exports (<see langword="null"/> for none).</summary>
    /// <exception cref="InvalidDataException">A reference is out of range or cannot be resolved.</exception>
    public ImmutableArray<ExportRef?> Resolve(IReadOnlyList<StoredReference> references) => [.. references.Select(_packages.Reference)];

    private PhysicalSurfaceType OfMaterial(ExportRef material, int depth)
    {
        if (_materials.TryGetValue(material.Path, out PhysicalSurfaceType surface))
        {
            return surface;
        }

        if (depth > MaxParentDepth)
        {
            throw new InvalidDataException($"{material.Path}: the material parent chain is too long");
        }

        CookedPackage package = material.Package;
        UnversionedHeader header = UnversionedHeader.Read(package.Data, CookedPackage.StartOf(material.Export));
        if (material.Export.Class == s_material)
        {
            surface = header.IsNonZero(MaterialMembers.PhysMaterial)
                ? OfPhysicalMaterial(new StoredReference(package, package.ReadInt32(header.ValuesOffset))) ?? PhysicalSurfaceType.Default
                : PhysicalSurfaceType.Default;
        }
        else if (InstancePart(material.Export) is { } part)
        {
            surface = PhysicalSurfaceType.Default;
            int physMaterial = part + MaterialInstanceMembers.PhysMaterial;
            int parentIndex = part + MaterialInstanceMembers.Parent;
            PhysicalSurfaceType? own = header.IsNonZero(physMaterial)
                ? OfPhysicalMaterial(new StoredReference(package, package.ReadInt32(InstanceValueOffset(header, part, physMaterial))))
                : null;
            if (own is { } found)
            {
                surface = found;
            }
            else if (header.IsNonZero(parentIndex)
                && _packages.Reference(package, package.ReadInt32(InstanceValueOffset(header, part, parentIndex))) is { } parent)
            {
                surface = OfMaterial(parent, depth + 1);
            }
        }
        else
        {
            throw new InvalidDataException($"{material.Path} is a {ClassName(material)}, not a known material class");
        }

        _materials[material.Path] = surface;
        return surface;
    }

    /// <summary>Where the material instance part starts in a material instance class's chain; <see langword="null"/> for other classes.</summary>
    private static int? InstancePart(ZenExport export) =>
        export.Class == s_materialInstanceConstant ? MaterialInstanceConstantMembers.MaterialInstancePart
        : export.Class == s_materialInstanceDynamic ? MaterialInstanceDynamicMembers.MaterialInstancePart
        : null;

    /// <summary>
    /// Offset of a material instance member's value; every stored member before it must be one of the part's object
    /// references.
    /// </summary>
    private static int InstanceValueOffset(UnversionedHeader header, int part, int index)
    {
        return ValueOffset(header, index, member => member - part is >= MaterialInstanceMembers.PhysMaterial and <= MaterialInstanceMembers.Parent
            ? MaterialInstanceMembers.ReferenceSize
            : null);
    }

    private ImmutableArray<ExportRef?> ReadMeshMaterials(CookedPackage package, ZenExport mesh)
    {
        UnversionedHeader header = UnversionedHeader.Read(package.Data, CookedPackage.StartOf(mesh));
        if (!header.IsNonZero(StaticMeshMembers.StaticMaterials))
        {
            return [];
        }

        int at = ValueOffset(header, StaticMeshMembers.StaticMaterials, _ => null);
        int end = CookedPackage.EndOf(mesh);
        int count = package.ReadInt32(at);
        if (count is < 0 or > MaxMaterialSlots)
        {
            throw new InvalidDataException($"{package.Name}: {mesh.Name} has {count} material slots");
        }

        at += sizeof(int);
        ImmutableArray<ExportRef?>.Builder materials = ImmutableArray.CreateBuilder<ExportRef?>(count);
        for (int slot = 0; slot < count; slot++)
        {
            UnversionedHeader element = UnversionedHeader.Read(package.Data, at, StaticMaterialMembers.MemberCount);
            at = element.ValuesOffset;
            ExportRef? material = null;
            foreach (UnversionedMember member in element.Members.Where(member => member.IsNonZero))
            {
                switch (member.Index)
                {
                    case StaticMaterialMembers.MaterialInterface:
                        material = _packages.Reference(package, package.ReadInt32(at));
                        break;
                    case StaticMaterialMembers.UvChannelData:
                        UnversionedHeader channels = UnversionedHeader.Read(package.Data, at, MeshUvChannelInfoMembers.MemberCount);
                        at = channels.ValuesOffset + channels.Members.Where(value => value.IsNonZero).Sum(value => MeshUvChannelInfoMembers.ValueSize(value.Index));
                        continue;
                }

                at += StaticMaterialMembers.FixedValueSize(member.Index)
                    ?? throw new InvalidDataException($"{package.Name}: static material member {member.Index} of {mesh.Name} has an unknown size");
            }

            if (at > end)
            {
                throw new InvalidDataException($"{package.Name}: the material slots of {mesh.Name} run past the export");
            }

            materials.Add(material);
        }

        return materials.MoveToImmutable();
    }

    /// <summary>Offset of member <paramref name="index"/>'s value when every stored member before it has a known fixed size.</summary>
    private static int ValueOffset(UnversionedHeader header, int index, Func<int, int?> fixedSize)
    {
        return header.ValuesOffset + header.Members
            .TakeWhile(member => member.Index < index)
            .Where(member => member.IsNonZero)
            .Sum(member => fixedSize(member.Index) ?? throw new InvalidDataException($"Member {member.Index} before member {index} has an unknown size"));
    }

    private string ClassName(ExportRef export) => _packages.ScriptPath(export.Export.Class) ?? export.Export.Class.ToString();
}
