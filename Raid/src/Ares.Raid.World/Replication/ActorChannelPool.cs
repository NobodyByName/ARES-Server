using System.Collections;
using Ares.Raid.Transport;

namespace Ares.Raid.World.Replication;

/// <summary>Where a pooled actor channel stands.</summary>
internal enum ActorChannelStage
{
    /// <summary>The classes the actor needs are exported.</summary>
    Exporting,

    /// <summary>The channel opens, spawning the actor.</summary>
    Spawning,

    /// <summary>The actor's components are named.</summary>
    Naming,

    /// <summary>The channel is open and sends the actor's changes.</summary>
    Open,

    /// <summary>The channel closes.</summary>
    Closing,
}

/// <summary>An actor's channel in an <see cref="ActorChannelPool{TLink}"/>.</summary>
internal interface IActorChannelLink
{
    /// <summary>Where the channel stands.</summary>
    ActorChannelStage Stage { get; }

    /// <summary>The channel.</summary>
    int Channel { get; }
}

/// <summary>What one family of pooled actors sends at each step of its channels.</summary>
/// <typeparam name="TLink">The family's channel state.</typeparam>
internal interface IActorChannelSteps<TLink>
    where TLink : struct, IActorChannelLink
{
    /// <summary>Whether the client still lacks a bunch the link waits for.</summary>
    /// <param name="sender">The connection's sender.</param>
    /// <param name="index">The actor's index in its family.</param>
    /// <param name="link">Its channel.</param>
    /// <returns>True while the link waits.</returns>
    bool Waits(ReliableSender sender, int index, TLink link);

    /// <summary>Whether a link that waits for nothing ends without another bunch.</summary>
    /// <param name="index">The actor's index in its family.</param>
    /// <param name="link">Its channel.</param>
    /// <returns>True to free the channel now.</returns>
    bool Abandons(int index, TLink link);

    /// <summary>Opens the channel once the exports arrived.</summary>
    /// <param name="sender">The connection's sender.</param>
    /// <param name="index">The actor's index in its family.</param>
    /// <param name="link">Its channel, <see cref="ActorChannelStage.Exporting"/>.</param>
    /// <returns>The link, <see cref="ActorChannelStage.Spawning"/>.</returns>
    TLink Spawn(ReliableSender sender, int index, TLink link);

    /// <summary>Names the actor's components once it spawned.</summary>
    /// <param name="sender">The connection's sender.</param>
    /// <param name="index">The actor's index in its family.</param>
    /// <param name="link">Its channel, <see cref="ActorChannelStage.Spawning"/>.</param>
    /// <returns>The link, <see cref="ActorChannelStage.Naming"/>.</returns>
    TLink Name(ReliableSender sender, int index, TLink link);

    /// <summary>Sends the actor's first state once its components are named.</summary>
    /// <param name="sender">The connection's sender.</param>
    /// <param name="index">The actor's index in its family.</param>
    /// <param name="link">Its channel, <see cref="ActorChannelStage.Naming"/>.</param>
    /// <returns>The link, <see cref="ActorChannelStage.Open"/>.</returns>
    TLink Fill(ReliableSender sender, int index, TLink link);

    /// <summary>Sends what changed on an open channel, or starts closing it.</summary>
    /// <param name="sender">The connection's sender.</param>
    /// <param name="index">The actor's index in its family.</param>
    /// <param name="link">Its channel, <see cref="ActorChannelStage.Open"/>.</param>
    /// <param name="waiting">Whether the client still lacks a bunch the link waits for.</param>
    /// <returns>The link, still open or <see cref="ActorChannelStage.Closing"/>.</returns>
    TLink Update(ReliableSender sender, int index, TLink link, bool waiting);

    /// <summary>Called once a closing channel is freed.</summary>
    /// <param name="index">The actor's index in its family.</param>
    /// <param name="link">Its channel as it closed.</param>
    void Closed(int index, TLink link);
}

/// <summary>
/// A run of channels one connection shares among a family of actors (by index in their family): an actor holds a
/// channel from its delivery until the channel closes again, and each channel goes through
/// <see cref="ActorChannelStage"/> as the client acknowledges its bunches.
/// </summary>
/// <typeparam name="TLink">The family's channel state.</typeparam>
internal sealed class ActorChannelPool<TLink> : IReadOnlyCollection<KeyValuePair<int, TLink>>
    where TLink : struct, IActorChannelLink
{
    private readonly ChannelRange _channels;
    private readonly Dictionary<int, TLink> _links;

    /// <summary>Creates an empty pool.</summary>
    /// <param name="channels">The channels it shares.</param>
    public ActorChannelPool(ChannelRange channels)
        : this(channels, [])
    {
    }

    private ActorChannelPool(ChannelRange channels, Dictionary<int, TLink> links)
    {
        _channels = channels;
        _links = links;
    }

    /// <summary>Channels in use (open or on the way).</summary>
    public int Count => _links.Count;

    /// <summary>Whether an actor holds a channel.</summary>
    /// <param name="index">The actor's index in its family.</param>
    /// <returns>True while it does.</returns>
    public bool Contains(int index) => _links.ContainsKey(index);

    /// <summary>The channel an actor holds.</summary>
    /// <param name="index">The actor's index in its family.</param>
    /// <param name="link">Its channel, when it holds one.</param>
    /// <returns>True when it holds one.</returns>
    public bool TryGet(int index, out TLink link) => _links.TryGetValue(index, out link);

    /// <summary>Gives an actor a channel or replaces its channel state.</summary>
    /// <param name="index">The actor's index in its family.</param>
    /// <param name="link">Its channel.</param>
    public void Set(int index, TLink link) => _links[index] = link;

    /// <summary>The lowest channel no actor holds.</summary>
    /// <returns>The channel, or null when all are in use.</returns>
    public int? FreeChannel()
    {
        for (int channel = _channels.First; channel < _channels.End; channel++)
        {
            if (_links.Values.All(link => link.Channel != channel))
            {
                return channel;
            }
        }

        return null;
    }

    /// <summary>
    /// Moves each channel on by the steps whose bunches the client has. An open channel is updated even while it
    /// waits, so a newer state can follow; other stages wait for the client.
    /// </summary>
    /// <typeparam name="TSteps">The family's steps.</typeparam>
    /// <param name="sender">The connection's sender.</param>
    /// <param name="steps">What the family sends at each step.</param>
    public void Advance<TSteps>(ReliableSender sender, TSteps steps)
        where TSteps : IActorChannelSteps<TLink>
    {
        foreach ((int index, TLink link) in _links.ToArray())
        {
            bool waiting = steps.Waits(sender, index, link);
            switch (waiting)
            {
                case true when link.Stage != ActorChannelStage.Open:
                    continue;
                case false when steps.Abandons(index, link):
                    _links.Remove(index);
                    continue;
            }

            switch (link.Stage)
            {
                case ActorChannelStage.Exporting:
                    _links[index] = steps.Spawn(sender, index, link);
                    break;
                case ActorChannelStage.Spawning:
                    _links[index] = steps.Name(sender, index, link);
                    break;
                case ActorChannelStage.Naming:
                    _links[index] = steps.Fill(sender, index, link);
                    break;
                case ActorChannelStage.Open:
                    _links[index] = steps.Update(sender, index, link, waiting);
                    break;
                case ActorChannelStage.Closing:
                    _links.Remove(index);
                    steps.Closed(index, link);
                    break;
            }
        }
    }

    /// <summary>A copy for speculative packet processing.</summary>
    /// <returns>The copy.</returns>
    public ActorChannelPool<TLink> Copy() => new(_channels, new Dictionary<int, TLink>(_links));

    /// <inheritdoc />
    public IEnumerator<KeyValuePair<int, TLink>> GetEnumerator() => _links.GetEnumerator();

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
