using Ares.Common.Game.Loot;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Game.World;

namespace Ares.Raid.World;

/// <summary>The loot containers of a map that do not stand on prop sockets, and which of them a raid may spawn.</summary>
/// <param name="Groups">Dynamic loot groups: each picks some of its spawners when its round runs.</param>
/// <param name="Placed">Container actors the map places.</param>
/// <param name="Observed">Containers the game server places where the map data holds no placement.</param>
/// <param name="SpawnerClassEnabled">Whether a spawner may place a container of a class (by class path).</param>
public sealed record FreeStandingLoot(
    IReadOnlyList<DynamicLootGroup> Groups,
    IReadOnlyList<PlacedLootContainer> Placed,
    IReadOnlyList<ObservedLootContainer> Observed,
    Func<string, bool> SpawnerClassEnabled)
{
    /// <summary>The Dam's spawners, placed and observed containers with ARES's default spawner classes.</summary>
    public static FreeStandingLoot Dam { get; } = new(DamDynamicLoot.Groups, DamPlacedLoot.Containers, DamPlacedLoot.Observed, DynamicLootDefaults.Enabled);

    /// <summary>No free-standing containers.</summary>
    public static FreeStandingLoot None { get; } = new([], [], [], _ => false);

    /// <summary>Every spawner of the groups, each once, in group order.</summary>
    public IReadOnlyList<DynamicLootSpawner> Spawners => [.. Groups.SelectMany(group => group.Spawners).Distinct()];
}

/// <summary>A container a dynamic loot spawner places when it runs.</summary>
/// <param name="Class">Container class path.</param>
/// <param name="Location">World location.</param>
/// <param name="Rotation">World rotation in degrees.</param>
public readonly record struct DynamicLootPlacement(string Class, Vector3D Location, Rotator Rotation);

/// <summary>Which dynamic loot spawners run in a raid and what they place.</summary>
public static class DynamicLootRuns
{
    /// <summary>
    /// The groups of a round that activate. A group belongs to the round when its <see cref="DynamicLootGroup.Rounds"/>
    /// list the round's configuration. A partial choice of subnodes above it (<see cref="DynamicLootGroup.ParentSelections"/>)
    /// picks once per raid a uniform number in [Min, Max] (at most the node's children) of its children by a shuffle;
    /// a group whose child on the way down was not picked stays inactive. A node's children are told apart by the
    /// first path entry where its groups differ; children without groups of their own count towards
    /// <see cref="DynamicLootSelection.Of"/>.
    /// </summary>
    /// <param name="groups">The map's groups.</param>
    /// <param name="round">Name of the round configuration.</param>
    /// <param name="random">The raid's random numbers.</param>
    /// <returns>The active groups, in the order given.</returns>
    public static IReadOnlyList<DynamicLootGroup> ActiveGroups(IReadOnlyList<DynamicLootGroup> groups, string round, Random random)
    {
        ArgumentNullException.ThrowIfNull(groups);
        ArgumentNullException.ThrowIfNull(round);
        ArgumentNullException.ThrowIfNull(random);
        List<DynamicLootGroup> inRound = [.. groups.Where(group => group.Rounds.Contains(round, StringComparer.Ordinal))];
        Dictionary<string, (int Depth, HashSet<string> Picked)> choices = new(StringComparer.Ordinal);
        foreach (IGrouping<string, (DynamicLootSelection Selection, DynamicLootGroup Group)> node in inRound
            .SelectMany(group => group.ParentSelections.Select(selection => (Selection: selection, Group: group)))
            .GroupBy(entry => entry.Selection.Node, StringComparer.Ordinal))
        {
            DynamicLootSelection selection = node.First().Selection;
            List<IReadOnlyList<string>> paths = [.. node.Select(entry => entry.Group.Path)];
            int depth = ChildDepth(paths);
            string[] children = [.. paths.Select(path => Child(path, depth)).Distinct(StringComparer.Ordinal)];
            int count = Math.Min(random.Next(selection.Min, selection.Max + 1), selection.Of);
            int[] positions = [.. Enumerable.Range(0, Math.Max(selection.Of, children.Length))];
            random.Shuffle(positions);
            HashSet<string> picked = new(StringComparer.Ordinal);
            for (int child = 0; child < children.Length; child++)
            {
                if (Array.IndexOf(positions, child) < count)
                {
                    picked.Add(children[child]);
                }
            }

            choices[node.Key] = (depth, picked);
        }

        return [.. inRound.Where(group => group.ParentSelections.All(selection => Chosen(group, selection)))];

        bool Chosen(DynamicLootGroup group, DynamicLootSelection selection)
        {
            (int depth, HashSet<string> picked) = choices[selection.Node];
            return picked.Contains(Child(group.Path, depth));
        }
    }

    /// <summary>
    /// The spawners that run in a round: each active group (<see cref="ActiveGroups"/>) picks a uniform number in
    /// [Min, Max] (at most its spawner count) of its spawners by a shuffle. A spawner picked by several groups runs once.
    /// </summary>
    /// <param name="groups">The map's groups.</param>
    /// <param name="round">Name of the round configuration.</param>
    /// <param name="random">The raid's random numbers.</param>
    /// <returns>The spawners that run, in the order they were picked.</returns>
    public static IReadOnlyList<DynamicLootSpawner> Pick(IReadOnlyList<DynamicLootGroup> groups, string round, Random random)
    {
        List<DynamicLootSpawner> picked = [];
        HashSet<DynamicLootSpawner> seen = [];
        foreach (DynamicLootGroup group in ActiveGroups(groups, round, random))
        {
            DynamicLootSpawner[] order = [.. group.Spawners];
            random.Shuffle(order);
            int count = Math.Min(random.Next(group.Min, group.Max + 1), order.Length);
            picked.AddRange(order.Take(count).Where(seen.Add));
        }

        return picked;
    }

    /// <summary>
    /// What a spawner places when it runs: <see cref="DynamicLootSpawner.Count"/> containers, each with a class drawn by
    /// weight and a point drawn by weight among the points this run has not used, turned to a random yaw when the
    /// spawner randomises the rotation.
    /// </summary>
    /// <param name="spawner">The spawner.</param>
    /// <param name="random">The raid's random numbers.</param>
    /// <returns>The placements.</returns>
    public static IReadOnlyList<DynamicLootPlacement> Run(DynamicLootSpawner spawner, Random random)
    {
        ArgumentNullException.ThrowIfNull(spawner);
        ArgumentNullException.ThrowIfNull(random);
        List<DynamicLootPoint> points = [.. spawner.Points];
        List<DynamicLootPlacement> placements = [];
        for (int actor = 0; actor < spawner.Count && points.Count > 0; actor++)
        {
            DynamicLootClass type = spawner.Classes[Weighted(spawner.Classes.Select(entry => entry.Weight), random)];
            int index = Weighted(points.Select(point => point.Weight), random);
            DynamicLootPoint point = points[index];
            points.RemoveAt(index);
            Rotator rotation = spawner.RandomRotation ? point.Rotation with { Yaw = random.NextDouble() * 360 } : point.Rotation;
            placements.Add(new DynamicLootPlacement(type.Class, point.Location, rotation));
        }

        return placements;
    }

    // An index drawn with chances proportional to the weights; equal chances when no weight is positive.
    private static int Weighted(IEnumerable<double> weights, Random random)
    {
        double[] all = [.. weights.Select(weight => Math.Max(0, weight))];
        double total = all.Sum();
        if (total <= 0)
        {
            return random.Next(all.Length);
        }

        double roll = random.NextDouble() * total;
        for (int index = 0; index < all.Length; index++)
        {
            roll -= all[index];
            if (roll < 0)
            {
                return index;
            }
        }

        return all.Length - 1;
    }

    // The path depth at which the groups under one node part into its children: the length of their common path prefix,
    // at most the last entry of the shortest path.
    private static int ChildDepth(List<IReadOnlyList<string>> paths)
    {
        int shortest = paths.Min(path => path.Count);
        int depth = 0;
        while (depth < shortest - 1 && paths.All(path => string.Equals(path[depth], paths[0][depth], StringComparison.Ordinal)))
        {
            depth++;
        }

        return depth;
    }

    private static string Child(IReadOnlyList<string> path, int depth) => path[Math.Min(depth, path.Count - 1)];
}
