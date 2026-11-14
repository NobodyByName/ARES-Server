namespace Ares.Structs.Game.Movement;

/// <summary>
/// How a character's feet find and follow the ground. The character stands on a plane fitted to ground probes around
/// it, so it walks over anything lower than its knees, and its height settles on that plane while it moves.
/// </summary>
public static class GroundContactParameters
{
    /// <summary>
    /// Radius of the pill that carries and blocks a moving character, narrower than its capsule: walls stop the
    /// character's axis this far away, and the outer ground probes are this far from it.
    /// </summary>
    public const double PillRadius = 30;

    /// <summary>Height above the feet where the pill starts: ground up to here is stepped onto, and only what is higher blocks.</summary>
    public const double KneeHeight = 40;

    /// <summary>Height of a step the character steps up onto.</summary>
    public const double StepUpHeight = 50;

    /// <summary>Highest step the character can step up onto.</summary>
    public const double MaximumStepUpHeight = 70;

    /// <summary>Half height of the character's capsule, caps included, while it crouches.</summary>
    public const double CrouchedHalfHeight = 60;

    /// <summary>Depth below the feet within which ground carries the character.</summary>
    public const double GroundReach = 100;

    /// <summary>Lowest height of the feet above the ground a character's state can hold: this far under it.</summary>
    public const double GroundDistanceMinimum = -100;

    /// <summary>Greatest height of the feet above the ground a character's state can hold.</summary>
    public const double GroundDistanceMaximum = 2000;

    /// <summary>Smallest vertical component of the ground plane's unit normal a character can stand on (about 45 degrees).</summary>
    public const double WalkableNormalZ = 0.70723;

    /// <summary>
    /// Time a moving character's feet take to settle on the ground: each tick they advance along the smoothest path
    /// that brings their height and vertical speed to rest on the ground after this long.
    /// </summary>
    public const double SettleSeconds = 0.15;

    /// <summary>Angular frequency of the spring holding the feet from a landing until they have settled, in radians per second.</summary>
    public const double LandingFrequency = 18.5;

    /// <summary>Share of the vertical speed a jump keeps when its landing reaches the ground.</summary>
    public const double JumpLandingSpeedShare = 0.58;

    /// <summary>Distance from the ground and vertical speed below which a landing has settled.</summary>
    public const double SettledDistance = 0.5;

    /// <summary>
    /// Depth below the feet within which a walking character keeps the ground on its next tick; ground lower than this
    /// leaves it falling.
    /// </summary>
    public const double StepDownReach = 30;

    /// <summary>Height above the ground within which a falling character, or one placed by a re-seed or a mechanic, stands on it.</summary>
    public const double StandingDistance = 20;

    /// <summary>Longest a moving character's landing settles before its feet follow the ground like a walking character's.</summary>
    public const double LandingSettleSeconds = 0.2;

    /// <summary>See <see cref="SettledDistance"/>.</summary>
    public const double SettledSpeed = 15;
}
