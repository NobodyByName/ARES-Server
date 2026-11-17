namespace Ares.Structs.Game.Movement;

/// <summary>A cubic curve key with weighted arrive and leave tangents.</summary>
public readonly record struct WeightedCurveKey(float Time, float Value, float ArriveTangent, float ArriveWeight, float LeaveTangent, float LeaveWeight);

/// <summary>Sprint speed and burst parameters.</summary>
public readonly record struct SprintMovementParameters(double BaseSpeed, double DurationBonus = 0, bool BurstAllowed = true)
{
    /// <summary>Sprinting with an equipped weapon.</summary>
    public static SprintMovementParameters Armed { get; } = new(550);

    /// <summary>Sprinting with empty hands.</summary>
    public static SprintMovementParameters Unarmed { get; } = new(650);

    /// <summary>Upper burst speed relative to the sustained sprint speed.</summary>
    public const double BurstSpeedMultiplier = 2;
}
