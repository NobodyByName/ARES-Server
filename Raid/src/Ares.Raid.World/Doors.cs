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
/// (cell package, world, persistent level, actor and the two named buttons). The exports carry checksum 0,
/// which the client does not check.
/// </summary>
public sealed class DoorCatalog
{
    private const ulong FirstGuid = 0x200001;

    private readonly ReplicationContext _replication;
    private readonly Dictionary<ulong, Door> _buttons = [];

    /// <summary>Builds the door exports.</summary>
    /// <param name="replication">Replication context (knows the interaction-state enum).</param>
    public DoorCatalog(ReplicationContext replication)
    {
        _replication = replication ?? throw new ArgumentNullException(nameof(replication));
        List<StaticObjectExport> records = [];
        Dictionary<string, ulong> levels = new(StringComparer.Ordinal);
        List<Door> doors = [];

        foreach (DoorPlacement placement in DamDoors.All)
        {
            if (!levels.TryGetValue(placement.Cell, out ulong level))
            {
                level = Add(Add(Add(0, placement.Cell), DamMap.WorldName), DamMap.PersistentLevelName);
                levels[placement.Cell] = level;
            }

            ulong actor = Add(level, placement.Actor);
            Door door = new(doors.Count, placement.Cell, actor, [.. DamDoors.InteractionComponents.Select(name => Add(actor, name))]);
            doors.Add(door);
            foreach (ulong button in door.Buttons)
            {
                _buttons[button] = door;
            }
        }

        Exports = new ObjectExports(records);
        Doors = doors;
        return;

        ulong Add(ulong outer, string path)
        {
            ulong guid = FirstGuid + (2 * (ulong)records.Count);
            records.Add(new StaticObjectExport(guid, outer, path, 0, true));
            return guid;
        }
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
    /// <param name="doorState">Replicated door state.</param>
    /// <returns>The bunch.</returns>
    public ChannelBunch OpenBunch(Door door, int channel, int sequence, CurrentInteractionState buttons, ushort doorState)
    {
        ArgumentNullException.ThrowIfNull(door);
        ActorContentBlock actor = _replication.Encode(DamDoors.ActorClass, new Dictionary<string, object?>
        {
            [PropertyNames.Role] = (ulong)NetRole.Authority,
            [PropertyNames.RemoteRole] = (ulong)NetRole.SimulatedProxy,
            [PropertyNames.DoorState] = doorState,
        });
        return new StaticActorInitialization(new StaticActorReference(door.Guid), [actor]) { Components = ButtonContent(door, buttons) }.ToBunch(channel, sequence);
    }

    /// <summary>Updates an open door channel.</summary>
    /// <param name="door">The door.</param>
    /// <param name="channel">Channel index.</param>
    /// <param name="doorState">Replicated door state.</param>
    /// <param name="buttons">Button state.</param>
    /// <returns>The bunch.</returns>
    public ChannelBunch UpdateBunch(Door door, int channel, ushort doorState, CurrentInteractionState buttons)
    {
        ArgumentNullException.ThrowIfNull(door);
        ActorContentBlock actor = _replication.Encode(DamDoors.ActorClass, new Dictionary<string, object?> { [PropertyNames.DoorState] = doorState });
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
/// Door state of one raid, shared by its connections: a press starts the opening or closing animation,
/// which completes after a fixed time. Changes carry a revision so each connection sends them once.
/// </summary>
public sealed class DoorStates
{
    private const double AnimationSeconds = 2.0;
    private const ushort OpenStateMask = 7;

    private readonly Dictionary<int, (ushort State, int Revision, double? Until)> _states = [];

    /// <summary>A door's replicated state, its revision and when its animation ends (null when idle).</summary>
    /// <param name="index">Door index.</param>
    /// <returns>The state.</returns>
    public (ushort State, int Revision, double? Until) Get(int index) => _states.GetValueOrDefault(index, ((ushort)DoorOpenState.Closed, 0, null));

    /// <summary>Starts opening a closed door, or closing an open one; ignored while it moves.</summary>
    /// <param name="index">Door index.</param>
    /// <param name="now">Current time in seconds.</param>
    /// <returns>Whether the press was applied.</returns>
    public bool Press(int index, double now)
    {
        (ushort state, int revision, double? until) = Get(index);
        if (until is not null)
        {
            return false;
        }

        DoorOpenState next = (DoorOpenState)(state & OpenStateMask) == DoorOpenState.Closed ? DoorOpenState.Opening : DoorOpenState.Closing;
        _states[index] = (WithOpenState(state, next), revision + 1, now + AnimationSeconds);
        return true;
    }

    /// <summary>Completes the animations that ended.</summary>
    /// <param name="now">Current time in seconds.</param>
    public void Advance(double now)
    {
        foreach ((int index, (ushort state, int revision, double? until)) in _states.ToArray())
        {
            if (until is not { } end || now < end)
            {
                continue;
            }

            DoorOpenState final = (DoorOpenState)(state & OpenStateMask) == DoorOpenState.Opening ? DoorOpenState.Open : DoorOpenState.Closed;
            _states[index] = (WithOpenState(state, final), revision + 1, null);
        }
    }

    private static ushort WithOpenState(ushort state, DoorOpenState open) => (ushort)((state & ~OpenStateMask) | (ushort)open);
}
