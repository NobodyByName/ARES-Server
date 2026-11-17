using System.Collections.Immutable;
using Ares.GameDataExtract.Packages;
using Ares.GameDataExtract.Properties;
using Ares.Structs.Game.World;

namespace Ares.GameDataExtract.World;

/// <summary>
/// Reads the lighting assets a map's level script names: the lighting scenarios of its lookup asset
/// (<see cref="LightingScenario"/>) and the round lighting groups (<see cref="RoundLightingGroup"/>), both in the order
/// their maps store them.
/// </summary>
internal static class LightingReader
{
    private const double MaxWeight = 1000;

    /// <summary>The scenarios of the lookup asset <paramref name="lighting"/>.</summary>
    /// <exception cref="InvalidDataException">A scenario does not decode, names a lighting level, or has an invalid weight.</exception>
    public static ImmutableArray<LightingScenario> Scenarios(MapRuleSources sources, ExportRef lighting)
    {
        PropertyBag asset = sources.Members(lighting, LightingAssetMembers.LightingScenarios);
        ImmutableArray<LightingScenario>.Builder rows = ImmutableArray.CreateBuilder<LightingScenario>();
        foreach ((object key, object value) in asset.Get<KeyValuePair<object, object>[]>(LightingAssetMembers.LightingScenarios))
        {
            string tag = MapRuleSources.Tag((PropertyBag)key);
            PropertyBag scenario = (PropertyBag)value;
            double weight = scenario.Get(LightingAssetMembers.Weight, 0.0);
            if (!scenario.Get(LightingAssetMembers.Level, SoftObjectPath.Null).IsNull || weight is not (>= 0 and <= MaxWeight))
            {
                throw new InvalidDataException($"{lighting.Path}: unsupported lighting scenario {tag}");
            }

            (string layer, ulong hash) = sources.PublicExport(lighting.Package, scenario.Get<ObjectReference>(LightingAssetMembers.DataLayer));
            rows.Add(new LightingScenario(tag, weight, scenario.Get(LightingAssetMembers.FilterTags, Array.Empty<string>()), layer, hash));
        }

        return rows.ToImmutable();
    }

    /// <summary>The round lighting groups of <paramref name="groups"/>.</summary>
    /// <exception cref="InvalidDataException">A group does not decode or has an invalid weight.</exception>
    public static ImmutableArray<RoundLightingGroup> Groups(MapRuleSources sources, ExportRef groups)
    {
        PropertyBag asset = sources.Members(groups, LightingAssetMembers.LightingScenarioEntries);
        ImmutableArray<RoundLightingGroup>.Builder rows = ImmutableArray.CreateBuilder<RoundLightingGroup>();
        foreach ((object key, object value) in asset.Get<KeyValuePair<object, object>[]>(LightingAssetMembers.LightingScenarioEntries))
        {
            string tag = MapRuleSources.Tag((PropertyBag)key);
            WeightedLighting[] choices =
            [
                .. ((PropertyBag)value).Get(LightingAssetMembers.Entries, Array.Empty<object>()).Cast<PropertyBag>().Select(entry =>
                    new WeightedLighting(MapRuleSources.Tag(entry.Get<PropertyBag>(LightingAssetMembers.ScenarioName)), entry.Get(LightingAssetMembers.Weight, 0.0))),
            ];
            if (choices.Any(choice => choice.Weight is not (>= 0 and <= MaxWeight)))
            {
                throw new InvalidDataException($"{groups.Path}: invalid weight in round lighting group {tag}");
            }

            rows.Add(new RoundLightingGroup(tag, choices));
        }

        return rows.ToImmutable();
    }
}
