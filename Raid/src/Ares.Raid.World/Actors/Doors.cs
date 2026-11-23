using Ares.Common.GameData;
using Ares.Raid.World.Replication;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Game.Replication;
using Ares.Structs.Game.World;
using Ares.Structs.Net.Replication;
using Ares.Structs.Net.Session;

namespace Ares.Raid.World.Actors;

/// <summary>A regular map door with its exported ids.</summary>
/// <param name="Index">Index in the map's doors (<see cref="MapData.Doors"/>).</param>
/// <param name="Cell">World Partition cell package holding the door.</param>
/// <param name="Guid">The door actor.</param>
/// <param name="Buttons">Its open/close button components.</param>
/// <param name="Breach">Its breach button component.</param>
/// <param name="Unlock">Its unlock button component when the door starts locked with unlock items, else null.</param>
/// <param name="Keys">The items its unlock button asks for.</param>
/// <param name="Area">Its locked room's exploit prevention area actor, or null.</param>
public sealed record Door(int Index, string Cell, ulong Guid, IReadOnlyList<ulong> Buttons, ulong Breach, ulong? Unlock, IReadOnlyList<DoorKey> Keys, ulong? Area);

/// <summary>A locked room's exploit prevention area, replicated with its door's cell.</summary>
/// <param name="Index">Index in <see cref="DoorCatalog.Areas"/>.</param>
/// <param name="Door">The door of the room.</param>
/// <param name="Guid">The area actor.</param>
/// <param name="Placement">Where the map places it, with its trigger boxes.</param>
public sealed record ExploitArea(int Index, Door Door, ulong Guid, ExploitPreventionAreaPlacement Placement)
{
    /// <summary>Whether a location is inside one of the area's trigger boxes.</summary>
    /// <param name="location">World location, in centimetres.</param>
    /// <returns>Whether it is inside.</returns>
    public bool Contains(Vector3D location) => Placement.Triggers.Any(box => Inside(box, location));

    private static bool Inside(ExploitTriggerBox box, Vector3D location)
    {
        Vector3D offset = new(location.X - box.Center.X, location.Y - box.Center.Y, location.Z - box.Center.Z);
        return Math.Abs(Dot(offset, box.Rotation.Rotate(new Vector3D(1, 0, 0)))) <= box.HalfExtent.X
            && Math.Abs(Dot(offset, box.Rotation.Rotate(new Vector3D(0, 1, 0)))) <= box.HalfExtent.Y
            && Math.Abs(Dot(offset, box.Rotation.Rotate(new Vector3D(0, 0, 1)))) <= box.HalfExtent.Z;
    }

    private static double Dot(Vector3D a, Vector3D b) => (a.X * b.X) + (a.Y * b.Y) + (a.Z * b.Z);
}

/// <summary>An item a locked door's unlock button asks for.</summary>
/// <param name="AssetIndexes">Asset indexes of the catalogue items of the required item class.</param>
/// <param name="Amount">How many of them the player must carry.</param>
/// <param name="Consume">Unlocking takes them from the player.</param>
public sealed record DoorKey(IReadOnlySet<ushort> AssetIndexes, int Amount, bool Consume);

/// <summary>
/// The regular Dam doors. The client loads them with their World Partition cells; their buttons stay
/// disabled until the server replicates their state, so the server references each door by its map path
/// (cell package, world, persistent level, actor and its named buttons). Doors that start locked with unlock items
/// also have their unlock button replicated.
/// </summary>
public sealed class DoorCatalog
{
    private readonly ReplicationContext _replication;
    private readonly Dictionary<ulong, Door> _buttons = [];
    private readonly Dictionary<ulong, Door> _breaches = [];
    private readonly Dictionary<ulong, Door> _unlocks = [];

    /// <summary>Names the doors.</summary>
    /// <param name="replication">Replication context (knows the interaction-state enum).</param>
    /// <param name="paths">Map path ids.</param>
    /// <param name="map">The map's data: its doors.</param>
    /// <param name="items">The item catalogue, which names the unlock items' classes.</param>
    /// <exception cref="WorldPreparationException">An unlock item's class is not in the catalogue, or a door names an unknown exploit prevention area.</exception>
    public DoorCatalog(ReplicationContext replication, MapPaths paths, MapData map, ItemCatalog items)
    {
        _replication = replication ?? throw new ArgumentNullException(nameof(replication));
        ArgumentNullException.ThrowIfNull(paths);
        ArgumentNullException.ThrowIfNull(map);
        ArgumentNullException.ThrowIfNull(items);
        Dictionary<string, HashSet<ushort>> byClass = items.All.GroupBy(item => item.ItemClass, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Select(item => checked((ushort)item.UsableIndex)).ToHashSet(), StringComparer.Ordinal);
        List<Door> doors = [];
        foreach (Door door in map.Doors.Select((placement, index) => Create(index, placement)))
        {
            doors.Add(door);
            _breaches[door.Breach] = door;
            if (door.Unlock is { } unlock)
            {
                _unlocks[unlock] = door;
            }

            foreach (ulong button in door.Buttons)
            {
                _buttons[button] = door;
            }
        }

        Exports = paths.Exports();
        Doors = doors;
        Dictionary<(string Cell, string Actor), ExploitPreventionAreaPlacement> areas = map.ExploitPreventionAreas.ToDictionary(area => (area.Cell, area.Actor));
        Areas = [.. doors.Where(door => door.Area is not null).Select((door, index) => new ExploitArea(index, door, door.Area!.Value,
            areas.GetValueOrDefault((door.Cell, map.Doors[door.Index].ExploitPreventionArea!))
                ?? throw new WorldPreparationException($"Door {map.Doors[door.Index].Actor} names the unknown exploit prevention area {map.Doors[door.Index].ExploitPreventionArea}")))];

        return;

        Door Create(int index, DoorPlacement placement)
        {
            ulong actor = paths.MapObject(paths.Level(placement.Cell), placement.Actor);
            bool lockedWithKeys = placement is { StartAsLocked: true, UnlockItems.Length: > 0 };
            DoorKey[] keys = [.. placement.UnlockItems.Select(item => new DoorKey(
                byClass.TryGetValue(item.ItemClass, out HashSet<ushort>? indexes)
                    ? indexes
                    : throw new WorldPreparationException($"Door {placement.Actor} asks for the unknown item class {item.ItemClass}"),
                item.Amount, item.Consume))];
            return new Door(index, placement.Cell, actor, [.. DoorClasses.InteractionComponents.Select(name => paths.MapObject(actor, name))],
                paths.MapObject(actor, DoorClasses.BreachInteractionComponent),
                lockedWithKeys ? paths.MapObject(actor, DoorClasses.UnlockInteractionComponent) : null, keys,
                placement.ExploitPreventionArea is { } area ? paths.MapObject(paths.Level(placement.Cell), area) : null);
        }
    }

    /// <summary>The door exports.</summary>
    private ObjectExports Exports { get; }

    /// <summary>All doors.</summary>
    public IReadOnlyList<Door> Doors { get; }

    /// <summary>The exploit prevention areas of the locked rooms, in door order.</summary>
    public IReadOnlyList<ExploitArea> Areas { get; }

    /// <summary>The door a button belongs to.</summary>
    /// <param name="button">Button id.</param>
    /// <returns>The door, or null.</returns>
    public Door? ByButton(ulong button) => _buttons.GetValueOrDefault(button);

    /// <summary>The door a breach button belongs to.</summary>
    /// <param name="button">Breach button id.</param>
    /// <returns>The door, or null.</returns>
    public Door? ByBreach(ulong button) => _breaches.GetValueOrDefault(button);

    /// <summary>The door an unlock button belongs to.</summary>
    /// <param name="button">Unlock button id.</param>
    /// <returns>The door, or null.</returns>
    public Door? ByUnlock(ulong button) => _unlocks.GetValueOrDefault(button);

    /// <summary>
    /// Exports a door's buttons (with the door and its cell): the open/close and breach buttons in one bunch, the unlock
    /// button in another (every export repeats the whole path, and all four do not fit in one packet).
    /// </summary>
    /// <param name="door">The door.</param>
    /// <returns>The bunches.</returns>
    public IReadOnlyList<ChannelBunch> ExportBunches(Door door)
    {
        ArgumentNullException.ThrowIfNull(door);
        ChannelBunch buttons = Bunches.Exports(Exports, [.. door.Buttons, door.Breach]);
        return door.Unlock is { } unlock ? [buttons, Bunches.Exports(Exports, [unlock])] : [buttons];
    }

    /// <summary>Exports an exploit prevention area (with its cell).</summary>
    /// <param name="area">The area.</param>
    /// <returns>The bunch.</returns>
    public ChannelBunch AreaExportBunch(ExploitArea area)
    {
        ArgumentNullException.ThrowIfNull(area);
        return Bunches.Exports(Exports, [area.Guid]);
    }

    /// <summary>Opens an exploit prevention area's channel with its state.</summary>
    /// <param name="area">The area.</param>
    /// <param name="channel">Channel index.</param>
    /// <param name="sequence">Reliable sequence number.</param>
    /// <param name="state">Area state.</param>
    /// <returns>The bunch.</returns>
    public ChannelBunch AreaOpenBunch(ExploitArea area, int channel, int sequence, ExploitPreventionAreaState state)
    {
        ArgumentNullException.ThrowIfNull(area);
        ActorContentBlock actor = _replication.Encode(DoorClasses.ExploitPreventionAreaClass, new Dictionary<string, object?>
        {
            [PropertyNames.Role] = (ulong)NetRole.Authority,
            [PropertyNames.RemoteRole] = (ulong)NetRole.SimulatedProxy,
            [PropertyNames.SystemState] = (ulong)state,
        });
        return new StaticActorInitialization(new StaticActorReference(area.Guid), [actor]).ToBunch(channel, sequence);
    }

    /// <summary>Updates an open exploit prevention area channel.</summary>
    /// <param name="channel">Channel index.</param>
    /// <param name="state">Area state.</param>
    /// <returns>The bunch.</returns>
    public ChannelBunch AreaUpdateBunch(int channel, ExploitPreventionAreaState state)
    {
        ActorContentBlock actor = _replication.Encode(DoorClasses.ExploitPreventionAreaClass, new Dictionary<string, object?> { [PropertyNames.SystemState] = (ulong)state });
        return Bunches.Actor(channel, actor.Write);
    }

    /// <summary>Opens the door's channel with its state and button state.</summary>
    /// <param name="door">The door.</param>
    /// <param name="channel">Channel index.</param>
    /// <param name="sequence">Reliable sequence number.</param>
    /// <param name="buttons">Open/close, breach and unlock button states.</param>
    /// <param name="doorState">Door state.</param>
    /// <returns>The bunch.</returns>
    public ChannelBunch OpenBunch(Door door, int channel, int sequence, DoorButtons buttons, DoorState doorState)
    {
        ArgumentNullException.ThrowIfNull(door);
        ActorContentBlock actor = _replication.Encode(DoorClasses.ActorClass, new Dictionary<string, object?>
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
    /// <param name="buttons">Open/close, breach and unlock button states.</param>
    /// <returns>The bunch.</returns>
    public ChannelBunch UpdateBunch(Door door, int channel, DoorState doorState, DoorButtons buttons)
    {
        ArgumentNullException.ThrowIfNull(door);
        ActorContentBlock actor = _replication.Encode(DoorClasses.ActorClass, new Dictionary<string, object?> { [PropertyNames.DoorState] = doorState.Pack() });
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

    private StaticSubobjectContent[] ButtonContent(Door door, DoorButtons buttons)
    {
        StaticSubobjectContent[] content = [.. door.Buttons.Select(guid => new StaticSubobjectContent(guid, Button(buttons.OpenClose))), new(door.Breach, Button(buttons.Breach))];
        return door.Unlock is { } unlock ? [.. content, new StaticSubobjectContent(unlock, Button(buttons.Unlock))] : content;
    }

    private ActorContentBlock Button(CurrentInteractionState state) =>
        _replication.Encode(DoorClasses.InteractionClass, new Dictionary<string, object?> { [PropertyNames.CurrentInteractionState] = (ulong)state });
}

/// <summary>States of a door's buttons.</summary>
/// <param name="OpenClose">Both open/close buttons.</param>
/// <param name="Breach">The breach button.</param>
/// <param name="Unlock">The unlock button (replicated only on doors that have one).</param>
public readonly record struct DoorButtons(CurrentInteractionState OpenClose, CurrentInteractionState Breach, CurrentInteractionState Unlock);

/// <summary>
/// Door state of one raid, shared by its connections. Each door starts as the map places it (open, locked,
/// barricaded, powered). A press on a usable door starts the opening or closing animation, which completes
/// after a fixed time. Holding a barricaded door's breach button removes the barricade once the alignment and
/// hold time have passed; doors whose map flag says so then open. A cancelled breach starts over. A locked door with
/// unlock items stays unlocked once a player unlocks it; it cannot be unlocked while its room's exploit prevention area
/// fires. Changes carry a revision so each connection sends them once.
/// </summary>
public sealed class DoorStates
{
    private const double AnimationSeconds = 2.0;
    private const double BreachSeconds = DoorClasses.BreachAlignmentSeconds + DoorClasses.BreachHoldSeconds;

    private readonly IReadOnlyList<DoorPlacement> _placements;
    private readonly Dictionary<int, (DoorState State, int Revision, double? Until)> _states = [];
    private readonly Dictionary<int, double> _breaches = [];
    private readonly HashSet<int> _guarded = [];

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

    /// <summary>Whether a door's unlock button works: the door is locked, has unlock items and its room's area does not fire.</summary>
    /// <param name="index">Door index.</param>
    /// <returns>True while the door waits for its unlock items.</returns>
    public bool Unlockable(int index) => _states[index].State.Lock == DoorLockState.Locked && HasUnlock(index) && !_guarded.Contains(index);

    /// <summary>Marks whether a door's room fires its exploit prevention area, which keeps the door from being unlocked.</summary>
    /// <param name="index">Door index.</param>
    /// <param name="firing">Whether the area fires.</param>
    public void SetRoomFiring(int index, bool firing)
    {
        if (firing ? !_guarded.Add(index) : !_guarded.Remove(index))
        {
            return;
        }

        (DoorState state, int revision, double? until) = Get(index);
        _states[index] = (state, revision + 1, until);
    }

    /// <summary>States of a door's buttons.</summary>
    /// <param name="index">Door index.</param>
    /// <returns>
    /// Open/close: Disabled on unusable doors, SuccessIdle while the door moves, Idle otherwise. Breach: Idle on
    /// a barricaded door, Ongoing while it is breached, SuccessDisabled once breached (the client plays the
    /// interaction's success ending on that state; Idle or Disabled would end it as failed), Disabled on doors
    /// that never had a barricade. Unlock: Idle on a locked door with unlock items, Disabled while its room's area fires,
    /// SuccessDisabled once unlocked (as for the breach), Disabled otherwise.
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
        CurrentInteractionState unlock = Unlockable(index) ? CurrentInteractionState.Idle
            : HasUnlock(index) && _placements[index].StartAsLocked && _states[index].State.Lock == DoorLockState.Unlocked ? CurrentInteractionState.SuccessDisabled
            : CurrentInteractionState.Disabled;

        return new DoorButtons(openClose, breach, unlock);
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

    /// <summary>
    /// Whether a door's room is still locked: false once a door that started locked is unlocked (the unlock cleans up
    /// the room's exploit prevention).
    /// </summary>
    /// <param name="index">Door index.</param>
    /// <returns>True until the door is unlocked.</returns>
    public bool RoomLocked(int index) => !(_placements[index].StartAsLocked && _states[index].State.Lock == DoorLockState.Unlocked);

    /// <summary>Unlocks a locked door with unlock items; its open/close buttons then work (unless it is barricaded or without power).</summary>
    /// <param name="index">Door index.</param>
    /// <returns>Whether the door was unlocked.</returns>
    public bool Unlock(int index)
    {
        if (!Unlockable(index))
        {
            return false;
        }

        (DoorState state, int revision, double? until) = Get(index);
        _states[index] = (state with { Lock = DoorLockState.Unlocked }, revision + 1, until);
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

    private bool HasUnlock(int index) => _placements[index].UnlockItems.Length > 0;

    // What the door script applies at BeginPlay from the placed door's start flags.
    private static DoorState StartState(DoorPlacement placement) => new(
        placement.StartAsOpened ? DoorOpenState.Open : DoorOpenState.Closed,
        placement.StartAsSealed ? DoorSealState.Sealed : DoorSealState.Unsealed,
        placement.StartAsLocked ? DoorLockState.Locked : DoorLockState.Unlocked,
        placement.StartAsPowered ? DoorPowerState.Powered : DoorPowerState.Unpowered,
        DoorSwingDirection.Forward);
}
