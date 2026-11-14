using Ares.Structs.Game.Collision;

namespace Ares.Structs.Game.Movement;

/// <summary>
/// How ice changes ground acceleration (<c>FIceMovementProperties</c> of <c>UIceMovementTweakableObject</c>). On a ground
/// whose physical surface is <see cref="Surface"/>, the ground step replaces its friction with the physical material's
/// friction times the friction multiplier and multiplies its maximum acceleration and braking deceleration by the
/// acceleration multiplier. Walking uses the general values, the slide's ground step the sliding values.
/// </summary>
/// <param name="FrictionMultiplier">Multiplier of the physical material's friction.</param>
/// <param name="AccelerationMultiplier">Multiplier of the maximum acceleration and of the braking deceleration.</param>
public readonly record struct IceMovementParameters(double FrictionMultiplier, double AccelerationMultiplier)
{
    /// <summary>The physical surface type the ice rule applies to (surface name <c>Ice</c>).</summary>
    public const PhysicalSurfaceType Surface = PhysicalSurfaceType.Ice;

    /// <summary>Walking, running and sprinting on ice.</summary>
    public static IceMovementParameters General { get; } = new(1, 0.8);

    /// <summary>The slide's ground step on ice.</summary>
    public static IceMovementParameters Sliding { get; } = new(0.8, 0.8);
}
