namespace Ares.Structs.Game.Movement;

/// <summary>
/// Zipline riding rules (<c>UPendulumRideZiplineNodeData</c> and the <c>AZippableLine</c> it rides). The ride is
/// procedural: each tick the node moves the character's hands along the line and pulls them onto it.
/// </summary>
public static class ZiplineParameters
{
    /// <summary>Highest speed along the line the acceleration adds up to (<c>MaximumSpeed</c> of a zippable line).</summary>
    public const double MaximumSpeed = 3500;

    /// <summary>Acceleration along the line while moving level or upwards (<c>Acceleration</c> of a zippable line).</summary>
    public const double Acceleration = 200;

    /// <summary>Acceleration along the line while moving straight down (<c>MaxAcceleration</c> of a zippable line).</summary>
    public const double MaxAcceleration = 750;

    /// <summary>Pitch of the velocity, in degrees, at which <see cref="MaxAcceleration"/> applies; level gives <see cref="Acceleration"/>.</summary>
    public const double MaxAccelerationPitch = -90;

    /// <summary>Highest speed a rider keeps when leaving at the end of the line (<c>MaxEndLaunchVelocity</c>).</summary>
    public const double MaxEndLaunchVelocity = 800;

    /// <summary>
    /// Distance from the line start within which a character attaching on the start half rides towards the end
    /// (<c>ForcedTravelDirectionThresholdAway</c>).
    /// </summary>
    public const double ForcedTravelDirectionThresholdAway = 200;

    /// <summary>Horizontal speed a jump off the line adds along the input direction (<c>HorizontalJumpOffForce</c>).</summary>
    public const double HorizontalJumpOffForce = 450;

    /// <summary>Upward speed a jump off the line reaches, less any upward speed the rider already has (<c>VerticalJumpOffForce</c>).</summary>
    public const double VerticalJumpOffForce = 500;

    /// <summary>Seconds after attaching during which the rider is caught onto the line and cannot jump or take exits.</summary>
    public const double EntrySeconds = 0.5;

    /// <summary>Share of the hands' distance from the line covered per tick while pulling them onto it.</summary>
    public const double CentringShare = 0.2;

    /// <summary>Distance beyond either line end the hands may be before they are pulled back.</summary>
    public const double EndMargin = 10;

    /// <summary>Share of the speed along the line compared with the overshoot when the first tick decides whether to help the entry.</summary>
    public const double EntryHelpSpeedShare = 0.5;

    /// <summary>Rate at which the rider's own velocity fades while caught onto the line, in centimetres per second squared.</summary>
    public const double EntryDeceleration = 500;

    /// <summary>Further rate at which an upward speed fades while caught onto the line.</summary>
    public const double EntryRiseDeceleration = 300;

    /// <summary>Downward speed beyond which a line flatter than <see cref="CatchMaxPitch"/> cannot catch the rider.</summary>
    public const double CatchFallSpeed = -2500;

    /// <summary>Steepest line pitch, in degrees, that lets a fast fall through.</summary>
    public const double CatchMaxPitch = 75;

    /// <summary>Rate of the fade towards zero of the vertical speed of a rider the line cannot catch, per second.</summary>
    public const float FallingFadeRate = 5;

    /// <summary>Downward speed of a rider the line cannot catch below which the ride has not ended yet.</summary>
    public const double FallingExitSpeed = -1500;

    /// <summary>Rate at which the speed along the line brakes while the acceleration opposes it.</summary>
    public const double BrakeDeceleration = 1000;

    /// <summary>Speed along the line above which an opposing acceleration brakes it.</summary>
    public const double BrakeMinimumSpeed = 100;

    /// <summary>Speed change per tick above which an opposing acceleration brakes the speed along the line.</summary>
    public const double BrakeMinimumChange = 0.1;

    /// <summary>Distance to a walkable floor below which a rider being caught onto the line does not move down.</summary>
    public const double EntryFloorDistance = 50;

    /// <summary>Distance added to the attach interaction's prompt distance within which the ride can start.</summary>
    public const double PromptDistanceMargin = 70;

    /// <summary>Bit of the character claims flags (<see cref="PendulumInput.CharacterClaimsFlagsField"/>) set while electrified.</summary>
    public const int ElectrifiedBit = 3;

    /// <summary>Bit of the character claims flags set while interactions are soft-blocked.</summary>
    public const int SoftBlockInteractionsBit = 4;

    /// <summary>Transient state the ride raises every tick.</summary>
    public const int RidingTransientState = 0;

    /// <summary>Transient state the ride raises when it ends (the graph's default edge back to ground movement).</summary>
    public const int FinishedTransientState = 3;

    /// <summary>Graph state flags of the ride: reload and context interaction blocked, and the attached-to-a-zipline tag.</summary>
    public const ulong RidingFlags = GraphStateTags.AbilityBlockReloadPendulumMove | GraphStateTags.AbilityBlockContextInteractPendulumMove
        | GraphStateTags.GameplayEffectAttachedTraversalZipline;

    /// <summary>Action-layer stance while riding.</summary>
    public const CharacterStance ActionStance = CharacterStance.Uncontrolled;

    /// <summary>Action-layer locomotion while riding.</summary>
    public const LocoMode ActionLocomotion = LocoMode.Still;

    /// <summary>Movement mode while riding.</summary>
    public const RoughMovementMode MovementMode = RoughMovementMode.Air;
}
