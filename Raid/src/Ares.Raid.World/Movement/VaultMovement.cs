using Ares.Raid.World.Collision;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Game.Movement;

namespace Ares.Raid.World.Movement;

/// <summary>Persistent state of a running vault.</summary>
/// <param name="Clip">The vault animation and its settings.</param>
/// <param name="Kind">Whether the vault goes over the obstacle or onto it.</param>
/// <param name="Anchor">World position the animation's root path is placed at: the obstacle edge.</param>
/// <param name="Heading">Heading of the root path's forward axis, in radians.</param>
/// <param name="Rate">Animation seconds per second.</param>
/// <param name="StartTime">Animation time the vault starts at.</param>
/// <param name="Offset">Feet position at the start minus the root path's start position; it fades out over the entry blend.</param>
/// <param name="Ticks">Vault steps already taken.</param>
public readonly record struct VaultState(
    VaultClip Clip,
    VaultKind Kind,
    Vector3D Anchor,
    double Heading,
    double Rate,
    double StartTime,
    Vector3D Offset,
    int Ticks = 0);

/// <summary>Result of one vault step.</summary>
/// <param name="State">Persistent vault state after the step.</param>
/// <param name="Position">World position of the feet after the step.</param>
/// <param name="Velocity">Velocity of the step.</param>
/// <param name="ActionStance">Stance of the action layer.</param>
/// <param name="ActionLocomotion">Locomotion of the action layer.</param>
/// <param name="Mode">Rough movement mode: air over the obstacle, ground onto it.</param>
/// <param name="Finished">Whether the vault requests to leave for ground movement after this step.</param>
public readonly record struct VaultStep(
    VaultState State,
    Vector3D Position,
    Vector3D Velocity,
    CharacterStance ActionStance,
    LocoMode ActionLocomotion,
    RoughMovementMode Mode,
    bool Finished);

/// <summary>Vault motion: the clip's root path placed at the obstacle edge and played from its distance-matched start.</summary>
public static class VaultMovement
{
    /// <summary>Whether the clip carries a root path to move along.</summary>
    /// <param name="clip">The clip.</param>
    /// <returns>True when <see cref="Begin"/> can play it.</returns>
    public static bool CanPlay(VaultClip clip)
    {
        ArgumentNullException.ThrowIfNull(clip);
        return clip.Sequence.Path.Count >= 2;
    }

    /// <summary>
    /// Starts a vault: places the root path at the edge, facing the wall (or along the approach for clips that keep
    /// the direction), and picks the start time closest to where the feet will be two ticks later.
    /// </summary>
    /// <param name="clip">The chosen clip.</param>
    /// <param name="definitions">The definition set the clip comes from.</param>
    /// <param name="trace">The obstacle.</param>
    /// <param name="feet">World position of the feet.</param>
    /// <param name="velocity">Velocity before the vault.</param>
    /// <param name="onGround">Whether the character is on the ground.</param>
    /// <returns>The vault state before its first step.</returns>
    public static VaultState Begin(VaultClip clip, VaultDefinitionSet definitions, VaultTrace trace, Vector3D feet, Vector3D velocity, bool onGround)
    {
        ArgumentNullException.ThrowIfNull(clip);
        ArgumentNullException.ThrowIfNull(definitions);
        ArgumentNullException.ThrowIfNull(trace);
        if (!CanPlay(clip))
        {
            throw new ArgumentException("The clip has no root path.", nameof(clip));
        }

        double heading = clip.KeepDirection
            ? Math.Atan2(trace.Edge.Y - trace.SearchStart.Y, trace.Edge.X - trace.SearchStart.X)
            : Math.Atan2(-trace.WallNormal.Y, -trace.WallNormal.X);
        VaultState state = new(clip, trace.Kind, trace.Edge, heading, clip.PlayRate * definitions.GlobalPlayRate, 0, default);
        double start = 0;
        if (clip.DistanceMatching)
        {
            const double Lead = VaultParameters.MatchingLeadTicks * PawnBlackboard.TickSeconds;
            Double3 ahead = Double3.From(feet) + new Double3(velocity.X * Lead, velocity.Y * Lead, 0);
            double latest = onGround ? clip.MaxEntrySecondsFromGround : clip.MaxEntrySeconds;
            start = ClosestTime(clip.Sequence, Local(state, ahead), latest);
        }

        Double3 offset = Double3.From(feet) - Double3.From(Position(state, start));
        return state with { StartTime = start, Offset = offset.ToVector3D() };
    }

    /// <summary>Vault steps before the vault hands over to ground movement, when nothing ends it early.</summary>
    /// <param name="state">The vault state from <see cref="Begin"/>.</param>
    /// <returns>Number of vault steps.</returns>
    public static int Duration(VaultState state)
    {
        int ticks = 1;
        while (!EndsAt(state, ticks))
        {
            ticks++;
        }

        return ticks;
    }

    /// <summary>Advances a running vault by one simulation tick.</summary>
    /// <param name="state">Vault state.</param>
    /// <param name="previous">World position of the feet before the step.</param>
    /// <param name="seconds">Simulation step duration.</param>
    /// <returns>Position, velocity, action posture, mode and whether the vault ends.</returns>
    public static VaultStep Step(VaultState state, Vector3D previous, double seconds)
    {
        if (!double.IsFinite(seconds) || seconds <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(seconds));
        }

        int tick = state.Ticks + 1;
        double fade = Math.Max(0, 1 - (tick * PawnBlackboard.TickSeconds / VaultParameters.EntryBlendSeconds));
        Double3 position = Double3.From(Position(state, AnimationSeconds(state, tick))) + (Double3.From(state.Offset) * fade);
        Double3 velocity = (position - Double3.From(previous)) * (1 / seconds);
        RoughMovementMode mode = state.Kind == VaultKind.Over ? RoughMovementMode.Air : RoughMovementMode.Ground;
        return new VaultStep(state with { Ticks = tick }, position.ToVector3D(), velocity.ToVector3D(), CharacterStance.Uncontrolled, LocoMode.Running,
            mode, EndsAt(state, tick));
    }

    /// <summary>World position of the clip's root at an animation time.</summary>
    /// <param name="state">Vault state.</param>
    /// <param name="seconds">Animation time.</param>
    /// <returns>World position of the feet.</returns>
    public static Vector3D Position(VaultState state, double seconds)
    {
        Double3 local = Sample(state.Clip.Sequence, seconds);
        (double sin, double cos) = Math.SinCos(state.Heading);
        return new Vector3D(state.Anchor.X + (local.X * cos) - (local.Y * sin), state.Anchor.Y + (local.X * sin) + (local.Y * cos), state.Anchor.Z + local.Z);
    }

    // The animation advances by whole ticks scaled by its play rate, from the start time.
    private static double AnimationSeconds(VaultState state, int tick) =>
        state.StartTime + (Math.Floor((tick * state.Rate) + 0.5) * PawnBlackboard.TickSeconds);

    // The vault hands over once the rest of its animation fits in the exit blend.
    private static bool EndsAt(VaultState state, int tick) =>
        state.Clip.Sequence.Length - AnimationSeconds(state, tick) <= VaultParameters.ExitBlendSeconds;

    private static Double3 Local(VaultState state, Double3 world)
    {
        Double3 offset = world - Double3.From(state.Anchor);
        (double sin, double cos) = Math.SinCos(state.Heading);
        return new Double3((offset.X * cos) + (offset.Y * sin), (offset.Y * cos) - (offset.X * sin), offset.Z);
    }

    private static Double3 Sample(VaultSequence sequence, double seconds)
    {
        IReadOnlyList<VaultPathPoint> path = sequence.Path;
        double index = Math.Clamp(seconds / sequence.SampleSeconds, 0, path.Count - 1);
        int low = Math.Min((int)index, path.Count - 2);
        double share = index - low;
        Double3 a = Point(path[low]);
        return a + ((Point(path[low + 1]) - a) * share);
    }

    // Animation time of the point of the root path closest to a position, among the path up to the latest time.
    private static double ClosestTime(VaultSequence sequence, Double3 position, double latest)
    {
        IReadOnlyList<VaultPathPoint> path = sequence.Path;
        double bestTime = 0;
        double bestDistance = double.MaxValue;
        for (int index = 0; index + 1 < path.Count && index * sequence.SampleSeconds <= latest; index++)
        {
            Double3 a = Point(path[index]);
            Double3 segment = Point(path[index + 1]) - a;
            double squared = segment.LengthSquared();
            double share = squared > 0 ? Math.Clamp(Double3.Dot(position - a, segment) / squared, 0, 1) : 0;
            double time = Math.Min((index + share) * sequence.SampleSeconds, latest);
            share = (time / sequence.SampleSeconds) - index;
            double distance = (a + (segment * share) - position).LengthSquared();
            if (distance < bestDistance)
            {
                (bestTime, bestDistance) = (time, distance);
            }
        }

        return bestTime;
    }

    private static Double3 Point(VaultPathPoint point) => new(point.X, point.Y, point.Z);
}
