using Ares.Structs.Backend.Progression;

namespace Ares.Common.Game.Inventory;

/// <summary>One item a player carries out of a raid, with where it sits.</summary>
/// <param name="Place">See <see cref="CarriedPlaces"/>.</param>
/// <param name="Slot">Zero-based position in the backpack, quick-use or safe pocket places; 0 for single equipment places.</param>
/// <param name="Item">The item, without nested slot references.</param>
public sealed record CarriedItem(string Place, int Slot, InventoryItem Item);

/// <summary>Places a carried item can sit in.</summary>
public static class CarriedPlaces
{
    /// <summary>The equipped augment.</summary>
    public const string Augment = "augment";

    /// <summary>The equipped shield.</summary>
    public const string Shield = "shield";

    /// <summary>The main weapon.</summary>
    public const string MainWeapon = "mainWeapon";

    /// <summary>The secondary weapon.</summary>
    public const string SecondaryWeapon = "secondaryWeapon";

    /// <summary>Backpack slots.</summary>
    public const string Backpack = "backpack";

    /// <summary>Quick-use slots.</summary>
    public const string QuickUse = "quickUse";

    /// <summary>Safe pocket slots; these items survive a lost raid.</summary>
    public const string SafePockets = "safePockets";
}

/// <summary>How a player's raid ended.</summary>
public static class RaidOutcomes
{
    /// <summary>Left through the extraction elevator: the whole loadout comes home.</summary>
    public const string Extracted = "EXTRACTED";

    /// <summary>Surrender, death or disconnect: only the safe pocket items come home.</summary>
    public const string Lost = "LOST";
}

/// <summary>What one player carries home from a finished raid.</summary>
/// <param name="TicketId">The matchmaking ticket the player was admitted with (canonical UUID).</param>
/// <param name="UserId">Tenancy user id.</param>
/// <param name="Revision">Inventory revision of the loadout Raid was given, so Host can reject a stale result.</param>
/// <param name="Outcome">See <see cref="RaidOutcomes"/>.</param>
/// <param name="Items">The carried items as a flat list.</param>
public sealed record RaidResult(string TicketId, long UserId, long Revision, string Outcome, IReadOnlyList<CarriedItem> Items);

/// <summary>Validation of raid results.</summary>
public static class RaidResults
{
    private static readonly HashSet<string> s_singlePlaces = new(StringComparer.Ordinal)
    {
        CarriedPlaces.Augment,
        CarriedPlaces.Shield,
        CarriedPlaces.MainWeapon,
        CarriedPlaces.SecondaryWeapon,
    };

    private static readonly HashSet<string> s_listPlaces = new(StringComparer.Ordinal)
    {
        CarriedPlaces.Backpack,
        CarriedPlaces.QuickUse,
        CarriedPlaces.SafePockets,
    };

    /// <summary>Checks a result for internal consistency.</summary>
    /// <exception cref="InvalidDataException">The result is malformed.</exception>
    public static void Validate(RaidResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        if (!Guid.TryParseExact(result.TicketId, "D", out Guid ticket) || ticket.ToString("D") != result.TicketId)
        {
            throw new InvalidDataException("Invalid raid result ticket");
        }

        if (result.UserId <= 0 || result.Revision < 0
            || result.Outcome is not (RaidOutcomes.Extracted or RaidOutcomes.Lost)
            || result.Items is null || result.Items.Count > InventorySnapshots.MaximumItems)
        {
            throw new InvalidDataException("Invalid raid result identity, outcome or size");
        }

        HashSet<(string, int)> positions = [];
        HashSet<string> ids = new(StringComparer.Ordinal);
        foreach (CarriedItem? carried in result.Items)
        {
            if (carried is null)
            {
                throw new InvalidDataException("Raid result contains a null carried item");
            }

            if (carried.Item is null || carried.Place is null
                || !(s_singlePlaces.Contains(carried.Place) || s_listPlaces.Contains(carried.Place)))
            {
                throw new InvalidDataException("Invalid carried item place");
            }

            InventoryItem item = carried.Item;
            if (carried.Slot < 0 || (carried.Slot != 0 && s_singlePlaces.Contains(carried.Place))
                || !positions.Add((carried.Place, carried.Slot)))
            {
                throw new InvalidDataException("Invalid or duplicate carried item slot");
            }

            if (result.Outcome == RaidOutcomes.Lost && carried.Place != CarriedPlaces.SafePockets)
            {
                throw new InvalidDataException("A lost raid carries only safe pocket items");
            }

            if (!InventorySnapshots.ValidId(item.InstanceId) || !ids.Add(item.InstanceId) || item.GameAssetId == 0
                || HasSlots(item.Slots) || item.Amount <= 0
                || !double.IsFinite(item.Durability) || !double.IsFinite(item.MaxDurability)
                || item.Durability < 0 || item.Durability > item.MaxDurability)
            {
                throw new InvalidDataException("Invalid or duplicate carried item");
            }
        }
    }

    // Carried items are flat records: slots come from JSON, so a missing list is as invalid as a non-empty one.
    private static bool HasSlots(IReadOnlyList<string>? slots) => slots is null || slots.Count != 0;
}
