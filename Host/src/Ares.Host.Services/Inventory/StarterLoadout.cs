using System.Globalization;
using Ares.Structs.Backend.Progression;

namespace Ares.Host.Services.Inventory;

/// <summary>The empty standard player inventory for a local account.</summary>
internal static class StarterLoadout
{
    public static InventoryImport Create(long revision, DateTimeOffset now)
    {
        Dictionary<long, ItemDescription> definitions = StorageItems.Definitions.Concat(LoadoutItems.Definitions)
            .ToDictionary(item => item.GameAssetId);
        List<InventoryItem> items = [];
        string root = CreateItem(LoadoutItems.FrameSlotAssetId);
        return new InventoryImport(items.AsReadOnly(), LoadoutItems.Definitions, [root]);

        string CreateItem(long asset)
        {
            string id = Guid.NewGuid().ToString("D");
            IReadOnlyList<string> children = definitions[asset].SlotConfig switch
            {
                SingleItemSlot { DefaultSlotAssetId: not 0 } slot => [CreateItem(slot.DefaultSlotAssetId)],
                ItemSlotArray slots => [.. Enumerable.Range(0, checked((int)slots.NumberOfSlots)).Select(_ => CreateItem(slots.SlotAssetId))],
                StructuralItemSlots slots => [.. slots.SlotAssetIds.Select(CreateItem)],
                _ => [],
            };
            items.Add(new InventoryItem(id, asset, 1, children, StorageItems.InitialDurability, StorageItems.InitialMaxDurability,
                "", "ares-host-inventory-" + revision.ToString(CultureInfo.InvariantCulture), now.ToUnixTimeMilliseconds()));
            return id;
        }
    }
}
