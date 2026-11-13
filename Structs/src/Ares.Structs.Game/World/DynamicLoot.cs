using Ares.Structs.Core.Geometry;

namespace Ares.Structs.Game.World;

/// <summary>A container class a dynamic loot spawner can place and the weight its draw has.</summary>
/// <param name="Class">Blueprint class path of the container.</param>
/// <param name="Weight">Relative chance of the class among the spawner's classes.</param>
public sealed record DynamicLootClass(string Class, double Weight);

/// <summary>A place where a dynamic loot spawner can put a container.</summary>
/// <param name="Location">World location.</param>
/// <param name="Rotation">World rotation in degrees (used unless the spawner randomises it).</param>
/// <param name="Weight">Relative chance of the point among the spawner's points.</param>
public sealed record DynamicLootPoint(Vector3D Location, Rotator Rotation, double Weight);

/// <summary>
/// A spawner the map places for free-standing loot containers. When it runs it places <see cref="Count"/> containers:
/// each draws a class by weight and a point by weight, and a point already used by this run is not drawn again.
/// </summary>
/// <param name="Cell">
/// Generated World Partition cell that places the spawner (the last segment of its package name), or empty for the
/// map's persistent level.
/// </param>
/// <param name="Actor">The spawner actor.</param>
/// <param name="DataLayer">Data layer that streams the spawner, or null when it is always present.</param>
/// <param name="Classes">Container classes the spawner draws from.</param>
/// <param name="Points">Candidate points; never empty.</param>
/// <param name="Count">Containers one run places; never more than the number of points.</param>
/// <param name="RandomRotation">True when the spawned container gets a random yaw (0 to 360 degrees) instead of the point's rotation.</param>
public sealed record DynamicLootSpawner(
    string Cell,
    string Actor,
    string? DataLayer,
    IReadOnlyList<DynamicLootClass> Classes,
    IReadOnlyList<DynamicLootPoint> Points,
    int Count,
    bool RandomRotation);

/// <summary>A partial choice of subnodes on the path to a group: fewer of its children are active than it has.</summary>
/// <param name="Node">The activity node.</param>
/// <param name="Min">Fewest children chosen.</param>
/// <param name="Max">Most children chosen.</param>
/// <param name="Of">Children the node has.</param>
/// <param name="Weighted">True when the choice favours children by weight.</param>
public sealed record DynamicLootSelection(string Node, int Min, int Max, int Of, bool Weighted);

/// <summary>
/// A set of dynamic loot spawners of which a run activates a random number: between <see cref="Min"/> and
/// <see cref="Max"/> of <see cref="Spawners"/>, chosen at random without repeats.
/// </summary>
/// <param name="Id">Activity node and position of the group within it.</param>
/// <param name="Path">Titles of the activity nodes from the root down to the group's node.</param>
/// <param name="Rounds">
/// Round configurations in which the group is active (every node on the path applies to them); a group runs only when
/// the round's configuration is listed.
/// </param>
/// <param name="Min">Fewest spawners activated.</param>
/// <param name="Max">Most spawners activated.</param>
/// <param name="Spawners">The group's spawners (a spawner can belong to several groups).</param>
/// <param name="ParentSelections">Partial choices of subnodes above the group that decide whether it is active at all.</param>
public sealed record DynamicLootGroup(
    string Id,
    IReadOnlyList<string> Path,
    IReadOnlyList<string> Rounds,
    int Min,
    int Max,
    IReadOnlyList<DynamicLootSpawner> Spawners,
    IReadOnlyList<DynamicLootSelection> ParentSelections);
