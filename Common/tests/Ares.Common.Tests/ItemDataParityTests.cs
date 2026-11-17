using System.Globalization;
using System.Text.Json.Serialization.Metadata;
using Ares.Common.GameData;
using Ares.Structs.Backend.Progression;
using Ares.Structs.Game.Inventory;
using Ares.Structs.Game.World;

namespace Ares.Common.Tests;

/// <summary>
/// Local data checks: the global item tables that ARES-GameDataExtract writes from the installed game equal the
/// generated Structs data (the usable-item catalogue without its menu quality, item values, the world item list) and
/// reproduce the generated item constants (Ferro, hand items, Light Shield). They run only when
/// <see cref="DamWorldDataParityTests.DirectoryVariable"/> names an extracted game data directory and are skipped
/// otherwise.
/// </summary>
/// <remarks>
/// One deliberate difference: the catalogue has a row for <c>DA_Item_Zipline</c>, which the generated data lists as not
/// cooked (its generator found packages by file name; the extractor finds them by package id). Its usable index is the
/// same either way.
/// </remarks>
public sealed class ItemDataParityTests
{
    /// <summary>The catalogue items the generated data has no entry for.</summary>
    private static readonly string[] s_notGenerated = ["DA_Item_Zipline"];

    [Fact]
    public void Usable_items_equal_the_generated_catalogue_without_quality_levels()
    {
        IReadOnlyList<ItemDefinition> items = Table(GameTables.UsableItems, GameDataJsonContext.Default.ItemDefinition);

        Assert.Equal(UsableItems.Count, items.Count);
        Assert.Equal(Enumerable.Range(0, items.Count), items.Select(item => item.UsableIndex));
        Assert.Equal(UsableItems.All.Select(Text), UsableItems.All.Select(item => Text(items[item.UsableIndex])));
        Assert.Equal(s_notGenerated, items.Select(item => item.TechnicalName).Except(UsableItems.All.Select(item => item.TechnicalName), StringComparer.Ordinal));
    }

    [Fact]
    public void Item_values_equal_the_generated_scrap_values()
    {
        IReadOnlyList<ItemValue> values = Table(GameTables.ItemValues, GameDataJsonContext.Default.ItemValue);

        Assert.Equal(ItemValues.ScrapValues, values.Select(value => KeyValuePair.Create(value.AssetId, value.ScrapValue)));
    }

    [Fact]
    public void World_item_list_equals_the_generated_list()
    {
        Assert.Equal(WorldItemList.Classes, Table(GameTables.WorldItemList, GameDataJsonContext.Default.String));
    }

    [Fact]
    public void Hand_items_follow_from_the_catalogue()
    {
        ItemDefinition unarmed = Item("DA_Item_Unarmed");
        ItemDefinition raiderTool = Item("DA_Item_BasicMeleeWeapon");

        Assert.Equal(
            (HandItems.UnarmedItemClass, HandItems.UnarmedUsableIndex, HandItems.UnarmedEquipSeconds, HandItems.UnarmedUnequipSeconds, HandItems.UnarmedUnequipToItemsSeconds),
            (unarmed.ItemClass, unarmed.UsableIndex, unarmed.EquipSeconds, unarmed.UnequipSeconds, unarmed.UnequipToItemsSeconds));
        Assert.Equal(
            (HandItems.RaiderToolItemClass, HandItems.RaiderToolUsableIndex, HandItems.RaiderToolEquipSeconds, HandItems.RaiderToolUnequipSeconds, HandItems.RaiderToolUnequipToItemsSeconds),
            (raiderTool.ItemClass, raiderTool.UsableIndex, raiderTool.EquipSeconds, raiderTool.UnequipSeconds, raiderTool.UnequipToItemsSeconds));
        Assert.Equal((HandItems.ReEquipFromItemSeconds, HandItems.ReEquipFromItemSeconds), (unarmed.ReEquipFromItemSeconds, raiderTool.ReEquipFromItemSeconds));
        Assert.Equal(
            (HandItems.UnarmedItemInstanceField, HandItems.RaiderToolHandle, HandItems.RaiderToolItemInstanceField),
            (HandItemFields.UnarmedItemInstanceField, HandItemFields.RaiderToolHandle, HandItemFields.RaiderToolItemInstanceField));
    }

    [Fact]
    public void Ferro_defaults_follow_from_the_catalogue_and_its_firearm_class()
    {
        ItemDefinition ferro = Item("DA_Item_BattleRifle_BreachAction_01");
        FirearmDefinition firearm = Assert.Single(Table(GameTables.Firearms, GameDataJsonContext.Default.FirearmDefinition), row => row.ItemClass == ferro.ItemClass);

        Assert.Equal((FerroDefaults.ItemClass, FerroDefaults.FiringMode), (ferro.ItemClass, FerroProtocol.FiringMode));
        Assert.Equal(
            (FerroDefaults.EquipSeconds, FerroDefaults.UnequipSeconds, FerroDefaults.UnequipToItemsSeconds, FerroDefaults.ReEquipFromItemSeconds),
            (ferro.EquipSeconds, ferro.UnequipSeconds, ferro.UnequipToItemsSeconds, ferro.ReEquipFromItemSeconds));
        Assert.Equal(
            ((double)FerroDefaults.ClipSize, FerroDefaults.FireRate, FerroDefaults.ReloadSeconds, FerroDefaults.ZoomSeconds, FerroDefaults.ModSlotCount, FerroDefaults.PartialReloadSeconds),
            (firearm.ClipSize, firearm.FireRate, firearm.BaseReloadSeconds + firearm.ReloadModifierSeconds[0], firearm.ZoomSeconds, firearm.ModSlots.Count, firearm.PartialReloadSeconds));
    }

    [Fact]
    public void Light_shield_defaults_follow_from_its_shield_class()
    {
        ItemDefinition light = Item("DA_Item_Armor_LightBody");
        ShieldDefinition shield = Assert.Single(Table(GameTables.Shields, GameDataJsonContext.Default.ShieldDefinition), row => row.ItemClass == light.ItemClass);

        Assert.Equal(ShieldDefaults.ComponentName, ShieldProtocol.ComponentName);
        Assert.Equal(
            (ShieldDefaults.MaxHealth, ShieldDefaults.HealthPerPlate, ShieldDefaults.ModSlotCount),
            (shield.MaxArmorPlates * shield.ArmorPerPlate, shield.ArmorPerPlate, shield.ModSlotCount));
        Assert.Equal(shield.BaseArmorValue, shield.MaxArmorPlates * shield.ArmorPerPlate);
    }

    /// <summary>A catalogue row of the extracted table, by technical name.</summary>
    internal static ItemDefinition Item(string technicalName) =>
        Assert.Single(Table(GameTables.UsableItems, GameDataJsonContext.Default.ItemDefinition), item => item.TechnicalName == technicalName);

    /// <summary>The rows of an extracted table; skips the test when no game data directory is configured.</summary>
    internal static IReadOnlyList<TRow> Table<TRow>(string name, JsonTypeInfo<TRow> rowType)
    {
        string directory = Environment.GetEnvironmentVariable(DamWorldDataParityTests.DirectoryVariable) ?? string.Empty;
        Assert.SkipWhen(directory.Length == 0,
            $"Local data check: set {DamWorldDataParityTests.DirectoryVariable} to a game data directory written by ARES-GameDataExtract extract to compare it with the generated Structs data.");
        return GameDataDirectory.Open(directory).ReadTable(name, rowType);
    }

    /// <summary>The fields a catalogue row shares with a generated entry (all but the menu quality level), as text.</summary>
    private static string Text(ItemDefinition item) => Text(item.UsableIndex, item.TechnicalName, item.DataAsset, item.AssetId, item.BaseAssetId,
        item.QualityAssetIds, item.Category, item.Rarity, item.MaxStack, item.Weight, item.HasDurability, item.Unique, item.HasChildSlots,
        item.ItemTypeTags, item.DropTags, item.Tags, item.LootPlacements, item.LootAvoidGroups);

    private static string Text(UsableItem item) => Text(item.UsableIndex, item.TechnicalName, item.DataAsset, item.AssetId, item.BaseAssetId,
        item.QualityAssetIds, item.Category, item.Rarity, item.MaxStack, item.Weight, item.HasDurability, item.Unique, item.HasChildSlots,
        item.ItemTypeTags, item.DropTags, item.Tags, item.LootPlacements, item.LootAvoidGroups);

    private static string Text(int index, string name, string dataAsset, long assetId, long baseAssetId, IReadOnlyList<long> qualities,
        ItemCategory category, ItemRarity rarity, int maxStack, double weight, bool durability, bool unique, bool childSlots,
        IReadOnlyList<string> types, IReadOnlyList<string> drops, IReadOnlyList<string> tags, IReadOnlyList<LootPlacement> placements,
        IReadOnlyList<string> avoid)
    {
        string placementText = string.Join(";", placements.Select(placement => placement.SocketTag + "-" + string.Join(",", placement.ExcludeTags)));
        return string.Join(" | ", index, name, dataAsset, assetId, baseAssetId, string.Join(",", qualities), category, rarity, maxStack,
            weight.ToString("R", CultureInfo.InvariantCulture), durability, unique, childSlots, string.Join(",", types), string.Join(",", drops),
            string.Join(",", tags), placementText, string.Join(",", avoid));
    }
}
