using Ares.Raid.World.Movement;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Core.Serialization;
using Ares.Structs.Game.Movement;

namespace Ares.Raid.Tests.World.Movement;

public sealed class CharacterSimulationTests
{
    [Fact]
    public void Flashlight_press_hold_and_release_match_the_character_rules()
    {
        CharacterSimulation simulation = new(new Vector3D(100, 200, 300));
        (ulong Flags, bool Enabled)[] sequence = [(0, false), (512, true), (1536, true), (1024, true), (0, true), (512, false)];
        uint frame = 1;
        foreach ((ulong flags, bool enabled) in sequence)
        {
            Assert.Equal(enabled, simulation.Step(Claims(frame++, flags)).Flashlight);
        }

        Assert.Equal(new Vector3D(100, 200, 300), simulation.State.Position);
    }

    [Fact]
    public void Electrification_switches_the_light_off_and_a_held_button_does_not_reenable_it()
    {
        CharacterSimulation simulation = new(default, true);
        Assert.False(simulation.Step(Claims(1, 512), electrified: true).Flashlight);
        Assert.False(simulation.Step(Claims(2, 1536)).Flashlight);
        Assert.False(simulation.Step(Claims(3, 1024)).Flashlight);
        Assert.True(simulation.Step(Claims(4, 512)).Flashlight);
    }

    [Fact]
    public void A_speculative_simulation_does_not_change_the_original()
    {
        CharacterSimulation original = new(new Vector3D(100, 200, 300));
        CharacterSimulation copy = original.Copy();
        copy.Step(Claims(1, 512), new Vector3D(400, 500, 600));

        Assert.Equal(new CharacterState(0, new Vector3D(100, 200, 300), false), original.State);
        Assert.Equal(new CharacterState(1, new Vector3D(400, 500, 600), true), copy.State);
    }

    [Fact]
    public void Character_claims_reject_another_slot_descriptor_or_size()
    {
        int descriptor = PendulumInput.SlotDescriptors[PendulumInput.CharacterClaimsSlot];
        PendulumSlot slot = new(PendulumInput.CharacterClaimsSlot, 0, 1, descriptor, PendulumCodec.Default.DefaultValue(descriptor));
        Assert.Throws<InvalidPacketException>(() => CharacterInput.Read(1, slot with { Slot = 0 }));
        Assert.Throws<InvalidPacketException>(() => CharacterInput.Read(1, slot with { DescriptorId = 0 }));
        Assert.Throws<InvalidPacketException>(() => CharacterInput.Read(1, slot with { Value = [] }));
    }

    [Fact]
    public void Crouch_shuffle_expires_and_ground_steps_preserve_the_flashlight()
    {
        CharacterSimulation simulation = new(default);
        for (uint frame = 1; frame <= 14; frame++)
        {
            CharacterState state = GroundStep(simulation, frame, crouch: true, moving: true, frame == 1 ? 512UL : 0);
            Assert.True(state.Shuffling);
            Assert.True(state.Flashlight);
            Assert.Equal((CharacterStance.Crouch, LocoMode.Walking), (state.Stance, state.Locomotion));
        }

        Assert.Equal(new Vector3D(75, 0, 0), simulation.State.Velocity);
        CharacterState after = GroundStep(simulation, 15, crouch: true, moving: true);
        Assert.False(after.Shuffling);
        Assert.Equal(new Vector3D(100, 0, 0), after.Velocity);
        Assert.True(after.Flashlight);
    }

    [Fact]
    public void Stopping_resets_the_crouch_shuffle_timer_for_the_next_walk()
    {
        CharacterSimulation simulation = new(default);
        for (uint frame = 1; frame <= 20; frame++)
        {
            GroundStep(simulation, frame, crouch: true, moving: true);
        }

        Assert.Equal(new Vector3D(150, 0, 0), simulation.State.Velocity);
        for (uint frame = 21; frame <= 40; frame++)
        {
            GroundStep(simulation, frame, crouch: true, moving: false);
        }

        Assert.Null(simulation.State.MovementStartedAt);
        Assert.Equal(LocoMode.Still, simulation.State.Locomotion);
        Assert.True(GroundStep(simulation, 41, crouch: true, moving: true).Shuffling);
    }

    [Fact]
    public void Running_mode_survives_braking_until_the_character_has_stopped()
    {
        CharacterSimulation simulation = new(default);
        for (uint frame = 1; frame <= 20; frame++)
        {
            GroundStep(simulation, frame, crouch: false, moving: true);
        }

        uint next = 21;
        do
        {
            Assert.Equal(LocoMode.Running, GroundStep(simulation, next++, crouch: false, moving: false).Locomotion);
        }
        while (simulation.State.Velocity != default);

        Assert.Equal(LocoMode.Still, GroundStep(simulation, next, crouch: false, moving: false).Locomotion);
    }

    [Fact]
    public void A_crouch_press_while_sprinting_slides_after_one_more_sprint_tick()
    {
        CharacterSimulation sliding = Sprinting(out uint frame);
        CharacterSimulation sprinting = sliding.Copy();
        const MovementButtons Press = MovementButtons.Sprint | MovementButtons.Crouch;
        const MovementButtons Hold = Press | MovementButtons.PreviousCrouch;

        CharacterState pressed = SlideStep(sliding, frame, 126, Press);
        Assert.Equal(CharacterStance.Stand, pressed.Stance);
        Assert.Equal(SlideStep(sprinting, frame, 126, MovementButtons.Sprint).Velocity, pressed.Velocity);

        CharacterState first = SlideStep(sliding, frame + 1, 126, Hold);
        Assert.Equal(CharacterStance.Slide, first.Stance);
        Assert.Equal(SlideStep(sprinting, frame + 1, 126, MovementButtons.Sprint).Velocity, first.Velocity);
        Assert.Equal(1, first.Slide?.Ticks);

        CharacterState second = SlideStep(sliding, frame + 2, 126, Hold);
        Assert.Equal(CharacterStance.Slide, second.Stance);
        Assert.True(second.Velocity.X < first.Velocity.X * (SlideMovementParameters.CrouchStanceSpeedFactor + 0.01));
    }

    [Fact]
    public void A_slide_reports_its_last_tick_then_crouches()
    {
        CharacterSimulation simulation = Sprinting(out uint frame);
        const MovementButtons Hold = MovementButtons.Sprint | MovementButtons.Crouch | MovementButtons.PreviousCrouch;
        SlideStep(simulation, frame++, 126, MovementButtons.Sprint | MovementButtons.Crouch);
        int slideTicks = 0;
        CharacterState state;
        do
        {
            state = SlideStep(simulation, frame++, 0, Hold);
            Assert.Equal(CharacterStance.Slide, state.Stance);
            slideTicks++;
        }
        while (state.Slide is not null);

        Assert.True(slideTicks > SlideMovementParameters.MinimumCancelSeconds * 30);
        Assert.Null(state.SlideStartedAt);
        Assert.True(state.Velocity.X <= GroundMovementParameters.Crouched.MaximumSpeed);
        Assert.Equal(CharacterStance.Crouch, SlideStep(simulation, frame, 0, Hold).Stance);
    }

    [Fact]
    public void Releasing_crouch_ends_the_slide_exactly_at_the_minimum_slide_time()
    {
        CharacterSimulation simulation = Sprinting(out uint frame);
        SlideStep(simulation, frame++, 126, MovementButtons.Sprint | MovementButtons.Crouch);
        int ticks = 0;
        CharacterState state;
        do
        {
            state = SlideStep(simulation, frame++, 126, MovementButtons.Sprint);
            ticks++;
        }
        while (state.Slide is not null);

        Assert.Equal((int)Math.Round(SlideMovementParameters.MinimumCancelSeconds * 30), ticks);
    }

    [Fact]
    public void Slide_braking_blends_into_crouch_braking_over_the_exit_ticks()
    {
        CharacterSimulation simulation = Sprinting(out uint frame);
        const MovementButtons Hold = MovementButtons.Sprint | MovementButtons.Crouch | MovementButtons.PreviousCrouch;
        SlideStep(simulation, frame++, 126, MovementButtons.Sprint | MovementButtons.Crouch);
        while (SlideStep(simulation, frame++, 0, Hold).Slide is not null)
        {
        }

        MovementInput still = new(frame, 0, 0, Hold);
        for (int tick = 1; tick <= SlideMovementParameters.ExitBlendTicks; tick++)
        {
            Vector3D previous = simulation.State.Velocity;
            Vector3D crouch = FlatGroundMovement.StepVelocity(previous, still with { Frame = frame }, 0, 1.0 / 30,
                GroundMovementParameters.Crouched, steerAboveMaximum: true, fixBrakingOvershoot: false);
            CharacterState state = SlideStep(simulation, frame++, 0, Hold);
            Assert.Equal(CharacterStance.Crouch, state.Stance);
            if (tick < SlideMovementParameters.ExitBlendTicks)
            {
                Assert.True(state.Velocity.X > crouch.X);
            }
            else
            {
                Assert.Equal(crouch, state.Velocity);
            }
        }
    }

    [Fact]
    public void Without_slide_parameters_a_crouch_press_while_sprinting_crouches()
    {
        CharacterSimulation simulation = Sprinting(out uint frame);
        CharacterState state = simulation.StepOnGround(new CharacterInput(frame, 0), new MovementInput(frame, 126, 0, MovementButtons.Sprint | MovementButtons.Crouch),
            0, 1.0 / 30, GroundMovementParameters.Armed, steerAboveMaximum: true, fixBrakingOvershoot: false, sprint: SprintMovementParameters.Armed);
        Assert.Equal(CharacterStance.Crouch, state.Stance);
        Assert.Null(state.SlideStartedAt);
    }

    private static CharacterSimulation Sprinting(out uint frame)
    {
        CharacterSimulation simulation = new(default);
        for (frame = 1; frame <= 45; frame++)
        {
            SlideStep(simulation, frame, 126, MovementButtons.Sprint);
        }

        return simulation;
    }

    private static CharacterState SlideStep(CharacterSimulation simulation, uint frame, sbyte forward, MovementButtons buttons)
        => simulation.StepOnGround(new CharacterInput(frame, 0), new MovementInput(frame, forward, 0, buttons), 0, 1.0 / 30,
            GroundMovementParameters.Armed, steerAboveMaximum: true, fixBrakingOvershoot: false,
            sprint: SprintMovementParameters.Armed, slide: SlideMovementParameters.Default);

    private static CharacterState GroundStep(CharacterSimulation simulation, uint frame, bool crouch, bool moving, ulong claims = 0)
    {
        MovementInput input = new(frame, moving ? (sbyte)126 : (sbyte)0, 0, crouch ? MovementButtons.Crouch : MovementButtons.None);
        return simulation.StepOnGround(new CharacterInput(frame, claims), input, 0, 1.0 / 30,
            crouch ? GroundMovementParameters.Crouched : GroundMovementParameters.Armed, steerAboveMaximum: false, fixBrakingOvershoot: false);
    }

    private static CharacterInput Claims(uint frame, ulong flags)
    {
        int descriptor = PendulumInput.SlotDescriptors[PendulumInput.CharacterClaimsSlot];
        byte[] value = PendulumCodec.Default.Value(descriptor, [new PendulumIntegerValue(PendulumInput.CharacterClaimsFlagsField, flags)]);
        return CharacterInput.Read(frame, new PendulumSlot(PendulumInput.CharacterClaimsSlot, 0, 1, descriptor, value));
    }
}
