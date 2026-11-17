using Ares.Common.GameData;
using Ares.Structs.Game.World;

namespace Ares.Common.Tests;

/// <summary>
/// Local data checks: the Dam loot area tables (points of interest, loot limiters, dynamic loot spawners and groups)
/// that ARES-GameDataExtract writes from the installed game equal the generated Structs data, row for row, in the same
/// order and with every double exactly equal. They run only when <see cref="DamWorldDataParityTests.DirectoryVariable"/>
/// names an extracted game data directory and are skipped otherwise.
/// </summary>
public sealed class DamLootAreaParityTests
{
    private static readonly string[] s_noStrings = [];
    private static readonly LootVolumeHull[] s_noHulls = [];
    private static readonly LootLimiterVolume[] s_noVolumes = [];
    private static readonly LootLimit[] s_noLimits = [];
    private static readonly DynamicLootClass[] s_noClasses = [];
    private static readonly DynamicLootPoint[] s_noPoints = [];
    private static readonly int[] s_noIndices = [];
    private static readonly DynamicLootSelection[] s_noSelections = [];

    [Fact]
    public void Points_of_interest_equal_the_generated_points()
    {
        GameDataDirectory data = OpenOrSkip();

        IReadOnlyList<PointOfInterest> points = data.ReadTable(MapTables.PointsOfInterest(DamMap.WorldName), GameDataJsonContext.Default.PointOfInterest);

        Assert.Equal(DamPointsOfInterest.All.Select(WithoutLists), points.Select(WithoutLists));
        Assert.Equal(DamPointsOfInterest.All.Select(point => point.LootThemes), points.Select(point => point.LootThemes));
        Assert.Equal(DamPointsOfInterest.All.Select(point => Planes(point.Hulls)), points.Select(point => Planes(point.Hulls)));
    }

    [Fact]
    public void Loot_limiters_equal_the_generated_limiters()
    {
        GameDataDirectory data = OpenOrSkip();

        IReadOnlyList<LootLimiter> limiters = data.ReadTable(MapTables.LootLimiters(DamMap.WorldName), GameDataJsonContext.Default.LootLimiter);

        Assert.Equal(DamLootLimiters.All.Select(WithoutLists), limiters.Select(WithoutLists));
        Assert.Equal(DamLootLimiters.All.Select(limiter => limiter.Limits), limiters.Select(limiter => limiter.Limits));
        Assert.Equal(DamLootLimiters.All.Select(limiter => limiter.Volumes.Select(volume => volume.Actor)), limiters.Select(limiter => limiter.Volumes.Select(volume => volume.Actor)));
        Assert.Equal(
            DamLootLimiters.All.Select(limiter => limiter.Volumes.Select(volume => Planes(volume.Hulls))),
            limiters.Select(limiter => limiter.Volumes.Select(volume => Planes(volume.Hulls))));
    }

    [Fact]
    public void Dynamic_loot_spawners_equal_the_generated_spawners()
    {
        GameDataDirectory data = OpenOrSkip();

        IReadOnlyList<DynamicLootSpawner> spawners = data.ReadTable(MapTables.DynamicLootSpawners(DamMap.WorldName), GameDataJsonContext.Default.DynamicLootSpawner);

        Assert.Equal(DamDynamicLoot.Spawners.Select(WithoutLists), spawners.Select(WithoutLists));
        Assert.Equal(DamDynamicLoot.Spawners.Select(spawner => spawner.Classes), spawners.Select(spawner => spawner.Classes));
        Assert.Equal(DamDynamicLoot.Spawners.Select(spawner => spawner.Points), spawners.Select(spawner => spawner.Points));
    }

    [Fact]
    public void Dynamic_loot_groups_equal_the_generated_groups()
    {
        GameDataDirectory data = OpenOrSkip();
        Dictionary<DynamicLootSpawner, int> indices = new(ReferenceEqualityComparer.Instance);
        for (int i = 0; i < DamDynamicLoot.Spawners.Count; i++)
        {
            indices[DamDynamicLoot.Spawners[i]] = i;
        }

        DynamicLootGroupEntry[] expected = [.. DamDynamicLoot.Groups.Select(group =>
            new DynamicLootGroupEntry(group.Id, group.Path, group.Rounds, group.Min, group.Max, [.. group.Spawners.Select(spawner => indices[spawner])], group.ParentSelections))];

        IReadOnlyList<DynamicLootGroupEntry> groups = data.ReadTable(MapTables.DynamicLootGroups(DamMap.WorldName), GameDataJsonContext.Default.DynamicLootGroupEntry);

        Assert.Equal(expected.Select(WithoutLists), groups.Select(WithoutLists));
        Assert.Equal(expected.Select(group => group.Path), groups.Select(group => group.Path));
        Assert.Equal(expected.Select(group => group.Rounds), groups.Select(group => group.Rounds));
        Assert.Equal(expected.Select(group => group.Spawners), groups.Select(group => group.Spawners));
        Assert.Equal(expected.Select(group => group.ParentSelections), groups.Select(group => group.ParentSelections));
        Assert.Equal(DamDynamicLoot.Groups.Select(group => group.Spawners), groups.Select(group => group.Resolve(DamDynamicLoot.Spawners).Spawners));
    }

    private static GameDataDirectory OpenOrSkip()
    {
        string directory = Environment.GetEnvironmentVariable(DamWorldDataParityTests.DirectoryVariable) ?? string.Empty;
        Assert.SkipWhen(directory.Length == 0,
            $"Local data check: set {DamWorldDataParityTests.DirectoryVariable} to a game data directory written by ARES-GameDataExtract extract to compare it with the generated Structs data.");
        return GameDataDirectory.Open(directory);
    }

    /// <summary>Each hull's planes, compared as values (a plane compares its doubles exactly).</summary>
    private static IEnumerable<IReadOnlyList<LootVolumePlane>> Planes(IReadOnlyList<LootVolumeHull> hulls) => hulls.Select(hull => hull.Planes);

    /// <summary>The point with its lists replaced by shared empty ones, so that records compare by value.</summary>
    private static PointOfInterest WithoutLists(PointOfInterest point) => point with { LootThemes = s_noStrings, Hulls = s_noHulls };

    /// <summary>The limiter with its lists replaced by shared empty ones, so that records compare by value.</summary>
    private static LootLimiter WithoutLists(LootLimiter limiter) => limiter with { Volumes = s_noVolumes, Limits = s_noLimits };

    /// <summary>The spawner with its lists replaced by shared empty ones, so that records compare by value.</summary>
    private static DynamicLootSpawner WithoutLists(DynamicLootSpawner spawner) => spawner with { Classes = s_noClasses, Points = s_noPoints };

    /// <summary>The group with its lists replaced by shared empty ones, so that records compare by value.</summary>
    private static DynamicLootGroupEntry WithoutLists(DynamicLootGroupEntry group) =>
        group with { Path = s_noStrings, Rounds = s_noStrings, Spawners = s_noIndices, ParentSelections = s_noSelections };
}
