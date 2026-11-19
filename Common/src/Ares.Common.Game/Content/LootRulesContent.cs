using System.Collections.Frozen;
using System.Globalization;
using Ares.Common.Game.Inventory;
using Ares.Common.Game.Loot;
using Ares.Common.GameData;
using Ares.Structs.Game.Inventory;
using Ares.Structs.Game.Loot;

namespace Ares.Common.Game.Content;

/// <summary>Turns <c>loot-rules.json</c> into <see cref="LootRules"/>, refusing anything the game data does not have.</summary>
internal static class LootRulesContent
{
    /// <summary>Checks the rules and resolves their items.</summary>
    /// <param name="document">The file.</param>
    /// <param name="data">The game data.</param>
    /// <param name="items">The item definitions.</param>
    /// <param name="invalid">Makes the error for a problem.</param>
    /// <returns>The rules.</returns>
    public static LootRules Resolve(LootRulesDocument document, GameDataSet data, LoadoutDefinitions items, Func<string, AresContentException> invalid)
    {
        if (document.Rules.Count == 0)
        {
            return LootRules.None;
        }

        string[] containerTags = [.. data.Loot.ContainerTypes.Select(type => type.Tag)];
        HashSet<string> ids = new(data.Maps.SelectMany(map => map.LootSockets.Select(LootContainerIds.Of)
            .Concat(map.PlacedLoot.Select(LootContainerIds.Of))
            .Concat(map.DynamicLootSpawners.Select(LootContainerIds.Of))), StringComparer.Ordinal);
        Dictionary<string, ItemDefinition> containerItems = items.ContainerItems.ToDictionary(item => item.TechnicalName, StringComparer.Ordinal);
        List<LootRule> rules = [];
        foreach ((LootRuleDocument? entry, int position) in document.Rules.Select((rule, index) => ((LootRuleDocument?)rule, index + 1)))
        {
            // The reader keeps null list entries, so each list is checked for them.
            LootRuleDocument rule = entry ?? throw invalid(string.Create(CultureInfo.InvariantCulture, $"rule #{position} is null"));
            string name = string.IsNullOrWhiteSpace(rule.Name) ? string.Create(CultureInfo.InvariantCulture, $"#{position}") : rule.Name;
            string[] tags = [.. (rule.Tags ?? []).OfType<string>()];
            string[] containers = [.. (rule.Containers ?? []).OfType<string>()];
            if (tags.Length != (rule.Tags?.Count ?? 0) || containers.Length != (rule.Containers?.Count ?? 0))
            {
                throw Problem("tags and containers must not hold null");
            }

            if (rule is { SpawnChance: null, Loot: null })
            {
                throw Problem("sets neither spawnChance nor loot");
            }

            if (rule.SpawnChance is { } and not (>= 0 and <= 1))
            {
                throw Problem("spawnChance must be from 0 to 1");
            }

            if (tags.FirstOrDefault(tag => !Array.Exists(containerTags, known => LootTags.IsTagOrBelow(known, tag))) is { } unknownTag)
            {
                throw Problem($"no container has the tag {unknownTag} or a tag below it");
            }

            if (containers.FirstOrDefault(id => !ids.Contains(id)) is { } unknownId)
            {
                throw Problem($"no map has the container {unknownId}");
            }

            rules.Add(new LootRule(name, tags, containers.ToFrozenSet(StringComparer.Ordinal), rule.SpawnChance, rule.Loot is { } loot ? List(loot) : null));
            continue;

            AresContentException Problem(string problem) => invalid($"rule {name}: {problem}");

            LootList List(LootListDocument list)
            {
                LootItemDocument[] entries = [.. list.Items.OfType<LootItemDocument>()];
                if (entries.Length == 0 || entries.Length != list.Items.Count)
                {
                    throw Problem("loot must list at least one item, and no null");
                }

                LootRange? stacks = list.Stacks is { } range ? Count(range, "loot stacks", 1, int.MaxValue) : null;
                return new LootList(stacks, [.. entries.Select(Item)]);
            }

            LootListItem Item(LootItemDocument listed)
            {
                if (!containerItems.TryGetValue(listed.Item, out ItemDefinition? item))
                {
                    throw Problem(data.Items.ByTechnicalName.ContainsKey(listed.Item)
                        ? $"{listed.Item} cannot be put in a loot container yet"
                        : $"the game data has no item {listed.Item}");
                }

                if (!(double.IsFinite(listed.Weight) && listed.Weight > 0))
                {
                    throw Problem($"the weight of {listed.Item} must be more than 0");
                }

                LootRange amount = listed.Amount is { } range ? Count(range, $"the amount of {listed.Item}", 1, item.MaxStack) : new LootRange(1, 1);
                return new LootListItem(item, listed.Weight, amount);
            }

            LootRange Count(LootCountDocument range, string what, int lowest, int highest)
            {
                return range.Min >= lowest && range.Max >= range.Min && range.Max <= highest ? new LootRange(range.Min, range.Max)
                    : throw Problem(string.Create(CultureInfo.InvariantCulture, $"{what} must have {lowest} <= min <= max{(highest == int.MaxValue ? string.Empty : $" <= {highest}")}"));
            }
        }

        return new LootRules(rules);
    }
}
