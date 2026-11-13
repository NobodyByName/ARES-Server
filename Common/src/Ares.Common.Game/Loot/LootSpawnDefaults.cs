using Ares.Structs.Game.World;

namespace Ares.Common.Game.Loot;

/// <summary>The chance that a loot container socket rolls a container in a raid.</summary>
/// <param name="Tag">
/// Socket tag the chance applies to: the tag itself and every tag below it that has no entry of its own. Tag names
/// compare without case.
/// </param>
/// <param name="Chance">Chance from 0 to 1 that a socket's roll succeeds, before the group and area caps.</param>
public sealed record LootSpawnChance(string Tag, double Chance);

/// <summary>How much a loot value of the area around a socket scales the chance of its roll.</summary>
/// <param name="Value">Loot value the multiplier applies to.</param>
/// <param name="Multiplier">Factor on the socket tag's chance; the result is capped at 1.</param>
public sealed record LootValueMultiplier(PointOfInterestLootValue Value, double Multiplier);

/// <summary>
/// ARES's default container spawn chances. Every socket rolls once per raid; a successful roll still yields no
/// container when the socket's group or a placement limiter around it is full, so these chances sit above the share
/// of sockets that end up with a container. The tag chances are those of a socket outside every area with a loot
/// value; areas with one multiply them.
/// </summary>
public static class LootSpawnDefaults
{
    /// <summary>Chances by socket tag: one per container category, a few container types on their own.</summary>
    public static IReadOnlyList<LootSpawnChance> Chances { get; } =
    [
        new("Socket.LootContainer", 0.038),
        new("Socket.LootContainer.Commercial", 0.094),
        new("Socket.LootContainer.Commercial.GvtDesk.Drawer", 0.451),
        new("Socket.LootContainer.Commercial.Wrh.FilingCabinet.Drawer", 0.0023),
        new("Socket.LootContainer.Electrical", 0.0071),
        new("Socket.LootContainer.Industrial", 0.0138),
        new("Socket.LootContainer.Mechanical", 0.033),
        new("Socket.LootContainer.Medical", 0.080),
        new("Socket.LootContainer.Nature", 0.023),
        new("Socket.LootContainer.Nature.Mushroom", 0.135),
        new("Socket.LootContainer.Nature.Resin", 0.0015),
        new("Socket.LootContainer.Residential", 0.033),
        new("Socket.LootContainer.Tech", 0.0385),
        new("Socket.LootContainer.Tech.Computer02.Screen", 0.0173),
        new("Socket.LootContainer.Tech.Server.Door", 0.396),
    ];

    /// <summary>
    /// Multipliers by the loot value of the area around a socket. <see cref="PointOfInterestLootValue.Unknown"/> is the
    /// factor of a socket outside every area with a loot value; a value without an entry of its own uses it.
    /// </summary>
    public static IReadOnlyList<LootValueMultiplier> ValueMultipliers { get; } =
    [
        new(PointOfInterestLootValue.Unknown, 1.0),
        new(PointOfInterestLootValue.Medium, 4.6),
        new(PointOfInterestLootValue.High, 3.4),
    ];

    private static readonly Dictionary<string, double> s_byTag = Chances.ToDictionary(entry => entry.Tag, entry => entry.Chance, StringComparer.OrdinalIgnoreCase);

    private static readonly Dictionary<PointOfInterestLootValue, double> s_byValue = ValueMultipliers.ToDictionary(entry => entry.Value, entry => entry.Multiplier);

    /// <summary>
    /// The chance of a socket tag: the entry of the tag itself, else of its nearest parent tag; 0 for a tag outside
    /// every entry.
    /// </summary>
    /// <param name="tag">The socket's container tag.</param>
    /// <returns>The chance from 0 to 1.</returns>
    public static double Chance(string tag)
    {
        ArgumentNullException.ThrowIfNull(tag);
        string current = tag;
        while (current.Length > 0)
        {
            if (s_byTag.TryGetValue(current, out double chance))
            {
                return chance;
            }

            int dot = current.LastIndexOf('.');
            current = dot < 0 ? string.Empty : current[..dot];
        }

        return 0;
    }

    /// <summary>The multiplier of a loot value: its own entry, else the one of an area without a loot value.</summary>
    /// <param name="value">The loot value of the area around a socket.</param>
    /// <returns>The factor on a tag's chance.</returns>
    public static double Multiplier(PointOfInterestLootValue value) => s_byValue.GetValueOrDefault(value, s_byValue[PointOfInterestLootValue.Unknown]);

    /// <summary>The chance of a socket tag in an area of a loot value: the tag's chance times the value's multiplier, at most 1.</summary>
    /// <param name="tag">The socket's container tag.</param>
    /// <param name="value">The loot value of the area around the socket.</param>
    /// <returns>The chance from 0 to 1.</returns>
    public static double Chance(string tag, PointOfInterestLootValue value) => Math.Min(1, Chance(tag) * Multiplier(value));
}
