using Ares.Structs.Core.Geometry;
using Ares.Structs.Game.Movement;

namespace Ares.Raid.World.Movement;

/// <summary>Persistent state of a running heavy landing.</summary>
/// <param name="Level">Index of the landing level in its level list.</param>
/// <param name="Clip">Animation the landing plays.</param>
/// <param name="Heading">Heading the animation's root motion is played along, in radians, fixed at the start.</param>
/// <param name="Recovery">Whether a recovery roll softens the landing.</param>
/// <param name="Ticks">Landing steps already taken.</param>
public readonly record struct HeavyLandingState(int Level, HeavyLandingClip Clip, double Heading, bool Recovery, int Ticks = 0);

/// <summary>Result of one heavy-landing step.</summary>
/// <param name="State">Persistent landing state after the step.</param>
/// <param name="Velocity">World velocity of the step.</param>
/// <param name="ActionStance">Stance of the action layer.</param>
/// <param name="ActionLocomotion">Locomotion of the action layer.</param>
/// <param name="InAir">Whether the character is off the ground (rough movement mode air instead of ground).</param>
/// <param name="Exit">The state that hands over to ground movement after this step, or null while the landing goes on.</param>
/// <param name="ExitBlendTicks">Ticks of the blend into ground movement after this step; zero while the landing goes on.</param>
public readonly record struct HeavyLandingStep(
    HeavyLandingState State,
    Vector3D Velocity,
    CharacterStance ActionStance,
    LocoMode ActionLocomotion,
    bool InAir,
    HeavyLandingTransientState? Exit,
    int ExitBlendTicks)
{
    /// <summary>Whether the landing requests to leave for ground movement after this step.</summary>
    public bool Finished => Exit is not null;
}

/// <summary>Heavy-landing level, animation choice and root-motion velocity.</summary>
public static class HeavyLandingMovement
{
    /// <summary>Whether a landing from this fall height is heavy.</summary>
    /// <param name="fallHeight">Fall height from <see cref="HeavyLandingParameters.FallHeight"/>.</param>
    /// <returns>True when the character lands heavily.</returns>
    public static bool IsHeavy(double fallHeight) => fallHeight > HeavyLandingParameters.Height;

    /// <summary>Whether a heavy landing from this fall height becomes a recovery roll.</summary>
    /// <param name="fallHeight">Fall height of the heavy landing.</param>
    /// <param name="pressed">Whether a dodge-roll or crouch press counted within <see cref="HeavyLandingParameters.RecoveryRollPressSeconds"/> before.</param>
    /// <param name="blocked">Whether recovery rolls are blocked (a landing detected during a landing).</param>
    /// <returns>True when the landing is softened by a roll.</returns>
    public static bool IsRecoveryRoll(double fallHeight, bool pressed, bool blocked = false) =>
        fallHeight < HeavyLandingParameters.RecoveryRollMaximumHeight && pressed && !blocked;

    /// <summary>The landing level of a fall height: the last level whose height the fall exceeds, the first one otherwise.</summary>
    /// <param name="fallHeight">Fall height that picks the level.</param>
    /// <param name="levels">Levels, lowest height first.</param>
    /// <returns>Index into <paramref name="levels"/>.</returns>
    public static int Level(double fallHeight, IReadOnlyList<HeavyLandingLevel> levels)
    {
        if (levels.Count == 0)
        {
            throw new ArgumentException("A landing needs at least one level", nameof(levels));
        }

        int best = 0;
        for (int index = 0; index < levels.Count; index++)
        {
            double low = Math.Min(fallHeight, levels[best].Height);
            double high = Math.Max(fallHeight, levels[best].Height);
            if (levels[index].Height >= low && levels[index].Height < high)
            {
                best = index;
            }
        }

        return best;
    }

    /// <summary>The eight-way direction of a horizontal velocity relative to a heading.</summary>
    /// <param name="velocity">World velocity; its vertical part is ignored.</param>
    /// <param name="heading">Heading in radians.</param>
    /// <returns>The direction, <see cref="InputDirection.None"/> without horizontal motion.</returns>
    public static InputDirection Direction(Vector3D velocity, double heading)
    {
        double squared = (velocity.X * velocity.X) + (velocity.Y * velocity.Y);
        if (squared < HeavyLandingParameters.DirectionTolerance)
        {
            return InputDirection.None;
        }

        double length = Math.Sqrt(squared);
        double x = velocity.X / length;
        double y = velocity.Y / length;
        int forward = Snap((x * Math.Cos(heading)) + (y * Math.Sin(heading)));
        int right = Snap((y * Math.Cos(heading)) - (x * Math.Sin(heading)));
        return (forward, right) switch
        {
            (1, 1) => InputDirection.ForwardRight,
            (1, -1) => InputDirection.ForwardLeft,
            (1, _) => InputDirection.Forward,
            (-1, 1) => InputDirection.BackwardRight,
            (-1, -1) => InputDirection.BackwardLeft,
            (-1, _) => InputDirection.Backward,
            (_, 1) => InputDirection.Right,
            (_, -1) => InputDirection.Left,
            _ => InputDirection.None,
        };
    }

    /// <summary>The animation a level plays for a direction; missing ones fall back to the level's no-direction animation.</summary>
    /// <param name="level">Landing level.</param>
    /// <param name="direction">Impact direction relative to the heading.</param>
    /// <returns>The animation.</returns>
    private static HeavyLandingClip Clip(HeavyLandingLevel level, InputDirection direction) => direction switch
    {
        InputDirection.Forward => level.Forward,
        InputDirection.Backward => level.Backward,
        InputDirection.Left or InputDirection.ForwardLeft or InputDirection.BackwardLeft => level.Left,
        InputDirection.Right or InputDirection.ForwardRight or InputDirection.BackwardRight => level.Right,
        _ => null,
    } ?? level.NoDirection;

    /// <summary>Starts a heavy landing: picks the level from the fall height and the animation from the impact direction.</summary>
    /// <param name="fallHeight">Fall height that picks the level.</param>
    /// <param name="impact">Velocity when the landing was detected, the tick before the character touches down.</param>
    /// <param name="velocity">Velocity when the landing starts; a recovery roll follows its direction.</param>
    /// <param name="heading">The character's heading in radians; the impact direction is taken relative to it.</param>
    /// <param name="recovery">Whether a recovery roll softens the landing.</param>
    /// <returns>The landing state before its first step.</returns>
    public static HeavyLandingState Begin(double fallHeight, Vector3D impact, Vector3D velocity, double heading, bool recovery = false)
    {
        if (!double.IsFinite(heading))
        {
            throw new ArgumentOutOfRangeException(nameof(heading));
        }

        IReadOnlyList<HeavyLandingLevel> levels = recovery ? HeavyLandingAnimations.Recovery : HeavyLandingAnimations.Heavy;
        int level = Level(fallHeight, levels);
        HeavyLandingClip clip = Clip(levels[level], Direction(impact, heading));
        double speedSquared = (velocity.X * velocity.X) + (velocity.Y * velocity.Y) + (velocity.Z * velocity.Z);
        double playback = recovery && speedSquared > HeavyLandingParameters.RecoveryHeadingSpeedSquared ? Math.Atan2(velocity.Y, velocity.X) : heading;
        return new HeavyLandingState(level, clip, playback, recovery);
    }

    /// <summary>Landing steps before the landing hands over to ground movement, when nothing ends it early.</summary>
    /// <param name="clip">The landing's animation.</param>
    /// <returns>Number of landing steps.</returns>
    public static int Duration(HeavyLandingClip clip)
    {
        int last = (int)Math.Ceiling(clip.Windows.Max(window => window.Start) * clip.SampleRate) + 1;
        for (int tick = 1; tick <= last; tick++)
        {
            if (Raised(clip, tick) is not null)
            {
                return tick;
            }
        }

        throw new ArgumentException("The animation never hands over", nameof(clip));
    }

    /// <summary>Advances a running heavy landing by one simulation tick.</summary>
    /// <param name="state">Landing state.</param>
    /// <param name="previous">World velocity before the step.</param>
    /// <param name="onGround">Whether the character stands on walkable ground.</param>
    /// <param name="gravity">Vertical acceleration while off the ground, in world units per second squared (negative is down).</param>
    /// <param name="seconds">Simulation step duration.</param>
    /// <returns>Velocity, action posture and whether the landing ends.</returns>
    public static HeavyLandingStep Step(HeavyLandingState state, Vector3D previous, bool onGround, double gravity, double seconds)
    {
        if (!double.IsFinite(seconds) || seconds <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(seconds));
        }

        int tick = state.Ticks + 1;
        HeavyLandingRootPoint to = Root(state.Clip, tick);
        HeavyLandingRootPoint from = Root(state.Clip, tick - 1);
        double right = (to.Right - (double)from.Right) / seconds;
        double forward = (to.Forward - (double)from.Forward) / seconds;
        double cos = Math.Cos(state.Heading);
        double sin = Math.Sin(state.Heading);
        double vertical = previous.Z + (onGround ? 0 : gravity * seconds);
        Vector3D velocity = new((forward * cos) - (right * sin), (forward * sin) + (right * cos), vertical);

        HeavyLandingTransientState? exit = Raised(state.Clip, tick);
        if (exit is null && !onGround && previous.Z < HeavyLandingParameters.FallingExitVerticalSpeed)
        {
            exit = HeavyLandingTransientState.Finished;
        }

        int blend = exit switch
        {
            HeavyLandingTransientState.QuickExit => Ticks(HeavyLandingParameters.QuickExitBlendSeconds, seconds),
            HeavyLandingTransientState.Finished => Ticks(HeavyLandingParameters.FinishedBlendSeconds, seconds),
            _ => 0,
        };
        CharacterStance stance = onGround ? CharacterStance.Uncontrolled : CharacterStance.InAir;
        return new HeavyLandingStep(state with { Ticks = tick }, velocity, stance, LocoMode.Still, !onGround, exit, blend);
    }

    // The quick exit takes precedence where both windows overlap: it is the earlier hand-over.
    private static HeavyLandingTransientState? Raised(HeavyLandingClip clip, int tick)
    {
        double seconds = tick / clip.SampleRate;
        HeavyLandingTransientState? raised = null;
        foreach (HeavyLandingWindow window in clip.Windows)
        {
            if (seconds >= window.Start && seconds <= window.End && raised is not HeavyLandingTransientState.QuickExit)
            {
                raised = window.State;
            }
        }

        return raised;
    }

    private static HeavyLandingRootPoint Root(HeavyLandingClip clip, int frame) =>
        clip.Root.Count == 0 ? default : clip.Root[Math.Clamp(frame, 0, clip.Root.Count - 1)];

    private static int Ticks(double blendSeconds, double seconds) => (int)Math.Round(blendSeconds / seconds);

    // Axis value to -1, 0 or 1 at the eight-way thresholds (sine of 22.5 degrees).
    private static int Snap(double value) =>
        value >= DodgeRollParameters.DirectionThreshold ? 1 : value > DodgeRollParameters.NegativeDirectionThreshold ? 0 : -1;
}
