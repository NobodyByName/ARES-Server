using Ares.Structs.Game.Movement;

namespace Ares.Raid.World.Movement;

/// <summary>
/// The ground-movement node's own variable (<see cref="PawnGroundNodeState"/>) as the client keeps it between ticks:
/// what the simulation decides each ground tick, and the fields it does not simulate, which keep their last values.
/// </summary>
/// <param name="Kept">
/// Values of the fields the simulation does not decide: those of the pawn's initial state, or of the state the simulation
/// started from.
/// </param>
public readonly record struct GroundNodeState(IReadOnlyList<PendulumIntegerValue> Kept)
{
    private static readonly int[] s_simulated =
    [
        PawnGroundNodeState.UnknownField0, PawnGroundNodeState.CurrentMaxMoveSpeedField, PawnGroundNodeState.StartedInputTimeField,
        PawnGroundNodeState.StartSprintTimeField, PawnGroundNodeState.ResultingInputDirectionField, PawnGroundNodeState.PlayerInputDirectionField,
        PawnGroundNodeState.LocoModeField, PawnGroundNodeState.CharacterStanceField, PawnGroundNodeState.NodeStartTimeField, PawnGroundNodeState.FlagsField,
    ];

    private static readonly IReadOnlyList<PendulumIntegerValue> s_freshKept =
        [new(PawnGroundNodeState.UnknownField4, PawnGroundNodeState.UnknownField4Value)];

    private static readonly IReadOnlyList<PendulumField> s_fields = PendulumRegistry.Descriptors[PawnGroundNodeState.DescriptorId].Fields;

    /// <summary>The node's state in the pawn's initial state.</summary>
    public static GroundNodeState Initial { get; } = From(PawnBlackboard.InitialState.First(variable => variable.Slot == PawnGroundNodeState.InitialSlot).Values
        .OfType<PendulumIntegerValue>());

    /// <summary>The tick the node's variable was added; its times count from it.</summary>
    private uint StartFrame { get; init; }

    /// <summary>The node's flags other than <see cref="PawnGroundNodeState.ShufflingFlag"/>, as stored.</summary>
    private ulong Flags { get; init; }

    /// <summary>Whether the crouched character shuffles.</summary>
    private bool Shuffling { get; init; }

    /// <summary>Whether movement input was held, during a slide too.</summary>
    public bool InputHeld { get; init; }

    /// <summary>Strength of the movement input, from zero to one.</summary>
    public double InputStrength { get; private init; }

    /// <summary>The time since the node's start of the tick the current movement input started, as stored.</summary>
    private ulong StartedInputTime { get; init; } = Default(PawnGroundNodeState.StartedInputTimeField);

    /// <summary>The time since the node's start of the tick the latest sprint started, as stored.</summary>
    private ulong StartSprintTime { get; init; } = Default(PawnGroundNodeState.StartSprintTimeField);

    /// <summary>The ground movement's maximum speed in world units per second.</summary>
    private double MaxMoveSpeed { get; init; }

    /// <summary>Whether the node moves the character by its input: movement input held and no slide under way.</summary>
    private bool Moving { get; init; }

    /// <summary>The direction the node moves in.</summary>
    private InputDirection ResultingDirection { get; init; }

    /// <summary>The direction of this tick's movement input, which the action state shows.</summary>
    public InputDirection InputDirection { get; private init; }

    /// <summary>The direction of the player's movement input as the node keeps it; a slide leaves it unchanged.</summary>
    private InputDirection PlayerDirection { get; init; }

    /// <summary>The node's locomotion.</summary>
    private LocoMode Locomotion { get; init; }

    /// <summary>The node's stance.</summary>
    private CharacterStance Stance { get; init; }

    /// <summary>Whether the ground movement sprinted.</summary>
    private bool Sprinting { get; init; }

    /// <summary>The action state's sprint timer (<see cref="PawnBlackboard.BlockItemFromSprintTimerField"/>), in simulation time units.</summary>
    public ulong SprintTimer { get; private init; }

    /// <summary>Whether a sprint's burst lifts its maximum speed above the sustained sprint speed.</summary>
    public bool SprintBoosted { get; private init; }

    /// <summary>
    /// The share of the input that steep ground last left (<see cref="PawnGroundNodeState.SlopeInputScalarField"/>); the
    /// node keeps it until steep ground sets it again. Null until then.
    /// </summary>
    public double? SlopeInputScalar { get; private init; }

    /// <summary>A node state from stored field values; the fields the simulation decides are taken from them too.</summary>
    /// <param name="values">Stored integer field values.</param>
    /// <returns>The node state.</returns>
    public static GroundNodeState From(IEnumerable<PendulumIntegerValue> values)
    {
        Dictionary<int, ulong> stored = values.ToDictionary(value => value.Field, value => value.Value);
        return new GroundNodeState([.. stored.Where(pair => !s_simulated.Contains(pair.Key)).Select(pair => new PendulumIntegerValue(pair.Key, pair.Value))])
        {
            StartFrame = (uint)((Value(PawnGroundNodeState.NodeStartTimeField) - PawnSlideState.TimeBias) / PawnBlackboard.TimePerTick),
            Flags = Value(PawnGroundNodeState.FlagsField) & ~PawnGroundNodeState.ShufflingFlag,
            Shuffling = (Value(PawnGroundNodeState.FlagsField) & PawnGroundNodeState.ShufflingFlag) != 0,
            InputHeld = Value(PawnGroundNodeState.UnknownField0) != Default(PawnGroundNodeState.UnknownField0),
            StartedInputTime = Value(PawnGroundNodeState.StartedInputTimeField),
            StartSprintTime = Value(PawnGroundNodeState.StartSprintTimeField),
            MaxMoveSpeed = Value(PawnGroundNodeState.CurrentMaxMoveSpeedField) * PawnSlideState.MaxMoveSpeedRange
                / (((PendulumScalarField)s_fields[PawnGroundNodeState.CurrentMaxMoveSpeedField]).Maximum - 1),
            Moving = Value(PawnGroundNodeState.UnknownField0) != Default(PawnGroundNodeState.UnknownField0),
            ResultingDirection = (InputDirection)Value(PawnGroundNodeState.ResultingInputDirectionField),
            PlayerDirection = (InputDirection)Value(PawnGroundNodeState.PlayerInputDirectionField),
            InputDirection = (InputDirection)Value(PawnGroundNodeState.PlayerInputDirectionField),
            Locomotion = (LocoMode)Value(PawnGroundNodeState.LocoModeField),
            Stance = (CharacterStance)Value(PawnGroundNodeState.CharacterStanceField),
        };

        ulong Value(int field) => stored.TryGetValue(field, out ulong value) ? value : Default(field);
    }

    /// <summary>The variable the node adds when it starts again, at a tick: initial field values apart from its start.</summary>
    /// <param name="frame">The tick it starts on.</param>
    /// <returns>The node state.</returns>
    /// <param name="movement">The tick's movement input; input held then does not count as an input start.</param>
    public static GroundNodeState Fresh(uint frame, MovementInput movement) =>
        new(s_freshKept)
        {
            StartFrame = frame,
            InputHeld = movement.Forward != 0 || movement.Right != 0,
            InputStrength = Strength(movement),
            MaxMoveSpeed = Default(PawnGroundNodeState.CurrentMaxMoveSpeedField) * PawnSlideState.MaxMoveSpeedRange
                / (((PendulumScalarField)s_fields[PawnGroundNodeState.CurrentMaxMoveSpeedField]).Maximum - 1),
        };

    /// <summary>The node's state after a ground tick.</summary>
    /// <param name="frame">The tick's input frame.</param>
    /// <param name="movement">The tick's movement input.</param>
    /// <param name="maximumSpeed">The ground movement's maximum speed this tick.</param>
    /// <param name="sprinting">Whether the ground movement sprints this tick.</param>
    /// <param name="sprintStarted">Whether a sprint started this tick.</param>
    /// <param name="boosted">Whether a sprint's burst lifts the maximum speed above the sustained sprint speed.</param>
    /// <param name="locomotion">The ground movement's locomotion.</param>
    /// <param name="stance">The ground movement's stance.</param>
    /// <param name="shuffling">Whether the crouched character shuffles.</param>
    /// <param name="sliding">Whether a slide moves the character this tick.</param>
    /// <param name="slopeScalar">The share of the input steep ground left this tick, or null where the ground set none.</param>
    /// <returns>The next state.</returns>
    public GroundNodeState Next(uint frame, MovementInput movement, double maximumSpeed, bool sprinting, bool sprintStarted, bool boosted, LocoMode locomotion,
        CharacterStance stance, bool shuffling, bool sliding, double? slopeScalar = null)
    {
        bool held = movement.Forward != 0 || movement.Right != 0;
        InputDirection direction = DodgeRollMovement.Direction(movement);
        return this with
        {
            StartedInputTime = held && !InputHeld ? Time(frame) : StartedInputTime,
            StartSprintTime = sprintStarted ? Time(frame) : StartSprintTime,
            MaxMoveSpeed = maximumSpeed,
            InputHeld = held,
            InputStrength = Strength(movement),
            Moving = held && !sliding,
            Shuffling = shuffling,
            ResultingDirection = sliding ? InputDirection.None : direction,
            PlayerDirection = sliding ? PlayerDirection : direction,
            InputDirection = direction,
            Locomotion = locomotion,
            Stance = stance,
            Sprinting = sprinting,
            SprintTimer = sprinting && Sprinting ? PawnBlackboard.ActionBlockTimerValue
                : SprintTimer > PawnBlackboard.TimePerTick ? SprintTimer - PawnBlackboard.TimePerTick : 0,
            SprintBoosted = boosted,
            SlopeInputScalar = slopeScalar ?? SlopeInputScalar,
        };
    }

    /// <summary>The variable's field values.</summary>
    /// <returns>Integer field values.</returns>
    public IEnumerable<PendulumIntegerValue> Values()
    {
        PendulumScalarField speed = (PendulumScalarField)s_fields[PawnGroundNodeState.CurrentMaxMoveSpeedField];
        yield return new PendulumIntegerValue(PawnGroundNodeState.NodeStartTimeField, PawnSlideState.TimeBias + ((ulong)StartFrame * PawnBlackboard.TimePerTick));
        yield return new PendulumIntegerValue(PawnGroundNodeState.FlagsField, Flags | (Shuffling ? PawnGroundNodeState.ShufflingFlag : 0));
        foreach (PendulumIntegerValue kept in Kept)
        {
            yield return kept;
        }

        yield return new PendulumIntegerValue(PawnGroundNodeState.UnknownField0,
            Default(PawnGroundNodeState.UnknownField0) + (Moving ? PawnGroundNodeState.UnknownField0MovingOffset : 0));
        yield return new PendulumIntegerValue(PawnGroundNodeState.CurrentMaxMoveSpeedField,
            (ulong)Math.Clamp(Math.Round(MaxMoveSpeed * (speed.Maximum - 1) / PawnSlideState.MaxMoveSpeedRange), 0, speed.Maximum - 1));
        yield return new PendulumIntegerValue(PawnGroundNodeState.StartedInputTimeField, StartedInputTime);
        yield return new PendulumIntegerValue(PawnGroundNodeState.StartSprintTimeField, StartSprintTime);
        yield return new PendulumIntegerValue(PawnGroundNodeState.ResultingInputDirectionField, (ulong)ResultingDirection);
        yield return new PendulumIntegerValue(PawnGroundNodeState.PlayerInputDirectionField, (ulong)PlayerDirection);
        yield return new PendulumIntegerValue(PawnGroundNodeState.LocoModeField, (ulong)Locomotion);
        yield return new PendulumIntegerValue(PawnGroundNodeState.CharacterStanceField, (ulong)Stance);
    }

    private ulong Time(uint frame) => PawnSlideState.TimeBias + ((ulong)(frame - StartFrame) * PawnBlackboard.TimePerTick);

    private static double Strength(MovementInput movement) =>
        Math.Min(1, Math.Sqrt((movement.Forward * movement.Forward) + (movement.Right * movement.Right)) / PendulumInput.MovementAxisMaximum);

    private static ulong Default(int field) => s_fields[field] switch
    {
        PendulumScalarField scalar => scalar.Default,
        PendulumBitfieldField bits => bits.Default,
        _ => throw new InvalidOperationException("Not an integer field of the ground node"),
    };
}
