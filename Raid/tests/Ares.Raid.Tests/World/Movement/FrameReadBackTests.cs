using System.Numerics;
using Ares.Raid.World.Collision;
using Ares.Raid.World.Movement;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Game.Inventory;
using Ares.Structs.Game.Movement;

namespace Ares.Raid.Tests.World.Movement;

public sealed class FrameReadBackTests
{
    private const uint Start = 30;
    private const uint Slide = Start + 30;
    private const uint Release = Slide + 40;
    private const uint Last = Release + 40;

    private static readonly CollisionWorld s_floor = ScriptedClient.World((new Vector3D(0, 0, -10), new Vector3(4000, 4000, 10)));
    private static readonly Vector3D s_start = new(-1500, 0, 0);

    // Action state fields written from the ground node's input tracking, and the sprint boost's graph tag, which the
    // node's own variable does not hold: a ground node read back from a frame starts without them.
    private static readonly int[] s_notReadBack =
        [PawnBlackboard.ActionInputStrengthField, PawnBlackboard.BlockItemFromSprintTimerField, PawnBlackboard.ActionInputDirectionField];

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void A_frame_reads_back_as_the_ground_node_and_node_layout_it_was_written_from(int weapons)
    {
        PendulumVariable first = PawnBlackboard.InitialState.First(variable => variable.Slot == WeaponProtocol.FirstWeaponSlot);
        List<PendulumVariable> equipped = [.. Enumerable.Range(0, weapons)
            .Select(index => first with { Slot = first.Slot + index, Position = first.Position + (index * first.Size) })];
        Dictionary<uint, CharacterState> run = ReferenceRun.Run(s_floor, s_start, Start + 2, Last,
            frame => ReferenceRun.Input(frame, Input(frame), Input(frame - 1)));

        // Initial layout, sprint, slide with its transitions in and out, crouch and walk after the slide.
        Assert.Contains(run.Values, state => state.Nodes.SlideSlot >= 0);
        Assert.Contains(run.Values, state => state.Nodes.TransitionSlot >= 0);
        Assert.Contains(run.Values, state => state.Nodes is { SlideSlot: -1, TransitionSlot: -1 } && state.Nodes.GroundSlot != PawnGroundNodeState.InitialSlot);
        foreach ((uint frame, CharacterState state) in run)
        {
            IReadOnlyDictionary<int, PendulumSlot> written = Frame(frame, state, equipped);
            // While a slide runs, the frame has no ground node variable and the character keeps its own.
            GroundNodeState ground = written.Values.SingleOrDefault(slot => slot.DescriptorId == PawnGroundNodeState.DescriptorId) is { } node
                ? GroundNodeState.From(PendulumRegistry.Descriptors[PawnGroundNodeState.DescriptorId].Fields
                    .Select((field, index) => (field, index)).Where(pair => pair.field is PendulumScalarField or PendulumBitfieldField)
                    .Select(pair => new PendulumIntegerValue(pair.index, PendulumCodec.Integer(pair.field, node.Value))))
                : state.GroundNode;
            NodeLayout layout = NodeLayout.From(written, frame);

            // The layout reads back its slots and a running transition. The slide's own state stays the character's,
            // and whether this tick added the ground node needs the frame before.
            Assert.Equal(Slots(state.Nodes), Slots(layout));
            // The slide's own variable is written from the slide's state, which is not read back.
            int[] slide = [.. written.Values.Where(slot => slot.DescriptorId == PawnSlideState.DescriptorId).Select(slot => slot.Slot)];
            Assert.Equal(Text(written, slide), Text(Frame(frame, state with { GroundNode = ground, Nodes = layout }, equipped), slide));
        }
    }

    [Fact]
    public void A_movement_started_mid_slide_continues_as_the_uninterrupted_run()
    {
        Dictionary<uint, CharacterState> reference = ReferenceRun.Run(s_floor, s_start, Start + 2, Last,
            frame => ReferenceRun.Input(frame, Input(frame), Input(frame - 1)));
        List<CharacterState> whole = ScriptedClient.Deliver(new PlayerMovement(Start, s_start, 0, s_floor), Script, ScriptedClient.InOrder(Start + 3, Last));
        const uint Seed = Slide + 10;
        CharacterState seed = whole[(int)(Seed - Start - 1)];
        Assert.NotNull(seed.Slide);

        List<CharacterState> continued = ScriptedClient.Deliver(PlayerMovement.From(Seed, seed, 0, s_floor), Script, ScriptedClient.InOrder(Seed + 1, Last));

        Assert.Equal(whole[(int)(Seed - Start)..], continued);
        return;

        ScriptedFrame Script(uint frame) => Input(frame) with { Position = frame <= Start + 2 ? s_start : reference[Math.Min(frame - 1, Last)].Position };
    }

    // Sprint forward, slide, keep crouching after the slide, then release crouch and walk on.
    private static ScriptedFrame Input(uint frame) => new(default, frame >= Start + 2 ? (sbyte)126 : (sbyte)0, Buttons: frame switch
    {
        < Slide => MovementButtons.Sprint,
        < Release => MovementButtons.Sprint | MovementButtons.Crouch,
        _ => MovementButtons.None,
    });

    private static (ulong, int, int, int, bool, uint, (int, int), int) Slots(NodeLayout layout) =>
        (layout.LaidOut, layout.GroundSlot, layout.SlideSlot, layout.TransitionSlot, layout is { TransitionSlot: >= 0, TransitionOut: true },
            layout.TransitionSlot < 0 ? 0 : layout.TransitionStart, layout.TransitionSlot < 0 ? default : layout.TransitionSlots,
            layout.ActiveSlot);

    private static IReadOnlyDictionary<int, PendulumSlot> Frame(uint frame, CharacterState state, IReadOnlyList<PendulumVariable> weapons) =>
        PawnBlackboardFrames.State(frame, new PawnMotion(state.Position, state.Velocity, state.Yaw, state), 0, weapons: weapons);

    // The frame's slots; the action and graph states as their integer fields apart from those not read back.
    private static List<string> Text(IReadOnlyDictionary<int, PendulumSlot> frame, int[] skipped) =>
        [.. frame.Where(entry => !skipped.Contains(entry.Key)).OrderBy(entry => entry.Key).Select(entry => $"{entry.Key} {entry.Value.Position} {entry.Value.Size} {entry.Value.DescriptorId} {Value(entry.Value)}")];

    private static string Value(PendulumSlot slot) => slot switch
    {
        { Slot: PawnBlackboard.ActionStateSlot, DescriptorId: { } descriptor } => Fields(slot, descriptor,
            (index, value) => s_notReadBack.Contains(index) ? 0 : value),
        { Slot: PawnBlackboard.GraphStateSlot, DescriptorId: { } descriptor } => Fields(slot, descriptor,
            (index, value) => index == PawnBlackboard.GraphUpperTagsField ? value & ~GraphStateUpperTags.UiSkillStateActiveSprintBoost : value),
        _ => Convert.ToHexStringLower(slot.Value),
    };

    private static string Fields(PendulumSlot slot, int descriptor, Func<int, ulong, ulong> kept) =>
        string.Join(' ', PendulumRegistry.Descriptors[descriptor].Fields
            .Select((field, index) => (field, index))
            .Where(pair => pair.field is PendulumScalarField or PendulumBitfieldField)
            .Select(pair => kept(pair.index, PendulumCodec.Integer(pair.field, slot.Value))));
}
