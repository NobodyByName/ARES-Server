using Ares.Structs.Core.Serialization;
using Ares.Structs.Net.Replication;

namespace Ares.Structs.Game.Characters;

/// <summary>A granted ability in the ability component's list.</summary>
/// <param name="ReplicationId">Element replication id (unique in the list).</param>
/// <param name="Handle">Ability spec handle.</param>
/// <param name="Ability">Exported class default object of the ability.</param>
public readonly record struct GrantedAbility(int ReplicationId, int Handle, ulong Ability);

/// <summary>
/// The ability component's granted abilities (<c>ActivatableAbilities</c>), a fast array sent through its own
/// delta serializer. This game's fast-array header has one more bit after the base replication key than the
/// stock engine's.
/// </summary>
public static class ActivatableAbilities
{
    /// <summary>Largest number of abilities in one update.</summary>
    public const int MaxAbilities = 64;

    private const int NoIndex = -1;

    /// <summary>Writes the field payload of the list's first state: every ability added, at level 0 with no input.</summary>
    /// <param name="writer">Destination.</param>
    /// <param name="abilities">The abilities.</param>
    public static void WriteInitial(BitWriter writer, IReadOnlyList<GrantedAbility> abilities)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(abilities);
        if (abilities.Count is < 1 or > MaxAbilities || abilities.Any(ability => ability.Ability < 3 || !NetGuid.IsStatic(ability.Ability)))
        {
            throw new InvalidPacketException("Invalid ability list");
        }

        writer.WriteBit(false); // Not the delta-struct form.
        WriteInt(writer, 1); // Array replication key.
        WriteInt(writer, NoIndex); // Base replication key.
        writer.WriteBit(false);
        WriteInt(writer, 0); // Deleted elements.
        WriteInt(writer, abilities.Count); // Changed elements.
        foreach (GrantedAbility ability in abilities)
        {
            WriteInt(writer, ability.ReplicationId);
            WriteInt(writer, ability.Handle);
            NetGuid.Write(writer, ability.Ability);
            WriteInt(writer, 0); // Level.
            WriteInt(writer, NoIndex); // Input id.
            NetGuid.Write(writer, 0); // Source object.
            writer.WriteBit(true); // No dynamic ability tags.
            writer.WriteBits(0, 16); // Replicated instances.
        }
    }

    private static void WriteInt(BitWriter writer, int value) => writer.WriteBits(unchecked((uint)value), 32);
}
