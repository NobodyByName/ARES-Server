using Ares.Structs.Core.Geometry;
using Ares.Structs.Game.Collision;
using Ares.Structs.Game.Movement;

namespace Ares.Raid.World.Movement;

/// <summary>What a hook trace reached.</summary>
/// <param name="Point">World position of the hit.</param>
/// <param name="Normal">Unit surface normal at the hit, facing the trace.</param>
/// <param name="Surface">Surface type of what was hit.</param>
/// <param name="IsPlayer">Whether the hit belongs to a player (the hook glances off players).</param>
/// <param name="IsArcShield">Whether the hit is an ARC shield (the hook never attaches to one).</param>
/// <param name="StartPenetrating">Whether the trace started inside what it hit.</param>
public readonly record struct GrappleHit(
    Vector3D Point,
    Vector3D Normal,
    PhysicalSurfaceType Surface = PhysicalSurfaceType.Default,
    bool IsPlayer = false,
    bool IsArcShield = false,
    bool StartPenetrating = false);

/// <summary>Traces the hook's path from one point to another and returns the first thing it reaches, if any.</summary>
/// <param name="from">Start of the trace.</param>
/// <param name="to">End of the trace.</param>
/// <returns>The first hit, or <see langword="null"/>.</returns>
public delegate GrappleHit? GrappleTrace(Vector3D from, Vector3D to);

/// <summary>What a flight step of the hook ended in.</summary>
public enum GrappleFlightOutcome : byte
{
    /// <summary>The hook is still flying.</summary>
    Flying = 0,

    /// <summary>The hook attached where it hit.</summary>
    Attached = 1,

    /// <summary>The hook missed: it starts retracting.</summary>
    Missed = 2,
}

/// <summary>
/// The grappling hook item node's own state (<c>UPendulumGrapplingHookItemNode</c>): whether the hook is out and
/// attached, its flight, the attachment and the rope held while not swinging. Ticks are simulation ticks; -1 means none.
/// </summary>
/// <param name="Thrown">Whether the hook is out (<c>bHookThrown</c>); a thrown hook blocks another throw.</param>
/// <param name="Attached">Whether the hook is attached (<c>bHookAttached</c>).</param>
/// <param name="HasTarget">Whether the hook hit something to attach to (<c>bHasTarget</c>).</param>
/// <param name="CreatedTick">Tick of the throw (<c>HookCreatedTimeStamp</c>).</param>
/// <param name="RetractStartTick">Tick the retract counts from (<c>RetractStartTimeStamp</c>).</param>
/// <param name="Position">Hook position (<c>HookPosition</c>).</param>
/// <param name="Velocity">Hook velocity in flight (<c>HookVelocity</c>).</param>
/// <param name="RetractStartLocation">Where the retracting hook travels back from.</param>
/// <param name="AttachmentNormal">Surface normal where the hook attached.</param>
/// <param name="Surface">Surface type where the hook attached (<c>AttachSurfaceType</c>).</param>
/// <param name="DeflectedOffPlayer">Whether the hook glanced off a player during this flight (that player is ignored afterwards).</param>
/// <param name="HeldRopeLength">Rope length held by the ground reel, or -1 when not reeling.</param>
/// <param name="HeldWinchSpeed">Speed of the ground reel.</param>
public readonly record struct GrappleHookState(
    bool Thrown = false,
    bool Attached = false,
    bool HasTarget = false,
    int CreatedTick = -1,
    int RetractStartTick = -1,
    Vector3D Position = default,
    Vector3D Velocity = default,
    Vector3D RetractStartLocation = default,
    Vector3D AttachmentNormal = default,
    PhysicalSurfaceType Surface = PhysicalSurfaceType.Default,
    bool DeflectedOffPlayer = false,
    double HeldRopeLength = -1,
    double HeldWinchSpeed = 0)
{
    /// <summary>The node's starting state: no hook out.</summary>
    public static GrappleHookState Stowed => new(Thrown: false);

    /// <summary>Whether the hook is out and not retracting (<c>IsHookCommitted</c>): the secondary input retracts it.</summary>
    public bool Committed => Thrown && RetractStartTick < 0;

    /// <summary>Whether the hook is retracting.</summary>
    public bool Retracting => Thrown && RetractStartTick >= 0;
}

/// <summary>Result of one flight step.</summary>
/// <param name="State">Hook state after the step.</param>
/// <param name="Outcome">Whether the hook flies on, attached or missed.</param>
/// <param name="Hit">What the hook attached to, if it did.</param>
public readonly record struct GrappleFlightStep(GrappleHookState State, GrappleFlightOutcome Outcome, GrappleHit? Hit);

/// <summary>
/// The grappling hook as a projectile inside the character simulation: the throw, the flight under reduced gravity on
/// a rope that cannot run out further than <see cref="GrapplingHookParameters.HookRange"/>, attaching, glancing off
/// players, missing, retracting, and the rope held by the ground reel while not swinging. Rope wrapping around edges
/// is not modelled: the rope is a single straight segment from the character to the hook.
/// </summary>
public static class GrapplingHookMovement
{
    /// <summary>Velocity of a throw: the camera aim at the launch speed, lifted, plus the character's ground velocity.</summary>
    /// <param name="aimForward">Unit forward vector of the camera aim.</param>
    /// <param name="groundVelocity">The character's ground velocity (the movement state's stored ground velocity).</param>
    /// <returns>The hook's starting velocity.</returns>
    public static Vector3D LaunchVelocity(Vector3D aimForward, Vector3D groundVelocity) =>
        new((aimForward.X * GrapplingHookParameters.MaxLaunchVelocity) + groundVelocity.X,
            (aimForward.Y * GrapplingHookParameters.MaxLaunchVelocity) + groundVelocity.Y,
            (aimForward.Z * GrapplingHookParameters.MaxLaunchVelocity) + GrapplingHookParameters.ThrowUpwardVelocity + groundVelocity.Z);

    /// <summary>Throws the hook (a primary release that is not a cancel, while no hook is out).</summary>
    /// <param name="tick">Tick of the throw.</param>
    /// <param name="throwLocation">Where the hook leaves the character (its throwable location).</param>
    /// <param name="aimForward">Unit forward vector of the camera aim.</param>
    /// <param name="groundVelocity">The character's ground velocity.</param>
    /// <returns>The flying hook.</returns>
    public static GrappleHookState Throw(int tick, Vector3D throwLocation, Vector3D aimForward, Vector3D groundVelocity) =>
        new(Thrown: true, CreatedTick: tick, Position: throwLocation, Velocity: LaunchVelocity(aimForward, groundVelocity));

    /// <summary>Advances a flying hook by one tick.</summary>
    /// <param name="hook">The flying hook.</param>
    /// <param name="tick">The tick being simulated.</param>
    /// <param name="ropeOrigin">Where the rope starts: the character's location (capsule centre).</param>
    /// <param name="capsuleHalfHeight">The character's capsule half height.</param>
    /// <param name="ownerInAir">Whether the character's movement mode is the air (a falling owner lets the hook fly on at the rope end).</param>
    /// <param name="seconds">Simulation step duration.</param>
    /// <param name="trace">The hook's world trace (ignoring the owner and any player the hook already glanced off).</param>
    /// <returns>The hook after the step and what happened.</returns>
    public static GrappleFlightStep Fly(GrappleHookState hook, int tick, Vector3D ropeOrigin, double capsuleHalfHeight, bool ownerInAir, double seconds,
        GrappleTrace trace)
    {
        ArgumentNullException.ThrowIfNull(trace);
        if (!double.IsFinite(seconds) || seconds <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(seconds));
        }

        const double Range = GrapplingHookParameters.HookRange;
        Vector3D velocity = hook.Velocity with
        {
            Z = hook.Velocity.Z + (-AirMovementParameters.FallAcceleration * GrapplingHookParameters.HookGravityScale * seconds),
        };
        Vector3D next = Vectors.Add(hook.Position, Vectors.Scale(velocity, seconds));
        bool atRopeEnd = ClampToRopeEnd(ropeOrigin, Range, ref next, ref velocity);
        GrappleHit? hit = trace(hook.Position, next);
        if (hit is { IsPlayer: false } wall)
        {
            if (!IsWithinReach(ropeOrigin, wall.Point, Range) || wall.IsArcShield)
            {
                return new GrappleFlightStep(Retract(hook with { Velocity = velocity }, tick, true), GrappleFlightOutcome.Missed, null);
            }

            GrappleHookState attached = hook with
            {
                Velocity = velocity,
                HasTarget = true,
                Attached = true,
                Position = Vectors.Add(wall.Point, Vectors.Scale(wall.Normal, GrapplingHookParameters.SurfaceOffset)),
                AttachmentNormal = wall.Normal,
                Surface = wall.Surface,
            };
            return new GrappleFlightStep(attached, GrappleFlightOutcome.Attached, wall);
        }

        GrappleHookState flying = hook with { Position = next, Velocity = velocity };
        if (hit is { } player)
        {
            flying = flying with { DeflectedOffPlayer = true };
            if (!player.StartPenetrating && !Vectors.IsNearlyZero(player.Normal, 1e-4))
            {
                Vector3D deflected = DeflectVelocity(velocity, player.Normal, GrapplingHookParameters.PlayerDeflectMinAngle,
                    GrapplingHookParameters.PlayerDeflectMaxAngle, GrapplingHookParameters.PlayerDeflectSpeedScale);
                Vector3D position = Vectors.Add(player.Point, Vectors.Scale(player.Normal, GrapplingHookParameters.SurfaceOffset));
                atRopeEnd = ClampToRopeEnd(ropeOrigin, Range, ref position, ref deflected);
                flying = flying with { Position = position, Velocity = deflected };
            }
        }

        if (ownerInAir)
        {
            return new GrappleFlightStep(flying, GrappleFlightOutcome.Flying, null);
        }

        bool belowCapsuleTop = flying.Position.Z < ropeOrigin.Z + capsuleHalfHeight;
        double flightSeconds = (tick - hook.CreatedTick) * (double)(float)PawnBlackboard.TickSeconds;
        if ((atRopeEnd && belowCapsuleTop) || flightSeconds >= GrapplingHookParameters.MaxFlightDuration)
        {
            return new GrappleFlightStep(Retract(flying, tick, true), GrappleFlightOutcome.Missed, null);
        }

        return new GrappleFlightStep(flying, GrappleFlightOutcome.Flying, null);
    }

    /// <summary>
    /// Starts retracting the hook (secondary input, a miss, a lost anchor): it detaches and, after
    /// <see cref="GrapplingHookParameters.RetractDelay"/>, travels back. A miss skips the delay.
    /// </summary>
    /// <param name="hook">The hook.</param>
    /// <param name="tick">The tick being simulated.</param>
    /// <param name="skipDelay">Whether the retract starts travelling at once.</param>
    /// <returns>The retracting hook.</returns>
    public static GrappleHookState Retract(GrappleHookState hook, int tick, bool skipDelay) =>
        hook with
        {
            Attached = false,
            HasTarget = false,
            Surface = PhysicalSurfaceType.Default,
            HeldRopeLength = -1,
            HeldWinchSpeed = 0,
            RetractStartTick = skipDelay ? Math.Max(tick - TimeToTicks(GrapplingHookParameters.RetractDelay), 0) : tick,
            RetractStartLocation = hook.Position,
        };

    /// <summary>
    /// Moves a retracting hook: it waits <see cref="GrapplingHookParameters.RetractDelay"/>, then travels back to the
    /// throw location over <see cref="GrapplingHookParameters.RetractDuration"/>; on arrival the hook is put away.
    /// </summary>
    /// <param name="hook">The retracting hook.</param>
    /// <param name="tick">The tick being simulated.</param>
    /// <param name="throwLocation">Where the hook returns to (the character's throwable location this tick).</param>
    /// <returns>The hook after the step: the stowed default state once it is back.</returns>
    public static GrappleHookState RetractStep(GrappleHookState hook, int tick, Vector3D throwLocation)
    {
        double elapsed = (tick - hook.RetractStartTick) * (double)(float)PawnBlackboard.TickSeconds;
        const double Delay = GrapplingHookParameters.RetractDelay;
        if (elapsed < Delay)
        {
            return hook;
        }

        double alpha = Math.Clamp((elapsed - Delay) / GrapplingHookParameters.RetractDuration, 0, 1);
        Vector3D start = hook.RetractStartLocation;
        Vector3D position = Vectors.Add(start, Vectors.Scale(Vectors.Subtract(throwLocation, start), alpha));
        return alpha >= 1 ? GrappleHookState.Stowed : hook with { Position = position };
    }

    /// <summary>
    /// The rope the ground reel holds while the character is not swinging (<c>ResolveHeldRopeLength</c>): without the
    /// primary input held the rope is the full range; holding it starts from the current rope path and shortens it by
    /// the reel, down to <see cref="GrapplingHookParameters.MinHeldRopeLength"/>.
    /// </summary>
    /// <param name="holding">Whether the primary input is held (and firing is not blocked).</param>
    /// <param name="ropePath">Length of the rope path from the character to the anchor.</param>
    /// <param name="reelDelta">Rope reeled in this tick (winch speed times the step).</param>
    /// <param name="held">The rope held so far, or -1; updated.</param>
    /// <returns>The rope's working length.</returns>
    public static double HeldRopeLength(bool holding, double ropePath, double reelDelta, ref double held)
    {
        if (!holding)
        {
            held = -1;
            return GrapplingHookParameters.HookRange;
        }

        if (held < 0)
        {
            held = Math.Min(ropePath, GrapplingHookParameters.HookRange);
        }

        held = Math.Max(held - reelDelta, GrapplingHookParameters.MinHeldRopeLength);
        return held;
    }

    /// <summary>Factor on the ground reel's speed: lower while the flat movement input points away from the anchor.</summary>
    /// <param name="inputWorldFlat">Movement input in world space, flat.</param>
    /// <param name="toPivotFlat">Flat unit direction from the character to the anchor.</param>
    /// <returns>The factor.</returns>
    public static double ReelSpeedScale(Vector3D inputWorldFlat, Vector3D toPivotFlat)
    {
        if (Vectors.IsNearlyZero(inputWorldFlat, 1e-4) || Vectors.IsNearlyZero(toPivotFlat, 1e-4))
        {
            return 1;
        }

        Vector3D input = Vectors.Normal(inputWorldFlat with { Z = 0 });
        return Vectors.Dot(input, toPivotFlat) > GrapplingHookParameters.BackpedalCosine ? 1 : GrapplingHookParameters.BackpedalReelSpeedScale;
    }

    /// <summary>
    /// Keeps a position within <paramref name="maxLength"/> of <paramref name="origin"/> (<c>ClampToRopeEnd</c>): a
    /// position beyond it is pulled back onto the sphere and the velocity loses its outward part.
    /// </summary>
    /// <param name="origin">Rope origin.</param>
    /// <param name="maxLength">Rope length.</param>
    /// <param name="position">Position; moved onto the sphere when beyond it.</param>
    /// <param name="velocity">Velocity; loses its outward part when the position was beyond the sphere.</param>
    /// <returns>Whether the position was beyond the rope end.</returns>
    private static bool ClampToRopeEnd(Vector3D origin, double maxLength, ref Vector3D position, ref Vector3D velocity)
    {
        Vector3D offset = Vectors.Subtract(position, origin);
        double distance = Vectors.Length(offset);
        if (distance <= maxLength || distance <= 1e-8)
        {
            return false;
        }

        Vector3D outward = Vectors.Normal(offset);
        position = Vectors.Add(origin, Vectors.Scale(outward, maxLength));
        velocity = Vectors.Subtract(velocity, Vectors.Scale(outward, Math.Max(Vectors.Dot(outward, velocity), 0)));
        return true;
    }

    /// <summary>
    /// Turns a velocity that runs into a surface away from it (<c>DeflectVelocity</c>): by twice the angle of incidence
    /// (as a grazing angle), clamped to [<paramref name="minAngleDeg"/>, <paramref name="maxAngleDeg"/>], about the axis
    /// across the velocity and the normal, keeping <paramref name="speedScale"/> of the speed.
    /// </summary>
    /// <param name="velocity">Incoming velocity.</param>
    /// <param name="normal">Surface normal.</param>
    /// <param name="minAngleDeg">Smallest turn, in degrees.</param>
    /// <param name="maxAngleDeg">Largest turn, in degrees.</param>
    /// <param name="speedScale">Share of the speed kept.</param>
    /// <returns>The outgoing velocity; unchanged when it does not run into the surface.</returns>
    public static Vector3D DeflectVelocity(Vector3D velocity, Vector3D normal, double minAngleDeg, double maxAngleDeg, double speedScale)
    {
        Vector3D direction = Vectors.Normal(velocity);
        Vector3D unitNormal = Vectors.Normal(normal);
        double into = -Vectors.Dot(direction, unitNormal);
        if (into <= 1e-8)
        {
            return velocity;
        }

        Vector3D axis = Vectors.Cross(direction, unitNormal);
        if (Vectors.IsNearlyZero(axis, 1e-4))
        {
            axis = FirstTangent(direction);
        }

        double angle = Math.Clamp(2 * (Math.Asin(Math.Min(into, 1)) * 180 / Math.PI), minAngleDeg, maxAngleDeg);
        Vector3D turned = Vectors.RotateAngleAxis(direction, angle, Vectors.Normal(axis));
        return Vectors.Scale(Vectors.Scale(turned, Vectors.Length(velocity)), speedScale);
    }

    /// <summary>Whether a point lies within the rope's reach of the origin.</summary>
    /// <param name="origin">Rope origin.</param>
    /// <param name="point">Point.</param>
    /// <param name="maxLength">Rope length.</param>
    /// <returns>Whether the distance is at most the rope length.</returns>
    private static bool IsWithinReach(Vector3D origin, Vector3D point, double maxLength)
    {
        Vector3D offset = Vectors.Subtract(point, origin);
        return Vectors.Dot(offset, offset) <= maxLength * maxLength;
    }

    /// <summary>Seconds as whole simulation ticks.</summary>
    /// <param name="seconds">Seconds.</param>
    /// <returns>The ticks.</returns>
    public static int TimeToTicks(double seconds) => (int)Math.Round(seconds / PawnBlackboard.TickSeconds);

    // The first of two tangents of a direction (reference up, or right when the direction is vertical).
    private static Vector3D FirstTangent(Vector3D direction)
    {
        Vector3D unit = Vectors.Normal(direction);
        Vector3D reference = 1 - Math.Abs(unit.Z) < 1e-6 ? new Vector3D(0, 1, 0) : new Vector3D(0, 0, 1);
        return Vectors.Normal(Vectors.Cross(unit, reference));
    }
}

file static class Vectors
{
    public static Vector3D Add(Vector3D a, Vector3D b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);

    public static Vector3D Subtract(Vector3D a, Vector3D b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);

    public static Vector3D Scale(Vector3D a, double factor) => new(a.X * factor, a.Y * factor, a.Z * factor);

    public static double Dot(Vector3D a, Vector3D b) => (a.X * b.X) + (a.Y * b.Y) + (a.Z * b.Z);

    public static Vector3D Cross(Vector3D a, Vector3D b) => new((a.Y * b.Z) - (a.Z * b.Y), (a.Z * b.X) - (a.X * b.Z), (a.X * b.Y) - (a.Y * b.X));

    public static double Length(Vector3D a) => Math.Sqrt(Dot(a, a));

    public static bool IsNearlyZero(Vector3D a, double tolerance) => Math.Abs(a.X) <= tolerance && Math.Abs(a.Y) <= tolerance && Math.Abs(a.Z) <= tolerance;

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
        double xx = axis.X * axis.X;
        double yy = axis.Y * axis.Y;
        double zz = axis.Z * axis.Z;
        double xy = axis.X * axis.Y;
        double yz = axis.Y * axis.Z;
        double zx = axis.Z * axis.X;
        double xs = axis.X * s;
        double ys = axis.Y * s;
        double zs = axis.Z * s;
        double omc = 1 - c;
        return new Vector3D(
            (((omc * xx) + c) * v.X) + (((omc * xy) - zs) * v.Y) + (((omc * zx) + ys) * v.Z),
            (((omc * xy) + zs) * v.X) + (((omc * yy) + c) * v.Y) + (((omc * yz) - xs) * v.Z),
            (((omc * zx) - ys) * v.X) + (((omc * yz) + xs) * v.Y) + (((omc * zz) + c) * v.Z));
    }
}
