namespace Ares.Structs.Game.World;

/// <summary>State of an interaction component (<c>ECurrentInteractionState</c>).</summary>
public enum CurrentInteractionState : byte
{
    Uninitialized = 0,
    Disabled = 1,
    Idle = 2,
    Ongoing = 3,
    Succeeding = 4,
    Cancelling = 5,
    SuccessIdle = 6,
    SuccessDisabled = 7,
}

/// <summary>Type of a standard interaction button (<c>EStandardInteractionType</c>).</summary>
public enum StandardInteractionType : byte
{
    Press = 0,
    Hold = 1,
    Continuous = 2,
}
