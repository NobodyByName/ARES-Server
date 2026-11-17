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

    /// <summary>The character position the client predicted in its latest input frame, if any.</summary>
    Vector3D? InputPosition { get; }

    /// <summary>The heading of the client's control rotation in its latest input frame, in radians, if any.</summary>
    double? InputYaw { get; }

    /// <summary>
    /// Where the player is for relevance: an operator teleport's destination, else the latest reported position, else
    /// the spawn.
    /// </summary>
    Vector3D RelevancePosition { get; }

    /// <summary>Whether map actors may be sent: the character simulation has started and settled.</summary>
    /// <param name="now">Raid clock.</param>
    /// <returns>True once they may.</returns>
    bool MapActorsAllowed(double now);

    /// <summary>A request of an input frame changed the inventory: no pawn frame before that frame goes out.</summary>
    /// <param name="frame">Input frame.</param>
    void InventoryChangedAt(uint frame);
}
