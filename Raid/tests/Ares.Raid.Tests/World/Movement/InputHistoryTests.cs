using Ares.Raid.World.Movement;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Core.Serialization;
using Ares.Structs.Game.Movement;
using Ares.Structs.Game.Pendulum;

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
            PendulumSlot movement = PawnBlackboardFrames.State(TestGameData.Data, frame, new PawnMotion(ReadPosition(current)), 0)[PawnBlackboard.MovementStateSlot];
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

    [Fact]
    public void Stock_client_input_names_descriptors_by_their_index_in_the_input_list()
    {
        // A stock client's first two input payloads after spawning, both coded against the defaults (frames 1 and 1-2).
        InputHistory history = new();
        Assert.Equal(1U, history.Read(Convert.FromHexString(
            "010000001cfaffff03411008c1201481111c0112f4ffafb13164dfbf63530804783620c081858110508081ffffaf0967e6a561e7f172afff3526")).Latest);
        Assert.Equal(2U, history.Read(Convert.FromHexString(
            "010000002cfaffff03411008c1201481111c0112f4ffafb13164dfbf63530804783620c081858110508081ffffaf0967e6a561e7f172afff352664fc6f0ae14b330fc111764c2d17452d22b4b80866249038ae550f6b88ff1be68d4300de98f93fa451f672")).Latest);
        Assert.NotNull(history.Movement(2));
        Assert.NotNull(history.Claims(2));
        Assert.NotNull(history.Yaw(2));
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
        PendulumCodec.Input.WriteFrame(writer,
            [new PendulumSlot(PendulumInput.PositionSlot, 0, 1, descriptor, value)], new Dictionary<int, PendulumSlot>());

        InputHistory history = new();
        history.Read(writer.ToArray());
        return Assert.IsType<Vector3D>(history.Position(1));
    }
}
