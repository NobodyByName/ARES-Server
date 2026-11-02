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
}
