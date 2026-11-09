using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Ares.Common.Game.Inventory;
using Ares.Structs.Backend.Progression;

namespace Ares.Host.Services.Inventory;

/// <summary>Builds the game's inventory graph from owned items, purchased stash capacity and shared loadout definitions.</summary>
internal sealed class InventoryProjection
{
    private readonly Dictionary<string, InventoryPosition> _positions = new(StringComparer.Ordinal);
    private readonly Dictionary<string, IReadOnlyList<long>> _allowed = new(StringComparer.Ordinal);

    private InventoryProjection(long userId, PlayerInventory inventory, bool allowOverflow = false)
    {
        Inventory = Validate(inventory, allowOverflow);
        long selectedFrame = InventoryCatalog.FrameAssetId(inventory.Loadout);
        List<InventoryItem> items = [];
        List<string> roots = [];
        Dictionary<long, string> frames = [];
        if (inventory.Stash.SlotCount > 0)
        {
            BuildNode(StorageItems.StashSlotAssetId, "stash", new InventoryPosition(InventoryArea.Stash, 0));
        }

        // The stock menu looks up the next frame and migrates items locally before committing
        // operations. Keep every supported frame present, with independent permanent slot IDs.
        foreach (long frame in AugmentItems.FrameAssetIds.Values.Prepend(LoadoutItems.StandardFrameAssetId))
        {
            string path = frame == LoadoutItems.StandardFrameAssetId ? "carried/container" : "frames/" + frame.ToString(CultureInfo.InvariantCulture);
            frames.Add(frame, BuildNode(frame, path, null, frame));
        }

        roots.Add(BuildNode(LoadoutItems.FrameSlotAssetId, "carried", null));
        roots.Add(BuildNode(AugmentItems.AugmentSlotAssetId, "augment", new InventoryPosition(InventoryArea.Augment, 0)));

        Snapshot = InventorySnapshots.Copy(new InventorySnapshot(userId, inventory.Revision, items.AsReadOnly(), roots.AsReadOnly()));
        return;

        string BuildNode(long asset, string path, InventoryPosition? position, long? frameAssetId = null)
        {
            ItemDescription definition = InventoryCatalog.Definition(inventory, InventoryCatalog.ByAsset[asset]);
            string id = StructuralId(userId, path);
            List<string> children = [];
            switch (definition.SlotConfig)
            {
                case SingleItemSlot { DefaultSlotAssetId: not 0, IsRequired: true } single:
                    children.Add(asset == LoadoutItems.FrameSlotAssetId ? frames[selectedFrame]
                        : BuildNode(single.DefaultSlotAssetId, path + "/container", position, frameAssetId));
                    break;
                case StructuralItemSlots structural:
                    children.AddRange(structural.SlotAssetIds.Select(branch =>
                        BuildNode(branch, path + "/" + LoadoutDefinitions.StandardBranch(branch).ToString(CultureInfo.InvariantCulture),
                            InventoryCatalog.Branch(branch) is { } location ? location with { FrameAssetId = frameAssetId } : null, frameAssetId)));

                    break;
                case ItemSlotArray array:
                    int count = checked((int)array.NumberOfSlots);
                    for (int index = 0; index < count; index++)
                    {
                        InventoryPosition? next = position is null ? null
                            : position with { Index = position.Index + index };
                        children.Add(BuildNode(array.SlotAssetId, path + "/" + index.ToString(CultureInfo.InvariantCulture), next, frameAssetId));
                    }

                    break;
                case SingleItemSlot { IsRequired: false } single:
                    if (position is null)
                    {
                        // Slots without a position stay empty: the default melee structure and weapon mod slots.
                        break;
                    }

                    _positions.Add(id, position);
                    _allowed.Add(id, single.AllowedSlotAssetIds);
                    IReadOnlyList<OwnedItem> contents = frameAssetId is null || frameAssetId == selectedFrame
                        ? InventoryCatalog.Items(inventory, position.Area) : [];
                    OwnedItem? owned = position.Area == InventoryArea.Stash
                        ? contents.OrderBy(item => item.Type, StringComparer.Ordinal).ThenBy(item => item.InstanceId, StringComparer.Ordinal).ElementAtOrDefault(position.Index)
                        : InventoryCatalog.UsesSlots(position.Area) ? contents.SingleOrDefault(item => item.Slot == position.Index)
                        : contents.SingleOrDefault();
                    if (owned is not null)
                    {
                        long itemAsset = InventoryCatalog.AssetId(owned.Type);
                        if (!single.AllowedSlotAssetIds.Contains(itemAsset))
                        {
                            throw new InvalidDataException("Item is incompatible with its equipment slot");
                        }

                        // The menu shows a weapon's mod slots only from these child nodes. They stay empty until mods are supported.
                        List<string> modSlots = InventoryCatalog.ByAsset[itemAsset].SlotConfig is StructuralItemSlots weaponSlots
                            ? [.. weaponSlots.SlotAssetIds.Select((slot, index) =>
                                BuildNode(slot, "items/" + owned.InstanceId + "/" + index.ToString(CultureInfo.InvariantCulture), null))]
                            : [];
                        items.Add(new InventoryItem(owned.InstanceId, itemAsset, owned.Quantity, modSlots.AsReadOnly(), owned.Durability, owned.MaxDurability,
                            "", owned.ETag, owned.UpdatedAt));
                        children.Add(owned.InstanceId);
                    }

                    break;
                default:
                    throw new InvalidDataException("Unsupported inventory structure");
            }

            string etag = "ares-structure-" + Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join('|', children))));
            items.Add(new InventoryItem(id, asset, 1, children.AsReadOnly(), StorageItems.InitialDurability, StorageItems.InitialMaxDurability, "", etag, 0));
            return id;
        }
    }

    internal PlayerInventory Inventory { get; }
    internal InventorySnapshot Snapshot { get; }
    internal static InventoryProjection Create(long userId, PlayerInventory inventory) => new(userId, inventory);
    // A batch may temporarily remove an augment before moving items out of its larger containers.
    // Only the final projection is validated and saved; provisional over-capacity items stay owned.
    internal static InventoryProjection ForOperations(long userId, PlayerInventory inventory) => new(userId, inventory, allowOverflow: true);

    internal InventoryPosition Position(string slotId) => _positions.GetValueOrDefault(slotId)
        ?? throw new InvalidDataException("Unknown inventory destination slot");

    internal bool Allows(string slotId, string itemType) => _allowed.TryGetValue(slotId, out IReadOnlyList<long>? assets)
        && assets.Contains(InventoryCatalog.AssetId(itemType));

    private static PlayerInventory Validate(PlayerInventory inventory, bool allowOverflow)
    {
        if (inventory is not
            {
                Revision: >= 0, Stash.SlotCount: >= 0 and <= InventorySnapshots.MaximumItems,
                Loadout:
                {
                    Backpack.Items.Count: <= InventorySnapshots.MaximumItems,
                    QuickUse.Items.Count: <= InventorySnapshots.MaximumItems,
                    SafePockets.Items.Count: <= InventorySnapshots.MaximumItems,
                },
                OperationReceipts.Count: <= 32,
            }
            || inventory.RaidTicketId is { } ticket && (!Guid.TryParseExact(ticket, "D", out Guid id) || id.ToString("D") != ticket)
            || inventory.OperationReceipts.Any(receipt => receipt is not { RequestId.Length: > 0 and <= 128, Sha256.Length: 64 }
                || receipt.RequestId.Any(character => char.IsControl(character) || char.IsWhiteSpace(character))
                || receipt.Sha256.Any(character => !char.IsAsciiHexDigitLower(character))
                || receipt.DeletedItemIds is not { Count: <= InventorySnapshots.MaximumItems }
                || receipt.DeletedItemIds.Any(deletedId => deletedId is not { Length: > 0 and <= 128 } || deletedId.Any(char.IsControl))
                || receipt.DeletedItemIds.Distinct(StringComparer.Ordinal).Count() != receipt.DeletedItemIds.Count)
            || inventory.OperationReceipts.Select(receipt => receipt.RequestId).Distinct(StringComparer.Ordinal).Count() != inventory.OperationReceipts.Count)
        {
            throw new InvalidDataException("Invalid player inventory");
        }

        HashSet<string> ids = [];
        foreach (InventoryArea area in Enum.GetValues<InventoryArea>())
        {
            IReadOnlyList<OwnedItem> contents = InventoryCatalog.Items(inventory, area)
                ?? throw new InvalidDataException("Missing inventory item array");
            int capacity = InventoryCatalog.Capacity(inventory, area);
            HashSet<int> slots = [];
            if (!allowOverflow && contents.Count > capacity)
            {
                throw new InvalidDataException("Inventory capacity exceeded");
            }

            foreach (OwnedItem item in contents)
            {
                if (item is not { InstanceId.Length: > 0 and <= 128, Type.Length: > 0 and <= 256, Quantity: > 0, ETag.Length: <= 128, UpdatedAt: >= 0 }
                    || item.InstanceId.Any(char.IsControl) || !ids.Add(item.InstanceId)
                    || !double.IsFinite(item.Durability) || !double.IsFinite(item.MaxDurability)
                    || item.Durability < 0 || item.MaxDurability < item.Durability
                    || !InventoryCatalog.UsesSlots(area) && item.Slot is not null
                    || InventoryCatalog.UsesSlots(area) && (item.Slot is not { } slot || slot < 0 || slot >= InventorySnapshots.MaximumItems || !allowOverflow && slot >= capacity || !slots.Add(slot)))
                {
                    throw new InvalidDataException("Invalid owned item or position");
                }

                ItemDescription definition = InventoryCatalog.ByAsset[InventoryCatalog.AssetId(item.Type)];
                if (item.Quantity > definition.MaxAmount || !definition.Stackable && item.Quantity != 1)
                {
                    throw new InvalidDataException("Invalid item quantity");
                }
            }
        }

        return inventory;
    }

    private static string StructuralId(long userId, string path)
    {
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(userId.ToString(CultureInfo.InvariantCulture) + "/" + path));
        return new Guid(hash.AsSpan(0, 16)).ToString("D");
    }
}
