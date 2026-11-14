namespace Ares.Structs.Game.Movement;

/// <summary>Transient movement states a heavy-landing animation raises; each one hands over to ground movement.</summary>
public enum HeavyLandingTransientState : byte
{
    /// <summary>The landing is over; ground movement takes over with <see cref="HeavyLandingParameters.FinishedBlendSeconds"/>.</summary>
    Finished = 3,

    /// <summary>The landing ends early; ground movement takes over with <see cref="HeavyLandingParameters.QuickExitBlendSeconds"/>.</summary>
    QuickExit = 4,
}

/// <summary>A span of a heavy-landing animation during which it raises a transient state.</summary>
/// <param name="State">The raised state.</param>
/// <param name="Start">Start, in animation seconds.</param>
/// <param name="End">End, in animation seconds.</param>
public readonly record struct HeavyLandingWindow(HeavyLandingTransientState State, float Start, float End);

/// <summary>Root position of a heavy-landing animation at one frame, relative to the character's facing.</summary>
/// <param name="Right">Distance to the character's right, in world units.</param>
/// <param name="Forward">Distance ahead of the character, in world units.</param>
public readonly record struct HeavyLandingRootPoint(float Right, float Forward);

/// <summary>One heavy-landing animation.</summary>
/// <param name="Name">Animation name.</param>
/// <param name="SampleRate">Root frames per animation second.</param>
/// <param name="Root">Horizontal root position at each frame; empty when the animation does not move the character.</param>
/// <param name="Windows">Transient-state spans of the animation.</param>
public sealed record HeavyLandingClip(string Name, double SampleRate, IReadOnlyList<HeavyLandingRootPoint> Root, IReadOnlyList<HeavyLandingWindow> Windows);

/// <summary>
/// One landing level: the animations played by the direction of the horizontal impact velocity relative to the
/// character's heading. Diagonal directions use the side animation; a missing animation falls back to
/// <paramref name="NoDirection"/>.
/// </summary>
/// <param name="Height">Fall height this level starts above, in world units.</param>
/// <param name="NoDirection">Animation without a horizontal impact direction.</param>
/// <param name="Forward">Animation for a forward impact.</param>
/// <param name="Left">Animation for a left impact, forward-left and backward-left included.</param>
/// <param name="Right">Animation for a right impact, forward-right and backward-right included.</param>
/// <param name="Backward">Animation for a backward impact.</param>
public sealed record HeavyLandingLevel(
    double Height,
    HeavyLandingClip NoDirection,
    HeavyLandingClip? Forward,
    HeavyLandingClip? Left,
    HeavyLandingClip? Right,
    HeavyLandingClip? Backward);

/// <summary>
/// When a landing is heavy and how long its recovery holds the character. Just before touching down, the game turns
/// the fall into the height it is worth: the distance left to the ground, half a tick of the vertical speed and the
/// height a fall needs to reach that speed (<c>vz² / 2g</c>). Above <see cref="Height"/> the character lands heavily.
/// </summary>
public static class HeavyLandingParameters
{
    /// <summary>Fall height above which a landing is heavy, in world units.</summary>
    public const double Height = 600;

    /// <summary>Fall height from which a heavy landing can no longer be softened by a recovery roll, in world units.</summary>
    public const double RecoveryRollMaximumHeight = 1000;

    /// <summary>
    /// Seconds before the landing within which a dodge-roll or crouch press asks for a recovery roll. A press counts only
    /// when the previously counted one is older than <see cref="RecoveryRollPressRepeatSeconds"/>.
    /// </summary>
    public const double RecoveryRollPressSeconds = 0.5;

    /// <summary>Seconds after a counted recovery-roll press before another press counts.</summary>
    public const double RecoveryRollPressRepeatSeconds = 2;

    /// <summary>
    /// A falling character is checked for a heavy landing when the ground below its feet is nearer than this plus the
    /// distance it falls in the tick, in world units.
    /// </summary>
    public const double DetectionDistance = 40;

    /// <summary>Vertical speed below which a fall can end in a heavy landing (negative is down).</summary>
    public const double DetectionVerticalSpeed = -100;

    /// <summary>Off the ground and falling faster than this (negative is down), the character leaves the landing at once.</summary>
    public const double FallingExitVerticalSpeed = -1450;

    /// <summary>Squared horizontal impact speed below which the landing has no direction.</summary>
    public const double DirectionTolerance = 1e-8f;

    /// <summary>Squared speed above which a recovery roll is played along the velocity instead of the heading.</summary>
    public const double RecoveryHeadingSpeedSquared = 0.1;

    /// <summary>With the fall-damage armor modifier, the height that picks the landing level is this much lower.</summary>
    public const double ArmorModifierHeightReduction = 1000;

    /// <summary>Seconds of the blend from the movement a heavy landing interrupts into the landing.</summary>
    public const double EntryBlendSeconds = 0.1;

    /// <summary>Seconds of the blend into ground movement after <see cref="HeavyLandingTransientState.Finished"/>.</summary>
    public const double FinishedBlendSeconds = 0.5;

    /// <summary>Seconds of the blend into ground movement after <see cref="HeavyLandingTransientState.QuickExit"/>.</summary>
    public const double QuickExitBlendSeconds = 0.1;

    /// <summary>Step the fall height adds for the vertical speed of the touching tick, in seconds (half of it is used).</summary>
    public const double ImpactSeconds = 0.033;

    /// <summary>The fall height is rounded to this many steps per world unit.</summary>
    public const double HeightResolution = 100;

    /// <summary>Ticks the recovery from a heavy landing holds the character.</summary>
    public const int RecoveryTicks = 30;

    /// <summary>Stance of the action layer during the recovery.</summary>
    public const CharacterStance ActionStance = CharacterStance.Uncontrolled;

    /// <summary>Locomotion of the action layer during the recovery.</summary>
    public const LocoMode ActionLocomotion = LocoMode.Still;

    /// <summary>The fall height of a landing.</summary>
    /// <param name="distance">Distance from the feet to the ground before the touching tick.</param>
    /// <param name="vertical">Vertical speed before the touching tick (negative is down).</param>
    /// <param name="fallAcceleration">Magnitude of the fall acceleration.</param>
    /// <returns>The height, rounded like the game rounds it.</returns>
    public static double FallHeight(double distance, double vertical, double fallAcceleration)
    {
        double height = Math.Max(0, distance) - (vertical * 0.5 * ImpactSeconds) + (vertical * vertical / (2 * fallAcceleration));
        return Math.Round(height * HeightResolution) / HeightResolution;
    }
}
