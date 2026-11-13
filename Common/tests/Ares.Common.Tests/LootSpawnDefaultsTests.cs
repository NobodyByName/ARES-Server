using Ares.Common.Game.Loot;
using Ares.Structs.Game.World;

namespace Ares.Common.Tests;

public sealed class LootSpawnDefaultsTests
{
    [Fact]
    public void A_tag_takes_its_own_chance_else_its_nearest_parents()
    {
        Assert.Equal(0.451, LootSpawnDefaults.Chance("Socket.LootContainer.Commercial.GvtDesk.Drawer"));
        Assert.Equal(0.094, LootSpawnDefaults.Chance("Socket.LootContainer.Commercial.Centerbench.Drawers"));
        Assert.Equal(0.080, LootSpawnDefaults.Chance("Socket.LootCOntainer.Medical.RsrCartShelf.Drawer01"));
        Assert.Equal(0.038, LootSpawnDefaults.Chance("Socket.LootContainer.ARC.Courier"));
        Assert.Equal(0, LootSpawnDefaults.Chance("Socket.Loot.Table"));
        Assert.Equal(0, LootSpawnDefaults.Chance(""));
    }

    [Fact]
    public void Chances_are_probabilities_with_one_entry_per_tag()
    {
        Assert.All(LootSpawnDefaults.Chances, entry => Assert.InRange(entry.Chance, 0, 1));
        Assert.Equal(LootSpawnDefaults.Chances.Count, LootSpawnDefaults.Chances.Select(entry => entry.Tag.ToUpperInvariant()).Distinct().Count());
    }

    [Fact]
    public void A_loot_value_scales_the_tags_chance_and_a_value_without_an_entry_counts_as_no_value()
    {
        const string Tag = "Socket.LootContainer.Medical.Rsr.CabinetM01.Drawer";
        Assert.Equal(LootSpawnDefaults.Chance(Tag), LootSpawnDefaults.Chance(Tag, PointOfInterestLootValue.Unknown));
        Assert.Equal(LootSpawnDefaults.Chance(Tag), LootSpawnDefaults.Chance(Tag, PointOfInterestLootValue.Low));
        Assert.Equal(LootSpawnDefaults.Multiplier(PointOfInterestLootValue.Unknown), LootSpawnDefaults.Multiplier(PointOfInterestLootValue.Low));
        Assert.Equal(LootSpawnDefaults.Chance(Tag) * LootSpawnDefaults.Multiplier(PointOfInterestLootValue.Medium), LootSpawnDefaults.Chance(Tag, PointOfInterestLootValue.Medium));
        Assert.Equal(LootSpawnDefaults.Chance(Tag) * LootSpawnDefaults.Multiplier(PointOfInterestLootValue.High), LootSpawnDefaults.Chance(Tag, PointOfInterestLootValue.High));
        Assert.True(LootSpawnDefaults.Chance(Tag, PointOfInterestLootValue.Medium) > LootSpawnDefaults.Chance(Tag, PointOfInterestLootValue.Unknown));
        Assert.True(LootSpawnDefaults.Chance(Tag, PointOfInterestLootValue.High) > LootSpawnDefaults.Chance(Tag, PointOfInterestLootValue.Unknown));
        Assert.Equal(0, LootSpawnDefaults.Chance("Socket.Loot.Table", PointOfInterestLootValue.High));
    }

    [Fact]
    public void A_scaled_chance_never_exceeds_one()
    {
        const string Desk = "Socket.LootContainer.Commercial.GvtDesk.Drawer";
        Assert.True(LootSpawnDefaults.Chance(Desk) * LootSpawnDefaults.Multiplier(PointOfInterestLootValue.High) > 1);
        Assert.Equal(1, LootSpawnDefaults.Chance(Desk, PointOfInterestLootValue.High));
        Assert.All(
            LootSpawnDefaults.Chances.SelectMany(entry => Enum.GetValues<PointOfInterestLootValue>().Select(value => LootSpawnDefaults.Chance(entry.Tag, value))),
            chance => Assert.InRange(chance, 0, 1));
    }

    [Fact]
    public void Multipliers_are_positive_with_one_entry_per_loot_value_and_the_plain_value_as_the_unit()
    {
        Assert.All(LootSpawnDefaults.ValueMultipliers, entry => Assert.True(entry.Multiplier > 0));
        Assert.Equal(LootSpawnDefaults.ValueMultipliers.Count, LootSpawnDefaults.ValueMultipliers.Select(entry => entry.Value).Distinct().Count());
        Assert.Equal(1, LootSpawnDefaults.Multiplier(PointOfInterestLootValue.Unknown));
    }

    [Fact]
    public void Spawners_place_only_the_enabled_classes_and_each_is_a_class_the_dam_spawners_draw()
    {
        HashSet<string> drawn = [.. DamDynamicLoot.Spawners.SelectMany(spawner => spawner.Classes).Select(entry => entry.Class)];
        Assert.All(DynamicLootDefaults.EnabledClasses, path => Assert.Contains(path, drawn));
        Assert.All(DynamicLootDefaults.EnabledClasses, path => Assert.True(DynamicLootDefaults.Enabled(path)));
        Assert.Equal(DynamicLootDefaults.EnabledClasses.Count, drawn.Count(DynamicLootDefaults.Enabled));
    }
}
