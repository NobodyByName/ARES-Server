using Ares.Common.GameData;
using Ares.Structs.Game.World;

namespace Ares.Raid.World.Actors;

/// <summary>An elevator's extraction timer (<c>FExtractionInfo</c>).</summary>
/// <param name="StartedAt">Server world time the extraction became possible, in seconds; the settings' "no extraction"
/// time once the elevator has left.</param>
/// <param name="Seconds">Seconds from that time until the elevator leaves (its doors closed).</param>
public readonly record struct ExtractionTimer(double StartedAt, double Seconds);

/// <summary>A descend switch hold on a ready elevator.</summary>
/// <param name="Button">The switch, as an index in the map's elevator buttons (<see cref="MapData.ExtractionButtons"/>).</param>
/// <param name="Player">User id of the player who holds it.</param>
/// <param name="PressedAt">Server world time of the press: the character starts lining up with the switch.</param>
/// <param name="Holding">Whether the hold itself runs (the alignment is over).</param>
public readonly record struct ExtractionDescent(int Button, long Player, double PressedAt, bool Holding = false)
{
    /// <summary>Server world time the hold starts: the switch's alignment after the press.</summary>
    /// <param name="map">The map's data: its elevator buttons.</param>
    /// <returns>The time.</returns>
    public double HoldStartsAt(MapData map)
    {
        ArgumentNullException.ThrowIfNull(map);
        return PressedAt + map.ExtractionButtons[Button].AlignmentSeconds;
    }

    /// <summary>Server world time the hold succeeds: the settings' extraction hold after its start.</summary>
    /// <param name="map">The map's data: its elevator buttons and settings.</param>
    /// <returns>The time.</returns>
    public double EndsAt(MapData map) => HoldStartsAt(map) + map.ExtractionElevator.Settings.ExtractionHoldSeconds;
}

/// <summary>An elevator's state in one raid.</summary>
/// <param name="State">Its state.</param>
/// <param name="ChangedAt">Server world time the state started, in seconds; null while it keeps its start state.</param>
/// <param name="CalledBy">User id of the player who called it, or null.</param>
/// <param name="CalledAt">Server world time its call button was pressed, while that press runs; otherwise null.</param>
public readonly record struct ExtractionPointState(ExtractionState State, double? ChangedAt, long? CalledBy, double? CalledAt = null)
{
    /// <summary>The descend hold under way while the elevator is ready, or null.</summary>
    public ExtractionDescent? Descent { get; init; }

    /// <summary>The descend hold that sent the elevator off, until it is ready again; null when it left on its own.</summary>
    public ExtractionDescent? Departed { get; init; }

    /// <summary>Whether a descend hold ran on this elevator: its descend switches send their hold start time from then on.</summary>
    public bool HoldRan { get; init; }

    /// <summary>Server world time the elevator was last ready, once it is closing or has left; null otherwise.</summary>
    public double? ReadyAt { get; init; }

    /// <summary>Server world time its doors last started closing, once they did; null from its next arrival on.</summary>
    public double? ClosingAt { get; init; }

    /// <summary>Whether players can leave with the elevator: its doors are open, or closing with them inside.</summary>
    public bool CanExtract => State is ExtractionState.Ready or ExtractionState.Closing;

    /// <summary>
    /// The extraction timer: it starts when the elevator is ready and lasts the settings' extraction time plus the
    /// doors' closing; once the doors start closing it ends when they are closed, and after that it keeps its length
    /// with no start.
    /// </summary>
    /// <param name="map">The map's data: what its elevators share.</param>
    /// <returns>The timer, or null.</returns>
    public ExtractionTimer? Timer(MapData map)
    {
        ArgumentNullException.ThrowIfNull(map);
        ExtractionElevator elevator = map.ExtractionElevator;
        return (State, ChangedAt, ReadyAt, ClosingAt) switch
        {
            (ExtractionState.Ready, { } readyAt, _, _) => new ExtractionTimer(readyAt, elevator.Settings.DefaultExtractionSeconds + elevator.DoorsSeconds),
            (ExtractionState.Closing, _, { } readyAt, { } closingAt) => new ExtractionTimer(readyAt, closingAt - readyAt + elevator.DoorsSeconds),
            (ExtractionState.Closed, _, { } readyAt, { } closingAt) => new ExtractionTimer(elevator.NoExtractionStartedTime, closingAt - readyAt + elevator.DoorsSeconds),
            _ => null,
        };
    }

    /// <summary>States of the elevator's buttons, in <see cref="MapData.ExtractionButtons"/> order.</summary>
    /// <remarks>
    /// The call button is usable while the elevator is closed, runs while it is pressed, and has succeeded (unusable)
    /// from the elevator's arrival until it closes. The descend switches are usable once it is ready; the held one runs,
    /// and the one that sent the elevator off has succeeded (unusable) until it is ready again. Repair buttons stay
    /// disabled.
    /// </remarks>
    /// <param name="map">The map's data: its elevator buttons.</param>
    /// <returns>The button states.</returns>
    public IReadOnlyList<CurrentInteractionState> Buttons(MapData map)
    {
        ArgumentNullException.ThrowIfNull(map);
        return [.. map.ExtractionButtons.Select(ButtonState)];
    }

    /// <summary>
    /// Hold start time each button sends, in <see cref="MapData.ExtractionButtons"/> order: none (null) before a descend
    /// hold ran; then on the descend switches the hold's start while it runs, otherwise the "no hold" time.
    /// </summary>
    /// <param name="map">The map's data: its elevator buttons and what its elevators share.</param>
    /// <returns>The hold start times.</returns>
    public IReadOnlyList<double?> HoldStarts(MapData map)
    {
        ArgumentNullException.ThrowIfNull(map);
        double?[] starts = new double?[map.ExtractionButtons.Length];
        for (int index = 0; index < starts.Length; index++)
        {
            starts[index] = HoldRan && map.ExtractionButtons[index].Role == ExtractionButtonRole.Descend
                ? Descent is { Holding: true } descent && descent.Button == index ? descent.HoldStartsAt(map) : map.ExtractionElevator.NoHoldStartedTime
                : null;
        }

        return starts;
    }

    private CurrentInteractionState ButtonState(ExtractionButton button, int index) => button.Role switch
    {
        ExtractionButtonRole.Summon => State switch
        {
            ExtractionState.Closed => CalledAt is null ? CurrentInteractionState.Idle : CurrentInteractionState.Ongoing,
            ExtractionState.Requested or ExtractionState.Arriving or ExtractionState.Ready or ExtractionState.Closing => CurrentInteractionState.SuccessDisabled,
            _ => CurrentInteractionState.Disabled,
        },
        ExtractionButtonRole.Descend when State == ExtractionState.Ready =>
            Descent?.Button == index ? CurrentInteractionState.Ongoing : CurrentInteractionState.Idle,
        ExtractionButtonRole.Descend when Departed?.Button == index => CurrentInteractionState.SuccessDisabled,
        _ => CurrentInteractionState.Disabled,
    };
}
