using Ares.Structs.Core.Serialization;
using Ares.Structs.Game.Characters;
using Ares.Structs.Game.Replication;
using Ares.Structs.Net.Replication;

namespace Ares.Structs.Tests.Game;

public sealed class CharacterDataTests
{
    private static ReplicationCatalog Catalog => GameReplication.Catalog;

    [Fact]
    public void Attribute_defaults_name_replicated_attributes()
    {
        Assert.Equal(3, CharacterDefaults.AttributeSets.Count);
        foreach (AttributeSetDefaults set in CharacterDefaults.AttributeSets)
        {
            HashSet<string> attributes = [.. Catalog.Records(set.Class).Select(record => Catalog.Property(record.Property).Name)];
            Assert.All(set.Defaults, attribute => Assert.Contains(attribute.Name, attributes));
        }
    }

    [Fact]
    public void Ability_system_and_appearance_use_known_component_classes()
    {
        Assert.True(Catalog.Classes.ContainsKey(CharacterDefaults.AbilitySystem.Class));
        DefaultAppearance appearance = CharacterDefaults.Appearance;
        Assert.True(Catalog.IsA(appearance.ComponentClass, ClassPaths.ActorComponent));
        Assert.All(appearance.SlotSelections, index =>
            Assert.True(index == DefaultAppearance.DefaultSelection || index < appearance.RegistryCount));
    }

    [Fact]
    public void Default_abilities_are_distinct()
    {
        Assert.Equal(12, DefaultAbilities.Raider.Select(grant => (grant.Package, grant.ClassName)).Distinct().Count());
    }

    [Fact]
    public void Added_effect_is_written_as_a_member_stream()
    {
        BitWriter writer = new();
        ActiveGameplayEffects.WriteAdded(writer, 4, 0, [new ActiveEffect(1, 4403, SpawnInEffect.Seconds, 160.9f)]);
        Assert.Equal(
            ("0900000000000000000000000400000004000000143823220000003000000004420000000050000000fc61e810020000402200000080020000c0a20000300000000034e0000000f0303307190200", 619),
            (Convert.ToHexStringLower(writer.ToArray()), writer.Length));
    }

    [Fact]
    public void Removed_effect_is_written_by_replication_id()
    {
        BitWriter writer = new();
        ActiveGameplayEffects.WriteRemoved(writer, 21, 19, [1]);
        Assert.Equal(("2b0000002600000004000000000000000400000000", 162), (Convert.ToHexStringLower(writer.ToArray()), writer.Length));
    }

    [Fact]
    public void Effect_lists_reject_invalid_elements()
    {
        Assert.Throws<InvalidPacketException>(() => ActiveGameplayEffects.WriteAdded(new BitWriter(), 2, 0, []));
        Assert.Throws<InvalidPacketException>(() => ActiveGameplayEffects.WriteAdded(new BitWriter(), 2, 0, [new ActiveEffect(1, 4402, 4, 0)]));
        Assert.Throws<InvalidPacketException>(() => ActiveGameplayEffects.WriteAdded(new BitWriter(), 2, 0, [new ActiveEffect(1, 4403, float.NaN, 0)]));
        Assert.Throws<InvalidPacketException>(() => ActiveGameplayEffects.WriteRemoved(new BitWriter(), 3, 2, []));
    }
}
