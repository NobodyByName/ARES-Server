namespace Ares.Structs.Game.Movement;

/// <summary>Slide speed and surface friction modifiers.</summary>
/// <param name="MaximumSpeed">Maximum slide speed in world units per second.</param>
/// <param name="FrictionMultiplier">Combined surface and ability friction multiplier.</param>
public readonly record struct SlideMovementParameters(double MaximumSpeed, double FrictionMultiplier = 1)
{
    /// <summary>Slide parameters on a surface with no friction modifier.</summary>
    public static SlideMovementParameters Default { get; } = new(1300);

    /// <summary>Minimum horizontal speed to enter a slide before movement speed modifiers.</summary>
    public const double EntrySpeed = 200;

    /// <summary>Minimum sum of absolute movement axes to enter a slide.</summary>
    public const double EntryInputThreshold = 0.1;

    /// <summary>Velocity-relative forward input below which a slide requests cancellation.</summary>
    public const double CancelForwardThreshold = -0.7f;

    /// <summary>Seconds over which the initial directional boost falls to zero.</summary>
    public const double BoostSeconds = 1.5;

    /// <summary>Fraction of normalized velocity retained as slide input momentum.</summary>
    public const double MomentumFactor = 0.5;

    /// <summary>Steepness scale above which accumulated slowdown decreases.</summary>
    public const double SlowdownRecoveryThreshold = 0.85;

    /// <summary>Rate at which accumulated slowdown decreases on steep slopes.</summary>
    public const double SlowdownRecoveryRate = -0.15f;

    /// <summary>Base rate of accumulated slowdown.</summary>
    public const double SlowdownRate = 0.075;

    /// <summary>Lower bound on speed used to scale accumulated slowdown.</summary>
    public const double SlowdownMinimumSpeed = 100;

    /// <summary>Downward slope angle at which slope-dependent slide input begins.</summary>
    public const double SlopeStartDegrees = -10;

    /// <summary>Downward slope angle at which slope-dependent slide input is fully applied.</summary>
    public const double SlopeFullDegrees = -45;

    /// <summary>Minimum angle allowed for unrestricted slide steering.</summary>
    public const double FreeMoveMinimumDegrees = 25;

    /// <summary>Maximum angle allowed for unrestricted slide steering.</summary>
    public const double FreeMoveMaximumDegrees = 65;

    /// <summary>Alignment angle at the edge of slope-scaled slide input.</summary>
    public const double SlopeAlignmentDegrees = 90;

    /// <summary>Velocity-relative input applied by full slope braking.</summary>
    public const double SlopeBrakingInput = -0.8f;

    /// <summary>Scale applied to the downhill input contribution.</summary>
    public const double SlopeNudgeFactor = 0.7;

    /// <summary>Squared horizontal length below which a direction is treated as zero.</summary>
    public const double DirectionTolerance = 1e-8f;

    /// <summary>Ground acceleration, braking and friction while sliding.</summary>
    /// <remarks>Steering friction is 0.15; braking friction is 0.9 (friction times the braking factor).</remarks>
    public static GroundMovementParameters Ground { get; } = new(Default.MaximumSpeed, 1000, 100, 0.15f, 6);

    /// <summary>Ground braking and friction while slowing to leave a slide.</summary>
    public static GroundMovementParameters CancellingGround { get; } = Ground with { BrakingDeceleration = 200, Friction = 1, BrakingFrictionFactor = 3 };

    /// <summary>Fraction of speed kept when the stance changes to crouched on the second slide tick.</summary>
    public const double CrouchStanceSpeedFactor = 14.0 / 15;

    /// <summary>Ticks after a slide ends over which its braking blends into the following movement.</summary>
    public const int ExitBlendTicks = 3;

    /// <summary>Minimum elapsed slide time before ordinary input cancellation.</summary>
    public const double MinimumCancelSeconds = 1.2;

    /// <summary>Allowance for rounding in summed step times when comparing slide time.</summary>
    public const double ElapsedTolerance = 1e-6;

    /// <summary>Speed at or below which a cancelling slide ends immediately.</summary>
    public const double CancelSpeed = 412.5;

    /// <summary>Minimum velocity-relative forward input for full slide input strength.</summary>
    public const double ForwardInputThreshold = 0.2;

    /// <summary>Input magnitude limit when movement is not sufficiently forward.</summary>
    public const double SidewaysInputLimit = 0.5;

    /// <summary>Speed at which the largest steering angle applies.</summary>
    public const double MinimumSteeringSpeed = 100;

    /// <summary>Steering angle at the minimum steering speed.</summary>
    public const double MaximumSteeringDegrees = 100;

    /// <summary>Steering angle at maximum slide speed.</summary>
    public const double MinimumSteeringDegrees = 30;

    /// <summary>Squared world input magnitude below which relative input is zero.</summary>
    public const double InputTolerance = 0.0001f;
}
