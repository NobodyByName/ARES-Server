using Ares.Common.Game.Inventory;
using Ares.Structs.Net.Replication;

namespace Ares.Raid.World;

/// <summary>Prepares and releases the raid world for a reservation.</summary>
public interface IWorldLifecycle
{
    /// <summary>Prepares the world for a reservation; throws <see cref="WorldPreparationException"/> on failure.</summary>
    /// <param name="matchId">Match id of the reservation.</param>
    /// <param name="players">Reserved players.</param>
    /// <param name="loadouts">Inventory snapshots, or empty inventories when omitted.</param>
    /// <param name="profiles">Public squad profiles, or null when omitted.</param>
    void Prepare(string matchId, IReadOnlyList<long> players, IReadOnlyList<PlayerLoadout>? loadouts = null, IReadOnlyList<CompactPlayerProfile>? profiles = null);

    /// <summary>Releases the world of a reservation (expired or finished).</summary>
    /// <param name="matchId">Match id.</param>
    void Release(string matchId);

    /// <summary>Whether the world of a reservation accepts its players.</summary>
    /// <param name="matchId">Match id.</param>
    /// <returns>Whether players may connect.</returns>
    bool AcceptingPlayers(string matchId);
}

/// <summary>The world could not be prepared for a reservation.</summary>
/// <param name="message">Why.</param>
public sealed class WorldPreparationException(string message) : Exception(message);
