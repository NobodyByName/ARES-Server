using Ares.Raid.World.Movement;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Core.Serialization;
using Ares.Structs.Game.Movement;

namespace Ares.Raid.Tests.World.Movement;

public sealed class PawnBlackboardFramesTests
{
    private static readonly Vector3D s_spawn = new(1000.0, -2000.0, 300.0);

    [Fact]
    public void Pawn_positions_match_the_quantized_world_coordinates()
    {
        Vector3D position = new(515991.956948754, 590748.297616738, -45139.267750934);
        PendulumSlot movement = PawnBlackboardFrames.State(30, new PawnMotion(position), 0)[PawnBlackboard.MovementStateSlot];
        IReadOnlyList<PendulumField> fields = PendulumRegistry.Descriptors[movement.DescriptorId!.Value].Fields;
        Assert.Equal(13799175UL, PendulumCodec.Integer(fields[PawnBlackboard.PositionXField], movement.Value));
        Assert.Equal(14583052UL, PendulumCodec.Integer(fields[PawnBlackboard.PositionYField], movement.Value));
        Assert.Equal(7915288UL, PendulumCodec.Integer(fields[PawnBlackboard.PositionZField], movement.Value));
    }

    [Theory]
    [InlineData(false, false, false, RoughMovementMode.Ground, 0UL)]
    [InlineData(false, false, true, RoughMovementMode.Ground, GraphStateTags.AbilityJumpPending)]
    [InlineData(true, true, false, RoughMovementMode.Air, GraphStateTags.AnimationAbilityJumpStarted)]
    [InlineData(true, false, false, RoughMovementMode.Air, 0UL)]
    public void A_simulated_body_sets_the_movement_mode_and_the_jump_flags(bool airborne, bool jumping, bool pending, RoughMovementMode mode, ulong flags)
    {
        BodyState body = new(s_spawn, default, default) { Airborne = airborne, Jumping = jumping, JumpRequestedAt = pending ? 29 : null };
        CharacterState character = new(30, s_spawn, false) { Body = body };

        IReadOnlyDictionary<int, PendulumSlot> state = PawnBlackboardFrames.State(30, new PawnMotion(s_spawn, Character: character), 0);

        PendulumSlot movement = state[PawnBlackboard.MovementStateSlot];
        PendulumSlot graph = state[PawnBlackboard.GraphStateSlot];
        Assert.Equal((ulong)mode, PendulumCodec.Integer(
            PendulumRegistry.Descriptors[movement.DescriptorId!.Value].Fields[PawnBlackboard.RoughMovementModeField], movement.Value));
        Assert.Equal(flags, PendulumCodec.Integer(
            PendulumRegistry.Descriptors[graph.DescriptorId!.Value].Fields[PawnBlackboard.GraphTagsField], graph.Value)
            & (GraphStateTags.AnimationAbilityJumpStarted | GraphStateTags.AbilityJumpPending));
    }

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
    public void Every_turn_between_two_headings_can_be_written()
    {
        for (int from = -180; from < 180; from += 5)
        {
            IReadOnlyDictionary<int, PendulumSlot> previous = PawnBlackboardFrames.State(40, new PawnMotion(s_spawn, Yaw: from * Math.PI / 180), 0);
            for (int to = -1800; to < 1800; to++)
            {
                IReadOnlyDictionary<int, PendulumSlot> state = PawnBlackboardFrames.State(41, new PawnMotion(s_spawn, Yaw: to * Math.PI / 1800), 0);
                PawnBlackboardFrames.Write(new BitWriter(), state, previous);
            }
        }
    }

    [Fact]
    public void A_moving_frame_carries_the_velocity_and_facing()
    {
        // 375 world units per second (12.5 per tick) along X, facing a quarter turn to the left.
        IReadOnlyDictionary<int, PendulumSlot> state = PawnBlackboardFrames.State(40, new PawnMotion(s_spawn, new Vector3D(375, 0.0, 0.0), -Math.PI / 2), 0);
        PendulumSlot movement = state[PawnBlackboard.MovementStateSlot];
        IReadOnlyList<PendulumField> fields = PendulumRegistry.Descriptors[movement.DescriptorId!.Value].Fields;
        PendulumScalarField velocity = (PendulumScalarField)fields[PawnBlackboard.VelocityXField];

        Assert.Equal(velocity.Default + 128UL, PendulumCodec.Integer(velocity, movement.Value));
        Assert.Equal(velocity.Default, (uint)PendulumCodec.Integer(fields[PawnBlackboard.VelocityYField], movement.Value));
        PendulumQuaternionField facing = (PendulumQuaternionField)fields[PawnBlackboard.FacingField];
        Assert.Equal(-90.0, PendulumRotations.Yaw(PendulumCodec.Quaternion(facing, movement.Value), facing) * 180.0 / Math.PI, 1);
    }

    [Theory]
    [InlineData(-110.0)]
    [InlineData(15.0)]
    [InlineData(120.0)]
    public void The_action_rotation_turns_with_the_facing(double degrees)
    {
        PendulumSlot action = PawnBlackboardFrames.State(40, new PawnMotion(s_spawn, Yaw: degrees * Math.PI / 180), 0)[PawnBlackboard.ActionStateSlot];
        PendulumQuaternionField rotation = (PendulumQuaternionField)PendulumRegistry.Descriptors[action.DescriptorId!.Value].Fields[PawnBlackboard.ActionRotationField];
        PendulumQuaternion quaternion = PendulumCodec.Quaternion(rotation, action.Value);

        Assert.Equal(degrees, PendulumRotations.Yaw(quaternion, rotation) * 180.0 / Math.PI, 1.5);
        (double x, double y, _, _) = PendulumRotations.Components(quaternion, rotation);
        Assert.Equal((0.0, 0.0), (x, y));
    }

    [Theory]
    [InlineData(-6000.0, 0UL)]
    [InlineData(-1.0, 2047UL)]
    [InlineData(0.0, 2047UL)]
    [InlineData(60.0, 2068UL)]
    [InlineData(300.0, 2150UL)]
    [InlineData(5999.0, 4095UL)]
    [InlineData(9000.0, 4095UL)]
    public void Velocities_use_the_clients_quantization(double velocity, ulong expected)
    {
        PendulumSlot movement = PawnBlackboardFrames.State(40, new PawnMotion(s_spawn, new Vector3D(velocity, 0.0, 0.0)), 0)[PawnBlackboard.MovementStateSlot];
        IReadOnlyList<PendulumField> fields = PendulumRegistry.Descriptors[movement.DescriptorId!.Value].Fields;
        Assert.Equal(expected, PendulumCodec.Integer(fields[PawnBlackboard.VelocityXField], movement.Value));
    }

    [Fact]
    public void A_simulated_frame_carries_the_action_clock_and_the_ground_node()
    {
        CharacterState character = new(40, s_spawn, Flashlight: false)
        {
            GroundNode = (GroundNodeState.Initial with { InputHeld = false }).Next(40, new MovementInput(40, PendulumInput.MovementAxisMaximum, 0, default), 300, sprinting: false,
                sprintStarted: false, boosted: false, LocoMode.Running, CharacterStance.Stand, shuffling: false, sliding: false),
        };
        IReadOnlyDictionary<int, PendulumSlot> slots = PawnBlackboardFrames.State(40, new PawnMotion(s_spawn, Character: character), 0);
        PendulumSlot action = slots[PawnBlackboard.ActionStateSlot];
        IReadOnlyList<PendulumField> actionFields = PendulumRegistry.Descriptors[action.DescriptorId!.Value].Fields;
        Assert.Equal((39UL * PawnBlackboard.TimePerTick) + 1, PendulumCodec.Integer(actionFields[PawnBlackboard.ActionClockField], action.Value));
        Assert.Equal((ulong)InputDirection.Forward, PendulumCodec.Integer(actionFields[PawnBlackboard.ActionInputDirectionField], action.Value));
        Assert.Equal(31UL, PendulumCodec.Integer(actionFields[PawnBlackboard.ActionInputStrengthField], action.Value));

        PendulumSlot node = slots[PawnGroundNodeState.InitialSlot];
        IReadOnlyList<PendulumField> nodeFields = PendulumRegistry.Descriptors[PawnGroundNodeState.DescriptorId].Fields;
        Assert.Equal(614UL, PendulumCodec.Integer(nodeFields[PawnGroundNodeState.CurrentMaxMoveSpeedField], node.Value));
        Assert.Equal(PawnSlideState.TimeBias + (40UL * PawnBlackboard.TimePerTick), PendulumCodec.Integer(nodeFields[PawnGroundNodeState.StartedInputTimeField], node.Value));
        Assert.Equal((ulong)LocoMode.Running, PendulumCodec.Integer(nodeFields[PawnGroundNodeState.LocoModeField], node.Value));
    }

    [Fact]
    public void A_slide_lays_out_its_node_slots_and_keeps_them_after_it()
    {
        SlideState pressed = new(default, 0) { StartFrame = 40 };
        NodeLayout layout = NodeLayout.Initial.Next(40, pressed, 0);
        Assert.Equal((PawnGroundNodeState.InitialSlot, 12, 13), (layout.GroundSlot, layout.TransitionSlot, layout.SlideSlot));
        for (uint frame = 41; frame < 40 + PawnTransitionState.Ticks; frame++)
        {
            layout = layout.Next(frame, pressed with { Ticks = (int)(frame - 40) }, 0);
        }

        layout = layout.Next(40 + PawnTransitionState.Ticks, pressed with { Ticks = PawnTransitionState.Ticks }, 0);
        Assert.Equal((-1, -1, 13, 13), (layout.GroundSlot, layout.TransitionSlot, layout.SlideSlot, layout.ActiveSlot));

        layout = layout.Next(80, pressed with { Ticks = 40 }, 1);
        Assert.Equal((12, PawnGroundNodeState.InitialSlot, 13, true), (layout.GroundSlot, layout.TransitionSlot, layout.SlideSlot, layout.GroundAdded));

        CharacterState character = new(80, s_spawn, Flashlight: false) { Nodes = layout, Slide = pressed with { Ticks = 40 } };
        IReadOnlyDictionary<int, PendulumSlot> slots = PawnBlackboardFrames.State(80, new PawnMotion(s_spawn, Character: character), 0);
        Assert.Equal(PawnTransitionState.DescriptorId, slots[PawnGroundNodeState.InitialSlot].DescriptorId);
        Assert.Equal(PawnGroundNodeState.DescriptorId, slots[12].DescriptorId);
        Assert.Equal(PawnSlideState.DescriptorId, slots[13].DescriptorId);

        // A laid-out slot whose node ended reaches the client empty.
        character = character with { Nodes = layout with { TransitionSlot = -1 } };
        slots = PawnBlackboardFrames.State(81, new PawnMotion(s_spawn, Character: character), 0);
        BitWriter writer = new();
        PawnBlackboardFrames.Write(writer, slots, new Dictionary<int, PendulumSlot>());
        Dictionary<int, PendulumSlot> read = PendulumCodec.Default.ReadFrame(new BitReader(writer.ToArray()), new Dictionary<int, PendulumSlot>());
        Assert.Null(read[PawnGroundNodeState.InitialSlot].DescriptorId);
        Assert.Equal(NodeLayout.Position(PawnGroundNodeState.InitialSlot), read[PawnGroundNodeState.InitialSlot].Position);
    }

    [Fact]
    public void A_simulated_frame_carries_stance_locomotion_and_flashlight()
    {
        CharacterState character = new(40, s_spawn, Flashlight: true)
        {
            Stance = CharacterStance.Crouch,
            Locomotion = LocoMode.Walking,
            MovementStance = CharacterStance.Crouch,
            MovementLocomotion = LocoMode.Walking,
        };
        IReadOnlyDictionary<int, PendulumSlot> lit = PawnBlackboardFrames.State(40, new PawnMotion(s_spawn, Character: character), 0);
        IReadOnlyDictionary<int, PendulumSlot> dark = PawnBlackboardFrames.State(40, new PawnMotion(s_spawn, Character: character with { Flashlight = false }), 0);
        IReadOnlyDictionary<int, PendulumSlot> initial = PawnBlackboardFrames.State(40, new PawnMotion(s_spawn), 0);

        Assert.Equal((ulong)CharacterStance.Crouch, Field(lit, PawnBlackboard.MovementStateSlot, PawnBlackboard.CharacterStanceField));
        Assert.Equal((ulong)LocoMode.Walking, Field(lit, PawnBlackboard.MovementStateSlot, PawnBlackboard.LocoModeField));
        Assert.Equal((ulong)CharacterStance.Crouch, Field(lit, PawnBlackboard.ActionStateSlot, PawnBlackboard.ActionStanceField));
        Assert.Equal((ulong)LocoMode.Walking, Field(lit, PawnBlackboard.ActionStateSlot, PawnBlackboard.ActionLocoModeField));
        // The simulated flags replace the initial ones: a crouch by input, no sprint.
        ulong flags = (Field(initial, PawnBlackboard.GraphStateSlot, PawnBlackboard.GraphTagsField) & ~GraphStateFlags.Simulated) | GraphStateTags.AbilityCrouch;
        Assert.Equal(flags | GraphStateTags.AbilityFlashlightActive, Field(lit, PawnBlackboard.GraphStateSlot, PawnBlackboard.GraphTagsField));
        Assert.Equal(flags, Field(dark, PawnBlackboard.GraphStateSlot, PawnBlackboard.GraphTagsField));
    }

    [Theory]
    [InlineData(LocoMode.Sprinting, true)]
    [InlineData(LocoMode.Running, false)]
    public void The_sprint_flag_follows_the_sprint_locomotion(LocoMode locomotion, bool sprinting)
    {
        CharacterState character = new(40, s_spawn, Flashlight: false) { MovementLocomotion = locomotion, Locomotion = locomotion };
        ulong flags = Field(PawnBlackboardFrames.State(40, new PawnMotion(s_spawn, Character: character), 0), PawnBlackboard.GraphStateSlot,
            PawnBlackboard.GraphTagsField);
        Assert.Equal(sprinting, (flags & GraphStateTags.AbilitySprintActive) != 0);
    }

    [Fact]
    public void Rolling_and_recovering_frames_carry_their_flags()
    {
        CharacterState rolling = new(40, s_spawn, Flashlight: false) { Roll = new DodgeRollState(DodgeRollKind.Standard, new Vector3D(1, 0, 0), 5) };
        CharacterState recovering = new(40, s_spawn, Flashlight: false) { HeavyLandingTicks = 3 };
        Assert.Equal(DodgeRollParameters.RollingFlags, Graph(rolling) & DodgeRollParameters.RollingFlags);
        Assert.Equal(DodgeRollParameters.RollingFlags, Graph(rolling with { RollExitTicks = 1 }) ^ Graph(rolling));
        Assert.Equal(GraphStateFlags.HeavyLandingRecovery, Graph(recovering) & GraphStateFlags.HeavyLandingRecovery);
        Assert.Equal(0UL, Graph(recovering with { HeavyLandingTicks = 0 }) & GraphStateTags.AbilityLandingHeavy);
    }

    private static ulong Graph(CharacterState character) =>
        Field(PawnBlackboardFrames.State(40, new PawnMotion(s_spawn, Character: character), 0), PawnBlackboard.GraphStateSlot, PawnBlackboard.GraphTagsField);

    [Fact]
    public void A_sliding_frame_carries_the_action_and_movement_pairs_and_the_slide_state()
    {
        SlideState slide = new(new Vector3D(-297.05, -498.01, 0), 0.5f, 20) { StartFrame = 2133, MaximumSpeed = 1300 };
        CharacterState character = new(2141, s_spawn, false)
        {
            Stance = CharacterStance.Slide,
            Locomotion = LocoMode.Running,
            MovementStance = CharacterStance.Crouch,
            MovementLocomotion = LocoMode.Sprinting,
            Slide = slide,
            Nodes = NodeLayout.Initial.Next(2133, slide with { Ticks = 0 }, 0),
        };
        IReadOnlyDictionary<int, PendulumSlot> state = PawnBlackboardFrames.State(2141, new PawnMotion(s_spawn, Character: character), 0);

        Assert.Equal((ulong)CharacterStance.Slide, Field(state, PawnBlackboard.ActionStateSlot, PawnBlackboard.ActionStanceField));
        Assert.Equal((ulong)LocoMode.Running, Field(state, PawnBlackboard.ActionStateSlot, PawnBlackboard.ActionLocoModeField));
        Assert.Equal((ulong)CharacterStance.Crouch, Field(state, PawnBlackboard.MovementStateSlot, PawnBlackboard.CharacterStanceField));
        Assert.Equal((ulong)LocoMode.Sprinting, Field(state, PawnBlackboard.MovementStateSlot, PawnBlackboard.LocoModeField));
        PendulumSlot slot = state[PawnSlideState.Slot];
        Assert.Equal((PawnSlideState.Position, PawnSlideState.Size, PawnSlideState.DescriptorId), (slot.Position, slot.Size, slot.DescriptorId!.Value));
        Assert.Equal(0x7FF644UL, Field(state, PawnSlideState.Slot, PawnSlideState.InitialDirectionXField));
        Assert.Equal(0x7FEFAEUL, Field(state, PawnSlideState.Slot, PawnSlideState.InitialDirectionXField + 1));
        Assert.Equal(0x800000UL, Field(state, PawnSlideState.Slot, PawnSlideState.InitialDirectionXField + 2));
        Assert.Equal(2662UL, Field(state, PawnSlideState.Slot, PawnSlideState.CurrentMaxMoveSpeedField));
        Assert.Equal(16UL, Field(state, PawnSlideState.Slot, PawnSlideState.SlowDownAmountField));
        Assert.Equal(PawnSlideState.UnknownField6Value, Field(state, PawnSlideState.Slot, PawnSlideState.UnknownField6));
        Assert.Equal(0x8000A6A4UL, Field(state, PawnSlideState.Slot, PawnSlideState.StartTimeField));
        Assert.Equal(PawnSlideState.ConsumedSprintInputFlag, Field(state, PawnSlideState.Slot, PawnSlideState.FlagsField));
        Assert.False(PawnBlackboardFrames.State(2141, new PawnMotion(s_spawn, Character: character with { Slide = null, Nodes = NodeLayout.Initial }), 0).ContainsKey(PawnSlideState.Slot));
    }

    [Fact]
    public void The_press_tick_leaves_the_slide_state_to_the_client()
    {
        CharacterState character = new(2133, s_spawn, false) { Slide = new SlideState(default, 0) { StartFrame = 2133 } };
        Assert.False(PawnBlackboardFrames.State(2133, new PawnMotion(s_spawn, Character: character), 0).ContainsKey(PawnSlideState.Slot));
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

    private static ulong Field(IReadOnlyDictionary<int, PendulumSlot> state, int slot, int field) =>
        PendulumCodec.Integer(PendulumRegistry.Descriptors[state[slot].DescriptorId!.Value].Fields[field], state[slot].Value);
}
