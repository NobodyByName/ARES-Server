using Ares.Host.Services.Inventory;
using Ares.Structs.Backend.Progression;

namespace Ares.Host.Tests;

/// <summary>Stash states shared by inventory tests.</summary>
internal static class TestStash
{
    /// <summary>The stash container tier after <paramref name="tier"/>.</summary>
    internal static long Next(long tier) => StorageItems.Definitions.Single(definition => definition.GameAssetId == tier).Upgrade!.NextItemGameAssetId;

    /// <summary>Pads the stash with single ducks until every slot is used.</summary>
    internal static PlayerInventory Full(PlayerInventory inventory) => inventory with
    {
        Stash = inventory.Stash with
        {
            Items =
            [
                .. inventory.Stash.Items,
                .. Enumerable.Range(0, inventory.Stash.SlotCount - inventory.Stash.Items.Count).Select(index => new OwnedItem(
                    "padding-" + index.ToString(System.Globalization.CultureInfo.InvariantCulture), ItemTechnicalNames.RubberDuck, 1,
                    StorageItems.InitialDurability, StorageItems.InitialMaxDurability, "test", 0)),
            ],
        },
    };
}
