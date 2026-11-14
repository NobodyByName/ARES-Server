using Ares.Raid.World.Movement;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Core.Serialization;
using Ares.Structs.Game.Movement;
using Ares.Structs.Game.World;

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
    public void A_crouch_press_while_sprinting_slides_and_blends_its_velocity_in_over_three_ticks()
    {
        CharacterSimulation sliding = Sprinting(out uint frame);
        CharacterSimulation sprinting = sliding.Copy();
        const MovementButtons Press = MovementButtons.Sprint | MovementButtons.Crouch;
        const MovementButtons Hold = Press | MovementButtons.PreviousCrouch;

        CharacterState pressed = SlideStep(sliding, frame, 126, Press);
        Assert.Equal(CharacterStance.Stand, pressed.Stance);
        Assert.Equal(SlideStep(sprinting, frame, 126, MovementButtons.Sprint).Velocity, pressed.Velocity);
        Assert.Equal(new SlideState(default, 0) { StartFrame = frame }, pressed.Slide);

        // First slide tick: a third of the slide's velocity, two thirds of the sprint it leaves.
        Vector3D sprint = SlideStep(sprinting, frame + 1, 126, MovementButtons.Sprint).Velocity;
        Vector3D slide = FlatSlideMovement.Step(pressed.Slide!.Value, pressed.Velocity, new MovementInput(frame + 1, 126, 0, Hold), 0, 1.0 / 30, 1.0f / 30,
            SlideMovementParameters.Default, steerAboveMaximum: true, fixBrakingOvershoot: false).Velocity;
        CharacterState first = SlideStep(sliding, frame + 1, 126, Hold);
        Assert.Equal((CharacterStance.Slide, LocoMode.Running), (first.Stance, first.Locomotion));
        Assert.Equal((CharacterStance.Stand, LocoMode.Sprinting), (first.MovementStance, first.MovementLocomotion));
        Assert.Equal(sprint.X + ((slide.X - sprint.X) / 3), first.Velocity.X, 9);
        Assert.Equal(1, first.Slide?.Ticks);

        // Second: two thirds of the slide's, one third of the crouched movement's braking; the movement state crouches.
        CharacterState second = SlideStep(sliding, frame + 2, 126, Hold);
        Assert.Equal((CharacterStance.Slide, LocoMode.Running), (second.Stance, second.Locomotion));
        Assert.Equal((CharacterStance.Crouch, LocoMode.Sprinting), (second.MovementStance, second.MovementLocomotion));
        Assert.InRange(second.Velocity.X / first.Velocity.X, 0.92, 0.95);
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
        while (state.SlideStartedAt is not null);

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
        while (state.SlideStartedAt is not null);

        Assert.Equal((int)Math.Round(SlideMovementParameters.MinimumCancelSeconds * 30), ticks);
    }

    [Fact]
    public void Slide_braking_blends_into_crouch_braking_over_the_exit_ticks()
    {
        CharacterSimulation simulation = Sprinting(out uint frame);
        const MovementButtons Hold = MovementButtons.Sprint | MovementButtons.Crouch | MovementButtons.PreviousCrouch;
        SlideStep(simulation, frame++, 126, MovementButtons.Sprint | MovementButtons.Crouch);
        while (SlideStep(simulation, frame++, 0, Hold).SlideStartedAt is not null)
        {
        }

        MovementInput still = new(frame, 0, 0, Hold);
        for (int tick = 1; tick <= SlideMovementParameters.BlendTicks; tick++)
        {
            Vector3D previous = simulation.State.Velocity;
            Vector3D crouch = FlatGroundMovement.StepVelocity(previous, still with { Frame = frame }, 0, 1.0 / 30,
                GroundMovementParameters.Crouched, steerAboveMaximum: true, fixBrakingOvershoot: false);
            CharacterState state = SlideStep(simulation, frame++, 0, Hold);
            Assert.Equal(CharacterStance.Crouch, state.Stance);
            if (tick < SlideMovementParameters.BlendTicks)
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

    [Fact]
    public void A_roll_press_with_input_rolls_from_the_next_tick_and_blends_back_into_ground_movement()
    {
        CharacterSimulation simulation = new(default);
        uint frame = 1;
        for (; frame <= 20; frame++)
        {
            SlideStep(simulation, frame, 126, MovementButtons.None);
        }

        CharacterState pressed = SlideStep(simulation, frame++, 126, MovementButtons.DodgeRoll);
        int duration = DodgeRollMovement.Duration(DodgeRollKind.Standard);
        Assert.Equal(CharacterStance.Stand, pressed.Stance);
        Assert.False(pressed.Rolling);
        Assert.NotNull(pressed.Roll);

        List<CharacterState> roll = [];
        for (int tick = 0; tick < duration + DodgeRollParameters.ExitBlendTicks; tick++)
        {
            // Holding roll and moving on starts no second roll while one runs.
            roll.Add(SlideStep(simulation, frame++, 126, MovementButtons.DodgeRoll | MovementButtons.PreviousDodgeRoll));
        }

        foreach (CharacterState state in roll.Take(duration))
        {
            Assert.True(state.Rolling);
            Assert.Equal(CharacterStance.Uncontrolled, state.Stance);
            Assert.Equal(LocoMode.Running, state.Locomotion);
            Assert.Equal(pressed.MovementLocomotion, state.MovementLocomotion);
            Assert.Equal(DodgeRollParameters.RollingFlags, state.GraphFlags & DodgeRollParameters.RollingFlags);
        }

        // The tick after the last step still moves with the animation; then ground movement takes over in equal steps.
        CharacterState exit = roll[duration];
        Assert.False(exit.Rolling);
        Assert.Equal(1, exit.RollExitTicks);
        Assert.Equal(0UL, exit.GraphFlags & DodgeRollParameters.RollingFlags);
        Assert.Equal(2, roll[duration + 1].RollExitTicks);
        Assert.Null(roll[^1].Roll);
        Assert.Equal(CharacterStance.Stand, roll[^1].Stance);
    }

    [Theory]
    [InlineData(-1613.0, true)]
    [InlineData(-1414.0, false)]
    [InlineData(-877.0, false)]
    public void Landings_are_heavy_from_the_fall_height_they_are_worth(double vertical, bool heavy) =>
        Assert.Equal(heavy, HeavyLandingParameters.FallHeight(0, vertical, AirMovementParameters.FallAcceleration) > HeavyLandingParameters.Height);

    [Fact]
    public void A_climb_moves_up_the_ladder_line_and_a_crouch_drops_off_keeping_the_crouch_block_for_a_while()
    {
        LadderLine line = LadderLine.From(DamLadders.All[0]);
        Vector3D feet = line.Origin with { Z = line.Origin.Z + LadderParameters.LineStartHeight + 100 };
        CharacterSimulation simulation = new(feet);
        LadderState attached = LadderMovement.Begin(line, topEntry: false) with { Ticks = (int)Math.Ceiling(LadderParameters.AlignmentSeconds * 30) + 1 };
        CharacterState state = Ladder(simulation, 1, 126, 0, attached);
        for (uint frame = 2; frame <= 30; frame++)
        {
            state = Ladder(simulation, frame, 126, 0);
        }

        Assert.True(state.OnLadder);
        Assert.Equal(CharacterStance.Ladder, state.Stance);
        Assert.Equal(LadderParameters.ClimbingFlags | LadderParameters.CrouchInputBlockedFlag,
            state.GraphFlags & (LadderParameters.ClimbingFlags | LadderParameters.CrouchInputBlockedFlag));
        Assert.True(state.Position.Z > feet.Z + 100);

        // Holding crouch drops off the ladder; the crouch input stays blocked a few ticks longer.
        CharacterState dropped = Ladder(simulation, 31, 0, 1UL << LadderParameters.CrouchButtonDownBit);
        Assert.Null(dropped.Ladder);
        CharacterState after = GroundStep(simulation, 32, crouch: false, moving: false);
        Assert.False(after.OnLadder);
        Assert.Equal(0UL, after.GraphFlags & LadderParameters.ClimbingFlags);
        Assert.Equal(LadderParameters.CrouchInputBlockedFlag, after.GraphFlags & LadderParameters.CrouchInputBlockedFlag);
    }

    private static CharacterState Ladder(CharacterSimulation simulation, uint frame, sbyte forward, ulong claims, LadderState? start = null) =>
        simulation.StepOnLadder(new CharacterInput(frame, claims), new MovementInput(frame, forward, 0, MovementButtons.None), 0, 1.0 / 30, null, null, start);

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
