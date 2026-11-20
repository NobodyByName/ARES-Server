using Ares.Common.Game.Loot;
using Ares.Structs.Game.World;

namespace Ares.Common.Tests;

/// <summary>Logic tier: how spawn settings turn a container or loose item tag and an area's loot value into a chance. Values are invented.</summary>
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
        new HashSet<string>(StringComparer.Ordinal),
        new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
        {
            ["Socket.Loot"] = 0.1,
            ["Socket.Loot.Nature"] = 0.01,
        });

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

    [Fact]
    public void A_loose_item_tag_takes_its_own_chance_else_its_nearest_parents_scaled_by_the_area()
    {
        Assert.Equal(0.01, s_settings.LooseChance("Socket.Loot.Nature.Pine"));
        Assert.Equal(0.1, s_settings.LooseChance("Socket.Loot.Console"));
        Assert.Equal(0.1, s_settings.LooseChance("socket.loot"));
        Assert.Equal(0, s_settings.LooseChance("Socket.LootContainer.Tech"));
        Assert.Equal(0, s_settings.LooseChance(Drawer));
        Assert.Equal(0, s_settings.Chance("Socket.Loot.Console"));
        Assert.Equal(0.1 * 2, s_settings.LooseChance("Socket.Loot.Console", PointOfInterestLootValue.Medium), 12);
        Assert.Equal(0.01 * 0.5, s_settings.LooseChance("Socket.Loot.Nature.Pine", PointOfInterestLootValue.Low), 12);
    }

    [Fact]
    public void Limiter_defaults_fill_only_what_the_limiter_and_its_limits_leave_unset()
    {
        LootLimiterDefaults defaults = new(LootLimiterShape.Sphere, 1000, true, 0);
        LootLimiter unset = new("", "Limiter", "Name", null, null, default, default, null, [],
            [new LootLimit("Socket.LootContainer", null, null), new LootLimit("Socket.Loot", false, 3)]);
        LootLimiter set = unset with { Shape = LootLimiterShape.Box, Radius = 200, Limits = [new LootLimit("Socket.LootContainer", false, null)] };

        LootLimiter resolved = defaults.Apply(unset);
        Assert.Equal((LootLimiterShape.Sphere, 1000.0), (resolved.Shape, resolved.Radius));
        Assert.Equal([new LootLimit("Socket.LootContainer", true, 0), new LootLimit("Socket.Loot", false, 3)], resolved.Limits);
        LootLimiter kept = defaults.Apply(set);
        Assert.Equal((LootLimiterShape.Box, 200.0), (kept.Shape, kept.Radius));
        Assert.Equal([new LootLimit("Socket.LootContainer", false, 0)], kept.Limits);
    }
}
