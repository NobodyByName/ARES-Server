using Ares.Structs.Game.Movement;

namespace Ares.Structs.Tests.Game;

public sealed class MovementInputTests
{
    [Theory]
    [InlineData(217UL, 217UL, 89, 89)]
    [InlineData(128UL, 2UL, 0, -126)]
    [InlineData(217UL, 39UL, 89, -89)]
    [InlineData(128UL, 128UL, 0, 0)]
    public void Movement_axes_decode_to_signed_values(ulong forward, ulong right, int expectedForward, int expectedRight)
    {
        int descriptor = PendulumInput.SlotDescriptors[PendulumInput.MovementSlot];
        byte[] value = PendulumCodec.Default.Value(descriptor,
        [
            new PendulumIntegerValue(PendulumInput.ForwardField, forward),
            new PendulumIntegerValue(PendulumInput.RightField, right),
            new PendulumIntegerValue(PendulumInput.MovementFlagsField, 0),
        ]);
        MovementInput input = MovementInput.Read(123, new PendulumSlot(PendulumInput.MovementSlot, 0, 1, descriptor, value));
        Assert.Equal((123U, expectedForward, expectedRight, MovementButtons.None), (input.Frame, (int)input.Forward, (int)input.Right, input.Buttons));
    }

    [Theory]
    [InlineData(MovementButtons.Jump, true, false, false)]
    [InlineData(MovementButtons.Jump | MovementButtons.PreviousJump, false, false, false)]
    [InlineData(MovementButtons.DodgeRoll, false, true, false)]
    [InlineData(MovementButtons.DodgeRoll | MovementButtons.PreviousDodgeRoll, false, false, false)]
    [InlineData(MovementButtons.Crouch, false, false, true)]
    [InlineData(MovementButtons.Crouch | MovementButtons.PreviousCrouch, false, false, false)]
    [InlineData(MovementButtons.PreviousCrouch, false, false, false)]
    public void Movement_presses_exclude_a_held_or_released_button(MovementButtons buttons, bool jump, bool roll, bool crouch)
    {
        MovementInput input = new(1, 0, 0, buttons);
        Assert.Equal((jump, roll, crouch), (input.JumpPressed, input.DodgeRollPressed, input.CrouchPressed));
    }
}
