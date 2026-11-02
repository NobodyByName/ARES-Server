namespace Ares.Structs.Game.World;

/// <summary>A regular door placed in a World Partition cell's persistent level.</summary>
/// <param name="Cell">Generated cell package.</param>
/// <param name="Actor">Door actor name in the cell.</param>
public readonly record struct DoorPlacement(string Cell, string Actor)
{
    /// <summary>Starts open (<c>bStartAsOpened</c>).</summary>
    public bool StartAsOpened { get; init; }

    /// <summary>Starts locked; opening needs the unlock interaction (<c>bStartAsLocked</c>).</summary>
    public bool StartAsLocked { get; init; }

    /// <summary>Starts barricaded; opening needs a breach (<c>bStartAsSealed</c>).</summary>
    public bool StartAsSealed { get; init; }

    /// <summary>Starts powered (<c>bStartAsPowered</c>); the door class default is true.</summary>
    public bool StartAsPowered { get; init; } = true;
}

/// <summary>Replicated open state of a door (<c>EDoorOpenState</c>).</summary>
public enum DoorOpenState : byte
{
    /// <summary>Closed, animation finished.</summary>
    Closed = 0,

    /// <summary>Open, animation finished.</summary>
    Open = 1,

    /// <summary>Plays the opening animation.</summary>
    Opening = 2,

    /// <summary>Plays the closing animation.</summary>
    Closing = 3,
}

/// <summary>Barricade state of a door (<c>EDoorSealState</c>).</summary>
public enum DoorSealState : byte
{
    /// <summary>Not set yet.</summary>
    Uninitialized = 0,

    /// <summary>Barricaded by the map.</summary>
    Sealed = 1,

    /// <summary>Not barricaded, or breached.</summary>
    Unsealed = 2,

    /// <summary>Blocked by a player's door blocker.</summary>
    SealedWithDoorBlocker = 3,
}

/// <summary>Lock state of a door (<c>EDoorLockState</c>).</summary>
public enum DoorLockState : byte
{
    /// <summary>Not set yet.</summary>
    Uninitialized = 0,

    /// <summary>Locked (red panel).</summary>
    Locked = 1,

    /// <summary>Unlocked (green panel).</summary>
    Unlocked = 2,
}

/// <summary>Power state of a door (<c>EDoorPowerState</c>).</summary>
public enum DoorPowerState : byte
{
    /// <summary>Not set yet.</summary>
    Uninitialized = 0,

    /// <summary>Without power (yellow panel).</summary>
    Unpowered = 1,

    /// <summary>Powered.</summary>
    Powered = 2,
}

/// <summary>Which way a door swings open (<c>EDoorSwingDirection</c>).</summary>
public enum DoorSwingDirection : byte
{
    /// <summary>The animation's own direction.</summary>
    Forward = 0,

    /// <summary>The mirrored direction.</summary>
    Reverse = 1,
}

/// <summary>
/// A door's replicated <c>DoorState</c> (uint16), packed as the door script does: open state in bits 0..2,
/// seal 3..5, lock 6..8, power 9..11 and swing direction 12..13.
/// </summary>
/// <param name="Open">Open state.</param>
/// <param name="Seal">Barricade state.</param>
/// <param name="Lock">Lock state.</param>
/// <param name="Power">Power state.</param>
/// <param name="Swing">Swing direction.</param>
public readonly record struct DoorState(DoorOpenState Open, DoorSealState Seal, DoorLockState Lock, DoorPowerState Power, DoorSwingDirection Swing)
{
    private const int SealShift = 3;
    private const int LockShift = 6;
    private const int PowerShift = 9;
    private const int SwingShift = 12;
    private const int StateMask = 7;
    private const int SwingMask = 3;

    /// <summary>Packs the state into its replicated value.</summary>
    /// <returns>The <c>DoorState</c> value.</returns>
    public ushort Pack() =>
        (ushort)((int)Open | ((int)Seal << SealShift) | ((int)Lock << LockShift) | ((int)Power << PowerShift) | ((int)Swing << SwingShift));

    /// <summary>Unpacks a replicated value.</summary>
    /// <param name="value">The <c>DoorState</c> value.</param>
    /// <returns>The state.</returns>
    public static DoorState Unpack(ushort value) => new(
        (DoorOpenState)(value & StateMask),
        (DoorSealState)((value >> SealShift) & StateMask),
        (DoorLockState)((value >> LockShift) & StateMask),
        (DoorPowerState)((value >> PowerShift) & StateMask),
        (DoorSwingDirection)((value >> SwingShift) & SwingMask));
}
