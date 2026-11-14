using Ares.Structs.Core.Geometry;
using Ares.Structs.Game.Movement;

namespace Ares.Raid.World.Movement;

/// <summary>Persistent state of a running dodge roll.</summary>
/// <param name="Kind">Roll variant chosen at the start.</param>
/// <param name="Direction">Horizontal unit direction of the roll, fixed at the start.</param>
/// <param name="Ticks">Roll steps already taken.</param>
public readonly record struct DodgeRollState(DodgeRollKind Kind, Vector3D Direction, int Ticks = 0);

/// <summary>Result of one dodge-roll step.</summary>
/// <param name="State">Persistent roll state after the step.</param>
/// <param name="Velocity">World velocity of the step.</param>
/// <param name="ActionStance">Stance of the action layer.</param>
/// <param name="ActionLocomotion">Locomotion of the action layer.</param>
/// <param name="InAir">Whether the character is off the ground (rough movement mode air instead of ground).</param>
/// <param name="Finished">Whether the roll requests to leave for ground movement after this step.</param>
public readonly record struct DodgeRollStep(
    DodgeRollState State,
    Vector3D Velocity,
    CharacterStance ActionStance,
    LocoMode ActionLocomotion,
    bool InAir,
    bool Finished);

/// <summary>Dodge-roll entry, direction and root-motion velocity.</summary>
public static class DodgeRollMovement
{
    /// <summary>Whether this input starts a roll from ground movement or a slide.</summary>
    /// <param name="input">Movement axes and buttons of the tick.</param>
    /// <param name="inAir">Whether the character is off the ground.</param>
    /// <param name="blocked">Whether an ability blocks rolling.</param>
    /// <returns>True when the roll starts after this tick.</returns>
    public static bool CanStart(MovementInput input, bool inAir, bool blocked = false)
    {
        float strength = MathF.Abs(Axis(input.Forward)) + MathF.Abs(Axis(input.Right));
        return input.DodgeRollPressed && !inAir && !blocked && strength > DodgeRollParameters.EntryInputThreshold;
    }

    /// <summary>The roll variant for the character's state when the roll starts.</summary>
    /// <param name="sprinting">Whether the sprint ability is active.</param>
    /// <param name="exhausted">Whether the character is exhausted.</param>
    /// <param name="heavy">Whether the character carries a heavy load.</param>
    /// <param name="skill">Whether the character has the roll-improving skill.</param>
    /// <returns>The variant.</returns>
    public static DodgeRollKind Select(bool sprinting, bool exhausted = false, bool heavy = false, bool skill = false)
    {
        DodgeRollKind ordinary = skill ? DodgeRollKind.Ninja : DodgeRollKind.Standard;
        if (sprinting && heavy)
        {
            return ordinary;
        }

        if (exhausted || heavy)
        {
            return DodgeRollKind.Exhausted;
        }

        return sprinting ? DodgeRollKind.Sprint : ordinary;
    }

    /// <summary>The eight-way direction of the movement axes.</summary>
    /// <param name="input">Movement axes.</param>
    /// <returns>The direction.</returns>
    public static InputDirection Direction(MovementInput input)
    {
        double throttle = Axis(input.Forward);
        double strafe = Axis(input.Right);
        int forward = throttle >= DodgeRollParameters.DirectionThreshold ? 1 : throttle > DodgeRollParameters.NegativeDirectionThreshold ? 0 : -1;
        int right = strafe >= DodgeRollParameters.DirectionThreshold ? 1 : strafe > DodgeRollParameters.NegativeDirectionThreshold ? 0 : -1;
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

    /// <summary>Starts a roll: picks its horizontal direction from the eight-way input relative to the control heading.</summary>
    /// <param name="kind">Roll variant.</param>
    /// <param name="input">Movement axes of the tick that starts the roll.</param>
    /// <param name="yaw">Control heading in radians.</param>
    /// <param name="velocity">Velocity when the roll starts; its direction is used without an eight-way input.</param>
    /// <param name="facing">Character heading in radians, used when neither gives a direction.</param>
    /// <returns>The roll state before its first step.</returns>
    public static DodgeRollState Begin(DodgeRollKind kind, MovementInput input, double yaw, Vector3D velocity, double facing)
    {
        if (!double.IsFinite(yaw) || !double.IsFinite(facing))
        {
            throw new ArgumentOutOfRangeException(nameof(yaw));
        }

        InputDirection snapped = Direction(input);
        (double forward, double right) = Coordinates(snapped);
        Vector3D direction = new((forward * Math.Cos(yaw)) - (right * Math.Sin(yaw)), (forward * Math.Sin(yaw)) + (right * Math.Cos(yaw)), 0);
        if (snapped == InputDirection.None)
        {
            double squared = (velocity.X * velocity.X) + (velocity.Y * velocity.Y) + (velocity.Z * velocity.Z);
            double length = Math.Sqrt(squared);
            direction = squared >= DodgeRollParameters.DirectionTolerance ? new Vector3D(velocity.X / length, velocity.Y / length, velocity.Z / length) : default;
        }

        if (direction == default)
        {
            direction = new Vector3D(Math.Cos(facing), Math.Sin(facing), 0);
        }

        double horizontal = Math.Sqrt((direction.X * direction.X) + (direction.Y * direction.Y));
        Vector3D flat = horizontal > 0 ? new Vector3D(direction.X / horizontal, direction.Y / horizontal, 0) : new Vector3D(Math.Cos(facing), Math.Sin(facing), 0);
        return new DodgeRollState(kind, flat);
    }

    /// <summary>Roll steps before the roll hands over to ground movement, when nothing ends it early.</summary>
    /// <param name="kind">Roll variant.</param>
    /// <returns>Number of roll steps.</returns>
    public static int Duration(DodgeRollKind kind)
    {
        int ticks = 1;
        while (!EndsAt(kind, ticks))
        {
            ticks++;
        }

        return ticks;
    }

    /// <summary>Advances a running roll by one simulation tick.</summary>
    /// <param name="state">Roll state.</param>
    /// <param name="previous">World velocity before the step.</param>
    /// <param name="onGround">Whether the character stands on walkable ground.</param>
    /// <param name="gravity">Vertical acceleration while off the ground, in world units per second squared (negative is down).</param>
    /// <param name="seconds">Simulation step duration.</param>
    /// <param name="crouch">Whether crouch is held (and not blocked).</param>
    /// <param name="collided">Whether the roll's movement hit an obstacle.</param>
    /// <param name="crouchExitDisabled">Whether the game's crouch-exit fix keeps crouch from ending the roll.</param>
    /// <returns>Velocity, action posture and whether the roll ends.</returns>
    public static DodgeRollStep Step(DodgeRollState state, Vector3D previous, bool onGround, double gravity, double seconds,
        bool crouch = false, bool collided = false, bool crouchExitDisabled = false)
    {
        if (!double.IsFinite(seconds) || seconds <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(seconds));
        }

        DodgeRollSequence sequence = DodgeRollParameters.Sequence(state.Kind);
        int tick = state.Ticks + 1;
        double travelled = Root(sequence, AnimationTick(sequence, tick)) - Root(sequence, AnimationTick(sequence, tick - 1));
        double speed = travelled * sequence.Distance / DodgeRollParameters.AuthoredDistance(state.Kind) / seconds;
        double vertical = previous.Z + (onGround ? 0 : gravity * seconds);
        Vector3D velocity = new(state.Direction.X * speed, state.Direction.Y * speed, vertical);
        double animationSeconds = AnimationTick(sequence, tick) / sequence.SampleRate;
        bool crouchExit = Active(sequence, DodgeRollTransientState.CrouchExit, animationSeconds);
        bool finished = EndsAt(state.Kind, tick);
        CharacterStance stance = onGround ? CharacterStance.Uncontrolled : CharacterStance.InAir;
        if (crouch && crouchExit)
        {
            stance = CharacterStance.Crouch;
            finished |= !crouchExitDisabled;
        }

        if (!collided)
        {
            return new DodgeRollStep(state with { Ticks = tick }, velocity, stance, LocoMode.Running, !onGround, finished);
        }

        velocity = new Vector3D(0, 0, velocity.Z);
        // The game measures the roll's age in ticks against a time in seconds, so a collision can end the roll
        // from its second step on.
        finished |= state.Ticks > DodgeRollParameters.CollisionGraceSeconds
            && !crouchExit && !Active(sequence, DodgeRollTransientState.CollisionGrace, animationSeconds);
        return new DodgeRollStep(state with { Ticks = tick }, velocity, stance, LocoMode.Running, !onGround, finished);
    }

    private static bool EndsAt(DodgeRollKind kind, int tick)
    {
        // The roll hands over once the rest of its animation fits in the exit blend.
        DodgeRollSequence sequence = DodgeRollParameters.Sequence(kind);
        double remaining = (sequence.RootForward.Count - 1 - AnimationTick(sequence, tick)) / sequence.SampleRate;
        return remaining <= DodgeRollParameters.ExitBlendSeconds;
    }

    private static bool Active(DodgeRollSequence sequence, DodgeRollTransientState state, double seconds) =>
        sequence.Windows.Any(window => window.State == state && seconds >= window.Start && seconds <= window.End);

    // The animation advances by whole ticks scaled by its play rate.
    private static int AnimationTick(DodgeRollSequence sequence, int tick) => (int)Math.Floor((tick * sequence.Speed) + 0.5);

    private static double Root(DodgeRollSequence sequence, int key) => sequence.RootForward[Math.Clamp(key, 0, sequence.RootForward.Count - 1)];

    private static float Axis(sbyte value) => value / (float)PendulumInput.MovementAxisMaximum;

    /// <summary>Forward and right components of an eight-way direction.</summary>
    internal static (double Forward, double Right) Coordinates(InputDirection direction) => direction switch
    {
        InputDirection.Forward => (1, 0),
        InputDirection.Backward => (-1, 0),
        InputDirection.Left => (0, -1),
        InputDirection.Right => (0, 1),
        InputDirection.ForwardLeft => (DodgeRollParameters.Diagonal, DodgeRollParameters.NegativeDiagonal),
        InputDirection.ForwardRight => (DodgeRollParameters.Diagonal, DodgeRollParameters.Diagonal),
        InputDirection.BackwardLeft => (DodgeRollParameters.NegativeDiagonal, DodgeRollParameters.NegativeDiagonal),
        InputDirection.BackwardRight => (DodgeRollParameters.NegativeDiagonal, DodgeRollParameters.Diagonal),
        _ => (0, 0),
    };
}
