using Ares.Common.GameData;
using Ares.Structs.Game.World;
using Ares.Structs.Net.GameplayTags;
using Ares.Structs.Net.Replication;

namespace Ares.Common.Tests;

/// <summary>
/// Local data checks: the Dam gameplay tables that ARES-GameDataExtract writes from the installed game equal the
/// generated Structs data, row for row and in the same order. They run only when
/// <see cref="DamWorldDataParityTests.DirectoryVariable"/> names an extracted game data directory and are skipped
/// otherwise. The hand-written constants that replace generated ones are compared with them always.
/// </summary>
public sealed class DamGameplayDataParityTests
{
    private static readonly string[] s_noTags = [];
    private static readonly RoundDataLayer[] s_noLayers = [];
    private static readonly GameplayTagQuery s_noQuery = new(s_noTags, []);

    [Fact]
    public void Game_mode_equals_the_generated_map_identity()
    {
        GameDataDirectory data = OpenOrSkip();

        MapGameMode mode = Assert.Single(data.ReadTable(MapTables.GameMode(DamMap.WorldName), GameDataJsonContext.Default.MapGameMode));

        Assert.Equal(new MapGameMode(DamMap.GameModeClass, DamMap.GameState, DamMap.LocationTag), mode);
    }

    [Fact]
    public void Doors_equal_the_generated_doors()
    {
        GameDataDirectory data = OpenOrSkip();

        Assert.Equal(DamDoors.All, data.ReadTable(MapTables.Doors(DamMap.WorldName), GameDataJsonContext.Default.DoorPlacement));
    }

    [Fact]
    public void Rounds_equal_the_generated_rounds()
    {
        GameDataDirectory data = OpenOrSkip();

        IReadOnlyList<RoundConfiguration> rounds = data.ReadTable(MapTables.Rounds(DamMap.WorldName), GameDataJsonContext.Default.RoundConfiguration);

        Assert.Equal(DamRounds.All.Select(WithoutLists), rounds.Select(WithoutLists));
        Assert.Equal(DamRounds.All.Select(Lists), rounds.Select(Lists));
    }

    [Fact]
    public void Lighting_scenarios_equal_the_generated_scenarios()
    {
        GameDataDirectory data = OpenOrSkip();

        IReadOnlyList<LightingScenario> scenarios = data.ReadTable(MapTables.LightingScenarios(DamMap.WorldName), GameDataJsonContext.Default.LightingScenario);

        Assert.Equal(DamLighting.Scenarios.Select(WithoutTags), scenarios.Select(WithoutTags));
        Assert.Equal(DamLighting.Scenarios.Select(row => string.Join('|', row.FilterTags)), scenarios.Select(row => string.Join('|', row.FilterTags)));
    }

    [Fact]
    public void Round_lighting_groups_equal_the_generated_groups()
    {
        GameDataDirectory data = OpenOrSkip();

        IReadOnlyList<RoundLightingGroup> groups = data.ReadTable(MapTables.RoundLightingGroups(DamMap.WorldName), GameDataJsonContext.Default.RoundLightingGroup);

        Assert.Equal(DamLighting.RoundLightingGroups.Keys, groups.Select(group => group.Tag));
        Assert.Equal(DamLighting.RoundLightingGroups.Values, groups.Select(group => group.Choices));
    }

    [Fact]
    public void Extraction_tables_equal_the_generated_extraction()
    {
        GameDataDirectory data = OpenOrSkip();

        ExtractionElevator elevator = Assert.Single(data.ReadTable(MapTables.ExtractionElevator(DamMap.WorldName), GameDataJsonContext.Default.ExtractionElevator));

        Assert.Equal(
            new ExtractionElevator(DamExtraction.ExtractionType, DamExtraction.SettingsAsset, DamExtraction.Settings, DamExtraction.ZoneHalfExtent,
                DamExtraction.ZoneLocation, DamExtraction.ZoneRotation, DamExtraction.NoHoldStartedTime, DamExtraction.NoExtractionStartedTime, DamExtraction.DoorsSeconds),
            elevator);
        Assert.Equal(DamExtraction.Buttons, data.ReadTable(MapTables.ExtractionButtons(DamMap.WorldName), GameDataJsonContext.Default.ExtractionButton));
        Assert.Equal(DamExtraction.All, data.ReadTable(MapTables.ExtractionPoints(DamMap.WorldName), GameDataJsonContext.Default.ExtractionPointPlacement));
    }

    [Fact]
    public void Zipline_defaults_equal_the_generated_defaults()
    {
        GameDataDirectory data = OpenOrSkip();

        ZiplineLineDefaults defaults = Assert.Single(data.ReadTable(MapTables.ZiplineDefaults(DamMap.WorldName), GameDataJsonContext.Default.ZiplineLineDefaults));

        Assert.Equal(
            new ZiplineLineDefaults(DamZiplines.ForcedTravelDirectionThresholdHome, DamZiplines.AttachmentOffset, DamZiplines.CharacterMeshLocation, DamZiplines.CharacterMeshYaw),
            defaults);
    }

    [Fact]
    public void Player_start_capsule_equals_the_generated_capsule()
    {
        GameDataDirectory data = OpenOrSkip();

        Assert.Equal(DamPlayerStarts.Capsule, Assert.Single(data.ReadTable(MapTables.PlayerStartCapsule(DamMap.WorldName), GameDataJsonContext.Default.PlayerStartCapsule)));
    }

    [Fact]
    public void Door_constants_equal_the_generated_constants()
    {
        Assert.Equal(DamDoors.ActorClass, DoorClasses.ActorClass);
        Assert.Equal(DamDoors.InteractionClass, DoorClasses.InteractionClass);
        Assert.Equal(DamDoors.InteractionComponents, DoorClasses.InteractionComponents);
        Assert.Equal(DamDoors.BreachInteractionComponent, DoorClasses.BreachInteractionComponent);
        Assert.Equal(DamDoors.BreachHoldSeconds, DoorClasses.BreachHoldSeconds);
        Assert.Equal(DamDoors.BreachAlignmentSeconds, DoorClasses.BreachAlignmentSeconds);
        AssertEnum(DamDoors.InteractionState, DoorClasses.InteractionState);
    }

    [Fact]
    public void Extraction_constants_equal_the_generated_constants()
    {
        Assert.Equal(DamExtraction.Elevator, ExtractionClasses.Elevator);
        Assert.Equal(DamExtraction.ActorClass, ExtractionClasses.ActorClass);
        Assert.Equal(DamExtraction.Buttons.Select(button => (button.Component, button.Role)), ExtractionClasses.Buttons);
        AssertEnum(DamExtraction.StateEnum, ExtractionClasses.StateEnum);
    }

    [Fact]
    public void Round_assets_equal_the_generated_rounds()
    {
        Assert.Equal(DamRounds.All.Select(round => round.Package), RoundConfigurationAssets.Dam);
    }

    private static GameDataDirectory OpenOrSkip()
    {
        string directory = Environment.GetEnvironmentVariable(DamWorldDataParityTests.DirectoryVariable) ?? string.Empty;
        Assert.SkipWhen(directory.Length == 0,
            $"Local data check: set {DamWorldDataParityTests.DirectoryVariable} to a game data directory written by ARES-GameDataExtract extract to compare it with the generated Structs data.");
        return GameDataDirectory.Open(directory);
    }

    private static void AssertEnum(EnumDefinition expected, EnumDefinition actual)
    {
        Assert.Equal(expected.Path, actual.Path);
        Assert.Equal(expected.MaxValue, actual.MaxValue);
        Assert.Equal(expected.Names, actual.Names);
    }

    /// <summary>The round with its lists replaced by shared empty ones, so that records compare by value.</summary>
    private static RoundConfiguration WithoutLists(RoundConfiguration round) =>
        round with { ContentQuery = s_noQuery, ContentQueryPvE = s_noQuery, DataLayers = s_noLayers, Tags = s_noTags };

    /// <summary>The round's lists, joined for comparison.</summary>
    private static string Lists(RoundConfiguration round) => string.Join(" / ",
        Query(round.ContentQuery), Query(round.ContentQueryPvE), string.Join('|', round.DataLayers), string.Join('|', round.Tags));

    private static string Query(GameplayTagQuery query) => $"{string.Join('|', query.TagDictionary)} {string.Join(',', query.TokenStream)}";

    /// <summary>The scenario with its tag list replaced by a shared empty one, so that records compare by value.</summary>
    private static LightingScenario WithoutTags(LightingScenario scenario) => scenario with { FilterTags = s_noTags };
}
