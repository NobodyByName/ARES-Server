using Ares.Raid.World.Movement;
using Ares.Structs.Core.Serialization;
using Ares.Structs.Game.Movement;

namespace Ares.Raid.Tests.World.Movement;

public sealed class PlayerMovementTests
{
    private const uint Start = 30;
    private const ulong FlashlightDown = 1UL << PendulumInput.FlashlightDownBit;

    [Fact]
    public void Each_input_frame_is_simulated_once_in_order()
    {
        InputHistory history = new();
        Send(history, 0, Start + 3, _ => (PendulumInput.MovementAxisMaximum, MovementButtons.None, 0));
        PlayerMovement movement = new(Start, default, 0);
        CharacterSimulation expected = new(default);

        for (uint frame = Start + 1; frame <= Start + 3; frame++)
        {
            CharacterState state = movement.Advance(history, armed: true);
            CharacterState reference = expected.StepOnGround(new CharacterInput(frame, 0), new MovementInput(frame, PendulumInput.MovementAxisMaximum, 0, MovementButtons.None),
                0, PawnBlackboard.TickSeconds, GroundMovementParameters.Armed, true, false, sprint: SprintMovementParameters.Armed, slide: SlideMovementParameters.Default);
            Assert.Equal(reference.Velocity, state.Velocity);
            Assert.Equal(state, movement.State(frame));
        }

        Assert.Equal(Start + 3, movement.Frame);
        Assert.Equal(0, movement.MissingFrames);
        Assert.Null(movement.State(Start + 4));
    }

    [Fact]
    public void A_missing_frame_holds_the_previous_buttons_without_pressing_them_again()
    {
        InputHistory history = new();
        Send(history, 0, Start + 1, _ => (0, MovementButtons.Crouch, FlashlightDown));
        Send(history, Start + 1, Start + 5, _ => (0, MovementButtons.Crouch | MovementButtons.PreviousCrouch, 0));
        PlayerMovement movement = new(Start, default, 0);

        Assert.True(movement.Advance(history, armed: true).Flashlight);
        CharacterState missing = movement.Advance(history, armed: true);

        Assert.Equal(1, movement.MissingFrames);
        Assert.True(missing.Flashlight);
        Assert.Equal(CharacterStance.Crouch, missing.Stance);
    }

    [Fact]
    public void Armed_characters_run_slower_than_unarmed_ones()
    {
        InputHistory history = new();
        PlayerMovement armed = new(Start, default, 0);
        PlayerMovement unarmed = new(Start, default, 0);
        for (uint latest = Start + 3; latest <= Start + 60; latest += 3)
        {
            Send(history, latest == Start + 3 ? 0 : latest - 3, latest, _ => (PendulumInput.MovementAxisMaximum, MovementButtons.None, 0));
            for (int frame = 0; frame < 3; frame++)
            {
                armed.Advance(history, armed: true);
                unarmed.Advance(history, armed: false);
            }
        }

        Assert.Equal(GroundMovementParameters.Armed.MaximumSpeed, Speed(armed.Latest), 3);
        Assert.Equal(GroundMovementParameters.Unarmed.MaximumSpeed, Speed(unarmed.Latest), 3);
    }

    [Fact]
    public void A_jump_press_takes_the_character_off_the_ground()
    {
        InputHistory history = new();
        Send(history, 0, Start + 3, frame => (0, frame switch
        {
            Start + 2 => MovementButtons.Jump,
            Start + 3 => MovementButtons.Jump | MovementButtons.PreviousJump,
            _ => MovementButtons.None,
        }, 0));
        PlayerMovement movement = new(Start, default, 0);

        Assert.False(movement.Advance(history, armed: true).Unsettled);
        Assert.True(movement.Advance(history, armed: true).Unsettled);
        Assert.True(movement.Advance(history, armed: true).Unsettled);
        Assert.True(movement.Copy().Advance(history, armed: true).Unsettled);
    }

    [Fact]
    public void A_copy_simulates_independently()
    {
        InputHistory history = new();
        Send(history, 0, Start + 2, _ => (PendulumInput.MovementAxisMaximum, MovementButtons.None, 0));
        PlayerMovement movement = new(Start, default, 0);
        movement.Advance(history, armed: true);

        PlayerMovement copy = movement.Copy();
        copy.Advance(history, armed: true);

        Assert.Equal(Start + 1, movement.Frame);
        Assert.Null(movement.State(Start + 2));
        Assert.Equal(Start + 2, copy.Frame);
    }

    private static double Speed(CharacterState state) => Math.Sqrt((state.Velocity.X * state.Velocity.X) + (state.Velocity.Y * state.Velocity.Y));

    // A payload with the movement and claims slots of the frames after the baseline (at most three, like the client).
    private static void Send(InputHistory history, uint baseline, uint latest, Func<uint, (sbyte Forward, MovementButtons Buttons, ulong Claims)> input)
    {
        BitWriter writer = new();
        writer.WriteBit(true);
        writer.WriteBounded(baseline, PendulumFrames.BaselineRange);
        PendulumFrames.WriteRelative(writer, latest, baseline);
        Dictionary<int, PendulumSlot> previous = baseline == 0 ? [] : Slots(baseline);
        for (uint frame = Math.Max(baseline + 1, latest - 2); frame <= latest; frame++)
        {
            Dictionary<int, PendulumSlot> current = Slots(frame);
            PendulumCodec.Default.WriteFrame(writer, current.Values, previous);
            previous = current;
        }

        history.Read(writer.ToArray());
        return;

        Dictionary<int, PendulumSlot> Slots(uint frame)
        {
            (sbyte forward, MovementButtons buttons, ulong claims) = input(frame);
            int movement = PendulumInput.SlotDescriptors[PendulumInput.MovementSlot];
            int character = PendulumInput.SlotDescriptors[PendulumInput.CharacterClaimsSlot];
            byte[] movementValue = PendulumCodec.Default.Value(movement,
            [
                new PendulumIntegerValue(PendulumInput.ForwardField, (ulong)(forward + PendulumInput.MovementAxisBias)),
                new PendulumIntegerValue(PendulumInput.RightField, PendulumInput.MovementAxisBias),
                new PendulumIntegerValue(PendulumInput.MovementFlagsField, (ulong)buttons),
            ]);
            byte[] claimsValue = PendulumCodec.Default.Value(character, [new PendulumIntegerValue(PendulumInput.CharacterClaimsFlagsField, claims)]);
            return new Dictionary<int, PendulumSlot>
            {
                [PendulumInput.MovementSlot] = new(PendulumInput.MovementSlot, 0, (movementValue.Length + 255) / 256, movement, movementValue),
                [PendulumInput.CharacterClaimsSlot] = new(PendulumInput.CharacterClaimsSlot, (movementValue.Length + 255) / 256, (claimsValue.Length + 255) / 256, character, claimsValue),
            };
        }
    }
}
