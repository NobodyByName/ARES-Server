using Ares.Raid.World.Movement;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Game.Collision;

namespace Ares.Raid.Tests.World.Movement;

public sealed class GrapplingHookMovementTests
{
    private const double Tick = 1.0 / 30;
    private const double HalfHeight = 92;
    private static readonly Vector3D s_origin = new(0, 0, 100);

    [Fact]
    public void A_throw_flies_along_the_aim_lifted_and_carried_by_the_ground_velocity()
    {
        Vector3D launch = GrapplingHookMovement.LaunchVelocity(new Vector3D(1, 0, 0), new Vector3D(30, -12, 0));
        Assert.Equal(new Vector3D(2190, -12, 450), launch);
        GrappleHookState hook = GrapplingHookMovement.Throw(40, new Vector3D(10, 20, 150), new Vector3D(0, 1, 0), default);
        Assert.True(hook.Thrown);
        Assert.True(hook.Committed);
        Assert.Equal(40, hook.CreatedTick);
        Assert.Equal(new Vector3D(0, 2160, 450), hook.Velocity);
    }

    [Fact]
    public void In_flight_half_gravity_slows_the_rise_before_the_hook_moves()
    {
        GrappleHookState hook = GrapplingHookMovement.Throw(0, s_origin, new Vector3D(1, 0, 0), default);
        GrappleFlightStep step = GrapplingHookMovement.Fly(hook, 1, s_origin, HalfHeight, false, Tick, static (_, _) => null);
        Assert.Equal(GrappleFlightOutcome.Flying, step.Outcome);
        const double Vz = 450 - (1964 * 0.5 * Tick);
        Assert.Equal(Vz, step.State.Velocity.Z, 9);
        Assert.Equal(100 + (Vz * Tick), step.State.Position.Z, 9);
        Assert.Equal(2160 * Tick, step.State.Position.X, 9);
    }

    [Fact]
    public void A_wall_within_reach_takes_the_hook_one_centimetre_off_its_surface()
    {
        GrappleHookState hook = GrapplingHookMovement.Throw(0, s_origin, new Vector3D(1, 0, 0), default);
        GrappleHit wall = new(new Vector3D(50, 0, 110), new Vector3D(-1, 0, 0), PhysicalSurfaceType.Concrete);
        GrappleFlightStep step = GrapplingHookMovement.Fly(hook, 1, s_origin, HalfHeight, false, Tick, (_, _) => wall);
        Assert.Equal(GrappleFlightOutcome.Attached, step.Outcome);
        Assert.True(step.State.Attached);
        Assert.True(step.State.HasTarget);
        Assert.Equal(new Vector3D(49, 0, 110), step.State.Position);
        Assert.Equal(PhysicalSurfaceType.Concrete, step.State.Surface);
    }

    [Fact]
    public void Arc_shields_and_hits_out_of_reach_are_misses_that_retract_without_the_delay()
    {
        GrappleHookState hook = GrapplingHookMovement.Throw(0, s_origin, new Vector3D(1, 0, 0), default) with { Position = new Vector3D(1990, 0, 100) };
        GrappleHit far = new(new Vector3D(2050, 0, 100), new Vector3D(-1, 0, 0));
        GrappleFlightStep beyond = GrapplingHookMovement.Fly(hook, 20, s_origin, HalfHeight, true, Tick, (_, _) => far);
        Assert.Equal(GrappleFlightOutcome.Missed, beyond.Outcome);
        Assert.Equal(20 - 6, beyond.State.RetractStartTick);
        Assert.Equal(hook.Position, beyond.State.RetractStartLocation);
        Assert.False(beyond.State.Committed);

        GrappleHit shield = new(new Vector3D(200, 0, 100), new Vector3D(-1, 0, 0), IsArcShield: true);
        GrappleFlightStep shielded = GrapplingHookMovement.Fly(hook with { Position = s_origin }, 3, s_origin, HalfHeight, true, Tick, (_, _) => shield);
        Assert.Equal(GrappleFlightOutcome.Missed, shielded.Outcome);
        Assert.Equal(0, shielded.State.RetractStartTick);
    }

    [Fact]
    public void The_rope_end_holds_the_hook_and_a_grounded_owner_misses_once_it_falls_below_the_capsule_top()
    {
        GrappleHookState hook = new(Thrown: true, CreatedTick: 0, Position: new Vector3D(1995, 0, 100), Velocity: new Vector3D(600, 0, -300));
        GrappleFlightStep airborne = GrapplingHookMovement.Fly(hook, 10, s_origin, HalfHeight, true, Tick, static (_, _) => null);
        Assert.Equal(GrappleFlightOutcome.Flying, airborne.Outcome);
        Assert.Equal(2000, Distance(airborne.State.Position, s_origin), 9);
        Vector3D outward = Unit(Subtract(airborne.State.Position, s_origin));
        Assert.True(Dot(outward, airborne.State.Velocity) <= 1e-9);

        GrappleFlightStep grounded = GrapplingHookMovement.Fly(hook, 10, s_origin, HalfHeight, false, Tick, static (_, _) => null);
        Assert.Equal(GrappleFlightOutcome.Missed, grounded.Outcome);
    }

    [Fact]
    public void A_hook_that_flies_two_seconds_is_a_miss()
    {
        GrappleHookState hook = GrapplingHookMovement.Throw(100, s_origin, new Vector3D(0, 0, 1), default) with { Velocity = new Vector3D(0, 0, 400) };
        Assert.Equal(GrappleFlightOutcome.Flying, GrapplingHookMovement.Fly(hook, 159, s_origin, HalfHeight, false, Tick, static (_, _) => null).Outcome);
        Assert.Equal(GrappleFlightOutcome.Missed, GrapplingHookMovement.Fly(hook, 160, s_origin, HalfHeight, false, Tick, static (_, _) => null).Outcome);
    }

    [Fact]
    public void A_player_deflects_the_hook_by_fifteen_to_thirty_degrees_keeping_most_of_its_speed()
    {
        Vector3D velocity = new(1000, 0, 0);
        Vector3D grazing = GrapplingHookMovement.DeflectVelocity(velocity, Unit(new Vector3D(-0.05, 1, 0)), 15, 30, 0.9);
        Assert.Equal(900, Length(grazing), 9);
        Assert.Equal(15, Angle(velocity, grazing), 6);
        Vector3D steep = GrapplingHookMovement.DeflectVelocity(velocity, Unit(new Vector3D(-1, 1, 0)), 15, 30, 0.9);
        Assert.Equal(30, Angle(velocity, steep), 6);
        Assert.Equal(velocity, GrapplingHookMovement.DeflectVelocity(velocity, new Vector3D(1, 0, 0), 15, 30, 0.9));

        GrappleHookState hook = GrapplingHookMovement.Throw(0, s_origin, new Vector3D(1, 0, 0), default);
        GrappleHit player = new(new Vector3D(60, 0, 114), Unit(new Vector3D(-1, 0.2, 0)), IsPlayer: true);
        GrappleFlightStep step = GrapplingHookMovement.Fly(hook, 1, s_origin, HalfHeight, true, Tick, (_, _) => player);
        Assert.Equal(GrappleFlightOutcome.Flying, step.Outcome);
        Assert.True(step.State.DeflectedOffPlayer);
        Assert.Equal(0.9 * Length(hook.Velocity with { Z = hook.Velocity.Z - (1964 * 0.5 * Tick) }), Length(step.State.Velocity), 6);
    }

    [Fact]
    public void A_retract_waits_then_travels_back_and_puts_the_hook_away()
    {
        GrappleHookState hook = GrapplingHookMovement.Retract(new GrappleHookState(Thrown: true, Attached: true, Position: new Vector3D(900, 0, 600)), 30, false);
        Assert.Equal(30, hook.RetractStartTick);
        Assert.True(hook.Retracting);
        Assert.False(hook.Attached);
        Vector3D home = new(0, 0, 150);
        Assert.Equal(hook.Position, GrapplingHookMovement.RetractStep(hook, 35, home).Position);
        GrappleHookState halfway = GrapplingHookMovement.RetractStep(hook, 36 + 4, home);
        Assert.Equal(900 - (900 * ((10 * Tick) - 0.2) / 0.3), halfway.Position.X, 3);
        GrappleHookState stowed = GrapplingHookMovement.RetractStep(hook, 45, home);
        Assert.Equal(GrappleHookState.Stowed, stowed);
        Assert.Equal(-1, stowed.CreatedTick);
        Assert.False(stowed.Thrown);
    }

    [Fact]
    public void The_ground_reel_holds_the_rope_path_and_shortens_it_to_two_metres()
    {
        double held = -1;
        Assert.Equal(2000, GrapplingHookMovement.HeldRopeLength(false, 700, 10, ref held));
        Assert.Equal(-1, held);
        Assert.Equal(700 - (200 * Tick), GrapplingHookMovement.HeldRopeLength(true, 700, 200 * Tick, ref held), 9);
        held = 201;
        Assert.Equal(200, GrapplingHookMovement.HeldRopeLength(true, 700, 200 * Tick, ref held));
        Assert.Equal(1, GrapplingHookMovement.ReelSpeedScale(new Vector3D(1, 0, 0), new Vector3D(0, 1, 0)));
        Assert.Equal(0.5, GrapplingHookMovement.ReelSpeedScale(new Vector3D(-1, -1, 0), new Vector3D(0, 1, 0)));
        Assert.Equal(1, GrapplingHookMovement.ReelSpeedScale(default, new Vector3D(0, 1, 0)));
    }

    private static Vector3D Subtract(Vector3D a, Vector3D b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);

    private static double Dot(Vector3D a, Vector3D b) => (a.X * b.X) + (a.Y * b.Y) + (a.Z * b.Z);

    private static double Length(Vector3D a) => Math.Sqrt(Dot(a, a));

    private static double Distance(Vector3D a, Vector3D b) => Length(Subtract(a, b));

    private static Vector3D Unit(Vector3D a)
    {
        double length = Length(a);
        return new Vector3D(a.X / length, a.Y / length, a.Z / length);
    }

    private static double Angle(Vector3D a, Vector3D b) => Math.Acos(Math.Clamp(Dot(Unit(a), Unit(b)), -1, 1)) * 180 / Math.PI;
}
