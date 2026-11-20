using Ares.Common.Game;
using Ares.Common.GameData;
using Ares.Raid.World.Replication;
using Ares.Structs.Game.Replication;
using Ares.Structs.Game.World;
using Ares.Structs.Net.Replication;
using Ares.Structs.Net.Session;

namespace Ares.Raid.World.Actors;

/// <summary>
/// The map's extraction elevators. They are actors of the map's persistent level, loaded with the map; their
/// buttons start as the Blueprint sets them until the server replicates their state, so the server references
/// each elevator by its map path (map package, world, persistent level, actor and its named buttons).
/// </summary>
public sealed class ExtractionCatalog
{
    private readonly ReplicationContext _replication;
    private readonly MapData _map;
    private readonly RaidSettings _raid;
    private readonly ObjectExports _exports;
    private readonly Dictionary<ulong, (ExtractionPoint Point, ExtractionButton Button)> _buttons = [];
    private readonly ulong[] _animations;

    /// <summary>Names the elevators.</summary>
    /// <param name="replication">Replication context (knows the extraction and interaction-state enums).</param>
    /// <param name="paths">Map path ids.</param>
    /// <param name="map">The map's data: its elevators and their buttons.</param>
    /// <param name="raid">The raid settings: the extraction time.</param>
    public ExtractionCatalog(ReplicationContext replication, MapPaths paths, MapData map, RaidSettings raid)
    {
        _replication = replication ?? throw new ArgumentNullException(nameof(replication));
        ArgumentNullException.ThrowIfNull(paths);
        _map = map ?? throw new ArgumentNullException(nameof(map));
        _raid = raid ?? throw new ArgumentNullException(nameof(raid));
        ulong level = paths.Level(map.MapPackage);
        List<ExtractionPoint> points = [];
        foreach (ExtractionPoint point in map.ExtractionPoints.Select((placement, index) => Create(index, placement)))
        {
            points.Add(point);
            for (int index = 0; index < point.Buttons.Count; index++)
            {
                _buttons[point.Buttons[index]] = (point, map.ExtractionButtons[index]);
            }
        }

        // The animation sets of the buttons, which the character's interactor state names. The elevator Blueprint keeps
        // them loaded, so the client finds them like the map objects.
        _animations = [.. map.ExtractionButtons.Select(button => button.AnimationContainer is { } container
            ? paths.MapObject(paths.MapObject(0, container), container[(container.LastIndexOf('/') + 1)..])
            : 0UL)];
        _exports = paths.Exports();
        Points = points;

        return;

        ExtractionPoint Create(int index, ExtractionPointPlacement placement)
        {
            ulong actor = paths.MapObject(level, placement.Actor);
            return new ExtractionPoint(index, actor, [.. map.ExtractionButtons.Select(button => paths.MapObject(actor, button.Component))], placement,
                map.ExtractionElevator);
        }
    }

    /// <summary>All elevators.</summary>
    public IReadOnlyList<ExtractionPoint> Points { get; }

    /// <summary>The elevator and button a button id names.</summary>
    /// <param name="button">Button id.</param>
    /// <returns>The elevator and its button, or null.</returns>
    public (ExtractionPoint Point, ExtractionButton Button)? ByButton(ulong button) =>
        _buttons.TryGetValue(button, out (ExtractionPoint, ExtractionButton) found) ? found : null;

    /// <summary>Id of the animation set a button's interaction plays.</summary>
    /// <param name="button">Index in <see cref="MapData.ExtractionButtons"/>.</param>
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
        ActorContentBlock actor = _replication.Encode(ExtractionClasses.ActorClass, values);
        IReadOnlyList<CurrentInteractionState> buttons = state.Buttons(_map);
        IReadOnlyList<double?> holds = state.HoldStarts(_map);
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
        IReadOnlyList<CurrentInteractionState> was = previous.Buttons(_map);
        IReadOnlyList<CurrentInteractionState> buttons = state.Buttons(_map);
        IReadOnlyList<double?> held = previous.HoldStarts(_map);
        IReadOnlyList<double?> holds = state.HoldStarts(_map);
        StaticSubobjectContent[] components = [.. Enumerable.Range(0, point.Buttons.Count)
            .Where(index => was[index] != buttons[index] || !Nullable.Equals(held[index], holds[index]))
            .Select(index => Button(point, index, buttons[index], holds[index]))];
        if (changes.Count == 0 && components.Length == 0)
        {
            return null;
        }

        ActorContentBlock? actor = changes.Count == 0 ? null : _replication.Encode(ExtractionClasses.ActorClass, changes);
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
    private Dictionary<string, object?> ActorValues(ExtractionPointState state, double? changedAt, bool opening)
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

        if (state.Timer(_map, _raid) is not { } timer)
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

        return new StaticSubobjectContent(point.Buttons[index], _replication.Encode(_map.ExtractionButtons[index].Class, values));
    }
}
