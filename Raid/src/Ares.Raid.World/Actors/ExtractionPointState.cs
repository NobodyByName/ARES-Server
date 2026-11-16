using Ares.Structs.Game.World;

namespace Ares.Raid.World.Actors;

/// <summary>An elevator's extraction timer (<c>FExtractionInfo</c>).</summary>
/// <param name="StartedAt">Server world time the extraction became possible, in seconds; the settings' "no extraction"
/// time once the elevator has left.</param>
/// <param name="Seconds">Seconds from that time until the elevator leaves (its doors closed).</param>
public readonly record struct ExtractionTimer(double StartedAt, double Seconds);

/// <summary>A descend switch hold on a ready elevator.</summary>
/// <param name="Button">The switch, as an index in <see cref="DamExtraction.Buttons"/>.</param>
/// <param name="Player">User id of the player who holds it.</param>
/// <param name="PressedAt">Server world time of the press: the character starts lining up with the switch.</param>
/// <param name="Holding">Whether the hold itself runs (the alignment is over).</param>
public readonly record struct ExtractionDescent(int Button, long Player, double PressedAt, bool Holding = false)
{
    /// <summary>Server world time the hold starts: the switch's alignment after the press.</summary>
    public double HoldStartsAt => PressedAt + DamExtraction.Buttons[Button].AlignmentSeconds;

    /// <summary>Server world time the hold succeeds: the settings' extraction hold after its start.</summary>
    public double EndsAt => HoldStartsAt + DamExtraction.Settings.ExtractionHoldSeconds;
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
    public ExtractionTimer? Timer => (State, ChangedAt, ReadyAt, ClosingAt) switch
    {
        (ExtractionState.Ready, { } readyAt, _, _) => new ExtractionTimer(readyAt, DamExtraction.Settings.DefaultExtractionSeconds + DamExtraction.DoorsSeconds),
        (ExtractionState.Closing, _, { } readyAt, { } closingAt) => new ExtractionTimer(readyAt, closingAt - readyAt + DamExtraction.DoorsSeconds),
        (ExtractionState.Closed, _, { } readyAt, { } closingAt) => new ExtractionTimer(DamExtraction.NoExtractionStartedTime, closingAt - readyAt + DamExtraction.DoorsSeconds),
        _ => null,
    };

    /// <summary>States of the elevator's buttons, in <see cref="DamExtraction.Buttons"/> order.</summary>
    /// <remarks>
    /// The call button is usable while the elevator is closed, runs while it is pressed, and has succeeded (unusable)
    /// from the elevator's arrival until it closes. The descend switches are usable once it is ready; the held one runs,
    /// and the one that sent the elevator off has succeeded (unusable) until it is ready again. Repair buttons stay
    /// disabled.
    /// </remarks>
    public IReadOnlyList<CurrentInteractionState> Buttons => [.. DamExtraction.Buttons.Select(ButtonState)];

    /// <summary>
    /// Hold start time each button sends, in <see cref="DamExtraction.Buttons"/> order: none (null) before a descend
    /// hold ran; then on the descend switches the hold's start while it runs, otherwise the "no hold" time.
    /// </summary>
    public IReadOnlyList<double?> HoldStarts => [.. DamExtraction.Buttons.Select(HoldStart)];

    private double? HoldStart(ExtractionButton button, int index) => HoldRan && button.Role == ExtractionButtonRole.Descend
        ? Descent is { Holding: true } descent && descent.Button == index ? descent.HoldStartsAt : DamExtraction.NoHoldStartedTime
        : null;

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
