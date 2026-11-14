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
