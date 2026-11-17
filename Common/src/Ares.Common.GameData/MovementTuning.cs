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
        _graphTags = GameDataIndex.Unique(GraphStateTags.Select((tag, index) => (tag, new GraphTagBit(tag, index))), "graph state tag", StringComparer.Ordinal);
        SnapHookItem = content.SnapHookItem;
        SprintBurstCurve = content.SprintBurstCurve;
        InteractionAnimationContainers = content.InteractionAnimationContainers;
        TaggedInteractionAnimationContainers = content.TaggedInteractionAnimationContainers;
    }

    /// <summary>The dodge-roll variants, in <see cref="DodgeRollKind"/> order.</summary>
    public ImmutableArray<DodgeRollSequence> DodgeRolls { get; }

    /// <summary>Vaults with a weapon.</summary>
    public VaultDefinitionSet ArmedVaults { get; }

    /// <summary>Vaults without a weapon.</summary>
    public VaultDefinitionSet UnarmedVaults { get; }

    /// <summary>Vaults with a weapon while exhausted.</summary>
    public VaultDefinitionSet ArmedExhaustedVaults { get; }

    /// <summary>Vaults without a weapon while exhausted.</summary>
    public VaultDefinitionSet UnarmedExhaustedVaults { get; }

    /// <summary>Levels of an ordinary heavy landing, lowest fall height first.</summary>
    public ImmutableArray<HeavyLandingLevel> HeavyLandingLevels { get; }

    /// <summary>Levels of a heavy landing softened by a recovery roll.</summary>
    public ImmutableArray<HeavyLandingLevel> RecoveryLandingLevels { get; }

    /// <summary>
    /// The gameplay tags the character graph tracks, in bit order; <see cref="FindGraphTag"/> gives a tag's bit.
    /// </summary>
    public ImmutableArray<string> GraphStateTags { get; }

    /// <summary>The snap hook item's class defaults.</summary>
    public SnapHookItemDefaults SnapHookItem { get; }

    /// <summary>The sprint burst alpha curve over elapsed seconds.</summary>
    public ImmutableArray<WeightedCurveKey> SprintBurstCurve { get; }

    /// <summary>The interaction animation containers that change character movement.</summary>
    public ImmutableArray<InteractionAnimationContainer> InteractionAnimationContainers { get; }

    /// <summary>
    /// The character's fallback interaction animation containers, in list order: the first is used when no entry's tags
    /// match.
    /// </summary>
    public ImmutableArray<TaggedInteractionAnimationContainer> TaggedInteractionAnimationContainers { get; }

    /// <summary>The vault definition set for the character's equipment and condition.</summary>
    /// <param name="armed">Whether an item other than empty hands is held.</param>
    /// <param name="exhausted">Whether the character is exhausted (and exhausted vaults are not disabled) or carries a heavy load.</param>
    public VaultDefinitionSet Vaults(bool armed, bool exhausted) => (armed, exhausted) switch
    {
        (true, false) => ArmedVaults,
        (false, false) => UnarmedVaults,
        (true, true) => ArmedExhaustedVaults,
        (false, true) => UnarmedExhaustedVaults,
    };

    /// <summary>The dodge roll of a kind.</summary>
    public DodgeRollSequence DodgeRoll(DodgeRollKind kind) => DodgeRolls[(int)kind];

    /// <summary>The bit of a graph state tag, or null when the graph does not track it.</summary>
    /// <param name="tag">Tag name, for example <c>Ability.Crouch</c>.</param>
    public GraphTagBit? FindGraphTag(string tag) => _graphTags.TryGetValue(tag, out GraphTagBit bit) ? bit : null;

    /// <summary>
    /// The interaction animation container with the given object or package path, or null.
    /// </summary>
    /// <param name="path">Object path, or package path of a data asset named like its package.</param>
    public InteractionAnimationContainer? FindInteractionAnimation(string path) => InteractionAnimationContainers.FirstOrDefault(container =>
        container.Path == path || container.Path.StartsWith(path + ".", StringComparison.Ordinal));

    private static ImmutableArray<TRow> Exactly<TRow>(ImmutableArray<TRow> rows, int count, string table) => rows.Length == count ? rows
        : throw new GameDataException($"Game data table {table} holds {rows.Length} rows, but ARES expects {count}. Run the ARES-GameDataExtract of this ARES version again.");
}
