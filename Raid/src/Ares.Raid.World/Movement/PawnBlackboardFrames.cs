using Ares.Structs.Core.Geometry;
using Ares.Structs.Core.Serialization;
using Ares.Structs.Game.Movement;

namespace Ares.Raid.World.Movement;

/// <summary>The character's movement in a pawn frame.</summary>
/// <param name="Position">World position.</param>
/// <param name="Step">Position change over the last tick (zero at rest).</param>
/// <param name="Yaw">Facing in radians, or null to keep the initial facing.</param>
public readonly record struct PawnMotion(Vector3D Position, Vector3D Step = default, double? Yaw = null);

/// <summary>
/// The pawn's blackboard frames. The first (reset) frame holds the initial state at the client's latest input
/// frame with the spawn position and starts the client's character simulation, which then runs from the
/// client's own input. The client applies each later frame as its full character state, so later frames carry
/// the position the client reported.
/// </summary>
public static class PawnBlackboardFrames
{
    /// <summary>Earliest input frame a frame may use.</summary>
    public const uint FirstTick = 26;

    /// <summary>Latest input frame a frame may use.</summary>
    public const uint LastTick = 400_000;

    private const int PositionBits = 24;

    private static readonly IReadOnlyList<PendulumField> s_movementFields =
        PendulumRegistry.Descriptors[PawnBlackboard.InitialState.First(variable => variable.Slot == PawnBlackboard.MovementStateSlot).DescriptorId].Fields;

    /// <summary>The pawn's state at a tick.</summary>
    /// <param name="tick">Simulation tick (an input frame of the client).</param>
    /// <param name="motion">Character movement.</param>
    /// <param name="focus">Interaction the character may use (its focused button), or 0 for none.</param>
    /// <returns>The slots by index.</returns>
    public static IReadOnlyDictionary<int, PendulumSlot> State(uint tick, PawnMotion motion, ulong focus)
    {
        if (tick is < FirstTick or > LastTick)
        {
            throw new InvalidPacketException("Blackboard tick outside its range");
        }

        Dictionary<int, PendulumSlot> slots = [];
        foreach (PendulumVariable variable in PawnBlackboard.InitialState)
        {
            List<PendulumValue> values = [.. variable.Values];
            switch (variable.Slot)
            {
                case PawnBlackboard.GraphStateSlot:
                    values.Add(new PendulumIntegerValue(PawnBlackboard.TickField, tick));
                    values.Add(new PendulumIntegerValue(PawnBlackboard.TimeField, (ulong)tick * PawnBlackboard.TimePerTick));
                    break;
                case PawnBlackboard.MovementStateSlot:
                    values.Add(new PendulumIntegerValue(PawnBlackboard.PositionXField, Quantize(motion.Position.X)));
                    values.Add(new PendulumIntegerValue(PawnBlackboard.PositionYField, Quantize(motion.Position.Y)));
                    values.Add(new PendulumIntegerValue(PawnBlackboard.PositionZField, Quantize(motion.Position.Z)));
                    values.Add(Velocity(PawnBlackboard.VelocityXField, motion.Step.X));
                    values.Add(Velocity(PawnBlackboard.VelocityYField, motion.Step.Y));
                    values.Add(Velocity(PawnBlackboard.VelocityZField, motion.Step.Z));

                    if (motion.Yaw is { } yaw)
                    {
                        PendulumQuaternionField facing = (PendulumQuaternionField)s_movementFields[PawnBlackboard.FacingField];
                        values.RemoveAll(value => value.Field == PawnBlackboard.FacingField);
                        values.Add(new PendulumQuaternionValue(PawnBlackboard.FacingField, PendulumRotations.FromYaw(yaw, facing)));
                    }

                    break;
                case PawnBlackboard.InteractorSlot when focus != 0:
                    values.Add(new PendulumIntegerValue(PawnBlackboard.FocusedInteractionField, focus));
                    values.Add(new PendulumIntegerValue(PawnBlackboard.InteractorFlagsField, PawnBlackboard.CanAccessFocusedInteractionFlag));
                    break;
            }

            slots[variable.Slot] = new PendulumSlot(variable.Slot, variable.Position, variable.Size, variable.DescriptorId, PendulumCodec.Default.Value(variable.DescriptorId, values));
        }

        return slots;
    }

    /// <summary>Writes a frame body coded against the previous frame.</summary>
    /// <param name="writer">Destination.</param>
    /// <param name="state">The frame's state.</param>
    /// <param name="previous">The previous frame's state (empty for the reset frame).</param>
    public static void Write(BitWriter writer, IReadOnlyDictionary<int, PendulumSlot> state, IReadOnlyDictionary<int, PendulumSlot> previous)
    {
        ArgumentNullException.ThrowIfNull(state);
        PendulumCodec.Default.WriteFrame(writer, state.Values, previous);
    }

    // A velocity component from the position change over one tick, around the field's resting value.
    private static PendulumIntegerValue Velocity(int field, double step)
    {
        PendulumScalarField scalar = (PendulumScalarField)s_movementFields[field];
        double steps = step * (1 << (PositionBits - 1)) / PawnBlackboard.PositionRange;
        double quantized = Math.Clamp(scalar.Default + Math.Round(steps * PawnBlackboard.VelocityPerPositionStep), 0, scalar.Maximum - 1);
        return new PendulumIntegerValue(field, (ulong)quantized);
    }

    private static ulong Quantize(double world)
    {
        double quantized = (1 << (PositionBits - 1)) + Math.Round(world * (1 << (PositionBits - 1)) / PawnBlackboard.PositionRange, MidpointRounding.ToEven);
        return quantized is >= 0 and < 1 << PositionBits ? (ulong)quantized : throw new InvalidPacketException("Position outside the blackboard range");
    }
}
