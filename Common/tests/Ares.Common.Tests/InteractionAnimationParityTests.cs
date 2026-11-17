using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Ares.Common.Game.Loot;
using Ares.Common.GameData;
using Ares.Structs.Game.Loot;
using Ares.Structs.Game.Movement;
using Ares.Structs.Game.World;

namespace Ares.Common.Tests;

/// <summary>
/// Local data checks: the interaction animation tables that ARES-GameDataExtract writes from the installed game equal the
/// generated <see cref="InteractionAnimationContainers"/>, row for row and in the same order (rows compare by their JSON
/// form). They run only when <see cref="DamWorldDataParityTests.DirectoryVariable"/> names an extracted game data
/// directory and are skipped otherwise.
/// </summary>
/// <remarks>
/// The container table holds the search animations of every loot container class that decodes; the generated list holds
/// those of the classes ARES serves, so the comparison leaves out the containers that only classes of
/// <see cref="LootContainerSupport.Unsupported"/> use.
/// </remarks>
public sealed class InteractionAnimationParityTests
{
    private static readonly GameDataJsonContext s_json = GameDataJsonContext.Default;

    [Fact]
    public void Served_containers_equal_the_generated_containers()
    {
        GameDataDirectory data = OpenOrSkip();
        IReadOnlyList<InteractionAnimationContainer> containers = data.ReadTable(GameTables.InteractionAnimationContainers, s_json.InteractionAnimationContainer);
        IReadOnlyList<LootContainerClass> classes = data.ReadTable(GameTables.LootContainerClasses, s_json.LootContainerClass);
        HashSet<string> served = [.. classes.Where(row => LootContainerSupport.IsSupported(row.Container.Class)).Select(row => row.SearchAnimation).OfType<string>().Select(ObjectPath)];
        served.UnionWith(data.ReadTable(MapTables.ExtractionButtons(DamMap.WorldName), s_json.ExtractionButton).Select(button => button.AnimationContainer).OfType<string>().Select(ObjectPath));
        served.UnionWith(data.ReadTable(GameTables.TaggedInteractionAnimationContainers, s_json.TaggedInteractionAnimationContainer).Select(entry => entry.Container));
        string[] unservedOnly =
        [
            .. classes.Where(row => !LootContainerSupport.IsSupported(row.Container.Class)).Select(row => row.SearchAnimation).OfType<string>().Select(ObjectPath)
                .Where(path => !served.Contains(path) && InteractionAnimationContainers.All.All(container => container.Path != path)).Distinct(StringComparer.Ordinal),
        ];

        Assert.Equal(Rows(InteractionAnimationContainers.All, s_json.InteractionAnimationContainer),
            Rows(containers.Where(container => !unservedOnly.Contains(container.Path, StringComparer.Ordinal)), s_json.InteractionAnimationContainer));
        Assert.All(unservedOnly, path => Assert.Contains(containers, container => container.Path == path));
    }

    [Fact]
    public void Tag_based_entries_equal_the_generated_entries()
    {
        GameDataDirectory data = OpenOrSkip();

        Assert.Equal(Rows(InteractionAnimationContainers.TagBased, s_json.TaggedInteractionAnimationContainer),
            Rows(data.ReadTable(GameTables.TaggedInteractionAnimationContainers, s_json.TaggedInteractionAnimationContainer), s_json.TaggedInteractionAnimationContainer));
    }

    private static GameDataDirectory OpenOrSkip()
    {
        string directory = Environment.GetEnvironmentVariable(DamWorldDataParityTests.DirectoryVariable) ?? string.Empty;
        Assert.SkipWhen(directory.Length == 0,
            $"Local data check: set {DamWorldDataParityTests.DirectoryVariable} to a game data directory written by ARES-GameDataExtract extract to compare it with the generated Structs data.");
        return GameDataDirectory.Open(directory);
    }

    /// <summary>The object path of a data asset named like its package (a package path gets the asset's name).</summary>
    private static string ObjectPath(string path)
    {
        string name = path[(path.LastIndexOf('/') + 1)..];
        return name.Contains('.', StringComparison.Ordinal) ? path : $"{path}.{name}";
    }

    private static string[] Rows<T>(IEnumerable<T> rows, JsonTypeInfo<T> type) => [.. rows.Select(row => JsonSerializer.Serialize(row, type))];
}
