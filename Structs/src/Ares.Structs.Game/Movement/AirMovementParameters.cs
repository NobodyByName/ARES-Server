namespace Ares.Structs.Game.Movement;

/// <summary>Movement of a character off the ground.</summary>
public static class AirMovementParameters
{
    /// <summary>Downward acceleration of a jumping or falling character, in world units per second squared.</summary>
    public const double FallAcceleration = 1964;

    /// <summary>Upward speed a jump starts with, in world units per second; it replaces the vertical speed.</summary>
    public const double JumpSpeed = 675;

    /// <summary>Seconds from a jump press on the ground to the take-off.</summary>
    public const double JumpDelaySeconds = 0.15;

    /// <summary>Acceleration towards the movement input, in world units per second squared.</summary>
    public const double Acceleration = 100;

    /// <summary>Constant deceleration above the maximum speed or without input.</summary>
    public const double BrakingDeceleration = 10;

    /// <summary>How fast the velocity turns towards the input, and the speed-proportional braking.</summary>
    public const double Friction = 0.2;

    /// <summary>Factor on <see cref="Friction"/> while braking.</summary>
    public const double BrakingFrictionFactor = 1;

    /// <summary>Air ticks after a jump's take-off that carry the jump ability tag.</summary>
    public const int JumpTagTicks = 4;

    /// <summary>Ticks from a jump press on the ground to the take-off.</summary>
    public static int JumpDelayTicks { get; } = (int)Math.Ceiling(JumpDelaySeconds / PawnBlackboard.TickSeconds);

    /// <summary>Horizontal movement parameters off the ground for a maximum speed.</summary>
    /// <param name="maximumSpeed">Maximum speed the character's movement allows.</param>
    /// <returns>The parameters.</returns>
    public static GroundMovementParameters Horizontal(double maximumSpeed) =>
        new(maximumSpeed, Acceleration, BrakingDeceleration, Friction, BrakingFrictionFactor);
}
