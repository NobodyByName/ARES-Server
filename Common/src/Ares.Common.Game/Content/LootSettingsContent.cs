using System.Collections.Frozen;
using System.Text.Json;
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
    [property: JsonPropertyName("spawnerClasses")] IReadOnlyList<string> SpawnerClasses,
    [property: JsonPropertyName("looseChances")] IReadOnlyDictionary<string, double> LooseChances);

/// <summary><c>loot-contents.json</c>: what loot containers hold.</summary>
internal sealed record LootContentsDocument(
    [property: JsonPropertyName("excludedDropTags")] IReadOnlyList<string> ExcludedDropTags,
    [property: JsonPropertyName("tiers")] IReadOnlyList<LootTierDocument> Tiers,
    [property: JsonPropertyName("firstStackTier")] string? FirstStackTier,
    [property: JsonPropertyName("tablePools")] IReadOnlyList<LootTablePoolDocument> TablePools,
    [property: JsonPropertyName("extraStack")] LootExtraStackDocument ExtraStack,
    [property: JsonPropertyName("typePools")] IReadOnlyList<LootTypePoolDocument> TypePools,
    [property: JsonPropertyName("looseItems")] LooseItemsDocument LooseItems,
    [property: JsonPropertyName("profiles")] IReadOnlyList<LootProfileDocument>? Profiles = null);

/// <summary>What loose item sockets hold, in <c>loot-contents.json</c>.</summary>
internal sealed record LooseItemsDocument(
    [property: JsonPropertyName("tables")] IReadOnlyList<string> Tables,
    [property: JsonPropertyName("amount")] LootCountDocument Amount,
    [property: JsonPropertyName("pools")] IReadOnlyList<LooseItemPoolDocument> Pools);

/// <summary>A pool of loose item sockets.</summary>
internal sealed record LooseItemPoolDocument(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("tags")] IReadOnlyList<string> Tags,
    [property: JsonPropertyName("entries")] IReadOnlyList<LootTypeEntryDocument> Entries,
    [property: JsonPropertyName("tableWeight")] double TableWeight = 1);

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

/// <summary>
/// A contents profile of <c>loot-contents.json</c>. <see cref="FirstStackTier"/> stays a raw value: an absent member
/// inherits, <c>null</c> forces no tier.
/// </summary>
internal sealed record LootProfileDocument(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("tag")] string? Tag = null,
    [property: JsonPropertyName("class")] string? Class = null,
    [property: JsonPropertyName("only")] IReadOnlyList<LootMatchDocument>? Only = null,
    [property: JsonPropertyName("weights")] IReadOnlyList<LootProfileWeightDocument>? Weights = null,
    [property: JsonPropertyName("add")] IReadOnlyList<LootProfileAddDocument>? Add = null,
    [property: JsonPropertyName("tiers")] IReadOnlyDictionary<string, double>? Tiers = null,
    [property: JsonPropertyName("firstStackTier")] JsonElement FirstStackTier = default,
    [property: JsonPropertyName("stacks")] LootCountDocument? Stacks = null,
    [property: JsonPropertyName("extraStack")] LootProfileExtraStackDocument? ExtraStack = null);

/// <summary>A match of a profile's <c>only</c> list: a drop tag or an item.</summary>
internal sealed record LootMatchDocument(
    [property: JsonPropertyName("dropTag")] string? DropTag = null,
    [property: JsonPropertyName("item")] string? Item = null);

/// <summary>A weight of a profile: a match and its multiplier.</summary>
internal sealed record LootProfileWeightDocument(
    [property: JsonPropertyName("multiplier")] double Multiplier,
    [property: JsonPropertyName("dropTag")] string? DropTag = null,
    [property: JsonPropertyName("item")] string? Item = null);

/// <summary>An added item of a profile: a match, its amount and optionally its tier.</summary>
internal sealed record LootProfileAddDocument(
    [property: JsonPropertyName("amount")] LootCountDocument Amount,
    [property: JsonPropertyName("dropTag")] string? DropTag = null,
    [property: JsonPropertyName("item")] string? Item = null,
    [property: JsonPropertyName("tier")] string? Tier = null);

/// <summary>The extra stack of a profile; each absent part inherits.</summary>
internal sealed record LootProfileExtraStackDocument(
    [property: JsonPropertyName("chance")] double? Chance = null,
    [property: JsonPropertyName("entries")] IReadOnlyList<LootTypeEntryDocument>? Entries = null);

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

        Dictionary<string, double> looseChances = new(StringComparer.OrdinalIgnoreCase);
        foreach ((string tag, double chance) in document.LooseChances)
        {
            names.LooseTag(tag, "looseChances");
            names.Fraction(chance, $"the chance of {tag}");
            if (!looseChances.TryAdd(tag, chance))
            {
                throw invalid($"looseChances lists {tag} twice");
            }
        }

        names.NoNulls(document.SpawnerClasses, "spawnerClasses");
        foreach (string path in document.SpawnerClasses)
        {
            names.ContainerClass(path, "spawnerClasses");
        }

        return new LootSpawnSettings(chances.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase), names.AreaFactors(document.AreaMultipliers, "areaMultipliers"),
            document.GroupCaps, document.Limiters, document.SpawnerClasses.ToFrozenSet(StringComparer.Ordinal),
            looseChances.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase));
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
                names.Table(table, where);
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

        return new LootContentSettings(document.ExcludedDropTags, tiers, document.FirstStackTier, tablePools, extra, typePools,
            Profiles(document.Profiles ?? [], tiers, names, invalid), LooseItems(document.LooseItems, names, invalid));
    }

    private static List<LootProfile> Profiles(IReadOnlyList<LootProfileDocument> documents, List<LootTier> tiers, Names names, Func<string, AresContentException> invalid)
    {
        names.NoNulls(documents, "profiles");
        List<LootProfile> profiles = [];
        foreach (LootProfileDocument document in documents)
        {
            string where = $"profile {document.Name}";
            if (profiles.Exists(other => string.Equals(other.Name, document.Name, StringComparison.Ordinal)))
            {
                throw invalid($"profiles list {document.Name} twice");
            }

            if ((document.Tag is null) == (document.Class is null))
            {
                throw invalid($"{where} must set exactly one of tag and class");
            }

            bool loose = false;
            if (document.Tag is { } tag)
            {
                loose = names.ProfileTag(tag, where);
                if (profiles.Exists(other => other.Tag is { } otherTag && string.Equals(otherTag, tag, StringComparison.OrdinalIgnoreCase)))
                {
                    throw invalid($"two profiles have the tag {tag}");
                }
            }
            else if (document.Class is { } path)
            {
                names.ServedContainerClass(path, where);
                if (profiles.Exists(other => string.Equals(other.Class, path, StringComparison.Ordinal)))
                {
                    throw invalid($"two profiles have the class {path}");
                }
            }

            if (loose && (document.FirstStackTier.ValueKind != JsonValueKind.Undefined || document.Stacks is not null || document.ExtraStack is not null))
            {
                throw invalid($"{where}: a loose item socket profile cannot set firstStackTier, stacks or extraStack");
            }

            profiles.Add(new LootProfile(document.Name, document.Tag, document.Class, Only(document.Only, names, where), Weights(document.Weights ?? [], names, invalid, where),
                Adds(document.Add ?? [], tiers, names, invalid, where), ProfileTiers(document.Tiers, tiers, names, invalid, where),
                FirstStackTier(document.FirstStackTier, tiers, invalid, where), Stacks(document.Stacks, invalid, where), ExtraStack(document.ExtraStack, names, where)));
        }

        return profiles;
    }

    private static List<LootMatch>? Only(IReadOnlyList<LootMatchDocument>? only, Names names, string where)
    {
        if (only is null)
        {
            return null;
        }

        names.NoNulls(only, $"{where} only");
        return [.. only.Select(match => names.Match(match.DropTag, match.Item, where))];
    }

    private static List<LootProfileWeight> Weights(IReadOnlyList<LootProfileWeightDocument> weights, Names names, Func<string, AresContentException> invalid, string where)
    {
        names.NoNulls(weights, $"{where} weights");
        List<LootProfileWeight> result = [];
        foreach (LootProfileWeightDocument weight in weights)
        {
            LootMatch match = names.Match(weight.DropTag, weight.Item, where);
            if (result.Exists(other => other.Match.SameKey(match)))
            {
                throw invalid($"{where}: weights list {match} twice");
            }

            names.Weight(weight.Multiplier, $"{where}: the multiplier of {match}");
            result.Add(new LootProfileWeight(match, weight.Multiplier));
        }

        return result;
    }

    private static List<LootProfileAdd> Adds(IReadOnlyList<LootProfileAddDocument> adds, List<LootTier> tiers, Names names, Func<string, AresContentException> invalid, string where)
    {
        names.NoNulls(adds, $"{where} add");
        List<LootProfileAdd> result = [];
        foreach (LootProfileAddDocument add in adds)
        {
            LootMatch match = names.Match(add.DropTag, add.Item, where);
            if (result.Exists(other => other.Match.SameKey(match)))
            {
                throw invalid($"{where}: add lists {match} twice");
            }

            if (add.Tier is { } tier)
            {
                Tier(tier, tiers, invalid, where);
            }

            result.Add(new LootProfileAdd(match, names.Count(add.Amount, $"{where}: the amount of {match}"), add.Tier));
        }

        return result;
    }

    private static FrozenDictionary<string, double> ProfileTiers(IReadOnlyDictionary<string, double>? factors, List<LootTier> tiers, Names names,
        Func<string, AresContentException> invalid, string where)
    {
        if (factors is null)
        {
            return FrozenDictionary<string, double>.Empty;
        }

        foreach ((string tier, double factor) in factors)
        {
            Tier(tier, tiers, invalid, where);
            names.Weight(factor, $"{where}: the multiplier of tier {tier}");
        }

        return factors.ToFrozenDictionary(StringComparer.Ordinal);
    }

    private static LootFirstStackTier? FirstStackTier(JsonElement value, List<LootTier> tiers, Func<string, AresContentException> invalid, string where)
    {
        switch (value.ValueKind)
        {
            case JsonValueKind.Undefined:
                return null;
            case JsonValueKind.Null:
                return new LootFirstStackTier(null);
            case JsonValueKind.String:
                string tier = value.GetString()!;
                Tier(tier, tiers, invalid, where);
                return new LootFirstStackTier(tier);
            default:
                throw invalid($"{where}: firstStackTier must be a tier name or null");
        }
    }

    private static LootRange? Stacks(LootCountDocument? stacks, Func<string, AresContentException> invalid, string where) => stacks switch
    {
        null => null,
        { Min: >= 0 } when stacks.Max >= stacks.Min => new LootRange(stacks.Min, stacks.Max),
        _ => throw invalid($"{where} stacks must have 0 <= min <= max"),
    };

    private static LootProfileExtraStack? ExtraStack(LootProfileExtraStackDocument? extra, Names names, string where)
    {
        if (extra is null)
        {
            return null;
        }

        if (extra.Chance is { } chance)
        {
            names.Fraction(chance, $"{where}: the extra stack's chance");
        }

        return new LootProfileExtraStack(extra.Chance, extra.Entries is { } entries ? names.Entries(entries, $"{where} extra stack") : null);
    }

    private static void Tier(string tier, List<LootTier> tiers, Func<string, AresContentException> invalid, string where)
    {
        if (!tiers.Exists(known => string.Equals(known.Name, tier, StringComparison.Ordinal)))
        {
            throw invalid($"{where}: {tier} is not a tier");
        }
    }

    private static LooseItemSettings LooseItems(LooseItemsDocument document, Names names, Func<string, AresContentException> invalid)
    {
        names.NoNulls(document.Tables, "looseItems tables");
        names.NoNulls(document.Pools, "looseItems pools");
        foreach (string table in document.Tables)
        {
            names.Table(table, "looseItems");
        }

        List<LooseItemPool> pools = [];
        HashSet<string> listed = new(StringComparer.OrdinalIgnoreCase);
        foreach (LooseItemPoolDocument pool in document.Pools)
        {
            string where = $"loose item pool {pool.Name}";
            names.NoNulls(pool.Tags, where);
            foreach (string tag in pool.Tags)
            {
                names.LooseTag(tag, where);
                if (!listed.Add(tag))
                {
                    throw invalid($"loose item pools list the tag {tag} twice");
                }
            }

            names.Weight(pool.TableWeight, $"{where}: tableWeight");
            pools.Add(new LooseItemPool(pool.Name, pool.Tags, pool.TableWeight, names.Entries(pool.Entries, where)));
        }

        return new LooseItemSettings(document.Tables, names.Count(document.Amount, "looseItems amount"), pools);
    }

    // Checks of names against the game data, and of numbers.
    private sealed class Names(GameDataSet data, Func<string, AresContentException> invalid)
    {
        private readonly string[] _containerTags = [.. data.Loot.ContainerTypes.Select(type => type.Tag)];
        private readonly HashSet<string> _dropTags = new(data.Items.All.SelectMany(item => item.DropTags), StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _looseTags = new(data.Maps.SelectMany(map => map.LooseItemSockets).Select(socket => socket.Tag), StringComparer.OrdinalIgnoreCase);

        public void ContainerTag(string tag, string where)
        {
            if (!Array.Exists(_containerTags, known => LootTags.IsTagOrBelow(known, tag)))
            {
                throw invalid($"{where}: no container has the tag {tag} or a tag below it");
            }
        }

        public void LooseTag(string tag, string where)
        {
            if (!_looseTags.Any(known => LootTags.IsTagOrBelow(known, tag)))
            {
                throw invalid($"{where}: no loose item socket has the tag {tag} or a tag below it");
            }
        }

        public void Table(string table, string where)
        {
            // A name may belong to tables in several folders; a pool draws from all of them.
            if (!data.Loot.StaticValueTables.Any(known => string.Equals(known.Name, table, StringComparison.OrdinalIgnoreCase)))
            {
                throw invalid($"{where}: the game data has no loot table {table}");
            }
        }

        // Whether a profile's tag is a loose item socket tag (else a container tag): a tag at least one socket of that kind
        // has (or a tag below it), and no parent of both kinds.
        public bool ProfileTag(string tag, string where)
        {
            if (LooseItemTags.IsLooseItemTag(tag))
            {
                LooseTag(tag, where);
                return true;
            }

            ContainerTag(tag, where);
            return _looseTags.Any(known => LootTags.IsTagOrBelow(known, tag))
                ? throw invalid($"{where}: the tag {tag} covers both containers and loose item sockets")
                : false;
        }

        public void ServedContainerClass(string path, string where)
        {
            if (!LootContainerSupport.IsSupported(path) || data.Loot.FindContainerClass(path) is null)
            {
                throw invalid($"{where}: ARES serves no container class {path}");
            }
        }

        public LootMatch Match(string? dropTag, string? item, string where)
        {
            if ((dropTag is null) == (item is null))
            {
                throw invalid($"{where}: a match must set exactly one of dropTag and item");
            }

            if (dropTag is not null)
            {
                DropTag(dropTag, where);
            }
            else if (item is not null && !data.Items.ByTechnicalName.ContainsKey(item))
            {
                throw invalid($"{where}: the game data has no item {item}");
            }

            return new LootMatch(dropTag, item);
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
