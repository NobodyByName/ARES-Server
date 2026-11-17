using System.Collections.Immutable;
using System.Numerics;
using Ares.Common.Maps;
using Ares.GameDataExtract.Bodies;
using Ares.GameDataExtract.Cells;
using Ares.GameDataExtract.Components;
using Ares.GameDataExtract.Landscape;
using Ares.GameDataExtract.Materials;
using Ares.GameDataExtract.Packages;
using Ares.GameDataExtract.Profiles;
using Ares.Structs.Core;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Game.Collision;
using Ares.Structs.Game.World;
using Quaternion = System.Numerics.Quaternion;

namespace Ares.GameDataExtract.Extraction;

/// <summary>
/// Builds the static collision of the Dam map from the installed game: every cell the map's persistent level lists
/// (the level itself included) with its data layers, the landscape tiles of every cell, and one placement for each
/// component or instance whose collision blocks any channel and whose body has geometry. Static mesh
/// components and instances use their mesh's body, spline mesh and brush components the body setup they own, and box,
/// sphere and capsule components a body of their own shape. Components that cannot be decoded are counted in the
/// summary instead of failing the extraction.
/// </summary>
internal sealed class CollisionExtractor
{
    private static readonly PackageObjectIndex s_bodySetup = PackageObjectIndex.FromScriptPath(CollisionObjectClasses.BodySetup);

    private readonly PackageCache _packages;
    private readonly CollisionProfiles _profiles;
    private readonly ComponentReader _components;
    private readonly BodyLibrary _bodies;
    private readonly SurfaceLibrary _surfaces;
    private readonly MapCollisionBuilder _builder = new(GameBuild.SteamBuildId, DamMap.WorldName);
    private readonly ExtractionSummary _summary = new();

    private CollisionExtractor(PackageCache packages, CollisionProfiles profiles)
    {
        _packages = packages;
        _profiles = profiles;
        _components = new ComponentReader(packages);
        _bodies = new BodyLibrary(packages, profiles.DefaultShapeComplexity);
        _surfaces = new SurfaceLibrary(packages);
    }

    /// <summary>Extracts the Dam map's collision from the map package and its cells <paramref name="cellPackages"/> of <paramref name="packages"/>.</summary>
    /// <exception cref="InvalidDataException">The map, its cell list or its landscape cannot be read.</exception>
    public static (MapCollision Map, ExtractionSummary Summary) Extract(PackageCache packages, CollisionProfiles profiles, IReadOnlyList<string> cellPackages)
    {
        CollisionExtractor extractor = new(packages, profiles);
        extractor.Run([DamMap.MapPackage, .. cellPackages]);
        MapCollision map = extractor._builder.Build();
        extractor._summary.ComplexMeshBodies = map.Bodies.Count(body => !body.ComplexMeshes.IsEmpty);
        extractor._summary.WithoutComplexCollision = map.Placements.Count(placement =>
            map.Bodies[placement.BodyIndex] is { UsesSimpleAsComplex: false, ComplexMeshes.IsEmpty: true });
        return (map, extractor._summary);
    }

    private void Run(IReadOnlyList<string> cellPackages)
    {
        CookedPackage level = _packages.Load(DamMap.MapPackage)
            ?? throw new InvalidDataException($"The map package {DamMap.MapPackage} is not in the game archives");
        IReadOnlyList<MapCell> cells = MapCellReader.ReadCells(level, cellPackages, DamDataLayers.All);
        LandscapeReader landscape = new(_components.Scenes, _surfaces);
        foreach (MapCell cell in cells)
        {
            int cellIndex;
            CookedPackage package = _packages.Load(cell.PackageName) ?? throw new InvalidDataException($"{cell.PackageName} is not in the game archives");
            try
            {
                cellIndex = _builder.AddCell(cell.PackageName, cell.DataLayers);
                foreach (HeightFieldTile tile in landscape.ReadCell(package, cellIndex))
                {
                    _builder.AddHeightField(tile);
                    _summary.HeightFields++;
                }
            }
            catch (ArgumentException error)
            {
                throw new InvalidDataException($"{cell.PackageName}: {error.Message}", error);
            }

            CellComponents components = _components.ReadCell(cell.PackageName);
            foreach (PlacedComponent component in components.Placed)
            {
                AddPlaced(package, component, cellIndex);
            }

            foreach (InstancedComponent component in components.Instanced)
            {
                AddInstanced(component, cellIndex);
            }

            foreach (ComponentFailure failure in components.Failures)
            {
                _summary.AddFailure(failure.Reason, WouldBlock(package, package.Exports[failure.ExportIndex]));
            }
        }

        _summary.Cells = cells.Count;
        _summary.BaseMapCells = cells.Count(cell => cell.IsBaseMap);
        _summary.Bodies = _builder.BodyCount;
    }

    private void AddPlaced(CookedPackage cell, PlacedComponent component, int cellIndex)
    {
        ComponentCounts counts = _summary.Class(component.Kind.ToString());
        counts.Components++;
        if (component is { Kind: CollisionComponentKind.StaticMesh, Mesh: null })
        {
            _summary.WithoutMesh++;
            return;
        }

        ZenExport export = cell.Exports[component.ExportIndex];
        AssetBody? mesh;
        CollisionClassification? classification;
        try
        {
            mesh = component.Mesh is { } meshPath ? _bodies.GetMeshBody(meshPath) : null;
            classification = ComponentCollision.Classify(_profiles, component.Collision, mesh);
        }
        catch (InvalidDataException error)
        {
            _summary.AddFailure(error.Message, null);
            return;
        }

        if (classification is not { } blocks)
        {
            _summary.AddFailure($"The class default {component.Collision.ClassDefaultObject} is not known", null);
            return;
        }

        PlacementFlags flags = blocks.Channels;
        if (flags == PlacementFlags.None)
        {
            return;
        }

        counts.Blocking++;
        AssetBody body;
        try
        {
            body = mesh ?? OwnBody(cell, export, component.Kind);
        }
        catch (InvalidDataException error)
        {
            _summary.AddFailure(error.Message, null);
            return;
        }

        if (body.Body is not { } shapes)
        {
            counts.WithoutGeometry++;
            return;
        }

        if (component.Kind == CollisionComponentKind.SplineMesh && shapes is { TraceFlag: CollisionTraceFlag.SimpleAndComplex, ComplexMeshes.IsEmpty: true })
        {
            shapes = SplineComplexCollision(cell, export, shapes);
        }

        (PhysicalSurfaceType surface, ImmutableArray<PhysicalSurfaceType> materialSurfaces) = PlacedSurfaces(cell, export, component, body, shapes);
        try
        {
            _builder.AddPlacement(_builder.AddBody(shapes), cellIndex, component.World, flags, surface, materialSurfaces);
            counts.Placements++;
            CountPlayerCapsuleMismatch(blocks);
        }
        catch (ArgumentException error)
        {
            _summary.AddFailure(error.Message, true);
        }
    }

    private void AddInstanced(InstancedComponent component, int cellIndex)
    {
        ComponentCounts counts = _summary.Class(component.Kind.ToString());
        counts.Components++;
        AssetBody body;
        try
        {
            body = _bodies.GetMeshBody(component.Mesh);
        }
        catch (InvalidDataException error)
        {
            _summary.AddFailure(error.Message, null);
            return;
        }

        CollisionClassification blocks = ComponentCollision.Classify(_profiles, component.Body, ComponentCollisionDefaults.InstancedStaticMesh.ProfileName);
        PlacementFlags flags = blocks.Channels;
        if (flags == PlacementFlags.None)
        {
            return;
        }

        counts.Blocking += component.Instances.Count;
        if (body.Body is not { } shapes)
        {
            counts.WithoutGeometry += component.Instances.Count;
            return;
        }

        (PhysicalSurfaceType surface, ImmutableArray<PhysicalSurfaceType> materialSurfaces) = Surfaces(
            () => _surfaces.OfCollision(component.Body?.PhysMaterialOverride, body.PhysMaterial, _surfaces.MeshMaterials(component.Mesh), HasTriangleMeshes(shapes)));
        if (_components.StoresInstancedOverrideMaterials(component))
        {
            _summary.InstancedOverrideMaterialsNotRead++;
        }

        int bodyIndex;
        try
        {
            bodyIndex = _builder.AddBody(shapes);
        }
        catch (ArgumentException error)
        {
            _summary.AddFailure(error.Message, true);
            return;
        }

        foreach (TransformMatrix instance in component.Instances)
        {
            try
            {
                _builder.AddPlacement(bodyIndex, cellIndex, TransformMath.FromMatrix(instance), flags, surface, materialSurfaces);
                counts.Placements++;
                CountPlayerCapsuleMismatch(blocks);
            }
            catch (Exception error) when (error is InvalidDataException or ArgumentException)
            {
                _summary.AddFailure($"Instance: {error.Message}", true);
            }
        }
    }

    /// <summary>
    /// The surface types of a placed component's collision: static mesh and spline mesh components take their materials
    /// from the mesh's slots, replaced slot by slot by the component's override materials; other components show no
    /// materials.
    /// </summary>
    private (PhysicalSurfaceType Surface, ImmutableArray<PhysicalSurfaceType> MaterialSurfaces) PlacedSurfaces(
        CookedPackage cell, ZenExport export, PlacedComponent component, AssetBody body, CollisionBody shapes)
    {
        return Surfaces(() =>
        {
            ImmutableArray<ExportRef?> materials = [];
            if (component.Kind is not (CollisionComponentKind.StaticMesh or CollisionComponentKind.SplineMesh))
            {
                return _surfaces.OfCollision(component.Collision.PhysMaterialOverride, body.PhysMaterial, materials, HasTriangleMeshes(shapes));
            }

            ImmutableArray<ExportRef?> slots = _components.StaticMesh(cell, export) is { } mesh ? _surfaces.MeshMaterials(mesh.Path) : [];
            ImmutableArray<ExportRef?> overrides = _components.OverrideMaterials(cell, export) is { } stored ? _surfaces.Resolve(stored) : [];
            materials = [.. Enumerable.Range(0, Math.Max(slots.Length, overrides.Length))
                .Select(slot => (slot < overrides.Length ? overrides[slot] : null) ?? (slot < slots.Length ? slots[slot] : null))];
            return _surfaces.OfCollision(component.Collision.PhysMaterialOverride, body.PhysMaterial, materials, HasTriangleMeshes(shapes));
        });
    }

    /// <summary>Surface types from <paramref name="read"/>; a failure is counted in the summary and gives the default surface type.</summary>
    private (PhysicalSurfaceType Surface, ImmutableArray<PhysicalSurfaceType> MaterialSurfaces) Surfaces(
        Func<(PhysicalSurfaceType Surface, ImmutableArray<PhysicalSurfaceType> MaterialSurfaces)> read)
    {
        try
        {
            (PhysicalSurfaceType surface, ImmutableArray<PhysicalSurfaceType> materialSurfaces) = read();
            _summary.AddSurface(surface);
            return (surface, materialSurfaces);
        }
        catch (InvalidDataException error)
        {
            _summary.AddSurfaceFailure(error.Message);
            return (PhysicalSurfaceType.Default, []);
        }
    }

    /// <summary>
    /// The body of a component that does not show a mesh: the body setup a spline mesh or brush component owns, or the
    /// shape of a box, sphere or capsule component.
    /// </summary>
    /// <exception cref="InvalidDataException">The body cannot be found or decoded.</exception>
    private AssetBody OwnBody(CookedPackage cell, ZenExport export, CollisionComponentKind kind)
    {
        if (kind is CollisionComponentKind.Box or CollisionComponentKind.Sphere or CollisionComponentKind.Capsule)
        {
            string path = cell.Header.ExportPath(export.Index);
            return new AssetBody(path, ShapeBody(path, kind, _components.Shape(cell, export, kind)), ComponentCollisionDefaults.ShapeComponentTraceFlag, null);
        }

        ZenExport[] owned = [.. cell.ChildrenOf(export.Index).Where(child => child.Class == s_bodySetup)];
        return owned.Length == 1
            ? _bodies.GetCellBody(new ExportRef(cell, owned[0]))
            : throw new InvalidDataException($"The component owns {owned.Length} body setups");
    }

    /// <summary>
    /// The complex collision of a spline mesh component whose own body has none: its static mesh's complex triangle
    /// meshes bent along the component's segment. When the static mesh has no complex collision or the segment or the
    /// mesh bounds cannot be decoded, the component's simple shapes answer complex queries instead (counted in the
    /// summary).
    /// </summary>
    private CollisionBody SplineComplexCollision(CookedPackage cell, ZenExport export, CollisionBody shapes)
    {
        try
        {
            ExportRef mesh = _components.StaticMesh(cell, export) ?? throw new InvalidDataException("The spline mesh component shows no mesh");
            ImmutableArray<TriangleMesh> complex = _bodies.GetMeshBody(mesh.Path).Body switch
            {
                { ComplexMeshes.IsEmpty: false } source => source.ComplexMeshes,
                { TraceFlag: CollisionTraceFlag.ComplexAsSimple } source => source.TriangleMeshes,
                _ => [],
            };
            if (complex.IsEmpty)
            {
                throw new InvalidDataException("The static mesh has no complex collision");
            }

            SplineMeshParams spline = ComponentReader.SplineParams(cell, export);
            (Vector3D origin, Vector3D extent) = StaticMeshValues.ExtendedBounds(mesh.Package, mesh.Export);
            ImmutableArray<TriangleMesh> bent = [.. complex.Select(source => SplineMeshDeformation.Bend(source, spline, origin.X - extent.X, 2 * extent.X))];
            _summary.BentSplineMeshes++;
            return shapes with { ComplexMeshes = bent };
        }
        catch (InvalidDataException error)
        {
            _summary.AddSplineFallback(error.Message);
            return shapes with { TraceFlag = CollisionTraceFlag.SimpleAsComplex };
        }
    }

    /// <summary>
    /// Whether a component that could not be placed would have blocked something: its collision settings and body
    /// decoded on their own. <see langword="null"/> when they do not decode either.
    /// </summary>
    private bool? WouldBlock(CookedPackage cell, ZenExport export)
    {
        if (ComponentReader.CollisionKindOf(export) is not { } kind)
        {
            return null;
        }

        try
        {
            CollisionSettings settings = _components.Collision(cell, export);
            AssetBody? mesh = null;
            if (kind == CollisionComponentKind.StaticMesh)
            {
                if (_components.StaticMesh(cell, export) is not { } meshExport)
                {
                    return false;
                }

                mesh = _bodies.GetMeshBody(meshExport.Path);
            }

            if (ComponentCollision.Classify(_profiles, settings, mesh) is not { } blocks)
            {
                return null;
            }

            return blocks.Channels != PlacementFlags.None && !(mesh ?? OwnBody(cell, export, kind)).IsEmpty;
        }
        catch (InvalidDataException)
        {
            return null;
        }
    }

    /// <summary>The body of a box, sphere or capsule component: one shape at the component's origin.</summary>
    private static CollisionBody ShapeBody(string name, CollisionComponentKind kind, ComponentShape shape)
    {
        CollisionBody body = kind switch
        {
            CollisionComponentKind.Box => new CollisionBody(name,
                [new CollisionBox(Vector3.Zero, Quaternion.Identity, new Vector3((float)shape.BoxExtent.X, (float)shape.BoxExtent.Y, (float)shape.BoxExtent.Z))],
                [], [], [], []),
            CollisionComponentKind.Sphere => new CollisionBody(name, [], [new CollisionSphere(Vector3.Zero, (float)shape.Radius)], [], [], []),
            _ => new CollisionBody(name, [], [],
                [new CollisionCapsule(Vector3.Zero, Quaternion.Identity, (float)shape.Radius, (float)Math.Max(0, shape.HalfHeight - shape.Radius))],
                [], []),
        };
        return body with { TraceFlag = ComponentCollisionDefaults.ShapeComponentTraceFlag };
    }

    private static bool HasTriangleMeshes(CollisionBody body) => !body.TriangleMeshes.IsEmpty || !body.ComplexMeshes.IsEmpty;

    /// <summary>Counts a placement whose player channel bit disagrees with whether it blocks the player capsule.</summary>
    private void CountPlayerCapsuleMismatch(CollisionClassification blocks)
    {
        if (blocks.Channels.Blocks(GameCollisionChannels.PlayerPawn) != blocks.BlocksPlayerCapsule)
        {
            _summary.PlayerChannelMismatches++;
        }
    }
}
