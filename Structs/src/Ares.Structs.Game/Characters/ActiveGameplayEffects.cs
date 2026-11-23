using Ares.Structs.Core.Serialization;
using Ares.Structs.Net.Replication;

namespace Ares.Structs.Game.Characters;

/// <summary>An effect in the ability component's list of active effects, applied with no instigator.</summary>
/// <param name="ReplicationId">Element replication id (unique in the list).</param>
/// <param name="Definition">Exported class default object of the effect.</param>
/// <param name="Duration">How long the effect lasts, in seconds. The server ends it; clients do not.</param>
/// <param name="StartServerWorldTime">Server world time the effect started at, in seconds.</param>
public readonly record struct ActiveEffect(int ReplicationId, ulong Definition, float Duration, float StartServerWorldTime);

/// <summary>
/// The effect a character carries while it spawns in. It grants the tags that keep the character immobile and its
/// input disabled; while they are set, the client turns the character's body to the view direction.
/// </summary>
public static class SpawnInEffect
{
    /// <summary>How long a spawning character carries the effect, in seconds.</summary>
    public const float Seconds = 4.0f;

    /// <summary>The effect's class.</summary>
    public static AbilityGrant ClassDefault { get; } = new("/Script/Angelscript", "CharacterSpawnInEffect");
}

/// <summary>
/// The ability component's active effects (<c>ActiveGameplayEffects</c>), a fast array sent through its own delta
/// serializer, each element as a stream of numbered members. The list's replication key starts at 1 and grows by one
/// with each element added or removed. This game's fast-array header has one more bit after the base replication key
/// than the stock engine's.
/// </summary>
public static class ActiveGameplayEffects
{
    /// <summary>Largest number of elements in one update.</summary>
    public const int MaxEffects = 64;

    /// <summary>The list's replication key before its first change.</summary>
    public const int InitialKey = 1;

    private const int ContextBits = 12;
    private const uint UnknownContext = 2;
    private const int ContextReplicatedMembers = 7;

    // Members of an element, numbered from 1 in the order they are sent.
    private enum Member
    {
        End,
        Definition,
        ModifiedAttributes,
        Duration,
        Period,
        ChanceToApplyToTarget,
        DynamicGrantedTags,
        DynamicAssetTags,
        Modifiers,
        StackCount,
        GrantedAbilitySpecs,
        EffectContext,
        Level,
        PredictionKey,
        GrantedAbilityHandles,
        StartServerWorldTime,
    }

    /// <summary>Writes the field payload that adds effects to the list.</summary>
    /// <param name="writer">Destination.</param>
    /// <param name="arrayKey">The list's replication key after the change.</param>
    /// <param name="baseKey">The list's replication key the client last received (0 before the first update).</param>
    /// <param name="effects">The added effects.</param>
    public static void WriteAdded(BitWriter writer, int arrayKey, int baseKey, IReadOnlyList<ActiveEffect> effects)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(effects);
        if (effects.Count is < 1 or > MaxEffects
            || effects.Any(effect => effect.Definition < 3 || !NetGuid.IsStatic(effect.Definition) || !float.IsFinite(effect.Duration) || !float.IsFinite(effect.StartServerWorldTime)))
        {
            throw new InvalidPacketException("Invalid effect list");
        }

        WriteHeader(writer, arrayKey, baseKey, 0, effects.Count);
        foreach (ActiveEffect effect in effects)
        {
            WriteInt(writer, effect.ReplicationId);
            writer.WriteBit(true); // Unknown; set on every element.
            WriteMember(writer, Member.Definition);
            NetGuid.Write(writer, effect.Definition);
            WriteEmptyArray(writer, Member.ModifiedAttributes);
            WriteFloat(writer, Member.Duration, effect.Duration);
            WriteFloat(writer, Member.Period, 0);
            WriteFloat(writer, Member.ChanceToApplyToTarget, 1);
            WriteMember(writer, Member.DynamicGrantedTags);
            writer.WriteBit(true); // No tags.
            WriteMember(writer, Member.DynamicAssetTags);
            writer.WriteBit(true); // No tags.
            WriteEmptyArray(writer, Member.Modifiers);
            WriteMember(writer, Member.StackCount);
            WriteInt(writer, 1);
            WriteEmptyArray(writer, Member.GrantedAbilitySpecs);
            WriteMember(writer, Member.EffectContext);
            writer.WriteBit(true); // Has a context.
            writer.WriteBits(UnknownContext, ContextBits);
            writer.WriteBits(0, ContextReplicatedMembers); // No instigator, causer or other context member.
            WriteFloat(writer, Member.Level, 0);
            WriteMember(writer, Member.PredictionKey);
            writer.WriteBits(0, 2); // Not predicted.
            WriteEmptyArray(writer, Member.GrantedAbilityHandles);
            WriteFloat(writer, Member.StartServerWorldTime, effect.StartServerWorldTime);
            WriteMember(writer, Member.End);
        }
    }

    /// <summary>Writes the field payload that removes effects from the list.</summary>
    /// <param name="writer">Destination.</param>
    /// <param name="arrayKey">The list's replication key after the change.</param>
    /// <param name="baseKey">The list's replication key the client last received.</param>
    /// <param name="replicationIds">Replication ids of the removed elements.</param>
    public static void WriteRemoved(BitWriter writer, int arrayKey, int baseKey, IReadOnlyList<int> replicationIds)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(replicationIds);
        if (replicationIds.Count is < 1 or > MaxEffects)
        {
            throw new InvalidPacketException("Invalid effect list");
        }

        WriteHeader(writer, arrayKey, baseKey, replicationIds.Count, 0);
        foreach (int id in replicationIds)
        {
            WriteInt(writer, id);
        }
    }

    private static void WriteHeader(BitWriter writer, int arrayKey, int baseKey, int removed, int changed)
    {
        writer.WriteBit(true); // The delta-struct form.
        WriteInt(writer, arrayKey);
        WriteInt(writer, baseKey);
        writer.WriteBit(false);
        WriteInt(writer, removed);
        WriteInt(writer, changed);
    }

    private static void WriteEmptyArray(BitWriter writer, Member member)
    {
        WriteMember(writer, member);
        writer.WriteBits(0, 16); // Element count.
        WriteMember(writer, Member.End);
    }

    private static void WriteFloat(BitWriter writer, Member member, float value)
    {
        WriteMember(writer, member);
        writer.WriteBits(BitConverter.SingleToUInt32Bits(value), 32);
    }

    private static void WriteMember(BitWriter writer, Member member) => writer.WritePacked((uint)member);

    private static void WriteInt(BitWriter writer, int value) => writer.WriteBits(unchecked((uint)value), 32);
}
