using Ares.Structs.Game.Collision;

namespace Ares.Structs.Game.Movement;

/// <summary>Slide speed and surface friction modifiers.</summary>
/// <param name="MaximumSpeed">Maximum slide speed in world units per second.</param>
/// <param name="FrictionMultiplier">Combined surface and ability friction multiplier.</param>
public readonly record struct SlideMovementParameters(double MaximumSpeed, double FrictionMultiplier = 1)
{
    /// <summary>Slide parameters on a surface with no friction modifier.</summary>
    public static SlideMovementParameters Default { get; } = new(1300);

    /// <summary>Slide friction of low-friction surfaces.</summary>
    public const double LowFriction = 1;

    /// <summary>Slide friction of medium-friction surfaces.</summary>
    public const double MediumFriction = 2;

    /// <summary>Slide friction of high-friction surfaces.</summary>
    public const double HighFriction = 50;

    /// <summary>Slide friction of very-low-friction surfaces.</summary>
    public const double VeryLowFriction = 0.3;

    // Slide friction of the physical surface types Stone (SurfaceType1) to Ice (SurfaceType38), by surface type minus one.
    private static readonly double[] s_surfaceFriction =
    [
        HighFriction, LowFriction, LowFriction, LowFriction, HighFriction, MediumFriction, LowFriction, LowFriction, LowFriction, LowFriction,
        MediumFriction, HighFriction, MediumFriction, MediumFriction, MediumFriction, MediumFriction, LowFriction, MediumFriction, HighFriction, MediumFriction,
        MediumFriction, MediumFriction, MediumFriction, MediumFriction, MediumFriction, HighFriction, MediumFriction, MediumFriction, MediumFriction, MediumFriction,
        LowFriction, MediumFriction, MediumFriction, MediumFriction, LowFriction, LowFriction, LowFriction, VeryLowFriction,
    ];

    /// <summary>
    /// Slide parameters on a physical surface type (the movement state's surface type). Surface friction scales only the
    /// accumulated slowdown; types without a slide friction category keep <see cref="Default"/>.
    /// </summary>
    /// <param name="surfaceType">Physical surface type.</param>
    /// <returns>The parameters.</returns>
    public static SlideMovementParameters ForSurface(PhysicalSurfaceType surfaceType) =>
        surfaceType >= PhysicalSurfaceType.Stone && (int)surfaceType <= s_surfaceFriction.Length
            ? Default with { FrictionMultiplier = s_surfaceFriction[(int)surfaceType - 1] }
            : Default;

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

    /// <summary>Angle between the velocity and the downhill direction above which a slide on a slope goes uphill and brakes.</summary>
    public const double SlopeUphillAlignmentDegrees = 110;

    /// <summary>Speed whose share of the maximum slide speed sets the smallest slope input scale.</summary>
    public const double SlopeMinimumInputSpeed = 100;

    /// <summary>Factor on the smallest slope input scale.</summary>
    public const double SlopeMinimumInputMargin = 1.05;

    /// <summary>Squared horizontal length below which a direction is treated as zero.</summary>
    public const double DirectionTolerance = 1e-8f;

    /// <summary>Ground acceleration, braking and friction while sliding.</summary>
    public static GroundMovementParameters Ground { get; } = new(Default.MaximumSpeed, 1000, 100, 0.3f);

    /// <summary>Ground braking and friction while slowing to leave a slide.</summary>
    public static GroundMovementParameters CancellingGround { get; } = Ground with { BrakingDeceleration = 200, Friction = 1, BrakingFrictionFactor = 3 };

    /// <summary>
    /// Ticks over which the velocity of the movement a character leaves blends into the velocity of the one it enters,
    /// when a slide starts and when it ends: on the n-th tick of the new movement its share is n / BlendTicks.
    /// </summary>
    public const int BlendTicks = 3;

    /// <summary>Action-layer stance while a slide runs.</summary>
    public const CharacterStance ActionStance = CharacterStance.Slide;

    /// <summary>Action-layer locomotion while a slide runs.</summary>
    public const LocoMode ActionLocomotion = LocoMode.Running;

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
