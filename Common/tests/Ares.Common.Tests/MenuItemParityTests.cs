using System.Text.Json;
using Ares.Common.GameData;
using Ares.Structs.Backend.Progression;
using Ares.Structs.Game.Inventory;
using static Ares.Common.Tests.ItemDataParityTests;

namespace Ares.Common.Tests;

/// <summary>
/// Local data checks: the menu item tables that ARES-GameDataExtract writes from the installed game equal the generated
/// Structs definitions row for row and in the same order, and the generated item constants (asset ids, weights, usable
/// indices, augment frames and carrying limits) follow from the extracted tables. Skipped without
/// <see cref="DamWorldDataParityTests.DirectoryVariable"/>, like <see cref="ItemDataParityTests"/>.
/// </summary>
public sealed class MenuItemParityTests
{
    private const string StandardFrame = "DA_Inventory_LF_Standard";

    [Fact]
    public void Menu_tables_equal_the_generated_definitions()
    {
        Assert.Equal(Json(CurrencyItems.Definitions), Json(Menu(GameTables.MenuCurrency)));
        Assert.Equal(Json(GadgetItems.Definitions), Json(Menu(GameTables.MenuGadgets)));
        Assert.Equal(Json(SalvageItems.Definitions), Json(Menu(GameTables.MenuSalvage)));
        Assert.Equal(Json(ShieldItems.Definitions), Json(Menu(GameTables.MenuShields)));
        Assert.Equal(Json(WeaponItems.Definitions), Json(Menu(GameTables.MenuWeapons)));
        Assert.Equal(Json(AugmentItems.Definitions), Json(Menu(GameTables.MenuAugments)));
    }

    [Fact]
    public void Currency_gadget_and_shield_constants_follow_from_the_catalogue()
    {
        ItemDefinition coins = Item("DA_Item_GrindCurrency");
        ItemDefinition snapHook = Item("DA_Item_SnapHook");
        ItemDefinition lightShield = Item("DA_Item_Armor_LightBody");

        Assert.Equal(CurrencyItems.CoinsAssetId, coins.AssetId);
        Assert.Equal(CurrencyItems.ItemAssetIds, new Dictionary<string, long> { [coins.TechnicalName] = coins.AssetId });
        Assert.Equal(
            (GadgetItems.SnapHookAssetId, GadgetItems.SnapHookWeight, GadgetItems.SnapHookUsableIndex, GadgetItems.SnapHookEquipSeconds, GadgetItems.SnapHookUnequipSeconds, GadgetItems.SnapHookItemClass),
            (snapHook.AssetId, snapHook.Weight, snapHook.UsableIndex, snapHook.EquipSeconds, snapHook.UnequipSeconds, snapHook.ItemClass));
        Assert.Equal((ShieldItems.LightShieldAssetId, ShieldItems.LightShieldWeight, ShieldItems.LightShieldUsableIndex), (lightShield.AssetId, lightShield.Weight, lightShield.UsableIndex));
    }

    [Fact]
    public void Weapon_constants_follow_from_the_catalogue_and_the_menu_table()
    {
        ItemDefinition ferro = Item("DA_Item_BattleRifle_BreachAction_01");
        ItemDefinition ammo = Item("DA_Item_HeavyAmmo");
        Dictionary<string, long> structures = Menu(GameTables.MenuWeapons).ToDictionary(item => item.TechnicalName, item => item.Description.GameAssetId, StringComparer.Ordinal);

        Assert.Equal(
            (WeaponItems.FerroIAssetId, WeaponItems.FerroIWeight, WeaponItems.FerroIUsableIndex, WeaponItems.HeavyAmmoAssetId, WeaponItems.HeavyAmmoWeight, WeaponItems.HeavyAmmoUsableIndex),
            (ferro.AssetId, ferro.Weight, ferro.UsableIndex, ammo.AssetId, ammo.Weight, ammo.UsableIndex));
        Assert.Equal(
            (WeaponItems.MuzzleModSlotAssetId, WeaponItems.UnderBarrelModSlotAssetId, WeaponItems.StockModSlotAssetId),
            (structures["DA_ModSlot_Firearm_Muzzle"], structures["DA_ModSlot_Firearm_UnderBarrel"], structures["DA_ModSlot_Firearm_Stock"]));
    }

    [Fact]
    public void Salvage_constants_follow_from_the_catalogue()
    {
        ItemDefinition[] salvage = [.. SalvageItems.ItemAssetIds.Keys.Select(Item)];

        Assert.Equal(SalvageItems.ItemAssetIds, salvage.ToDictionary(item => item.TechnicalName, item => item.AssetId));
        Assert.Equal(SalvageItems.UsableIndices, salvage.ToDictionary(item => item.AssetId, item => item.UsableIndex));
        Assert.Equal(SalvageItems.ItemWeights, salvage.ToDictionary(item => item.AssetId, item => item.Weight));
    }

    [Fact]
    public void Augment_constants_follow_from_the_augment_frame_and_menu_tables()
    {
        ItemDefinition[] augments = [.. AugmentItems.ItemAssetIds.Keys.Select(Item)];
        Dictionary<string, AugmentDefinition> definitions = Table(GameTables.Augments, GameDataJsonContext.Default.AugmentDefinition).ToDictionary(row => row.TechnicalName, StringComparer.Ordinal);
        Dictionary<long, LoadoutFrameDefinition> frames = Table(GameTables.LoadoutFrames, GameDataJsonContext.Default.LoadoutFrameDefinition).ToDictionary(frame => frame.AssetId);
        Dictionary<string, long> structures = Menu(GameTables.MenuAugments).ToDictionary(item => item.TechnicalName, item => item.Description.GameAssetId, StringComparer.Ordinal);
        Dictionary<int, long> standard = frames.Values.Single(frame => frame.TechnicalName == StandardFrame).Containers.ToDictionary(container => container.Type, container => container.ContainerSlotAssetId);

        Assert.Equal(
            (AugmentItems.AugmentSlotAssetId, AugmentItems.AugmentContainerAssetId, AugmentItems.AugmentContentSlotAssetId),
            (structures["DA_Inventory_ContainerSlot_Augment"], structures["DA_Inventory_AugmentContainer"], structures["DA_Inventory_ContentSlot_Augment_Generic"]));
        Assert.Equal(
            (AugmentItems.CombatMk1AssetId, AugmentItems.LootingMk1AssetId, AugmentItems.TacticalMk1AssetId),
            (Item("DA_Item_Augment_Combat_T1").AssetId, Item("DA_Item_Augment_Looting_T1").AssetId, Item("DA_Item_Augment_Utility_T1").AssetId));
        Assert.Equal(AugmentItems.DefaultMaxWeight, ItemDataRules.DefaultMaxLoadoutWeight);
        Assert.Equal(AugmentItems.ItemAssetIds, augments.ToDictionary(item => item.TechnicalName, item => item.AssetId));
        Assert.Equal(AugmentItems.UsableIndices, augments.ToDictionary(item => item.AssetId, item => item.UsableIndex));
        Assert.Equal(AugmentItems.ItemWeights, augments.ToDictionary(item => item.AssetId, item => item.Weight));
        Assert.Equal(AugmentItems.FrameAssetIds, augments.ToDictionary(item => item.AssetId, item => definitions[item.TechnicalName].FrameAssetId));
        Assert.Equal(AugmentItems.MaxWeights, augments.ToDictionary(item => item.AssetId, item => definitions[item.TechnicalName].MaxWeight));
        Assert.Equal(
            AugmentItems.BranchAssetIds.OrderBy(pair => pair.Key),
            augments.SelectMany(item => frames[definitions[item.TechnicalName].FrameAssetId].Containers)
                .Select(container => KeyValuePair.Create(container.ContainerSlotAssetId, standard[container.Type]))
                .OrderBy(pair => pair.Key));
    }

    private static IReadOnlyList<MenuItem> Menu(string table) => Table(table, GameDataJsonContext.Default.MenuItem);

    private static string[] Json(IEnumerable<ItemDescription> definitions) =>
        [.. definitions.Select(definition => JsonSerializer.Serialize(definition, GameDataJsonContext.Default.ItemDescription))];

    private static string[] Json(IEnumerable<MenuItem> items) => Json(items.Select(item => item.Description));
}
