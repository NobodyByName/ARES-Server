using Ares.Structs.Core.Serialization;
using Ares.Structs.Game.Enemies;
using Ares.Structs.Game.Pendulum;

namespace Ares.Raid.World.Actors;

/// <summary>
/// An ARC's blackboard frames. The first frame, a reset, rides in the actor's open bunch; each later frame goes
/// unreliably, coded against the newest frame the client confirmed. Only the transform changes while the ARC is idle.
/// Frames are numbered per client from <see cref="First"/>, and go less often the farther the client is.
/// </summary>
public static class ArcFrames
{
    /// <summary>Number of the first (reset) frame.</summary>
    public const uint First = 1;

    private const int PositionBits = 24;

    // Seconds between frames for clients beyond the last distance below.
    private const double FarInterval = 1.0;

    // Seconds between frames for clients up to each distance (centimetres); beyond the last, the final interval.
    private static readonly (double Distance, double Interval)[] s_intervals =
    [
        (5000, 1.0 / 30),
        (7500, 0.07),
        (10000, 0.23),
        (12500, 0.3),
        (15000, 0.47),
        (20000, 0.7),
    ];

    private static readonly IReadOnlyList<PendulumField> s_transformFields = PendulumRegistry.Descriptors[ArcTransform.DescriptorId].Fields;

    /// <summary>Seconds between frames for a client at a distance.</summary>
    /// <param name="distance">Distance from the ARC to the client's character, in centimetres.</param>
    /// <returns>The interval.</returns>
    public static double Interval(double distance)
    {
        foreach ((double limit, double interval) in s_intervals)
        {
            if (distance < limit)
            {
                return interval;
            }
        }

        return FarInterval;
    }

    /// <summary>
    /// An ARC's blackboard state in a pose: the initial variables, with the pose in the transform. A pose at rest without
    /// rotation leaves the transform's rotation and velocities at their defaults, as a freshly spawned ARC has them.
    /// </summary>
    /// <param name="blackboard">The ARC's blackboard.</param>
    /// <param name="pose">Its pose.</param>
    /// <returns>The slots by index.</returns>
    public static IReadOnlyDictionary<int, PendulumSlot> State(ArcBlackboard blackboard, ArcPose pose)
    {
        ArgumentNullException.ThrowIfNull(blackboard);
        Dictionary<int, PendulumSlot> slots = [];
        foreach (PendulumVariable variable in blackboard.InitialState)
        {
            IReadOnlyList<PendulumValue> values = variable.Slot == blackboard.TransformSlot ? [.. variable.Values, .. Transform(pose)] : variable.Values;
            slots[variable.Slot] = new PendulumSlot(variable.Slot, variable.Position, variable.Size, variable.DescriptorId,
                PendulumCodec.Default.Value(variable.DescriptorId, values));
        }

        return slots;
    }

    /// <summary>Writes a frame: the delta-struct flag, the update header, then the body coded against the baseline.</summary>
    /// <param name="writer">Destination.</param>
    /// <param name="baseline">Frame number the body is coded against (0 for a reset).</param>
    /// <param name="frame">The frame's number, above the baseline.</param>
    /// <param name="state">The frame's state.</param>
    /// <param name="previous">The baseline's state (empty for a reset).</param>
    public static void Write(BitWriter writer, uint baseline, uint frame, IReadOnlyDictionary<int, PendulumSlot> state, IReadOnlyDictionary<int, PendulumSlot> previous)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(previous);
        PendulumCodec codec = PendulumCodec.Others;
        writer.WriteBit(true); // The server supports the delta-struct form.
        BitWriter body = new();
        codec.WriteFrame(body, state.Values, previous);
        BlackboardUpdate.Write(writer, baseline, frame, baseline == 0, codec.Fields, body.ToArray(), body.Length, preferShortHeader: true);
    }

    private static List<PendulumValue> Transform(ArcPose pose)
    {
        List<PendulumValue> values =
        [
            new PendulumIntegerValue(ArcTransform.PositionXField, Position(pose.Location.X)),
            new PendulumIntegerValue(ArcTransform.PositionXField + 1, Position(pose.Location.Y)),
            new PendulumIntegerValue(ArcTransform.PositionXField + 2, Position(pose.Location.Z)),
        ];
        if (pose.Yaw != 0 || pose.Pitch != 0 || pose.Roll != 0)
        {
            PendulumQuaternionField rotation = (PendulumQuaternionField)s_transformFields[ArcTransform.RotationField];
            values.Add(new PendulumQuaternionValue(ArcTransform.RotationField, PendulumRotations.FromRotation(pose.Yaw, pose.Pitch, pose.Roll, rotation)));
        }

        values.Add(Velocity(ArcTransform.VelocityXField, pose.Velocity.X));
        values.Add(Velocity(ArcTransform.VelocityXField + 1, pose.Velocity.Y));
        values.Add(Velocity(ArcTransform.VelocityXField + 2, pose.Velocity.Z));
        if (pose.Flying)
        {
            values.Add(new PendulumIntegerValue(ArcTransform.UnknownFlightField, ArcTransform.UnknownFlightValue));
        }

        return values;
    }

    private static ulong Position(double world)
    {
        double quantized = Math.Round((world + ArcTransform.PositionRange) * ((1 << PositionBits) - 1) / (2 * ArcTransform.PositionRange), MidpointRounding.ToEven);
        return quantized is >= 0 and < 1 << PositionBits ? (ulong)quantized : throw new InvalidPacketException("Position outside the blackboard range");
    }

    // A velocity component, in world units per second, as steps from the field's default (at rest).
    private static PendulumIntegerValue Velocity(int field, double velocity)
    {
        PendulumScalarField scalar = (PendulumScalarField)s_transformFields[field];
        double quantized = scalar.Default + Math.Round(velocity * (scalar.Maximum - 1) / (2 * ArcTransform.VelocityRange));
        return new PendulumIntegerValue(field, (ulong)Math.Clamp(quantized, 0, scalar.Maximum - 1));
    }
}
