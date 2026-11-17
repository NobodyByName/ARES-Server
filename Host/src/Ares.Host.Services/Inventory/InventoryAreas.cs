namespace Ares.Host.Services.Inventory;

/// <summary>The owned items of each inventory area of a player's state.</summary>
internal static class InventoryAreas
{
    internal static bool UsesSlots(InventoryArea area) => area is InventoryArea.Backpack or InventoryArea.QuickUse or InventoryArea.SafePockets;

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
        InventoryArea.Wallet => inventory.Wallet,
        _ => throw new ArgumentOutOfRangeException(nameof(area)),
    };

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
            InventoryArea.Wallet => inventory with { Wallet = items },
            _ => throw new ArgumentOutOfRangeException(nameof(area)),
        };
    }

    private static IReadOnlyList<OwnedItem> Single(OwnedItem? item) => item is null ? [] : [item];
}
