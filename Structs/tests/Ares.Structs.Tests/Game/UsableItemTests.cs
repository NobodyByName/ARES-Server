using Ares.Structs.Backend.Progression;
using Ares.Structs.Game.Inventory;

namespace Ares.Structs.Tests.Game;

public sealed class UsableItemTests
{
    private static readonly int[] s_serverCabinetItems = [707, 519, 704];

    [Fact]
    public void Usable_indices_follow_the_technical_names_ordered_without_case()
    {
        Assert.Equal(898, UsableItems.Count);
        Assert.Equal(897, UsableItems.All.Count);
        Assert.Equal([.. UsableItems.All.Select(item => item.UsableIndex).Order()], UsableItems.All.Select(item => item.UsableIndex));
        Assert.All(UsableItems.All.Zip(UsableItems.All.Skip(1)), pair =>
            Assert.True(string.CompareOrdinal(pair.First.TechnicalName.ToLowerInvariant(), pair.Second.TechnicalName.ToLowerInvariant()) < 0));
        Assert.Equal(InventoryBlackboard.RubberDuckIndex, UsableItems.ByTechnicalName[ItemTechnicalNames.RubberDuck].UsableIndex);
        Assert.Equal(["DA_Item_Salvage_Sensors", "DA_Item_Salvage_FriedMotherboard", "DA_Item_Salvage_ScrapMetal"],
            [.. s_serverCabinetItems.Select(index => UsableItems.All.Single(item => item.UsableIndex == index).TechnicalName)]);
    }

    [Fact]
    public void Every_item_has_one_menu_asset_id_per_quality_level()
    {
        Assert.All(UsableItems.All, item =>
        {
            Assert.Equal(item.QualityAssetIds.Count > 0 ? item.QualityAssetIds[0] : item.BaseAssetId, item.AssetId);
            Assert.InRange(item.MaxStack, 1, int.MaxValue);
            Assert.InRange(item.Weight, 0, 100);
            Assert.All(item.DropTags, tag => Assert.StartsWith("Item.Drop.", tag, StringComparison.Ordinal));
            Assert.Equal(item.Category == ItemCategory.Generator ? "Generator" : "GameItem", item.ItemType);
        });
        Assert.Equal(UsableItems.All.Sum(item => Math.Max(item.QualityAssetIds.Count, 1)), UsableItems.ByAssetId.Count);
        UsableItem ferro = UsableItems.ByTechnicalName[ItemTechnicalNames.FerroI];
        Assert.Equal((WeaponItems.FerroIAssetId, 4, true), (ferro.AssetId, ferro.QualityAssetIds.Count, ferro.HasChildSlots));
        Assert.Same(ferro, UsableItems.ByAssetId[ferro.QualityAssetIds[3]]);
    }

    [Fact]
    public void Only_weapons_have_child_slots()
    {
        Assert.All(UsableItems.All.Where(item => item.HasChildSlots), item => Assert.Equal(ItemCategory.Rifle, item.Category));
        Assert.Equal(88, UsableItems.All.Count(item => item.HasChildSlots));
        Assert.Equal(622, UsableItems.All.Count(item => item.DropTags.Count > 0));
    }

    [Fact]
    public void Items_name_the_container_sockets_they_may_spawn_in()
    {
        Assert.Equal(249, UsableItems.All.Count(item => item.LootPlacements.Count > 0));
        Assert.Equal(324, UsableItems.All.Count(item => item.LootAvoidGroups.Count > 0));
        UsableItem chemicals = UsableItems.ByTechnicalName[ItemTechnicalNames.Chemicals];
        Assert.Equal(new LootPlacement("Socket.Loot", ["Socket.Loot.Nature", "Socket.Loot.Armchair", "Socket.Loot.Sofa"]), chemicals.LootPlacements[0],
            (a, b) => a.SocketTag == b.SocketTag && a.ExcludeTags.SequenceEqual(b.ExcludeTags));
        Assert.Equal(["Item.Drop.Type.Salvage.BasicMaterial"], chemicals.LootAvoidGroups);
        Assert.All(UsableItems.All.SelectMany(item => item.LootPlacements), placement =>
            Assert.True(placement.SocketTag.Length == 0 || placement.SocketTag.StartsWith("Socket.", StringComparison.Ordinal)));
    }

    [Fact]
    public void Rarity_comes_from_the_item_menu_metadata()
    {
        Assert.Equal(ItemRarity.Common, UsableItems.ByTechnicalName[ItemTechnicalNames.MetalParts].Rarity);
        Assert.Equal(ItemRarity.Uncommon, UsableItems.ByTechnicalName[ItemTechnicalNames.Battery].Rarity);
        Assert.Equal(ItemRarity.Legendary, UsableItems.ByTechnicalName[ItemTechnicalNames.SnapHook].Rarity);
        Assert.All(UsableItems.All.Where(item => item.DropTags.Contains("Item.Drop.Rarity.03Rare") && item.Rarity != ItemRarity.Amplified),
            item => Assert.Equal(ItemRarity.Rare, item.Rarity));
    }

    [Fact]
    public void Supported_item_tables_agree_with_the_catalogue()
    {
        ItemDescription[] definitions =
        [
            .. new[] { StorageItems.Definitions, AugmentItems.Definitions, ShieldItems.Definitions, WeaponItems.Definitions, GadgetItems.Definitions,
                    SalvageItems.Definitions, CurrencyItems.Definitions }
                .SelectMany(table => table).Where(definition => definition.ItemType == "GameItem"),
        ];
        Assert.Equal(30, definitions.Length);
        Assert.All(definitions, definition =>
        {
            UsableItem item = UsableItems.ByAssetId[definition.GameAssetId];
            Assert.Equal(((long)item.MaxStack, item.Stackable, item.Unique, item.BaseAssetId, (long)item.QualityLevel, item.HasChildSlots),
                (definition.MaxAmount, definition.Stackable, definition.Unique, definition.BaseGameAssetId, definition.QualityLevel,
                    definition.SlotConfig is not NoItemSlots));
            if (!item.HasChildSlots)
            {
                Assert.Equal(item.DescriptionTags, definition.Tags);
            }
        });
        Assert.All(ItemTechnicalNames.AssetIds, pair => Assert.Equal(pair.Value, UsableItems.ByTechnicalName[pair.Key].AssetId));
        Assert.All(ItemTechnicalNames.DataAssets, pair => Assert.Equal(pair.Value, UsableItems.ByTechnicalName[pair.Key].DataAsset));

        Dictionary<long, (int Index, double Weight)> expected = new()
        {
            [StorageItems.RubberDuckAssetId] = (InventoryBlackboard.RubberDuckIndex, StorageItems.RubberDuckWeight),
            [ShieldItems.LightShieldAssetId] = (ShieldItems.LightShieldUsableIndex, ShieldItems.LightShieldWeight),
            [WeaponItems.FerroIAssetId] = (WeaponItems.FerroIUsableIndex, WeaponItems.FerroIWeight),
            [WeaponItems.HeavyAmmoAssetId] = (WeaponItems.HeavyAmmoUsableIndex, WeaponItems.HeavyAmmoWeight),
            [GadgetItems.SnapHookAssetId] = (GadgetItems.SnapHookUsableIndex, GadgetItems.SnapHookWeight),
        };
        foreach ((long asset, int index) in AugmentItems.UsableIndices.Concat(SalvageItems.UsableIndices))
        {
            expected[asset] = (index, AugmentItems.ItemWeights.GetValueOrDefault(asset, SalvageItems.ItemWeights.GetValueOrDefault(asset)));
        }

        Assert.All(expected, pair => Assert.Equal(pair.Value, (UsableItems.ByAssetId[pair.Key].UsableIndex, UsableItems.ByAssetId[pair.Key].Weight)));
    }
}
