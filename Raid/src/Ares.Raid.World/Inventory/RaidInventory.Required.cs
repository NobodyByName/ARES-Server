using Ares.Structs.Game.Inventory;

namespace Ares.Raid.World.Inventory;

/// <summary>Items an interaction asks the player to carry, such as the key of a locked door.</summary>
public sealed partial class RaidInventory
{
    /// <summary>How many items of <paramref name="assetIndexes"/> the carried containers hold, counting stack amounts.</summary>
    /// <param name="assetIndexes">Asset indexes of the items.</param>
    /// <returns>The amount carried.</returns>
    public int Carrying(IReadOnlySet<ushort> assetIndexes)
    {
        ArgumentNullException.ThrowIfNull(assetIndexes);
        return Carried.SelectMany(container => container.Items).OfType<ContainerItem>().Where(item => assetIndexes.Contains(item.AssetIndex)).Sum(item => item.Amount);
    }

    /// <summary>Takes <paramref name="amount"/> items of <paramref name="assetIndexes"/> from the carried containers, first slots first.</summary>
    /// <param name="assetIndexes">Asset indexes of the items.</param>
    /// <param name="amount">How many to take.</param>
    /// <returns>Whether they were taken; nothing changes when the carried containers hold fewer.</returns>
    public bool Take(IReadOnlySet<ushort> assetIndexes, int amount)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(amount);
        if (Carrying(assetIndexes) < amount)
        {
            return false;
        }

        ItemContainerState[] updated = [.. Containers];
        int needed = amount;
        for (int container = 0; container < updated.Length && needed > 0; container++)
        {
            if (updated[container].Id == OpenLoot)
            {
                continue;
            }

            ContainerItem?[] contents = [.. updated[container].Items];
            for (int slot = 0; slot < contents.Length && needed > 0; slot++)
            {
                if (contents[slot] is not { } item || !assetIndexes.Contains(item.AssetIndex))
                {
                    continue;
                }

                int taken = Math.Min(needed, item.Amount);
                contents[slot] = item.Amount == taken ? null : item with { Amount = item.Amount - taken };
                needed -= taken;
            }

            updated[container] = updated[container] with { Items = Array.AsReadOnly(contents) };
        }

        Containers = Array.AsReadOnly(updated);
        Version++;
        Blackboard = ItemContainerState.Encode(Containers, Version, OpenLoot);
        return true;
    }
}
