namespace Ares.Structs.Game.Movement;

/// <summary>
/// The blackboard variable of a transition between two movement nodes, such as from ground movement into a slide and
/// back. It holds the transition's start and the slots of the node variables it blends from and to, and lasts
/// <see cref="Ticks"/> ticks.
/// </summary>
public static class PawnTransitionState
{
    /// <summary>Descriptor of the transition variable.</summary>
    public const int DescriptorId = 88;

    /// <summary>Unknown field: <see cref="SlideEntryUnknownField0Value"/> into a slide, <see cref="SlideExitUnknownField0Value"/> out of one.</summary>
    public const int UnknownField0 = 0;

    /// <summary>Value of <see cref="UnknownField0"/> on the transition into a slide.</summary>
    public const ulong SlideEntryUnknownField0Value = 2147484114;

    /// <summary>Value of <see cref="UnknownField0"/> on the transition out of a slide.</summary>
    public const ulong SlideExitUnknownField0Value = 2147484165;

    /// <summary>Field holding the transition's start: <see cref="PawnSlideState.TimeBias"/> plus the simulation time of its first tick.</summary>
    public const int StartTimeField = 1;

    /// <summary>Field holding the slot of the node variable the transition blends from.</summary>
    public const int FromSlotField = 2;

    /// <summary>Field holding the slot of the node variable the transition blends to.</summary>
    public const int ToSlotField = 3;

    /// <summary>Ticks a transition's variable lasts, its first tick included.</summary>
    public const int Ticks = 15;
}
