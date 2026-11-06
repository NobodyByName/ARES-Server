using Ares.Structs.Core.Serialization;

namespace Ares.Structs.Game.Movement;

/// <summary>Buttons carried by a movement input.</summary>
[Flags]
public enum MovementButtons : ushort
{
    None = 0,
    Jump = 1 << 0,
    Unstick = 1 << 1,
    Crouch = 1 << 2,
    DodgeRoll = 1 << 3,
    PreviousJump = 1 << 4,
    PreviousCrouch = 1 << 5,
    PreviousDodgeRoll = 1 << 6,
    Sprint = 1 << 7,
    BlendToReplication = 1 << 8,
    Unknown9 = 1 << 9,
    UsingGamepad = 1 << 10,
    PreviousUsingGamepad = 1 << 11,
}

/// <summary>Movement axes and buttons associated with one simulation input frame.</summary>
/// <param name="Frame">Input frame number.</param>
/// <param name="Forward">Signed forward axis before normalization.</param>
/// <param name="Right">Signed right axis before normalization.</param>
/// <param name="Buttons">Movement buttons.</param>
public readonly record struct MovementInput(uint Frame, sbyte Forward, sbyte Right, MovementButtons Buttons)
{
    public bool JumpPressed => Buttons.HasFlag(MovementButtons.Jump) && !Buttons.HasFlag(MovementButtons.PreviousJump);
    public bool CrouchPressed => Buttons.HasFlag(MovementButtons.Crouch) && !Buttons.HasFlag(MovementButtons.PreviousCrouch);
    public bool DodgeRollPressed => Buttons.HasFlag(MovementButtons.DodgeRoll) && !Buttons.HasFlag(MovementButtons.PreviousDodgeRoll);

    /// <summary>Reads signed movement axes and buttons from the movement slot.</summary>
    /// <param name="frame">Input frame number.</param>
    /// <param name="slot">Decoded movement slot.</param>
    /// <returns>The movement input.</returns>
    public static MovementInput Read(uint frame, PendulumSlot slot)
    {
        ArgumentNullException.ThrowIfNull(slot);
        int descriptor = PendulumInput.SlotDescriptors[PendulumInput.MovementSlot];
        if (slot.Slot != PendulumInput.MovementSlot || slot.DescriptorId != descriptor
            || slot.Value.Length != PendulumRegistry.Descriptors[descriptor].Size)
        {
            throw new InvalidPacketException("Invalid movement input slot");
        }

        IReadOnlyList<PendulumField> fields = PendulumRegistry.Descriptors[descriptor].Fields;
        sbyte forward = checked((sbyte)((int)PendulumCodec.Integer(fields[PendulumInput.ForwardField], slot.Value) - PendulumInput.MovementAxisBias));
        sbyte right = checked((sbyte)((int)PendulumCodec.Integer(fields[PendulumInput.RightField], slot.Value) - PendulumInput.MovementAxisBias));
        MovementButtons buttons = (MovementButtons)PendulumCodec.Integer(fields[PendulumInput.MovementFlagsField], slot.Value);
        return new MovementInput(frame, forward, right, buttons);
    }
}
