using Ares.Common.Game.Loot;
using Ares.Raid.Replication;
using Ares.Raid.World;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Game.Loot;
using Ares.Structs.Game.World;

namespace Ares.Raid.Tests.World;

public sealed class FreeStandingLootTests
{
    private const int Seeds = 200;

    private static readonly string s_round = DamRounds.All[0].Name;

    private static readonly ReplicationContext s_replication = new ScriptedPlayer().Worlds.Current!.State.Replication;

    private static readonly string[] s_spawnerClasses = [.. DamDynamicLoot.Spawners.SelectMany(spawner => spawner.Classes).Select(entry => entry.Class).Distinct()];

    // A class a spawner may place and the server serves; one it serves but ARES does not enable; one enabled but not served.
    private static readonly string s_enabled = s_spawnerClasses.First(path => DynamicLootDefaults.Enabled(path) && LootContainerClasses.Find(path) is not null);
    private static readonly string s_disabled = s_spawnerClasses.First(path => !DynamicLootDefaults.Enabled(path) && LootContainerClasses.Find(path) is not null);
    private static readonly string s_unsupported = s_spawnerClasses.First(path => DynamicLootDefaults.Enabled(path) && LootContainerClasses.Find(path) is null);

    [Fact]
    public void A_group_picks_between_its_min_and_max_spawners_the_same_way_for_a_seed()
    {
        DynamicLootSpawner[] spawners = [.. Enumerable.Range(0, 10).Select(index => Spawner($"Spawner_{index}", s_enabled))];
        DynamicLootGroup[] groups = [Group("Group/0", ["Root", "Cases"], 3, 5, spawners)];
        HashSet<int> counts = [];
        for (int seed = 1; seed <= Seeds; seed++)
        {
            IReadOnlyList<DynamicLootSpawner> picked = DynamicLootRuns.Pick(groups, s_round, new Random(seed));
            Assert.InRange(picked.Count, 3, 5);
            Assert.Equal(picked.Count, picked.Distinct().Count());
            Assert.Equal(picked, DynamicLootRuns.Pick(groups, s_round, new Random(seed)));
            counts.Add(picked.Count);
        }

        Assert.Equal([3, 4, 5], counts.Order());
        Assert.Empty(DynamicLootRuns.Pick([Group("Group/0", ["Root"], 3, 5, spawners, rounds: ["Coldsnap"])], s_round, new Random(1)));
    }

    [Fact]
    public void Only_the_groups_listing_the_round_activate_in_the_dam()
    {
        DynamicLootGroup[] inRound = [.. DamDynamicLoot.Groups.Where(group => group.Rounds.Contains(s_round))];
        Assert.NotEmpty(inRound);
        Assert.All(inRound, group => Assert.Empty(group.ParentSelections));
        Assert.Equal(inRound, DynamicLootRuns.ActiveGroups(DamDynamicLoot.Groups, s_round, new Random(1)));
        Assert.DoesNotContain(DynamicLootRuns.ActiveGroups(DamDynamicLoot.Groups, s_round, new Random(1)), group => group.Rounds.Contains("Coldsnap") && !group.Rounds.Contains(s_round));
    }

    [Fact]
    public void A_parent_selection_activates_between_its_min_and_max_children()
    {
        // The Dam's Extractor clusters: 7 of the 13 children of one node have container groups, and 7 to 8 children run.
        DynamicLootGroup[] clusters = [.. DamDynamicLoot.Groups.Where(group => group.ParentSelections.Count > 0)];
        DynamicLootSelection selection = clusters[0].ParentSelections[0];
        double active = 0;
        for (int seed = 1; seed <= Seeds; seed++)
        {
            int count = DynamicLootRuns.ActiveGroups(clusters, "Extractor", new Random(seed)).Count;
            Assert.InRange(count, Math.Max(0, selection.Min - (selection.Of - clusters.Length)), Math.Min(clusters.Length, selection.Max));
            active += count;
        }

        double expected = clusters.Length * (selection.Min + selection.Max) / 2.0 / selection.Of;
        Assert.InRange(active / Seeds, expected - 0.5, expected + 0.5);

        // Exactly one of two children: never both, never neither, each about half the time.
        DynamicLootSelection oneOfTwo = new("Node", 1, 1, 2, false);
        DynamicLootGroup left = Group("Left/0", ["Root", "Node", "Left"], 1, 1, [Spawner("A", s_enabled)], [oneOfTwo]);
        DynamicLootGroup right = Group("Right/0", ["Root", "Node", "Right"], 1, 1, [Spawner("B", s_enabled)], [oneOfTwo]);
        int lefts = 0;
        for (int seed = 1; seed <= Seeds; seed++)
        {
            DynamicLootGroup chosen = Assert.Single(DynamicLootRuns.ActiveGroups([left, right], s_round, new Random(seed)));
            lefts += chosen == left ? 1 : 0;
        }

        Assert.InRange(lefts, Seeds / 4, Seeds * 3 / 4);
    }

    [Fact]
    public void A_spawner_places_its_count_without_repeating_a_point_and_turns_only_when_it_says_so()
    {
        DynamicLootSpawner cluster = Spawner("Cluster", s_enabled, points: 11, count: 5, randomRotation: true);
        DynamicLootSpawner fixedTurn = Spawner("Fixed", s_enabled, points: 3);
        for (int seed = 1; seed <= Seeds; seed++)
        {
            IReadOnlyList<DynamicLootPlacement> placements = DynamicLootRuns.Run(cluster, new Random(seed));
            Assert.Equal(5, placements.Count);
            Assert.Equal(5, placements.Select(placement => placement.Location).Distinct().Count());
            Assert.All(placements, placement => Assert.InRange(placement.Rotation.Yaw, 0, 360));
            Assert.All(placements, placement => Assert.Equal((0.0, 0.0), (placement.Rotation.Pitch, placement.Rotation.Roll)));

            DynamicLootPlacement single = Assert.Single(DynamicLootRuns.Run(fixedTurn, new Random(seed)));
            Assert.Contains(fixedTurn.Points, point => point.Location == single.Location && point.Rotation == single.Rotation);
        }
    }

    [Fact]
    public void A_drawn_class_that_is_disabled_or_unsupported_places_nothing()
    {
        DynamicLootSpawner[] spawners = [Spawner("Enabled", s_enabled), Spawner("Disabled", s_disabled), Spawner("Unsupported", s_unsupported), Spawner("Layered", s_enabled) with { DataLayer = "DataLayer_Inactive" }, Spawner("Elsewhere", s_enabled)];
        DynamicLootGroup[] groups = [Group("All/0", ["Root"], 4, 4, spawners[..4]), Group("Other/0", ["Root"], 1, 1, spawners[4..], rounds: ["Coldsnap"])];
        LootCatalog catalog = Catalog(new FreeStandingLoot(groups, [], [], DynamicLootDefaults.Enabled), LootSpawnRules.EverySocket);

        LootSpawnerCounts counts = catalog.Counts.Spawners;
        Assert.Equal(new LootSpawnerCounts(5, 3, 1, 1, 1, 1, 1), counts);
        LootContainer container = Assert.Single(catalog.Containers);
        Assert.Equal((LootContainerSource.Spawner, s_enabled, (LootSocket?)null), (container.Source, container.Class.Container.Class, container.Socket));
        Assert.Equal(spawners[0].Points[0].Location, container.Location);
    }

    [Fact]
    public void Placed_machine_doors_roll_their_tag_and_snowpiles_skip_a_round_without_their_layer()
    {
        LootCatalog always = Catalog(FreeStandingLoot.Dam with { Groups = [] }, new LootSpawnRules([], [], (_, _) => 1, true));
        LootPlacedCounts placed = always.Counts.Placed;
        int snowpiles = DamPlacedLoot.Containers.Count(container => container.DataLayer is not null);
        Assert.Equal((DamPlacedLoot.Containers.Count, DamPlacedLoot.Containers.Count - snowpiles, snowpiles, 0, 0, 0), (placed.Containers, placed.Spawned, placed.InactiveLayer, placed.Unsupported, placed.NoContainerType, placed.SkippedByRoll));
        Assert.All(always.Containers.Where(container => container.Source == LootContainerSource.Placed), container => Assert.Contains("Wrh_Machine_M_02_Door", container.Class.Container.Class, StringComparison.Ordinal));
        Assert.Contains(always.Containers, container => container.Location == new Vector3D(477312.75, 562716.08, -37598.27) && container.Rotation == new Rotator(90, 0, 0));

        // The roll uses the chance of the door's container tag.
        List<string> tags = [];
        LootCatalog never = Catalog(FreeStandingLoot.Dam with { Groups = [] }, new LootSpawnRules([], [], (tag, _) => { tags.Add(tag); return 0; }, true));
        Assert.Equal(DamPlacedLoot.Containers.Count - snowpiles, never.Counts.Placed.SkippedByRoll);
        Assert.Empty(never.Containers);
        Assert.All(tags, tag => Assert.StartsWith("Socket.LootContainer.Mechanical.", tag, StringComparison.Ordinal));

        // With the snow layer active the snowpiles stay out: the server cannot serve them yet.
        string[] snow = [.. DamPlacedLoot.Containers.Select(container => container.DataLayer).OfType<string>().Distinct()];
        LootCatalog snowy = Catalog(FreeStandingLoot.Dam with { Groups = [] }, new LootSpawnRules([], [], (_, _) => 1, true), snow);
        Assert.Equal((0, snowpiles), (snowy.Counts.Placed.InactiveLayer, snowy.Counts.Placed.Unsupported));

        // Observed plants wait for their classes.
        Assert.Equal((DamPlacedLoot.Observed.Count, 0, DamPlacedLoot.Observed.Count), (always.Counts.Observed.Containers, always.Counts.Observed.Spawned, always.Counts.Observed.Unsupported));
    }

    [Fact]
    public void Dam_raids_place_only_enabled_spawner_classes_and_ids_stay_unique_across_sources()
    {
        string rack = s_spawnerClasses.Single(path => path.Contains("WeaponsRack", StringComparison.Ordinal));
        for (int seed = 1; seed <= 20; seed++)
        {
            LootCatalog catalog = new(s_replication, DamLootSockets.All, DamLootSockets.Meshes, s_round, [], seed, LootSpawnRules.Dam with { Chance = (_, _) => 1 });
            LootSpawnCounts counts = catalog.Counts;
            LootContainer[] dynamic = [.. catalog.Containers.Where(container => container.Source == LootContainerSource.Spawner)];
            Assert.Equal(counts.Spawners.Spawned, dynamic.Length);
            Assert.All(dynamic, container => Assert.True(DynamicLootDefaults.Enabled(container.Class.Container.Class)));
            Assert.InRange(dynamic.Count(container => container.Class.Container.Class == rack), 5, 6);
            Assert.Equal(counts.Spawners.Spawners, counts.Spawners.Run + counts.Spawners.NotPicked + counts.Spawners.InactiveLayer);
            Assert.Equal(DamPlacedLoot.Containers.Count(container => container.DataLayer is null), counts.Placed.Spawned);
            Assert.Equal(counts.Spawned + counts.Spawners.Spawned + counts.Placed.Spawned + counts.Observed.Spawned, catalog.Containers.Count);
            Assert.Equal(Enumerable.Range(0, catalog.Containers.Count), catalog.Containers.Select(container => container.Index));
            ulong[] ids = [.. catalog.Containers.SelectMany(container => container.Components.Append(container.Guid))];
            Assert.Equal(ids.Length, ids.Distinct().Count());
            Assert.All(catalog.Containers, container => Assert.Same(container, catalog.ByInteraction(container.Interaction)));
            Assert.Contains(catalog.Containers, container => container.Source == LootContainerSource.Socket);
            Assert.Contains(catalog.Containers, container => container.Source == LootContainerSource.Placed);
            if (seed > 3)
            {
                continue;
            }

            LootCatalog again = new(s_replication, DamLootSockets.All, DamLootSockets.Meshes, s_round, [], seed, LootSpawnRules.Dam with { Chance = (_, _) => 1 });
            Assert.Equal(catalog.Containers.Select(container => (container.Source, container.Location, container.Class)), again.Containers.Select(container => (container.Source, container.Location, container.Class)));
        }
    }

    [Fact]
    public void A_dam_raid_lists_its_free_standing_containers_with_a_class()
    {
        for (int seed = 1; seed <= 20; seed++)
        {
            LootCatalog catalog = new(s_replication, s_round, [], seed, LootSpawnRules.Dam with { Chance = (_, _) => 1 });
            Assert.Equal(seed, catalog.Seed);
            Assert.Equal(catalog.Containers.Count(container => container.Source != LootContainerSource.Socket), catalog.FreeStanding.Count);
            foreach (FreeStandingContainer entry in catalog.FreeStanding)
            {
                Assert.False(string.IsNullOrEmpty(entry.Class));
                Assert.DoesNotContain("BP_", entry.Class, StringComparison.Ordinal);
                Assert.False(entry.Class.EndsWith("_C", StringComparison.Ordinal));
                Assert.True(entry.ParentArea is null || entry.Area is not null);
            }
        }
    }

    [Fact]
    public void A_container_inside_an_area_resolves_to_the_innermost_named_area_and_its_parent()
    {
        FreeStandingAreas areas = new(DamPointsOfInterest.All);
        string path = s_enabled;
        DynamicLootPoint annex = DamDynamicLoot.Spawners.First(spawner => spawner.Actor == "ActorSpawner_116").Points[0];
        FreeStandingContainer inAnnex = areas.Describe(path, annex.Location);
        Assert.Equal("DAM_TESTINGANNEXMONITORROOM1", inAnnex.Area);
        Assert.Equal("DAM_TESTINGANNEX", inAnnex.ParentArea);

        DynamicLootPoint admin = DamDynamicLoot.Spawners.First(spawner => spawner.Actor == "ActorSpawner_104").Points[0];
        FreeStandingContainer inAdmin = areas.Describe(path, admin.Location);
        Assert.Equal("DAM_ADMIN", inAdmin.Area);
        Assert.Equal("DAM_PRIMARYFACILITY", inAdmin.ParentArea);

        FreeStandingContainer nowhere = areas.Describe(path, new Vector3D(0, 0, 1e9));
        Assert.Null(nowhere.Area);
        Assert.Null(nowhere.ParentArea);
    }

    private static LootCatalog Catalog(FreeStandingLoot free, LootSpawnRules rules, IReadOnlyCollection<string>? activeDataLayers = null) =>
        new(s_replication, [], DamLootSockets.Meshes, s_round, activeDataLayers ?? [], 7, rules with { FreeStanding = free });

    private static DynamicLootSpawner Spawner(string actor, string type, int points = 1, int count = 1, bool randomRotation = false) =>
        new(
            "",
            actor,
            null,
            [new DynamicLootClass(type, 1)],
            [.. Enumerable.Range(0, points).Select(index => new DynamicLootPoint(new Vector3D(100000 + (1000 * index), 200000, 0), new Rotator(0, 10 * index, 0), 1))],
            count,
            randomRotation);

    private static DynamicLootGroup Group(string id, IReadOnlyList<string> path, int min, int max, IReadOnlyList<DynamicLootSpawner> spawners, IReadOnlyList<DynamicLootSelection>? selections = null, IReadOnlyList<string>? rounds = null) =>
        new(id, path, rounds ?? [s_round], min, max, spawners, selections ?? []);
}
