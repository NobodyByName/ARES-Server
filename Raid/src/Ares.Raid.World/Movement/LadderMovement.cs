using System.Numerics;
using Ares.Common.Maps;
using Ares.Raid.World.Collision;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Game.Movement;
using Ares.Structs.Game.World;

namespace Ares.Raid.World.Movement;

/// <summary>
/// The climbing line of a placed ladder: it starts <see cref="LadderParameters.LineStartHeight"/> above its origin and
/// rises <see cref="Height"/> along the world's up axis.
/// </summary>
/// <param name="Origin">World point of the line at the ladder's origin height.</param>
/// <param name="Yaw">Heading of the ladder's forward axis, in radians; a climbing character faces it.</param>
/// <param name="Height">The ladder's replicated climb height, measured from the line start.</param>
/// <param name="AutoClimbOffTop">Whether climbing up past the top exit height leaves over the top; otherwise the climb stops there.</param>
public readonly record struct LadderLine(Vector3D Origin, double Yaw, double Height, bool AutoClimbOffTop = LadderParameters.AutoClimbOffTop)
{
    /// <summary>The climbing line of a placed ladder.</summary>
    /// <param name="placement">The ladder.</param>
    /// <returns>Its line.</returns>
    public static LadderLine From(LadderPlacement placement)
    {
        double yaw = placement.Yaw * Math.PI / 180;
        (double sin, double cos) = Math.SinCos(yaw);
        Vector3D origin = new(placement.Location.X + (LadderParameters.LineForwardOffset * cos),
            placement.Location.Y + (LadderParameters.LineForwardOffset * sin), placement.Location.Z);
        return new LadderLine(origin, yaw, LadderLogic.LadderHeight(placement.SplineTop, DamLadders.CycleHeight, DamLadders.EndInteractionDistance));
    }

    /// <summary>A world point in the line's frame: forward, right and up from the line start.</summary>
    /// <param name="world">World point.</param>
    /// <returns>The point relative to the line start.</returns>
    public Vector3D Local(Vector3D world)
    {
        (double sin, double cos) = Math.SinCos(Yaw);
        double x = world.X - Origin.X;
        double y = world.Y - Origin.Y;
        return new Vector3D((x * cos) + (y * sin), (y * cos) - (x * sin), world.Z - Origin.Z - LadderParameters.LineStartHeight);
    }

    /// <summary>A direction of the line's frame in world axes.</summary>
    /// <param name="local">Direction relative to the line (forward, right, up).</param>
    /// <returns>The world direction.</returns>
    public Vector3D Direction(Vector3D local)
    {
        (double sin, double cos) = Math.SinCos(Yaw);
        return new Vector3D((local.X * cos) - (local.Y * sin), (local.X * sin) + (local.Y * cos), local.Z);
    }
}

/// <summary>How a climb ends.</summary>
public enum LadderExit : byte
{
    /// <summary>Still climbing.</summary>
    None = 0,

    /// <summary>Back to ground movement with the ordinary blend (the ladder raises transient state 2).</summary>
    StepOff = 1,

    /// <summary>Back to ground movement without a blend: too far from the line (transient state 4).</summary>
    FallOff = 2,

    /// <summary>A jump off the ladder: the jump-off animation takes over from <see cref="LadderStep.ExitStart"/>.</summary>
    JumpOff = 3,

    /// <summary>Over the top: the climb-off animation takes over and ends at <see cref="LadderStep.ExitTarget"/>.</summary>
    ClimbOffTop = 4,
}

/// <summary>What a climbing character's input asks for on one tick.</summary>
/// <param name="Direction">Eight-way direction of the movement axes; forward climbs up, backward climbs down.</param>
/// <param name="Sprint">Whether sprint is held (faster up, sliding down).</param>
/// <param name="JumpPressed">Whether jump was pressed this tick.</param>
/// <param name="CrouchHeld">Whether crouch is held (character claims, <see cref="LadderParameters.CrouchButtonDownBit"/>).</param>
/// <param name="ControlYaw">Control heading in radians (the jump-off direction).</param>
/// <param name="Exhausted">Whether the character is exhausted (no sprint climbing).</param>
/// <param name="ClimberBelow">Whether another climbing character is close below (the climb does not go down into it).</param>
public readonly record struct LadderInput(
    InputDirection Direction,
    bool Sprint,
    bool JumpPressed,
    bool CrouchHeld,
    double ControlYaw,
    bool Exhausted = false,
    bool ClimberBelow = false)
{
    /// <summary>The ladder input of a tick.</summary>
    /// <param name="movement">Movement axes and buttons.</param>
    /// <param name="claims">Character claims.</param>
    /// <param name="controlYaw">Control heading in radians.</param>
    /// <param name="exhausted">Whether the character is exhausted.</param>
    /// <returns>The input.</returns>
    public static LadderInput From(MovementInput movement, CharacterInput claims, double controlYaw, bool exhausted) =>
        new(DodgeRollMovement.Direction(movement), movement.Buttons.HasFlag(MovementButtons.Sprint), movement.JumpPressed,
            (claims.Flags & (1UL << LadderParameters.CrouchButtonDownBit)) != 0, controlYaw, exhausted);
}

/// <summary>Persistent state of a climb.</summary>
/// <param name="Line">The ladder's climbing line.</param>
/// <param name="TopEntry">Whether the climb started from the top, with the entry animation.</param>
/// <param name="Ticks">Climbing steps already taken.</param>
public readonly record struct LadderState(LadderLine Line, bool TopEntry, int Ticks = 0)
{
    /// <summary>Seconds on the ladder before the next step, in the single precision the game's tick-to-time conversion uses.</summary>
    public double Seconds => Ticks * (float)PawnBlackboard.TickSeconds;

    /// <summary>Whether the next step still pulls the character onto the line.</summary>
    public bool Aligning => Seconds < LadderParameters.AlignmentSeconds;

    /// <summary>Whether the next step is still the top entry animation's.</summary>
    public bool PlayingTopEntry => TopEntry && Seconds < LadderParameters.TopEntrySeconds;
}

/// <summary>Result of one climbing step.</summary>
/// <param name="State">Climb state after the step.</param>
/// <param name="Velocity">Velocity of the step.</param>
/// <param name="Position">Feet after moving with <paramref name="Velocity"/> for the step, without collision.</param>
/// <param name="Yaw">Heading the character turns to (the ladder's), in radians.</param>
/// <param name="ActionLocomotion">Locomotion of the action layer (its stance is <see cref="CharacterStance.Ladder"/>).</param>
/// <param name="Flags">Graph state flags the step sets (<see cref="LadderParameters"/>).</param>
/// <param name="Exit">Whether and how the climb ends after this step.</param>
/// <param name="ExitStart">For a jump-off: where its animation starts (feet frame).</param>
/// <param name="ExitTarget">For a climb off the top: where the climb-off animation ends, before it is put on the floor.</param>
/// <param name="JumpDirection">For a jump-off: the input direction in world axes (zero without input).</param>
/// <param name="StaminaDrain">Whether the sprint stamina drain runs (sprinting upwards; <see cref="LadderParameters.SprintStaminaMagnitude"/>).</param>
public readonly record struct LadderStep(
    LadderState State,
    Vector3D Velocity,
    Vector3D Position,
    double Yaw,
    LocoMode ActionLocomotion,
    ulong Flags,
    LadderExit Exit,
    Vector3D? ExitStart = null,
    Vector3D? ExitTarget = null,
    Vector3D? JumpDirection = null,
    bool StaminaDrain = false);

/// <summary>
/// Ladder climbing: a velocity each tick that pulls the character onto the ladder's climbing line and moves it up or
/// down it, and the exits (bottom, crouch, too far from the line, jump, over the top).
/// </summary>
public static class LadderMovement
{
    /// <summary>Crouch input block ticks each climbing step renews.</summary>
    public static int CrouchBlockTicks => (int)Math.Round(LadderParameters.CrouchBlockSeconds / PawnBlackboard.TickSeconds);

    /// <summary>Starts a climb on a ladder.</summary>
    /// <param name="line">The ladder's climbing line.</param>
    /// <param name="topEntry">Whether the climb starts from the top interaction, with the entry animation.</param>
    /// <returns>The climb state before its first step.</returns>
    public static LadderState Begin(LadderLine line, bool topEntry) => new(line, topEntry);

    /// <summary>Where a top entry's animation is placed: the ladder's origin, below the feet.</summary>
    /// <param name="placement">The ladder.</param>
    /// <param name="feet">World position of the feet when the climb starts.</param>
    /// <returns>The animation's world position.</returns>
    public static Vector3D TopEntryAnchor(LadderPlacement placement, Vector3D feet) =>
        placement.Location with { Z = feet.Z - LadderParameters.TopEntryDrop - LadderParameters.RungLength };

    /// <summary>Advances a climb by one simulation tick.</summary>
    /// <param name="state">Climb state.</param>
    /// <param name="input">The tick's input.</param>
    /// <param name="feet">World position of the feet before the step.</param>
    /// <param name="velocity">Velocity before the step.</param>
    /// <param name="halfHeight">Capsule half height (feet to centre).</param>
    /// <param name="seconds">Simulation step duration.</param>
    /// <param name="world">Collision for the floor search at the bottom; without it a floor is assumed.</param>
    /// <returns>Velocity, heading, action locomotion, flags and the exit, if any.</returns>
    public static LadderStep Step(LadderState state, LadderInput input, Vector3D feet, Vector3D velocity, double halfHeight, double seconds, CollisionWorld? world)
    {
        if (!double.IsFinite(seconds) || seconds <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(seconds));
        }

        if (!double.IsFinite(halfHeight) || halfHeight <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(halfHeight));
        }

        LadderState next = state with { Ticks = state.Ticks + 1 };
        bool sprinting = input is { Sprint: true, Exhausted: false };
        LocoMode locomotion = sprinting ? LocoMode.Sprinting : LocoMode.Still;
        ulong flags = LadderParameters.ClimbingFlags | (input.Sprint ? LadderParameters.SprintActiveFlag : 0);
        double yaw = state.Line.Yaw;
        if (state.PlayingTopEntry)
        {
            // The entry animation moves the character; the climb itself starts once it has played.
            return new LadderStep(next, default, feet, yaw, locomotion, flags, LadderExit.None);
        }

        Vector3D centre = feet with { Z = feet.Z + halfHeight };
        if (input.JumpPressed && !state.Aligning)
        {
            (double sin, double cos) = Math.SinCos(yaw);
            Vector3D offset = LadderParameters.JumpOffStartOffset;
            Vector3D start = new(centre.X + (offset.X * cos) - (offset.Y * sin), centre.Y + (offset.X * sin) + (offset.Y * cos),
                centre.Z - LadderParameters.JumpOffHalfHeight + offset.Z);
            return new LadderStep(next, default, feet, yaw, locomotion, flags, LadderExit.JumpOff, ExitStart: start,
                JumpDirection: JumpDirection(input));
        }

        Vector3D local = state.Line.Local(centre);
        double height = local.Z - halfHeight - (state.Aligning ? LadderParameters.AlignmentLift : 0);
        // What brings the character back onto the line: across to it, and up to its start or down to its top.
        double correctionZ = height < 0 ? -height : height > state.Line.Height ? state.Line.Height - height : 0;
        Vector3D correction = new(-local.X, -local.Y, correctionZ);
        return state.Aligning
            ? Align(state, next, input, feet, velocity, height, correction, seconds, locomotion, flags)
            : Climb(state, next, input, feet, centre, velocity, height, correction, seconds, locomotion, flags, world);
    }

    // The first ticks on the ladder: a share of the way onto the line each tick; a falling character keeps most of its
    // vertical speed and slides.
    private static LadderStep Align(LadderState state, LadderState next, LadderInput input, Vector3D feet, Vector3D velocity, double height,
        Vector3D correction, double seconds, LocoMode locomotion, ulong flags)
    {
        Vector3D pull = state.Line.Direction(correction);
        double share = LadderParameters.AlignmentShare / seconds;
        Vector3D aligned = new(pull.X * share, pull.Y * share, pull.Z * share);
        double yaw = state.Line.Yaw;
        if (height + correction.Z <= 0)
        {
            if (velocity.Z >= -LadderParameters.SlideDownSpeed)
            {
                return Result(next, aligned, feet, seconds, yaw, locomotion, flags, LadderExit.None);
            }

            Vector3D falling = velocity with { Z = velocity.Z - (LadderParameters.FallAcceleration * seconds) };
            return Result(next, falling, feet, seconds, yaw, locomotion, flags, LadderExit.StepOff);
        }

        bool sliding = velocity.Z < 0;
        if (sliding)
        {
            double fade = Math.Min((float)state.Seconds / (float)LadderParameters.CatchSlowdownSeconds, 1f);
            aligned = aligned with { Z = velocity.Z + ((0 - velocity.Z) * fade) };
        }

        flags |= sliding ? LadderParameters.SlidingFlag : 0;
        Vector3D moved = aligned.Z < 0 && input.ClimberBelow ? default : aligned;
        return Result(next, moved, feet, seconds, yaw, locomotion, flags, LadderExit.None);
    }

    // Climbing: up or down the line at the climbing speed, reached at a constant vertical acceleration, and across to
    // the line at the remaining distance per second.
    private static LadderStep Climb(LadderState state, LadderState next, LadderInput input, Vector3D feet, Vector3D centre, Vector3D velocity,
        double height, Vector3D correction, double seconds, LocoMode locomotion, ulong flags, CollisionWorld? world)
    {
        double yaw = state.Line.Yaw;
        if ((correction.X * correction.X) + (correction.Y * correction.Y) > LadderParameters.FallOffDistanceSquared)
        {
            return Result(next, default, feet, seconds, yaw, locomotion, flags, LadderExit.FallOff);
        }

        bool sprinting = input is { Sprint: true, Exhausted: false };
        bool sliding = false;
        bool climbOffTop = false;
        double travel = correction.Z;
        switch (input.Direction)
        {
            case InputDirection.Forward or InputDirection.ForwardLeft or InputDirection.ForwardRight:
                double speed = sprinting ? LadderParameters.SprintUpSpeed : LadderParameters.ClimbUpSpeed;
                double top = LadderParameters.TopExitHeight(state.Line.Height);
                if (state.Line.AutoClimbOffTop)
                {
                    travel += speed * seconds;
                    climbOffTop = height + travel > top;
                }
                else
                {
                    travel += Math.Min(Math.Max(top - height, 0), speed * seconds);
                }

                break;
            case InputDirection.Backward or InputDirection.BackwardLeft or InputDirection.BackwardRight:
                sliding = input.Sprint;
                travel -= (input.Sprint ? LadderParameters.SlideDownSpeed : LadderParameters.ClimbDownSpeed) * seconds;
                break;
        }

        flags |= LadderParameters.CrouchInputBlockedFlag;
        if (input.CrouchHeld)
        {
            return Result(next, default, feet, seconds, yaw, locomotion, flags, LadderExit.StepOff);
        }

        bool fast = velocity.Z < -LadderParameters.SlideDownSpeed;
        double vertical = InterpolateConstant(Math.Min(velocity.Z, LadderParameters.StartSpeedLimit), travel / seconds, seconds,
            fast ? LadderParameters.FastVerticalAcceleration : LadderParameters.VerticalAcceleration);
        Vector3D moving = state.Line.Direction(correction with { Z = vertical });
        sliding |= fast;
        Vector3D moved = vertical < 0 && input.ClimberBelow ? default : moving;
        flags |= sliding ? LadderParameters.SlidingFlag : 0;
        bool drain = moving.Z > 0 && sprinting;
        LadderExit exit = climbOffTop ? LadderExit.ClimbOffTop : LadderExit.None;
        Vector3D? target = climbOffTop ? ClimbOffTopTarget(centre, yaw) : null;
        if (height + travel > 0)
        {
            return Result(next, moved, feet, seconds, yaw, locomotion, flags, exit) with { ExitTarget = target, StaminaDrain = drain };
        }

        // The bottom of the line: step off onto the floor below, or stay on the ladder if there is none.
        exit = fast || input.Sprint || FloorBelow(world, centre) ? LadderExit.StepOff : LadderExit.None;
        return Result(next, default, feet, seconds, yaw, locomotion, flags, exit) with { StaminaDrain = drain };
    }

    /// <summary>Where climbing off the top ends: in front of the capsule centre, before it is put on the floor.</summary>
    /// <param name="centre">World position of the capsule centre.</param>
    /// <param name="yaw">Character heading in radians.</param>
    /// <returns>The end point.</returns>
    private static Vector3D ClimbOffTopTarget(Vector3D centre, double yaw)
    {
        (double sin, double cos) = Math.SinCos(yaw);
        return new Vector3D(centre.X + (LadderParameters.ClimbOffTopReach * cos), centre.Y + (LadderParameters.ClimbOffTopReach * sin), centre.Z);
    }

    /// <summary>
    /// Puts the climb-off end on the first surface found from <see cref="LadderParameters.ClimbOffTopProbe"/> above it to as
    /// far below; without a surface it stays where it is. Pawn-blocking bodies stand in for the game's visibility trace.
    /// </summary>
    /// <param name="world">Collision.</param>
    /// <param name="target">The end point from <see cref="ClimbOffTopTarget"/>.</param>
    /// <returns>The adjusted end point.</returns>
    public static Vector3D ClimbOffTopFloor(CollisionWorld world, Vector3D target)
    {
        ArgumentNullException.ThrowIfNull(world);
        Vector3D from = target with { Z = target.Z + LadderParameters.ClimbOffTopProbe };
        return world.Raycast(from, -Vector3.UnitZ, 2 * LadderParameters.ClimbOffTopProbe, PlacementFlags.BlocksPawn) is { } hit ? hit.Point : target;
    }

    // The engine's constant-rate interpolation.
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

    private static bool FloorBelow(CollisionWorld? world, Vector3D centre) =>
        world is null || world.Raycast(centre, -Vector3.UnitZ, LadderParameters.FloorProbeDepth, PlacementFlags.BlocksClimbable) is not null;

    private static Vector3D JumpDirection(LadderInput input)
    {
        (double sin, double cos) = Math.SinCos(input.ControlYaw);
        (double forward, double right) = DodgeRollMovement.Coordinates(input.Direction);
        return new Vector3D((forward * cos) - (right * sin), (forward * sin) + (right * cos), 0);
    }

    private static LadderStep Result(LadderState next, Vector3D velocity, Vector3D feet, double seconds, double yaw, LocoMode locomotion, ulong flags, LadderExit exit) =>
        new(next, velocity, new Vector3D(feet.X + (velocity.X * seconds), feet.Y + (velocity.Y * seconds), feet.Z + (velocity.Z * seconds)), yaw, locomotion, flags, exit);
}
