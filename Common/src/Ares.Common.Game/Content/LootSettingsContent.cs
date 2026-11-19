using System.Collections.Frozen;
using System.Text.Json.Serialization;
using Ares.Common.Game.Loot;
using Ares.Common.GameData;
using Ares.Structs.Game.Loot;
using Ares.Structs.Game.World;

namespace Ares.Common.Game.Content;

/// <summary><c>loot-spawn.json</c>: how loot containers spawn.</summary>
internal sealed record LootSpawnDocument(
    [property: JsonPropertyName("chances")] IReadOnlyDictionary<string, double> Chances,
    [property: JsonPropertyName("areaMultipliers")] IReadOnlyDictionary<string, double> AreaMultipliers,
    [property: JsonPropertyName("groupCaps")] bool GroupCaps,
    [property: JsonPropertyName("limiters")] bool Limiters,
    [property: JsonPropertyName("spawnerClasses")] IReadOnlyList<string> SpawnerClasses);

/// <summary><c>loot-contents.json</c>: what loot containers hold.</summary>
internal sealed record LootContentsDocument(
    [property: JsonPropertyName("excludedDropTags")] IReadOnlyList<string> ExcludedDropTags,
    [property: JsonPropertyName("tiers")] IReadOnlyList<LootTierDocument> Tiers,
    [property: JsonPropertyName("firstStackTier")] string? FirstStackTier,
    [property: JsonPropertyName("tablePools")] IReadOnlyList<LootTablePoolDocument> TablePools,
    [property: JsonPropertyName("extraStack")] LootExtraStackDocument ExtraStack,
    [property: JsonPropertyName("typePools")] IReadOnlyList<LootTypePoolDocument> TypePools);

/// <summary>A tier of <c>loot-contents.json</c>.</summary>
internal sealed record LootTierDocument(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("weight")] double Weight,
    [property: JsonPropertyName("dropTag")] string? DropTag = null,
    [property: JsonPropertyName("areaMultipliers")] IReadOnlyDictionary<string, double>? AreaMultipliers = null);

/// <summary>A table pool of <c>loot-contents.json</c>.</summary>
internal sealed record LootTablePoolDocument(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("tag")] string Tag,
    [property: JsonPropertyName("tables")] IReadOnlyList<string> Tables,
    [property: JsonPropertyName("stacks")] LootCountDocument? Stacks = null,
    [property: JsonPropertyName("extraStack")] bool ExtraStack = true);

/// <summary>An entry of a type pool or of the extra stack.</summary>
internal sealed record LootTypeEntryDocument(
    [property: JsonPropertyName("dropTag")] string DropTag,
    [property: JsonPropertyName("amount")] LootCountDocument Amount,
    [property: JsonPropertyName("weight")] double Weight = 1,
    [property: JsonPropertyName("excludedDropTags")] IReadOnlyList<string>? ExcludedDropTags = null);

/// <summary>The extra stack of <c>loot-contents.json</c>.</summary>
internal sealed record LootExtraStackDocument(
    [property: JsonPropertyName("chance")] double Chance,
    [property: JsonPropertyName("entries")] IReadOnlyList<LootTypeEntryDocument> Entries);

/// <summary>A type pool of <c>loot-contents.json</c>.</summary>
internal sealed record LootTypePoolDocument(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("stacks")] LootCountDocument Stacks,
    [property: JsonPropertyName("entries")] IReadOnlyList<LootTypeEntryDocument> Entries,
    [property: JsonPropertyName("classes")] IReadOnlyList<string>? Classes = null,
    [property: JsonPropertyName("tags")] IReadOnlyList<string>? Tags = null);

/// <summary>Turns <c>loot-spawn.json</c> and <c>loot-contents.json</c> into settings, refusing anything the game data does not have.</summary>
internal static class LootSettingsContent
{
    /// <summary>Checks the spawn settings.</summary>
    public static LootSpawnSettings Spawn(LootSpawnDocument document, GameDataSet data, Func<string, AresContentException> invalid)
    {
        Names names = new(data, invalid);
        Dictionary<string, double> chances = new(StringComparer.OrdinalIgnoreCase);
        foreach ((string tag, double chance) in document.Chances)
        {
            names.ContainerTag(tag, "chances");
            names.Fraction(chance, $"the chance of {tag}");
            if (!chances.TryAdd(tag, chance))
            {
                throw invalid($"chances lists {tag} twice");
            }
        }

        names.NoNulls(document.SpawnerClasses, "spawnerClasses");
        foreach (string path in document.SpawnerClasses)
        {
            names.ContainerClass(path, "spawnerClasses");
        }

        return new LootSpawnSettings(chances.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase), names.AreaFactors(document.AreaMultipliers, "areaMultipliers"),
            document.GroupCaps, document.Limiters, document.SpawnerClasses.ToFrozenSet(StringComparer.Ordinal));
    }

    /// <summary>Checks the content settings.</summary>
    public static LootContentSettings Contents(LootContentsDocument document, GameDataSet data, Func<string, AresContentException> invalid)
    {
        Names names = new(data, invalid);
        names.NoNulls(document.ExcludedDropTags, "excludedDropTags");
        names.NoNulls(document.Tiers, "tiers");
        names.NoNulls(document.TablePools, "tablePools");
        names.NoNulls(document.TypePools, "typePools");
        foreach (string tag in document.ExcludedDropTags)
        {
            names.DropTag(tag, "excludedDropTags");
        }

        List<LootTier> tiers = [];
        foreach (LootTierDocument tier in document.Tiers)
        {
            if (tiers.Exists(other => string.Equals(other.Name, tier.Name, StringComparison.Ordinal)))
            {
                throw invalid($"tiers lists {tier.Name} twice");
            }

            if (tier.DropTag is { } dropTag)
            {
                names.DropTag(dropTag, $"tier {tier.Name}");
            }

            names.Weight(tier.Weight, $"the weight of tier {tier.Name}");
            tiers.Add(new LootTier(tier.Name, tier.DropTag, tier.Weight, names.AreaFactors(tier.AreaMultipliers ?? new Dictionary<string, double>(), $"tier {tier.Name}")));
        }

        if (tiers.Count(tier => tier.DropTag is null) != 1)
        {
            throw invalid("tiers must hold exactly one tier without a dropTag, for every other item");
        }

        if (document.FirstStackTier is { } first && !tiers.Exists(tier => string.Equals(tier.Name, first, StringComparison.Ordinal)))
        {
            throw invalid($"firstStackTier {first} is not a tier");
        }

        List<LootTablePool> tablePools = [];
        foreach (LootTablePoolDocument pool in document.TablePools)
        {
            string where = $"table pool {pool.Name}";
            names.ContainerTag(pool.Tag, where);
            if (tablePools.Exists(other => string.Equals(other.Tag, pool.Tag, StringComparison.OrdinalIgnoreCase)))
            {
                throw invalid($"two table pools have the tag {pool.Tag}");
            }

            names.NoNulls(pool.Tables, where);
            foreach (string table in pool.Tables)
            {
                // A name may belong to tables in several folders; a pool draws from all of them.
                if (!data.Loot.StaticValueTables.Any(known => string.Equals(known.Name, table, StringComparison.OrdinalIgnoreCase)))
                {
                    throw invalid($"{where}: the game data has no loot table {table}");
                }
            }

            tablePools.Add(new LootTablePool(pool.Name, pool.Tag, pool.Tables, pool.Stacks is { } stacks ? names.Count(stacks, $"{where} stacks") : null, pool.ExtraStack));
        }

        names.Fraction(document.ExtraStack.Chance, "the extra stack's chance");
        LootExtraStack extra = new(document.ExtraStack.Chance, names.Entries(document.ExtraStack.Entries, "the extra stack"));
        List<LootTypePool> typePools = [];
        foreach (LootTypePoolDocument pool in document.TypePools)
        {
            string where = $"type pool {pool.Name}";
            IReadOnlyList<string> classes = pool.Classes ?? [];
            IReadOnlyList<string> tags = pool.Tags ?? [];
            names.NoNulls(classes, where);
            names.NoNulls(tags, where);
            foreach (string path in classes)
            {
                names.ContainerClass(path, where);
            }

            foreach (string tag in tags)
            {
                names.ContainerTag(tag, where);
            }

            typePools.Add(new LootTypePool(pool.Name, classes, tags, names.Count(pool.Stacks, $"{where} stacks"), names.Entries(pool.Entries, where)));
        }

        return new LootContentSettings(document.ExcludedDropTags, tiers, document.FirstStackTier, tablePools, extra, typePools);
    }

    // Checks of names against the game data, and of numbers.
    private sealed class Names(GameDataSet data, Func<string, AresContentException> invalid)
    {
        private readonly string[] _containerTags = [.. data.Loot.ContainerTypes.Select(type => type.Tag)];
        private readonly HashSet<string> _dropTags = new(data.Items.All.SelectMany(item => item.DropTags), StringComparer.OrdinalIgnoreCase);

        public void ContainerTag(string tag, string where)
        {
            if (!Array.Exists(_containerTags, known => LootTags.IsTagOrBelow(known, tag)))
            {
                throw invalid($"{where}: no container has the tag {tag} or a tag below it");
            }
        }

        public void ContainerClass(string path, string where)
        {
            if (data.Loot.FindContainerClass(path) is null)
            {
                throw invalid($"{where}: the game data has no container class {path}");
            }
        }

        public void DropTag(string tag, string where)
        {
            if (!_dropTags.Any(known => LootTags.IsTagOrBelow(known, tag)))
            {
                throw invalid($"{where}: no item has the drop tag {tag} or a tag below it");
            }
        }

        public void Fraction(double value, string what)
        {
            if (value is not (>= 0 and <= 1))
            {
                throw invalid($"{what} must be from 0 to 1");
            }
        }

        public void Weight(double value, string what)
        {
            if (!(double.IsFinite(value) && value >= 0))
            {
                throw invalid($"{what} must be 0 or more");
            }
        }

        public void NoNulls<T>(IReadOnlyList<T> list, string where)
        {
            // The reader keeps null list entries.
            if (list.Any(entry => entry is null))
            {
                throw invalid($"{where} must not hold null");
            }
        }

        public LootRange Count(LootCountDocument range, string what) =>
            range.Min >= 1 && range.Max >= range.Min ? new LootRange(range.Min, range.Max) : throw invalid($"{what} must have 1 <= min <= max");

        public FrozenDictionary<PointOfInterestLootValue, double> AreaFactors(IReadOnlyDictionary<string, double> factors, string where)
        {
            Dictionary<PointOfInterestLootValue, double> result = [];
            foreach ((string name, double factor) in factors)
            {
                if (!Enum.TryParse(name, ignoreCase: false, out PointOfInterestLootValue value) || !Enum.IsDefined(value))
                {
                    throw invalid($"{where}: {name} is not a loot value ({string.Join(", ", Enum.GetNames<PointOfInterestLootValue>())})");
                }

                Weight(factor, $"{where}: the factor of {name}");
                result[value] = factor;
            }

            return result.ToFrozenDictionary();
        }

        public List<LootTypeEntry> Entries(IReadOnlyList<LootTypeEntryDocument> entries, string where)
        {
            NoNulls(entries, where);
            List<LootTypeEntry> result = [];
            foreach (LootTypeEntryDocument entry in entries)
            {
                DropTag(entry.DropTag, where);
                IReadOnlyList<string> excluded = entry.ExcludedDropTags ?? [];
                NoNulls(excluded, where);
                foreach (string tag in excluded)
                {
                    DropTag(tag, where);
                }

                Weight(entry.Weight, $"{where}: the weight of {entry.DropTag}");
                result.Add(new LootTypeEntry(entry.DropTag, excluded, Count(entry.Amount, $"{where}: the amount of {entry.DropTag}"), entry.Weight));
            }

            return result;
        }
    }
}
