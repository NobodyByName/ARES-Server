using Ares.Common.Game.Inventory;
using Ares.Structs.Net.Replication;

namespace Ares.Raid.World;

/// <summary>Prepares the raid world for its reservations, adds and removes their parties, and releases it.</summary>
public interface IWorldLifecycle
{
    /// <summary>Prepares the world for a reservation; throws <see cref="WorldPreparationException"/> on failure.</summary>
    /// <param name="matchId">Match id of the reservation.</param>
    /// <param name="players">Reserved players.</param>
    /// <param name="loadouts">Inventory snapshots, or empty inventories when omitted.</param>
    /// <param name="profiles">Public squad profiles, or null when omitted.</param>
    void Prepare(string matchId, IReadOnlyList<long> players, IReadOnlyList<PlayerLoadout>? loadouts = null, IReadOnlyList<CompactPlayerProfile>? profiles = null);

    /// <summary>Adds another party to a prepared world; throws <see cref="WorldPreparationException"/> when it cannot take them.</summary>
    /// <param name="matchId">Match id of the prepared world.</param>
    /// <param name="players">The joining players.</param>
    /// <param name="loadouts">Inventory snapshots, or empty inventories when omitted.</param>
    /// <param name="profiles">Public squad profiles, or null when omitted.</param>
    void Join(string matchId, IReadOnlyList<long> players, IReadOnlyList<PlayerLoadout>? loadouts = null, IReadOnlyList<CompactPlayerProfile>? profiles = null);

    /// <summary>Takes players out of the world after their reservation expired; they cannot rejoin it.</summary>
    /// <param name="matchId">Match id of the world.</param>
    /// <param name="players">The leaving players.</param>
    void Leave(string matchId, IReadOnlyList<long> players);

    /// <summary>Releases the world once none of its reservations remain.</summary>
    /// <param name="matchId">Match id.</param>
    void Release(string matchId);

    /// <summary>Whether the world of a reservation accepts its players.</summary>
    /// <param name="matchId">Match id.</param>
    /// <returns>Whether players may connect.</returns>
    bool AcceptingPlayers(string matchId);

    /// <summary>Whether a player has left the world (for example by surrendering), so their reservation can end.</summary>
    /// <param name="matchId">Match id.</param>
    /// <param name="player">User id.</param>
    /// <returns>Whether the player has left.</returns>
    bool HasLeft(string matchId, long player);
}

/// <summary>The world could not be prepared for a reservation.</summary>
/// <param name="message">Why.</param>
public sealed class WorldPreparationException(string message) : Exception(message);
