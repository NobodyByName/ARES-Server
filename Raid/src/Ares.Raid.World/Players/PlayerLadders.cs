using Ares.Raid.Transport;
using Ares.Raid.World.Actors;
using Ares.Raid.World.Movement;
using Ares.Raid.World.Replication;
using Ares.Structs.Game.Movement;
using Ares.Structs.Game.Pendulum;
using Ares.Structs.Game.World;

namespace Ares.Raid.World.Players;

/// <summary>
/// The map ladders as one player sees them: the climbable actor of each ladder in the cells the client reports
/// visible, and the player's climbs.
/// </summary>
internal sealed class PlayerLadders : IPlayerFamily
{
    private const int MaxLaddersInFlight = 8;

    private readonly ChannelRange _channels;
    private Dictionary<int, (LadderChannel State, ReliableKey? Key)> _ladders = [];
    private HashSet<int> _failed = [];

    /// <summary>Creates the ladders of a connection that has none yet.</summary>
    /// <param name="channels">The ladders' channels, one each.</param>
    public PlayerLadders(ChannelRange channels) => _channels = channels;

    private enum LadderChannel
    {
        Closed,
        Exporting,
        Spawning,
        NamingInteractions,
        EnablingInteractions,
        Open,
    }

    /// <summary>Ladders whose climbable actor and interactions this client has.</summary>
    public int Ready => _ladders.Values.Count(ladder => ladder.State == LadderChannel.Open);

    /// <summary>Ladders whose delivery started and has not finished.</summary>
    public int InProgress => _ladders.Values.Count(ladder => ladder.State is not (LadderChannel.Closed or LadderChannel.Open));

    /// <summary>A copy for speculative packet processing.</summary>
    /// <returns>The copy.</returns>
    public PlayerLadders Copy()
    {
        PlayerLadders copy = (PlayerLadders)MemberwiseClone();
        copy._ladders = new Dictionary<int, (LadderChannel, ReliableKey?)>(_ladders);
        copy._failed = [.. _failed];
        return copy;
    }

    /// <inheritdoc />
    public bool ChannelFailed(IPlayerChannelsContext player, int channel)
    {
        if (_channels.IndexOf(channel) is not { } ladder)
        {
            return false;
        }

        _failed.Add(ladder);
        return true;
    }

    /// <inheritdoc />
    public bool TryHandle(IPlayerChannelsContext player, InteractionRequest request, bool repeated, double now)
    {
        // The client climbs a ladder it presses, from the bottom or the top; from that frame the character simulation
        // climbs it. The request still goes on to the families after this one.
        if ((request.Request & PendulumInput.PressRequest) == 0 || repeated || ClimbableLadder(player.Raid, request.Target) is not { } ladder)
        {
            return false;
        }

        string component = LadderLogic.InteractionComponents[Enumerable.Range(0, ladder.Interactions.Count).First(index => ladder.Interactions[index] == request.Target)];
        player.Climb(request.Frame, ladder.Index, component == LadderParameters.TopEntryInteraction);
        return false;
    }

    /// <inheritdoc />
    public bool Usable(IPlayerChannelsContext player, ulong target) => false;

    /// <inheritdoc />
    public void Service(IPlayerChannelsContext player, ReliableSender sender, double now)
    {
        // Gives the client the climbable actor of each ladder in the cells it reports visible: exports the class
        // default and the placed ladder, spawns the actor attached to it, then names and enables its climb
        // interactions. The actors stay when their cell unloads, as the game's own ladder actors do.
        if (!player.MapActorsAllowed(now))
        {
            return;
        }

        RaidWorld raid = player.Raid;
        int inFlight = _ladders.Values.Count(ladder => ladder.Key is { } key && sender.Pending(key));
        foreach (Ladder ladder in raid.Ladders.Ladders)
        {
            (LadderChannel state, ReliableKey? key) = _ladders.GetValueOrDefault(ladder.Index, (LadderChannel.Closed, null));
            if (key is { } waiting && sender.Pending(waiting))
            {
                continue;
            }

            int channel = _channels.Channel(ladder.Index);
            switch (state)
            {
                case LadderChannel.Closed when inFlight < MaxLaddersInFlight && player.LevelVisible(ladder.Cell) && !_failed.Contains(ladder.Index):
                    (state, key) = (LadderChannel.Exporting, sender.Queue(raid.Ladders.ExportBunch(ladder)));
                    inFlight++;
                    break;
                case LadderChannel.Exporting:
                    (state, key) = (LadderChannel.Spawning, sender.Queue(raid.Ladders.OpenBunch(ladder, channel, sender.NextSequence(channel))));
                    break;
                case LadderChannel.Spawning:
                    (state, key) = (LadderChannel.NamingInteractions, sender.Queue(raid.Ladders.InteractionExportBunch(ladder)));
                    break;
                case LadderChannel.NamingInteractions:
                    (state, key) = (LadderChannel.EnablingInteractions, sender.Queue(raid.Ladders.InteractionBunch(ladder, channel)));
                    break;
                case LadderChannel.EnablingInteractions:
                    (state, key) = (LadderChannel.Open, null);
                    break;
            }

            _ladders[ladder.Index] = (state, key);
        }
    }

    // The ladder of a climb interaction the client has.
    private Ladder? ClimbableLadder(RaidWorld raid, ulong interaction) =>
        raid.Ladders.Ladders.FirstOrDefault(candidate => candidate.Interactions.Contains(interaction)) is { } ladder
        && _ladders.GetValueOrDefault(ladder.Index).State == LadderChannel.Open ? ladder : null;
}
