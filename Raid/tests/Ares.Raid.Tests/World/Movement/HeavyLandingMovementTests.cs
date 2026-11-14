using Ares.Raid.World.Movement;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Game.Movement;

namespace Ares.Raid.Tests.World.Movement;

public sealed class HeavyLandingMovementTests
{
    private const double Tick = 1.0 / 30;
    private const double Gravity = -AirMovementParameters.FallAcceleration;
    private static readonly Vector3D s_falling = new(0, 0, -1650);

    [Fact]
    public void A_landing_is_heavy_above_the_height_and_a_recovery_roll_needs_a_press_below_its_limit()
    {
        Assert.False(HeavyLandingMovement.IsHeavy(600));
        Assert.True(HeavyLandingMovement.IsHeavy(600.01));
        Assert.True(HeavyLandingMovement.IsRecoveryRoll(999.99, pressed: true));
        Assert.False(HeavyLandingMovement.IsRecoveryRoll(1000, pressed: true));
        Assert.False(HeavyLandingMovement.IsRecoveryRoll(700, pressed: false));
        Assert.False(HeavyLandingMovement.IsRecoveryRoll(700, pressed: true, blocked: true));
    }

    [Theory]
    [InlineData(690, 0)]
    [InlineData(1200, 0)]
    [InlineData(1200.01, 1)]
    [InlineData(1800.5, 2)]
    [InlineData(3000, 3)]
    public void The_level_is_the_last_one_whose_height_the_fall_exceeds(double fallHeight, int level)
    {
        Assert.Equal(level, HeavyLandingMovement.Level(fallHeight, HeavyLandingAnimations.Heavy));
    }

    [Theory]
    [InlineData(400, 0, "HM_Landing_A_Com_R_HeavyFallFwd_00")]
    [InlineData(-400, 0, "HM_Landing_A_Com_R_HeavyFallBwd_00")]
    [InlineData(0, 400, "HM_Landing_A_Com_R_HeavyFallRight_00")]
    [InlineData(400, 400, "HM_Landing_A_Com_R_HeavyFallRight_00")]
    [InlineData(-400, -400, "HM_Landing_A_Com_R_HeavyFallLeft_00")]
    [InlineData(0, 0, "HM_Landing_A_Com_R_HeavyFallOnSpot_00")]
    public void The_animation_follows_the_impact_direction_relative_to_the_heading(double x, double y, string clip)
    {
        HeavyLandingState state = HeavyLandingMovement.Begin(690, new Vector3D(x, y, -1650), default, 0);
        Assert.Equal(clip, state.Clip.Name);
        Assert.Equal(0, state.Level);
        Assert.Equal(clip, HeavyLandingMovement.Begin(690, new Vector3D(-y, x, -1650), default, Math.PI / 2).Clip.Name);
    }

    [Fact]
    public void High_levels_play_one_animation_in_every_direction()
    {
        Assert.Equal("HM_Landing_A_UA_Pancake_00", HeavyLandingMovement.Begin(1900, new Vector3D(0, 400, -2000), default, 0).Clip.Name);
        Assert.Equal("HM_Landing_A_UA_SuperPancakeGetUp_00", HeavyLandingMovement.Begin(2500, new Vector3D(400, 0, -2000), default, 0).Clip.Name);
        Assert.Equal("HM_Landing_A_UA_SuperHeavyFwd_00", HeavyLandingMovement.Begin(1300, s_falling, default, 0).Clip.Name);
    }

    [Theory]
    [InlineData("HM_Landing_A_Com_R_HeavyFallFwd_00", 30)]
    [InlineData("HM_Landing_A_Com_R_HeavyFallBwd_00", 31)]
    [InlineData("HM_Landing_A_Com_R_HeavyFallLeft_00", 30)]
    [InlineData("HM_Landing_A_Com_R_HeavyFallRight_00", 30)]
    [InlineData("HM_Landing_A_Com_R_HeavyFallOnSpot_00", 30)]
    [InlineData("HM_Landing_A_UA_SuperHeavyFwd_00", 60)]
    [InlineData("HM_Landing_A_UA_Pancake_00", 91)]
    [InlineData("HM_Landing_A_UA_SuperPancakeGetUp_00", 100)]
    [InlineData("HM_Landing_A_R_RecoveryRoll_00", 27)]
    public void The_landing_lasts_until_its_animation_hands_over(string clip, int ticks)
    {
        HeavyLandingClip animation = HeavyLandingAnimations.Heavy.Concat(HeavyLandingAnimations.Recovery)
            .SelectMany(level => new[] { level.NoDirection, level.Forward, level.Left, level.Right, level.Backward })
            .First(candidate => candidate?.Name == clip)!;
        Assert.Equal(ticks, HeavyLandingMovement.Duration(animation));
    }

    [Fact]
    public void Each_step_moves_by_the_root_motion_along_the_heading_and_keeps_the_vertical_speed_on_the_ground()
    {
        HeavyLandingState state = HeavyLandingMovement.Begin(690, new Vector3D(0, 400, -1650), default, Math.PI / 2);
        Assert.Equal("HM_Landing_A_Com_R_HeavyFallFwd_00", state.Clip.Name);
        HeavyLandingStep step = HeavyLandingMovement.Step(state, s_falling, onGround: true, Gravity, Tick);
        Assert.Equal(0, step.Velocity.X, 9);
        Assert.Equal(5.66465855 * 30, step.Velocity.Y, 4);
        Assert.Equal(-1650, step.Velocity.Z);
        Assert.Equal(CharacterStance.Uncontrolled, step.ActionStance);
        Assert.Equal(LocoMode.Still, step.ActionLocomotion);
        Assert.False(step.InAir);
        Assert.False(step.Finished);
        Assert.Equal(1, step.State.Ticks);

        HeavyLandingStep air = HeavyLandingMovement.Step(state, new Vector3D(0, 0, -100), onGround: false, Gravity, Tick);
        Assert.Equal(-100 + (Gravity * Tick), air.Velocity.Z, 9);
        Assert.Equal(CharacterStance.InAir, air.ActionStance);
        Assert.True(air.InAir);
    }

    [Fact]
    public void The_side_animation_moves_to_the_characters_right()
    {
        HeavyLandingState state = HeavyLandingMovement.Begin(690, new Vector3D(0, 400, -1650), default, 0);
        double travelled = 0;
        for (int tick = 0; tick < 30; tick++)
        {
            HeavyLandingStep step = HeavyLandingMovement.Step(state, default, onGround: true, Gravity, Tick);
            state = step.State;
            travelled += step.Velocity.Y * Tick;
        }

        Assert.Equal(-HeavyLandingAnimations.HeavyFallRight.Root[0].Right + HeavyLandingAnimations.HeavyFallRight.Root[30].Right, travelled, 3);
        Assert.True(travelled > 90);
    }

    [Fact]
    public void The_last_step_hands_over_with_the_finished_blend()
    {
        HeavyLandingState state = HeavyLandingMovement.Begin(690, new Vector3D(400, 0, -1650), default, 0);
        for (int tick = 1; tick < 30; tick++)
        {
            HeavyLandingStep step = HeavyLandingMovement.Step(state, default, onGround: true, Gravity, Tick);
            Assert.False(step.Finished);
            state = step.State;
        }

        HeavyLandingStep last = HeavyLandingMovement.Step(state, default, onGround: true, Gravity, Tick);
        Assert.Equal(HeavyLandingTransientState.Finished, last.Exit);
        Assert.Equal(15, last.ExitBlendTicks);
    }

    [Fact]
    public void Falling_fast_again_ends_the_landing_at_once()
    {
        HeavyLandingState state = HeavyLandingMovement.Begin(690, s_falling, default, 0);
        Assert.True(HeavyLandingMovement.Step(state, new Vector3D(0, 0, -1451), onGround: false, Gravity, Tick).Finished);
        Assert.False(HeavyLandingMovement.Step(state, new Vector3D(0, 0, -1449), onGround: false, Gravity, Tick).Finished);
        Assert.False(HeavyLandingMovement.Step(state, new Vector3D(0, 0, -1451), onGround: true, Gravity, Tick).Finished);
    }

    [Fact]
    public void A_recovery_roll_follows_the_velocity_and_leaves_with_the_quick_blend()
    {
        HeavyLandingState state = HeavyLandingMovement.Begin(800, new Vector3D(0, 300, -1500), new Vector3D(0, 300, -1500), 0, recovery: true);
        Assert.Equal("HM_Landing_A_R_RecoveryRoll_00", state.Clip.Name);
        Assert.Equal(Math.Atan2(300, 0), state.Heading, 12);
        Assert.Equal(0, HeavyLandingMovement.Begin(800, s_falling, default, 0, recovery: true).Heading);

        HeavyLandingStep step = default;
        for (int tick = 0; tick < 27; tick++)
        {
            step = HeavyLandingMovement.Step(state, default, onGround: true, Gravity, Tick);
            state = step.State;
        }

        Assert.Equal(HeavyLandingTransientState.QuickExit, step.Exit);
        Assert.Equal(3, step.ExitBlendTicks);
    }
}
