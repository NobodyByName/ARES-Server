using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Text.RegularExpressions;
using Ares.Common.GameData;
using Ares.Structs.Game.Movement;

namespace Ares.Common.Tests;

/// <summary>
/// Local data checks: the movement tables that ARES-GameDataExtract writes from the installed game equal the generated
/// Structs data exactly, row for row and in the same order. Rows are compared by their JSON form, which writes every
/// float and double as its shortest round-trip text (negative zero included), so equal text is equal bits. They run only
/// when <see cref="DamWorldDataParityTests.DirectoryVariable"/> names an extracted game data directory and are skipped
/// otherwise.
/// </summary>
public sealed partial class MovementDataParityTests
{
    [Fact]
    public void Dodge_rolls_equal_the_generated_sequences()
    {
        GameDataDirectory data = OpenOrSkip();

        AssertSameRows([DodgeRollSequences.Standard, DodgeRollSequences.Sprint, DodgeRollSequences.Exhausted, DodgeRollSequences.Ninja],
            data.ReadTable(GameTables.DodgeRolls, GameDataJsonContext.Default.DodgeRollSequence), GameDataJsonContext.Default.DodgeRollSequence);
    }

    [Fact]
    public void Vault_definitions_equal_the_generated_sets()
    {
        GameDataDirectory data = OpenOrSkip();

        AssertSameRows([VaultDefinitions.Armed, VaultDefinitions.Unarmed, VaultDefinitions.ArmedExhausted, VaultDefinitions.UnarmedExhausted],
            data.ReadTable(GameTables.VaultDefinitions, GameDataJsonContext.Default.VaultDefinitionSet), GameDataJsonContext.Default.VaultDefinitionSet);
    }

    [Fact]
    public void Heavy_landing_levels_equal_the_generated_levels()
    {
        GameDataDirectory data = OpenOrSkip();

        AssertSameRows(HeavyLandingAnimations.Heavy, data.ReadTable(GameTables.HeavyLandingLevels, GameDataJsonContext.Default.HeavyLandingLevel),
            GameDataJsonContext.Default.HeavyLandingLevel);
        AssertSameRows(HeavyLandingAnimations.Recovery, data.ReadTable(GameTables.RecoveryLandingLevels, GameDataJsonContext.Default.HeavyLandingLevel),
            GameDataJsonContext.Default.HeavyLandingLevel);
    }

    [Fact]
    public void Graph_state_tags_equal_the_generated_tag_bits()
    {
        GameDataDirectory data = OpenOrSkip();

        IReadOnlyList<string> tags = data.ReadTable(GameTables.GraphStateTags, GameDataJsonContext.Default.String);

        Assert.Equal(GraphStateTags.Count, tags.Count);
        Assert.Equal(TagBits(typeof(GraphStateTags), 0).Concat(TagBits(typeof(GraphStateUpperTags), 1)), tags.Select(tag => Pascal(tag.Replace(".", "", StringComparison.Ordinal))));
    }

    [Fact]
    public void Snap_hook_item_equals_the_generated_defaults()
    {
        GameDataDirectory data = OpenOrSkip();

        AssertSameRows([new SnapHookItemDefaults(SnapHookItem.DataAsset, SnapHookItem.Range, SnapHookItem.ActivationCost)],
            data.ReadTable(GameTables.SnapHookItem, GameDataJsonContext.Default.SnapHookItemDefaults), GameDataJsonContext.Default.SnapHookItemDefaults);
    }

    [Fact]
    public void Sprint_burst_curve_equals_the_generated_keys()
    {
        GameDataDirectory data = OpenOrSkip();

        AssertSameRows(SprintMovementParameters.BurstCurve, data.ReadTable(GameTables.SprintBurstCurve, GameDataJsonContext.Default.WeightedCurveKey),
            GameDataJsonContext.Default.WeightedCurveKey);
    }

    private static GameDataDirectory OpenOrSkip()
    {
        string directory = Environment.GetEnvironmentVariable(DamWorldDataParityTests.DirectoryVariable) ?? string.Empty;
        Assert.SkipWhen(directory.Length == 0,
            $"Local data check: set {DamWorldDataParityTests.DirectoryVariable} to a game data directory written by ARES-GameDataExtract extract to compare it with the generated Structs data.");
        return GameDataDirectory.Open(directory);
    }

    private static void AssertSameRows<TRow>(IReadOnlyList<TRow> expected, IReadOnlyList<TRow> actual, JsonTypeInfo<TRow> rowType)
    {
        Assert.Equal(expected.Select(row => JsonSerializer.Serialize(row, rowType)), actual.Select(row => JsonSerializer.Serialize(row, rowType)));
    }

    /// <summary>Names of a tag word's constants in bit order; each must be a single bit of that word.</summary>
    private static IEnumerable<string> TagBits(Type type, int word)
    {
        FieldInfo[] bits = [.. type.GetFields(BindingFlags.Public | BindingFlags.Static).Where(field => field.FieldType == typeof(ulong))];
        Assert.All(bits, field => Assert.Equal(1UL, ulong.PopCount((ulong)field.GetRawConstantValue()!)));
        Assert.True(bits.Length <= MovementAssets.GraphTagWordBits, $"Tag word {word} has more than {MovementAssets.GraphTagWordBits} tags");
        return bits.OrderBy(field => ulong.TrailingZeroCount((ulong)field.GetRawConstantValue()!)).Select(field => field.Name);
    }

    /// <summary>
    /// The C# name the generated data gives a game name: words split at case changes (an upper-case run is one
    /// acronym word) and digits, adjacent single capitals joined, each word capitalized and acronyms lower-cased after
    /// their first letter.
    /// </summary>
    private static string Pascal(string name)
    {
        List<string> words = [];
        bool singles = false;
        foreach (string word in name.Split('_').SelectMany(segment => WordPattern().Matches(segment).Select(match => match.Value)))
        {
            bool single = word.Length == 1 && char.IsUpper(word[0]);
            if (single && singles)
            {
                words[^1] += word;
            }
            else
            {
                words.Add(word);
            }

            singles = single;
        }

        return string.Concat(words.Select(word => word.Any(char.IsLetter) && !word.Any(char.IsLower)
            ? char.ToUpperInvariant(word[0]) + word[1..].ToLowerInvariant()
            : char.ToUpperInvariant(word[0]) + word[1..]));
    }

    [GeneratedRegex("[A-Z]+(?![a-z])|[A-Z]?[a-z]+|[0-9]+")]
    private static partial Regex WordPattern();
}
