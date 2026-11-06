using Ares.Structs.Core.Serialization;
using Ares.Structs.Game.Movement;

namespace Ares.Structs.Game.Inventory;

/// <summary>Weapon controls in one client input frame.</summary>
public readonly record struct WeaponInput(uint Frame, ulong Flags, ulong EquipTarget, byte EquipChange)
{
    public bool FireDown => (Flags & (1UL << WeaponProtocol.FireDownBit)) != 0;
    public bool FirePressed => (Flags & (1UL << WeaponProtocol.FirePressedBit)) != 0;
    public bool FireBlocked => (Flags & (1UL << WeaponProtocol.FireBlockedBit)) != 0;
    public bool AllowPrimary => (Flags & (1UL << WeaponProtocol.AllowPrimaryBit)) != 0;
    public bool ReloadDown => (Flags & (1UL << WeaponProtocol.ReloadDownBit)) != 0;

    /// <summary>Reads the supported weapon input descriptor without trusting its claimed ammo state.</summary>
    public static WeaponInput Read(uint frame, PendulumSlot slot)
    {
        if (slot.DescriptorId != WeaponProtocol.InputDescriptor || slot.Value.Length != PendulumRegistry.Descriptors[WeaponProtocol.InputDescriptor].Size)
        {
            throw new InvalidPacketException("Invalid weapon input descriptor");
        }

        IReadOnlyList<PendulumField> fields = PendulumRegistry.Descriptors[WeaponProtocol.InputDescriptor].Fields;
        return new WeaponInput(frame, PendulumCodec.Integer(fields[WeaponProtocol.InputFlagsField], slot.Value),
            PendulumCodec.Integer(fields[WeaponProtocol.InputEquipTargetField], slot.Value),
            checked((byte)PendulumCodec.Integer(fields[WeaponProtocol.InputEquipChangeField], slot.Value)));
    }
}
