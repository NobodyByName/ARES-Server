using System.Collections.ObjectModel;
using System.Security.Cryptography;
using System.Text.Json;
using Ares.Structs.Backend.Progression;

namespace Ares.Common.Game.Inventory;

/// <summary>A player's inventory at one revision, including the roots selected for a raid.</summary>
public sealed record InventorySnapshot(long UserId, long Revision, IReadOnlyList<InventoryItem> Items, IReadOnlyList<string> LoadoutRoots);

/// <summary>The immutable item tree handed to a raid for one player.</summary>
public sealed record PlayerLoadout(long UserId, long Revision, IReadOnlyList<InventoryItem> Items);

/// <summary>Acknowledges the exact player revision and item tree accepted by Raid.</summary>
public sealed record LoadoutReceipt(long UserId, long Revision, string Sha256);

/// <summary>Validation and defensive copies shared by inventory storage and raid admission.</summary>
public static class InventorySnapshots
{
    /// <summary>Largest selected item tree accepted for a Raid handoff.</summary>
    public const int MaximumItems = 256;

    /// <summary>Menu inventory also includes the empty frames needed for local augment changes.</summary>
    public const int MaximumSnapshotItems = 1024;

    private const int MaximumDepth = 32;
    private const int MaximumTextLength = 128;

    /// <summary>Validates a snapshot and copies its lists so the caller cannot change it afterwards.</summary>
    public static InventorySnapshot Copy(InventorySnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (snapshot.UserId <= 0 || snapshot.Revision < 0 || snapshot.Items is null || snapshot.LoadoutRoots is null
            || snapshot.Items.Count > MaximumSnapshotItems || snapshot.LoadoutRoots.Count > MaximumItems)
        {
            throw new InvalidDataException("Invalid inventory identity, revision or size");
        }

        Dictionary<string, InventoryItem> items = new(StringComparer.Ordinal);
        HashSet<string> children = new(StringComparer.Ordinal);
        foreach (InventoryItem? item in snapshot.Items)
        {
            if (item is null)
            {
                throw new InvalidDataException("Inventory contains a null item");
            }

            ReadOnlyCollection<string> slots = Freeze(item.Slots);
            if (!ValidId(item.InstanceId) || item.GameAssetId == 0 || item.Amount <= 0
                || slots.Count > MaximumItems
                || !double.IsFinite(item.Durability) || !double.IsFinite(item.MaxDurability)
                || item.Durability < 0 || item.MaxDurability < item.Durability
                || !ValidText(item.LockedInRound) || !ValidText(item.ETag) || item.UpdatedAt < 0
                || !items.TryAdd(item.InstanceId, item with { Slots = slots }))
            {
                throw new InvalidDataException("Invalid or duplicate inventory item");
            }

            if (slots.Any(child => !ValidId(child) || !children.Add(child)))
            {
                throw new InvalidDataException("Invalid or multiply owned inventory slot reference");
            }
        }

        if (children.Any(child => !items.ContainsKey(child)))
        {
            throw new InvalidDataException("Inventory references a missing item");
        }

        HashSet<string> visited = new(StringComparer.Ordinal);
        foreach (string root in items.Keys.Where(id => !children.Contains(id)))
        {
            Visit(root, 0);
        }

        if (visited.Count != items.Count)
        {
            throw new InvalidDataException("Inventory contains a cycle");
        }

        HashSet<string> loadout = new(StringComparer.Ordinal);
        if (snapshot.LoadoutRoots.Any(root => !ValidId(root) || !items.ContainsKey(root) || children.Contains(root) || !loadout.Add(root)))
        {
            throw new InvalidDataException("Loadout selections must be distinct inventory roots");
        }

        return snapshot with { Items = Array.AsReadOnly<InventoryItem>([.. items.Values]), LoadoutRoots = Freeze(snapshot.LoadoutRoots) };

        void Visit(string id, int depth)
        {
            if (depth > MaximumDepth || !visited.Add(id))
            {
                throw new InvalidDataException("Inventory tree is cyclic or too deep");
            }

            foreach (string child in items[id].Slots)
            {
                Visit(child, depth + 1);
            }
        }
    }

    /// <summary>Copies the selected trees, excluding items kept in the stash.</summary>
    public static PlayerLoadout Loadout(InventorySnapshot snapshot)
    {
        InventorySnapshot copy = Copy(snapshot);
        Dictionary<string, InventoryItem> items = copy.Items.ToDictionary(item => item.InstanceId, StringComparer.Ordinal);
        List<InventoryItem> selected = [];
        foreach (string root in copy.LoadoutRoots)
        {
            Add(root);
        }

        return Copy(new PlayerLoadout(copy.UserId, copy.Revision, selected.AsReadOnly()));

        void Add(string id)
        {
            InventoryItem item = items[id];
            selected.Add(item);
            foreach (string child in item.Slots)
            {
                Add(child);
            }
        }
    }

    /// <summary>Validates and freezes the item tree received by a raid.</summary>
    public static PlayerLoadout Copy(PlayerLoadout loadout)
    {
        ArgumentNullException.ThrowIfNull(loadout);
        if (loadout.Items is null || loadout.Items.Count > MaximumItems)
        {
            throw new InvalidDataException("Invalid player loadout size");
        }

        InventorySnapshot copy = Copy(new InventorySnapshot(loadout.UserId, loadout.Revision, loadout.Items, []));
        return new PlayerLoadout(copy.UserId, copy.Revision, copy.Items);
    }

    /// <summary>Compares a retry with the original snapshot, including ordered slot references.</summary>
    public static bool Same(PlayerLoadout left, PlayerLoadout right)
    {
        if (left.UserId != right.UserId || left.Revision != right.Revision || left.Items.Count != right.Items.Count)
        {
            return false;
        }

        Dictionary<string, InventoryItem> items = right.Items.ToDictionary(item => item.InstanceId, StringComparer.Ordinal);
        return left.Items.All(item => items.TryGetValue(item.InstanceId, out InventoryItem? other)
            && item with { Slots = other.Slots } == other && item.Slots.SequenceEqual(other.Slots));
    }

    /// <summary>Requires exactly one validated loadout per reserved player, or supplies empty loadouts for older callers.</summary>
    public static IReadOnlyList<PlayerLoadout> ForPlayers(IReadOnlyList<long> players, IReadOnlyList<PlayerLoadout>? loadouts)
    {
        ArgumentNullException.ThrowIfNull(players);
        if (players.Count == 0 || players.Any(player => player <= 0) || players.Distinct().Count() != players.Count)
        {
            throw new InvalidDataException("Invalid loadout membership");
        }

        if (loadouts is null)
        {
            return Array.AsReadOnly<PlayerLoadout>([.. players.Select(player => new PlayerLoadout(player, 0, []))]);
        }

        if (loadouts.Count != players.Count || loadouts.Any(loadout => loadout is not { UserId: > 0 })
            || !loadouts.Select(loadout => loadout.UserId).Order().SequenceEqual(players.Order()))
        {
            throw new InvalidDataException("Loadouts do not match the reserved players");
        }

        return Array.AsReadOnly<PlayerLoadout>([.. loadouts.Select(Copy).OrderBy(loadout => loadout.UserId)]);
    }

    /// <summary>Hashes a validated snapshot in canonical item order; slot order remains significant.</summary>
    public static LoadoutReceipt Receipt(PlayerLoadout loadout)
    {
        PlayerLoadout copy = Copy(loadout);
        byte[] data = JsonSerializer.SerializeToUtf8Bytes(copy with { Items = [.. copy.Items.OrderBy(item => item.InstanceId, StringComparer.Ordinal)] });
        return new LoadoutReceipt(copy.UserId, copy.Revision, Convert.ToHexStringLower(SHA256.HashData(data)));
    }

    internal static bool ValidId(string? value) => !string.IsNullOrWhiteSpace(value) && value.Length <= MaximumTextLength
        && value.All(character => !char.IsControl(character));

    private static bool ValidText(string? value) => value is { Length: <= MaximumTextLength };

    private static ReadOnlyCollection<T> Freeze<T>(IReadOnlyList<T>? values) => values is null
        ? throw new InvalidDataException("Inventory contains a null list")
        : Array.AsReadOnly<T>([.. values]);
}
