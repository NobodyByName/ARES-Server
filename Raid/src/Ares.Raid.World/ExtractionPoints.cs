using Ares.Raid.Replication;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Game.Replication;
using Ares.Structs.Game.World;
using Ares.Structs.Net.Replication;
using Ares.Structs.Net.Session;

namespace Ares.Raid.World;

/// <summary>An extraction elevator with its exported ids.</summary>
/// <param name="Index">Index in <see cref="DamExtraction.All"/>.</param>
/// <param name="Guid">The elevator actor.</param>
/// <param name="Buttons">Its button components, in <see cref="DamExtraction.Buttons"/> order.</param>
public sealed record ExtractionPoint(int Index, ulong Guid, IReadOnlyList<ulong> Buttons)
{
    /// <summary>Actor name of the elevator in the map's persistent level.</summary>
    public string Name => DamExtraction.All[Index].Actor;

    /// <summary>Index of a button in <see cref="DamExtraction.Buttons"/>.</summary>
    /// <param name="button">Button id.</param>
    /// <returns>The index, or -1 when the id is not one of this elevator's buttons.</returns>
    public int ButtonIndex(ulong button)
    {
        for (int index = 0; index < Buttons.Count; index++)
        {
            if (Buttons[index] == button)
            {
                return index;
            }
        }

        return -1;
    }

    /// <summary>Whether a location is inside the elevator's extraction zone (a box placed relative to the elevator).</summary>
    /// <param name="location">World location, in centimetres.</param>
    /// <returns>Whether it is inside.</returns>
    public bool Contains(Vector3D location)
    {
        ExtractionPointPlacement placement = DamExtraction.All[Index];
        Vector3D zone = placement.Rotation.Rotate(DamExtraction.ZoneLocation);
        Vector3D offset = new(location.X - placement.Location.X - zone.X, location.Y - placement.Location.Y - zone.Y, location.Z - placement.Location.Z - zone.Z);
        Vector3D half = DamExtraction.ZoneHalfExtent;
        return Math.Abs(Dot(offset, Axis(new Vector3D(1, 0, 0)))) <= half.X
            && Math.Abs(Dot(offset, Axis(new Vector3D(0, 1, 0)))) <= half.Y
            && Math.Abs(Dot(offset, Axis(new Vector3D(0, 0, 1)))) <= half.Z;

        Vector3D Axis(Vector3D unit) => placement.Rotation.Rotate(DamExtraction.ZoneRotation.Rotate(unit));
    }

    private static double Dot(Vector3D a, Vector3D b) => (a.X * b.X) + (a.Y * b.Y) + (a.Z * b.Z);
}

/// <summary>
/// The Dam extraction elevators. They are actors of the map's persistent level, loaded with the map; their
/// buttons start as the Blueprint sets them until the server replicates their state, so the server references
/// each elevator by its map path (map package, world, persistent level, actor and its named buttons).
/// </summary>
public sealed class ExtractionCatalog
{
    private readonly ReplicationContext _replication;
    private readonly ObjectExports _exports;
    private readonly Dictionary<ulong, (ExtractionPoint Point, ExtractionButton Button)> _buttons = [];
    private readonly ulong[] _animations;

    /// <summary>Names the elevators.</summary>
    /// <param name="replication">Replication context (knows the extraction and interaction-state enums).</param>
    /// <param name="paths">Map path ids.</param>
    public ExtractionCatalog(ReplicationContext replication, MapPaths paths)
    {
        _replication = replication ?? throw new ArgumentNullException(nameof(replication));
        ArgumentNullException.ThrowIfNull(paths);
        ulong level = paths.Level(DamMap.MapPackage);
        List<ExtractionPoint> points = [];
        foreach (ExtractionPointPlacement placement in DamExtraction.All)
        {
            ulong actor = paths.MapObject(level, placement.Actor);
            ExtractionPoint point = new(points.Count, actor, [.. DamExtraction.Buttons.Select(button => paths.MapObject(actor, button.Component))]);
            points.Add(point);
            for (int index = 0; index < point.Buttons.Count; index++)
            {
                _buttons[point.Buttons[index]] = (point, DamExtraction.Buttons[index]);
            }
        }

        // The animation sets of the buttons, which the character's interactor state names. The elevator Blueprint keeps
        // them loaded, so the client finds them like the map objects.
        _animations = [.. DamExtraction.Buttons.Select(button => button.AnimationContainer is { } container
            ? paths.MapObject(paths.MapObject(0, container), container[(container.LastIndexOf('/') + 1)..])
            : 0UL)];
        _exports = paths.Exports();
        Points = points;
    }

    /// <summary>All elevators.</summary>
    public IReadOnlyList<ExtractionPoint> Points { get; }

    /// <summary>The elevator and button a button id names.</summary>
    /// <param name="button">Button id.</param>
    /// <returns>The elevator and its button, or null.</returns>
    public (ExtractionPoint Point, ExtractionButton Button)? ByButton(ulong button) =>
        _buttons.TryGetValue(button, out (ExtractionPoint, ExtractionButton) found) ? found : null;

    /// <summary>Id of the animation set a button's interaction plays.</summary>
    /// <param name="button">Index in <see cref="DamExtraction.Buttons"/>.</param>
    /// <returns>The id, or 0 when the button has none.</returns>
    public ulong Animation(int button) => _animations[button];

    /// <summary>
    /// Exports an elevator's buttons (each with the elevator and the persistent level), one bunch per button: every
    /// export repeats the whole path, and all of an elevator's buttons do not fit in one packet. The animation sets of
    /// the buttons follow, one bunch each.
    /// </summary>
    /// <param name="point">The elevator.</param>
    /// <returns>The bunches.</returns>
    public IReadOnlyList<ChannelBunch> ExportBunches(ExtractionPoint point)
    {
        ArgumentNullException.ThrowIfNull(point);
        return [.. point.Buttons.Concat(_animations.Where(animation => animation != 0).Distinct()).Select(guid => Bunches.Exports(_exports, [guid]))];
    }

    /// <summary>Opens the elevator's channel with its state and its button states.</summary>
    /// <param name="point">The elevator.</param>
    /// <param name="channel">Channel index.</param>
    /// <param name="sequence">Reliable sequence number.</param>
    /// <param name="state">Elevator state.</param>
    /// <param name="changedAt">Server world time of the state's start, in seconds, when the state does not know it.</param>
    /// <returns>The bunch.</returns>
    public ChannelBunch OpenBunch(ExtractionPoint point, int channel, int sequence, ExtractionPointState state, double changedAt)
    {
        ArgumentNullException.ThrowIfNull(point);
        Dictionary<string, object?> values = ActorValues(state, state.ChangedAt ?? changedAt, opening: true);
        values[PropertyNames.Role] = (ulong)NetRole.Authority;
        values[PropertyNames.RemoteRole] = (ulong)NetRole.SimulatedProxy;
        values[PropertyNames.IsEnabled] = true;
        ActorContentBlock actor = _replication.Encode(DamExtraction.ActorClass, values);
        IReadOnlyList<CurrentInteractionState> buttons = state.Buttons;
        IReadOnlyList<double?> holds = state.HoldStarts;
        StaticSubobjectContent[] components = [.. point.Buttons.Select((_, index) => Button(point, index, buttons[index], holds[index]))];
        return new StaticActorInitialization(new StaticActorReference(point.Guid), [actor]) { Components = components }.ToBunch(channel, sequence);
    }

    /// <summary>Updates an open elevator channel with what changed between two states.</summary>
    /// <param name="point">The elevator.</param>
    /// <param name="channel">Channel index.</param>
    /// <param name="previous">The state the client has.</param>
    /// <param name="state">The new state.</param>
    /// <returns>The bunch, or null when nothing the client sees changed.</returns>
    public ChannelBunch? UpdateBunch(ExtractionPoint point, int channel, ExtractionPointState previous, ExtractionPointState state)
    {
        ArgumentNullException.ThrowIfNull(point);
        Dictionary<string, object?> before = ActorValues(previous, previous.ChangedAt, opening: false);
        Dictionary<string, object?> changes = ActorValues(state, state.ChangedAt, opening: false)
            .Where(entry => !before.TryGetValue(entry.Key, out object? value) || !Equals(value, entry.Value))
            .ToDictionary();
        IReadOnlyList<CurrentInteractionState> was = previous.Buttons;
        IReadOnlyList<CurrentInteractionState> buttons = state.Buttons;
        IReadOnlyList<double?> held = previous.HoldStarts;
        IReadOnlyList<double?> holds = state.HoldStarts;
        StaticSubobjectContent[] components = [.. Enumerable.Range(0, point.Buttons.Count)
            .Where(index => was[index] != buttons[index] || !Nullable.Equals(held[index], holds[index]))
            .Select(index => Button(point, index, buttons[index], holds[index]))];
        if (changes.Count == 0 && components.Length == 0)
        {
            return null;
        }

        ActorContentBlock? actor = changes.Count == 0 ? null : _replication.Encode(DamExtraction.ActorClass, changes);
        return Bunches.Actor(channel, writer =>
        {
            actor?.Write(writer);
            foreach (StaticSubobjectContent component in components)
            {
                component.Write(writer);
            }
        });
    }

    // Replicated members of the elevator for a state. bCanExtract is first sent once it is true; the extraction
    // timer while it is set (between two cycles the client keeps the last one).
    private static Dictionary<string, object?> ActorValues(ExtractionPointState state, double? changedAt, bool opening)
    {
        Dictionary<string, object?> values = new() { [PropertyNames.State] = (ulong)state.State };
        if (changedAt is { } at)
        {
            values[PropertyNames.StateChangeTimestamp] = at;
        }

        if (!opening || state.CanExtract)
        {
            values[PropertyNames.CanExtract] = state.CanExtract;
        }

        if (state.Timer is not { } timer)
        {
            return values;
        }

        values[$"{PropertyNames.ExtractionInfo}.{PropertyNames.ExtractionStartedTs}"] = timer.StartedAt;
        values[$"{PropertyNames.ExtractionInfo}.{PropertyNames.ExtractionTime}"] = timer.Seconds;
        return values;
    }

    // A button's state, with its hold start time once the button replicates it.
    private StaticSubobjectContent Button(ExtractionPoint point, int index, CurrentInteractionState state, double? holdStartedAt)
    {
        Dictionary<string, object?> values = new() { [PropertyNames.CurrentInteractionState] = (ulong)state };
        if (holdStartedAt is { } hold)
        {
            values[PropertyNames.InteractionServerHoldStartedTime] = hold;
        }

        return new StaticSubobjectContent(point.Buttons[index], _replication.Encode(DamExtraction.Buttons[index].Class, values));
    }
}

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

/// <summary>Players inside an elevator when it left (its doors closed).</summary>
/// <param name="At">Server world time the doors started closing, in seconds.</param>
/// <param name="Players">User ids of the players inside its extraction zone once the doors closed.</param>
public sealed record ExtractionDeparture(double At, IReadOnlyList<long> Players);

/// <summary>
/// Elevator state of one raid, shared by its connections. Every elevator starts closed with only its call button
/// usable. A press on the call button of a closed elevator calls it: once the press has run (the button's alignment
/// and the settings' summon hold) the elevator arrives, and it is ready (doors open, descend switches usable) the
/// settings' time to extract ready later. A held descend switch (its alignment, then the settings' extraction hold)
/// closes the doors; so does the end of the settings' extraction time with nobody descending. The elevator has left
/// (closed, callable again) once the doors have closed.
/// </summary>
public sealed class ExtractionStates
{
    private static readonly ExtractionButton s_summon = DamExtraction.Buttons.Single(button => button.Role == ExtractionButtonRole.Summon);

    private readonly ExtractionPointState[] _states;
    private readonly ExtractionDeparture?[] _departures;

    /// <summary>Sets each elevator to its start state.</summary>
    /// <param name="count">Number of elevators.</param>
    public ExtractionStates(int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        _states = [.. Enumerable.Repeat(new ExtractionPointState(ExtractionState.Closed, null, null), count)];
        _departures = new ExtractionDeparture?[count];
    }

    /// <summary>Seconds from a press on the call button to the elevator's arrival.</summary>
    public static double CallSeconds => s_summon.AlignmentSeconds + DamExtraction.Settings.SummonHoldSeconds;

    /// <summary>An elevator's state.</summary>
    /// <param name="index">Elevator index.</param>
    /// <returns>The state.</returns>
    public ExtractionPointState Get(int index) => _states[index];

    /// <summary>States of an elevator's buttons, in <see cref="DamExtraction.Buttons"/> order.</summary>
    /// <param name="index">Elevator index.</param>
    /// <returns>The button states.</returns>
    public IReadOnlyList<CurrentInteractionState> Buttons(int index)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, _states.Length);
        return _states[index].Buttons;
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
    /// <param name="button">The switch, as an index in <see cref="DamExtraction.Buttons"/>.</param>
    /// <param name="player">User id of the player.</param>
    /// <param name="at">Server world time of the press, in seconds.</param>
    /// <returns>Whether the hold started.</returns>
    public bool Descend(int index, int button, long player, double at)
    {
        ExtractionPointState state = _states[index];
        if (state.State != ExtractionState.Ready || state.Descent is not null || DamExtraction.Buttons[button].Role != ExtractionButtonRole.Descend)
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

            if (state is { State: ExtractionState.Arriving, ChangedAt: { } arriving } && at >= arriving + DamExtraction.Settings.TimeToExtractReadySeconds)
            {
                state = state with { State = ExtractionState.Ready, ChangedAt = arriving + DamExtraction.Settings.TimeToExtractReadySeconds, Departed = null };
            }

            if (state is { State: ExtractionState.Ready, ChangedAt: { } ready })
            {
                double timeout = ready + DamExtraction.Settings.DefaultExtractionSeconds;
                ExtractionDescent? descent = state.Descent;
                double closing = descent is { } held && held.EndsAt < timeout ? held.EndsAt : timeout;
                if (at >= closing)
                {
                    state = state with
                    {
                        State = ExtractionState.Closing,
                        ChangedAt = closing,
                        ReadyAt = ready,
                        ClosingAt = closing,
                        Descent = null,
                        Departed = descent is { } done && done.EndsAt.Equals(closing) ? done : null,
                    };
                }
                else if (descent is { Holding: false } aligning && at >= aligning.HoldStartsAt)
                {
                    state = state with { Descent = aligning with { Holding = true } };
                }
            }

            if (state is { State: ExtractionState.Closing, ChangedAt: { } closingAt } && at >= closingAt + DamExtraction.DoorsSeconds)
            {
                state = state with { State = ExtractionState.Closed, ChangedAt = closingAt + DamExtraction.DoorsSeconds, CalledBy = null };
                _departures[index] = new ExtractionDeparture(closingAt, inside?.Invoke(index) ?? []);
            }

            _states[index] = state;
        }
    }
}
