using System.Collections.Immutable;
using Ares.Structs.Core.Assets;
using Ares.Structs.Core.Geometry;

namespace Ares.Structs.Game.World;

/// <summary>An AI spline path (<c>ASplinePath</c>) placed in the persistent level or a World Partition cell's persistent level.</summary>
/// <param name="Name">Path actor name in its level, which the actors that use the path name it by.</param>
/// <param name="Closed">Whether the path's spline is a closed loop (<c>bClosedLoop</c>): its last point joins its first.</param>
public sealed record SplinePathPlacement(string Name, bool Closed)
{
    /// <summary>The path's gameplay tags (<see cref="SplinePathClasses.TagsMember"/>), in stored order.</summary>
    public ImmutableArray<string> Tags { get; init; } = [];

    /// <summary>
    /// World locations of the spline's points (<see cref="SplinePathClasses.PointsMember"/>), in centimetres, in spline
    /// point order.
    /// </summary>
    public ImmutableArray<Vector3D> Points { get; init; } = [];
}

/// <summary>The spline path actors the map tables list and the parts of them the server reads.</summary>
public static class SplinePathClasses
{
    /// <summary>Class of the placed spline paths.</summary>
    public const string Blueprint = "/Game/Pioneer/Core/AI/Paths/BP_SplinePath.BP_SplinePath_C";

    /// <summary>Name of a path's spline component (a <c>USplineComponent</c>, see <see cref="Collision.SplineComponentMembers"/>).</summary>
    public const string SplineComponent = "Spline";

    /// <summary>Script member holding the path's gameplay tags.</summary>
    public const string TagsMember = "PathTags";

    /// <summary>
    /// Script member holding the world locations of the path's spline points (<c>APregeneratedPath.PathPoints</c>), which
    /// the path keeps in world space when it is placed, whatever the spline is attached to.
    /// </summary>
    public const string PointsMember = "PathPoints";
}

/// <summary>
/// Members of the native <c>FNavAgentProperties</c> struct, a member of the spline paths' script chain stored before
/// <see cref="SplinePathClasses.PointsMember"/>: its own members, then those of <c>FMovementProperties</c>.
/// </summary>
public static class NavAgentPropertiesMembers
{
    /// <summary>Name of the struct (with its prefix), as script members name it.</summary>
    public const string Name = "FNavAgentProperties";

    /// <summary>The struct's chain.</summary>
    public static StructSchema Schema { get; } = new("NavAgentProperties",
    [
        new PropertyField("AgentRadius", PropertyType.Float),
        new PropertyField("AgentHeight", PropertyType.Float),
        new PropertyField("AgentStepHeight", PropertyType.Float),
        new PropertyField("NavWalkingSearchHeightScale", PropertyType.Float),
        new PropertyField("PreferredNavData", PropertyType.StructOf(EngineStructs.SoftClassPath)),
        new PropertyField("bCanCrouch", PropertyType.Bool),
        new PropertyField("bCanJump", PropertyType.Bool),
        new PropertyField("bCanWalk", PropertyType.Bool),
        new PropertyField("bCanSwim", PropertyType.Bool),
        new PropertyField("bCanFly", PropertyType.Bool),
    ]);
}
