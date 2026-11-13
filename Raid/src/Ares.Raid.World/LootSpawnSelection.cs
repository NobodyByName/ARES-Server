using Ares.Common.Game.Loot;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Game.Loot;
using Ares.Structs.Game.World;

namespace Ares.Raid.World;

/// <summary>How a loot placement limiter decides which sockets it covers and which of its caps apply to a socket.</summary>
public static class LootLimiterRules
{
    /// <summary>
    /// Whether the limiter covers a point: within its sphere, inside its turned box (half extent
    /// <see cref="LootLimiter.Radius"/> / 2 on each axis) or inside any convex part of its volumes. A point with a
    /// coordinate that is not finite is never covered.
    /// </summary>
    /// <param name="limiter">The limiter.</param>
    /// <param name="point">World location.</param>
    /// <returns>True when the limiter covers the point.</returns>
    public static bool IsPointInside(LootLimiter limiter, Vector3D point)
    {
        ArgumentNullException.ThrowIfNull(limiter);
        if (!double.IsFinite(point.X) || !double.IsFinite(point.Y) || !double.IsFinite(point.Z))
        {
            return false;
        }

        Vector3D offset = new(point.X - limiter.Location.X, point.Y - limiter.Location.Y, point.Z - limiter.Location.Z);
        switch (limiter.Shape)
        {
            case LootLimiterShape.Sphere:
                return Dot(offset, offset) <= limiter.Radius * limiter.Radius;
            case LootLimiterShape.Box:
                double half = limiter.Radius / 2;
                return Math.Abs(Dot(offset, limiter.Rotation.Rotate(new Vector3D(1, 0, 0)))) <= half
                    && Math.Abs(Dot(offset, limiter.Rotation.Rotate(new Vector3D(0, 1, 0)))) <= half
                    && Math.Abs(Dot(offset, limiter.Rotation.Rotate(new Vector3D(0, 0, 1)))) <= half;
            case LootLimiterShape.Volumes:
                return limiter.Volumes.Any(volume => LootVolumeRules.Contains(volume.Hulls, point));
            default:
                return false;
        }
    }

    /// <summary>Whether a limit entry's tag applies to a socket tag: it is the tag itself or one of its parents, without case.</summary>
    /// <param name="limitTag">The limit entry's tag.</param>
    /// <param name="socketTag">The socket's tag.</param>
    /// <returns>True when the entry applies.</returns>
    public static bool Matches(string limitTag, string socketTag)
    {
        ArgumentNullException.ThrowIfNull(limitTag);
        ArgumentNullException.ThrowIfNull(socketTag);
        return socketTag.Length == limitTag.Length
            ? string.Equals(socketTag, limitTag, StringComparison.OrdinalIgnoreCase)
            : socketTag.Length > limitTag.Length && socketTag[limitTag.Length] == '.' && socketTag.StartsWith(limitTag, StringComparison.OrdinalIgnoreCase);
    }

    private static double Dot(Vector3D a, Vector3D b) => (a.X * b.X) + (a.Y * b.Y) + (a.Z * b.Z);
}

/// <summary>How a raid picks the container sockets that get a container.</summary>
/// <param name="Limiters">The map's placement limiters.</param>
/// <param name="Areas">The map's points of interest; those with a loot value scale the chance of the sockets inside them.</param>
/// <param name="Chance">Chance from 0 to 1 that a socket of a tag rolls a container, given the loot value of the area around it.</param>
/// <param name="Rolls">False to give every socket a container, with no roll and no cap.</param>
/// <param name="FreeStanding">
/// Containers off the sockets: spawner groups, placed and observed containers. Placed and observed ones roll the
/// chance of their class's container tag (or all spawn without rolls); limiters and group caps apply to sockets only.
/// Null for none.
/// </param>
public sealed record LootSpawnRules(IReadOnlyList<LootLimiter> Limiters, IReadOnlyList<PointOfInterest> Areas, Func<string, PointOfInterestLootValue, double> Chance, bool Rolls, FreeStandingLoot? FreeStanding = null)
{
    /// <summary>The Dam's limiters and free-standing containers with ARES's default chances.</summary>
    public static LootSpawnRules Dam { get; } = new(DamLootLimiters.All, DamPointsOfInterest.All, LootSpawnDefaults.Chance, true, FreeStandingLoot.Dam);

    /// <summary>A container on every socket, and none off the sockets.</summary>
    public static LootSpawnRules EverySocket { get; } = new([], [], (_, _) => 1, false);
}

/// <summary>Which sockets of a raid get a container, and why the others do not.</summary>
/// <param name="Spawns">Per socket, in the order given: whether it gets a container.</param>
/// <param name="Values">Per socket, in the order given: the loot value of the area around it.</param>
/// <param name="SkippedByRoll">Sockets whose roll failed.</param>
/// <param name="SkippedByGroupCap">Sockets whose group already had its type's most containers.</param>
/// <param name="SkippedByLimiter">Sockets inside a limiter whose cap for one of their tags was reached.</param>
public sealed record LootSpawnSelection(IReadOnlyList<bool> Spawns, IReadOnlyList<PointOfInterestLootValue> Values, int SkippedByRoll, int SkippedByGroupCap, int SkippedByLimiter)
{
    /// <summary>
    /// Rolls the sockets of a raid. The sockets take turns in a random order; each rolls its tag's chance (scaled by
    /// the loot value of the area around it, see <see cref="LootValueAreas"/>), and a
    /// successful roll spawns unless the socket's group (the sockets of one placed component with the same tag)
    /// already holds the type's <see cref="LootContainerType.MaxPerGroup"/> containers, or a limiter of an active data
    /// layer that covers the socket has reached the cap of a limited entry matching its tag. A spawn counts towards
    /// every such entry.
    /// </summary>
    /// <param name="sockets">The sockets that may get a container.</param>
    /// <param name="rules">Chances and limiters.</param>
    /// <param name="activeDataLayers">Names of the raid's active data layers.</param>
    /// <param name="random">The raid's random numbers.</param>
    /// <returns>The selection.</returns>
    public static LootSpawnSelection Select(IReadOnlyList<LootSocket> sockets, LootSpawnRules rules, IReadOnlyCollection<string> activeDataLayers, Random random)
    {
        ArgumentNullException.ThrowIfNull(sockets);
        ArgumentNullException.ThrowIfNull(rules);
        ArgumentNullException.ThrowIfNull(activeDataLayers);
        ArgumentNullException.ThrowIfNull(random);
        bool[] spawns = new bool[sockets.Count];
        LootValueAreas areas = new(rules.Areas);
        PointOfInterestLootValue[] values = [.. sockets.Select(socket => areas.At(socket.Location))];
        if (!rules.Rolls)
        {
            Array.Fill(spawns, true);
            return new LootSpawnSelection(spawns, values, 0, 0, 0);
        }

        HashSet<string> active = new(activeDataLayers, StringComparer.Ordinal);
        List<LootLimiter> limiters = [.. rules.Limiters.Where(limiter => limiter.DataLayer is null || active.Contains(limiter.DataLayer))];
        int[][] spawned = [.. limiters.Select(limiter => new int[limiter.Limits.Count])];
        Dictionary<(string Cell, string Actor, string Component, string Tag), int> groups = [];
        (int rolls, int groupCaps, int limiterCaps) = (0, 0, 0);
        int[] order = [.. Enumerable.Range(0, sockets.Count)];
        random.Shuffle(order);
        foreach (int index in order)
        {
            LootSocket socket = sockets[index];
            if (random.NextDouble() >= rules.Chance(socket.ContainerTag, values[index]))
            {
                rolls++;
                continue;
            }

            (string, string, string, string) group = (socket.Cell, socket.Actor, socket.Component, socket.ContainerTag.ToUpperInvariant());
            int inGroup = groups.GetValueOrDefault(group);
            if (inGroup >= (LootContainerTypes.Find(socket.ContainerTag)?.MaxPerGroup ?? int.MaxValue))
            {
                groupCaps++;
                continue;
            }

            List<(int Limiter, int Entry)> entries = Entries(limiters, socket);
            if (entries.Exists(entry => spawned[entry.Limiter][entry.Entry] >= limiters[entry.Limiter].Limits[entry.Entry].Limit))
            {
                limiterCaps++;
                continue;
            }

            spawns[index] = true;
            groups[group] = inGroup + 1;
            foreach ((int limiter, int entry) in entries)
            {
                spawned[limiter][entry]++;
            }
        }

        return new LootSpawnSelection(spawns, values, rolls, groupCaps, limiterCaps);
    }

    /// <summary>How many sockets got a container in areas of a loot value.</summary>
    /// <param name="value">The loot value.</param>
    /// <returns>The number of spawned sockets.</returns>
    public int Spawned(PointOfInterestLootValue value)
    {
        return Enumerable.Range(0, Spawns.Count).Count(index => Spawns[index] && Values[index] == value);
    }

    // The limited entries that apply to a socket: of every limiter covering it, those matching its tag.
    private static List<(int Limiter, int Entry)> Entries(List<LootLimiter> limiters, LootSocket socket)
    {
        List<(int Limiter, int Entry)> entries = [];
        for (int limiter = 0; limiter < limiters.Count; limiter++)
        {
            IReadOnlyList<LootLimit> limits = limiters[limiter].Limits;
            if (!limits.Any(limit => !limit.Unlimited && LootLimiterRules.Matches(limit.Tag, socket.ContainerTag)) || !LootLimiterRules.IsPointInside(limiters[limiter], socket.Location))
            {
                continue;
            }

            for (int entry = 0; entry < limits.Count; entry++)
            {
                if (!limits[entry].Unlimited && LootLimiterRules.Matches(limits[entry].Tag, socket.ContainerTag))
                {
                    entries.Add((limiter, entry));
                }
            }
        }

        return entries;
    }
}
