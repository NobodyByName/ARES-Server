using System.Numerics;
using Ares.Common.Maps;
using Ares.Raid.World.Collision;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Game.Collision;
using Ares.Structs.Game.Movement;
using Ares.Structs.Game.World;

namespace Ares.Raid.World.Movement;

/// <summary>The ground under a character: a plane fitted to the probes around its axis.</summary>
/// <param name="Supported">Whether any probe found ground within reach.</param>
/// <param name="Walkable">Whether the plane is flat enough to stand on.</param>
/// <param name="Height">Height of the plane on the character's axis.</param>
/// <param name="Normal">Unit normal of the plane.</param>
/// <param name="Surface">Surface type of the ground the axis probe reaches, else of the first probe that reaches ground.</param>
public readonly record struct GroundContact(bool Supported, bool Walkable, double Height, Vector3 Normal, PhysicalSurfaceType Surface = PhysicalSurfaceType.Default)
{
    /// <summary>Whether the ground carries a character.</summary>
    public bool Carries => Supported && Walkable;

    /// <summary>Height of the highest ground any probe reached: the ground still under the pill at an edge.</summary>
    public double Highest { get; init; }
}

/// <summary>A character's body in the level: where its feet are, how it moves and what it stands on.</summary>
/// <param name="Position">World position of the feet.</param>
/// <param name="Velocity">Velocity in world units per second.</param>
/// <param name="Ground">The ground under the position.</param>
public readonly record struct BodyState(Vector3D Position, Vector3D Velocity, GroundContact Ground)
{
    /// <summary>Whether the character is off the ground.</summary>
    public bool Airborne { get; init; }

    /// <summary>Whether the character left the ground by jumping and has not landed.</summary>
    public bool Jumping { get; init; }

    /// <summary>Input frame of a jump press that waits for its take-off.</summary>
    public uint? JumpRequestedAt { get; init; }

    /// <summary>Whether the feet are still settling after a landing.</summary>
    public bool Landing { get; init; }

    /// <summary>Ticks the landing has settled for.</summary>
    public int LandingTicks { get; init; }

    /// <summary>
    /// Height of the feet above the ground as the last ground trace measured it, negative when it found them under the
    /// ground. A body that stands still is not traced and keeps its last measure.
    /// </summary>
    public double GroundDistance { get; init; }

    /// <summary>
    /// Whether the last ground trace was a high one. Traces alternate, and every other one reads the ground one step
    /// of its height's precision higher.
    /// </summary>
    public bool HighTrace { get; init; }

    /// <summary>Whether a body in the air has reached ground that carries it: its next tick is on the ground.</summary>
    public bool Touching => Airborne && Ground.Carries
        && (Jumping ? Velocity.Z <= 0 && Position.Z <= Ground.Height : Position.Z - Ground.Height <= GroundContactParameters.StandingDistance);
}

/// <summary>
/// Moves a character's body through the level's collision: jumping, falling and landing, walking on the ground
/// plane under it and sliding along what blocks it. What drives the horizontal velocity (walking, a slide, a roll)
/// is the caller's: <see cref="Begin"/> settles whether the tick is on the ground or in the air, the caller then
/// chooses the horizontal velocity for it and <see cref="Move"/> carries it out.
/// </summary>
public static class BodyMotion
{
    private const PlacementFlags Blocking = PlacementFlags.BlocksPawn;

    // The blocking surface is approached to this distance, and a slide follows at most this many surfaces per tick.
    private const double Skin = 0.05;
    private const int SlidePasses = 3;

    // Probe directions around the character's axis, with the axis itself.
    private static readonly (double X, double Y)[] s_probes =
    [
        (0, 0), (1, 0), (Math.Sqrt(0.5), Math.Sqrt(0.5)), (0, 1), (-Math.Sqrt(0.5), Math.Sqrt(0.5)),
        (-1, 0), (-Math.Sqrt(0.5), -Math.Sqrt(0.5)), (0, -1), (Math.Sqrt(0.5), -Math.Sqrt(0.5)),
    ];

    // The part of the character that walls block: a pill from knee height to the top of its capsule.
    private static readonly double s_pillHalfHeight = ((DamPlayerStarts.Capsule.HalfHeight * 2) - GroundContactParameters.KneeHeight) / 2;
    private static readonly double s_pillCenter = GroundContactParameters.KneeHeight + s_pillHalfHeight;

    /// <summary>A body at rest or in motion at a position, on the ground when the ground there carries it.</summary>
    /// <param name="world">The level's collision.</param>
    /// <param name="position">World position of the feet.</param>
    /// <param name="velocity">Velocity in world units per second.</param>
    /// <returns>The body.</returns>
    public static BodyState Start(CollisionWorld world, Vector3D position, Vector3D velocity = default)
    {
        ArgumentNullException.ThrowIfNull(world);
        GroundContact ground = Probe(world, position);
        bool airborne = !ground.Carries || position.Z - ground.Height > GroundContactParameters.StandingDistance;
        return new BodyState(position, velocity, ground)
        {
            Airborne = airborne,
            Jumping = airborne && velocity.Z > 0,
            HighTrace = true,
            GroundDistance = ground.Supported ? Distance(position.Z, ground, high: true) : 0,
        };
    }

    /// <summary>
    /// Settles how the body spends a tick: it leaves the ground when the ground no longer carries it or a jump
    /// takes off, and lands when it reaches ground that does. A jump pressed on the ground takes off
    /// <see cref="AirMovementParameters.JumpDelayTicks"/> ticks later, or at once when the ground is lost first.
    /// </summary>
    /// <param name="body">The body after the previous tick.</param>
    /// <param name="frame">Input frame of the tick.</param>
    /// <param name="jumpPressed">Whether the tick's input presses jump.</param>
    /// <returns>The body as the tick starts; its horizontal velocity is still the previous tick's.</returns>
    public static BodyState Begin(BodyState body, uint frame, bool jumpPressed)
    {
        bool airborne = body.Airborne;
        bool jumping = body.Jumping;
        bool landing = body.Landing;
        double vertical = body.Velocity.Z;
        int landingTicks = body.LandingTicks;
        if (!airborne)
        {
            // Once no probe under the pill finds ground within a step down, the character falls.
            airborne = !body.Ground.Carries || body.Position.Z - body.Ground.Highest > GroundContactParameters.StepDownReach;
            jumping = false;
        }
        else if (body.Touching)
        {
            vertical *= jumping ? GroundContactParameters.JumpLandingSpeedShare : 1;
            (airborne, jumping, landing, landingTicks) = (false, false, true, 0);
        }

        uint? requested = body.JumpRequestedAt;
        switch (requested)
        {
            case { } pressedAt when airborne || frame >= pressedAt + (uint)AirMovementParameters.JumpDelayTicks:
                vertical = AirMovementParameters.JumpSpeed;
                (airborne, jumping, landing, requested) = (true, true, false, null);
                break;
            case null when jumpPressed && !airborne:
                requested = frame;
                break;
        }

        return body with
        {
            Velocity = body.Velocity with { Z = vertical },
            Airborne = airborne,
            Jumping = jumping,
            Landing = landing,
            LandingTicks = landingTicks,
            JumpRequestedAt = requested,
        };
    }

    /// <summary>
    /// Moves the body for a tick with a horizontal velocity. In the air it falls; on the ground its feet settle on the
    /// ground plane while it moves and keep their height while it stands. What blocks the way turns the motion along itself.
    /// </summary>
    /// <param name="world">The level's collision.</param>
    /// <param name="body">The body from <see cref="Begin"/>.</param>
    /// <param name="horizontal">Horizontal velocity for the tick, in world units per second.</param>
    /// <param name="seconds">Duration of the tick.</param>
    /// <returns>The body after the tick.</returns>
    public static BodyState Move(CollisionWorld world, BodyState body, Vector3D horizontal, double seconds)
    {
        ArgumentNullException.ThrowIfNull(world);
        if (!double.IsFinite(seconds) || seconds <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(seconds));
        }

        double vertical = body.Airborne ? body.Velocity.Z - (AirMovementParameters.FallAcceleration * seconds) : body.Velocity.Z;
        Vector3D motion = new(horizontal.X * seconds, horizontal.Y * seconds, body.Airborne ? vertical * seconds : 0);
        (Vector3D position, Vector3D moved, bool blocked) = Slide(world, body.Position, motion, body.Airborne);
        Vector3D velocity = blocked
            ? new Vector3D(moved.X / seconds, moved.Y / seconds, body.Airborne ? moved.Z / seconds : vertical)
            : horizontal with { Z = vertical };
        GroundContact ground = Probe(world, position);
        bool landing = body.Landing;
        int landingTicks = body.LandingTicks;
        bool high = body.HighTrace;
        double distance = body.GroundDistance;
        switch (body.Airborne)
        {
            case true when ground is { Supported: true, Walkable: false } && position.Z < ground.Height:
                // Ground too steep to stand on carries nothing but still stops the fall: the body slides down it.
                position = position with { Z = ground.Height };
                double into = Math.Min(0, (velocity.X * ground.Normal.X) + (velocity.Y * ground.Normal.Y) + (velocity.Z * ground.Normal.Z));
                velocity = new Vector3D(velocity.X - (into * ground.Normal.X), velocity.Y - (into * ground.Normal.Y), velocity.Z - (into * ground.Normal.Z));
                break;
            case false when ground.Carries && landing:
                // A critically damped spring between the feet and the ground plane.
                const double Frequency = GroundContactParameters.LandingFrequency;
                double offset = position.Z - ground.Height;
                double pull = (velocity.Z + (Frequency * offset)) * seconds;
                double decay = Math.Exp(-Frequency * seconds);
                offset = (offset + pull) * decay;
                velocity = velocity with { Z = (velocity.Z - (Frequency * pull)) * decay };
                position = position with { Z = ground.Height + offset };
                // Settled once at rest against the ground, which rises or falls under a moving body. A moving body hands over
                // to the walking settle after the settle time: on a slope the spring trails the ground it follows.
                landingTicks++;
                bool settled = Math.Abs(offset) <= GroundContactParameters.SettledDistance
                    && Math.Abs(velocity.Z - Along(ground, velocity)) <= GroundContactParameters.SettledSpeed;
                bool walking = velocity.X != 0 || velocity.Y != 0;
                landing = !settled && (!walking || landingTicks * seconds < GroundContactParameters.LandingSettleSeconds);
                break;
            case false when ground.Carries && position.Z - ground.Highest <= GroundContactParameters.StepDownReach:
                // A body that has stopped keeps its height; a moving one settles on the ground under its new position.
                // The ground is traced on a tick that starts or ends in motion, and each tick that starts in motion takes
                // the other kind of trace.
                bool wasMoving = body.Velocity.X != 0 || body.Velocity.Y != 0;
                bool moving = velocity.X != 0 || velocity.Y != 0 || Math.Abs(velocity.Z) > GroundMovementParameters.BrakingStopSpeed;
                high = wasMoving ? !high : high;
                distance = wasMoving || moving ? Distance(position.Z, ground, high) : distance;
                double rise = moving ? Settle(Along(body.Ground, velocity), distance, seconds) : 0;
                velocity = velocity with { Z = rise };
                position = position with { Z = position.Z + (rise * seconds) };
                break;
            case false:
                position = position with { Z = position.Z + (velocity.Z * seconds) };
                break;
        }

        return body with
        {
            Position = position,
            Velocity = velocity,
            Ground = ground,
            Landing = landing,
            LandingTicks = landingTicks,
            HighTrace = high,
            GroundDistance = distance,
        };
    }

    // Height of the feet above the ground as a trace measures it.
    private static double Distance(double feet, GroundContact ground, bool high) =>
        feet - (high ? MathF.BitIncrement((float)ground.Height) : ground.Height);

    // Vertical speed of a velocity that runs along the ground the body stood on, in whole units like the walking velocity.
    private static double Along(GroundContact ground, Vector3D velocity) =>
        ground.Carries && (velocity.X != 0 || velocity.Y != 0)
            ? Math.Floor((-((ground.Normal.X * velocity.X) + (ground.Normal.Y * velocity.Y)) / ground.Normal.Z) + 0.5)
            : 0;

    // Vertical speed after a tick on the smoothest path (the one whose acceleration changes least) that takes feet at a
    // height above the ground, moving at a vertical speed, to rest on the ground within the settle time.
    private static double Settle(double speed, double height, double seconds)
    {
        double share = seconds / GroundContactParameters.SettleSeconds;
        if (share >= 1)
        {
            return -height / seconds;
        }

        double rest = (1 - share) * (1 - share);
        double kept = rest * (1 + (5 * share)) * (1 - (3 * share));
        double pull = 30 * share * share * rest / GroundContactParameters.SettleSeconds;
        return (speed * kept) - (height * pull);
    }

    /// <summary>Whether a character has room to stand at a position: nothing blocks its standing capsule above knee height.</summary>
    /// <param name="world">The level's collision.</param>
    /// <param name="feet">World position of the feet.</param>
    /// <returns>True when the character can stand up there.</returns>
    public static bool CanStand(CollisionWorld world, Vector3D feet)
    {
        ArgumentNullException.ThrowIfNull(world);
        Vector3D center = feet with { Z = feet.Z + s_pillCenter };
        return !world.OverlapCapsule(center, System.Numerics.Quaternion.Identity, GroundContactParameters.PillRadius, s_pillHalfHeight, Blocking);
    }

    /// <summary>The ground under a position: probes from knee height down to the ground reach, fitted to a plane.</summary>
    /// <param name="world">The level's collision.</param>
    /// <param name="feet">World position of the feet.</param>
    /// <returns>The ground, unsupported when no probe reaches any.</returns>
    public static GroundContact Probe(CollisionWorld world, Vector3D feet)
    {
        ArgumentNullException.ThrowIfNull(world);
        Span<Double3> hits = stackalloc Double3[s_probes.Length];
        int count = 0;
        double? axis = null;
        double highest = double.MinValue;
        PhysicalSurfaceType surface = PhysicalSurfaceType.Default;
        foreach ((double x, double y) in s_probes)
        {
            Vector3D origin = new(feet.X + (x * GroundContactParameters.PillRadius), feet.Y + (y * GroundContactParameters.PillRadius),
                feet.Z + GroundContactParameters.KneeHeight);
            // A probe that starts inside something found an obstacle, not ground.
            if (world.Raycast(origin, -Vector3.UnitZ, GroundContactParameters.KneeHeight + GroundContactParameters.GroundReach, Blocking) is not { Distance: > 0 } hit)
            {
                continue;
            }

            surface = count == 0 || (x == 0 && y == 0) ? hit.Surface : surface;
            hits[count++] = new Double3(hit.Point.X - feet.X, hit.Point.Y - feet.Y, hit.Point.Z);
            axis = x == 0 && y == 0 ? hit.Point.Z : axis;
            highest = Math.Max(highest, hit.Point.Z);
        }

        if (count == 0)
        {
            return default;
        }

        // The plane carries the character over dips and past edges; ground rising above it on the axis lifts the feet.
        // Ground heights have single precision.
        (double slopeX, double slopeY, double height) = Plane(hits[..count]);
        height = (float)Math.Max(height, axis ?? height);
        double length = Math.Sqrt((slopeX * slopeX) + (slopeY * slopeY) + 1);
        Vector3 normal = new((float)(-slopeX / length), (float)(-slopeY / length), (float)(1 / length));
        return new GroundContact(true, normal.Z >= GroundContactParameters.WalkableNormalZ, height, normal, surface) { Highest = highest };
    }

    // Least-squares plane z = slopeX x + slopeY y + height through points relative to the axis; level through their
    // mean height when they do not span a plane.
    private static (double SlopeX, double SlopeY, double Height) Plane(ReadOnlySpan<Double3> points)
    {
        double meanX = 0;
        double meanY = 0;
        double meanZ = 0;
        foreach (Double3 point in points)
        {
            meanX += point.X;
            meanY += point.Y;
            meanZ += point.Z;
        }

        meanX /= points.Length;
        meanY /= points.Length;
        meanZ /= points.Length;
        double xx = 0;
        double xy = 0;
        double yy = 0;
        double xz = 0;
        double yz = 0;
        foreach (Double3 point in points)
        {
            double x = point.X - meanX;
            double y = point.Y - meanY;
            double z = point.Z - meanZ;
            xx += x * x;
            xy += x * y;
            yy += y * y;
            xz += x * z;
            yz += y * z;
        }

        double determinant = (xx * yy) - (xy * xy);
        if (points.Length < 3 || determinant < 1)
        {
            return (0, 0, meanZ);
        }

        double slopeX = ((xz * yy) - (yz * xy)) / determinant;
        double slopeY = ((yz * xx) - (xz * xy)) / determinant;
        return (slopeX, slopeY, meanZ - (slopeX * meanX) - (slopeY * meanY));
    }

    // Moves the feet by a motion, turning it along each surface that blocks the character's capsule above knee height.
    // On the ground only the horizontal part of a surface turns the motion; the feet's height is the ground's. The
    // distance moved leaves out what pushing the capsule out of a surface added, so it is never longer than the motion.
    private static (Vector3D Position, Vector3D Moved, bool Blocked) Slide(CollisionWorld world, Vector3D feet, Vector3D motion, bool airborne)
    {
        Double3 position = Double3.From(feet);
        Double3 remaining = Double3.From(motion);
        Double3 pushed = default;
        bool blocked = false;
        for (int pass = 0; pass < SlidePasses && remaining.LengthSquared() > Skin * Skin; pass++)
        {
            Double3 center = position + new Double3(0, 0, s_pillCenter);
            if (world.SweepCapsule(center.ToVector3D(), (center + remaining).ToVector3D(), GroundContactParameters.PillRadius, s_pillHalfHeight, Blocking) is not { } hit)
            {
                position += remaining;
                break;
            }

            blocked = true;
            Double3 normal = Double3.From(hit.Normal);
            if (!airborne)
            {
                normal = normal with { Z = 0 };
                if (normal.LengthSquared() < 1e-6)
                {
                    // A surface over or under the capsule does not turn a walk.
                    position += remaining;
                    break;
                }

                normal = normal.Normalized();
            }

            if (hit.StartPenetrating)
            {
                Double3 push = normal * (hit.PenetrationDepth + Skin);
                position += push;
                pushed += push;
                continue;
            }

            double length = remaining.Length();
            double travelled = Math.Max(0, hit.Distance - Skin);
            position += remaining * (travelled / length);
            remaining *= 1 - (travelled / length);
            remaining -= normal * Math.Min(0, Double3.Dot(remaining, normal));
        }

        Vector3D end = position.ToVector3D();
        return (end, new Vector3D(end.X - feet.X - pushed.X, end.Y - feet.Y - pushed.Y, end.Z - feet.Z - pushed.Z), blocked);
    }
}
