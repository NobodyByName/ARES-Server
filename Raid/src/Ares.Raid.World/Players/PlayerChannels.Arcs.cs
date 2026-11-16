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
    /// <summary>Channels that carry ARCs for this client; an ARC holds one while the client has it.</summary>
    private const int ArcChannels = 16;

    private Dictionary<int, ArcLink> _arcLinks = [];
    private Dictionary<ulong, ReliableKey> _arcExports = [];
    private Dictionary<int, ArcStage> _failedArcs = [];
    private double _arcRelevantAt;

    // Where an ARC's channel stands: the classes are exported, the actor spawned with its attribute sets, its ability
    // system named, then filled in; the channel then stays open until the ARC is despawned or no longer relevant.
    private enum ArcStage
    {
        Exporting,
        Spawning,
        Naming,
        Open,
        Closing,
    }

    /// <summary>ARCs whose actor and ability system this client has.</summary>
    public int ArcsReady => _arcLinks.Values.Count(link => link.Stage == ArcStage.Open);

    /// <summary>Whether this client has an ARC.</summary>
    /// <param name="arc">ARC index.</param>
    /// <returns>True from the ability system's content until the channel closes.</returns>
    public bool HasArc(int arc) => _arcLinks.TryGetValue(arc, out ArcLink link) && link.Stage == ArcStage.Open;

    /// <summary>ARC channels open now (or on the way).</summary>
    public int ArcChannelsInUse => _arcLinks.Count;

    /// <summary>ARCs whose channel the client refused, as index@stage.</summary>
    public IReadOnlyList<string> ArcFailures => [.. _failedArcs.OrderBy(entry => entry.Key).Select(entry => $"{entry.Key}@{entry.Value}")];

    /// <summary>What happened to the raid's ARCs, oldest first.</summary>
    public IReadOnlyList<string> ArcEvents => _raid.Arcs.Events;

    // The first ARC channel: after the pickup channels.
    private int FirstArcChannel => FirstPickupChannel + PickupChannels;

    private void CopyArcs(PlayerChannels copy)
    {
        copy._arcLinks = new Dictionary<int, ArcLink>(_arcLinks);
        copy._arcExports = new Dictionary<ulong, ReliableKey>(_arcExports);
        copy._failedArcs = new Dictionary<int, ArcStage>(_failedArcs);
    }

    private int? ArcIndex(int channel) =>
        channel - FirstArcChannel is var index and >= 0 and < ArcChannels ? index : null;

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

        ServiceArcLinks(sender);
        if (now < _arcRelevantAt)
        {
            return;
        }

        _arcRelevantAt = now + LootRelevanceInterval;
        foreach (WorldArc arc in _raid.Arcs.All)
        {
            if (_arcLinks.ContainsKey(arc.Index) || _failedArcs.ContainsKey(arc.Index) || !WorldArcs.Relevant(arc, LootPosition)
                || FreeArcChannel() is not { } channel)
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

            _arcLinks[arc.Index] = new ArcLink(ArcStage.Exporting, channel, null);
        }

        ServiceArcLinks(sender);
    }

    // Moves each ARC channel on by the steps whose bunches the client has.
    private void ServiceArcLinks(ReliableSender sender)
    {
        foreach ((int index, ArcLink current) in _arcLinks.ToArray())
        {
            ArcLink link = current;
            WorldArc arc = _raid.Arcs.All[index];
            bool waiting = link.Stage == ArcStage.Exporting
                ? _raid.Arcs.Roots(arc).Any(root => sender.Pending(_arcExports[root]))
                : link.Key is { } key && sender.Pending(key);
            if (waiting)
            {
                continue;
            }

            bool relevant = WorldArcs.Relevant(arc, LootPosition);
            if (_failedArcs.ContainsKey(index) || (link.Stage == ArcStage.Exporting && !relevant))
            {
                // The client refused the channel, or the ARC went away before it opened: nothing more goes to it.
                _arcLinks.Remove(index);
                continue;
            }

            switch (link.Stage)
            {
                case ArcStage.Exporting:
                    link = link with { Stage = ArcStage.Spawning, Key = sender.Queue(_raid.Arcs.OpenBunch(arc, link.Channel, sender.NextSequence(link.Channel))) };
                    break;
                case ArcStage.Spawning:
                    link = link with { Stage = ArcStage.Naming, Key = sender.Queue(WorldArcs.ComponentExportBunch(arc)) };
                    break;
                case ArcStage.Naming:
                    link = link with { Stage = ArcStage.Open, Key = sender.Queue(_raid.Arcs.AbilitySystemBunch(arc, link.Channel)) };
                    break;
                case ArcStage.Open when !relevant:
                    link = link with { Stage = ArcStage.Closing, Key = sender.Queue(Bunches.Close(link.Channel, arc.Destroyed ? Bunches.Destroyed : Bunches.Relevancy)) };
                    break;
                case ArcStage.Open:
                    break;
                case ArcStage.Closing:
                    _arcLinks.Remove(index);
                    continue;
            }

            _arcLinks[index] = link;
        }
    }

    // An ARC channel no ARC uses.
    private int? FreeArcChannel()
    {
        for (int channel = FirstArcChannel; channel < FirstArcChannel + ArcChannels; channel++)
        {
            if (_arcLinks.Values.All(link => link.Channel != channel))
            {
                return channel;
            }
        }

        return null;
    }

    // An ARC's channel on this connection: its stage and the bunch it waits for.
    private readonly record struct ArcLink(ArcStage Stage, int Channel, ReliableKey? Key);
}
