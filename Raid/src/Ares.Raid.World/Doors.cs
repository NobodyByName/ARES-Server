using Ares.Raid.Replication;
using Ares.Structs.Game.Replication;
using Ares.Structs.Game.World;
using Ares.Structs.Net.Replication;
using Ares.Structs.Net.Session;

namespace Ares.Raid.World;

/// <summary>A regular map door with its exported ids.</summary>
/// <param name="Index">Index in <see cref="DamDoors.All"/>.</param>
/// <param name="Cell">World Partition cell package holding the door.</param>
/// <param name="Guid">The door actor.</param>
/// <param name="Buttons">Its open/close button components.</param>
/// <param name="Breach">Its breach button component.</param>
public sealed record Door(int Index, string Cell, ulong Guid, IReadOnlyList<ulong> Buttons, ulong Breach);

/// <summary>
/// The regular Dam doors. The client loads them with their World Partition cells; their buttons stay
/// disabled until the server replicates their state, so the server references each door by its map path
/// (cell package, world, persistent level, actor and the two named buttons).
/// </summary>
public sealed class DoorCatalog
{
    private readonly ReplicationContext _replication;
    private readonly Dictionary<ulong, Door> _buttons = [];
    private readonly Dictionary<ulong, Door> _breaches = [];

    /// <summary>Names the doors.</summary>
    /// <param name="replication">Replication context (knows the interaction-state enum).</param>
    /// <param name="paths">Map path ids.</param>
    public DoorCatalog(ReplicationContext replication, MapPaths paths)
    {
        _replication = replication ?? throw new ArgumentNullException(nameof(replication));
        ArgumentNullException.ThrowIfNull(paths);
        List<Door> doors = [];
        foreach (DoorPlacement placement in DamDoors.All)
        {
            ulong actor = paths.MapObject(paths.Level(placement.Cell), placement.Actor);
            Door door = new(doors.Count, placement.Cell, actor, [.. DamDoors.InteractionComponents.Select(name => paths.MapObject(actor, name))],
                paths.MapObject(actor, DamDoors.BreachInteractionComponent));
            doors.Add(door);
            _breaches[door.Breach] = door;
            foreach (ulong button in door.Buttons)
            {
                _buttons[button] = door;
            }
        }

        Exports = paths.Exports();
        Doors = doors;
    }

    /// <summary>The door exports.</summary>
    private ObjectExports Exports { get; }

    /// <summary>All doors.</summary>
    public IReadOnlyList<Door> Doors { get; }

    /// <summary>The door a button belongs to.</summary>
    /// <param name="button">Button id.</param>
    /// <returns>The door, or null.</returns>
    public Door? ByButton(ulong button) => _buttons.GetValueOrDefault(button);

    /// <summary>The door a breach button belongs to.</summary>
    /// <param name="button">Breach button id.</param>
    /// <returns>The door, or null.</returns>
    public Door? ByBreach(ulong button) => _breaches.GetValueOrDefault(button);

    /// <summary>Exports a door's buttons (with the door and its cell).</summary>
    /// <param name="door">The door.</param>
    /// <returns>The bunch.</returns>
    public ChannelBunch ExportBunch(Door door)
    {
        ArgumentNullException.ThrowIfNull(door);
        return Bunches.Exports(Exports, [.. door.Buttons, door.Breach]);
    }

    /// <summary>Opens the door's channel with its state and button state.</summary>
    /// <param name="door">The door.</param>
    /// <param name="channel">Channel index.</param>
    /// <param name="sequence">Reliable sequence number.</param>
    /// <param name="buttons">Open/close and breach button states.</param>
    /// <param name="doorState">Door state.</param>
    /// <returns>The bunch.</returns>
    public ChannelBunch OpenBunch(Door door, int channel, int sequence, DoorButtons buttons, DoorState doorState)
    {
        ArgumentNullException.ThrowIfNull(door);
        ActorContentBlock actor = _replication.Encode(DamDoors.ActorClass, new Dictionary<string, object?>
        {
            [PropertyNames.Role] = (ulong)NetRole.Authority,
            [PropertyNames.RemoteRole] = (ulong)NetRole.SimulatedProxy,
            [PropertyNames.DoorState] = doorState.Pack(),
        });
        return new StaticActorInitialization(new StaticActorReference(door.Guid), [actor]) { Components = ButtonContent(door, buttons) }.ToBunch(channel, sequence);
    }

    /// <summary>Updates an open door channel.</summary>
    /// <param name="door">The door.</param>
    /// <param name="channel">Channel index.</param>
    /// <param name="doorState">Door state.</param>
    /// <param name="buttons">Open/close and breach button states.</param>
    /// <returns>The bunch.</returns>
    public ChannelBunch UpdateBunch(Door door, int channel, DoorState doorState, DoorButtons buttons)
    {
        ArgumentNullException.ThrowIfNull(door);
        ActorContentBlock actor = _replication.Encode(DamDoors.ActorClass, new Dictionary<string, object?> { [PropertyNames.DoorState] = doorState.Pack() });
        IReadOnlyList<StaticSubobjectContent> components = ButtonContent(door, buttons);
        return Bunches.Actor(channel, writer =>
        {
            actor.Write(writer);
            foreach (StaticSubobjectContent component in components)
            {
                component.Write(writer);
            }
        });
    }

    private StaticSubobjectContent[] ButtonContent(Door door, DoorButtons buttons) =>
        [.. door.Buttons.Select(guid => new StaticSubobjectContent(guid, Button(buttons.OpenClose))), new(door.Breach, Button(buttons.Breach))];

    private ActorContentBlock Button(CurrentInteractionState state) =>
        _replication.Encode(DamDoors.InteractionClass, new Dictionary<string, object?> { [PropertyNames.CurrentInteractionState] = (ulong)state });
}

/// <summary>States of a door's buttons.</summary>
/// <param name="OpenClose">Both open/close buttons.</param>
/// <param name="Breach">The breach button.</param>
public readonly record struct DoorButtons(CurrentInteractionState OpenClose, CurrentInteractionState Breach);

/// <summary>
/// Door state of one raid, shared by its connections. Each door starts as the map places it (open, locked,
/// barricaded, powered). A press on a usable door starts the opening or closing animation, which completes
/// after a fixed time. Holding a barricaded door's breach button removes the barricade once the alignment and
/// hold time have passed; doors whose map flag says so then open. A cancelled breach starts over. Changes carry a revision so each connection
/// sends them once.
/// </summary>
public sealed class DoorStates
{
    private const double AnimationSeconds = 2.0;
    private const double BreachSeconds = DamDoors.BreachAlignmentSeconds + DamDoors.BreachHoldSeconds;

    private readonly IReadOnlyList<DoorPlacement> _placements;
    private readonly Dictionary<int, (DoorState State, int Revision, double? Until)> _states = [];
    private readonly Dictionary<int, double> _breaches = [];

    /// <summary>Sets each door to its start state.</summary>
    /// <param name="placements">The doors, by index.</param>
    public DoorStates(IReadOnlyList<DoorPlacement> placements)
    {
        _placements = placements ?? throw new ArgumentNullException(nameof(placements));
        for (int index = 0; index < placements.Count; index++)
        {
            _states[index] = (StartState(placements[index]), 0, null);
        }
    }

    /// <summary>A door's state, its revision and when its animation ends (null when idle).</summary>
    /// <param name="index">Door index.</param>
    /// <returns>The state.</returns>
    public (DoorState State, int Revision, double? Until) Get(int index) => _states[index];

    /// <summary>Whether a door's open/close buttons work: not barricaded, locked or without power.</summary>
    /// <param name="index">Door index.</param>
    /// <returns>True when a press can open or close it.</returns>
    public bool Usable(int index) =>
        _states[index].State is { Seal: not (DoorSealState.Sealed or DoorSealState.SealedWithDoorBlocker), Lock: not DoorLockState.Locked, Power: not DoorPowerState.Unpowered };

    /// <summary>Whether a door's breach button works: the door is barricaded by the map.</summary>
    /// <param name="index">Door index.</param>
    /// <returns>True while the barricade is up.</returns>
    public bool Breachable(int index) => _states[index].State.Seal == DoorSealState.Sealed;

    /// <summary>Whether a breach of the door is under way.</summary>
    /// <param name="index">Door index.</param>
    /// <returns>True from the breach start until the barricade comes off.</returns>
    public bool Breaching(int index) => _breaches.ContainsKey(index);

    /// <summary>States of a door's buttons.</summary>
    /// <param name="index">Door index.</param>
    /// <returns>
    /// Open/close: Disabled on unusable doors, SuccessIdle while the door moves, Idle otherwise. Breach: Idle on
    /// a barricaded door, Ongoing while it is breached, SuccessDisabled once breached (the client plays the
    /// interaction's success ending on that state; Idle or Disabled would end it as failed), Disabled on doors
    /// that never had a barricade.
    /// </returns>
    public DoorButtons Buttons(int index)
    {
        CurrentInteractionState openClose = !Usable(index) ? CurrentInteractionState.Disabled
            : _states[index].Until is null ? CurrentInteractionState.Idle
            : CurrentInteractionState.SuccessIdle;
        CurrentInteractionState breach = Breaching(index) ? CurrentInteractionState.Ongoing
            : Breachable(index) ? CurrentInteractionState.Idle
            : _placements[index].StartAsSealed ? CurrentInteractionState.SuccessDisabled
            : CurrentInteractionState.Disabled;

        return new DoorButtons(openClose, breach);
    }

    /// <summary>Starts opening a closed door, or closing an open one; ignored while it moves or when unusable.</summary>
    /// <param name="index">Door index.</param>
    /// <param name="now">Current time in seconds.</param>
    /// <returns>Whether the press was applied.</returns>
    public bool Press(int index, double now)
    {
        (DoorState state, int revision, double? until) = Get(index);
        if (until is not null || !Usable(index))
        {
            return false;
        }

        DoorOpenState next = state.Open == DoorOpenState.Closed ? DoorOpenState.Opening : DoorOpenState.Closing;
        _states[index] = (state with { Open = next }, revision + 1, now + AnimationSeconds);
        return true;
    }

    /// <summary>Starts breaching a barricaded door; ignored when it is not barricaded or already being breached.</summary>
    /// <param name="index">Door index.</param>
    /// <param name="now">Current time in seconds.</param>
    /// <returns>Whether the breach started.</returns>
    public bool StartBreach(int index, double now)
    {
        if (!Breachable(index) || Breaching(index))
        {
            return false;
        }

        _breaches[index] = now + BreachSeconds;
        (DoorState state, int revision, double? until) = Get(index);
        _states[index] = (state, revision + 1, until);
        return true;
    }

    /// <summary>Stops a breach under way; the barricade stays and the breach button is usable again.</summary>
    /// <param name="index">Door index.</param>
    public void CancelBreach(int index)
    {
        if (!_breaches.Remove(index))
        {
            return;
        }

        (DoorState state, int revision, double? until) = Get(index);
        _states[index] = (state, revision + 1, until);
    }

    /// <summary>Completes the animations and breaches that ended.</summary>
    /// <param name="now">Current time in seconds.</param>
    public void Advance(double now)
    {
        foreach ((int index, double end) in _breaches.ToArray())
        {
            if (now < end)
            {
                continue;
            }

            _breaches.Remove(index);
            (DoorState state, int revision, double? until) = Get(index);
            state = state with { Seal = DoorSealState.Unsealed };
            _states[index] = (state, revision + 1, until);
            if (_placements[index].BreachOpensDoor && state.Open == DoorOpenState.Closed)
            {
                Press(index, now);
            }
        }

        foreach ((int index, (DoorState state, int revision, double? until)) in _states.ToArray())
        {
            if (until is not { } end || now < end)
            {
                continue;
            }

            DoorOpenState final = state.Open == DoorOpenState.Opening ? DoorOpenState.Open : DoorOpenState.Closed;
            _states[index] = (state with { Open = final }, revision + 1, null);
        }
    }

    // What the door script applies at BeginPlay from the placed door's start flags.
    private static DoorState StartState(DoorPlacement placement) => new(
        placement.StartAsOpened ? DoorOpenState.Open : DoorOpenState.Closed,
        placement.StartAsSealed ? DoorSealState.Sealed : DoorSealState.Unsealed,
        placement.StartAsLocked ? DoorLockState.Locked : DoorLockState.Unlocked,
        placement.StartAsPowered ? DoorPowerState.Powered : DoorPowerState.Unpowered,
        DoorSwingDirection.Forward);
}
