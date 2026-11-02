using Ares.Structs.Core.Geometry;
using Ares.Structs.Core.Serialization;
using Ares.Structs.Game.Movement;

namespace Ares.Raid.World.Movement;

/// <summary>
/// The pawn's first blackboard frame: the initial state at the client's latest input frame, with the
/// spawn position. It starts the client's character simulation, which then runs from the client's own
/// input (no further frames are sent).
/// </summary>
public static class PawnBlackboardFrames
{
    /// <summary>Earliest input frame the first frame may use.</summary>
    public const uint FirstTick = 26;

    /// <summary>Latest input frame the first frame may use.</summary>
    public const uint LastTick = 400_000;

    private const int PositionBits = 24;

    /// <summary>Writes the reset frame body.</summary>
    /// <param name="writer">Destination.</param>
    /// <param name="tick">Simulation tick (the client's latest input frame).</param>
    /// <param name="position">Spawn position.</param>
    public static void WriteReset(BitWriter writer, uint tick, Vector3D position)
    {
        if (tick is < FirstTick or > LastTick)
        {
            throw new InvalidPacketException("Blackboard tick outside its range");
        }

        List<PendulumSlot> slots = [];
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
                    values.Add(new PendulumIntegerValue(PawnBlackboard.PositionXField, Quantize(position.X)));
                    values.Add(new PendulumIntegerValue(PawnBlackboard.PositionYField, Quantize(position.Y)));
                    values.Add(new PendulumIntegerValue(PawnBlackboard.PositionZField, Quantize(position.Z)));
                    break;
            }

            slots.Add(new PendulumSlot(variable.Slot, variable.Position, variable.Size, variable.DescriptorId, PendulumCodec.Default.Value(variable.DescriptorId, values)));
        }

        PendulumCodec.Default.WriteFrame(writer, slots, new Dictionary<int, PendulumSlot>());
    }

    private static ulong Quantize(double world)
    {
        double quantized = (1 << (PositionBits - 1)) + Math.Round(world * (1 << (PositionBits - 1)) / PawnBlackboard.PositionRange, MidpointRounding.ToEven);
        return quantized is >= 0 and < 1 << PositionBits ? (ulong)quantized : throw new InvalidPacketException("Position outside the blackboard range");
    }
}
