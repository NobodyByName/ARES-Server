using System.Collections.Immutable;
using Ares.Structs.Game.Movement;

namespace Ares.Common.GameData;

/// <summary>Movement tuning read from the game: dodge rolls, vaults, landings, graph tags, the snap hook, sprint and interaction animations.</summary>
public sealed class MovementTuning
{
    private readonly Dictionary<string, GraphTagBit> _graphTags;

    internal MovementTuning(GameDataContent content)
    {
        DodgeRolls = Exactly(content.DodgeRolls, Enum.GetValues<DodgeRollKind>().Length, GameTables.DodgeRolls);
        ImmutableArray<VaultDefinitionSet> vaults = Exactly(content.VaultDefinitions, MovementAssets.VaultPackages.Count, GameTables.VaultDefinitions);
        (ArmedVaults, UnarmedVaults, ArmedExhaustedVaults, UnarmedExhaustedVaults) = (vaults[0], vaults[1], vaults[2], vaults[3]);
        HeavyLandingLevels = content.HeavyLandingLevels;
        RecoveryLandingLevels = content.RecoveryLandingLevels;
        GraphStateTags = content.GraphStateTags;
        _graphTags = GameDataIndex.Unique(GraphStateTags.Select((tag, index) => (tag, new GraphTagBit(index))), "graph state tag", StringComparer.Ordinal);
        SnapHookItem = content.SnapHookItem;
        SprintBurstCurve = content.SprintBurstCurve;
        InteractionAnimationContainers = content.InteractionAnimationContainers;
        TaggedInteractionAnimationContainers = content.TaggedInteractionAnimationContainers;
    }

    /// <summary>The dodge-roll variants, in <see cref="DodgeRollKind"/> order (generated: <c>DodgeRollSequences</c>).</summary>
    public ImmutableArray<DodgeRollSequence> DodgeRolls { get; }

    /// <summary>Vaults with a weapon (generated: <c>VaultDefinitions.Armed</c>).</summary>
    public VaultDefinitionSet ArmedVaults { get; }

    /// <summary>Vaults without a weapon (generated: <c>VaultDefinitions.Unarmed</c>).</summary>
    public VaultDefinitionSet UnarmedVaults { get; }

    /// <summary>Vaults with a weapon while exhausted (generated: <c>VaultDefinitions.ArmedExhausted</c>).</summary>
    public VaultDefinitionSet ArmedExhaustedVaults { get; }

    /// <summary>Vaults without a weapon while exhausted (generated: <c>VaultDefinitions.UnarmedExhausted</c>).</summary>
    public VaultDefinitionSet UnarmedExhaustedVaults { get; }

    /// <summary>Levels of an ordinary heavy landing, lowest fall height first (generated: <c>HeavyLandingAnimations.Heavy</c>).</summary>
    public ImmutableArray<HeavyLandingLevel> HeavyLandingLevels { get; }

    /// <summary>Levels of a heavy landing softened by a recovery roll (generated: <c>HeavyLandingAnimations.Recovery</c>).</summary>
    public ImmutableArray<HeavyLandingLevel> RecoveryLandingLevels { get; }

    /// <summary>
    /// The gameplay tags the character graph tracks, in bit order (generated: <c>GraphStateTags</c> and
    /// <c>GraphStateUpperTags</c>); <see cref="FindGraphTag"/> gives a tag's bit.
    /// </summary>
    public ImmutableArray<string> GraphStateTags { get; }

    /// <summary>The snap hook item's class defaults (generated: <c>SnapHookItem</c>).</summary>
    public SnapHookItemDefaults SnapHookItem { get; }

    /// <summary>The sprint burst alpha curve over elapsed seconds (generated: <c>SprintMovementParameters.BurstCurve</c>).</summary>
    public ImmutableArray<WeightedCurveKey> SprintBurstCurve { get; }

    /// <summary>The interaction animation containers that change character movement (generated: <c>InteractionAnimationContainers.All</c>).</summary>
    public ImmutableArray<InteractionAnimationContainer> InteractionAnimationContainers { get; }

    /// <summary>
    /// The character's fallback interaction animation containers, in list order: the first is used when no entry's tags
    /// match (generated: <c>InteractionAnimationContainers.TagBased</c>).
    /// </summary>
    public ImmutableArray<TaggedInteractionAnimationContainer> TaggedInteractionAnimationContainers { get; }

    /// <summary>The dodge roll of a kind.</summary>
    public DodgeRollSequence DodgeRoll(DodgeRollKind kind) => DodgeRolls[(int)kind];

    /// <summary>The bit of a graph state tag, or null when the graph does not track it.</summary>
    /// <param name="tag">Tag name, for example <c>Ability.Crouch</c>.</param>
    public GraphTagBit? FindGraphTag(string tag) => _graphTags.TryGetValue(tag, out GraphTagBit bit) ? bit : null;

    /// <summary>
    /// The interaction animation container with the given object or package path, or null (generated:
    /// <c>InteractionAnimationContainers.Find</c>).
    /// </summary>
    /// <param name="path">Object path, or package path of a data asset named like its package.</param>
    public InteractionAnimationContainer? FindInteractionAnimation(string path) => InteractionAnimationContainers.FirstOrDefault(container =>
        container.Path == path || container.Path.StartsWith(path + ".", StringComparison.Ordinal));

    private static ImmutableArray<TRow> Exactly<TRow>(ImmutableArray<TRow> rows, int count, string table) => rows.Length == count ? rows
        : throw new GameDataException($"Game data table {table} holds {rows.Length} rows, but ARES expects {count}. Run the ARES-GameDataExtract of this ARES version again.");
}

/// <summary>A graph state tag's bit: bit <c>Index % 64</c> of tag word <c>Index / 64</c>.</summary>
/// <param name="Index">The tag's position in the graph's tag list.</param>
public readonly record struct GraphTagBit(int Index)
{
    /// <summary>The tag word holding the bit: 0 for the graph tag word, 1 for the upper tag word.</summary>
    public int Word => Index / MovementAssets.GraphTagWordBits;

    /// <summary>The bit within its word.</summary>
    public ulong Mask => 1UL << (Index % MovementAssets.GraphTagWordBits);
}
