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
/// <param name="Buttons">Its button components.</param>
public sealed record Door(int Index, string Cell, ulong Guid, IReadOnlyList<ulong> Buttons);

/// <summary>
/// The regular Dam doors. The client loads them with their World Partition cells; their buttons stay
/// disabled until the server replicates their state, so the server references each door by its map path
/// (cell package, world, persistent level, actor and the two named buttons).
/// </summary>
public sealed class DoorCatalog
{
    private readonly ReplicationContext _replication;
    private readonly Dictionary<ulong, Door> _buttons = [];

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
            Door door = new(doors.Count, placement.Cell, actor, [.. DamDoors.InteractionComponents.Select(name => paths.MapObject(actor, name))]);
            doors.Add(door);
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

    /// <summary>Exports a door's buttons (with the door and its cell).</summary>
    /// <param name="door">The door.</param>
    /// <returns>The bunch.</returns>
    public ChannelBunch ExportBunch(Door door) => Bunches.Exports(Exports, door.Buttons);

    /// <summary>Opens the door's channel with its state and button state.</summary>
    /// <param name="door">The door.</param>
    /// <param name="channel">Channel index.</param>
    /// <param name="sequence">Reliable sequence number.</param>
    /// <param name="buttons">Button state.</param>
    /// <param name="doorState">Door state.</param>
    /// <returns>The bunch.</returns>
    public ChannelBunch OpenBunch(Door door, int channel, int sequence, CurrentInteractionState buttons, DoorState doorState)
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
    /// <param name="buttons">Button state.</param>
    /// <returns>The bunch.</returns>
    public ChannelBunch UpdateBunch(Door door, int channel, DoorState doorState, CurrentInteractionState buttons)
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

    private StaticSubobjectContent[] ButtonContent(Door door, CurrentInteractionState buttons)
    {
        ActorContentBlock block = _replication.Encode(DamDoors.InteractionClass, new Dictionary<string, object?> { [PropertyNames.CurrentInteractionState] = (ulong)buttons });
        return [.. door.Buttons.Select(guid => new StaticSubobjectContent(guid, block))];
    }
}

/// <summary>
/// Door state of one raid, shared by its connections. Each door starts as the map places it (open, locked,
/// barricaded, powered). A press on a usable door starts the opening or closing animation, which completes
/// after a fixed time. Changes carry a revision so each connection sends them once.
/// </summary>
public sealed class DoorStates
{
    private const double AnimationSeconds = 2.0;

    private readonly Dictionary<int, (DoorState State, int Revision, double? Until)> _states = [];

    /// <summary>Sets each door to its start state.</summary>
    /// <param name="placements">The doors, by index.</param>
    public DoorStates(IReadOnlyList<DoorPlacement> placements)
    {
        ArgumentNullException.ThrowIfNull(placements);
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

    /// <summary>State of a door's open/close buttons.</summary>
    /// <param name="index">Door index.</param>
    /// <returns>Disabled on unusable doors, SuccessIdle while the door moves, Idle otherwise.</returns>
    public CurrentInteractionState Buttons(int index) =>
        !Usable(index) ? CurrentInteractionState.Disabled
        : _states[index].Until is null ? CurrentInteractionState.Idle
        : CurrentInteractionState.SuccessIdle;

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

    /// <summary>Completes the animations that ended.</summary>
    /// <param name="now">Current time in seconds.</param>
    public void Advance(double now)
    {
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
