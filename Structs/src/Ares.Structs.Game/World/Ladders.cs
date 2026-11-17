using Ares.Structs.Core.Geometry;

namespace Ares.Structs.Game.World;

/// <summary>
/// A climbable ladder placed in a World Partition cell's persistent level: a spline of ladder meshes. The
/// client draws it, but only a server-spawned ladder actor attached to it makes it climbable.
/// </summary>
/// <param name="Cell">Generated cell package.</param>
/// <param name="Actor">Ladder actor name in the cell.</param>
/// <param name="SplineTop">Height of the ladder spline's last point in the spline's own space, in centimetres.</param>
/// <param name="Location">World location of the ladder (and of its climbable actor), in centimetres.</param>
/// <param name="Yaw">World yaw of the ladder (and of its climbable actor), in degrees; ladders stand upright.</param>
public readonly record struct LadderPlacement(string Cell, string Actor, double SplineTop, Vector3D Location, double Yaw);

/// <summary>
/// How the game sizes the climbable actor of a placed ladder (<c>ULadderLogicSpawningComponent</c>,
/// <c>AClimbableLadder</c>): the climb covers whole climb cycles up to the spline top and ends a fixed
/// distance below that height.
/// </summary>
public static class LadderLogic
{
    /// <summary>
    /// Height of one climb cycle (<c>ULadderLogicData.CycleHeight</c>), in centimetres: the script constructor default,
    /// which the ladders' logic data asset does not change.
    /// </summary>
    public const double CycleHeight = 132.0;

    /// <summary>
    /// Distance below the climbing height where the climb ends (<c>AClimbableLadder.EndLadderInteractionDistance</c>),
    /// in centimetres: the script constructor default of a member that is not reflected, so no asset changes it.
    /// </summary>
    public const double EndInteractionDistance = 100.0;

    /// <summary>Class whose replication layout the spawned Blueprint uses (it adds no replicated members).</summary>
    public const string ActorClass = "/Script/Angelscript.ClimbableLadder";

    /// <summary>Class of the climb interactions.</summary>
    public const string InteractionClass = "/Script/Angelscript.StandardInteractionComponent";

    /// <summary>
    /// Names of the climb interaction components (bottom and top entry: <c>AttachInteraction</c> from
    /// <c>ATraversableLine</c>, <c>TopEntryInteraction</c> from <c>AClimbableLadder</c>).
    /// </summary>
    public static IReadOnlyList<string> InteractionComponents { get; } = ["AttachInteraction", "TopEntryInteraction"];

    /// <summary>Name of the component of a placed ladder that spawns its climbable actor.</summary>
    public const string SpawningComponent = "LadderLogicSpawningComponent";

    /// <summary>Name of a placed ladder's root spline component.</summary>
    public const string SplineComponent = "Spline";

    /// <summary>The replicated <c>LadderHeight</c> of a ladder's climbable actor.</summary>
    /// <param name="splineTop">The spline top (<see cref="LadderPlacement.SplineTop"/>).</param>
    /// <param name="cycleHeight">Height of one climb cycle.</param>
    /// <param name="endInteractionDistance">Distance below the climbing height where the climb ends.</param>
    /// <returns>The height, in centimetres.</returns>
    public static double LadderHeight(double splineTop, double cycleHeight, double endInteractionDistance)
    {
        if (!double.IsFinite(splineTop) || !(cycleHeight > 0))
        {
            throw new ArgumentOutOfRangeException(nameof(splineTop), "Spline top and cycle height must be finite, the cycle height positive.");
        }

        // The script computes fmod, which C# % gives for doubles (the result has the dividend's sign).
        return splineTop - (splineTop % cycleHeight) - endInteractionDistance;
    }
}

/// <summary>
/// A cooked point of a spline curve (<c>FInterpCurvePoint</c>): one unversioned header word listing the five members
/// with a zero mask (<see cref="HeaderWord"/>), the one-byte mask, then the members that are not zero, in order.
/// </summary>
public static class InterpCurvePointMembers
{
    /// <summary>The header word of a cooked point: one last fragment of <see cref="MemberCount"/> members with a zero mask.</summary>
    public const ushort HeaderWord = 0x0B80;

    /// <summary><c>InVal</c>: float, the curve parameter (the distance along the curve for a reparameterization table).</summary>
    public const int InVal = 0;

    /// <summary><c>OutVal</c>: the point's value (a vector of three doubles, or a float).</summary>
    public const int OutVal = 1;

    /// <summary><c>ArriveTangent</c>: like <see cref="OutVal"/>.</summary>
    public const int ArriveTangent = 2;

    /// <summary><c>LeaveTangent</c>: like <see cref="OutVal"/>.</summary>
    public const int LeaveTangent = 3;

    /// <summary><c>InterpMode</c>: one byte (<see cref="LinearMode"/> for straight segments).</summary>
    public const int InterpMode = 4;

    /// <summary>Members of the struct.</summary>
    public const int MemberCount = 5;

    /// <summary>The <see cref="InterpMode"/> of a straight segment.</summary>
    public const byte LinearMode = 0;

    /// <summary>The fewest points of a stored curve the readers look for.</summary>
    public const int MinPoints = 2;

    /// <summary>The most points of a stored curve the readers look for.</summary>
    public const int MaxPoints = 64;
}
