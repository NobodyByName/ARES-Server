using Ares.Raid.Transport;
using Ares.Raid.World.Movement;

namespace Ares.Raid.World.Players;

/// <summary>
/// One family of world actors on a player's connection (doors, ladders, pickups, ARCs): it owns what the client has
/// of them, sends them, and takes the client's interaction requests on them. Each family also has a <c>Copy</c> for
/// speculative packet processing that shares nothing it changes in place with the original.
/// </summary>
internal interface IPlayerFamily
{
    /// <summary>Sends the family's actors and their changes to the client.</summary>
    /// <param name="player">The player's channels.</param>
    /// <param name="sender">The connection's sender.</param>
    /// <param name="now">Raid clock.</param>
    void Service(IPlayerChannelsContext player, ReliableSender sender, double now);

    /// <summary>Takes an interaction request the families before it left.</summary>
    /// <param name="player">The player's channels.</param>
    /// <param name="request">The request.</param>
    /// <param name="repeated">Whether it repeats the request of the previous input frame (a held press).</param>
    /// <param name="now">Raid clock.</param>
    /// <returns>True when the request ends here; false passes it on to the next family.</returns>
    bool TryHandle(IPlayerChannelsContext player, InteractionRequest request, bool repeated, double now);

    /// <summary>Whether the client may use an interaction target it focuses: pawn frames keep it as their focus.</summary>
    /// <param name="player">The player's channels.</param>
    /// <param name="target">The focused object.</param>
    /// <returns>True when the target is one of the family's and usable now.</returns>
    bool Usable(IPlayerChannelsContext player, ulong target);

    /// <summary>The client closed a channel; when it is one of the family's, nothing more goes to the actor on it.</summary>
    /// <param name="player">The player's channels.</param>
    /// <param name="channel">The channel.</param>
    /// <returns>True when the channel is one of the family's.</returns>
    bool ChannelFailed(IPlayerChannelsContext player, int channel);
}
