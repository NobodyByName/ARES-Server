using Ares.Common.Game;
using Ares.Common.GameData;
using Ares.Structs.Game.World;

namespace Ares.Raid.World.Actors;

/// <summary>Players inside an elevator when it left (its doors closed).</summary>
/// <param name="At">Server world time the doors started closing, in seconds.</param>
/// <param name="Players">User ids of the players inside its extraction zone once the doors closed.</param>
public sealed record ExtractionDeparture(double At, IReadOnlyList<long> Players);

/// <summary>
/// Elevator state of one raid, shared by its connections. Every elevator starts closed with only its call button
/// usable. A press on the call button of a closed elevator calls it: once the press has run (the button's alignment
/// and the settings' summon hold) the elevator arrives, and it is ready (doors open, descend switches usable) the raid
/// settings' extraction ready time later. A held descend switch (its alignment, then the settings' extraction hold)
/// closes the doors; so does the end of the raid settings' extraction time with nobody descending. The elevator has left
/// (closed, callable again) once the doors have closed.
/// </summary>
public sealed class ExtractionStates
{
    private readonly MapData _map;
    private readonly RaidSettings _raid;
    private readonly ExtractionPointState[] _states;
    private readonly ExtractionDeparture?[] _departures;

    /// <summary>Sets each elevator to its start state.</summary>
    /// <param name="map">The map's data: its elevator buttons and what its elevators share.</param>
    /// <param name="raid">The raid settings: the extraction ready and extraction times.</param>
    /// <param name="count">Number of elevators.</param>
    public ExtractionStates(MapData map, RaidSettings raid, int count)
    {
        _map = map ?? throw new ArgumentNullException(nameof(map));
        _raid = raid ?? throw new ArgumentNullException(nameof(raid));
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        ExtractionButton summon = map.ExtractionButtons.Single(button => button.Role == ExtractionButtonRole.Summon);
        CallSeconds = summon.AlignmentSeconds + map.ExtractionElevator.Settings.SummonHoldSeconds;
        _states = [.. Enumerable.Repeat(new ExtractionPointState(ExtractionState.Closed, null, null), count)];
        _departures = new ExtractionDeparture?[count];
    }

    /// <summary>Seconds from a press on the call button to the elevator's arrival.</summary>
    public double CallSeconds { get; }

    /// <summary>An elevator's state.</summary>
    /// <param name="index">Elevator index.</param>
    /// <returns>The state.</returns>
    public ExtractionPointState Get(int index) => _states[index];

    /// <summary>States of an elevator's buttons, in <see cref="MapData.ExtractionButtons"/> order.</summary>
    /// <param name="index">Elevator index.</param>
    /// <returns>The button states.</returns>
    public IReadOnlyList<CurrentInteractionState> Buttons(int index)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, _states.Length);
        return _states[index].Buttons(_map);
    }

    /// <summary>The players inside an elevator when it last left.</summary>
    /// <param name="index">Elevator index.</param>
    /// <returns>The departure, or null before the elevator first left.</returns>
    public ExtractionDeparture? Departure(int index) => _departures[index];

    /// <summary>Calls a closed elevator; ignored while it is not closed or its call already runs.</summary>
    /// <param name="index">Elevator index.</param>
    /// <param name="player">User id of the caller.</param>
    /// <param name="at">Server world time of the press, in seconds.</param>
    /// <returns>Whether the call was applied.</returns>
    public bool Call(int index, long player, double at)
    {
        ExtractionPointState state = _states[index];
        if (state.State != ExtractionState.Closed || state.CalledAt is not null)
        {
            return false;
        }

        _states[index] = state with { CalledBy = player, CalledAt = at };
        return true;
    }

    /// <summary>Starts a hold on a descend switch of a ready elevator; ignored while another hold runs.</summary>
    /// <param name="index">Elevator index.</param>
    /// <param name="button">The switch, as an index in <see cref="MapData.ExtractionButtons"/>.</param>
    /// <param name="player">User id of the player.</param>
    /// <param name="at">Server world time of the press, in seconds.</param>
    /// <returns>Whether the hold started.</returns>
    public bool Descend(int index, int button, long player, double at)
    {
        ExtractionPointState state = _states[index];
        if (state.State != ExtractionState.Ready || state.Descent is not null || _map.ExtractionButtons[button].Role != ExtractionButtonRole.Descend)
        {
            return false;
        }

        _states[index] = state with { Descent = new ExtractionDescent(button, player, at), HoldRan = true };
        return true;
    }

    /// <summary>Stops a player's descend hold before it succeeded; the switch is usable again.</summary>
    /// <param name="index">Elevator index.</param>
    /// <param name="player">User id of the player.</param>
    /// <returns>Whether a hold of that player stopped.</returns>
    public bool CancelDescent(int index, long player)
    {
        ExtractionPointState state = _states[index];
        if (state.Descent?.Player != player)
        {
            return false;
        }

        _states[index] = state with { Descent = null };
        return true;
    }

    /// <summary>
    /// Moves elevators on to where the time puts them, each change at its exact time: called ones arrive once the call
    /// press has run and are ready after the arrival time; a ready one starts closing when a descend hold succeeds or
    /// its extraction time is over, whichever comes first, and has left once its doors have closed.
    /// </summary>
    /// <param name="at">Server world time, in seconds.</param>
    /// <param name="inside">User ids of the players inside an elevator's extraction zone, by elevator index; asked when its
    /// doors have closed.</param>
    public void Advance(double at, Func<int, IReadOnlyList<long>>? inside = null)
    {
        for (int index = 0; index < _states.Length; index++)
        {
            ExtractionPointState state = _states[index];
            if (state is { State: ExtractionState.Closed, CalledAt: { } called } && at >= called + CallSeconds)
            {
                state = state with { State = ExtractionState.Arriving, ChangedAt = called + CallSeconds, CalledAt = null, ReadyAt = null, ClosingAt = null };
            }

            if (state is { State: ExtractionState.Arriving, ChangedAt: { } arriving } && at >= arriving + _raid.ExtractionReadySeconds)
            {
                state = state with { State = ExtractionState.Ready, ChangedAt = arriving + _raid.ExtractionReadySeconds, Departed = null };
            }

            if (state is { State: ExtractionState.Ready, ChangedAt: { } ready })
            {
                double timeout = ready + _raid.ExtractionSeconds;
                ExtractionDescent? descent = state.Descent;
                double closing = descent is { } held && held.EndsAt(_map) < timeout ? held.EndsAt(_map) : timeout;
                if (at >= closing)
                {
                    state = state with
                    {
                        State = ExtractionState.Closing,
                        ChangedAt = closing,
                        ReadyAt = ready,
                        ClosingAt = closing,
                        Descent = null,
                        Departed = descent is { } done && done.EndsAt(_map).Equals(closing) ? done : null,
                    };
                }
                else if (descent is { Holding: false } aligning && at >= aligning.HoldStartsAt(_map))
                {
                    state = state with { Descent = aligning with { Holding = true } };
                }
            }

            if (state is { State: ExtractionState.Closing, ChangedAt: { } closingAt } && at >= closingAt + _map.ExtractionElevator.DoorsSeconds)
            {
                state = state with { State = ExtractionState.Closed, ChangedAt = closingAt + _map.ExtractionElevator.DoorsSeconds, CalledBy = null };
                _departures[index] = new ExtractionDeparture(closingAt, inside?.Invoke(index) ?? []);
            }

            _states[index] = state;
        }
    }
}
