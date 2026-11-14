using Ares.MapData.Packages;
using Ares.MapData.Profiles;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Game.Collision;

namespace Ares.MapData.Components;

/// <summary>Component classes that carry their own collision.</summary>
internal enum CollisionComponentKind
{
    /// <summary><c>UStaticMeshComponent</c>: the collision of its static mesh.</summary>
    StaticMesh,

    /// <summary><c>USplineMeshComponent</c>: its own body setup, already bent along the spline.</summary>
    SplineMesh,

    /// <summary><c>UBrushComponent</c>: its own body setup (volumes).</summary>
    Brush,

    /// <summary><c>UBoxComponent</c>.</summary>
    Box,

    /// <summary><c>USphereComponent</c>.</summary>
    Sphere,

    /// <summary><c>UCapsuleComponent</c>.</summary>
    Capsule,
}

/// <summary>Instanced static mesh component classes: one mesh drawn at many instance transforms.</summary>
internal enum InstancedComponentKind
{
    /// <summary>The scatter tool's <c>UScatterMeshComponent</c> (a hierarchical instanced component).</summary>
    Scatter,

    /// <summary><c>UInstancedStaticMeshComponent</c>.</summary>
    Instanced,

    /// <summary><c>UHierarchicalInstancedStaticMeshComponent</c>.</summary>
    HierarchicalInstanced,
}

/// <summary>
/// A component's collision settings as stored: each member comes from the component itself or the nearest template
/// that stores it (<see langword="null"/> when none does), plus the class default object the template chain ends at.
/// </summary>
/// <param name="UseDefaultCollision">Static mesh components: whether the mesh's own body setup decides the collision.</param>
/// <param name="ProfileName">Collision profile name.</param>
/// <param name="CollisionEnabled">Collision enabled state stored with the profile.</param>
/// <param name="ObjectType">Object type stored with the profile.</param>
/// <param name="Responses">Response overrides stored with the profile.</param>
/// <param name="ClassDefaultObject">Path of the native or script class default object the template chain ends at.</param>
/// <param name="ClassDefault">That default object's collision, when known; it applies to every member not stored.</param>
/// <param name="PhysMaterialOverride">The body instance's physical material override as stored (a zero reference for none).</param>
internal sealed record CollisionSettings(
    bool? UseDefaultCollision,
    string? ProfileName,
    CollisionEnabled? CollisionEnabled,
    CollisionChannel? ObjectType,
    IReadOnlyList<CollisionResponseOverride>? Responses,
    string ClassDefaultObject,
    ComponentCollisionDefault? ClassDefault,
    StoredReference? PhysMaterialOverride = null);

/// <summary>A placed component with its own collision.</summary>
/// <param name="Cell">Package path of the cell (or map) that places it.</param>
/// <param name="ExportIndex">Index of the component export in that package.</param>
/// <param name="Path">Object path of the component.</param>
/// <param name="Kind">Component class.</param>
/// <param name="World">World transform (location in centimetres, rotation, scale).</param>
/// <param name="Mesh">Static mesh components: object path of the static mesh, or <see langword="null"/> when it shows none.</param>
/// <param name="BodySetup">Spline mesh and brush components: object path of the body setup the component owns, when it owns exactly one.</param>
/// <param name="Collision">Collision settings.</param>
internal sealed record PlacedComponent(
    string Cell,
    int ExportIndex,
    string Path,
    CollisionComponentKind Kind,
    Transform World,
    string? Mesh,
    string? BodySetup,
    CollisionSettings Collision);

/// <summary>
/// The shape of a box, sphere or capsule component in component space, before the component's scale. Each member comes
/// from the component or the nearest template that stores it, else the class default.
/// </summary>
/// <param name="BoxExtent">Boxes: half the box size along each axis; zero for other shapes.</param>
/// <param name="Radius">Spheres and capsules: the radius; zero for boxes.</param>
/// <param name="HalfHeight">Capsules: half the height along Z, caps included; zero for other shapes.</param>
internal sealed record ComponentShape(Vector3D BoxExtent, double Radius, double HalfHeight);

/// <summary>A placed instanced static mesh component.</summary>
/// <param name="Cell">Package path of the cell that places it.</param>
/// <param name="ExportIndex">Index of the component export in that package.</param>
/// <param name="Path">Object path of the component.</param>
/// <param name="Kind">Component class.</param>
/// <param name="Mesh">Object path of the static mesh.</param>
/// <param name="Body">The body instance stored with the mesh, when found.</param>
/// <param name="Template">Object path of the template that holds the mesh and instances when the component stores none itself.</param>
/// <param name="Instances">World matrix of each instance.</param>
internal sealed record InstancedComponent(
    string Cell,
    int ExportIndex,
    string Path,
    InstancedComponentKind Kind,
    string Mesh,
    BodyInstanceValues? Body,
    string? Template,
    IReadOnlyList<TransformMatrix> Instances);

/// <summary>A collision component that could not be decoded.</summary>
/// <param name="Cell">Package path of the cell.</param>
/// <param name="ExportIndex">Index of the component export.</param>
/// <param name="Path">Object path of the component.</param>
/// <param name="Reason">What failed.</param>
internal sealed record ComponentFailure(string Cell, int ExportIndex, string Path, string Reason);

/// <summary>The collision components of one cell package.</summary>
/// <param name="Cell">Package path.</param>
/// <param name="Placed">Components with their own collision.</param>
/// <param name="Instanced">Instanced static mesh components.</param>
/// <param name="Failures">Components that could not be decoded.</param>
internal sealed record CellComponents(
    string Cell,
    IReadOnlyList<PlacedComponent> Placed,
    IReadOnlyList<InstancedComponent> Instanced,
    IReadOnlyList<ComponentFailure> Failures);
