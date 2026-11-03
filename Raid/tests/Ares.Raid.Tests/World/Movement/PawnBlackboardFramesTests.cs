using Ares.Raid.World.Movement;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Core.Serialization;
using Ares.Structs.Game.Movement;

namespace Ares.Raid.Tests.World.Movement;

public sealed class PawnBlackboardFramesTests
{
    private static readonly Vector3D s_spawn = new(1000.0, -2000.0, 300.0);

    [Fact]
    public void A_focus_frame_changes_only_the_tick_and_the_interactor_state()
    {
        IReadOnlyDictionary<int, PendulumSlot> reset = PawnBlackboardFrames.State(30, new PawnMotion(s_spawn), 0);
        IReadOnlyDictionary<int, PendulumSlot> focused = PawnBlackboardFrames.State(40, new PawnMotion(s_spawn), 0x20008D);

        BitWriter writer = new();
        PawnBlackboardFrames.Write(writer, focused, reset);
        Dictionary<int, PendulumSlot> decoded = PendulumCodec.Default.ReadFrame(new BitReader(writer.ToArray(), writer.Length), reset);

        Assert.Equal(focused.Keys.Order(), decoded.Keys.Order());
        Assert.All(focused.Values, slot => Assert.Equal(slot.Value, decoded[slot.Slot].Value));
        int[] changed = [.. reset.Values.Where(slot => !slot.Value.AsSpan().SequenceEqual(focused[slot.Slot].Value)).Select(slot => slot.Slot).Order()];
        Assert.Equal([PawnBlackboard.GraphStateSlot, PawnBlackboard.InteractorSlot], changed);

        PendulumSlot interactor = decoded[PawnBlackboard.InteractorSlot];
        IReadOnlyList<PendulumField> fields = PendulumRegistry.Descriptors[interactor.DescriptorId!.Value].Fields;
        Assert.Equal(0x20008DUL, PendulumCodec.Integer(fields[PawnBlackboard.FocusedInteractionField], interactor.Value));
        Assert.Equal(PawnBlackboard.CanAccessFocusedInteractionFlag, PendulumCodec.Integer(fields[PawnBlackboard.InteractorFlagsField], interactor.Value));
    }

    [Fact]
    public void Losing_focus_restores_the_initial_interactor_state()
    {
        IReadOnlyDictionary<int, PendulumSlot> initial = PawnBlackboardFrames.State(30, new PawnMotion(s_spawn), 0);
        IReadOnlyDictionary<int, PendulumSlot> unfocused = PawnBlackboardFrames.State(50, new PawnMotion(s_spawn), 0);
        Assert.Equal(initial[PawnBlackboard.InteractorSlot].Value, unfocused[PawnBlackboard.InteractorSlot].Value);
    }

    [Fact]
    public void A_moving_frame_carries_the_velocity_and_facing()
    {
        // 12.5 world units per tick along X, facing a quarter turn to the left.
        IReadOnlyDictionary<int, PendulumSlot> state = PawnBlackboardFrames.State(40, new PawnMotion(s_spawn, new Vector3D(12.5, 0.0, 0.0), -Math.PI / 2), 0);
        PendulumSlot movement = state[PawnBlackboard.MovementStateSlot];
        IReadOnlyList<PendulumField> fields = PendulumRegistry.Descriptors[movement.DescriptorId!.Value].Fields;
        PendulumScalarField velocity = (PendulumScalarField)fields[PawnBlackboard.VelocityXField];

        Assert.Equal(velocity.Default + 128UL, PendulumCodec.Integer(velocity, movement.Value));
        Assert.Equal(velocity.Default, (uint)PendulumCodec.Integer(fields[PawnBlackboard.VelocityYField], movement.Value));
        PendulumQuaternionField facing = (PendulumQuaternionField)fields[PawnBlackboard.FacingField];
        Assert.Equal(-90.0, PendulumRotations.Yaw(PendulumCodec.Quaternion(facing, movement.Value), facing) * 180.0 / Math.PI, 1);
    }

    [Fact]
    public void A_stationary_spawn_has_no_velocity_on_any_axis()
    {
        PendulumSlot movement = PawnBlackboardFrames.State(30, new PawnMotion(s_spawn), 0)[PawnBlackboard.MovementStateSlot];
        IReadOnlyList<PendulumField> fields = PendulumRegistry.Descriptors[movement.DescriptorId!.Value].Fields;
        foreach (int index in (int[])[PawnBlackboard.VelocityXField, PawnBlackboard.VelocityYField, PawnBlackboard.VelocityZField])
        {
            PendulumScalarField axis = (PendulumScalarField)fields[index];
            Assert.Equal(axis.Default, PendulumCodec.Integer(axis, movement.Value));
        }
    }

    [Fact]
    public void Ticks_outside_the_simulation_range_are_rejected() =>
        Assert.Throws<InvalidPacketException>(() => PawnBlackboardFrames.State(PawnBlackboardFrames.FirstTick - 1, new PawnMotion(s_spawn), 0));
}
