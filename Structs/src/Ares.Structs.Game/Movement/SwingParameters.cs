namespace Ares.Structs.Game.Movement;

/// <summary>Phase of a rope swing (<c>ESwingPhase</c>).</summary>
public enum SwingPhase : byte
{
    /// <summary>Hanging still under the anchor.</summary>
    Idle = 0,

    /// <summary>Turning around at the top of an arc.</summary>
    Reversal = 1,

    /// <summary>Swinging down towards the bottom of the arc.</summary>
    Descending = 2,

    /// <summary>Swinging up away from the bottom of the arc.</summary>
    Ascending = 3,

    /// <summary>Leaving the rope.</summary>
    Detaching = 4,
}

/// <summary>Where a swinging character is relative to a wall (<c>EWallState</c>).</summary>
public enum SwingWallState : byte
{
    /// <summary>No wall nearby.</summary>
    Off = 0,

    /// <summary>Swinging towards a wall.</summary>
    HeadingTowards = 1,

    /// <summary>Against a wall face.</summary>
    OnWall = 2,

    /// <summary>Standing on a face too steep to stand on without the rope.</summary>
    Standing = 3,

    /// <summary>Hitting a wall.</summary>
    Colliding = 4,
}

/// <summary>
/// Rope swing rules (<c>UPendulumSwingMovementNodeData</c> and the <c>SwingMovement</c> helpers): a character hanging
/// on a taut grappling-hook rope swings under its anchor, climbs or rappels the rope, kicks off walls and jumps off.
/// </summary>
public static class SwingParameters
{
    /// <summary>Factor on the fall acceleration at full swing speed.</summary>
    public const double GravityMultiplier = 1.2;

    /// <summary>Factor on the fall acceleration at the top of an arc (no swing speed).</summary>
    public const double ApexGravityMultiplier = 1;

    /// <summary>Swing speed at which the fall acceleration reaches <see cref="GravityMultiplier"/>.</summary>
    public const double GravityApexBlendSpeed = 200;

    /// <summary>Factor on the fall acceleration while against a wall face.</summary>
    public const double WallGravityMultiplier = 1.1;

    /// <summary>Quadratic drag on the swing speed, per centimetre.</summary>
    public const double QuadraticDragCoefficient = 9e-05;

    /// <summary>Factor on the drag while climbing the rope.</summary>
    public const double ClimbDragScale = 3;

    /// <summary>Factor on the drag on the shortest rope (<see cref="ClimbMinRopeLength"/>).</summary>
    public const double ShortRopeDragScale = 3;

    /// <summary>Factor on the drag on the shortest rope while against a wall face.</summary>
    public const double WallShortRopeDragScale = 6;

    /// <summary>Rope length from which the drag starts growing towards the short-rope factor.</summary>
    public const double ShortRopeDragLength = 250;

    /// <summary>Damping of the swing speed without steering input (or while climbing), per second on the reference rope.</summary>
    public const double NoInputSettleDamping = 0.2;

    /// <summary>Highest acceleration with which the character follows an anchor moving away.</summary>
    public const double MaxAnchorFollowAccel = 3000;

    /// <summary>Highest speed taken from a wrap point moving with the rope.</summary>
    public const double MaxWrapLocalPivotSpeed = 2000;

    /// <summary>Acceleration from steering input along the swing, on the reference rope.</summary>
    public const double LeanAccel = 110;

    /// <summary>Extra share of the steering acceleration while it points along the swing.</summary>
    public const double InPhaseLeanBonus = 0.5;

    /// <summary>Share of the steering acceleration removed while it points against the swing.</summary>
    public const double AntiPhaseLeanDamping = 0.8;

    /// <summary>Factor on the steering against the swing while against a wall face.</summary>
    public const double WallAntiPhaseLeanScale = -1.5;

    /// <summary>Reference speed of a pumping kick, per unit of the tangential share of gravity.</summary>
    public const double KickApexReferenceSpeed = 600;

    /// <summary>Share of the kick reference speed added per second on the reference rope.</summary>
    public const double KickAccelFraction = 0.5;

    /// <summary>Kick reference speed when starting to swing from hanging still.</summary>
    public const double KickStartReferenceSpeed = 400;

    /// <summary>Tilt of the rope, in degrees, under which a kick counts as starting a swing.</summary>
    public const double KickStartTiltDeg = 10;

    /// <summary>Swing speed under which the swing counts as slow (phase and kick start).</summary>
    public const double PhaseLowSpeedThreshold = 200;

    /// <summary>Swing speed under which a swing away from the bottom counts as turning around.</summary>
    public const double ReversalSpeedThreshold = 100;

    /// <summary>Horizontal distance from under the anchor within which the character may hang still.</summary>
    public const double MaxIdleHorizontalOffset = 85;

    /// <summary>Swing speed that ends hanging still.</summary>
    public const double IdleExitSpeed = 400;

    /// <summary>Horizontal distance from under the anchor that ends hanging still.</summary>
    public const double MaxIdleExitHorizontalOffset = 170;

    /// <summary>Factor on the idle distances while against a wall face.</summary>
    public const double FaceIdleBandScale = 3;

    /// <summary>Swing speed under which a character against a wall face hangs still.</summary>
    public const double FaceIdleSpeed = 10;

    /// <summary>Swing speed that ends hanging still against a wall face.</summary>
    public const double FaceIdleExitSpeed = 20;

    /// <summary>Alignment of the camera with the swing above which the swing direction is taken from it.</summary>
    public const double SwingDirectionLatchThreshold = 0.1;

    /// <summary>Stiffness of the rope's pull back to its length, per second on the reference rope.</summary>
    public const double RopeSpringStiffness = 10;

    /// <summary>Share of the outward speed a stretched rope removes.</summary>
    public const double RopeSpringDamping = 1;

    /// <summary>Highest speed towards the anchor the rope allows while not reeling.</summary>
    public const double RopeReseatSpeed = 500;

    /// <summary>Highest change of the speed along the rope, in centimetres per second squared.</summary>
    public const double RopeCatchDecel = 9000;

    /// <summary>Outward speed above which a taut rope slips (pays out) under the load.</summary>
    public const double RopeSlipLoadSpeed = 1450;

    /// <summary>Stretch beyond the rope length that overloads and breaks the rope.</summary>
    public const double RopeBreakStretch = 850;

    /// <summary>Shortest rope a climb reaches.</summary>
    public const double ClimbMinRopeLength = 50;

    /// <summary>Shortest rope the swing physics uses for its length scale.</summary>
    public const double MinRopeLength = 200;

    /// <summary>
    /// Free rope beyond <see cref="ClimbMinRopeLength"/> within which a climbing character tries to vault out at the top.
    /// </summary>
    public const double AutoVaultClimbReachThreshold = 10;

    /// <summary>Rope length up to which a vault out of the swing also releases the rope.</summary>
    public const double AutoVaultDetachRopeLength = 410;

    /// <summary>Rope length the swing's length scale is relative to.</summary>
    public const double ReferenceRopeLength = 1000;

    /// <summary>Rope length the rope starts at when the swing begins, at least.</summary>
    public const double MinEntryRopeLength = 150;

    /// <summary>Climbing speed up the rope.</summary>
    public const double ClimbSpeed = 100;

    /// <summary>Factor on the climbing speed while exhausted.</summary>
    public const double ExhaustedClimbSpeedScale = 0.75;

    /// <summary>Stamina per second climbing costs (server only).</summary>
    public const double ClimbStaminaCostPerSecond = 3;

    /// <summary>Stamina per second kicking costs (server only).</summary>
    public const double KickStaminaCostPerSecond = 7;

    /// <summary>Stamina the rope catching a falling character costs (server only).</summary>
    public const double RopeCatchStaminaCost = 15;

    /// <summary>Rappelling speed down the rope.</summary>
    public const double RappelSpeed = 150;

    /// <summary>Climbing speed up the rope while braced on a wall face.</summary>
    public const double WallClimbSpeed = 135;

    /// <summary>Rappelling speed while braced on a wall face.</summary>
    public const double WallDescendSpeed = 135;

    /// <summary>Rappelling speed while kicking off a wall.</summary>
    public const double WallRappelSpeed = 400;

    /// <summary>Acceleration of the reel towards the climbing speed.</summary>
    public const double ClimbReelAccel = 1200;

    /// <summary>Acceleration of the reel towards the rappelling speed.</summary>
    public const double RappelReelAccel = 1500;

    /// <summary>Deceleration of the reel down to the rappelling speed.</summary>
    public const double RappelReelDecel = 100;

    /// <summary>Seconds crouch input stays blocked after rappelling.</summary>
    public const double RappelCrouchBlockTime = 0.3;

    /// <summary>Acceleration of the reel back to zero when neither climbing nor rappelling.</summary>
    public const double ReelReleaseAccel = 2400;

    /// <summary>Reel speeds under this count as zero.</summary>
    public const double ReelSpeedDeadzone = 1;

    /// <summary>Angle, in degrees, beyond which a surface counts as a face to hang against (from the vertical normal).</summary>
    public const double FaceEnterAngleDeg = 44.99;

    /// <summary>Angle, in degrees, under which a face counts as ground again.</summary>
    public const double FaceExitAngleDeg = 42.99;

    /// <summary>Angle, in degrees, beyond which a face is steep enough to kick off.</summary>
    public const double KickFaceAngleDeg = 65;

    /// <summary>Distance to the ground within which the character counts as touching it.</summary>
    public const double GroundContactDistance = 50;

    /// <summary>Slack added to the rope when the character lands.</summary>
    public const double LandingRopeSlack = 30;

    /// <summary>Speed of a wall kick away from the face.</summary>
    public const double WallKickSpeed = 300;

    /// <summary>Alignment of the input with the face normal needed for a wall kick.</summary>
    public const double WallKickInputAlignment = 0.77;

    /// <summary>Seconds after a kick input starts before the kick applies.</summary>
    public const double WallKickDelay = 0.2;

    /// <summary>Seconds a wall kick lasts.</summary>
    public const double WallKickDuration = 0.5;

    /// <summary>Highest speed along a wall face.</summary>
    public const double WallRunMaxSpeed = 700;

    /// <summary>Acceleration along a wall face from steering input.</summary>
    public const double WallRunAccel = 175;

    /// <summary>Gap a character keeps from a wall face.</summary>
    public const double WallStandoffDistance = 5;

    /// <summary>Rate at which the gap to a wall face is restored, per second.</summary>
    public const double WallStandoffRecoverRate = 10;

    /// <summary>Wall gap meaning "no face".</summary>
    public const double NoFaceGap = 100000;

    /// <summary>Horizontal speed of a jump off the rope along the movement input.</summary>
    public const double JumpForce = 200;

    /// <summary>Upward speed a jump off the rope adds.</summary>
    public const double JumpUpwardForce = 500;

    /// <summary>Upward speed a climb started from the ground jumps with, at least.</summary>
    public const double GroundClimbEntryJumpZ = 450;

    /// <summary>Seconds a climb started from the ground keeps climbing without input.</summary>
    public const double GroundClimbEntryForcedClimbTime = 0.5;

    /// <summary>Angle, in degrees, the rope must rise above the horizontal for the character to swing.</summary>
    public const double MinAnchorAngleAboveHorizontalDeg = 20;

    /// <summary>Angle, in degrees, the rope must rise above the horizontal to start a swing standing on the ground.</summary>
    public const double MinAnchorAngleAboveHorizontalDegGround = 80;

    /// <summary>Clearance the lowest point of the swing arc must keep from the ground for a falling character to swing.</summary>
    public const double MinSwingArcGroundClearance = 50;

    /// <summary>Rope end reason when the character lets go (jump, vault release).</summary>
    public const byte ReleasedEndReason = 1;

    /// <summary>Rope end reason when the rope breaks (overloaded or overstretched).</summary>
    public const byte BrokenEndReason = 2;

    /// <summary>Transient state the swing raises when it ends (the graph returns to ground movement).</summary>
    public const int FinishedTransientState = 3;

    /// <summary>Transient state the swing raises on every tick it runs to the end.</summary>
    public const int UnknownSwingTransientState = 8;

    /// <summary>Graph state flags while the swing runs: equipping another item is blocked.</summary>
    public const ulong SwingingFlags = GraphStateTags.AbilityBlockEquipItemPendulumMove;

    /// <summary>Graph state flag raised while climbing, rappelling or kicking: stamina does not recharge.</summary>
    public const ulong StaminaBlockFlag = GraphStateTags.AttributeStaminaBlockRechargePendulumMove;

    /// <summary>Graph state flag raised on the tick of a jump off the rope.</summary>
    public const ulong JumpStartedFlag = GraphStateTags.AnimationAbilityJumpStarted;

    /// <summary>Action-layer stance while swinging.</summary>
    public const CharacterStance ActionStance = CharacterStance.Uncontrolled;

    /// <summary>Action-layer locomotion while swinging.</summary>
    public const LocoMode ActionLocomotion = LocoMode.Running;

    /// <summary>Movement mode while swinging.</summary>
    public const RoughMovementMode MovementMode = RoughMovementMode.Air;

    /// <summary>Lowest value of the node's quantized reel speed.</summary>
    public const double ReelSpeedMinimum = -500;

    /// <summary>Highest value of the node's quantized reel speed.</summary>
    public const double ReelSpeedMaximum = 500;

    /// <summary>Bits of the node's quantized reel speed.</summary>
    public const int ReelSpeedBits = 11;

    /// <summary>Highest value of the node's quantized rope length.</summary>
    public const double RopeLengthMaximum = 4000;

    /// <summary>Bits of the node's quantized rope length.</summary>
    public const int RopeLengthBits = 13;
}
