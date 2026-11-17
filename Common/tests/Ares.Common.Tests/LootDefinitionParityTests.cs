using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Ares.Common.Game.Loot;
using Ares.Common.GameData;
using Ares.Structs.Game.Loot;

namespace Ares.Common.Tests;

/// <summary>
/// Local data checks: the global loot tables that ARES-GameDataExtract writes from the installed game equal the generated
/// Structs data, row for row and in the same order (rows compare by their JSON form, so nested lists compare by value).
/// They run only when <see cref="DamWorldDataParityTests.DirectoryVariable"/> names an extracted game data directory and
/// are skipped otherwise.
/// </summary>
/// <remarks>
/// The class table lists every container class that decodes; the generated <see cref="LootContainerClasses.All"/> holds
/// the ones ARES serves, so the comparison leaves out <see cref="LootContainerSupport.Unsupported"/>.
/// </remarks>
public sealed class LootDefinitionParityTests
{
    private static readonly GameDataJsonContext s_json = GameDataJsonContext.Default;

    [Fact]
    public void Container_types_equal_the_generated_types()
    {
        GameDataDirectory data = OpenOrSkip();

        Assert.Equal(Rows(LootContainerTypes.All, s_json.LootContainerType), Rows(data.ReadTable(GameTables.LootContainerTypes, s_json.LootContainerType), s_json.LootContainerType));
    }

    [Fact]
    public void Served_container_classes_equal_the_generated_classes()
    {
        GameDataDirectory data = OpenOrSkip();

        IReadOnlyList<LootContainerClass> classes = data.ReadTable(GameTables.LootContainerClasses, s_json.LootContainerClass);

        Assert.Equal(
            Rows(LootContainerClasses.All, s_json.LootContainerClass),
            Rows(classes.Where(row => LootContainerSupport.IsSupported(row.Container.Class)), s_json.LootContainerClass));
        Assert.Equal(LootContainerSupport.Unsupported.Keys.Order(StringComparer.Ordinal), classes.Select(row => row.Container.Class).Where(path => !LootContainerSupport.IsSupported(path)));
    }

    [Fact]
    public void Unsupported_container_classes_equal_the_generated_list()
    {
        Assert.Equal(LootContainerClasses.Unsupported.OrderBy(pair => pair.Key, StringComparer.Ordinal), LootContainerSupport.Unsupported.OrderBy(pair => pair.Key, StringComparer.Ordinal));
    }

    [Fact]
    public void Container_defaults_equal_the_generated_defaults()
    {
        GameDataDirectory data = OpenOrSkip();

        LootContainerDefaults defaults = Assert.Single(data.ReadTable(GameTables.LootContainerDefaults, s_json.LootContainerDefaults));

        Assert.Equal(LootContainerClasses.NetCullDistance, defaults.NetCullDistance);
        Assert.Equal(StaticValueLootTables.ContainerTargetStacks, defaults.TargetStacks);
    }

    [Fact]
    public void Value_loot_tables_equal_the_generated_tables()
    {
        GameDataDirectory data = OpenOrSkip();

        Assert.Equal(Rows(StaticValueLootTables.All, s_json.StaticValueLootTable), Rows(data.ReadTable(GameTables.StaticValueLootTables, s_json.StaticValueLootTable), s_json.StaticValueLootTable));
    }

    [Fact]
    public void Skill_loot_tables_equal_the_generated_tables()
    {
        GameDataDirectory data = OpenOrSkip();

        Assert.Equal(Rows(SkillLootTables.All, s_json.SkillLootTable), Rows(data.ReadTable(GameTables.SkillLootTables, s_json.SkillLootTable), s_json.SkillLootTable));
    }

    [Fact]
    public void Spawn_tweakables_equal_the_generated_tweakables()
    {
        GameDataDirectory data = OpenOrSkip();

        LootSpawnTweakables tweakables = Assert.Single(data.ReadTable(GameTables.StaticLootSpawnTweakables, s_json.LootSpawnTweakables));

        Assert.Equal(
            new LootSpawnTweakables(
                StaticLootSpawnTweakables.NumPlacementsWeight,
                StaticLootSpawnTweakables.NumTargetItemsWeight,
                StaticLootSpawnTweakables.NumAvoidItemsWeight,
                StaticLootSpawnTweakables.NumMissingTargetStacksWeight,
                StaticLootSpawnTweakables.NumStacksWeight,
                StaticLootSpawnTweakables.NumItemsWeight,
                tweakables.ContainerContentsWeights),
            tweakables);
        Assert.Equal(StaticLootSpawnTweakables.ContainerContentsWeights, tweakables.ContainerContentsWeights);
    }

    private static GameDataDirectory OpenOrSkip()
    {
        string directory = Environment.GetEnvironmentVariable(DamWorldDataParityTests.DirectoryVariable) ?? string.Empty;
        Assert.SkipWhen(directory.Length == 0,
            $"Local data check: set {DamWorldDataParityTests.DirectoryVariable} to a game data directory written by ARES-GameDataExtract extract to compare it with the generated Structs data.");
        return GameDataDirectory.Open(directory);
    }

    private static string[] Rows<T>(IEnumerable<T> rows, JsonTypeInfo<T> type) => [.. rows.Select(row => JsonSerializer.Serialize(row, type))];
}
