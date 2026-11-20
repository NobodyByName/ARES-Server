namespace Ares.Common.Game;

/// <summary>
/// Plain raid values the game files do not hold: <c>ares-host.conf</c> sets them (each defaults to the constant below)
/// and Host passes them to every Raid it starts.
/// </summary>
/// <param name="LootCullDistance">
/// Distance within which a loot container is relevant to a player, in centimetres. A container is dormant from its
/// spawn: a player gets it once it is relevant, then each change of its state.
/// </param>
/// <param name="ExtractionSeconds">Seconds an elevator stays ready (doors open) before it leaves on its own.</param>
/// <param name="ExtractionReadySeconds">Seconds from the end of an elevator's call to its doors opening.</param>
public sealed record RaidSettings(int LootCullDistance, int ExtractionSeconds, int ExtractionReadySeconds)
{
    /// <summary>Default of <see cref="LootCullDistance"/>.</summary>
    public const int DefaultLootCullDistance = 20000;

    /// <summary>Default of <see cref="ExtractionSeconds"/>.</summary>
    public const int DefaultExtractionSeconds = 120;

    /// <summary>Default of <see cref="ExtractionReadySeconds"/>.</summary>
    public const int DefaultExtractionReadySeconds = 45;

    /// <summary>The settings with every default.</summary>
    public static RaidSettings Default { get; } = new(DefaultLootCullDistance, DefaultExtractionSeconds, DefaultExtractionReadySeconds);
}
