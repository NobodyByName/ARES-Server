using Ares.Raid.World.Movement;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Game.Collision;
using Ares.Structs.Game.Movement;

namespace Ares.Raid.Tests.World.Movement;

public sealed class SlideMovementTests
{
    [Fact]
    public void Slide_entry_needs_a_new_crouch_press_sprint_movement_and_enough_speed()
    {
        MovementInput input = new(1, 126, 0, MovementButtons.Sprint | MovementButtons.Crouch);
        Assert.True(SlideMovement.CanStart(input, 200));
        Assert.False(SlideMovement.CanStart(input, 199));
        Assert.True(SlideMovement.CanStart(input, 150, 0.75));
        Assert.False(SlideMovement.CanStart(input with { Buttons = input.Buttons | MovementButtons.PreviousCrouch }, 650));
        Assert.False(SlideMovement.CanStart(input with { Buttons = MovementButtons.Crouch }, 650));
        Assert.False(SlideMovement.CanStart(input with { Forward = 0 }, 650));
        Assert.False(SlideMovement.CanStart(input with { Forward = 12 }, 650));
        Assert.True(SlideMovement.CanStart(input with { Forward = 13 }, 650));
        Assert.True(SlideMovement.CanStart(input with { Forward = 0, Right = -126 }, 650));
    }

    [Fact]
    public void Cancellation_uses_velocity_relative_input_crouch_speed_and_root_motion()
    {
        Assert.False(SlideMovement.WantsToCancel(-0.7f, true, 150, 150, false));
        Assert.True(SlideMovement.WantsToCancel(-0.71, true, 650, 150, false));
        Assert.True(SlideMovement.WantsToCancel(1, false, 650, 150, false));
        Assert.True(SlideMovement.WantsToCancel(1, true, 149, 150, false));
        Assert.True(SlideMovement.WantsToCancel(1, true, 650, 150, true));
    }

    [Fact]
    public void The_initial_boost_fades_with_time_and_uses_current_speed()
    {
        Vector3D direction = new(0, -1, 0);
        Assert.Equal(new Vector3D(0, -0.5, 0), SlideMovement.InitialBoost(direction, 650, SlideMovementParameters.Default, 0));
        Assert.Equal(new Vector3D(0, -0.25, 0), SlideMovement.InitialBoost(direction, 650, SlideMovementParameters.Default, 0.75));
        Assert.Equal(default, SlideMovement.InitialBoost(direction, 650, SlideMovementParameters.Default, 1.5));
        Assert.Equal(default, SlideMovement.InitialBoost(direction, 650, SlideMovementParameters.Default, 2));
        Assert.Equal(new Vector3D(0, -0.125, 0), SlideMovement.InitialBoost(direction, 325, SlideMovementParameters.Default, 0.75));
    }

    [Fact]
    public void Momentum_follows_current_velocity_instead_of_the_initial_direction()
    {
        Assert.Equal(new Vector3D(-0.25, 0, 0), SlideMovement.Momentum(new Vector3D(-1, 0, 0), 650, SlideMovementParameters.Default));
        Assert.Equal(default, SlideMovement.Momentum(new Vector3D(-1, 0, 0), 0, SlideMovementParameters.Default));
    }

    [Fact]
    public void Slowdown_grows_faster_at_low_speed_and_decreases_on_steep_slopes()
    {
        Assert.Equal(0.15, SlideMovement.SlowdownRate(0, 650, SlideMovementParameters.Default), 10);
        Assert.Equal(0.975, SlideMovement.SlowdownRate(0, 0, SlideMovementParameters.Default), 10);
        Assert.Equal(0.975, SlideMovement.SlowdownRate(0, 100, SlideMovementParameters.Default), 10);
        Assert.Equal(0.075, SlideMovement.SlowdownRate(0.5, 650, SlideMovementParameters.Default), 10);
        Assert.Equal(0.0225, SlideMovement.SlowdownRate(0.85, 650, SlideMovementParameters.Default), 10);
        Assert.Equal(-0.15f, SlideMovement.SlowdownRate(0.851, 650, SlideMovementParameters.Default));
        Assert.Equal(0.3, SlideMovement.SlowdownRate(0, 650, SlideMovementParameters.Default with { FrictionMultiplier = 2 }), 10);
        Assert.Equal(0, SlideMovement.SlowdownRate(0, 650, SlideMovementParameters.Default with { FrictionMultiplier = 0 }));
    }

    [Theory]
    [InlineData(1, SlideMovementParameters.HighFriction)]
    [InlineData(2, SlideMovementParameters.LowFriction)]
    [InlineData(23, SlideMovementParameters.MediumFriction)]
    [InlineData(25, SlideMovementParameters.MediumFriction)]
    [InlineData(38, SlideMovementParameters.VeryLowFriction)]
    [InlineData(0, 1)]
    [InlineData(39, 1)]
    public void Surface_friction_follows_the_surface_types_slide_category(int surfaceType, double friction)
    {
        SlideMovementParameters parameters = SlideMovementParameters.ForSurface((PhysicalSurfaceType)surfaceType);
        Assert.Equal(friction, parameters.FrictionMultiplier);
        Assert.Equal(SlideMovementParameters.Default.MaximumSpeed, parameters.MaximumSpeed);
    }

    [Fact]
    public void Slowdown_accumulates_at_float_precision_and_clamps_at_both_bounds()
    {
        float slowdown = 0;
        for (int tick = 0; tick < 200; tick++)
        {
            slowdown = SlideMovement.AccumulateSlowdown(slowdown, 0, 650, SlideMovementParameters.Default, 1.0f / 30);
        }

        Assert.InRange(slowdown, 0.99999f, 1);
        Assert.Equal(1, SlideMovement.AccumulateSlowdown(slowdown, 0, 650, SlideMovementParameters.Default, 1));
        Assert.Equal(0, SlideMovement.AccumulateSlowdown(0.01f, 1, 650, SlideMovementParameters.Default, 1));
        Assert.Equal(0.4f, SlideMovement.AccumulateSlowdown(0.4f, 0, 650, SlideMovementParameters.Default, 0));
    }
}
