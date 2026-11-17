using System.Collections.Immutable;
using Ares.Structs.Backend.Progression;
using Ares.Structs.Game.Inventory;

namespace Ares.GameDataExtract.Items;

/// <summary>The menu item tables (see <c>GameTables.Menu…</c>).</summary>
internal sealed record MenuItemTables(
    ImmutableArray<MenuItem> Augments,
    ImmutableArray<MenuItem> Currency,
    ImmutableArray<MenuItem> Gadgets,
    ImmutableArray<MenuItem> Salvage,
    ImmutableArray<MenuItem> Shields,
    ImmutableArray<MenuItem> Weapons);

/// <summary>
/// Builds the menu definitions ARES gives the items its menus support today, from the install. The item sets are
/// ARES's choice; the definitions follow the shape of the backend's: an item is a <c>GameItem</c>
/// (<c>Generator</c> for outpost stations) with its menu asset id, base asset id, stack size, uniqueness and data asset
/// tags without the <c>Inventory.*</c> ones, quality 0, and child slots only for a firearm's mod slots; an inventory
/// structure is an <c>InventoryStructure</c> of one, without base asset id, with the child slots of
/// <see cref="InventoryStructures.SlotsOf"/>.
/// </summary>
internal static class MenuItems
{
    private const string GameItemType = "GameItem";
    private const string GeneratorType = "Generator";
    private const string StructureType = "InventoryStructure";
    private const string InventoryTagPrefix = "Inventory.";

    /// <summary>The structure that holds a raider's augment.</summary>
    private const string AugmentSlot = "DA_Inventory_ContainerSlot_Augment";

    /// <summary>The loadout frame of a raider without augment.</summary>
    private const string StandardFrame = "DA_Inventory_LF_Standard";

    /// <summary>The tier-1 augments; their frames' structures and the augment slot come with them.</summary>
    private static readonly string[] s_augments = ["DA_Item_Augment_Combat_T1", "DA_Item_Augment_Looting_T1", "DA_Item_Augment_Utility_T1"];

    private static readonly string[] s_currency = ["DA_Item_GrindCurrency"];
    private static readonly string[] s_gadgets = ["DA_Item_SnapHook"];
    private static readonly string[] s_shields = ["DA_Item_Armor_LightBody"];

    /// <summary>The firearms and ammunition; the firearms' mod slots come with them.</summary>
    private static readonly string[] s_weapons = ["DA_Item_BattleRifle_BreachAction_01", "DA_Item_HeavyAmmo"];

    private static readonly string[] s_salvage =
    [
        "Battery", "Canister", "Chemicals", "DuctTape", "ElectricalComponents", "ExplosiveMixture", "Fabric", "GunParts", "Magnet",
        "Mat_ARC_Metal", "MechanicalComponents", "Oil", "Planks", "Plastic", "Rubber", "ScrapMetal", "Seeds", "SheetMetal", "SteelSpring",
        "Wires", "WovenCloth",
    ];

    /// <summary>Builds the tables.</summary>
    /// <exception cref="InvalidDataException">An item of the sets is not in the catalogue, or a structure does not decode.</exception>
    public static MenuItemTables Build(IReadOnlyList<ItemAsset> items, InventoryStructures structures, IReadOnlyList<FirearmDefinition> firearms)
    {
        Dictionary<string, ItemAsset> byName = items.ToDictionary(item => item.Row.TechnicalName, StringComparer.Ordinal);
        Dictionary<string, FirearmDefinition> firearmsByClass = firearms.ToDictionary(firearm => firearm.ItemClass, StringComparer.Ordinal);
        ItemAsset Item(string name) => byName.TryGetValue(name, out ItemAsset? item) ? item : throw new InvalidDataException($"The usable-item catalogue has no {name}");
        ImmutableArray<MenuItem> Items(IEnumerable<string> names) => [.. names.Select(Item).OrderBy(item => item.Row.UsableIndex).Select(item => GameItem(item, firearmsByClass))];

        ItemAsset[] weapons = [.. s_weapons.Select(Item).OrderBy(item => item.Row.UsableIndex)];
        ImmutableArray<MenuItem> weaponRows =
        [
            .. weapons.Select(item => GameItem(item, firearmsByClass)),
            .. weapons.Where(item => item.Row.HasChildSlots)
                .SelectMany(item => firearmsByClass[item.Row.ItemClass].ModSlots)
                .Distinct()
                .Select(id => structures.PackagesOf(InventoryStructures.ModSlotClass).Select(structures.Get).Single(slot => slot.AssetId == id))
                .Select(slot => Structure(slot, structures)),
        ];
        return new MenuItemTables(
            Augments(s_augments.Select(Item), structures),
            Items(s_currency),
            Items(s_gadgets),
            Items(s_salvage.Select(name => "DA_Item_Salvage_" + name)),
            Items(s_shields),
            weaponRows);
    }

    /// <summary>
    /// The augments, the structures of their frames (container slots, containers, content slots) and the augment slot's,
    /// by asset id; the structures of the standard frame's tree are left out (the loadout definitions hold them).
    /// </summary>
    private static ImmutableArray<MenuItem> Augments(IEnumerable<ItemAsset> augments, InventoryStructures structures)
    {
        HashSet<long> standard = [];
        Queue<InventoryStructure> pending = new([structures.Named(StandardFrame)]);
        while (pending.TryDequeue(out InventoryStructure? structure))
        {
            if (standard.Add(structure.AssetId))
            {
                foreach (InventoryStructure child in structures.Children(structure))
                {
                    pending.Enqueue(child);
                }
            }
        }

        SortedDictionary<long, MenuItem> rows = [];
        foreach (ItemAsset augment in augments)
        {
            rows[augment.Row.AssetId] = GameItem(augment, new Dictionary<string, FirearmDefinition>());
            pending.Enqueue(structures.Referenced(augment.Package, augment.Values, "LoadoutFrameDataAsset"));
        }

        pending.Enqueue(structures.Named(AugmentSlot));
        while (pending.TryDequeue(out InventoryStructure? structure))
        {
            if (!standard.Contains(structure.AssetId) && rows.TryAdd(structure.AssetId, Structure(structure, structures)))
            {
                foreach (InventoryStructure child in structures.Children(structure))
                {
                    pending.Enqueue(child);
                }
            }
        }

        return [.. rows.Values];
    }

    private static MenuItem GameItem(ItemAsset item, IReadOnlyDictionary<string, FirearmDefinition> firearms)
    {
        ItemDefinition row = item.Row;
        ItemSlotConfiguration slots = row.HasChildSlots && firearms.TryGetValue(row.ItemClass, out FirearmDefinition? firearm)
            ? new StructuralItemSlots(firearm.ModSlots)
            : new NoItemSlots();
        return new MenuItem(row.TechnicalName, new ItemDescription(
            row.AssetId, row.Category == ItemCategory.Generator ? GeneratorType : GameItemType, row.MaxStack, row.MaxStack > 1, row.Unique, MenuTags(row.Tags), [])
        {
            BaseGameAssetId = row.BaseAssetId,
            QualityLevel = 0,
            SlotConfig = slots,
        });
    }

    private static MenuItem Structure(InventoryStructure structure, InventoryStructures structures)
    {
        string[] tags = MenuTags(structure.Values.Get<string[]>("Tags", []));
        return new MenuItem(structure.Asset.AssetName, new ItemDescription(structure.AssetId, StructureType, 1, false, tags.Contains(ItemDataRules.UniqueTag), tags, [])
        {
            BaseGameAssetId = 0,
            QualityLevel = 0,
            SlotConfig = structures.SlotsOf(structure),
        });
    }

    private static string[] MenuTags(IEnumerable<string> tags) => [.. tags.Where(tag => !tag.StartsWith(InventoryTagPrefix, StringComparison.Ordinal))];
}
