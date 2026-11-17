using Ares.Raid.Transport;
using Ares.Raid.World.Actors;
using Ares.Raid.World.Movement;
using Ares.Raid.World.Replication;
using Ares.Structs.Game.Pendulum;
using Ares.Structs.Game.World;

namespace Ares.Raid.World.Players;

/// <summary>
/// The map doors as one player sees them: the doors of the cells the client reports visible, with their state
/// changes, and the player's presses and breaches. The doors come last among the families taking interaction
/// requests: they take every request the others left.
/// </summary>
internal sealed class PlayerDoors : IPlayerFamily
{
    private const int MaxDoorsInFlight = 8;

    private readonly ChannelRange _channels;
    private Dictionary<int, (DoorChannel State, ReliableKey? Key)> _doors = [];
    private Dictionary<int, int> _revisions = [];
    private Dictionary<int, ReliableKey> _updates = [];
    private HashSet<int> _failed = [];
    private Dictionary<int, int> _breachRequests = [];
    private (int Door, uint Checked)? _breach;

    /// <summary>Creates the doors of a connection that has none yet.</summary>
    /// <param name="channels">The doors' channels, one each.</param>
    public PlayerDoors(ChannelRange channels) => _channels = channels;

    private enum DoorChannel
    {
        Closed,
        Exporting,
        Opening,
        Open,
        Closing,
    }

    /// <summary>Door presses applied.</summary>
    public int Presses { get; private set; }

    /// <summary>Door requests ignored (not a press, door not open on this connection, or still moving).</summary>
    public int IgnoredRequests { get; private set; }

    /// <summary>Door breaches started.</summary>
    public int Breaches { get; private set; }

    /// <summary>Door breaches this player stopped by moving.</summary>
    public int CancelledBreaches { get; private set; }

    /// <summary>Request values received on breach buttons, with their counts (a hold sends more than a press).</summary>
    public IReadOnlyDictionary<int, int> BreachRequests => _breachRequests;

    /// <summary>Whether this player breaches a door now.</summary>
    public bool Breaching => _breach is not null;

    /// <summary>A copy for speculative packet processing.</summary>
    /// <returns>The copy.</returns>
    public PlayerDoors Copy()
    {
        PlayerDoors copy = (PlayerDoors)MemberwiseClone();
        copy._doors = new Dictionary<int, (DoorChannel, ReliableKey?)>(_doors);
        copy._revisions = new Dictionary<int, int>(_revisions);
        copy._updates = new Dictionary<int, ReliableKey>(_updates);
        copy._failed = [.. _failed];
        copy._breachRequests = new Dictionary<int, int>(_breachRequests);
        return copy;
    }

    /// <inheritdoc />
    public bool ChannelFailed(int channel)
    {
        if (_channels.IndexOf(channel) is not { } index)
        {
            return false;
        }

        _failed.Add(index);
        return true;
    }

    /// <inheritdoc />
    public bool TryHandle(IPlayerChannelsContext player, InteractionRequest request, bool repeated, double now)
    {
        RaidWorld raid = player.Raid;
        if (raid.Doors.ByBreach(request.Target) is not null)
        {
            _breachRequests[request.Request] = _breachRequests.GetValueOrDefault(request.Request) + 1;
        }

        // The request is a set of flags; a press has the press flag, with the take-out-raider-tool flag when
        // the breach is pressed with the tool holstered. Other flags are not decoded yet.
        if ((request.Request & PendulumInput.PressRequest) == 0)
        {
            IgnoredRequests++;
        }
        else if (UsableDoor(player, request.Target) is { } door && raid.DoorStates.Press(door.Index, now))
        {
            Presses++;
        }
        else if (BreachableDoor(player, request.Target) is { } barricaded && raid.DoorStates.StartBreach(barricaded.Index, now))
        {
            Breaches++;
            _breach = (barricaded.Index, request.Frame);
        }
        else
        {
            IgnoredRequests++;
            if (!repeated)
            {
                player.LogUnhandled(request);
            }
        }

        return true;
    }

    /// <inheritdoc />
    public bool Usable(IPlayerChannelsContext player, ulong target) => UsableDoor(player, target) is not null || BreachableDoor(player, target) is not null;

    /// <summary>
    /// Watches the breaching player's movement axes after the start frame: the breach is a hold that movement input
    /// cancels (the client stops it on its side and sends no request when it does).
    /// </summary>
    /// <param name="player">The player's channels.</param>
    /// <param name="now">Raid clock.</param>
    public void CancelBreachOnMovement(IPlayerChannelsContext player, double now)
    {
        if (_breach is not { } breach)
        {
            return;
        }

        if (!player.Raid.DoorStates.Breaching(breach.Door))
        {
            _breach = null;
            player.HoldFrames(now + PlayerChannels.BreachEndingSeconds);
            return;
        }

        if (player.MovedSince(breach.Checked))
        {
            player.Raid.DoorStates.CancelBreach(breach.Door);
            CancelledBreaches++;
            _breach = null;
            return;
        }

        _breach = breach with { Checked = Math.Max(breach.Checked, player.InputFrame ?? breach.Checked) };
    }

    /// <inheritdoc />
    public void Service(IPlayerChannelsContext player, ReliableSender sender, double now)
    {
        // Replicates the doors of the cells the client reports visible: exports each door's path, opens its
        // channel with usable buttons, sends state changes, and closes the channel when the cell unloads.
        if (!player.MapActorsAllowed(now))
        {
            return;
        }

        RaidWorld raid = player.Raid;
        raid.DoorStates.Advance(now);
        int inFlight = _doors.Values.Count(door => door.Key is { } key && sender.Pending(key));
        foreach (Door door in raid.Doors.Doors)
        {
            (DoorChannel state, ReliableKey? key) = _doors.GetValueOrDefault(door.Index, (DoorChannel.Closed, null));
            if (key is { } waiting && sender.Pending(waiting))
            {
                continue;
            }

            int channel = _channels.Channel(door.Index);
            (DoorState value, int revision, _) = raid.DoorStates.Get(door.Index);
            switch (state)
            {
                case DoorChannel.Exporting:
                    (state, key) = (DoorChannel.Opening, sender.Queue(raid.Doors.OpenBunch(door, channel, sender.NextSequence(channel), raid.DoorStates.Buttons(door.Index), value)));
                    _revisions[door.Index] = revision;
                    break;
                case DoorChannel.Opening:
                    (state, key) = (DoorChannel.Open, null);
                    break;
                case DoorChannel.Closing:
                    (state, key) = (DoorChannel.Closed, null);
                    break;
            }

            bool wanted = player.LevelVisible(door.Cell) && !_failed.Contains(door.Index);
            switch (state)
            {
                case DoorChannel.Closed when wanted && inFlight < MaxDoorsInFlight:
                    (state, key) = (DoorChannel.Exporting, sender.Queue(raid.Doors.ExportBunch(door)));
                    inFlight++;
                    break;
                case DoorChannel.Open when !wanted:
                    (state, key) = (DoorChannel.Closing, sender.Queue(Bunches.Close(channel, Bunches.LevelUnloaded)));
                    _updates.Remove(door.Index);
                    break;
                case DoorChannel.Open when revision != _revisions.GetValueOrDefault(door.Index)
                    && (!_updates.TryGetValue(door.Index, out ReliableKey update) || !sender.Pending(update)):
                    _updates[door.Index] = sender.Queue(raid.Doors.UpdateBunch(door, channel, value, raid.DoorStates.Buttons(door.Index)));
                    _revisions[door.Index] = revision;
                    break;
            }

            _doors[door.Index] = (state, key);
        }
    }

    // The door of a button the client may use: possessed, door replicated on this connection, cell visible,
    // and the door not barricaded, locked or without power.
    private Door? UsableDoor(IPlayerChannelsContext player, ulong button) =>
        player.Raid.Doors.ByButton(button) is { } door && Replicated(player, door) && player.Raid.DoorStates.Usable(door.Index) ? door : null;

    // The door of a breach button the client may use: replicated as above and still barricaded.
    private Door? BreachableDoor(IPlayerChannelsContext player, ulong button) =>
        player.Raid.Doors.ByBreach(button) is { } door && Replicated(player, door) && player.Raid.DoorStates.Breachable(door.Index) ? door : null;

    private bool Replicated(IPlayerChannelsContext player, Door door) =>
        player.Phase == DeliveryPhase.Possessed && !_failed.Contains(door.Index)
        && _doors.GetValueOrDefault(door.Index).State == DoorChannel.Open && player.LevelVisible(door.Cell);
}
