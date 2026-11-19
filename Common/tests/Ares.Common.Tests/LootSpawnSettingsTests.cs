using Ares.Common.Game.Loot;
using Ares.Structs.Game.World;

namespace Ares.Common.Tests;

/// <summary>Logic tier: how spawn settings turn a container tag and an area's loot value into a chance. Values are invented.</summary>
public sealed class LootSpawnSettingsTests
{
    private const string Drawer = "Socket.Test.Desk.Drawer";

    private static readonly LootSpawnSettings s_settings = new(
        new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
        {
            ["Socket.Test"] = 0.1,
            ["Socket.Test.Desk"] = 0.2,
            [Drawer] = 0.4,
        },
        new Dictionary<PointOfInterestLootValue, double>
        {
            [PointOfInterestLootValue.Unknown] = 0.5,
            [PointOfInterestLootValue.Medium] = 2,
            [PointOfInterestLootValue.High] = 3,
        },
        GroupCaps: true,
        Limiters: true,
        new HashSet<string>(StringComparer.Ordinal));

    [Fact]
    public void A_tag_takes_its_own_chance_else_its_nearest_parents_else_none()
    {
        Assert.Equal(0.4, s_settings.Chance(Drawer));
        Assert.Equal(0.2, s_settings.Chance("Socket.Test.Desk.Shelf"));
        Assert.Equal(0.2, s_settings.Chance("Socket.Test.Desk.Shelf.Left"));
        Assert.Equal(0.1, s_settings.Chance("Socket.Test.Cabinet"));
        Assert.Equal(0.1, s_settings.Chance("Socket.Test"));

        // A sibling whose name merely starts the same is not below the tag.
        Assert.Equal(0, s_settings.Chance("Socket.Tester"));
        Assert.Equal(0, s_settings.Chance("Socket"));
        Assert.Equal(0, s_settings.Chance(""));
        Assert.Throws<ArgumentNullException>(() => s_settings.Chance(null!));
    }

    [Fact]
    public void A_loot_value_without_a_multiplier_uses_the_one_of_no_value_else_one()
    {
        Assert.Equal(0.5, s_settings.Multiplier(PointOfInterestLootValue.Unknown));
        Assert.Equal(0.5, s_settings.Multiplier(PointOfInterestLootValue.Low));
        Assert.Equal(2, s_settings.Multiplier(PointOfInterestLootValue.Medium));

        LootSpawnSettings withoutUnknown = s_settings with { AreaMultipliers = new Dictionary<PointOfInterestLootValue, double> { [PointOfInterestLootValue.High] = 3 } };
        Assert.Equal(1, withoutUnknown.Multiplier(PointOfInterestLootValue.Unknown));
        Assert.Equal(1, withoutUnknown.Multiplier(PointOfInterestLootValue.Low));
        Assert.Equal(3, withoutUnknown.Multiplier(PointOfInterestLootValue.High));
    }

    [Fact]
    public void A_loot_value_scales_the_tags_chance_up_to_one()
    {
        Assert.Equal(0.2 * 0.5, s_settings.Chance("Socket.Test.Desk", PointOfInterestLootValue.Unknown));
        Assert.Equal(0.2 * 0.5, s_settings.Chance("Socket.Test.Desk", PointOfInterestLootValue.Low));
        Assert.Equal(0.2 * 2, s_settings.Chance("Socket.Test.Desk", PointOfInterestLootValue.Medium));
        Assert.Equal(0.2 * 3, s_settings.Chance("Socket.Test.Desk", PointOfInterestLootValue.High));

        // 0.4 times 3 would exceed 1; an unknown tag stays at 0 whatever the area.
        Assert.Equal(1, s_settings.Chance(Drawer, PointOfInterestLootValue.High));
        Assert.Equal(0, s_settings.Chance("Socket.Other", PointOfInterestLootValue.High));
        Assert.All(
            s_settings.Chances.Keys.SelectMany(tag => Enum.GetValues<PointOfInterestLootValue>().Select(value => s_settings.Chance(tag, value))),
            chance => Assert.InRange(chance, 0, 1));
    }
}
