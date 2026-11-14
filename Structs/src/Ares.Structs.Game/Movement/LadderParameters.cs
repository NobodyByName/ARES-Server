using Ares.Structs.Core.Geometry;

namespace Ares.Structs.Game.Movement;

/// <summary>
/// Ladder climbing rules (<c>UPendulumClimbLadderNodeData</c> and the <c>AClimbableLadder</c> it climbs). The climb is
/// procedural: the node sets a velocity each tick from the input and the character's place on the climbing line.
/// </summary>
public static class LadderParameters
{
    /// <summary>Climbing speed up (<c>BaseMaxClimbUpSpeed</c>), in centimetres per second.</summary>
    public const double ClimbUpSpeed = 140;

    /// <summary>Climbing speed up while sprinting and not exhausted (<c>MaximumSprintUpSpeed</c>).</summary>
    public const double SprintUpSpeed = 170;

    /// <summary>Climbing speed down (<c>BaseMaxClimbDownSpeed</c>).</summary>
    public const double ClimbDownSpeed = 155;

    /// <summary>Sliding speed down while sprint is held (<c>MaximumSlideDownSpeed</c>); also the downward speed of a fast fall.</summary>
    public const double SlideDownSpeed = 500;

    /// <summary>Stamina cost of sprinting up per application of the drain effect (<c>BaseSprintStaminaCostPerApplicationPeriod</c>).</summary>
    public const double SprintStaminaCost = 6;

    /// <summary>Share of <see cref="SprintStaminaCost"/> the drain effect is given as its magnitude.</summary>
    public const double SprintStaminaShare = 0.2;

    /// <summary>Vertical acceleration towards the climbing speed, in centimetres per second squared.</summary>
    public const double VerticalAcceleration = 500;

    /// <summary>Vertical acceleration while falling faster than <see cref="SlideDownSpeed"/>.</summary>
    public const double FastVerticalAcceleration = 5000;

    /// <summary>Highest upward speed the vertical acceleration starts from.</summary>
    public const double StartSpeedLimit = SprintUpSpeed;

    /// <summary>Climbable ladder rung spacing (<c>AClimbableLadder.RungLength</c>).</summary>
    public const double RungLength = 33;

    /// <summary>Rungs below the climb height where climbing up leaves over the top (<c>AClimbableLadder.RungsForTopExit</c>).</summary>
    public const int RungsForTopExit = 3;

    /// <summary>Whether climbing up past the top exit height leaves over the top (<c>bAllowAutoClimbOffTop</c>); otherwise the climb stops there.</summary>
    public const bool AutoClimbOffTop = true;

    /// <summary>Height above the ladder's origin where the climbing line starts (<c>ATraversableLine.LocalActorStartAttachOffset</c>).</summary>
    public const double LineStartHeight = 33;

    /// <summary>Position of the climbing line along the ladder's own forward axis, relative to the ladder's origin.</summary>
    public const double LineForwardOffset = -55;

    /// <summary>Seconds after getting on during which the character is pulled onto the line and cannot jump off.</summary>
    public const double AlignmentSeconds = 0.5;

    /// <summary>Share of the remaining distance to the line covered per tick while aligning.</summary>
    public const double AlignmentShare = 0.3;

    /// <summary>Extra height above the line start the character is pulled to while aligning.</summary>
    public const double AlignmentLift = 10;

    /// <summary>Seconds over which a falling character's vertical speed would fade out while aligning.</summary>
    public const double CatchSlowdownSeconds = 4;

    /// <summary>Square of the horizontal distance from the line beyond which the character falls off.</summary>
    public const double FallOffDistanceSquared = 3500;

    /// <summary>Depth below the capsule centre searched for a floor when the bottom of the line is reached.</summary>
    public const double FloorProbeDepth = 400;

    /// <summary>Crouch input stays blocked this long after each climbing tick, in seconds.</summary>
    public const double CrouchBlockSeconds = 0.2;

    /// <summary>Fall acceleration of a fast catch that leaves the ladder at its bottom.</summary>
    public const double FallAcceleration = 1964;

    /// <summary>Standing capsule half height the jump-off start subtracts from the capsule centre.</summary>
    public const double JumpOffHalfHeight = 92;

    /// <summary>Start of a jump off the ladder relative to the feet, in the character's frame (forward, right, up).</summary>
    public static Vector3D JumpOffStartOffset { get; } = new(70, 0, 150);

    /// <summary>Distance in front of the capsule centre where climbing off the top ends, before it is put on the floor.</summary>
    public const double ClimbOffTopReach = 99;

    /// <summary>Height above and below the climb-off end searched for the floor.</summary>
    public const double ClimbOffTopProbe = 150;

    /// <summary>Seconds a top entry plays its entry animation before climbing.</summary>
    public const double TopEntrySeconds = 1;

    /// <summary>Name of the ladder's interaction component that gets the character on from the top.</summary>
    public const string TopEntryInteraction = "TopEntryInteraction";

    /// <summary>Height below the feet, besides <see cref="RungLength"/>, where a top entry animation is placed.</summary>
    public const double TopEntryDrop = 187;

    /// <summary>Bit of the character claims flags (<see cref="PendulumInput.CharacterClaimsFlagsField"/>) set while crouch is held.</summary>
    public const int CrouchButtonDownBit = 5;

    /// <summary>
    /// Graph state flags of the ladder state: fire primary, fire secondary, equip, reload and context interaction
    /// blocked, and the attached-to-a-ladder tag.
    /// </summary>
    public const ulong ClimbingFlags = GraphStateFlags.BlockedActions | GraphStateTags.AbilityBlockContextInteractPendulumMove
        | GraphStateTags.GameplayEffectAttachedTraversalLadder;

    /// <summary>Graph state flag set while crouch input is blocked ; each climbing tick renews it for <see cref="CrouchBlockSeconds"/>.</summary>
    public const ulong CrouchInputBlockedFlag = GraphStateTags.AbilityBlockInputCrouchPendulumMove;

    /// <summary>Graph state flag of the ladder slide cue sliding down, or catching the ladder while falling.</summary>
    public const ulong SlidingFlag = GraphStateTags.GameplayCueAbilityLadderSlide;

    /// <summary>Graph state flag of an active sprint ; on a ladder it follows the sprint button.</summary>
    public const ulong SprintActiveFlag = GraphStateTags.AbilitySprintActive;

    /// <summary>Stamina change per application of the sprint drain effect (skills and flow state unchanged).</summary>
    public static double SprintStaminaMagnitude => -SprintStaminaCost * SprintStaminaShare;

    /// <summary>Climbing height of the line's top, above its start, where climbing up leaves over the top.</summary>
    /// <param name="ladderHeight">The ladder's replicated climb height.</param>
    /// <returns>The height, in centimetres.</returns>
    public static double TopExitHeight(double ladderHeight) => ladderHeight - (RungLength * RungsForTopExit);
}
