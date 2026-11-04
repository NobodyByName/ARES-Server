using Ares.Structs.Backend.Progression;

namespace Ares.Host.Services.Inventory;

/// <summary>Maps technical item names and gameplay locations to shared definitions.</summary>
internal static class InventoryCatalog
{
    internal static IReadOnlyList<ItemDescription> Definitions { get; } = [.. StorageItems.Definitions, .. LoadoutItems.Definitions];
    internal static IReadOnlyDictionary<long, ItemDescription> ByAsset { get; } = Definitions.ToDictionary(item => item.GameAssetId);

    private static long ContainerAsset(long branch) => ((SingleItemSlot)ByAsset[branch].SlotConfig!).DefaultSlotAssetId;

    private static int DefaultCapacity(long branch)
    {
        return checked((int)((ItemSlotArray)ByAsset[ContainerAsset(branch)].SlotConfig!).NumberOfSlots);
    }

    internal static bool UsesSlots(InventoryArea area) => area is InventoryArea.Backpack or InventoryArea.QuickUse or InventoryArea.SafePockets;

    internal static int Capacity(PlayerInventory inventory, InventoryArea area) => area == InventoryArea.Stash
        ? inventory.Stash.SlotCount : LoadoutCapacity(inventory.Loadout, area);

    internal static int LoadoutCapacity(PlayerLoadoutState loadout, InventoryArea area) => (loadout.Augment, area) switch
    {
        (null, InventoryArea.Backpack) => DefaultCapacity(LoadoutItems.BackpackSlotAssetId),
        (null, InventoryArea.QuickUse) => DefaultCapacity(LoadoutItems.BeltSlotAssetId),
        (null, InventoryArea.SafePockets) => DefaultCapacity(LoadoutItems.SafePocketSlotAssetId),
        (null, InventoryArea.Augment or InventoryArea.Shield or InventoryArea.MainWeapon or InventoryArea.SecondaryWeapon) => 1,
        (null, _) => throw new ArgumentOutOfRangeException(nameof(area)),
        _ => throw new InvalidDataException("Augment items are not supported yet"),
    };

    internal static long AssetId(string name) => ItemTechnicalNames.AssetIds.TryGetValue(name, out long assetId) ? assetId
        : throw new InvalidDataException("Unknown inventory item technical name");

    internal static IReadOnlyList<OwnedItem> Items(PlayerInventory inventory, InventoryArea area) => area switch
    {
        InventoryArea.Stash => inventory.Stash.Items,
        InventoryArea.Backpack => inventory.Loadout.Backpack.Items,
        InventoryArea.QuickUse => inventory.Loadout.QuickUse.Items,
        InventoryArea.SafePockets => inventory.Loadout.SafePockets.Items,
        InventoryArea.Augment => Single(inventory.Loadout.Augment),
        InventoryArea.Shield => Single(inventory.Loadout.Shield),
        InventoryArea.MainWeapon => Single(inventory.Loadout.MainWeapon),
        InventoryArea.SecondaryWeapon => Single(inventory.Loadout.SecondaryWeapon),
        _ => throw new ArgumentOutOfRangeException(nameof(area)),
    };

    private static IReadOnlyList<OwnedItem> Single(OwnedItem? item) => item is null ? [] : [item];

    internal static PlayerInventory WithItems(PlayerInventory inventory, InventoryArea area, IReadOnlyList<OwnedItem> items)
    {
        PlayerLoadoutState loadout = inventory.Loadout;
        return area switch
        {
            InventoryArea.Stash => inventory with { Stash = inventory.Stash with { Items = items } },
            InventoryArea.Backpack => inventory with { Loadout = loadout with { Backpack = new PlayerItemContainer(items) } },
            InventoryArea.QuickUse => inventory with { Loadout = loadout with { QuickUse = new PlayerItemContainer(items) } },
            InventoryArea.SafePockets => inventory with { Loadout = loadout with { SafePockets = new PlayerItemContainer(items) } },
            InventoryArea.Augment => inventory with { Loadout = loadout with { Augment = items.SingleOrDefault() } },
            InventoryArea.Shield => inventory with { Loadout = loadout with { Shield = items.SingleOrDefault() } },
            InventoryArea.MainWeapon => inventory with { Loadout = loadout with { MainWeapon = items.SingleOrDefault() } },
            InventoryArea.SecondaryWeapon => inventory with { Loadout = loadout with { SecondaryWeapon = items.SingleOrDefault() } },
            _ => throw new ArgumentOutOfRangeException(nameof(area)),
        };
    }

    internal static InventoryPosition? Branch(long assetId) => assetId switch
    {
        LoadoutItems.BackpackSlotAssetId => new InventoryPosition(InventoryArea.Backpack, 0),
        LoadoutItems.SafePocketSlotAssetId => new InventoryPosition(InventoryArea.SafePockets, 0),
        LoadoutItems.BeltSlotAssetId => new InventoryPosition(InventoryArea.QuickUse, 0),
        LoadoutItems.Firearm1SlotAssetId => new InventoryPosition(InventoryArea.MainWeapon, 0),
        LoadoutItems.Firearm2SlotAssetId => new InventoryPosition(InventoryArea.SecondaryWeapon, 0),
        LoadoutItems.ArmorSlotAssetId => new InventoryPosition(InventoryArea.Shield, 0),
        LoadoutItems.MeleeSlotAssetId => null,
        _ => throw new InvalidDataException("Unsupported equipment branch"),
    };

    internal static ItemDescription Definition(PlayerInventory inventory, ItemDescription definition)
    {
        if (definition.SlotConfig is not ItemSlotArray array)
        {
            return definition;
        }

        return definition.GameAssetId == StorageItems.StashContainerAssetId
            ? definition with { SlotConfig = array with { NumberOfSlots = inventory.Stash.SlotCount } }
            : definition;
    }
}
