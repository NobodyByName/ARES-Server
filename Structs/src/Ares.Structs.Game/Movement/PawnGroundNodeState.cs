namespace Ares.Structs.Game.Movement;

/// <summary>
/// The ground-movement node's own blackboard variable: the node's input, speed and stance state and the times it
/// keeps between ticks. The pawn's initial state holds it at <see cref="InitialSlot"/>.
/// </summary>
public static class PawnGroundNodeState
{
    /// <summary>Slot of the ground-node variable in the pawn's initial state.</summary>
    public const int InitialSlot = 8;

    /// <summary>Descriptor of the ground-node variable.</summary>
    public const int DescriptorId = 69;

    /// <summary>Unknown field; it holds <see cref="UnknownField0MovingOffset"/> above its default while movement input is held.</summary>
    public const int UnknownField0 = 0;

    /// <summary>Offset of <see cref="UnknownField0"/> from its default while movement input is held on level ground.</summary>
    public const ulong UnknownField0MovingOffset = 8;

    /// <summary>Field holding the slope part of the ground input.</summary>
    public const int SlopeInputScalarField = 1;

    /// <summary>Field holding the ground movement's current maximum speed, over <see cref="PawnSlideState.MaxMoveSpeedRange"/> from zero.</summary>
    public const int CurrentMaxMoveSpeedField = 2;

    /// <summary>Unknown field; every node variable holds <see cref="UnknownField4Value"/>.</summary>
    public const int UnknownField4 = 4;

    /// <summary>Value of <see cref="UnknownField4"/>.</summary>
    public const ulong UnknownField4Value = 2147483652;

    /// <summary>
    /// Field holding the node's start: <see cref="PawnSlideState.TimeBias"/> plus the simulation time of the tick the
    /// variable was added. The node's other times count from it.
    /// </summary>
    public const int NodeStartTimeField = 5;

    /// <summary>Field holding the time since the node's start of the tick the current movement input started (<see cref="PawnSlideState.TimeBias"/> added).</summary>
    public const int StartedInputTimeField = 8;

    /// <summary>Field holding the time since the node's start of the tick the latest sprint started (<see cref="PawnSlideState.TimeBias"/> added).</summary>
    public const int StartSprintTimeField = 9;

    /// <summary>Field holding the node's flags.</summary>
    public const int FlagsField = 11;

    /// <summary>Flag of a crouched character shuffling at reduced speed.</summary>
    public const ulong ShufflingFlag = 1UL << 2;

    /// <summary>Field holding the eight-way direction the node moves in (<see cref="InputDirection"/>).</summary>
    public const int ResultingInputDirectionField = 13;

    /// <summary>Field holding the eight-way direction of the player's movement input (<see cref="InputDirection"/>).</summary>
    public const int PlayerInputDirectionField = 14;

    /// <summary>Field holding the node's locomotion.</summary>
    public const int LocoModeField = 15;

    /// <summary>Field holding the node's stance.</summary>
    public const int CharacterStanceField = 16;
}
