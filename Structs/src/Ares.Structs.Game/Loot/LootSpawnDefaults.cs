namespace Ares.Structs.Game.Loot;

/// <summary>Defaults every loot container class shares.</summary>
/// <param name="NetCullDistance">
/// Distance within which a container is relevant to a player (the net cull distance of every container). A container
/// is dormant from its spawn: a player gets it once it is relevant, then each change of its state.
/// </param>
/// <param name="TargetStacks">
/// Weighted target numbers of item stacks for a container (<c>StaticLootSpawnTargetStacks</c>); no container class
/// overrides them.
/// </param>
public sealed record LootContainerDefaults(double NetCullDistance, IReadOnlyList<LootTargetStacks> TargetStacks);

/// <summary>
/// Defaults of the static loot spawn manager's tweakables (<c>UStaticLootSpawnManagerTweakables</c>): weights the
/// manager uses when it distributes loot over placements.
/// </summary>
/// <param name="NumPlacementsWeight">The game's <c>NumPlacementsWeight</c>.</param>
/// <param name="NumTargetItemsWeight">The game's <c>NumTargetItemsWeight</c>.</param>
/// <param name="NumAvoidItemsWeight">The game's <c>NumAvoidItemsWeight</c>.</param>
/// <param name="NumMissingTargetStacksWeight">The game's <c>NumMissingTargetStacksWeight</c>.</param>
/// <param name="NumStacksWeight">The game's <c>NumStacksWeight</c>.</param>
/// <param name="NumItemsWeight">The game's <c>NumItemsWeight</c>.</param>
/// <param name="ContainerContentsWeights">The game's <c>ContainerContentsWeightMap</c>: a weight per key, in ascending key order.</param>
public sealed record LootSpawnTweakables(
    double NumPlacementsWeight,
    double NumTargetItemsWeight,
    double NumAvoidItemsWeight,
    double NumMissingTargetStacksWeight,
    double NumStacksWeight,
    double NumItemsWeight,
    IReadOnlyDictionary<int, double> ContainerContentsWeights);
