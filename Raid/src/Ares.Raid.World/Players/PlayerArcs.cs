using Ares.Raid.Transport;
using Ares.Raid.World.Actors;
using Ares.Raid.World.Movement;
using Ares.Raid.World.Replication;
using Ares.Structs.Game.Pendulum;
using Ares.Structs.Net.Replication;

namespace Ares.Raid.World.Players;

/// <summary>
/// The raid's ARCs as one player sees them: each ARC relevant to the player is delivered on its own channel, which
/// stays open while the ARC is relevant and closes when it is despawned or out of range. A flying ARC's movement
/// follows as blackboard frames (<see cref="ArcFrames"/>).
/// </summary>
internal sealed class PlayerArcs : IPlayerFamily
{
    // Most unconfirmed frames kept per ARC; older ones are forgotten.
    private const int MaxFramesInFlight = 32;

    private static readonly IReadOnlyDictionary<int, PendulumSlot> s_noBaseline = new Dictionary<int, PendulumSlot>();

    private readonly ChannelRange _channels;

    // The channels that carry ARCs for this client; an ARC holds one while the client has it.
    private ActorChannelPool<ArcLink> _links;
    private Dictionary<ulong, ReliableKey> _exports = [];
    private Dictionary<int, ActorChannelStage> _failed = [];
    private double _relevantAt;
    private int _framesSent;
    private int _framesConfirmed;

    /// <summary>Creates the ARCs of a connection that has none yet.</summary>
    /// <param name="channels">The channels the ARCs share.</param>
    public PlayerArcs(ChannelRange channels)
    {
        _channels = channels;
        _links = new ActorChannelPool<ArcLink>(channels);
    }

    /// <summary>ARCs whose actor and ability system this client has.</summary>
    public int Ready => _links.Count(entry => entry.Value.Stage == ActorChannelStage.Open);

    /// <summary>Blackboard frames sent to this client after the ARCs' first ones, and how many of them it confirmed.</summary>
    public (int Sent, int Confirmed) Frames => (_framesSent, _framesConfirmed);

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
        RaidWorld raid = player.Raid;
        raid.Arcs.Advance(now);
        _links.Advance(sender, new ArcSteps(this, player, now));
        if (now < _relevantAt)
        {
            return;
        }

        _relevantAt = now + PlayerChannels.LootRelevanceInterval;
        foreach (WorldArc arc in raid.Arcs.All)
        {
            if (_links.Contains(arc.Index) || _failed.ContainsKey(arc.Index) || !raid.Arcs.Relevant(arc, player.RelevancePosition)
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

        _links.Advance(sender, new ArcSteps(this, player, now));
    }

    // An ARC's channel on this connection: its stage and the bunch it waits for, then its blackboard frames: the
    // number of the last one sent and when, the newest one the client confirmed (number and state; no state while a
    // reset is on its way), and the unreliable frames whose delivery is not known yet.
    private readonly record struct ArcLink(ActorChannelStage Stage, int Channel, ReliableKey? Key) : IActorChannelLink
    {
        public uint Sent { get; init; }

        public double SentAt { get; init; }

        public uint Baseline { get; init; }

        public IReadOnlyDictionary<int, PendulumSlot>? State { get; init; }

        public ArcFrame[] InFlight { get; init; } = [];
    }

    // An unreliable blackboard frame sent to this client.
    private readonly record struct ArcFrame(ReliableKey Key, uint Number, IReadOnlyDictionary<int, PendulumSlot> State);

    // What an ARC's channel sends: the classes are exported, the actor spawned with its attribute sets and first frame,
    // its ability system named, then filled in; the channel then stays open, carrying the ARC's frames, until the ARC
    // is despawned or no longer relevant.
    private readonly struct ArcSteps(PlayerArcs arcs, IPlayerChannelsContext player, double now) : IActorChannelSteps<ArcLink>
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
            || (link.Stage == ActorChannelStage.Exporting && !World.Relevant(World.All[index], player.RelevancePosition));

        // The first frame rides in the reliable open bunch, so it is the baseline once the channel is open.
        public ArcLink Spawn(ReliableSender sender, int index, ArcLink link)
        {
            WorldArc arc = World.All[index];
            IReadOnlyDictionary<int, PendulumSlot> state = WorldArcs.State(arc, World.Pose(arc));
            return link with
            {
                Stage = ActorChannelStage.Spawning,
                Key = sender.Queue(World.OpenBunch(arc, link.Channel, sender.NextSequence(link.Channel), state)),
                Sent = ArcFrames.First,
                SentAt = now,
                Baseline = ArcFrames.First,
                State = state,
            };
        }

        public ArcLink Name(ReliableSender sender, int index, ArcLink link) =>
            link with { Stage = ActorChannelStage.Naming, Key = sender.Queue(WorldArcs.ComponentExportBunch(World.All[index])) };

        public ArcLink Fill(ReliableSender sender, int index, ArcLink link) =>
            link with { Stage = ActorChannelStage.Open, Key = sender.Queue(World.AbilitySystemBunch(World.All[index], link.Channel)) };

        // The channel stays open while the ARC is relevant, sending its frames; a channel waiting for the client is left as it is.
        public ArcLink Update(ReliableSender sender, int index, ArcLink link, bool waiting)
        {
            WorldArc arc = World.All[index];
            if (waiting)
            {
                return link;
            }

            if (World.Relevant(arc, player.RelevancePosition))
            {
                return link.Stage == ActorChannelStage.Open ? SendFrame(sender, arc, link) : link;
            }

            return link with { Stage = ActorChannelStage.Closing, Key = sender.Queue(Bunches.Close(link.Channel, arc.Destroyed ? Bunches.Destroyed : Bunches.Relevancy)) };
        }

        // A flying ARC's next frame once its interval for this client's distance has passed, coded against the newest
        // frame the client confirmed; a reset when none is recent enough.
        private ArcLink SendFrame(ReliableSender sender, WorldArc arc, ArcLink link)
        {
            link = Confirmed(sender, link);
            ArcPose pose = World.Pose(arc);
            // A reset clears the client's frames: its outcome comes first.
            if (!pose.Flying || (link.State is null && link.InFlight.Length != 0)
                || now - link.SentAt < ArcFrames.Interval(ArcRoutes.Distance(pose.Location, player.RelevancePosition)))
            {
                return link;
            }

            IReadOnlyDictionary<int, PendulumSlot> state = WorldArcs.State(arc, pose);
            uint number = link.Sent + 1;
            bool reset = link.State is null || number - link.Baseline >= PendulumFrames.History;
            ActorContentBlock frame = reset
                ? WorldArcs.Frame(arc, 0, number, state, s_noBaseline)
                : WorldArcs.Frame(arc, link.Baseline, number, state, link.State!);
            ArcFrame sent = new(sender.QueueUnreliable(Bunches.Actor(link.Channel, frame)), number, state);
            arcs._framesSent++;
            return link with
            {
                Sent = number,
                SentAt = now,
                State = reset ? null : link.State,
                InFlight = reset ? [sent] : [.. link.InFlight.TakeLast(MaxFramesInFlight - 1), sent],
            };
        }

        // The link with its baseline moved to the newest frame the client confirmed; lost or forgotten frames are dropped.
        private ArcLink Confirmed(ReliableSender sender, ArcLink link)
        {
            ArcLink result = link;
            List<ArcFrame> waiting = [];
            foreach (ArcFrame frame in link.InFlight)
            {
                bool arrived = sender.Arrived(frame.Key) is true;
                arcs._framesConfirmed += arrived ? 1 : 0;
                if (arrived && (result.State is null || frame.Number > result.Baseline))
                {
                    result = result with { Baseline = frame.Number, State = frame.State };
                }
                else if (sender.Pending(frame.Key))
                {
                    waiting.Add(frame);
                }
            }

            return result with { InFlight = [.. waiting.Where(frame => frame.Number > result.Baseline)] };
        }

        public void Closed(int index, ArcLink link)
        {
        }
    }
}
