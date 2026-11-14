using Ares.Raid.World.Movement;
using Ares.Structs.Game.Movement;

namespace Ares.Raid.Tests.World.Movement;

public sealed class HeldItemMovementTests
{
    private const double Tick = 1.0 / 30;
    private static readonly HeldItemInput s_rifle = new(HeldItemMovementParameters.Default) { Weapon = true, EnablesZoom = true, ZoomAllowed = true };

    [Fact]
    public void Holding_the_aim_control_aims_and_releasing_it_ends_the_aim()
    {
        HeldItemState held = HeldItemMovement.Step(default, s_rifle with { SecondaryDown = true }, 1, Tick);
        Assert.True(held.Zoom);
        Assert.True(held.ZoomStarted);
        held = HeldItemMovement.Step(held, s_rifle with { SecondaryDown = true, SecondaryDownBefore = true }, 1 + Tick, Tick);
        Assert.True(held.Zoom);
        Assert.False(held.ZoomStarted);
        held = HeldItemMovement.Step(held, s_rifle with { SecondaryDownBefore = true }, 1 + (2 * Tick), Tick);
        Assert.False(held.Zoom);
        Assert.Equal(1 + (2 * Tick), held.ZoomEndedAt);
    }

    [Fact]
    public void A_toggle_press_flips_the_aim_and_a_held_toggle_keeps_it()
    {
        HeldItemState held = HeldItemMovement.Step(default, s_rifle with { ToggleZoomDown = true }, 1, Tick);
        Assert.True(held.Zoom);
        held = HeldItemMovement.Step(held, s_rifle with { ToggleZoomDown = true, ToggleZoomDownBefore = true }, 1 + Tick, Tick);
        Assert.True(held.Zoom);
        held = HeldItemMovement.Step(held, s_rifle with { ToggleZoomDownBefore = true }, 1 + (2 * Tick), Tick);
        Assert.True(held.Zoom);
        held = HeldItemMovement.Step(held, s_rifle with { ToggleZoomDown = true }, 1 + (3 * Tick), Tick);
        Assert.False(held.Zoom);
    }

    [Theory]
    [InlineData(false, true, false)]
    [InlineData(true, false, false)]
    [InlineData(true, true, true)]
    public void Aiming_needs_an_item_that_enables_it_and_allows_it_now(bool enables, bool allowed, bool zoom)
    {
        HeldItemInput input = s_rifle with { SecondaryDown = true, EnablesZoom = enables, ZoomAllowed = allowed };
        Assert.Equal(zoom, HeldItemMovement.Step(default, input, 1, Tick).Zoom);
    }

    [Fact]
    public void An_equip_change_ends_the_aim_and_a_forcing_item_aims_without_the_control()
    {
        HeldItemState aiming = new() { Zoom = true };
        Assert.False(HeldItemMovement.Step(aiming, s_rifle with { SecondaryDown = true, Changing = true }, 1, Tick).Zoom);
        Assert.True(HeldItemMovement.Step(default, s_rifle with { ForcingZoom = true }, 1, Tick).Zoom);
    }

    [Fact]
    public void The_retrigger_delay_holds_off_a_new_aim_after_one_ends()
    {
        HeldItemInput input = s_rifle with { Item = HeldItemMovementParameters.Default with { ZoomRetriggerDelaySeconds = 0.5 }, SecondaryDown = true };
        HeldItemState ended = new() { ZoomEndedAt = 1 };
        Assert.False(HeldItemMovement.Step(ended, input, 1.4, Tick).Zoom);
        Assert.True(HeldItemMovement.Step(ended, input, 1.6, Tick).Zoom);
    }

    [Fact]
    public void Only_an_item_that_walks_on_secondary_use_forces_a_walk_while_aiming()
    {
        HeldItemInput aim = s_rifle with { SecondaryDown = true };
        Assert.False(HeldItemMovement.Step(default, aim, 1, Tick).ForceWalk);
        HeldItemInput walking = aim with { Item = HeldItemMovementParameters.Default with { WalkOnSecondaryUse = true } };
        Assert.True(HeldItemMovement.Step(default, walking, 1, Tick).ForceWalk);
    }

    [Fact]
    public void An_item_that_walks_on_primary_use_forces_a_walk_while_its_use_fires()
    {
        HeldItemInput input = new(HeldItemMovementParameters.Default with { WalkOnPrimaryUse = true }) { FireDown = true, AllowPrimary = true };
        Assert.True(HeldItemMovement.Step(default, input, 1, Tick).ForceWalk);
        Assert.False(HeldItemMovement.Step(default, input with { FireBlocked = true }, 1, Tick).ForceWalk);
    }

    [Fact]
    public void A_use_blocks_sprinting_for_the_one_byte_timer_and_it_counts_down_without_shots()
    {
        // 0.35 s + 0.33 s is 408 time units; the timer keeps its low byte.
        Assert.Equal(152, HeldItemMovement.NextUseSprintBlockTimer(0, s_rifle with { Fired = true }, Tick));
        Assert.Equal(132, HeldItemMovement.NextUseSprintBlockTimer(0, s_rifle with { BeingUsed = true }, Tick));
        Assert.Equal(112, HeldItemMovement.NextUseSprintBlockTimer(132, s_rifle, Tick));
        Assert.Equal(0, HeldItemMovement.NextUseSprintBlockTimer(12, s_rifle, Tick));
        Assert.Equal(0, HeldItemMovement.NextUseSprintBlockTimer(132, s_rifle with { Fired = true, PrimaryStartAllowed = false }, Tick));
        HeldItemInput free = s_rifle with { Item = HeldItemMovementParameters.Default with { BlockSprintDuringPrimaryUse = false }, Fired = true };
        Assert.Equal(0, HeldItemMovement.NextUseSprintBlockTimer(0, free, Tick));
    }

    [Fact]
    public void A_running_reload_blocks_sprinting_unless_the_sprint_came_after_its_press()
    {
        HeldItemState reloading = new() { Reloading = true, ReloadBlocksSprint = true, ReloadPressedAt = 2 };
        Assert.False(HeldItemMovement.AllowsSprint(reloading, 1, sprintPressed: false));
        Assert.True(HeldItemMovement.AllowsSprint(reloading, 3, sprintPressed: false));
        Assert.True(HeldItemMovement.AllowsSprint(reloading, 1, sprintPressed: true));
        Assert.True(HeldItemMovement.AllowsSprint(reloading with { ReloadBlocksSprint = false }, 1, sprintPressed: false));
        Assert.False(HeldItemMovement.AllowsSprint(new HeldItemState { UseSprintBlockTimer = 20 }, null, sprintPressed: true));
        Assert.False(HeldItemMovement.AllowsSprint(new HeldItemState { ForceWalk = true }, null, sprintPressed: true));
    }

    [Theory]
    [InlineData(0.5, true, false)]
    [InlineData(0.55, true, true)]
    [InlineData(2, false, false)]
    public void Sprinting_past_half_a_second_blocks_a_running_reload(double sprintSeconds, bool reloading, bool blocked) =>
        Assert.Equal(blocked, HeldItemMovement.BlocksReload(sprintSeconds, reloading));

    [Fact]
    public void Aiming_with_a_walking_item_walks_at_aiming_speed_refuses_sprint_and_sets_the_tags()
    {
        CharacterSimulation simulation = new(default);
        HeldItemInput aim = s_rifle with { Item = HeldItemMovementParameters.Default with { WalkOnSecondaryUse = true }, SecondaryDown = true };
        MovementInput movement = new(1, 126, 0, MovementButtons.Sprint);
        CharacterState state = default;
        for (uint frame = 1; frame <= 60; frame++)
        {
            state = simulation.StepOnGround(new CharacterInput(frame, 0), movement with { Frame = frame }, 0, Tick, GroundMovementParameters.Armed,
                steerAboveMaximum: true, fixBrakingOvershoot: true, sprint: SprintMovementParameters.Armed,
                heldItem: aim with { SecondaryDownBefore = frame > 1 });
        }

        Assert.NotEqual(LocoMode.Sprinting, state.Locomotion);
        Assert.Equal(GroundMovementParameters.Aiming.MaximumSpeed, state.Velocity.X, 0);
        Assert.NotEqual(0UL, state.GraphFlags & GraphStateTags.AbilityZooming);
    }

    [Fact]
    public void Without_a_held_item_the_ground_step_keeps_no_held_item_state()
    {
        CharacterSimulation simulation = new(default);
        CharacterState state = simulation.StepOnGround(new CharacterInput(1, 0), new MovementInput(1, 126, 0, MovementButtons.Sprint), 0, Tick,
            GroundMovementParameters.Armed, steerAboveMaximum: true, fixBrakingOvershoot: true, sprint: SprintMovementParameters.Armed);
        Assert.Null(state.HeldItem);
        Assert.Equal(LocoMode.Sprinting, state.Locomotion);
        Assert.Equal(0UL, state.GraphFlags & GraphStateFlags.HeldItem);
    }
}
