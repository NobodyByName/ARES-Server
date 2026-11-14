namespace Ares.Structs.Game.Movement;

/// <summary>
/// The slide's own blackboard variable. The client adds it to the pawn's blackboard on the tick its crouch press starts
/// a slide; from the next tick it holds the slide's initial velocity, maximum speed and accumulated slowdown.
/// </summary>
public static class PawnSlideState
{
    /// <summary>Slot of the slide-state variable.</summary>
    public const int Slot = 13;

    /// <summary>Position of the slide-state variable in the blackboard.</summary>
    public const int Position = 35;

    /// <summary>Size of the slide-state variable.</summary>
    public const int Size = 1;

    /// <summary>Descriptor of the slide-state variable.</summary>
    public const int DescriptorId = 78;

    /// <summary>Field holding the X component of the velocity the slide started with (Y and Z follow).</summary>
    public const int InitialDirectionXField = 0;

    /// <summary>World units per second mapped onto the initial-direction fields' 24-bit range around zero.</summary>
    public const double InitialDirectionRange = 1000000.0;

    /// <summary>Field holding the slope part of the slide input.</summary>
    public const int SlopeInputScalarField = 3;

    /// <summary>Field holding the slide's current maximum speed.</summary>
    public const int CurrentMaxMoveSpeedField = 4;

    /// <summary>World units per second mapped onto the maximum-speed field's range from zero.</summary>
    public const double MaxMoveSpeedRange = 2000.0;

    /// <summary>Field holding the accumulated slowdown, over its range from zero to one.</summary>
    public const int SlowDownAmountField = 5;

    /// <summary>Unknown field; the client stores <see cref="UnknownField6Value"/> in every slide.</summary>
    public const int UnknownField6 = 6;

    /// <summary>Value of <see cref="UnknownField6"/> in every slide.</summary>
    public const ulong UnknownField6Value = 2147483659;

    /// <summary>Field holding the slide's start: <see cref="TimeBias"/> plus the simulation time (in time units) of the tick that started it.</summary>
    public const int StartTimeField = 7;

    /// <summary>Value added to a time stored in the slide-state variable.</summary>
    public const ulong TimeBias = 1UL << 31;

    /// <summary>Field holding the slide's flags.</summary>
    public const int FlagsField = 10;

    /// <summary>Flag of a slide whose character is off the ground.</summary>
    public const ulong FallingFlag = 1UL << 2;

    /// <summary>Flag of a slide another action ends.</summary>
    public const ulong ForceCancelFlag = 1UL << 3;

    /// <summary>Flag of a slide that has consumed the sprint input.</summary>
    public const ulong ConsumedSprintInputFlag = 1UL << 4;

    /// <summary>Ticks after the tick that started a slide from which the slide has consumed the sprint input.</summary>
    public const int ConsumedSprintInputTicks = 8;
}
