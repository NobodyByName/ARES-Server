using Ares.Structs.Game.Movement;

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

/// <summary>
/// The class defaults of the standard interaction component (<c>UStandardInteractionComponent</c>): what a component
/// template that does not set a member takes. Its alignment time is
/// <see cref="InteractionMovementParameters.DefaultAlignmentSeconds"/>.
/// </summary>
public static class StandardInteractionDefaults
{
    /// <summary><c>InteractionType</c>.</summary>
    public const StandardInteractionType InteractionType = StandardInteractionType.Press;

    /// <summary><c>HoldDuration</c>, in seconds.</summary>
    public const double HoldSeconds = 0;

    /// <summary><c>InteractionServerHoldStartedTime</c>: the hold start time of an interaction nobody holds.</summary>
    public const double NoHoldStartedTime = -1;
}
