using Ares.Structs.Game.Pendulum;

namespace Ares.Structs.Game.Enemies;

/// <summary>
/// An ARC's blackboard: the Pendulum variables its actor replicates as one delta-serialized field. The client applies
/// each frame as the ARC's state; out of combat only the transform variable changes.
/// </summary>
/// <param name="Field">Index of the blackboard field in the ARC class's field list.</param>
/// <param name="FieldCount">Number of fields of the ARC class.</param>
/// <param name="TransformSlot">Slot of the transform variable (<see cref="ArcTransform"/>).</param>
/// <param name="InitialState">The variables of a freshly spawned ARC, by slot; fields not listed keep their defaults.</param>
public sealed record ArcBlackboard(int Field, int FieldCount, int TransformSlot, IReadOnlyList<PendulumVariable> InitialState)
{
    /// <summary>The Wasp's blackboard.</summary>
    public static ArcBlackboard Wasp { get; } = new(14, 20, 6,
    [
        new PendulumVariable(0, 0, 8, 17, []),
        new PendulumVariable(1, 8, 5, 24, []),
        new PendulumVariable(2, 13, 4, 0, []),
        new PendulumVariable(3, 17, 1, 29, []),
        new PendulumVariable(4, 18, 1, 15, [
            new PendulumIntegerValue(0, 8390136UL),
            new PendulumIntegerValue(4, 80UL),
        ]),
        new PendulumVariable(5, 19, 1, 25, []),
        new PendulumVariable(6, 20, 1, ArcTransform.DescriptorId, []),
        new PendulumVariable(7, 21, 1, 16, [
            new PendulumIntegerValue(0, 1UL),
        ]),
        new PendulumVariable(8, 22, 1, 33, []),
    ]);
}

/// <summary>
/// Fields of an ARC's transform variable: its rotation, linear and angular velocity and position. A freshly spawned
/// ARC's transform holds only its position; the others start at their defaults (no rotation, at rest).
/// </summary>
public static class ArcTransform
{
    /// <summary>Descriptor of the transform variable.</summary>
    public const int DescriptorId = 37;

    /// <summary>Field of the actor rotation (a quaternion).</summary>
    public const int RotationField = 0;

    /// <summary>First of the three velocity fields (X, Y, Z), in world units per second.</summary>
    public const int VelocityXField = 1;

    /// <summary>First of the three angular velocity fields (X, Y, Z).</summary>
    public const int AngularVelocityXField = 4;

    /// <summary>First of the three position fields (X, Y, Z).</summary>
    public const int PositionXField = 7;

    /// <summary>A field whose meaning is not known; it is <see cref="UnknownFlightValue"/> while the ARC flies.</summary>
    public const int UnknownFlightField = 10;

    /// <summary>Value of <see cref="UnknownFlightField"/> while the ARC flies.</summary>
    public const ulong UnknownFlightValue = 170;

    /// <summary>
    /// World units per second mapped onto a velocity field's range around its default (at rest):
    /// quantized = default + round(velocity * (maximum - 1) / (2 * VelocityRange)).
    /// </summary>
    public const double VelocityRange = 2000.0;

    /// <summary>
    /// World units mapped onto the 24-bit position range around the origin:
    /// quantized = round((world + PositionRange) * (2^24 - 1) / (2 * PositionRange)).
    /// </summary>
    public const double PositionRange = 800000.0;
}
