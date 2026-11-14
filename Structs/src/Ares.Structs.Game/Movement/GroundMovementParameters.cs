namespace Ares.Structs.Game.Movement;

/// <summary>Ground acceleration and braking parameters.</summary>
/// <param name="MaximumSpeed">Maximum world speed at full input.</param>
/// <param name="Acceleration">World acceleration at full input.</param>
/// <param name="BrakingDeceleration">Constant braking deceleration.</param>
/// <param name="Friction">Steering and ground friction.</param>
/// <param name="BrakingFrictionFactor">Multiplier applied to friction during braking.</param>
public readonly record struct GroundMovementParameters(
    double MaximumSpeed,
    double Acceleration = 750,
    double BrakingDeceleration = 50,
    double Friction = 2,
    double BrakingFrictionFactor = 3)
{
    /// <summary>Running with an equipped weapon.</summary>
    public static GroundMovementParameters Armed { get; } = new(300);

    /// <summary>Running with empty hands.</summary>
    public static GroundMovementParameters Unarmed { get; } = new(375);

    /// <summary>Crouch movement.</summary>
    public static GroundMovementParameters Crouched { get; } = new(150);

    /// <summary>Slow walking.</summary>
    public static GroundMovementParameters Walking { get; } = new(200);

    /// <summary>
    /// Movement while the held item forces a walk (<see cref="HeldItemMovementParameters"/>), standing or crouched;
    /// the <c>ADSSpeedModifier</c> attribute scales it.
    /// </summary>
    public static GroundMovementParameters Aiming { get; } = new(150);

    /// <summary>Initial crouch movement speed multiplier.</summary>
    public const double CrouchShuffleSpeedFactor = 0.5;

    /// <summary>Seconds of initial crouch movement at shuffle speed.</summary>
    public const double CrouchShuffleSeconds = 0.45;

    /// <summary>Input strengths below this threshold remain within walk range.</summary>
    public const double WalkInputStrengthThreshold = 0.75;

    /// <summary>Speed below which nonzero expected movement is classified as walking.</summary>
    public const double WalkingModeSpeedThreshold = 250;

    /// <summary>Input strength near one is treated as full input for expected-speed classification.</summary>
    public const double InputStrengthSnapTolerance = 0.01;

    /// <summary>Speed treated as zero for locomotion classification.</summary>
    public const double NearlyZeroSpeed = 0.00000001;

    /// <summary>Relative margin before a velocity is considered above maximum speed.</summary>
    public const double OverspeedTolerance = 1.01;

    /// <summary>Speed below which braking with constant deceleration stops the character.</summary>
    public const double BrakingStopSpeed = 10;

    /// <summary>Squared speed treated as zero during braking.</summary>
    public const double NearlyZeroSquaredSpeed = 0.0001;
}
