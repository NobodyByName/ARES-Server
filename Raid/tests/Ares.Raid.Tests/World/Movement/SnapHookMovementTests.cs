using Ares.Raid.World.Movement;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Game.Movement;

namespace Ares.Raid.Tests.World.Movement;

public sealed class SnapHookMovementTests
{
    private const double Tick = 1.0 / 30;
    private const int TickTime = 20;
    private static readonly SnapHookGround s_high = new(500, false);
    private static readonly SnapHookInput s_idle = new(default, false, false);

    [Fact]
    public void The_item_blueprint_shortens_the_range_and_sets_the_cost()
    {
        Assert.Equal(1800, SnapHookItem.Range);
        Assert.Equal(60, SnapHookItem.ActivationCost);
        Assert.Equal(TickTime, SnapHookThrow.TimeUnits(Tick));
        Assert.Equal(210, SnapHookThrow.TimeUnits(SnapHookParameters.PullDelaySeconds));
    }

    [Fact]
    public void Gadget_power_pays_for_a_throw_and_recharges()
    {
        Assert.True(SnapHookThrow.CanThrow(SnapHookHook.Stowed, 60));
        Assert.False(SnapHookThrow.CanThrow(SnapHookHook.Stowed, 59.9));
        Assert.True(SnapHookThrow.CanThrow(SnapHookHook.Stowed, 0, infinitePower: true));
        Assert.False(SnapHookThrow.CanThrow(new SnapHookHook(Thrown: true), 100));
        Assert.Equal(40, SnapHookThrow.PowerAfterThrow(100));
        Assert.Equal(0, SnapHookThrow.PowerAfterThrow(50));
        Assert.Equal(40.15, SnapHookThrow.RechargePower(40, Tick), 9);
        Assert.Equal(100, SnapHookThrow.RechargePower(100, Tick));
        Assert.Equal(40, SnapHookThrow.RechargePower(40, Tick, blocked: true));
    }

    [Fact]
    public void A_throw_traces_the_range_along_the_aim()
    {
        Vector3D from = default;
        Vector3D to = default;
        SnapHookHook miss = SnapHookThrow.Throw(100, new Vector3D(0, 0, 150), new Vector3D(10, 0, 150), (a, b) =>
        {
            (from, to) = (a, b);
            return null;
        });

        Assert.Equal(new Vector3D(0, 0, 150), from);
        Assert.Equal(new Vector3D(1800, 0, 150), to);
        Assert.True(miss.Thrown);
        Assert.False(miss.HasTarget);
        Assert.Equal(to, miss.AttachPoint);
        Assert.Equal(100, miss.ThrownAt);

        SnapHookHook hit = SnapHookThrow.Throw(100, default, new Vector3D(0, 1, 0), (_, _) => new GrappleHit(new Vector3D(0, 600, 0), new Vector3D(0, -1, 0)));
        Assert.True(hit.HasTarget);
        Assert.Equal(new Vector3D(0, 600, 0), hit.AttachPoint);
    }

    [Fact]
    public void A_hit_attaches_on_arrival_and_its_target_pulls_after_the_delay()
    {
        Vector3D start = new(0, 0, 80);
        SnapHookHook hook = new(Thrown: true, HasTarget: true, ThrownAt: 0, AttachPoint: new Vector3D(600, 0, 80));

        // The hook flies 2400 cm/s: 560 cm out after 7 ticks, within 100 cm of the hit.
        SnapHookHookStep step = default;
        int tick = 0;
        while (step.Event == SnapHookHookEvent.None)
        {
            tick++;
            step = SnapHookThrow.Tick(hook, tick * TickTime, start, Tick, 0, null, SnapHookPullEnd.World);
        }

        Assert.Equal(7, tick);
        Assert.Equal(SnapHookHookEvent.Attached, step.Event);
        Assert.True(step.Hook.Attached);
        Assert.True(step.Hook.PullsPlayer);
        SnapHookTarget target = Assert.IsType<SnapHookTarget>(step.Target);
        Assert.Equal(140 + 210, target.CreatedAt);
        Assert.Equal(2000, target.Speed, 9);
        Assert.Equal(750, target.AllowedWiggleSpeed);
        Assert.Equal(new Vector3D(480, 0, 80), SnapHookThrow.HookLocation(hook, start, 0.2));

        SnapHookHookStep held = SnapHookThrow.Tick(step.Hook, 400, start, Tick, 0, target, SnapHookPullEnd.World);
        Assert.Equal(SnapHookHookEvent.None, held.Event);
        SnapHookHookStep done = SnapHookThrow.Tick(step.Hook, 420, start, Tick, 0, target.Invalidate(400), SnapHookPullEnd.World);
        Assert.Equal(SnapHookHookEvent.Cleared, done.Event);
        Assert.Equal(0, done.Refund);
        Assert.False(done.Hook.Thrown);
    }

    [Fact]
    public void A_hit_on_a_shield_lets_go_without_a_refund()
    {
        SnapHookHook hook = new(Thrown: true, HasTarget: true, ThrownAt: 0, AttachPoint: new Vector3D(50, 0, 80), OnShield: true);
        SnapHookHookStep step = SnapHookThrow.Tick(hook, TickTime, new Vector3D(0, 0, 80), Tick, 0, null, SnapHookPullEnd.World);
        Assert.Equal(SnapHookHookEvent.Cleared, step.Event);
        Assert.Null(step.Target);
        Assert.Equal(0, step.Refund);
    }

    [Fact]
    public void A_miss_sinks_while_flying_out_then_returns_and_refunds()
    {
        Vector3D start = new(0, 0, 80);
        SnapHookHook hook = new(Thrown: true, ThrownAt: 0, AttachPoint: new Vector3D(1800, 0, 80));
        SnapHookHookStep step = default;
        int tick = 0;
        while (step.Event == SnapHookHookEvent.None)
        {
            tick++;
            step = SnapHookThrow.Tick(hook, tick * TickTime, start, Tick, 0, null, SnapHookPullEnd.World);
            hook = step.Event == SnapHookHookEvent.None ? step.Hook : hook;
        }

        // 22 ticks under 0.75 s sink the end by 0.16 g each; the hook is back after 0.75 + 0.5 s.
        Assert.Equal(38, tick);
        Assert.Equal(80 - (22 * 1964 * 0.16 * Tick), hook.AttachPoint.Z, 9);
        Assert.Equal(SnapHookHookEvent.Cleared, step.Event);
        Assert.Equal(60, step.Refund);
        Assert.Equal(new Vector3D(1800, 0, 80), SnapHookThrow.HookLocation(hook with { AttachPoint = new Vector3D(1800, 0, 80) }, start, 0.75));
        Assert.Equal(new Vector3D(0, 0, 80), SnapHookThrow.HookLocation(hook with { AttachPoint = new Vector3D(1800, 0, 80) }, start, 1.25));
    }

    [Fact]
    public void A_heavy_landing_cancels_the_hook()
    {
        SnapHookHook flying = new(Thrown: true, HasTarget: true, ThrownAt: 0, AttachPoint: new Vector3D(1000, 0, 0));
        SnapHookHookStep step = SnapHookThrow.Tick(flying, TickTime, default, Tick, GraphStateTags.AbilityLandingHeavy, null, SnapHookPullEnd.World);
        Assert.Equal(SnapHookHookEvent.Cancelled, step.Event);
        Assert.Equal(60, step.Refund);
        Assert.False(step.Hook.Cancelled);

        SnapHookHookStep attached = SnapHookThrow.Cancel(flying with { Attached = true });
        Assert.True(attached.Hook.Cancelled);
        Assert.Equal(0, attached.Refund);
    }

    [Fact]
    public void The_pull_moves_the_owner_unless_the_hooked_object_is_light()
    {
        Assert.Equal(2000, SnapHookThrow.PullSpeed(SnapHookPullEnd.World), 9);
        Assert.Equal(2000, SnapHookThrow.PullSpeed(SnapHookPullEnd.Character), 9);
        Assert.Equal(0, SnapHookThrow.PullSpeed(new SnapHookPullEnd(true, false, 194)));
        Assert.Equal(1500, SnapHookThrow.PullSpeed(new SnapHookPullEnd(true, false, 300)), 9);
    }

    [Fact]
    public void The_pull_starts_within_a_second_of_its_target_becoming_valid()
    {
        SnapHookTarget target = new(new Vector3D(1000, 0, 0), 350, 2000, 750);
        Assert.False(SnapHookMovement.CanStart(null, 400));
        Assert.False(SnapHookMovement.CanStart(target, 340));
        Assert.True(SnapHookMovement.CanStart(target, 360));
        Assert.True(SnapHookMovement.CanStart(target, 950));
        Assert.False(SnapHookMovement.CanStart(target, 960));
        Assert.False(SnapHookMovement.CanStart(target, 360, hookHeld: false));
        Assert.False(SnapHookMovement.CanStart(target.Invalidate(355), 360));
    }

    [Fact]
    public void The_velocity_catches_up_with_the_pull_over_half_a_second()
    {
        SnapHookState state = SnapHookMovement.Begin(new SnapHookTarget(new Vector3D(1000, 0, 0), 0, 2000, 750), default);
        Assert.Equal(new Vector3D(1, 0, 0), state.PullDirection);

        SnapHookStep first = SnapHookMovement.Step(state, s_idle, default, default, s_high, TickTime, Tick);
        Assert.Equal(SnapHookExit.None, first.Exit);
        Assert.Equal(5000.0 / 15 * Tick, first.Velocity.X, 9);
        Assert.Equal(0, first.Velocity.Z, 9);
        Assert.Equal(SnapHookParameters.PullingFlags, first.Flags);
        Assert.True(first.HeavyLandingQuery);
        Assert.True(first.ActionLayerWritten);
        Assert.Equal(CharacterStance.InAir, first.Stance);
        Assert.Null(first.Mode);

        SnapHookStep later = SnapHookMovement.Step(state with { Ticks = 14 }, s_idle, default, default, new SnapHookGround(40, true), 300, Tick);
        Assert.Equal(5000 * Tick, later.Velocity.X, 9);
        Assert.False(later.HeavyLandingQuery);
        Assert.Equal(CharacterStance.Stand, later.Stance);
    }

    [Fact]
    public void The_flight_accelerates_to_the_pull_speed()
    {
        Assert.Equal(7500 * Tick, SnapHookMovement.FlyingVelocity(default, new Vector3D(2000, 0, 0), 2000, Tick).X, 9);
        Assert.Equal(2000, SnapHookMovement.FlyingVelocity(new Vector3D(1990, 0, 0), new Vector3D(2000, 0, 0), 2000, Tick).X, 9);
    }

    [Fact]
    public void A_jump_lifts_the_requested_velocity()
    {
        SnapHookState state = new(new SnapHookTarget(new Vector3D(1000, 0, 0), 0, 2000, 750), new Vector3D(1, 0, 0), 20);
        Vector3D moving = new(1000, 0, 0);
        SnapHookStep plain = SnapHookMovement.Step(state, s_idle, default, moving, s_high, 500, Tick);
        SnapHookStep jumping = SnapHookMovement.Step(state, s_idle with { Jump = true }, default, moving, s_high, 500, Tick);
        Assert.Equal(0, plain.Velocity.Z, 9);
        Assert.True(jumping.Velocity.Z > 0);
    }

    [Fact]
    public void The_pull_ends_near_the_target_and_tries_a_vault()
    {
        SnapHookState state = new(new SnapHookTarget(new Vector3D(150, 0, 0), 0, 2000, 750), new Vector3D(1, 0, 0), 20);
        SnapHookStep step = SnapHookMovement.Step(state, s_idle, default, default, s_high, 500, Tick);
        Assert.Equal(SnapHookExit.Arrived, step.Exit);
        Assert.True(step.Finished);
        Assert.Equal(RoughMovementMode.Air, step.Mode);
        Assert.False(step.State.Target.IsActive);
        Assert.Equal(500, step.State.Target.RemovedAt);
        Assert.Equal(-1964 * Tick, step.Velocity.Z, 9);

        SnapHookStep vaulting = SnapHookMovement.Step(state, s_idle, default, default, s_high, 500, Tick, vault: (_, _) => true);
        Assert.True(vaulting.Vaulting);
        Assert.False(vaulting.Finished);
        Assert.Equal(RoughMovementMode.Ground, vaulting.Mode);
    }

    [Fact]
    public void Passing_the_target_cancelling_or_losing_it_ends_the_pull()
    {
        SnapHookState state = new(new SnapHookTarget(new Vector3D(-1000, 0, 0), 0, 2000, 750), new Vector3D(1, 0, 0), 20);
        Assert.Equal(SnapHookExit.Passed, SnapHookMovement.Step(state, s_idle, default, default, s_high, 500, Tick).Exit);

        SnapHookState ahead = state with { Target = state.Target with { Position = new Vector3D(1000, 0, 0) } };
        Assert.Equal(SnapHookExit.Cancelled, SnapHookMovement.Step(ahead, s_idle with { SecondaryPressed = true }, default, default, s_high, 500, Tick).Exit);
        Assert.Equal(SnapHookExit.NotHeld, SnapHookMovement.Step(ahead, s_idle with { HookHeld = false }, default, default, s_high, 500, Tick).Exit);
        SnapHookState removed = ahead with { Target = ahead.Target with { RemovedAt = 480 } };
        Assert.Equal(SnapHookExit.TargetLost, SnapHookMovement.Step(removed, s_idle, default, default, s_high, 500, Tick).Exit);
    }

    [Fact]
    public void A_weak_pull_stalls_unless_the_character_moves_away()
    {
        SnapHookState weak = new(new SnapHookTarget(new Vector3D(1000, 0, 0), 0, 250, 750), new Vector3D(1, 0, 0), 20);
        Assert.Equal(SnapHookExit.Stalled, SnapHookMovement.Step(weak, s_idle, default, default, s_high, 500, Tick).Exit);
        Assert.Equal(SnapHookExit.None, SnapHookMovement.Step(weak, s_idle, default, new Vector3D(-200, 0, 0), s_high, 500, Tick).Exit);

        SnapHookState slow = weak with { Target = weak.Target with { Speed = 2000 }, Ticks = 149 };
        Assert.Equal(SnapHookExit.None, SnapHookMovement.Step(slow, s_idle, default, new Vector3D(50, 0, 0), s_high, 3000, Tick).Exit);
        Assert.Equal(SnapHookExit.Stalled, SnapHookMovement.Step(slow with { Ticks = 150 }, s_idle, default, new Vector3D(50, 0, 0), s_high, 3020, Tick).Exit);
    }

    [Fact]
    public void Blending_out_releases_the_target_without_finishing()
    {
        SnapHookState state = new(new SnapHookTarget(new Vector3D(1000, 0, 0), 0, 2000, 750), new Vector3D(1, 0, 0), 20);
        SnapHookStep step = SnapHookMovement.Step(state, s_idle, default, new Vector3D(300, 0, 0), new SnapHookGround(0, true), 500, Tick, blendingOut: true);
        Assert.Equal(SnapHookExit.BlendingOut, step.Exit);
        Assert.False(step.Finished);
        Assert.False(step.ActionLayerWritten);
        Assert.False(step.State.Target.IsActive);
        Assert.Equal(0, step.Velocity.Z);
    }
}
