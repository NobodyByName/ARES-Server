using System.Collections.Immutable;
using Ares.MapData.Components;
using Ares.MapData.Materials;
using Ares.MapData.Packages;
using Ares.Structs.Game.Collision;
using static Ares.MapData.Tests.PropertyBlocks;

namespace Ares.MapData.Tests;

public sealed class SurfaceLibraryTests
{
    private const string PackagePath = "/Game/Test/Materials";

    // Export references are 1-based export indices.
    private const int Concrete = 1;
    private const int Metal = 2;
    private const int Material = 3;
    private const int Instance = 4;
    private const int InstanceWithOwn = 5;
    private const int Dynamic = 6;
    private const int Mesh = 7;
    private const int Component = 8;

    private static readonly PackageObjectIndex s_physicalMaterial = PackageObjectIndex.FromScriptPath(PhysicalMaterialMembers.ClassPath);
    private static readonly PackageObjectIndex s_material = PackageObjectIndex.FromScriptPath(MaterialMembers.ClassPath);
    private static readonly PackageObjectIndex s_instance = PackageObjectIndex.FromScriptPath(MaterialInstanceConstantMembers.ClassPath);
    private static readonly PackageObjectIndex s_dynamic = PackageObjectIndex.FromScriptPath(MaterialInstanceDynamicMembers.ClassPath);
    private static readonly PackageObjectIndex s_staticMesh = PackageObjectIndex.FromScriptPath(CollisionObjectClasses.StaticMesh);
    private static readonly PackageObjectIndex s_staticMeshComponent = PackageObjectIndex.FromScriptPath(StaticMeshComponentMembers.ClassPath);

    [Fact]
    public void A_physical_material_gives_its_surface_type_after_the_members_before_it()
    {
        (SurfaceLibrary surfaces, CookedPackage package) = Library();

        Assert.Equal(PhysicalSurfaceType.Concrete, surfaces.OfPhysicalMaterial(Export(package, Concrete)));
        Assert.Equal(PhysicalSurfaceType.Metal, surfaces.OfPhysicalMaterial(new StoredReference(package, Metal)));
        Assert.Null(surfaces.OfPhysicalMaterial(new StoredReference(package, 0)));
        Assert.Null(surfaces.OfPhysicalMaterial(null));
    }

    [Fact]
    public void A_material_instance_takes_its_own_physical_material_else_its_parents()
    {
        (SurfaceLibrary surfaces, CookedPackage package) = Library();

        Assert.Equal(PhysicalSurfaceType.Concrete, surfaces.OfMaterial(Export(package, Material)));
        Assert.Equal(PhysicalSurfaceType.Concrete, surfaces.OfMaterial(Export(package, Instance)));
        Assert.Equal(PhysicalSurfaceType.Metal, surfaces.OfMaterial(Export(package, InstanceWithOwn)));
        Assert.Equal(PhysicalSurfaceType.Metal, surfaces.OfMaterial(Export(package, Dynamic)));
        Assert.Equal(PhysicalSurfaceType.Default, surfaces.OfMaterial(null));
    }

    [Fact]
    public void Something_that_is_not_a_material_is_refused()
    {
        (SurfaceLibrary surfaces, CookedPackage package) = Library();

        Assert.Throws<InvalidDataException>(() => surfaces.OfMaterial(Export(package, Mesh)));
        Assert.Throws<InvalidDataException>(() => surfaces.OfPhysicalMaterial(Export(package, Material)));
    }

    [Fact]
    public void A_static_mesh_lists_the_materials_of_its_slots()
    {
        (SurfaceLibrary surfaces, CookedPackage package) = Library();

        ImmutableArray<ExportRef?> slots = surfaces.MeshMaterials($"{PackagePath}.SM_Ramp");

        Assert.Equal(2, slots.Length);
        Assert.Equal(package.Exports[Instance - 1], slots[0]?.Export);
        Assert.Null(slots[1]);
    }

    [Fact]
    public void Collision_takes_the_body_override_then_the_body_setup_then_the_first_slot()
    {
        (SurfaceLibrary surfaces, CookedPackage package) = Library();
        ExportRef?[] materials = [Export(package, InstanceWithOwn), Export(package, Material)];

        Assert.Equal(PhysicalSurfaceType.Concrete, surfaces.OfCollision(new StoredReference(package, Concrete), new StoredReference(package, Metal), materials, false).Surface);
        Assert.Equal(PhysicalSurfaceType.Concrete, surfaces.OfCollision(new StoredReference(package, 0), new StoredReference(package, Concrete), materials, false).Surface);
        Assert.Equal(PhysicalSurfaceType.Metal, surfaces.OfCollision(null, null, materials, false).Surface);
        Assert.Equal(PhysicalSurfaceType.Default, surfaces.OfCollision(null, null, [], false).Surface);
        Assert.True(surfaces.OfCollision(null, null, materials, false).MaterialSurfaces.IsEmpty);
        Assert.Equal([PhysicalSurfaceType.Metal, PhysicalSurfaceType.Concrete], surfaces.OfCollision(new StoredReference(package, Concrete), null, materials, true).MaterialSurfaces);
    }

    [Fact]
    public void A_static_mesh_component_stores_override_materials_after_its_streaming_texture_data()
    {
        (_, CookedPackage package) = Library();
        ComponentReader components = new(new PackageCache(path => path == PackagePath ? package : null, _ => null));

        IReadOnlyList<StoredReference>? overrides = components.OverrideMaterials(package, package.Exports[Component - 1]);

        Assert.NotNull(overrides);
        Assert.Equal([0, InstanceWithOwn], overrides.Select(reference => reference.Value));
    }

    private static (SurfaceLibrary Surfaces, CookedPackage Package) Library()
    {
        byte[] concrete =
        [
            .. Header(PhysicalMaterialMembers.Friction, PhysicalMaterialMembers.FrictionCombineMode, PhysicalMaterialMembers.SurfaceType),
            .. BitConverter.GetBytes(0.7f), 2, (byte)PhysicalSurfaceType.Concrete,
        ];
        byte[] metal = [.. Header(PhysicalMaterialMembers.SurfaceType), (byte)PhysicalSurfaceType.Metal];
        byte[] material = [.. Header(MaterialMembers.PhysMaterial), .. BitConverter.GetBytes(Concrete)];
        const int ConstantPart = MaterialInstanceConstantMembers.MaterialInstancePart;
        byte[] instance = [.. Header(ConstantPart + MaterialInstanceMembers.Parent), .. BitConverter.GetBytes(Material)];
        byte[] instanceWithOwn =
        [
            .. Header(ConstantPart + MaterialInstanceMembers.PhysMaterial, ConstantPart + MaterialInstanceMembers.Parent),
            .. BitConverter.GetBytes(Metal), .. BitConverter.GetBytes(Material),
        ];
        byte[] dynamic = [.. Header(MaterialInstanceDynamicMembers.MaterialInstancePart + MaterialInstanceMembers.Parent), .. BitConverter.GetBytes(InstanceWithOwn)];
        byte[] mesh =
        [
            .. Header(StaticMeshMembers.StaticMaterials),
            .. BitConverter.GetBytes(2),
            .. Header(StaticMaterialMembers.MaterialInterface, StaticMaterialMembers.MaterialSlotName, StaticMaterialMembers.UvChannelData),
            .. BitConverter.GetBytes(Instance), .. Name(0),
            .. Header(MeshUvChannelInfoMembers.Initialized, MeshUvChannelInfoMembers.LocalUvDensities), 1, .. BitConverter.GetBytes(512f),
            .. Header(StaticMaterialMembers.MaterialSlotName), .. Name(0),
        ];
        byte[] component =
        [
            .. Header(
                StaticMeshComponentMembers.StaticMesh,
                StaticMeshComponentMembers.StreamingTextureData,
                StaticMeshComponentMembers.Unknown48,
                StaticMeshComponentMembers.Unknown49,
                StaticMeshComponentMembers.OverrideMaterials),
            .. BitConverter.GetBytes(Mesh),
            .. BitConverter.GetBytes(1), .. Header(0, 1, 2), .. new byte[3 * StreamingTextureBuildInfoMembers.ValueSize],
            1,
            .. BitConverter.GetBytes(5f),
            .. BitConverter.GetBytes(2), .. BitConverter.GetBytes(0), .. BitConverter.GetBytes(InstanceWithOwn),
        ];
        byte[] data = SyntheticArchives.Package(PackagePath,
        [
            ("PM_Concrete", PackageObjectIndex.Null, s_physicalMaterial, 0, concrete),
            ("PM_Metal", PackageObjectIndex.Null, s_physicalMaterial, 0, metal),
            ("M_Base", PackageObjectIndex.Null, s_material, 0, material),
            ("MI_Base", PackageObjectIndex.Null, s_instance, 0, instance),
            ("MI_Metal", PackageObjectIndex.Null, s_instance, 0, instanceWithOwn),
            ("MID_Metal", PackageObjectIndex.Null, s_dynamic, 0, dynamic),
            ("SM_Ramp", PackageObjectIndex.Null, s_staticMesh, 0, mesh),
            ("StaticMeshComponent0", PackageObjectIndex.Null, s_staticMeshComponent, 0, component),
        ]);
        CookedPackage package = CookedPackage.Parse(data);
        return (new SurfaceLibrary(new PackageCache(path => path == PackagePath ? package : null, _ => null)), package);
    }

    private static ExportRef Export(CookedPackage package, int reference) => new(package, package.Exports[reference - 1]);
}
