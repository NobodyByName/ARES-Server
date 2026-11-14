using Ares.Structs.Core.Geometry;
using Ares.Structs.Game.Movement;

namespace Ares.Raid.World.Movement;

/// <summary>
/// What the snap hook pulls its owner towards (<c>UPendulumSnaphookMoveTarget</c>). Times are simulation time in
/// <see cref="PawnBlackboard.TimePerSecond"/> units; <see cref="Invalid"/> marks an unset time.
/// </summary>
/// <param name="Position">World position of the target (follow the hooked component when it moves).</param>
/// <param name="CreatedAt">Simulation time from which the target is valid.</param>
/// <param name="Speed">Pull speed.</param>
/// <param name="AllowedWiggleSpeed">Speed the movement input steers with.</param>
/// <param name="RemovedAt">Simulation time from which the target is no longer valid.</param>
public readonly record struct SnapHookTarget(Vector3D Position, int CreatedAt, double Speed, double AllowedWiggleSpeed, int RemovedAt = SnapHookTarget.Invalid)
{
    /// <summary>An unset time.</summary>
    private const int Invalid = -1;

    /// <summary>Whether the target has not been invalidated.</summary>
    public bool IsActive => CreatedAt != Invalid;

    /// <summary>Whether the target pulls at a simulation time.</summary>
    /// <param name="time">Simulation time.</param>
    /// <returns>Whether it is valid then.</returns>
    public bool IsValidAt(int time) => CreatedAt != Invalid && time >= CreatedAt && (RemovedAt == Invalid || time < RemovedAt);

    /// <summary>The target after the pull ends.</summary>
    /// <param name="time">Simulation time the pull ends at.</param>
    /// <returns>The invalidated target.</returns>
    public SnapHookTarget Invalidate(int time) => this with { CreatedAt = Invalid, RemovedAt = time };
}

/// <summary>What a pulled character's input asks for on one tick.</summary>
/// <param name="Direction">Movement input as a world direction; its length is the input strength.</param>
/// <param name="Jump">Whether the jump claim is set.</param>
/// <param name="SecondaryPressed">Whether secondary use went down this tick (it ends the pull).</param>
/// <param name="HookHeld">
/// Whether the equipped item allows the pull: always, unless the weapon rule (feature flag
/// <c>disable_using_snaphook_while_holding_weapon</c>) is on and the equipped item is not the snap hook.
/// </param>
/// <param name="Wind">Gameplay wind direction times its intensity (zero indoors and without wind).</param>
public readonly record struct SnapHookInput(Vector3D Direction, bool Jump, bool SecondaryPressed, bool HookHeld = true, Vector3D Wind = default);

/// <summary>The ground under a pulled character as the node reads it.</summary>
/// <param name="Distance">Distance from the feet down to the ground.</param>
/// <param name="Walkable">Whether the floor is walkable.</param>
public readonly record struct SnapHookGround(double Distance, bool Walkable);

/// <summary>Persistent state of a pull (the node's own state).</summary>
/// <param name="Target">The move target.</param>
/// <param name="PullDirection">Unit direction to the target when the pull started.</param>
/// <param name="Ticks">Pull steps already taken.</param>
public readonly record struct SnapHookState(SnapHookTarget Target, Vector3D PullDirection, int Ticks = 0);

/// <summary>Why a pull ends.</summary>
public enum SnapHookExit : byte
{
    /// <summary>Still pulled.</summary>
    None = 0,

    /// <summary>Close to the target.</summary>
    Arrived = 1,

    /// <summary>The direction to the target turned too far from the first pull direction (the character passed it).</summary>
    Passed = 2,

    /// <summary>Secondary use was pressed.</summary>
    Cancelled = 3,

    /// <summary>The target is no longer valid.</summary>
    TargetLost = 4,

    /// <summary>The pull is too weak, or the character has been nearly still for a long pull.</summary>
    Stalled = 5,

    /// <summary>The equipped item does not allow the pull.</summary>
    NotHeld = 6,

    /// <summary>The graph is blending out of the node: the target is released and the step is a plain air or ground step.</summary>
    BlendingOut = 7,
}

/// <summary>Result of one pull step.</summary>
/// <param name="State">Pull state after the step.</param>
/// <param name="Velocity">Velocity of the step.</param>
/// <param name="Position">Location after moving with <paramref name="Velocity"/> for the step, without collision.</param>
/// <param name="Flags">Graph state flags (<see cref="SnapHookParameters.PullingFlags"/>).</param>
/// <param name="Exit">Why the pull ends after this step, if it does.</param>
/// <param name="Finished">Whether the step raises the finished state, so the graph returns to ground movement.</param>
/// <param name="Vaulting">Whether the step's vault query found a vault (the vault takes over instead of finishing).</param>
/// <param name="Mode">Movement mode the step writes, if any.</param>
/// <param name="HeavyLandingQuery">Whether the step runs the ground movement's heavy-landing query.</param>
/// <param name="ActionLayerWritten">Whether the step writes the action layer's stance and locomotion.</param>
/// <param name="Stance">Action layer stance (in the air above <see cref="SnapHookParameters.AirStanceHeight"/>).</param>
public readonly record struct SnapHookStep(
    SnapHookState State,
    Vector3D Velocity,
    Vector3D Position,
    ulong Flags,
    SnapHookExit Exit,
    bool Finished,
    bool Vaulting,
    RoughMovementMode? Mode,
    bool HeavyLandingQuery,
    bool ActionLayerWritten,
    CharacterStance Stance);

/// <summary>
/// Being pulled by the snap hook (<c>UPendulumSnapHookNodeData</c>): when the pull starts, then each tick a flight
/// towards the target at the pull speed, steered by the movement input and lifted by a jump, whose velocity catches
/// up over the first half second, and the ends (arrival, passing the target, cancelling, losing the target, stalling),
/// which try a vault before returning to ground movement. The character keeps its heading.
/// </summary>
public static class SnapHookMovement
{
    /// <summary>
    /// Whether the pull starts this tick (<c>UPendulumToSnapHookTransitionNodeData</c>, from ground movement, a ladder
    /// or a zipline): a target valid now and at most <see cref="SnapHookParameters.StartWindowSeconds"/> past its
    /// creation, and the item allowing it.
    /// </summary>
    /// <param name="target">The move target, if any.</param>
    /// <param name="time">Simulation time of the tick.</param>
    /// <param name="hookHeld">Whether the equipped item allows the pull.</param>
    /// <returns>Whether the pull starts.</returns>
    public static bool CanStart(SnapHookTarget? target, int time, bool hookHeld = true) =>
        target is { } found && time <= found.CreatedAt + SnapHookThrow.TimeUnits(SnapHookParameters.StartWindowSeconds) && hookHeld && found.IsValidAt(time);

    /// <summary>Starts a pull: the first pull direction points from the character's location to the target.</summary>
    /// <param name="target">The move target.</param>
    /// <param name="location">The character's location (capsule centre).</param>
    /// <returns>The pull state before its first step.</returns>
    public static SnapHookState Begin(SnapHookTarget target, Vector3D location) =>
        new(target, Vectors.Normal(Vectors.Subtract(target.Position, location)));

    /// <summary>Advances a pull by one simulation tick.</summary>
    /// <param name="state">Pull state.</param>
    /// <param name="input">The tick's input.</param>
    /// <param name="location">The character's location (capsule centre) before the step.</param>
    /// <param name="velocity">Velocity before the step.</param>
    /// <param name="ground">The ground under the character.</param>
    /// <param name="time">Simulation time of the tick.</param>
    /// <param name="seconds">Simulation step duration.</param>
    /// <param name="blendingOut">Whether the graph is blending out of the node.</param>
    /// <param name="vault">The vault query an ending pull runs (from the location, facing the control heading), if any.</param>
    /// <param name="facing">Flat control facing for the vault query.</param>
    /// <returns>Velocity, flags and the exit, if any.</returns>
    public static SnapHookStep Step(SnapHookState state, SnapHookInput input, Vector3D location, Vector3D velocity, SnapHookGround ground, int time,
        double seconds, bool blendingOut = false, RopeSwingVaultQuery? vault = null, Vector3D facing = default)
    {
        if (!double.IsFinite(seconds) || seconds <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(seconds));
        }

        SnapHookState next = state with { Ticks = state.Ticks + 1 };
        if (blendingOut)
        {
            next = next with { Target = next.Target.IsActive ? next.Target.Invalidate(time) : next.Target };
            Vector3D plain = EndVelocity(input, velocity, ground, seconds);
            return new SnapHookStep(next, plain, Advance(location, plain, seconds), SnapHookParameters.PullingFlags, SnapHookExit.BlendingOut, false, false,
                null, false, false, CharacterStance.Stand);
        }

        CharacterStance stance = ground.Distance > SnapHookParameters.AirStanceHeight ? CharacterStance.InAir : CharacterStance.Stand;
        SnapHookExit exit = Ends(next, input, location, velocity, time, out Vector3D pull);
        if (exit != SnapHookExit.None)
        {
            return End(next, exit, input, location, velocity, ground, time, seconds, stance, vault, facing);
        }

        Vector3D steer = Vectors.Normal(input.Direction);
        if (Vectors.Dot(steer, steer) > 1e-8)
        {
            steer = Vectors.Scale(steer, next.Target.AllowedWiggleSpeed);
        }

        if (input.Jump)
        {
            steer = Vectors.Add(JumpTilt(steer), new Vector3D(0, 0, SnapHookParameters.JumpUpSpeed));
        }

        Vector3D flying = FlyingVelocity(velocity, Vectors.Add(pull, steer), next.Target.Speed, seconds);
        double share = Math.Min(next.Ticks * PawnBlackboard.TickSeconds / SnapHookParameters.CatchUpSeconds, 1);
        Vector3D result = InterpolateConstant(velocity, flying, seconds, share * SnapHookParameters.CatchUpRate);
        result = Vectors.Add(result, Vectors.Scale(input.Wind, SnapHookParameters.WindPushSpeed * seconds));
        return new SnapHookStep(next, result, Advance(location, result, seconds), SnapHookParameters.PullingFlags, SnapHookExit.None, false, false, null,
            share < 1, true, stance);
    }

    /// <summary>
    /// The pulled flight's velocity step towards a requested velocity: the engine's flying velocity rule (friction
    /// turns the velocity towards the request, braking above the speed limit, fluid drag, then acceleration along the
    /// request, its length capped at one, up to the limit) with the node's flying parameters.
    /// </summary>
    /// <param name="velocity">Velocity before the step.</param>
    /// <param name="request">Requested velocity (pull plus steering).</param>
    /// <param name="maximumSpeed">Speed limit (the pull speed).</param>
    /// <param name="seconds">Simulation step duration.</param>
    /// <returns>The velocity after the step.</returns>
    public static Vector3D FlyingVelocity(Vector3D velocity, Vector3D request, double maximumSpeed, double seconds)
    {
        double size = Vectors.Length(request);
        double strength = Math.Min(size, 1);
        Vector3D acceleration = size > 0 ? Vectors.Scale(request, SnapHookParameters.FlyingAcceleration * strength / size) : default;
        double maximum = Math.Max(maximumSpeed * strength, 0);
        const double Friction = SnapHookParameters.FlyingFriction;
        bool overspeed = Vectors.Dot(velocity, velocity) > Square(maximum * GroundMovementParameters.OverspeedTolerance);
        if (size == 0 || overspeed)
        {
            Vector3D old = velocity;
            velocity = Brake(velocity, Friction * SnapHookParameters.FlyingBrakingFrictionFactor, SnapHookParameters.FlyingBrakingDeceleration, seconds);
            if (overspeed && Vectors.Dot(velocity, velocity) < Square(maximum) && Vectors.Dot(acceleration, old) > 0)
            {
                velocity = Vectors.Scale(Vectors.Normal(old), maximum);
            }
        }
        else
        {
            Vector3D direction = Vectors.Normal(acceleration);
            double speed = Vectors.Length(velocity);
            velocity = Vectors.Subtract(velocity, Vectors.Scale(Vectors.Subtract(velocity, Vectors.Scale(direction, speed)), Math.Min(seconds * Friction, 1)));
        }

        velocity = Vectors.Scale(velocity, 1 - Math.Min(Friction * seconds, 1));
        if (size == 0)
        {
            return velocity;
        }

        double limit = Vectors.Dot(velocity, velocity) > Square(maximum * GroundMovementParameters.OverspeedTolerance) ? Vectors.Length(velocity) : maximum;
        velocity = Vectors.Add(velocity, Vectors.Scale(acceleration, seconds));
        double after = Vectors.Length(velocity);
        return after > limit && after > 0 ? Vectors.Scale(velocity, limit / after) : velocity;
    }

    // The checks in the node's order; the pull towards the target comes out for the flight.
    private static SnapHookExit Ends(SnapHookState state, SnapHookInput input, Vector3D location, Vector3D velocity, int time, out Vector3D pull)
    {
        pull = default;
        if (input.SecondaryPressed)
        {
            return SnapHookExit.Cancelled;
        }

        if (!input.HookHeld)
        {
            return SnapHookExit.NotHeld;
        }

        Vector3D toTarget = Vectors.Subtract(state.Target.Position, location);
        Vector3D direction = Vectors.Normal(toTarget);
        pull = Vectors.Scale(direction, state.Target.Speed);
        double distanceSquared = Vectors.Dot(toTarget, toTarget);
        if (distanceSquared < Square(SnapHookParameters.StopDistance))
        {
            return SnapHookExit.Arrived;
        }

        if (Vectors.Dot(state.PullDirection, direction) <= SnapHookParameters.MaximumTurnCosine)
        {
            return SnapHookExit.Passed;
        }

        if (distanceSquared < Square(SnapHookParameters.ArrivalDistance))
        {
            return SnapHookExit.Arrived;
        }

        double along = Vectors.Dot(velocity, pull);
        if (along < 0)
        {
            pull = Vectors.Scale(pull, -along * SnapHookParameters.OpposedPullScale);
        }

        if (!state.Target.IsValidAt(time))
        {
            return SnapHookExit.TargetLost;
        }

        bool stalled = state.Ticks * SnapHookThrow.TimeUnits(PawnBlackboard.TickSeconds) > SnapHookThrow.TimeUnits(SnapHookParameters.StallSeconds)
            && Vectors.Dot(velocity, velocity) < Square(SnapHookParameters.StallSpeed);
        return stalled || Vectors.Length(pull) < SnapHookParameters.MinimumPullSpeed ? SnapHookExit.Stalled : SnapHookExit.None;
    }

    // The pull ends: a plain air or ground step, the target released, then a vault query; without a vault the node
    // finishes in the air.
    private static SnapHookStep End(SnapHookState state, SnapHookExit exit, SnapHookInput input, Vector3D location, Vector3D velocity, SnapHookGround ground,
        int time, double seconds, CharacterStance stance, RopeSwingVaultQuery? vault, Vector3D facing)
    {
        Vector3D result = EndVelocity(input, velocity, ground, seconds);
        SnapHookState next = state with { Target = state.Target.IsActive ? state.Target.Invalidate(time) : state.Target };
        bool vaulting = vault is not null && vault(location, facing);
        return new SnapHookStep(next, result, Advance(location, result, seconds), SnapHookParameters.PullingFlags, exit, !vaulting, vaulting,
            vaulting ? RoughMovementMode.Ground : RoughMovementMode.Air, false, true, stance);
    }

    // Off walkable ground: the falling step without input (its speed limit the current speed); on it: the ground step
    // with the node's ground parameters, limited to the current horizontal speed.
    private static Vector3D EndVelocity(SnapHookInput input, Vector3D velocity, SnapHookGround ground, double seconds)
    {
        if (!ground.Walkable)
        {
            Vector3D flat = FlatGroundMovement.CalculateVelocity(velocity, default, seconds, AirMovementParameters.Horizontal(Vectors.Length(velocity)), true, true);
            return flat with { Z = velocity.Z - (AirMovementParameters.FallAcceleration * seconds) };
        }

        double horizontal = Math.Sqrt((velocity.X * velocity.X) + (velocity.Y * velocity.Y));
        GroundMovementParameters parameters = new(horizontal, SnapHookParameters.EndGroundAcceleration, SnapHookParameters.EndGroundBrakingDeceleration,
            SnapHookParameters.EndGroundFriction, SnapHookParameters.EndGroundBrakingFrictionFactor);
        return FlatGroundMovement.CalculateVelocity(velocity with { Z = 0 }, input.Direction, seconds, parameters, true, true);
    }

    // A jump pitches the steering up about its own right axis.
    private static Vector3D JumpTilt(Vector3D steer)
    {
        double yaw = Vectors.Dot(steer, steer) > 0 ? Math.Atan2(steer.Y, steer.X) : 0;
        Vector3D right = new(-Math.Sin(yaw), Math.Cos(yaw), 0);
        return Vectors.RotateAngleAxis(steer, SnapHookParameters.JumpTiltDegrees, right);
    }

    // The engine's velocity braking, in sub-steps of at most 1/33 s.
    private static Vector3D Brake(Vector3D velocity, double friction, double deceleration, double seconds)
    {
        if (Vectors.Dot(velocity, velocity) == 0 || (friction <= 0 && deceleration <= 0))
        {
            return velocity;
        }

        const double SubStep = 1.0 / 33;
        Vector3D old = velocity;
        Vector3D reverse = deceleration > 0 ? Vectors.Scale(Vectors.Normal(velocity), -deceleration) : default;
        double remaining = seconds;
        while (remaining >= 1e-6)
        {
            double dt = remaining > SubStep && friction > 0 ? Math.Min(SubStep, remaining * 0.5) : remaining;
            remaining -= dt;
            velocity = Vectors.Add(velocity, Vectors.Scale(Vectors.Add(Vectors.Scale(velocity, -friction), reverse), dt));
            if (Vectors.Dot(velocity, old) <= 0)
            {
                return default;
            }
        }

        double square = Vectors.Dot(velocity, velocity);
        return square <= GroundMovementParameters.NearlyZeroSquaredSpeed
            || (deceleration > 0 && square <= Square(GroundMovementParameters.BrakingStopSpeed)) ? default : velocity;
    }

    // The engine's constant-rate interpolation of a vector.
    private static Vector3D InterpolateConstant(Vector3D current, Vector3D target, double seconds, double rate)
    {
        Vector3D delta = Vectors.Subtract(target, current);
        double size = Vectors.Length(delta);
        double step = rate * seconds;
        if (size <= step)
        {
            return target;
        }

        return step > 0 ? Vectors.Add(current, Vectors.Scale(delta, step / size)) : current;
    }

    private static double Square(double value) => value * value;

    private static Vector3D Advance(Vector3D location, Vector3D velocity, double seconds) =>
        new(location.X + (velocity.X * seconds), location.Y + (velocity.Y * seconds), location.Z + (velocity.Z * seconds));
}

file static class Vectors
{
    public static Vector3D Add(Vector3D a, Vector3D b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);

    public static Vector3D Subtract(Vector3D a, Vector3D b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);

    public static Vector3D Scale(Vector3D a, double factor) => new(a.X * factor, a.Y * factor, a.Z * factor);

    public static double Dot(Vector3D a, Vector3D b) => (a.X * b.X) + (a.Y * b.Y) + (a.Z * b.Z);

    public static double Length(Vector3D a) => Math.Sqrt(Dot(a, a));

    // The engine's safe normal: zero for (nearly) zero vectors.
    public static Vector3D Normal(Vector3D a)
    {
        double square = Dot(a, a);
        return square > 1e-8 ? Scale(a, 1 / Math.Sqrt(square)) : default;
    }

    // The engine's rotation of a vector by an angle in degrees about a unit axis.
    public static Vector3D RotateAngleAxis(Vector3D v, double angleDeg, Vector3D axis)
    {
        (double s, double c) = Math.SinCos(angleDeg * Math.PI / 180);
        double omc = 1 - c;
        return new Vector3D(
            (((omc * axis.X * axis.X) + c) * v.X) + (((omc * axis.X * axis.Y) - (axis.Z * s)) * v.Y) + (((omc * axis.Z * axis.X) + (axis.Y * s)) * v.Z),
            (((omc * axis.X * axis.Y) + (axis.Z * s)) * v.X) + (((omc * axis.Y * axis.Y) + c) * v.Y) + (((omc * axis.Y * axis.Z) - (axis.X * s)) * v.Z),
            (((omc * axis.Z * axis.X) - (axis.Y * s)) * v.X) + (((omc * axis.Y * axis.Z) + (axis.X * s)) * v.Y) + (((omc * axis.Z * axis.Z) + c) * v.Z));
    }
}
