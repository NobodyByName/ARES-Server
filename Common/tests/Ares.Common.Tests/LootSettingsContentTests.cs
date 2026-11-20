using System.Globalization;
using System.Text.Json.Nodes;
using Ares.Common.Game.Content;
using Ares.Common.Game.Loot;
using Ares.Common.GameData;
using Ares.Common.Testing;
using Ares.Structs.Game.World;

namespace Ares.Common.Tests;

/// <summary>
/// <c>loot-spawn.json</c> and <c>loot-contents.json</c>: the shipped files resolve against the extracted game data, and
/// an edited copy that names something the game data does not have, or contradicts itself, is refused. The real-data
/// tests are skipped without <see cref="RealGameData.DirectoryVariable"/>.
/// </summary>
public sealed class LootSettingsContentTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "ares-loot-settings-" + Guid.NewGuid().ToString("N"));

    private static GameDataSet Data => RealGameData.Data;

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    [RealDataFact]
    public void The_shipped_loot_settings_resolve_against_the_game_data()
    {
        LootSpawnSettings spawn = ShippedContent.Value.LootSpawn(Data);
        LootContentSettings contents = ShippedContent.Value.LootContents(Data);

        Assert.True(spawn.GroupCaps);
        Assert.True(spawn.Limiters);
        Assert.Equal(0.451, spawn.Chance("Socket.LootContainer.Commercial.GvtDesk.Drawer"));
        Assert.Equal(0.094, spawn.Chance("Socket.LootContainer.Commercial.Centerbench.Drawers"));
        Assert.Equal(0.080, spawn.Chance("Socket.LootCOntainer.Medical.RsrCartShelf.Drawer01"));
        Assert.Equal(0.038, spawn.Chance("Socket.LootContainer.ARC.Courier"));
        Assert.Equal(1, spawn.Multiplier(PointOfInterestLootValue.Unknown));
        Assert.Equal(1, spawn.Multiplier(PointOfInterestLootValue.Low));
        Assert.Equal(["Common", "Uncommon", "Rare", "Epic", "Recyclables", "Trinkets", "Other"], contents.Tiers.Select(tier => tier.Name));
        Assert.Equal("Common", contents.FirstStackTier);
        Assert.NotEmpty(contents.TablePools);
        Assert.NotEmpty(contents.TypePools);
        Assert.Equal(0.082, spawn.LooseChance("Socket.Loot.Console"));
        Assert.Equal(0.004, spawn.LooseChance("Socket.Loot.Nature.Pine"));
        Assert.Equal(0, spawn.LooseChance("Socket.LootContainer.Tech"));
        Assert.Equal(new LootRange(1, 2), contents.LooseItems.Amount);
        Assert.Equal(["Supplies", "Medical", "Tables"], contents.LooseItems.Pools.Select(pool => pool.Name));
    }

    [RealDataFact]
    public void The_shipped_defaults_for_what_the_game_data_leaves_unset_are_the_official_ones()
    {
        LootSpawnSettings spawn = ShippedContent.Value.LootSpawn(Data);

        Assert.Equal(2, spawn.MaxPerGroup);
        Assert.Equal(new LootLimiterDefaults(LootLimiterShape.Sphere, 1000, true, 0), spawn.LimiterDefaults);
        Assert.Equal(1, spawn.SpawnerCount);
        Assert.Equal([new LootTargetStacks(2, 2), new LootTargetStacks(3, 3)], ShippedContent.Value.LootContents(Data).TargetStacks);
    }

    [RealDataFact]
    public void The_shipped_spawner_classes_are_classes_the_dam_spawners_draw()
    {
        HashSet<string> drawn = [.. Data.Names.Dam.DynamicLootSpawners.SelectMany(spawner => spawner.Classes).Select(entry => entry.Class)];
        IReadOnlySet<string> enabled = ShippedContent.Value.LootSpawn(Data).SpawnerClasses;

        Assert.NotEmpty(enabled);
        Assert.All(enabled, path => Assert.Contains(path, drawn));
    }

    [RealDataTheory]
    [InlineData(AresContent.LootSpawnFile, "chances/Socket.Nope", "0.1", "chances: no container has the tag Socket.Nope or a tag below it")]
    [InlineData(AresContent.LootSpawnFile, "chances/Socket.LootContainer", "1.5", "the chance of Socket.LootContainer must be from 0 to 1")]
    [InlineData(AresContent.LootSpawnFile, "chances/Socket.LootContainer", "-0.1", "the chance of Socket.LootContainer must be from 0 to 1")]
    [InlineData(AresContent.LootSpawnFile, "chances/socket.lootcontainer", "0.5", "chances lists socket.lootcontainer twice")]
    [InlineData(AresContent.LootSpawnFile, "areaMultipliers/Huge", "2", "areaMultipliers: Huge is not a loot value")]
    [InlineData(AresContent.LootSpawnFile, "areaMultipliers/high", "2", "areaMultipliers: high is not a loot value")]
    [InlineData(AresContent.LootSpawnFile, "areaMultipliers/High", "-1", "areaMultipliers: the factor of High must be 0 or more")]
    [InlineData(AresContent.LootSpawnFile, "spawnerClasses[0]", "\"/Game/Nope.Nope_C\"", "spawnerClasses: the game data has no container class /Game/Nope.Nope_C")]
    [InlineData(AresContent.LootSpawnFile, "spawnerClasses[0]", "null", "spawnerClasses must not hold null")]
    [InlineData(AresContent.LootContentsFile, "excludedDropTags[0]", "\"Item.Drop.Nope\"", "excludedDropTags: no item has the drop tag Item.Drop.Nope or a tag below it")]
    [InlineData(AresContent.LootContentsFile, "excludedDropTags[0]", "null", "excludedDropTags must not hold null")]
    [InlineData(AresContent.LootContentsFile, "tiers[0]", "null", "tiers must not hold null")]
    [InlineData(AresContent.LootContentsFile, "tiers[1]/name", "\"Common\"", "tiers lists Common twice")]
    [InlineData(AresContent.LootContentsFile, "tiers[0]/dropTag", "\"Item.Drop.Nope\"", "tier Common: no item has the drop tag Item.Drop.Nope or a tag below it")]
    [InlineData(AresContent.LootContentsFile, "tiers[0]/weight", "-1", "the weight of tier Common must be 0 or more")]
    [InlineData(AresContent.LootContentsFile, "tiers[2]/areaMultipliers/Huge", "2", "tier Rare: Huge is not a loot value")]
    [InlineData(AresContent.LootContentsFile, "tiers[2]/areaMultipliers/High", "-1", "tier Rare: the factor of High must be 0 or more")]
    [InlineData(AresContent.LootContentsFile, "tiers[6]/dropTag", "\"Item.Drop.Type.Ammo\"", "tiers must hold exactly one tier without a dropTag")]
    [InlineData(AresContent.LootContentsFile, "tiers[0]/dropTag", "null", "tiers must hold exactly one tier without a dropTag")]
    [InlineData(AresContent.LootContentsFile, "firstStackTier", "\"Legendary\"", "firstStackTier Legendary is not a tier")]
    [InlineData(AresContent.LootContentsFile, "tablePools[0]/tag", "\"Socket.Nope\"", "table pool Tech: no container has the tag Socket.Nope or a tag below it")]
    [InlineData(AresContent.LootContentsFile, "tablePools[1]/tag", "\"socket.lootcontainer.tech\"", "two table pools have the tag socket.lootcontainer.tech")]
    [InlineData(AresContent.LootContentsFile, "tablePools[0]/tables[0]", "\"StaticValueLootTable_Nope\"", "table pool Tech: the game data has no loot table StaticValueLootTable_Nope")]
    [InlineData(AresContent.LootContentsFile, "tablePools[0]/tables[0]", "null", "table pool Tech must not hold null")]
    [InlineData(AresContent.LootContentsFile, "tablePools[0]/stacks", """{ "min": 0, "max": 1 }""", "table pool Tech stacks must have 1 <= min <= max")]
    [InlineData(AresContent.LootContentsFile, "tablePools[0]/stacks", """{ "min": 3, "max": 2 }""", "table pool Tech stacks must have 1 <= min <= max")]
    [InlineData(AresContent.LootContentsFile, "extraStack/chance", "1.5", "the extra stack's chance must be from 0 to 1")]
    [InlineData(AresContent.LootContentsFile, "extraStack/chance", "-0.5", "the extra stack's chance must be from 0 to 1")]
    [InlineData(AresContent.LootContentsFile, "extraStack/entries[0]", "null", "the extra stack must not hold null")]
    [InlineData(AresContent.LootContentsFile, "extraStack/entries[0]/dropTag", "\"Item.Drop.Nope\"", "the extra stack: no item has the drop tag Item.Drop.Nope or a tag below it")]
    [InlineData(AresContent.LootContentsFile, "extraStack/entries[0]/amount", """{ "min": 5, "max": 4 }""", "the extra stack: the amount of Item.Drop.Type.Ammo must have 1 <= min <= max")]
    [InlineData(AresContent.LootContentsFile, "extraStack/entries[0]/amount", """{ "min": 0, "max": 4 }""", "the extra stack: the amount of Item.Drop.Type.Ammo must have 1 <= min <= max")]
    [InlineData(AresContent.LootContentsFile, "typePools[0]", "null", "typePools must not hold null")]
    [InlineData(AresContent.LootContentsFile, "typePools[0]/classes[0]", "\"/Game/Nope.Nope_C\"", "type pool RaiderAmmo: the game data has no container class /Game/Nope.Nope_C")]
    [InlineData(AresContent.LootContentsFile, "typePools[0]/classes[0]", "null", "type pool RaiderAmmo must not hold null")]
    [InlineData(AresContent.LootContentsFile, "typePools[4]/tags[0]", "\"Socket.Nope\"", "type pool RaiderWeapons: no container has the tag Socket.Nope or a tag below it")]
    [InlineData(AresContent.LootContentsFile, "typePools[0]/stacks", """{ "min": 0, "max": 3 }""", "type pool RaiderAmmo stacks must have 1 <= min <= max")]
    [InlineData(AresContent.LootContentsFile, "typePools[0]/entries[0]/weight", "-1", "type pool RaiderAmmo: the weight of Item.Drop.Type.Ammo must be 0 or more")]
    [InlineData(AresContent.LootContentsFile, "typePools[3]/entries[0]/excludedDropTags[0]", "\"Item.Drop.Nope\"", "type pool RaiderWeaponsHigh: no item has the drop tag Item.Drop.Nope or a tag below it")]
    [InlineData(AresContent.LootContentsFile, "typePools[3]/entries[0]/excludedDropTags[0]", "null", "type pool RaiderWeaponsHigh must not hold null")]
    public void An_edit_the_game_data_or_the_file_itself_contradicts_is_refused_naming_the_problem(string file, string path, string value, string expected)
    {
        AresContent content = Load(file, path, value);

        AresContentException error = Assert.Throws<AresContentException>(() =>
        {
            _ = content.LootSpawn(Data);
            _ = content.LootContents(Data);
        });
        Assert.StartsWith(Path.Combine(_directory, file) + ": ", error.Message, StringComparison.Ordinal);
        Assert.Contains(expected, error.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(AresContent.LootSpawnFile, "extra", "1")]
    [InlineData(AresContent.LootSpawnFile, "groupCaps", "null")]
    [InlineData(AresContent.LootSpawnFile, "chances/Socket.LootContainer", "\"0.5\"")]
    [InlineData(AresContent.LootContentsFile, "extra", "1")]
    [InlineData(AresContent.LootContentsFile, "tiers", "null")]
    [InlineData(AresContent.LootContentsFile, "tiers[0]/chance", "1")]
    [InlineData(AresContent.LootContentsFile, "tablePools[0]/stacks", """{ "min": 1, "max": 2, "mean": 1 }""")]
    [InlineData(AresContent.LootContentsFile, "extraStack/entries[0]/count", "1")]
    [InlineData(AresContent.LootContentsFile, "typePools[0]/stacks", """{ "min": 1 }""")]
    [InlineData(AresContent.LootSpawnFile, "looseChances", "null")]
    [InlineData(AresContent.LootContentsFile, "looseItems/pools[0]/weight", "1")]
    public void A_file_with_an_unknown_missing_or_mistyped_member_is_refused_when_loaded(string file, string path, string value)
    {
        Write(file, path, value);

        Assert.Contains($"{file} is not valid content", Assert.Throws<AresContentException>(() => AresContent.Load(_directory)).Message, StringComparison.Ordinal);
    }

    private AresContent Load(string file, string path, string value)
    {
        Write(file, path, value);
        return AresContent.Load(_directory);
    }

    // Sets one member of a file in a copy of the shipped content. The path's steps are separated by '/' (tags hold
    // dots); a step ending in [n] picks the n-th entry of a list.
    private void Write(string file, string path, string value)
    {
        ShippedContent.CopyTo(_directory);
        string target = Path.Combine(_directory, file);
        JsonNode root = JsonNode.Parse(File.ReadAllText(target))!;
        string[] steps = path.Split('/');
        JsonNode parent = steps[..^1].Aggregate(root, Step);

        JsonNode? node = JsonNode.Parse(value);
        string last = steps[^1];
        if (Index(last) is ({ } name, { } index))
        {
            parent[name]![index] = node;
        }
        else
        {
            parent[last] = node;
        }

        File.WriteAllText(target, root.ToJsonString());
    }

    private static JsonNode Step(JsonNode node, string step) => Index(step) is ({ } name, { } index) ? node[name]![index]! : node[step]!;

    // The member name and list index of a step "name[n]", or nulls for a plain member.
    private static (string? Name, int? Index) Index(string step)
    {
        int open = step.IndexOf('[', StringComparison.Ordinal);
        return open < 0 ? (null, null) : (step[..open], int.Parse(step[(open + 1)..^1], CultureInfo.InvariantCulture));
    }
}
