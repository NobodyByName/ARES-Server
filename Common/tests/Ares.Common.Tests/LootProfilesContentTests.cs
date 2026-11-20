using Ares.Common.Game.Content;
using Ares.Common.Game.Loot;
using Ares.Common.GameData;
using Ares.Common.Testing;
using Ares.Structs.Game.Loot;
using Ares.Structs.Game.World;

namespace Ares.Common.Tests;

/// <summary>
/// The <c>profiles</c> of <c>loot-contents.json</c>, on made-up game data: a profile reads into its settings, and one that
/// names something the game data does not have, or contradicts itself, is refused naming the problem.
/// </summary>
public sealed class LootProfilesContentTests : IDisposable
{
    private const string Salvage = "Item.Drop.Type.Salvage";
    private const string Fabric = "DA_Item_Synth_Fabric";
    private const string Container = $"\"tag\": \"{SyntheticGameData.ContainerTag}\"";
    private const string Loose = $"\"tag\": \"{SyntheticGameData.LooseItemTag}\"";

    private static readonly string s_unsupportedClass = LootContainerSupport.Unsupported.Keys.First();

    private readonly string _directory = Path.Combine(Path.GetTempPath(), "ares-loot-profiles-" + Guid.NewGuid().ToString("N"));

    private readonly GameDataSet _data = new SyntheticGameData()
        .AddItem(Fabric, item => item with { DropTags = [$"{Salvage}.Fabric"] })
        .AddItem("DA_Item_Synth_Gem", item => item with { DropTags = ["Item.Drop.Type.Valuable"] })
        .With(content => content with
        {
            LootContainerClasses =
            [
                .. content.LootContainerClasses,
                content.LootContainerClasses[0] with { Container = new ArchetypeClass(s_unsupportedClass, s_unsupportedClass + "_Default") },
            ],
        })
        .Build();

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    [Fact]
    public void A_profile_reads_into_its_settings_and_absent_members_inherit()
    {
        IReadOnlyList<LootProfile> profiles = Profiles($$"""
            {
              "name": "Shelves", {{Container}},
              "only": [ { "dropTag": "{{Salvage}}" }, { "item": "DA_Item_Synth_Gem" } ],
              "weights": [ { "dropTag": "{{Salvage}}.Fabric", "multiplier": 0 }, { "item": "DA_Item_Synth_Gem", "multiplier": 2.5 } ],
              "add": [ { "item": "DA_Item_Synth_Gem", "amount": { "min": 1, "max": 3 }, "tier": "Other" }, { "dropTag": "{{Salvage}}", "amount": { "min": 2, "max": 2 } } ],
              "tiers": { "Common": 0.5 },
              "firstStackTier": null,
              "stacks": { "min": 0, "max": 3 },
              "extraStack": { "chance": 0.3 }
            },
            { "name": "Boxes", "class": "{{SyntheticGameData.ContainerClass}}", "firstStackTier": "Common", "extraStack": { "entries": [ { "dropTag": "{{Salvage}}", "amount": { "min": 1, "max": 1 } } ] } },
            { "name": "Tables", {{Loose}}, "weights": [ { "item": "{{Fabric}}", "multiplier": 3 } ] },
            { "name": "Extra", "tag": "Socket.LootContainer", "extraStack": { } }
            """);

        LootProfile shelves = profiles[0];
        Assert.Equal(("Shelves", SyntheticGameData.ContainerTag, (string?)null), (shelves.Name, shelves.Tag, shelves.Class));
        Assert.Equal([new LootMatch(Salvage, null), new LootMatch(null, "DA_Item_Synth_Gem")], shelves.Only!);
        Assert.Equal([new LootProfileWeight(new LootMatch($"{Salvage}.Fabric", null), 0), new LootProfileWeight(new LootMatch(null, "DA_Item_Synth_Gem"), 2.5)], shelves.Weights);
        Assert.Equal([new LootProfileAdd(new LootMatch(null, "DA_Item_Synth_Gem"), new LootRange(1, 3), "Other"), new LootProfileAdd(new LootMatch(Salvage, null), new LootRange(2, 2), null)],
            shelves.Add);
        Assert.Equal(0.5, shelves.Tiers["Common"]);
        Assert.Equal(new LootFirstStackTier(null), shelves.FirstStackTier);
        Assert.Equal(new LootRange(0, 3), shelves.Stacks);
        Assert.Equal(0.3, shelves.ExtraStack!.Chance);
        Assert.Null(shelves.ExtraStack.Entries);

        LootProfile boxes = profiles[1];
        Assert.Equal((null, SyntheticGameData.ContainerClass), (boxes.Tag, boxes.Class));
        Assert.Equal(new LootFirstStackTier("Common"), boxes.FirstStackTier);
        Assert.Null(boxes.ExtraStack!.Chance);
        Assert.Equal(Salvage, Assert.Single(boxes.ExtraStack.Entries!).DropTag);

        // Absent members inherit: no only list, no first stack tier, stacks or extra stack.
        LootProfile tables = profiles[2];
        Assert.Equal((null, null, null, null), (tables.Only, tables.FirstStackTier, tables.Stacks, tables.ExtraStack));
        Assert.Empty(tables.Add);
        Assert.Empty(tables.Tiers);

        // An extra stack with no part still sets it.
        Assert.Equal(new LootProfileExtraStack(null, null), profiles[3].ExtraStack);
    }

    [Theory]
    [InlineData("null", "profiles must not hold null")]
    [InlineData($$"""{ "name": "A", {{Container}} }, { "name": "A", {{Loose}} }""", "profiles list A twice")]
    [InlineData("""{ "name": "A" }""", "profile A must set exactly one of tag and class")]
    [InlineData($$"""{ "name": "A", {{Container}}, "class": "{{SyntheticGameData.ContainerClass}}" }""", "profile A must set exactly one of tag and class")]
    [InlineData($$"""{ "name": "A", {{Container}} }, { "name": "B", "tag": "socket.lootCONTAINER.synthetic" }""", "two profiles have the tag socket.lootCONTAINER.synthetic")]
    [InlineData($$"""{ "name": "A", "class": "{{SyntheticGameData.ContainerClass}}" }, { "name": "B", "class": "{{SyntheticGameData.ContainerClass}}" }""", "two profiles have the class")]
    [InlineData("""{ "name": "A", "tag": "Socket.LootContainer.Nope" }""", "profile A: no container has the tag Socket.LootContainer.Nope or a tag below it")]
    [InlineData("""{ "name": "A", "tag": "Socket.Loot.Nope" }""", "profile A: no loose item socket has the tag Socket.Loot.Nope or a tag below it")]
    [InlineData("""{ "name": "A", "tag": "Socket" }""", "profile A: the tag Socket covers both containers and loose item sockets")]
    [InlineData("""{ "name": "A", "class": "/Game/Nope.Nope_C" }""", "profile A: ARES serves no container class /Game/Nope.Nope_C")]
    [InlineData($$"""{ "name": "A", {{Container}}, "only": [ null ] }""", "profile A only must not hold null")]
    [InlineData($$"""{ "name": "A", {{Container}}, "only": [ { } ] }""", "profile A: a match must set exactly one of dropTag and item")]
    [InlineData($$"""{ "name": "A", {{Container}}, "only": [ { "dropTag": "{{Salvage}}", "item": "{{Fabric}}" } ] }""", "profile A: a match must set exactly one of dropTag and item")]
    [InlineData($$"""{ "name": "A", {{Container}}, "only": [ { "item": "DA_Item_Nope" } ] }""", "profile A: the game data has no item DA_Item_Nope")]
    [InlineData($$"""{ "name": "A", {{Container}}, "only": [ { "dropTag": "Item.Drop.Nope" } ] }""", "profile A: no item has the drop tag Item.Drop.Nope or a tag below it")]
    [InlineData($$"""{ "name": "A", {{Container}}, "weights": [ { "dropTag": "{{Salvage}}", "multiplier": -1 } ] }""", "profile A: the multiplier of Item.Drop.Type.Salvage must be 0 or more")]
    [InlineData($$"""{ "name": "A", {{Container}}, "weights": [ { "dropTag": "{{Salvage}}", "multiplier": 1 }, { "dropTag": "item.drop.type.salvage", "multiplier": 2 } ] }""", "profile A: weights list item.drop.type.salvage twice")]
    [InlineData($$"""{ "name": "A", {{Container}}, "add": [ { "item": "{{Fabric}}", "amount": { "min": 0, "max": 1 } } ] }""", "profile A: the amount of DA_Item_Synth_Fabric must have 1 <= min <= max")]
    [InlineData($$"""{ "name": "A", {{Container}}, "add": [ { "item": "{{Fabric}}", "amount": { "min": 2, "max": 1 } } ] }""", "profile A: the amount of DA_Item_Synth_Fabric must have 1 <= min <= max")]
    [InlineData($$"""{ "name": "A", {{Container}}, "add": [ { "item": "{{Fabric}}", "amount": { "min": 1, "max": 1 }, "tier": "Legendary" } ] }""", "profile A: Legendary is not a tier")]
    [InlineData($$"""{ "name": "A", {{Container}}, "add": [ { "item": "{{Fabric}}", "amount": { "min": 1, "max": 1 } }, { "item": "{{Fabric}}", "amount": { "min": 2, "max": 2 } } ] }""", "profile A: add lists DA_Item_Synth_Fabric twice")]
    [InlineData($$"""{ "name": "A", {{Container}}, "tiers": { "Legendary": 1 } }""", "profile A: Legendary is not a tier")]
    [InlineData($$"""{ "name": "A", {{Container}}, "tiers": { "Common": -1 } }""", "profile A: the multiplier of tier Common must be 0 or more")]
    [InlineData($$"""{ "name": "A", {{Container}}, "firstStackTier": "Legendary" }""", "profile A: Legendary is not a tier")]
    [InlineData($$"""{ "name": "A", {{Container}}, "firstStackTier": 3 }""", "profile A: firstStackTier must be a tier name or null")]
    [InlineData($$"""{ "name": "A", {{Container}}, "stacks": { "min": -1, "max": 1 } }""", "profile A stacks must have 0 <= min <= max")]
    [InlineData($$"""{ "name": "A", {{Container}}, "stacks": { "min": 3, "max": 2 } }""", "profile A stacks must have 0 <= min <= max")]
    [InlineData($$"""{ "name": "A", {{Container}}, "extraStack": { "chance": 1.5 } }""", "profile A: the extra stack's chance must be from 0 to 1")]
    [InlineData($$"""{ "name": "A", {{Container}}, "extraStack": { "entries": [ { "dropTag": "Item.Drop.Nope", "amount": { "min": 1, "max": 1 } } ] } }""", "profile A extra stack: no item has the drop tag Item.Drop.Nope")]
    [InlineData($$"""{ "name": "A", {{Loose}}, "firstStackTier": null }""", "profile A: a loose item socket profile cannot set firstStackTier, stacks or extraStack")]
    [InlineData($$"""{ "name": "A", {{Loose}}, "stacks": { "min": 1, "max": 1 } }""", "profile A: a loose item socket profile cannot set firstStackTier, stacks or extraStack")]
    [InlineData($$"""{ "name": "A", {{Loose}}, "extraStack": { "chance": 1 } }""", "profile A: a loose item socket profile cannot set firstStackTier, stacks or extraStack")]
    public void A_profile_the_game_data_or_the_file_itself_contradicts_is_refused_naming_the_problem(string profiles, string expected)
    {
        AresContentException error = Assert.Throws<AresContentException>(() => Profiles(profiles));

        Assert.StartsWith(Path.Combine(_directory, AresContent.LootContentsFile) + ": ", error.Message, StringComparison.Ordinal);
        Assert.Contains(expected, error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_profile_for_a_class_ares_does_not_serve_is_refused()
    {
        Assert.NotNull(_data.Loot.FindContainerClass(s_unsupportedClass));

        AresContentException error = Assert.Throws<AresContentException>(() => Profiles($$"""{ "name": "A", "class": "{{s_unsupportedClass}}" }"""));
        Assert.Contains($"profile A: ARES serves no container class {s_unsupportedClass}", error.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData($$"""{ "name": "A", {{Container}}, "bogus": 1 }""")]
    [InlineData($$"""{ "name": "A", {{Container}}, "weights": [ { "dropTag": "{{Salvage}}" } ] }""")]
    [InlineData($$"""{ "name": "A", {{Container}}, "add": [ { "dropTag": "{{Salvage}}" } ] }""")]
    [InlineData($$"""{ "name": "A", {{Container}}, "extraStack": { "chance": 1, "count": 1 } }""")]
    [InlineData($$"""{ {{Container}} }""")]
    public void A_profile_with_an_unknown_missing_or_mistyped_member_is_refused_when_read(string profiles)
    {
        Assert.Contains("is not valid content", Assert.Throws<AresContentException>(() => Profiles(profiles)).Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_file_without_profiles_has_none()
    {
        Assert.Empty(Read(null).Profiles);
    }

    // The profiles of a loot-contents.json made for the made-up data around them.
    private IReadOnlyList<LootProfile> Profiles(string profiles) => Read(profiles).Profiles;

    private LootContentSettings Read(string? profiles)
    {
        Directory.CreateDirectory(_directory);
        string member = profiles is null ? string.Empty : $"\"profiles\": [ {profiles} ],";
        File.WriteAllText(Path.Combine(_directory, AresContent.LootContentsFile), $$"""
            {
              "excludedDropTags": [],
              "tiers": [ { "name": "Common", "weight": 1, "dropTag": "{{Salvage}}" }, { "name": "Other", "weight": 1 } ],
              "firstStackTier": "Common",
              "targetStacks": [ { "target": 2, "weight": 1 } ],
              "tablePools": [],
              "extraStack": { "chance": 0, "entries": [] },
              "typePools": [],
              {{member}}
              "looseItems": { "tables": [], "amount": { "min": 1, "max": 1 }, "pools": [] }
            }
            """);
        LootContentsDocument document = AresContentReader.Read<LootContentsDocument>(_directory, AresContent.LootContentsFile);
        return LootSettingsContent.Contents(document, _data, problem => AresContentReader.Invalid(_directory, AresContent.LootContentsFile, problem));
    }
}
