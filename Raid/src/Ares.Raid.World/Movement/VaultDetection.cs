using System.Numerics;
using Ares.Common.Maps;
using Ares.Raid.World.Collision;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Game.Movement;
using Quaternion = System.Numerics.Quaternion;

namespace Ares.Raid.World.Movement;

/// <summary>The character's capsule as the vault traces use it.</summary>
/// <param name="Radius">Capsule radius.</param>
/// <param name="HalfHeight">Current half height, caps included.</param>
/// <param name="StandingHalfHeight">Half height when standing.</param>
/// <param name="CrouchedHalfHeight">Half height when crouched.</param>
public readonly record struct VaultCapsule(double Radius, double HalfHeight, double StandingHalfHeight, double CrouchedHalfHeight);

/// <summary>What the vault search knows about the character in the tick it runs.</summary>
/// <param name="Center">World position of the capsule centre.</param>
/// <param name="Facing">Character heading in radians.</param>
/// <param name="ControlYaw">Control heading in radians.</param>
/// <param name="ControlPitch">Control pitch in radians (positive looks up).</param>
/// <param name="Input">Eight-way direction of the movement input.</param>
/// <param name="Velocity">Velocity before the tick's move.</param>
/// <param name="GroundNormal">Unit normal of the ground under the character.</param>
/// <param name="OnGround">Whether the ground under the character is walkable.</param>
/// <param name="Falling">Whether the character is in the air (rough movement mode air).</param>
/// <param name="Capsule">The character's capsule.</param>
public readonly record struct VaultQuery(
    Vector3D Center,
    double Facing,
    double ControlYaw,
    double ControlPitch,
    InputDirection Input,
    Vector3D Velocity,
    Vector3 GroundNormal,
    bool OnGround,
    bool Falling,
    VaultCapsule Capsule);

/// <summary>Why a vault search found nothing to vault.</summary>
public enum VaultMiss : byte
{
    /// <summary>A vault was found.</summary>
    None = 0,

    /// <summary>The movement input points away from the view.</summary>
    InputAway,

    /// <summary>The search along the movement direction hit nothing.</summary>
    NoWall,

    /// <summary>The search found only floor up to the highest obstacle.</summary>
    OnlyFloor,

    /// <summary>The edge scan found no top edge on the wall.</summary>
    NoEdge,

    /// <summary>The edge is above the highest obstacle.</summary>
    TooHigh,

    /// <summary>The last edge candidate was below the lowest obstacle in the air.</summary>
    TooLow,

    /// <summary>The last edge candidate had no room to cross over the edge.</summary>
    NoPassage,

    /// <summary>The last edge candidate had no clear way up from the character.</summary>
    NoClimb,

    /// <summary>The last edge candidate was a low obstacle too far away.</summary>
    TooFar,
}

/// <summary>An obstacle the vault search found and how to cross it.</summary>
/// <param name="Kind">Whether the vault goes over the obstacle or onto it.</param>
/// <param name="CanStand">Whether the character can stand up where a vault onto ends.</param>
/// <param name="Edge">The obstacle's top edge where the vault crosses it; the vault animation is placed there.</param>
/// <param name="WallNormal">Horizontal unit normal of the obstacle's face, towards the character.</param>
/// <param name="Landing">Where the vault aims to put the feet.</param>
/// <param name="Height">Height of the edge above the feet.</param>
/// <param name="HorizontalDistance">Horizontal distance from the capsule centre to the edge.</param>
/// <param name="Distance">Distance from the capsule centre to the edge.</param>
/// <param name="LedgeHeight">Drop from the edge height to the ground under the character.</param>
/// <param name="SearchStart">Capsule centre the search started from.</param>
/// <param name="Wall">The surface the search found the obstacle on.</param>
public sealed record VaultTrace(
    VaultKind Kind,
    bool CanStand,
    Vector3D Edge,
    Vector3 WallNormal,
    Vector3D Landing,
    double Height,
    double HorizontalDistance,
    double Distance,
    double LedgeHeight,
    Vector3D SearchStart,
    CollisionHit Wall);

/// <summary>
/// Vault detection: when a tick looks for a vault, the traces that find an obstacle and its edge in the level's
/// collision (what blocks climbing), and the clip chosen for it.
/// </summary>
public static class VaultDetection
{
    private const PlacementFlags Climbable = PlacementFlags.BlocksClimbable;
    private static readonly double s_floorNormalZ = Math.Cos(VaultParameters.FloorAngle * Math.PI / 180);

    /// <summary>Whether the client's input asks for a vault this tick.</summary>
    /// <param name="claims">Character claims of the tick.</param>
    /// <returns>True when the request bit is set.</returns>
    public static bool Requested(CharacterInput claims) => (claims.Flags & (1UL << VaultParameters.RequestBit)) != 0;

    /// <summary>
    /// Whether ground movement looks for a vault this tick without a request: a new jump press with movement input,
    /// or, high above the floor and not just after a take-off, a jump that is held or was pressed recently.
    /// </summary>
    /// <param name="input">Movement axes and buttons of the tick.</param>
    /// <param name="claims">Character claims of the tick.</param>
    /// <param name="frame">Input frame of the tick.</param>
    /// <param name="jumpPressedAt">Input frame of the last jump press, if any.</param>
    /// <param name="jumpTakeOffAt">Input frame of the last jump take-off, if any.</param>
    /// <param name="floorDistance">Height of the feet above the floor.</param>
    /// <returns>True when the vault search runs.</returns>
    public static bool ShouldAttempt(MovementInput input, CharacterInput claims, uint frame, uint? jumpPressedAt, uint? jumpTakeOffAt, double floorDistance)
    {
        bool immobilized = (claims.Flags & (1UL << VaultParameters.ImmobilizedBit)) != 0;
        if (input.JumpPressed && !immobilized && DodgeRollMovement.Direction(input) != InputDirection.None)
        {
            return true;
        }

        bool recentTakeOff = jumpTakeOffAt is { } tookOff && Seconds(frame, tookOff) <= VaultParameters.JumpWindowSeconds;
        bool recentPress = jumpPressedAt is { } pressed && Seconds(frame, pressed) <= VaultParameters.JumpWindowSeconds;
        return floorDistance > VaultParameters.AirAttemptFloorDistance && !recentTakeOff
            && (input.Buttons.HasFlag(MovementButtons.Jump) || recentPress);
    }

    /// <summary>Whether a vault search may run at this vertical speed.</summary>
    /// <param name="velocity">Velocity before the tick's move.</param>
    /// <returns>False while falling faster than <see cref="VaultParameters.MaximumFallSpeed"/>.</returns>
    public static bool FallSlowEnough(Vector3D velocity) => velocity.Z >= -VaultParameters.MaximumFallSpeed;

    /// <summary>Lowest obstacle the search accepts at a horizontal speed.</summary>
    /// <param name="horizontalSpeed">Horizontal speed.</param>
    /// <returns>Height in world units.</returns>
    public static double MinimumHeight(double horizontalSpeed)
    {
        double speed = RoundToInt(horizontalSpeed);
        double share = Math.Clamp(speed / VaultParameters.MinimumHeightTopSpeed, 0, 1);
        return RoundToInt(VaultParameters.MinimumHeightAtRest + (share * (VaultParameters.MinimumHeightAtSpeed - VaultParameters.MinimumHeightAtRest)));
    }

    /// <summary>Highest obstacle the search accepts.</summary>
    /// <param name="falling">Whether the character is in the air.</param>
    /// <param name="verticalSpeed">Vertical speed (negative is down).</param>
    /// <returns>Height in world units.</returns>
    public static double MaximumHeight(bool falling, double verticalSpeed) =>
        !falling ? VaultParameters.GroundMaximumHeight
        : verticalSpeed < -VaultParameters.FallCatchSpeed ? VaultParameters.FallingMaximumHeight
        : VaultParameters.AirMaximumHeight;

    /// <summary>Whether a found vault becomes a ledge catch instead: falling fast.</summary>
    /// <param name="falling">Whether the character is in the air.</param>
    /// <param name="velocity">Velocity before the tick's move.</param>
    /// <returns>True for a ledge catch.</returns>
    public static bool FallCatch(bool falling, Vector3D velocity) => falling && velocity.Z < -VaultParameters.FallCatchSpeed;

    /// <summary>The capsule centre the vault traces start from: rounded to tenths, then to whole units.</summary>
    /// <param name="center">Capsule centre.</param>
    /// <returns>The rounded centre.</returns>
    private static Vector3D SearchCenter(Vector3D center) => new(Precise(center.X), Precise(center.Y), Precise(center.Z));

    /// <summary>
    /// Looks for an obstacle to vault: a capsule search along the movement input (or the view), an edge scan up the
    /// wall it finds, then checks that the far side, the way up and the landing are clear.
    /// </summary>
    /// <param name="world">The level's collision.</param>
    /// <param name="query">The character.</param>
    /// <returns>The obstacle, or null when there is nothing to vault.</returns>
    public static VaultTrace? Trace(CollisionWorld world, in VaultQuery query) => Trace(world, query, out _);

    /// <inheritdoc cref="Trace(CollisionWorld, in VaultQuery)"/>
    /// <param name="world">The level's collision.</param>
    /// <param name="query">The character.</param>
    /// <param name="miss">Why nothing was found, or <see cref="VaultMiss.None"/>.</param>
    public static VaultTrace? Trace(CollisionWorld world, in VaultQuery query, out VaultMiss miss)
    {
        ArgumentNullException.ThrowIfNull(world);
        double horizontalSpeed = Math.Sqrt((query.Velocity.X * query.Velocity.X) + (query.Velocity.Y * query.Velocity.Y));
        return Trace(world, query, MinimumHeight(horizontalSpeed), MaximumHeight(query.Falling, query.Velocity.Z), out miss);
    }

    /// <summary>The clip for a found obstacle, or null when the definitions have none for it.</summary>
    /// <param name="definitions">The character's definition set.</param>
    /// <param name="trace">The obstacle.</param>
    /// <param name="falling">Whether the character is in the air.</param>
    /// <param name="velocity">Velocity before the tick's move.</param>
    /// <returns>The clip.</returns>
    public static VaultClip? Select(VaultDefinitionSet definitions, VaultTrace trace, bool falling, Vector3D velocity)
    {
        ArgumentNullException.ThrowIfNull(definitions);
        ArgumentNullException.ThrowIfNull(trace);
        bool fallCatch = FallCatch(falling, velocity);
        IReadOnlyList<VaultHeightDefinition> rows = fallCatch && definitions.FallCatch.Count > 0 ? definitions.FallCatch
            : falling && definitions.InAir.Count > 0 ? definitions.InAir
            : definitions.Ground;
        double height = fallCatch ? trace.LedgeHeight : trace.Height;
        VaultHeightDefinition? row = null;
        double best = double.MaxValue;
        foreach (VaultHeightDefinition candidate in rows)
        {
            double difference = Math.Abs(candidate.Height - height);
            if (candidate.Height <= height && difference < best)
            {
                (row, best) = (candidate, difference);
            }
        }

        double speed = Math.Round(Math.Sqrt((velocity.X * velocity.X) + (velocity.Y * velocity.Y)), MidpointRounding.AwayFromZero);
        VaultSpeedDefinition? match = null;
        best = double.MaxValue;
        foreach (VaultSpeedDefinition candidate in row?.Speeds ?? [])
        {
            double difference = Math.Abs(candidate.Speed - speed);
            if (difference < best)
            {
                (match, best) = (candidate, difference);
            }
        }

        return match is null ? null : trace.Kind == VaultKind.Over ? match.Over : match.Onto;
    }

    private static VaultTrace? Trace(CollisionWorld world, in VaultQuery query, double minimum, double maximum, out VaultMiss miss)
    {
        (double radius, double halfHeight, _, _) = query.Capsule;
        Double3 root = Double3.From(SearchCenter(query.Center));
        Double3 location = Double3.From(query.Center);
        Double3 direction = SearchDirection(query);
        if (direction == default)
        {
            miss = VaultMiss.InputAway;
            return null;
        }

        Double3 search = SlopeDirection(query, direction);
        double shortest = radius + VaultParameters.SearchMargin;
        double speed = Double3.From(query.Velocity).Length();
        double reach = query.Falling
            ? Math.Clamp(speed * VaultParameters.AirSearchSpeedShare, shortest, VaultParameters.AirSearchDistance)
            : Math.Clamp(speed, shortest, VaultParameters.GroundSearchDistance);
        Double3 lift = new(0, 0, VaultParameters.SearchLift);
        Double3 start = root + lift - (search * (radius * VaultParameters.SearchSetBack));
        Double3 end = start + (search * reach) - lift;
        double searchRadius = radius * VaultParameters.SearchRadiusShare;
        if (Sweep(world, start, end, searchRadius, halfHeight) is not { } wall)
        {
            miss = VaultMiss.NoWall;
            return null;
        }

        if (IsFloor(wall.Normal) || Behind(wall.Point, root, direction))
        {
            double step = maximum / VaultParameters.FloorSearchDivisions;
            for (double height = minimum; height < maximum;)
            {
                start += new Double3(0, 0, step);
                end += new Double3(0, 0, step);
                if (Sweep(world, start, end, searchRadius, halfHeight) is not { } raised)
                {
                    miss = VaultMiss.NoWall;
                    return null;
                }

                wall = raised;
                if (!IsFloor(wall.Normal))
                {
                    break;
                }

                height += step;
            }

            if (IsFloor(wall.Normal))
            {
                miss = VaultMiss.OnlyFloor;
                return null;
            }
        }

        Double3 normal = Flat(Double3.From(wall.Normal));
        return Edge(world, query, root, location, wall, normal, minimum, maximum, out miss);
    }

    // Scans up the wall with a small capsule leaning into it: hits on its upper end are the wall's face, the first
    // other hit is the top edge.
    private static VaultTrace? Edge(CollisionWorld world, in VaultQuery query, Double3 root, Double3 location, CollisionHit wall, Double3 normal,
        double minimum, double maximum, out VaultMiss miss)
    {
        miss = VaultMiss.NoEdge;
        VaultCapsule capsule = query.Capsule;
        double radius = capsule.Radius;
        double feet = root.Z - capsule.HalfHeight;
        double step = maximum / VaultParameters.EdgeScanDivisions;
        const double Pitch = -VaultParameters.EdgeProbePitch * Math.PI / 180;
        double probeHalfHeight = ((step * 0.5) + VaultParameters.EdgeProbeMargin) / Math.Cos(Pitch);
        Double3 into = -normal;
        Double3 axis = new(into.X * Math.Sin(Pitch), into.Y * Math.Sin(Pitch), Math.Cos(Pitch));
        Quaternion lean = Quaternion.CreateFromAxisAngle(Vector3.Normalize(Double3.Cross(new Double3(0, 0, 1), axis).ToVector3()), (float)Pitch);
        Double3 scan = Double3.From(wall.Point) + (normal * (radius * VaultParameters.EdgeScanStandOff));
        Double3 travel = into * (radius * VaultParameters.EdgeScanReach);
        int misses = 0;
        for (int probe = 0; probe < VaultParameters.EdgeScanSteps; probe++)
        {
            Double3 from = scan;
            scan += new Double3(0, 0, step);
            CollisionHit? found = world.SweepCapsule(from.ToVector3D(), (from + travel).ToVector3D(), lean, VaultParameters.EdgeProbeRadius, probeHalfHeight, Climbable);
            if (found is not { } hit)
            {
                if (++misses == VaultParameters.EdgeScanMisses)
                {
                    return null;
                }

                continue;
            }

            misses = 0;
            Double3 center = from + (travel.Normalized() * hit.Distance);
            Double3 probeTop = center + (axis * (probeHalfHeight - VaultParameters.EdgeProbeRadius));
            if ((Double3.From(hit.Point) - probeTop).Length() < VaultParameters.EdgeProbeRadius + VaultParameters.ProbeTopTolerance)
            {
                continue;
            }

            Double3 edge = Double3.From(hit.Point);
            Double3 toward = Flat(location - edge);
            VaultKind kind = FarSide(world, edge, toward, capsule);
            // Over: aim beyond the edge. Onto: land on the top found by a sphere dropped beyond the edge (nothing
            // found leaves the origin and no height, as in the game).
            Double3 landing = edge - (normal * (2 * radius));
            double height = edge.Z - feet;
            if (kind == VaultKind.Onto)
            {
                Double3 above = edge - (normal * (radius + VaultParameters.LandingMargin)) + new Double3(0, 0, capsule.CrouchedHalfHeight + VaultParameters.FloorMargin);
                CollisionHit? ground = world.SweepSphere(above.ToVector3D(), (above - new Double3(0, 0, capsule.HalfHeight)).ToVector3D(),
                    radius * VaultParameters.ProbeRadiusShare, Climbable);
                (landing, height) = ground is { } top ? (Double3.From(top.Point), RoundToInt(top.Point.Z - feet)) : (default, 0);
            }

            if (query.Falling && height < minimum)
            {
                miss = VaultMiss.TooLow;
                continue;
            }

            // The game measures the edge against a last grounded position it leaves at the world origin.
            if (edge.Z > maximum || height > maximum)
            {
                miss = VaultMiss.TooHigh;
                return null;
            }

            if (!CanMoveOverEdge(world, edge, landing, normal, capsule))
            {
                miss = VaultMiss.NoPassage;
                continue;
            }

            if (!CanClimbUpToEdge(world, edge, normal, capsule, root))
            {
                miss = VaultMiss.NoClimb;
                continue;
            }

            bool canStand = kind != VaultKind.Onto || CanStand(world, landing, capsule);
            Double3 offset = edge - root;
            double horizontal = Math.Sqrt((offset.X * offset.X) + (offset.Y * offset.Y));
            if (height is >= 0 and < VaultParameters.LowObstacleHeight && horizontal > VaultParameters.LowObstacleDistance)
            {
                miss = VaultMiss.TooFar;
                continue;
            }

            CollisionHit? below = world.Raycast(new Vector3D(root.X, root.Y, edge.Z), -Vector3.UnitZ, VaultParameters.LedgeProbeDepth, Climbable);
            double ledge = below is { } floor ? (float)floor.Distance : VaultParameters.LedgeProbeDepth;
            miss = VaultMiss.None;
            return new VaultTrace(kind, canStand, edge.ToVector3D(), normal.ToVector3(), landing.ToVector3D(), edge.Z - root.Z + capsule.HalfHeight,
                horizontal, offset.Length(), ledge, root.ToVector3D(), wall);
        }

        return null;
    }

    // The movement input turned by the character's heading, unless it points away from the view; the view without input.
    private static Double3 SearchDirection(in VaultQuery query)
    {
        Double3 view = new(Math.Cos(query.ControlPitch) * Math.Cos(query.ControlYaw), Math.Cos(query.ControlPitch) * Math.Sin(query.ControlYaw), Math.Sin(query.ControlPitch));
        if (query.Input == InputDirection.None)
        {
            return view;
        }

        (double forward, double right) = DodgeRollMovement.Coordinates(query.Input);
        Double3 input = new Double3((forward * Math.Cos(query.Facing)) - (right * Math.Sin(query.Facing)), (forward * Math.Sin(query.Facing)) + (right * Math.Cos(query.Facing)), 0).Normalized();
        double angle = Math.Acos(Math.Clamp(Double3.Dot(view.Normalized(), input), -1, 1)) * 180 / Math.PI;
        return angle > VaultParameters.MaximumInputAngle ? default : input;
    }

    // The search direction laid on the ground plane, pitched up a slope at most to the steepest followed angle; while
    // rising in the air it also tilts with the velocity.
    private static Double3 SlopeDirection(in VaultQuery query, Double3 direction)
    {
        (double pitch, double yaw, double roll) = FromZx(Double3.From(query.GroundNormal), direction);
        const double Maximum = VaultParameters.MaximumSlopeAngle * Math.PI / 180;
        pitch = pitch < VaultParameters.SlopeCutoffAngle * Math.PI / 180 ? Math.Clamp(pitch, 0, Maximum) : 0;
        Double3 search = new(Math.Cos(pitch) * Math.Cos(yaw), Math.Cos(pitch) * Math.Sin(yaw), Math.Sin(pitch));
        if (query is not { OnGround: false, Velocity.Z: > 0 })
        {
            return search;
        }

        (double rising, _, _) = FromZx(Double3.From(query.Velocity), direction);
        rising = Math.Clamp(rising, VaultParameters.MinimumRisingPitch * Math.PI / 180, VaultParameters.MaximumRisingPitch * Math.PI / 180);
        Double3 right = new((Math.Sin(roll) * Math.Sin(pitch) * Math.Cos(yaw)) - (Math.Cos(roll) * Math.Sin(yaw)),
            (Math.Sin(roll) * Math.Sin(pitch) * Math.Sin(yaw)) + (Math.Cos(roll) * Math.Cos(yaw)), -Math.Sin(roll) * Math.Cos(pitch));
        return RotateAround(search, right, rising);
    }

    // Pitch, yaw and roll of the frame whose Z axis is z and whose X axis is x made perpendicular to it.
    private static (double Pitch, double Yaw, double Roll) FromZx(Double3 z, Double3 x)
    {
        Double3 axisZ = z.Normalized();
        Double3 axisY = Double3.Cross(axisZ, x.Normalized()).Normalized();
        Double3 axisX = Double3.Cross(axisY, axisZ);
        double pitch = Math.Atan2(axisX.Z, Math.Sqrt((axisX.X * axisX.X) + (axisX.Y * axisX.Y)));
        double yaw = Math.Atan2(axisX.Y, axisX.X);
        Double3 level = new(-Math.Sin(yaw), Math.Cos(yaw), 0);
        double roll = Math.Atan2(Double3.Dot(axisZ, level), Double3.Dot(axisY, level));
        return (pitch, yaw, roll);
    }

    private static Double3 RotateAround(Double3 value, Double3 axis, double angle)
    {
        Double3 unit = axis.Normalized();
        return (value * Math.Cos(angle)) + (Double3.Cross(unit, value) * Math.Sin(angle)) + (unit * (Double3.Dot(unit, value) * (1 - Math.Cos(angle))));
    }

    // Probes straight down just beyond the edge: open space or a deep drop there means over, a top surface onto.
    private static VaultKind FarSide(CollisionWorld world, Double3 edge, Double3 toward, VaultCapsule capsule)
    {
        Double3 beyond = edge - (toward * (VaultParameters.FarSideRadii * capsule.Radius));
        double span = capsule.CrouchedHalfHeight + VaultParameters.FloorMargin;
        CollisionHit? hit = world.Raycast((beyond + new Double3(0, 0, span)).ToVector3D(), -Vector3.UnitZ, 2 * span, Climbable);
        return hit is { } top && top.Point.Z >= edge.Z - VaultParameters.FarSideDrop ? VaultKind.Onto : VaultKind.Over;
    }

    // A crouched capsule fits from above the near side of the edge to above the landing.
    private static bool CanMoveOverEdge(CollisionWorld world, Double3 edge, Double3 landing, Double3 normal, VaultCapsule capsule)
    {
        Double3 raise = new(0, 0, capsule.CrouchedHalfHeight + VaultParameters.FloorMargin);
        return Sweep(world, edge + (normal * capsule.Radius) + raise, landing + raise, capsule.Radius * VaultParameters.ProbeRadiusShare, capsule.CrouchedHalfHeight) is null;
    }

    // A sphere fits from above the near side of the edge down to the capsule centre.
    private static bool CanClimbUpToEdge(CollisionWorld world, Double3 edge, Double3 normal, VaultCapsule capsule, Double3 root)
    {
        Double3 start = edge + (normal * (2 * capsule.Radius)) + new Double3(0, 0, capsule.CrouchedHalfHeight + VaultParameters.FloorMargin);
        return world.SweepSphere(start.ToVector3D(), (root + new Double3(0, 0, VaultParameters.FloorMargin)).ToVector3D(),
            capsule.Radius * VaultParameters.ClimbProbeRadiusShare, Climbable) is null;
    }

    // A slightly shorter standing capsule, raised above the landing, overlaps nothing.
    private static bool CanStand(CollisionWorld world, Double3 landing, VaultCapsule capsule)
    {
        Double3 center = landing + new Double3(0, 0, capsule.StandingHalfHeight + (s_floorNormalZ * VaultParameters.StandProbeLift));
        double halfHeight = capsule.StandingHalfHeight - (s_floorNormalZ * VaultParameters.StandProbeShrink);
        return Sweep(world, center, center, capsule.Radius, halfHeight) is null;
    }

    private static CollisionHit? Sweep(CollisionWorld world, Double3 start, Double3 end, double radius, double halfHeight) =>
        world.SweepCapsule(start.ToVector3D(), end.ToVector3D(), radius, Math.Max(halfHeight, radius), Climbable);

    private static bool IsFloor(Vector3 normal) => normal.Z < -s_floorNormalZ || normal.Z >= s_floorNormalZ;

    private static bool Behind(Vector3D point, Double3 root, Double3 direction) =>
        Double3.Dot(Flat(Double3.From(point) - root), Flat(direction)) < 0;

    private static Double3 Flat(Double3 value)
    {
        double length = Math.Sqrt((value.X * value.X) + (value.Y * value.Y));
        return length * length < VaultParameters.DirectionTolerance ? default : new Double3(value.X / length, value.Y / length, 0);
    }

    private static double Seconds(uint frame, uint since) => (int)(frame - since) / (float)Math.Round(1 / PawnBlackboard.TickSeconds);

    private static double Precise(double value)
    {
        float tenths = (float)Math.Floor((value * VaultParameters.PositionPrecision) + 0.5) / (float)VaultParameters.PositionPrecision;
        return MathF.Floor(tenths + 0.5f);
    }

    private static double RoundToInt(double value) => Math.Floor(value + 0.5);
}
