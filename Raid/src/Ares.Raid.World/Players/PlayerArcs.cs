using Ares.Raid.Transport;
using Ares.Raid.World.Actors;
using Ares.Raid.World.Movement;
using Ares.Raid.World.Replication;

namespace Ares.Raid.World.Players;

/// <summary>
/// The raid's ARCs as one player sees them: each ARC relevant to the player is delivered on its own channel, which
/// stays open while the ARC is relevant and closes when it is despawned or out of range.
/// </summary>
internal sealed class PlayerArcs : IPlayerFamily
{
    private readonly ChannelRange _channels;

    // The channels that carry ARCs for this client; an ARC holds one while the client has it.
    private ActorChannelPool<ArcLink> _links;
    private Dictionary<ulong, ReliableKey> _exports = [];
    private Dictionary<int, ActorChannelStage> _failed = [];
    private double _relevantAt;

    /// <summary>Creates the ARCs of a connection that has none yet.</summary>
    /// <param name="channels">The channels the ARCs share.</param>
    public PlayerArcs(ChannelRange channels)
    {
        _channels = channels;
        _links = new ActorChannelPool<ArcLink>(channels);
    }

    /// <summary>ARCs whose actor and ability system this client has.</summary>
    public int Ready => _links.Count(entry => entry.Value.Stage == ActorChannelStage.Open);

    /// <summary>ARC channels open now (or on the way).</summary>
    public int ChannelsInUse => _links.Count;

    /// <summary>ARCs whose channel the client refused, as index@stage.</summary>
    public IReadOnlyList<string> Failures => [.. _failed.OrderBy(entry => entry.Key).Select(entry => $"{entry.Key}@{entry.Value}")];

    /// <summary>Whether this client has an ARC.</summary>
    /// <param name="arc">ARC index.</param>
    /// <returns>True from the ability system's content until the channel closes.</returns>
    public bool Has(int arc) => _links.TryGet(arc, out ArcLink link) && link.Stage == ActorChannelStage.Open;

    /// <summary>A copy for speculative packet processing.</summary>
    /// <returns>The copy.</returns>
    public PlayerArcs Copy()
    {
        PlayerArcs copy = (PlayerArcs)MemberwiseClone();
        copy._links = _links.Copy();
        copy._exports = new Dictionary<ulong, ReliableKey>(_exports);
        copy._failed = new Dictionary<int, ActorChannelStage>(_failed);
        return copy;
    }

    /// <inheritdoc />
    public bool ChannelFailed(IPlayerChannelsContext player, int channel)
    {
        if (!_channels.Contains(channel))
        {
            return false;
        }

        foreach ((int arc, ArcLink link) in _links)
        {
            if (link.Channel == channel)
            {
                _failed.TryAdd(arc, link.Stage);
            }
        }

        return true;
    }

    /// <inheritdoc />
    public bool TryHandle(IPlayerChannelsContext player, InteractionRequest request, bool repeated, double now) => false;

    /// <inheritdoc />
    public bool Usable(IPlayerChannelsContext player, ulong target) => false;

    /// <inheritdoc />
    public void Service(IPlayerChannelsContext player, ReliableSender sender, double now)
    {
        if (!player.MapActorsAllowed(now))
        {
            return;
        }

        RaidWorld raid = player.Raid;
        _links.Advance(sender, new ArcSteps(this, player));
        if (now < _relevantAt)
        {
            return;
        }

        _relevantAt = now + PlayerChannels.LootRelevanceInterval;
        foreach (WorldArc arc in raid.Arcs.All)
        {
            if (_links.Contains(arc.Index) || _failed.ContainsKey(arc.Index) || !WorldArcs.Relevant(arc, player.RelevancePosition)
                || _links.FreeChannel() is not { } channel)
            {
                continue;
            }

            // The class default and the attribute set classes go once per client.
            ulong[] missing = [.. raid.Arcs.Roots(arc).Where(root => !_exports.ContainsKey(root))];
            if (missing.Length > 0)
            {
                ReliableKey key = sender.Queue(raid.Arcs.ExportBunch(missing));
                foreach (ulong root in missing)
                {
                    _exports[root] = key;
                }
            }

            _links.Set(arc.Index, new ArcLink(ActorChannelStage.Exporting, channel, null));
        }

        _links.Advance(sender, new ArcSteps(this, player));
    }

    // An ARC's channel on this connection: its stage and the bunch it waits for.
    private readonly record struct ArcLink(ActorChannelStage Stage, int Channel, ReliableKey? Key) : IActorChannelLink;

    // What an ARC's channel sends: the classes are exported, the actor spawned with its attribute sets, its ability
    // system named, then filled in; the channel then stays open until the ARC is despawned or no longer relevant.
    private readonly struct ArcSteps(PlayerArcs arcs, IPlayerChannelsContext player) : IActorChannelSteps<ArcLink>
    {
        private WorldArcs World => player.Raid.Arcs;

        public bool Waits(ReliableSender sender, int index, ArcLink link)
        {
            if (link.Stage != ActorChannelStage.Exporting)
            {
                return link.Key is { } key && sender.Pending(key);
            }

            Dictionary<ulong, ReliableKey> exports = arcs._exports;
            return World.Roots(World.All[index]).Any(root => sender.Pending(exports[root]));
        }

        // The client refused the channel, or the ARC went away before it opened: nothing more goes to it.
        public bool Abandons(int index, ArcLink link) =>
            arcs._failed.ContainsKey(index)
            || (link.Stage == ActorChannelStage.Exporting && !WorldArcs.Relevant(World.All[index], player.RelevancePosition));

        public ArcLink Spawn(ReliableSender sender, int index, ArcLink link) =>
            link with { Stage = ActorChannelStage.Spawning, Key = sender.Queue(World.OpenBunch(World.All[index], link.Channel, sender.NextSequence(link.Channel))) };

        public ArcLink Name(ReliableSender sender, int index, ArcLink link) =>
            link with { Stage = ActorChannelStage.Naming, Key = sender.Queue(WorldArcs.ComponentExportBunch(World.All[index])) };

        public ArcLink Fill(ReliableSender sender, int index, ArcLink link) =>
            link with { Stage = ActorChannelStage.Open, Key = sender.Queue(World.AbilitySystemBunch(World.All[index], link.Channel)) };

        // The channel stays open while the ARC is relevant; a channel waiting for the client is left as it is.
        public ArcLink Update(ReliableSender sender, int index, ArcLink link, bool waiting)
        {
            WorldArc arc = World.All[index];
            if (waiting || WorldArcs.Relevant(arc, player.RelevancePosition))
            {
                return link;
            }

            return link with { Stage = ActorChannelStage.Closing, Key = sender.Queue(Bunches.Close(link.Channel, arc.Destroyed ? Bunches.Destroyed : Bunches.Relevancy)) };
        }

        public void Closed(int index, ArcLink link)
        {
        }
    }
}
