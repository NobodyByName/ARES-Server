using Ares.Raid.World.Movement;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Game.Movement;

namespace Ares.Raid.Tests.World.Movement;

public sealed class AirMotionTests
{
    // One step of a reported position.
    private const double Step = 2 * PawnBlackboard.PositionRange / (1 << 21);
    private const double FallPerTick = AirMovementParameters.FallAcceleration * PawnBlackboard.TickSeconds * PawnBlackboard.TickSeconds;

    [Fact]
    public void A_fall_counts_until_the_height_stops_dropping_faster()
    {
        bool[] unsettled = Unsettled(Fall(ticks: 10, landed: 6));

        Assert.All(unsettled[..10], Assert.True);
        // The landing shows two ticks late.
        Assert.Equal([true, true, false, false], unsettled[10..14]);
    }

    [Fact]
    public void A_landing_lasts_while_the_client_blocks_firing_after_it()
    {
        bool[] unsettled = Unsettled(Fall(ticks: 25, landed: 50), fireBlocked: tick => tick is >= 24 and < 55);

        Assert.All(unsettled[..55], Assert.True);
        Assert.Equal([true, true, false, false], unsettled[55..59]);
    }

    [Fact]
    public void A_client_that_keeps_blocking_fire_after_a_landing_is_not_waited_for()
    {
        bool[] unsettled = Unsettled(Fall(ticks: 25, landed: 100), fireBlocked: _ => true);

        Assert.False(unsettled[^1]);
    }

    [Fact]
    public void Blocked_firing_on_the_ground_is_not_a_landing()
    {
        Assert.All(Unsettled([.. Enumerable.Repeat(0.0, 20)], fireBlocked: _ => true), Assert.False);
    }

    [Fact]
    public void A_jump_counts_from_its_press_to_its_landing()
    {
        List<double> heights = [.. Enumerable.Repeat(0.0, 8)];
        double speed = 21.4;
        while (heights[^1] >= 0)
        {
            heights.Add(heights[^1] + speed);
            speed -= FallPerTick;
        }

        heights[^1] = 0;
        heights.AddRange(Enumerable.Repeat(0.0, 6));
        bool[] unsettled = Unsettled(heights, jumpPressedAt: 1);

        int landing = heights.Count - 7 - 1;
        Assert.False(unsettled[0]);
        Assert.All(unsettled[1..landing], Assert.True);
        Assert.False(unsettled[^1]);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-12.0)]
    [InlineData(9.0)]
    public void Steady_ground_movement_is_not_a_fall(double slope)
    {
        Assert.All(Unsettled([.. Enumerable.Range(0, 40).Select(tick => tick * slope)]), Assert.False);
    }

    [Fact]
    public void A_fall_at_a_steady_high_speed_still_counts()
    {
        Assert.All(Unsettled([.. Enumerable.Range(0, 20).Select(tick => tick * -45.0)]), Assert.True);
    }

    [Fact]
    public void A_jump_press_that_lifts_nothing_runs_out()
    {
        bool[] unsettled = Unsettled([.. Enumerable.Repeat(0.0, 14)], jumpPressedAt: 1);

        Assert.Equal(9, unsettled.Count(value => value));
        Assert.False(unsettled[^1]);
    }

    [Fact]
    public void An_unknown_position_leaves_the_fall_as_it_was()
    {
        AirMotion falling = default(AirMotion).Next(false, false, At(0), At(0), At(-FallPerTick));

        Assert.True(falling.Unsettled);
        Assert.Equal(falling, falling.Next(false, false, At(0), null, At(-3 * FallPerTick)));
    }

    // Whether the character is unsettled in each tick between three reported heights in a row.
    private static bool[] Unsettled(List<double> heights, int? jumpPressedAt = null, Func<int, bool>? fireBlocked = null)
    {
        AirMotion motion = default;
        bool[] unsettled = new bool[heights.Count - 2];
        for (int tick = 0; tick < unsettled.Length; tick++)
        {
            motion = motion.Next(tick == jumpPressedAt, fireBlocked?.Invoke(tick) ?? false, At(heights[tick]), At(heights[tick + 1]), At(heights[tick + 2]));
            unsettled[tick] = motion.Unsettled;
        }

        return unsettled;
    }

    // Heights of a character standing, falling freely for some ticks and standing where it landed.
    private static List<double> Fall(int ticks, int landed)
    {
        List<double> heights = [0, 0];
        double speed = 0;
        for (int tick = 0; tick < ticks; tick++)
        {
            speed += FallPerTick;
            heights.Add(heights[^1] - speed);
        }

        heights.AddRange(Enumerable.Repeat(heights[^1], landed));
        return heights;
    }

    // A position as the client reports it: on the grid of its input field.
    private static Vector3D At(double height) => new(0, 0, Math.Floor(height / Step) * Step);
}
