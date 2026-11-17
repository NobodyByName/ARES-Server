using Ares.Raid.Transport;
using Ares.Raid.World.Movement;
using Ares.Structs.Core.Geometry;

namespace Ares.Raid.World.Players;

/// <summary>What a family of world actors (see <see cref="IPlayerFamily"/>) sees of the rest of a player's channels.</summary>
internal interface IPlayerChannelsContext
{
    /// <summary>The raid.</summary>
    RaidWorld Raid { get; }

    /// <summary>The player's raid state: inventory, weapons and profiles.</summary>
    RaidPlayer Member { get; }

    /// <summary>The player's actors.</summary>
    WorldPlayer Player { get; }

    /// <summary>Where the actor delivery stands.</summary>
    DeliveryPhase Phase { get; }

    /// <summary>Latest client input frame received.</summary>
    uint? InputFrame { get; }

    /// <summary>The character position the client predicted in its latest input frame, if any.</summary>
    Vector3D? InputPosition { get; }

    /// <summary>The heading of the client's control rotation in its latest input frame, in radians, if any.</summary>
    double? InputYaw { get; }

    /// <summary>
    /// Where the player is for relevance: an operator teleport's destination, else the latest reported position, else
    /// the spawn.
    /// </summary>
    Vector3D RelevancePosition { get; }

    /// <summary>Whether the client reports a level visible.</summary>
    /// <param name="level">Level (cell) name.</param>
    /// <returns>True while it does.</returns>
    bool LevelVisible(string level);

    /// <summary>Whether map actors may be sent: the character simulation has started and settled.</summary>
    /// <param name="now">Raid clock.</param>
    /// <returns>True once they may.</returns>
    bool MapActorsAllowed(double now);

    /// <summary>Whether the player gave movement input after an input frame.</summary>
    /// <param name="frame">Input frame.</param>
    /// <returns>True when a later frame moves.</returns>
    bool MovedSince(uint frame);

    /// <summary>
    /// Sends no pawn frame before a time, so the client keeps its interaction's ending long enough to play it (replaces
    /// an earlier hold).
    /// </summary>
    /// <param name="until">Raid clock.</param>
    void HoldFrames(double until);

    /// <summary>
    /// The character's pawn frames show an interaction the server runs for it (a container search, a descend hold),
    /// times in raid world seconds (replaces an earlier one).
    /// </summary>
    /// <param name="interaction">The interaction.</param>
    void RunInteraction(PawnInteraction interaction);

    /// <summary>The server-run interaction, if any, ended successfully: the pawn frames show it succeeded.</summary>
    void SucceedInteraction();

    /// <summary>Whether the client has the character's interactor named, so it takes the interaction calls on it.</summary>
    bool InteractorExported { get; }

    /// <summary>Names the character's interactor to the client once; it counts as named once acknowledged.</summary>
    /// <param name="sender">The connection's sender.</param>
    void ExportInteractor(ReliableSender sender);

    /// <summary>Numbers a new server-run interaction.</summary>
    /// <returns>Its sequence number.</returns>
    uint NextInteractionSequence();

    /// <summary>A request of an input frame changed the inventory: no pawn frame before that frame goes out.</summary>
    /// <param name="frame">Input frame.</param>
    void InventoryChangedAt(uint frame);

    /// <summary>The character climbs a ladder from an input frame on.</summary>
    /// <param name="frame">Input frame of the press.</param>
    /// <param name="ladder">Ladder index.</param>
    /// <param name="topEntry">Whether the climb starts at the top.</param>
    void Climb(uint frame, int ladder, bool topEntry);

    /// <summary>Logs a press no family acted on when its target is no interaction the server gave the client.</summary>
    /// <param name="request">The request.</param>
    void LogUnhandled(InteractionRequest request);
}
