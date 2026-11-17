using System.Collections.Immutable;
using Ares.Structs.Game.Loot;

namespace Ares.Common.GameData;

/// <summary>The loot definitions: container types and classes, value and skill loot tables, spawn tweakables.</summary>
public sealed class LootData
{
    private readonly Dictionary<string, LootContainerType> _typesByTag;
    private readonly Dictionary<string, LootContainerClass> _classesByPath;
    private readonly Dictionary<string, StaticValueLootTable> _valueTablesByName;

    internal LootData(GameDataContent content)
    {
        ContainerTypes = content.LootContainerTypes;
        _typesByTag = GameDataIndex.Unique(ContainerTypes.Select(type => (type.Tag, type)), "loot container type", StringComparer.OrdinalIgnoreCase);
        ContainerClasses = content.LootContainerClasses;
        _classesByPath = GameDataIndex.Unique(ContainerClasses.Select(type => (type.Container.Class, type)), "loot container class", StringComparer.Ordinal);
        ContainerDefaults = content.LootContainerDefaults;
        StaticValueTables = content.StaticValueLootTables;
        _valueTablesByName = StaticValueTables.GroupBy(table => table.Name, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() == 1)
            .ToDictionary(group => group.Key, group => group.Single(), StringComparer.OrdinalIgnoreCase);
        SkillTables = content.SkillLootTables;
        SpawnTweakables = content.StaticLootSpawnTweakables;
    }

    /// <summary>The loot container types the game registers, ordered by tag (generated: <c>LootContainerTypes.All</c>).</summary>
    public ImmutableArray<LootContainerType> ContainerTypes { get; }

    /// <summary>
    /// Every loot container class that decodes, ordered by class path. Generated <c>LootContainerClasses.All</c> held only
    /// the classes a server serves: filter with <c>LootContainerSupport.IsSupported</c> where that matters.
    /// </summary>
    public ImmutableArray<LootContainerClass> ContainerClasses { get; }

    /// <summary>
    /// What every container class shares: its net cull distance (generated: <c>LootContainerClasses.NetCullDistance</c>)
    /// and target stacks (generated: <c>StaticValueLootTables.ContainerTargetStacks</c>).
    /// </summary>
    public LootContainerDefaults ContainerDefaults { get; }

    /// <summary>The value loot tables, ordered by package (generated: <c>StaticValueLootTables.All</c>).</summary>
    public ImmutableArray<StaticValueLootTable> StaticValueTables { get; }

    /// <summary>The skill loot tables, ordered by package (generated: <c>SkillLootTables.All</c>).</summary>
    public ImmutableArray<SkillLootTable> SkillTables { get; }

    /// <summary>The static loot spawn manager's tweakables (generated: <c>StaticLootSpawnTweakables</c>).</summary>
    public LootSpawnTweakables SpawnTweakables { get; }

    /// <summary>
    /// The container type of a socket tag (compared like the game compares tag names: ignoring case), or null
    /// (generated: <c>LootContainerTypes.Find</c>).
    /// </summary>
    public LootContainerType? FindContainerType(string tag) => _typesByTag.GetValueOrDefault(tag);

    /// <summary>
    /// The container class with this class path, or null when it does not decode. Unlike generated
    /// <c>LootContainerClasses.Find</c>, it also finds classes a server does not serve (see <see cref="ContainerClasses"/>).
    /// </summary>
    public LootContainerClass? FindContainerClass(string classPath) => _classesByPath.GetValueOrDefault(classPath);

    /// <summary>
    /// The value loot table with an asset name, ignoring case; null when no table or more than one table (in different
    /// folders) has it (generated: <c>StaticValueLootTables.Find</c>).
    /// </summary>
    /// <param name="name">Asset name, such as <c>StaticValueLootTable_ElectricalAll</c>.</param>
    public StaticValueLootTable? FindStaticValueTable(string name) => _valueTablesByName.GetValueOrDefault(name);
}
