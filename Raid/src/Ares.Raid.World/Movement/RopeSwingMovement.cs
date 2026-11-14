using Ares.Structs.Core.Geometry;
using Ares.Structs.Game.Movement;

namespace Ares.Raid.World.Movement;

/// <summary>
/// The grappling hook's rope as the swing sees it: a single straight segment from the character to a fixed anchor.
/// </summary>
/// <param name="Pivot">World position of the anchor the rope runs to.</param>
/// <param name="WorkingLength">Current rope length (the rope budget's working length).</param>
/// <param name="MaxLength">Longest the rope may pay out to (the rope budget's maximum, the hook range).</param>
/// <param name="AnchorVelocity">Velocity of the anchor (zero for the static world).</param>
/// <param name="Anchored">Whether the rope is still anchored.</param>
/// <param name="EndReason">Why the rope ended, once it did (<see cref="SwingParameters.ReleasedEndReason"/>, <see cref="SwingParameters.BrokenEndReason"/>).</param>
public readonly record struct GrappleRope(
    Vector3D Pivot,
    double WorkingLength,
    double MaxLength = GrapplingHookParameters.HookRange,
    Vector3D AnchorVelocity = default,
    bool Anchored = true,
    byte EndReason = 0)
{
    /// <summary>Whether the rope is taut for a rope end at <paramref name="ropeEnd"/>: the path is longer than the rope.</summary>
    /// <param name="ropeEnd">Where the rope meets the character.</param>
    /// <returns>Whether it is taut.</returns>
    public bool IsTaut(Vector3D ropeEnd) => Vectors.Length(Vectors.Subtract(Pivot, ropeEnd)) > WorkingLength;

    /// <summary>The rope released or broken.</summary>
    /// <param name="reason">Why it ends.</param>
    /// <returns>The ended rope.</returns>
    public GrappleRope End(byte reason) => this with { Anchored = false, EndReason = reason };
}

/// <summary>The ground under a character as the swing reads it.</summary>
/// <param name="Distance">Distance from the feet down to the ground.</param>
/// <param name="Walkable">Whether the floor is walkable.</param>
/// <param name="Normal">Unit ground normal.</param>
public readonly record struct RopeSwingGround(double Distance, bool Walkable, Vector3D Normal)
{
    /// <summary>No ground within reach.</summary>
    public static RopeSwingGround None => new(double.MaxValue, false, default);
}

/// <summary>What a swinging character's input and status ask for on one tick.</summary>
/// <param name="Throttle">Forward movement axis (-1 to 1).</param>
/// <param name="Strafe">Right movement axis (-1 to 1).</param>
/// <param name="ControlYaw">Yaw of the control rotation in radians; the character faces it and the input turns with it.</param>
/// <param name="FireDown">Whether the primary input is held (climbs the rope).</param>
/// <param name="CrouchDown">Whether the crouch button is held (rappels down the rope).</param>
/// <param name="JumpPressed">Whether jump was pressed this tick (lets go with a jump).</param>
/// <param name="Exhausted">Whether the previous result had <c>Status.Exhausted</c> (no steering, slower climbing).</param>
/// <param name="VaultRequested">Whether a vault request is buffered.</param>
/// <param name="BlendingOut">Whether the node is blending out (it only keeps the velocity).</param>
public readonly record struct RopeSwingInput(
    double Throttle,
    double Strafe,
    double ControlYaw,
    bool FireDown = false,
    bool CrouchDown = false,
    bool JumpPressed = false,
    bool Exhausted = false,
    bool VaultRequested = false,
    bool BlendingOut = false)
{
    /// <summary>Whether any movement axis is used.</summary>
    public bool HasMoveInput => Math.Abs(Strafe) + Math.Abs(Throttle) > 1e-8;
}

/// <summary>
/// The swing node's own state (<c>UPendulumSwingMovementNode</c>) plus the ticks it has run. Wall faces are not
/// modelled, so the wall members keep their starting values.
/// </summary>
/// <param name="RopeLength">Rope length the swing works with (<c>RopeLength</c>).</param>
/// <param name="CurrentReelSpeed">Reel speed: negative climbs, positive pays out (<c>CurrentReelSpeed</c>).</param>
/// <param name="Kicking">Whether the character pumps the swing (<c>bKicking</c>).</param>
/// <param name="RopeOverloaded">Whether the rope is stretched past breaking (<c>bRopeOverloaded</c>).</param>
/// <param name="Phase">Swing phase.</param>
/// <param name="SwingingForward">Whether the swing runs along the camera (<c>bSwingingForward</c>).</param>
/// <param name="ClimbingUpRope">Whether the character climbs (<c>bClimbingUpRope</c>).</param>
/// <param name="Rappelling">Whether the character rappels (<c>bRappelling</c>).</param>
/// <param name="GroundClimbEntry">Whether the swing started as a climb from the ground (<c>bIsGroundClimbEntry</c>).</param>
/// <param name="WallKickStartTick">Tick a wall kick input started, or -1.</param>
/// <param name="WallImpactEndTick">Tick a wall impact ends, or -1.</param>
/// <param name="HadWallKickInput">Whether a wall kick input was held.</param>
/// <param name="WallState">Where the character is relative to a wall.</param>
/// <param name="Ticks">Swing steps already taken.</param>
public readonly record struct RopeSwingState(
    double RopeLength,
    double CurrentReelSpeed = 0,
    bool Kicking = false,
    bool RopeOverloaded = false,
    SwingPhase Phase = SwingPhase.Idle,
    bool SwingingForward = true,
    bool ClimbingUpRope = false,
    bool Rappelling = false,
    bool GroundClimbEntry = false,
    int WallKickStartTick = -1,
    int WallImpactEndTick = -1,
    bool HadWallKickInput = false,
    SwingWallState WallState = SwingWallState.Off,
    int Ticks = 0);

/// <summary>Why a swing ends.</summary>
public enum RopeSwingExit : byte
{
    /// <summary>Still swinging.</summary>
    None = 0,

    /// <summary>The rope is no longer anchored (retracted, lost).</summary>
    Unanchored = 1,

    /// <summary>The character landed on walkable ground; the rope gets slack.</summary>
    Landed = 2,

    /// <summary>The rope was stretched past breaking.</summary>
    RopeBroke = 3,

    /// <summary>A jump off the rope.</summary>
    JumpOff = 4,

    /// <summary>A vault out of the swing.</summary>
    Vault = 5,

    /// <summary>The node is blending out.</summary>
    BlendingOut = 6,
}

/// <summary>Where a swinging character tries a vault: the probe location and facing the vault query runs with.</summary>
/// <param name="location">Character location for the vault query (50 back from the facing and 92 up).</param>
/// <param name="facing">Flat facing direction of the vault query.</param>
/// <returns>Whether a vault starts.</returns>
public delegate bool RopeSwingVaultQuery(Vector3D location, Vector3D facing);

/// <summary>Result of one swing step.</summary>
/// <param name="State">Swing state after the step.</param>
/// <param name="Rope">Rope after the step.</param>
/// <param name="Velocity">Velocity of the step.</param>
/// <param name="Position">Feet after moving with <paramref name="Velocity"/> for the step, without collision.</param>
/// <param name="Yaw">Heading the character turns to (the control yaw), in radians, or <see langword="null"/> to keep its heading.</param>
/// <param name="Flags">Graph state flags of the step (<see cref="SwingParameters"/>).</param>
/// <param name="Exit">Why the swing ends after this step, if it does.</param>
/// <param name="Finished">Whether the step raises the finished state, so the graph returns to ground movement.</param>
/// <param name="HeavyLandingQuery">Whether the step runs the ground movement's heavy-landing query.</param>
/// <param name="ActionLayerWritten">Whether the step writes the action layer's stance and locomotion.</param>
/// <param name="CrouchBlockTicks">Ticks the crouch input stays blocked at least (rappelling), else 0.</param>
/// <param name="StaminaCost">Stamina the server drains for this step.</param>
public readonly record struct RopeSwingStep(
    RopeSwingState State,
    GrappleRope Rope,
    Vector3D Velocity,
    Vector3D Position,
    double? Yaw,
    ulong Flags,
    RopeSwingExit Exit,
    bool Finished,
    bool HeavyLandingQuery,
    bool ActionLayerWritten,
    int CrouchBlockTicks,
    double StaminaCost);

/// <summary>
/// Swinging on the grappling hook's rope (<c>UPendulumSwingMovementNodeData</c>): when the swing may start, its
/// start, and each tick the pendulum physics under the anchor (gravity, steering, pumping, drag, the rope's reel,
/// stretch and catch), climbing, rappelling and the exits (landing, letting go, jumping off, the rope breaking, a
/// vault). The rope is a single straight segment; wall faces, rope wrapping and other tethers are not modelled.
/// </summary>
public static class RopeSwingMovement
{
    private static readonly double s_minPivotUpFraction = Math.Sin(SwingParameters.MinAnchorAngleAboveHorizontalDeg * Math.PI / 180);
    private static readonly double s_minPivotUpFractionGround = Math.Sin(SwingParameters.MinAnchorAngleAboveHorizontalDegGround * Math.PI / 180);
    private static readonly double s_faceEnterNormalZ = Math.Cos(SwingParameters.FaceEnterAngleDeg * Math.PI / 180);
    private static readonly double s_faceExitNormalZ = Math.Cos(SwingParameters.FaceExitAngleDeg * Math.PI / 180);
    private static readonly double s_kickStartTiltSin = Math.Sin(SwingParameters.KickStartTiltDeg * Math.PI / 180);

    /// <summary>Where the rope meets the character: the top of the capsule.</summary>
    /// <param name="feet">World position of the feet.</param>
    /// <param name="halfHeight">Capsule half height.</param>
    /// <returns>The rope end.</returns>
    public static Vector3D RopeEnd(Vector3D feet, double halfHeight) => feet with { Z = feet.Z + (2 * halfHeight) };

    /// <summary>
    /// Whether the swing starts from the current movement (<c>UPendulumToSwingMovementTransitionNodeData</c>): the rope
    /// is anchored and pulling, rises steeply enough above the rope end (steeper when standing on the ground), and the
    /// character either holds the primary input, has the rope taut, stands on a face too steep to stand on, or falls
    /// with room under the swing arc.
    /// </summary>
    /// <param name="rope">The rope.</param>
    /// <param name="feet">World position of the feet.</param>
    /// <param name="halfHeight">Capsule half height.</param>
    /// <param name="velocity">Current velocity.</param>
    /// <param name="ground">The ground under the character.</param>
    /// <param name="mode">Current movement mode.</param>
    /// <param name="fireDown">Whether the primary input is held.</param>
    /// <param name="ownedPullFraction">Pull fraction of the character's own tether (1 unless the server lowers it).</param>
    /// <returns>Whether the swing starts.</returns>
    public static bool CanStart(GrappleRope rope, Vector3D feet, double halfHeight, Vector3D velocity, RopeSwingGround ground,
        RoughMovementMode mode, bool fireDown, double ownedPullFraction = 1)
    {
        if (!rope.Anchored || !(ownedPullFraction > 0))
        {
            return false;
        }

        Vector3D ropeEnd = RopeEnd(feet, halfHeight);
        Vector3D toPivot = Vectors.Subtract(rope.Pivot, ropeEnd);
        double distance = Vectors.Length(toPivot);
        if (distance <= 1e-8)
        {
            return false;
        }

        bool steep = ground.Walkable && IsTooSteepToStand(ground.Normal, false);
        bool onGround = ground is { Walkable: true, Distance: < SwingParameters.GroundContactDistance } && !steep;
        if (Vectors.Normal(toPivot).Z < (onGround ? s_minPivotUpFractionGround : s_minPivotUpFraction))
        {
            return false;
        }

        if (fireDown || rope.IsTaut(ropeEnd))
        {
            return true;
        }

        if (ground.Distance < SwingParameters.GroundContactDistance && IsTooSteepToStand(ground.Normal, false))
        {
            return true;
        }

        if (velocity.Z >= 0 || mode != RoughMovementMode.Air)
        {
            return false;
        }

        double lowestArc = Math.Max(distance, SwingParameters.MinRopeLength) - toPivot.Z + SwingParameters.MinSwingArcGroundClearance;
        return lowestArc <= ground.Distance;
    }

    /// <summary>
    /// Starts a swing (<c>InitSelfBP</c>): the rope takes the current path (at least
    /// <see cref="SwingParameters.MinEntryRopeLength"/>), the reel starts at the speed away from the anchor, and holding
    /// the primary input on walkable ground starts a climb with a jump.
    /// </summary>
    /// <param name="rope">The rope.</param>
    /// <param name="feet">World position of the feet.</param>
    /// <param name="halfHeight">Capsule half height.</param>
    /// <param name="velocity">Velocity before the swing; the ground climb lifts it.</param>
    /// <param name="mode">Movement mode before the swing; a ground climb turns it to air.</param>
    /// <param name="floorWalkable">Whether the floor under the character is walkable.</param>
    /// <param name="fireDown">Whether the primary input is held.</param>
    /// <param name="staminaCost">Stamina the server drains for the rope catching a falling character.</param>
    /// <returns>The swing state before its first step, and the rope.</returns>
    public static (RopeSwingState State, GrappleRope Rope) Begin(GrappleRope rope, Vector3D feet, double halfHeight, ref Vector3D velocity,
        ref RoughMovementMode mode, bool floorWalkable, bool fireDown, out double staminaCost)
    {
        Vector3D ropeEnd = RopeEnd(feet, halfHeight);
        double distance = Vectors.Length(Vectors.Subtract(rope.Pivot, ropeEnd));
        double working = Math.Max((float)SwingParameters.MinEntryRopeLength, (float)distance);
        Vector3D outward = Vectors.Normal(Vectors.Subtract(ropeEnd, rope.Pivot));
        double reel = (float)Math.Clamp(Vectors.Dot(outward, velocity), 0, SwingParameters.ReelSpeedMaximum);
        staminaCost = mode == RoughMovementMode.Air ? SwingParameters.RopeCatchStaminaCost : 0;
        bool groundClimb = fireDown && mode == RoughMovementMode.Ground && floorWalkable;
        if (groundClimb)
        {
            velocity = velocity with { Z = Math.Max(velocity.Z, SwingParameters.GroundClimbEntryJumpZ) };
            mode = RoughMovementMode.Air;
        }

        RopeSwingState state = new(working, reel, GroundClimbEntry: groundClimb);
        return (state, rope with { WorkingLength = working });
    }

    /// <summary>Advances a swing by one simulation tick (<c>SimUpdateSelfBP</c> without wall faces).</summary>
    /// <param name="state">Swing state.</param>
    /// <param name="rope">The rope.</param>
    /// <param name="input">The tick's input.</param>
    /// <param name="feet">World position of the feet before the step.</param>
    /// <param name="velocity">Velocity before the step.</param>
    /// <param name="halfHeight">Current capsule half height.</param>
    /// <param name="ground">The ground under the character.</param>
    /// <param name="seconds">Simulation step duration.</param>
    /// <param name="vault">The vault query a climb to the top or a vault request runs, if any.</param>
    /// <returns>Velocity, heading, flags, the rope and the exit, if any.</returns>
    public static RopeSwingStep Step(RopeSwingState state, GrappleRope rope, RopeSwingInput input, Vector3D feet, Vector3D velocity, double halfHeight,
        RopeSwingGround ground, double seconds, RopeSwingVaultQuery? vault = null)
    {
        if (!double.IsFinite(seconds) || seconds <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(seconds));
        }

        double yaw = input.ControlYaw;
        RopeSwingState next = state with { Ticks = state.Ticks + 1 };
        if (input.BlendingOut)
        {
            return Result(next with { Phase = SwingPhase.Detaching }, rope, velocity, feet, seconds, null, 0, RopeSwingExit.BlendingOut, false, false, 0, 0);
        }

        if (!rope.Anchored)
        {
            return End(next, rope, velocity, feet, seconds, RopeSwingExit.Unanchored, false);
        }

        bool forcedClimb = state.GroundClimbEntry && next.Ticks * (double)(float)PawnBlackboard.TickSeconds < SwingParameters.GroundClimbEntryForcedClimbTime;
        next = next with { ClimbingUpRope = input.FireDown || forcedClimb, Rappelling = input.CrouchDown, HadWallKickInput = false };
        ulong flags = SwingParameters.SwingingFlags;
        int crouchBlock = 0;
        if (next.Rappelling)
        {
            flags |= SwingParameters.StaminaBlockFlag;
            crouchBlock = GrapplingHookMovement.TimeToTicks(SwingParameters.RappelCrouchBlockTime);
        }

        if (next.ClimbingUpRope)
        {
            next = next with { WallKickStartTick = -1 };
        }

        Vector3D steerWorld = Steer(input);
        Vector3D ropeEnd = RopeEnd(feet, halfHeight);
        bool nearGround = ground is { Distance: < SwingParameters.GroundContactDistance, Walkable: true };
        bool steepGround = nearGround && IsTooSteepToStand(ground.Normal, false);
        bool pivotHighEnough = Vectors.Normal(Vectors.Subtract(rope.Pivot, ropeEnd)).Z >= s_minPivotUpFraction;
        if (nearGround && !steepGround && !(velocity.Z > 0) && !(next.ClimbingUpRope && pivotHighEnough))
        {
            double slack = (float)(Vectors.Length(Vectors.Subtract(rope.Pivot, ropeEnd)) + SwingParameters.LandingRopeSlack);
            return End(next, rope with { WorkingLength = slack }, velocity, feet, seconds, RopeSwingExit.Landed, false);
        }

        next = next with { RopeLength = rope.WorkingLength };
        double distance = Vectors.Length(Vectors.Subtract(rope.Pivot, ropeEnd));
        if (next.ClimbingUpRope)
        {
            double shortest = Math.Max((float)SwingParameters.ClimbMinRopeLength, (float)distance);
            if (shortest < rope.WorkingLength)
            {
                rope = rope with { WorkingLength = shortest };
                next = next with { RopeLength = shortest };
            }
        }

        double free = rope.WorkingLength;
        if ((next.ClimbingUpRope && free <= SwingParameters.ClimbMinRopeLength + SwingParameters.AutoVaultClimbReachThreshold) || input.VaultRequested)
        {
            (double sin, double cos) = Math.SinCos(yaw);
            Vector3D facing = new(cos, sin, 0);
            Vector3D centre = feet with { Z = feet.Z + halfHeight };
            Vector3D probe = new(centre.X - (50 * facing.X), centre.Y - (50 * facing.Y), centre.Z + 92);
            if (vault is not null && vault(probe, facing))
            {
                GrappleRope vaulted = rope.WorkingLength <= SwingParameters.AutoVaultDetachRopeLength ? rope.End(SwingParameters.ReleasedEndReason) : rope;
                return Result(next with { Phase = SwingPhase.Detaching }, vaulted, velocity, feet, seconds, null, flags, RopeSwingExit.Vault, false, true, crouchBlock, 0);
            }
        }

        double inputSquared = (input.Throttle * input.Throttle) + (input.Strafe * input.Strafe);
        double inputShare = inputSquared > 1e-8 ? Math.Abs(input.Throttle) / Math.Sqrt(inputSquared) : 0;
        Vector3D steer = input.FireDown || input.Exhausted ? default : Vectors.Scale(steerWorld, inputShare);
        SwingSimulationInput simulation = new(seconds, ropeEnd, velocity, rope.Pivot, rope.AnchorVelocity, free, rope.MaxLength, steer,
            new Vector3D(Math.Cos(yaw), Math.Sin(yaw), 0), input.HasMoveInput, next.ClimbingUpRope,
            input.Exhausted ? SwingParameters.ExhaustedClimbSpeedScale : 1, next.Rappelling);
        Vector3D result = TickSwingPhysics(ref next, simulation);
        double stamina = 0;
        if (next.Kicking)
        {
            flags |= SwingParameters.StaminaBlockFlag;
            stamina += SwingParameters.KickStaminaCostPerSecond * seconds;
        }

        rope = rope with { WorkingLength = next.RopeLength };
        if (next.RopeOverloaded)
        {
            return End(next, rope.End(SwingParameters.BrokenEndReason), result, feet, seconds, RopeSwingExit.RopeBroke, true) with
            {
                Flags = flags,
                StaminaCost = stamina,
                CrouchBlockTicks = crouchBlock,
            };
        }

        if (next is { ClimbingUpRope: true, RopeLength: <= SwingParameters.ClimbMinRopeLength })
        {
            next = next with { ClimbingUpRope = false };
        }
        else if (next.Rappelling && next.RopeLength >= rope.MaxLength)
        {
            next = next with { Rappelling = false };
        }

        if (next.ClimbingUpRope)
        {
            flags |= SwingParameters.StaminaBlockFlag;
            stamina += SwingParameters.ClimbStaminaCostPerSecond * seconds;
        }

        if (!input.JumpPressed)
        {
            return Result(next, rope, result, feet, seconds, yaw, flags, RopeSwingExit.None, false, true, crouchBlock, stamina);
        }

        next = next with { Phase = SwingPhase.Detaching };
        rope = rope.End(SwingParameters.ReleasedEndReason);
        result = new Vector3D(result.X + (steerWorld.X * SwingParameters.JumpForce), result.Y + (steerWorld.Y * SwingParameters.JumpForce),
            result.Z + (steerWorld.Z * SwingParameters.JumpForce) + SwingParameters.JumpUpwardForce);
        flags |= SwingParameters.JumpStartedFlag;
        return Result(next, rope, result, feet, seconds, yaw, flags, RopeSwingExit.JumpOff, true, true, crouchBlock, stamina);
    }

    /// <summary>Whether a surface normal is too steep to stand on (a face), with hysteresis while already on a face.</summary>
    /// <param name="normal">Surface normal.</param>
    /// <param name="alreadyOnFace">Whether the character already stands on a face.</param>
    /// <returns>Whether it is too steep.</returns>
    private static bool IsTooSteepToStand(Vector3D normal, bool alreadyOnFace) =>
        !Vectors.IsNearlyZero(normal, 1e-4) && normal.Z < (alreadyOnFace ? s_faceExitNormalZ : s_faceEnterNormalZ);

    /// <summary>
    /// The swing phase after a tick (<c>ClassifyPhase</c> then <c>StepPhase</c>): hanging still near the bottom without
    /// steering, turning around when slow away from the bottom, else descending or ascending by whether gravity speeds
    /// the swing up; a direct change between descending and ascending passes through a reversal.
    /// </summary>
    /// <param name="current">Phase before the tick.</param>
    /// <param name="tangentSpeed">Speed across the rope.</param>
    /// <param name="swingOffset">Horizontal distance from under the anchor.</param>
    /// <param name="gravityAlongSwing">Dot product of gravity across the rope with the swing direction.</param>
    /// <param name="idleAllowed">Whether there is no steering.</param>
    /// <returns>The new phase.</returns>
    public static SwingPhase NextPhase(SwingPhase current, double tangentSpeed, double swingOffset, double gravityAlongSwing, bool idleAllowed)
    {
        bool idle = current == SwingPhase.Idle;
        double exitSpeed = idle ? SwingParameters.IdleExitSpeed : SwingParameters.PhaseLowSpeedThreshold;
        double band = idle ? SwingParameters.MaxIdleExitHorizontalOffset : SwingParameters.MaxIdleHorizontalOffset;
        bool underAnchor = swingOffset < band;
        SwingPhase desired;
        if (tangentSpeed < exitSpeed && underAnchor && idleAllowed)
        {
            desired = SwingPhase.Idle;
        }
        else if (!idle && tangentSpeed < SwingParameters.ReversalSpeedThreshold && !underAnchor)
        {
            desired = SwingPhase.Reversal;
        }
        else
        {
            desired = gravityAlongSwing > 0 ? SwingPhase.Descending : SwingPhase.Ascending;
        }

        bool flips = (current == SwingPhase.Ascending && desired == SwingPhase.Descending) || (current == SwingPhase.Descending && desired == SwingPhase.Ascending);
        return flips ? SwingPhase.Reversal : desired;
    }

    /// <summary>
    /// Moves the reel speed towards what the input asks (<c>UpdateReelSpeed</c>): climbing reels in (limited to the rope
    /// left above the shortest climb), rappelling pays out, otherwise the reel returns to zero.
    /// </summary>
    /// <param name="state">Swing state (rope length and current reel speed).</param>
    /// <param name="climbHeld">Whether the character climbs.</param>
    /// <param name="rappelHeld">Whether the character rappels.</param>
    /// <param name="climbSpeedScale">Factor on the climbing speed.</param>
    /// <param name="maxLength">Longest the rope may pay out to.</param>
    /// <param name="seconds">Simulation step duration.</param>
    /// <returns>The new reel speed as stored, and the reel speed the rope uses (zero inside the dead zone).</returns>
    private static (double Stored, double Effective) ReelSpeed(RopeSwingState state, bool climbHeld, bool rappelHeld, double climbSpeedScale, double maxLength,
        double seconds)
    {
        double target = 0;
        double rate = SwingParameters.ReelReleaseAccel;
        if (climbHeld)
        {
            double left = Math.Max((float)(state.RopeLength - SwingParameters.ClimbMinRopeLength), 0f) / Math.Max(seconds, 1e-8);
            target = -Math.Min(left, SwingParameters.ClimbSpeed * climbSpeedScale);
            rate = SwingParameters.ClimbReelAccel;
        }
        else if (rappelHeld && state.RopeLength < Math.Min(maxLength, SwingParameters.RopeLengthMaximum))
        {
            target = SwingParameters.RappelSpeed;
            rate = state.CurrentReelSpeed > target ? SwingParameters.RappelReelDecel : SwingParameters.RappelReelAccel;
        }

        double stored = (float)InterpolateConstant(state.CurrentReelSpeed, target, seconds, rate);
        return (stored, Math.Abs(stored) < SwingParameters.ReelSpeedDeadzone ? 0 : stored);
    }

    // One tick of pendulum physics away from wall faces (TickSwingPhysics).
    private static Vector3D TickSwingPhysics(ref RopeSwingState state, SwingSimulationInput input)
    {
        Vector3D velocity = input.Velocity;
        Vector3D toPivot = Vectors.Subtract(input.Pivot, input.Location);
        double distance = Vectors.Length(toPivot);
        double dt = input.Seconds;
        if (distance <= 1e-8)
        {
            state = state with { RopeOverloaded = false };
            return velocity with { Z = velocity.Z - (AirMovementParameters.FallAcceleration * SwingParameters.GravityMultiplier * dt) };
        }

        Vector3D ropeDirection = Vectors.Scale(toPivot, 1 / distance);
        Vector3D outward = Vectors.Scale(ropeDirection, -1);
        double lengthScale = Math.Max(input.FreeRopeLength, SwingParameters.MinRopeLength) / SwingParameters.ReferenceRopeLength;
        double share = Math.Clamp(Vectors.Length(Vectors.PlaneProject(velocity, outward)) / SwingParameters.GravityApexBlendSpeed, 0, 1);
        double gravity = SwingParameters.ApexGravityMultiplier + ((SwingParameters.GravityMultiplier - SwingParameters.ApexGravityMultiplier) * share);
        velocity = velocity with { Z = velocity.Z + (-AirMovementParameters.FallAcceleration * gravity * dt) };

        Vector3D acceleration = Vectors.PlaneProject(Vectors.Scale(input.SteerDirection, SwingParameters.LeanAccel * lengthScale), outward);
        Vector3D across = Vectors.PlaneProject(velocity, outward);
        double tangentSpeed = Vectors.Length(across);
        Vector3D swingDirection = tangentSpeed > 1e-8 ? Vectors.Scale(across, 1 / tangentSpeed) : default;
        double lean = 1;
        double accelerationSquared = Vectors.Dot(acceleration, acceleration);
        if (tangentSpeed > 1e-8 && accelerationSquared > 1e-8)
        {
            double alignment = Vectors.Dot(swingDirection, Vectors.Scale(acceleration, 1 / Math.Sqrt(accelerationSquared)));
            lean = 1 + (alignment * (alignment < 0 ? SwingParameters.AntiPhaseLeanDamping : SwingParameters.InPhaseLeanBonus));
        }

        velocity = Vectors.Add(velocity, Vectors.Scale(Vectors.Scale(acceleration, lean), dt));
        Vector3D gravityAcross = Vectors.PlaneProject(new Vector3D(0, 0, -1), outward);
        double swingOffset = Math.Sqrt(((input.Location.X - input.Pivot.X) * (input.Location.X - input.Pivot.X)) + ((input.Location.Y - input.Pivot.Y) * (input.Location.Y - input.Pivot.Y)));
        velocity = Vectors.Add(velocity, Kick(ref state, input, swingDirection, gravityAcross, tangentSpeed, lengthScale));

        bool idleAllowed = Vectors.Dot(input.SteerDirection, input.SteerDirection) <= 1e-8;
        SwingPhase phase = NextPhase(state.Phase, tangentSpeed, swingOffset, Vectors.Dot(gravityAcross, swingDirection), idleAllowed);
        state = state with { Phase = phase };
        if (phase is SwingPhase.Descending or SwingPhase.Ascending)
        {
            double cameraAlignment = Vectors.Dot(input.CameraForward, swingDirection);
            if (Math.Abs(cameraAlignment) > SwingParameters.SwingDirectionLatchThreshold)
            {
                state = state with { SwingingForward = cameraAlignment >= 0 };
            }
        }

        double shortRope = MapClamped(input.FreeRopeLength, SwingParameters.ShortRopeDragLength, SwingParameters.ClimbMinRopeLength, 1, SwingParameters.ShortRopeDragScale);
        double drag = SwingParameters.QuadraticDragCoefficient * shortRope * (input.ClimbHeld ? SwingParameters.ClimbDragScale : 1);
        Vector3D swing = Vectors.PlaneProject(velocity, outward);
        velocity = Vectors.Subtract(velocity, Vectors.Scale(Vectors.Scale(swing, Vectors.Length(swing) * drag), dt));
        if (input is { HasInput: true, ClimbHeld: false })
        {
            return ApplyRopeConstraint(ref state, input, velocity, ropeDirection, outward, distance, lengthScale);
        }

        double settle = Math.Min(SwingParameters.NoInputSettleDamping * Math.Sqrt(1 / Math.Max(lengthScale, 1e-8)) * dt, 1);
        velocity = Vectors.Subtract(velocity, Vectors.Scale(swing, settle));
        return ApplyRopeConstraint(ref state, input, velocity, ropeDirection, outward, distance, lengthScale);
    }

    // Pumping the swing (UpdateKick): steering along the swing while descending, or downhill at a turn or from hanging
    // still, adds speed along the steering.
    private static Vector3D Kick(ref RopeSwingState state, SwingSimulationInput input, Vector3D swingDirection, Vector3D gravityAcross,
        double tangentSpeed, double lengthScale)
    {
        Vector3D steer = Vectors.Normal(input.SteerDirection);
        Vector3D downhill = Vectors.Normal(gravityAcross);
        bool steerDownhill = Vectors.IsNearlyZero(downhill, 1e-4) || Vectors.Dot(downhill, steer) > 0;
        bool alongDescent = state.Phase == SwingPhase.Descending && Vectors.Dot(swingDirection, steer) > 0;
        double tilt = Vectors.Length(gravityAcross);
        bool kickStart = tilt < s_kickStartTiltSin && tangentSpeed < SwingParameters.PhaseLowSpeedThreshold;
        bool atTurn = kickStart || (state.Phase == SwingPhase.Reversal && steerDownhill);
        bool kicking = (atTurn || alongDescent) && Vectors.Dot(input.SteerDirection, input.SteerDirection) > 1e-8;
        state = state with { Kicking = kicking };
        if (!kicking)
        {
            return default;
        }

        double reference = Math.Max(SwingParameters.KickApexReferenceSpeed * tilt, kickStart ? SwingParameters.KickStartReferenceSpeed : 0);
        return Vectors.Scale(steer, reference * SwingParameters.KickAccelFraction * lengthScale * input.Seconds);
    }

    // The rope (ApplyRopeConstraint): the reel climbs or pays out, a stretched rope slips under load, pulls back with a
    // spring and removes outward speed, the speed along the rope changes by at most the catch deceleration, and the
    // character never moves towards the anchor faster than the reseat speed unless reeling.
    private static Vector3D ApplyRopeConstraint(ref RopeSwingState state, SwingSimulationInput input, Vector3D velocity, Vector3D ropeDirection,
        Vector3D outward, double distance, double lengthScale)
    {
        double dt = input.Seconds;
        double stretch = distance - input.FreeRopeLength;
        bool taut = stretch > 0;
        double outSpeed = Vectors.Dot(velocity, outward);
        (double stored, double reel) = ReelSpeed(state, input.ClimbHeld, input.RappelHeld, input.ClimbSpeedScale, input.TetherMaxLength, dt);
        state = state with { RopeOverloaded = stretch > SwingParameters.RopeBreakStretch, CurrentReelSpeed = stored };
        if (reel < 0)
        {
            state = AdvanceRopeLength(state, reel * dt, SwingParameters.RopeLengthMaximum);
            double inward = Math.Max(-reel, -outSpeed);
            velocity = Vectors.Add(Vectors.PlaneProject(velocity, outward), Vectors.Scale(ropeDirection, inward));
        }
        else
        {
            double payout = reel;
            double excess = outSpeed - reel;
            if (taut && excess > SwingParameters.RopeSlipLoadSpeed)
            {
                payout += excess - SwingParameters.RopeSlipLoadSpeed;
            }

            if (!(state.RopeLength < SwingParameters.RopeLengthMaximum && state.RopeLength < input.TetherMaxLength))
            {
                payout = 0;
            }

            state = AdvanceRopeLength(state, payout * dt, Math.Min(input.TetherMaxLength, SwingParameters.RopeLengthMaximum));
            if (taut || payout > 0)
            {
                velocity = Vectors.Add(Vectors.PlaneProject(velocity, outward), Vectors.Scale(outward, payout));
            }
            else
            {
                double allowed = -stretch / Math.Max(dt, 1e-8);
                double moving = Vectors.Dot(velocity, outward);
                if (moving > allowed)
                {
                    velocity = Vectors.Subtract(velocity, Vectors.Scale(outward, moving - allowed));
                }
            }
        }

        if (taut)
        {
            double stiffness = SwingParameters.RopeSpringStiffness / Math.Max(lengthScale, 1e-8);
            double outward2 = Vectors.Dot(velocity, outward);
            double pull = 1 - Math.Exp(-stiffness * dt);
            velocity = Vectors.Add(velocity, Vectors.Scale(ropeDirection, stretch * pull / Math.Max(dt, 1e-8)));
            if (outward2 > 0)
            {
                velocity = Vectors.Subtract(velocity, Vectors.Scale(outward, outward2 * SwingParameters.RopeSpringDamping));
            }

            double anchorAway = Vectors.Dot(input.AnchorVelocity, ropeDirection);
            if (anchorAway > 0)
            {
                double follow = Math.Clamp(anchorAway - Vectors.Dot(velocity, ropeDirection), 0, SwingParameters.MaxAnchorFollowAccel * dt);
                velocity = Vectors.Add(velocity, Vectors.Scale(ropeDirection, follow));
            }
        }

        double change = Vectors.Dot(velocity, outward) - outSpeed;
        double limit = SwingParameters.RopeCatchDecel * dt;
        if (Math.Abs(change) > limit)
        {
            velocity = Vectors.Add(velocity, Vectors.Scale(outward, Math.Clamp(change, -limit, limit) - change));
        }

        double inwardRelative = Vectors.Dot(velocity, ropeDirection) - Math.Max(Vectors.Dot(input.AnchorVelocity, ropeDirection), 0);
        if (reel == 0 && inwardRelative > SwingParameters.RopeReseatSpeed)
        {
            velocity = Vectors.Subtract(velocity, Vectors.Scale(ropeDirection, inwardRelative - SwingParameters.RopeReseatSpeed));
        }

        return velocity;
    }

    private static RopeSwingState AdvanceRopeLength(RopeSwingState state, double delta, double ceiling) =>
        state with { RopeLength = (float)Math.Min(state.RopeLength + delta, ceiling) };

    // The movement axes in world space: (throttle, strafe) turned by the control yaw.
    private static Vector3D Steer(RopeSwingInput input)
    {
        (double sin, double cos) = Math.SinCos(input.ControlYaw);
        return new Vector3D((input.Throttle * cos) - (input.Strafe * sin), (input.Throttle * sin) + (input.Strafe * cos), 0);
    }

    // The engine's clamped linear mapping of a value from one range to another.
    private static double MapClamped(double value, double inFrom, double inTo, double outFrom, double outTo)
    {
        double alpha = Math.Clamp((value - inFrom) / (inTo - inFrom), 0, 1);
        return outFrom + (alpha * (outTo - outFrom));
    }

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

    // The swing ends (EndSwing): detaching, the velocity kept, no turn, the finished state raised.
    private static RopeSwingStep End(RopeSwingState state, GrappleRope rope, Vector3D velocity, Vector3D feet, double seconds, RopeSwingExit exit,
        bool afterActionLayer) =>
        Result(state with { Phase = SwingPhase.Detaching }, rope, velocity, feet, seconds, null, SwingParameters.SwingingFlags, exit, true, afterActionLayer, 0, 0);

    private static RopeSwingStep Result(RopeSwingState state, GrappleRope rope, Vector3D velocity, Vector3D feet, double seconds, double? yaw, ulong flags,
        RopeSwingExit exit, bool finished, bool actionLayer, int crouchBlock, double stamina) =>
        new(state, rope, velocity, new Vector3D(feet.X + (velocity.X * seconds), feet.Y + (velocity.Y * seconds), feet.Z + (velocity.Z * seconds)),
            yaw, flags, exit, finished, actionLayer, actionLayer, crouchBlock, stamina);

    // The physics input of one tick (FSwingSimIn without the wall members).
    private readonly record struct SwingSimulationInput(
        double Seconds,
        Vector3D Location,
        Vector3D Velocity,
        Vector3D Pivot,
        Vector3D AnchorVelocity,
        double FreeRopeLength,
        double TetherMaxLength,
        Vector3D SteerDirection,
        Vector3D CameraForward,
        bool HasInput,
        bool ClimbHeld,
        double ClimbSpeedScale,
        bool RappelHeld);
}

file static class Vectors
{
    public static Vector3D Add(Vector3D a, Vector3D b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);

    public static Vector3D Subtract(Vector3D a, Vector3D b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);

    public static Vector3D Scale(Vector3D a, double factor) => new(a.X * factor, a.Y * factor, a.Z * factor);

    public static double Dot(Vector3D a, Vector3D b) => (a.X * b.X) + (a.Y * b.Y) + (a.Z * b.Z);

    public static double Length(Vector3D a) => Math.Sqrt(Dot(a, a));

    public static bool IsNearlyZero(Vector3D a, double tolerance) => Math.Abs(a.X) <= tolerance && Math.Abs(a.Y) <= tolerance && Math.Abs(a.Z) <= tolerance;

    // The part of a vector across a unit plane normal.
    public static Vector3D PlaneProject(Vector3D a, Vector3D normal) => Subtract(a, Scale(normal, Dot(a, normal)));

    // The engine's safe normal: zero for (nearly) zero vectors.
    public static Vector3D Normal(Vector3D a)
    {
        double square = Dot(a, a);
        return square > 1e-8 ? Scale(a, 1 / Math.Sqrt(square)) : default;
    }
}
