using Ares.Raid.Transport;
using Ares.Raid.World.Actors;
using Ares.Raid.World.Movement;
using Ares.Raid.World.Replication;
using Ares.Structs.Game.Movement;
using Ares.Structs.Game.Pendulum;
using Ares.Structs.Game.Replication;
using Ares.Structs.Game.World;

namespace Ares.Raid.World.Players;

/// <summary>
/// The extraction elevators as one player sees them: each elevator's state and buttons on its own channel, the
/// player's calls, and the player's descend hold.
/// </summary>
internal sealed class PlayerElevators : IPlayerFamily
{
    private readonly ChannelRange _channels;
    private Dictionary<int, (ElevatorChannel State, ReliableKey[] Keys)> _elevators = [];
    private Dictionary<int, ExtractionPointState> _states = [];
    private HashSet<int> _failed = [];
    private BoundedLog _requests = new(PlayerChannels.InteractionRequestLog);
    private BoundedLog _changes = new(PlayerChannels.InteractionRequestLog);

    // This player's descend hold: the elevator, the switch (index in the elevator's buttons), the input frame of the
    // press and its server world time.
    private (ExtractionPoint Point, int Button, uint Frame, double PressedAt)? _descent;

    /// <summary>Creates the elevators of a connection that has none yet.</summary>
    /// <param name="channels">The elevators' channels, one each.</param>
    public PlayerElevators(ChannelRange channels) => _channels = channels;

    private enum ElevatorChannel
    {
        Closed,
        Exporting,
        Opening,
        Open,
    }

    /// <summary>Extraction elevators whose state and buttons this client has.</summary>
    public int Ready => _elevators.Values.Count(elevator => elevator.State == ElevatorChannel.Open);

    /// <summary>
    /// Elevator states this client has, by elevator index: the state (Called while the call press runs) and the
    /// server world time it started, with the descend switch held (+switch@press, ~hold@hold start once the hold runs)
    /// or, once the elevator left, the one that sent it (^switch).
    /// </summary>
    public string States => string.Join(' ', _states.OrderBy(entry => entry.Key).Select(entry => Describe(entry.Key, entry.Value)));

    /// <summary>Calls of an elevator by this player.</summary>
    public int Calls { get; private set; }

    /// <summary>Elevator state changes this client got, oldest first, as elevator:state@server time (see <see cref="States"/>).</summary>
    public IReadOnlyList<string> Changes => _changes;

    /// <summary>Descend holds started by this player.</summary>
    public int Descents { get; private set; }

    /// <summary>Descend holds this player stopped by moving.</summary>
    public int CancelledDescents { get; private set; }

    /// <summary>Descend holds of this player that sent an elevator off.</summary>
    public int CompletedDescents { get; private set; }

    /// <summary>Requests received on elevator buttons, oldest first, as elevator/button request@input frame.</summary>
    public IReadOnlyList<string> Requests => _requests;

    /// <summary>Whether this player holds a descend switch now.</summary>
    public bool Descending => _descent is not null;

    /// <summary>A copy for speculative packet processing.</summary>
    /// <returns>The copy.</returns>
    public PlayerElevators Copy()
    {
        PlayerElevators copy = (PlayerElevators)MemberwiseClone();
        copy._elevators = new Dictionary<int, (ElevatorChannel, ReliableKey[])>(_elevators);
        copy._states = new Dictionary<int, ExtractionPointState>(_states);
        copy._failed = [.. _failed];
        copy._requests = _requests.Copy();
        copy._changes = _changes.Copy();
        return copy;
    }

    /// <inheritdoc />
    public bool ChannelFailed(IPlayerChannelsContext player, int channel)
    {
        if (_channels.IndexOf(channel) is not { } elevator)
        {
            return false;
        }

        _failed.Add(elevator);
        return true;
    }

    /// <inheritdoc />
    public bool TryHandle(IPlayerChannelsContext player, InteractionRequest request, bool repeated, double now)
    {
        RaidWorld raid = player.Raid;
        if (raid.Extraction.ByButton(request.Target) is not { } elevator)
        {
            return false;
        }

        if (!repeated)
        {
            _requests.Add($"{elevator.Point.Name}/{elevator.Button.Component} {request.Request}@{request.Frame}");
        }

        // A press on a closed elevator's call button calls it. The client runs the press itself (the button
        // is focused and usable in its pawn frames); no frame may reset it until its ending has played.
        // A press on a descend switch of a ready elevator starts its hold, which the client runs the same way
        // (lining up, then holding); the server ends it.
        if ((request.Request & PendulumInput.PressRequest) != 0 && Callable(player, request.Target) is { } called
            && raid.ExtractionStates.Call(called.Index, player.Member.Id, raid.WorldSeconds(now)))
        {
            Calls++;
            player.ExtendFrameHold(now + ExtractionStates.CallSeconds + PlayerChannels.BreachEndingSeconds);
        }
        else if ((request.Request & PendulumInput.PressRequest) != 0 && _descent is null && Descendable(player, request.Target) is { } descend
            && raid.ExtractionStates.Descend(descend.Point.Index, descend.Button, player.Member.Id, raid.WorldSeconds(now)))
        {
            Descents++;
            _descent = (descend.Point, descend.Button, request.Frame, raid.WorldSeconds(now));
        }

        return true;
    }

    /// <inheritdoc />
    public bool Usable(IPlayerChannelsContext player, ulong target) => Callable(player, target) is not null || Descendable(player, target) is not null;

    /// <summary>
    /// A descend hold is a hold that movement input cancels, like a breach (the client stops it on its side and sends
    /// no request): the server watches the player's movement axes after the press frame.
    /// </summary>
    /// <param name="player">The player's channels.</param>
    public void CancelDescentOnMovement(IPlayerChannelsContext player)
    {
        if (_descent is not { } descent || !player.MovedSince(descent.Frame) || !player.Raid.ExtractionStates.CancelDescent(descent.Point.Index, player.Member.Id))
        {
            return;
        }

        CancelledDescents++;
        _descent = null;
    }

    /// <summary>The player left with an elevator: their descend hold ends with it.</summary>
    public void Depart() => _descent = null;

    /// <inheritdoc />
    public void Service(IPlayerChannelsContext player, ReliableSender sender, double now)
    {
        // Gives the client each extraction elevator: exports its buttons by path, then opens its channel with the
        // raid's elevator state and the button states. The channel stays open and gets every later change.
        RaidWorld raid = player.Raid;
        raid.ExtractionStates.Advance(raid.WorldSeconds(now), raid.PlayersInside);
        ServiceDescent(player, sender, now);
        if (!player.MapActorsAllowed(now))
        {
            return;
        }

        foreach (ExtractionPoint point in raid.Extraction.Points)
        {
            (ElevatorChannel state, ReliableKey[] keys) = _elevators.GetValueOrDefault(point.Index, (ElevatorChannel.Closed, []));
            if (Array.Exists(keys, sender.Pending))
            {
                continue;
            }

            int channel = _channels.Channel(point.Index);
            ExtractionPointState value = raid.ExtractionStates.Get(point.Index);
            switch (state)
            {
                case ElevatorChannel.Closed when !_failed.Contains(point.Index):
                    (state, keys) = (ElevatorChannel.Exporting, [.. raid.Extraction.ExportBunches(point).Select(sender.Queue)]);
                    break;
                case ElevatorChannel.Exporting:
                    (state, keys) = (ElevatorChannel.Opening, [sender.Queue(raid.Extraction.OpenBunch(point, channel, sender.NextSequence(channel), value,
                        raid.WorldSeconds(now)))]);
                    _states[point.Index] = value;
                    LogChange(point.Index, value);
                    break;
                case ElevatorChannel.Opening:
                    (state, keys) = (ElevatorChannel.Open, []);
                    break;
                case ElevatorChannel.Open when value != _states[point.Index]:
                    keys = raid.Extraction.UpdateBunch(point, channel, _states[point.Index], value) is { } update ? [sender.Queue(update)] : [];
                    _states[point.Index] = value;
                    LogChange(point.Index, value);
                    break;
            }

            _elevators[point.Index] = (state, keys);
        }
    }

    private static string Describe(int index, ExtractionPointState state)
    {
        string text = $"{index}:{(state.CalledAt is null ? state.State.ToString() : "Called")}" + (state.ChangedAt is { } at ? $"@{PlayerChannels.Seconds(at)}" : string.Empty);
        if (state.Descent is { } descent)
        {
            text += $"+{DamExtraction.Buttons[descent.Button].Component}@{PlayerChannels.Seconds(descent.PressedAt)}"
                + (descent.Holding ? $"~hold@{PlayerChannels.Seconds(descent.HoldStartsAt)}" : string.Empty);
        }
        else if (state.Departed is { } departed && state.State != ExtractionState.Ready)
        {
            text += $"^{DamExtraction.Buttons[departed.Button].Component}";
        }

        return text;
    }

    // Ends this player's descend hold once the elevator moved on. When the hold sent the elevator off, the character's
    // interactor gets the succeeded hold (the switch's animation set) and the end of the interaction, and no pawn frame
    // is sent until its ending has played; otherwise (the elevator left on its own) the hold just stops.
    private void ServiceDescent(IPlayerChannelsContext player, ReliableSender sender, double now)
    {
        if (_descent is not { } descent)
        {
            return;
        }

        RaidWorld raid = player.Raid;
        ExtractionPointState state = raid.ExtractionStates.Get(descent.Point.Index);
        if (state.Descent?.Player == player.Member.Id)
        {
            return;
        }

        if (state.Departed is { } departed && departed.Player == player.Member.Id && departed.PressedAt.Equals(descent.PressedAt))
        {
            if (!player.InteractorExported)
            {
                return;
            }

            ulong button = descent.Point.Buttons[descent.Button];
            player.RunInteraction(new PawnInteraction(button, LastInteractionEvent.Succeeded, departed.PressedAt, departed.PressedAt, departed.HoldStartsAt,
                player.NextInteractionSequence(), Animation: raid.Extraction.Animation(descent.Button)));
            sender.Queue(Bunches.Actor(ChannelLayout.Pawn, raid.State.InteractorCall(player.Player, FunctionNames.ClientEndInteractionSuccessfullyImplementation, button,
                DamExtraction.Buttons[descent.Button].Class).Write));
            CompletedDescents++;
        }

        _descent = null;
        player.ExtendFrameHold(now + PlayerChannels.BreachEndingSeconds);
    }

    private void LogChange(int index, ExtractionPointState state)
    {
        string entry = Describe(index, state);
        if (_changes.Count == 0 || _changes[^1] != entry)
        {
            _changes.Add(entry);
        }
    }

    // The elevator of a call button this player may press: possessed, the elevator on this connection, closed and
    // not called yet.
    private ExtractionPoint? Callable(IPlayerChannelsContext player, ulong button) =>
        player.Phase == DeliveryPhase.Possessed && player.Raid.Extraction.ByButton(button) is ({ } point, { Role: ExtractionButtonRole.Summon })
        && _elevators.GetValueOrDefault(point.Index).State == ElevatorChannel.Open
        && player.Raid.ExtractionStates.Get(point.Index) is { State: ExtractionState.Closed, CalledAt: null } ? point : null;

    // The elevator and switch of a descend switch this player may press: possessed, the elevator on this connection,
    // ready and nobody holding a switch.
    private (ExtractionPoint Point, int Button)? Descendable(IPlayerChannelsContext player, ulong button) =>
        player.Phase == DeliveryPhase.Possessed && player.Raid.Extraction.ByButton(button) is ({ } point, { Role: ExtractionButtonRole.Descend })
        && _elevators.GetValueOrDefault(point.Index).State == ElevatorChannel.Open
        && player.Raid.ExtractionStates.Get(point.Index) is { State: ExtractionState.Ready, Descent: null } ? (point, point.ButtonIndex(button)) : null;
}
