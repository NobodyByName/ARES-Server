using Ares.Structs.Core.Geometry;

namespace Ares.Structs.Game.World;

/// <summary>
/// A loot container actor the map places itself, as opposed to a container the server spawns on a mesh socket
/// (<see cref="LootSocket"/>).
/// </summary>
/// <param name="Cell">
/// Generated World Partition cell that places the actor (the last segment of its package name), or empty for the
/// map's persistent level.
/// </param>
/// <param name="Actor">Placed actor name.</param>
/// <param name="Class">Container class path (<c>/Game/.../BP_X.BP_X_C</c>).</param>
/// <param name="Location">World location of the actor's root.</param>
/// <param name="Rotation">World rotation in degrees.</param>
/// <param name="Scale">World scale.</param>
/// <param name="DataLayer">Data layer that streams the actor, or null when it is always present.</param>
public sealed record PlacedLootContainer(
    string Cell,
    string Actor,
    string Class,
    Vector3D Location,
    Rotator Rotation,
    Vector3D Scale,
    string? DataLayer);

/// <summary>A container the game server spawned at a place the map data does not hold.</summary>
/// <param name="Class">Container class path (<c>/Game/.../BP_X.BP_X_C</c>).</param>
/// <param name="Location">World location.</param>
/// <param name="Rotation">World rotation in degrees, or null when it is not known.</param>
public sealed record ObservedLootContainer(string Class, Vector3D Location, Rotator? Rotation);
