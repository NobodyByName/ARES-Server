using System.Globalization;
using Ares.Structs.Backend.Progression;

namespace Ares.Host.Services.Inventory;

/// <summary>One rubber duck in a basic stash for a new local account.</summary>
internal static class StarterStorage
{
    public static InventoryImport Create(long revision, DateTimeOffset now)
    {
        string stash = NewId();
        string container = NewId();
        string duck = NewId();
        string[] slots = [.. Enumerable.Range(0, StorageItems.StashSlotCount).Select(_ => NewId())];
        List<InventoryItem> items =
        [
            Item(stash, StorageItems.StashSlotAssetId, [container]),
            Item(container, StorageItems.StashContainerAssetId, slots),
            .. slots.Select((id, index) => Item(id, StorageItems.ContentSlotAssetId, index == 0 ? [duck] : [])),
            Item(duck, StorageItems.RubberDuckAssetId, []),
        ];
        return new InventoryImport(items.AsReadOnly(), StorageItems.Definitions, []);

        InventoryItem Item(string id, long asset, IReadOnlyList<string> children) => new(id, asset, 1, children,
            StorageItems.InitialDurability, StorageItems.InitialMaxDurability, "",
            "ares-host-inventory-" + revision.ToString(CultureInfo.InvariantCulture), now.ToUnixTimeMilliseconds());
    }

    private static string NewId() => Guid.NewGuid().ToString("D");
}
