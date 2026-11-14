using Ares.Raid.World.Movement;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Game.Movement;
using Ares.Structs.Game.World;

namespace Ares.Raid.Tests.World.Movement;

public sealed class ZiplineMovementTests
{
    private const double Tick = 1.0 / 30;
    private const int Riding = 20;
    private static readonly double s_halfHeight = DamPlayerStarts.Capsule.HalfHeight;

    // A line from (0, 0, 1000) running 2000 cm along +X while dropping 1000 cm (pitch -26.57 degrees), no exit points.
    private static readonly ZiplinePlacement s_placement = new("/Game/Cell", "BP_PlaceableZipline", new Vector3D(0, 0, 1000), new Rotator(0, 0, 0),
        new Vector3D(2000, 0, -1000), []);
    private static readonly ZiplineLine s_line = ZiplineLine.From(s_placement);
    private static readonly Vector3D s_down = s_line.Rotation.Rotate(new Vector3D(1, 0, 0));

    [Fact]
    public void Dam_has_twelve_rideable_lifts_and_five_launchers()
    {
        Assert.Equal(12, DamZiplines.Lines.Count);
        Assert.Equal(5, DamZiplines.Launchers.Count);
        Assert.All(DamZiplines.Lines, placement =>
        {
            ZiplineLine line = ZiplineLine.From(placement);
            Assert.True(line.Rotation.Pitch > 89, $"{placement.Actor} pitch {line.Rotation.Pitch}");
            Assert.InRange(line.Length, 1400, 8700);
            ZiplineExitPoint exit = Assert.Single(placement.ExitPoints);
            Assert.Equal(ZiplineTravelDirection.Away, exit.TravelDirection);
            Assert.True(exit.MoreExact);
            Assert.InRange(exit.Fraction, 0.8, 1);
        });
    }

    [Fact]
    public void The_hands_hang_above_the_capsule_centre()
    {
        Vector3D offset = ZiplineMovement.HangOffset;
        Assert.Equal(94.8175048828125 - 225, offset.Z, 9);
        Assert.Equal(0.0008559224661439657, offset.X, 9);
        Assert.Equal(0, offset.Y, 9);
        Assert.Equal(15, ZiplineMovement.EntryTicks);
    }

    [Fact]
    public void The_riding_direction_comes_from_the_attach_point_then_from_the_camera()
    {
        Vector3D back = new(-1, 0, 0);
        Assert.Equal(ZiplineTravelDirection.Away, ZiplineMovement.Begin(s_line, Centre(100), back).Direction);
        Assert.Equal(ZiplineTravelDirection.Home, ZiplineMovement.Begin(s_line, Centre(s_line.Length - 450), s_down).Direction);
        Assert.Equal(ZiplineTravelDirection.Away, ZiplineMovement.Begin(s_line, Centre(800), s_down).Direction);
        Assert.Equal(ZiplineTravelDirection.Home, ZiplineMovement.Begin(s_line, Centre(800), back).Direction);
        Assert.Equal(ZiplineTravelDirection.Away, ZiplineMovement.Begin(s_line, Centre(s_line.Length - 600), new Vector3D(0, 1, 0)).Direction);
    }

    [Fact]
    public void Being_caught_fades_the_own_velocity_and_pulls_the_hands_onto_the_line()
    {
        ZiplineState state = ZiplineMovement.Begin(s_line, Centre(800), s_down);
        ZiplineStep step = ZiplineMovement.Step(state, Input(), Feet(800) with { Y = 10 }, Scale(s_down, 300), s_halfHeight, Tick);
        Assert.Equal(ZiplineExit.None, step.Exit);
        Assert.False(step.Finished);
        Assert.True(step.HeavyLandingQuery);
        Assert.True(step.State.AllowFallDamage);
        // The own velocity loses 500 cm/s per second; the 10 cm offset across the line is pulled in by a fifth.
        Vector3D along = Scale(s_down, 300 - (500 * Tick));
        AssertClose(along with { Y = -10 * ZiplineParameters.CentringShare / Tick }, step.Velocity, 1e-6);
        Assert.Equal(ZiplineParameters.RidingFlags, step.Flags);
        Assert.Equal(GraphStateTags.GameplayEffectAttachedTraversalZipline, step.Flags & GraphStateTags.GameplayEffectAttachedTraversalZipline);
    }

    [Fact]
    public void An_upward_speed_fades_faster_while_being_caught()
    {
        ZiplineState state = ZiplineMovement.Begin(s_line, Centre(800), s_down);
        ZiplineStep step = ZiplineMovement.Step(state, Input(), Feet(800), new Vector3D(0, 0, 400), s_halfHeight, Tick);
        // Only the part along the line is kept; across it the hands are already on the line.
        const double Rising = 400 - (500 * Tick) - (300 * Tick);
        AssertClose(Scale(s_down, Rising * s_down.Z), step.Velocity, 1e-6);
    }

    [Fact]
    public void Riding_accelerates_with_the_slope_of_the_motion()
    {
        ZiplineState state = Ride(ZiplineTravelDirection.Away);
        ZiplineStep step = ZiplineMovement.Step(state, Input(), Feet(800), Scale(s_down, 100), s_halfHeight, Tick);
        double pitch = Math.Atan2(-1000, 2000) * 180 / Math.PI;
        double acceleration = 750 + ((pitch + 90) / 90 * (200 - 750));
        Assert.Equal(acceleration, ZiplineMovement.AccelerationFromPitch(Scale(s_down, 100)), 9);
        AssertClose(Scale(s_down, 100 + (acceleration * Tick)), step.Velocity, 1e-6);
        Assert.False(step.HeavyLandingQuery);
        Assert.Equal(ZiplineParameters.Acceleration, ZiplineMovement.AccelerationFromPitch(new Vector3D(0, 0, 10)), 9);
        Assert.Equal(ZiplineParameters.MaxAcceleration, ZiplineMovement.AccelerationFromPitch(new Vector3D(0, 0, -10)), 9);
    }

    [Fact]
    public void Riding_away_stops_accelerating_at_the_highest_speed()
    {
        ZiplineStep step = ZiplineMovement.Step(Ride(ZiplineTravelDirection.Away), Input(), Feet(800), Scale(s_down, 3499), s_halfHeight, Tick);
        AssertClose(Scale(s_down, 3500), step.Velocity, 1e-6);
    }

    [Fact]
    public void Moving_against_the_riding_direction_brakes_and_allows_fall_damage()
    {
        ZiplineStep step = ZiplineMovement.Step(Ride(ZiplineTravelDirection.Home), Input(), Feet(800), Scale(s_down, 300), s_halfHeight, Tick);
        double change = -ZiplineMovement.AccelerationFromPitch(s_down) * Tick;
        AssertClose(Scale(s_down, 300 - (1000 * Tick) + change), step.Velocity, 1e-6);
        Assert.True(step.HeavyLandingQuery);
        Assert.True(step.State.AllowFallDamage);
    }

    [Fact]
    public void Riding_past_the_end_leaves_with_the_velocity_clamped()
    {
        Vector3D velocity = Scale(s_down, 1200);
        ZiplineStep step = ZiplineMovement.Step(Ride(ZiplineTravelDirection.Away), Input(), Feet(s_line.Length + 1), velocity, s_halfHeight, Tick);
        Assert.Equal(ZiplineExit.LineEnd, step.Exit);
        Assert.True(step.Finished);
        Assert.True(step.State.StartedDetaching);
        AssertClose(Scale(s_down, ZiplineParameters.MaxEndLaunchVelocity), step.Velocity, 1e-6);
        ZiplineStep home = ZiplineMovement.Step(Ride(ZiplineTravelDirection.Home), Input(), Feet(-1), Scale(s_down, -100), s_halfHeight, Tick);
        Assert.Equal(ZiplineExit.LineEnd, home.Exit);
        AssertClose(Scale(s_down, -100), home.Velocity, 1e-6);
    }

    [Fact]
    public void Hands_past_the_line_end_are_pulled_back_by_a_fifth_per_tick()
    {
        ZiplineStep step = ZiplineMovement.Step(Ride(ZiplineTravelDirection.Home), Input(), Feet(s_line.Length + 30), Scale(s_down, -50), s_halfHeight, Tick);
        Assert.Equal(ZiplineExit.None, step.Exit);
        AssertClose(Scale(s_down, -30 * ZiplineParameters.CentringShare / Tick), step.Velocity, 1e-6);
    }

    [Fact]
    public void A_Dam_lift_launches_the_rider_onto_its_exit_target()
    {
        ZiplinePlacement placement = DamZiplines.Lines[0];
        ZiplineLine line = ZiplineLine.From(placement);
        ZiplineExitPoint exit = placement.ExitPoints[0];
        ZiplineState state = ZiplineMovement.Begin(line, line.World(new Vector3D(100, 0, 0)), new Vector3D(1, 0, 0)) with { Ticks = Riding };
        Assert.Equal(ZiplineTravelDirection.Away, state.Direction);
        Vector3D hands = line.World(new Vector3D((exit.Fraction * line.Length) + 1, 0, 0));
        Vector3D feet = new(hands.X + ZiplineMovement.HangOffset.X, hands.Y + ZiplineMovement.HangOffset.Y, hands.Z + ZiplineMovement.HangOffset.Z - s_halfHeight);
        ZiplineStep step = ZiplineMovement.Step(state, Input(), feet, new Vector3D(0, 0, 900), s_halfHeight, Tick);
        Assert.Equal(ZiplineExit.ExitPoint, step.Exit);
        Assert.True(step.Finished);
        // The arc keeps the exit's horizontal speed and lands on the target under the game's gravity.
        Vector3D target = line.ActorPoint(exit.LaunchTarget);
        double horizontal = Math.Sqrt((step.Velocity.X * step.Velocity.X) + (step.Velocity.Y * step.Velocity.Y));
        Assert.Equal(exit.LaunchSpeed, horizontal, 6);
        double distance = Math.Sqrt(((target.X - feet.X) * (target.X - feet.X)) + ((target.Y - feet.Y) * (target.Y - feet.Y)));
        double time = distance / horizontal;
        double landing = feet.Z + (step.Velocity.Z * time) - (0.5 * AirMovementParameters.FallAcceleration * time * time);
        Assert.Equal(target.Z, landing, 6);
    }

    [Fact]
    public void Jumping_off_adds_the_jump_forces_once_the_rider_is_caught()
    {
        Vector3D velocity = Scale(s_down, 400);
        ZiplineInput jump = Input() with { JumpPressed = true, Direction = InputDirection.Right };
        ZiplineStep riding = ZiplineMovement.Step(Ride(ZiplineTravelDirection.Away), Input(), Feet(800), velocity, s_halfHeight, Tick);
        ZiplineStep step = ZiplineMovement.Step(Ride(ZiplineTravelDirection.Away), jump, Feet(800), velocity, s_halfHeight, Tick);
        Assert.Equal(ZiplineExit.JumpOff, step.Exit);
        Assert.True(step.Finished);
        // Control yaw 0: right is +Y; the rider moves down, so the full vertical force applies.
        AssertClose(new Vector3D(riding.Velocity.X, riding.Velocity.Y + 450, riding.Velocity.Z + 500), step.Velocity, 1e-6);
        ZiplineStep entering = ZiplineMovement.Step(ZiplineMovement.Begin(s_line, Centre(800), s_down), jump, Feet(800), velocity, s_halfHeight, Tick);
        Assert.Equal(ZiplineExit.None, entering.Exit);
    }

    [Fact]
    public void Electrified_or_colliding_riders_let_go()
    {
        ZiplineStep electrified = ZiplineMovement.Step(Ride(ZiplineTravelDirection.Away), Input() with { Electrified = true }, Feet(800), Scale(s_down, 400), s_halfHeight, Tick);
        Assert.Equal(ZiplineExit.Detached, electrified.Exit);
        Assert.True(electrified.Finished);
        ZiplineStep collided = ZiplineMovement.Step(Ride(ZiplineTravelDirection.Away), Input(), Feet(800), Scale(s_down, 400), s_halfHeight, Tick, collided: true);
        Assert.Equal(ZiplineExit.Detached, collided.Exit);
        ZiplineStep caught = ZiplineMovement.Step(ZiplineMovement.Begin(s_line, Centre(800), s_down), Input(), Feet(800), Scale(s_down, 400), s_halfHeight, Tick, collided: true);
        Assert.Equal(ZiplineExit.None, caught.Exit);
    }

    [Fact]
    public void A_fast_fall_onto_a_flat_line_falls_through_until_slow_enough()
    {
        ZiplineState state = ZiplineMovement.Begin(s_line, Centre(800), s_down);
        ZiplineStep fast = ZiplineMovement.Step(state, Input(), Feet(800), new Vector3D(0, 0, -3000), s_halfHeight, Tick);
        Assert.Equal(ZiplineExit.FellThrough, fast.Exit);
        const double Fade = (float)Tick * 5f;
        Assert.Equal(-3000 + (Fade * 3000), fast.Velocity.Z, 9);
        Assert.False(fast.Finished);
        Assert.False(fast.ActionLayerWritten);
        ZiplineStep slower = ZiplineMovement.Step(fast.State, Input(), Feet(800), new Vector3D(0, 0, -1550), s_halfHeight, Tick);
        Assert.True(slower.Finished);
    }

    [Fact]
    public void Being_caught_just_above_a_floor_does_not_move_down()
    {
        ZiplineState state = ZiplineMovement.Begin(s_line, Centre(800), s_down);
        ZiplineStep step = ZiplineMovement.Step(state, Input(), Feet(800) with { Z = Feet(800).Z + 20 }, default, s_halfHeight, Tick, floorDistance: 30);
        Assert.Equal(0, step.Velocity.Z, 9);
        ZiplineStep high = ZiplineMovement.Step(state, Input(), Feet(800) with { Z = Feet(800).Z + 20 }, default, s_halfHeight, Tick, floorDistance: 60);
        Assert.True(high.Velocity.Z < 0);
    }

    private static ZiplineInput Input() => new(InputDirection.None, 0, false);

    private static ZiplineState Ride(ZiplineTravelDirection direction) => new(s_line, direction, Riding);

    // Capsule centre and feet that put the hands on the line, the given distance from its start.
    private static Vector3D Centre(double along)
    {
        Vector3D hands = s_line.World(new Vector3D(along, 0, 0));
        Vector3D offset = ZiplineMovement.HangOffset;
        return new Vector3D(hands.X + offset.X, hands.Y + offset.Y, hands.Z + offset.Z);
    }

    private static Vector3D Feet(double along)
    {
        Vector3D centre = Centre(along);
        return centre with { Z = centre.Z - s_halfHeight };
    }

    private static Vector3D Scale(Vector3D vector, double factor) => new(vector.X * factor, vector.Y * factor, vector.Z * factor);

    private static void AssertClose(Vector3D expected, Vector3D actual, double tolerance)
    {
        Assert.True(Math.Abs(expected.X - actual.X) < tolerance && Math.Abs(expected.Y - actual.Y) < tolerance && Math.Abs(expected.Z - actual.Z) < tolerance,
            $"expected {expected}, got {actual}");
    }
}
