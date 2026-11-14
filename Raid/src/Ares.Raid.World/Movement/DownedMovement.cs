using Ares.Structs.Core.Geometry;
using Ares.Structs.Game.Movement;

namespace Ares.Raid.World.Movement;

/// <summary>What the downed node is doing on a tick.</summary>
public enum DownedPhase : byte
{
    /// <summary>Held while the character goes down.</summary>
    GoingDown = 0,

    /// <summary>Crawling.</summary>
    Crawling = 1,

    /// <summary>Held while the character stands up.</summary>
    StandingUp = 2,

    /// <summary>Held on walkable floor with the downed health used up.</summary>
    Depleted = 3,
}

/// <summary>How the downed node hands over.</summary>
public enum DownedExit : byte
{
    /// <summary>The character stood up; ground movement takes over.</summary>
    StoodUp = 0,

    /// <summary>The character went down again while standing up; the death node takes over.</summary>
    DownedAgain = 1,
}

/// <summary>Persistent state of the downed node.</summary>
/// <param name="Ticks">Node steps already taken.</param>
/// <param name="SkipGoingDown">Whether the hold while going down is skipped (the node was entered from a fatal fall).</param>
/// <param name="OutOfDownedTick">Node step of the first tick without the downed status, zero before.</param>
/// <param name="GoingDownTag">Whether <c>Animation.PendulumState.DeathToDBNO</c> is held.</param>
/// <param name="StandingUpTag">Whether <c>Animation.PendulumState.DBNOToBaseMove</c> is held.</param>
public readonly record struct DownedState(
    int Ticks = 0,
    bool SkipGoingDown = false,
    int OutOfDownedTick = 0,
    bool GoingDownTag = false,
    bool StandingUpTag = false);

/// <summary>What the downed node reads on a tick.</summary>
/// <param name="Movement">Movement input; only its axes are read.</param>
/// <param name="Yaw">Control yaw in radians.</param>
/// <param name="Downed">Whether the previous tick's result carries <c>GameplayEffect.DBNOStatus</c>.</param>
/// <param name="HealthDepleted">Whether the downed health is used up.</param>
/// <param name="WalkableFloor">Whether the character stands on walkable floor.</param>
/// <param name="GroundNormal">Normal of the ground below, up when there is none.</param>
/// <param name="Immobilized">Whether the input claims mark the character immobilized (<c>Status.Immobilized</c>).</param>
/// <param name="SpeedMultiplier">Downed-movement skill multiplier.</param>
/// <param name="FromFatalFall">Whether a fatal heavy landing hands the character over on this tick.</param>
public readonly record struct DownedInput(
    MovementInput Movement,
    double Yaw,
    bool Downed,
    bool HealthDepleted,
    bool WalkableFloor,
    Vector3D GroundNormal,
    bool Immobilized = false,
    double SpeedMultiplier = DownedMovementParameters.DefaultSpeedMultiplier,
    bool FromFatalFall = false);

/// <summary>Result of one downed-node step.</summary>
/// <param name="State">Node state after the step.</param>
/// <param name="Phase">What the node did.</param>
/// <param name="Velocity">Horizontal world velocity; the body solver owns the vertical.</param>
/// <param name="Yaw">Facing in radians; the character faces the control yaw.</param>
/// <param name="ActionLocomotion">Locomotion of the action layer; the action stance is <see cref="CharacterStance.Dbno"/>.</param>
/// <param name="InAir">Whether the floor is not walkable (rough movement mode air instead of ground).</param>
/// <param name="Crouched">Whether the character stands on the crouched capsule.</param>
/// <param name="ResultingDirection">Eight-way direction of the crawl input, when the crawl wrote it.</param>
/// <param name="MaxMoveSpeed">Current maximum crawl speed, when the crawl wrote it.</param>
/// <param name="SlopeInputScalar">Slope part of the crawl input, when the slope wrote it.</param>
/// <param name="RaisedTransients">Raised transient states, one bit per <see cref="DownedTransientState"/> value.</param>
/// <param name="GraphTags">Graph tags the node holds after the step (<see cref="GraphStateTags"/>).</param>
/// <param name="Exit">How the node hands over after this step, or null while it goes on.</param>
public readonly record struct DownedStep(
    DownedState State,
    DownedPhase Phase,
    Vector3D Velocity,
    double Yaw,
    LocoMode ActionLocomotion,
    bool InAir,
    bool Crouched,
    InputDirection? ResultingDirection,
    double? MaxMoveSpeed,
    double? SlopeInputScalar,
    uint RaisedTransients,
    ulong GraphTags,
    DownedExit? Exit)
{
    /// <summary>Stance of the action layer.</summary>
    public static CharacterStance ActionStance => CharacterStance.Dbno;

    /// <summary>Whether the step raised a transient state.</summary>
    /// <param name="state">The transient state.</param>
    /// <returns>True when raised.</returns>
    public bool Raises(DownedTransientState state) => (RaisedTransients & (1u << (int)state)) != 0;
}

/// <summary>The downed node: going down, crawling, standing up.</summary>
public static class DownedMovement
{
    /// <summary>Advances the downed node by one simulation tick.</summary>
    /// <param name="state">Node state.</param>
    /// <param name="input">What the node reads.</param>
    /// <param name="previous">World velocity before the step.</param>
    /// <param name="seconds">Simulation step duration.</param>
    /// <param name="steerAboveMaximum">Whether input steering is enabled above maximum speed.</param>
    /// <param name="fixBrakingOvershoot">Whether braking overshoot is corrected when accelerating forward.</param>
    /// <returns>Velocity, posture, tags and whether the node hands over.</returns>
    public static DownedStep Step(DownedState state, DownedInput input, Vector3D previous, double seconds,
        bool steerAboveMaximum, bool fixBrakingOvershoot)
    {
        if (!double.IsFinite(seconds) || seconds <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(seconds));
        }

        if (!double.IsFinite(input.Yaw))
        {
            throw new ArgumentOutOfRangeException(nameof(input));
        }

        int tick = state.Ticks + 1;
        Vector3D raw = FlatGroundMovement.Direction(input.Movement, input.Yaw);
        uint raised = input.WalkableFloor ? 0 : Bit(DownedTransientState.UnknownTransient0);
        DownedState next = state with { Ticks = tick };
        if (input is { HealthDepleted: true, WalkableFloor: true })
        {
            return Held(DownedPhase.Depleted, next, raised | Bit(DownedTransientState.UnknownTransient1));
        }

        if (state.StandingUpTag && input.Downed)
        {
            return Held(DownedPhase.StandingUp, next with { StandingUpTag = false }, raised, DownedExit.DownedAgain);
        }

        bool early = tick < Ticks(DownedMovementParameters.IntoDownedSeconds, seconds);
        next = next with { SkipGoingDown = state.SkipGoingDown || input.FromFatalFall };
        switch (early)
        {
            case true when !next.SkipGoingDown:
                return Held(DownedPhase.GoingDown, next with { GoingDownTag = true }, raised);
            case false when state.GoingDownTag:
                next = next with { GoingDownTag = false };
                break;
        }

        if (!input.Downed)
        {
            if (next.OutOfDownedTick == 0)
            {
                next = next with { OutOfDownedTick = tick };
            }

            next = next with { StandingUpTag = true };
            if (tick - next.OutOfDownedTick > Ticks(DownedMovementParameters.OutOfDownedSeconds, seconds))
            {
                return Held(DownedPhase.StandingUp, next with { StandingUpTag = false }, raised | Bit(DownedTransientState.Finished),
                    DownedExit.StoodUp);
            }

            return Held(DownedPhase.StandingUp, next, raised);
        }

        InputDirection direction = HeavyLandingMovement.Direction(raw, input.Yaw);
        double maximum = DownedMovementParameters.CrawlSpeed * input.SpeedMultiplier;
        Vector3D crawl = input.Immobilized ? default : Remap(raw);
        double? slopeScalar = null;
        if (SlopeDegrees(input.GroundNormal) > DownedMovementParameters.SlopeStartDegrees)
        {
            (crawl, double scalar) = Slope(crawl, input.GroundNormal);
            slopeScalar = scalar;
        }

        bool moving = (crawl.X * crawl.X) + (crawl.Y * crawl.Y) > 0;
        if (input.GroundNormal.Z < DownedMovementParameters.SteepGroundNormalZ)
        {
            raised |= Bit(DownedTransientState.UnknownTransient9);
        }

        Vector3D velocity = Velocity(previous, crawl, maximum, input.WalkableFloor, seconds, steerAboveMaximum, fixBrakingOvershoot);
        return new DownedStep(next, DownedPhase.Crawling, velocity, input.Yaw, moving ? LocoMode.Walking : LocoMode.Still,
            !input.WalkableFloor, input.WalkableFloor, direction, maximum, slopeScalar, raised, Tags(next), null);

        DownedStep Held(DownedPhase phase, DownedState held, uint transients, DownedExit? exit = null) =>
            new(held, phase, Velocity(previous, raw, DownedMovementParameters.HeldSpeed, input.WalkableFloor, seconds, steerAboveMaximum,
                    fixBrakingOvershoot),
                input.Yaw, LocoMode.Still, !input.WalkableFloor, input.WalkableFloor, null, null, null, transients, Tags(held), exit);
    }

    /// <summary>
    /// The crawl's stick remapping: input in walk range grows toward full strength (weak input stays weaker), stronger
    /// input becomes full input, and the result never exceeds full input.
    /// </summary>
    /// <param name="input">World-space movement input.</param>
    /// <returns>The remapped horizontal input.</returns>
    public static Vector3D Remap(Vector3D input)
    {
        double strength = Math.Sqrt((input.X * input.X) + (input.Y * input.Y));
        if (strength <= 0)
        {
            return default;
        }

        double scale = 1;
        if (strength < DownedMovementParameters.WalkStickInputThreshold)
        {
            double share = Math.Clamp(strength / DownedMovementParameters.WalkStickInputThreshold, 0, 1);
            const double Strongest = DownedMovementParameters.WalkRunThreshold / DownedMovementParameters.StickRemapBaseSpeed;
            scale = DownedMovementParameters.MinimumWalkInputStrength + (share * (Strongest - DownedMovementParameters.MinimumWalkInputStrength));
        }

        scale = Math.Min(scale, 1);
        return new Vector3D(input.X / strength * scale, input.Y / strength * scale, 0);
    }

    /// <summary>Slope of the ground in degrees.</summary>
    /// <param name="groundNormal">Ground normal.</param>
    /// <returns>Angle between the normal and straight up.</returns>
    public static double SlopeDegrees(Vector3D groundNormal) => Degrees(Math.Acos(Math.Clamp(groundNormal.Z, -1, 1)));

    /// <summary>
    /// Scales input on a slope: the steeper the slope and the more the input points uphill, the weaker the input.
    /// Input longer than full input is shortened to full input first.
    /// </summary>
    /// <param name="input">Horizontal world-space input.</param>
    /// <param name="groundNormal">Ground normal, steeper than <see cref="DownedMovementParameters.SlopeStartDegrees"/>.</param>
    /// <returns>The scaled input and the scale.</returns>
    public static (Vector3D Input, double Scalar) Slope(Vector3D input, Vector3D groundNormal)
    {
        Vector3D direction = Normal(input);
        Vector3D downhill = Normal(groundNormal with { Z = 0 });
        double fromDownhill = Degrees(Math.Acos(Math.Clamp((downhill.X * direction.X) + (downhill.Y * direction.Y), -1, 1)));
        double uphill = MapClamped(180 - fromDownhill, DownedMovementParameters.UphillNoneDegrees, DownedMovementParameters.UphillFullDegrees, 0, 1);
        double steep = MapClamped(SlopeDegrees(groundNormal), DownedMovementParameters.SlopeStartDegrees, DownedMovementParameters.SlopeFullDegrees,
            0, DownedMovementParameters.MaximumSlopeReduction);
        double scalar = (float)(1 - (steep * uphill));
        Vector3D clamped = (input.X * input.X) + (input.Y * input.Y) > 1 ? direction : input;
        return (new Vector3D(clamped.X * scalar, clamped.Y * scalar, 0), scalar);
    }

    private static Vector3D Velocity(Vector3D previous, Vector3D input, double maximum, bool walkable, double seconds,
        bool steerAboveMaximum, bool fixBrakingOvershoot)
    {
        GroundMovementParameters parameters = walkable
            ? DownedMovementParameters.Crawl(maximum)
            : AirMovementParameters.Horizontal(Math.Sqrt((previous.X * previous.X) + (previous.Y * previous.Y) + (previous.Z * previous.Z)));
        return FlatGroundMovement.CalculateVelocity(previous, input, seconds, parameters, steerAboveMaximum, fixBrakingOvershoot);
    }

    private static ulong Tags(DownedState state) =>
        DownedMovementParameters.NodeTags
        | (state.GoingDownTag ? GraphStateTags.AnimationPendulumStateDeathToDbno : 0)
        | (state.StandingUpTag ? GraphStateTags.AnimationPendulumStateDbnoToBaseMove : 0);

    private static uint Bit(DownedTransientState state) => 1u << (int)state;

    // Node times are whole ticks; a duration is compared as the number of ticks it spans.
    private static int Ticks(double durationSeconds, double seconds) => (int)Math.Round(durationSeconds / seconds);

    private static Vector3D Normal(Vector3D vector)
    {
        double squared = (vector.X * vector.X) + (vector.Y * vector.Y) + (vector.Z * vector.Z);
        if (squared < HeavyLandingParameters.DirectionTolerance)
        {
            return default;
        }

        double length = Math.Sqrt(squared);
        return new Vector3D(vector.X / length, vector.Y / length, vector.Z / length);
    }

    private static double Degrees(double radians) => radians * 180 / Math.PI;

    private static double MapClamped(double value, double inFrom, double inTo, double outFrom, double outTo) =>
        outFrom + (Math.Clamp((value - inFrom) / (inTo - inFrom), 0, 1) * (outTo - outFrom));
}
