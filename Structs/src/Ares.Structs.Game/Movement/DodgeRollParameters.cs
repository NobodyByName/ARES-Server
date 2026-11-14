namespace Ares.Structs.Game.Movement;

/// <summary>Dodge-roll variant, chosen when the roll starts.</summary>
public enum DodgeRollKind : byte
{
    Standard = 0,
    Sprint = 1,
    Exhausted = 2,
    Ninja = 3,
}

/// <summary>Eight-way direction of the movement axes.</summary>
public enum InputDirection : byte
{
    None = 0,
    Forward = 1,
    Backward = 2,
    Left = 3,
    Right = 4,
    ForwardLeft = 5,
    ForwardRight = 6,
    BackwardLeft = 7,
    BackwardRight = 8,
}

/// <summary>Transient movement states a dodge roll reads or raises.</summary>
public enum DodgeRollTransientState : byte
{
    /// <summary>Raised by the animation early in the roll; a collision does not end the roll while it is active.</summary>
    CollisionGrace = 2,

    /// <summary>Raised by the roll to leave it for ground movement.</summary>
    Finished = 3,

    /// <summary>Raised by the animation late in the roll; holding crouch while it is active ends the roll in a crouch.</summary>
    CrouchExit = 4,
}

/// <summary>A span of a roll animation during which it raises a transient state.</summary>
/// <param name="State">The raised state.</param>
/// <param name="Start">Start, in animation seconds.</param>
/// <param name="End">End, in animation seconds.</param>
public readonly record struct DodgeRollWindow(DodgeRollTransientState State, float Start, float End);

/// <summary>Authored metrics and root motion of one dodge-roll variant.</summary>
/// <param name="Distance">Distance the roll covers, in world units; scales the animation's root motion.</param>
/// <param name="Speed">Animation play rate.</param>
/// <param name="StaminaCost">Stamina spent when the roll starts.</param>
/// <param name="ContactAnimationSeconds">Animation time at which the body touches the ground.</param>
/// <param name="SampleRate">Root motion keys per animation second.</param>
/// <param name="RootForward">Root translation along the roll direction at each key, in world units.</param>
/// <param name="Windows">Transient-state spans of the animation.</param>
public sealed record DodgeRollSequence(
    double Distance,
    double Speed,
    double StaminaCost,
    double ContactAnimationSeconds,
    double SampleRate,
    IReadOnlyList<float> RootForward,
    IReadOnlyList<DodgeRollWindow> Windows);

/// <summary>Dodge-roll entry, direction and timing rules.</summary>
public static class DodgeRollParameters
{
    /// <summary>Root distance the standard animation was authored for; the roll's distance scales against it.</summary>
    public const double StandardAuthoredDistance = 400;

    /// <summary>Root distance the sprint animation was authored for.</summary>
    public const double SprintAuthoredDistance = 600;

    /// <summary>Root distance the exhausted animation was authored for.</summary>
    public const double ExhaustedAuthoredDistance = 350;

    /// <summary>Root distance the skill roll's animation was authored for.</summary>
    public const double NinjaAuthoredDistance = 400;

    /// <summary>Minimum sum of absolute movement axes to start a roll.</summary>
    public const double EntryInputThreshold = 0.1f;

    /// <summary>Axis value at or above which the eight-way direction includes that axis (sine of 22.5 degrees).</summary>
    public const double DirectionThreshold = 0.38268343236508984;

    /// <summary>Axis value at or below which the eight-way direction includes the negative axis.</summary>
    public const double NegativeDirectionThreshold = -0.38268343f;

    /// <summary>Positive component of a diagonal eight-way direction.</summary>
    public const double Diagonal = 0.7071067811865476;

    /// <summary>Negative component of a diagonal eight-way direction.</summary>
    public const double NegativeDiagonal = -0.70710677f;

    /// <summary>Squared length below which the start velocity gives no roll direction.</summary>
    public const double DirectionTolerance = 1e-8f;

    /// <summary>Seconds into the roll after which a collision can end it.</summary>
    public const double CollisionGraceSeconds = 0.033;

    /// <summary>Blend time of the transition from the roll back to ground movement.</summary>
    public const double ExitBlendSeconds = 0.1f;

    /// <summary>
    /// Ticks of the exit blend: the first still moves with the roll's animation, and over the others ground movement
    /// takes over in equal steps from the velocity of that first tick.
    /// </summary>
    public const int ExitBlendTicks = 3;

    /// <summary>Graph-state tags (<see cref="GraphStateTags"/>) held while a roll runs.</summary>
    public const ulong RollingFlags = GraphStateFlags.BlockedActions | GraphStateTags.AbilityBlockContextInteractPendulumMove
        | GraphStateTags.AnimationPendulumDisplaySyncedPose | GraphStateTags.AbilityDodgeRollActive;

    /// <summary>The authored metrics of a roll variant.</summary>
    /// <param name="kind">Roll variant.</param>
    /// <returns>Its sequence.</returns>
    public static DodgeRollSequence Sequence(DodgeRollKind kind) => kind switch
    {
        DodgeRollKind.Standard => DodgeRollSequences.Standard,
        DodgeRollKind.Sprint => DodgeRollSequences.Sprint,
        DodgeRollKind.Exhausted => DodgeRollSequences.Exhausted,
        DodgeRollKind.Ninja => DodgeRollSequences.Ninja,
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    /// <summary>The root distance a roll variant's animation was authored for.</summary>
    /// <param name="kind">Roll variant.</param>
    /// <returns>Authored distance in world units.</returns>
    public static double AuthoredDistance(DodgeRollKind kind) => kind switch
    {
        DodgeRollKind.Standard => StandardAuthoredDistance,
        DodgeRollKind.Sprint => SprintAuthoredDistance,
        DodgeRollKind.Exhausted => ExhaustedAuthoredDistance,
        DodgeRollKind.Ninja => NinjaAuthoredDistance,
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };
}
