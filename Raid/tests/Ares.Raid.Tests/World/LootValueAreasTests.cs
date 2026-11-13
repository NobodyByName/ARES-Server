using Ares.Common.Game.Loot;
using Ares.Raid.World;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Game.World;

namespace Ares.Raid.Tests.World;

public sealed class LootValueAreasTests
{
    private const string Tag = "Socket.LootContainer.Commercial.Centerbench.Drawers";

    private static readonly LootValueAreas s_dam = new(DamPointsOfInterest.All);

    [Fact]
    public void A_socket_inside_the_testing_annex_is_medium_and_one_inside_research_and_administration_is_high()
    {
        Assert.Equal(PointOfInterestLootValue.Medium, s_dam.At(SocketIn("ID_LOCATION_DAM_TESTINGANNEX").Location));
        Assert.Equal(PointOfInterestLootValue.High, s_dam.At(SocketIn("ID_LOCATION_DAM_ADMIN").Location));
    }

    [Fact]
    public void A_point_outside_every_area_with_a_loot_value_has_none()
    {
        Assert.Equal(PointOfInterestLootValue.Unknown, s_dam.At(new Vector3D(1e7, 1e7, 1e7)));
        Assert.Equal(PointOfInterestLootValue.Unknown, s_dam.At(new Vector3D(double.NaN, 0, 0)));
        Assert.Contains(DamLootSockets.All, socket => s_dam.At(socket.Location) == PointOfInterestLootValue.Unknown);
    }

    [Fact]
    public void Where_areas_overlap_the_highest_loot_value_counts()
    {
        PointOfInterest low = Area(PointOfInterestLootValue.Low, 0, 1000);
        PointOfInterest medium = Area(PointOfInterestLootValue.Medium, 0, 500);
        PointOfInterest high = Area(PointOfInterestLootValue.High, 0, 100);
        PointOfInterest plain = Area(PointOfInterestLootValue.Unknown, 0, 2000);
        LootValueAreas areas = new([plain, high, low, medium]);

        Assert.Equal(PointOfInterestLootValue.High, areas.At(new Vector3D(50, 50, 50)));
        Assert.Equal(PointOfInterestLootValue.Medium, areas.At(new Vector3D(300, 50, 50)));
        Assert.Equal(PointOfInterestLootValue.Low, areas.At(new Vector3D(800, 50, 50)));
        Assert.Equal(PointOfInterestLootValue.Unknown, areas.At(new Vector3D(1500, 50, 50)));
    }

    [Fact]
    public void A_socket_rolls_the_chance_of_the_loot_value_around_it()
    {
        PointOfInterest high = Area(PointOfInterestLootValue.High, 0, 100);
        LootSocket[] inside = [.. Enumerable.Range(0, 20).Select(index => Socket($"In{index}", new Vector3D(50, 50, 50)))];
        LootSocket[] outside = [.. Enumerable.Range(0, 20).Select(index => Socket($"Out{index}", new Vector3D(5000, 50, 50)))];
        LootSpawnRules rules = new([], [high], (_, value) => value == PointOfInterestLootValue.High ? 1 : 0, true);

        LootSpawnSelection selection = LootSpawnSelection.Select([.. inside, .. outside], rules, [], new Random(1));
        Assert.All(selection.Spawns.Take(inside.Length), Assert.True);
        Assert.All(selection.Spawns.Skip(inside.Length), Assert.False);
        Assert.Equal(inside.Length, selection.Spawned(PointOfInterestLootValue.High));
        Assert.Equal(0, selection.Spawned(PointOfInterestLootValue.Unknown));
        Assert.Equal(outside.Length, selection.SkippedByRoll);
    }

    [Fact]
    public void The_default_multipliers_fill_valuable_areas_more_often_than_plain_ones_over_many_seeds()
    {
        PointOfInterest medium = Area(PointOfInterestLootValue.Medium, 0, 100);
        PointOfInterest high = Area(PointOfInterestLootValue.High, 200, 300);
        LootSocket[] plain = [.. Enumerable.Range(0, 100).Select(index => Socket($"Plain{index}", new Vector3D(5000, 50, 50)))];
        LootSocket[] middle = [.. Enumerable.Range(0, 100).Select(index => Socket($"Middle{index}", new Vector3D(50, 50, 50)))];
        LootSocket[] best = [.. Enumerable.Range(0, 100).Select(index => Socket($"Best{index}", new Vector3D(250, 250, 250)))];
        LootSpawnRules rules = new([], [medium, high], LootSpawnDefaults.Chance, true);
        LootSpawnSelection[] selections = [.. Enumerable.Range(1, 100).Select(seed => LootSpawnSelection.Select([.. plain, .. middle, .. best], rules, [], new Random(seed)))];

        (double untiered, double mediumShare, double highShare) = (Share(selections, PointOfInterestLootValue.Unknown, 100), Share(selections, PointOfInterestLootValue.Medium, 100), Share(selections, PointOfInterestLootValue.High, 100));
        Assert.InRange(untiered, 0.094 * 0.9, 0.094 * 1.1);
        Assert.InRange(mediumShare, 0.094 * 4.6 * 0.9, 0.094 * 4.6 * 1.1);
        Assert.InRange(highShare, 0.094 * 3.4 * 0.9, 0.094 * 3.4 * 1.1);
        Assert.True(untiered < mediumShare);
        Assert.True(untiered < highShare);
    }

    [Fact]
    public void Dam_raids_fill_a_loot_value_area_more_than_the_open_map_without_breaking_a_cap()
    {
        LootSocket[] sockets = [.. DamLootSockets.All.Where(socket => socket.DataLayer is null && LootSocketClasses.Supported(socket).Count > 0)];
        LootSpawnSelection[] selections = [.. Enumerable.Range(1, 100).Select(seed => LootSpawnSelection.Select(sockets, LootSpawnRules.Dam, [], new Random(seed)))];
        double untiered = Share(selections, PointOfInterestLootValue.Unknown, selections[0].Values.Count(entry => entry == PointOfInterestLootValue.Unknown));
        double medium = Share(selections, PointOfInterestLootValue.Medium, selections[0].Values.Count(entry => entry == PointOfInterestLootValue.Medium));
        double high = Share(selections, PointOfInterestLootValue.High, selections[0].Values.Count(entry => entry == PointOfInterestLootValue.High));
        Assert.Contains(PointOfInterestLootValue.Medium, selections[0].Values);
        Assert.Contains(PointOfInterestLootValue.High, selections[0].Values);
        Assert.True(untiered < medium);
        Assert.True(medium <= high * 1.15);
        Assert.Equal(
            selections.Sum(selection => selection.Spawns.Count(spawns => spawns)),
            selections.Sum(selection => Enum.GetValues<PointOfInterestLootValue>().Sum(selection.Spawned)));
    }

    private static double Share(LootSpawnSelection[] selections, PointOfInterestLootValue value, int sockets) => selections.Average(selection => selection.Spawned(value)) / sockets;

    private static LootSocket SocketIn(string areaName)
    {
        PointOfInterest area = DamPointsOfInterest.All.Single(candidate => candidate.Name == areaName);
        return DamLootSockets.All.First(socket => LootVolumeRules.Contains(area.Hulls, socket.Location));
    }

    private static LootSocket Socket(string actor, Vector3D location) =>
        new("Cell", actor, "Component", "Socket", Tag, location, new Rotator(0, 0, 0), Vector3D.One, 0, [], null);

    private static PointOfInterest Area(PointOfInterestLootValue value, double low, double high) => new(
        $"Area{value}{low}",
        null,
        null,
        value,
        [],
        [
            new LootVolumeHull(
            [
                new LootVolumePlane(new Vector3D(1, 0, 0), high),
                new LootVolumePlane(new Vector3D(-1, 0, 0), -low),
                new LootVolumePlane(new Vector3D(0, 1, 0), high),
                new LootVolumePlane(new Vector3D(0, -1, 0), -low),
                new LootVolumePlane(new Vector3D(0, 0, 1), high),
                new LootVolumePlane(new Vector3D(0, 0, -1), -low),
            ]),
        ]);
}
