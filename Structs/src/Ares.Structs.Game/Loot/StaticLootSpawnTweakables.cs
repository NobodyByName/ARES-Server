namespace Ares.Structs.Game.Loot;

/// <summary>
/// Defaults of the static loot spawn manager's tweakables (<c>UStaticLootSpawnManagerTweakables</c>): weights the
/// manager uses when it distributes loot over placements.
/// </summary>
public static class StaticLootSpawnTweakables
{
    /// <summary>The game's <c>NumPlacementsWeight</c>.</summary>
    public const double NumPlacementsWeight = 0.5;

    /// <summary>The game's <c>NumTargetItemsWeight</c>.</summary>
    public const double NumTargetItemsWeight = 1.0;

    /// <summary>The game's <c>NumAvoidItemsWeight</c>.</summary>
    public const double NumAvoidItemsWeight = 0.5;

    /// <summary>The game's <c>NumMissingTargetStacksWeight</c>.</summary>
    public const double NumMissingTargetStacksWeight = 0.4;

    /// <summary>The game's <c>NumStacksWeight</c>.</summary>
    public const double NumStacksWeight = 0.025;

    /// <summary>The game's <c>NumItemsWeight</c>.</summary>
    public const double NumItemsWeight = 0.022;

    /// <summary>The game's <c>ContainerContentsWeightMap</c>: a weight per key.</summary>
    public static IReadOnlyDictionary<int, double> ContainerContentsWeights { get; } = new Dictionary<int, double>
    {
        [0] = 1.0,
        [1] = 4.0,
        [2] = 4.0,
        [3] = 6.2,
        [4] = 1.0,
    };
}
