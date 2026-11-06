using Ares.Raid.World.Movement;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Core.Serialization;
using Ares.Structs.Game.Movement;

namespace Ares.Raid.Tests.World.Movement;

public sealed class InputHistoryTests
{
    [Theory]
    [InlineData(0U, -800000.0)]
    [InlineData(2097151U, 800000.0)]
    [InlineData(1725612U, 516538.10336022545)]
    [InlineData(1739939U, 527468.742117282)]
    public void Reported_positions_use_both_endpoints_of_the_quantization_range(uint quantized, double expected)
    {
        Vector3D position = ReadPosition(quantized);
        Assert.Equal(expected, position.X, 8);
        Assert.Equal(expected, position.Y, 8);
        Assert.Equal(expected, position.Z, 8);
    }

    [Theory]
    [InlineData(0U)]
    [InlineData(1U)]
    [InlineData(1048574U)]
    [InlineData(1048576U)]
    [InlineData(1725612U)]
    [InlineData(1739939U)]
    [InlineData(2097150U)]
    [InlineData(2097151U)]
    public void Repeated_stationary_frames_preserve_the_clients_report_on_every_axis(uint quantized)
    {
        uint current = quantized;
        for (uint frame = 30; frame < 50; frame++)
        {
            PendulumSlot movement = PawnBlackboardFrames.State(frame, new PawnMotion(ReadPosition(current)), 0)[PawnBlackboard.MovementStateSlot];
            IReadOnlyList<PendulumField> fields = PendulumRegistry.Descriptors[movement.DescriptorId!.Value].Fields;
            foreach (int field in (int[])[PawnBlackboard.PositionXField, PawnBlackboard.PositionYField, PawnBlackboard.PositionZField])
            {
                ulong coordinate = PendulumCodec.Integer(fields[field], movement.Value);
                double world = (coordinate / 10.485759375) - 800000.0;
                current = (uint)Math.Round((world + 800000.0) * 1.310719375);
                Assert.Equal(quantized, current);
            }
        }
    }

    private static Vector3D ReadPosition(uint quantized)
    {
        int descriptor = PendulumInput.SlotDescriptors[PendulumInput.PositionSlot];
        byte[] value = PendulumCodec.Default.Value(descriptor,
        [
            new PendulumIntegerValue(PendulumInput.PositionXField, quantized),
            new PendulumIntegerValue(PendulumInput.PositionYField, quantized),
            new PendulumIntegerValue(PendulumInput.PositionZField, quantized),
        ]);
        BitWriter writer = new();
        writer.WriteBit(true);
        writer.WriteBounded(0, PendulumFrames.BaselineRange);
        PendulumFrames.WriteRelative(writer, 1, 0);
        PendulumCodec.Default.WriteFrame(writer,
            [new PendulumSlot(PendulumInput.PositionSlot, 0, 1, descriptor, value)], new Dictionary<int, PendulumSlot>());

        InputHistory history = new();
        history.Read(writer.ToArray());
        return Assert.IsType<Vector3D>(history.Position(1));
    }
}
