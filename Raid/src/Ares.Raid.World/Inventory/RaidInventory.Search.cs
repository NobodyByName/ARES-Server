using Ares.Structs.Game.Inventory;

namespace Ares.Raid.World.Inventory;

/// <summary>The search of the open world container: which of its slots still hide their items.</summary>
public sealed partial class RaidInventory
{
    // The containers' modification counter during a search. Every other update sends Version as each container's
    // modification counter; a search-status change only advances Version and leaves that counter as it was.
    private uint _searchModification;

    // Version as of the latest search-status change.
    private uint _searchVersion;

    /// <summary>Search status of each slot of the open world container, or null when every slot shows its item.</summary>
    public IReadOnlyList<ItemSearchStatus>? LootSearchStatuses =>
        OpenLoot is { } id ? Containers[id].SearchStatuses : throw new InvalidOperationException("No open container");

    /// <summary>Shows hidden slots of the open world container and marks the next one as being revealed.</summary>
    /// <param name="revealed">Slots that show their items from now on.</param>
    /// <param name="searching">The hidden slot revealed next, or null when none is left.</param>
    public void RevealLootSlots(IReadOnlyCollection<int> revealed, int? searching)
    {
        ArgumentNullException.ThrowIfNull(revealed);
        if (OpenLoot is not { } id)
        {
            throw new InvalidOperationException("No open container");
        }

        ItemContainerState loot = Containers[id];
        ItemSearchStatus[] statuses = loot.SearchStatuses is { } current ? [.. current] : new ItemSearchStatus[loot.Items.Count];
        foreach (int slot in revealed)
        {
            statuses[slot] = ItemSearchStatus.Shown;
        }

        if (searching is { } next)
        {
            statuses[next] = ItemSearchStatus.HiddenSearching;
        }

        ItemContainerState[] containers = [.. Containers];
        containers[id] = loot with { SearchStatuses = Array.AsReadOnly(statuses) };
        Containers = Array.AsReadOnly(containers);
        if (_searchVersion != Version)
        {
            _searchModification = Version;
        }

        Version++;
        _searchVersion = Version;
        Blackboard = ItemContainerState.Encode(Containers, _searchModification, OpenLoot, dirtyCount: Version);
    }

    // A container whose slot shows its item: whatever the player put in or took out of a slot is in plain view.
    private static ItemContainerState ShowingLootSlot(ItemContainerState container, int slot)
    {
        if (container.SearchStatuses is not { } statuses || statuses[slot] == ItemSearchStatus.Shown)
        {
            return container;
        }

        ItemSearchStatus[] shown = [.. statuses];
        shown[slot] = ItemSearchStatus.Shown;
        return container with { SearchStatuses = Array.AsReadOnly(shown) };
    }
}
