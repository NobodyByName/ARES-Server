using Ares.Structs.Net.Control;
using Ares.Structs.Net.Session;

namespace Ares.Raid.Transport;

/// <summary>The reliable channel state of one connection, shared by the control and world layers.</summary>
/// <param name="Sender">Outgoing reliable delivery.</param>
/// <param name="Receiver">Incoming reliable delivery.</param>
public sealed record ChannelIo(ReliableSender Sender, ReliableReceiver Receiver);

/// <summary>The raid world as the connection layer sees it.</summary>
public interface IRaidWorld
{
    /// <summary>The Welcome for an admitted player, or null while the world cannot take the player yet.</summary>
    /// <param name="principal">The player.</param>
    /// <returns>The Welcome, or null.</returns>
    ControlWelcome? WelcomeFor(SessionPrincipal principal);

    /// <summary>Starts the player's world channels after the client joined the map.</summary>
    /// <param name="principal">The player.</param>
    /// <param name="login">What the player's login gives its actors.</param>
    /// <param name="io">The connection's channels.</param>
    /// <returns>The player's channel state.</returns>
    IPlayerChannels StartPlayer(SessionPrincipal principal, PlayerLogin login, ChannelIo io);
}

/// <summary>
/// One player's world channel state. Calls happen on a copy of the connection state; nothing outside the
/// player's own state changes until <see cref="Commit"/>.
/// </summary>
public interface IPlayerChannels
{
    /// <summary>A copy for speculative packet processing.</summary>
    /// <returns>The copy.</returns>
    IPlayerChannels Copy();

    /// <summary>Applies an already validated acknowledgement and advances delivery.</summary>
    /// <param name="io">The connection's channels.</param>
    /// <param name="header">The client's notification header.</param>
    void Acknowledge(ChannelIo io, NotificationHeader header);

    /// <summary>Handles a bunch on an actor channel.</summary>
    /// <param name="io">The connection's channels.</param>
    /// <param name="bunch">The bunch.</param>
    void Receive(ChannelIo io, ChannelBunch bunch);

    /// <summary>Handles a direct-replication payload from the client.</summary>
    /// <param name="kind">Stream kind.</param>
    /// <param name="payload">The payload.</param>
    /// <returns>Whether the stream kind is handled (others are dropped).</returns>
    bool ObserveDirect(int kind, byte[] payload);

    /// <summary>Queues periodic deliveries (acknowledgements, frames, streaming, doors).</summary>
    /// <param name="io">The connection's channels.</param>
    /// <param name="now">Current time in seconds.</param>
    void Service(ChannelIo io, double now);

    /// <summary>The server time the player's packets carry: the world time of the player's raid.</summary>
    /// <param name="now">Current time in seconds.</param>
    /// <returns>Server time in seconds.</returns>
    double ServerSeconds(double now);

    /// <summary>The client failed to create the actor of a channel.</summary>
    /// <param name="channel">Channel index.</param>
    void ActorChannelFailed(int channel);

    /// <summary>Applies deferred changes to the shared world (the packet was accepted).</summary>
    void Commit();

    /// <summary>The committed connection was removed. Speculative packet copies are never notified.</summary>
    void Closed();
}
