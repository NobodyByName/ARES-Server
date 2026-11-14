namespace Ares.Structs.Game.Movement;

/// <summary>Transient movement states the downed node raises.</summary>
public enum DownedTransientState : byte
{
    /// <summary>Unknown state; raised on every tick the floor below is not walkable.</summary>
    UnknownTransient0 = 0,

    /// <summary>Unknown state; raised, with the velocity zeroed, while the downed health is used up on walkable floor.</summary>
    UnknownTransient1 = 1,

    /// <summary>The character has stood up; ground movement takes over.</summary>
    Finished = 3,

    /// <summary>Unknown state; while crawling, raised when the ground normal is steeper than <see cref="DownedMovementParameters.SteepGroundNormalZ"/>.</summary>
    UnknownTransient9 = 9,
}

/// <summary>
/// The downed (down-but-not-out) movement node, <c>UPendulumDBNOMovementNodeData</c>. The node runs while the character has
/// <c>GameplayEffect.DBNOStatus</c>. It first holds the character for <see cref="IntoDownedSeconds"/>, then crawls it with
/// ground movement at <see cref="CrawlSpeed"/> along the control yaw, and once the status is gone holds it again for
/// <see cref="OutOfDownedSeconds"/> while it stands up. The crawl reads only the movement axes; the character faces the
/// control yaw on every tick and stands on the crouched capsule while the floor is walkable.
/// </summary>
public static class DownedMovementParameters
{
    /// <summary>Maximum crawl speed at full input, before the downed-movement skill multiplier.</summary>
    public const double CrawlSpeed = 100;

    /// <summary>Ground acceleration of the crawl (the template ground acceleration; the node does not change it).</summary>
    public const double Acceleration = 1000;

    /// <summary>Braking deceleration of the crawl.</summary>
    public const double BrakingDeceleration = 1000;

    /// <summary>Ground friction of the crawl.</summary>
    public const double Friction = 0.9;

    /// <summary>Multiplier applied to the friction while braking.</summary>
    public const double BrakingFrictionFactor = 3;

    /// <summary>Crawl speed multiplier without the downed-movement skill (<c>CharacterSkillData.DBNOMovementSpeedMultiplier</c>).</summary>
    public const double DefaultSpeedMultiplier = 1;

    /// <summary>
    /// Maximum speed while the node holds the character (going down, standing up, used-up downed health): the default of
    /// the movement modifications, which the node leaves unchanged on those ticks.
    /// </summary>
    public const double HeldSpeed = 0;

    /// <summary>Seconds from the node's start during which the character is held while it goes down.</summary>
    public const double IntoDownedSeconds = 1.5;

    /// <summary>Seconds the character is held while it stands up; the node finishes on the first tick past them.</summary>
    public const double OutOfDownedSeconds = 2;

    /// <summary>
    /// Distance from the feet to the ground above which the death node treats the character as falling; a downed
    /// character leaves the death node for the downed node only within it.
    /// </summary>
    public const double NearGroundDistance = 50;

    /// <summary>Ground normal height below which the crawl raises <see cref="DownedTransientState.UnknownTransient9"/>.</summary>
    public const double SteepGroundNormalZ = 0.75;

    /// <summary>Base speed the crawl's stick input is remapped against (<c>RemapStickInput</c>).</summary>
    public const double StickRemapBaseSpeed = 100;

    /// <summary>Stick input strengths below this are in walk range and remapped; stronger input becomes full input.</summary>
    public const double WalkStickInputThreshold = GroundMovementParameters.WalkInputStrengthThreshold;

    /// <summary>Remapped strength of the weakest walk-range input.</summary>
    public const double MinimumWalkInputStrength = 0.3;

    /// <summary>Speed the walk range remaps to, divided by <see cref="StickRemapBaseSpeed"/> for the strongest walk-range input.</summary>
    public const double WalkRunThreshold = GroundMovementParameters.WalkingModeSpeedThreshold;

    /// <summary>Ground slope, in degrees, above which the slope scales the crawl input.</summary>
    public const double SlopeStartDegrees = 15;

    /// <summary>Ground slope, in degrees, at which the slope reduction reaches <see cref="MaximumSlopeReduction"/>.</summary>
    public const double SlopeFullDegrees = 65;

    /// <summary>Largest share of the input the slope removes, for input straight uphill.</summary>
    public const double MaximumSlopeReduction = 0.35;

    /// <summary>Angle from straight uphill, in degrees, within which the input counts as fully uphill.</summary>
    public const double UphillFullDegrees = 50;

    /// <summary>Angle from straight uphill, in degrees, from which the input no longer counts as uphill.</summary>
    public const double UphillNoneDegrees = 90;

    /// <summary>Tags the downed node holds on every tick it runs.</summary>
    public const ulong NodeTags = GraphStateTags.AnimationPendulumStateDbno | GraphStateTags.AbilityBlockKnockbackPendulumMove
        | GraphStateTags.AbilityBlockFirePrimaryPendulumMove | GraphStateTags.AbilityBlockFireSecondaryPendulumMove
        | GraphStateTags.AbilityBlockReloadPendulumMove;

    /// <summary>Ground movement parameters of the crawl for a maximum speed.</summary>
    /// <param name="maximumSpeed">Maximum crawl speed.</param>
    /// <returns>The parameters.</returns>
    public static GroundMovementParameters Crawl(double maximumSpeed) =>
        new(maximumSpeed, Acceleration, BrakingDeceleration, Friction);
}

/// <summary>
/// The downed node's own blackboard variable: the ground-movement interface fields (slope input, maximum speed, jump
/// times, input directions) and the node's knockout state.
/// </summary>
public static class PawnDownedNodeState
{
    /// <summary>Descriptor of the downed-node variable.</summary>
    public const int DescriptorId = 47;

    /// <summary>Field holding the slope part of the crawl input.</summary>
    public const int SlopeInputScalarField = 0;

    /// <summary>Field holding the crawl's current maximum speed.</summary>
    public const int CurrentMaxMoveSpeedField = 1;

    /// <summary>Field holding the steering angle, in degrees from 0 to 360, kept while the character is knocked out.</summary>
    public const int SteeringAngleOnDeathField = 2;

    /// <summary>Field holding the node time of the first tick without the downed status, zero before.</summary>
    public const int OutOfDownedTimeField = 7;

    /// <summary>Field holding the node's flags, the knocked-out flag among them.</summary>
    public const int FlagsField = 8;

    /// <summary>Field holding the eight-way direction the crawl moves in (<see cref="InputDirection"/>).</summary>
    public const int ResultingInputDirectionField = 10;

    /// <summary>Field holding the eight-way direction of the player's movement input (<see cref="InputDirection"/>).</summary>
    public const int PlayerInputDirectionField = 11;

    /// <summary>Field holding the player knocking the character out.</summary>
    public const int KnockoutAttackerField = 12;
}
