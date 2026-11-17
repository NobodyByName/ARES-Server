using Ares.Raid.Transport;
using Ares.Raid.World.Actors;
using Ares.Raid.World.Replication;

namespace Ares.Raid.World.Players;

/// <summary>
/// The raid's ARCs as this player sees them: each ARC relevant to the player is delivered on its own channel, which
/// stays open while the ARC is relevant and closes when it is despawned or out of range.
/// </summary>
public sealed partial class PlayerChannels
{
    // The channels that carry ARCs for this client; an ARC holds one while the client has it.
    private ActorChannelPool<ArcLink> _arcLinks;
    private Dictionary<ulong, ReliableKey> _arcExports = [];
    private Dictionary<int, ActorChannelStage> _failedArcs = [];
    private double _arcRelevantAt;

    /// <summary>ARCs whose actor and ability system this client has.</summary>
    public int ArcsReady => _arcLinks.Count(entry => entry.Value.Stage == ActorChannelStage.Open);

    /// <summary>Whether this client has an ARC.</summary>
    /// <param name="arc">ARC index.</param>
    /// <returns>True from the ability system's content until the channel closes.</returns>
    public bool HasArc(int arc) => _arcLinks.TryGet(arc, out ArcLink link) && link.Stage == ActorChannelStage.Open;

    /// <summary>ARC channels open now (or on the way).</summary>
    public int ArcChannelsInUse => _arcLinks.Count;

    /// <summary>ARCs whose channel the client refused, as index@stage.</summary>
    public IReadOnlyList<string> ArcFailures => [.. _failedArcs.OrderBy(entry => entry.Key).Select(entry => $"{entry.Key}@{entry.Value}")];

    /// <summary>What happened to the raid's ARCs, oldest first.</summary>
    public IReadOnlyList<string> ArcEvents => _raid.Arcs.Events;

    private void CopyArcs(PlayerChannels copy)
    {
        copy._arcLinks = _arcLinks.Copy();
        copy._arcExports = new Dictionary<ulong, ReliableKey>(_arcExports);
        copy._failedArcs = new Dictionary<int, ActorChannelStage>(_failedArcs);
    }

    private void ArcChannelFailed(int channel)
    {
        foreach ((int arc, ArcLink link) in _arcLinks)
        {
            if (link.Channel == channel)
            {
                _failedArcs.TryAdd(arc, link.Stage);
            }
        }
    }

    private void ServiceArcs(ReliableSender sender, double now)
    {
        if (!MapActorsAllowed(now))
        {
            return;
        }

        _arcLinks.Advance(sender, new ArcSteps(this));
        if (now < _arcRelevantAt)
        {
            return;
        }

        _arcRelevantAt = now + LootRelevanceInterval;
        foreach (WorldArc arc in _raid.Arcs.All)
        {
            if (_arcLinks.Contains(arc.Index) || _failedArcs.ContainsKey(arc.Index) || !WorldArcs.Relevant(arc, LootPosition)
                || _arcLinks.FreeChannel() is not { } channel)
            {
                continue;
            }

            // The class default and the attribute set classes go once per client.
            ulong[] missing = [.. _raid.Arcs.Roots(arc).Where(root => !_arcExports.ContainsKey(root))];
            if (missing.Length > 0)
            {
                ReliableKey key = sender.Queue(_raid.Arcs.ExportBunch(missing));
                foreach (ulong root in missing)
                {
                    _arcExports[root] = key;
                }
            }

            _arcLinks.Set(arc.Index, new ArcLink(ActorChannelStage.Exporting, channel, null));
        }

        _arcLinks.Advance(sender, new ArcSteps(this));
    }

    // An ARC's channel on this connection: its stage and the bunch it waits for.
    private readonly record struct ArcLink(ActorChannelStage Stage, int Channel, ReliableKey? Key) : IActorChannelLink;

    // What an ARC's channel sends: the classes are exported, the actor spawned with its attribute sets, its ability
    // system named, then filled in; the channel then stays open until the ARC is despawned or no longer relevant.
    private readonly struct ArcSteps(PlayerChannels channels) : IActorChannelSteps<ArcLink>
    {
        public bool Waits(ReliableSender sender, int index, ArcLink link)
        {
            if (link.Stage != ActorChannelStage.Exporting)
            {
                return link.Key is { } key && sender.Pending(key);
            }

            Dictionary<ulong, ReliableKey> exports = channels._arcExports;
            return channels._raid.Arcs.Roots(channels._raid.Arcs.All[index]).Any(root => sender.Pending(exports[root]));
        }

        // The client refused the channel, or the ARC went away before it opened: nothing more goes to it.
        public bool Abandons(int index, ArcLink link) =>
            channels._failedArcs.ContainsKey(index)
            || (link.Stage == ActorChannelStage.Exporting && !WorldArcs.Relevant(channels._raid.Arcs.All[index], channels.LootPosition));

        public ArcLink Spawn(ReliableSender sender, int index, ArcLink link) =>
            link with { Stage = ActorChannelStage.Spawning, Key = sender.Queue(channels._raid.Arcs.OpenBunch(channels._raid.Arcs.All[index], link.Channel, sender.NextSequence(link.Channel))) };

        public ArcLink Name(ReliableSender sender, int index, ArcLink link) =>
            link with { Stage = ActorChannelStage.Naming, Key = sender.Queue(WorldArcs.ComponentExportBunch(channels._raid.Arcs.All[index])) };

        public ArcLink Fill(ReliableSender sender, int index, ArcLink link) =>
            link with { Stage = ActorChannelStage.Open, Key = sender.Queue(channels._raid.Arcs.AbilitySystemBunch(channels._raid.Arcs.All[index], link.Channel)) };

        // The channel stays open while the ARC is relevant; a channel waiting for the client is left as it is.
        public ArcLink Update(ReliableSender sender, int index, ArcLink link, bool waiting)
        {
            WorldArc arc = channels._raid.Arcs.All[index];
            if (waiting || WorldArcs.Relevant(arc, channels.LootPosition))
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
