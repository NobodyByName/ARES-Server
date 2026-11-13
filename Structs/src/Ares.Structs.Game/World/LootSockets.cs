using Ares.Structs.Core.Geometry;

namespace Ares.Structs.Game.World;

/// <summary>
/// A loot container socket: a socket of a placed prop's static mesh, tagged as a place where the server may spawn
/// a loot container. The socket's cell, actor, component and name identify it.
/// </summary>
/// <param name="Cell">
/// Generated World Partition cell that places the prop (the last segment of its package name), or empty for the
/// map's persistent level.
/// </param>
/// <param name="Actor">Placed actor that owns the prop.</param>
/// <param name="Component">The actor's static mesh component that shows the prop.</param>
/// <param name="Socket">Socket name in the mesh.</param>
/// <param name="ContainerTag">
/// The socket's <c>Socket.LootContainer.*</c> gameplay tag, spelled as in the mesh (gameplay tag names compare
/// without case).
/// </param>
/// <param name="Location">World location.</param>
/// <param name="Rotation">World rotation in degrees.</param>
/// <param name="Scale">World scale.</param>
/// <param name="Mesh">Index of the prop's mesh in the map's mesh list.</param>
/// <param name="OverrideMaterials">
/// The component's override materials by material slot; a null entry, or a slot past the end, keeps the mesh's
/// material.
/// </param>
/// <param name="DataLayer">Data layer that streams the prop, or null when it is always present.</param>
public sealed record LootSocket(
    string Cell,
    string Actor,
    string Component,
    string Socket,
    string ContainerTag,
    Vector3D Location,
    Rotator Rotation,
    Vector3D Scale,
    int Mesh,
    IReadOnlyList<string?> OverrideMaterials,
    string? DataLayer);

/// <summary>A static mesh that carries loot container sockets.</summary>
/// <param name="Path">Mesh object path.</param>
/// <param name="Materials">The mesh's materials by material slot.</param>
public sealed record LootSocketMesh(string Path, IReadOnlyList<string?> Materials);
