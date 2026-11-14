using Ares.Structs.Core.Geometry;
using Ares.Structs.Core.Serialization;
using Ares.Structs.Game.Movement;

namespace Ares.Raid.World.Movement;

/// <summary>The character's movement in a pawn frame.</summary>
/// <param name="Position">World position.</param>
/// <param name="Velocity">Velocity in world units per second.</param>
/// <param name="Yaw">Facing in radians, or null to keep the initial facing.</param>
/// <param name="Character">Simulated stance, locomotion and flashlight, or null to keep the initial values.</param>
/// <param name="ActionRotation">The coarse control rotation of the tick's input (<see cref="PendulumInput.ActionRotationField"/>), which the action state takes over; null to use <paramref name="Yaw"/>.</param>
public readonly record struct PawnMotion(Vector3D Position, Vector3D Velocity = default, double? Yaw = null, CharacterState? Character = null,
    PendulumQuaternion? ActionRotation = null);

/// <summary>
/// The pawn's blackboard frames. The first (reset) frame holds the initial state at the client's latest input
/// frame with the spawn position and starts the client's character simulation, which then runs from the
/// client's own input. The client applies each later frame as its full character state, so later frames carry
/// the simulated velocity, stance and flashlight with the simulated position, or the position the client reported
/// when the body is not simulated.
/// </summary>
public static class PawnBlackboardFrames
{
    /// <summary>Earliest input frame a frame may use.</summary>
    public const uint FirstTick = 26;

    /// <summary>Latest input frame a frame may use.</summary>
    public const uint LastTick = 400_000;

    private const int PositionBits = 24;

    // The slide tick from which the action state's clock stops.
    private const int SlideClockStopTicks = 3;

    // Horizontal normal components below this are float noise of the ground plane fit.
    private const double LevelNormalTolerance = 1e-5;

    private static readonly PendulumQuaternionField s_actionRotation = (PendulumQuaternionField)PendulumRegistry.Descriptors[
        PawnBlackboard.InitialState.First(variable => variable.Slot == PawnBlackboard.ActionStateSlot).DescriptorId].Fields[PawnBlackboard.ActionRotationField];

    private static readonly PendulumScalarField s_inputStrength = (PendulumScalarField)PendulumRegistry.Descriptors[
        PawnBlackboard.InitialState.First(variable => variable.Slot == PawnBlackboard.ActionStateSlot).DescriptorId].Fields[PawnBlackboard.ActionInputStrengthField];

    private static readonly PendulumScalarField s_timeOffset = (PendulumScalarField)PendulumRegistry.Descriptors[
        PawnBlackboard.InitialState.First(variable => variable.Slot == PawnBlackboard.GraphStateSlot).DescriptorId].Fields[PawnBlackboard.TimeOffsetField];

    private static readonly IReadOnlyList<PendulumField> s_movementFields =
        PendulumRegistry.Descriptors[PawnBlackboard.InitialState.First(variable => variable.Slot == PawnBlackboard.MovementStateSlot).DescriptorId].Fields;

    private static readonly IReadOnlyList<PendulumField> s_slideFields = PendulumRegistry.Descriptors[PawnSlideState.DescriptorId].Fields;

    /// <summary>The pawn's state at a tick.</summary>
    /// <param name="tick">Simulation tick (an input frame of the client).</param>
    /// <param name="motion">Character movement.</param>
    /// <param name="focus">Interaction the character may use (its focused button), or 0 for none.</param>
    /// <param name="inventory">The player's container state.</param>
    /// <param name="weapons">Weapon simulation variables replacing or extending the initial state.</param>
    /// <param name="timeOffset">Time units from the tick's simulation time to the server's time (see <see cref="PawnBlackboard.TimeOffsetField"/>).</param>
    /// <param name="interaction">The interaction under way and its last event, if the server runs one (an open container).</param>
    /// <returns>The slots by index.</returns>
    public static IReadOnlyDictionary<int, PendulumSlot> State(uint tick, PawnMotion motion, ulong focus, PendulumVariable? inventory = null,
        IReadOnlyList<PendulumVariable>? weapons = null, long timeOffset = 0, PawnInteraction? interaction = null)
    {
        long storedOffset = s_timeOffset.Default + timeOffset;
        if (storedOffset < 0 || storedOffset >= s_timeOffset.Maximum)
        {
            throw new InvalidPacketException("Time offset outside its range");
        }

        if (tick is < FirstTick or > LastTick)
        {
            throw new InvalidPacketException("Blackboard tick outside its range");
        }

        Dictionary<int, PendulumSlot> slots = [];
        NodeSlots nodeSlots = NodeSlots.After(PawnBlackboard.InitialState.Where(initial => initial.Slot != PawnGroundNodeState.InitialSlot)
            .Select(initial => inventory is not null && initial.Slot == inventory.Slot ? inventory : initial).Concat(weapons ?? []));
        foreach (PendulumVariable initial in PawnBlackboard.InitialState.Where(initial => weapons is null || weapons.All(item => item.Slot != initial.Slot))
            .Concat(weapons ?? []))
        {
            PendulumVariable variable = inventory is not null && initial.Slot == inventory.Slot ? inventory : initial;
            List<PendulumValue> values = [.. variable.Values];
            switch (variable.Slot)
            {
                case PawnBlackboard.GraphStateSlot:
                    values.Add(new PendulumIntegerValue(PawnBlackboard.TickField, tick));
                    values.Add(new PendulumIntegerValue(PawnBlackboard.TimeField, (ulong)tick * PawnBlackboard.TimePerTick));
                    Replace(values, new PendulumIntegerValue(PawnBlackboard.TimeOffsetField, (ulong)storedOffset));
                    if (motion.Character is { } graph)
                    {
                        // The simulation decides its own flags; the others keep their initial values.
                        ulong flags = values.OfType<PendulumIntegerValue>().LastOrDefault(value => value.Field == PawnBlackboard.GraphTagsField)?.Value ?? 0;
                        ulong simulated = GraphStateFlags.Simulated | (graph.HeldItem is not null ? GraphStateFlags.HeldItem : 0);
                        Replace(values, new PendulumIntegerValue(PawnBlackboard.GraphTagsField, (flags & ~simulated) | graph.GraphFlags));
                        ulong upper = values.OfType<PendulumIntegerValue>().LastOrDefault(value => value.Field == PawnBlackboard.GraphUpperTagsField)?.Value ?? 0;
                        Replace(values, new PendulumIntegerValue(PawnBlackboard.GraphUpperTagsField, (upper & ~GraphStateFlags.SimulatedUpper) | graph.UpperGraphFlags));
                    }

                    break;
                case PawnBlackboard.ActionStateSlot:
                    if (motion.ActionRotation is { } actionRotation)
                    {
                        Replace(values, new PendulumQuaternionValue(PawnBlackboard.ActionRotationField, actionRotation));
                    }
                    else if (motion.Yaw is { } actionYaw)
                    {
                        Replace(values, new PendulumQuaternionValue(PawnBlackboard.ActionRotationField, PendulumRotations.FromYaw(actionYaw, s_actionRotation)));
                    }

                    if (motion.Character is { } action)
                    {
                        Replace(values, new PendulumIntegerValue(PawnBlackboard.ActionStanceField, (ulong)action.Stance));
                        Replace(values, new PendulumIntegerValue(PawnBlackboard.ActionLocoModeField, (ulong)action.Locomotion));
                        Replace(values, new PendulumIntegerValue(PawnBlackboard.ActionInputDirectionField, (ulong)action.GroundNode.InputDirection));
                        Replace(values, new PendulumIntegerValue(PawnBlackboard.ActionInputStrengthField,
                            (ulong)Math.Round(action.GroundNode.InputStrength * (s_inputStrength.Maximum - 1))));
                        Replace(values, new PendulumIntegerValue(PawnBlackboard.ActionClockField, ActionClock(tick, action.Slide)));
                        Replace(values, new PendulumIntegerValue(PawnBlackboard.BlockItemFromSprintTimerField, action.GroundNode.SprintTimer));
                        // An aim that starts blocks the sprint input for its tick.
                        ulong aimSprintBlock = action.HeldItem is { ZoomStarted: true } ? PawnBlackboard.TimePerTick : 0UL;
                        Replace(values, new PendulumIntegerValue(PawnBlackboard.BlockSprintInputTimerField, Math.Max(SlideSprintTimer(action.Slide), aimSprintBlock)));
                    }

                    break;
                case PawnBlackboard.MovementStateSlot:
                    values.Add(new PendulumIntegerValue(PawnBlackboard.PositionXField, Quantize(motion.Position.X)));
                    values.Add(new PendulumIntegerValue(PawnBlackboard.PositionYField, Quantize(motion.Position.Y)));
                    values.Add(new PendulumIntegerValue(PawnBlackboard.PositionZField, Quantize(motion.Position.Z)));
                    Replace(values, Velocity(PawnBlackboard.VelocityXField, motion.Velocity.X));
                    Replace(values, Velocity(PawnBlackboard.VelocityYField, motion.Velocity.Y));
                    Replace(values, Velocity(PawnBlackboard.VelocityZField, motion.Velocity.Z));
                    if (motion.Character is { } character)
                    {
                        Replace(values, new PendulumIntegerValue(PawnBlackboard.CharacterStanceField, (ulong)character.MovementStance));
                        Replace(values, new PendulumIntegerValue(PawnBlackboard.LocoModeField, (ulong)character.MovementLocomotion));
                        Replace(values, new PendulumIntegerValue(PawnBlackboard.ActiveNodeSlotField, (ulong)nodeSlots.Slot(character.Nodes.ActiveSlot)));
                        if (character.UnknownMovementVector is { } vector)
                        {
                            Replace(values, new PendulumIntegerValue(PawnBlackboard.UnknownMovementVectorXField, vector.X));
                            Replace(values, new PendulumIntegerValue(PawnBlackboard.UnknownMovementVectorXField + 1, vector.Y));
                            Replace(values, new PendulumIntegerValue(PawnBlackboard.UnknownMovementVectorXField + 2, vector.Z));
                        }

                        ulong movementFlags = values.OfType<PendulumIntegerValue>().LastOrDefault(value => value.Field == PawnBlackboard.MovementFlagsField)?.Value ?? 0;
                        bool crouched = character.Stance is CharacterStance.Crouch or CharacterStance.Slide || character.MovementStance == CharacterStance.Crouch;
                        Replace(values, new PendulumIntegerValue(PawnBlackboard.MovementFlagsField,
                            crouched ? movementFlags | PawnBlackboard.CrouchedMovementFlag : movementFlags & ~PawnBlackboard.CrouchedMovementFlag));
                        if (character.Body is { } body)
                        {
                            Replace(values, new PendulumIntegerValue(PawnBlackboard.RoughMovementModeField,
                                (ulong)(body is { Airborne: true, Touching: false } ? RoughMovementMode.Air : RoughMovementMode.Ground)));
                            Replace(values, new PendulumIntegerValue(PawnBlackboard.SurfaceTypeField, (ulong)body.Ground.Surface));
                            Replace(values, new PendulumQuaternionValue(PawnBlackboard.GroundNormalField, GroundNormal(body.Ground.Normal)));
                            if (!body.Airborne)
                            {
                                Replace(values, GroundDistance(body.GroundDistance));
                                ulong traced = values.OfType<PendulumIntegerValue>().Last(value => value.Field == PawnBlackboard.MovementFlagsField).Value;
                                Replace(values, new PendulumIntegerValue(PawnBlackboard.MovementFlagsField, body.GroundDistance < 0
                                    ? traced | PawnBlackboard.FeetUnderGroundMovementFlag
                                    : traced & ~PawnBlackboard.FeetUnderGroundMovementFlag));
                            }
                        }
                    }

                    if (motion.Yaw is { } yaw)
                    {
                        PendulumQuaternionField facing = (PendulumQuaternionField)s_movementFields[PawnBlackboard.FacingField];
                        values.RemoveAll(value => value.Field == PawnBlackboard.FacingField);
                        values.Add(new PendulumQuaternionValue(PawnBlackboard.FacingField, PendulumRotations.FromYaw(yaw, facing)));
                    }

                    break;
                case PawnBlackboard.InteractorSlot when focus != 0 || interaction is not null:
                    if (focus != 0)
                    {
                        values.Add(new PendulumIntegerValue(PawnBlackboard.FocusedInteractionField, focus));
                    }

                    // A character busy with an interaction cannot use the focused one.
                    if (focus != 0 && interaction is not { Event: LastInteractionEvent.Started or LastInteractionEvent.Restaged })
                    {
                        values.Add(new PendulumIntegerValue(PawnBlackboard.InteractorFlagsField, PawnBlackboard.CanAccessFocusedInteractionFlag));
                    }

                    if (interaction is { } current)
                    {
                        values.Add(new PendulumIntegerValue(PawnBlackboard.CurrentInteractionField, current.Current));
                        values.Add(new PendulumIntegerValue(PawnBlackboard.LastInteractionEventField, (ulong)current.Event));
                        values.Add(new PendulumIntegerValue(PawnBlackboard.InteractionRequestedTimeField, InteractionTime(current.RequestedAt)));
                        values.Add(new PendulumIntegerValue(PawnBlackboard.AlignmentStartedTimeField, InteractionTime(current.AlignedAt)));
                        values.Add(new PendulumIntegerValue(PawnBlackboard.InteractionStartedTimeField, InteractionTime(current.StartedAt)));
                        values.Add(new PendulumIntegerValue(PawnBlackboard.InteractionSequenceField, 0x80000000UL + current.Sequence));
                        values.Add(new PendulumIntegerValue(PawnBlackboard.InteractionLocationXField, InteractionTime(current.Location.X)));
                        values.Add(new PendulumIntegerValue(PawnBlackboard.InteractionLocationXField + 1, InteractionTime(current.Location.Y)));
                        values.Add(new PendulumIntegerValue(PawnBlackboard.InteractionLocationXField + 2, InteractionTime(current.Location.Z)));
                        if (current.Animation != 0)
                        {
                            values.Add(new PendulumIntegerValue(PawnBlackboard.InteractionAnimationField, current.Animation));
                        }
                    }

                    break;
            }

            slots[variable.Slot] = new PendulumSlot(variable.Slot, variable.Position, variable.Size, variable.DescriptorId, PendulumCodec.Default.Value(variable.DescriptorId, values));
        }

        // The movement nodes' own variables in the slots the simulation laid out for them; a slot whose node ended stays empty.
        if (motion.Character is not { } nodes)
        {
            return slots;
        }

        for (int slot = 0; slot < PendulumCodec.SlotCount; slot++)
        {
            if ((nodes.Nodes.LaidOut & (1UL << slot)) == 0)
            {
                continue;
            }

            PendulumSlot node = NodeSlot(tick, slot, nodes, nodeSlots);
            slots[node.Slot] = node;
        }

        return slots;
    }

    // A node variable in the slot the client numbers it with (see NodeSlots).
    private static PendulumSlot NodeSlot(uint tick, int node, CharacterState character, NodeSlots nodeSlots)
    {
        NodeLayout layout = character.Nodes;
        int slot = nodeSlots.Slot(node);
        int position = nodeSlots.Position(slot);
        if (node == layout.GroundSlot)
        {
            return new PendulumSlot(slot, position, 1, PawnGroundNodeState.DescriptorId,
                PendulumCodec.Default.Value(PawnGroundNodeState.DescriptorId, character.GroundNode.Values()));
        }

        if (node == layout.TransitionSlot)
        {
            List<PendulumValue> transition =
            [
                new PendulumIntegerValue(PawnTransitionState.UnknownField0,
                    layout.TransitionOut ? PawnTransitionState.SlideExitUnknownField0Value : PawnTransitionState.SlideEntryUnknownField0Value),
                new PendulumIntegerValue(PawnTransitionState.StartTimeField, PawnSlideState.TimeBias + ((ulong)layout.TransitionStart * PawnBlackboard.TimePerTick)),
                new PendulumIntegerValue(PawnTransitionState.FromSlotField, (ulong)nodeSlots.Slot(layout.TransitionSlots.From)),
                new PendulumIntegerValue(PawnTransitionState.ToSlotField, (ulong)nodeSlots.Slot(layout.TransitionSlots.To)),
            ];
            return new PendulumSlot(slot, position, 1, PawnTransitionState.DescriptorId, PendulumCodec.Default.Value(PawnTransitionState.DescriptorId, transition));
        }

        if (node == layout.SlideSlot && (character.Slide ?? layout.Slide) is { } slide)
        {
            return new PendulumSlot(slot, position, 1, PawnSlideState.DescriptorId,
                PendulumCodec.Default.Value(PawnSlideState.DescriptorId, SlideValues(tick, slide, character.Body is { Airborne: true, Touching: false })));
        }

        return new PendulumSlot(slot, position, 1, null, []);
    }

    // The slide's own variable; on the press tick it holds its initial values apart from the slide's start.
    private static List<PendulumValue> SlideValues(uint tick, SlideState slide, bool falling)
    {
        List<PendulumValue> start =
        [
            new PendulumIntegerValue(PawnSlideState.UnknownField6, PawnSlideState.UnknownField6Value),
            new PendulumIntegerValue(PawnSlideState.StartTimeField, PawnSlideState.TimeBias + ((ulong)slide.StartFrame * PawnBlackboard.TimePerTick)),
        ];
        if (slide.Ticks == 0)
        {
            return start;
        }

        return
        [
            .. start,
            Centered(PawnSlideState.InitialDirectionXField, slide.InitialVelocity.X),
            Centered(PawnSlideState.InitialDirectionXField + 1, slide.InitialVelocity.Y),
            Centered(PawnSlideState.InitialDirectionXField + 2, slide.InitialVelocity.Z),
            Scaled(PawnSlideState.CurrentMaxMoveSpeedField, slide.MaximumSpeed / PawnSlideState.MaxMoveSpeedRange),
            Scaled(PawnSlideState.SlowDownAmountField, slide.Slowdown),
            new PendulumIntegerValue(PawnSlideState.FlagsField, (tick - slide.StartFrame >= PawnSlideState.ConsumedSprintInputTicks ? PawnSlideState.ConsumedSprintInputFlag : 0)
                | (falling ? PawnSlideState.FallingFlag : 0)),
        ];
    }

    // The action state's clock: it stops on a slide's third tick and stays there to the slide's finishing tick.
    private static ulong ActionClock(uint tick, SlideState? slide)
    {
        uint clocked = slide is { Ticks: >= SlideClockStopTicks } sliding ? sliding.StartFrame + SlideClockStopTicks - 1 : tick;
        return ((ulong)(clocked - 1) * PawnBlackboard.TimePerTick) + 1;
    }

    // The action state's timer that starts when a slide has consumed the sprint input.
    private static ulong SlideSprintTimer(SlideState? slide)
    {
        if (slide is not { Ticks: >= PawnSlideState.ConsumedSprintInputTicks } sliding)
        {
            return 0;
        }

        ulong elapsed = (ulong)(sliding.Ticks - PawnSlideState.ConsumedSprintInputTicks) * PawnBlackboard.TimePerTick;
        return elapsed < PawnBlackboard.ActionBlockTimerValue ? PawnBlackboard.ActionBlockTimerValue - elapsed : 0;
    }

    // A slide-state value spread over its field's whole range around zero.
    private static PendulumIntegerValue Centered(int field, double value)
    {
        PendulumScalarField scalar = (PendulumScalarField)s_slideFields[field];
        double quantized = Math.Round((value + PawnSlideState.InitialDirectionRange) * (scalar.Maximum - 1) / (2 * PawnSlideState.InitialDirectionRange), MidpointRounding.ToEven);
        return new PendulumIntegerValue(field, (ulong)Math.Clamp(quantized, 0, scalar.Maximum - 1));
    }

    // A slide-state share of its field's range from zero.
    private static PendulumIntegerValue Scaled(int field, double share)
    {
        PendulumScalarField scalar = (PendulumScalarField)s_slideFields[field];
        return new PendulumIntegerValue(field, (ulong)Math.Clamp(Math.Round(share * (scalar.Maximum - 1)), 0, scalar.Maximum - 1));
    }

    /// <summary>Writes a frame body coded against the previous frame.</summary>
    /// <param name="writer">Destination.</param>
    /// <param name="state">The frame's state.</param>
    /// <param name="previous">The previous frame's state (empty for the reset frame).</param>
    /// <param name="codec">The codec: the owner's (default) or <see cref="PendulumCodec.Others"/> for other clients.</param>
    public static void Write(BitWriter writer, IReadOnlyDictionary<int, PendulumSlot> state, IReadOnlyDictionary<int, PendulumSlot> previous,
        PendulumCodec? codec = null)
    {
        ArgumentNullException.ThrowIfNull(state);
        (codec ?? PendulumCodec.Default).WriteFrame(writer, state.Values, previous);
    }

    // The ground normal's stored rotation. On level ground the body's plane fit leaves float noise far below the
    // terrain's own precision; such a normal is stored as straight up.
    private static PendulumQuaternion GroundNormal(System.Numerics.Vector3 normal)
    {
        PendulumQuaternionField field = (PendulumQuaternionField)s_movementFields[PawnBlackboard.GroundNormalField];
        bool level = Math.Abs(normal.X) < LevelNormalTolerance && Math.Abs(normal.Y) < LevelNormalTolerance;
        return level ? PendulumRotations.FromGroundNormal(0, 0, 1, field) : PendulumRotations.FromGroundNormal(normal.X, normal.Y, normal.Z, field);
    }

    private static void Replace(List<PendulumValue> values, PendulumValue value)
    {
        values.RemoveAll(existing => existing.Field == value.Field);
        values.Add(value);
    }

    // A velocity component in world units per second, rounded to the nearest step (halves down).
    private static PendulumIntegerValue Velocity(int field, double velocity)
    {
        PendulumScalarField scalar = (PendulumScalarField)s_movementFields[field];
        double quantized = Math.Ceiling(((velocity + PawnBlackboard.VelocityRange) * (scalar.Maximum - 1) / (2 * PawnBlackboard.VelocityRange)) - 0.5);
        return new PendulumIntegerValue(field, (ulong)Math.Clamp(quantized, 0, scalar.Maximum - 1));
    }

    // The height of the feet above the ground on the field's scale, whose ends are the lowest and greatest heights.
    private static PendulumIntegerValue GroundDistance(double distance)
    {
        PendulumScalarField scalar = (PendulumScalarField)s_movementFields[PawnBlackboard.GroundDistanceField];
        double share = (distance - GroundContactParameters.GroundDistanceMinimum)
            / (GroundContactParameters.GroundDistanceMaximum - GroundContactParameters.GroundDistanceMinimum);
        return new PendulumIntegerValue(PawnBlackboard.GroundDistanceField, (ulong)Math.Clamp(Math.Round(share * (scalar.Maximum - 1)), 0, scalar.Maximum - 1));
    }

    private static ulong Quantize(double world)
    {
        double quantized = Math.Round((world + PawnBlackboard.PositionRange) * ((1 << PositionBits) - 1) / (2 * PawnBlackboard.PositionRange), MidpointRounding.ToEven);
        return quantized is >= 0 and < 1 << PositionBits ? (ulong)quantized : throw new InvalidPacketException("Position outside the blackboard range");
    }

    // An interactor-state time (seconds on the character's clock) as its quantized field value.
    private static ulong InteractionTime(double seconds)
    {
        const double Middle = 1 << 23;
        double quantized = Math.Round(Middle + (seconds * Middle / PawnBlackboard.InteractionTimeRange));
        return (ulong)Math.Clamp(quantized, 0, (2 * Middle) - 1);
    }
}

/// <summary>An interaction the server runs for the character, as its interactor state shows it.</summary>
/// <param name="Current">The interaction component.</param>
/// <param name="Event">Its last event.</param>
/// <param name="RequestedAt">When it was requested, in seconds on the character's clock.</param>
/// <param name="AlignedAt">When the character started lining up.</param>
/// <param name="StartedAt">When it (or its hold) started.</param>
/// <param name="Sequence">The character's interaction count, this one included.</param>
/// <param name="Location">Where it started, in the space of the interaction's prompt.</param>
/// <param name="Animation">Animation set the character plays for it (0 for none).</param>
public readonly record struct PawnInteraction(ulong Current, LastInteractionEvent Event, double RequestedAt, double AlignedAt, double StartedAt, uint Sequence,
    Vector3D Location = default, ulong Animation = 0);

/// <summary>
/// How the client numbers the movement nodes' slots: the ground node's initial slot, then the slots after every other
/// variable (a second weapon moves them on by one), with their variables on the pages after the others'.
/// </summary>
/// <param name="FirstSlot">The first node slot after the other variables.</param>
/// <param name="FirstPosition">The first page after the other variables.</param>
internal readonly record struct NodeSlots(int FirstSlot, int FirstPosition)
{
    private static readonly int s_groundPosition = PawnBlackboard.InitialState.First(variable => variable.Slot == PawnGroundNodeState.InitialSlot).Position;

    /// <summary>The node slots after the given variables.</summary>
    public static NodeSlots After(IEnumerable<PendulumVariable> others)
    {
        List<PendulumVariable> variables = [.. others];
        return new NodeSlots(variables.Max(variable => variable.Slot) + 1, variables.Max(variable => variable.Position + variable.Size));
    }

    /// <summary>The slot of a node slot as <see cref="NodeLayout"/> numbers it.</summary>
    public int Slot(int node) => node is PawnGroundNodeState.InitialSlot or < 0 ? node : FirstSlot + node - NodeLayout.FirstExtraSlot;

    /// <summary>The page of a node slot's variable.</summary>
    public int Position(int slot) => slot == PawnGroundNodeState.InitialSlot ? s_groundPosition : FirstPosition + slot - FirstSlot;
}
