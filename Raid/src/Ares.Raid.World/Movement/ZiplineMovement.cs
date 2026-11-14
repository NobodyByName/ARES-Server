using Ares.Structs.Core.Geometry;
using Ares.Structs.Game.Movement;
using Ares.Structs.Game.World;

namespace Ares.Raid.World.Movement;

/// <summary>
/// A rideable zipline in the frame the ride uses: its X axis runs from the line start towards the end (the look-at
/// rotation from start to end), so a rider's hands sit at X along the line and Y/Z off it.
/// </summary>
/// <param name="Start">World position of the line start.</param>
/// <param name="Rotation">Look-at rotation from the start towards the end.</param>
/// <param name="Length">Line length.</param>
/// <param name="ActorLocation">World location of the line actor (its exit points are in the actor's space).</param>
/// <param name="ActorRotation">World rotation of the line actor.</param>
/// <param name="ExitPoints">The line's exit points.</param>
/// <param name="ForcedTravelDirectionThresholdHome">Distance from the end within which attaching rides back towards the start.</param>
public sealed record ZiplineLine(
    Vector3D Start,
    Rotator Rotation,
    double Length,
    Vector3D ActorLocation,
    Rotator ActorRotation,
    IReadOnlyList<ZiplineExitPoint> ExitPoints,
    double ForcedTravelDirectionThresholdHome)
{
    /// <summary>The ride frame of a placed line: from its actor location to its end.</summary>
    /// <param name="placement">The line.</param>
    /// <returns>Its ride frame.</returns>
    public static ZiplineLine From(ZiplinePlacement placement)
    {
        Vector3D end = placement.End;
        Vector3D direction = Vectors.Subtract(end, placement.Location);
        double horizontal = Math.Sqrt((direction.X * direction.X) + (direction.Y * direction.Y));
        Rotator rotation = new(Math.Atan2(direction.Z, horizontal) * 180 / Math.PI, Math.Atan2(direction.Y, direction.X) * 180 / Math.PI, 0);
        return new ZiplineLine(placement.Location, rotation, Vectors.Length(direction), placement.Location, placement.Rotation,
            placement.ExitPoints, DamZiplines.ForcedTravelDirectionThresholdHome);
    }

    /// <summary>A world point in the line frame.</summary>
    /// <param name="world">World point.</param>
    /// <returns>Along the line, then off it.</returns>
    public Vector3D Local(Vector3D world) => LocalDirection(Vectors.Subtract(world, Start));

    /// <summary>A world direction in the line frame.</summary>
    /// <param name="world">World direction.</param>
    /// <returns>The direction in line axes.</returns>
    public Vector3D LocalDirection(Vector3D world)
    {
        Vector3D x = Rotation.Rotate(new Vector3D(1, 0, 0));
        Vector3D y = Rotation.Rotate(new Vector3D(0, 1, 0));
        Vector3D z = Rotation.Rotate(new Vector3D(0, 0, 1));
        return new Vector3D(Vectors.Dot(world, x), Vectors.Dot(world, y), Vectors.Dot(world, z));
    }

    /// <summary>A point of the line frame in world space.</summary>
    /// <param name="local">Point in line axes.</param>
    /// <returns>The world point.</returns>
    public Vector3D World(Vector3D local) => Vectors.Add(Start, Rotation.Rotate(local));

    /// <summary>A point of the line actor's space in world space.</summary>
    /// <param name="actorLocal">Point in the actor's space.</param>
    /// <returns>The world point.</returns>
    public Vector3D ActorPoint(Vector3D actorLocal) => Vectors.Add(ActorLocation, ActorRotation.Rotate(actorLocal));
}

/// <summary>Why a ride ends.</summary>
public enum ZiplineExit : byte
{
    /// <summary>Still riding.</summary>
    None = 0,

    /// <summary>Past the end of the line in the riding direction: the rider keeps its velocity, clamped.</summary>
    LineEnd = 1,

    /// <summary>An exit point of the line launched the rider.</summary>
    ExitPoint = 2,

    /// <summary>A jump off the line.</summary>
    JumpOff = 3,

    /// <summary>Let go: electrified, interactions soft-blocked, or the rider collided with something.</summary>
    Detached = 4,

    /// <summary>While being caught onto the line the rider fell too fast for it.</summary>
    FellThrough = 5,
}

/// <summary>What a rider's input and status ask for on one tick.</summary>
/// <param name="Direction">Eight-way direction of the movement axes (only a jump-off uses it).</param>
/// <param name="ControlYaw">Control heading in radians; the rider faces it.</param>
/// <param name="JumpPressed">Whether jump was pressed this tick.</param>
/// <param name="Electrified">Whether the rider is electrified (claims bit or the previous result's tag).</param>
/// <param name="SoftBlockInteractions">Whether interactions are soft-blocked (character claims).</param>
public readonly record struct ZiplineInput(
    InputDirection Direction,
    double ControlYaw,
    bool JumpPressed,
    bool Electrified = false,
    bool SoftBlockInteractions = false);

/// <summary>Persistent state of a ride (the node's own state: direction, entry help, detach and fall-damage flags).</summary>
/// <param name="Line">The line.</param>
/// <param name="Direction">Riding direction, fixed when the ride starts.</param>
/// <param name="Ticks">Ride steps already taken.</param>
/// <param name="NeedsEntryHelp">Whether hands past a line end are pulled back while being caught.</param>
/// <param name="ShouldDetach">Whether the rider lets go.</param>
/// <param name="Jumped">Whether the rider jumped off.</param>
/// <param name="StartedDetaching">Whether the ride has ended.</param>
/// <param name="AllowFallDamage">Whether the last step allowed fall damage (being caught, or braking).</param>
public readonly record struct ZiplineState(
    ZiplineLine Line,
    ZiplineTravelDirection Direction,
    int Ticks = 0,
    bool NeedsEntryHelp = false,
    bool ShouldDetach = false,
    bool Jumped = false,
    bool StartedDetaching = false,
    bool AllowFallDamage = false)
{
    /// <summary>Whether the next step is still catching the rider onto the line.</summary>
    public bool Entering => Ticks + 1 < ZiplineMovement.EntryTicks;
}

/// <summary>Result of one ride step.</summary>
/// <param name="State">Ride state after the step.</param>
/// <param name="Velocity">Velocity of the step.</param>
/// <param name="Position">Feet after moving with <paramref name="Velocity"/> for the step, without collision.</param>
/// <param name="Yaw">Heading the rider turns to (the control heading), in radians.</param>
/// <param name="Flags">Graph state flags (<see cref="ZiplineParameters.RidingFlags"/>).</param>
/// <param name="Exit">Why the ride ends after this step, if it does.</param>
/// <param name="Finished">Whether the step raises the finished state, so the graph returns to ground movement.</param>
/// <param name="HeavyLandingQuery">Whether the step runs the ground movement's heavy-landing query.</param>
/// <param name="ActionLayerWritten">Whether the step writes the action layer's stance and locomotion.</param>
public readonly record struct ZiplineStep(
    ZiplineState State,
    Vector3D Velocity,
    Vector3D Position,
    double Yaw,
    ulong Flags,
    ZiplineExit Exit,
    bool Finished,
    bool HeavyLandingQuery,
    bool ActionLayerWritten);

/// <summary>
/// Zipline riding: the riding direction chosen on attaching, then each tick a velocity that moves the rider's hands
/// along the line (accelerating with its slope) and pulls them onto it, and the exits (line end, exit points, jump,
/// letting go, falling through).
/// </summary>
public static class ZiplineMovement
{
    /// <summary>Ride ticks during which the rider is caught onto the line.</summary>
    public static int EntryTicks => (int)Math.Round(ZiplineParameters.EntrySeconds / PawnBlackboard.TickSeconds);

    /// <summary>
    /// Where the hands hang relative to the capsule centre, negated: the inverse of the character mesh's relative
    /// placement plus the inverse of the line attachment pose, both as locations.
    /// </summary>
    public static Vector3D HangOffset
    {
        get
        {
            Vector3D mesh = new Rotator(0, -DamZiplines.CharacterMeshYaw, 0).Rotate(DamZiplines.CharacterMeshLocation);
            Transform attachment = DamZiplines.AttachmentOffset;
            Vector3D pose = InverseRotate(attachment.Rotation, attachment.Translation);
            return new Vector3D(-mesh.X - pose.X, -mesh.Y - pose.Y, -mesh.Z - pose.Z);
        }
    }

    /// <summary>
    /// Starts a ride: the riding direction from where the rider attaches (near the start: away; near the end: home),
    /// else from whether the camera looks along the line.
    /// </summary>
    /// <param name="line">The line.</param>
    /// <param name="centre">World position of the capsule centre.</param>
    /// <param name="controlForward">Forward direction of the full control rotation.</param>
    /// <returns>The ride state before its first step.</returns>
    public static ZiplineState Begin(ZiplineLine line, Vector3D centre, Vector3D controlForward)
    {
        ArgumentNullException.ThrowIfNull(line);
        double along = line.Local(centre).X;
        double fraction = along / line.Length;
        ZiplineTravelDirection direction = ZiplineTravelDirection.None;
        if (fraction <= 0.5)
        {
            if (along < ZiplineParameters.ForcedTravelDirectionThresholdAway)
            {
                direction = ZiplineTravelDirection.Away;
            }
        }
        else if (line.Length - along < line.ForcedTravelDirectionThresholdHome)
        {
            direction = ZiplineTravelDirection.Home;
        }

        if (direction != ZiplineTravelDirection.None)
        {
            return new ZiplineState(line, direction);
        }

        Vector3D forward = line.Rotation.Rotate(new Vector3D(1, 0, 0));
        direction = Vectors.Dot(controlForward, forward) < 0 ? ZiplineTravelDirection.Home : ZiplineTravelDirection.Away;
        return new ZiplineState(line, direction);
    }

    /// <summary>Advances a ride by one simulation tick.</summary>
    /// <param name="state">Ride state.</param>
    /// <param name="input">The tick's input.</param>
    /// <param name="feet">World position of the feet before the step.</param>
    /// <param name="velocity">Velocity before the step.</param>
    /// <param name="halfHeight">Current capsule half height (feet to centre).</param>
    /// <param name="seconds">Simulation step duration.</param>
    /// <param name="collided">Whether the previous movement sweep collided.</param>
    /// <param name="floorDistance">Distance to a walkable floor below, or <see langword="null"/> without one.</param>
    /// <returns>Velocity, heading, flags and the exit, if any.</returns>
    public static ZiplineStep Step(ZiplineState state, ZiplineInput input, Vector3D feet, Vector3D velocity, double halfHeight, double seconds,
        bool collided = false, double? floorDistance = null)
    {
        if (!double.IsFinite(seconds) || seconds <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(seconds));
        }

        if (!double.IsFinite(halfHeight) || halfHeight <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(halfHeight));
        }

        ZiplineLine line = state.Line;
        bool entering = state.Entering;
        ZiplineState next = state with
        {
            Ticks = state.Ticks + 1,
            AllowFallDamage = false,
            ShouldDetach = state.ShouldDetach || input.Electrified || input.SoftBlockInteractions,
        };
        Vector3D centre = feet with { Z = feet.Z + halfHeight };
        Vector3D hands = Vectors.Subtract(centre, HangOffset);
        Vector3D local = line.Local(hands);
        double yaw = input.ControlYaw;
        bool launched = Launches(entering, line, local, feet, centre, velocity, state.Direction, out Vector3D launch, out ZiplineExit exit);
        if (launched || (collided && !entering))
        {
            next = next with { ShouldDetach = true };
        }

        Vector3D result = launch;
        bool heavyLanding = false;
        if (!launched)
        {
            double over = local.X <= -ZiplineParameters.EndMargin ? local.X
                : local.X >= line.Length + ZiplineParameters.EndMargin ? local.X - line.Length : 0;
            bool within = local.X > -ZiplineParameters.EndMargin && local.X < line.Length + ZiplineParameters.EndMargin;
            Vector3D along;
            if (entering)
            {
                next = next with { AllowFallDamage = true };
                heavyLanding = true;
                if (next.ShouldDetach || (velocity.Z < ZiplineParameters.CatchFallSpeed && Math.Abs(line.Rotation.Pitch) < ZiplineParameters.CatchMaxPitch))
                {
                    ZiplineExit why = next.ShouldDetach ? ZiplineExit.Detached : ZiplineExit.FellThrough;
                    return FallThrough(next with { ShouldDetach = true }, why, feet, velocity, seconds, yaw);
                }

                along = Catch(next, velocity, over, within, seconds, out bool help);
                next = next with { NeedsEntryHelp = help };
            }
            else
            {
                along = Ride(line, state.Direction, velocity, over, within, seconds, out bool braking);
                heavyLanding = braking;
                next = next with { AllowFallDamage = braking };
            }

            along = along with
            {
                Y = -Math.Max(Math.Abs(local.Y) * ZiplineParameters.CentringShare / seconds, 0) * Math.Sign(local.Y),
                Z = -Math.Max(Math.Abs(local.Z) * ZiplineParameters.CentringShare / seconds, 0) * Math.Sign(local.Z),
            };
            Vector3D target = line.World(Vectors.Add(local, Vectors.Scale(along, seconds)));
            result = Vectors.Divide(Vectors.Subtract(target, hands), seconds);
        }

        if (input.JumpPressed && !entering)
        {
            next = next with { Jumped = true };
        }

        bool finished = false;
        if (state.StartedDetaching)
        {
            result = velocity;
            finished = true;
        }
        else if (next.ShouldDetach || next.Jumped)
        {
            if (next.Jumped)
            {
                Vector3D push = Vectors.Scale(JumpDirection(input), ZiplineParameters.HorizontalJumpOffForce);
                double rise = Math.Max(ZiplineParameters.VerticalJumpOffForce - Math.Max(velocity.Z, 0), 0);
                result = new Vector3D(result.X + push.X, result.Y + push.Y, result.Z + push.Z + rise);
                exit = exit == ZiplineExit.None ? ZiplineExit.JumpOff : exit;
            }

            exit = exit == ZiplineExit.None ? ZiplineExit.Detached : exit;
            next = next with { StartedDetaching = true };
            finished = true;
        }

        if (entering && floorDistance < ZiplineParameters.EntryFloorDistance)
        {
            result = result with { Z = Math.Max(result.Z, 0) };
        }

        return new ZiplineStep(next, result, Advance(feet, result, seconds), yaw, ZiplineParameters.RidingFlags, exit, finished, heavyLanding, true);
    }

    // The first ticks: the rider's own velocity fades (an upward speed faster), and hands past a line end are pulled
    // back when they would not get onto the line by themselves.
    private static Vector3D Catch(ZiplineState next, Vector3D velocity, double over, bool within, double seconds, out bool help)
    {
        double rising = velocity.Z;
        Vector3D faded = InterpolateConstant(velocity, default, seconds, ZiplineParameters.EntryDeceleration);
        if (rising >= 0)
        {
            faded = faded with { Z = InterpolateConstant(faded.Z, 0, seconds, ZiplineParameters.EntryRiseDeceleration) };
        }

        Vector3D along = next.Line.LocalDirection(faded);
        help = next.NeedsEntryHelp;
        if (within)
        {
            return along;
        }

        if (next.Ticks == 1)
        {
            help = !(Math.Abs(along.X) * ZiplineParameters.EntryHelpSpeedShare > Math.Abs(over));
        }

        if (!help)
        {
            help = Math.Sign(along.X) == Math.Sign(over);
        }

        return help ? along with { X = PullBack(over, seconds) } : along;
    }

    // Riding: the speed along the line grows with the slope of the motion, up to the line's highest speed, and brakes
    // while the acceleration opposes it; hands past a line end are pulled back.
    private static Vector3D Ride(ZiplineLine line, ZiplineTravelDirection direction, Vector3D velocity, double over, bool within, double seconds, out bool braking)
    {
        Vector3D along = line.LocalDirection(velocity);
        double change = Math.Min(AccelerationFromPitch(velocity) * seconds, ZiplineParameters.MaximumSpeed - along.X);
        if (direction == ZiplineTravelDirection.Home)
        {
            change = -change;
        }

        braking = false;
        if (!within)
        {
            return along with { X = PullBack(over, seconds) };
        }

        double speed = along.X;
        if (!(Math.Abs(speed) > ZiplineParameters.BrakeMinimumSpeed && Math.Abs(change) > ZiplineParameters.BrakeMinimumChange
            && Math.Sign(speed) != Math.Sign(change)))
        {
            return along with { X = speed + change };
        }

        braking = true;
        speed = InterpolateConstant(speed, 0, seconds, ZiplineParameters.BrakeDeceleration);
        return along with { X = speed + change };
    }

    // A rider being caught lets go or falls too fast for the line: its vertical speed fades towards zero and the ride
    // ends once it is slow enough; the action layer is not written on such a tick.
    private static ZiplineStep FallThrough(ZiplineState next, ZiplineExit exit, Vector3D feet, Vector3D velocity, double seconds, double yaw)
    {
        float alpha = (float)seconds * ZiplineParameters.FallingFadeRate;
        double fading = velocity.Z + (alpha * (0 - velocity.Z));
        Vector3D result = velocity with { Z = Math.Min(fading, 0) };
        bool finished = result.Z > ZiplineParameters.FallingExitSpeed;
        return new ZiplineStep(next, result, Advance(feet, result, seconds), yaw, ZiplineParameters.RidingFlags, exit, finished, true, false);
    }

    // Exit points are not taken while the rider is being caught.
    private static bool Launches(bool entering, ZiplineLine line, Vector3D local, Vector3D feet, Vector3D centre, Vector3D velocity,
        ZiplineTravelDirection direction, out Vector3D launch, out ZiplineExit reason)
    {
        if (!entering)
        {
            return TryExitPoints(line, local, feet, centre, velocity, direction, out launch, out reason);
        }

        launch = default;
        reason = ZiplineExit.None;
        return false;
    }

    /// <summary>
    /// Exit points of the line and its far end (<c>TryExitPoints</c>): past an exit point's fraction in the riding
    /// direction the rider is launched; past the end it keeps its velocity, clamped to <see cref="ZiplineParameters.MaxEndLaunchVelocity"/>.
    /// </summary>
    /// <param name="line">The line.</param>
    /// <param name="local">The hands in the line frame.</param>
    /// <param name="feet">World position of the feet.</param>
    /// <param name="centre">World position of the capsule centre.</param>
    /// <param name="velocity">Velocity before the step.</param>
    /// <param name="direction">Riding direction.</param>
    /// <param name="launch">The velocity the rider leaves with.</param>
    /// <param name="reason">Which exit applies.</param>
    /// <returns>Whether the ride ends here.</returns>
    private static bool TryExitPoints(ZiplineLine line, Vector3D local, Vector3D feet, Vector3D centre, Vector3D velocity, ZiplineTravelDirection direction,
        out Vector3D launch, out ZiplineExit reason)
    {
        ArgumentNullException.ThrowIfNull(line);
        double fraction = local.X / line.Length;
        foreach (ZiplineExitPoint point in line.ExitPoints)
        {
            bool passed = direction == ZiplineTravelDirection.Home ? fraction < point.Fraction : fraction > point.Fraction;
            if (point.TravelDirection != direction || !passed)
            {
                continue;
            }

            reason = ZiplineExit.ExitPoint;
            launch = default;
            if (!(Math.Abs(point.LaunchSpeed) > 0))
            {
                return true;
            }

            Vector3D target = line.ActorPoint(point.LaunchTarget);
            launch = point.MoreExact ? ExactLaunch(feet, point.LaunchSpeed, target, -AirMovementParameters.FallAcceleration)
                : Vectors.Scale(Vectors.Normal(Vectors.Subtract(target, centre)), point.LaunchSpeed);
            if (point is { MoreExact: false, Additive: true })
            {
                launch = Vectors.Add(launch, velocity);
            }

            return true;
        }

        if ((fraction >= 1 && direction == ZiplineTravelDirection.Away) || (fraction <= 0 && direction == ZiplineTravelDirection.Home))
        {
            double size = Vectors.Length(velocity);
            launch = size > ZiplineParameters.MaxEndLaunchVelocity ? Vectors.Scale(velocity, ZiplineParameters.MaxEndLaunchVelocity / size) : velocity;
            reason = ZiplineExit.LineEnd;
            return true;
        }

        launch = default;
        reason = ZiplineExit.None;
        return false;
    }

    /// <summary>
    /// A ballistic launch from <paramref name="from"/> that lands on <paramref name="target"/> with the given horizontal
    /// speed (<c>CalculateMoreExactExitVelocity</c>).
    /// </summary>
    /// <param name="from">Launch point.</param>
    /// <param name="groundSpeed">Horizontal speed.</param>
    /// <param name="target">Landing point.</param>
    /// <param name="gravity">Vertical acceleration (negative downwards).</param>
    /// <returns>The launch velocity.</returns>
    private static Vector3D ExactLaunch(Vector3D from, double groundSpeed, Vector3D target, double gravity)
    {
        Vector3D offset = Vectors.Subtract(target, from);
        double horizontal = Math.Sqrt((offset.X * offset.X) + (offset.Y * offset.Y));
        double time = horizontal / groundSpeed;
        double vertical = (offset.Z - (gravity * time * time * 0.5)) / time;
        Vector3D flat = horizontal * horizontal > 1e-8 ? new Vector3D(offset.X / horizontal, offset.Y / horizontal, 0) : default;
        return new Vector3D(flat.X * groundSpeed, flat.Y * groundSpeed, vertical);
    }

    /// <summary>Acceleration along the line from the pitch of the motion: level or up gives the base rate, straight down the highest.</summary>
    /// <param name="velocity">World velocity.</param>
    /// <returns>The acceleration.</returns>
    public static double AccelerationFromPitch(Vector3D velocity)
    {
        Vector3D direction = Vectors.Normal(velocity);
        double pitch = Math.Atan2(direction.Z, Math.Sqrt((direction.X * direction.X) + (direction.Y * direction.Y))) * 180 / Math.PI;
        double share = Math.Clamp((pitch - ZiplineParameters.MaxAccelerationPitch) / (0 - ZiplineParameters.MaxAccelerationPitch), 0, 1);
        return ZiplineParameters.MaxAcceleration + (share * (ZiplineParameters.Acceleration - ZiplineParameters.MaxAcceleration));
    }

    private static double PullBack(double over, double seconds) =>
        -Math.Max(Math.Abs(over) * ZiplineParameters.CentringShare / seconds, 0) * Math.Sign(over);

    // The engine's constant-rate interpolation of a number.
    private static double InterpolateConstant(double current, double target, double seconds, double rate)
    {
        double distance = target - current;
        if (distance * distance < 1e-8)
        {
            return target;
        }

        double step = rate * seconds;
        return current + Math.Clamp(distance, -step, step);
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

    private static Vector3D JumpDirection(ZiplineInput input)
    {
        (double sin, double cos) = Math.SinCos(input.ControlYaw);
        (double forward, double right) = DodgeRollMovement.Coordinates(input.Direction);
        return new Vector3D((forward * cos) - (right * sin), (forward * sin) + (right * cos), 0);
    }

    private static Vector3D InverseRotate(Quaternion rotation, Vector3D vector)
    {
        // Rotation by the conjugate: v + 2w(u x v) + 2u x (u x v) with u = -(x, y, z).
        Vector3D u = new(-rotation.X, -rotation.Y, -rotation.Z);
        Vector3D t = Vectors.Scale(Vectors.Cross(u, vector), 2);
        return Vectors.Add(Vectors.Add(vector, Vectors.Scale(t, rotation.W)), Vectors.Cross(u, t));
    }

    private static Vector3D Advance(Vector3D feet, Vector3D velocity, double seconds) =>
        new(feet.X + (velocity.X * seconds), feet.Y + (velocity.Y * seconds), feet.Z + (velocity.Z * seconds));
}

file static class Vectors
{
    public static Vector3D Add(Vector3D a, Vector3D b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);

    public static Vector3D Subtract(Vector3D a, Vector3D b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);

    public static Vector3D Scale(Vector3D a, double factor) => new(a.X * factor, a.Y * factor, a.Z * factor);

    public static Vector3D Divide(Vector3D a, double divisor) => new(a.X / divisor, a.Y / divisor, a.Z / divisor);

    public static double Dot(Vector3D a, Vector3D b) => (a.X * b.X) + (a.Y * b.Y) + (a.Z * b.Z);

    public static Vector3D Cross(Vector3D a, Vector3D b) => new((a.Y * b.Z) - (a.Z * b.Y), (a.Z * b.X) - (a.X * b.Z), (a.X * b.Y) - (a.Y * b.X));

    public static double Length(Vector3D a) => Math.Sqrt(Dot(a, a));

    // The engine's safe normal: zero for (nearly) zero vectors.
    public static Vector3D Normal(Vector3D a)
    {
        double square = Dot(a, a);
        return square > 1e-8 ? Scale(a, 1 / Math.Sqrt(square)) : default;
    }
}
