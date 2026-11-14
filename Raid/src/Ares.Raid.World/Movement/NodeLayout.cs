using Ares.Structs.Game.Movement;

namespace Ares.Raid.World.Movement;

/// <summary>
/// Which pawn slots hold the movement nodes' own variables. A node that starts takes the lowest free node slot (the
/// ground node's initial slot, then the slots after the fixed variables, laid out in turn); a slot whose node ends
/// keeps its place in the layout, empty. A slide adds the transition into it and the slide's variable on its press
/// tick; when the transition's <see cref="PawnTransitionState.Ticks"/> are over, it ends together with the ground
/// node. The slide's finishing tick adds the transition out of it and a new ground node; when that transition is over,
/// it ends together with the slide's variable. A slide another movement ends early (a roll, a vault, a jump, a ladder)
/// leaves its slots empty and the ground node running again.
/// </summary>
public readonly record struct NodeLayout
{
    /// <summary>The first node slot after the pawn's initial variables.</summary>
    public static int FirstExtraSlot { get; } = PawnBlackboard.InitialState.Where(variable => variable.Slot != PawnGroundNodeState.InitialSlot)
        .Max(variable => variable.Slot) + 1;

    private static readonly int s_firstExtraPosition = PawnBlackboard.InitialState.Max(variable => variable.Position + variable.Size);

    private static readonly int s_groundPosition = PawnBlackboard.InitialState.First(variable => variable.Slot == PawnGroundNodeState.InitialSlot).Position;

    /// <summary>The layout of the pawn's initial state: the ground node in its initial slot.</summary>
    public static NodeLayout Initial { get; } = new()
    {
        LaidOut = 1UL << PawnGroundNodeState.InitialSlot,
        GroundSlot = PawnGroundNodeState.InitialSlot,
        TransitionSlot = -1,
        SlideSlot = -1,
    };

    /// <summary>Node slots in the layout, one bit per slot.</summary>
    public ulong LaidOut { get; init; }

    /// <summary>Slot of the ground node's variable, or -1 while it has none.</summary>
    public int GroundSlot { get; init; }

    /// <summary>Slot of the slide's variable, or -1.</summary>
    public int SlideSlot { get; private init; }

    /// <summary>Slot of the running transition's variable, or -1.</summary>
    public int TransitionSlot { get; init; }

    /// <summary>Ticks the running transition has lasted after its first one.</summary>
    private int TransitionTicks { get; init; }

    /// <summary>Whether the running transition leaves a slide (else it enters one).</summary>
    public bool TransitionOut { get; private init; }

    /// <summary>First tick of the running transition.</summary>
    public uint TransitionStart { get; private init; }

    /// <summary>Slots of the node variables the running transition blends from and to.</summary>
    public (int From, int To) TransitionSlots { get; private init; }

    /// <summary>Slot of the active node's variable: the running transition's, else the ground node's, else the slide's.</summary>
    public int ActiveSlot => TransitionSlot >= 0 ? TransitionSlot : GroundSlot >= 0 ? GroundSlot : SlideSlot;

    /// <summary>Whether this tick added the ground node's variable.</summary>
    public bool GroundAdded { get; private init; }

    /// <summary>The slide's last state, which its variable keeps until it ends.</summary>
    public SlideState? Slide { get; private init; }

    /// <summary>The layout of a frame's slots, the running transition counted from its start.</summary>
    /// <param name="frame">The frame's slots.</param>
    /// <param name="tick">The frame's tick.</param>
    /// <returns>The layout.</returns>
    public static NodeLayout From(IReadOnlyDictionary<int, PendulumSlot> frame, uint tick)
    {
        // The frame's node slots follow its other variables (equipped weapons move them); the layout numbers them as
        // if they started at FirstExtraSlot.
        int firstSlot = frame.Values.Where(slot => slot.DescriptorId is { } descriptor && !IsNodeDescriptor(descriptor))
            .Max(slot => slot.Slot) + 1;
        NodeLayout layout = new()
        {
            LaidOut = frame.Keys.Where(slot => slot == PawnGroundNodeState.InitialSlot || slot >= firstSlot)
                .Aggregate(0UL, (mask, slot) => mask | (1UL << Logical(slot))),
            GroundSlot = Find(PawnGroundNodeState.DescriptorId),
            SlideSlot = Find(PawnSlideState.DescriptorId),
            TransitionSlot = Find(PawnTransitionState.DescriptorId),
        };
        if (layout.TransitionSlot < 0)
        {
            return layout;
        }

        IReadOnlyList<PendulumField> fields = PendulumRegistry.Descriptors[PawnTransitionState.DescriptorId].Fields;
        byte[] value = frame.Values.First(slot => slot.DescriptorId == PawnTransitionState.DescriptorId).Value;
        uint start = (uint)((PendulumCodec.Integer(fields[PawnTransitionState.StartTimeField], value) - PawnSlideState.TimeBias) / PawnBlackboard.TimePerTick);
        int from = Logical((int)PendulumCodec.Integer(fields[PawnTransitionState.FromSlotField], value));
        return layout with
        {
            TransitionStart = start,
            TransitionTicks = (int)(tick - start),
            TransitionSlots = (from, Logical((int)PendulumCodec.Integer(fields[PawnTransitionState.ToSlotField], value))),
            TransitionOut = from == layout.SlideSlot,
        };

        int Logical(int slot) => slot is PawnGroundNodeState.InitialSlot or < 0 ? slot : slot - firstSlot + FirstExtraSlot;
        int Find(int descriptor) => Logical(frame.Values.FirstOrDefault(slot => slot.DescriptorId == descriptor)?.Slot ?? -1);
    }

    // Whether a descriptor is a movement node's variable.
    private static bool IsNodeDescriptor(int descriptor) =>
        descriptor is PawnGroundNodeState.DescriptorId or PawnSlideState.DescriptorId or PawnTransitionState.DescriptorId;

    /// <summary>Position of a node slot's variable in the blackboard.</summary>
    /// <param name="slot">The node slot.</param>
    /// <returns>Its position.</returns>
    public static int Position(int slot) => slot == PawnGroundNodeState.InitialSlot ? s_groundPosition : s_firstExtraPosition + slot - FirstExtraSlot;

    /// <summary>The layout after a tick.</summary>
    /// <param name="frame">The tick's input frame.</param>
    /// <param name="slide">The slide this tick shows, or null.</param>
    /// <param name="exitTicks">Ticks of the slide's exit taken so far (1 on its finishing tick).</param>
    /// <returns>The next layout.</returns>
    public NodeLayout Next(uint frame, SlideState? slide, int exitTicks)
    {
        NodeLayout next = this with { GroundAdded = false, Slide = slide ?? Slide };
        if (slide is null && SlideSlot >= 0 && !(TransitionSlot >= 0 && TransitionOut))
        {
            // Another movement ended the slide before its exit: the slide's variables end and the ground node runs again.
            next = next with { SlideSlot = -1, Slide = null, TransitionSlot = -1, TransitionTicks = 0 };
            if (next.GroundSlot >= 0)
            {
                return next;
            }

            int restarted = next.Free();
            return next with { GroundSlot = restarted, LaidOut = next.LaidOut | (1UL << restarted), GroundAdded = true };
        }

        if (TransitionSlot >= 0)
        {
            next = TransitionTicks + 1 < PawnTransitionState.Ticks
                ? next with { TransitionTicks = TransitionTicks + 1 }
                : TransitionOut
                    ? next with { TransitionSlot = -1, TransitionTicks = 0, SlideSlot = -1, Slide = null }
                    : next with { TransitionSlot = -1, TransitionTicks = 0, GroundSlot = -1 };
        }

        if (slide is { Ticks: 0 } && next.SlideSlot < 0)
        {
            int transition = next.Free();
            int sliding = next.Free(transition);
            return next with
            {
                LaidOut = next.LaidOut | (1UL << transition) | (1UL << sliding),
                TransitionSlot = transition,
                TransitionTicks = 0,
                TransitionOut = false,
                TransitionStart = frame,
                TransitionSlots = (next.GroundSlot, sliding),
                SlideSlot = sliding,
                Slide = slide,
            };
        }

        if (exitTicks != 1 || slide is null || next.SlideSlot < 0)
        {
            return next;
        }

        next = next with { TransitionSlot = -1 };
        int exitTransition = next.Free();
        bool added = next.GroundSlot < 0;
        int ground = added ? next.Free(exitTransition) : next.GroundSlot;
        return next with
        {
            LaidOut = next.LaidOut | (1UL << exitTransition) | (1UL << ground),
            TransitionSlot = exitTransition,
            TransitionTicks = 0,
            TransitionOut = true,
            TransitionStart = frame,
            TransitionSlots = (next.SlideSlot, ground),
            GroundSlot = ground,
            GroundAdded = added,
        };
    }

    // The lowest node slot no node variable holds, other than one just taken.
    private int Free(int taken = -1)
    {
        for (int slot = PawnGroundNodeState.InitialSlot; slot < PendulumCodec.SlotCount; slot = slot == PawnGroundNodeState.InitialSlot ? FirstExtraSlot : slot + 1)
        {
            if (slot != GroundSlot && slot != SlideSlot && slot != TransitionSlot && slot != taken)
            {
                return slot;
            }
        }

        throw new InvalidOperationException("No free node slot");
    }
}
