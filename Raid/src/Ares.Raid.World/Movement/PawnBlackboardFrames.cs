using Ares.Structs.Core.Geometry;
using Ares.Structs.Core.Serialization;
using Ares.Structs.Game.Movement;

namespace Ares.Raid.World.Movement;

/// <summary>The character's movement in a pawn frame.</summary>
/// <param name="Position">World position.</param>
/// <param name="Velocity">Velocity in world units per second.</param>
/// <param name="Yaw">Facing in radians, or null to keep the initial facing.</param>
/// <param name="Character">Simulated stance, locomotion and flashlight, or null to keep the initial values.</param>
public readonly record struct PawnMotion(Vector3D Position, Vector3D Velocity = default, double? Yaw = null, CharacterState? Character = null);

/// <summary>
/// The pawn's blackboard frames. The first (reset) frame holds the initial state at the client's latest input
/// frame with the spawn position and starts the client's character simulation, which then runs from the
/// client's own input. The client applies each later frame as its full character state, so later frames carry
/// the position the client reported, with the simulated velocity, stance and flashlight.
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
    /// <param name="inventory">The player's container state.</param>
    /// <returns>The slots by index.</returns>
    public static IReadOnlyDictionary<int, PendulumSlot> State(uint tick, PawnMotion motion, ulong focus, PendulumVariable? inventory = null)
    {
        if (tick is < FirstTick or > LastTick)
        {
            throw new InvalidPacketException("Blackboard tick outside its range");
        }

        Dictionary<int, PendulumSlot> slots = [];
        foreach (PendulumVariable initial in PawnBlackboard.InitialState)
        {
            PendulumVariable variable = inventory is not null && initial.Slot == inventory.Slot ? inventory : initial;
            List<PendulumValue> values = [.. variable.Values];
            switch (variable.Slot)
            {
                case PawnBlackboard.GraphStateSlot:
                    values.Add(new PendulumIntegerValue(PawnBlackboard.TickField, tick));
                    values.Add(new PendulumIntegerValue(PawnBlackboard.TimeField, (ulong)tick * PawnBlackboard.TimePerTick));
                    if (motion.Character is { } flashlight)
                    {
                        ulong flags = values.OfType<PendulumIntegerValue>().LastOrDefault(value => value.Field == PawnBlackboard.FlashlightFlagsField)?.Value ?? 0;
                        Replace(values, new PendulumIntegerValue(PawnBlackboard.FlashlightFlagsField,
                            flashlight.Flashlight ? flags | PawnBlackboard.FlashlightFlag : flags & ~PawnBlackboard.FlashlightFlag));
                    }

                    break;
                case PawnBlackboard.ActionStateSlot when motion.Character is { } action:
                    Replace(values, new PendulumIntegerValue(PawnBlackboard.ActionStanceField, (ulong)action.Stance));
                    Replace(values, new PendulumIntegerValue(PawnBlackboard.ActionLocoModeField, (ulong)action.Locomotion));
                    break;
                case PawnBlackboard.MovementStateSlot:
                    values.Add(new PendulumIntegerValue(PawnBlackboard.PositionXField, Quantize(motion.Position.X)));
                    values.Add(new PendulumIntegerValue(PawnBlackboard.PositionYField, Quantize(motion.Position.Y)));
                    values.Add(new PendulumIntegerValue(PawnBlackboard.PositionZField, Quantize(motion.Position.Z)));
                    Replace(values, Velocity(PawnBlackboard.VelocityXField, motion.Velocity.X));
                    Replace(values, Velocity(PawnBlackboard.VelocityYField, motion.Velocity.Y));
                    Replace(values, Velocity(PawnBlackboard.VelocityZField, motion.Velocity.Z));
                    if (motion.Character is { } character)
                    {
                        Replace(values, new PendulumIntegerValue(PawnBlackboard.CharacterStanceField, (ulong)character.Stance));
                        Replace(values, new PendulumIntegerValue(PawnBlackboard.LocoModeField, (ulong)character.Locomotion));
                    }

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

    private static void Replace(List<PendulumValue> values, PendulumValue value)
    {
        values.RemoveAll(existing => existing.Field == value.Field);
        values.Add(value);
    }

    // A velocity component in world units per second.
    private static PendulumIntegerValue Velocity(int field, double velocity)
    {
        PendulumScalarField scalar = (PendulumScalarField)s_movementFields[field];
        double quantized = Math.Floor((velocity + PawnBlackboard.VelocityRange) * (scalar.Maximum - 1) / (2 * PawnBlackboard.VelocityRange));
        return new PendulumIntegerValue(field, (ulong)Math.Clamp(quantized, 0, scalar.Maximum - 1));
    }

    private static ulong Quantize(double world)
    {
        double quantized = Math.Round((world + PawnBlackboard.PositionRange) * ((1 << PositionBits) - 1) / (2 * PawnBlackboard.PositionRange), MidpointRounding.ToEven);
        return quantized is >= 0 and < 1 << PositionBits ? (ulong)quantized : throw new InvalidPacketException("Position outside the blackboard range");
    }
}
