using System.Numerics;
using Ares.Raid.World.Collision;
using Ares.Raid.World.Movement;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Game.Movement;
using Ares.Structs.Game.World;

namespace Ares.Raid.Tests.World.Movement;

public sealed class LadderWiringTests
{
    private const uint Start = 30;
    private const sbyte Up = 126;
    private const sbyte Down = -126;

    // The tallest ladder, with a floor at the foot of its line.
    private static readonly int s_ladder = Enumerable.Range(0, DamLadders.All.Count).MaxBy(index => LadderLine.From(DamLadders.All[index]).Height);
    private static readonly LadderLine s_line = LadderLine.From(DamLadders.All[s_ladder]);
    private static readonly CollisionWorld s_world = ScriptedClient.World((s_line.Origin with { Z = s_line.Origin.Z - 10 }, new Vector3(600, 600, 10)));

    [Fact]
    public void Getting_on_climbing_sprinting_up_and_sliding_down_are_simulated_and_the_bottom_steps_off()
    {
        const uint Climb = Start + 3;
        Vector3D floor = s_line.Origin;
        PlayerMovement movement = new(Start, floor, 0, s_world, double.PositiveInfinity);
        movement.Climb(Climb, s_ladder, topEntry: false);
        Dictionary<uint, CharacterState> states = Run(movement, Script, Climb + 160);

        foreach (CharacterState state in Range(states, Climb, Climb + 79))
        {
            Assert.True(state.OnLadder);
            Assert.Equal(CharacterStance.Ladder, state.Stance);
            Assert.False(state.Body!.Value.Airborne);
        }

        Assert.True(states[Climb].Ladder!.Value.Aligning);
        Assert.False(states[Climb + 19].Ladder!.Value.Aligning);
        double climbing = Speed(states, Climb + 50, Climb + 59);
        double sprinting = Speed(states, Climb + 70, Climb + 79);
        Assert.Equal(LadderParameters.ClimbUpSpeed, climbing, 1);
        Assert.Equal(LadderParameters.SprintUpSpeed, sprinting, 1);
        Assert.Equal(LadderParameters.SprintActiveFlag, states[Climb + 75].GraphFlags & LadderParameters.SprintActiveFlag);
        Assert.Contains(Range(states, Climb + 80, Climb + 160), state => (state.GraphFlags & LadderParameters.SlidingFlag) != 0);
        Assert.False(states[Climb + 159].OnLadder);
        Assert.Equal(0, movement.Reseeds);
        return;

        ScriptedFrame Script(uint frame) => (int)(frame - Climb) switch
        {
            < 20 => new ScriptedFrame(floor),
            < 60 => new ScriptedFrame(floor, Up),
            < 80 => new ScriptedFrame(floor, Up, Buttons: MovementButtons.Sprint),
            _ => new ScriptedFrame(floor, Down, Buttons: MovementButtons.Sprint),
        };
    }

    [Fact]
    public void Holding_crouch_drops_off_and_the_crouch_input_stays_blocked_for_its_timer()
    {
        const uint Climb = Start + 2;
        const uint Crouch = Climb + 10;
        Vector3D feet = Feet(150);
        PlayerMovement movement = new(Start, feet, 0, s_world, double.PositiveInfinity);
        movement.Climb(Climb, s_ladder, topEntry: false, attached: true);
        Dictionary<uint, CharacterState> states = Run(movement,
            frame => new ScriptedFrame(feet, Claims: frame == Crouch ? 1UL << LadderParameters.CrouchButtonDownBit : 0), Climb + 30);

        Assert.True(states[Crouch - 1].OnLadder);
        Assert.Null(states[Crouch].Ladder);
        Assert.False(states[Crouch + 1].OnLadder);
        int blocked = Range(states, Crouch + 1, Climb + 30).Count(state => (state.GraphFlags & LadderParameters.CrouchInputBlockedFlag) != 0);
        Assert.Equal(LadderMovement.CrouchBlockTicks - 1, blocked);
    }

    [Fact]
    public void A_top_entry_follows_the_reports_for_its_animation_counted_once_then_climbs()
    {
        const uint Climb = Start + 2;
        Vector3D top = Feet(s_line.Height);
        PlayerMovement movement = new(Start, top, 0, s_world, double.PositiveInfinity);
        movement.Climb(Climb, s_ladder, topEntry: true);
        Dictionary<uint, CharacterState> states = Run(movement, Script, Climb + 60);

        int animated = (int)Math.Round(LadderParameters.TopEntrySeconds * 30);
        Assert.All(Range(states, Climb, Climb + (uint)animated - 2), state =>
        {
            Assert.True(state.Ladder!.Value.PlayingTopEntry);
            Assert.Equal(CharacterStance.Ladder, state.Stance);
            Assert.InRange(state.Position.Z - Script(state.Frame + 1).Position.Z, -1, 1);
        });
        Assert.False(states[Climb + (uint)animated + 5].Ladder!.Value.PlayingTopEntry);
        Assert.True(Math.Abs(Script(Climb + 50).Position.Z - states[Climb + 49].Position.Z) > 10);
        Assert.Equal(1, movement.Reseeds);
        return;

        ScriptedFrame Script(uint frame) => new(top with { Z = top.Z - (3.0 * Math.Max(0, (int)(frame - Climb))) });
    }

    [Fact]
    public void Climbing_off_the_top_follows_the_client_counted_once_for_at_most_the_follow_cap()
    {
        const uint Climb = Start + 2;
        Vector3D near = Feet(LadderParameters.TopExitHeight(s_line.Height) - 2);
        PlayerMovement movement = new(Start, near, 0, s_world, double.PositiveInfinity);
        movement.Climb(Climb, s_ladder, topEntry: false, attached: true);
        Dictionary<uint, CharacterState> states = Run(movement, _ => new ScriptedFrame(near, Up, FireBlocked: true), Climb + 100);

        CharacterState exit = states.Values.First(state => state.LadderFollowedTicks == 1);
        int longest = states.Values.Max(state => state.LadderFollowedTicks);
        Assert.Equal(60, longest);
        Assert.Equal(0, states[states.Values.First(state => state.LadderFollowedTicks == longest).Frame + 1].LadderFollowedTicks);
        Assert.InRange(exit.Position.Z - near.Z, -1, 1);
        Assert.Equal(1, movement.Reseeds);
    }

    [Fact]
    public void Jumping_off_follows_the_client_while_it_is_locked_in_the_jump_counted_once()
    {
        const uint Climb = Start + 2;
        const uint Jump = Climb + 5;
        Vector3D feet = Feet(200);
        PlayerMovement movement = new(Start, feet, 0, s_world, double.PositiveInfinity);
        movement.Climb(Climb, s_ladder, topEntry: false, attached: true);
        Dictionary<uint, CharacterState> states = Run(movement,
            frame => new ScriptedFrame(feet, Buttons: frame == Jump ? MovementButtons.Jump : MovementButtons.None, FireBlocked: frame is > Jump and <= Jump + 10),
            Climb + 40);

        Assert.Equal(1, states[Jump].LadderFollowedTicks);
        Assert.Null(states[Jump].Ladder);
        // The jump press keeps the client unsettled for a few ticks, and it stays fire-blocked to the tenth.
        Assert.Equal(10, states.Values.Max(state => state.LadderFollowedTicks));
        Assert.Equal(1, movement.Reseeds);
    }

    [Fact]
    public void A_climb_off_the_reports_continues_from_them_counted_once_and_falls_off_far_from_the_line()
    {
        const uint Climb = Start + 2;
        const uint Push = Climb + 10;
        Vector3D feet = Feet(200);
        (double sin, double cos) = Math.SinCos(s_line.Yaw);
        Vector3D away = feet with { X = feet.X - (70 * sin), Y = feet.Y + (70 * cos) };
        PlayerMovement movement = new(Start, feet, 0, s_world);
        movement.Climb(Climb, s_ladder, topEntry: false, attached: true);
        Dictionary<uint, int> reseeds = [];
        Dictionary<uint, CharacterState> states = Run(movement, frame => new ScriptedFrame(frame > Push ? away : feet), Push + 3,
            packet => reseeds[packet] = movement.Reseeds);

        Assert.True(states[Push - 1].OnLadder);
        Assert.True(states[Push].LadderDiverged);
        Assert.False(states[Push + 1].OnLadder);
        Assert.Equal(0, reseeds[Push]);
        Assert.Equal(1, reseeds[Push + 2]);
    }

    private static Vector3D Feet(double height) => s_line.Origin with { Z = s_line.Origin.Z + LadderParameters.LineStartHeight + height };

    private static Dictionary<uint, CharacterState> Run(PlayerMovement movement, Func<uint, ScriptedFrame> script, uint last, Action<uint>? delivered = null) =>
        ScriptedClient.Deliver(movement, script, ScriptedClient.InOrder(Start + 3, last + 1), delivered).ToDictionary(state => state.Frame);

    private static IEnumerable<CharacterState> Range(Dictionary<uint, CharacterState> states, uint first, uint last) =>
        states.Where(entry => entry.Key >= first && entry.Key <= last).Select(entry => entry.Value);

    private static double Speed(Dictionary<uint, CharacterState> states, uint from, uint to) =>
        (states[to].Position.Z - states[from].Position.Z) / ((to - from) * PawnBlackboard.TickSeconds);
}
