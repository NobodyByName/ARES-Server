using System.Numerics;
using Ares.Raid.World.Collision;
using Ares.Raid.World.Movement;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Game.Movement;
using Ares.Structs.Game.World;

namespace Ares.Raid.Tests.World.Movement;

public sealed class LateInputMechanicsTests
{
    private const uint Start = 30;
    private const uint Last = Start + 90;

    private static readonly CollisionWorld s_floor = ScriptedClient.World((new Vector3D(0, 0, -10), new Vector3(4000, 4000, 10)));

    public static TheoryData<string> Deliveries => ["late", "reordered", "duplicated"];

    [Theory]
    [MemberData(nameof(Deliveries))]
    public void A_heavy_landing_resimulated_from_late_input_ends_in_the_in_order_state(string delivery)
    {
        Vector3D top = new(0, 0, 900);
        Dictionary<uint, CharacterState> reference = ReferenceRun.Run(s_floor, top, Start + 2, Last, frame => (new MovementInput(frame, 0, 0, default), new CharacterInput(frame, 0)));
        uint landed = reference.First(entry => entry.Value.Landing is not null).Key;

        List<CharacterState> expected = Run(s_floor, top, Script, ScriptedClient.InOrder(Start + 3, Last));
        List<CharacterState> actual = Run(s_floor, top, Script, Order(delivery, landed));

        Assert.Contains(expected, state => state.HeavyLandingTicks > 0);
        Assert.Equal(expected, actual);
        return;

        ScriptedFrame Script(uint frame) => new(frame <= Start + 2 ? top : reference[Math.Min(frame - 1, Last)].Position, Forward: frame > landed ? (sbyte)126 : (sbyte)0);
    }

    [Theory]
    [MemberData(nameof(Deliveries))]
    public void A_ladder_climb_resimulated_from_late_input_ends_in_the_in_order_state(string delivery)
    {
        int ladder = Enumerable.Range(0, DamLadders.All.Count).MaxBy(index => LadderLine.From(DamLadders.All[index]).Height);
        LadderLine line = LadderLine.From(DamLadders.All[ladder]);
        CollisionWorld world = ScriptedClient.World((line.Origin with { Z = line.Origin.Z - 10 }, new Vector3(600, 600, 10)));
        const uint Climb = Start + 10;

        List<CharacterState> expected = Run(world, line.Origin, Script, ScriptedClient.InOrder(Start + 3, Last), ladder, Climb);
        List<CharacterState> actual = Run(world, line.Origin, Script, Order(delivery, Climb), ladder, Climb);

        Assert.Contains(expected, state => state.Ladder is { Aligning: false });
        Assert.Equal(expected, actual);
        return;

        ScriptedFrame Script(uint frame) => new(line.Origin, frame > Climb + 20 ? (sbyte)126 : (sbyte)0,
            Buttons: frame > Climb + 40 ? MovementButtons.Sprint : MovementButtons.None);
    }

    [Theory]
    [MemberData(nameof(Deliveries))]
    public void A_vault_resimulated_from_late_input_ends_in_the_in_order_state(string delivery)
    {
        CollisionWorld world = ScriptedClient.World((new Vector3D(0, 0, -10), new Vector3(4000, 4000, 10)), (new Vector3D(100, 0, 57.5), new Vector3(10, 500, 57.5f)));
        Vector3D start = new(-100, 0, 0);
        const uint Request = Start + 12;
        Dictionary<uint, CharacterState> reference = ReferenceRun.Run(world, start, Start + 2, Last, frame => ReferenceRun.Input(frame, Input(frame), Input(frame - 1)));

        List<CharacterState> expected = Run(world, start, Script, ScriptedClient.InOrder(Start + 3, Last));
        List<CharacterState> actual = Run(world, start, Script, Order(delivery, Request));

        Assert.Contains(expected, state => state.Vault is not null);
        Assert.Equal(expected, actual);
        return;

        ScriptedFrame Input(uint frame) => new(default, frame >= Start + 2 ? (sbyte)126 : (sbyte)0, Claims: frame == Request ? 1UL << VaultParameters.RequestBit : 0);

        ScriptedFrame Script(uint frame) => Input(frame) with { Position = frame <= Start + 2 ? start : reference[Math.Min(frame - 1, Last)].Position };
    }

    [Theory]
    [MemberData(nameof(Deliveries))]
    public void A_slide_cut_short_by_a_roll_resimulated_from_late_input_ends_in_the_in_order_state(string delivery)
    {
        Vector3D start = new(-1500, 0, 0);
        const uint Slide = Start + 30;
        const uint Roll = Slide + 8;
        Dictionary<uint, CharacterState> reference = ReferenceRun.Run(s_floor, start, Start + 2, Last, frame => ReferenceRun.Input(frame, Input(frame), Input(frame - 1)));

        List<CharacterState> expected = Run(s_floor, start, Script, ScriptedClient.InOrder(Start + 3, Last));
        List<CharacterState> actual = Run(s_floor, start, Script, Order(delivery, Roll));

        Assert.Contains(expected, state => state.Nodes.SlideSlot >= 0);
        Assert.Contains(expected, state => state.Rolling);
        Assert.True(expected[^1].Nodes is { SlideSlot: -1, TransitionSlot: -1, GroundSlot: >= 0 });
        Assert.Equal(expected, actual);
        return;

        ScriptedFrame Input(uint frame) => new(default, frame >= Start + 2 ? (sbyte)126 : (sbyte)0, Buttons: frame switch
        {
            < Slide => MovementButtons.Sprint,
            < Roll => MovementButtons.Sprint | MovementButtons.Crouch,
            Roll => MovementButtons.DodgeRoll,
            _ => MovementButtons.None,
        });

        ScriptedFrame Script(uint frame) => Input(frame) with { Position = frame <= Start + 2 ? start : reference[Math.Min(frame - 1, Last)].Position };
    }

    // Packets around a frame: held back and delivered a few packets later, reversed in groups, or each sent twice.
    private static IEnumerable<uint> Order(string delivery, uint around)
    {
        uint[] gap = [around - 1, around, around + 1, around + 2];
        IEnumerable<uint> packets = ScriptedClient.InOrder(Start + 3, Last);
        return delivery switch
        {
            "late" => packets.Except(gap).SelectMany(packet => packet == around + 6 ? [packet, .. gap] : (uint[])[packet]),
            "reordered" => packets.Chunk(4).SelectMany(group => group.Reverse()),
            _ => packets.SelectMany(packet => (uint[])[packet, packet]),
        };
    }

    // Runs a delivery; the climb request, if any, reaches Raid with the first packet that carries its frame.
    private static List<CharacterState> Run(CollisionWorld world, Vector3D start, Func<uint, ScriptedFrame> script, IEnumerable<uint> packets,
        int? ladder = null, uint climb = 0)
    {
        PlayerMovement movement = new(Start, start, 0, world);
        bool climbed = false;
        List<CharacterState> states = ScriptedClient.Deliver(movement, script, packets, packet =>
        {
            if (ladder is not { } index || climbed || packet < climb || packet > climb + 2)
            {
                return;
            }

            movement.Climb(climb, index, topEntry: false);
            climbed = true;
        });
        Assert.Equal(Last - 1, movement.Frame);
        return states;
    }
}
