using Ares.Raid.World.Movement;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Core.Serialization;
using Ares.Structs.Game.Inventory;
using Ares.Structs.Game.Movement;

namespace Ares.Raid.Tests.World.Movement;

public sealed class LateInputTests
{
    private const uint Start = 30;
    private const uint Last = Start + 80;

    // Ticks after the start: flashlight presses, a crouch press that starts a slide, a jump press, then a reported
    // jump arc, a landing whose recovery blocks firing, and the weapon put away.
    private const int FlashlightPress = 19;
    private const int SlidePress = 21;
    private const int SecondFlashlightPress = 45;
    private const int JumpPress = 44;
    private const int TakeOff = 50;
    private const int Landing = 62;
    private const int RecoveryEnd = 75;
    private const int PutAway = 70;
    private const double TakeOffSpeed = 250;
    private const int Shot = 19;
    private const int ReloadStart = 40;

    // Packets withheld for a while: each carries its latest three frames, so the frames only they carry go missing.
    private static readonly uint[] s_slideGap = [Start + 20, Start + 21, Start + 22, Start + 23, Start + 24];
    private static readonly uint[] s_jumpGap = [Start + 44, Start + 45, Start + 46, Start + 47];

    [Fact]
    public void Batches_dropped_then_delivered_later_end_in_the_in_order_state()
    {
        Run expected = Deliver(InOrder());
        IEnumerable<uint> late = InOrder().Except(s_slideGap).Except(s_jumpGap)
            .SelectMany(packet => packet switch
            {
                Start + 30 => [packet, .. s_slideGap],
                Start + 52 => [packet, .. s_jumpGap],
                _ => (uint[])[packet],
            });

        Run actual = Deliver(late);

        Assert.True(actual.MostMissing >= 3);
        Assert.True(actual.Revisions > 0);
        AssertSame(expected, actual);
    }

    [Fact]
    public void Reordered_batches_end_in_the_in_order_state()
    {
        Run expected = Deliver(InOrder());

        Run actual = Deliver(InOrder().Chunk(4).SelectMany(group => group.Reverse()));

        Assert.True(actual.MostMissing > 0);
        AssertSame(expected, actual);
    }

    [Fact]
    public void Duplicated_batches_end_in_the_in_order_state()
    {
        Run expected = Deliver(InOrder());

        Run actual = Deliver(InOrder().SelectMany(packet => packet % 5 == 0 ? [packet, packet, packet - 3] : (uint[])[packet, packet]));

        AssertSame(expected, actual);
    }

    [Fact]
    public void Frames_that_never_arrive_hold_the_previous_input_whatever_the_delivery()
    {
        Run inOrder = Deliver(InOrder().Except(s_slideGap));
        Run reordered = Deliver(InOrder().Except(s_slideGap).Chunk(4).SelectMany(group => group.Reverse()));
        Run duplicated = Deliver(InOrder().Except(s_slideGap).SelectMany(packet => (uint[])[packet, packet]));

        Assert.Equal(3, inOrder.Missing);
        AssertSame(inOrder, reordered);
        AssertSame(inOrder, duplicated);

        // The flashlight pressed just before the gap stays on: the repeated input holds the button, it does not press it.
        Assert.All(inOrder.States.Where(state => state.Frame is >= Start + FlashlightPress and < Start + SecondFlashlightPress),
            state => Assert.True(state.Flashlight));
    }

    [Fact]
    public void A_frame_older_than_the_kept_ticks_is_ignored()
    {
        Run never = Deliver(InOrder().Except(s_slideGap));

        // The input history still holds the frames when they arrive, but their ticks have left the kept window.
        Run tooLate = Deliver(InOrder().Except(s_slideGap).SelectMany(packet => packet == Start + 62 ? [packet, .. s_slideGap] : (uint[])[packet]));

        Assert.Equal(3, tooLate.Missing);
        Assert.Equal(0, tooLate.Revisions);
        AssertSame(never, tooLate);
    }

    [Fact]
    public void The_in_order_run_covers_flashlight_slide_air_and_recovery()
    {
        Run run = Deliver(InOrder());

        Assert.Equal(0, run.Missing);
        Assert.Contains(run.States, state => state.Stance == CharacterStance.Slide);
        Assert.Contains(run.States, state => state.SlideExitTicks > 0);
        Assert.Contains(run.States, state => state is { Unsettled: true, Frame: >= Start + Landing });
        Assert.Contains(run.States, state => state is { Unsettled: false, Frame: > Start + Landing });
        Assert.False(run.States[^1].Flashlight);
    }

    [Fact]
    public void A_shot_and_a_reload_end_in_the_in_order_state_after_late_frames()
    {
        Run expected = Deliver(InOrder(), Weapon);
        IEnumerable<uint> late = InOrder().Except(s_slideGap).Except(s_jumpGap)
            .SelectMany(packet => packet switch
            {
                Start + 30 => [packet, .. s_slideGap],
                Start + 52 => [packet, .. s_jumpGap],
                _ => (uint[])[packet],
            });

        Run actual = Deliver(late, Weapon);

        Assert.True(actual.Revisions > 0);
        AssertSame(expected, actual);
        Assert.Contains(expected.States, state => state is { Frame: > Start + Shot, HeldItem.UseSprintBlockTimer: > 0 });
        Assert.Contains(expected.States, state => state.HeldItem is { Reloading: true });
    }

    // The held weapon: one shot just before the slide gap, then a reload across the jump gap.
    private static HeldItemInput Weapon(uint frame) =>
        new(HeldItemMovementParameters.Default)
        {
            Weapon = true,
            Fired = frame == Start + Shot,
            Firing = frame is >= Start + Shot and < Start + Shot + 3,
            Reloading = frame is >= Start + ReloadStart and < Start + ReloadStart + 12,
        };

    private static IEnumerable<uint> InOrder()
    {
        for (uint latest = Start + 1; latest <= Last; latest++)
        {
            yield return latest;
        }
    }

    // Whether Raid withholds a frame for this state: a slide, its exit blend, a jump, fall or landing recovery.
    private static bool Withheld(CharacterState state) => state.Stance == CharacterStance.Slide || state.SlideExitTicks > 0 || state.Unsettled;

    private static void AssertSame(Run expected, Run actual)
    {
        Assert.Equal(expected.Missing, actual.Missing);
        Assert.Equal(expected.States.Select(Withheld), actual.States.Select(Withheld));
        Assert.Equal(expected.States, actual.States);
    }

    // Delivers the packets with these latest frames in this order, simulating like Raid after each one, and keeps the
    // state of each frame as it was when it left the revisable window (or at the end).
    private static Run Deliver(IEnumerable<uint> packets, Func<uint, HeldItemInput>? held = null)
    {
        InputHistory history = new();
        PlayerMovement movement = new(Start, default, 0);
        Dictionary<uint, CharacterState> settled = [];
        uint latest = 0;
        int mostMissing = 0;
        int revisions = 0;
        foreach (uint packet in packets)
        {
            latest = Math.Max(latest, history.Read(Payload(packet)).Latest ?? 0);
            if (movement.Revise(history) is not null)
            {
                revisions++;
            }

            while (movement.Frame + 1 < latest)
            {
                movement.Advance(history, armed: movement.Frame + 1 < Start + PutAway, held?.Invoke(movement.Frame + 1));
            }

            mostMissing = Math.Max(mostMissing, movement.MissingFrames);
            for (uint frame = Start + 1; frame <= movement.Frame; frame++)
            {
                if (movement.State(frame) is { } state)
                {
                    settled[frame] = state;
                }
            }
        }

        Assert.Equal(Last - 1, movement.Frame);
        Assert.Equal(movement.Latest, settled[movement.Frame]);
        return new Run([.. settled.OrderBy(entry => entry.Key).Select(entry => entry.Value)], movement.MissingFrames, mostMissing, revisions);
    }

    // A client payload coded against the defaults, carrying the latest three frames.
    private static byte[] Payload(uint latest)
    {
        BitWriter writer = new();
        writer.WriteBit(true);
        writer.WriteBounded(0, PendulumFrames.BaselineRange);
        PendulumFrames.WriteRelative(writer, latest, 0);
        Dictionary<int, PendulumSlot> previous = [];
        for (uint frame = latest - 2; frame <= latest; frame++)
        {
            Dictionary<int, PendulumSlot> current = Slots(frame);
            PendulumCodec.Default.WriteFrame(writer, current.Values, previous);
            previous = current;
        }

        return writer.ToArray();
    }

    private static Dictionary<int, PendulumSlot> Slots(uint frame)
    {
        int tick = (int)frame - (int)Start;
        int positionDescriptor = PendulumInput.SlotDescriptors[PendulumInput.PositionSlot];
        Vector3D at = Position(tick);
        List<PendulumValue> reported =
        [
            Quantized(positionDescriptor, PendulumInput.PositionXField, at.X),
            Quantized(positionDescriptor, PendulumInput.PositionYField, at.Y),
            Quantized(positionDescriptor, PendulumInput.PositionZField, at.Z),
        ];
        if (tick is > Landing and < RecoveryEnd)
        {
            reported.Add(new PendulumIntegerValue(WeaponProtocol.InputFlagsField, 1UL << WeaponProtocol.FireBlockedBit));
        }

        int movementDescriptor = PendulumInput.SlotDescriptors[PendulumInput.MovementSlot];
        MovementButtons buttons = (tick < SlidePress + 4 ? MovementButtons.Sprint : MovementButtons.None)
            | (Down(tick, SlidePress, 3) ? MovementButtons.Crouch : MovementButtons.None)
            | (Down(tick - 1, SlidePress, 3) ? MovementButtons.PreviousCrouch : MovementButtons.None)
            | (Down(tick, JumpPress, 2) ? MovementButtons.Jump : MovementButtons.None)
            | (Down(tick - 1, JumpPress, 2) ? MovementButtons.PreviousJump : MovementButtons.None);
        int claimsDescriptor = PendulumInput.SlotDescriptors[PendulumInput.CharacterClaimsSlot];
        ulong claims = (Flashlight(tick) ? 1UL << PendulumInput.FlashlightDownBit : 0)
            | (Flashlight(tick - 1) ? 1UL << PendulumInput.FlashlightPreviousDownBit : 0);
        (int Slot, int Descriptor, byte[] Value)[] values =
        [
            (PendulumInput.PositionSlot, positionDescriptor, PendulumCodec.Default.Value(positionDescriptor, reported)),
            (PendulumInput.MovementSlot, movementDescriptor, PendulumCodec.Default.Value(movementDescriptor,
            [
                new PendulumIntegerValue(PendulumInput.ForwardField, PendulumInput.MovementAxisMaximum + PendulumInput.MovementAxisBias),
                new PendulumIntegerValue(PendulumInput.RightField, PendulumInput.MovementAxisBias),
                new PendulumIntegerValue(PendulumInput.MovementFlagsField, (ulong)buttons),
            ])),
            (PendulumInput.CharacterClaimsSlot, claimsDescriptor, PendulumCodec.Default.Value(claimsDescriptor,
                [new PendulumIntegerValue(PendulumInput.CharacterClaimsFlagsField, claims)])),
        ];
        Dictionary<int, PendulumSlot> slots = [];
        int offset = 0;
        foreach ((int slot, int descriptor, byte[] value) in values)
        {
            int size = (value.Length + 255) / 256;
            slots[slot] = new PendulumSlot(slot, offset, size, descriptor, value);
            offset += size;
        }

        return slots;

        static bool Down(int tick, int press, int ticks) => tick >= press && tick < press + ticks;

        static bool Flashlight(int tick) => Down(tick, FlashlightPress, 5) || Down(tick, SecondFlashlightPress, 2);
    }

    // Running forward on level ground, then a jump arc onto lower ground.
    private static Vector3D Position(int tick)
    {
        double seconds = (Math.Clamp(tick, TakeOff, Landing) - TakeOff) * PawnBlackboard.TickSeconds;
        double height = (TakeOffSpeed * seconds) - (AirMovementParameters.FallAcceleration * seconds * seconds / 2);
        return new Vector3D(1000 + (10.0 * tick), 2000, 500 + height);
    }

    private static PendulumIntegerValue Quantized(int descriptor, int field, double world)
    {
        PendulumScalarField scalar = (PendulumScalarField)PendulumRegistry.Descriptors[descriptor].Fields[field];
        return new PendulumIntegerValue(field, (ulong)Math.Round((world + PawnBlackboard.PositionRange) * (scalar.Maximum - 1) / (2 * PawnBlackboard.PositionRange)));
    }

    // Each frame's state once settled, the frames still simulated without their input, the most at any time, and the
    // packets after which ticks were simulated again.
    private sealed record Run(IReadOnlyList<CharacterState> States, int Missing, int MostMissing, int Revisions);
}
