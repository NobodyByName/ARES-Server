using Ares.Raid.World.Movement;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Game.Inventory;
using Ares.Structs.Game.Movement;
using Ares.Structs.Game.World;

namespace Ares.Raid.Tests.World.Movement;

public sealed class NodeLayoutTests
{
    private const double Tick = 1.0 / 30;

    [Theory]
    [InlineData(5)]
    [InlineData(25)]
    public void A_roll_that_cuts_a_slide_short_returns_to_a_ground_layout(int slideTicks)
    {
        CharacterSimulation simulation = Sprinting(out uint frame);
        Step(simulation, frame++, MovementButtons.Sprint | MovementButtons.Crouch);
        for (int tick = 1; tick < slideTicks; tick++)
        {
            Step(simulation, frame++, MovementButtons.Sprint | MovementButtons.Crouch | MovementButtons.PreviousCrouch);
        }

        Assert.True(simulation.State.Nodes.SlideSlot >= 0);
        Step(simulation, frame++, MovementButtons.DodgeRoll);
        for (int tick = 0; tick < 60; tick++)
        {
            Step(simulation, frame++, MovementButtons.None);
        }

        AssertGround(simulation.State);
    }

    [Fact]
    public void A_roll_then_a_slide_lays_the_slide_out_again()
    {
        CharacterSimulation simulation = Sprinting(out uint frame);
        Step(simulation, frame++, MovementButtons.DodgeRoll);
        for (int tick = 0; tick < 50; tick++)
        {
            Step(simulation, frame++, MovementButtons.Sprint);
        }

        CharacterState pressed = Step(simulation, frame++, MovementButtons.Sprint | MovementButtons.Crouch);
        Assert.Equal((PawnGroundNodeState.InitialSlot, 12, 13), (pressed.Nodes.GroundSlot, pressed.Nodes.TransitionSlot, pressed.Nodes.SlideSlot));
        for (int tick = 0; tick < 120; tick++)
        {
            Step(simulation, frame++, MovementButtons.None);
        }

        AssertGround(simulation.State);
    }

    [Fact]
    public void A_ladder_or_a_followed_vault_after_a_slide_leaves_a_ground_layout()
    {
        foreach (bool ladder in new[] { true, false })
        {
            CharacterSimulation simulation = Sliding(out uint frame, 20);
            for (int tick = 0; tick < 10; tick++, frame++)
            {
                if (ladder)
                {
                    simulation.StepOnLadder(new CharacterInput(frame, 0), new MovementInput(frame, 126, 0, MovementButtons.None), 0, Tick, null, null,
                        tick == 0 ? LadderMovement.Begin(LadderLine.From(DamLadders.All[0]), topEntry: false) : null);
                }
                else
                {
                    simulation.StepFollowingVault(new CharacterInput(frame, 0), Tick, new Vector3D(frame, 0, 0), new Vector3D(frame - 1, 0, 0), 0);
                }
            }

            for (int tick = 0; tick < 30; tick++)
            {
                Step(simulation, frame++, MovementButtons.None);
            }

            AssertGround(simulation.State);
        }
    }

    [Theory]
    [InlineData(3)]
    [InlineData(30)]
    public void A_slide_ended_without_its_exit_frees_its_slots_and_starts_the_ground_node_again(int slideTicks)
    {
        // A jump, a fall, a heavy landing or a re-seed ends the slide state without the slide's finishing tick.
        SlideState slide = new(default, 0) { StartFrame = 100 };
        NodeLayout layout = NodeLayout.Initial;
        for (int tick = 0; tick < slideTicks; tick++)
        {
            layout = layout.Next(100 + (uint)tick, slide with { Ticks = tick }, 0);
        }

        layout = layout.Next(100 + (uint)slideTicks, null, 0);
        Assert.Equal((-1, -1, layout.GroundSlot), (layout.SlideSlot, layout.TransitionSlot, layout.ActiveSlot));
        Assert.Equal(slideTicks >= PawnTransitionState.Ticks, layout.GroundAdded);
        Assert.Equal(PawnGroundNodeState.InitialSlot, layout.GroundSlot);

        // The next slide lays out as after any slide.
        layout = layout.Next(200, slide with { StartFrame = 200 }, 0);
        Assert.Equal((PawnGroundNodeState.InitialSlot, 12, 13), (layout.GroundSlot, layout.TransitionSlot, layout.SlideSlot));
    }

    [Theory]
    [InlineData(1, 12, 34)]
    [InlineData(2, 13, 35)]
    public void Node_slots_follow_the_equipped_weapons(int weapons, int groundSlot, int groundPosition)
    {
        // After a slide the ground node sits in the first node slot after the other variables.
        NodeLayout layout = NodeLayout.Initial with { LaidOut = (1UL << PawnGroundNodeState.InitialSlot) | (1UL << 12) | (1UL << 13), GroundSlot = 12 };
        CharacterState character = new(40, default, Flashlight: false) { Nodes = layout };
        PendulumVariable first = PawnBlackboard.InitialState.First(variable => variable.Slot == WeaponProtocol.FirstWeaponSlot);
        List<PendulumVariable> equipped = [first];
        if (weapons == 2)
        {
            equipped.Add(first with { Slot = WeaponProtocol.FirstWeaponSlot + 1, Position = first.Position + first.Size });
        }

        IReadOnlyDictionary<int, PendulumSlot> slots = PawnBlackboardFrames.State(40, new PawnMotion(default, Character: character), 0, weapons: equipped);
        Assert.Equal(PawnGroundNodeState.DescriptorId, slots[groundSlot].DescriptorId);
        Assert.Equal(groundPosition, slots[groundSlot].Position);
        Assert.Equal(first.DescriptorId, slots[WeaponProtocol.FirstWeaponSlot + weapons - 1].DescriptorId);
        Assert.Null(slots[PawnGroundNodeState.InitialSlot].DescriptorId);
        PendulumSlot movement = slots[PawnBlackboard.MovementStateSlot];
        IReadOnlyList<PendulumField> fields = PendulumRegistry.Descriptors[movement.DescriptorId!.Value].Fields;
        Assert.Equal((ulong)groundSlot, PendulumCodec.Integer(fields[PawnBlackboard.ActiveNodeSlotField], movement.Value));
    }

    private static void AssertGround(CharacterState state)
    {
        NodeLayout layout = state.Nodes;
        Assert.True(layout.GroundSlot >= 0);
        Assert.Equal((-1, -1, layout.GroundSlot), (layout.SlideSlot, layout.TransitionSlot, layout.ActiveSlot));
        Assert.NotEqual(0UL, layout.LaidOut & (1UL << layout.GroundSlot));
    }

    private static CharacterSimulation Sprinting(out uint frame)
    {
        CharacterSimulation simulation = new(default);
        for (frame = 1; frame <= 45; frame++)
        {
            Step(simulation, frame, MovementButtons.Sprint);
        }

        return simulation;
    }

    private static CharacterSimulation Sliding(out uint frame, int ticks)
    {
        CharacterSimulation simulation = Sprinting(out frame);
        Step(simulation, frame++, MovementButtons.Sprint | MovementButtons.Crouch);
        for (int tick = 1; tick < ticks; tick++)
        {
            Step(simulation, frame++, MovementButtons.Sprint | MovementButtons.Crouch | MovementButtons.PreviousCrouch);
        }

        return simulation;
    }

    private static CharacterState Step(CharacterSimulation simulation, uint frame, MovementButtons buttons) =>
        simulation.StepOnGround(new CharacterInput(frame, 0), new MovementInput(frame, 126, 0, buttons), 0, Tick,
            GroundMovementParameters.Armed, steerAboveMaximum: true, fixBrakingOvershoot: false,
            sprint: SprintMovementParameters.Armed, slide: SlideMovementParameters.Default);
}
