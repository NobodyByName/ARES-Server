using Ares.Structs.Core.Serialization;

namespace Ares.Structs.Game.Movement;

/// <summary>Character button claims associated with one simulation input frame.</summary>
/// <param name="Frame">Input frame number.</param>
/// <param name="Flags">Packed character button flags.</param>
public readonly record struct CharacterInput(uint Frame, ulong Flags)
{
    /// <summary>Whether slow walking is requested.</summary>
    public bool WalkRequested => (Flags & (1UL << PendulumInput.WalkBit)) != 0;

    /// <summary>A new flashlight press, rather than a held button.</summary>
    public bool FlashlightPressed => (Flags & (1UL << PendulumInput.FlashlightDownBit)) != 0
        && (Flags & (1UL << PendulumInput.FlashlightPreviousDownBit)) == 0;

    /// <summary>Reads the character button claims from a decoded input slot.</summary>
    /// <param name="frame">Input frame number.</param>
    /// <param name="slot">Decoded claims slot.</param>
    /// <returns>The frame's claims.</returns>
    public static CharacterInput Read(uint frame, PendulumSlot slot)
    {
        ArgumentNullException.ThrowIfNull(slot);
        int descriptor = PendulumInput.SlotDescriptors[PendulumInput.CharacterClaimsSlot];
        if (slot.Slot != PendulumInput.CharacterClaimsSlot || slot.DescriptorId != descriptor
            || slot.Value.Length != PendulumRegistry.Descriptors[descriptor].Size)
        {
            throw new InvalidPacketException("Invalid character input slot");
        }

        PendulumField flags = PendulumRegistry.Descriptors[descriptor].Fields[PendulumInput.CharacterClaimsFlagsField];
        return new CharacterInput(frame, PendulumCodec.Integer(flags, slot.Value));
    }
}
