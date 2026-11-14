using Ares.Raid.World.Movement;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Game.Movement;
using Ares.Structs.Game.World;

namespace Ares.Raid.Tests.World.Movement;

public sealed class RopeSwingMovementTests
{
    private const double Tick = 1.0 / 30;
    private static readonly double s_halfHeight = DamPlayerStarts.Capsule.HalfHeight;
    private static readonly Vector3D s_pivot = new(0, 0, 3000);
    private static readonly RopeSwingInput s_idle = new(0, 0, 0);

    [Fact]
    public void A_swing_starts_only_under_an_anchor_high_enough_above_the_rope_end()
    {
        GrappleRope rope = new(s_pivot, 1000);
        Vector3D falling = new(0, 0, -400);
        Assert.True(RopeSwingMovement.CanStart(rope, Feet(At(45, 1001)), s_halfHeight, falling, RopeSwingGround.None, RoughMovementMode.Air, false));
        Assert.False(RopeSwingMovement.CanStart(rope, Feet(At(75, 1001)), s_halfHeight, falling, RopeSwingGround.None, RoughMovementMode.Air, true));
        Assert.False(RopeSwingMovement.CanStart(rope, Feet(At(45, 1001)), s_halfHeight, falling, RopeSwingGround.None, RoughMovementMode.Air, true, 0));
        Assert.False(RopeSwingMovement.CanStart(rope.End(SwingParameters.ReleasedEndReason), Feet(At(45, 1001)), s_halfHeight, falling,
            RopeSwingGround.None, RoughMovementMode.Air, true));

        RopeSwingGround floor = new(0, true, new Vector3D(0, 0, 1));
        Assert.False(RopeSwingMovement.CanStart(rope, Feet(At(30, 900)), s_halfHeight, default, floor, RoughMovementMode.Ground, true));
        Assert.True(RopeSwingMovement.CanStart(rope, Feet(At(5, 900)), s_halfHeight, default, floor, RoughMovementMode.Ground, true));
    }

    [Fact]
    public void A_falling_character_on_a_slack_rope_swings_only_with_room_under_the_arc()
    {
        GrappleRope rope = new(s_pivot, 1000);
        Vector3D feet = Feet(At(30, 600));
        Vector3D falling = new(0, 0, -100);
        double drop = Math.Max(600, 200) - (600 * Math.Cos(30 * Math.PI / 180)) + 50;
        Assert.True(RopeSwingMovement.CanStart(rope, feet, s_halfHeight, falling, new RopeSwingGround(drop + 1, false, default), RoughMovementMode.Air, false));
        Assert.False(RopeSwingMovement.CanStart(rope, feet, s_halfHeight, falling, new RopeSwingGround(drop - 1, false, default), RoughMovementMode.Air, false));
        Assert.False(RopeSwingMovement.CanStart(rope, feet, s_halfHeight, new Vector3D(0, 0, 10), RopeSwingGround.None, RoughMovementMode.Air, false));
    }

    [Fact]
    public void Starting_takes_the_rope_path_and_the_outward_speed_and_a_ground_climb_jumps()
    {
        Vector3D velocity = new(0, 0, -800);
        RoughMovementMode mode = RoughMovementMode.Air;
        (RopeSwingState state, GrappleRope rope) = RopeSwingMovement.Begin(new GrappleRope(s_pivot, 2000), Feet(At(0, 700)), s_halfHeight, ref velocity,
            ref mode, false, false, out double cost);
        Assert.Equal(700, state.RopeLength, 3);
        Assert.Equal(700, rope.WorkingLength, 3);
        Assert.Equal(500, state.CurrentReelSpeed);
        Assert.Equal(SwingParameters.RopeCatchStaminaCost, cost);
        Assert.Equal(SwingPhase.Idle, state.Phase);
        Assert.True(state.SwingingForward);

        Vector3D still = default;
        RoughMovementMode ground = RoughMovementMode.Ground;
        (RopeSwingState climb, GrappleRope shortRope) = RopeSwingMovement.Begin(new GrappleRope(s_pivot, 2000), Feet(At(0, 100)), s_halfHeight, ref still,
            ref ground, true, true, out double free);
        Assert.Equal(150, shortRope.WorkingLength);
        Assert.True(climb.GroundClimbEntry);
        Assert.Equal(450, still.Z);
        Assert.Equal(RoughMovementMode.Air, ground);
        Assert.Equal(0, free);
    }

    [Fact]
    public void A_free_swing_never_rises_above_its_release_height_and_keeps_to_the_rope()
    {
        (RopeSwingState state, GrappleRope rope, Vector3D feet, Vector3D velocity) = Released(60, 1000);
        double start = RopeEnd(feet).Z;
        List<double> heights = [start];
        double longest = 0;
        HashSet<SwingPhase> phases = [];
        for (int tick = 0; tick < 600; tick++)
        {
            RopeSwingStep step = RopeSwingMovement.Step(state, rope, s_idle, feet, velocity, s_halfHeight, RopeSwingGround.None, Tick);
            Assert.Equal(RopeSwingExit.None, step.Exit);
            (state, rope, feet, velocity) = (step.State, step.Rope, step.Position, step.Velocity);
            heights.Add(RopeEnd(feet).Z);
            longest = Math.Max(longest, Distance(RopeEnd(feet), s_pivot));
            phases.Add(state.Phase);
            Assert.False(state.RopeOverloaded);
        }

        Assert.True(longest < 1000 + 40, $"longest {longest}");
        List<double> peaks = [.. Enumerable.Range(1, heights.Count - 2).Where(i => heights[i] >= heights[i - 1] && heights[i] > heights[i + 1]).Select(i => heights[i])];
        Assert.True(peaks.Count >= 2, $"{peaks.Count} peaks");
        Assert.All(peaks, peak => Assert.True(peak <= start + 1e-6, $"peak {peak} above {start}"));
        for (int i = 2; i < peaks.Count; i++)
        {
            Assert.True(peaks[i] <= peaks[i - 2] + 0.5, $"peak {i} {peaks[i]} rose over {peaks[i - 2]}");
        }

        Assert.Contains(SwingPhase.Descending, phases);
        Assert.Contains(SwingPhase.Ascending, phases);
        Assert.Contains(SwingPhase.Reversal, phases);
        Assert.Equal(1000, rope.WorkingLength, 3);
    }

    [Fact]
    public void Steering_along_the_descent_pumps_the_swing()
    {
        (RopeSwingState idleState, GrappleRope idleRope, Vector3D idleFeet, Vector3D idleVelocity) = Released(20, 1000);
        (RopeSwingState state, GrappleRope rope, Vector3D feet, Vector3D velocity) = (idleState, idleRope, idleFeet, idleVelocity);
        bool kicked = false;
        for (int tick = 0; tick < 12; tick++)
        {
            RopeSwingStep idle = RopeSwingMovement.Step(idleState, idleRope, s_idle, idleFeet, idleVelocity, s_halfHeight, RopeSwingGround.None, Tick);
            (idleState, idleRope, idleFeet, idleVelocity) = (idle.State, idle.Rope, idle.Position, idle.Velocity);
            RopeSwingInput towardsBottom = new(-1, 0, 0);
            RopeSwingStep pumped = RopeSwingMovement.Step(state, rope, towardsBottom, feet, velocity, s_halfHeight, RopeSwingGround.None, Tick);
            (state, rope, feet, velocity) = (pumped.State, pumped.Rope, pumped.Position, pumped.Velocity);
            kicked |= state.Kicking;
        }

        Assert.True(kicked);
        Assert.True(Math.Abs(velocity.X) > Math.Abs(idleVelocity.X), $"{velocity.X} vs {idleVelocity.X}");
    }

    [Fact]
    public void Holding_fire_climbs_the_rope_at_one_metre_per_second()
    {
        (RopeSwingState state, GrappleRope rope, Vector3D feet, Vector3D velocity) = Released(0, 600);
        RopeSwingInput climb = new(0, 0, 0, FireDown: true);
        RopeSwingStep first = RopeSwingMovement.Step(state, rope, climb, feet, velocity, s_halfHeight, RopeSwingGround.None, Tick);
        Assert.True(first.State.ClimbingUpRope);
        Assert.Equal(-1200 * Tick, first.State.CurrentReelSpeed, 4);
        Assert.Equal(600 - (1200 * Tick * Tick), first.State.RopeLength, 3);
        Assert.NotEqual(0UL, first.Flags & SwingParameters.StaminaBlockFlag);
        Assert.Equal(SwingParameters.ClimbStaminaCostPerSecond * Tick, first.StaminaCost, 9);

        (state, rope, feet, velocity) = (first.State, first.Rope, first.Position, first.Velocity);
        for (int tick = 0; tick < 60; tick++)
        {
            RopeSwingStep step = RopeSwingMovement.Step(state, rope, climb, feet, velocity, s_halfHeight, RopeSwingGround.None, Tick);
            (state, rope, feet, velocity) = (step.State, step.Rope, step.Position, step.Velocity);
        }

        Assert.Equal(-SwingParameters.ClimbSpeed, state.CurrentReelSpeed, 3);
        Assert.InRange(rope.WorkingLength, 600 - 2.05 * SwingParameters.ClimbSpeed, 600 - 1.9 * SwingParameters.ClimbSpeed);
        Assert.InRange(Distance(RopeEnd(feet), s_pivot), rope.WorkingLength - 5, rope.WorkingLength + 5);
    }

    [Fact]
    public void Crouch_rappels_down_and_blocks_crouch_input()
    {
        (RopeSwingState state, GrappleRope rope, Vector3D feet, Vector3D velocity) = Released(0, 600);
        RopeSwingInput rappel = new(0, 0, 0, CrouchDown: true);
        RopeSwingStep first = RopeSwingMovement.Step(state, rope, rappel, feet, velocity, s_halfHeight, RopeSwingGround.None, Tick);
        Assert.True(first.State.Rappelling);
        Assert.Equal(1500 * Tick, first.State.CurrentReelSpeed, 4);
        Assert.Equal(9, first.CrouchBlockTicks);
        Assert.NotEqual(0UL, first.Flags & SwingParameters.StaminaBlockFlag);
        (state, rope, feet, velocity) = (first.State, first.Rope, first.Position, first.Velocity);
        for (int tick = 0; tick < 30; tick++)
        {
            RopeSwingStep step = RopeSwingMovement.Step(state, rope, rappel, feet, velocity, s_halfHeight, RopeSwingGround.None, Tick);
            (state, rope, feet, velocity) = (step.State, step.Rope, step.Position, step.Velocity);
        }

        Assert.Equal(SwingParameters.RappelSpeed, state.CurrentReelSpeed, 3);
        Assert.True(rope.WorkingLength > 700, $"{rope.WorkingLength}");
    }

    [Fact]
    public void A_jump_lets_go_with_the_input_push_and_a_lift()
    {
        (RopeSwingState state, GrappleRope rope, Vector3D feet, Vector3D velocity) = Released(0, 600);
        RopeSwingInput jump = new(1, 0, Math.PI / 2, JumpPressed: true);
        RopeSwingStep plain = RopeSwingMovement.Step(state, rope, new RopeSwingInput(1, 0, Math.PI / 2), feet, velocity, s_halfHeight, RopeSwingGround.None, Tick);
        RopeSwingStep step = RopeSwingMovement.Step(state, rope, jump, feet, velocity, s_halfHeight, RopeSwingGround.None, Tick);
        Assert.Equal(RopeSwingExit.JumpOff, step.Exit);
        Assert.True(step.Finished);
        Assert.False(step.Rope.Anchored);
        Assert.Equal(SwingParameters.ReleasedEndReason, step.Rope.EndReason);
        Assert.Equal(SwingPhase.Detaching, step.State.Phase);
        Assert.NotEqual(0UL, step.Flags & SwingParameters.JumpStartedFlag);
        Assert.Equal(plain.Velocity.Y + 200, step.Velocity.Y, 9);
        Assert.Equal(plain.Velocity.Z + 500, step.Velocity.Z, 9);
        Assert.Equal(Math.PI / 2, step.Yaw);
    }

    [Fact]
    public void Landing_on_walkable_ground_ends_the_swing_with_slack()
    {
        (RopeSwingState state, GrappleRope rope, Vector3D feet, Vector3D velocity) = Released(10, 800);
        RopeSwingGround floor = new(20, true, new Vector3D(0, 0, 1));
        RopeSwingStep step = RopeSwingMovement.Step(state, rope, s_idle, feet, velocity, s_halfHeight, floor, Tick);
        Assert.Equal(RopeSwingExit.Landed, step.Exit);
        Assert.True(step.Finished);
        Assert.False(step.ActionLayerWritten);
        Assert.Null(step.Yaw);
        Assert.Equal(Distance(RopeEnd(feet), s_pivot) + 30, step.Rope.WorkingLength, 3);
        Assert.True(step.Rope.Anchored);

        RopeSwingGround steep = new(20, true, new Vector3D(0.8, 0, 0.6));
        Assert.Equal(RopeSwingExit.None, RopeSwingMovement.Step(state, rope, s_idle, feet, velocity, s_halfHeight, steep, Tick).Exit);
    }

    [Fact]
    public void A_rope_stretched_past_breaking_snaps()
    {
        (RopeSwingState state, GrappleRope _, Vector3D feet, Vector3D velocity) = Released(0, 1000);
        GrappleRope shortened = new(s_pivot, 100);
        RopeSwingStep step = RopeSwingMovement.Step(state with { RopeLength = 100 }, shortened, s_idle, feet, velocity, s_halfHeight, RopeSwingGround.None, Tick);
        Assert.Equal(RopeSwingExit.RopeBroke, step.Exit);
        Assert.Equal(SwingParameters.BrokenEndReason, step.Rope.EndReason);
        Assert.False(step.Rope.Anchored);
        Assert.True(step.Finished);
    }

    [Fact]
    public void A_lost_anchor_ends_the_swing_keeping_the_velocity()
    {
        (RopeSwingState state, GrappleRope rope, Vector3D feet, _) = Released(30, 1000);
        Vector3D velocity = new(300, 0, -50);
        RopeSwingStep step = RopeSwingMovement.Step(state, rope.End(SwingParameters.ReleasedEndReason), s_idle, feet, velocity, s_halfHeight, RopeSwingGround.None, Tick);
        Assert.Equal(RopeSwingExit.Unanchored, step.Exit);
        Assert.Equal(velocity, step.Velocity);
        Assert.True(step.Finished);
    }

    [Fact]
    public void Phases_pass_through_a_reversal_and_hang_still_only_without_steering()
    {
        Assert.Equal(SwingPhase.Reversal, RopeSwingMovement.NextPhase(SwingPhase.Descending, 300, 400, -0.3, false));
        Assert.Equal(SwingPhase.Ascending, RopeSwingMovement.NextPhase(SwingPhase.Reversal, 300, 400, -0.3, false));
        Assert.Equal(SwingPhase.Reversal, RopeSwingMovement.NextPhase(SwingPhase.Ascending, 50, 400, 0.3, true));
        Assert.Equal(SwingPhase.Idle, RopeSwingMovement.NextPhase(SwingPhase.Descending, 150, 60, 0.1, true));
        Assert.Equal(SwingPhase.Descending, RopeSwingMovement.NextPhase(SwingPhase.Descending, 150, 60, 0.1, false));
        Assert.Equal(SwingPhase.Idle, RopeSwingMovement.NextPhase(SwingPhase.Idle, 390, 160, 0.1, true));
        Assert.Equal(SwingPhase.Descending, RopeSwingMovement.NextPhase(SwingPhase.Idle, 410, 160, 0.1, true));
    }

    // A character hanging on a just-taut rope at an angle from the vertical, at rest.
    private static (RopeSwingState State, GrappleRope Rope, Vector3D Feet, Vector3D Velocity) Released(double degrees, double length)
    {
        Vector3D velocity = default;
        RoughMovementMode mode = RoughMovementMode.Air;
        Vector3D feet = Feet(At(degrees, length));
        (RopeSwingState state, GrappleRope rope) = RopeSwingMovement.Begin(new GrappleRope(s_pivot, length), feet, s_halfHeight, ref velocity, ref mode, false,
            false, out _);
        return (state, rope, feet, velocity);
    }

    // The rope end at an angle from the vertical below the pivot, in the X/Z plane.
    private static Vector3D At(double degrees, double length)
    {
        double radians = degrees * Math.PI / 180;
        return new Vector3D(s_pivot.X + (length * Math.Sin(radians)), s_pivot.Y, s_pivot.Z - (length * Math.Cos(radians)));
    }

    private static Vector3D Feet(Vector3D ropeEnd) => ropeEnd with { Z = ropeEnd.Z - (2 * s_halfHeight) };

    private static Vector3D RopeEnd(Vector3D feet) => RopeSwingMovement.RopeEnd(feet, s_halfHeight);

    private static double Distance(Vector3D a, Vector3D b) =>
        Math.Sqrt(((a.X - b.X) * (a.X - b.X)) + ((a.Y - b.Y) * (a.Y - b.Y)) + ((a.Z - b.Z) * (a.Z - b.Z)));
}
