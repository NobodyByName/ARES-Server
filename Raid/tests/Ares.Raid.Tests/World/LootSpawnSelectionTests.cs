using Ares.Common.Game.Loot;
using Ares.Raid.World;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Game.Loot;
using Ares.Structs.Game.World;

namespace Ares.Raid.Tests.World;

public sealed class LootSpawnSelectionTests
{
    private const string Tag = "Socket.LootContainer.Tech.Server.Door";
    private const int Seeds = 200;

    private static readonly LootSocket[] s_damSockets =
    [
        .. DamLootSockets.All.Where(socket => socket.DataLayer is null && LootSocketClasses.Supported(socket).Count > 0),
    ];

    private static readonly LootSpawnSelection[] s_damSelections =
    [
        .. Enumerable.Range(1, Seeds).Select(seed => LootSpawnSelection.Select(s_damSockets, LootSpawnRules.Dam, [], new Random(seed))),
    ];

    [Fact]
    public void A_sphere_limiter_covers_points_within_its_radius()
    {
        LootLimiter sphere = Limiter(LootLimiterShape.Sphere, new Rotator(0, 0, 0), 500, []);
        Assert.True(LootLimiterRules.IsPointInside(sphere, new Vector3D(1000, 2000, 300)));
        Assert.True(LootLimiterRules.IsPointInside(sphere, new Vector3D(1000, 2499, 300)));
        Assert.False(LootLimiterRules.IsPointInside(sphere, new Vector3D(1000, 2501, 300)));
        Assert.False(LootLimiterRules.IsPointInside(sphere, new Vector3D(1300, 2300, 600)));
    }

    [Fact]
    public void A_box_limiter_covers_half_its_radius_along_each_turned_axis()
    {
        LootLimiter box = Limiter(LootLimiterShape.Box, new Rotator(0, 45, 0), 2000, []);
        double diagonal = 990 / Math.Sqrt(2);

        // 990 cm along the box's turned axes is inside; 990 cm along both world x and y is past its turned x face.
        Assert.True(LootLimiterRules.IsPointInside(box, new Vector3D(1000 + diagonal, 2000 + diagonal, 300)));
        Assert.True(LootLimiterRules.IsPointInside(box, new Vector3D(1000 - diagonal, 2000 + diagonal, 300 + 990)));
        Assert.False(LootLimiterRules.IsPointInside(box, new Vector3D(1000 + 1010, 2000 + 1010, 300)));
        Assert.False(LootLimiterRules.IsPointInside(box, new Vector3D(1000, 2000, 300 + 1010)));
        Assert.True(LootLimiterRules.IsPointInside(box, new Vector3D(1000 + 990, 2000, 300)));
        Assert.False(LootLimiterRules.IsPointInside(box, new Vector3D(1000 + 990, 2000 + 990, 300)));
    }

    [Fact]
    public void A_volume_limiter_covers_points_inside_any_convex_part()
    {
        LootLimiter volumes = Limiter(LootLimiterShape.Volumes, new Rotator(0, 0, 0), 0, [new LootLimiterVolume("Volume", [Cube(0, 100), Cube(500, 600)])]);
        Assert.True(LootLimiterRules.IsPointInside(volumes, new Vector3D(50, 50, 50)));
        Assert.True(LootLimiterRules.IsPointInside(volumes, new Vector3D(550, 600, 500)));
        Assert.False(LootLimiterRules.IsPointInside(volumes, new Vector3D(300, 50, 50)));
        Assert.False(LootLimiterRules.IsPointInside(volumes, new Vector3D(50, 50, 101)));
    }

    [Fact]
    public void A_point_without_finite_coordinates_is_never_inside_a_limiter()
    {
        LootLimiter[] limiters =
        [
            Limiter(LootLimiterShape.Sphere, new Rotator(0, 0, 0), 1e300, []),
            Limiter(LootLimiterShape.Box, new Rotator(10, 20, 30), 1e300, []),
            Limiter(LootLimiterShape.Volumes, new Rotator(0, 0, 0), 0, [new LootLimiterVolume("Volume", [Cube(-1e300, 1e300)])]),
            .. DamLootLimiters.All,
        ];
        Vector3D[] points =
        [
            new(double.NaN, 0, 0),
            new(0, double.PositiveInfinity, 0),
            new(0, 0, double.NegativeInfinity),
            new(1e308, -1e308, 1e308),
        ];

        Assert.All(limiters, limiter => Assert.All(points[..3], point => Assert.False(LootLimiterRules.IsPointInside(limiter, point))));
        Assert.All(DamLootLimiters.All, limiter => Assert.False(LootLimiterRules.IsPointInside(limiter, points[3])));
    }

    [Fact]
    public void A_limit_applies_to_its_own_tag_and_the_tags_below_it_without_case()
    {
        Assert.True(LootLimiterRules.Matches(Tag, Tag));
        Assert.True(LootLimiterRules.Matches("socket.lootcontainer.TECH", Tag));
        Assert.True(LootLimiterRules.Matches("Socket.LootContainer", "Socket.LootCOntainer.Medical.RsrCartShelf.Drawer01"));
        Assert.False(LootLimiterRules.Matches("Socket.Loot", Tag));
        Assert.False(LootLimiterRules.Matches("Socket.LootContainer.Tech.Server", "Socket.LootContainer.Tech.ServerRack"));
        Assert.False(LootLimiterRules.Matches(Tag, "Socket.LootContainer.Tech"));
    }

    [Fact]
    public void A_group_gets_no_more_than_its_types_most_containers()
    {
        int most = LootContainerTypes.Find(Tag)!.MaxPerGroup;
        LootSocket[] rack = [.. Enumerable.Range(0, most + 3).Select(index => Socket("Rack", $"Door_{index}", Tag, new Vector3D(0, 0, 0)))];
        LootSocket[] otherRack = [.. rack.Select(socket => socket with { Component = "Other" })];
        LootSpawnRules always = new([], [], (_, _) => 1, true);

        LootSpawnSelection selection = LootSpawnSelection.Select([.. rack, .. otherRack], always, [], new Random(3));
        Assert.Equal(most, selection.Spawns.Take(rack.Length).Count(spawns => spawns));
        Assert.Equal(most, selection.Spawns.Skip(rack.Length).Count(spawns => spawns));
        Assert.Equal((0, 6, 0), (selection.SkippedByRoll, selection.SkippedByGroupCap, selection.SkippedByLimiter));
    }

    [Fact]
    public void A_limiter_caps_its_tag_and_the_tags_below_it_and_unlimited_or_inactive_entries_cap_nothing()
    {
        LootSocket[] servers = [.. Enumerable.Range(0, 6).Select(index => Socket($"Server{index}", "Door", Tag, new Vector3D(1000, 2000 + index, 300)))];
        LootSocket[] outside = [.. servers.Select(socket => socket with { Location = new Vector3D(9000, 9000, 9000) })];
        LootSocket[] medical = [.. Enumerable.Range(0, 4).Select(index => Socket($"Cabinet{index}", "Drawer", "Socket.LootContainer.Medical.Rsr.CabinetM01.Drawer", new Vector3D(1000, 2000, 300)))];
        LootLimiter limiter = Limiter(LootLimiterShape.Sphere, new Rotator(0, 0, 0), 500, []) with
        {
            Limits = [new LootLimit("Socket.LootContainer.TECH", false, 2), new LootLimit("Socket.LootContainer.Medical", true, 0)],
        };
        LootLimiter inactive = limiter with { DataLayer = "DataLayer_Inactive", Limits = [new LootLimit("Socket.LootContainer", false, 0)] };
        LootSpawnRules always = new([limiter, inactive], [], (_, _) => 1, true);

        LootSpawnSelection selection = LootSpawnSelection.Select([.. servers, .. outside, .. medical], always, [], new Random(5));
        Assert.Equal(2, selection.Spawns.Take(servers.Length).Count(spawns => spawns));
        Assert.All(selection.Spawns.Skip(servers.Length), Assert.True);
        Assert.Equal((0, 0, 4), (selection.SkippedByRoll, selection.SkippedByGroupCap, selection.SkippedByLimiter));
    }

    [Fact]
    public void Without_rolls_every_socket_spawns()
    {
        LootSpawnSelection selection = LootSpawnSelection.Select(s_damSockets, LootSpawnRules.EverySocket, [], new Random(1));
        Assert.All(selection.Spawns, Assert.True);
    }

    [Fact]
    public void A_seed_always_selects_the_same_sockets()
    {
        LootSpawnSelection again = LootSpawnSelection.Select(s_damSockets, LootSpawnRules.Dam, [], new Random(1));
        Assert.Equal(s_damSelections[0], again with { Spawns = s_damSelections[0].Spawns, Values = s_damSelections[0].Values });
        Assert.Equal(s_damSelections[0].Values, again.Values);
        Assert.Equal(s_damSelections[0].Spawns, again.Spawns);
        Assert.NotEqual(s_damSelections[0].Spawns, s_damSelections[1].Spawns);
    }

    [Fact]
    public void Dam_raids_never_exceed_a_group_or_limiter_cap()
    {
        foreach (LootSpawnSelection selection in s_damSelections)
        {
            LootSocket[] spawned = [.. s_damSockets.Where((_, index) => selection.Spawns[index])];
            Assert.All(
                spawned.GroupBy(socket => (socket.Cell, socket.Actor, socket.Component, Tag: socket.ContainerTag.ToUpperInvariant())),
                group => Assert.True(group.Count() <= LootContainerTypes.Find(group.Key.Tag)!.MaxPerGroup, $"{group.Key} has {group.Count()}"));
            foreach (LootLimiter limiter in DamLootLimiters.All)
            {
                LootSocket[] inside = [.. spawned.Where(socket => LootLimiterRules.IsPointInside(limiter, socket.Location))];
                Assert.All(
                    limiter.Limits.Where(limit => !limit.Unlimited),
                    limit => Assert.True(inside.Count(socket => LootLimiterRules.Matches(limit.Tag, socket.ContainerTag)) <= limit.Limit, $"{limiter.Name} {limit.Tag}"));
            }
        }
    }

    [Fact]
    public void Dam_server_racks_never_get_more_than_two_containers()
    {
        foreach (LootSpawnSelection selection in s_damSelections)
        {
            int most = s_damSockets
                .Where((socket, index) => selection.Spawns[index] && string.Equals(socket.ContainerTag, Tag, StringComparison.OrdinalIgnoreCase))
                .GroupBy(socket => (socket.Cell, socket.Actor, socket.Component))
                .Select(group => group.Count())
                .DefaultIfEmpty(0)
                .Max();
            Assert.InRange(most, 0, 2);
        }
    }

    [Fact]
    public void Dam_sockets_roll_their_chance_and_group_caps_take_at_most_two_fifths_of_the_hits_outside_limiters()
    {
        bool[] limited = [.. s_damSockets.Select(socket => DamLootLimiters.All.Any(limiter => limiter.Limits.Any(limit => !limit.Unlimited && LootLimiterRules.Matches(limit.Tag, socket.ContainerTag)) && LootLimiterRules.IsPointInside(limiter, socket.Location)))];
        foreach (IGrouping<double, int> sameChance in Enumerable.Range(0, s_damSockets.Length).GroupBy(index => LootSpawnDefaults.Chance(s_damSockets[index].ContainerTag, s_damSelections[0].Values[index])))
        {
            double chance = sameChance.Key;
            int rolls = sameChance.Count() * Seeds;
            int spawned = s_damSelections.Sum(selection => sameChance.Count(index => selection.Spawns[index]));
            Assert.True(spawned / (double)rolls <= chance + (4 * Math.Sqrt(chance * (1 - chance) / rolls)), $"{chance}");

            // Area caps take a large part of the hits and a chance of 1 leaves the group cap alone to decide, while the
            // five-socket server racks lose up to about 37% of their hits at a chance near 0.4; so the lower bound is
            // checked on sockets no limiter caps, at chances below one half.
            int[] free = [.. sameChance.Where(index => !limited[index])];
            if (free.Length == 0 || chance >= 0.5)
            {
                continue;
            }

            int freeSpawned = s_damSelections.Sum(selection => free.Count(index => selection.Spawns[index]));
            Assert.True(freeSpawned / (double)(free.Length * Seeds) >= chance * 0.6, $"{chance}");
        }

        // Every roll is counted once: as a spawn or under the reason it did not spawn.
        double hits = s_damSelections.Average(selection => selection.Spawns.Count(spawns => spawns) + selection.SkippedByGroupCap + selection.SkippedByLimiter);
        double expected = s_damSockets.Select((socket, index) => LootSpawnDefaults.Chance(socket.ContainerTag, s_damSelections[0].Values[index])).Sum();
        Assert.InRange(hits, expected * 0.97, expected * 1.03);
        Assert.All(s_damSelections, selection => Assert.Equal(s_damSockets.Length, selection.Spawns.Count(spawns => spawns) + selection.SkippedByRoll + selection.SkippedByGroupCap + selection.SkippedByLimiter));
    }

    [Fact]
    public void A_dam_raid_fills_about_one_socket_in_fourteen()
    {
        double share = s_damSelections.Average(selection => selection.Spawns.Count(spawns => spawns)) / s_damSockets.Length;
        Assert.InRange(share, 0.06, 0.085);
        Assert.All(s_damSelections, selection => Assert.True(selection.SkippedByGroupCap + selection.SkippedByLimiter > 0));
    }

    [Fact]
    public void A_raid_uses_the_dam_spawn_rolls_by_default()
    {
        DamWorlds worlds = new(() => 0, DamWorlds.Lightings[0], () => 123456789);
        worlds.Prepare("match", [1]);
        LootSpawnCounts counts = worlds.Current!.Loot.Counts;
        Assert.InRange(counts.Spawned, 150, 350);
        Assert.True(counts.SkippedByRoll > 3000);
        Assert.Equal(counts.Spawned, counts.SpawnedByLootValue.Values.Sum());
        Assert.True(counts.SpawnedByLootValue[PointOfInterestLootValue.High] > 0);
        Assert.Equal(counts.Sockets, counts.Spawned + counts.InactiveLayer + counts.NoContainerType + counts.Unsupported + counts.SkippedByRoll + counts.SkippedByGroupCap + counts.SkippedByLimiter);
    }

    private static LootSocket Socket(string actor, string socket, string tag, Vector3D location) =>
        new("Cell", actor, "Component", socket, tag, location, new Rotator(0, 0, 0), Vector3D.One, 0, [], null);

    private static LootLimiter Limiter(LootLimiterShape shape, Rotator rotation, double radius, IReadOnlyList<LootLimiterVolume> volumes) =>
        new("Cell", "Limiter", "Test", null, shape, new Vector3D(1000, 2000, 300), rotation, radius, volumes, [new LootLimit("Socket.LootContainer", false, 1)]);

    private static LootVolumeHull Cube(double low, double high) => new(
    [
        new LootVolumePlane(new Vector3D(1, 0, 0), high),
        new LootVolumePlane(new Vector3D(-1, 0, 0), -low),
        new LootVolumePlane(new Vector3D(0, 1, 0), high),
        new LootVolumePlane(new Vector3D(0, -1, 0), -low),
        new LootVolumePlane(new Vector3D(0, 0, 1), high),
        new LootVolumePlane(new Vector3D(0, 0, -1), -low),
    ]);
}
