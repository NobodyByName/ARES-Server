using Ares.GameDataExtract.Items;
using Ares.GameDataExtract.Packages;
using Ares.GameDataExtract.Properties;
using Ares.GameDataExtract.Registry;
using Ares.GameDataExtract.Scripts;
using Ares.Structs.Core.Assets;
using Ares.Structs.Game.Inventory;

namespace Ares.GameDataExtract.Tests;

public sealed class ItemReaderTests
{
    [Fact]
    public void Tag_queries_match_tags_and_tags_under_them()
    {
        // ANY( Inventory.ItemType.Armor.Medium, Inventory.ItemType.Armor.Light )
        GameplayTagQuery any = Query(["Inventory.ItemType.Armor.Medium", "Inventory.ItemType.Armor.Light"], [0, 1, 1, 2, 0, 1]);

        Assert.True(any.Matches(["Inventory.ItemType.Armor.Light"]));
        Assert.True(any.Matches(["Inventory.ItemType.Armor.Medium.Heavy"]));
        Assert.False(any.Matches(["Inventory.ItemType.Armor"]));
        Assert.False(any.Matches(["Inventory.ItemType.Armor.LightBody"]));
        Assert.Equal(1, any.FirstTagHeld(["Other", "Inventory.ItemType.Armor.Light"]));
        Assert.Equal(int.MaxValue, any.FirstTagHeld(["Other"]));
    }

    [Fact]
    public void Tag_query_expressions_combine_and_an_empty_query_matches_nothing()
    {
        // ALL( ANY_EXACT( A ), NONE( B ) )
        GameplayTagQuery query = Query(["A", "B"], [0, 1, 5, 2, 7, 1, 0, 3, 1, 1]);

        Assert.True(query.Matches(["A", "C"]));
        Assert.False(query.Matches(["A.Sub"]));
        Assert.False(query.Matches(["A", "B.Sub"]));
        Assert.False(Query([], [.. "\0\0"u8]).Matches(["A"]));
        Assert.False(Query([], []).Matches(["A"]));
        Assert.Throws<InvalidDataException>(() => Query(["A"], [0, 1, 1, 1, 4]).Matches(["A"]));
        Assert.Throws<InvalidDataException>(() => Query(["A"], [0, 1, 1, 2, 0]).Matches(["A"]));
        Assert.Throws<InvalidDataException>(() => Query(["A"], [0, 1, 9, 0]).Matches(["A"]));
    }

    [Fact]
    public void A_member_the_block_does_not_hold_takes_the_class_default()
    {
        ItemSources sources = Sources();
        PropertyBag stored = new(EngineStructs.GameplayTag, new Dictionary<string, object> { ["EquipTime"] = 1.25, ["BaseClipSize"] = 30f });
        PropertyBag empty = new(EngineStructs.GameplayTag, []);

        Assert.Equal(1.25, sources.Number(stored, "UItemDataAsset", "EquipTime", ItemDataRules.DefaultEquipSeconds));
        Assert.Equal(ItemDataRules.DefaultEquipSeconds, sources.Number(empty, "UItemDataAsset", "EquipTime", ItemDataRules.DefaultEquipSeconds));
        Assert.Equal(30.0, sources.Number(stored, "UFirearmItem", "BaseClipSize", WeaponItemDefaults.ClipSize));
        Assert.Equal(WeaponItemDefaults.ClipSize, sources.Number(empty, "UFirearmItem", "BaseClipSize", WeaponItemDefaults.ClipSize));
    }

    [Fact]
    public void A_member_the_class_chain_lacks_or_without_a_known_default_does_not_read()
    {
        ItemSources sources = Sources();
        PropertyBag empty = new(EngineStructs.GameplayTag, []);

        Assert.Contains("has no member NoSuchMember",
            Assert.Throws<InvalidDataException>(() => sources.Number(empty, "UItemDataAsset", "NoSuchMember", 1.0)).Message, StringComparison.Ordinal);
        Assert.Contains("is not set",
            Assert.Throws<InvalidDataException>(() => sources.Number(empty, "UItemDataAsset", "EquipTime")).Message, StringComparison.Ordinal);
    }

    /// <summary>Item sources over the current script layouts, without packages or registry assets.</summary>
    private static ItemSources Sources() =>
        new(new PackageCache(_ => null, _ => null), AssetRegistry.Parse(SyntheticLootScripts.EmptyRegistry()), ScriptLayouts.Current);

    private static GameplayTagQuery Query(string[] tags, byte[] tokens)
    {
        object[] dictionary = [.. tags.Select(tag => new PropertyBag(EngineStructs.GameplayTag, new Dictionary<string, object> { ["TagName"] = tag }))];
        return GameplayTagQuery.Of(new PropertyBag(EngineStructs.GameplayTagQuery, new Dictionary<string, object>
        {
            ["TagDictionary"] = dictionary,
            ["QueryTokenStream"] = tokens.Cast<object>().ToArray(),
        }));
    }
}
